using System;
using Avalonia.Controls;

namespace MiniDashboard.Avalonia;

/// <summary>A catalog entry that creates a fresh control for a persistent content ID.</summary>
public sealed class DashboardContentDefinition
{
    /// <summary>Gets the stable, nonlocalized persistence key.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the title displayed in the content picker.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the body factory. Each invocation must return a fresh, unparented non-Tile control.</summary>
    public Func<Control>? Factory { get; init; }

    /// <summary>Gets an optional complete-tile factory. Specify exactly one of Factory and TileFactory.</summary>
    public Func<Tile>? TileFactory { get; init; }
}
