using DevOpsApi.WorkItemDependency.Domain;

namespace DevOpsApi.ReleaseDocumentGeneration.Domain;

public class ReleaseDocument
{
    public IEnumerable<ReleaseItem> Items { get; set; } = [];
	
    public IEnumerable<ReleaseStep> Steps { get; set; } = [];
}