using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;

namespace MiniDashboard.Avalonia.ScottPlot.Cartesian;

/// <summary>
/// Scatter plot tile supporting multiple data input modes: individual traces, point tuples, or separate X/Y sequences.
/// </summary>
public class ScatterPlotTile : CartesianPlotTile
{
    /// <summary>Explicit (X,Y) tuples.</summary>
    public static readonly StyledProperty<IEnumerable<(double X, double Y)>?> PointsProperty =
        AvaloniaProperty.Register<ScatterPlotTile, IEnumerable<(double X, double Y)>?>(nameof(Points));

    /// <summary>Controls legend visibility (null = auto).</summary>
    public static readonly StyledProperty<bool?> ShowLegendProperty =
        AvaloniaProperty.Register<ScatterPlotTile, bool?>(nameof(ShowLegend));

    /// <summary>Collection of rich traces (overrides <see cref="Points"/>, <see cref="Xs"/>, <see cref="Ys"/>).</summary>
    public static readonly StyledProperty<IEnumerable<ScatterTrace>?> TracesProperty =
        AvaloniaProperty.Register<ScatterPlotTile, IEnumerable<ScatterTrace>?>(nameof(Traces));

    /// <summary>X values sequence (used with <see cref="Ys"/> when <see cref="Points"/> and <see cref="Traces"/> absent).</summary>
    public static readonly StyledProperty<IEnumerable<double>?> XsProperty =
        AvaloniaProperty.Register<ScatterPlotTile, IEnumerable<double>?>(nameof(Xs));

    /// <summary>Y values sequence (paired with <see cref="Xs"/> or indexes).</summary>
    public static readonly StyledProperty<IEnumerable<double>?> YsProperty =
        AvaloniaProperty.Register<ScatterPlotTile, IEnumerable<double>?>(nameof(Ys));

    static ScatterPlotTile()
    {
        PointsProperty.Changed.Subscribe(static e => ((ScatterPlotTile)e.Sender).Rebuild());
        YsProperty.Changed.Subscribe(static e => ((ScatterPlotTile)e.Sender).Rebuild());
        TracesProperty.Changed.Subscribe(static e => ((ScatterPlotTile)e.Sender).OnTracesChanged(e.OldValue, e.NewValue));
        ShowLegendProperty.Changed.Subscribe(static e => ((ScatterPlotTile)e.Sender).ComposePlotBuilder());
    }

    /// <inheritdoc cref="PointsProperty"/>
    public IEnumerable<(double X, double Y)>? Points
    {
        get { return GetValue(PointsProperty); }
        set { SetValue(PointsProperty, value); }
    }

    /// <inheritdoc cref="ShowLegendProperty"/>
    public bool? ShowLegend
    {
        get { return GetValue(ShowLegendProperty); }
        set { SetValue(ShowLegendProperty, value); }
    }

    /// <inheritdoc cref="TracesProperty"/>
    public IEnumerable<ScatterTrace>? Traces
    {
        get { return GetValue(TracesProperty); }
        set { SetValue(TracesProperty, value); }
    }

    /// <inheritdoc cref="XsProperty"/>
    public IEnumerable<double>? Xs
    {
        get { return GetValue(XsProperty); }
        set { SetValue(XsProperty, value); }
    }

    /// <inheritdoc cref="YsProperty"/>
    public IEnumerable<double>? Ys
    {
        get { return GetValue(YsProperty); }
        set { SetValue(YsProperty, value); }
    }

    /// <summary>
    /// Update subscriptions when trace collection reference changes.
    /// </summary>
    private void OnTracesChanged(object? oldValue, object? newValue)
    {
        Unsubscribe(oldValue as IEnumerable<ScatterTrace>);
        Subscribe(newValue as IEnumerable<ScatterTrace>);
        Rebuild();
    }

    private void Subscribe(IEnumerable<ScatterTrace>? traces)
    {
        if (traces is INotifyCollectionChanged incc)
            incc.CollectionChanged += OnTraceCollectionChanged;
    }

    private void Unsubscribe(IEnumerable<ScatterTrace>? traces)
    {
        if (traces is INotifyCollectionChanged incc)
            incc.CollectionChanged -= OnTraceCollectionChanged;
    }

    private void OnTraceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Rebuild();
    }

    /// <summary>
    /// Determine active data input mode and configure <see cref="DataBuilder"/> accordingly.
    /// </summary>
    private void Rebuild()
    {
        // Priority: Traces > Points > Ys/Xs
        if (Traces is { } traces && traces.Any())
        {
            var tracesSnapshot = traces.Where(t => t.Visible).ToList(); // Snapshot to avoid mutation issues
            DataBuilder = avaPlot =>
            {
                avaPlot.Plot.Clear();
                foreach (var t in tracesSnapshot)
                    avaPlot.Plot.Add.Scatter(t.X, t.Y);
            };
            ComposePlotBuilder();
            return;
        }

        if (Points is not null)
        {
            var xs = Points.Select(p => p.X).ToArray();
            var ys = Points.Select(p => p.Y).ToArray();
            DataBuilder = avaPlot =>
            {
                avaPlot.Plot.Clear();
                avaPlot.Plot.Add.Scatter(xs, ys);
            };
            ComposePlotBuilder();
            return;
        }

        if (Ys is not null)
        {
            var ys = Ys.ToArray();
            var xs = Xs is not null ? Xs.ToArray() : Enumerable.Range(0, ys.Length).Select(i => (double)i).ToArray();
            DataBuilder = avaPlot =>
            {
                avaPlot.Plot.Clear();
                avaPlot.Plot.Add.Scatter(xs, ys);
            };
            ComposePlotBuilder();
            return;
        }

        // No data
        DataBuilder = null;
        ComposePlotBuilder();
    }
}
