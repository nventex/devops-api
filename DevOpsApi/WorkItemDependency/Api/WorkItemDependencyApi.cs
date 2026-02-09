namespace DevOpsApi.WorkItemDependency.Api;

public static class WorkItemDependencyApi
{
    public static void MapWorkItemDependencyApi(this WebApplication app)
    {
        app.MapGet("/work-item-dependency/{id:int}", async (int id, GetWorkItemDependencyHandler handler) => await handler.HandleAsync(id)).WithName(nameof(GetWorkItemDependencyHandler));
    }
}