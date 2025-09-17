using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;

namespace MiniDashboard.Avalonia.ScottPlot.Cartesian;

/// <summary>
/// Basic OHLC (candle) chart tile expecting tuples of (X, Open, High, Low, Close).
/// </summary>
public class FinancialPlotTile : CartesianPlotTile
{
    /// <summary>Sequence of OHLC tuples.</summary>
    public static readonly StyledProperty<IEnumerable<(double X, double Open, double High, double Low, double Close)>?> OhlcProperty =
        AvaloniaProperty.Register<FinancialPlotTile, IEnumerable<(double X, double Open, double High, double Low, double Close)>?>(nameof(Ohlc));

    static FinancialPlotTile()
    {
        OhlcProperty.Changed.Subscribe(static e => ((FinancialPlotTile)e.Sender).Rebuild());
    }

    /// <inheritdoc cref="OhlcProperty"/>
    public IEnumerable<(double X, double Open, double High, double Low, double Close)>? Ohlc
    {
        get { return GetValue(OhlcProperty); }
        set { SetValue(OhlcProperty, value); }
    }

    /// <summary>
    /// Rebuild the plot data when OHLC collection changes.
    /// </summary>
    private void Rebuild()
    {
        var data = Ohlc?.ToArray();
        if (data is null || data.Length == 0)
        {
            DataBuilder = null; // Nothing to display
            ComposePlotBuilder();
            return;
        }

        DataBuilder = avaPlot =>
        {
            avaPlot.Plot.Clear();
            foreach (var item in data)
            {
                var x = item.X;
                // High-low vertical line
                var hiLo = avaPlot.Plot.Add.Line(x, item.Low, x, item.High);
                // Open tick (left)
                var open = avaPlot.Plot.Add.Line(x - 0.2, item.Open, x, item.Open);
                // Close tick (right)
                var close = avaPlot.Plot.Add.Line(x, item.Close, x + 0.2, item.Close);
                if (DefaultColor != null)
                {
                    // DefaultColor is nullable Color? from base class
                    var c = DefaultColor.Value;
                    hiLo.Color = c;
                    open.Color = c;
                    close.Color = c;
                }
            }
        };
        ComposePlotBuilder();
    }
}
