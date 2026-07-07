# MiniDashboard.Avalonia

[![NuGet](https://img.shields.io/nuget/v/MiniDashboard.Avalonia?style=flat-square&logo=nuget&color=blue)](https://www.nuget.org/packages/MiniDashboard.Avalonia)

**MiniDashboard.Avalonia** provides dashboard controls for **Avalonia 12** applications. It includes a fixed-grid dashboard panel, draggable and resizable tiles, collision-aware placement, and live snap feedback while moving or resizing tiles.

## Packages

- `MiniDashboard.Avalonia` - core dashboard panel and tile controls.
- `MiniDashboard.Avalonia.ScottPlot` - optional ScottPlot chart tiles.
- `MiniDashboard.Avalonia.TreeDataGrid` - optional TreeDataGrid tiles.

The projects target `net10.0`, matching the recommended target for Avalonia 12.

## Features

- **DashboardPanel**: fixed `Rows` x `Columns` grid, attached layout properties, snap preview, and collision-aware move/resize resolution.
- **Tile**: base content tile with grid position/size properties, custom header content, optional resize grip, styling properties, and placement-valid feedback.
- **TextTile**: simple text display tile.
- **ImageTile**: image display tile using `ImageSource` or `SourceUri`.
- **ScottPlot tiles**: `PlotTile`, cartesian chart tiles, and pie chart tiles.
- **TreeDataGrid tiles**: `TreeDataGridTile` and `CsvGridTile`.

## Install

Install the core package first:

```bash
dotnet add package MiniDashboard.Avalonia
```

Install optional extension packages only when needed:

```bash
dotnet add package MiniDashboard.Avalonia.ScottPlot
dotnet add package MiniDashboard.Avalonia.TreeDataGrid
```

`MiniDashboard.Avalonia.TreeDataGrid` depends on Avalonia's TreeDataGrid package. Avalonia 12 TreeDataGrid package usage may require an Avalonia UI license in consuming applications.

## Register Styles

Add the style include for each package you use in `App.axaml`:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:dashboard="clr-namespace:MiniDashboard.Avalonia.Themes;assembly=MiniDashboard.Avalonia"
             xmlns:dashboardScottPlot="clr-namespace:MiniDashboard.Avalonia.ScottPlot.Themes;assembly=MiniDashboard.Avalonia.ScottPlot"
             xmlns:dashboardGrid="clr-namespace:MiniDashboard.Avalonia.TreeDataGrid.Themes;assembly=MiniDashboard.Avalonia.TreeDataGrid">
  <Application.Styles>
    <FluentTheme />
    <dashboard:MiniDashboardStyles />

    <!-- Optional extension styles -->
    <dashboardScottPlot:ScottPlotStyles />
    <dashboardGrid:TreeDataGridStyles />
  </Application.Styles>
</Application>
```

## Basic Usage

```xml
<dash:DashboardPanel xmlns:dash="clr-namespace:MiniDashboard.Avalonia;assembly=MiniDashboard.Avalonia"
                     Rows="6"
                     Columns="8">
  <dash:TextTile GridX="0" GridY="0" GridW="2" GridH="2"
                 TileHeader="Notes"
                 Text="Drag me" />

  <dash:Tile GridX="2" GridY="0" GridW="3" GridH="2"
             TileHeader="Status">
    <StackPanel>
      <TextBlock Text="CPU" />
      <ProgressBar Minimum="0" Maximum="100" Value="57" />
    </StackPanel>
  </dash:Tile>
</dash:DashboardPanel>
```

Drag a tile by its header. Resize it from the bottom-right grip when `IsResizable` is `true`.

## Layout Model

Tiles expose grid properties that are synchronized to the parent `DashboardPanel` attached properties:

- `GridX` and `GridY` define the top-left grid cell.
- `GridW` and `GridH` define the tile span in cells.
- `MinGridW` and `MinGridH` define resize minimums.

The panel resolves overlapping moves and resizes to the nearest valid placement. Snap preview color indicates whether the requested placement was exact or adjusted.

## Data Binding And Persistence

Bind grid properties to view model fields to save and restore layouts:

```xml
<dash:TextTile GridX="{Binding X}"
               GridY="{Binding Y}"
               GridW="{Binding W}"
               GridH="{Binding H}" />
```

Recommended persistence shape:

1. Store one view model per tile with `Id`, `X`, `Y`, `W`, `H`, tile type, and payload.
2. At startup, deserialize the layout, create each tile, and apply the grid properties.
3. After user move/resize, persist the updated grid values from the bound view models.

## Custom Header Content

```xml
<dash:Tile GridX="0" GridY="0" GridW="3" GridH="2"
           TileBackground="#0ea5e9">
  <dash:Tile.HeaderContent>
    <StackPanel Orientation="Horizontal" Spacing="6">
      <Path Data="M2,2 L12,7 L2,12 Z"
            Width="14"
            Height="14"
            Stroke="DarkBlue"
            StrokeThickness="1.5" />
      <TextBlock Text="System" FontWeight="SemiBold" />
      <Button Content="..." Padding="2,0" MinWidth="24" />
    </StackPanel>
  </dash:Tile.HeaderContent>

  <TextBlock Text="Body text" />
</dash:Tile>
```

Interactive controls in the header, such as buttons and text inputs, do not start tile dragging.

## ScottPlot Extension

Register `ScottPlotStyles`, then use chart tiles inside a dashboard:

```xml
<cartesian:ScatterPlotTile xmlns:cartesian="clr-namespace:MiniDashboard.Avalonia.ScottPlot.Cartesian;assembly=MiniDashboard.Avalonia.ScottPlot"
                           GridX="0" GridY="0" GridW="5" GridH="3"
                           TileHeader="Scatter"
                           Ys="{Binding SimpleSeries}" />
```

Available chart types include `ScatterPlotTile`, `SignalPlotTile`, `HistogramPlotTile`, `BarsPlotTile`, `FinancialPlotTile`, and `PiePlotTile`.

## TreeDataGrid Extension

Register `TreeDataGridStyles`, then bind a `TreeDataGridSource` to `TreeDataGridTile.Source`:

```xml
<data:TreeDataGridTile xmlns:data="clr-namespace:MiniDashboard.Avalonia.TreeDataGrid;assembly=MiniDashboard.Avalonia.TreeDataGrid"
                       GridX="2" GridY="0" GridW="5" GridH="4"
                       TileHeader="People"
                       Source="{Binding PeopleGridSource}" />
```

`CsvGridTile` can load a simple comma-separated file path. It intentionally uses a minimal CSV parser and does not support quoting or escaping.

## Demo App

The demo app builds without the TreeDataGrid demo by default so the solution can be built without an Avalonia UI license key:

```bash
dotnet build .\MiniDashboard.Avalonia.slnx
```

To include the TreeDataGrid demo surfaces, build with:

```bash
dotnet build .\DemoApp\DemoApp.csproj -p:EnableTreeDataGridDemo=true
```

## Build And Package

Build the full solution:

```bash
dotnet build .\MiniDashboard.Avalonia.slnx
```

Create NuGet packages explicitly with `dotnet pack`:

```bash
dotnet pack .\MiniDashboard.Avalonia\MiniDashboard.Avalonia.csproj -c Release
dotnet pack .\MiniDashboard.ScottPlot\MiniDashboard.Avalonia.ScottPlot.csproj -c Release
dotnet pack .\MiniDashboard.TreeDataGrid\MiniDashboard.Avalonia.TreeDataGrid.csproj -c Release
```

Packages are written to `Build/<Configuration>/Packages/`.

## Extending

To create a custom tile:

1. Derive from `Tile`, or from an extension base such as `CartesianPlotTile`.
2. Register Avalonia styled properties with `AvaloniaProperty.Register`.
3. Provide a control theme or template under your application's styles.
4. Bind template parts to the tile properties.

Example:

```csharp
using System;
using Avalonia;
using MiniDashboard.Avalonia;

public class ClockTile : Tile
{
    public static readonly StyledProperty<DateTime> NowProperty =
        AvaloniaProperty.Register<ClockTile, DateTime>(nameof(Now), DateTime.Now);

    public DateTime Now
    {
        get => GetValue(NowProperty);
        set => SetValue(NowProperty, value);
    }
}
```

## License

MIT. See [LICENSE.txt](LICENSE.txt).
