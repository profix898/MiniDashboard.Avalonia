# MiniDashboard.Avalonia

Dashboard controls for Avalonia 12 applications.

Install the core package:

```bash
dotnet add package MiniDashboard.Avalonia
```

Register the styles:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:dashboard="clr-namespace:MiniDashboard.Avalonia.Themes;assembly=MiniDashboard.Avalonia">
	<Application.Styles>
		<FluentTheme />
		<dashboard:MiniDashboardStyles />
	</Application.Styles>
</Application>
```

Optional packages:

```bash
dotnet add package MiniDashboard.Avalonia.ScottPlot
dotnet add package MiniDashboard.Avalonia.TreeDataGrid
```

See the GitHub README for full setup, examples, and extension package notes.
