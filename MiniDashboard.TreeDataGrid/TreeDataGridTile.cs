using Avalonia;
using Avalonia.Controls;

namespace MiniDashboard.Avalonia.TreeDataGrid;

/// <summary>
/// Tile that hosts a tree data grid source. Inherits base tile behavior from Tile.
/// </summary>
public class TreeDataGridTile : Tile
{
    // Styled property to control whether the user can resize columns.
    public static readonly StyledProperty<bool> CanUserResizeColumnsProperty =
        AvaloniaProperty.Register<TreeDataGridTile, bool>(nameof(CanUserResizeColumns), true);

    // Styled property backing field for the tree data source.
    public static readonly StyledProperty<ITreeDataGridSource?> SourceProperty =
        AvaloniaProperty.Register<TreeDataGridTile, ITreeDataGridSource?>(nameof(Source));

    /// <summary>
    /// Gets or sets a value indicating whether the user can resize columns in the grid.
    /// </summary>
    public bool CanUserResizeColumns
    {
        get { return GetValue(CanUserResizeColumnsProperty); }
        set { SetValue(CanUserResizeColumnsProperty, value); }
    }

    /// <summary>
    /// Gets or sets the data source used by the tree data grid inside this tile.
    /// </summary>
    public ITreeDataGridSource? Source
    {
        get { return GetValue(SourceProperty); }
        set { SetValue(SourceProperty, value); }
    }
}
