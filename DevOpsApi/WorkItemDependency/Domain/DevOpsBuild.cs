namespace DevOpsApi.WorkItemDependency.Domain;

public class DevOpsBuild
{
    public int Id { get; set; }
	
    public string PipelineName { get; set; }
	
    public string BuildNumber { get; set; }
	
    public int RelatedPRNumber
    {
        get
        {
            return int.TryParse(CiMessage?.Substring(0, CiMessage.IndexOf(":")).Replace("Merged PR ", string.Empty, StringComparison.OrdinalIgnoreCase), out var prNumber)
                ? prNumber : 0;
        }
    }
	
    public DateTime? QueueTime { get; set; }
	
    public DateTime? StartTime { get; set; }
	
    public string SourceBranch { get; set; }
	
    public string Status { get; set; }

    public ICollection<DevOpsWorkItem> Dependees { get; set; } = [];

    public ICollection<DevOpsTimeline> Timeline { get; set; }
	
    public int BuildDefinitionId { get; set; }
		
    public string CiMessage { get; set; }
	
    public string SourceVersion { get; set; }
	
    public DevOpsTimeline CurrentBuildTimeline => Timeline.OrderByDescending(t => t.Order).Where(t => t.State.Equals("Completed", StringComparison.OrdinalIgnoreCase) && t.Result.Equals("Succeeded", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

    public bool ReleasedToProduction => Timeline.Any(t => (t.Identifier.Equals("Production", StringComparison.OrdinalIgnoreCase) || t.Identifier.Equals("PRD", StringComparison.OrdinalIgnoreCase)) && t.State.Equals("Completed", StringComparison.OrdinalIgnoreCase) && t.Result.Equals("Succeeded", StringComparison.OrdinalIgnoreCase));

    public string RepositoryId { get; set; }

    public DateTime? FinishTime { get; set; }
}
