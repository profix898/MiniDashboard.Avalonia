using System.Collections.ObjectModel;
using Avalonia.Controls;

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
        Source = new HierarchicalTreeDataGridSource<Folder>([Root]);
        Source.WithHierarchicalExpanderTextColumn("Folder", f => f.Name, f => f.Children);
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
