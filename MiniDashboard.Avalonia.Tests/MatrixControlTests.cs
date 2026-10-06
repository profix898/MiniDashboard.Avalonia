using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace MiniDashboard.Avalonia.Tests;

public class MatrixControlTests
{
    [AvaloniaFact]
    public void NativeGridContainsGlobalSplittersAndMinimumDimensions()
    {
        using var host = new Host(2, 3);
        
        Assert.Equal(3, host.Grid.RowDefinitions.Count);
        Assert.Equal(5, host.Grid.ColumnDefinitions.Count);
        Assert.Equal(6, host.Grid.Children.OfType<Tile>().Count());
        
        var splitters = host.Grid.Children.OfType<GridSplitter>().ToArray();
        
        Assert.Equal(3, splitters.Length);
        Assert.All(splitters, s =>
        {
            Assert.Equal(GridResizeBehavior.PreviousAndNext, s.ResizeBehavior);
            Assert.True(s.Focusable);
            Assert.NotEmpty(AutomationProperties.GetName(s)!);
            Assert.Equal(8d, s.ResizeDirection == GridResizeDirection.Rows ? s.Bounds.Height : s.Bounds.Width);
        });
        Assert.Equal(6d, host.Grid.ColumnDefinitions[1].ActualWidth);
        Assert.Equal(96d, host.Grid.ColumnDefinitions[0].MinWidth);
        Assert.Equal(72d, host.Grid.RowDefinitions[0].MinHeight);
        Assert.All(splitters.Where(s => s.ResizeDirection == GridResizeDirection.Columns),
                   s => Assert.Equal(3, Grid.GetRowSpan(s)));
        Assert.All(host.Grid.Children.OfType<Tile>(), c =>
        {
            Assert.Equal(0, Grid.GetRow(c) % 2);
            Assert.Equal(0, Grid.GetColumn(c) % 2);
        });
    }

    [AvaloniaFact]
    public void ExpandedHitTargetResizesAndUpdatesPersistentWeights()
    {
        using var host = new Host(1, 2);
        var splitter = host.Grid.Children.OfType<GridSplitter>().Single();
        var start = splitter.TranslatePoint(new Point(3, 100), host.Window)!.Value;
        
        host.Window.MouseMove(start);
        
        var hit = host.Window.InputHitTest(start) as Visual;
        
        Assert.True(hit is not null && (ReferenceEquals(hit, splitter) || hit.GetVisualAncestors().Contains(splitter)),
                    $"Hit={hit}; start={start}; splitter={splitter.Bounds}; grid={host.Grid.Bounds}; window={host.Window.Bounds}; content={host.Matrix.Bounds}");
        
        var before = host.Grid.ColumnDefinitions[0].ActualWidth;
        
        host.Window.MouseDown(start, MouseButton.Left);
        host.Window.MouseMove(start + new Vector(50, 0));
        host.Window.MouseUp(start + new Vector(50, 0), MouseButton.Left);
        host.Window.UpdateLayout();
        
        var snapshot = host.Matrix.GetSnapshot();
        
        Assert.True(host.Grid.ColumnDefinitions[0].ActualWidth > before);
        Assert.True(snapshot.Columns[0] > snapshot.Columns[1]);
        
        var oldWeight = snapshot.Columns[0];
        
        host.Matrix.InsertColumnAfter(0);
        
        Assert.Equal(oldWeight / 2, host.Matrix.Layout.Columns[0]);
        Assert.Equal(oldWeight / 2, host.Matrix.Layout.Columns[1]);
    }

    [AvaloniaFact]
    public void KeyboardResizingAndProportionVetoWork()
    {
        using var host = new Host(2, 2);
        var splitter = host.Grid.Children.OfType<GridSplitter>().First(s => s.ResizeDirection == GridResizeDirection.Rows);
        
        splitter.Focus(NavigationMethod.Tab);
        host.Window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        host.Window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        host.Window.UpdateLayout();
        
        var resized = host.Matrix.GetSnapshot();
        
        Assert.True(resized.Rows[0] > resized.Rows[1]);
        
        host.Matrix.LayoutChanging += (_, e) => e.Cancel = e.Kind == DashboardMatrixChangeKind.Proportions;
        host.Window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        host.Window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        host.Matrix.GetSnapshot();
        
        Assert.Same(resized, host.Matrix.Layout);
        Assert.Equal(resized.Rows[0], host.Grid.RowDefinitions[0].Height.Value);
    }

    [AvaloniaFact]
    public void RemovalVetoHappensBeforeFactoriesOrDisposal()
    {
        using var host = new Host(2, 2);
        var created = new List<DisposableControl>();
        host.Matrix.ContentDefinitions = Catalog(created);
        host.Matrix.DisposeRemovedContent = true;
        
        var id = host.Matrix.Layout.Cells[0].Id;
        
        host.Matrix.SetContent(id, "a");
        
        var before = host.Matrix.Layout;
        var changed = 0;
        host.Matrix.LayoutChanged += (_, _) => changed++;
        host.Matrix.LayoutChanging += (_, e) =>
        {
            Assert.Same(before, e.Before);
            Assert.Contains(e.AffectedCells, c => c.Id == id && c.ContentId == "a");
            
            e.Cancel = true;
        };
        
        Assert.False(host.Matrix.RemoveRow(0));
        Assert.Same(before, host.Matrix.Layout);
        Assert.Equal(0, changed);
        Assert.Equal(0, created[0].DisposeCount);
        Assert.Single(created);
    }

    [AvaloniaFact]
    public void StructuralEditsAndSwapsRetainLiveInstances()
    {
        using var host = new Host(2, 2);
        var created = new List<DisposableControl>();
        host.Matrix.ContentDefinitions = Catalog(created);
        host.Matrix.DisposeRemovedContent = true;
        
        var a = host.Matrix.Layout.Cells[0].Id;
        var b = host.Matrix.Layout.Cells[3].Id;
        
        host.Matrix.SetContent(a, "a");
        host.Matrix.SetContent(b, "b");
        
        var first = Cell(host, a).Content;
        var second = Cell(host, b).Content;
        
        host.Matrix.InsertColumnBefore(0);
        
        Assert.Same(first, Cell(host, a).Content);
        
        host.Matrix.SwapContent(a, b);
        
        Assert.Same(first, Cell(host, b).Content);
        Assert.Same(second, Cell(host, a).Content);
        Assert.Equal(2, created.Count);
        Assert.All(created, c => Assert.Equal(0, c.DisposeCount));
        
        host.Matrix.ClearContent(a);
        
        Assert.Equal(1, ((DisposableControl) second!).DisposeCount);
        
        host.Matrix.RemoveRow(1);
        
        Assert.Equal(1, ((DisposableControl) first!).DisposeCount);
    }

    [AvaloniaFact]
    public void FactoryFailuresAndReusedControlsDoNotReplaceAssignments()
    {
        using var host = new Host(1, 2);
        var control = new DisposableControl();
        host.Matrix.ContentDefinitions = new[] { Definition("a", () => control), Definition("bad", () => throw new InvalidOperationException("factory failed")) };
        
        var id = host.Matrix.Layout.Cells[0].Id;
        
        host.Matrix.SetContent(id, "a");
        
        var before = host.Matrix.Layout;
        
        Assert.Throws<InvalidOperationException>(() => host.Matrix.SetContent(id, "bad"));
        Assert.Same(before, host.Matrix.Layout);
        Assert.Same(control, Cell(host, id).Content);
        Assert.Throws<InvalidOperationException>(() => host.Matrix.SetContent(host.Matrix.Layout.Cells[1].Id, "a"));
        Assert.Same(before, host.Matrix.Layout);
        Assert.Equal(0, control.DisposeCount);
    }

    [AvaloniaFact]
    public void FailedRestoreDisposesOnlyNewUncommittedInstances()
    {
        using var host = new Host(1, 2);
        var created = new List<DisposableControl>();
        host.Matrix.DisposeRemovedContent = true;
        host.Matrix.ContentDefinitions = Catalog(created).Append(Definition("bad", () => throw new Exception("failed"))).ToArray();
        
        var before = host.Matrix.Layout;
        var next = before.WithContent(before.Cells[0].Id, "a").WithContent(before.Cells[1].Id, "bad");
        
        Assert.Throws<Exception>(() => host.Matrix.RestoreLayout(next));
        Assert.Same(before, host.Matrix.Layout);
        Assert.Equal(1, Assert.Single(created).DisposeCount);
    }

    [AvaloniaFact]
    public void UnknownIdsRecoverWhenObservableCatalogChanges()
    {
        using var host = new Host(1, 1);
        var definitions = new ObservableCollection<DashboardContentDefinition>();
        host.Matrix.ContentDefinitions = definitions;
        
        var id = host.Matrix.Layout.Cells[0].Id;
        
        host.Matrix.SetContent(id, "late");
        
        Assert.IsType<TextBlock>(Cell(host, id).Content);
        
        var control = new DisposableControl();
        
        definitions.Add(Definition("late", () => control));
        
        Assert.Same(control, Cell(host, id).Content);
        Assert.Equal("late", host.Matrix.Layout.GetCell(id).ContentId);
        
        definitions.Clear();
        
        Assert.IsType<TextBlock>(Cell(host, id).Content);
        Assert.Equal(0, control.DisposeCount); // opt-in ownership
    }

    [AvaloniaFact]
    public void DetachAndRetemplateRetainContentAndReleaseCatalogSubscription()
    {
        using var host = new Host(1, 1);
        var definitions = new ObservableCollection<DashboardContentDefinition>();
        var control = new DisposableControl();
        
        definitions.Add(Definition("a", () => control));
        host.Matrix.ContentDefinitions = definitions;
        host.Matrix.DisposeRemovedContent = true;
        
        var id = host.Matrix.Layout.Cells[0].Id;
        
        host.Matrix.SetContent(id, "a");
        host.Window.Content = null;
        
        Assert.Equal(0, control.DisposeCount);
        
        host.Window.Content = host.Matrix;
        host.Window.UpdateLayout();
        
        Assert.Same(control, Cell(host, id).Content);
        
        host.Matrix.Template = new FuncControlTemplate<DashboardMatrix>((_, scope) =>
        {
            var grid = new Grid { Name = "PART_Grid" };
            
            scope.Register("PART_Grid", grid);
            
            return grid;
        });
        host.Matrix.ApplyTemplate();
        host.Window.UpdateLayout();
        
        Assert.Same(control, Cell(host, id).Content);
        
        host.Matrix.Dispose();
        
        Assert.Equal(1, control.DisposeCount);
        
        definitions.Add(Definition("b", () => throw new Exception("disposed matrix must not subscribe")));
    }

    [AvaloniaFact]
    public void CapabilityFlagsAndMoveConstraintsAreEnforced()
    {
        using var host = new Host(1, 2);
        host.Matrix.CanModifyStructure = false;
        
        Assert.False(host.Matrix.InsertRowAfter(0));
        
        host.Matrix.CanResize = false;
        
        Assert.All(host.Grid.Children.OfType<GridSplitter>(), s => Assert.False(s.IsHitTestVisible));
        
        var ids = host.Matrix.Layout.Cells.Select(c => c.Id).ToArray();
        
        host.Matrix.SetContent(ids[0], "unknown");
        host.Matrix.MoveContent(ids[0], ids[1]);
        
        Assert.Null(host.Matrix.Layout.GetCell(ids[0]).ContentId);
        Assert.Equal("unknown", host.Matrix.Layout.GetCell(ids[1]).ContentId);
        Assert.Throws<InvalidOperationException>(() => host.Matrix.MoveContent(ids[0], ids[1]));
    }

    [AvaloniaFact]
    public void ActionsAreFocusableAndDoNotReplaceHostedContextMenu()
    {
        using var host = new Host(1, 1);
        var menu = new ContextMenu();
        var editor = new TextBox { ContextMenu = menu };
        host.Matrix.ContentDefinitions = new[] { Definition("editor", () => editor) };
        host.Matrix.SetContent(host.Matrix.Layout.Cells[0].Id, "editor");
        host.Window.UpdateLayout();
        
        var cell = host.Grid.Children.OfType<Tile>().Single();
        var button = cell.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_Actions");
        
        Assert.True(button.IsVisible);
        Assert.True(button.Focusable);
        Assert.True(button.Bounds.Width >= 28);
        Assert.Equal(0, button.Opacity);
        
        button.Focus();
        
        Assert.True(button.Opacity > 0);
        Assert.Null(cell.ContextMenu);
        Assert.Same(menu, editor.ContextMenu);
        
        button.Focus();
        host.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        host.Window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void SplitterIntersectionHasDeterministicHitTargets()
    {
        using var host = new Host(2, 2);
        var column = host.Grid.Children.OfType<GridSplitter>().First(s => s.ResizeDirection == GridResizeDirection.Columns);
        var row = host.Grid.Children.OfType<GridSplitter>().First(s => s.ResizeDirection == GridResizeDirection.Rows);
        var crossing = host.Grid.TranslatePoint(new Point(column.Bounds.Center.X, row.Bounds.Center.Y), host.Window)!.Value;
        
        host.Window.MouseMove(crossing);
        
        Assert.True(IsHit(host.Window.InputHitTest(crossing), row));
        Assert.True(IsHit(host.Window.InputHitTest(crossing + new Vector(0, 30)), column));
        Assert.True(IsHit(host.Window.InputHitTest(crossing + new Vector(30, 0)), row));

        static bool IsHit(IInputElement? hit, GridSplitter expected) => hit is Visual visual && (ReferenceEquals(hit, expected) || visual.GetVisualAncestors().Contains(expected));
    }

    [AvaloniaFact]
    public void PointerResizeRespectsMinimumSizeAndDisabledInput()
    {
        using var host = new Host(1, 2);
        var splitter = host.Grid.Children.OfType<GridSplitter>().Single();
        var start = splitter.TranslatePoint(new Point(4, 80), host.Window)!.Value;
        
        host.Window.MouseDown(start, MouseButton.Left);
        host.Window.MouseMove(start + new Vector(1000, 0));
        host.Window.MouseUp(start + new Vector(1000, 0), MouseButton.Left);
        host.Window.UpdateLayout();
        
        Assert.True(host.Grid.ColumnDefinitions[2].ActualWidth >= host.Matrix.MinCellWidth);
        
        var before = host.Matrix.GetSnapshot();
        host.Matrix.CanResize = false;
        splitter = host.Grid.Children.OfType<GridSplitter>().Single();
        start = splitter.TranslatePoint(new Point(4, 80), host.Window)!.Value;
        host.Window.MouseDown(start, MouseButton.Left);
        host.Window.MouseMove(start - new Vector(100, 0));
        host.Window.MouseUp(start - new Vector(100, 0), MouseButton.Left);
        
        Assert.Equal(before.Columns, host.Matrix.GetSnapshot().Columns);
    }

    [AvaloniaFact]
    public void ReplacementInAnotherCellPreservesEditorFocus()
    {
        using var host = new Host(1, 2);
        var editor = new TextBox { Text = "unsaved" };
        host.Matrix.ContentDefinitions = new[] { Definition("edit", () => editor), Definition("other", () => new TextBlock()) };
        
        var first = host.Matrix.Layout.Cells[0].Id;
        
        host.Matrix.SetContent(first, "edit");
        host.Window.UpdateLayout();
        editor.Focus();
        host.Matrix.SetContent(host.Matrix.Layout.Cells[1].Id, "other");
        
        Assert.True(editor.IsFocused);
        Assert.Equal("unsaved", editor.Text);
    }

    [AvaloniaFact]
    public void DisposalFailuresStillCommitAndAttemptEveryDiscardedControl()
    {
        using var host = new Host(2, 2);
        var count = 0;
        host.Matrix.DisposeRemovedContent = true;
        host.Matrix.ContentDefinitions = new[] { Definition("throws", () => new ThrowingDisposable(() => count++)) };
        foreach (var cell in host.Matrix.Layout.Cells.Where(c => c.Row == 0))
            host.Matrix.SetContent(cell.Id, "throws");
        
        var notified = false;
        host.Matrix.LayoutChanged += (_, e) => notified = e.Kind == DashboardMatrixChangeKind.RemoveRow;
        
        var error = Assert.Throws<AggregateException>(() => host.Matrix.RemoveRow(0));
        Assert.Equal(2, error.InnerExceptions.Count);
        Assert.Equal(2, count);
        Assert.True(notified);
        Assert.Single(host.Matrix.Layout.Rows);
        Assert.Equal(2, host.Grid.Children.OfType<Tile>().Count());
    }

    [AvaloniaFact]
    public void CancelledReplacementAndNestedMutationLeaveContentUntouched()
    {
        using var host = new Host(1, 1);
        var calls = 0;
        host.Matrix.ContentDefinitions = new[]
        {
            Definition("a", () =>
            {
                calls++;
                
                return new TextBlock();
            })
        };
        host.Matrix.LayoutChanging += (_, e) =>
        {
            Assert.Throws<InvalidOperationException>(() => host.Matrix.InsertRowAfter(0));
            Assert.Throws<InvalidOperationException>(() => host.Matrix.Dispose());
            
            e.Cancel = true;
        };
        
        Assert.False(host.Matrix.SetContent(host.Matrix.Layout.Cells[0].Id, "a"));
        Assert.Equal(0, calls);
        Assert.Null(host.Matrix.Layout.Cells[0].ContentId);
    }

    [AvaloniaFact]
    public void DetachingUnsubscribesUntilReattachedAndDisposeIsIdempotent()
    {
        using var host = new Host(1, 1);
        var catalog = new ObservableCollection<DashboardContentDefinition>();
        host.Matrix.ContentDefinitions = catalog;
        host.Matrix.SetContent(host.Matrix.Layout.Cells[0].Id, "a");
        host.Matrix.DisposeRemovedContent = true;
        
        var created = 0;
        var control = new DisposableControl();
        host.Window.Content = null;
        catalog.Add(Definition("a", () =>
        {
            created++;
            
            return control;
        }));
        
        Assert.Equal(0, created);
        
        host.Window.Content = host.Matrix;
        host.Window.UpdateLayout();
        
        Assert.Equal(1, created);
        
        host.Matrix.Dispose();
        host.Matrix.Dispose();
        
        Assert.Equal(1, control.DisposeCount);
    }

    private sealed class ThrowingDisposable : Control, IDisposable
    {
        private readonly Action _onDispose;

        public ThrowingDisposable(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose();
            throw new InvalidOperationException("test disposal");
        }
    }

    [AvaloniaFact]
    public void EscapeDuringResizeRestoresAllColumnProportions()
    {
        using var host = new Host(1, 3);
        var before = host.Matrix.GetSnapshot();
        var splitter = host.Grid.Children.OfType<GridSplitter>().First();
        var start = splitter.TranslatePoint(new Point(4, 80), host.Window)!.Value;
        
        host.Window.MouseDown(start, MouseButton.Left);
        host.Window.MouseMove(start + new Vector(50, 0));
        host.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        host.Window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        host.Window.MouseUp(start + new Vector(50, 0), MouseButton.Left);
        
        var after = host.Matrix.GetSnapshot();
        var beforeTotal = before.Columns.Sum();
        var afterTotal = after.Columns.Sum();
        for (var i = 0; i < before.Columns.Count; i++)
            Assert.Equal(before.Columns[i] / beforeTotal, after.Columns[i] / afterTotal, 5);
    }

    [AvaloniaFact]
    public void SplitterKeyboardFocusHasAVisibleOutline()
    {
        using var host = new Host(1, 2);
        var splitter = host.Grid.Children.OfType<GridSplitter>().Single();
        
        splitter.Focus(NavigationMethod.Tab);
        host.Window.UpdateLayout();
        
        var target = splitter.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_HitTarget");
        
        Assert.Equal(new Thickness(1), target.BorderThickness);
        Assert.NotNull(target.BorderBrush);
    }

    [AvaloniaFact]
    public void LosingFocusDuringResizeRestoresEveryRow()
    {
        using var host = new Host(3, 1);
        var before = host.Matrix.GetSnapshot();
        var splitter = host.Grid.Children.OfType<GridSplitter>().First();
        var start = splitter.TranslatePoint(new Point(100, 4), host.Window)!.Value;
        
        host.Window.MouseDown(start, MouseButton.Left);
        host.Window.MouseMove(start + new Vector(0, 30));
        
        var button = host.Matrix.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_Actions");
        
        button.Focus();
        host.Window.MouseUp(start + new Vector(0, 30), MouseButton.Left);
        
        Assert.Equal(before.Rows, host.Matrix.GetSnapshot().Rows);
    }

    private static Tile Cell(Host host, Guid id)
    {
        var model = host.Matrix.Layout.GetCell(id);
        
        return host.Matrix.GetVisualDescendants().OfType<Tile>()
                   .Single(c => Grid.GetRow(c) == model.Row * 2 && Grid.GetColumn(c) == model.Column * 2);
    }

    private static DashboardContentDefinition Definition(string id, Func<Control> factory) => new DashboardContentDefinition { Id = id, Title = id, Factory = factory };

    private static DashboardContentDefinition[] Catalog(List<DisposableControl> created)
    {
        Control Create()
        {
            var control = new DisposableControl();
            
            created.Add(control);
            
            return control;
        }

        return new[] { Definition("a", Create), Definition("b", Create) };
    }

    private sealed class DisposableControl : Control, IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;
    }

    private sealed class Host : IDisposable
    {
        public Host(int rows, int columns)
        {
            Matrix = new DashboardMatrix();
            Matrix.RestoreLayout(new DashboardMatrixLayout(Enumerable.Repeat(1d, rows).ToArray(),
                                                           Enumerable.Repeat(1d, columns).ToArray(), null));
            Window = new Window { Width = 800, Height = 500, Content = Matrix };
            Window.Show();
            Window.UpdateLayout();
        }

        public DashboardMatrix Matrix { get; }

        public Window Window { get; }

        public Grid Grid => Matrix.GetVisualDescendants().OfType<Grid>().First(g => g.Name == "PART_Grid");

        public void Dispose()
        {
            Window.Close();
            Matrix.Dispose();
        }
    }
}
