using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using ScottPlot;

namespace MiniDashboard.Avalonia.ScottPlot.Pie;

/// <summary>
/// Concrete pie chart tile that takes a sequence of <see cref="PieSlice"/> objects.
/// </summary>
public class PiePlotTile : PieChartPlotTile
{
    /// <summary>Collection of slices to display.</summary>
    public static readonly StyledProperty<IEnumerable<PieSlice>> SlicesProperty =
        AvaloniaProperty.Register<PiePlotTile, IEnumerable<PieSlice>>(nameof(Slices));

    static PiePlotTile()
    {
        SlicesProperty.Changed.Subscribe(static e => ((PiePlotTile)e.Sender).Rebuild());
    }

    /// <inheritdoc cref="SlicesProperty"/>
    public IEnumerable<PieSlice> Slices
    {
        get { return GetValue(SlicesProperty); }
        set { SetValue(SlicesProperty, value); }
    }

    /// <summary>
    /// Update <see cref="PieChartPlotTile.DataBuilder"/> when slice collection changes.
    /// </summary>
    private void Rebuild()
    {
        if (Slices is not null)
        {
            var slicesList = Slices.ToList(); // Snapshot
            DataBuilder = avaPlot =>
            {
                var pie = avaPlot.Plot.Add.Pie(slicesList);
                // Auto-show legend if any slice has a label
                if (pie.Slices.Any(s => !String.IsNullOrWhiteSpace(s.Label)))
                    avaPlot.Plot.ShowLegend();
            };
            ComposePlotBuilder();
            return;
        }

        // No slices -> clear builder
        DataBuilder = null;
        ComposePlotBuilder();
    }
}
