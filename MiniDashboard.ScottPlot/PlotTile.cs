using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ScottPlot.Avalonia;

namespace MiniDashboard.Avalonia.ScottPlot;

/// <summary>
/// Base tile that hosts an <see cref="AvaPlot" /> and exposes a <see cref="PlotBuilder" /> delegate.
/// </summary>
public class PlotTile : Tile
{
    /// <summary>
    /// Styled property containing the delegate that builds the plot when the control is applied.
    /// </summary>
    public static readonly StyledProperty<Action<AvaPlot>?> PlotBuilderProperty =
        AvaloniaProperty.Register<PlotTile, Action<AvaPlot>?>(nameof(PlotBuilder));

    private AvaPlot? _avaPlot;

    static PlotTile()
    {
        PlotBuilderProperty.Changed.Subscribe(static e => ((PlotTile) e.Sender).ApplyPlotBuilder());
    }

    /// <summary>
    /// Delegate used to populate the <see cref="AvaPlot" /> control with plottables and styling.
    /// </summary>
    public Action<AvaPlot>? PlotBuilder
    {
        get { return GetValue(PlotBuilderProperty); }
        set { SetValue(PlotBuilderProperty, value); }
    }

    /// <summary>
    /// Applies the template then invokes the plot builder if both the part and delegate are available.
    /// </summary>
    /// <param name="e">Template application event args.</param>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _avaPlot = e.NameScope.Find<AvaPlot>("PART_AvaPlot");
        ApplyPlotBuilder();
    }

    private void ApplyPlotBuilder()
    {
        if (_avaPlot is null || PlotBuilder is null)
            return;

        PlotBuilder(_avaPlot);
        _avaPlot.Refresh();
    }
}
