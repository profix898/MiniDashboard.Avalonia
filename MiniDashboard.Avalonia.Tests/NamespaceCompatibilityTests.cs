using System;
using MiniDashboard.Avalonia.Content;
using MiniDashboard.Avalonia.Grid;
using MiniDashboard.Avalonia.Matrix;
using MiniDashboard.Avalonia.Picking;
using MiniDashboard.Avalonia.Tiles;
using Xunit;

namespace MiniDashboard.Avalonia.Tests;

public class NamespaceCompatibilityTests
{
    [Theory]
    [InlineData(typeof(DashboardPanel), "Grid")]
    [InlineData(typeof(DashboardItemsPanel), "Grid")]
    [InlineData(typeof(DashboardMatrix), "Matrix")]
    [InlineData(typeof(Tile), "Tiles")]
    [InlineData(typeof(DashboardContentTile), "Tiles")]
    [InlineData(typeof(DashboardHeader), "Tiles")]
    [InlineData(typeof(TextTile), "Tiles")]
    [InlineData(typeof(ImageTile), "Tiles")]
    [InlineData(typeof(TableViewTile), "Tiles")]
    public void DashboardAndTileControlsUseTheirOwningNamespace(Type control, string area)
    {
        Assert.Equal($"MiniDashboard.Avalonia.{area}", control.Namespace);
    }

    [Theory]
    [InlineData(typeof(DashboardPlacement), "Grid")]
    [InlineData(typeof(DashboardPlacementChangingEventArgs), "Grid")]
    [InlineData(typeof(DraggableBehavior), "Grid")]
    [InlineData(typeof(DashboardMatrixLayout), "Matrix")]
    [InlineData(typeof(DashboardMatrixCellModel), "Matrix")]
    [InlineData(typeof(DashboardMatrixChangeEventArgs), "Matrix")]
    [InlineData(typeof(DashboardContentCatalog), "Content")]
    [InlineData(typeof(DashboardContentDefinition), "Content")]
    [InlineData(typeof(DashboardContentCreationContext), "Content")]
    [InlineData(typeof(DashboardContentPicker), "Content")]
    [InlineData(typeof(IDashboardContentPicker), "Content")]
    [InlineData(typeof(IDashboardTilePicker), "Picking")]
    public void CompanionTypesUseTheirOwningNamespace(Type companion, string area)
    {
        Assert.Equal($"MiniDashboard.Avalonia.{area}", companion.Namespace);
    }
}
