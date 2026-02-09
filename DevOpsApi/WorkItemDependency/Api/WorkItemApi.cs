namespace DevOpsApi.WorkItemDependency.Api;

public static class WorkItemApi
{
    public static void MapWorkItemApi(this WebApplication app)
    {
        app.MapGet("/work-items/sprint/{sprint:int?}",
            async (int? sprint, GetWorkItemsHandler handler, CancellationToken cancellationToken) => await handler.HandleAsync(sprint, cancellationToken)).WithName(nameof(GetWorkItemsHandler));
    }
}