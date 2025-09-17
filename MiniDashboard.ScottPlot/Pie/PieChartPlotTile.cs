using System;
using Avalonia;
using ScottPlot.Avalonia;

namespace MiniDashboard.Avalonia.ScottPlot.Pie;

/// <summary>
/// Base pie chart tile using a delegate to populate slices and applying title & legend rules.
/// </summary>
public class PieChartPlotTile : PlotTile
{
    /// <summary>Delegate that constructs pie chart slices (called after clearing).</summary>
    public static readonly StyledProperty<Action<AvaPlot>?> DataBuilderProperty =
        AvaloniaProperty.Register<PieChartPlotTile, Action<AvaPlot>?>(nameof(DataBuilder));

    /// <summary>Legend visibility (null = auto if labeled slices exist).</summary>
    public static readonly StyledProperty<bool?> ShowLegendProperty =
        AvaloniaProperty.Register<PieChartPlotTile, bool?>(nameof(ShowLegend));

    /// <summary>Optional chart title.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<PieChartPlotTile, string?>(nameof(Title));

    static PieChartPlotTile()
    {
        DataBuilderProperty.Changed.Subscribe(static e => ((PieChartPlotTile)e.Sender).ComposePlotBuilder());
        TitleProperty.Changed.Subscribe(static e => ((PieChartPlotTile)e.Sender).ComposePlotBuilder());
        ShowLegendProperty.Changed.Subscribe(static e => ((PieChartPlotTile)e.Sender).ComposePlotBuilder());
    }

    /// <inheritdoc cref="DataBuilderProperty"/>
    public Action<AvaPlot>? DataBuilder
    {
        get { return GetValue(DataBuilderProperty); }
        set { SetValue(DataBuilderProperty, value); }
    }

    /// <inheritdoc cref="ShowLegendProperty"/>
    public bool? ShowLegend
    {
        get { return GetValue(ShowLegendProperty); }
        set { SetValue(ShowLegendProperty, value); }
    }

    /// <inheritdoc cref="TitleProperty"/>
    public string? Title
    {
        get { return GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    /// <summary>
    /// Compose final plot builder applying data delegate then title/legend logic.
    /// </summary>
    protected void ComposePlotBuilder()
    {
        PlotBuilder = avaPlot =>
        {
            avaPlot.Plot.Clear(); // Always start clean
            DataBuilder?.Invoke(avaPlot); // Populate slices
            if (!String.IsNullOrWhiteSpace(Title))
                avaPlot.Plot.Title(Title);

            // Legend handling: explicit override or auto when labeled slices exist
            if (ShowLegend == false)
                avaPlot.Plot.Legend.IsVisible = false;
            else if (ShowLegend == true)
                avaPlot.Plot.ShowLegend();
            else if (avaPlot.Plot.Legend.GetItems().Length > 0)
                avaPlot.Plot.ShowLegend();
        };
    }
}
