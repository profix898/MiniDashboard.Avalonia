using System.Collections.Generic;
using System.ComponentModel;

namespace MiniDashboard.Avalonia.Matrix;

/// <summary>A matrix transaction, including old and proposed snapshots for persistence or confirmation.</summary>
public sealed class DashboardMatrixChangeEventArgs : CancelEventArgs
{
    internal DashboardMatrixChangeEventArgs(DashboardMatrixChangeKind kind,
                                            DashboardMatrixLayout before, DashboardMatrixLayout after, IReadOnlyList<DashboardMatrixCellModel> affectedCells)
    {
        Kind = kind;
        Before = before;
        After = after;
        AffectedCells = affectedCells;
    }

    /// <summary>Gets the operation kind.</summary>
    public DashboardMatrixChangeKind Kind { get; }

    /// <summary>Gets the previous snapshot.</summary>
    public DashboardMatrixLayout Before { get; }

    /// <summary>Gets the proposed or committed snapshot.</summary>
    public DashboardMatrixLayout After { get; }

    /// <summary>Gets the previous assignments affected by the operation.</summary>
    public IReadOnlyList<DashboardMatrixCellModel> AffectedCells { get; }
}
