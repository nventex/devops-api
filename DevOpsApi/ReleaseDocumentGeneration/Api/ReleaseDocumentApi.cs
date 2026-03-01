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
            app.MapGet("/release-document/sprint/{sprint:int?}",
                async ([FromHeader(Name = "Authorization")] string token, int? sprint, GetWorkItemsHandler itemsHandler, GetWorkItemDependencyHandler itemHandler, 
                    CreateReleaseDocumentHandler createdDocHandler, AuthenticationHandler authHandler, CancellationToken cancellationToken) =>
                {
                    var model = await authHandler.Handle(new AuthenticationModel { AccessToken = token }, cancellationToken);
                
                    if (!model.IsAuthenticated)
                    {
                        throw new UnauthorizedAccessException();
                    }
                
                    return await WorkItemsDependencyApiCoordinator.GetReleaseDocument(model, createdDocHandler, itemsHandler, itemHandler, sprint, cancellationToken);
            
                }).WithName(nameof(CreateReleaseDocumentHandler));
        }    
    }
}