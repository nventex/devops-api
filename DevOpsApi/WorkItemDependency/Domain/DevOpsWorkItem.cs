using System.Text.Json.Serialization;

namespace DevOpsApi.WorkItemDependency.Domain;

public class DevOpsWorkItem : WorkItemBase, IWorkItemPullRequests
{

    public DevOpsWorkItem(int id)
    {
        Id = id;
        IsRelated = false;
    }

    public DevOpsWorkItem(int id, bool isRelated)
    {
        Id = id;
        IsRelated = isRelated;
    }

    [JsonIgnore]
    public ICollection<PullRequest> PullRequests { get; set; } = [];
}