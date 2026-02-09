namespace DevOpsApi.WorkItemDependency.Domain;

public class DevOpsTimeline 
{
    public string Name { get; set; }
	
    public string Identifier { get; set; }
	
    public string State { get; set; }
	
    public string Result { get; set; }
	
    public int ParentDevOpsBuildId { get; set; }
	
    public int? Order { get; set; }
}
