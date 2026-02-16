using DevOpsApi.Common.Infrastructure.Authentication;
using DevOpsApi.Common.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DevOpsApi.WorkItemDependency.Api;

public static class CopilotApi
{
    extension(WebApplication app)
    {
        public void MapCopilotWorkItemsDependencyApi()
        {
            app.MapGet("/copilot/work-items-dependency",
                async ([FromHeader(Name = "api-key")] string apiKey, IOptions<DevOpsSettings> settings,
                    GetWorkItemsHandler itemsHandler, GetWorkItemDependencyHandler itemDependencyHandler, GetWorkItemsDependencyHandler itemsDependencyHandler, CancellationToken cancellationToken) =>
                {
                    if (!settings.Value.ApiKey.Equals(apiKey))
                    {
                        throw new UnauthorizedAccessException();
                    }

                    return await WorkItemsDependencyApiExtensions.GetWorkItemsDependency(AuthenticationModel.UseApiKey(), itemsHandler, 0, cancellationToken, itemDependencyHandler, itemsDependencyHandler);
                }).WithName($"Copilot{nameof(GetWorkItemsDependencyHandler)}");
        }    
    }
}