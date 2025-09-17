MiniDashboard.Avalonia
==========
[![Nuget](https://img.shields.io/nuget/v/MiniDashboard.Avalonia?style=flat-square&logo=nuget&color=blue)](https://www.nuget.org/packages/MiniDashboard.Avalonia)

**MiniDashboard.Avalonia** provides a minimalistic dashboard implementation for **[Avalonia](https://avaloniaui.net/)** with drag & resize tiles. Tiles align to a fixed grid, avoid collisions automatically, and show live green/red snap feedback while moving or resizing.

Packages in this repository
- Core: `MiniDashboard.Avalonia`
- Tree data grid extension: `MiniDashboard.Avalonia.TreeDataGrid`
- ScottPlot charts extension: `MiniDashboard.Avalonia.ScottPlot`

## Features
- **DashboardPanel**: fixed Rows × Columns grid, attached layout properties (X,Y,W,H), preview overlay, collision-aware placement & resizing.
- **Tile (base class)**: GridX/GridY/GridW/GridH, MinGridW/MinGridH, header (text or custom content), optional resize grip, styling (TileBackground/TileForeground/BorderBrush/CornerRadius/Padding), IsPlacementValid for preview tint.
- **Built-in tiles**: 
  - Core:
    - `Tile` (customizable base control)
    - `TextTile` — simple text content
    - `ImageTile` — displays images via ImageSource or SourceUri
  - TreeDataGrid extension:
    - `TreeDataGridTile` — hierarchical/grid data display
    - `CsvGridTile` — simple CSV-to-grid loader
  - ScottPlot extension (charts):
    - `PlotTile` (base)
    - `CartesianPlotTile` (base for cartesian plots)
    - `ScatterPlotTile`
    - `SignalPlotTile`
    - `HistogramPlotTile`
    - `BarsPlotTile`
    - `FinancialPlotTile`
    - `PiePlotTile` (base for pie charts)
    - `PieChartPlotTile`

## Quick Start
Install the packages you need:
```
# core
dotnet add package MiniDashboard.Avalonia
# optional extensions
# dotnet add package MiniDashboard.Avalonia.TreeDataGrid
# dotnet add package MiniDashboard.Avalonia.ScottPlot
```

Register styles in App.axaml (namespaces abbreviated):
```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:dash="clr-namespace:MiniDashboard.Avalonia;assembly=MiniDashboard.Avalonia"
             xmlns:tree="clr-namespace:MiniDashboard.Avalonia.TreeDataGrid;assembly=MiniDashboard.Avalonia.TreeDataGrid"
             xmlns:sp="clr-namespace:MiniDashboard.Avalonia.ScottPlot;assembly=MiniDashboard.Avalonia.ScottPlot">
  <Application.Styles>
    <FluentTheme Mode="Dark" />
    <dash:MiniDashboardStyles />
    <!-- optional -->
    <sp:ScottPlotStyles />
    <tree:TreeDataGridStyles />
  </Application.Styles>
</Application>
```

Basic dashboard example:
```xml
<dash:DashboardPanel Rows="6" Columns="8">
  <tiles:TextTile GridX="0" GridY="0" GridW="2" GridH="2"
                  TileHeader="Notes" Text="Drag me" />
  <tiles:TextTile GridX="2" GridY="0" GridW="3" GridH="2"
                  TileHeader="Status" Text="OK" />
</dash:DashboardPanel>
```
Drag via the header; resize via the bottom-right grip when IsResizable is true.

## Concepts
- **DashboardPanel**: fixed logical grid (`Rows × Columns`). Child tiles use attached properties `X,Y,W,H` internally synced from tile properties.
- **Tile**: visual container with header + body + optional resize grip.
- **Collision Resolution**: moving/resizing always resolves to a valid placement; preview shows green (exact) or red (adjusted); header tint uses `IsPlacementValid`.
- **Styling**: global grid stripes + per‑tile background/foreground; radius/margins applied consistently.ision resolution: the panel adjusts moves and resizes to avoid overlap; preview overlay indicates exact vs adjusted placement.

---
## Examples
### Custom Content & Header
```xml
<dash:DashboardPanel Rows="3" Columns="8">
  <!-- Custom body/content -->
  <dash:Tile GridX="0" GridY="0" GridW="3" GridH="2" TileHeader="System">
    <StackPanel>
      <TextBlock Text="CPU" />
      <ProgressBar Minimum="0" Maximum="100" Value="57" />
    </StackPanel>
  </dash:Tile>

  <!-- Custom header and body/content -->
  <dash:Tile GridX="3" GridY="0" GridW="3" GridH="2" TileBackground="#0ea5e9">
    <dash:Tile.HeaderContent>
      <StackPanel Orientation="Horizontal" Spacing="6">
        <Path Data="M2,2 L12,7 L2,12 Z" Width="14" Height="14" Stroke="DarkBlue" StrokeThickness="1.5" />
        <TextBlock Text="Custom" FontWeight="SemiBold" />
        <Button Content="⋯" Padding="2,0" MinWidth="24" />
      </StackPanel>
    </dash:Tile.HeaderContent>
    <TextBlock Text="Body text" />
  </dash:Tile>
</dash:DashboardPanel>
```

### Data Binding & Persistence
Bind grid properties to view model fields so you can save/restore layouts:
```xml
<tiles:TextTile GridX="{Binding X}" GridY="{Binding Y}" GridW="{Binding W}" GridH="{Binding H}" />
```

1. Represent each tile by a view model with `Id`, `X`, `Y`, `W`, `H`, plus type + payload.
2. At startup: deserialize, instantiate tiles, apply grid properties.
3. On user move/resize: the tile updates its own `Grid*` properties -> bind two‑way to auto update VM -> serialize on demand.

Serialize the collection of tile view models (e.g. JSON) containing `X,Y,W,H` to persist user layouts.

## Extension Packages

### Data Grid Tiles (`MiniDashboard.Avalonia.TreeDataGrid`)
MiniDashboard.Avalonia.TreeDataGrid
- TreeDataGridTile: exposes Source (ITreeDataGridSource) and column prefs.
- CsvGridTile: simple CSV-to-grid loader (no quoting/escaping).

Example:
```xml
<data:TreeDataGridTile GridX="2" GridY="0" GridW="5" GridH="4"
                       TileHeader="People"
                       Source="{Binding FlatGrid.Source}" />
```

CSV example:
```xml
<data:CsvGridTile GridX="0" GridY="4" GridW="6" GridH="3"
                  TileHeader="CSV"
                  FilePath="data/people.csv" />
```

### ScottPlot Tiles (`MiniDashboard.Avalonia.ScottPlot`)
Base classes:
- `PlotTile` – hosts `AvaPlot`; exposes `PlotBuilder` delegate.
- `CartesianPlotTile` – adds XY helpers: `AutoClear`, `DataBuilder`, labels (`Title`, `XLabel`, `YLabel`), limits (`XMin/XMax/YMin/YMax`), `ShowGrid`, styling defaults (`DefaultColor`, `DefaultLineWidth`, `DefaultLinePattern`, `DefaultMarkerShape`, `DefaultMarkerSize`).
- `PieChartPlotTile` – `DataBuilder`, `Title`, `ShowLegend`.


Concrete charts:
- `ScatterPlotTile` (`Traces` or `Points` or `Xs`+`Ys`)
- `SignalPlotTile` (`Values`, `SamplePeriod`, aggregation placeholders)
- `HistogramPlotTile` (`Samples`, `BinCount`, `BarWidthScale`)
- `BarsPlotTile` (`Values` or `(X,Y)` `Points`)
- `FinancialPlotTile` (`Ohlc` tuples)
- `PiePlotTile` (`Slices` with auto legend)

Example (Scatter + Pie):
```xml
<dash:DashboardPanel Rows="6" Columns="10">
  <sp:ScatterPlotTile GridX="0" GridY="0" GridW="5" GridH="3"
                      TileHeader="Scatter"
                      Points="10,20 11,25 12,22 13,30" />
  <sp:PiePlotTile GridX="5" GridY="0" GridW="5" GridH="3"
                  TileHeader="Pie" Title="Shares" Slices="{Binding PieSlices}" />
</dash:DashboardPanel>
```

---
## Extending (Create Custom Tiles)

Typical steps:
1. Derive from `Tile` (or an extension base like `CartesianPlotTile`).
2. Register styled properties with `AvaloniaProperty.Register`.
3. Provide a control template with header + body layout (under `Themes/Controls`).
4. Bind properties inside the template.
5. (Optional) Override template parts / attach behaviors in `OnApplyTemplate`.

### Example: ClockTile (simplified)
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
Control template style (place in a styles resource dictionary):
```xml
<Style Selector="ClockTile">
  <Setter Property="Template">
    <ControlTemplate TargetType="ClockTile">
      <Border Background="{TemplateBinding TileBackground}" Padding="8">
        <StackPanel>
          <TextBlock Text="{TemplateBinding TileHeader}" FontWeight="SemiBold"/>
          <TextBlock Text="{Binding Now, RelativeSource={RelativeSource TemplatedParent}, StringFormat='{}{0:HH:mm:ss}'}"
                     FontSize="24"/>
        </StackPanel>
      </Border>
    </ControlTemplate>
  </Setter>
  <Setter Property="MinGridW" Value="2"/>
  <Setter Property="MinGridH" Value="1"/>
  <Setter Property="TileHeader" Value="Clock"/>
</Style>
```
Periodic update (e.g. ViewModel):
```csharp
DispatcherTimer.Run(() => { clockTile.Now = DateTime.Now; return true; }, TimeSpan.FromSeconds(1));
```

## License
MIT (see LICENSE.txt).
