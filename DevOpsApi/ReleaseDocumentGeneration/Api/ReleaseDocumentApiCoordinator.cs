using DevOpsApi.Common.Infrastructure.Authentication;
using DevOpsApi.WorkItemDependency;

namespace DevOpsApi.ReleaseDocumentGeneration.Api;

public static class WorkItemsDependencyApiCoordinator
{
    public static async Task GetReleaseDocument(AuthenticationModel model,
        CreateReleaseDocumentHandler createdDocHandler,
        GetWorkItemsHandler getItemsHandler, GetWorkItemDependencyHandler itemHandler, int? sprint,
        CancellationToken cancellationToken)
    {
        var items = await getItemsHandler.HandleAsync(sprint, model, cancellationToken);

        var itemTasks = items.Select(x => itemHandler.HandleAsync(x.WorkItemId, model, null, x));
        
        var dtos = await Task.WhenAll(itemTasks);
        
        await createdDocHandler.Handle(dtos.Select(x => x.WorkItem).ToList(), model);
    }
}