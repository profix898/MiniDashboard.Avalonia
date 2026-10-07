using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaGrid = Avalonia.Controls.Grid;

namespace MiniDashboard.Avalonia.Matrix;

// Avalonia 12.1 converts every star definition to pixel-valued stars while dragging,
// but its cancellation restores only the adjacent pair. Restore the complete axis
// so Escape/focus loss cannot distort the remaining columns or rows.
// All pointer capture, drag calculations, constraints, and keyboard resizing remain native.
internal sealed class DashboardMatrixSplitter : GridSplitter
{
    private AvaloniaGrid? _dragGrid;
    private ColumnDefinition[]? _columns;
    private RowDefinition[]? _rows;
    private GridLength[]? _lengths;

    protected override Type StyleKeyOverride => typeof(GridSplitter);

    protected override void OnDragStarted(VectorEventArgs e)
    {
        if (_lengths is null && Parent is AvaloniaGrid grid)
        {
            _dragGrid = grid;
            if (ResizeDirection == GridResizeDirection.Columns)
            {
                _columns = grid.ColumnDefinitions.ToArray();
                _lengths = _columns.Select(d => d.Width).ToArray();
            }
            else
            {
                _rows = grid.RowDefinitions.ToArray();
                _lengths = _rows.Select(d => d.Height).ToArray();
            }
        }

        base.OnDragStarted(e);
    }

    protected override void OnDragCompleted(VectorEventArgs e)
    {
        base.OnDragCompleted(e);
        ClearSnapshot();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
            RestoreSnapshot();
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        RestoreSnapshot();
    }

    private void RestoreSnapshot()
    {
        if (_lengths is not null && _dragGrid is not null)
        {
            if (_columns is not null && _dragGrid.ColumnDefinitions.SequenceEqual(_columns))
            {
                for (var i = 0; i < _columns.Length; i++)
                    _columns[i].Width = _lengths[i];
            }

            if (_rows is not null && _dragGrid.RowDefinitions.SequenceEqual(_rows))
            {
                for (var i = 0; i < _rows.Length; i++)
                    _rows[i].Height = _lengths[i];
            }
        }

        ClearSnapshot();
    }

    private void ClearSnapshot()
    {
        _dragGrid = null;
        _columns = null;
        _rows = null;
        _lengths = null;
    }
}
