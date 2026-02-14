using DevOpsApi.Authentication;
using DevOpsApi.Common.Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace DevOpsApi.WorkItemDependency.Api;

public static class WorkItemApi
{
    public static void MapWorkItemsApi(this WebApplication app)
    {
        app.MapGet("/work-items/sprint/{sprint:int?}",
            async ([FromHeader(Name = "Authorization")] string token, int? sprint, GetWorkItemsHandler handler, AuthenticationHandler authHandler, CancellationToken cancellationToken) =>
            {
                var model = await authHandler.Handle(new AuthenticationModel { AccessToken = token }, cancellationToken);
                
                if (!model.IsAuthenticated)
                {
                    throw new UnauthorizedAccessException();
                }
                
                return await handler.HandleAsync(sprint, model, cancellationToken);
            
            }).WithName(nameof(GetWorkItemsHandler));
    }
}