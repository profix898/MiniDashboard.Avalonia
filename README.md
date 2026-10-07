# MiniDashboard.Avalonia

[![NuGet](https://img.shields.io/nuget/v/MiniDashboard.Avalonia?style=flat-square&logo=nuget&color=blue)](https://www.nuget.org/packages/MiniDashboard.Avalonia)

**MiniDashboard.Avalonia** provides dashboard controls for **Avalonia 12** applications. Choose free tile placement in a fixed grid with `DashboardPanel`, or one tile per cell in a dynamically editable matrix with `DashboardMatrix`. Both hosts share the same tiles, content catalogs, and header styling.

![Tile-grid dashboard with text, custom content, image, chart, and table tiles](Screenshot.png)

*Tile-grid dashboard demo: draggable, resizable tiles with shared headers, ScottPlot charts, and a service table.*

## Packages

| Package | Contents |
|---|---|
| `MiniDashboard.Avalonia` | Layout hosts, tile controls, catalogs, factories, and pickers |
| `MiniDashboard.Avalonia.ScottPlot` | Scatter, signal, histogram, bar, financial, and pie chart tiles |
| `MiniDashboard.Avalonia.TreeDataGrid` | TreeDataGrid and CSV tiles using Avalonia's TreeDataGrid package |
| `MiniDashboard.Avalonia.TreeDataGridOS` | TreeDataGrid and CSV tiles using the MIT-licensed community fork |

The projects target **.NET 10**. The TreeDataGrid packages are alternatives; do not reference both. Avalonia's TreeDataGrid package may require an Avalonia UI license in consuming applications. The community alternative depends on the MIT-licensed `TreeDataGrid.Avalonia` fork.

## Features

- **DashboardPanel**: fixed rows/columns, tile spans, drag/resize, snap previews, and collision-aware placement.
- **DashboardItemsPanel**: the same tile grid populated from view models, templates, or runtime collections.
- **DashboardMatrix**: editable rows/columns, proportional splitters, headerless boundary menus, and layout snapshots.
- **Shared tiles**: text, images, read-only tables, custom content and headers, and optional chart/tree-grid extensions.
- **Content catalogs**: reusable recipes that create independent views, with contextual factories for restoring application state.
- **Content pickers**: compact menus, grouped searchable lists, or application-provided selection UI.
- **Command targeting**: explicitly select a live tile/cell with pointer or keyboard without activating its content.
- **Lifecycle and persistence**: cancellable changes, stable cell identities, opt-in disposal, and application-owned content state.

## Choosing a dashboard

| Host | Layout | Use it for |
|---|---|---|
| `DashboardPanel` | Individually positioned tiles with spans inside fixed rows/columns | Static XAML dashboards |
| `DashboardItemsPanel` | The same tile grid populated from an items source | View-model-driven dashboards |
| `DashboardMatrix` | One tile per cell; shared row/column sizes and structural editing | Comparing views in a structured layout |

A matrix insertion adds an **entire row or column**, not a nested split. Matrix tiles cannot span cells or resize independently. Start with the [tile-grid](#tile-grids) or [matrix](#matrix-dashboards) examples, then choose [tiles and custom content](#tiles-and-custom-content) for the views you need.

## Installation and styles

Install the core package and only the extensions you need:

```powershell
dotnet add package MiniDashboard.Avalonia
dotnet add package MiniDashboard.Avalonia.ScottPlot
```

Register the core styles after your application theme:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:dashboard="clr-namespace:MiniDashboard.Avalonia.Themes;assembly=MiniDashboard.Avalonia">
    <Application.Styles>
        <FluentTheme />
        <dashboard:MiniDashboardStyles />
    </Application.Styles>
</Application>
```

Add the corresponding style collection for each extension:

| Extension | XAML namespace | Style collection |
|---|---|---|
| ScottPlot | `clr-namespace:MiniDashboard.Avalonia.ScottPlot.Themes;assembly=MiniDashboard.Avalonia.ScottPlot` | `ScottPlotStyles` |
| TreeDataGrid | `clr-namespace:MiniDashboard.Avalonia.TreeDataGrid.Themes;assembly=MiniDashboard.Avalonia.TreeDataGrid` | `TreeDataGridStyles` |
| TreeDataGridOS | `clr-namespace:MiniDashboard.Avalonia.TreeDataGridOS.Themes;assembly=MiniDashboard.Avalonia.TreeDataGridOS` | `TreeDataGridStyles` |

### API namespaces

Use the namespace for each responsibility, even when controls appear in the same view:

| Namespace | Public API examples |
|---|---|
| `MiniDashboard.Avalonia.Grid` | `DashboardPanel`, `DashboardItemsPanel`, `DashboardPlacement`, placement events, `DraggableBehavior` |
| `MiniDashboard.Avalonia.Matrix` | `DashboardMatrix`, `DashboardMatrixLayout`, `DashboardMatrixCellModel`, matrix change events |
| `MiniDashboard.Avalonia.Tiles` | `Tile`, `DashboardContentTile`, `DashboardHeader`, `TextTile`, `ImageTile`, `TableViewTile` |
| `MiniDashboard.Avalonia.Content` | `DashboardContentCatalog`, `DashboardContentDefinition`, creation context, content events, content pickers |
| `MiniDashboard.Avalonia.Picking` | `IDashboardTilePicker` for one-shot command targeting |
| `MiniDashboard.Avalonia.Themes` | `MiniDashboardStyles` |

**Upgrading from the root-namespace API:** update C# imports, fully qualified references, and XAML declarations. Hosts and tiles no longer use the root `MiniDashboard.Avalonia` namespace. For example, map `grid` to `.Grid`, `matrix` to `.Matrix`, and `tiles` to `.Tiles`. Custom templates using `DraggableBehavior` must reference `.Grid`. Source folders follow the same responsibility boundaries.

## Tile grids

### Static tiles

```xml
<grid:DashboardPanel xmlns="https://github.com/avaloniaui"
                     xmlns:grid="clr-namespace:MiniDashboard.Avalonia.Grid;assembly=MiniDashboard.Avalonia"
                     xmlns:tiles="clr-namespace:MiniDashboard.Avalonia.Tiles;assembly=MiniDashboard.Avalonia"
                     Rows="6" Columns="8">
    <tiles:TextTile GridX="0" GridY="0" GridW="2" GridH="2"
                    TileHeader="Notes" Text="Drag me" />
    <tiles:Tile GridX="2" GridY="0" GridW="3" GridH="2" TileHeader="Status">
        <StackPanel>
            <TextBlock Text="CPU" />
            <ProgressBar Minimum="0" Maximum="100" Value="57" />
        </StackPanel>
    </tiles:Tile>
</grid:DashboardPanel>
```

Drag a tile by its header; resize from the bottom-right grip when `IsResizable` is true. Interactive header controls do not start dragging. Only direct dashboard children participate; nested content cannot move or resize its outer tile.

| Tile property | Meaning |
|---|---|
| `GridX`, `GridY` | Zero-based top-left cell |
| `GridW`, `GridH` | Width/height in cells |
| `MinGridW`, `MinGridH` | Minimum spans during resizing |

These values synchronize with the panel's attached `X`, `Y`, `W`, and `H` properties. The panel resolves colliding moves to a nearby free position and shrinks colliding resize requests to fit. Snap previews and `LastPlacementFailureReason` describe whether a placement can be applied exactly.

Use `TrySetPlacement(child, x, y, width, height)` for explicit placement edits. `PlacementChanging` can veto them; `PlacementChanged` reports accepted edits. See [persistence and transactions](#persistence-and-transactions) for saving coordinates and restoring layouts.

### Items-source tiles

`DashboardItemsPanel` derives from `DashboardPanel`. `ItemTemplate` or its `DataTemplates` creates one direct dashboard child per item; no item-container wrapper interferes with tile interactions.

```xml
<grid:DashboardItemsPanel xmlns="https://github.com/avaloniaui"
                          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                          xmlns:grid="clr-namespace:MiniDashboard.Avalonia.Grid;assembly=MiniDashboard.Avalonia"
                          xmlns:tiles="clr-namespace:MiniDashboard.Avalonia.Tiles;assembly=MiniDashboard.Avalonia"
                          xmlns:vm="clr-namespace:MyApp.ViewModels"
                          ItemsSource="{Binding Tiles}" Rows="10" Columns="15"
                          DisposeRemovedTiles="True">
    <grid:DashboardItemsPanel.DataTemplates>
        <DataTemplate DataType="vm:NoteTileViewModel" x:DataType="vm:NoteTileViewModel">
            <tiles:TextTile Text="{Binding Text}" />
        </DataTemplate>
    </grid:DashboardItemsPanel.DataTemplates>
</grid:DashboardItemsPanel>
```

The containing application supplies `Tiles` and the view-model namespace. Template-created controls receive the item as `DataContext`. A direct `Control` item is borrowed without replacing its data context or bindings. A missing or incompatible template throws.

Generated tiles use these conventions by default:

| Property | Default |
|---|---|
| `TileHeaderPath` | `Title` |
| `GridXPath`, `GridYPath` | `X`, `Y` |
| `GridWPath`, `GridHPath` | `Width`, `Height` |
| `GridBindingMode` | `TwoWay` |
| `PreserveStaticChildren` | `True` |
| `DisposeRemovedTiles` | `False` |

Set a convention path to `{x:Null}` to disable it. Observable collection changes reconcile items; unchanged item references retain their generated controls through moves/resets. Changing templates regenerates template-owned controls. Dispose the items host when retiring it permanently. Temporary visual detachment preserves its instances. `DisposeRemovedTiles` applies to owned generated controls, not borrowed direct `Control` items or application-owned static children.

## Matrix dashboards

A matrix starts as **1×1**. Give it finite available width and height:

```xml
<matrix:DashboardMatrix xmlns="https://github.com/avaloniaui"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:matrix="clr-namespace:MiniDashboard.Avalonia.Matrix;assembly=MiniDashboard.Avalonia"
                        x:Name="Matrix" MinCellWidth="96" MinCellHeight="72"
                        DisposeRemovedContent="True" />
```

Supply a catalog, assign a cell, and insert rows/columns on the Avalonia UI thread:

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
Matrix.InsertColumnAfter(0); // 1×2; existing column weight is divided equally.
Matrix.InsertRowAfter(0);    // 2×2; inserts across every column.
```

### Structure, sizing, and movement

- `InsertRowBefore/After` and `InsertColumnBefore/After` use zero-based row/column indices and insert empty cells.
- `RemoveRow` and `RemoveColumn` discard the whole row/column, transferring its weight to the previous neighbor, or the next at index zero.
- At least one row and one column remain. Cell IDs survive coordinate changes.
- `SetContent(cellId, id)` assigns a definition; `ClearContent(cellId)` clears it.
- `MoveContent(sourceId, targetId)` requires an empty target. `SwapContent(firstId, secondId)` swaps occupied or empty cells. Both preserve live content instances.
- `GetTile(cellId)` returns the live tile after template application. `GetContent(cellId)` returns the factory-created body or complete tile.

Splitters resize whole rows/columns proportionally. Tab to a splitter and use arrow keys. Minimum cell sizes can make a large matrix exceed its viewport; limit its dimensions or provide scrolling.

| Property | Default | Behavior |
|---|---|---|
| `CanResize` | `True` | Enable pointer/keyboard splitter input |
| `CanModifyStructure` | `True` | Enable row/column insertion/removal; content editing remains separate |
| `MinCellWidth`, `MinCellHeight` | `96`, `72` DIP | Minimum cell dimensions |
| `CellSpacing` | `6` DIP | Uniform inter-cell spacing |
| `SplitterThickness` | `1` DIP | Visible divider width |
| `SplitterHitThickness` | `8` DIP | Input width overlapping adjacent edges |
| `Padding` | `6` DIP | Outer spacing |
| `IsHeaderVisible` | `True` | Show common tile headers |
| `BoundaryActionSize` | `24` DIP, minimum `16` | Headerless action button size |

The reserved divider gap is `max(CellSpacing, SplitterThickness)`; increasing hit width does not increase spacing. At intersections the row splitter takes pointer priority; the column splitter remains reachable elsewhere or by keyboard. Splitters reveal on hover or keyboard focus.

### Cell menus and headerless interaction

Cell header menus provide content selection, clearing, move/swap to adjacent cells, insertion, and row/column removal. Complete tiles keep their custom `HeaderActions` under **Tile actions**. Body tiles show the catalog title; complete tiles keep their own header content/title.

With `IsHeaderVisible="False"`, hover, tap, or keyboard-focus a boundary to reveal its action button. The menu offers **Insert row/column here** and commands for adjacent cells. Outer-edge actions keep a 1×1 matrix editable. Tap once to reveal, then tap the button to open it; drag the remaining boundary to resize. Tap elsewhere to dismiss. Hidden buttons do not intercept hosted content. Header toggling preserves instances, assignments, and proportions. Matrix overrides suppress individual resize grips without overwriting the tile's `IsResizable`, spans, or header properties.

See [persistence and transactions](#persistence-and-transactions) for change-event contracts, confirmation, and saving/restoring matrix snapshots.

## Tiles and custom content

Tiles work in either dashboard family. Place them directly in a tile grid or create them through a matrix catalog's complete-tile factory.

| Tile | Purpose |
|---|---|
| `Tile` | Custom body content with shared header, actions, and layout properties |
| `TextTile` | A block of text through `Text` |
| `ImageTile` | An image through `ImageSource` or `SourceUri` |
| `TableViewTile` | Read-only tabular data using native Avalonia `TableView` columns |
| `DashboardContentTile` | A body selected from a content catalog |

### Custom bodies and headers

Put application controls inside `Tile` without creating a new tile type. `HeaderContent` / `HeaderTemplate` replace the title presentation; `HeaderActions` supplies a tile-local flyout. Interactive header controls preserve their own input. Plain titles trim with ellipsis and expose the full title in a tooltip. Hosted content keeps its own context menu.

```xml
<tiles:Tile xmlns="https://github.com/avaloniaui"
            xmlns:tiles="clr-namespace:MiniDashboard.Avalonia.Tiles;assembly=MiniDashboard.Avalonia"
            GridX="0" GridY="0" GridW="3" GridH="2">
    <tiles:Tile.HeaderContent>
        <StackPanel Orientation="Horizontal" Spacing="6">
            <TextBlock Text="System" />
            <Button Content="Details" />
        </StackPanel>
    </tiles:Tile.HeaderContent>
    <TextBlock Text="Body text" />
</tiles:Tile>
```

### Images and tables

`ImageTile` accepts `ImageSource` or `SourceUri`, with stretch properties. URI loading supports embedded `avares://`/`resm://` assets and local files; HTTP/HTTPS loading is not implemented.

`TableViewTile` is a read-only Avalonia `TableView`. Define native columns and bind the application's row source; `CanUserResizeColumns` defaults to true. Per-column resizing remains configurable.

```xml
<tiles:TableViewTile xmlns="https://github.com/avaloniaui"
                     xmlns:tiles="clr-namespace:MiniDashboard.Avalonia.Tiles;assembly=MiniDashboard.Avalonia"
                     TileHeader="Services" ItemsSource="{Binding Services}">
    <tiles:TableViewTile.Columns>
        <TableViewColumn Header="Name" Binding="{Binding Name}" Width="2*" />
        <TableViewColumn Header="Status" Binding="{Binding Status}" Width="*" />
    </tiles:TableViewTile.Columns>
</tiles:TableViewTile>
```

Application row bindings/templates should specify their appropriate data types for compiled binding.

### ScottPlot tiles

After registering `ScottPlotStyles`, use chart tiles like any other tile:

```xml
<cartesian:ScatterPlotTile xmlns="https://github.com/avaloniaui"
                           xmlns:cartesian="clr-namespace:MiniDashboard.Avalonia.ScottPlot.Cartesian;assembly=MiniDashboard.Avalonia.ScottPlot"
                           GridX="0" GridY="0" GridW="5" GridH="3"
                           TileHeader="Scatter" Ys="{Binding Series}" />
```

Available types include `ScatterPlotTile`, `SignalPlotTile`, `HistogramPlotTile`, `BarsPlotTile`, and `FinancialPlotTile` in `.ScottPlot.Cartesian`, and `PiePlotTile` in `.ScottPlot.Pie`. Complete-tile factories let the same chart tile work in either dashboard family.

### TreeDataGrid tiles

Use `.TreeDataGrid` for the Avalonia package, or **`.TreeDataGridOS`** for the community package. Their CLR namespaces and assembly names are distinct:

```xml
<data:TreeDataGridTile xmlns="https://github.com/avaloniaui"
                       xmlns:data="clr-namespace:MiniDashboard.Avalonia.TreeDataGridOS;assembly=MiniDashboard.Avalonia.TreeDataGridOS"
                       GridX="2" GridY="0" GridW="5" GridH="4"
                       TileHeader="People" Source="{Binding PeopleGridSource}" />
```

`Source` accepts the corresponding package's `ITreeDataGridSource`. `CsvGridTile.FilePath` loads a simple comma-separated file; its minimal parser does not support quoting or escaping.

## Extending: new tile types

For reusable tile behavior, derive from `Tile` or an appropriate chart/grid tile, register styled properties, and supply a theme/template. Override `StyleKeyOverride` when reusing a base tile's theme:

```csharp
using System;
using MiniDashboard.Avalonia.Tiles;

public class LogTile : TableViewTile
{
    protected override Type StyleKeyOverride => typeof(TableViewTile);
}
```

Templates shared between layout families should bind `IsHeaderPresented`, `IsResizeGripVisible`, `EffectiveHeaderActions`, and `AddContentActions` rather than ignoring the matrix host overrides. Keep `PART_Add` for direct custom-picker opening and the resize/header parts used by the built-in themes. Use `DraggableBehavior` from `.Grid` for draggable headers. Dashboard child styles use `:is(...)` selectors, so derived controls retain common tile-grid borders, radii, and spacing.

## Content catalogs and factories

A content definition is a recipe, not a live view. IDs are stable, case-sensitive, nonlocalized persistence keys. `Title` is display text; optional `Category` and `Description` support search. Each factory call must return a fresh, unparented control. Reusing a live control across cells fails.

Specify **exactly one** factory per `DashboardContentDefinition`:

| Factory | Returns | Use it for |
|---|---|---|
| `Factory` | Non-`Tile` `Control` | Body content; the matrix supplies a `DashboardContentTile` |
| `TileFactory` | `Tile` | Complete core, chart, grid, or application tile |
| `ContextFactory` | Non-`Tile` `Control` | Body content requiring creation/restoration context |
| `ContextTileFactory` | `Tile` | Complete tile requiring creation/restoration context |

Every matrix cell hosts a tile directly, without nested tile wrappers. A standalone `DashboardContentTile` accepts body factories only; complete tiles belong directly in a dashboard.

```csharp
using Avalonia.Controls;
using MiniDashboard.Avalonia.Content;
using MiniDashboard.Avalonia.Tiles;

var catalog = new DashboardContentCatalog
{
    new DashboardContentDefinition
    {
        Id = "notes", Title = "Notes", Category = "General",
        Factory = () => new TextBox { AcceptsReturn = true }
    },
    new DashboardContentDefinition
    {
        Id = "status", Title = "Status", Category = "Monitoring",
        TileFactory = () => new TextTile { TileHeader = "Status", Text = "Healthy" }
    }
};
matrix.ContentDefinitions = catalog;
matrix.SetContent(matrix.Layout.Cells[0].Id, "status");
tileGrid.Children.Add(catalog.CreateTile("status")); // Fresh complete tile.
tileGrid.Children.Add(catalog.CreateTile("notes"));  // Fresh body-content tile.
```

For a selectable body tile, set `ContentDefinitions`, `ContentId`, and optionally `DisposeRemovedContent`. `ContentId` binds two-way by default; `SetContent(id)` / `SetContent(null)` preserve existing bindings. `ContentChanging`, `ContentChanged`, and `ContentFailed` also apply to standalone body tiles.

`DashboardContentCatalog` is observable and indexed; duplicate/blank IDs, blank titles, and invalid factory combinations are rejected. Helpers adapt existing models:

- `FromItems(items, idSelector, titleSelector, factory)` creates body definitions.
- `FromTemplate(items, idSelector, titleSelector, template)` creates bodies and assigns the model as `DataContext`.
- `FromTiles(items, idSelector, titleSelector, tileFactory)` creates complete tile definitions.

Use `FromTiles` if a template builds tiles. `CreateTile(id)` creates an independent tile from either definition kind; it does not add it to a dashboard or manage its application lifetime.

### Refresh and ownership

Observable catalog edits reconcile attached hosts. For ordinary enumerable catalogs, or after changing mutable `Title`/`Category`/`Description`, call `RefreshContent()`. Reuse definition objects for unchanged recipes: replacing a definition object recreates its assigned views; metadata updates on the same object can refresh without recreation. Unknown IDs remain assigned and show an unavailable placeholder. Factory failures leave previous assignments/controls intact; menu-driven failures show feedback and direct APIs propagate exceptions.

Matrix content survives structural edits, resizing, move/swap, and temporary visual detachment. `DashboardContentTile` also retains its body through temporary detachment. Detached observable catalogs reconcile on reattachment. Call `Dispose()` when permanently retiring either generated-content host.

`DisposeRemovedContent` defaults to false. Enable it when the matrix/body tile owns factory-created `IDisposable` content: replacement, clearing, structural removal, catalog reconciliation, failed provisional creation, and explicit disposal release discarded instances. Disposal attempts continue after failures, then aggregate exceptions. Already-committed layouts remain committed. A plain `DashboardPanel` does not dispose application-owned child tiles for you.

### Creation context and application state

`DashboardContentCreationContext` contains:

| Member | Meaning |
|---|---|
| `InstanceId` | Fresh ID for this creation attempt; capture it in your application model if needed |
| `ContentId` | Catalog definition ID |
| `CellId` | Creation cell in a matrix; null for standalone/catalog tile creation |
| `State` | Opaque value supplied by `DashboardMatrix.ContentStateProvider`; otherwise null |

```csharp
using Avalonia.Controls;
using MiniDashboard.Avalonia.Content;

// savedNotes is an application-owned dictionary keyed by matrix cell ID.
matrix.ContentStateProvider = cell => savedNotes.TryGetValue(cell.Id, out var state) ? state : null;
matrix.ContentDefinitions = new DashboardContentCatalog
{
    new DashboardContentDefinition
    {
        Id = "notes", Title = "Notes",
        ContextFactory = context => new TextBox
        {
            AcceptsReturn = true, Text = context.State as string ?? string.Empty
        }
    }
};
// Set the provider/catalog before RestoreLayout materializes saved assignments.
matrix.RestoreLayout(savedLayout);
```

Factories are synchronous and provisional until the transaction commits. Start asynchronous work or register live instances after committed notifications, not from a constructor before success is known. Cell identity describes placement; instance identity describes content. Moving/swapping preserves the live instance and does not invoke the factory again. Its creation `CellId` is not its permanent location. Save content state separately and resolve current placement through the current layout and `GetContent`.

## Content selection: menu, searchable list, or custom UI

**Content selection chooses what to put in a tile/cell.** It does not select a target for an export or other application command. The default content chooser is a compact catalog menu.

Enable the built-in grouped searchable chooser through the attached property on either dashboard:

```csharp
using MiniDashboard.Avalonia.Content;

DashboardContentPicker.SetPicker(matrix, new DashboardContentPicker { GroupByCategory = true });
DashboardContentPicker.SetPicker(tileGrid, new DashboardContentPicker { GroupByCategory = true });
// Passing null restores the compact menu; changing the setting refreshes selection menus.
```

The setting also works on a standalone `DashboardContentTile`. It inherits through tile-grid children and is propagated to matrix cells. Changing `GroupByCategory` on an existing picker affects its next opening. `Title`, `Category`, and `Description` are searched case-insensitively; results sort by category/title. Group headings are nonselectable, and the current definition is selected when visible.

- **+ Add content** opens the searchable chooser directly below the clicked button.
- **Change content** in a header menu opens it below the header action button after the menu closes.
- Avalonia may reposition the popup to fit the screen. If no visible header action anchor exists, the tile is the fallback anchor.
- Choose a result with **Select**, double-click, or Enter in the list. Closing the popup or detaching its anchor cancels.

The demo's **Searchable content picker** checkbox switches this content chooser; **Pick a cell** is the separate command-target interaction described below.

For a custom chooser, implement `IDashboardContentPicker` and attach it with the same property:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using MiniDashboard.Avalonia.Content;

public sealed class ApplicationContentPicker : IDashboardContentPicker
{
    private readonly Func<Control, IReadOnlyList<DashboardContentDefinition>, string?, Task<string?>> _show;

    public ApplicationContentPicker(
        Func<Control, IReadOnlyList<DashboardContentDefinition>, string?, Task<string?>> show)
        => _show = show;

    public Task<string?> PickAsync(Control anchor,
        IReadOnlyList<DashboardContentDefinition> definitions, string? currentId)
        => _show(anchor, definitions, currentId);
}
```

Your delegate can display an application dialog, flyout, tree, or grouped list. The contract supplies the invoking anchor, available definitions, and current ID. Return a definition ID or null to cancel. Attach the instance with `DashboardContentPicker.SetPicker(matrix, new ApplicationContentPicker(showChooser))`, where `showChooser` is your asynchronous UI delegate. The same call works with a tile grid or body-content tile. Do not create controls or mutate assignments inside the chooser: the host applies the chosen ID through its transactional APIs, preserving veto, factory validation, and disposal. Factories and pickers are complementary: **the picker chooses a recipe; the factory creates its content**.

## Command-target selection

**Target selection chooses an existing tile/cell for an application command.** Both dashboard families implement `IDashboardTilePicker`; no persistent active tile or implicit command target is introduced.

```csharp
using System.Threading;
using Avalonia.Controls;
using MiniDashboard.Avalonia.Picking;
using MiniDashboard.Avalonia.Tiles;

// dashboard can be DashboardPanel, DashboardItemsPanel, or DashboardMatrix.
IDashboardTilePicker picker = dashboard;
Tile? target = await picker.PickTileAsync(
    tile => tile.Content is TextBox,
    cancellationToken);
if (target != null)
{
    // Execute your command against this live tile/application model.
}
```

For a matrix command that needs stable cell identity or factory-created content, use `PickCellAsync`:

```csharp
using System;
using Avalonia.Controls;
using MiniDashboard.Avalonia.Matrix;

Guid? cellId = await matrix.PickCellAsync(
    (cell, content) => cell.ContentId != null && content is TextBox,
    cancellationToken);
if (cellId.HasValue)
{
    Control? target = matrix.GetContent(cellId.Value);
    // Recheck your target's lifetime/capability before executing the command.
}
```

`PickTileAsync` passes the whole tile, including complete tiles. `PickCellAsync` passes the cell model and `GetContent` result (body control or complete tile); empty cells can be eligible if your predicate allows them. Start picking after layout/template application on the UI thread. Only one request per host may be pending; a second request throws. No eligible targets returns null immediately.

For complete chart tiles, test the tile type itself in `PickTileAsync`, or the factory-created tile in `PickCellAsync`. For wrapped bodies, inspect `tile.Content` or the cell's `content` argument.

Picking overlays intercept hosted input, so selecting does not click content, pan charts, drag tiles, or resize them. Matrix splitters/structural editing are temporarily suspended. Pointer input selects; arrows, Tab/Shift+Tab cycle eligible targets; Enter accepts; Escape cancels. Focus is restored afterward. The application supplies the instruction/status text and owns command execution.

| API | All hosts | Matrix-only alternative |
|---|---|---|
| Start | `PickTileAsync(predicate, token)` | `PickCellAsync(predicate, token)` |
| Pending state | `IsPickingTile` | `IsPickingCell` |
| Cancel button | `CancelTilePick()` | `CancelCellPick()` |

Cancellation returns null rather than throwing `OperationCanceledException`. Requests also cancel on host detachment or bounds changes. Tile grids cancel when children, target bounds, or target content change; matrices cancel on committed layout changes or content reconciliation. Eligibility is evaluated at selection start: revalidate application capabilities after selection. This API does not support multi-selection or retain the result as dashboard state.

The default overlay is 80% opaque, dropping to approximately 65% over an eligible hovered target. Hover and keyboard focus use a bold label and muted 1 DIP outline. Theme-aware resources are customizable: `DashboardTargetOverlayBrush`, `DashboardTargetHoverOverlayBrush`, `DashboardTargetHighlightBrush`, and `DashboardTargetPickerButtonTheme`.

## Persistence and transactions

### Tile-grid layouts

Persist bound tile coordinates or accepted `PlacementChanged` events. A typical application stores tile ID, type/content ID, `GridX/Y`, `GridW/H`, and payload. At startup, recreate each tile and restore these values. The tile grid does not supply a built-in layout snapshot serializer.

### Matrix transactions

`LayoutChanging` supplies `Kind`, `Before`, `After`, and `AffectedCells`; set `Cancel = true` to veto. `ContentChanging` can veto assignment changes. `ContentChanged` and `LayoutChanged` report committed operations. `ContentFailed` reports creation errors. Insertion, removal, assignment, restoration, and proportion changes participate in the transaction policy.

Operations return false when cancelled, disabled, or unchanged; invalid inputs and factory failures throw. A self-move/swap is a no-op, while `TrySetPlacement` in a tile grid can return true for an already-valid unchanged placement. Do not mutate the matrix synchronously inside its change events. For asynchronous confirmation, veto first, ask the user, then schedule/retry after validating the target. An exception from a post-commit handler does not undo an already committed layout.

### Saving and restoring a matrix

```csharp
using System.Text.Json;
using MiniDashboard.Avalonia.Matrix;

// GetSnapshot flushes pending native splitter proportions before saving.
string json = JsonSerializer.Serialize(Matrix.GetSnapshot());
DashboardMatrixLayout saved = JsonSerializer.Deserialize<DashboardMatrixLayout>(json)!;
Matrix.RestoreLayout(saved);
```

Snapshots contain row/column star weights, cell IDs/coordinates, and content IDs. Weights are positive and finite and need not sum to one. Layout models are immutable; `WithContent`, `WithWeights`, and structural methods return new models to apply with `RestoreLayout`. Explicit restoration is available even when interactive structural editing is disabled. Snapshots contain **no controls, factories, or application-specific content state**; save that state separately, keyed by cell/instance identity. Use [contextual factories](#creation-context-and-application-state) to restore application state when the layout materializes its content.

## Styling and shared resources

Override common resources in application or container scope:

| Resource | Default / purpose |
|---|---|
| `DashboardCellCornerRadius` | `6` DIP |
| `DashboardContentPadding` | `6` DIP body padding |
| `DashboardHeaderMinHeight` | `32` DIP |
| `DashboardActionSize` | `28` DIP header action target |
| `TileBackgroundBrush`, `TileHeaderBackgroundBrush`, `TileForegroundBrush`, `TileBorderBrush` | Light/dark tile colors |
| `DashboardContentActionsLabel`, `DashboardAddContentLabel` | Header/empty-state action labels |
| `DashboardEmpty` | Empty-cell title, resolved during content creation |

Menu strings can be overridden with keys such as `DashboardAddContent`, `DashboardChangeContent`, `DashboardClearContent`, and `DashboardInsertRowAbove`; they are resolved when menus are built. The built-in searchable picker and target overlay currently use English UI labels. Tile-grid `TileMargin` defaults to `3` DIP on each edge, producing the same `6` DIP inter-tile gap as the matrix. Header actions reveal on hover/focus and remain visible while their flyout is open.

## Demo and development

```powershell
dotnet run --project DemoApp/DemoApp.csproj
dotnet test MiniDashboard.Avalonia.Tests/MiniDashboard.Avalonia.Tests.csproj -c Debug
dotnet build MiniDashboard.Avalonia.slnx -c Release
```

Demo tabs are **Static**, **Dynamic (ItemsSource)**, **Matrix**, and **Shared catalog**. Matrix demonstrates row/column editing, headerless controls, cancellable removal, searchable content selection, one-shot cell targeting, and JSON snapshots. Shared catalog demonstrates independent views created from the same recipes in both layouts and explicit tile/cell targeting. Debug startup enables Avalonia developer tools.

## License

MIT. See [LICENSE.txt](LICENSE.txt).
