using DevOpsApi.WorkItemDependency.Domain;
using DevOpsApi.WorkItemDependency.Dtos;

namespace DevOpsApi.WorkItemDependency;

public class GetWorkItemsDependencyHandler
{
    public async Task<IEnumerable<WorkItemDto>> HandleAsync(IEnumerable<DevOpsWorkItem> items)
    {
        var workItemGraph = new List<WorkItemDto>();
        
        foreach (var workItem in items)
        {
            var topLevelWorkItem = new WorkItemDto { WorkItemId = workItem.WorkItemId, State = workItem.State, 
                BoardColumn = workItem.BoardColumn, Title = workItem.Title, BoardColumnDone = workItem.BoardColumnDone };
            
            var workItemNumber = workItem.WorkItemId;
            
            var repos = workItem.PullRequests.DistinctBy(x => x.RepositoryId);
            var builds = repos.SelectMany(r => r.Builds).OrderByDescending(b => b.QueueTime);

            foreach (var build in builds)
            {
                var related = build.Dependees.FirstOrDefault(d => d.IsRelated);

                if (related == null)
                    continue;
                
                if (related?.WorkItemId == workItemNumber)
                {
                    topLevelWorkItem.HasRelatedPrWorkItem = true;
                    topLevelWorkItem.PipelineNames.Add(build.PipelineName);
                }
                else if (topLevelWorkItem.HasRelatedPrWorkItem && new[] { "Active", "In QA" }.Contains(related.State, StringComparer.OrdinalIgnoreCase))
                {
                    topLevelWorkItem.DependsOn.Add(new WorkItemDto { WorkItemId = related.WorkItemId, State = related.State, 
                        BoardColumnDone = related.BoardColumnDone, BoardColumn = related.BoardColumn, Title = related.Title, PipelineNames = [build.PipelineName] });
                }
                
                if (topLevelWorkItem.DependsOn.Any(d => d.WorkItemId == related.WorkItemId))
                {
                    topLevelWorkItem.DependsOn.FirstOrDefault(d => d.WorkItemId == related.WorkItemId).PipelineNames.Add(build.PipelineName);
                }
            }
            
            workItemGraph.Add(topLevelWorkItem);
        }

        return await Task.FromResult(workItemGraph);
    }
}