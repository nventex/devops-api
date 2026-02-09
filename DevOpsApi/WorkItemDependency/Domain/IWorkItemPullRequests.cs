namespace DevOpsApi.WorkItemDependency.Domain;

public interface IWorkItemPullRequests 
{
    public ICollection<PullRequest> PullRequests { get; set; }

    public void SetPullrequests(ICollection<PullRequest> pullRequests) { PullRequests = pullRequests; }
}
