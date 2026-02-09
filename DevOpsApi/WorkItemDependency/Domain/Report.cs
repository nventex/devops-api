using System.Text.Json.Serialization;

namespace DevOpsApi.WorkItemDependency.Domain;

public class Report
{
    public DevOpsWorkItem WorkItem { get; set; }
	
    [JsonIgnore()]
    public IEnumerable<DevOpsBuild> Builds { get; set; }
	
    public IEnumerable<IGrouping<string, DevOpsBuild>> PipelineBuilds => Builds.OrderBy(b => b.QueueTime).GroupBy(b => b.PipelineName);
}