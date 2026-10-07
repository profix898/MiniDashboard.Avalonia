using System.ComponentModel;
using Avalonia.Controls;

namespace MiniDashboard.Avalonia.Grid;

/// <summary>A proposed or committed tile-grid-dashboard placement.</summary>
public sealed class DashboardPlacementChangingEventArgs : CancelEventArgs
{
    internal DashboardPlacementChangingEventArgs(Control child, DashboardPlacement before, DashboardPlacement after)
    {
        Child = child;
        Before = before;
        After = after;
    }

    /// <summary>Gets the dashboard child.</summary>
    public Control Child { get; }

    /// <summary>Gets its previous placement.</summary>
    public DashboardPlacement Before { get; }

    /// <summary>Gets its proposed placement. Cancellation is only honored by PlacementChanging.</summary>
    public DashboardPlacement After { get; }
}
