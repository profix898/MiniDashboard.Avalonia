# MiniDashboard.Avalonia

Dashboard controls for Avalonia 12 applications. Choose free tile placement in a fixed grid with `DashboardPanel` (or the MVVM-friendly `DashboardItemsPanel`), or a dynamically editable matrix with `DashboardMatrix` — one content slot per cell, with insertable/removable rows and columns and proportional splitters. Both hosts share the same tile controls, content catalogs, and header styling.

## Packages

- `MiniDashboard.Avalonia` — dashboard hosts, content catalogs, and tile controls.
- `MiniDashboard.Avalonia.ScottPlot` — optional chart tiles (`ScatterPlotTile`, `SignalPlotTile`, `BarsPlotTile`, `PiePlotTile`, and more).
- `MiniDashboard.Avalonia.TreeDataGrid` — optional `TreeDataGridTile` and `CsvGridTile`; the underlying TreeDataGrid package may require an Avalonia UI license in consuming applications.
- `MiniDashboard.Avalonia.TreeDataGridOS` — the same tiles backed by the MIT-licensed community fork. It is an alternative to the commercial package; do not reference both together.

## Quick start

Install the core package:

```bash
dotnet add package MiniDashboard.Avalonia
```

Register the styles in `App.axaml`:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:dashboard="clr-namespace:MiniDashboard.Avalonia.Themes;assembly=MiniDashboard.Avalonia">
    <Application.Styles>
        <FluentTheme />
        <dashboard:MiniDashboardStyles />
    </Application.Styles>
</Application>
```

Optional extension packages install the same way and add their own style includes (for example `ScottPlotStyles` or `TreeDataGridStyles`).

## Basic usage

```xml
<dash:DashboardPanel xmlns:dash="clr-namespace:MiniDashboard.Avalonia;assembly=MiniDashboard.Avalonia"
                     Rows="6" Columns="8">
  <dash:TextTile GridX="0" GridY="0" GridW="2" GridH="2" TileHeader="Notes" Text="Drag me" />
  <dash:Tile GridX="2" GridY="0" GridW="3" GridH="2" TileHeader="Status">
    <StackPanel>
      <TextBlock Text="CPU" />
      <ProgressBar Minimum="0" Maximum="100" Value="57" />
    </StackPanel>
  </dash:Tile>
</dash:DashboardPanel>
```

Tiles place themselves with `GridX`, `GridY`, `GridW`, and `GridH`. Drag a tile by its header; resize it from the bottom-right grip when `IsResizable` is `true`. Overlapping moves resolve to a nearby free position, and colliding resizes shrink to fit. Grid properties support two-way binding, so layouts can be saved and restored from view models.

For view-model-driven dashboards, use `DashboardItemsPanel` with `ItemsSource` and `DataTemplates`; generated tiles keep full drag and resize behavior and bind common layout properties (`Title`, `X`, `Y`, `Width`, `Height`) by convention.

## Matrix dashboards

`DashboardMatrix` hosts one content slot per cell. Rows and columns can be inserted, removed, and resized at runtime with native splitters; every structural change is cancellable and produces an immutable layout snapshot for persistence.

```xml
<dash:DashboardMatrix xmlns:dash="clr-namespace:MiniDashboard.Avalonia;assembly=MiniDashboard.Avalonia"
                      MinCellWidth="96" MinCellHeight="72" />
```

```csharp
Matrix.ContentDefinitions = new DashboardContentDefinition[]
{
    new() { Id = "notes", Title = "Notes", Factory = () => new TextBox { AcceptsReturn = true } }
};
Matrix.SetContent(Matrix.Layout.Cells[0].Id, "notes");
Matrix.InsertColumnAfter(0); // splits column zero's weight in half
Matrix.InsertRowAfter(0);    // inserts across every column
```

Definition IDs are stable persistence keys. Handle `LayoutChanging` to veto changes (for example, confirm before removing occupied cells) and `LayoutChanged` to save the resulting snapshot.

## Shared content

`DashboardContentCatalog` supplies fresh-control factories keyed by stable IDs. The same catalog works in both dashboards: assign it to `DashboardMatrix.ContentDefinitions`, or place `DashboardContentTile` instances in a classic dashboard. `FromItems`, `FromTemplate`, and `FromTiles` adapt existing model lists, data templates, and tile recipes. Both hosts offer cancellable content changes and opt-in disposal of generated controls.

## Extensions

After registering the extension styles, use chart and grid tiles like any other tile:

```xml
<cartesian:ScatterPlotTile xmlns:cartesian="clr-namespace:MiniDashboard.Avalonia.ScottPlot.Cartesian;assembly=MiniDashboard.Avalonia.ScottPlot"
                           GridX="0" GridY="0" GridW="5" GridH="3"
                           TileHeader="Scatter" Ys="{Binding SimpleSeries}" />
```

```xml
<data:TreeDataGridTile xmlns:data="clr-namespace:MiniDashboard.Avalonia.TreeDataGrid;assembly=MiniDashboard.Avalonia.TreeDataGrid"
                       GridX="2" GridY="0" GridW="5" GridH="4"
                       TileHeader="People" Source="{Binding PeopleGridSource}" />
```

The core package also includes `TextTile`, `ImageTile`, and the read-only `TableViewTile`. Custom tiles derive from `Tile` and register styled properties as usual.

See the GitHub README for the full documentation: layout rules, persistence and snapshots, header customization, theming resources, and extension package notes.
