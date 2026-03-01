namespace DevOpsApi.ReleaseDocumentGeneration.Dtos;

public class ReleasePipelineDto
{
    public int BuildId { get; init; }

    public string PipelineName { get; init; }
}