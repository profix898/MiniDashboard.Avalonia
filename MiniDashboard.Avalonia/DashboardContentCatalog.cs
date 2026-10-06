using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace MiniDashboard.Avalonia;

/// <summary>An observable, indexed catalog. Definition identities remain stable between edits.</summary>
public sealed class DashboardContentCatalog : ObservableCollection<DashboardContentDefinition>
{
    private readonly Dictionary<string, DashboardContentDefinition> _index = new Dictionary<string, DashboardContentDefinition>(StringComparer.Ordinal);

    /// <summary>Creates an empty catalog.</summary>
    public DashboardContentCatalog()
    {
    }

    /// <summary>Materializes and validates a sequence exactly once.</summary>
    public DashboardContentCatalog(IEnumerable<DashboardContentDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        
        foreach (var definition in definitions)
            Add(definition);
    }

    /// <summary>Looks up a stable, case-sensitive content ID.</summary>
    public DashboardContentDefinition? Find(string id) => _index.GetValueOrDefault(id);

    /// <summary>Creates a fresh tile for either dashboard. Body definitions become content tiles.</summary>
    public Tile CreateTile(string id)
    {
        var definition = Find(id) ?? throw new ArgumentException($"Unknown content ID '{id}'.", nameof(id));
        if (definition.TileFactory is not null)
            return (Tile) DashboardContentInstance.Create(id, definition, new HashSet<Control>()).Control;
        
        return new DashboardContentTile { ContentDefinitions = this, ContentId = id };
    }

    /// <summary>Creates entries for configured models. Every factory invocation must create a fresh view.</summary>
    public static DashboardContentCatalog FromItems<T>(IEnumerable<T> items, Func<T, string> idSelector,
                                                       Func<T, string> titleSelector, Func<T, Control> factory)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(idSelector);
        ArgumentNullException.ThrowIfNull(titleSelector);
        ArgumentNullException.ThrowIfNull(factory);
        
        return new DashboardContentCatalog(items.Select(item => new DashboardContentDefinition
        {
            Id = idSelector(item), Title = titleSelector(item), Factory = () => factory(item)
        }));
    }

    /// <summary>Adapts models and an Avalonia data template, assigning the model as DataContext.</summary>
    public static DashboardContentCatalog FromTemplate<T>(IEnumerable<T> items, Func<T, string> idSelector,
                                                          Func<T, string> titleSelector, IDataTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        
        return FromItems(items, idSelector, titleSelector, item =>
        {
            if (!template.Match(item))
                throw new InvalidOperationException("The data template does not match this catalog item.");
            
            var control = template.Build(item) ?? throw new InvalidOperationException("The data template returned null.");
            if (control.Parent is not null)
                throw new InvalidOperationException("The data template returned a parented control.");
            
            control.DataContext = item;
            
            return control;
        });
    }

    /// <summary>Adapts configured legacy tiles through a fresh-tile factory, preserving their own templates and settings.</summary>
    public static DashboardContentCatalog FromTiles<T>(IEnumerable<T> items, Func<T, string> idSelector,
                                                       Func<T, string> titleSelector, Func<T, Tile> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(idSelector);
        ArgumentNullException.ThrowIfNull(titleSelector);
        
        return new DashboardContentCatalog(items.Select(item => new DashboardContentDefinition
        {
            Id = idSelector(item), Title = titleSelector(item), TileFactory = () => factory(item)
        }));
    }

    /// <inheritdoc />
    protected override void InsertItem(int index, DashboardContentDefinition item)
    {
        CheckReentrancy();
        Validate(item, null);
        if (index < 0 || index > Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        
        _index.Add(item.Id, item);
        base.InsertItem(index, item);
    }

    /// <inheritdoc />
    protected override void SetItem(int index, DashboardContentDefinition item)
    {
        CheckReentrancy();
        
        var old = this[index];
        Validate(item, old.Id);
        _index.Remove(old.Id);
        _index[item.Id] = item;
        base.SetItem(index, item);
    }

    /// <inheritdoc />
    protected override void RemoveItem(int index)
    {
        CheckReentrancy();
        _index.Remove(this[index].Id);
        base.RemoveItem(index);
    }

    /// <inheritdoc />
    protected override void ClearItems()
    {
        CheckReentrancy();
        _index.Clear();
        base.ClearItems();
    }

    private void Validate(DashboardContentDefinition item, string? replacing)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (String.IsNullOrWhiteSpace(item.Id) || String.IsNullOrWhiteSpace(item.Title) || item.Factory is null == item.TileFactory is null)
            throw new ArgumentException("Catalog entries require nonempty IDs, titles, and exactly one body or tile factory.", nameof(item));
        if (item.Id != replacing && _index.ContainsKey(item.Id))
            throw new ArgumentException($"Duplicate content ID '{item.Id}'.", nameof(item));
    }
}
