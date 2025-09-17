using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;

namespace MiniDashboard.Avalonia.ScottPlot.Cartesian;

/// <summary>
/// Efficient signal plot tile for regularly spaced samples with optional aggregation parameters.
/// </summary>
public class SignalPlotTile : CartesianPlotTile
{
    /// <summary>Threshold beyond which down-sampling/aggregation may be applied.</summary>
    public static readonly StyledProperty<int> AggregationThresholdProperty =
        AvaloniaProperty.Register<SignalPlotTile, int>(nameof(AggregationThreshold), 20_000);

    /// <summary>Spacing between samples along X.</summary>
    public static readonly StyledProperty<double> SamplePeriodProperty =
        AvaloniaProperty.Register<SignalPlotTile, double>(nameof(SamplePeriod), 1.0);

    /// <summary>Enables aggregation logic (placeholder for future behavior).</summary>
    public static readonly StyledProperty<bool> UseAggregationProperty =
        AvaloniaProperty.Register<SignalPlotTile, bool>(nameof(UseAggregation), true);

    /// <summary>Sequence of Y sample values.</summary>
    public static readonly StyledProperty<IEnumerable<double>?> ValuesProperty =
        AvaloniaProperty.Register<SignalPlotTile, IEnumerable<double>?>(nameof(Values));

    static SignalPlotTile()
    {
        // Trigger data rebuild on relevant changes
        ValuesProperty.Changed.Subscribe(static e => ((SignalPlotTile)e.Sender).Rebuild());
        SamplePeriodProperty.Changed.Subscribe(static e => ((SignalPlotTile)e.Sender).Rebuild());
        UseAggregationProperty.Changed.Subscribe(static e => ((SignalPlotTile)e.Sender).Rebuild());
        AggregationThresholdProperty.Changed.Subscribe(static e => ((SignalPlotTile)e.Sender).Rebuild());
    }

    /// <inheritdoc cref="AggregationThresholdProperty"/>
    public int AggregationThreshold
    {
        get { return GetValue(AggregationThresholdProperty); }
        set { SetValue(AggregationThresholdProperty, value); }
    }

    /// <inheritdoc cref="SamplePeriodProperty"/>
    public double SamplePeriod
    {
        get { return GetValue(SamplePeriodProperty); }
        set { SetValue(SamplePeriodProperty, value); }
    }

    /// <inheritdoc cref="UseAggregationProperty"/>
    public bool UseAggregation
    {
        get { return GetValue(UseAggregationProperty); }
        set { SetValue(UseAggregationProperty, value); }
    }

    /// <inheritdoc cref="ValuesProperty"/>
    public IEnumerable<double>? Values
    {
        get { return GetValue(ValuesProperty); }
        set { SetValue(ValuesProperty, value); }
    }

    /// <summary>
    /// Build the data layer or clear if insufficient values.
    /// </summary>
    private void Rebuild()
    {
        var vals = Values?.ToArray();
        if (vals is null || vals.Length == 0)
        {
            DataBuilder = null; // Nothing to plot
            ComposePlotBuilder();
            return;
        }

        var samplePeriod = SamplePeriod <= 0 ? 1.0 : SamplePeriod; // Guard invalid value

        DataBuilder = avaPlot =>
        {
            avaPlot.Plot.Clear();
            avaPlot.Plot.Add.Signal(vals, samplePeriod);
        };
        ComposePlotBuilder();
    }
}
