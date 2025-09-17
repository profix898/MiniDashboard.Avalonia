// DashboardPanel.cs

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MiniDashboard.Avalonia;

/// <summary>
/// A grid-like panel that arranges tile tiles into a fixed number of rows and columns.
/// Provides occupancy tracking and snap-preview overlays for dragging/resizing tiles.
/// </summary>
public class DashboardPanel : Panel
{
    // Appearance: background pattern and grid lines (values overridden by theme resources via styles)
    /// <summary>
    /// Styled property for the brush used on even columns.
    /// </summary>
    public static readonly StyledProperty<IBrush> ColumnFillAProperty =
        AvaloniaProperty.Register<DashboardPanel, IBrush>(nameof(ColumnFillA), new SolidColorBrush(Color.FromArgb(40, 200, 200, 200)));

    /// <summary>
    /// Styled property for the brush used on odd columns.
    /// </summary>
    public static readonly StyledProperty<IBrush> ColumnFillBProperty =
        AvaloniaProperty.Register<DashboardPanel, IBrush>(nameof(ColumnFillB), new SolidColorBrush(Color.FromArgb(20, 200, 200, 200)));

    /// <summary>
    /// Number of columns in the dashboard grid.
    /// </summary>
    public static readonly StyledProperty<int> ColumnsProperty =
        AvaloniaProperty.Register<DashboardPanel, int>(nameof(Columns), 4);

    // Grid lines: disabled by default (transparent)
    /// <summary>
    /// Styled property for the brush used to draw grid lines.
    /// </summary>
    public static readonly StyledProperty<IBrush> GridLineBrushProperty =
        AvaloniaProperty.Register<DashboardPanel, IBrush>(nameof(GridLineBrush), new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)));

    /// <summary>
    /// Styled property for the thickness of grid lines.
    /// </summary>
    public static readonly StyledProperty<double> GridLineThicknessProperty =
        AvaloniaProperty.Register<DashboardPanel, double>(nameof(GridLineThickness), 1.0);

    /// <summary>
    /// Attached property for the height (span in rows) of tile tiles.
    /// </summary>
    public static readonly AttachedProperty<int> HProperty =
        AvaloniaProperty.RegisterAttached<DashboardPanel, Control, int>("H", 1);

    /// <summary>
    /// Styled property for the fill brush of an invalid preview.
    /// </summary>
    public static readonly StyledProperty<IBrush> PreviewInvalidFillProperty =
        AvaloniaProperty.Register<DashboardPanel, IBrush>(nameof(PreviewInvalidFill), new SolidColorBrush(Color.FromArgb(48, 239, 68, 68)));

    /// <summary>
    /// Styled property for the stroke brush of an invalid preview.
    /// </summary>
    public static readonly StyledProperty<IBrush> PreviewInvalidStrokeProperty =
        AvaloniaProperty.Register<DashboardPanel, IBrush>(nameof(PreviewInvalidStroke), new SolidColorBrush(Color.FromArgb(160, 239, 68, 68)));

    // Preview overlays (green/red) - keep defaults from previous implementation
    /// <summary>
    /// Styled property for the fill brush of a valid preview.
    /// </summary>
    public static readonly StyledProperty<IBrush> PreviewValidFillProperty =
        AvaloniaProperty.Register<DashboardPanel, IBrush>(nameof(PreviewValidFill), new SolidColorBrush(Color.FromArgb(48, 34, 197, 94)));

    /// <summary>
    /// Styled property for the stroke brush of a valid preview.
    /// </summary>
    public static readonly StyledProperty<IBrush> PreviewValidStrokeProperty =
        AvaloniaProperty.Register<DashboardPanel, IBrush>(nameof(PreviewValidStroke), new SolidColorBrush(Color.FromArgb(160, 34, 197, 94)));

    /// <summary>
    /// Styled property for the brush used to tint rows.
    /// </summary>
    public static readonly StyledProperty<IBrush> RowFillProperty =
        AvaloniaProperty.Register<DashboardPanel, IBrush>(nameof(RowFill), new SolidColorBrush(Color.FromArgb(10, 150, 150, 150)));

    /// <summary>
    /// Number of rows in the dashboard grid.
    /// </summary>
    public static readonly StyledProperty<int> RowsProperty =
        AvaloniaProperty.Register<DashboardPanel, int>(nameof(Rows), 4);

    // Global corner radius for all tiles (bound by tile styles)
    /// <summary>
    /// Styled property for the global corner radius applied to tiles.
    /// </summary>
    public static readonly StyledProperty<CornerRadius> TileCornerRadiusProperty =
        AvaloniaProperty.Register<DashboardPanel, CornerRadius>(nameof(TileCornerRadius), new CornerRadius(12));

    // Global border brush/ thickness for tiles
    /// <summary>
    /// Styled property for the global BorderBrush applied to tiles.
    /// </summary>
    public static readonly StyledProperty<IBrush?> TileBorderBrushProperty =
        AvaloniaProperty.Register<DashboardPanel, IBrush?>(nameof(TileBorderBrush));

    /// <summary>
    /// Styled property for the global BorderThickness applied to tiles.
    /// </summary>
    public static readonly StyledProperty<Thickness> TileBorderThicknessProperty =
        AvaloniaProperty.Register<DashboardPanel, Thickness>(nameof(TileBorderThickness), new Thickness(1));

    // Global margin applied around each tile when arranging (visual spacing between tiles)
    /// <summary>
    /// Styled property for the global margin around tiles.
    /// </summary>
    public static readonly StyledProperty<double> TileMarginProperty =
        AvaloniaProperty.Register<DashboardPanel, double>(nameof(TileMargin), 4d);

    /// <summary>
    /// Attached property for the width (span in columns) of tile tiles.
    /// </summary>
    public static readonly AttachedProperty<int> WProperty =
        AvaloniaProperty.RegisterAttached<DashboardPanel, Control, int>("W", 1);

    /// <summary>
    /// Attached property for the X coordinate (column index) of a tile tile.
    /// </summary>
    public static readonly AttachedProperty<int> XProperty =
        AvaloniaProperty.RegisterAttached<DashboardPanel, Control, int>("X");

    /// <summary>
    /// Attached property for the Y coordinate (row index) of a tile tile.
    /// </summary>
    public static readonly AttachedProperty<int> YProperty =
        AvaloniaProperty.RegisterAttached<DashboardPanel, Control, int>("Y");

    // Keep a mapping of last known valid position/size for each tile
    private readonly Dictionary<Control, (int x, int y, int w, int h)> _lastValid = new Dictionary<Control, (int x, int y, int w, int h)>();

    // Overlay control used to draw the snap preview
    private readonly PreviewOverlay? _overlay;

    private Control?[,]? _cells;

    /// <summary>
    /// Creates a new DashboardPanel and inserts the preview overlay into the visual tree.
    /// </summary>
    public DashboardPanel()
    {
        _overlay = new PreviewOverlay { IsHitTestVisible = false };

        // Insert overlay as first tile so it renders behind other tiles (we draw background grid here)
        Children.Insert(0, _overlay);
    }

    /// <summary>
    /// Brush for even columns.
    /// </summary>
    public IBrush ColumnFillA
    {
        get { return GetValue(ColumnFillAProperty); }
        set { SetValue(ColumnFillAProperty, value); }
    }

    /// <summary>
    /// Brush for odd columns.
    /// </summary>
    public IBrush ColumnFillB
    {
        get { return GetValue(ColumnFillBProperty); }
        set { SetValue(ColumnFillBProperty, value); }
    }

    /// <summary>
    /// Number of columns.
    /// </summary>
    public int Columns
    {
        get { return GetValue(ColumnsProperty); }
        set { SetValue(ColumnsProperty, value); }
    }

    /// <summary>
    /// Brush used to draw grid lines (may be transparent).
    /// </summary>
    public IBrush GridLineBrush
    {
        get { return GetValue(GridLineBrushProperty); }
        set { SetValue(GridLineBrushProperty, value); }
    }

    /// <summary>
    /// Thickness used when drawing grid lines.
    /// </summary>
    public double GridLineThickness
    {
        get { return GetValue(GridLineThicknessProperty); }
        set { SetValue(GridLineThicknessProperty, value); }
    }

    /// <summary>
    /// Brush used to fill an invalid snap preview overlay.
    /// </summary>
    public IBrush PreviewInvalidFill
    {
        get { return GetValue(PreviewInvalidFillProperty); }
        set { SetValue(PreviewInvalidFillProperty, value); }
    }

    /// <summary>
    /// Brush used to stroke an invalid snap preview overlay.
    /// </summary>
    public IBrush PreviewInvalidStroke
    {
        get { return GetValue(PreviewInvalidStrokeProperty); }
        set { SetValue(PreviewInvalidStrokeProperty, value); }
    }

    // Preview overlays (green/red) - keep defaults from previous implementation
    /// <summary>
    /// Brush used to fill a valid snap preview overlay.
    /// </summary>
    public IBrush PreviewValidFill
    {
        get { return GetValue(PreviewValidFillProperty); }
        set { SetValue(PreviewValidFillProperty, value); }
    }

    /// <summary>
    /// Brush used to stroke a valid snap preview overlay.
    /// </summary>
    public IBrush PreviewValidStroke
    {
        get { return GetValue(PreviewValidStrokeProperty); }
        set { SetValue(PreviewValidStrokeProperty, value); }
    }

    /// <summary>
    /// Brush used to tint rows.
    /// </summary>
    public IBrush RowFill
    {
        get { return GetValue(RowFillProperty); }
        set { SetValue(RowFillProperty, value); }
    }

    /// <summary>
    /// Number of rows.
    /// </summary>
    public int Rows
    {
        get { return GetValue(RowsProperty); }
        set { SetValue(RowsProperty, value); }
    }

    /// <summary>
    /// Global corner radius applied to tiles.
    /// </summary>
    public CornerRadius TileCornerRadius
    {
        get { return GetValue(TileCornerRadiusProperty); }
        set { SetValue(TileCornerRadiusProperty, value); }
    }

    /// <summary>
    /// Global BorderBrush applied to tiles.
    /// </summary>
    public IBrush? TileBorderBrush
    {
        get { return GetValue(TileBorderBrushProperty); }
        set { SetValue(TileBorderBrushProperty, value); }
    }

    /// <summary>
    /// Global BorderThickness applied to tiles.
    /// </summary>
    public Thickness TileBorderThickness
    {
        get { return GetValue(TileBorderThicknessProperty); }
        set { SetValue(TileBorderThicknessProperty, value); }
    }

    /// <summary>
    /// Global margin in device-independent units applied around each tile.
    /// </summary>
    public double TileMargin
    {
        get { return GetValue(TileMarginProperty); }
        set { SetValue(TileMarginProperty, value); }
    }

    /// <summary>
    /// Get the attached X coordinate for a tile control.
    /// </summary>
    public static int GetX(Control control)
    {
        return control.GetValue(XProperty);
    }

    /// <summary>
    /// Set the attached X coordinate for a tile control.
    /// </summary>
    public static void SetX(Control control, int x)
    {
        control.SetValue(XProperty, x);
    }

    /// <summary>
    /// Get the attached Y coordinate for a tile control.
    /// </summary>
    public static int GetY(Control control)
    {
        return control.GetValue(YProperty);
    }

    /// <summary>
    /// Set the attached Y coordinate for a tile control.
    /// </summary>
    public static void SetY(Control control, int y)
    {
        control.SetValue(YProperty, y);
    }

    /// <summary>
    /// Get the attached width (span in columns) for a tile control.
    /// </summary>
    public static int GetW(Control control)
    {
        return control.GetValue(WProperty);
    }

    /// <summary>
    /// Set the attached width (span in columns) for a tile control.
    /// </summary>
    public static void SetW(Control control, int w)
    {
        control.SetValue(WProperty, w);
    }

    /// <summary>
    /// Get the attached height (span in rows) for a tile control.
    /// </summary>
    public static int GetH(Control control)
    {
        return control.GetValue(HProperty);
    }

    /// <summary>
    /// Set the attached height (span in rows) for a tile control.
    /// </summary>
    public static void SetH(Control control, int h)
    {
        control.SetValue(HProperty, h);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var cell = GetCellSize(availableSize);
        var margin = Math.Max(0, TileMargin);
        foreach (var tile in Children)
        {
            if (tile == _overlay)
                continue; // Skip overlay from layout/measurement
            var w = Math.Max(1, GetW(tile));
            var h = Math.Max(1, GetH(tile));
            var size = new Size(cell.Width * w, cell.Height * h);

            // Deflate for margin so content is measured with actual inner space
            size = new Size(Math.Max(0, size.Width - (margin * 2)), Math.Max(0, size.Height - (margin * 2)));
            tile.Measure(size);
        }

        // We always take the available size
        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var cell = GetCellSize(finalSize);
        var margin = Math.Max(0, TileMargin);

        BuildOccupancy(); // From last valid or current props

        foreach (var tile in Children)
        {
            if (tile == _overlay)
                continue; // Skip overlay from grid arrangement
            var x = Clamp(GetX(tile), 0, Columns - 1);
            var y = Clamp(GetY(tile), 0, Rows - 1);
            var w = Clamp(Math.Max(1, GetW(tile)), 1, Columns - x);
            var h = Clamp(Math.Max(1, GetH(tile)), 1, Rows - y);

            // Ensure no overlap: if occupied, restore last valid or find nearest
            if (!IsFree(x, y, w, h, tile))
            {
                // Try last known valid placement
                if (_lastValid.TryGetValue(tile, out var last) && IsFree(last.x, last.y, last.w, last.h, tile))
                    (x, y, w, h) = last;
                else
                {
                    var best = FindNearestFree(x, y, w, h);
                    if (best is not null)
                        (x, y) = best.Value;
                }
            }

            var rect = new Rect(x * cell.Width, y * cell.Height, w * cell.Width, h * cell.Height);
            if (margin > 0)
                rect = rect.Deflate(new Thickness(margin));
            tile.Arrange(rect);
            _lastValid[tile] = (x, y, w, h);
            Mark(x, y, w, h, tile);
        }

        // Arrange overlay to cover entire panel so it can draw the preview on top
        if (_overlay != null)
        {
            _overlay.Measure(finalSize);
            _overlay.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
        }

        return finalSize;
    }

    /// <summary>
    /// Returns the cell size (width, height) for the given reference size.
    /// </summary>
    public (double Width, double Height) GetCellSize(Size reference)
    {
        var cw = Columns > 0 ? reference.Width / Columns : reference.Width;
        var ch = Rows > 0 ? reference.Height / Rows : reference.Height;

        return (cw, ch);
    }

    private static int Clamp(int i, int min, int max)
    {
        return Math.Min(Math.Max(i, min), max);
    }

    /// <summary>
    /// Attempts to resolve a move for a tile to the requested coordinates.
    /// Returns true when resolved exactly, false when a fallback was used.
    /// </summary>
    public bool TryResolveMove(Control tile, int targetX, int targetY, out int resolvedX, out int resolvedY)
    {
        var w = Math.Max(1, GetW(tile));
        var h = Math.Max(1, GetH(tile));

        targetX = Math.Clamp(targetX, 0, Columns - w);
        targetY = Math.Clamp(targetY, 0, Rows - h);

        BuildOccupancy(tile);
        if (IsFree(targetX, targetY, w, h))
        {
            resolvedX = targetX;
            resolvedY = targetY;
            return true;
        }

        // Find nearest free top-left position (by Manhattan distance)
        var best = FindNearestFree(targetX, targetY, w, h);
        if (best is not null)
        {
            (resolvedX, resolvedY) = best.Value;
            return true;
        }

        // Fallback to last valid placement if available
        if (_lastValid.TryGetValue(tile, out var last))
        {
            resolvedX = last.x;
            resolvedY = last.y;
            return false;
        }

        resolvedX = targetX;
        resolvedY = targetY;
        return false;
    }

    /// <summary>
    /// Attempts to resolve a resize to the requested width/height in cells.
    /// Returns true when resolved exactly, false when a fallback was used.
    /// </summary>
    public bool TryResolveResize(Control tile, int targetW, int targetH, out int resolvedW, out int resolvedH)
    {
        var x = Math.Max(0, GetX(tile));
        var y = Math.Max(0, GetY(tile));

        targetW = Math.Clamp(targetW, 1, Columns - x);
        targetH = Math.Clamp(targetH, 1, Rows - y);

        BuildOccupancy(tile);
        if (IsFree(x, y, targetW, targetH))
        {
            resolvedW = targetW;
            resolvedH = targetH;
            return true;
        }

        // Shrink until it fits
        for (var h = targetH; h >= 1; h--)
        {
            for (var w = targetW; w >= 1; w--)
            {
                if (IsFree(x, y, w, h))
                {
                    resolvedW = w;
                    resolvedH = h;
                    return false;
                }
            }
        }

        // Fallback to previous valid size if any
        if (_lastValid.TryGetValue(tile, out var last))
        {
            resolvedW = last.w;
            resolvedH = last.h;
            return false;
        }

        resolvedW = 1;
        resolvedH = 1;
        return false;
    }

    #region Private

    private void BuildOccupancy(Control? exceptControl = null)
    {
        _cells = new Control?[Columns, Rows];
        foreach (var child in Children)
        {
            if (child == exceptControl)
                continue;
            if (child == _overlay)
                continue; // Ignore overlay for occupancy

            // Take last known valid if available (more stable)
            int x, y, w, h;
            if (_lastValid.TryGetValue(child, out var last))
                (x, y, w, h) = last;
            else
            {
                (x, y, w, h) = (Clamp(GetX(child), 0, Columns - 1),
                                Clamp(GetY(child), 0, Rows - 1),
                                Clamp(Math.Max(1, GetW(child)), 1, Columns - GetX(child)),
                                Clamp(Math.Max(1, GetH(child)), 1, Rows - GetY(child)));
            }

            Mark(x, y, w, h, child);
        }
    }

    // Mark cells occupied by a control
    private void Mark(int x, int y, int w, int h, Control control)
    {
        if (_cells is null)
            return;
        for (var cx = x; cx < x + w; cx++)
        {
            for (var cy = y; cy < y + h; cy++)
            {
                if (cx >= 0 && cy >= 0 && cx < Columns && cy < Rows)
                    _cells[cx, cy] = control;
            }
        }
    }

    // Check if a rectangular region is free (optionally ignoring a control)
    private bool IsFree(int x, int y, int w, int h, Control? ignoreControl = null)
    {
        if (_cells is null)
            return true;
        for (var cx = x; cx < x + w; cx++)
        {
            for (var cy = y; cy < y + h; cy++)
            {
                if (cx < 0 || cy < 0 || cx >= Columns || cy >= Rows)
                    return false;
                var occ = _cells[cx, cy];
                if (occ is not null && occ != ignoreControl)
                    return false;
            }
        }
        return true;
    }

    // Find nearest free top-left position using expanding Manhattan radius
    private (int x, int y)? FindNearestFree(int aroundX, int aroundY, int w, int h)
    {
        var maxR = Math.Max(Columns, Rows);
        for (var r = 0; r <= maxR; r++)
        {
            for (var x = Math.Max(0, aroundX - r); x <= Math.Min(Columns - w, aroundX + r); x++)
            {
                // Top and bottom bands
                var y1 = Math.Max(0, aroundY - r);
                var y2 = Math.Min(Rows - h, aroundY + r);
                if (IsFree(x, y1, w, h))
                    return (x, y1);
                if (IsFree(x, y2, w, h))
                    return (x, y2);
            }
            for (var y = Math.Max(0, aroundY - r + 1); y <= Math.Min(Rows - h, aroundY + r - 1); y++)
            {
                // Left and right bands
                var x1 = Math.Max(0, aroundX - r);
                var x2 = Math.Min(Columns - w, aroundX + r);
                if (IsFree(x1, y, w, h))
                    return (x1, y);
                if (IsFree(x2, y, w, h))
                    return (x2, y);
            }
        }
        return null;
    }

    #endregion

    /// <summary>
    /// Show a snap preview rectangle at the specified grid coordinates.
    /// </summary>
    public void ShowSnapPreview(int x, int y, int w, int h, bool valid)
    {
        _overlay?.SetPreview(x, y, w, h, valid);
    }

    /// <summary>
    /// Hide any active snap preview.
    /// </summary>
    public void HideSnapPreview()
    {
        _overlay?.SetPreview();
    }

    #region Nested Type: PreviewOverlay

    private sealed class PreviewOverlay : Control
    {
        private (int x, int y, int w, int h, bool valid)? _pos;

        public void SetPreview((int x, int y, int w, int h, bool valid)? pos = null)
        {
            _pos = pos;
            InvalidateVisual();
        }

        public void SetPreview(int x, int y, int w, int h, bool valid)
        {
            SetPreview((x, y, w, h, valid));
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var panel = (DashboardPanel?) Parent;
            var cols = Math.Max(1, panel?.Columns ?? 1);
            var rows = Math.Max(1, panel?.Rows ?? 1);
            var cw = Bounds.Width / cols;
            var ch = Bounds.Height / rows;

            // Column fills from properties
            var colFillA = panel?.ColumnFillA ?? new SolidColorBrush(Color.FromArgb(40, 200, 200, 200));
            var colFillB = panel?.ColumnFillB ?? new SolidColorBrush(Color.FromArgb(20, 200, 200, 200));
            for (var x = 0; x < cols; x++)
            {
                var r = new Rect(x * cw, 0, cw, Bounds.Height);
                context.FillRectangle(x % 2 == 0 ? colFillA : colFillB, r);
            }

            // Row fills from property
            var rowFill = panel?.RowFill ?? new SolidColorBrush(Color.FromArgb(10, 150, 150, 150));
            for (var y = 0; y < rows; y++)
            {
                var r = new Rect(0, y * ch, Bounds.Width, ch);
                if (y % 2 == 0)
                    context.FillRectangle(rowFill, r);
            }

            // Grid lines if brush is not fully transparent
            var gridBrush = panel?.GridLineBrush;
            var drawGrid = !(gridBrush is SolidColorBrush scb && scb.Color.A == 0);
            if (gridBrush is null)
                drawGrid = false;

            if (drawGrid)
            {
                var thickness = Math.Max(0.0, panel?.GridLineThickness ?? 1.0);
                var gridPen = new Pen(gridBrush!, thickness);
                for (var x = 1; x < cols; x++)
                {
                    var gx = x * cw;
                    context.DrawLine(gridPen, new Point(gx, 0), new Point(gx, Bounds.Height));
                }
                for (var y = 1; y < rows; y++)
                {
                    var gy = y * ch;
                    context.DrawLine(gridPen, new Point(0, gy), new Point(Bounds.Width, gy));
                }
            }

            // Draw preview on top if any
            if (_pos is { } p)
            {
                var rect = new Rect(p.x * cw, p.y * ch, p.w * cw, p.h * ch);

                var fill = p.valid
                    ? panel?.PreviewValidFill ?? new SolidColorBrush(Color.FromArgb(48, 34, 197, 94))
                    : panel?.PreviewInvalidFill ?? new SolidColorBrush(Color.FromArgb(48, 239, 68, 68));
                var stroke = p.valid
                    ? panel?.PreviewValidStroke ?? new SolidColorBrush(Color.FromArgb(160, 34, 197, 94))
                    : panel?.PreviewInvalidStroke ?? new SolidColorBrush(Color.FromArgb(160, 239, 68, 68));

                context.FillRectangle(fill, rect);
                context.DrawRectangle(new Pen(stroke, 2, new DashStyle([4, 4], 0)), rect);
            }
        }
    }

    #endregion
}
