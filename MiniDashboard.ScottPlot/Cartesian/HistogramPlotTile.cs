using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using ScottPlot.Statistics;

namespace MiniDashboard.Avalonia.ScottPlot.Cartesian;

/// <summary>
/// Renders a histogram from numeric samples with configurable bin count and normalization.
/// </summary>
public class HistogramPlotTile : CartesianPlotTile
{
    /// <summary>Number of bins used when constructing the histogram.</summary>
    public static readonly StyledProperty<int> BinCountProperty =
        AvaloniaProperty.Register<HistogramPlotTile, int>(nameof(BinCount), 10);

    /// <summary>Whether to normalize bin heights (placeholder for future behavior).</summary>
    public static readonly StyledProperty<bool> NormalizeProperty =
        AvaloniaProperty.Register<HistogramPlotTile, bool>(nameof(Normalize));

    /// <summary>Sample values used to create the histogram.</summary>
    public static readonly StyledProperty<IEnumerable<double>?> SamplesProperty =
        AvaloniaProperty.Register<HistogramPlotTile, IEnumerable<double>?>(nameof(Samples));

    /// <summary>Relative scale of each bar compared to the bin width. Use a value &lt;= 1.0 to create spacing between bars.</summary>
    public static readonly StyledProperty<double> BarWidthScaleProperty =
        AvaloniaProperty.Register<HistogramPlotTile, double>(nameof(BarWidthScale), 0.8);

    static HistogramPlotTile()
    {
        // Rebuild histogram when inputs change
        SamplesProperty.Changed.Subscribe(static e => ((HistogramPlotTile)e.Sender).Rebuild());
        BinCountProperty.Changed.Subscribe(static e => ((HistogramPlotTile)e.Sender).Rebuild());
        NormalizeProperty.Changed.Subscribe(static e => ((HistogramPlotTile)e.Sender).Rebuild());
        BarWidthScaleProperty.Changed.Subscribe(static e => ((HistogramPlotTile)e.Sender).Rebuild());
    }

    /// <inheritdoc cref="BinCountProperty"/>
    public int BinCount
    {
        get { return GetValue(BinCountProperty); }
        set { SetValue(BinCountProperty, value); }
    }

    /// <inheritdoc cref="NormalizeProperty"/>
    public bool Normalize
    {
        get { return GetValue(NormalizeProperty); }
        set { SetValue(NormalizeProperty, value); }
    }

    /// <inheritdoc cref="SamplesProperty"/>
    public IEnumerable<double>? Samples
    {
        get { return GetValue(SamplesProperty); }
        set { SetValue(SamplesProperty, value); }
    }

    /// <inheritdoc cref="BarWidthScaleProperty"/>
    public double BarWidthScale
    {
        get => GetValue(BarWidthScaleProperty);
        set => SetValue(BarWidthScaleProperty, value);
    }

    /// <summary>
    /// Build the histogram data layer or clear if insufficient data.
    /// </summary>
    private void Rebuild()
    {
        var data = Samples?.ToArray();
        if (data is null || data.Length == 0 || BinCount <= 0)
        {
            DataBuilder = null; // Nothing to plot
            ComposePlotBuilder();
            return;
        }

        DataBuilder = avaPlot =>
        {
            avaPlot.Plot.Clear();
            var hist = Histogram.WithBinCount(BinCount, data);

            // Use Bars so individual bar sizes can be adjusted (ScottPlot 5 doesn't expose bar width directly on Histogram)
            var barPlot = avaPlot.Plot.Add.Bars(hist.Bins, hist.Counts);

            // Size each bar slightly less than the width of a bin to create spacing
            if (barPlot.Bars is not null)
            {
                var scale = Math.Max(0, BarWidthScale);
                foreach (var bar in barPlot.Bars)
                {
                    // hist.FirstBinSize gives the bin width; multiply by scale for the actual bar size
                    bar.Size = hist.FirstBinSize * scale;
                }
            }

            // Remove bottom margin so bars sit flush with the axis (same as example)
            avaPlot.Plot.Axes.Margins(bottom: 0);
        };
        ComposePlotBuilder();
    }
}
