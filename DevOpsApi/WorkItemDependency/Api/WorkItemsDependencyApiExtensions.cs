using DevOpsApi.Common.Infrastructure.Authentication;
using DevOpsApi.WorkItemDependency.Dtos;

namespace DevOpsApi.WorkItemDependency.Api;

public static class WorkItemsDependencyApiExtensions
{
    public static async Task<IEnumerable<WorkItemDto>> GetWorkItemsDependency(AuthenticationModel model, 
        GetWorkItemsHandler getItemsHandler, int? sprint, CancellationToken cancellationToken,
        GetWorkItemDependencyHandler getItemDependencyHandler, GetWorkItemsDependencyHandler getItemsDependencyHandler)
    {
        var items = await getItemsHandler.HandleAsync(sprint, model, cancellationToken);

        var itemsTasks = items.Select(i => getItemDependencyHandler.HandleAsync(0, model, workItem: i));
                    
        var itemsDependency = await Task.WhenAll(itemsTasks);

        return await getItemsDependencyHandler.HandleAsync(itemsDependency.Select(i => i.WorkItem));
    }
}