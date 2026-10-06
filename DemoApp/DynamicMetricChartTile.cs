using System;
using Avalonia.Data;
using DemoApp.Models;
using MiniDashboard.Avalonia.ScottPlot.Cartesian;

namespace DemoApp;

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
