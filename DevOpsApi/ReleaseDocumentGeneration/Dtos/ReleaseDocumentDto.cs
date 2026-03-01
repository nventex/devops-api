namespace DevOpsApi.ReleaseDocumentGeneration.Dtos;

public class ReleaseDocumentDto
{
    public IEnumerable<ReleaseWorkItemDto> WorkItems { get; init; }

    public IEnumerable<ReleasePipelineDto> Pipelines { get; init; }

    public string[] Repositories => Pipelines.Select(x => x.PipelineName.Replace("Deploy", string.Empty).Replace("Provision", string.Empty)).Select(x => x.Trim()).ToArray();
}