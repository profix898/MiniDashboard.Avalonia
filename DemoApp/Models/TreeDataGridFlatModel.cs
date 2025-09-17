using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;

namespace DemoApp.Models;

public class PersonFlat
{
    public int Age { get; init; }

    public string City { get; init; } = "";

    public string Name { get; init; } = "";
}

public class TreeDataGridFlatModel
{
    public TreeDataGridFlatModel()
    {
        // Flat table with columns
        var columns = new IColumn<PersonFlat>[]
        {
            new TextColumn<PersonFlat, string>("Name", p => p.Name),
            new TextColumn<PersonFlat, int>("Age", p => p.Age),
            new TextColumn<PersonFlat, string>("City", p => p.City)
        };

        Source = new FlatTreeDataGridSource<PersonFlat>(People);
        Source.Columns.Clear();
        foreach (var c in columns)
            Source.Columns.Add(c);
    }

    public ObservableCollection<PersonFlat> People { get; } =
    [
        new PersonFlat { Name = "Ada", Age = 28, City = "Berlin" },
        new PersonFlat { Name = "Linus", Age = 33, City = "Helsinki" },
        new PersonFlat { Name = "Grace", Age = 45, City = "New York" }
    ];

    public FlatTreeDataGridSource<PersonFlat> Source { get; }
}
