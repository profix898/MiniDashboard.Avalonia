using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace MiniDashboard.Avalonia.TreeDataGridOS.Themes;

/// <summary>
/// Style collection registering the TreeDataGrid tile theme resources.
/// </summary>
public class TreeDataGridStyles : Styles
{
    /// <summary>
    /// Construct and load defined XAML resources.
    /// </summary>
    public TreeDataGridStyles()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Loads the associated XAML for this style collection.
    /// </summary>
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
