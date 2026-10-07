namespace MiniDashboard.Avalonia.Grid;

/// <summary>
/// Describes why a dashboard placement request could not be applied exactly.
/// </summary>
public enum DashboardPlacementFailureReason
{
    /// <summary>
    /// The requested placement was accepted exactly.
    /// </summary>
    None,

    /// <summary>
    /// The requested placement exceeded the dashboard bounds.
    /// </summary>
    OutOfBounds,

    /// <summary>
    /// The requested placement collided with another child.
    /// </summary>
    Collision,

    /// <summary>
    /// The requested placement was smaller than the minimum supported size.
    /// </summary>
    MinSize,

    /// <summary>
    /// No free placement could be found for the requested child.
    /// </summary>
    NoSpace
}
