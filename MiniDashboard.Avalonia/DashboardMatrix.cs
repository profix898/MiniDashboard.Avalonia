using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace MiniDashboard.Avalonia;

/// <summary>A globally editable matrix of content cells, hosted by a native Grid.</summary>
public class DashboardMatrix : TemplatedControl, IDisposable
{
    /// <summary>Defines CanResize.</summary>
    public static readonly StyledProperty<bool> CanResizeProperty =
        AvaloniaProperty.Register<DashboardMatrix, bool>(nameof(CanResize), true);

    /// <summary>Whether splitters accept pointer and keyboard input.</summary>
    public bool CanResize
    {
        get { return GetValue(CanResizeProperty); }
        set { SetValue(CanResizeProperty, value); }
    }

    /// <summary>Defines CanModifyStructure.</summary>
    public static readonly StyledProperty<bool> CanModifyStructureProperty =
        AvaloniaProperty.Register<DashboardMatrix, bool>(nameof(CanModifyStructure), true);

    /// <summary>Whether row and column editing is allowed.</summary>
    public bool CanModifyStructure
    {
        get { return GetValue(CanModifyStructureProperty); }
        set { SetValue(CanModifyStructureProperty, value); }
    }

    /// <summary>Defines DisposeRemovedContent.</summary>
    public static readonly StyledProperty<bool> DisposeRemovedContentProperty =
        AvaloniaProperty.Register<DashboardMatrix, bool>(nameof(DisposeRemovedContent));

    /// <summary>Whether discarded generated controls implementing IDisposable are disposed.</summary>
    public bool DisposeRemovedContent
    {
        get { return GetValue(DisposeRemovedContentProperty); }
        set { SetValue(DisposeRemovedContentProperty, value); }
    }

    /// <summary>Defines MinCellWidth.</summary>
    public static readonly StyledProperty<double> MinCellWidthProperty =
        AvaloniaProperty.Register<DashboardMatrix, double>(nameof(MinCellWidth), 96d, validate: value => Double.IsFinite(value) && value > 0);

    /// <summary>Minimum logical cell width in DIP.</summary>
    public double MinCellWidth
    {
        get { return GetValue(MinCellWidthProperty); }
        set { SetValue(MinCellWidthProperty, value); }
    }

    /// <summary>Defines MinCellHeight.</summary>
    public static readonly StyledProperty<double> MinCellHeightProperty =
        AvaloniaProperty.Register<DashboardMatrix, double>(nameof(MinCellHeight), 72d, validate: value => Double.IsFinite(value) && value > 0);

    /// <summary>Minimum logical cell height in DIP.</summary>
    public double MinCellHeight
    {
        get { return GetValue(MinCellHeightProperty); }
        set { SetValue(MinCellHeightProperty, value); }
    }

    /// <summary>Defines SplitterThickness.</summary>
    public static readonly StyledProperty<double> SplitterThicknessProperty =
        AvaloniaProperty.Register<DashboardMatrix, double>(nameof(SplitterThickness), 1d, validate: value => Double.IsFinite(value) && value > 0);

    /// <summary>Visible divider thickness in DIP. The reserved gap is the larger of this value and CellSpacing.</summary>
    public double SplitterThickness
    {
        get { return GetValue(SplitterThicknessProperty); }
        set { SetValue(SplitterThicknessProperty, value); }
    }

    /// <summary>Defines SplitterHitThickness.</summary>
    public static readonly StyledProperty<double> SplitterHitThicknessProperty =
        AvaloniaProperty.Register<DashboardMatrix, double>(nameof(SplitterHitThickness), 8d, validate: value => Double.IsFinite(value) && value > 0);

    /// <summary>Splitter interaction thickness, overlapping adjacent cell edges.</summary>
    public double SplitterHitThickness
    {
        get { return GetValue(SplitterHitThicknessProperty); }
        set { SetValue(SplitterHitThicknessProperty, value); }
    }

    /// <summary>Defines the uniform gap between cells.</summary>
    public static readonly StyledProperty<double> CellSpacingProperty =
        AvaloniaProperty.Register<DashboardMatrix, double>(nameof(CellSpacing), 6d, validate: v => Double.IsFinite(v) && v >= 0);

    /// <summary>Defines the overlay button size for headerless boundary actions.</summary>
    public static readonly StyledProperty<double> BoundaryActionSizeProperty =
        AvaloniaProperty.Register<DashboardMatrix, double>(nameof(BoundaryActionSize), 24d,
                                                           validate: value => Double.IsFinite(value) && value >= 16);

    /// <summary>Headerless overlay button size in DIP; at least 16, default 24. Does not change cell spacing.</summary>
    public double BoundaryActionSize
    {
        get => GetValue(BoundaryActionSizeProperty);
        set => SetValue(BoundaryActionSizeProperty, value);
    }

    /// <summary>Defines the padding used by the matrix template.</summary>
    public static readonly DirectProperty<DashboardMatrix, Thickness> LayoutPaddingProperty =
        AvaloniaProperty.RegisterDirect<DashboardMatrix, Thickness>(nameof(LayoutPadding), m => m.LayoutPadding);

    /// <summary>Gets the configured padding; overlays never reserve additional space.</summary>
    public Thickness LayoutPadding => Padding;

    private double BoundaryGap => Math.Max(CellSpacing, SplitterThickness);

    private DashboardMatrixBoundaryButton? _pressedBoundary;
    private Point _boundaryPressPoint;
    private bool _boundaryDragged;

    /// <summary>Creates a matrix with pointer-aware boundary overlays.</summary>
    public DashboardMatrix()
    {
        AddHandler(PointerMovedEvent, BoundaryPointerMoved, RoutingStrategies.Tunnel, true);
        AddHandler(PointerPressedEvent, BoundaryPointerPressed, RoutingStrategies.Tunnel, true);
        AddHandler(PointerReleasedEvent, BoundaryPointerReleased, RoutingStrategies.Tunnel, true);
        PointerExited += (_, _) => SetBoundaryHover(null);
        AddHandler(PointerCaptureLostEvent, (_, _) =>
                   {
                       _pressedBoundary = null;
                       SetBoundaryHover(null);
                   },
                   RoutingStrategies.Bubble, true);
    }

    /// <summary>Defines whether catalog titles appear in the common cell header.</summary>
    public static readonly StyledProperty<bool> IsHeaderVisibleProperty =
        AvaloniaProperty.Register<DashboardMatrix, bool>(nameof(IsHeaderVisible), true);

    /// <summary>Gets or sets the inter-cell gap, independent of divider width and hit target.</summary>
    public double CellSpacing
    {
        get => GetValue(CellSpacingProperty);
        set => SetValue(CellSpacingProperty, value);
    }

    /// <summary>Gets or sets whether cells show the common header. Actions remain available when hidden.</summary>
    public bool IsHeaderVisible
    {
        get => GetValue(IsHeaderVisibleProperty);
        set => SetValue(IsHeaderVisibleProperty, value);
    }

    /// <summary>Defines the catalog used to materialize cell assignments.</summary>
    public static readonly StyledProperty<IEnumerable<DashboardContentDefinition>?> ContentDefinitionsProperty =
        AvaloniaProperty.Register<DashboardMatrix, IEnumerable<DashboardContentDefinition>?>(nameof(ContentDefinitions));

    /// <summary>Defines the immutable current layout snapshot.</summary>
    public static readonly DirectProperty<DashboardMatrix, DashboardMatrixLayout> LayoutProperty =
        AvaloniaProperty.RegisterDirect<DashboardMatrix, DashboardMatrixLayout>(nameof(Layout), matrix => matrix.Layout);

    private DashboardMatrixLayout _layout = new DashboardMatrixLayout();
    private Grid? _grid;
    private Dictionary<Guid, DashboardContentInstance> _entries = new Dictionary<Guid, DashboardContentInstance>();
    private readonly Dictionary<Guid, DashboardMatrixCell> _cells = new Dictionary<Guid, DashboardMatrixCell>();
    private DashboardContentCatalog _catalog = new DashboardContentCatalog();
    private INotifyCollectionChanged? _catalogSubscription;
    private bool _syncPending;
    private bool _updating;
    private bool _transaction;
    private bool _disposed;

    /// <summary>Raised before a transaction. Set Cancel to veto; handlers must not mutate this matrix.</summary>
    public event EventHandler<DashboardMatrixChangeEventArgs>? LayoutChanging;

    /// <summary>Raised after a committed transaction. Cancellation is ignored here.</summary>
    public event EventHandler<DashboardMatrixChangeEventArgs>? LayoutChanged;

    /// <summary>Raised before a content change; cancellation vetoes the entire matrix transaction.</summary>
    public event EventHandler<DashboardContentChangingEventArgs>? ContentChanging;

    /// <summary>Raised after content changes have committed.</summary>
    public event EventHandler<DashboardContentChangingEventArgs>? ContentChanged;

    /// <summary>Raised when creating content fails. Public APIs still propagate the exception.</summary>
    public event EventHandler<DashboardContentFailedEventArgs>? ContentFailed;

    /// <summary>Gets or sets the catalog. Observable collection changes refresh instantiated content.</summary>
    public IEnumerable<DashboardContentDefinition>? ContentDefinitions
    {
        get { return GetValue(ContentDefinitionsProperty); }
        set { SetValue(ContentDefinitionsProperty, value); }
    }

    /// <summary>Gets the current immutable layout. Use GetSnapshot to flush pending splitter changes.</summary>
    public DashboardMatrixLayout Layout => _layout;

    /// <summary>Returns the current snapshot after synchronizing native splitter weights.</summary>
    public DashboardMatrixLayout GetSnapshot()
    {
        VerifyAccess();
        
        SynchronizeWeights();
        
        return _layout;
    }

    /// <summary>Restores a validated snapshot; hosts may veto replacement through LayoutChanging.</summary>
    public bool RestoreLayout(DashboardMatrixLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        
        SynchronizeWeights();
        
        return Commit(layout, DashboardMatrixChangeKind.Restore, _layout.Cells);
    }

    /// <summary>Splits the specified row and inserts an empty row before it.</summary>
    public bool InsertRowBefore(int row) => Structure(l => l.InsertRowBefore(row), DashboardMatrixChangeKind.InsertRow);

    /// <summary>Splits the specified row and inserts an empty row after it.</summary>
    public bool InsertRowAfter(int row) => Structure(l => l.InsertRowAfter(row), DashboardMatrixChangeKind.InsertRow);

    /// <summary>Splits the specified column and inserts an empty column before it.</summary>
    public bool InsertColumnBefore(int column) => Structure(l => l.InsertColumnBefore(column), DashboardMatrixChangeKind.InsertColumn);

    /// <summary>Splits the specified column and inserts an empty column after it.</summary>
    public bool InsertColumnAfter(int column) => Structure(l => l.InsertColumnAfter(column), DashboardMatrixChangeKind.InsertColumn);

    /// <summary>Requests removal of a complete row, including its assignments.</summary>
    public bool RemoveRow(int row)
        => Structure(l => l.RemoveRow(row), DashboardMatrixChangeKind.RemoveRow,
                     c => c.Row == row);

    /// <summary>Requests removal of a complete column, including its assignments.</summary>
    public bool RemoveColumn(int column)
        => Structure(l => l.RemoveColumn(column), DashboardMatrixChangeKind.RemoveColumn,
                     c => c.Column == column);

    /// <summary>Assigns a catalog ID. Unknown IDs remain in the model and display a placeholder.</summary>
    public bool SetContent(Guid cell, string? contentId)
    {
        EnsureAvailable();
        
        SynchronizeWeights();
        
        var previous = _layout.GetCell(cell);
        if (previous.ContentId == contentId)
            return false;
        
        return Commit(_layout.WithContent(cell, contentId), DashboardMatrixChangeKind.Content, new[] { previous });
    }

    /// <summary>Clears a cell's assignment.</summary>
    public bool ClearContent(Guid cell) => SetContent(cell, null);

    /// <summary>Moves content to an empty cell without recreating its control. Moving a cell to itself is a no-op.</summary>
    public bool MoveContent(Guid source, Guid target)
    {
        EnsureAvailable();
        
        SynchronizeWeights();
        _layout.GetCell(source);
        var destination = _layout.GetCell(target);
        if (source == target)
            return false;
        if (destination.ContentId is not null)
            throw new InvalidOperationException("MoveContent requires an empty target. Use SwapContent for occupied cells.");
        
        return SwapContent(source, target);
    }

    /// <summary>Swaps assignments and existing control instances without changing matrix structure.</summary>
    public bool SwapContent(Guid first, Guid second)
    {
        EnsureAvailable();
        
        SynchronizeWeights();
        
        var a = _layout.GetCell(first);
        var b = _layout.GetCell(second);
        if (first == second || (a.ContentId is null && b.ContentId is null))
            return false;
        
        var next = _layout.WithContent(first, b.ContentId).WithContent(second, a.ContentId);
        var remapped = new Dictionary<Guid, DashboardContentInstance>(_entries);
        remapped.Remove(first);
        remapped.Remove(second);
        if (_entries.TryGetValue(first, out var ea))
            remapped[second] = ea;
        
        if (_entries.TryGetValue(second, out var eb))
            remapped[first] = eb;
        
        return Commit(next, DashboardMatrixChangeKind.MoveContent, new[] { a, b }, remapped);
    }

    /// <summary>Reconciles catalog changes. Factory failures leave current controls intact.</summary>
    public void RefreshContent()
    {
        RefreshCatalog();
        ReconcileContent();
    }

    private void ReconcileContent()
    {
        EnsureAvailable();
        
        _transaction = true;
        
        try
        {
            var next = Materialize(_layout, _entries);
            var discarded = ReplaceEntries(next);
            RenderCells();
            DisposeEntries(discarded);
        }
        finally
        {
            _transaction = false;
        }
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        SynchronizeWeights();
        ClearGrid();
        base.OnApplyTemplate(e);
        _grid = e.NameScope.Find<Grid>("PART_Grid");
        if (!_disposed)
        {
            ReconcileContent();
            RebuildGrid();
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!_disposed)
        {
            SubscribeCatalog();
            if (ContentDefinitions is INotifyCollectionChanged)
                RefreshCatalog();
            
            ReconcileContent();
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        SynchronizeWeights();
        UnsubscribeCatalog();

        // A tab switch must not destroy hosted controls or their unsaved state.
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_disposed)
            return;
        
        if (change.Property == PaddingProperty)
            RaisePropertyChanged(LayoutPaddingProperty, change.GetOldValue<Thickness>(), LayoutPadding);
        
        if (change.Property == ContentDefinitionsProperty)
        {
            UnsubscribeCatalog();
            if (this.IsAttachedToVisualTree())
                SubscribeCatalog();
            
            RefreshContent();
        }
        else if (change.Property == CanResizeProperty || change.Property == MinCellWidthProperty ||
                 change.Property == MinCellHeightProperty || change.Property == SplitterThicknessProperty ||
                 change.Property == SplitterHitThicknessProperty || change.Property == CellSpacingProperty ||
                 change.Property == IsHeaderVisibleProperty || change.Property == BoundaryActionSizeProperty || change.Property == PaddingProperty)
        {
            SynchronizeWeights();
            RebuildGrid();
        }
        else if (change.Property == CanModifyStructureProperty)
            RenderCells();
    }

    /// <summary>Releases subscriptions and generated content according to DisposeRemovedContent.</summary>
    public void Dispose()
    {
        VerifyAccess();
        if (_disposed)
            return;
        EnsureAvailable();
        
        _disposed = true;
        UnsubscribeCatalog();
        ClearGrid();
        foreach (var cell in _cells.Values)
            cell.Release();
        
        _cells.Clear();
        
        var discarded = _entries;
        _entries = new Dictionary<Guid, DashboardContentInstance>();
        DisposeEntries(discarded.Values);
        GC.SuppressFinalize(this);
    }

    internal IReadOnlyList<DashboardContentDefinition> GetCatalog() => _catalog;

    private void RefreshCatalog()
    {
        EnsureAvailable();
        
        _catalog = ContentDefinitions as DashboardContentCatalog ??
                   new DashboardContentCatalog(ContentDefinitions ?? Array.Empty<DashboardContentDefinition>());
    }

    private bool Structure(Func<DashboardMatrixLayout, DashboardMatrixLayout> edit,
                           DashboardMatrixChangeKind kind, Func<DashboardMatrixCellModel, bool>? affected = null)
    {
        EnsureAvailable();
        if (!CanModifyStructure)
            return false;
        
        SynchronizeWeights();
        
        return Commit(edit(_layout), kind, _layout.Cells.Where(affected ?? (_ => false)).ToArray());
    }

    private bool Commit(DashboardMatrixLayout next, DashboardMatrixChangeKind kind,
                        IReadOnlyList<DashboardMatrixCellModel> affected, Dictionary<Guid, DashboardContentInstance>? candidates = null)
    {
        EnsureAvailable();
        
        _transaction = true;
        
        try
        {
            var args = new DashboardMatrixChangeEventArgs(kind, _layout, next, affected);
            LayoutChanging?.Invoke(this, args);
            if (args.Cancel)
                return false;
            
            var contentChanges = GetContentChanges(next, kind);
            foreach (var change in contentChanges)
            {
                ContentChanging?.Invoke(this, change);
                if (change.Cancel)
                    return false;
            }
            
            var entries = kind == DashboardMatrixChangeKind.Proportions ? _entries : Materialize(next, candidates ?? _entries);
            var discarded = ReplaceEntries(entries);
            SetAndRaise(LayoutProperty, ref _layout, next);
            if (kind is DashboardMatrixChangeKind.Content or DashboardMatrixChangeKind.MoveContent)
                RenderCells();
            else if (kind != DashboardMatrixChangeKind.Proportions)
                RebuildGrid();
            
            try
            {
                DisposeEntries(discarded);
            }
            finally
            {
                foreach (var change in contentChanges)
                    ContentChanged?.Invoke(this, change);
                
                LayoutChanged?.Invoke(this, args);
            }
            
            return true;
        }
        finally
        {
            _transaction = false;
        }
    }

    private DashboardContentChangingEventArgs[] GetContentChanges(DashboardMatrixLayout next, DashboardMatrixChangeKind kind)
    {
        if (kind is DashboardMatrixChangeKind.Proportions or DashboardMatrixChangeKind.InsertRow or DashboardMatrixChangeKind.InsertColumn)
            return Array.Empty<DashboardContentChangingEventArgs>();
        
        var previous = _layout.Cells.ToDictionary(c => c.Id);
        var upcoming = next.Cells.ToDictionary(c => c.Id);
        
        return previous.Values.Select(c => new DashboardContentChangingEventArgs(c.ContentId, upcoming.GetValueOrDefault(c.Id)?.ContentId, c.Id))
                       .Concat(upcoming.Values.Where(c => c.ContentId is not null && !previous.ContainsKey(c.Id))
                                       .Select(c => new DashboardContentChangingEventArgs(null, c.ContentId, c.Id)))
                       .Where(c => c.BeforeId != c.AfterId).ToArray();
    }

    private Dictionary<Guid, DashboardContentInstance> Materialize(DashboardMatrixLayout layout, Dictionary<Guid, DashboardContentInstance> candidates)
    {
        var catalog = _catalog;
        var result = new Dictionary<Guid, DashboardContentInstance>();
        var created = new List<DashboardContentInstance>();
        var used = new HashSet<Control>(_entries.Values.Select(e => e.Control)) { this };
        try
        {
            foreach (var cell in layout.Cells)
            {
                if (cell.ContentId is null)
                    continue;
                
                var definition = catalog.Find(cell.ContentId);
                if (candidates.TryGetValue(cell.Id, out var retained) &&
                    retained.ContentId == cell.ContentId && ReferenceEquals(retained.Definition, definition))
                {
                    result.Add(cell.Id, retained);
                    continue;
                }
                
                var entry = DashboardContentInstance.Create(cell.ContentId, definition, used);
                created.Add(entry);
                result.Add(cell.Id, entry);
            }
            
            return result;
        }
        catch (Exception error)
        {
            try
            {
                DisposeEntries(created);
            }
            finally
            {
                ContentFailed?.Invoke(this, new DashboardContentFailedEventArgs(error));
            }
            throw;
        }
    }

    private DashboardContentInstance[] ReplaceEntries(Dictionary<Guid, DashboardContentInstance> next)
    {
        if (ReferenceEquals(next, _entries))
            return Array.Empty<DashboardContentInstance>();

        // Detach every changed slot before moving existing tiles to their new positions.
        foreach (var pair in _cells.ToArray())
        {
            if (!next.TryGetValue(pair.Key, out var entry) || !ReferenceEquals(pair.Value.Tile, entry.MatrixTile))
            {
                _grid?.Children.Remove(pair.Value.Tile);
                pair.Value.Release();
                _cells.Remove(pair.Key);
            }
        }
        
        var retained = next.Values.Select(e => e.Control).ToHashSet();
        var discarded = _entries.Values.Where(e => !retained.Contains(e.Control)).ToArray();
        _entries = next;
        
        return discarded;
    }

    private void DisposeEntries(IEnumerable<DashboardContentInstance> entries)
    {
        var discarded = entries.ToArray();
        foreach (var entry in discarded)
        {
            if (entry.MatrixTile is DashboardContentTile tile && !ReferenceEquals(tile, entry.Control))
                tile.ReleaseMatrixContent();
        }
        
        DashboardContentInstance.Release(discarded, DisposeRemovedContent);
    }

    private void RebuildGrid()
    {
        if (_grid is null || _disposed)
            return;
        
        _updating = true;
        
        try
        {
            ClearDefinitions();
            ClearBoundaryButtons();
            foreach (var splitter in _grid.Children.OfType<GridSplitter>().ToArray())
                _grid.Children.Remove(splitter);
            
            for (var r = 0; r < _layout.Rows.Count; r++)
            {
                if (r > 0)
                    _grid.RowDefinitions.Add(new RowDefinition(BoundaryGap, GridUnitType.Pixel));
                
                var definition = new RowDefinition(_layout.Rows[r], GridUnitType.Star) { MinHeight = MinCellHeight };
                definition.PropertyChanged += DefinitionChanged;
                _grid.RowDefinitions.Add(definition);
            }
            
            for (var c = 0; c < _layout.Columns.Count; c++)
            {
                if (c > 0)
                    _grid.ColumnDefinitions.Add(new ColumnDefinition(BoundaryGap, GridUnitType.Pixel));
                
                var definition = new ColumnDefinition(_layout.Columns[c], GridUnitType.Star) { MinWidth = MinCellWidth };
                definition.PropertyChanged += DefinitionChanged;
                _grid.ColumnDefinitions.Add(definition);
            }
            
            RenderCells();
            for (var c = 1; c < _layout.Columns.Count; c++)
                AddSplitter(false, (c * 2) - 1);
            
            for (var r = 1; r < _layout.Rows.Count; r++)
                AddSplitter(true, (r * 2) - 1);
            
            if (!IsHeaderVisible)
                AddBoundaryButtons();
        }
        finally
        {
            _updating = false;
        }
    }

    private void RenderCells()
    {
        if (_grid is null)
            return;
        
        var ids = _layout.Cells.Select(c => c.Id).ToHashSet();
        foreach (var id in _cells.Keys.Where(id => !ids.Contains(id)).ToArray())
        {
            _grid.Children.Remove(_cells[id].Tile);
            _cells[id].Release();
            _cells.Remove(id);
        }
        
        foreach (var model in _layout.Cells)
        {
            if (!_cells.TryGetValue(model.Id, out var cell))
            {
                var tile = _entries.TryGetValue(model.Id, out var entry) ? entry.MatrixTile : new DashboardContentTile();
                _cells.Add(model.Id, cell = new DashboardMatrixCell(this, model.Id, tile));
            }
            
            if (cell.Tile is DashboardContentTile bodyTile &&
                (!_entries.TryGetValue(model.Id, out var current) || !ReferenceEquals(current.Control, bodyTile)))
            {
                bodyTile.SetMatrixContent(model.ContentId, _catalog,
                                          _entries.TryGetValue(model.Id, out var body) ? body.Control : null,
                                          id => SetContent(model.Id, id), DashboardContentMenu.Text(this, "DashboardEmpty", "Empty"));
            }
            
            cell.Update(IsHeaderVisible);
            Grid.SetRow(cell.Tile, model.Row * 2);
            Grid.SetColumn(cell.Tile, model.Column * 2);
            Grid.SetRowSpan(cell.Tile, 1);
            Grid.SetColumnSpan(cell.Tile, 1);
            if (!_grid.Children.Contains(cell.Tile))
                _grid.Children.Add(cell.Tile);
        }
    }

    /// <summary>Gets the live tile for a cell once the matrix template has been applied.</summary>
    public Tile? GetTile(Guid cellId)
    {
        _layout.GetCell(cellId);
        
        return _cells.TryGetValue(cellId, out var cell) ? cell.Tile : null;
    }

    private void AddSplitter(bool row, int index)
    {
        if (_grid is null)
            return;
        
        var gap = BoundaryGap;
        var hit = Math.Max(gap, SplitterHitThickness);
        var overlap = (hit - gap) / 2;
        var splitter = new DashboardMatrixSplitter
        {
            ResizeDirection = row ? GridResizeDirection.Rows : GridResizeDirection.Columns, ResizeBehavior = GridResizeBehavior.PreviousAndNext, Background = Brushes.Transparent,
            Focusable = true, IsEnabled = CanResize, IsHitTestVisible = CanResize, ZIndex = 10, Margin = row ? new Thickness(0, -overlap) : new Thickness(-overlap, 0),
            HorizontalAlignment = row ? HorizontalAlignment.Stretch : HorizontalAlignment.Center, VerticalAlignment = row ? VerticalAlignment.Center : VerticalAlignment.Stretch,
            Template = new FuncControlTemplate<GridSplitter>((_, _) =>
            {
                var divider = new Border
                {
                    Width = row ? Double.NaN : SplitterThickness, Height = row ? SplitterThickness : Double.NaN,
                    HorizontalAlignment = row ? HorizontalAlignment.Stretch : HorizontalAlignment.Center,
                    VerticalAlignment = row ? VerticalAlignment.Center : VerticalAlignment.Stretch, IsHitTestVisible = false
                };
                divider.Bind(Border.BackgroundProperty, this.GetResourceObservable("TileBorderBrush"));
                
                return new Border { Name = "PART_HitTarget", Background = Brushes.Transparent, Child = divider };
            })
        };
        splitter.Classes.Add("matrix-splitter");
        if (row)
        {
            splitter.Height = hit;
            Grid.SetRow(splitter, index);
            Grid.SetColumnSpan(splitter, _grid.ColumnDefinitions.Count);
        }
        else
        {
            splitter.Width = hit;
            Grid.SetColumn(splitter, index);
            Grid.SetRowSpan(splitter, _grid.RowDefinitions.Count);
        }
        
        splitter.GotFocus += (_, e) =>
        {
            if (e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional)
                SetBoundaryHover(BoundaryButtons().FirstOrDefault(b => b.Horizontal == row && b.Boundary == (index + 1) / 2));
        };
        splitter.LostFocus += (_, _) => SetBoundaryHover(null);
        AutomationProperties.SetName(splitter, $"Resize {(row ? "rows" : "columns")} {(index + 1) / 2} and {(index + 3) / 2}");
        _grid.Children.Add(splitter);
    }

    private void ClearBoundaryButtons()
    {
        if (_grid is null)
            return;
        
        _pressedBoundary = null;
        foreach (var button in _grid.Children.OfType<DashboardMatrixBoundaryButton>().ToArray())
        {
            button.Flyout?.Hide();
            _grid.Children.Remove(button);
        }
    }

    private void AddBoundaryButtons()
    {
        for (var boundary = 0; boundary <= _layout.Columns.Count; boundary++)
        {
            for (var row = 0; row < _layout.Rows.Count; row++)
                AddBoundaryButton(false, boundary, row);
        }
        
        for (var boundary = 0; boundary <= _layout.Rows.Count; boundary++)
        {
            for (var column = 0; column < _layout.Columns.Count; column++)
                AddBoundaryButton(true, boundary, column);
        }
    }

    private void AddBoundaryButton(bool horizontal, int boundary, int segment)
    {
        if (_grid is null)
            return;
        
        var count = horizontal ? _layout.Rows.Count : _layout.Columns.Count;
        var size = BoundaryActionSize;
        var button = new DashboardMatrixBoundaryButton(() => CreateBoundaryMenu(horizontal, boundary, segment))
        {
            Horizontal = horizontal, Boundary = boundary, Segment = segment, Name = $"PART_Boundary_{(horizontal ? "Row" : "Column")}_{boundary}_{segment}",
            Content = horizontal ? "⋯" : "⋮", FontSize = 18, HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center,
            Width = horizontal ? Math.Max(28, size) : size, Height = horizontal ? size : Math.Max(28, size), Padding = new Thickness(0), BorderThickness = new Thickness(0),
            HorizontalAlignment = horizontal ? HorizontalAlignment.Center :
                boundary == 0 ? HorizontalAlignment.Left :
                boundary == count ? HorizontalAlignment.Right : HorizontalAlignment.Center,
            VerticalAlignment = !horizontal ? VerticalAlignment.Center :
                boundary == 0 ? VerticalAlignment.Top :
                boundary == count ? VerticalAlignment.Bottom : VerticalAlignment.Center,
            ZIndex = 20
        };
        button.Classes.Add("matrix-boundary-action");

        // Center internal overlays on the narrow gap. Keep outer overlays inside the matrix.
        if (boundary == 0)
        {
            var offset = Math.Min(size / 2, horizontal ? Padding.Top : Padding.Left);
            button.Margin = horizontal ? new Thickness(0, -offset, 0, 0) : new Thickness(-offset, 0, 0, 0);
        }
        else if (boundary == count)
        {
            var offset = Math.Min(size / 2, horizontal ? Padding.Bottom : Padding.Right);
            button.Margin = horizontal ? new Thickness(0, 0, 0, -offset) : new Thickness(0, 0, -offset, 0);
        }
        else
        {
            var overlap = Math.Max(0, (size - BoundaryGap) / 2);
            button.Margin = horizontal ? new Thickness(0, -overlap) : new Thickness(-overlap, 0);
        }
        
        var axis = boundary == 0 ? 0 :
            boundary == count ? (count - 1) * 2 : (boundary * 2) - 1;
        Grid.SetRow(button, horizontal ? axis : segment * 2);
        Grid.SetColumn(button, horizontal ? segment * 2 : axis);
        
        var label = horizontal ? $"Row boundary {boundary}, column {segment + 1} actions" : $"Column boundary {boundary}, row {segment + 1} actions";
        AutomationProperties.SetName(button, label);
        ToolTip.SetTip(button, label);
        _grid.Children.Add(button);
    }

    private IEnumerable<DashboardMatrixBoundaryButton> BoundaryButtons()
        => _grid?.Children.OfType<DashboardMatrixBoundaryButton>() ?? Enumerable.Empty<DashboardMatrixBoundaryButton>();

    private void SetBoundaryHover(DashboardMatrixBoundaryButton? selected)
    {
        foreach (var button in BoundaryButtons())
            button.SetBoundaryHover(ReferenceEquals(button, selected));
    }

    private DashboardMatrixBoundaryButton? FindBoundary(Point point)
    {
        if (_grid is null)
            return null;
        
        var buttons = BoundaryButtons().ToArray();

        // Once revealed, crossing from the boundary into the overlay must not dismiss it.
        var visible = buttons.FirstOrDefault(b => b.IsHitTestVisible && b.Bounds.Contains(point));
        if (visible is not null)
            return visible;
        
        var rowOffsets = new double[_grid.RowDefinitions.Count + 1];
        for (var i = 0; i < _grid.RowDefinitions.Count; i++)
            rowOffsets[i + 1] = rowOffsets[i] + _grid.RowDefinitions[i].ActualHeight;
        
        var columnOffsets = new double[_grid.ColumnDefinitions.Count + 1];
        for (var i = 0; i < _grid.ColumnDefinitions.Count; i++)
            columnOffsets[i + 1] = columnOffsets[i] + _grid.ColumnDefinitions[i].ActualWidth;
        
        var halfHit = Math.Max(3, SplitterHitThickness / 2);
        foreach (var button in buttons.Reverse())
        {
            var horizontal = button.Horizontal;
            var count = horizontal ? _layout.Rows.Count : _layout.Columns.Count;

            double RowOffset(int index) => rowOffsets[index];

            double ColumnOffset(int index) => columnOffsets[index];

            var axis = button.Boundary == 0 ? 0 :
                button.Boundary == count ? horizontal ? _grid.Bounds.Height : _grid.Bounds.Width :
                horizontal ? RowOffset((button.Boundary * 2) - 1) + (BoundaryGap / 2) : ColumnOffset((button.Boundary * 2) - 1) + (BoundaryGap / 2);
            var start = horizontal ? ColumnOffset(button.Segment * 2) : RowOffset(button.Segment * 2);
            var length = horizontal ? _grid.ColumnDefinitions[button.Segment * 2].ActualWidth : _grid.RowDefinitions[button.Segment * 2].ActualHeight;
            var across = horizontal ? point.Y : point.X;
            var along = horizontal ? point.X : point.Y;
            if (Math.Abs(across - axis) <= halfHit && along >= start && along <= start + length)
                return button;
        }
        
        return null;
    }

    private static double Distance(Point a, Point b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    private void BoundaryPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_grid is null || IsHeaderVisible)
            return;
        
        var point = e.GetPosition(_grid);
        if (_pressedBoundary is not null)
        {
            if (Distance(point, _boundaryPressPoint) > 5)
                _boundaryDragged = true;
            
            SetBoundaryHover(null);
            
            return;
        }
        
        if (e.Pointer.Type != PointerType.Touch)
            SetBoundaryHover(FindBoundary(point));
    }

    private void BoundaryPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_grid is null || IsHeaderVisible)
            return;
        
        _pressedBoundary = null;
        
        var target = e.Source as DashboardMatrixBoundaryButton ??
                     (e.Source as Visual)?.GetVisualAncestors().OfType<DashboardMatrixBoundaryButton>().FirstOrDefault();
        foreach (var button in BoundaryButtons())
        {
            if (!ReferenceEquals(button, target))
                button.RevealTap(false);
        }
        if (target is not null)
            return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        
        _boundaryPressPoint = e.GetPosition(_grid);
        _pressedBoundary = FindBoundary(_boundaryPressPoint);
        _boundaryDragged = false;
    }

    private void BoundaryPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_grid is null || _pressedBoundary is null)
            return;
        
        var button = _pressedBoundary;
        _pressedBoundary = null;
        if (!_boundaryDragged && Distance(e.GetPosition(_grid), _boundaryPressPoint) <= 5)
            button.RevealTap(true);
    }

    private MenuFlyout CreateBoundaryMenu(bool horizontal, int boundary, int segment)
    {
        var menu = new MenuFlyout();
        var count = horizontal ? _layout.Rows.Count : _layout.Columns.Count;
        menu.Items.Add(DashboardContentMenu.Action(this,
                                                   DashboardContentMenu.Text(this, horizontal ? "DashboardInsertRowHere" : "DashboardInsertColumnHere",
                                                                             horizontal ? "Insert row here" : "Insert column here"),
                                                   () => horizontal ? boundary == count ? InsertRowAfter(count - 1) : InsertRowBefore(boundary) :
                                                       boundary == count ? InsertColumnAfter(count - 1) : InsertColumnBefore(boundary),
                                                   CanModifyStructure));
        menu.Items.Add(new Separator());

        void AddSide(int index, string key, string fallback)
        {
            var model = _layout.Cells.Single(c => c.Row == (horizontal ? index : segment) &&
                                                  c.Column == (horizontal ? segment : index));
            menu.Items.Add(_cells[model.Id].CreateContentMenu(DashboardContentMenu.Text(this, key, fallback)));
        }

        if (boundary > 0)
        {
            AddSide(boundary - 1,
                    horizontal ? "DashboardCellAbove" : "DashboardCellLeft", horizontal ? "Cell above" : "Cell left");
        }
        
        if (boundary < count)
        {
            AddSide(boundary,
                    horizontal ? "DashboardCellBelow" : "DashboardCellRight", horizontal ? "Cell below" : "Cell right");
        }
        
        return menu;
    }

    private void ClearGrid()
    {
        if (_grid is null)
            return;
        
        ClearBoundaryButtons();
        _grid.Children.Clear();
        ClearDefinitions();
    }

    private void ClearDefinitions()
    {
        if (_grid is null)
            return;
        
        foreach (var definition in _grid.RowDefinitions)
            definition.PropertyChanged -= DefinitionChanged;
        
        foreach (var definition in _grid.ColumnDefinitions)
            definition.PropertyChanged -= DefinitionChanged;
        
        _grid.RowDefinitions.Clear();
        _grid.ColumnDefinitions.Clear();
    }

    private void DefinitionChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_updating || _syncPending ||
            (e.Property != RowDefinition.HeightProperty && e.Property != ColumnDefinition.WidthProperty))
            return;
        
        _syncPending = true;
        Dispatcher.UIThread.Post(() =>
        {
            if (_syncPending && !_disposed)
                SynchronizeWeights();
        }, DispatcherPriority.Background);
    }

    private void SynchronizeWeights()
    {
        VerifyAccess();
        if (!_syncPending || _grid is null || _updating || _transaction || _disposed)
            return;
        
        _syncPending = false;
        
        var rows = _grid.RowDefinitions.Where((_, i) => i % 2 == 0).Select(d => d.Height.Value).ToArray();
        var columns = _grid.ColumnDefinitions.Where((_, i) => i % 2 == 0).Select(d => d.Width.Value).ToArray();
        if (rows.SequenceEqual(_layout.Rows) && columns.SequenceEqual(_layout.Columns))
            return;
        
        var before = _layout;
        try
        {
            Commit(before.WithWeights(rows, columns), DashboardMatrixChangeKind.Proportions,
                   Array.Empty<DashboardMatrixCellModel>());
        }
        finally
        {
            // A veto or pre-commit exception must also roll back the native definitions.
            // Post-commit notification exceptions leave the committed snapshot applied.
            if (ReferenceEquals(_layout, before))
                RestoreNativeWeights(before);
        }
    }

    private void RestoreNativeWeights(DashboardMatrixLayout layout)
    {
        if (_grid is not null)
        {
            _updating = true;
            
            try
            {
                for (var r = 0; r < layout.Rows.Count; r++)
                    _grid.RowDefinitions[r * 2].Height = new GridLength(layout.Rows[r], GridUnitType.Star);
                
                for (var c = 0; c < layout.Columns.Count; c++)
                    _grid.ColumnDefinitions[c * 2].Width = new GridLength(layout.Columns[c], GridUnitType.Star);
            }
            finally
            {
                _updating = false;
            }
        }
    }

    private void SubscribeCatalog()
    {
        if (ContentDefinitions is INotifyCollectionChanged source)
        {
            _catalogSubscription = source;
            source.CollectionChanged += CatalogChanged;
        }
    }

    private void UnsubscribeCatalog()
    {
        if (_catalogSubscription is not null)
            _catalogSubscription.CollectionChanged -= CatalogChanged;
        
        _catalogSubscription = null;
    }

    private void CatalogChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshContent();

    private void EnsureAvailable()
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_transaction)
            throw new InvalidOperationException("Matrix changes cannot be nested inside change events.");
    }
}
