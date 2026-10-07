using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MiniDashboard.Avalonia.Grid;
using MiniDashboard.Avalonia.Tiles;
using Xunit;

namespace MiniDashboard.Avalonia.Tests;

public class TileGridDragRecoveryTests
{
    [AvaloniaFact]
    public void LeavingAndReenteringGridContinuesSameDrag()
    {
        using var host = new Host();

        host.Start();
        host.Window.MouseMove(host.Outside);

        Assert.False(host.Tile.IsPlacementValid);

        host.Window.MouseMove(host.Target);

        Assert.True(host.Tile.IsPlacementValid);

        host.Window.MouseUp(host.Target, MouseButton.Left);
        host.Window.UpdateLayout();

        Assert.Equal(1, host.Tile.GridX);
        Assert.True(host.Tile.IsPlacementValid);
    }

    [AvaloniaFact]
    public void ReleasingOutsideKeepsOriginalPositionAndClearsInvalidTint()
    {
        using var host = new Host();

        host.Start();
        host.Window.MouseMove(host.Outside);

        Assert.False(host.Tile.IsPlacementValid);

        host.Window.MouseUp(host.Outside, MouseButton.Left);

        Assert.True(host.Tile.IsPlacementValid);
        Assert.Equal(0, host.Tile.GridX);

        host.Start();
        host.Window.MouseMove(host.Target);
        host.Window.MouseUp(host.Target, MouseButton.Left);

        Assert.Equal(1, host.Tile.GridX);
    }

    [AvaloniaFact]
    public void LostCaptureCancelsDragAndClearsInvalidTint()
    {
        using var host = new Host();
        IPointer? pointer = null;

        host.Panel.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer,
                              RoutingStrategies.Tunnel, true);
        host.Start();
        host.Window.MouseMove(host.Outside);

        Assert.False(host.Tile.IsPlacementValid);

        pointer!.Capture(null);

        Assert.True(host.Tile.IsPlacementValid);

        host.Window.MouseMove(host.Target);
        host.Window.MouseUp(host.Target, MouseButton.Left);

        Assert.Equal(0, host.Tile.GridX);
    }

    [AvaloniaFact]
    public void HostVetoAfterReentryLeavesTileNeutralAtOriginalPosition()
    {
        using var host = new Host();
        host.Panel.PlacementChanging += (_, e) => e.Cancel = true;
        host.Start();
        host.Window.MouseMove(host.Outside);
        host.Window.MouseMove(host.Target);
        host.Window.MouseUp(host.Target, MouseButton.Left);

        Assert.Equal(0, host.Tile.GridX);
        Assert.True(host.Tile.IsPlacementValid);
    }

    private sealed class Host : IDisposable
    {
        public readonly Tile Tile = new Tile { TileHeader = "Drag me" };
        public readonly DashboardPanel Panel;
        public readonly Window Window;

        public Point StartPoint { get; }

        public Point Target => StartPoint + new Vector(200, 0);

        public Point Outside => new Point(700, 100);

        public Host()
        {
            Panel = new DashboardPanel { Rows = 2, Columns = 3, Margin = new Thickness(60), Children = { Tile } };
            Window = new Window { Width = 720, Height = 480, Content = Panel };
            Window.Show();
            Window.UpdateLayout();

            var header = Tile.GetVisualDescendants().OfType<DashboardHeader>().Single();
            StartPoint = header.TranslatePoint(new Point(35, 15), Window)!.Value;
        }

        public void Start() => Window.MouseDown(StartPoint, MouseButton.Left);

        public void Dispose() => Window.Close();
    }
}
