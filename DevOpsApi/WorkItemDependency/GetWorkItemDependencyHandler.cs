using DevOpsApi.Common.Infrastructure.Authentication;
using DevOpsApi.Common.Infrastructure.DevOps;
using DevOpsApi.Common.Settings;
using DevOpsApi.WorkItemDependency.Domain;
using DevOpsApi.WorkItemDependency.Dtos;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using PullRequest = DevOpsApi.WorkItemDependency.Domain.PullRequest;

namespace DevOpsApi.WorkItemDependency;

public class GetWorkItemDependencyHandler
{
	private readonly DevOpsClient _devOpsClient;
	private readonly string _project;

	public GetWorkItemDependencyHandler(DevOpsClient devOpsClient, IOptions<DevOpsSettings> settings)
	{
		_devOpsClient = devOpsClient;
		_project = settings.Value.Project;
	}	

	public async Task<ReportDto> HandleAsync(int id, AuthenticationModel model, IEnumerable<DevOpsWorkItem>? workItems = null, DevOpsWorkItem? workItem = null)
    {
		_devOpsClient.Connect(model);

		switch (true)
		{
			case var _ when workItems != null:
				break;
			case var _ when workItem != null:
				workItems = [workItem];
				break;
			default:
				var itemsToProcess = await GetWorkItems([id]);
				workItems = await GetWorkItemDetails(itemsToProcess.ToList());
				break;
		}
	    
	    workItems = await GetPullRequestDetails(workItems);
	    workItems = await GetBuilds(workItems);
	    workItems = (await GetBuildTimeline(workItems)).ToList();

	    var builds = workItems.SelectMany(i => i.PullRequests.SelectMany(pr => pr.Builds)).DistinctBy(i => new { i.PipelineName, i.BuildNumber }).ToList();

	    var allBuilds = (await GetWorkItemsBuildRelated(builds)).ToList();

	    var report = new ReportDto { WorkItem = workItems.FirstOrDefault(), WorkItems = workItems, Builds = allBuilds };
	    
        return report;
    }

	private async Task<IEnumerable<DevOpsWorkItem>> GetWorkItems(int[] ids)
	{
		var response = await _devOpsClient.WorkItemClient.QueryByWiqlAsync(new Wiql { Query = $"SELECT [System.Id], [System.Title], [System.State] FROM WorkItems WHERE [System.TeamProject] = '{_project}' AND [System.Id] IN ({string.Join(",", ids)}) ORDER BY [System.Id] DESC" } );

		return response.WorkItems.Select(wi => new DevOpsWorkItem(wi.Id));
	}

	private async Task<IEnumerable<DevOpsWorkItem>> GetWorkItemDetails(ICollection<DevOpsWorkItem> items)
	{
		var itemDetails = items.Select(i => _devOpsClient.WorkItemClient.GetWorkItemAsync(_project, i.WorkItemId, expand: WorkItemExpand.All));
		
		var detailTasks = await Task.WhenAll(itemDetails);

		return items.Select(t =>
		{
			var detail = detailTasks.FirstOrDefault(ta => ta.Id == t.WorkItemId);

			t.State = detail.Fields.GetCastedValueOrDefault<string, string>("System.State");
			t.BoardColumnDone = detail.Fields.GetCastedValueOrDefault<string, bool>("System.BoardColumnDone");
			t.BoardColumn = detail.Fields.GetCastedValueOrDefault<string, string>("System.BoardColumn");
			t.Title = detail.Fields.GetCastedValueOrDefault<string, string>("System.Title");
			var relations = detail.Relations?.Where(x => x.Rel.Equals("ArtifactLink", StringComparison.OrdinalIgnoreCase)
				&& x.Attributes["name"].ToString() == "Pull Request") ?? [];

			t.PullRequests.AddRange(relations.Select(r =>
			{
				var split = System.Net.WebUtility.UrlDecode(r.Url).Split('/');
				int.TryParse(split[^1], out var number);
				return new PullRequest { Number = number, ParentWorkItemId = t.WorkItemId };
			}));
			
			return t;
		});
	}

	private async Task<IEnumerable<DevOpsWorkItem>> GetPullRequestDetails(IEnumerable<DevOpsWorkItem> items)
	{
		var output = items.Select(async i =>
		{
			var prTasks = i.PullRequests.Select(pr => _devOpsClient.GitClient.GetPullRequestByIdAsync(_project, pr.Number));

			try
			{
				var prResults = await Task.WhenAll(prTasks);

				var pullTasks = i.PullRequests.Select(async r =>
				{
					var detail = prResults.FirstOrDefault(pr => pr.PullRequestId == r.Number);
					r.RepositoryId = detail.Repository.Id;
					r.RepositoryName = detail.Repository.Name;
					return r;
				}).ToList();
				
				i.PullRequests = await Task.WhenAll(pullTasks);
			}
			catch (Exception _)
			{
				// ignored
			}

			return i;
		});
		
		return await Task.WhenAll(output);
	}

	private async Task<IEnumerable<DevOpsWorkItem>> GetBuilds(IEnumerable<DevOpsWorkItem> items)
	{
		var output = items.Select(async i => {
			var buildTasks = i.PullRequests.DistinctBy(pr => pr.RepositoryId).Select(pr =>
			{
				return _devOpsClient.BuildClient.GetBuildsAsync(_project, repositoryId: pr.RepositoryId.ToString(), queryOrder: BuildQueryOrder.FinishTimeAscending, repositoryType: "TfsGit" );
			});

			var repoBuilds = await Task.WhenAll(buildTasks);

			i.PullRequests = i.PullRequests.Select(pr => {
				var builds = repoBuilds.SelectMany(b => b).Where(b => b.Repository.Id == pr.RepositoryId.ToString())
					.Select(b => new DevOpsBuild()
					{
						BuildNumber = b.BuildNumber,
						Id = b.Id,
						PipelineName = b.Definition.Name,
						Status = b.Status.ToString(),
						QueueTime = b.QueueTime,
						StartTime = b.StartTime,
						FinishTime = b.FinishTime,
						SourceBranch = b.SourceBranch,
						CiMessage = b.TriggerInfo.GetValueOrDefault("ci.message"),
						SourceVersion = b.SourceVersion,
						BuildDefinitionId = b.Definition.Id,
						RepositoryId = b.Repository.Id
					});
	
					pr.Builds = builds.OrderBy(b => b.PipelineName).ThenByDescending(b => b.BuildNumber).ToList();

					return pr;
			}).ToList();
			
			return i;
		});
		
		return await Task.WhenAll(output);
	}

	private async Task<IEnumerable<DevOpsWorkItem>> GetBuildTimeline(IEnumerable<DevOpsWorkItem> items)
	{
		var output = items.Select(async i =>
		{
			var pullRequestBuilds = i.PullRequests.Select(async pr =>
			{
				var timelineTasks = pr.Builds.Select(async b => new { Build = b, Timeline = await _devOpsClient.BuildClient.GetBuildTimelineAsync(_project, b.Id) });
				
				var buildTimeline = await Task.WhenAll(timelineTasks);

				pr.Builds = buildTimeline.Select(bt => {
					bt.Build.Timeline = bt.Timeline.Records.Where(r => r.RecordType.Equals("Stage", StringComparison.OrdinalIgnoreCase))
						.Select(t => new DevOpsTimeline () { Name = t.Name, Order = t.Order, Result = t.Result.ToString(), State = t.State.ToString(), ParentDevOpsBuildId = bt.Build.Id, Identifier = t.Identifier })
						.OrderBy(t => t.Order).ToList();
					return bt.Build;
				}).ToList();
				
				return pr;
			});

			i.PullRequests = await Task.WhenAll(pullRequestBuilds);

			return i;
		});
		
		return await Task.WhenAll(output);
	}

	private async Task<IEnumerable<DevOpsBuild>> GetWorkItemsBuildRelated(IEnumerable<DevOpsBuild> builds)
	{
		var output = builds.Where(b => b.RelatedPRNumber > 0).Select(async b =>
		{
			var workItemTasks = new { Build = b, RelatedItems = await _devOpsClient.BuildClient.GetBuildWorkItemsRefsAsync(_project, b.Id),
			PRRelatedItems = await _devOpsClient.GitClient.GetPullRequestWorkItemRefsAsync(b.RepositoryId, b.RelatedPRNumber) };

			var prRelated = workItemTasks.PRRelatedItems.Select(ri => new DevOpsWorkItem(int.Parse(ri.Id)));

			b.Dependees = workItemTasks.RelatedItems.Select(ri => new DevOpsWorkItem(int.Parse(ri.Id), prRelated.Any(x => x.WorkItemId == int.Parse(ri.Id)))).ToList();
			
			b.Dependees = (await GetWorkItemDetails(b.Dependees)).OrderByDescending(x => x.IsRelated).ToList();
			
			return b;
		}).ToList();

		return await Task.WhenAll(output);
	}

	private async Task<IEnumerable<DevOpsWorkItem>> GetBuildDetails(IEnumerable<DevOpsWorkItem> items)
	{
		var output = items.Select(async x =>
		{
			var prTasks = x.PullRequests.Select(async pr => {

				var buildTasks = pr.Builds.Select(async b =>
				{
					if (!string.IsNullOrEmpty(b.CiMessage) || string.IsNullOrEmpty(b.SourceVersion))
					{
						return b;
					}

					//_buildClient.GetBuildAsync(Project, b
					var commit = await _devOpsClient.GitClient.GetCommitAsync(_project, b.SourceVersion, pr.RepositoryId);
					b.CiMessage = commit.Comment;
					return b;
				});
				
				pr.Builds = await Task.WhenAll(buildTasks);
				return pr;
			});
			
			x.PullRequests = await Task.WhenAll(prTasks);
				
			return x;
		});
		
		return await Task.WhenAll(output);
	}

	private static VssConnection CreateEntraUsernameConnection(string organizationUrl, string username, string password)
	{
		var credentials = new VssAadCredential(username, password);
		return new VssConnection(new Uri(organizationUrl), credentials);
	}
}