using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.VisualTree;
using MiniDashboard.Avalonia.Tiles;

namespace MiniDashboard.Avalonia.Grid;

/// <summary>
/// A dashboard panel that materializes an items source directly into dashboard child controls.
/// </summary>
public class DashboardItemsPanel : DashboardPanel, IDisposable
{
    /// <summary>
    /// Defines the items used to generate dashboard children.
    /// </summary>
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<DashboardItemsPanel, IEnumerable?>(nameof(ItemsSource));

    /// <summary>
    /// Defines the primary data template used to generate dashboard children.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<DashboardItemsPanel, IDataTemplate?>(nameof(ItemTemplate));

    /// <summary>
    /// Defines whether generated controls that implement <see cref="IDisposable" /> are disposed when removed.
    /// </summary>
    public static readonly StyledProperty<bool> DisposeRemovedTilesProperty =
        AvaloniaProperty.Register<DashboardItemsPanel, bool>(nameof(DisposeRemovedTiles));

    /// <summary>
    /// Defines whether static children declared directly in XAML are preserved when generated children are rebuilt.
    /// </summary>
    public static readonly StyledProperty<bool> PreserveStaticChildrenProperty =
        AvaloniaProperty.Register<DashboardItemsPanel, bool>(nameof(PreserveStaticChildren), true);

    /// <summary>
    /// Defines the source property path bound to <see cref="Tile.TileHeader" /> on generated tiles.
    /// </summary>
    public static readonly StyledProperty<string?> TileHeaderPathProperty =
        AvaloniaProperty.Register<DashboardItemsPanel, string?>(nameof(TileHeaderPath), "Title");

    /// <summary>
    /// Defines the source property path bound to <see cref="Tile.GridX" /> on generated tiles.
    /// </summary>
    public static readonly StyledProperty<string?> GridXPathProperty =
        AvaloniaProperty.Register<DashboardItemsPanel, string?>(nameof(GridXPath), "X");

    /// <summary>
    /// Defines the source property path bound to <see cref="Tile.GridY" /> on generated tiles.
    /// </summary>
    public static readonly StyledProperty<string?> GridYPathProperty =
        AvaloniaProperty.Register<DashboardItemsPanel, string?>(nameof(GridYPath), "Y");

    /// <summary>
    /// Defines the source property path bound to <see cref="Tile.GridW" /> on generated tiles.
    /// </summary>
    public static readonly StyledProperty<string?> GridWPathProperty =
        AvaloniaProperty.Register<DashboardItemsPanel, string?>(nameof(GridWPath), "Width");

    /// <summary>
    /// Defines the source property path bound to <see cref="Tile.GridH" /> on generated tiles.
    /// </summary>
    public static readonly StyledProperty<string?> GridHPathProperty =
        AvaloniaProperty.Register<DashboardItemsPanel, string?>(nameof(GridHPath), "Height");

    /// <summary>
    /// Defines the binding mode used for generated tile grid bindings.
    /// </summary>
    public static readonly StyledProperty<BindingMode> GridBindingModeProperty =
        AvaloniaProperty.Register<DashboardItemsPanel, BindingMode>(nameof(GridBindingMode), BindingMode.TwoWay);

    private readonly List<GeneratedChild> _generatedChildren = [];
    private INotifyCollectionChanged? _collectionChangedSource;
    private bool _templateChanged;
    private bool _disposed;
    private bool _reconciling;

    static DashboardItemsPanel()
    {
        ItemsSourceProperty.Changed.Subscribe(static args =>
        {
            if (args.Sender is DashboardItemsPanel panel)
                panel.OnItemsSourceChanged();
        });

        ItemTemplateProperty.Changed.Subscribe(static args =>
        {
            if (args.Sender is DashboardItemsPanel panel)
            {
                panel._templateChanged = true;
                panel.RebuildGeneratedChildren();
            }
        });
    }

    /// <summary>
    /// Gets or sets the items used to generate dashboard children.
    /// </summary>
    public IEnumerable? ItemsSource
    {
        get { return GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }

    /// <summary>
    /// Gets or sets the primary template used before this control's data templates are searched.
    /// </summary>
    public IDataTemplate? ItemTemplate
    {
        get { return GetValue(ItemTemplateProperty); }
        set { SetValue(ItemTemplateProperty, value); }
    }

    /// <summary>
    /// Gets or sets whether removed generated controls are disposed when they implement <see cref="IDisposable" />.
    /// </summary>
    public bool DisposeRemovedTiles
    {
        get { return GetValue(DisposeRemovedTilesProperty); }
        set { SetValue(DisposeRemovedTilesProperty, value); }
    }

    /// <summary>
    /// Gets or sets whether static children declared directly in XAML are preserved when generated children are rebuilt.
    /// </summary>
    public bool PreserveStaticChildren
    {
        get { return GetValue(PreserveStaticChildrenProperty); }
        set { SetValue(PreserveStaticChildrenProperty, value); }
    }

    /// <summary>
    /// Gets or sets the source property path bound to <see cref="Tile.TileHeader" /> on generated tiles.
    /// </summary>
    public string? TileHeaderPath
    {
        get { return GetValue(TileHeaderPathProperty); }
        set { SetValue(TileHeaderPathProperty, value); }
    }

    /// <summary>
    /// Gets or sets the source property path bound to <see cref="Tile.GridX" /> on generated tiles.
    /// </summary>
    public string? GridXPath
    {
        get { return GetValue(GridXPathProperty); }
        set { SetValue(GridXPathProperty, value); }
    }

    /// <summary>
    /// Gets or sets the source property path bound to <see cref="Tile.GridY" /> on generated tiles.
    /// </summary>
    public string? GridYPath
    {
        get { return GetValue(GridYPathProperty); }
        set { SetValue(GridYPathProperty, value); }
    }

    /// <summary>
    /// Gets or sets the source property path bound to <see cref="Tile.GridW" /> on generated tiles.
    /// </summary>
    public string? GridWPath
    {
        get { return GetValue(GridWPathProperty); }
        set { SetValue(GridWPathProperty, value); }
    }

    /// <summary>
    /// Gets or sets the source property path bound to <see cref="Tile.GridH" /> on generated tiles.
    /// </summary>
    public string? GridHPath
    {
        get { return GetValue(GridHPathProperty); }
        set { SetValue(GridHPathProperty, value); }
    }

    /// <summary>
    /// Gets or sets the binding mode used for generated tile grid bindings.
    /// </summary>
    public BindingMode GridBindingMode
    {
        get { return GetValue(GridBindingModeProperty); }
        set { SetValue(GridBindingModeProperty, value); }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!_disposed)
        {
            SubscribeToCollection(ItemsSource);
            RebuildGeneratedChildren();
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UnsubscribeFromCollection();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnItemsSourceChanged()
    {
        if (_disposed)
            return;

        UnsubscribeFromCollection();
        if (this.IsAttachedToVisualTree())
            SubscribeToCollection(ItemsSource);

        RebuildGeneratedChildren();
    }

    private void SubscribeToCollection(IEnumerable? source)
    {
        if (source is INotifyCollectionChanged collectionChanged && !ReferenceEquals(_collectionChangedSource, collectionChanged))
        {
            _collectionChangedSource = collectionChanged;
            collectionChanged.CollectionChanged += OnItemsSourceCollectionChanged;
        }
    }

    private void UnsubscribeFromCollection()
    {
        if (_collectionChangedSource is not null)
        {
            _collectionChangedSource.CollectionChanged -= OnItemsSourceCollectionChanged;
            _collectionChangedSource = null;
        }
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildGeneratedChildren();

    private void RebuildGeneratedChildren()
    {
        if (_disposed || !this.IsAttachedToVisualTree())
            return;
        if (_reconciling)
            throw new InvalidOperationException("ItemsSource cannot change during materialization.");

        _reconciling = true;

        try
        {
            var available = new List<GeneratedChild>(_generatedChildren);
            var next = new List<GeneratedChild>();
            var created = new List<GeneratedChild>();
            try
            {
                foreach (var item in ItemsSource ?? Array.Empty<object>())
                {
                    // Reference identity preserves distinct equal-valued model objects and repeated occurrences.
                    var index = _templateChanged && item is not Control ? -1 : available.FindIndex(c => ReferenceEquals(c.Item, item));
                    if (index >= 0)
                    {
                        next.Add(available[index]);
                        available.RemoveAt(index);
                        continue;
                    }

                    var control = MaterializeItem(item);
                    if (next.Any(c => ReferenceEquals(c.Control, control)) ||
                        control.Parent is not null || control.GetVisualParent() is not null)
                        throw new InvalidOperationException("An item template must produce a fresh, unparented control.");

                    var child = new GeneratedChild(item, control, item is not Control);
                    created.Add(child);
                    if (child.Owned)
                    {
                        control.DataContext = item;
                        ApplyTileBindings(control);
                    }

                    next.Add(child);
                }
            }
            catch
            {
                DisposeChildren(created);
                throw;
            }

            foreach (var removed in available)
                Children.Remove(removed.Control);

            _generatedChildren.Clear();
            _generatedChildren.AddRange(next);
            if (!PreserveStaticChildren)
            {
                foreach (var child in Children.Where(c => !IsDashboardInternalChild(c) &&
                                                          !next.Any(g => ReferenceEquals(g.Control, c))).ToArray())
                    Children.Remove(child);
            }

            // Preserve attachment and focus on collection moves. Child order is updated in place.
            foreach (var child in next)
            {
                if (!Children.Contains(child.Control))
                    Children.Add(child.Control);
            }

            var slots = Children.Select((c, i) => (Control: c, Index: i))
                                .Where(p => next.Any(g => ReferenceEquals(g.Control, p.Control))).Select(p => p.Index).ToArray();
            for (var i = 0; i < next.Count; i++)
            {
                var oldIndex = Children.IndexOf(next[i].Control);
                if (oldIndex != slots[i])
                    Children.Move(oldIndex, slots[i]);
            }

            _templateChanged = false;
            InvalidateMeasure();
            DisposeChildren(available);
        }
        finally
        {
            _reconciling = false;
        }
    }

    private void DisposeChildren(IEnumerable<GeneratedChild> children)
    {
        if (!DisposeRemovedTiles)
            return;

        List<Exception>? failures = null;
        foreach (var child in children)
        {
            if (!child.Owned || child.Control is not IDisposable disposable)
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
            throw new AggregateException(failures);
    }

    /// <summary>Permanently releases generated children; visual detachment alone preserves them.</summary>
    public void Dispose()
    {
        CancelTilePick();
        VerifyAccess();
        if (_disposed)
            return;
        if (_reconciling)
            throw new InvalidOperationException("Cannot dispose during materialization.");

        _disposed = true;
        UnsubscribeFromCollection();

        var children = _generatedChildren.ToArray();
        _generatedChildren.Clear();
        foreach (var child in children)
            Children.Remove(child.Control);

        DisposeChildren(children);
        GC.SuppressFinalize(this);
    }

    private Control MaterializeItem(object? item)
    {
        if (item is Control control)
            return control;

        var template = this.FindDataTemplate(item, ItemTemplate);
        if (template is null)
            throw new InvalidOperationException($"DashboardItemsPanel could not find an item template for item type '{item?.GetType().FullName ?? "<null>"}'.");

        return template.Build(item) ??
               throw new InvalidOperationException("DashboardItemsPanel templates must create an Avalonia Control.");
    }

    private void ApplyTileBindings(Control control)
    {
        if (control is not Tile tile)
            return;

        BindIfPathSet(tile, Tile.TileHeaderProperty, TileHeaderPath, BindingMode.OneWay);
        BindIfPathSet(tile, Tile.GridXProperty, GridXPath, GridBindingMode);
        BindIfPathSet(tile, Tile.GridYProperty, GridYPath, GridBindingMode);
        BindIfPathSet(tile, Tile.GridWProperty, GridWPath, GridBindingMode);
        BindIfPathSet(tile, Tile.GridHProperty, GridHPath, GridBindingMode);
    }

    private static void BindIfPathSet<T>(Tile tile, AvaloniaProperty<T> property, string? path, BindingMode mode)
    {
        if (!String.IsNullOrWhiteSpace(path))
            tile.Bind(property, new Binding(path) { Mode = mode });
    }

    private void InvalidateDashboardLayout()
    {
        InvalidateMeasure();
        InvalidateArrange();
    }

    private sealed record GeneratedChild(object? Item, Control Control, bool Owned);
}
