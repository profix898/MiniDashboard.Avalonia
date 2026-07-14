using Avalonia;
using Avalonia.Controls;
using DemoApp.Models;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace DemoApp;

public partial class MainWindow : Window
{
    private int _dynamicTileCounter = 1;
    private string _dynamicLayoutSummary = "";

    public MainWindow()
    {
        InitializeComponent();

        ChartsModel = new ChartsModel();
        DynamicTiles.CollectionChanged += OnDynamicTilesChanged;
    SeedDynamicTiles();
        UpdateDynamicLayoutSummary();

        DataContext = this;
    }

    public ChartsModel ChartsModel { get; set; }

    public ObservableCollection<DynamicTileViewModel> DynamicTiles { get; } = [];

    public string DynamicLayoutSummary
    {
        get { return _dynamicLayoutSummary; }
        private set { SetAndRaise(DynamicLayoutSummaryProperty, ref _dynamicLayoutSummary, value); }
    }

    public static readonly DirectProperty<MainWindow, string> DynamicLayoutSummaryProperty =
        AvaloniaProperty.RegisterDirect<MainWindow, string>(
            nameof(DynamicLayoutSummary),
            window => window.DynamicLayoutSummary);

    public ObservableCollection<DashboardTableRow> TableRows { get; } =
    [
        new DashboardTableRow { Service = "API Gateway", Region = "eu-central", Status = "Healthy" },
        new DashboardTableRow { Service = "Worker Pool", Region = "us-east", Status = "Healthy" },
        new DashboardTableRow { Service = "Event Store", Region = "ap-south", Status = "Degraded" }
    ];

    private void AddDynamicTextTile_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var offset = DynamicTiles.Count % 5;
        DynamicTiles.Add(new DynamicTextTileViewModel
        {
            Title = $"Note {_dynamicTileCounter++}",
            Text = "Added at runtime. Resize me and the summary updates.",
            X = offset * 2,
            Y = 7,
            Width = 2,
            Height = 2
        });
    }

    private void RemoveDynamicTile_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DynamicTiles.Count > 0)
            DynamicTiles.RemoveAt(DynamicTiles.Count - 1);
    }

    private void ResetDynamicTiles_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        foreach (var tile in DynamicTiles)
            tile.PropertyChanged -= OnDynamicTilePropertyChanged;

        DynamicTiles.Clear();
        SeedDynamicTiles();
    }

    private void SeedDynamicTiles()
    {
        DynamicTiles.Add(new DynamicTextTileViewModel { Title = "Dynamic Note", Text = "Built from a view model via DashboardItemsPanel.", X = 0, Y = 0, Width = 3, Height = 2 });
        DynamicTiles.Add(new DynamicTableTileViewModel { Title = "Dynamic Table", X = 3, Y = 0, Width = 4, Height = 3, Rows = TableRows.ToArray() });
        DynamicTiles.Add(new DynamicScatterTileViewModel { Title = "Dynamic Scatter", X = 7, Y = 0, Width = 4, Height = 3, Series = new double[] { 6, 9, 5, 11, 14, 10, 16 } });
        DynamicTiles.Add(new DynamicServicesTileViewModel { Title = "Derived Table", X = 0, Y = 3, Width = 4, Height = 3, Rows = TableRows.ToArray() });
        DynamicTiles.Add(new DerivedMetricTileViewModel { Title = "Derived Scatter", X = 4, Y = 3, Width = 4, Height = 3, Series = new double[] { 12, 10, 13, 18, 16, 21, 19 } });
    }

    private void OnDynamicTilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<DynamicTileViewModel>())
                item.PropertyChanged -= OnDynamicTilePropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<DynamicTileViewModel>())
                item.PropertyChanged += OnDynamicTilePropertyChanged;
        }

        UpdateDynamicLayoutSummary();
    }

    private void OnDynamicTilePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DynamicTileViewModel.X) or nameof(DynamicTileViewModel.Y) or nameof(DynamicTileViewModel.Width) or nameof(DynamicTileViewModel.Height))
            UpdateDynamicLayoutSummary();
    }

    private void UpdateDynamicLayoutSummary()
    {
        DynamicLayoutSummary = String.Join("  |  ", DynamicTiles.Select(tile => $"{tile.Title}: {tile.X},{tile.Y} {tile.Width}x{tile.Height}"));
    }
}
