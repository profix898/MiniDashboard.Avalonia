using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace MiniDashboard.Avalonia.Tests;

public class SharedContentTests
{
    [Fact]
    public void CatalogRejectsDuplicatesWithoutChangingIndex()
    {
        var a = Definition("a", () => new Control());
        var catalog = new DashboardContentCatalog(new[] { a });
        
        Assert.Throws<ArgumentException>(() => catalog.Add(Definition("a", () => new Control())));
        Assert.Same(a, catalog.Find("a"));
        Assert.Single(catalog);
        
        catalog.Add(Definition("b", () => new Control()));
        
        Assert.Throws<ArgumentException>(() => catalog[1] = a);
        Assert.Equal("b", catalog[1].Id);
        
        catalog.RemoveAt(0);
        
        Assert.Null(catalog.Find("a"));
    }

    [AvaloniaFact]
    public void OneCatalogCreatesIndependentBodiesForBothLayouts()
    {
        var catalog = DashboardContentCatalog.FromItems(new[] { ("a", "Shared title") }, i => i.Item1, i => i.Item2,
                                                        _ => new TextBox { Text = "independent" });
        using var tile = new DashboardContentTile { ContentDefinitions = catalog, ContentId = "a" };
        using var matrix = new DashboardMatrix { ContentDefinitions = catalog };
        
        matrix.SetContent(matrix.Layout.Cells[0].Id, "a");
        
        var window = Show(new StackPanel { Children = { tile, matrix }, Height = 400 });
        try
        {
            Assert.Equal("Shared title", tile.TileHeader);
            
            var cell = matrix.GetVisualDescendants().OfType<Tile>().Single();
            
            Assert.Equal("Shared title", cell.TileHeader);
            Assert.IsType<TextBox>(tile.Content);
            Assert.IsType<TextBox>(cell.Content);
            Assert.NotSame(tile.Content, cell.Content);
            Assert.NotNull(tile.GetVisualDescendants().OfType<DashboardHeader>().Single().Actions);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ContentTileVetoAndFactoryFailureRetainIdAndControl()
    {
        using var tile = new DashboardContentTile
        {
            ContentDefinitions = new[] { Definition("a", () => new TextBox()), Definition("bad", () => throw new Exception("factory")) }, ContentId = "a"
        };
        var original = tile.Content;
        EventHandler<DashboardContentChangingEventArgs> veto = (_, e) => e.Cancel = true;
        tile.ContentChanging += veto;
        
        Assert.False(tile.SetContent("bad"));
        Assert.Equal("a", tile.ContentId);
        
        tile.ContentChanging -= veto;
        
        var errors = 0;
        tile.ContentFailed += (_, _) => errors++;
        
        Assert.Throws<Exception>(() => tile.SetContent("bad"));
        Assert.Equal(1, errors);
        Assert.Same(original, tile.Content);
        Assert.Equal("a", tile.ContentId);
    }

    [AvaloniaFact]
    public void ContentTileWritesThroughBindingsAndDisposesExactlyOnce()
    {
        var vm = new Assignment { Id = "a" };
        var a = new DisposableBody();
        var b = new DisposableBody();
        using var tile = new DashboardContentTile
        {
            DisposeRemovedContent = true, ContentDefinitions = new[] { Definition("a", () => a), Definition("b", () => b) }, DataContext = vm
        };
        
        tile.Bind(DashboardContentTile.ContentIdProperty, new Binding(nameof(Assignment.Id)));
        
        Assert.Same(a, tile.Content);
        Assert.True(tile.SetContent("b"));
        Assert.Equal("b", vm.Id);
        Assert.Equal(1, a.Count);
        
        vm.Id = null;
        
        Assert.Null(tile.ContentId);
        Assert.Null(tile.Content);
        Assert.Equal(1, b.Count);
    }

    [AvaloniaFact]
    public void ContentTileAndMatrixObserveTheSameCatalogEdits()
    {
        var catalog = new DashboardContentCatalog();
        using var tile = new DashboardContentTile { ContentDefinitions = catalog, ContentId = "late" };
        using var matrix = new DashboardMatrix { ContentDefinitions = catalog };
        
        matrix.SetContent(matrix.Layout.Cells[0].Id, "late");
        
        var window = Show(new StackPanel { Children = { tile, matrix } });
        try
        {
            catalog.Add(Definition("late", () => new TextBox()));
            
            Assert.IsType<TextBox>(tile.Content);
            Assert.IsType<TextBox>(matrix.GetVisualDescendants().OfType<Tile>().Single().Content);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MatrixContentVetoRollsBackWholeRemoval()
    {
        using var matrix = new DashboardMatrix();
        
        matrix.InsertColumnAfter(0);
        foreach (var cell in matrix.Layout.Cells)
            matrix.SetContent(cell.Id, "unknown");
        
        var before = matrix.Layout;
        matrix.ContentChanging += (_, e) => e.Cancel = e.AfterId is null;
        
        Assert.False(matrix.RemoveColumn(0));
        Assert.Same(before, matrix.Layout);
    }

    [AvaloniaFact]
    public void TemplateAndTileAdaptersProduceFreshViewsWithoutChangingTileSettings()
    {
        var items = new[] { new Assignment { Id = "one" } };
        var templateCatalog = DashboardContentCatalog.FromTemplate(items, i => i.Id!, _ => "Template",
                                                                   new FuncDataTemplate<Assignment>((_, _) => new TextBox()));
        var first = templateCatalog[0].Factory!();
        
        Assert.Same(items[0], first.DataContext);
        Assert.NotSame(first, templateCatalog[0].Factory!());
        
        var legacy = DashboardContentCatalog.FromTiles(items, i => i.Id!, _ => "Legacy",
                                                       _ => new TextTile { Text = "body" });
        
        var tile = Assert.IsType<TextTile>(legacy.CreateTile("one"));
        Assert.True(tile.IsHeaderVisible);
        Assert.True(tile.IsResizable);
        
        var panel = new DashboardPanel();
        
        panel.Children.Add(tile);
        
        var bad = DashboardContentCatalog.FromTiles(new[] { tile }, _ => "bad", _ => "Bad", t => t);
        
        Assert.Throws<InvalidOperationException>(() => bad.CreateTile("bad"));
    }

    [AvaloniaFact]
    public void ItemDashboardRetainsOnDetachAndDoesNotOwnBorrowedControls()
    {
        var borrowed = new DisposableBody { DataContext = "host" };
        using var panel = new DashboardItemsPanel { DisposeRemovedTiles = true, ItemsSource = new ObservableCollection<Control> { borrowed } };
        var window = Show(panel);
        window.Content = null;
        
        Assert.Contains(borrowed, panel.Children);
        Assert.Equal(0, borrowed.Count);
        
        window.Content = panel;
        window.UpdateLayout();
        
        Assert.Equal("host", borrowed.DataContext);
        
        panel.Dispose();
        
        Assert.Equal(0, borrowed.Count);
        
        window.Close();
    }

    [AvaloniaFact]
    public void ItemDashboardDisposesRemovedGeneratedBodiesAndPrunesPlacementCache()
    {
        var source = new ObservableCollection<object> { new object(), new object() };
        using var panel = new DashboardItemsPanel { DisposeRemovedTiles = true, ItemsSource = source, ItemTemplate = new FuncDataTemplate<object>((_, _) => new DisposableBody()) };
        var window = Show(panel);
        try
        {
            var body = panel.Children.OfType<DisposableBody>().First();
            
            source.RemoveAt(0);
            
            Assert.Equal(1, body.Count);
            
            var cache = (IDictionary) typeof(DashboardPanel).GetField("_lastValid", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(panel)!;
            
            Assert.False(cache.Contains(body));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MatrixInsertionKeepsHostedControlsAttachedAndWeightUpdatesShareCells()
    {
        var body = new TrackingBody();
        using var matrix = new DashboardMatrix { ContentDefinitions = new[] { Definition("a", () => body) } };
        
        matrix.SetContent(matrix.Layout.Cells[0].Id, "a");
        
        var window = Show(matrix);
        try
        {
            matrix.InsertRowAfter(0);
            matrix.InsertColumnBefore(0);
            window.UpdateLayout();
            
            Assert.Equal(0, body.Detaches);
            Assert.Same(matrix.Layout.Cells, matrix.Layout.WithWeights(new[] { 2d, 3d }, new[] { 4d, 1d }).Cells);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void PlacementApiVetoesAndPreservesTwoWayPositionBinding()
    {
        var vm = new Position();
        var tile = new Tile { DataContext = vm };
        
        tile.Bind(Tile.GridXProperty, new Binding(nameof(Position.X)) { Mode = BindingMode.TwoWay });
        
        var panel = new DashboardPanel { Columns = 3 };
        
        panel.Children.Add(tile);
        
        var window = Show(panel);
        try
        {
            Assert.True(panel.TrySetPlacement(tile, 1, 0, 1, 1));
            Assert.Equal(1, vm.X);
            
            panel.PlacementChanging += (_, e) => e.Cancel = true;
            
            Assert.False(panel.TrySetPlacement(tile, 2, 0, 1, 1));
            Assert.Equal(1, vm.X);
            
            vm.X = 2;
            
            Assert.Equal(2, tile.GridX);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void NestedTileHeaderCannotDragOuterTile()
    {
        var inner = new Tile { TileHeader = "inner" };
        var outer = new Tile { Content = inner };
        var panel = new DashboardPanel { Rows = 1, Columns = 2 };
        
        panel.Children.Add(outer);
        
        var window = Show(panel);
        try
        {
            var header = inner.GetVisualDescendants().OfType<DashboardHeader>().Single();
            var start = header.TranslatePoint(new Point(25, 15), window)!.Value;
            
            window.MouseDown(start, MouseButton.Left);
            window.MouseMove(start + new Vector(300, 0));
            window.MouseUp(start + new Vector(300, 0), MouseButton.Left);
            
            Assert.Equal(0, DashboardPanel.GetX(outer));
            Assert.Equal(0, outer.GridX);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void HiddenMatrixHeadersKeepKeyboardAndPointerActionAccess()
    {
        using var matrix = new DashboardMatrix { IsHeaderVisible = false };
        var window = Show(matrix);
        try
        {
            var cell = matrix.GetVisualDescendants().OfType<Tile>().Single();
            
            Assert.False(cell.GetVisualDescendants().OfType<DashboardHeader>().Single().IsVisible);
            
            var button = cell.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_OverlayActions");
            
            Assert.False(button.IsVisible);
            
            var boundary = matrix.GetVisualDescendants().OfType<Button>()
                                 .First(b => b.Classes.Contains("matrix-boundary-action"));
            
            Assert.True(boundary.IsVisible);
            Assert.False(boundary.IsHitTestVisible);
            
            boundary.Focus(NavigationMethod.Tab);
            
            Assert.Equal(1, boundary.Opacity);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ChangingTemplatePreservesBorrowedControl()
    {
        var borrowed = new TextBox { Text = "keep me" };
        using var panel = new DashboardItemsPanel { ItemsSource = new[] { borrowed } };
        var window = Show(panel);
        try
        {
            panel.ItemTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock());
            
            Assert.Same(borrowed, Assert.Single(panel.Children.OfType<TextBox>()));
            Assert.Equal("keep me", borrowed.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void NarrowHeaderTrimsTitleAndPreservesCustomContent()
    {
        var header = new DashboardHeader { Title = new string('W', 100), Width = 160, Actions = new MenuFlyout() };
        var window = Show(header);
        try
        {
            var title = header.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Title");
            var button = header.GetVisualDescendants().OfType<Button>().Single();
            
            Assert.Equal(TextTrimming.CharacterEllipsis, title.TextTrimming);
            Assert.True(title.Bounds.Width < 160);
            Assert.Equal(28, button.Bounds.Width);
            
            var custom = new TextBox { Text = "custom" };
            header.Content = custom;
            window.UpdateLayout();
            
            Assert.False(title.IsVisible);
            Assert.Contains(custom, header.GetVisualDescendants());
            
            header.Content = null;
            window.UpdateLayout();
            
            Assert.True(title.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void InvisibleHeaderActionAcceptsDirectPointerPressWithoutHover()
    {
        var menu = new MenuFlyout();
        
        menu.Items.Add(new MenuItem { Header = "Choose content" });
        
        var header = new DashboardHeader { Title = "Title", Actions = menu, Width = 200, Height = 32 };
        var window = Show(header);
        try
        {
            var button = header.GetVisualDescendants().OfType<Button>().Single();
            
            Assert.Equal(0, button.Opacity);
            
            var point = button.TranslatePoint(new Point(14, 14), window)!.Value;
            
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            
            Assert.True(menu.IsOpen);
            Assert.Equal(1, button.Opacity);
            
            menu.Hide();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ChromeResourcesUpdateBothHostsWithoutReplacingContent()
    {
        using var tile = new DashboardContentTile();
        using var matrix = new DashboardMatrix();
        var window = Show(new StackPanel { Children = { tile, matrix } });
        try
        {
            window.Resources["DashboardCellCornerRadius"] = new CornerRadius(9);
            window.Resources["DashboardContentPadding"] = new Thickness(12);
            window.Resources["DashboardActionSize"] = 36d;
            window.UpdateLayout();
            
            var cell = matrix.GetVisualDescendants().OfType<Tile>().Single();
            
            Assert.Equal(new CornerRadius(9), tile.CornerRadius);
            Assert.Equal(tile.CornerRadius, cell.CornerRadius);
            Assert.Equal(new Thickness(12), tile.Padding);
            Assert.Equal(tile.Padding, cell.Padding);
            Assert.All(window.GetVisualDescendants().OfType<Button>().Where(b => b.Name == "PART_Actions"),
                       b => Assert.Equal(36, b.Width));
        }
        finally
        {
            window.Close();
        }
    }

    private static DashboardContentDefinition Definition(string id, Func<Control> factory) => new DashboardContentDefinition { Id = id, Title = id, Factory = factory };

    private static Window Show(Control control)
    {
        var window = new Window { Width = 700, Height = 500, Content = control };
        
        window.Show();
        window.UpdateLayout();
        
        return window;
    }

    private sealed class DisposableBody : Control, IDisposable
    {
        public int Count { get; private set; }

        public void Dispose() => Count++;
    }

    private sealed class TrackingBody : Control
    {
        public int Detaches { get; private set; }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            Detaches++;
            base.OnDetachedFromVisualTree(e);
        }
    }

    private sealed class Assignment : INotifyPropertyChanged
    {
        private string? _id;

        public string? Id
        {
            get => _id;
            set
            {
                _id = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Id)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class Position : INotifyPropertyChanged
    {
        private int _x;

        public int X
        {
            get => _x;
            set
            {
                _x = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(X)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
