using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;
using MiniDashboard.Avalonia.Tests;
using MiniDashboard.Avalonia.Themes;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(TestApp))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MiniDashboard.Avalonia.Tests;

public class TestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new MiniDashboardStyles());
    }
}
