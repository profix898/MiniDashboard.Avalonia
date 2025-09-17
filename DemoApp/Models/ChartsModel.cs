using System;
using System.Collections.Generic;
using System.Linq;
using MiniDashboard.Avalonia.ScottPlot.Cartesian;
using ScottPlot;
using ScottPlot.Avalonia;

namespace DemoApp.Models;

public class ChartsModel
{
    public Action<AvaPlot> BuildRadarPlot
    {
        get
        {
            return avaPlot =>
            {
                double[,] values = { { 78, 83, 84, 76, 43 }, { 100, 50, 70, 60, 90 } };

                avaPlot.Plot.Add.Radar(values);
            };
        }
    }

    public IEnumerable<PieSlice> PieStatus { get; } = new List<PieSlice>
    {
        new PieSlice { Value = 5, FillColor = Colors.Red, Label = "Red", LegendText = "R" },
        new PieSlice { Value = 2, FillColor = Colors.Orange, Label = "Orange" },
        new PieSlice { Value = 8, FillColor = Colors.Gold, Label = "Yellow" },
        new PieSlice { Value = 4, FillColor = Colors.Green, Label = "Green", LegendText = "G" },
        new PieSlice { Value = 8, FillColor = Colors.Blue, Label = "Blue", LegendText = "B" }
    };

    public IEnumerable<double> Random { get; } =
        Enumerable.Range(0, 500).Select(_ => System.Random.Shared.NextDouble() * 1000.0);

    public IEnumerable<(double X, double Y)> RandomXY { get; } =
        Enumerable.Range(0, 50)
                  .Select(i => ((double) i, 10 + (5 * Math.Sin(i / 8.0)) + (System.Random.Shared.NextDouble() * 2)));

    public IEnumerable<double> SimpleSeries { get; } = new double[] { 12, 18, 15, 22, 30, 28, 35 };

    public IEnumerable<(string Label, double Value)> Status { get; } = new (string, double)[]
    {
        ("200", 740), ("400", 35), ("404", 19), ("500", 6)
    };

    // New: provide multiple scatter traces for the ScatterPlotTile example
    public IEnumerable<ScatterTrace> ScatterTraces { get; } = new List<ScatterTrace>
    {
        // Series A: sine-like
        new ScatterTrace(
            Enumerable.Range(0, 50).Select(i => (double)i),
            Enumerable.Range(0, 50).Select(i => 10 + (5 * Math.Sin(i / 8.0)) + (System.Random.Shared.NextDouble() * 2)),
            "Series A"),

        // Series B: cosine-like offset
        new ScatterTrace(
            Enumerable.Range(0, 50).Select(i => (double)i),
            Enumerable.Range(0, 50).Select(i => 12 + (3 * Math.Cos(i / 6.0)) + (System.Random.Shared.NextDouble() * 2)),
            "Series B")
    };
}
