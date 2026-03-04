using System.Reflection;
using DevOpsApi.Common.Infrastructure.Authentication;
using DevOpsApi.Common.Infrastructure.DevOps;
using DevOpsApi.Common.Settings;
using DevOpsApi.ReleaseDocumentGeneration.Dtos;
using DevOpsApi.WorkItemDependency.Domain;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Wiki.WebApi;
using Microsoft.VisualStudio.Services.Common;

namespace DevOpsApi.ReleaseDocumentGeneration;

public class CreateReleaseDocumentHandler
{
	private readonly DevOpsClient _devOpsClient;
	private readonly DevOpsSettings _settings;

	public CreateReleaseDocumentHandler(DevOpsClient devOpsClient, IOptions<DevOpsSettings> settings)
	{
		_devOpsClient = devOpsClient;
		_settings = settings.Value;
	}
	
	public async Task Handle(ICollection<DevOpsWorkItem> items, AuthenticationModel model)
	{
		var readyToReleaseWorkItems = items.Where(x => x.IsReadyForRelease).ToList();
		var readyToReleaseIds = readyToReleaseWorkItems.Select(x => x.WorkItemId).ToHashSet();
		var releasePipeline = new Dictionary<string, DevOpsBuild>();
		var sprint = items.Max(x => x.Sprint);
		
		foreach (var workItem in items)
		{
			var repos = workItem.PullRequests.DistinctBy(x => x.RepositoryId);
			var builds = repos.SelectMany(r => r.Builds).OrderByDescending(b => b.QueueTime);
			
			foreach (var build in builds)
			{
				if (readyToReleaseIds.Contains(build.Dependees.FirstOrDefault(x => x.IsRelated)?.WorkItemId ?? 0))
				{
					releasePipeline.TryAdd(build.PipelineName, build);
				}
			}
		}

		var dto = new ReleaseDocumentDto
		{
			Sprint = sprint,
			Pipelines = releasePipeline.Select(x => new ReleasePipelineDto { BuildId = x.Value.Id, PipelineName = x.Value.PipelineName, BuildNumber = x.Value.BuildNumber } ),
			WorkItems = readyToReleaseWorkItems.Select(x => new ReleaseWorkItemDto
				{ Id = x.WorkItemId, Title = x.Title })
		};
		
		await Create(model, dto);
	}
	
	private async Task Create(AuthenticationModel model, ReleaseDocumentDto dto)
	{
		await _devOpsClient.Connect(model);
		
		var pageExists = true;
		var wikiId = "Operations-Platform.wiki";
		var content = ConvertToMarkdown(dto);
		var param = new WikiPageCreateOrUpdateParameters { Content = content };
		var page = new WikiPageResponse();
		
		try
		{
			page = await _devOpsClient.WikiClient.GetPageAsync(_settings.Project, wikiId, $"/{_settings.Project}/Product Documentation/Releases/{dto.Sprint} Production Release: Ops Platform");
		}
		catch (VssServiceException ex)
		{
			pageExists = false;			
		}
		
		if (pageExists)
		{
			await _devOpsClient.WikiClient.UpdatePageByIdAsync(param, _settings.Project, wikiId, page.Page.Id ?? 0, page.ETag.ToList()[0]);
			return;
		}

		await _devOpsClient.WikiClient.CreateOrUpdatePageAsync(param, _settings.Project, wikiId, $"/{_settings.Project}/Product Documentation/Releases/{dto.Sprint} Production Release: Ops Platform", string.Empty);
	}

	private static string ConvertToMarkdown(ReleaseDocumentDto document)
	{
		var content = GetFile();

		var services = string.Join(Environment.NewLine, document.Repositories.Select(r => $"- {r}"));
		var workItems = string.Join(Environment.NewLine, document.WorkItems.Select(i => $"- #{i.Id}"));
		var steps = string.Join(Environment.NewLine, document.Pipelines.Select(i => $"1. {i.PipelineName} - [Pipelines - Run {i.BuildNumber}]({i.PipelineLink})"));

		content = content.Replace("<%services%>", services).Replace("<%items%>", workItems).Replace("<%steps%>", steps).Replace("<%date%>", DateTime.Now.ToString("yyyy-MM-dd"));
	
		return content;
	}

	private static string GetFile()
	{
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DevOpsApi.Common.release.md");
		var streamReader = new StreamReader(stream);
		return streamReader.ReadToEnd();
	}
}