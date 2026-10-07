// Controls/Tile.cs

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace MiniDashboard.Avalonia;

/// <summary>
/// Tile base control used in dashboard panels.
/// </summary>
public class Tile : ContentControl
{
    /// <summary>Defines the row span of this tile.</summary>
    public static readonly StyledProperty<int> GridHProperty =
        AvaloniaProperty.Register<Tile, int>(nameof(GridH), 1);

    /// <summary>Defines the column span of this tile.</summary>
    public static readonly StyledProperty<int> GridWProperty =
        AvaloniaProperty.Register<Tile, int>(nameof(GridW), 1);

    /// <summary>Defines the column position of this tile.</summary>
    public static readonly StyledProperty<int> GridXProperty =
        AvaloniaProperty.Register<Tile, int>(nameof(GridX));

    /// <summary>Defines the row position of this tile.</summary>
    public static readonly StyledProperty<int> GridYProperty =
        AvaloniaProperty.Register<Tile, int>(nameof(GridY));

    /// <summary>Defines custom header content replacing the title.</summary>
    public static readonly StyledProperty<object?> HeaderContentProperty =
        AvaloniaProperty.Register<Tile, object?>(nameof(HeaderContent));

    /// <summary>Defines the template rendering custom header content.</summary>
    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<Tile, IDataTemplate?>(nameof(HeaderTemplate));

    /// <summary>Defines whether the header area is visible.</summary>
    public static readonly StyledProperty<bool> IsHeaderVisibleProperty =
        AvaloniaProperty.Register<Tile, bool>(nameof(IsHeaderVisible), true);

    /// <summary>
    /// Whether the current placement is valid (used to indicate valid/invalid snap while resizing).
    /// </summary>
    public static readonly StyledProperty<bool> IsPlacementValidProperty =
        AvaloniaProperty.Register<Tile, bool>(nameof(IsPlacementValid), true);

    /// <summary>Defines whether the tile can be resized by the user.</summary>
    public static readonly StyledProperty<bool> IsResizableProperty =
        AvaloniaProperty.Register<Tile, bool>(nameof(IsResizable), true);

    /// <summary>Defines the minimum row span of this tile.</summary>
    public static readonly StyledProperty<int> MinGridHProperty =
        AvaloniaProperty.Register<Tile, int>(nameof(MinGridH), 1);

    /// <summary>Defines the minimum column span of this tile.</summary>
    public static readonly StyledProperty<int> MinGridWProperty =
        AvaloniaProperty.Register<Tile, int>(nameof(MinGridW), 1);

    /// <summary>Defines the background brush of the tile chrome.</summary>
    public static readonly StyledProperty<IBrush?> TileBackgroundProperty =
        AvaloniaProperty.Register<Tile, IBrush?>(nameof(TileBackground));

    /// <summary>Defines the foreground brush of the tile chrome.</summary>
    public static readonly StyledProperty<IBrush?> TileForegroundProperty =
        AvaloniaProperty.Register<Tile, IBrush?>(nameof(TileForeground));

    /// <summary>Defines the default header title.</summary>
    public static readonly StyledProperty<string?> TileHeaderProperty =
        AvaloniaProperty.Register<Tile, string?>(nameof(TileHeader), "Tile");

    /// <summary>Defines optional actions displayed by the common header.</summary>
    public static readonly StyledProperty<FlyoutBase?> HeaderActionsProperty =
        AvaloniaProperty.Register<Tile, FlyoutBase?>(nameof(HeaderActions));

    /// <summary>Gets or sets an independent header flyout; hosted content keeps its own context menu.</summary>
    public FlyoutBase? HeaderActions
    {
        get { return GetValue(HeaderActionsProperty); }
        set { SetValue(HeaderActionsProperty, value); }
    }

    /// <summary>Whether this host permits an individual resize grip.</summary>
    public static readonly DirectProperty<Tile, bool> IsResizeGripVisibleProperty =
        AvaloniaProperty.RegisterDirect<Tile, bool>(nameof(IsResizeGripVisible), t => t.IsResizeGripVisible);

    /// <summary>Whether this host permits an individual resize grip.</summary>
    public bool IsResizeGripVisible => IsResizable && _matrixHeaderVisible is null;

    /// <summary>Whether the header is visible in the current host.</summary>
    public static readonly DirectProperty<Tile, bool> IsHeaderPresentedProperty =
        AvaloniaProperty.RegisterDirect<Tile, bool>(nameof(IsHeaderPresented), t => t.IsHeaderPresented);

    /// <summary>Whether the header is visible in the current host.</summary>
    public bool IsHeaderPresented => _matrixHeaderVisible ?? IsHeaderVisible;

    /// <summary>The combined host and tile action menu.</summary>
    public static readonly DirectProperty<Tile, FlyoutBase?> EffectiveHeaderActionsProperty =
        AvaloniaProperty.RegisterDirect<Tile, FlyoutBase?>(nameof(EffectiveHeaderActions), t => t.EffectiveHeaderActions);

    /// <summary>The combined host and tile action menu.</summary>
    public FlyoutBase? EffectiveHeaderActions => _matrixActions ?? HeaderActions;

    /// <summary>The flyout opened by the empty-state add-content button.</summary>
    public static readonly DirectProperty<Tile, FlyoutBase?> AddContentActionsProperty =
        AvaloniaProperty.RegisterDirect<Tile, FlyoutBase?>(nameof(AddContentActions), t => t.AddContentActions);

    /// <summary>Gets the flyout opened by the empty-state add-content button; falls back to the header actions.</summary>
    public FlyoutBase? AddContentActions => _matrixAddActions ?? _addContentActions ?? EffectiveHeaderActions;

    private bool? _matrixHeaderVisible;
    private FlyoutBase? _matrixActions;
    private FlyoutBase? _addContentActions;
    private FlyoutBase? _matrixAddActions;

    internal void SetMatrixHost(bool? headers, FlyoutBase? actions, FlyoutBase? addActions)
    {
        var resize = IsResizeGripVisible;
        var header = IsHeaderPresented;
        var menu = EffectiveHeaderActions;
        var add = AddContentActions;
        _matrixHeaderVisible = headers;
        _matrixActions = actions;
        _matrixAddActions = addActions;
        RaisePropertyChanged(IsResizeGripVisibleProperty, resize, IsResizeGripVisible);
        RaisePropertyChanged(IsHeaderPresentedProperty, header, IsHeaderPresented);
        RaisePropertyChanged(EffectiveHeaderActionsProperty, menu, EffectiveHeaderActions);
        RaisePropertyChanged(AddContentActionsProperty, add, AddContentActions);
        UpdateOverlayActions();
    }

    internal void SetAddContentActions(FlyoutBase? flyout)
    {
        var previous = AddContentActions;
        _addContentActions = flyout;
        RaisePropertyChanged(AddContentActionsProperty, previous, AddContentActions);
    }

    private void UpdateOverlayActions() => PseudoClasses.Set(":overlay-actions", _matrixHeaderVisible is null && !IsHeaderPresented && EffectiveHeaderActions is not null);

    private DashboardTileResizeBehavior? _resizeBehavior;

    private ContentPresenter? _headerPresenter;

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

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Ensure the dashboard panel has the tile's values applied
        PushAllToDashboard();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _resizeBehavior?.Cancel();
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        (_resizeBehavior ??= new DashboardTileResizeBehavior(this)).Attach(e.NameScope.Find<Thumb>("PART_ResizeThumb_HitArea"),
                                                                           e.NameScope.Find<Thumb>("PART_ResizeThumb"));

        // Compatibility with application templates using the original named presenter.
        _headerPresenter = e.NameScope.Find<ContentPresenter>("PART_HeaderPresenter");
        SyncHeader();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsResizableProperty)
        {
            if (!IsResizable)
                _resizeBehavior?.Cancel();
            
            RaisePropertyChanged(IsResizeGripVisibleProperty, change.GetOldValue<bool>() && _matrixHeaderVisible is null, IsResizeGripVisible);
        }
        
        if (change.Property == IsHeaderVisibleProperty)
            RaisePropertyChanged(IsHeaderPresentedProperty, _matrixHeaderVisible ?? change.GetOldValue<bool>(), IsHeaderPresented);
        
        if (change.Property == HeaderActionsProperty)
        {
            RaisePropertyChanged(EffectiveHeaderActionsProperty, _matrixActions ?? change.GetOldValue<FlyoutBase?>(), EffectiveHeaderActions);
            RaisePropertyChanged(AddContentActionsProperty,
                                 _matrixAddActions ?? _addContentActions ?? _matrixActions ?? change.GetOldValue<FlyoutBase?>(), AddContentActions);
        }
        
        if (change.Property == IsHeaderVisibleProperty || change.Property == HeaderActionsProperty)
            UpdateOverlayActions();
        
        if (change.Property == HeaderContentProperty || change.Property == HeaderTemplateProperty ||
            change.Property == TileHeaderProperty)
            SyncHeader();
    }

    private void SyncHeader()
    {
        if (_headerPresenter is null)
            return;
        
        _headerPresenter.ContentTemplate = HeaderTemplate;
        _headerPresenter.Content = HeaderContent ?? TileHeader;
    }

    private static void SyncLocation(AvaloniaPropertyChangedEventArgs args)
    {
        if (args.Sender is Tile tile)
            tile.PushAllToDashboard();
    }

    // Push current tile grid properties to the parent DashboardPanel attached properties
    private void PushAllToDashboard()
    {
        if (ResolveDashboardPanel() is not null)
        {
            DashboardPanel.SetX(this, GridX);
            DashboardPanel.SetY(this, GridY);
            DashboardPanel.SetW(this, Math.Max(1, GridW));
            DashboardPanel.SetH(this, Math.Max(1, GridH));
        }
    }

    private DashboardPanel? ResolveDashboardPanel()
    {
        return Parent as DashboardPanel;
    }
}
