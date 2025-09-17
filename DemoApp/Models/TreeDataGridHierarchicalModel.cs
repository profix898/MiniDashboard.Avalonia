using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;

namespace DemoApp.Models;

public class Folder
{
    public ObservableCollection<Folder> Children { get; } = [];

    public string Name { get; init; } = "";
}

public class TreeDataGridHierarchicalModel
{
    public TreeDataGridHierarchicalModel()
    {
        var columns = new IColumn<Folder>[] { new HierarchicalExpanderColumn<Folder>(new TextColumn<Folder, string>("Folder", f => f.Name), f => f.Children) };

        Source = new HierarchicalTreeDataGridSource<Folder>([Root]);
        Source.Columns.Clear();
        foreach (var c in columns)
            Source.Columns.Add(c);
    }

    public Folder Root { get; } = new Folder
    {
        Name = "Root",
        Children =
        {
            new Folder { Name = "Docs", Children = { new Folder { Name = "Specs" }, new Folder { Name = "Readme" } } }, new Folder { Name = "Assets" }
        }
    };

    public HierarchicalTreeDataGridSource<Folder> Source { get; }
}
