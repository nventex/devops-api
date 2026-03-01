using DevOpsApi.ReleaseDocumentGeneration.Dtos;
using DevOpsApi.WorkItemDependency.Domain;

namespace DevOpsApi.ReleaseDocumentGeneration;

public class CreateReleaseDocumentHandler
{
	public ReleaseDocumentDto Handle(ICollection<DevOpsWorkItem> items)
	{
		var readyToReleaseWorkItems = items.Where(x => x.IsReadyForRelease).ToList();
		var readyToReleaseIds = readyToReleaseWorkItems.Select(x => x.WorkItemId).ToHashSet();
		var releasePipeline = new Dictionary<string, DevOpsBuild>();
		
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

		return new ReleaseDocumentDto
		{
			Pipelines = releasePipeline.Select(x => new ReleasePipelineDto { BuildId = x.Value.Id, PipelineName = x.Value.PipelineName } ),
			WorkItems = readyToReleaseWorkItems.Select(x => new ReleaseWorkItemDto
				{ Id = x.WorkItemId, Title = x.Title })
		};
	}
}