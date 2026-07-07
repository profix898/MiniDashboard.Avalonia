using Avalonia.Controls;
using DemoApp.Models;

namespace DemoApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ChartsModel = new ChartsModel();

        DataContext = this;
    }

    public ChartsModel ChartsModel { get; set; }
}
