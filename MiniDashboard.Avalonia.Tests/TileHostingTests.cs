using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DemoApp;
using MiniDashboard.Avalonia.Content;
using MiniDashboard.Avalonia.Grid;
using MiniDashboard.Avalonia.Matrix;
using MiniDashboard.Avalonia.Tiles;
using Xunit;
using AvaloniaGrid = Avalonia.Controls.Grid;

namespace MiniDashboard.Avalonia.Tests;

public class TileHostingTests
{
    [AvaloniaFact]
    public void CompleteTilesAreDirectGridChildrenAndKeepTheirOwnSettings()
    {
        var ownMenu = new MenuFlyout();
        var tile = new TextTile { TileHeader = "Custom title", Text = "Body", HeaderActions = ownMenu, IsHeaderVisible = false, GridW = 3, GridH = 2 };

        AvaloniaGrid.SetColumnSpan(tile, 3);

        using var matrix = new DashboardMatrix { ContentDefinitions = new[] { new DashboardContentDefinition { Id = "tile", Title = "Catalog title", TileFactory = () => tile } } };
        var id = matrix.Layout.Cells[0].Id;

        matrix.SetContent(id, "tile");

        var window = Show(matrix);
        try
        {
            Assert.Same(tile, matrix.GetTile(id));
            Assert.IsType<AvaloniaGrid>(tile.Parent);
            Assert.Single(matrix.GetVisualDescendants().OfType<Tile>());
            Assert.Equal(new Thickness(6), tile.Padding);
            Assert.Equal(new Thickness(1), tile.BorderThickness);
            Assert.Equal(1, AvaloniaGrid.GetColumnSpan(tile));
            Assert.True(tile.IsResizable);
            Assert.False(tile.IsResizeGripVisible);
            Assert.False(tile.IsHeaderVisible);
            Assert.True(tile.IsHeaderPresented);
            Assert.Equal("Custom title", tile.TileHeader);
            Assert.Same(ownMenu, tile.HeaderActions);
            Assert.NotSame(ownMenu, tile.EffectiveHeaderActions);
            Assert.False(tile.GetVisualDescendants().OfType<Thumb>().Single().IsVisible);

            matrix.IsHeaderVisible = false;

            Assert.False(tile.IsHeaderPresented);
            Assert.False(tile.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_OverlayActions").IsVisible);

            matrix.ClearContent(id);

            Assert.Null(tile.Parent);
            Assert.True(tile.IsResizeGripVisible);
            Assert.False(tile.IsHeaderPresented);
            Assert.Same(ownMenu, tile.EffectiveHeaderActions);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void BodyContentTilesDelegateSelectionAndVetoToMatrix()
    {
        using var matrix = new DashboardMatrix
        {
            ContentDefinitions = new[]
            {
                new DashboardContentDefinition { Id = "a", Title = "A", Factory = () => new TextBox { Text = "state" } },
                new DashboardContentDefinition { Id = "b", Title = "B", Factory = () => new TextBlock() }
            }
        };
        var window = Show(matrix);
        try
        {
            var id = matrix.Layout.Cells[0].Id;

            var empty = Assert.IsType<DashboardContentTile>(matrix.GetTile(id));
            Assert.True(empty.SetContent("a"));

            var tile = Assert.IsType<DashboardContentTile>(matrix.GetTile(id));
            var body = Assert.IsType<TextBox>(tile.Content);
            Assert.Equal("a", tile.ContentId);

            matrix.ContentChanging += (_, e) => e.Cancel = e.AfterId == "b";

            Assert.False(tile.SetContent("b"));

            tile.ContentId = "b";

            Assert.Equal("a", tile.ContentId);
            Assert.Same(body, tile.Content);
            Assert.Equal("a", matrix.Layout.GetCell(id).ContentId);

            matrix.InsertColumnAfter(0);

            var target = matrix.Layout.Cells[1].Id;

            matrix.MoveContent(id, target);

            Assert.Same(tile, matrix.GetTile(target));
            Assert.Same(body, tile.Content);
            Assert.Equal("state", body.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MatrixMenuSwapsCompleteTileInstances()
    {
        using var matrix = new DashboardMatrix
        {
            ContentDefinitions = new[]
            {
                new DashboardContentDefinition { Id = "a", Title = "A", TileFactory = () => new TextTile { Text = "A" } },
                new DashboardContentDefinition { Id = "b", Title = "B", TileFactory = () => new TextTile { Text = "B" } }
            }
        };

        matrix.InsertColumnAfter(0);

        var a = matrix.Layout.Cells[0].Id;
        var b = matrix.Layout.Cells[1].Id;

        matrix.SetContent(a, "a");
        matrix.SetContent(b, "b");

        var window = Show(matrix);
        try
        {
            var first = matrix.GetTile(a)!;
            var second = matrix.GetTile(b)!;

            var menu = Assert.IsType<MenuFlyout>(first.EffectiveHeaderActions);

            menu.ShowAt(first);

            var move = menu.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Move / swap tile"));

            Assert.Single(move.Items.OfType<MenuItem>()).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Same(first, matrix.GetTile(b));
            Assert.Same(second, matrix.GetTile(a));
            Assert.Equal("b", matrix.Layout.GetCell(a).ContentId);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MoveMenuListsOnlyAdjacentCells()
    {
        using var matrix = new DashboardMatrix { ContentDefinitions = new[] { new DashboardContentDefinition { Id = "notes", Title = "Notes", Factory = () => new TextBox() } } };
        matrix.InsertColumnAfter(0);
        matrix.InsertColumnAfter(0);
        matrix.InsertRowAfter(0);
        matrix.InsertRowAfter(0);
        var window = Show(matrix);
        try
        {
            var center = matrix.Layout.Cells.Single(c => c.Row == 1 && c.Column == 1);
            var centerMenu = Assert.IsType<MenuFlyout>(matrix.GetTile(center.Id)!.EffectiveHeaderActions);
            var centerMove = centerMenu.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Move / swap tile"));

            Assert.Equal(new[] { "Cell above", "Cell below", "Cell left", "Cell right" },
                         centerMove.Items.OfType<MenuItem>().Select(i => i.Header));
            Assert.False(centerMove.IsEnabled); // Empty source: nothing to move or swap yet.

            matrix.SetContent(center.Id, "notes");

            // Assigning body content wraps it in a new tile; re-read the live menu.
            centerMenu = Assert.IsType<MenuFlyout>(matrix.GetTile(center.Id)!.EffectiveHeaderActions);
            centerMove = centerMenu.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Move / swap tile"));
            Assert.True(centerMove.IsEnabled);

            var corner = matrix.Layout.Cells.Single(c => c.Row == 0 && c.Column == 0);
            var cornerMenu = Assert.IsType<MenuFlyout>(matrix.GetTile(corner.Id)!.EffectiveHeaderActions);
            var cornerMove = cornerMenu.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Move / swap tile"));

            Assert.Equal(new[] { "Cell below", "Cell right" }, cornerMove.Items.OfType<MenuItem>().Select(i => i.Header));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void CatalogCreatesCompleteOrContentTilesWithoutNesting()
    {
        var catalog = new DashboardContentCatalog(new[]
        {
            new DashboardContentDefinition { Id = "body", Title = "Body", Factory = () => new TextBox() },
            new DashboardContentDefinition { Id = "tile", Title = "Tile", TileFactory = () => new TextTile() }
        });

        using var body = Assert.IsType<DashboardContentTile>(catalog.CreateTile("body"));
        Assert.IsType<TextBox>(body.Content);
        Assert.IsType<TextTile>(catalog.CreateTile("tile"));
        Assert.Throws<InvalidOperationException>(() => body.SetContent("tile"));
        Assert.Equal("body", body.ContentId);
        Assert.Throws<ArgumentException>(() => catalog.Add(new DashboardContentDefinition
        {
            Id = "both", Title = "Invalid", Factory = () => new Control(), TileFactory = () => new Tile()
        }));
    }

    [AvaloniaFact]
    public void RemovingCompleteTileDisposesOnceAndRestoresHostSettings()
    {
        var tile = new DisposableTile();
        using var matrix = new DashboardMatrix
        {
            DisposeRemovedContent = true, ContentDefinitions = new[] { new DashboardContentDefinition { Id = "a", Title = "A", TileFactory = () => tile } }
        };
        var id = matrix.Layout.Cells[0].Id;

        matrix.SetContent(id, "a");

        var window = Show(matrix);
        try
        {
            matrix.ClearContent(id);
            matrix.Dispose();

            Assert.Equal(1, tile.Disposals);
            Assert.Null(tile.Parent);
            Assert.True(tile.IsResizeGripVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TileGridResizeBehaviorRespectsDisabledGrip()
    {
        var tile = new Tile();
        var panel = new DashboardPanel { Rows = 2, Columns = 3, Children = { tile } };
        var window = Show(panel);
        try
        {
            var grip = tile.GetVisualDescendants().OfType<Thumb>().Single();
            var start = grip.TranslatePoint(new Point(24, 24), window)!.Value;

            window.MouseDown(start, MouseButton.Left);
            window.MouseMove(start + new Vector(210, 0));
            window.MouseUp(start + new Vector(210, 0), MouseButton.Left);

            Assert.Equal(2, tile.GridW);

            tile.IsResizable = false;

            Assert.False(grip.IsVisible);
            Assert.False(tile.IsResizeGripVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HeaderlessTileWithoutActionsDoesNotCoverHostedContent()
    {
        var tile = new TextTile { IsHeaderVisible = false, IsResizable = false };
        var window = Show(tile);
        try
        {
            var overlay = tile.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_OverlayActions");

            Assert.False(overlay.IsVisible);

            tile.HeaderActions = new MenuFlyout();

            Assert.True(overlay.IsVisible);

            tile.HeaderActions = null;

            Assert.False(overlay.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void CompleteTileFactoryFailurePreservesCurrentTile()
    {
        var current = new TextTile();
        var borrowed = new TextTile();
        var panel = new DashboardPanel { Children = { borrowed } };
        using var matrix = new DashboardMatrix
        {
            ContentDefinitions = new[]
            {
                new DashboardContentDefinition { Id = "good", Title = "Good", TileFactory = () => current },
                new DashboardContentDefinition { Id = "bad", Title = "Bad", TileFactory = () => borrowed }
            }
        };
        var id = matrix.Layout.Cells[0].Id;

        matrix.SetContent(id, "good");

        var window = Show(matrix);
        try
        {
            Assert.Throws<InvalidOperationException>(() => matrix.SetContent(id, "bad"));
            Assert.Same(current, matrix.GetTile(id));
            Assert.Equal("good", matrix.Layout.GetCell(id).ContentId);
            Assert.Same(panel, borrowed.Parent);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void EmptyMatrixDemoAddButtonShowsVisibleCatalogChoices()
    {
        var demo = new MatrixDemo();
        var window = new Window { Width = 1000, Height = 650, Content = demo };

        window.Show();
        window.UpdateLayout();

        try
        {
            var matrix = demo.GetVisualDescendants().OfType<DashboardMatrix>().Single();
            var empty = matrix.Layout.Cells.First(c => c.ContentId is null);
            var tile = matrix.GetTile(empty.Id)!;
            var button = tile.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_Add");
            var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;

            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            var menu = Assert.IsType<MenuFlyout>(button.Flyout);
            Assert.True(menu.IsOpen,
                        $"Button={button.Bounds}; point={point}; visible={button.IsVisible}; hit={window.InputHitTest(point)}; items={menu.Items.Count}; tileParent={tile.Parent}");

            // The add button lists the catalog directly instead of the full cell menu.
            var choices = menu.Items.OfType<MenuItem>().ToArray();

            Assert.Equal(new[] { "Editable notes", "System status", "Scrollable event log" }, choices.Select(c => c.Header));

            var choice = choices[0];

            Assert.True(choice.IsAttachedToVisualTree());

            choice.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

            Assert.Equal("notes", matrix.Layout.GetCell(empty.Id).ContentId);

            matrix.Dispose();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void BoundaryChromeIsHiddenUntilHoveredOrKeyboardFocused()
    {
        using var matrix = new DashboardMatrix();

        matrix.InsertColumnAfter(0);

        var window = Show(matrix);
        try
        {
            var splitter = matrix.GetVisualDescendants().OfType<GridSplitter>().Single();

            Assert.Equal(0, splitter.Opacity);
            Assert.True(splitter.IsHitTestVisible);

            var boundary = splitter.TranslatePoint(new Point(4, 60), window)!.Value;

            window.MouseMove(boundary);

            Assert.True(splitter.Opacity > 0);

            window.MouseMove(new Point(15, 15));

            Assert.Equal(0, splitter.Opacity);

            splitter.Focus(NavigationMethod.Tab);

            Assert.True(splitter.Opacity > 0);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TileGridAndMatrixHaveMatchingSixDipGaps()
    {
        var first = new Tile();
        var second = new Tile { GridX = 1 };
        var tileGrid = new DashboardPanel { Rows = 1, Columns = 2, Children = { first, second } };
        var window = Show(tileGrid);
        try
        {
            Assert.Equal(6, second.Bounds.Left - first.Bounds.Right);

            using var matrix = new DashboardMatrix();

            matrix.InsertColumnAfter(0);
            window.Content = matrix;
            window.UpdateLayout();

            var cells = matrix.Layout.Cells.Select(c => matrix.GetTile(c.Id)!).ToArray();

            Assert.Equal(6, cells[1].Bounds.Left - cells[0].Bounds.Right);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TileGridContentMenuShowsNewCatalogEntriesOnReopen()
    {
        var catalog = new DashboardContentCatalog { new DashboardContentDefinition { Id = "one", Title = "One", Factory = () => new TextBox() } };
        using var tile = new DashboardContentTile { ContentDefinitions = catalog };
        var window = Show(tile);
        try
        {
            var menu = Assert.IsType<MenuFlyout>(tile.HeaderActions);

            menu.ShowAt(tile);
            Dispatcher.UIThread.RunJobs();

            var picker = menu.Items.OfType<MenuItem>().First();

            Assert.True(picker.IsAttachedToVisualTree());

            menu.Hide();
            catalog.Add(new DashboardContentDefinition { Id = "two", Title = "Two", Factory = () => new TextBlock() });
            menu.ShowAt(tile);
            Dispatcher.UIThread.RunJobs();
            picker = menu.Items.OfType<MenuItem>().First();

            Assert.Equal(2, picker.Items.Count);
            Assert.True(picker.IsAttachedToVisualTree());

            menu.Hide();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void PointerResizeDoesNotLeaveSplitterHighlightedAfterPointerLeaves()
    {
        using var matrix = new DashboardMatrix();

        matrix.InsertColumnAfter(0);

        var window = Show(matrix);
        try
        {
            var splitter = matrix.GetVisualDescendants().OfType<GridSplitter>().Single();
            var start = splitter.TranslatePoint(new Point(4, 80), window)!.Value;

            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            window.MouseMove(start + new Vector(45, 0));
            window.MouseUp(start + new Vector(45, 0), MouseButton.Left);

            var resized = matrix.GetSnapshot();

            Assert.NotEqual(resized.Columns[0], resized.Columns[1]);

            window.MouseMove(new Point(20, 80));

            Assert.Equal(0, splitter.Opacity);

            var hit = splitter.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_HitTarget");

            Assert.Equal(new Thickness(0), hit.BorderThickness);
            Assert.Equal(resized.Columns, matrix.GetSnapshot().Columns);

            // Keyboard users still get a visible boundary and focus outline.
            matrix.GetTile(matrix.Layout.Cells[0].Id)!.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_Actions").Focus();
            splitter.Focus(NavigationMethod.Tab);

            Assert.True(splitter.Opacity > 0);
            Assert.Equal(new Thickness(1), hit.BorderThickness);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TileChromeMatchesTheSharedVisualMetrics()
    {
        var tile = new DashboardContentTile();
        var window = Show(tile);
        try
        {
            // Inner content padding equals the six-DIP gap between tiles in both hosts.
            Assert.Equal(new Thickness(6), tile.Padding);

            var title = tile.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Title");

            Assert.Equal(FontWeight.Medium, title.FontWeight);
            Assert.Equal(new Thickness(6, 4), title.Margin);
        }
        finally
        {
            window.Close();
        }
    }

    private static Window Show(Control control)
    {
        var window = new Window { Width = 600, Height = 400, Content = control };

        window.Show();
        window.UpdateLayout();

        return window;
    }

    private sealed class DisposableTile : Tile, IDisposable
    {
        public int Disposals { get; private set; }

        public void Dispose() => Disposals++;
    }
}
