using System.Net;
using DevOpsApi.Common.Infrastructure.Authentication;
using DevOpsApi.Common.Infrastructure.DevOps;
using DevOpsApi.Common.Settings;
using DevOpsApi.WorkItemDependency.Domain;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;

namespace DevOpsApi.WorkItemDependency;

public class GetWorkItemsHandler
{
	private readonly DevOpsClient _devOpsClient;

	private readonly string _project;

	public GetWorkItemsHandler(DevOpsClient devOpsClient, IOptions<DevOpsSettings> settings)
	{
		_devOpsClient = devOpsClient;
		_project = settings.Value.Project;
	}

	public async Task<IEnumerable<DevOpsWorkItem>> HandleAsync(int? sprint, AuthenticationModel model, CancellationToken cancellationToken)
	{
		await _devOpsClient.Connect(model);
	    
	    var workItems = await GetWorkItems(sprint);
	    
	    return await GetWorkItemDetails(workItems.ToList());
    }

	private async Task<IEnumerable<DevOpsWorkItem>> GetWorkItems(int? sprint)
	{
		var sprintName = $"Sprint {sprint}";
		
		if (sprint is 0 or null)
		{
			var iterations = await _devOpsClient.WorkClient.GetTeamIterationsAsync(new TeamContext(_project), "current");
			sprintName = iterations.FirstOrDefault()?.Name;
		}

		var response = await _devOpsClient.WorkItemClient.QueryByWiqlAsync(new Wiql { Query = $"SELECT [System.Id], [System.Title], [System.State] FROM WorkItems WHERE [System.TeamProject] = '{_project}' AND [System.WorkItemType] IN ('Bug', 'User Story') AND [System.IterationPath] == '{_project}\\{sprintName}' AND [System.State] IN ('Active', 'In QA', 'Passed QA', 'Resolved') ORDER BY [System.State] DESC" } );

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
				var split = WebUtility.UrlDecode(r.Url)?.Split('/');
				int.TryParse(split[^1], out var number);
				return new PullRequest { Number = number, ParentWorkItemId = t.WorkItemId };
			}));
			
			return t;
		});
	}	
}