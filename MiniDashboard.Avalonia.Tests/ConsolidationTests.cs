using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using MiniDashboard.Avalonia.Content;
using MiniDashboard.Avalonia.Grid;
using MiniDashboard.Avalonia.Matrix;
using MiniDashboard.Avalonia.Tiles;
using Xunit;

namespace MiniDashboard.Avalonia.Tests;

public class ConsolidationTests
{
    [AvaloniaFact]
    public void FixesHeaderSubscriptionLostAfterReattach()
    {
        var tile = new Tile { TileHeader = "before" };
        var panel = new DashboardPanel();

        panel.Children.Add(tile);

        var window = new Window { Width = 600, Height = 400, Content = panel };
        try
        {
            window.Show();
            window.UpdateLayout();

            var presenter = tile.GetVisualDescendants().OfType<DashboardHeader>().Single();

            Assert.Equal("before", presenter.DisplayContent);

            panel.Children.Remove(tile);
            panel.Children.Add(tile);
            window.UpdateLayout();
            tile.TileHeader = "after";

            Assert.Equal("after", presenter.DisplayContent);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FixesTileGridMoveRecreatingContent()
    {
        var items = new ObservableCollection<string> { "one", "two" };
        var panel = new DashboardItemsPanel { ItemTemplate = new FuncDataTemplate<string>((item, _) => new TextBox { Text = item }), ItemsSource = items };
        var window = new Window { Width = 600, Height = 400, Content = panel };
        try
        {
            window.Show();
            window.UpdateLayout();

            var old = panel.Children.OfType<TextBox>().ToArray();
            old[0].Text = "unsaved";
            items.Move(0, 1);

            Assert.Contains(old[0], panel.Children);
            Assert.Contains(panel.Children.OfType<TextBox>(), x => x.Text == "unsaved");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FixesDragNotUpdatingTileGridProperties()
    {
        var tile = new Tile { TileHeader = "drag", GridX = 0, GridY = 0 };
        var panel = new DashboardPanel { Columns = 3, Rows = 2 };

        panel.Children.Add(tile);

        var window = new Window { Width = 600, Height = 400, Content = panel };
        try
        {
            window.Show();
            window.UpdateLayout();

            var header = tile.GetVisualDescendants().OfType<DashboardHeader>().Single(b => b.Name == "PART_Header");
            var start = header.TranslatePoint(new Point(40, 15), window)!.Value;

            window.MouseDown(start, MouseButton.Left);
            window.MouseMove(start + new Vector(200, 0));
            window.MouseUp(start + new Vector(200, 0), MouseButton.Left);
            window.UpdateLayout();

            Assert.Equal(1, DashboardPanel.GetX(tile));
            Assert.Equal(1, tile.GridX);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FixesMatrixStructuralEditBreakingLegacyTileHeader()
    {
        var tile = new Tile { TileHeader = "before" };
        var matrix = new DashboardMatrix { ContentDefinitions = new[] { new DashboardContentDefinition { Id = "tile", Title = "tile", TileFactory = () => tile } } };

        matrix.SetContent(matrix.Layout.Cells[0].Id, "tile");

        var window = new Window { Width = 600, Height = 400, Content = matrix };
        try
        {
            window.Show();
            window.UpdateLayout();

            var presenter = tile.GetVisualDescendants().OfType<DashboardHeader>().Single();

            matrix.InsertColumnAfter(0);
            window.UpdateLayout();
            tile.TileHeader = "after";

            Assert.Equal("after", presenter.DisplayContent);
        }
        finally
        {
            window.Close();
            matrix.Dispose();
        }
    }

    [AvaloniaFact]
    public void FixesLazyCatalogRecreatingUnchangedContent()
    {
        var calls = 0;
        var matrix = new DashboardMatrix();
        matrix.ContentDefinitions = new[] { "a", "b" }.Select(id => new DashboardContentDefinition
        {
            Id = id, Title = id, Factory = () =>
            {
                calls++;

                return new TextBox();
            }
        });
        matrix.InsertColumnAfter(0);
        matrix.SetContent(matrix.Layout.Cells[0].Id, "a");

        Assert.Equal(1, calls);

        matrix.SetContent(matrix.Layout.Cells[1].Id, "b");

        Assert.Equal(2, calls); // unchanged A is retained

        matrix.Dispose();
    }

    [AvaloniaFact]
    public void FixesTileGridItemsSourceInitializationOrderDependency()
    {
        var panel = new DashboardItemsPanel();
        panel.ItemsSource = new[] { "one" };
        panel.ItemTemplate = new FuncDataTemplate<string>((item, _) => new TextBlock { Text = item });
    }
}
