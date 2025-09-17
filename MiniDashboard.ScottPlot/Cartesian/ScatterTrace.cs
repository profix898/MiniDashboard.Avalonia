using System;
using System.Collections.Generic;
using System.Linq;
using ScottPlot;

namespace MiniDashboard.Avalonia.ScottPlot.Cartesian;

/// <summary>
/// Describes a single scatter trace with optional styling and legend label.
/// Raw arrays are stored for efficiency; enumerable inputs are eagerly materialized.
/// </summary>
public sealed class ScatterTrace
{
    /// <summary>
    /// Create a trace from enumerable sequences (materialized to arrays).
    /// </summary>
    /// <param name="x">X data.</param>
    /// <param name="y">Y data (must match length of X).</param>
    /// <param name="label">Optional legend label.</param>
    /// <exception cref="ArgumentException">Thrown if lengths mismatch.</exception>
    public ScatterTrace(IEnumerable<double> x, IEnumerable<double> y, string label = null)
    {
        X = x is double[] xa ? xa : x.ToArray(); // Avoid extra allocation when already array
        Y = y is double[] ya ? ya : y.ToArray();
        if (X.Length != Y.Length)
            throw new ArgumentException("X and Y length mismatch");
        Label = label;
    }

    /// <summary>
    /// Create a trace from pre-existing arrays (referenced directly without copy).
    /// </summary>
    public ScatterTrace(double[] x, double[] y, string label = null)
    {
        if (x.Length != y.Length)
            throw new ArgumentException("X and Y length mismatch");
        X = x;
        Y = y;
        Label = label;
    }

    /// <summary>Optional color override.</summary>
    public Color Color { get; init; }

    /// <summary>Legend label text.</summary>
    public string Label { get; init; }

    /// <summary>Optional line pattern override.</summary>
    public LinePattern LinePattern { get; init; }

    /// <summary>Optional line width override.</summary>
    public double LineWidth { get; init; }

    /// <summary>Optional marker shape override.</summary>
    public MarkerShape MarkerShape { get; init; }

    /// <summary>Optional marker size override.</summary>
    public double MarkerSize { get; init; }

    /// <summary>Controls trace visibility.</summary>
    public bool Visible { get; init; } = true;

    /// <summary>X data points.</summary>
    public double[] X { get; }

    /// <summary>Y data points (same length as <see cref="X"/>).</summary>
    public double[] Y { get; }
}
