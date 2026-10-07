using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace MiniDashboard.Avalonia.Content;

/// <summary>An application-replaceable content picker shared by both dashboard hosts.</summary>
public interface IDashboardContentPicker
{
    /// <summary>Returns a catalog ID, or null for cancellation. The host performs content creation.</summary>
    /// <param name="anchor">The invoking button or fallback tile to anchor the chooser to.</param>
    /// <param name="definitions">Available recipes; factories must not be invoked by the chooser.</param>
    /// <param name="currentId">The currently assigned definition ID, or null for an empty slot.</param>
    /// <returns>A chosen definition ID or null when the user cancels.</returns>
    /// <remarks>Called on the UI thread. Implementations own chooser dismissal and anchor-detachment handling.
    /// The host applies the result through its assignment APIs; do not mutate the dashboard from the chooser.</remarks>
    Task<string?> PickAsync(Control anchor, IReadOnlyList<DashboardContentDefinition> definitions, string? currentId);
}
