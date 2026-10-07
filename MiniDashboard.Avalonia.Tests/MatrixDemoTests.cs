using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using DemoApp;
using Xunit;

namespace MiniDashboard.Avalonia.Tests;

public class MatrixDemoTests
{
    [AvaloniaTheory]
    [InlineData(1000)]
    [InlineData(1001)]
    [InlineData(901)]
    public void RightmostTileBordersRemainVisibleAfterStarRounding(int width)
    {
        var demo = new MatrixDemo();
        var window = new Window { Width = width, Height = 650, Content = demo, RequestedThemeVariant = ThemeVariant.Light };
        try
        {
            window.Show();
            window.UpdateLayout();
            
            var matrix = demo.GetVisualDescendants().OfType<DashboardMatrix>().Single();
            using var frame = window.CaptureRenderedFrame();
            
            Assert.NotNull(frame);
            
            foreach (var model in matrix.Layout.Cells.Where(c => c.Column == matrix.Layout.Columns.Count - 1))
            {
                var tile = matrix.GetTile(model.Id)!;
                var edge = tile.TranslatePoint(new Point(tile.Bounds.Width - 1, tile.Bounds.Height / 2), window)!.Value;
                var pixel = Marshal.AllocHGlobal(4);
                try
                {
                    frame.CopyPixels(new PixelRect((int) Math.Round(edge.X), (int) Math.Round(edge.Y), 1, 1), pixel, 4, 4);
                    
                    Assert.True(Marshal.ReadByte(pixel, 0) < 240 && Marshal.ReadByte(pixel, 1) < 240 &&
                                Marshal.ReadByte(pixel, 2) < 240, $"Right border is missing at {edge}, width {width}, row {model.Row}.");
                }
                finally
                {
                    Marshal.FreeHGlobal(pixel);
                }
            }
            
            matrix.Dispose();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(1000, 650, false)]
    [InlineData(390, 800, true)]
    public void DemoRendersAndSnapshotButtonsRestoreGlobalStructure(int width, int height, bool dark)
    {
        var demo = new MatrixDemo();
        var window = new Window { Width = width, Height = height, Content = demo, RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light };
        try
        {
            window.Show();
            window.UpdateLayout();
            
            var matrix = demo.GetVisualDescendants().OfType<DashboardMatrix>().Single();
            using (var frame = window.CaptureRenderedFrame())
            {
                Assert.NotNull(frame);
                
                var directory = Environment.GetEnvironmentVariable("MATRIX_TEST_CAPTURE_DIR");
                if (!String.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                    frame.Save(Path.Combine(directory, $"matrix-{width}-{(dark ? "dark" : "light")}.png"), PngBitmapEncoderOptions.Default);
                }
            }
            
            Assert.Equal(6, matrix.Layout.Cells.Count);
            
            var buttons = demo.GetVisualDescendants().OfType<Button>().ToArray();
            
            buttons.Single(b => Equals(b.Content, "Save snapshot")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            matrix.InsertColumnAfter(0);
            
            Assert.Equal(8, matrix.Layout.Cells.Count);
            
            buttons.Single(b => Equals(b.Content, "Restore snapshot")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            
            Assert.Equal(6, matrix.Layout.Cells.Count);
            Assert.False(matrix.RemoveRow(0)); // Demo's host protection vetoes populated removal.
            
            matrix.Dispose();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(1000, 650, false)]
    [InlineData(390, 800, true)]
    public void SharedCatalogDemoRendersBothHostsAndAddsEntries(int width, int height, bool dark)
    {
        using var demo = new CatalogDemo();
        var window = new Window { Width = width, Height = height, Content = demo, RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light };
        try
        {
            window.Show();
            window.UpdateLayout();
            
            var matrix = demo.GetVisualDescendants().OfType<DashboardMatrix>().Single();
            var catalog = Assert.IsType<DashboardContentCatalog>(matrix.ContentDefinitions);
            var tiles = demo.GetVisualDescendants().OfType<DashboardContentTile>().Where(t => t.Parent is DashboardPanel).ToArray();
            
            Assert.Equal(3, tiles.Length);
            Assert.All(tiles, t => Assert.Same(catalog, t.ContentDefinitions));
            Assert.All(tiles, t => Assert.False(String.IsNullOrEmpty(t.TileHeader)));
            Assert.Equal(4, matrix.Layout.Cells.Count);
            Assert.Equal(3, matrix.Layout.Cells.Count(c => c.ContentId is not null));
            
            demo.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Add catalog entry"))
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            
            Assert.Equal(5, catalog.Count);
            
            // The new entry is instantly selectable in the add menus of both hosts.
            var classicMenu = Assert.IsType<MenuFlyout>(tiles.Single(t => t.ContentId is null).AddContentActions);
            Assert.Contains(classicMenu.Items.OfType<MenuItem>(), m => Equals(m.Header, "Added content 1"));
            
            var emptyCell = matrix.Layout.Cells.Single(c => c.ContentId is null);
            var matrixMenu = Assert.IsType<MenuFlyout>(matrix.GetTile(emptyCell.Id)!.AddContentActions);
            Assert.Contains(matrixMenu.Items.OfType<MenuItem>(), m => Equals(m.Header, "Added content 1"));
            
            using var frame = window.CaptureRenderedFrame();
            
            Assert.NotNull(frame);
            
            var directory = Environment.GetEnvironmentVariable("MATRIX_TEST_CAPTURE_DIR");
            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                frame.Save(Path.Combine(directory, $"catalog-{width}-{(dark ? "dark" : "light")}.png"), PngBitmapEncoderOptions.Default);
            }
        }
        finally
        {
            window.Close();
        }
    }
}
