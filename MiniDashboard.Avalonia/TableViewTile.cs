using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;

namespace MiniDashboard.Avalonia;

/// <summary>
/// Tile that displays read-only tabular data using an Avalonia <see cref="TableView"/>.
/// </summary>
public class TableViewTile : Tile
{
    /// <summary>
    /// Defines the collection of columns displayed by the table.
    /// </summary>
    public static readonly DirectProperty<TableViewTile, AvaloniaList<TableViewColumn>> ColumnsProperty =
        AvaloniaProperty.RegisterDirect<TableViewTile, AvaloniaList<TableViewColumn>>(
            nameof(Columns),
            tile => tile.Columns,
            (tile, value) => tile.Columns = value);

    /// <summary>
    /// Defines whether users can resize table columns.
    /// </summary>
    public static readonly StyledProperty<bool> CanUserResizeColumnsProperty =
        AvaloniaProperty.Register<TableViewTile, bool>(nameof(CanUserResizeColumns), true);

    /// <summary>
    /// Defines the items displayed by the table.
    /// </summary>
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<TableViewTile, IEnumerable?>(nameof(ItemsSource));

    private AvaloniaList<TableViewColumn> _columns = [];

    /// <summary>
    /// Gets or sets whether users can resize table columns.
    /// </summary>
    public bool CanUserResizeColumns
    {
        get { return GetValue(CanUserResizeColumnsProperty); }
        set { SetValue(CanUserResizeColumnsProperty, value); }
    }

    /// <summary>
    /// Gets or sets the columns displayed by the table.
    /// </summary>
    public AvaloniaList<TableViewColumn> Columns
    {
        get { return _columns; }
        set { SetAndRaise(ColumnsProperty, ref _columns, value); }
    }

    /// <summary>
    /// Gets or sets the items displayed by the table.
    /// </summary>
    public IEnumerable? ItemsSource
    {
        get { return GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }
}