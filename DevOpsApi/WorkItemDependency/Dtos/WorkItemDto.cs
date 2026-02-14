using System.Diagnostics.CodeAnalysis;

namespace DevOpsApi.WorkItemDependency.Dtos;

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

    public HashSet<string> PipelineNames { get; set; } = [];

    public string Pipelines => string.Join(", ", PipelineNames);

    public bool HasRelatedPrWorkItem { get; set; }

    public bool IsReadyForRelease => !State.Equals("Closed", StringComparison.OrdinalIgnoreCase) && (BoardColumn?.Contains("Ready for Release") ?? false || State.Equals("Ready for Release", StringComparison.OrdinalIgnoreCase));

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return (obj is WorkItemDto item ? item : default).WorkItemId == WorkItemId;
    }
}