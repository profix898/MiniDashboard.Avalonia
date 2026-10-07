using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using MiniDashboard.Avalonia;
using MiniDashboard.Avalonia.ScottPlot.Cartesian;

namespace DemoApp;

/// <summary>
/// Demonstrates a single <see cref="DashboardContentCatalog" /> feeding both dashboard hosts
/// with independent content instances and live catalog edits.
/// </summary>
public partial class CatalogDemo : UserControl, IDisposable
{
    private readonly DashboardContentCatalog _catalog;
    private readonly List<DashboardContentTile> _classicTiles = new();
    private readonly List<(string Id, Tile Tile)> _completeTiles = new();
    private int _next = 1;

    public CatalogDemo()
    {
        InitializeComponent();

        // Complete-tile recipes: any Tile subclass, even from other packages, becomes catalog content.
        var recipes = new[]
        {
            new TileRecipe("throughput", "Throughput",
                           () => new ScatterPlotTile { TileHeader = "Throughput", Ys = new double[] { 12, 18, 15, 22, 30, 28, 35 } })
        };
        _catalog = DashboardContentCatalog.FromTiles(recipes, r => r.Id, r => r.Title, r => r.Factory());

        // Body definitions: selectable in both hosts through the shared "+ Add content" menus.
        _catalog.Add(new DashboardContentDefinition
        {
            Id = "notes", Title = "Notes",
            Factory = () => new TextBox { AcceptsReturn = true, Text = "Independent instance", TextWrapping = TextWrapping.Wrap }
        });
        _catalog.Add(new DashboardContentDefinition
        {
            Id = "status", Title = "System status",
            Factory = () => new StackPanel
            {
                Spacing = 8,
                Children = { Gauge("CPU", 42), Gauge("Memory", 67), Gauge("Disk", 23) }
            }
        });
        _catalog.Add(new DashboardContentDefinition
        {
            Id = "log", Title = "Event log",
            Factory = () => new ListBox
            {
                ItemsSource = Enumerable.Range(1, 40).Select(i => $"Event {i:000}: service heartbeat").ToArray()
            }
        });

        // Classic side: one complete recipe tile plus body-content tiles. One tile stays empty
        // so the shared "+ Add content" menu is reachable in both hosts.
        var chart = _catalog.CreateTile("throughput");
        chart.GridX = 0;
        chart.GridY = 0;
        chart.GridW = 2;
        chart.GridH = 2;
        _completeTiles.Add(("throughput", chart));
        Classic.Children.Add(chart);
        AddClassicTile("notes", 2, 0);
        AddClassicTile(null, 3, 0);
        AddClassicTile("status", 2, 1, 2, 1);

        // Matrix side: the same definitions, each materialized as an independent instance.
        Matrix.ContentDefinitions = _catalog;
        Matrix.InsertColumnAfter(0);
        Matrix.InsertRowAfter(0);
        var cells = Matrix.Layout.Cells.OrderBy(c => c.Row).ThenBy(c => c.Column).ToArray();
        Matrix.SetContent(cells[0].Id, "notes");
        Matrix.SetContent(cells[1].Id, "throughput");
        Matrix.SetContent(cells[2].Id, "status");
        // cells[3] stays empty on purpose.

        Matrix.ContentChanged += (_, e) => Report("Matrix", e.AfterId);
        Matrix.LayoutChanged += (_, _) => RefreshLibrary();
        RefreshLibrary();
        Feedback.Text = "Pick '+ Add content' in either host: both menus list the same catalog.";
    }

    private static StackPanel Gauge(string label, int value) => new()
    {
        Spacing = 4,
        Children =
        {
            new TextBlock { Text = $"{label}  {value}%" },
            new ProgressBar { Minimum = 0, Maximum = 100, Value = value }
        }
    };

    private DashboardContentTile AddClassicTile(string? contentId, int x, int y, int w = 1, int h = 1)
    {
        var tile = new DashboardContentTile { ContentDefinitions = _catalog, ContentId = contentId, DisposeRemovedContent = true };
        tile.GridX = x;
        tile.GridY = y;
        tile.GridW = w;
        tile.GridH = h;
        tile.ContentChanged += (_, e) => Report("Classic", e.AfterId);
        _classicTiles.Add(tile);
        Classic.Children.Add(tile);

        return tile;
    }

    private void AddEntry_Click(object? sender, RoutedEventArgs e)
    {
        var number = _next++;
        _catalog.Add(new DashboardContentDefinition
        {
            Id = $"dynamic-{number}", Title = $"Added content {number}",
            Factory = () => new TextBlock { Text = $"Catalog item {number}", TextWrapping = TextWrapping.Wrap }
        });
        RefreshLibrary();
        Feedback.Text = $"Added 'Added content {number}' to the catalog; both hosts list it under '+ Add content' now.";
    }

    /// <summary>Counts the live views of a definition across both dashboards.</summary>
    private int Instances(string id) =>
        Matrix.Layout.Cells.Count(c => c.ContentId == id) +
        _classicTiles.Count(t => t.ContentId == id) +
        _completeTiles.Count(t => t.Id == id);

    /// <summary>Rebuilds the catalog strip with live instance counts for every definition.</summary>
    private void RefreshLibrary()
    {
        Library.Children.Clear();
        Library.Children.Add(new TextBlock { Text = "Catalog:", FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, 10, 0) });
        foreach (var definition in _catalog)
        {
            var count = Instances(definition.Id);
            Library.Children.Add(new TextBlock
            {
                Text = $"{definition.Title} ×{count}",
                Margin = new Thickness(0, 0, 14, 0),
                Opacity = count > 0 ? 1 : 0.55
            });
        }
    }

    private void Report(string host, string? id)
    {
        Feedback.Text = id is null
            ? $"{host}: content cleared."
            : $"{host}: placed '{_catalog.Find(id)?.Title ?? id}' — {Instances(id)} live view{(Instances(id) == 1 ? "" : "s")} across both hosts.";
        RefreshLibrary();
    }

    public void Dispose()
    {
        foreach (var child in Classic.Children.OfType<IDisposable>().ToArray())
            child.Dispose();

        Matrix.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed record TileRecipe(string Id, string Title, Func<Tile> Factory);
}
