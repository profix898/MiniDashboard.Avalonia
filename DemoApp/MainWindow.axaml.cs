using Avalonia.Controls;
using DemoApp.Models;

namespace DemoApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        FlatGrid = new TreeDataGridFlatModel();
        HierarchicalGrid = new TreeDataGridHierarchicalModel();
        ChartsModel = new ChartsModel();

        DataContext = this;
    }

    public ChartsModel ChartsModel { get; set; }

    public TreeDataGridFlatModel FlatGrid { get; set; }

    public TreeDataGridHierarchicalModel HierarchicalGrid { get; set; }
}
