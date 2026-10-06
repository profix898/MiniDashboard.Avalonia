# MiniDashboard.Avalonia

Dashboard controls for Avalonia 12 applications.

Install the core package:

```bash
dotnet add package MiniDashboard.Avalonia
```

Register the styles:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:dashboard="clr-namespace:MiniDashboard.Avalonia.Themes;assembly=MiniDashboard.Avalonia">
    <Application.Styles>
        <FluentTheme />
        <dashboard:MiniDashboardStyles />
    </Application.Styles>
</Application>
```

Optional packages:

```bash
dotnet add package MiniDashboard.Avalonia.ScottPlot
dotnet add package MiniDashboard.Avalonia.TreeDataGrid
dotnet add package MiniDashboard.Avalonia.TreeDataGridOS
```

`MiniDashboard.Avalonia.TreeDataGridOS` uses the community-maintained MIT `TreeDataGrid.Avalonia` fork and is an
alternative to the commercial TreeDataGrid extension. See the GitHub README for full setup, examples, and extension
package notes.

The core package also includes DashboardMatrix: one catalog-selected content slot per
cell, globally insertable/removable rows and columns, native proportional GridSplitters,
touch-accessible cell actions, cancellable changes, and immutable layout snapshots.
Register MiniDashboardStyles and supply DashboardContentDefinition factories.
See the repository README and the demo's Matrix tab for usage and ownership details.

## Shared dashboard content

`DashboardContentCatalog` supplies fresh-control factories keyed by stable IDs.
Use it with `DashboardMatrix` or `DashboardContentTile` inside a classic dashboard.
`FromItems`, `FromTemplate`, and `FromTiles` adapt existing model lists and factories.
Both hosts offer cancellable content changes and opt-in generated-control disposal.
Common headers show content titles and hover/focus actions; matrix headers are optional.
Matrix defaults use a 1 DIP divider, 8 DIP hit target, and consistent 6 DIP spacing.
Rows and columns have minimum cell sizes; the host should constrain matrix dimensions
or provide scrolling when the available space is smaller than those minimums.
See the repository README and the **Same tiles, two layouts** demo for examples.

Both dashboards now host the same `Tile` templates directly. Catalog `Factory`
entries create bodies inside `DashboardContentTile`; `TileFactory` entries create
complete tiles without nesting. `catalog.CreateTile(id)` works for classic dashboards.
Matrix tiles can move/swap through their menu, have optional headers, and never show
individual resize grips. Matrix splitters resize entire rows and columns.

Headerless matrices move tile actions to boundary menus, including the outer edges
of a 1×1 layout. Centered Add content buttons remain available. Configurable
`BoundaryActionSize` sets the temporary overlay size (24 DIP by default), without enlarging gaps.
Hover or tap a boundary, or use keyboard focus, to reveal its menu button.
