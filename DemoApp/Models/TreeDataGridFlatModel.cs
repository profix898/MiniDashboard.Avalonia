using System.Collections.ObjectModel;
using Avalonia.Controls;

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
        Source = new FlatTreeDataGridSource<PersonFlat>(People);
        Source.WithTextColumn("Name", p => p.Name);
        Source.WithTextColumn("Age", p => p.Age);
        Source.WithTextColumn("City", p => p.City);
    }

    public ObservableCollection<PersonFlat> People { get; } =
    [
        new PersonFlat { Name = "Ada", Age = 28, City = "Berlin" },
        new PersonFlat { Name = "Linus", Age = 33, City = "Helsinki" },
        new PersonFlat { Name = "Grace", Age = 45, City = "New York" }
    ];

    public FlatTreeDataGridSource<PersonFlat> Source { get; }
}
