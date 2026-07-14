# MiniDashboard.Avalonia

[![NuGet](https://img.shields.io/nuget/v/MiniDashboard.Avalonia?style=flat-square&logo=nuget&color=blue)](https://www.nuget.org/packages/MiniDashboard.Avalonia)

**MiniDashboard.Avalonia** provides dashboard controls for **Avalonia 12** applications. It includes a fixed-grid dashboard panel, draggable and resizable tiles, collision-aware placement, and live snap feedback while moving or resizing tiles.

## Packages

- `MiniDashboard.Avalonia` - core dashboard panel and tile controls.
- `MiniDashboard.Avalonia.ScottPlot` - optional ScottPlot chart tiles.
- `MiniDashboard.Avalonia.TreeDataGrid` - optional TreeDataGrid tiles.
- `MiniDashboard.Avalonia.TreeDataGridOS` - optional TreeDataGrid tiles backed by the community-maintained open-source fork.

The projects target `net10.0`, matching the recommended target for Avalonia 12.

## Features

- **DashboardPanel**: fixed `Rows` x `Columns` grid, attached layout properties, snap preview, and collision-aware move/resize resolution.
- **DashboardItemsPanel**: ItemsSource-based dashboard host that materializes generated tile controls directly as dashboard children.
- **Tile**: base content tile with grid position/size properties, custom header content, optional resize grip, styling properties, and placement-valid feedback.
- **TextTile**: simple text display tile.
- **ImageTile**: image display tile using `ImageSource` or `SourceUri`.
- **TableViewTile**: read-only tabular data display using Avalonia's `TableView` control.
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
dotnet add package MiniDashboard.Avalonia.TreeDataGridOS
```

`MiniDashboard.Avalonia.TreeDataGrid` depends on Avalonia's TreeDataGrid package. Avalonia 12 TreeDataGrid package usage may require an Avalonia UI license in consuming applications.
`MiniDashboard.Avalonia.TreeDataGridOS` instead depends on the MIT-licensed community `TreeDataGrid.Avalonia` fork. The two packages are alternatives and should not be referenced together.

## Register Styles

Add the style include for each package you use in `App.axaml`:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:dashboard="clr-namespace:MiniDashboard.Avalonia.Themes;assembly=MiniDashboard.Avalonia"
             xmlns:dashboardScottPlot="clr-namespace:MiniDashboard.Avalonia.ScottPlot.Themes;assembly=MiniDashboard.Avalonia.ScottPlot"
             xmlns:dashboardGrid="clr-namespace:MiniDashboard.Avalonia.TreeDataGrid.Themes;assembly=MiniDashboard.Avalonia.TreeDataGrid"
             xmlns:dashboardGridOS="clr-namespace:MiniDashboard.Avalonia.TreeDataGrid.Themes;assembly=MiniDashboard.Avalonia.TreeDataGridOS">
  <Application.Styles>
    <FluentTheme />
    <dashboard:MiniDashboardStyles />

    <!-- Optional extension styles -->
    <dashboardScottPlot:ScottPlotStyles />
    <dashboardGrid:TreeDataGridStyles />
    <!-- Use this instead of dashboardGrid:TreeDataGridStyles for the community fork. -->
    <!-- <dashboardGridOS:TreeDataGridStyles /> -->
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

## Dynamic ItemsSource Dashboards

Use `DashboardItemsPanel` for MVVM or config-driven dashboards. It derives from `DashboardPanel`, exposes `ItemsSource` and `ItemTemplate`, and uses the control's `DataTemplates` collection to build one direct dashboard child per item. It does not use `ContentPresenter` item containers, so generated `Tile` controls keep the same drag and resize behavior as static XAML children.

```xml
<dash:DashboardItemsPanel xmlns:dash="clr-namespace:MiniDashboard.Avalonia;assembly=MiniDashboard.Avalonia"
                          xmlns:vm="clr-namespace:MyApp.ViewModels"
                          xmlns:views="clr-namespace:MyApp.Views"
                          ItemsSource="{Binding Tiles}"
                          Rows="10"
                          Columns="15">
  <dash:DashboardItemsPanel.DataTemplates>
    <DataTemplate DataType="{x:Type vm:LogTileViewModel}">
      <views:LogTile />
    </DataTemplate>
    <DataTemplate DataType="{x:Type vm:MetricTileViewModel}">
      <views:MetricChartTile />
    </DataTemplate>
  </dash:DashboardItemsPanel.DataTemplates>
</dash:DashboardItemsPanel>
```

Each generated control receives the source item as its `DataContext`. If an item is already a `Control`, it is used directly. If no matching template can build a `Control`, `DashboardItemsPanel` throws a clear exception.

By default, generated `Tile` controls bind common layout conventions from the item view model:

- `TileHeaderPath="Title"`
- `GridXPath="X"`
- `GridYPath="Y"`
- `GridWPath="Width"`
- `GridHPath="Height"`
- `GridBindingMode="TwoWay"`

Set any path to `{x:Null}` to disable that convention binding. Set `DisposeRemovedTiles="True"` to dispose removed generated controls that implement `IDisposable`. `PreserveStaticChildren="True"` keeps any static children declared directly in the panel while generated children are rebuilt.

`DashboardPanel` remains the simplest choice for static XAML dashboards. `DashboardItemsPanel` is the recommended host when tiles come from view models, configuration files, or runtime collections.

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

## TableView Tile

`TableViewTile` is a lightweight read-only table backed by Avalonia 12.1's `TableView`. Define native `TableViewColumn` instances for bindings, templates, widths, alignment, and optional per-column resize behavior:

```xml
<dash:TableViewTile xmlns:dash="clr-namespace:MiniDashboard.Avalonia;assembly=MiniDashboard.Avalonia"
                    GridX="0" GridY="0" GridW="5" GridH="3"
                    TileHeader="Services"
                    ItemsSource="{Binding Services}">
  <dash:TableViewTile.Columns>
    <TableViewColumn Header="Name" Binding="{Binding Name}" Width="2*" />
    <TableViewColumn Header="Status" Binding="{Binding Status}" Width="*" />
  </dash:TableViewTile.Columns>
</dash:TableViewTile>
```

Set `CanUserResizeColumns="False"` on the tile to disable resizing for all columns, or set `TableViewColumn.CanUserResize` on an individual column.

## TreeDataGrid Extension

Register `TreeDataGridStyles`, then bind a `TreeDataGridSource` to `TreeDataGridTile.Source`:

```xml
<data:TreeDataGridTile xmlns:data="clr-namespace:MiniDashboard.Avalonia.TreeDataGrid;assembly=MiniDashboard.Avalonia.TreeDataGrid"
                       GridX="2" GridY="0" GridW="5" GridH="4"
                       TileHeader="People"
                       Source="{Binding PeopleGridSource}" />
```

`CsvGridTile` can load a simple comma-separated file path. It intentionally uses a minimal CSV parser and does not support quoting or escaping.

For the community package, use the same CLR namespace with the OS assembly name and register `dashboardGridOs:TreeDataGridStyles` instead. Its `TreeDataGridTile` accepts the fork's `ITreeDataGridSource`; `CsvGridTile` continues to work without API changes.

## Extending

To create a custom tile:

1. Derive from `Tile`, or from an extension base such as `CartesianPlotTile`.
2. Register Avalonia styled properties with `AvaloniaProperty.Register`.
3. Provide a control theme or template under your application's styles.
4. Bind template parts to the tile properties.

If a derived tile should reuse a base tile's existing control theme, override `StyleKeyOverride`:

```csharp
public class LogTile : TableViewTile
{
  protected override Type StyleKeyOverride => typeof(TableViewTile);
}
```

Dashboard child styles use `:is(...)` selectors so derived tile classes still receive the dashboard-level corner radius, border brush, and border thickness bindings.

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
