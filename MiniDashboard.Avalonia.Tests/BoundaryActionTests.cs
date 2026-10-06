using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace MiniDashboard.Avalonia.Tests;

public class BoundaryActionTests
{
    [AvaloniaFact]
    public void SingleCellPerimeterActionsAreNotHitTestableUntilRevealed()
    {
        using var matrix = new DashboardMatrix { IsHeaderVisible = false };
        var window = Show(matrix);
        try
        {
            var tile = matrix.GetTile(matrix.Layout.Cells[0].Id)!;
            var buttons = Boundaries(matrix);
            
            Assert.Equal(4, buttons.Length);
            
            foreach (var button in buttons)
            {
                Assert.Equal(0, button.Opacity);
                Assert.False(button.IsHitTestVisible);
                Assert.True(button.Bounds.Width >= 24 && button.Bounds.Height >= 24);
                Assert.NotEmpty(AutomationProperties.GetName(button)!);
            }
            
            Assert.False(tile.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_OverlayActions").IsVisible);
            Assert.True(tile.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_Add").IsVisible);
            
            var left = buttons.Single(b => b.Name == "PART_Boundary_Column_0_0");
            
            Open(window, left);
            
            var menu = Assert.IsType<MenuFlyout>(left.Flyout);
            
            var items = menu.Items.OfType<MenuItem>().ToArray();
            
            Assert.Equal(new[] { "Insert column here", "Cell right" }, items.Select(i => i.Header));
            Assert.True(items[0].IsAttachedToVisualTree());
            
            items[0].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            
            Assert.Equal(2, matrix.Layout.Columns.Count);
            Assert.Single(matrix.Layout.Rows);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void InternalBoundaryTargetsItsOwnAdjacentCellsAndUsesRemovalVeto()
    {
        using var matrix = PopulatedMatrix();
        var window = Show(matrix);
        try
        {
            var before = matrix.Layout.Cells.ToArray();
            var boundary = Boundaries(matrix).Single(b => b.Name == "PART_Boundary_Column_1_1");
            
            Open(window, boundary);
            
            var menu = Assert.IsType<MenuFlyout>(boundary.Flyout);
            
            var right = menu.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Cell right"));
            
            right.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Clear content"))
                 .RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            
            Assert.Null(matrix.Layout.GetCell(before[3].Id).ContentId);
            Assert.All(before.Take(3), c => Assert.Equal("notes", matrix.Layout.GetCell(c.Id).ContentId));
            
            menu.Hide();
            matrix.LayoutChanging += (_, e) => e.Cancel = e.Kind == DashboardMatrixChangeKind.RemoveRow;
            Open(window, boundary);
            menu = Assert.IsType<MenuFlyout>(boundary.Flyout);
            
            var left = menu.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Cell left"));
            
            left.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Remove row"))
                .RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            
            Assert.Equal(2, matrix.Layout.Rows.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HorizontalMenuAndDisabledStructureStillAllowContentEditing()
    {
        using var matrix = PopulatedMatrix();
        matrix.CanModifyStructure = false;
        matrix.CanResize = false;
        
        var window = Show(matrix);
        try
        {
            var button = Boundaries(matrix).Single(b => b.Name == "PART_Boundary_Row_1_0");
            
            button.Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Dispatcher.UIThread.RunJobs();
            
            var menu = Assert.IsType<MenuFlyout>(button.Flyout);
            Assert.True(menu.IsOpen);
            
            var items = menu.Items.OfType<MenuItem>().ToArray();
            
            Assert.Equal(new[] { "Insert row here", "Cell above", "Cell below" }, items.Select(i => i.Header));
            Assert.False(items[0].IsEnabled);
            
            var below = items[2];
            
            Assert.False(below.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Remove row")).IsEnabled);
            Assert.True(below.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Change content")).IsEnabled);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HeaderTogglePreservesInstancesAndRestoresCompactSpacing()
    {
        using var matrix = PopulatedMatrix();
        var window = Show(matrix);
        try
        {
            var ids = matrix.Layout.Cells.Select(c => c.Id).ToArray();
            var tiles = ids.Select(matrix.GetTile).ToArray();
            
            Assert.Equal(12, Boundaries(matrix).Length);
            
            matrix.IsHeaderVisible = true;
            window.UpdateLayout();
            
            Assert.Empty(Boundaries(matrix));
            Assert.Equal(new Thickness(6), matrix.LayoutPadding);
            
            var grid = matrix.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "PART_Grid");
            
            Assert.Equal(6, grid.ColumnDefinitions[1].ActualWidth);
            
            matrix.IsHeaderVisible = false;
            window.UpdateLayout();
            
            Assert.Equal(6, grid.ColumnDefinitions[1].ActualWidth);
            Assert.Equal(tiles, ids.Select(matrix.GetTile));
            
            using var frame = window.CaptureRenderedFrame();
            
            Assert.NotNull(frame);
            
            var directory = Environment.GetEnvironmentVariable("MATRIX_TEST_CAPTURE_DIR");
            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                frame.Save(Path.Combine(directory, "matrix-headerless-boundaries.png"), PngBitmapEncoderOptions.Default);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HeaderlessBoundaryDoesNotTakeAwaySplitterDragging()
    {
        using var matrix = PopulatedMatrix();
        var window = Show(matrix);
        try
        {
            var grid = matrix.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "PART_Grid");
            var splitter = grid.Children.OfType<GridSplitter>().First(s => s.ResizeDirection == GridResizeDirection.Columns);
            var start = splitter.TranslatePoint(new Point(splitter.Bounds.Width / 2, 20), window)!.Value;
            
            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            window.MouseMove(start + new Vector(35, 0));
            window.MouseUp(start + new Vector(35, 0), MouseButton.Left);
            
            var weights = matrix.GetSnapshot().Columns;
            
            Assert.NotEqual(weights[0], weights[1]);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void BoundaryRetainsCustomTileActionsAndContentContextMenu()
    {
        var custom = new MenuFlyout { Items = { new MenuItem { Header = "Custom operation" } } };
        var contentMenu = new ContextMenu { Items = { new MenuItem { Header = "Editor operation" } } };
        var editor = new TextBox { ContextMenu = contentMenu };
        var tile = new Tile { HeaderActions = custom, Content = editor };
        using var matrix = new DashboardMatrix
        {
            IsHeaderVisible = false, ContentDefinitions = new[] { new DashboardContentDefinition { Id = "custom", Title = "Custom", TileFactory = () => tile } }
        };
        
        matrix.SetContent(matrix.Layout.Cells[0].Id, "custom");
        
        var window = Show(matrix);
        try
        {
            var point = editor.TranslatePoint(new Point(editor.Bounds.Width - 5, 5), window)!.Value;
            
            window.MouseMove(point);
            
            var hit = Assert.IsAssignableFrom<Visual>(window.InputHitTest(point));
            Assert.True(ReferenceEquals(hit, editor) || hit.GetVisualAncestors().Contains(editor));
            
            var button = Boundaries(matrix).Single(b => b.Name == "PART_Boundary_Row_0_0");
            
            Open(window, button);
            
            var menu = Assert.IsType<MenuFlyout>(button.Flyout);
            
            var cellMenu = menu.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Cell below"));
            var action = cellMenu.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Tile actions"));
            
            menu.Hide();
            action.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            
            Assert.True(custom.IsOpen);
            Assert.Same(contentMenu, editor.ContextMenu);
            
            custom.Hide();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HeaderlessEmptyTileStillOpensCenteredAddContent()
    {
        using var matrix = new DashboardMatrix
        {
            IsHeaderVisible = false, ContentDefinitions = new[] { new DashboardContentDefinition { Id = "notes", Title = "Notes", Factory = () => new TextBox() } }
        };
        var window = Show(matrix);
        try
        {
            var id = matrix.Layout.Cells[0].Id;
            var tile = matrix.GetTile(id)!;
            var button = tile.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_Add");
            
            Open(window, button);
            
            var menu = Assert.IsType<MenuFlyout>(button.Flyout);
            
            var picker = menu.Items.OfType<MenuItem>().First();
            
            Assert.True(picker.IsAttachedToVisualTree());
            Assert.Single(picker.Items.OfType<MenuItem>()).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Equal("notes", matrix.Layout.GetCell(id).ContentId);
            Assert.IsType<TextBox>(matrix.GetTile(id)!.Content);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HoverRevealsOverlayAndMenuPinsItUntilDismissal()
    {
        using var matrix = PopulatedMatrix();
        var window = Show(matrix);
        try
        {
            var button = Boundaries(matrix).Single(b => b.Name == "PART_Boundary_Column_1_0");
            var tile = matrix.GetTile(matrix.Layout.Cells[0].Id)!;
            var boundary = tile.TranslatePoint(new Point(tile.Bounds.Width + 3, 20), window)!.Value;
            
            Assert.Equal(0, button.Opacity);
            
            window.MouseMove(boundary);
            
            Assert.Equal(1, button.Opacity);
            Assert.True(button.IsHitTestVisible);
            
            var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
            
            window.MouseMove(center);

            // Move off the thin boundary and onto the part overlapping the tile.
            var edge = center + new Vector(-8, 0);
            
            window.MouseMove(edge);
            
            Assert.Equal(1, button.Opacity);
            
            window.MouseDown(edge, MouseButton.Left);
            window.MouseUp(edge, MouseButton.Left);
            
            var menu = Assert.IsType<MenuFlyout>(button.Flyout);
            Assert.True(menu.IsOpen);
            
            window.MouseMove(new Point(100, 100));
            
            Assert.Equal(1, button.Opacity);
            
            menu.Hide();
            window.MouseMove(new Point(110, 110));
            
            Assert.Equal(0, button.Opacity);
            Assert.False(button.IsHitTestVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void BoundaryTapWithoutHoverRevealsButtonAndOutsideTapDismissesIt()
    {
        using var matrix = new DashboardMatrix { IsHeaderVisible = false };
        var window = Show(matrix);
        try
        {
            var button = Boundaries(matrix).Single(b => b.Name == "PART_Boundary_Column_0_0");
            var tile = matrix.GetTile(matrix.Layout.Cells[0].Id)!;
            var edge = tile.TranslatePoint(new Point(-2, 35), window)!.Value;
            
            Assert.Equal(0, button.Opacity);
            
            window.MouseDown(edge, MouseButton.Left);
            window.MouseUp(edge, MouseButton.Left);
            
            Assert.Equal(1, button.Opacity);
            
            window.MouseMove(new Point(100, 100));
            
            Assert.Equal(1, button.Opacity); // tap reveal survives travel to the button
            
            window.MouseDown(new Point(100, 100), MouseButton.Left);
            window.MouseUp(new Point(100, 100), MouseButton.Left);
            
            Assert.Equal(0, button.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OverlaySizeDoesNotChangeLayoutAndDraggingDoesNotLatchButtons()
    {
        using var matrix = PopulatedMatrix();
        var window = Show(matrix);
        try
        {
            var ids = matrix.Layout.Cells.Select(c => c.Id).ToArray();
            var bounds = ids.Select(id => matrix.GetTile(id)!.Bounds).ToArray();
            matrix.BoundaryActionSize = 40;
            window.UpdateLayout();
            
            Assert.Equal(bounds, ids.Select(id => matrix.GetTile(id)!.Bounds));
            Assert.Equal(new Thickness(6), matrix.LayoutPadding);
            
            var grid = matrix.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "PART_Grid");
            var splitter = grid.Children.OfType<GridSplitter>().First(s => s.ResizeDirection == GridResizeDirection.Columns);
            var point = splitter.TranslatePoint(new Point(splitter.Bounds.Width / 2, 20), window)!.Value;
            
            window.MouseDown(point, MouseButton.Left);
            window.MouseMove(point + new Vector(35, 0));
            window.MouseUp(point + new Vector(35, 0), MouseButton.Left);
            window.MouseMove(new Point(100, 100));
            
            Assert.All(Boundaries(matrix), b => Assert.Equal(0, b.Opacity));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TouchPointerTapRevealsPerimeterOverlay()
    {
        using var matrix = new DashboardMatrix { IsHeaderVisible = false };
        var window = Show(matrix);
        try
        {
            var tile = matrix.GetTile(matrix.Layout.Cells[0].Id)!;
            var point = tile.TranslatePoint(new Point(-2, 35), window)!.Value;
            using var pointer = new Pointer(42, PointerType.Touch, true);
            
            matrix.RaiseEvent(new PointerPressedEventArgs(matrix, pointer, window, point, 1,
                                                          new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
            matrix.RaiseEvent(new PointerReleasedEventArgs(matrix, pointer, window, point, 2,
                                                           new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None,
                                                           MouseButton.Left));
            
            var button = Boundaries(matrix).Single(b => b.Name == "PART_Boundary_Column_0_0");
            
            Assert.Equal(1, button.Opacity);
            Assert.True(button.IsHitTestVisible);
        }
        finally
        {
            window.Close();
        }
    }

    private static DashboardMatrix PopulatedMatrix()
    {
        var matrix = new DashboardMatrix
        {
            IsHeaderVisible = false,
            ContentDefinitions = new[]
            {
                new DashboardContentDefinition
                {
                    Id = "notes", Title = "Notes", Factory = () => new TextBox { Text = "Interactive content has no overlaid controls.", AcceptsReturn = true }
                }
            }
        };
        
        matrix.InsertColumnAfter(0);
        matrix.InsertRowAfter(0);
        foreach (var cell in matrix.Layout.Cells)
            matrix.SetContent(cell.Id, "notes");
        
        return matrix;
    }

    private static Button[] Boundaries(DashboardMatrix matrix) => matrix.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains("matrix-boundary-action")).ToArray();

    private static Window Show(Control content)
    {
        var window = new Window { Width = 800, Height = 500, Content = content };
        
        window.Show();
        window.UpdateLayout();
        
        return window;
    }

    private static void Open(Window window, Button button)
    {
        if (button.Classes.Contains("matrix-boundary-action"))
        {
            button.Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Dispatcher.UIThread.RunJobs();
            
            Assert.True(Assert.IsType<MenuFlyout>(button.Flyout).IsOpen);
            
            return;
        }
        
        var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
        
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        
        Assert.True(Assert.IsType<MenuFlyout>(button.Flyout).IsOpen);
    }
}
