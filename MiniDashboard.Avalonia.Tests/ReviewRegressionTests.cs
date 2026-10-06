using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace MiniDashboard.Avalonia.Tests;

public class ReviewRegressionTests
{
    [Fact]
    public void WeightReplacementRejectsNullArguments()
    {
        var layout = new DashboardMatrixLayout();
        
        Assert.Throws<ArgumentNullException>(() => layout.WithWeights(null!, new[] { 1d }));
        Assert.Throws<ArgumentNullException>(() => layout.WithWeights(new[] { 1d }, null!));
    }

    [AvaloniaFact]
    public void MovingAnOccupiedCellToItselfIsANoOp()
    {
        using var matrix = new DashboardMatrix();
        var id = matrix.Layout.Cells[0].Id;
        matrix.SetContent(id, "missing");
        
        Assert.False(matrix.MoveContent(id, id));
        Assert.Equal("missing", matrix.Layout.GetCell(id).ContentId);
    }

    [AvaloniaFact]
    public void DisposedItemsHostDoesNotSubscribeToNewSource()
    {
        using var panel = new DashboardItemsPanel();
        var window = Show(panel);
        try
        {
            panel.Dispose();
            var source = new TrackingSource();
            
            panel.ItemsSource = source;
            
            Assert.Equal(0, source.Subscribers);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ThrowingProportionVetoRestoresDisplayedWeights()
    {
        using var matrix = new DashboardMatrix();
        matrix.InsertColumnAfter(0);
        var window = Show(matrix);
        try
        {
            var grid = matrix.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "PART_Grid");
            var before = matrix.GetSnapshot();
            matrix.LayoutChanging += (_, e) =>
            {
                if (e.Kind == DashboardMatrixChangeKind.Proportions)
                    throw new InvalidOperationException("Host failure");
            };
            grid.ColumnDefinitions[0].Width = new GridLength(4, GridUnitType.Star);
            
            Assert.Throws<InvalidOperationException>(() => matrix.GetSnapshot());
            
            Assert.Same(before, matrix.Layout);
            Assert.Equal(before.Columns[0], grid.ColumnDefinitions[0].Width.Value);
            Assert.Equal(before.Columns[1], grid.ColumnDefinitions[2].Width.Value);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MatrixMenuReflectsChangedCustomTileActions()
    {
        var tile = new Tile();
        using var matrix = new DashboardMatrix
        {
            ContentDefinitions = new[] { new DashboardContentDefinition { Id = "tile", Title = "Tile", TileFactory = () => tile } }
        };
        matrix.SetContent(matrix.Layout.Cells[0].Id, "tile");
        var window = Show(matrix);
        try
        {
            tile.HeaderActions = new MenuFlyout();
            
            var menu = Assert.IsType<MenuFlyout>(tile.EffectiveHeaderActions);
            Assert.Contains(menu.Items.OfType<MenuItem>(), i => Equals(i.Header, "Tile actions"));
            
            tile.HeaderActions = null;
            
            Assert.DoesNotContain(menu.Items.OfType<MenuItem>(), i => Equals(i.Header, "Tile actions"));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SuccessfulNoOpPlacementClearsPreviousFailure()
    {
        var tile = new Tile();
        var panel = new DashboardPanel { Children = { tile } };
        Assert.False(panel.TrySetPlacement(tile, -1, 0, 1, 1));
        
        Assert.True(panel.TrySetPlacement(tile, 0, 0, 1, 1));
        
        Assert.Equal(DashboardPlacementFailureReason.None, panel.LastPlacementFailureReason);
    }

    [AvaloniaFact]
    public void MatrixEmptyTitleUsesSharedLocalizationResource()
    {
        using var matrix = new DashboardMatrix();
        matrix.Resources["DashboardEmpty"] = "Leer";
        var window = Show(matrix);
        try
        {
            var id = matrix.Layout.Cells[0].Id;
            
            Assert.Equal("Leer", matrix.GetTile(id)!.TileHeader);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MatrixDemoDisposalRetiresItsOwnedMatrix()
    {
        using var demo = new DemoApp.MatrixDemo();
        var matrix = demo.FindControl<DashboardMatrix>("Matrix")!;
        
        demo.Dispose();
        
        Assert.Throws<ObjectDisposedException>(() => matrix.InsertRowAfter(0));
    }

    private static Window Show(Control control)
    {
        var window = new Window { Width = 600, Height = 400, Content = control };
        window.Show();
        window.UpdateLayout();
        
        return window;
    }

    private sealed class TrackingSource : IEnumerable, INotifyCollectionChanged
    {
        public int Subscribers { get; private set; }

        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add { Subscribers++; }
            remove { Subscribers--; }
        }

        public IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();
    }
}
