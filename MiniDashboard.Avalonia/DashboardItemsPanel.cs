using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.VisualTree;

namespace MiniDashboard.Avalonia;

/// <summary>
/// A dashboard panel that materializes an items source directly into dashboard child controls.
/// </summary>
public class DashboardItemsPanel : DashboardPanel
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
                panel.RebuildGeneratedChildren();
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
        SubscribeToCollection(ItemsSource);
        RebuildGeneratedChildren();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UnsubscribeFromCollection();
        ClearGeneratedChildren();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnItemsSourceChanged()
    {
        UnsubscribeFromCollection();
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

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                AddGeneratedItems(e.NewItems, e.NewStartingIndex);
                break;

            case NotifyCollectionChangedAction.Remove:
                RemoveGeneratedItems(e.OldItems);
                break;

            case NotifyCollectionChangedAction.Replace:
                RemoveGeneratedItems(e.OldItems);
                AddGeneratedItems(e.NewItems, e.NewStartingIndex);
                break;

            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Reset:
                RebuildGeneratedChildren();
                break;
        }

        InvalidateDashboardLayout();
    }

    private void RebuildGeneratedChildren()
    {
        ClearGeneratedChildren();

        if (!PreserveStaticChildren)
            RemoveStaticChildren();

        AddGeneratedItems(ItemsSource, -1);
        InvalidateDashboardLayout();
    }

    private void AddGeneratedItems(IEnumerable? items, int itemIndex)
    {
        if (items is null)
            return;

        var insertIndex = itemIndex >= 0 && itemIndex <= _generatedChildren.Count ? itemIndex : _generatedChildren.Count;
        foreach (var item in items)
        {
            var control = MaterializeItem(item);
            control.DataContext = item;
            ApplyTileBindings(control);

            _generatedChildren.Insert(insertIndex, new GeneratedChild(item, control));
            Children.Add(control);
            insertIndex++;
        }
    }

    private void RemoveGeneratedItems(IList? items)
    {
        if (items is null)
            return;

        foreach (var item in items)
        {
            var index = _generatedChildren.FindIndex(child => ReferenceEquals(child.Item, item) || Equals(child.Item, item));
            if (index >= 0)
                RemoveGeneratedChildAt(index);
        }
    }

    private void ClearGeneratedChildren()
    {
        for (var i = _generatedChildren.Count - 1; i >= 0; i--)
            RemoveGeneratedChildAt(i);
    }

    private void RemoveGeneratedChildAt(int index)
    {
        var child = _generatedChildren[index];
        _generatedChildren.RemoveAt(index);
        Children.Remove(child.Control);

        if (DisposeRemovedTiles && child.Control is IDisposable disposable)
            disposable.Dispose();
    }

    private void RemoveStaticChildren()
    {
        for (var i = Children.Count - 1; i >= 0; i--)
        {
            var child = Children[i];
            if (child is null || IsDashboardInternalChild(child) || _generatedChildren.Exists(generated => ReferenceEquals(generated.Control, child)))
                continue;

            Children.RemoveAt(i);
        }
    }

    private Control MaterializeItem(object? item)
    {
        if (item is Control control)
            return control;

        var template = this.FindDataTemplate(item, ItemTemplate);
        if (template is null)
            throw new InvalidOperationException($"DashboardItemsPanel could not find an item template for item type '{item?.GetType().FullName ?? "<null>"}'.");

        var built = template.Build(item);
        if (built is Control builtControl)
            return builtControl;

        throw new InvalidOperationException($"DashboardItemsPanel item template for item type '{item?.GetType().FullName ?? "<null>"}' built '{built?.GetType().FullName ?? "<null>"}' instead of an Avalonia Control.");
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

    private sealed record GeneratedChild(object? Item, Control Control);
}
