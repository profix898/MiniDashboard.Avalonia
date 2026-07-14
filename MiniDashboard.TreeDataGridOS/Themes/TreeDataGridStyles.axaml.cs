using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace MiniDashboard.Avalonia.TreeDataGrid.Themes;

public class TreeDataGridStyles : Styles
{
    public TreeDataGridStyles()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
