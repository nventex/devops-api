namespace DevOpsApi.ReleaseDocumentGeneration.Dtos;

public class ReleasePipelineDto
{
    public int BuildId { get; init; }

    public string PipelineName { get; init; }

    public string PipelineLink => $"https://dev.azure.com/cpowerDR/Operations%20Platform/_build/results?buildId={BuildId}&view=results";
    public string BuildNumber { get; init; }
}