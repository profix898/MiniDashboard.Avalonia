using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace MiniDashboard.Avalonia;

// Cell coordinates and structural actions belong to the host, not the shared tile.
internal sealed class DashboardMatrixCell
{
    private readonly DashboardMatrix _owner;
    private readonly Guid _id;
    private readonly MenuFlyout _menu = new MenuFlyout();

    public Tile Tile { get; }

    public DashboardMatrixCell(DashboardMatrix owner, Guid id, Tile tile)
    {
        _owner = owner;
        _id = id;
        Tile = tile;
        Tile.PropertyChanged += TilePropertyChanged;
    }

    private void TilePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Tile.HeaderActionsProperty)
            Populate(_menu);
    }

    public void Update(bool headers)
    {
        Populate(_menu);
        Tile.SetMatrixHost(headers, _menu);
    }

    public void Release()
    {
        Tile.PropertyChanged -= TilePropertyChanged;
        _menu.Hide();
        Tile.SetMatrixHost(null, null);
        if (Tile is DashboardContentTile { IsMatrixManaged: true, ContentId: null } empty)
            empty.ReleaseMatrixContent();
    }

    public MenuItem CreateContentMenu(string label)
    {
        var temporary = new MenuFlyout();
        Populate(temporary, false);
        
        var items = temporary.Items.ToArray();
        temporary.Items.Clear();
        
        var submenu = new MenuItem { Header = label };
        foreach (var item in items)
            submenu.Items.Add(item);
        
        return submenu;
    }

    private void Populate(MenuFlyout menu, bool includeInsertion = true)
    {
        menu.Items.Clear();
        
        var model = _owner.Layout.GetCell(_id);
        if (Tile.HeaderActions is { } custom && Tile is not DashboardContentTile { IsMatrixManaged: true })
        {
            menu.Items.Add(Action(DashboardContentMenu.Text(Tile, "DashboardTileActions", "Tile actions"), () =>
            {
                custom.ShowAt(Tile);
                
                return true;
            }));
        }
        
        var picker = DashboardContentMenu.CreatePicker(Tile, _owner.GetCatalog(), model.ContentId, id => _owner.SetContent(_id, id));
        menu.Items.Add(picker);
        if (includeInsertion)
        {
            menu.Items.Add(new Separator());
            menu.Items.Add(Action(DashboardContentMenu.Text(Tile, "DashboardInsertRowAbove", "Insert row above"), () => _owner.InsertRowBefore(Current().Row),
                                  _owner.CanModifyStructure));
            menu.Items.Add(Action(DashboardContentMenu.Text(Tile, "DashboardInsertRowBelow", "Insert row below"), () => _owner.InsertRowAfter(Current().Row),
                                  _owner.CanModifyStructure));
            menu.Items.Add(Action(DashboardContentMenu.Text(Tile, "DashboardInsertColumnLeft", "Insert column left"), () => _owner.InsertColumnBefore(Current().Column),
                                  _owner.CanModifyStructure));
            menu.Items.Add(Action(DashboardContentMenu.Text(Tile, "DashboardInsertColumnRight", "Insert column right"), () => _owner.InsertColumnAfter(Current().Column),
                                  _owner.CanModifyStructure));
        }
        
        menu.Items.Add(new Separator());
        menu.Items.Add(Action(DashboardContentMenu.Text(Tile, "DashboardClearContent", "Clear content"), () => _owner.ClearContent(_id), model.ContentId is not null));
        menu.Items.Add(Action(DashboardContentMenu.Text(Tile, "DashboardRemoveRow", "Remove row"), () => _owner.RemoveRow(Current().Row),
                              _owner.CanModifyStructure && _owner.Layout.Rows.Count > 1));
        menu.Items.Add(Action(DashboardContentMenu.Text(Tile, "DashboardRemoveColumn", "Remove column"), () => _owner.RemoveColumn(Current().Column),
                              _owner.CanModifyStructure && _owner.Layout.Columns.Count > 1));

        var move = new MenuItem
        {
            Header = DashboardContentMenu.Text(Tile, "DashboardMoveTile", "Move / swap tile"), IsEnabled = model.ContentId is not null && _owner.Layout.Cells.Count > 1
        };
        foreach (var target in _owner.Layout.Cells)
        {
            if (target.Id == _id)
                continue;
            
            var title = $"Row {target.Row + 1}, column {target.Column + 1}";
            move.Items.Add(Action(title, () => _owner.SwapContent(_id, target.Id)));
        }
        
        menu.Items.Add(move);
    }

    private DashboardMatrixCellModel Current() => _owner.Layout.GetCell(_id);

    private MenuItem Action(string title, Func<bool> execute, bool enabled = true) => DashboardContentMenu.Action(Tile, title, execute, enabled);
}
