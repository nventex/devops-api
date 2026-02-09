using System.Text.Json.Serialization;

namespace DevOpsApi.WorkItemDependency.Domain;

public abstract class WorkItemBase
{
    [JsonIgnore]
    public string BoardColumn { get; set; }

    [JsonIgnore]
    public bool BoardColumnDone { get; internal set; }

    public string BoardStatus => $"{BoardColumn} {(BoardColumnDone ? "Done" : "Doing")}";

    public int Id { get; set; }

    public string Sprint { get; set; }

    public string State { get; set; }

    public string Title { get; set; }

    public bool IsRelated { get; set; }
	
    public bool IsReadyForRelease => !State.Equals("Closed", StringComparison.OrdinalIgnoreCase) && (BoardColumn?.Contains("Ready for Release") ?? false || State.Equals("Ready for Release", StringComparison.OrdinalIgnoreCase));

    public WorkItemBase()
    {
    }
}