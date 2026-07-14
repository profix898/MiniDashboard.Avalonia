using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using DemoApp.Models;
using MiniDashboard.Avalonia;
using MiniDashboard.Avalonia.ScottPlot.Cartesian;

namespace DemoApp;

public class DynamicServicesTile : TableViewTile
{
    public DynamicServicesTile()
    {
        Bind(ItemsSourceProperty, new Binding(nameof(DynamicServicesTileViewModel.Rows)));
        Columns =
        [
            new TableViewColumn { Header = "Service", Binding = new Binding(nameof(DashboardTableRow.Service)), Width = new GridLength(2, GridUnitType.Star) },
            new TableViewColumn { Header = "Region", Binding = new Binding(nameof(DashboardTableRow.Region)), Width = new GridLength(1, GridUnitType.Star) },
            new TableViewColumn { Header = "Status", Binding = new Binding(nameof(DashboardTableRow.Status)), Width = new GridLength(1, GridUnitType.Star) }
        ];
    }

    protected override Type StyleKeyOverride => typeof(TableViewTile);
}

public class DynamicMetricChartTile : ScatterPlotTile
{
    public DynamicMetricChartTile()
    {
        Bind(YsProperty, new Binding(nameof(DerivedMetricTileViewModel.Series)));
        Title = "Derived Metric";
        YLabel = "Value";
    }

    protected override Type StyleKeyOverride => typeof(ScatterPlotTile);
}