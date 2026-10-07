using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using MiniDashboard.Avalonia.Picking;
using MiniDashboard.Avalonia.Tiles;
using AvaloniaGrid = Avalonia.Controls.Grid;

namespace MiniDashboard.Avalonia.Matrix;

public partial class DashboardMatrix
{
    private DashboardTargetPicker<DashboardMatrixCellModel>? _cellPicker;

    /// <summary>Gets whether the matrix is temporarily selecting a command target.</summary>
    public bool IsPickingCell => _cellPicker?.IsPicking == true;

    /// <summary>Selects a live matrix tile using the same target contract as tile-grid dashboards.</summary>
    public async Task<Tile?> PickTileAsync(Func<Tile, bool> eligible, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eligible);

        var id = await PickCellAsync((cell, _) => GetTile(cell.Id) is { } tile && eligible(tile), cancellationToken);

        return id.HasValue ? GetTile(id.Value) : null;
    }

    /// <summary>Cancels pending tile or cell selection.</summary>
    public void CancelTilePick() => CancelCellPick();

    /// <summary>Gets whether a tile or cell target is being selected.</summary>
    public bool IsPickingTile => IsPickingCell;

    /// <summary>Selects one eligible cell without activating its content. Escape, detachment, layout changes and cancellation return null.</summary>
    /// <param name="eligible">Predicate evaluated at selection start with the cell and its factory-created body or complete tile.</param>
    /// <param name="cancellationToken">Cancels picking; cancellation returns null rather than throwing.</param>
    /// <returns>The selected cell ID, or null for cancellation or no eligible cells.</returns>
    /// <remarks>Call on the UI thread after template application. Only one cell/tile request may be pending.
    /// Picking intercepts content input and suspends structural editing; application commands remain caller-owned.</remarks>
    public async Task<Guid?> PickCellAsync(Func<DashboardMatrixCellModel, Control?, bool> eligible,
                                           CancellationToken cancellationToken = default)
    {
        EnsureAvailable();
        ArgumentNullException.ThrowIfNull(eligible);
        if (IsPickingCell)
            throw new InvalidOperationException("A cell selection is already pending.");
        if (_grid is null)
            throw new InvalidOperationException("Apply the matrix template before selecting a cell.");

        var picker = _cellPicker = new DashboardTargetPicker<DashboardMatrixCellModel>(this, _grid);
        var editing = _grid.Children.Where(c => c is GridSplitter or DashboardMatrixBoundaryButton).ToArray();
        var enabled = editing.Select(c => c.IsEnabled).ToArray();

        try
        {
            foreach (var control in editing)
                control.IsEnabled = false;

            var result = await picker.PickAsync(_layout.Cells, cell => eligible(cell, GetContent(cell.Id)),
                                                cell => $"Select row {cell.Row + 1}, column {cell.Column + 1}", (cell, button) =>
                                                {
                                                    button.Tag = cell;
                                                    AvaloniaGrid.SetRow(button, cell.Row * 2);
                                                    AvaloniaGrid.SetColumn(button, cell.Column * 2);
                                                }, cancellationToken);

            return result?.Id;
        }
        finally
        {
            for (var i = 0; i < editing.Length; i++)
                editing[i].IsEnabled = enabled[i];

            _cellPicker = null;
        }
    }

    /// <summary>Cancels a pending one-shot cell selection.</summary>
    public void CancelCellPick()
    {
        VerifyAccess();
        _cellPicker?.Cancel();
    }
}
