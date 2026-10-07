using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MiniDashboard.Avalonia.Picking;
using MiniDashboard.Avalonia.Tiles;

namespace MiniDashboard.Avalonia.Grid;

public partial class DashboardPanel
{
    private Canvas? _pickOverlay;
    private DashboardTargetPicker<Tile>? _tilePicker;

    /// <summary>Gets whether tile-grid tiles are temporarily selecting a command target.</summary>
    public bool IsPickingTile => _tilePicker?.IsPicking == true;

    /// <summary>Selects a live tile-grid tile without activating, dragging or resizing it. Also supported by DashboardItemsPanel.</summary>
    public async Task<Tile?> PickTileAsync(Func<Tile, bool> eligible, CancellationToken cancellationToken = default)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(eligible);
        if (IsPickingTile)
            throw new InvalidOperationException("A tile selection is already pending.");

        var tiles = Children.OfType<Tile>().ToArray();
        _pickOverlay = new Canvas { ZIndex = Int32.MaxValue, Background = Brushes.Transparent };
        var picker = _tilePicker = new DashboardTargetPicker<Tile>(this, _pickOverlay);
        Children.Add(_pickOverlay);
        foreach (var tile in tiles)
            tile.PropertyChanged += PlacementChangedDuringPick;

        try
        {
            return await picker.PickAsync(tiles, eligible, tile => $"Select {tile.TileHeader ?? "tile"}", (tile, button) =>
            {
                Canvas.SetLeft(button, tile.Bounds.X);
                Canvas.SetTop(button, tile.Bounds.Y);
                button.Width = tile.Bounds.Width;
                button.Height = tile.Bounds.Height;
            }, cancellationToken);
        }
        finally
        {
            foreach (var tile in tiles)
                tile.PropertyChanged -= PlacementChangedDuringPick;

            Children.Remove(_pickOverlay);
            _pickOverlay = null;
            _tilePicker = null;
        }
    }

    private void PlacementChangedDuringPick(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty || e.Property == Tile.ContentProperty)
            CancelTilePick();
    }

    /// <summary>Cancels pending tile-grid tile selection.</summary>
    public void CancelTilePick()
    {
        VerifyAccess();
        _tilePicker?.Cancel();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelTilePick();
        base.OnDetachedFromVisualTree(e);
    }
}
