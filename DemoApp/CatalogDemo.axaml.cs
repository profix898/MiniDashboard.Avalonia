using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using MiniDashboard.Avalonia;

namespace DemoApp;

public partial class CatalogDemo : UserControl, IDisposable
{
    private readonly DashboardContentCatalog _catalog;
    private int _next = 1;
    private readonly Tile _firstTile;

    public CatalogDemo()
    {
        InitializeComponent();
        
        var configuredTiles = new[]
        {
            new TileRecipe("summary", "Configured summary", "Built from a classic tile recipe"),
            new TileRecipe("note", "Configured note", "The same TextTile template in either dashboard")
        };
        _catalog = DashboardContentCatalog.FromTiles(configuredTiles, r => r.Id, r => r.Title,
                                                     r => new TextTile { TileHeader = r.Title, Text = r.Text });
        _catalog.Add(new DashboardContentDefinition
        {
            Id = "editor", Title = "Editable notes",
            Factory = () => new TextBox { AcceptsReturn = true, Text = "Independent editor instance", TextWrapping = TextWrapping.Wrap }
        });
        _firstTile = _catalog.CreateTile("summary");
        _firstTile.GridW = 2;
        _firstTile.GridH = 2;
        Classic.Children.Add(_firstTile);
        SecondTile.ContentDefinitions = _catalog;
        Matrix.ContentDefinitions = _catalog;
        SecondTile.ContentId = "editor";
        Matrix.InsertColumnAfter(0);
        Matrix.SetContent(Matrix.Layout.Cells[0].Id, "summary");
        Matrix.SetContent(Matrix.Layout.Cells[1].Id, "editor");
        Classic.PlacementChanged += (_, e) => Feedback.Text = $"Saved tile position: {e.After.X}, {e.After.Y}";
        Matrix.ContentChanged += (_, e) => Feedback.Text = $"Matrix selection: {e.AfterId ?? "empty"}";
        SecondTile.ContentChanged += (_, e) => Feedback.Text = $"Classic selection: {e.AfterId ?? "empty"}";
    }

    private void AddEntry_Click(object? sender, RoutedEventArgs e)
    {
        var number = _next++;
        _catalog.Add(new DashboardContentDefinition
        {
            Id = $"dynamic-{number}", Title = $"Added content {number}", Factory = () => new TextBlock { Text = $"Catalog item {number}" }
        });
        Feedback.Text = $"Added content {number} is now available in both notes tiles’ content menus.";
    }

    public void Dispose()
    {
        if (_firstTile is IDisposable disposable)
            disposable.Dispose();
        
        SecondTile.Dispose();
        Matrix.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed record TileRecipe(string Id, string Title, string Text);
}
