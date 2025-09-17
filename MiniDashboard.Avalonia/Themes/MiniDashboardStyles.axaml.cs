using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace MiniDashboard.Avalonia.Themes;

public class MiniDashboardStyles : Styles
{
    public MiniDashboardStyles()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
