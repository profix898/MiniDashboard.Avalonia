# MiniDashboard.Avalonia

Dashboard controls for Avalonia 12 / .NET 10 applications. Choose free tile placement in a fixed
grid with `DashboardPanel` (or the MVVM-friendly `DashboardItemsPanel`), or a dynamically editable
matrix with `DashboardMatrix`: one tile per cell, with insertable/removable rows and columns and
proportional splitters. Both layouts share tiles, catalogs, content pickers, and target selection.

## Packages

- `MiniDashboard.Avalonia`: dashboard hosts, content catalogs, pickers, and tile controls.
- `MiniDashboard.Avalonia.ScottPlot`: optional scatter, signal, histogram, bar, financial, and pie chart tiles.
- `MiniDashboard.Avalonia.TreeDataGrid`: optional TreeDataGrid/CSV tiles; the underlying Avalonia package may require an Avalonia UI license.
- `MiniDashboard.Avalonia.TreeDataGridOS`: the corresponding tiles using the MIT-licensed community fork; do not reference both grid packages.

## Quick start

```powershell
dotnet add package MiniDashboard.Avalonia
```

Register the core styles after your application theme in `App.axaml`:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:dashboard="clr-namespace:MiniDashboard.Avalonia.Themes;assembly=MiniDashboard.Avalonia">
    <Application.Styles>
        <FluentTheme />
        <dashboard:MiniDashboardStyles />
    </Application.Styles>
</Application>
```

Optional packages add their own style collections: `ScottPlotStyles` in `.ScottPlot.Themes`,
or `TreeDataGridStyles` in `.TreeDataGrid.Themes` / `.TreeDataGridOS.Themes`.

**Namespaces:** `.Grid` contains tile-grid hosts and placement APIs; `.Matrix` contains the matrix
host/layout/events; `.Tiles` contains shared tile controls; `.Content` contains catalogs/factories/
content pickers; `.Picking` contains the target-picker contract. These are under `MiniDashboard.Avalonia`.
When upgrading from the root-namespace API, update both C# imports and XAML declarations.

## Basic usage

```xml
<grid:DashboardPanel xmlns="https://github.com/avaloniaui"
                     xmlns:grid="clr-namespace:MiniDashboard.Avalonia.Grid;assembly=MiniDashboard.Avalonia"
                     xmlns:tiles="clr-namespace:MiniDashboard.Avalonia.Tiles;assembly=MiniDashboard.Avalonia"
                     Rows="6" Columns="8">
    <tiles:TextTile GridX="0" GridY="0" GridW="2" GridH="2" TileHeader="Notes" Text="Drag me" />
    <tiles:Tile GridX="2" GridY="0" GridW="3" GridH="2" TileHeader="Status">
        <TextBlock Text="Healthy" />
    </tiles:Tile>
</grid:DashboardPanel>
```

Drag headers and resize from the bottom-right grip. `GridX/Y` locate a tile; `GridW/H` define
spans. Collision handling finds nearby/free positions or adjusts resizing. Bind coordinates to
persist your layout; `PlacementChanging` can veto edits and `PlacementChanged` reports accepted edits.

For view-model-driven dashboards, use `DashboardItemsPanel.ItemsSource` with `ItemTemplate` or
`DataTemplates`. Generated tiles bind `Title`, `X`, `Y`, `Width`, and `Height` by convention.
Opt into `DisposeRemovedTiles` for owned template-created controls; direct control items are borrowed.

## Matrix dashboards

```xml
<matrix:DashboardMatrix xmlns="https://github.com/avaloniaui"
                        xmlns:matrix="clr-namespace:MiniDashboard.Avalonia.Matrix;assembly=MiniDashboard.Avalonia"
                        MinCellWidth="96" MinCellHeight="72" DisposeRemovedContent="True" />
```

```csharp
using Avalonia.Controls;
using MiniDashboard.Avalonia.Content;

Matrix.ContentDefinitions = new DashboardContentCatalog
{
    new DashboardContentDefinition
    {
        Id = "notes", Title = "Notes", Category = "General",
        Factory = () => new TextBox { AcceptsReturn = true }
    }
};
Matrix.SetContent(Matrix.Layout.Cells[0].Id, "notes");
Matrix.InsertColumnAfter(0); // Insert a whole column; preserve existing content.
Matrix.InsertRowAfter(0);
```

Matrices start at 1×1. Resize shared boundaries with pointer or keyboard; cells do not span or
resize independently. Headerless mode provides boundary menus. `LayoutChanging`/`ContentChanging`
can veto transactions. Save `GetSnapshot()` as JSON and apply it with `RestoreLayout`; snapshots
contain geometry/assignments, not controls or application content state. Move/swap preserves instances.

## Shared content

`DashboardContentCatalog` supplies fresh-control recipes with stable, case-sensitive IDs. Use it
in a matrix or tile-grid `DashboardContentTile`. Define exactly one `Factory` / `ContextFactory`
for a body, or `TileFactory` / `ContextTileFactory` for a complete tile. `FromItems`, `FromTemplate`,
`FromTiles`, and `CreateTile` adapt existing models. Contextual factories receive instance/cell IDs
and optional state from `ContentStateProvider`. Save that application state separately.

Reuse unchanged definitions to retain views; call `RefreshContent()` for metadata/plain-catalog edits.
Visual detachment preserves content. Permanently retire generated-content hosts with `Dispose()`;
opt into `DisposeRemovedContent` to dispose owned discarded factory content.

## Pickers and selectors

**Content selection chooses what to create.** The default is a compact menu. Attach a grouped
searchable chooser with `DashboardContentPicker.SetPicker(dashboard, new DashboardContentPicker
{ GroupByCategory = true })` from `.Content`. **+ Add content** opens it directly below the button;
search matches title/category/description. Implement `IDashboardContentPicker` for your own UI:
return a definition ID or null; the host handles assignment and factory lifecycle.

**Target selection chooses an existing tile for a command.** Both layout families implement
`.Picking.IDashboardTilePicker`: call `PickTileAsync(predicate, cancellationToken)`, or matrix
`PickCellAsync` for cell identity. Overlays intercept hosted input; clicks/Enter select, arrows/Tab
move focus, and Escape cancels. `CancelTilePick` / `CancelCellPick` support Cancel buttons.
Cancellation or relevant layout/content changes return null. The application supplies instructions
and executes its command; no persistent active tile is introduced.

## Extensions

After registering extension styles, chart and grid tiles work like other complete tiles. Chart
controls use `.ScottPlot.Cartesian` / `.ScottPlot.Pie`; grid controls use `.TreeDataGrid` or
**`.TreeDataGridOS`**, with matching assembly names. `CsvGridTile` has a minimal parser without
quoting/escaping support. Core `TableViewTile` provides read-only native Avalonia tables.

Custom tiles derive from `.Tiles.Tile`. Templates should honor `IsHeaderPresented`,
`IsResizeGripVisible`, `EffectiveHeaderActions`, and `AddContentActions`; keep `PART_Add` for direct
custom-picker opening. Theme resources control spacing, headers, target overlays, and light/dark colors.

See the [full user guide](https://github.com/profix898/MiniDashboard.Avalonia#readme) for detailed
examples, custom pickers, persistence, ownership, theming, and extension setup.
