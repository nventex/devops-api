using System.Text;
using System.Text.Json;
using DevOpsApi.Common.Infrastructure.Authentication;
using DevOpsApi.Common.Settings;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;

namespace DevOpsApi.Common.Infrastructure.DevOps;

public class DevOpsClient : IDisposable
{
    private readonly IAppCache _cache;
    private readonly DevOpsSettings _options;

    public WorkItemTrackingHttpClient WorkItemClient { get; private set; }
    
    public GitHttpClient GitClient { get; private set; }
    
    public BuildHttpClient BuildClient { get; private set; }    
    
    public WorkHttpClient WorkClient { get; private set; }

    public DevOpsClient(IAppCache cache, IOptions<DevOpsSettings> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public async Task Connect()
    {
        var authentication = await _cache.GetOrAddAsync("access-token", _ => GetAccessToken(), new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(25) });
        
        var credentials = new VssOAuthAccessTokenCredential(authentication.AccessToken);
        var connection = new VssConnection(new Uri($"https://dev.azure.com/{_options.Organization}"), credentials);

        SetupClients(connection);
    }

    private void SetupClients(VssConnection connection)
    {
        GitClient = connection.GetClient<GitHttpClient>();
        BuildClient = connection.GetClient<BuildHttpClient>();
        WorkItemClient = connection.GetClient<WorkItemTrackingHttpClient>();
        WorkClient = connection.GetClient<WorkHttpClient>();
    }

    public void Dispose()
    {
        WorkItemClient.Dispose();
        BuildClient.Dispose();
        GitClient.Dispose();
        WorkClient.Dispose();
    }    
    
    private static async Task<AuthenticationModel> GetAccessToken()
    {
        var process = new System.Diagnostics.Process();
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            RedirectStandardOutput = true,
            FileName = "cmd",
            Arguments = "/C az account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798"
        };
        process.StartInfo = startInfo;
        process.Start();
	
        var output = await process.StandardOutput.ReadToEndAsync();
	
        var bytes = Encoding.UTF8.GetBytes(output);
        using var stream = new MemoryStream(bytes);
        var model = await JsonSerializer.DeserializeAsync<AuthenticationModel>(stream, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
	
        await process.WaitForExitAsync();
	
        return model;
    }    
}