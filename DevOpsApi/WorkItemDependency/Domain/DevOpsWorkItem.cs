using System.Text.Json.Serialization;

namespace DevOpsApi.WorkItemDependency.Domain;

public class DevOpsWorkItem : WorkItemBase, IWorkItemPullRequests
{

    public DevOpsWorkItem(int workItemId)
    {
        WorkItemId = workItemId;
        IsRelated = false;
    }

    public DevOpsWorkItem(int workItemId, bool isRelated)
    {
        WorkItemId = workItemId;
        IsRelated = isRelated;
    }

    [JsonIgnore]
    public ICollection<PullRequest> PullRequests { get; set; } = [];
}