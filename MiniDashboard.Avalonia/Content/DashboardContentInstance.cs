using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using MiniDashboard.Avalonia.Tiles;

namespace MiniDashboard.Avalonia.Content;

// Shared preparation/commit boundary: creating instances never modifies the live visual tree.
internal sealed record DashboardContentInstance(string ContentId, DashboardContentDefinition? Definition, Control Control)
{
    private Tile? _matrixTile;

    public Tile MatrixTile => _matrixTile ??= Control as Tile ?? new DashboardContentTile();

    public static DashboardContentInstance Create(string id, DashboardContentDefinition? definition, ISet<Control> used,
                                                  Guid? cellId = null, object? state = null)
    {
        var context = new DashboardContentCreationContext(Guid.NewGuid(), id, cellId, state);
        var control = definition is null
            ? new TextBlock { Text = $"Unavailable content: {id}", TextWrapping = TextWrapping.Wrap }
            : (definition.ContextTileFactory is not null ? definition.ContextTileFactory(context) :
                definition.ContextFactory is not null ? definition.ContextFactory(context) :
                definition.TileFactory is not null ? definition.TileFactory() : definition.Factory!()) ?? throw new InvalidOperationException($"Factory '{id}' returned null.");
        if (control is Tile && definition?.TileFactory is null && definition?.ContextTileFactory is null)
            throw new InvalidOperationException($"Factory '{id}' returned a tile; use TileFactory for complete tiles.");
        if (control.Parent is not null || control.GetVisualParent() is not null || !used.Add(control))
            throw new InvalidOperationException($"Factory '{id}' must return a fresh, unparented control.");

        return new DashboardContentInstance(id, definition, control);
    }

    public static void Release(IEnumerable<DashboardContentInstance> entries, bool dispose)
    {
        if (!dispose)
            return;

        List<Exception>? failures = null;
        foreach (var entry in entries)
        {
            if (entry.Definition is null || entry.Control is not IDisposable disposable)
                continue;

            try
            {
                disposable.Dispose();
            }
            catch (Exception error)
            {
                (failures ??= new List<Exception>()).Add(error);
            }
        }
        if (failures is not null)
            throw new AggregateException("Content disposal failed; all discarded controls were given a disposal attempt.", failures);
    }
}
