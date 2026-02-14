using System.Text.Json.Serialization;
using DevOpsApi.WorkItemDependency.Domain;

namespace DevOpsApi.WorkItemDependency.Dtos;

public class ReportDto
{
    public DevOpsWorkItem WorkItem { get; set; }
    
    public IEnumerable<DevOpsWorkItem> WorkItems { get; set; }
	
    [JsonIgnore]
    public IEnumerable<DevOpsBuild> Builds { get; set; }
	
    public IEnumerable<IGrouping<string, DevOpsBuild>> PipelineBuilds => Builds.OrderBy(b => b.QueueTime).GroupBy(b => b.PipelineName);
}

public class ReportItemDto
{
	public ReportItemDto(DevOpsWorkItem workItem, IEnumerable<IGrouping<string, DevOpsBuild>> pipelineBuilds)
	{
		WorkItem = workItem;
		PipelineBuilds = pipelineBuilds;
	}
	
	public DevOpsWorkItem WorkItem { get; private set; }
	
	public IEnumerable<IGrouping<string, DevOpsBuild>> PipelineBuilds { get; private set; }
}