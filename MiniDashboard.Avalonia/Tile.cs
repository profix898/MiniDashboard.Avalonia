// Controls/Tile.cs

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace MiniDashboard.Avalonia;

/// <summary>
/// Tile base control used in dashboard panels.
/// </summary>
public class Tile : ContentControl
{
    public static readonly StyledProperty<int> GridHProperty =
        AvaloniaProperty.Register<Tile, int>(nameof(GridH), 1);

    public static readonly StyledProperty<int> GridWProperty =
        AvaloniaProperty.Register<Tile, int>(nameof(GridW), 1);

    // Grid position & span
    public static readonly StyledProperty<int> GridXProperty =
        AvaloniaProperty.Register<Tile, int>(nameof(GridX));

    public static readonly StyledProperty<int> GridYProperty =
        AvaloniaProperty.Register<Tile, int>(nameof(GridY));

    public static readonly StyledProperty<object?> HeaderContentProperty =
        AvaloniaProperty.Register<Tile, object?>(nameof(HeaderContent));

    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<Tile, IDataTemplate?>(nameof(HeaderTemplate));

    // Header controls (visible by default)
    public static readonly StyledProperty<bool> IsHeaderVisibleProperty =
        AvaloniaProperty.Register<Tile, bool>(nameof(IsHeaderVisible), true);

    /// <summary>
    /// Whether the current placement is valid (used to indicate valid/invalid snap while resizing).
    /// </summary>
    public static readonly StyledProperty<bool> IsPlacementValidProperty =
        AvaloniaProperty.Register<Tile, bool>(nameof(IsPlacementValid), true);

    // Resize grip toggle
    public static readonly StyledProperty<bool> IsResizableProperty =
        AvaloniaProperty.Register<Tile, bool>(nameof(IsResizable), true);

    public static readonly StyledProperty<int> MinGridHProperty =
        AvaloniaProperty.Register<Tile, int>(nameof(MinGridH), 1);

    // Size constraints in cells
    public static readonly StyledProperty<int> MinGridWProperty =
        AvaloniaProperty.Register<Tile, int>(nameof(MinGridW), 1);

    // Custom chrome (keep only custom properties; use base BorderBrush/Thickness/CornerRadius/Padding from TemplatedControl)
    public static readonly StyledProperty<IBrush?> TileBackgroundProperty =
        AvaloniaProperty.Register<Tile, IBrush?>(nameof(TileBackground));

    public static readonly StyledProperty<IBrush?> TileForegroundProperty =
        AvaloniaProperty.Register<Tile, IBrush?>(nameof(TileForeground));

    public static readonly StyledProperty<string?> TileHeaderProperty =
        AvaloniaProperty.Register<Tile, string?>(nameof(TileHeader), "Tile");

    private int _resizeLastWantedH;

    private int _resizeLastWantedW;

    private DashboardPanel? _resizePanel;
    private int _resizeStartGridH;
    private int _resizeStartGridW;

    // --- Changed fields: track resize start + last wanted cell deltas and panel ---
    private int _resizeStartGridX;
    private int _resizeStartGridY;

    static Tile()
    {
        // Keep tile position in sync with attached panel coordinates
        GridXProperty.Changed.Subscribe(SyncLocation);
        GridYProperty.Changed.Subscribe(SyncLocation);
        GridWProperty.Changed.Subscribe(SyncLocation);
        GridHProperty.Changed.Subscribe(SyncLocation);
    }

    /// <summary>
    /// Grid height (span in rows) for this tile.
    /// </summary>
    public int GridH
    {
        get { return GetValue(GridHProperty); }
        set { SetValue(GridHProperty, value); }
    }

    /// <summary>
    /// Grid width (span in columns) for this tile.
    /// </summary>
    public int GridW
    {
        get { return GetValue(GridWProperty); }
        set { SetValue(GridWProperty, value); }
    }

    /// <summary>
    /// Grid X coordinate for this tile.
    /// </summary>
    public int GridX
    {
        get { return GetValue(GridXProperty); }
        set { SetValue(GridXProperty, value); }
    }

    /// <summary>
    /// Grid Y coordinate for this tile.
    /// </summary>
    public int GridY
    {
        get { return GetValue(GridYProperty); }
        set { SetValue(GridYProperty, value); }
    }

    /// <summary>
    /// Custom header content for the tile.
    /// </summary>
    public object? HeaderContent
    {
        get { return GetValue(HeaderContentProperty); }
        set { SetValue(HeaderContentProperty, value); }
    }

    /// <summary>
    /// Template to render the header content.
    /// </summary>
    public IDataTemplate? HeaderTemplate
    {
        get { return GetValue(HeaderTemplateProperty); }
        set { SetValue(HeaderTemplateProperty, value); }
    }

    /// <summary>
    /// Whether the header area is visible.
    /// </summary>
    public bool IsHeaderVisible
    {
        get { return GetValue(IsHeaderVisibleProperty); }
        set { SetValue(IsHeaderVisibleProperty, value); }
    }

    /// <summary>
    /// Indicates if the current placement is valid.
    /// </summary>
    public bool IsPlacementValid
    {
        get { return GetValue(IsPlacementValidProperty); }
        set { SetValue(IsPlacementValidProperty, value); }
    }

    /// <summary>
    /// Whether the tile is resizable by the user.
    /// </summary>
    public bool IsResizable
    {
        get { return GetValue(IsResizableProperty); }
        set { SetValue(IsResizableProperty, value); }
    }

    /// <summary>
    /// Minimum grid height.
    /// </summary>
    public int MinGridH
    {
        get { return GetValue(MinGridHProperty); }
        set { SetValue(MinGridHProperty, value); }
    }

    /// <summary>
    /// Minimum grid width.
    /// </summary>
    public int MinGridW
    {
        get { return GetValue(MinGridWProperty); }
        set { SetValue(MinGridWProperty, value); }
    }

    /// <summary>
    /// Background brush for the tile chrome.
    /// </summary>
    public IBrush? TileBackground
    {
        get { return GetValue(TileBackgroundProperty); }
        set { SetValue(TileBackgroundProperty, value); }
    }

    /// <summary>
    /// Foreground brush for the tile chrome.
    /// </summary>
    public IBrush? TileForeground
    {
        get { return GetValue(TileForegroundProperty); }
        set { SetValue(TileForegroundProperty, value); }
    }

    /// <summary>
    /// Default header text.
    /// </summary>
    public string? TileHeader
    {
        get { return GetValue(TileHeaderProperty); }
        set { SetValue(TileHeaderProperty, value); }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Ensure the dashboard panel has the tile's values applied
        PushAllToDashboard();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Attach handlers to both the visible small grip and the larger invisible hit-area
        var hitThumb = e.NameScope.Find<Thumb>("PART_ResizeThumb_HitArea");
        var thumb = e.NameScope.Find<Thumb>("PART_ResizeThumb");

        void AttachHandlers(Thumb t)
        {
            t.DragStarted -= OnResizeStarted;
            t.DragStarted += OnResizeStarted;

            // use pointer-move based resizing, do not rely on per-event vector deltas
            t.DragCompleted -= OnResizeCompleted;
            t.DragCompleted += OnResizeCompleted;
        }

        if (hitThumb != null)
            AttachHandlers(hitThumb);
        if (thumb != null)
            AttachHandlers(thumb);

        // header presenter sync (pick HeaderContent or TileHeader string)
        if (e.NameScope.Find<ContentPresenter>("PART_HeaderPresenter") is { } cp)
        {
            void Sync()
            {
                cp.ContentTemplate = HeaderTemplate;
                cp.Content = HeaderContent ?? TileHeader;
            }

            Sync();

            HeaderContentProperty.Changed.Subscribe(_ => Sync());
            HeaderTemplateProperty.Changed.Subscribe(_ => Sync());
            TileHeaderProperty.Changed.Subscribe(_ => Sync());
        }
    }

    private void OnResizeStarted(object? sender, VectorEventArgs e)
    {
        if (this.GetVisualAncestors().OfType<DashboardPanel>().FirstOrDefault() is { } panel)
        {
            _resizePanel = panel;

            // capture starting grid pos and size
            // Use attached X/Y from the panel to ensure we anchor to the tile's actual arranged cell coordinates
            _resizeStartGridX = DashboardPanel.GetX(this);
            _resizeStartGridY = DashboardPanel.GetY(this);
            _resizeStartGridW = GridW;
            _resizeStartGridH = GridH;

            // initial preview at current arranged position
            panel.ShowSnapPreview(_resizeStartGridX, _resizeStartGridY, _resizeStartGridW, _resizeStartGridH, true);
            IsPlacementValid = true;

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
        wantedW = Math.Max(wantedW, MinGridW);
        wantedH = Math.Max(wantedH, MinGridH);

        // only act when a full-cell boundary was crossed (i.e. wanted changed)
        if (wantedW == _resizeLastWantedW && wantedH == _resizeLastWantedH)
            return;

        _resizeLastWantedW = wantedW;
        _resizeLastWantedH = wantedH;

        var ok = panel.TryResolveResize(this, wantedW, wantedH, out var rw, out var rh);
        var isExact = ok && rw == wantedW && rh == wantedH;

        // apply resolved size to this tile but DO NOT change its position
        GridW = rw;
        GridH = rh;

        // restore/keep GridX/GridY anchored to the captured start position to avoid moving the tile while resizing
        GridX = _resizeStartGridX;
        GridY = _resizeStartGridY;
        PushAllToDashboard();

        // show preview at resolved and mark header validity if it had to adjust
        panel.ShowSnapPreview(GridX, GridY, rw, rh, isExact);
        IsPlacementValid = isExact;
    }

    private void OnResizeCompleted(object? sender, VectorEventArgs e)
    {
        if (_resizePanel is { } panel)
        {
            panel.PointerMoved -= OnResizePointerMoved;
            panel.HideSnapPreview();
        }

        // final push to the panel and force arrange so visual size matches the last resolved values
        PushAllToDashboard();
        if (Parent is DashboardPanel dp)
            dp.InvalidateArrange();

        // reset tracked panel
        _resizePanel = null;

        IsPlacementValid = true;
    }

    private static void SyncLocation(AvaloniaPropertyChangedEventArgs args)
    {
        if (args.Sender is Tile tile)
            tile.PushAllToDashboard();
    }

    // Push current tile grid properties to the parent DashboardPanel attached properties
    private void PushAllToDashboard()
    {
        if (Parent is DashboardPanel)
        {
            DashboardPanel.SetX(this, GridX);
            DashboardPanel.SetY(this, GridY);
            DashboardPanel.SetW(this, Math.Max(1, GridW));
            DashboardPanel.SetH(this, Math.Max(1, GridH));
        }
    }
}
