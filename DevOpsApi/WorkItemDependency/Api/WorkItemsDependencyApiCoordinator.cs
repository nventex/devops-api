using DevOpsApi.Common.Infrastructure.Authentication;
using DevOpsApi.WorkItemDependency.Dtos;
using LazyCache;

namespace DevOpsApi.WorkItemDependency.Api;

public static class WorkItemsDependencyApiCoordinator
{
    public static async Task<IEnumerable<WorkItemDto>> GetWorkItemsDependency(AuthenticationModel model,
        GetWorkItemsHandler getItemsHandler, int? sprint, CancellationToken cancellationToken,
        GetWorkItemDependencyHandler getItemDependencyHandler, GetWorkItemsDependencyHandler getItemsDependencyHandler,
        bool useCache)
    {
        var cache = new CachingService();

        if (useCache)
        {
            return await cache.GetAsync<List<WorkItemDto>>("items-dependency");
        }
        
        var items = await getItemsHandler.HandleAsync(sprint, model, cancellationToken);

        var itemsTasks = items.Select(i => getItemDependencyHandler.HandleAsync(0, model, workItem: i));
                    
        var itemsDependency = await Task.WhenAll(itemsTasks);

        var itemsWithDependency = (await getItemsDependencyHandler.HandleAsync(itemsDependency.Select(i => i.WorkItem))).ToList();
        
        cache.Add("items-dependency", itemsWithDependency, TimeSpan.FromHours(1));
        
        return itemsWithDependency;
    }
}