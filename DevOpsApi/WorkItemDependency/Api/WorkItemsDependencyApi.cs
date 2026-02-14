using DevOpsApi.Authentication;
using DevOpsApi.Common.Infrastructure.Authentication;
using DevOpsApi.WorkItemDependency.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DevOpsApi.WorkItemDependency.Api;

public static class WorkItemsDependencyApi
{
    extension(WebApplication app)
    {
        public void MapWorkItemDependencyApi()
        {
            app.MapGet("/work-item-dependency/{id:int}",
                async ([FromHeader(Name = "Authorization")] string token, int id, AuthenticationHandler authHandler,
                    GetWorkItemDependencyHandler handler) =>
                {
                    var model = await authHandler.Handle(new AuthenticationModel { AccessToken = token }, CancellationToken.None);
                
                    if (!model.IsAuthenticated)
                    {
                        throw new UnauthorizedAccessException();
                    }
                
                    var dto = await handler.HandleAsync(id, model);

                    return new ReportItemDto(dto.WorkItem, dto.PipelineBuilds);

                }).WithName(nameof(GetWorkItemDependencyHandler));
        }

        public void MapWorkItemsDependencyApi()
        {
            app.MapGet("/work-items-dependency/sprint/{sprint:int?}",
                async ([FromHeader(Name = "Authorization")] string token, int? sprint, AuthenticationHandler authHandler,
                    GetWorkItemsHandler itemsHandler, GetWorkItemDependencyHandler itemDependencyHandler, GetWorkItemsDependencyHandler itemsDependencyHandler, CancellationToken cancellationToken) =>
                {
                    var model = await authHandler.Handle(new AuthenticationModel { AccessToken = token }, CancellationToken.None);
                
                    if (!model.IsAuthenticated)
                    {
                        throw new UnauthorizedAccessException();
                    }
                
                    var items = await itemsHandler.HandleAsync(sprint, model, cancellationToken);

                    var itemsTasks = items.Select(i => itemDependencyHandler.HandleAsync(0, model, workItem: i));
                    
                    var itemsDependency = await Task.WhenAll(itemsTasks);

                    return await itemsDependencyHandler.HandleAsync(itemsDependency.Select(i => i.WorkItem));

                }).WithName(nameof(GetWorkItemsDependencyHandler));
        }
    }
}