using System;
using Avalonia;
using ScottPlot;
using ScottPlot.Avalonia;

namespace MiniDashboard.Avalonia.ScottPlot.Cartesian;

/// <summary>
/// Base class for Cartesian (XY) ScottPlot tiles. Combines a data layer delegate (<see cref="DataBuilder"/>)
/// with styling and axis configuration producing the final <see cref="PlotBuilder"/> executed by <see cref="PlotTile"/>.
/// </summary>
public class CartesianPlotTile : PlotTile
{
    // --- Data and behavior configuration styled properties ---

    /// <summary>Whether to automatically clear the plot before invoking <see cref="DataBuilder"/>.</summary>
    public static readonly StyledProperty<bool> AutoClearProperty =
        AvaloniaProperty.Register<CartesianPlotTile, bool>(nameof(AutoClear), true);

    /// <summary>Delegate that adds plottables (no styling responsibility).</summary>
    public static readonly StyledProperty<Action<AvaPlot>?> DataBuilderProperty =
        AvaloniaProperty.Register<CartesianPlotTile, Action<AvaPlot>?>(nameof(DataBuilder));

    // --- Global styling defaults (used by derived plot tiles when individual trace values not set) ---
    /// <summary>Fallback color used when traces do not specify one.</summary>
    public static readonly StyledProperty<Color?> DefaultColorProperty =
        AvaloniaProperty.Register<CartesianPlotTile, Color?>(nameof(DefaultColor));

    /// <summary>Fallback line pattern.</summary>
    public static readonly StyledProperty<LinePattern> DefaultLinePatternProperty =
        AvaloniaProperty.Register<CartesianPlotTile, LinePattern>(nameof(DefaultLinePattern), LinePattern.Solid);

    /// <summary>Fallback line width.</summary>
    public static readonly StyledProperty<double> DefaultLineWidthProperty =
        AvaloniaProperty.Register<CartesianPlotTile, double>(nameof(DefaultLineWidth), 1.5);

    /// <summary>Fallback marker shape.</summary>
    public static readonly StyledProperty<MarkerShape> DefaultMarkerShapeProperty =
        AvaloniaProperty.Register<CartesianPlotTile, MarkerShape>(nameof(DefaultMarkerShape), MarkerShape.FilledCircle);

    /// <summary>Fallback marker size.</summary>
    public static readonly StyledProperty<double> DefaultMarkerSizeProperty =
        AvaloniaProperty.Register<CartesianPlotTile, double>(nameof(DefaultMarkerSize), 5);

    /// <summary>Toggle grid visibility.</summary>
    public static readonly StyledProperty<bool> ShowGridProperty =
        AvaloniaProperty.Register<CartesianPlotTile, bool>(nameof(ShowGrid), true);

    // --- Titles and labels ---
    /// <summary>Plot title text.</summary>
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<CartesianPlotTile, string>(nameof(Title));

    /// <summary>X-axis label text.</summary>
    public static readonly StyledProperty<string> XLabelProperty =
        AvaloniaProperty.Register<CartesianPlotTile, string>(nameof(XLabel));

    /// <summary>Optional X-axis upper limit.</summary>
    public static readonly StyledProperty<double?> XMaxProperty =
        AvaloniaProperty.Register<CartesianPlotTile, double?>(nameof(XMax));

    /// <summary>Optional X-axis lower limit.</summary>
    public static readonly StyledProperty<double?> XMinProperty =
        AvaloniaProperty.Register<CartesianPlotTile, double?>(nameof(XMin));

    /// <summary>Y-axis label text.</summary>
    public static readonly StyledProperty<string> YLabelProperty =
        AvaloniaProperty.Register<CartesianPlotTile, string>(nameof(YLabel));

    /// <summary>Optional Y-axis upper limit.</summary>
    public static readonly StyledProperty<double?> YMaxProperty =
        AvaloniaProperty.Register<CartesianPlotTile, double?>(nameof(YMax));

    /// <summary>Optional Y-axis lower limit.</summary>
    public static readonly StyledProperty<double?> YMinProperty =
        AvaloniaProperty.Register<CartesianPlotTile, double?>(nameof(YMin));

    // Subscribe to changes to rebuild composed plot builder
    static CartesianPlotTile()
    {
        // Re-compose whenever any styling or data source changes
        DataBuilderProperty.Changed.Subscribe(static e => ((CartesianPlotTile)e.Sender).ComposePlotBuilder());
        AutoClearProperty.Changed.Subscribe(static e => ((CartesianPlotTile)e.Sender).ComposePlotBuilder());
        TitleProperty.Changed.Subscribe(static e => ((CartesianPlotTile)e.Sender).ComposePlotBuilder());
        XLabelProperty.Changed.Subscribe(static e => ((CartesianPlotTile)e.Sender).ComposePlotBuilder());
        YLabelProperty.Changed.Subscribe(static e => ((CartesianPlotTile)e.Sender).ComposePlotBuilder());
        XMinProperty.Changed.Subscribe(static e => ((CartesianPlotTile)e.Sender).ComposePlotBuilder());
        XMaxProperty.Changed.Subscribe(static e => ((CartesianPlotTile)e.Sender).ComposePlotBuilder());
        YMinProperty.Changed.Subscribe(static e => ((CartesianPlotTile)e.Sender).ComposePlotBuilder());
        YMaxProperty.Changed.Subscribe(static e => ((CartesianPlotTile)e.Sender).ComposePlotBuilder());
        ShowGridProperty.Changed.Subscribe(static e => ((CartesianPlotTile)e.Sender).ComposePlotBuilder());
        DefaultColorProperty.Changed.Subscribe(static e => ((CartesianPlotTile)e.Sender).ComposePlotBuilder());
        DefaultLineWidthProperty.Changed.Subscribe(static e => ((CartesianPlotTile)e.Sender).ComposePlotBuilder());
        DefaultMarkerShapeProperty.Changed.Subscribe(static e => ((CartesianPlotTile)e.Sender).ComposePlotBuilder());
        DefaultMarkerSizeProperty.Changed.Subscribe(static e => ((CartesianPlotTile)e.Sender).ComposePlotBuilder());
        DefaultLinePatternProperty.Changed.Subscribe(static e => ((CartesianPlotTile)e.Sender).ComposePlotBuilder());
    }

    /// <inheritdoc cref="AutoClearProperty"/>
    public bool AutoClear
    {
        get { return GetValue(AutoClearProperty); }
        set { SetValue(AutoClearProperty, value); }
    }

    /// <inheritdoc cref="DataBuilderProperty"/>
    public Action<AvaPlot>? DataBuilder
    {
        get { return GetValue(DataBuilderProperty); }
        set { SetValue(DataBuilderProperty, value); }
    }

    /// <inheritdoc cref="DefaultColorProperty"/>
    public Color? DefaultColor
    {
        get { return GetValue(DefaultColorProperty); }
        set { SetValue(DefaultColorProperty, value); }
    }

    /// <inheritdoc cref="DefaultLinePatternProperty"/>
    public LinePattern DefaultLinePattern
    {
        get { return GetValue(DefaultLinePatternProperty); }
        set { SetValue(DefaultLinePatternProperty, value); }
    }

    /// <inheritdoc cref="DefaultLineWidthProperty"/>
    public double DefaultLineWidth
    {
        get { return GetValue(DefaultLineWidthProperty); }
        set { SetValue(DefaultLineWidthProperty, value); }
    }

    /// <inheritdoc cref="DefaultMarkerShapeProperty"/>
    public MarkerShape DefaultMarkerShape
    {
        get { return GetValue(DefaultMarkerShapeProperty); }
        set { SetValue(DefaultMarkerShapeProperty, value); }
    }

    /// <inheritdoc cref="DefaultMarkerSizeProperty"/>
    public double DefaultMarkerSize
    {
        get { return GetValue(DefaultMarkerSizeProperty); }
        set { SetValue(DefaultMarkerSizeProperty, value); }
    }

    /// <inheritdoc cref="ShowGridProperty"/>
    public bool ShowGrid
    {
        get { return GetValue(ShowGridProperty); }
        set { SetValue(ShowGridProperty, value); }
    }

    /// <inheritdoc cref="TitleProperty"/>
    public string Title
    {
        get { return GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    /// <inheritdoc cref="XLabelProperty"/>
    public string XLabel
    {
        get { return GetValue(XLabelProperty); }
        set { SetValue(XLabelProperty, value); }
    }

    /// <inheritdoc cref="XMaxProperty"/>
    public double? XMax
    {
        get { return GetValue(XMaxProperty); }
        set { SetValue(XMaxProperty, value); }
    }

    /// <inheritdoc cref="XMinProperty"/>
    public double? XMin
    {
        get { return GetValue(XMinProperty); }
        set { SetValue(XMinProperty, value); }
    }

    /// <inheritdoc cref="YLabelProperty"/>
    public string YLabel
    {
        get { return GetValue(YLabelProperty); }
        set { SetValue(YLabelProperty, value); }
    }

    /// <inheritdoc cref="YMaxProperty"/>
    public double? YMax
    {
        get { return GetValue(YMaxProperty); }
        set { SetValue(YMaxProperty, value); }
    }

    /// <inheritdoc cref="YMinProperty"/>
    public double? YMin
    {
        get { return GetValue(YMinProperty); }
        set { SetValue(YMinProperty, value); }
    }

    /// <summary>
    /// Compose and assign the final <see cref="PlotTile.PlotBuilder"/> combining data layer and styling.
    /// </summary>
    protected void ComposePlotBuilder()
    {
        PlotBuilder = avaPlot =>
        {
            // Optional clear
            if (AutoClear)
                avaPlot.Plot.Clear();

            // Data layer
            DataBuilder?.Invoke(avaPlot);

            // Styling layer (titles & labels)
            if (!String.IsNullOrWhiteSpace(Title))
                avaPlot.Plot.Title(Title);
            if (!String.IsNullOrWhiteSpace(XLabel))
                avaPlot.Plot.XLabel(XLabel);
            if (!String.IsNullOrWhiteSpace(YLabel))
                avaPlot.Plot.YLabel(YLabel);

            // Grid visibility (ScottPlot 5: IsVisible property)
            avaPlot.Plot.Grid.IsVisible = ShowGrid;

            // Apply axis limits only if all values present
            if (XMin is not null && XMax is not null && YMin is not null && YMax is not null)
                avaPlot.Plot.Axes.SetLimits(XMin.Value, XMax.Value, YMin.Value, YMax.Value);
        };
    }
}
