using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using MiniDashboard.Avalonia.Grid;
using MiniDashboard.Avalonia.Tiles;
using Xunit;

namespace MiniDashboard.Avalonia.Tests;

public class TileGridPickingTests
{
    [AvaloniaFact]
    public async Task TileGridSelectionInterceptsContentAndDoesNotChangePlacement()
    {
        var dashboard = new DashboardPanel { Rows = 1, Columns = 2 };
        var clicks = 0;
        var content = new Button { Content = "Live button" };
        content.Click += (_, _) => clicks++;
        var first = new Tile { TileHeader = "First", Content = content };
        var second = new Tile { TileHeader = "Second", GridX = 1 };
        DashboardPanel.SetX(second, 1);
        dashboard.Children.Add(first);
        dashboard.Children.Add(second);
        var window = new Window { Width = 600, Height = 300, Content = dashboard };
        window.Show();
        window.UpdateLayout();

        try
        {
            var original = first.Bounds;

            var pick = dashboard.PickTileAsync(tile => ReferenceEquals(tile, first));
            window.UpdateLayout();

            Assert.False(dashboard.TrySetPlacement(first, 1, 0, 1, 1));
            var overlay = dashboard.GetVisualDescendants().OfType<Button>().Single(b => ReferenceEquals(b.Tag, first));
            var point = overlay.TranslatePoint(new Point(100, 100), window)!.Value;
            var background = overlay.GetVisualDescendants().OfType<Border>().First(b => b.Background is ISolidColorBrush);
            var fill = Assert.IsAssignableFrom<ISolidColorBrush>(background.Background).Color;
            Assert.Equal(204, fill.A);

            window.MouseMove(point);
            window.UpdateLayout();

            Assert.Equal(166, Assert.IsAssignableFrom<ISolidColorBrush>(background.Background).Color.A);
            Assert.Equal(FontWeight.Bold, overlay.FontWeight);
            Assert.Equal(new Thickness(1), overlay.BorderThickness);
            Assert.Equal(Color.Parse("#FF8A94A3"), Assert.IsAssignableFrom<ISolidColorBrush>(overlay.BorderBrush).Color);

            window.MouseMove(new Point(0, 0));
            window.UpdateLayout();

            Assert.Equal(fill, Assert.IsAssignableFrom<ISolidColorBrush>(background.Background).Color);

            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);

            Assert.Same(first, await pick);
            Assert.Equal(0, clicks);
            Assert.Equal(original, first.Bounds);
            Assert.False(dashboard.IsPickingTile);
            Assert.DoesNotContain(dashboard.GetVisualDescendants().OfType<Button>(), b => b.Tag is Tile);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task ItemsDashboardSupportsKeyboardPickingAndRemovalCancelsSelection()
    {
        var first = new Tile { TileHeader = "First" };
        var second = new Tile { TileHeader = "Second", GridX = 1 };
        DashboardPanel.SetX(second, 1);
        using var dashboard = new DashboardItemsPanel { Rows = 1, Columns = 2 };
        dashboard.Children.Add(first);
        dashboard.Children.Add(second);
        var window = new Window { Width = 600, Height = 300, Content = dashboard };
        window.Show();
        window.UpdateLayout();

        try
        {
            var pick = dashboard.PickTileAsync(_ => true);
            window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);

            Assert.Same(second, await pick);

            pick = dashboard.PickTileAsync(_ => true);
            dashboard.Children.Remove(second);

            Assert.Null(await pick);
            Assert.False(dashboard.IsPickingTile);

            pick = dashboard.PickTileAsync(_ => true);
            window.Content = null;

            Assert.Null(await pick);
            Assert.False(dashboard.IsPickingTile);
        }
        finally
        {
            window.Close();
        }
    }
}
