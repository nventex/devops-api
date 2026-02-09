using System.Text.Json.Serialization;

namespace DevOpsApi.WorkItemDependency.Domain;

public class PullRequest 
{
    public int Number { get; set; }
	
    public Guid RepositoryId { get; set; }
	
    public string RepositoryName { get; set; }
	
    public int ParentWorkItemId { get; set; }
	
    [JsonIgnore]
    public ICollection<DevOpsBuild> Builds { get; set; } = [];
	
    public IDictionary<string, List<DevOpsBuild>> BuildsSinceLastRelease => GetBuildsSinceLastRelease();
	
    public IDictionary<string, List<DevOpsBuild>> GetBuildsSinceLastRelease()
    {
        var pipelineGroup = Builds.GroupBy(b => b.PipelineName);

        var builds = pipelineGroup.Select(pipeline =>
        {
            var pipelineBuilds = pipeline.Where(p => !p.SourceBranch.EndsWith("/merge"));
			
            var lastRelease = pipelineBuilds.FirstOrDefault(p => p.Timeline.Any(t => t.Identifier.Equals("PRD", StringComparison.OrdinalIgnoreCase) && t.State.Equals("Completed", StringComparison.OrdinalIgnoreCase) && t.Result.Equals("Succeeded", StringComparison.OrdinalIgnoreCase)));

            if (lastRelease is null)
            {
                return pipelineBuilds;
            }

            var results = pipelineBuilds.Where(p => string.CompareOrdinal(p.BuildNumber, lastRelease.BuildNumber) >= 0);

            return results;
        });

        return builds.SelectMany(x => x).GroupBy(x => x.PipelineName).ToDictionary(x => x.Key, y => y.ToList());
    }
}