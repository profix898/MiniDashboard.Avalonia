using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace MiniDashboard.Avalonia.ScottPlot.Themes;

/// <summary>
/// Style collection registering ScottPlot-related theme resources.
/// </summary>
public class ScottPlotStyles : Styles
{
    /// <summary>
    /// Construct and load defined XAML resources.
    /// </summary>
    public ScottPlotStyles()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Loads the associated XAML for this style collection.
    /// </summary>
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this); // Load resource dictionary
    }
}
