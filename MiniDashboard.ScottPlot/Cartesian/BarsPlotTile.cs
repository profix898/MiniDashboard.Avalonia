using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;

namespace MiniDashboard.Avalonia.ScottPlot.Cartesian;

/// <summary>
/// Displays simple bar charts from either (X,Y) points or a sequence of Y values.
/// </summary>
public class BarsPlotTile : CartesianPlotTile
{
    /// <summary>Width factor of bars (0..1 typical).</summary>
    public static readonly StyledProperty<double> BarWidthProperty =
        AvaloniaProperty.Register<BarsPlotTile, double>(nameof(BarWidth), 0.8);

    /// <summary>Explicit (X,Y) pairs. If set overrides <see cref="Values"/>.</summary>
    public static readonly StyledProperty<IEnumerable<(double X, double Y)>?> PointsProperty =
        AvaloniaProperty.Register<BarsPlotTile, IEnumerable<(double X, double Y)>?>(nameof(Points));

    /// <summary>Sequence of Y values (X becomes index). Ignored if <see cref="Points"/> provided.</summary>
    public static readonly StyledProperty<IEnumerable<double>?> ValuesProperty =
        AvaloniaProperty.Register<BarsPlotTile, IEnumerable<double>?>(nameof(Values));

    static BarsPlotTile()
    {
        // Rebuild data layer on any relevant change
        PointsProperty.Changed.Subscribe(static e => ((BarsPlotTile)e.Sender).Rebuild());
        ValuesProperty.Changed.Subscribe(static e => ((BarsPlotTile)e.Sender).Rebuild());
        BarWidthProperty.Changed.Subscribe(static e => ((BarsPlotTile)e.Sender).Rebuild());
    }

    /// <inheritdoc cref="BarWidthProperty"/>
    public double BarWidth
    {
        get { return GetValue(BarWidthProperty); }
        set { SetValue(BarWidthProperty, value); }
    }

    /// <inheritdoc cref="PointsProperty"/>
    public IEnumerable<(double X, double Y)>? Points
    {
        get { return GetValue(PointsProperty); }
        set { SetValue(PointsProperty, value); }
    }

    /// <inheritdoc cref="ValuesProperty"/>
    public IEnumerable<double>? Values
    {
        get { return GetValue(ValuesProperty); }
        set { SetValue(ValuesProperty, value); }
    }

    /// <summary>
    /// Decide which input source to use and produce an appropriate <see cref="CartesianPlotTile.DataBuilder"/>.
    /// </summary>
    private void Rebuild()
    {
        if (Points is not null)
        {
            var ys = Points.Select(p => p.Y).ToArray(); // X ignored for simple vertical bars here
            DataBuilder = avaPlot =>
            {
                avaPlot.Plot.Clear();
                avaPlot.Plot.Add.Bars(ys);
            };
            ComposePlotBuilder();
            return;
        }

        if (Values is not null)
        {
            var ys = Values.ToArray();
            DataBuilder = avaPlot =>
            {
                avaPlot.Plot.Clear();
                avaPlot.Plot.Add.Bars(ys);
            };
            ComposePlotBuilder();
            return;
        }

        // Nothing to show
        DataBuilder = null;
        ComposePlotBuilder();
    }
}
