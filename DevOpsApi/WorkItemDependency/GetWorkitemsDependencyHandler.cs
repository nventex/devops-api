using System.Diagnostics.CodeAnalysis;
using DevOpsApi.WorkItemDependency.Domain;

namespace DevOpsApi.WorkItemDependency;

public class GetWorkItemsDependencyHandler
{
    public async Task<IEnumerable<WorkItemDto>> HandleAsync(IEnumerable<DevOpsWorkItem> items)
    {
        var workItemGraph = new List<WorkItemDto>();
        
        foreach (var workItem in items)
        {
            var topLevelWorkItem = new WorkItemDto { WorkItemId = workItem.WorkItemId, State = workItem.State, BoardColumn = workItem.BoardColumn, Title = workItem.Title };
            var workItemNumber = workItem.WorkItemId;
            
            var repos = workItem.PullRequests.DistinctBy(x => x.RepositoryId);
            var builds = repos.SelectMany(r => r.Builds).OrderByDescending(b => b.QueueTime);

            foreach (var build in builds)
            {
                var related = build.Dependees.FirstOrDefault(d => d.IsRelated);
                
                if (related?.WorkItemId == workItemNumber)
                {
                    topLevelWorkItem.HasRelatedPrWorkItem = true;
                }
                else if (topLevelWorkItem.HasRelatedPrWorkItem && (related?.State.Equals("Active", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    topLevelWorkItem.DependsOn.Add(new WorkItemDto { WorkItemId = related.WorkItemId, State = related.State, 
                        BoardColumnDone = related.BoardColumnDone, BoardColumn = related.BoardColumn, Title = related.Title });
                }
            }
            
            workItemGraph.Add(topLevelWorkItem);
        }

        return await Task.FromResult(workItemGraph);
    }
}

public struct WorkItemDto
{
    public WorkItemDto()
    {
    }
    
    public int WorkItemId { get; set; }

    public string State { get; set; }

    public string Title { get; set; }

    public bool BoardColumnDone { get; set; }
    
    public string BoardColumn { get; set; }
    
    public string BoardStatus => $"{BoardColumn} {(BoardColumnDone ? "Done" : "Doing")}";

    public HashSet<WorkItemDto> DependsOn { get; set; } = [];

    public bool HasRelatedPrWorkItem { get; set; }

    public bool IsReadyForRelease => !State.Equals("Closed", StringComparison.OrdinalIgnoreCase) && (BoardColumn?.Contains("Ready for Release") ?? false || State.Equals("Ready for Release", StringComparison.OrdinalIgnoreCase));

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return (obj is WorkItemDto item ? item : default).WorkItemId == WorkItemId;
    }
}