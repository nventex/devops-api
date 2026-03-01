// using System.Net;
// using DevOpsApi.Common.Infrastructure.Authentication;
// using DevOpsApi.Common.Infrastructure.DevOps;
// using DevOpsApi.Common.Settings;
// using DevOpsApi.ReleaseDocumentGeneration.Domain;
// using DevOpsApi.WorkItemDependency.Domain;
// using Microsoft.Extensions.Options;
// using Microsoft.TeamFoundation.Core.WebApi.Types;
// using Microsoft.TeamFoundation.Wiki.WebApi;
// using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
// using Microsoft.VisualStudio.Services.Common;
//
// namespace DevOpsApi.ReleaseDocumentGeneration;
//
// public class CreateReleaseDocumentHandler
// {
// 	private readonly DevOpsClient _devOpsClient;
//
// 	private readonly string _project;
//
// 	public CreateReleaseDocumentHandler(DevOpsClient devOpsClient, IOptions<DevOpsSettings> settings)
// 	{
// 		_devOpsClient = devOpsClient;
// 		_project = settings.Value.Project;
// 	}
//
// 	public async Task<IEnumerable<DevOpsWorkItem>> HandleAsync(int? sprint, AuthenticationModel model, CancellationToken cancellationToken)
// 	{
// 		await _devOpsClient.Connect(model);
// 	    
// 	    var workItems = await GetWorkItems(sprint);
// 	    
// 	    return await GetWorkItemDetails(workItems.ToList());
//     }
//
// 	private async Task<IEnumerable<DevOpsWorkItem>> GetWorkItems(int? sprint)
// 	{
// 		var sprintName = $"Sprint {sprint}";
// 		
// 		if (sprint is 0 or null)
// 		{
// 			var iterations = await _devOpsClient.WorkClient.GetTeamIterationsAsync(new TeamContext(_project), "current");
// 			sprintName = iterations.FirstOrDefault()?.Name;
// 		}
//
// 		var response = await _devOpsClient.WorkItemClient.QueryByWiqlAsync(new Wiql { Query = $"SELECT [System.Id], [System.Title], [System.State] FROM WorkItems WHERE [System.TeamProject] = '{_project}' AND [System.WorkItemType] IN ('Bug', 'User Story') AND [System.IterationPath] == '{_project}\\{sprintName}' AND [System.State] IN ('Active', 'In QA', 'Passed QA', 'Resolved') ORDER BY [System.State] DESC" } );
//
// 		return response.WorkItems.Select(wi => new DevOpsWorkItem(wi.Id));
// 	}
// 	
// 	private async Task<IEnumerable<DevOpsWorkItem>> GetWorkItemDetails(ICollection<DevOpsWorkItem> items)
// 	{
// 		var pageExists = true;
// 		var wikiId = "Operations-Platform.wiki";
// 		var content = await ConvertToMarkdown(document);
// 		var param = new WikiPageCreateOrUpdateParameters() { Content = content };
// 		var page = new WikiPageResponse();
// 		
// 		try
// 		{
// 			page = await _devOpsClient.WikiClient.GetPageAsync(_project, wikiId, $"/{_project}/Product Documentation/Releases/{ReleaseDate} Production Release: Ops Platform");
// 		}
// 		catch (VssServiceException ex)
// 		{
// 			pageExists = false;			
// 		}
// 		
// 		if (pageExists)
// 		{
// 			await _devOpsClient.WikiClient.UpdatePageByIdAsync(param, _project, wikiId, page.Page.Id.Value, page.ETag.ToList()[0]);
// 			return;
// 		}
//
// 		await _devOpsClient.WikiClient.CreateOrUpdatePageAsync(param, _project, wikiId, $"/{_project}/Product Documentation/Releases/{ReleaseDate} Production Release: Ops Platform", string.Empty);
// 	}
//
// 	private static async Task<string> ConvertToMarkdown(ReleaseDocument document)
// 	{
// 		var httpClient = new HttpClient();
//
// 		var content = await httpClient.GetStringAsync("http://localhost:8080/release.md");
//
// 		var services = string.Join(Environment.NewLine, document.Steps.DistinctBy(s => s.ServiceName).Select(s => $"- {s.ServiceName}"));
// 		var workItems = string.Join(Environment.NewLine, document.Items.Select(i => $"- #{i.Id}"));
// 		var steps = string.Join(Environment.NewLine, document.Steps.Select(i => $"{i.PipelineLink}"));
//
// 		content = content.Replace("<%services%>", services).Replace("<%items%>", workItems).Replace("<%steps%>", steps);
// 	
// 		return content;
// 	}
// }