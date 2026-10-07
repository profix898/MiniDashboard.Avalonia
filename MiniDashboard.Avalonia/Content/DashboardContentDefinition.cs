using System;
using Avalonia.Controls;
using MiniDashboard.Avalonia.Tiles;

namespace MiniDashboard.Avalonia.Content;

/// <summary>A catalog entry that creates a fresh control for a persistent content ID.</summary>
/// <remarks>Specify exactly one of the four factory variants. Body factories return non-Tile controls;
/// complete-tile factories return Tile instances. Every invocation must return fresh, unparented content.
/// Preserve definition identity when updating metadata to avoid recreating assigned controls.</remarks>
public sealed class DashboardContentDefinition
{
    /// <summary>Gets the stable, nonlocalized persistence key.</summary>
    public required string Id { get; init; }

    /// <summary>Gets or sets the title displayed in the content picker.</summary>
    public required string Title { get; set; }

    /// <summary>Gets the body factory. Specify exactly one factory; each invocation returns a fresh, unparented control.</summary>
    public Func<Control>? Factory { get; init; }

    /// <summary>Gets an optional complete-tile factory. Specify exactly one of the four factory variants.</summary>
    public Func<Tile>? TileFactory { get; init; }

    /// <summary>Gets a body factory with instance and restoration context.</summary>
    public Func<DashboardContentCreationContext, Control>? ContextFactory { get; init; }

    /// <summary>Gets a complete-tile factory with instance and restoration context.</summary>
    public Func<DashboardContentCreationContext, Tile>? ContextTileFactory { get; init; }

    /// <summary>Gets or sets an optional category for grouping and searching the content picker.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets optional descriptive search text.</summary>
    public string? Description { get; set; }
}
