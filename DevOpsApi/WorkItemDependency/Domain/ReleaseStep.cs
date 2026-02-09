namespace DevOpsApi.WorkItemDependency.Domain;

public class ReleaseStep
{
	public string PipelineName { get; set; }
	
    public string ServiceName => PipelineName.Replace("Deploy", string.Empty).Replace("Provision", string.Empty).Trim();
	
    public int BuildId { get; set; }

    public string BuildNumber { get; set; }
}