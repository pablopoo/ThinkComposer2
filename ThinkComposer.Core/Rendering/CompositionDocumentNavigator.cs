namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionDocumentNavigator
{
    public static CompositionDocumentNavigationTarget? FindParentTarget(
        CompositionDocumentSnapshot document,
        string? currentViewId)
    {
        if (document is null || string.IsNullOrWhiteSpace(currentViewId))
        {
            return null;
        }

        var currentView = document.Views.FirstOrDefault(view =>
            string.Equals(view.Id, currentViewId, StringComparison.Ordinal));
        if (currentView is null || string.IsNullOrWhiteSpace(currentView.ContainerIdeaId))
        {
            return null;
        }

        var containerIdea = document.Ideas.FirstOrDefault(idea =>
            string.Equals(idea.Id, currentView.ContainerIdeaId, StringComparison.Ordinal));
        if (containerIdea is null)
        {
            return null;
        }

        var parentView = document.Views.FirstOrDefault(view =>
            view.Nodes.Any(node => string.Equals(node.Id, containerIdea.Id, StringComparison.Ordinal)));
        return parentView is null
            ? null
            : new CompositionDocumentNavigationTarget(parentView.Id, containerIdea.Id);
    }
}
