using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace MiniDashboard.Avalonia.Tests;

public class MatrixLayoutTests
{
    [Fact]
    public void ColumnInsertionIsGlobalAndPreservesAssignments()
    {
        var before = new DashboardMatrixLayout(new[] { 1d, 2d, 1d }, new[] { 2d, 4d }, null);
        before = before.WithContent(before.Cells[3].Id, "events");
        
        var after = before.InsertColumnBefore(1);
        
        Assert.Equal(new[] { 2d, 2d, 2d }, after.Columns);
        Assert.Equal(9, after.Cells.Count);
        Assert.All(before.Cells, old =>
        {
            var cell = after.GetCell(old.Id);
            
            Assert.Equal(old.Row, cell.Row);
            Assert.Equal(old.Column == 1 ? 2 : 0, cell.Column);
            Assert.Equal(old.ContentId, cell.ContentId);
        });
        Assert.All(after.Cells.Where(c => c.Column == 1), c => Assert.Null(c.ContentId));
        Assert.Equal(6, before.Cells.Count);
    }

    [Fact]
    public void RowInsertionAndRemovalPreserveTotalWeightsAndIdentity()
    {
        var before = new DashboardMatrixLayout();
        var split = before.InsertRowAfter(0);
        
        Assert.Equal(new[] { .5, .5 }, split.Rows);
        Assert.Equal(before.Cells[0].Id, split.Cells[0].Id);
        
        var removed = split.RemoveRow(0);
        
        Assert.Equal(new[] { 1d }, removed.Rows);
        Assert.Equal(split.Cells[1].Id, removed.Cells[0].Id);
        Assert.Equal(0, removed.Cells[0].Row);
        Assert.Throws<InvalidOperationException>(() => removed.RemoveRow(0));
        Assert.Throws<InvalidOperationException>(() => removed.RemoveColumn(0));
    }

    [Fact]
    public void RestoredWeightsAreSplitWithoutResettingOtherProportions()
    {
        var layout = new DashboardMatrixLayout(new[] { .6, .4 }, new[] { .28, .42, .3 }, null);
        var after = layout.InsertColumnAfter(1);
        
        Assert.Equal(new[] { .28, .21, .21, .3 }, after.Columns);
        Assert.Equal(layout.Rows, after.Rows);
        Assert.Equal(new[] { .6, .2, .2 }, layout.InsertRowBefore(1).Rows);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(Double.NaN)]
    [InlineData(Double.PositiveInfinity)]
    public void RejectsInvalidWeights(double weight) => Assert.Throws<ArgumentException>(() => new DashboardMatrixLayout(new[] { weight }, new[] { 1d }, null));

    [Fact]
    public void ValidatesCompleteUniqueCellCoverageAndIndices()
    {
        var cell = new DashboardMatrixCellModel(Guid.NewGuid(), 0, 0);
        
        Assert.Throws<ArgumentException>(() => new DashboardMatrixLayout(new[] { 1d }, new[] { 1d, 1d }, new[] { cell, cell }));
        Assert.Throws<ArgumentException>(() => new DashboardMatrixLayout(new[] { 1d }, new[] { 1d }, new[] { cell with { Row = 1 } }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DashboardMatrixLayout().InsertColumnAfter(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DashboardMatrixLayout().RemoveRow(1));
    }

    [Fact]
    public void SnapshotCopiesInputAndRoundTripsWithoutControlsOrFactories()
    {
        var rows = new[] { 1d };
        var layout = new DashboardMatrixLayout(rows, new[] { .3, .7 }, null);
        layout = layout.WithContent(layout.Cells[0].Id, "unavailable-plugin");
        rows[0] = 99;
        
        var json = JsonSerializer.Serialize(layout);
        var restored = JsonSerializer.Deserialize<DashboardMatrixLayout>(json)!;
        
        Assert.Equal(1d, layout.Rows[0]);
        Assert.Equal(layout.Rows, restored.Rows);
        Assert.Equal(layout.Columns, restored.Columns);
        Assert.Equal(layout.Cells, restored.Cells);
    }
}
