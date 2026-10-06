using System;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace MiniDashboard.Avalonia;

// Classic grid-span interaction is isolated from the reusable tile presentation.
internal sealed class DashboardTileResizeBehavior
{
    private readonly Tile _tile;
    private Thumb? _hitThumb;
    private Thumb? _thumb;

    public DashboardTileResizeBehavior(Tile tile) => _tile = tile;

    private int _resizeLastWantedH;

    private int _resizeLastWantedW;

    private DashboardPanel? _resizePanel;
    private int _resizeStartGridH;
    private int _resizeStartGridW;

    private int _resizeStartGridX;
    private int _resizeStartGridY;

    public void Attach(Thumb? hitThumb, Thumb? thumb)
    {
        Cancel();
        Detach(_hitThumb);
        Detach(_thumb);
        _hitThumb = hitThumb;
        _thumb = thumb;
        Subscribe(_hitThumb);
        if (!ReferenceEquals(_thumb, _hitThumb))
            Subscribe(_thumb);
    }

    private void Subscribe(Thumb? thumb)
    {
        if (thumb is null)
            return;
        
        thumb.DragStarted += OnResizeStarted;
        thumb.DragCompleted += OnResizeCompleted;
    }

    private void Detach(Thumb? thumb)
    {
        if (thumb is null)
            return;
        
        thumb.DragStarted -= OnResizeStarted;
        thumb.DragCompleted -= OnResizeCompleted;
    }

    public void Cancel()
    {
        if (_resizePanel is { } panel)
        {
            panel.PointerMoved -= OnResizePointerMoved;
            panel.HideSnapPreview();
            _resizePanel = null;
        }
        
        _tile.IsPlacementValid = true;
    }

    private void OnResizeStarted(object? sender, VectorEventArgs e)
    {
        if (_tile.IsResizeGripVisible && _tile.Parent is DashboardPanel panel)
        {
            _resizePanel = panel;

            // capture starting grid pos and size
            // Use attached X/Y from the panel to ensure we anchor to the tile's actual arranged cell coordinates
            _resizeStartGridX = DashboardPanel.GetX(_tile);
            _resizeStartGridY = DashboardPanel.GetY(_tile);
            _resizeStartGridW = _tile.GridW;
            _resizeStartGridH = _tile.GridH;

            // initial preview at current arranged position
            panel.ShowSnapPreview(_resizeStartGridX, _resizeStartGridY, _resizeStartGridW, _resizeStartGridH, true);
            _tile.IsPlacementValid = true;

            // initialize last-wanted with current size
            _resizeLastWantedW = _resizeStartGridW;
            _resizeLastWantedH = _resizeStartGridH;

            // subscribe to pointer moves on the panel to track absolute pointer position
            panel.PointerMoved -= OnResizePointerMoved;
            panel.PointerMoved += OnResizePointerMoved;
        }
    }

    private void OnResizePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_resizePanel is null)
            return;
        
        var panel = _resizePanel;

        // get pointer position relative to panel
        var pos = e.GetPosition(panel);

        // compute cell size
        var cell = panel.GetCellSize(panel.Bounds.Size);
        var cw = Math.Max(1.0, cell.Width);
        var ch = Math.Max(1.0, cell.Height);

        // determine which column/row the pointer is currently over
        var col = (int) Math.Floor(pos.X / cw);
        var row = (int) Math.Floor(pos.Y / ch);

        col = Math.Clamp(col, 0, panel.Columns - 1);
        row = Math.Clamp(row, 0, panel.Rows - 1);

        // compute wanted size as number of columns/rows from the start X/Y to the pointer column/row
        // ensure we use the start X/Y captured from the panel so resizing is anchored to the tile's position
        var wantedW = Math.Max(1, col - _resizeStartGridX + 1);
        var wantedH = Math.Max(1, row - _resizeStartGridY + 1);

        // enforce minimums
        wantedW = Math.Max(wantedW, _tile.MinGridW);
        wantedH = Math.Max(wantedH, _tile.MinGridH);

        // only act when a full-cell boundary was crossed (i.e. wanted changed)
        if (wantedW == _resizeLastWantedW && wantedH == _resizeLastWantedH)
            return;

        _resizeLastWantedW = wantedW;
        _resizeLastWantedH = wantedH;

        var ok = panel.TryResolveResize(_tile, wantedW, wantedH, out var rw, out var rh);
        var isExact = ok && rw == wantedW && rh == wantedH;

        if (!panel.TrySetPlacement(_tile, _resizeStartGridX, _resizeStartGridY, rw, rh))
        {
            _tile.IsPlacementValid = false;
            
            return;
        }

        // show preview at resolved and mark header validity if it had to adjust
        panel.ShowSnapPreview(_tile.GridX, _tile.GridY, rw, rh, isExact);
        _tile.IsPlacementValid = isExact;
    }

    private void OnResizeCompleted(object? sender, VectorEventArgs e)
    {
        var panel = _resizePanel ?? _tile.Parent as DashboardPanel;
        if (_resizePanel is { } resizePanel)
        {
            resizePanel.PointerMoved -= OnResizePointerMoved;
            resizePanel.HideSnapPreview();
        }

        // final push to the panel and force arrange so visual size matches the last resolved values

        panel?.InvalidateArrange();

        // reset tracked panel
        _resizePanel = null;

        _tile.IsPlacementValid = true;
    }
}
