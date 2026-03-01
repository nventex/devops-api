using DevOpsApi.Authentication;
using DevOpsApi.Common.Infrastructure.Authentication;
using DevOpsApi.WorkItemDependency;
using Microsoft.AspNetCore.Mvc;

namespace DevOpsApi.ReleaseDocumentGeneration.Api;

public static class ReleaseDocumentApi
{
    extension(WebApplication app)
    {
        public void MapReleaseDocumentApi()
        {
            app.MapGet("/release-document",
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
}