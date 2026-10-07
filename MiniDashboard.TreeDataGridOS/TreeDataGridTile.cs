using Avalonia;
using Avalonia.Controls;
using MiniDashboard.Avalonia.Tiles;

namespace MiniDashboard.Avalonia.TreeDataGridOS;

/// <summary>
/// Tile that hosts a tree data grid source. Inherits base tile behavior from Tile.
/// </summary>
public class TreeDataGridTile : Tile
{
    /// <summary>Defines whether the user can resize grid columns.</summary>
    public static readonly StyledProperty<bool> CanUserResizeColumnsProperty =
        AvaloniaProperty.Register<TreeDataGridTile, bool>(nameof(CanUserResizeColumns), true);

    /// <summary>Defines the data source displayed by the tree data grid.</summary>
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
