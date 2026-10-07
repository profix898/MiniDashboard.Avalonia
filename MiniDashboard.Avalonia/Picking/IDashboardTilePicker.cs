using System;
using System.Threading;
using System.Threading.Tasks;
using MiniDashboard.Avalonia.Tiles;

namespace MiniDashboard.Avalonia.Picking;

/// <summary>One-shot, explicit command targeting shared by tile-grid and matrix dashboards.</summary>
public interface IDashboardTilePicker
{
    /// <summary>Gets whether a target selection is pending.</summary>
    bool IsPickingTile { get; }

    /// <summary>Picks an eligible live tile; cancellation returns null without activating hosted content.</summary>
    /// <param name="eligible">Predicate evaluated at selection start against each live tile.</param>
    /// <param name="cancellationToken">Cancels selection; cancellation is represented by a null result.</param>
    /// <returns>The selected live tile, or null for cancellation or no eligible targets.</returns>
    /// <remarks>Call on the UI thread after layout. Only one request per host may be pending.
    /// The caller supplies instructions, revalidates target capabilities, and executes its command.</remarks>
    Task<Tile?> PickTileAsync(Func<Tile, bool> eligible, CancellationToken cancellationToken = default);

    /// <summary>Cancels a pending target selection.</summary>
    void CancelTilePick();
}
