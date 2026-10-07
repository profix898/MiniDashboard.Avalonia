using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace MiniDashboard.Avalonia.Themes;

/// <summary>
/// Style collection registering the dashboard theme resources.
/// </summary>
public class MiniDashboardStyles : Styles
{
    /// <summary>
    /// Construct and load defined XAML resources.
    /// </summary>
    public MiniDashboardStyles()
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
