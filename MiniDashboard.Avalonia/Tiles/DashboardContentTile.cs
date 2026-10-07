using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.VisualTree;
using MiniDashboard.Avalonia.Content;
using MiniDashboard.Avalonia.Matrix;

namespace MiniDashboard.Avalonia.Tiles;

/// <summary>A tile-grid draggable/resizable tile whose body is selected from a content catalog.</summary>
public class DashboardContentTile : Tile, IDisposable
{
    /// <summary>Defines the available content definitions.</summary>
    public static readonly StyledProperty<IEnumerable<DashboardContentDefinition>?> ContentDefinitionsProperty =
        DashboardMatrix.ContentDefinitionsProperty.AddOwner<DashboardContentTile>();

    /// <summary>Defines the persistent selected content ID; two-way by default.</summary>
    public static readonly StyledProperty<string?> ContentIdProperty =
        AvaloniaProperty.Register<DashboardContentTile, string?>(nameof(ContentId), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines ownership of removed generated content.</summary>
    public static readonly StyledProperty<bool> DisposeRemovedContentProperty =
        DashboardMatrix.DisposeRemovedContentProperty.AddOwner<DashboardContentTile>();

    private readonly MenuFlyout _menu = new MenuFlyout();
    private readonly MenuFlyout _addMenu = new MenuFlyout();
    private DashboardContentCatalog _catalog = new DashboardContentCatalog();
    private DashboardContentInstance? _instance;
    private INotifyCollectionChanged? _subscription;
    private string? _committedId;
    private bool _changing;
    private bool _disposed;
    private Func<string?, bool>? _matrixSelect;

    internal void SetMatrixContent(string? id, DashboardContentCatalog catalog, Control? body, Func<string?, bool> select, string emptyTitle)
    {
        _changing = true;

        try
        {
            _matrixSelect = select;
            _catalog = catalog;
            _committedId = id;
            SetCurrentValue(ContentIdProperty, id);
            Content = body;
            SetCurrentValue(TileHeaderProperty, id is null ? emptyTitle : catalog.Find(id)?.Title ?? id);
            PseudoClasses.Set(":empty", id is null);
        }
        finally
        {
            _changing = false;
        }
    }

    internal bool IsMatrixManaged => _matrixSelect is not null;

    internal void ReleaseMatrixContent()
    {
        if (_matrixSelect is null)
            return;

        _matrixSelect = null;
        Content = null;
        _committedId = null;
        _changing = true;

        try
        {
            SetCurrentValue(ContentIdProperty, null);
        }
        finally
        {
            _changing = false;
        }

        PseudoClasses.Set(":empty", true);
    }

    /// <summary>Creates a catalog-backed tile with an independent header menu.</summary>
    public DashboardContentTile()
    {
        PopulateMenu();
        HeaderActions = _menu;
        SetAddContentActions(_addMenu);
        TileHeader = "Empty";
        PseudoClasses.Set(":empty", true);
    }

    private void PopulateMenu()
    {
        _menu.Items.Clear();
        _menu.Items.Add(DashboardContentMenu.CreatePicker(this, _catalog.Where(d => d.TileFactory is null && d.ContextTileFactory is null),
                                                          ContentId, id => SetContent(id)));
        _menu.Items.Add(new Separator());
        _menu.Items.Add(DashboardContentMenu.Action(this, DashboardContentMenu.Text(this, "DashboardClearContent", "Clear content"),
                                                    () => SetContent(null), ContentId is not null));
        DashboardContentMenu.FillPickerFlyout(_addMenu, this, _catalog.Where(d => d.TileFactory is null && d.ContextTileFactory is null), id => SetContent(id));
        DashboardContentMenu.ConfigureTitleAction(this, _catalog.Where(d => d.TileFactory is null && d.ContextTileFactory is null),
                                                  ContentId, id => SetContent(id), _addMenu);
    }

    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(Tile);

    /// <summary>Gets or sets the catalog. Observable edits reconcile while attached.</summary>
    public IEnumerable<DashboardContentDefinition>? ContentDefinitions
    {
        get => GetValue(ContentDefinitionsProperty);
        set => SetValue(ContentDefinitionsProperty, value);
    }

    /// <summary>Gets or sets the stable selected content ID.</summary>
    public string? ContentId
    {
        get => GetValue(ContentIdProperty);
        set => SetValue(ContentIdProperty, value);
    }

    /// <summary>Gets or sets whether discarded factory-created content is disposed.</summary>
    public bool DisposeRemovedContent
    {
        get => GetValue(DisposeRemovedContentProperty);
        set => SetValue(DisposeRemovedContentProperty, value);
    }

    /// <summary>Raised before changing the selected ID. Set Cancel to retain the current assignment.</summary>
    public event EventHandler<DashboardContentChangingEventArgs>? ContentChanging;

    /// <summary>Raised after committing a selected ID.</summary>
    public event EventHandler<DashboardContentChangingEventArgs>? ContentChanged;

    /// <summary>Raised on creation failure, before the exception propagates to the caller.</summary>
    public event EventHandler<DashboardContentFailedEventArgs>? ContentFailed;

    /// <summary>Changes selection without replacing a ContentId binding; returns false when vetoed or unchanged.</summary>
    public bool SetContent(string? id)
    {
        EnsureAvailable();
        if (_matrixSelect is not null)
            return _matrixSelect(id);
        if (id == _committedId)
            return false;

        SetCurrentValue(ContentIdProperty, id);

        return _committedId == id;
    }

    /// <summary>Refreshes a plain enumerable catalog; definition objects are retained between refreshes.</summary>
    public void RefreshContent()
    {
        EnsureAvailable();
        if (_matrixSelect is not null)
            return;

        var catalog = ContentDefinitions as DashboardContentCatalog ??
                      new DashboardContentCatalog(ContentDefinitions ?? Array.Empty<DashboardContentDefinition>());
        ChangeContent(ContentId, catalog);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_disposed || _changing)
            return;

        if (change.Property == DashboardContentPicker.PickerProperty && !IsMatrixManaged)
            PopulateMenu();

        if (change.Property == ContentDefinitionsProperty)
        {
            Unsubscribe();
            if (this.IsAttachedToVisualTree())
                Subscribe();

            RefreshContent();
        }
        else if (change.Property == ContentIdProperty)
        {
            if (_matrixSelect is null)
                ChangeContent(ContentId, _catalog);
            else
            {
                var requested = ContentId;
                _changing = true;

                try
                {
                    SetCurrentValue(ContentIdProperty, _committedId);
                }
                finally
                {
                    _changing = false;
                }

                _matrixSelect(requested);
            }
        }
    }

    private void ChangeContent(string? id, DashboardContentCatalog catalog)
    {
        EnsureAvailable();

        _changing = true;

        var assignmentChanged = id != _committedId;
        var args = new DashboardContentChangingEventArgs(_committedId, id);
        try
        {
            if (id is not null && String.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Content IDs cannot be blank.", nameof(id));

            if (assignmentChanged)
            {
                ContentChanging?.Invoke(this, args);
                if (args.Cancel)
                {
                    SetCurrentValue(ContentIdProperty, _committedId);

                    return;
                }
            }

            var definition = id is null ? null : catalog.Find(id);
            DashboardContentInstance? next;
            try
            {
                if (definition?.TileFactory is not null || definition?.ContextTileFactory is not null)
                    throw new InvalidOperationException("Complete tiles belong directly in a dashboard; use a body factory in DashboardContentTile.");

                next = id is null ? null :
                    _instance is not null && _instance.ContentId == id && ReferenceEquals(_instance.Definition, definition) ? _instance :
                    DashboardContentInstance.Create(id, definition, _instance is null
                                                        ? new HashSet<Control> { this }
                                                        : new HashSet<Control> { this, _instance.Control });
            }
            catch (Exception error)
            {
                ContentFailed?.Invoke(this, new DashboardContentFailedEventArgs(error));
                throw;
            }

            var old = _instance;
            _catalog = catalog;
            _instance = next;
            _committedId = id;
            PopulateMenu();
            Content = next?.Control;
            PseudoClasses.Set(":empty", id is null);
            SetCurrentValue(TileHeaderProperty, definition?.Title ?? id ?? DashboardContentMenu.Text(this, "DashboardEmpty", "Empty"));

            try
            {
                if (old is not null && !ReferenceEquals(old, next))
                    DashboardContentInstance.Release(new[] { old }, DisposeRemovedContent);
            }
            finally
            {
                if (assignmentChanged)
                    ContentChanged?.Invoke(this, args);
            }
        }
        finally
        {
            SetCurrentValue(ContentIdProperty, _committedId);
            _changing = false;
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_disposed || _matrixSelect is not null)
            return;

        Subscribe();
        if (ContentDefinitions is INotifyCollectionChanged)
            RefreshContent();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Unsubscribe();
        base.OnDetachedFromVisualTree(e);
    }

    private void Subscribe()
    {
        Unsubscribe();
        if (ContentDefinitions is INotifyCollectionChanged source)
        {
            _subscription = source;
            source.CollectionChanged += CatalogChanged;
        }
    }

    private void Unsubscribe()
    {
        if (_subscription is not null)
            _subscription.CollectionChanged -= CatalogChanged;

        _subscription = null;
    }

    private void CatalogChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshContent();

    private void EnsureAvailable()
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_changing)
            throw new InvalidOperationException("Content changes cannot be nested inside change events.");
    }

    /// <summary>Permanently releases this tile's generated body according to DisposeRemovedContent.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        EnsureAvailable();

        _disposed = true;
        Unsubscribe();
        Content = null;

        var old = _instance;
        _instance = null;
        if (old is not null)
            DashboardContentInstance.Release(new[] { old }, DisposeRemovedContent);

        GC.SuppressFinalize(this);
    }
}
