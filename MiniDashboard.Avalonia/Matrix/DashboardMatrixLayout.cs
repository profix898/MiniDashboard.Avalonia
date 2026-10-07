using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace MiniDashboard.Avalonia.Matrix;

/// <summary>An immutable, serializable matrix snapshot. Structural edits return new snapshots.</summary>
public sealed class DashboardMatrixLayout
{
    /// <summary>Creates a one-cell, equally weighted matrix.</summary>
    public DashboardMatrixLayout()
        : this(new[] { 1d }, new[] { 1d }, (IReadOnlyList<DashboardMatrixCellModel>?) null)
    {
    }

    /// <summary>Creates a validated snapshot. Omitted cells produce a complete empty matrix.</summary>
    [JsonConstructor]
    public DashboardMatrixLayout(IReadOnlyList<double> rows, IReadOnlyList<double> columns,
                                 IReadOnlyList<DashboardMatrixCellModel>? cells)
    {
        Rows = ValidateWeights(rows, nameof(rows));
        Columns = ValidateWeights(columns, nameof(columns));

        var assignments = cells?.ToArray() ??
                          Enumerable.Range(0, Rows.Count).SelectMany(r =>
                                                                         Enumerable.Range(0, Columns.Count).Select(c => new DashboardMatrixCellModel(Guid.NewGuid(), r, c)))
                                    .ToArray();
        if (assignments.Length != checked(Rows.Count * Columns.Count) ||
            assignments.Any(c => c is null || c.Id == Guid.Empty || c.Row < 0 || c.Row >= Rows.Count ||
                                 c.Column < 0 || c.Column >= Columns.Count || (c.ContentId is not null && String.IsNullOrWhiteSpace(c.ContentId))) ||
            assignments.Select(c => c.Id).Distinct().Count() != assignments.Length ||
            assignments.Select(c => (c.Row, c.Column)).Distinct().Count() != assignments.Length)
            throw new ArgumentException("Cells must have unique identities and cover every coordinate exactly once.", nameof(cells));

        Cells = Array.AsReadOnly(assignments.OrderBy(c => c.Row).ThenBy(c => c.Column).ToArray());
    }

    private DashboardMatrixLayout(IReadOnlyList<double> rows, IReadOnlyList<double> columns, DashboardMatrixLayout source)
    {
        Rows = rows;
        Columns = columns;
        Cells = source.Cells;
    }

    /// <summary>Gets logical row star weights.</summary>
    public IReadOnlyList<double> Rows { get; }

    /// <summary>Gets logical column star weights.</summary>
    public IReadOnlyList<double> Columns { get; }

    /// <summary>Gets the complete row-major cell assignment list.</summary>
    public IReadOnlyList<DashboardMatrixCellModel> Cells { get; }

    /// <summary>Finds a cell by its stable identity.</summary>
    public DashboardMatrixCellModel GetCell(Guid id)
        => Cells.FirstOrDefault(c => c.Id == id) ?? throw new ArgumentException("The cell does not belong to this layout.", nameof(id));

    /// <summary>Returns a snapshot with a changed content assignment.</summary>
    public DashboardMatrixLayout WithContent(Guid id, string? contentId)
    {
        GetCell(id);

        return new DashboardMatrixLayout(Rows, Columns, Cells.Select(c => c.Id == id ? c with { ContentId = contentId } : c).ToArray());
    }

    /// <summary>Returns a snapshot with new proportional weights and unchanged assignments.</summary>
    public DashboardMatrixLayout WithWeights(IReadOnlyList<double> rows, IReadOnlyList<double> columns)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(columns);

        if (rows.Count != Rows.Count || columns.Count != Columns.Count)
            throw new ArgumentException("Weight counts must match the matrix dimensions.");

        return new DashboardMatrixLayout(ValidateWeights(rows, nameof(rows)), ValidateWeights(columns, nameof(columns)), this);
    }

    /// <summary>Splits a row, inserting an empty row before it.</summary>
    public DashboardMatrixLayout InsertRowBefore(int row) => Insert(true, row, false);

    /// <summary>Splits a row, inserting an empty row after it.</summary>
    public DashboardMatrixLayout InsertRowAfter(int row) => Insert(true, row, true);

    /// <summary>Splits a column, inserting an empty column before it.</summary>
    public DashboardMatrixLayout InsertColumnBefore(int column) => Insert(false, column, false);

    /// <summary>Splits a column, inserting an empty column after it.</summary>
    public DashboardMatrixLayout InsertColumnAfter(int column) => Insert(false, column, true);

    /// <summary>Removes a row, transferring its weight to the previous row, or the next at index zero.</summary>
    public DashboardMatrixLayout RemoveRow(int row) => Remove(true, row);

    /// <summary>Removes a column, transferring its weight to the previous column, or the next at index zero.</summary>
    public DashboardMatrixLayout RemoveColumn(int column) => Remove(false, column);

    private DashboardMatrixLayout Insert(bool row, int source, bool after)
    {
        var weights = (row ? Rows : Columns).ToList();
        CheckIndex(source, weights.Count);

        var index = source + (after ? 1 : 0);
        weights[source] /= 2;
        weights.Insert(index, weights[source]);

        var cells = Cells.Select(c => row
                                     ? c with { Row = c.Row >= index ? c.Row + 1 : c.Row }
                                     : c with { Column = c.Column >= index ? c.Column + 1 : c.Column }).ToList();
        for (var i = 0; i < (row ? Columns.Count : Rows.Count); i++)
            cells.Add(new DashboardMatrixCellModel(Guid.NewGuid(), row ? index : i, row ? i : index));

        return new DashboardMatrixLayout(row ? weights : Rows, row ? Columns : weights, cells);
    }

    private DashboardMatrixLayout Remove(bool row, int index)
    {
        var weights = (row ? Rows : Columns).ToList();
        CheckIndex(index, weights.Count);
        if (weights.Count == 1)
            throw new InvalidOperationException("A matrix must retain at least one row and column.");

        weights[index == 0 ? 1 : index - 1] += weights[index];
        weights.RemoveAt(index);

        var cells = Cells.Where(c => (row ? c.Row : c.Column) != index).Select(c => row
                                                                                   ? c with { Row = c.Row > index ? c.Row - 1 : c.Row }
                                                                                   : c with { Column = c.Column > index ? c.Column - 1 : c.Column }).ToArray();

        return new DashboardMatrixLayout(row ? weights : Rows, row ? Columns : weights, cells);
    }

    private static IReadOnlyList<double> ValidateWeights(IReadOnlyList<double> weights, string name)
    {
        ArgumentNullException.ThrowIfNull(weights, name);
        if (weights.Count == 0 || weights.Any(w => !Double.IsFinite(w) || w <= 0) ||
            !Double.IsFinite(weights.Sum()))
            throw new ArgumentException("Weights must be positive and finite, with a finite total.", name);

        return Array.AsReadOnly(weights.ToArray());
    }

    private static void CheckIndex(int index, int count)
    {
        if (index < 0 || index >= count)
            throw new ArgumentOutOfRangeException(nameof(index));
    }
}
