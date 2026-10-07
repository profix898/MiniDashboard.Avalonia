using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using MiniDashboard.Avalonia.Content;
using MiniDashboard.Avalonia.Matrix;

namespace DemoApp;

public partial class MatrixDemo : UserControl, IDisposable
{
    private string? _saved;

    public MatrixDemo()
    {
        InitializeComponent();
        Matrix.ContentDefinitions = new[]
        {
            new DashboardContentDefinition
            {
                Id = "notes", Title = "Editable notes",
                Factory = () => new TextBox
                {
                    Text = "Your notes stay intact when rows or columns are inserted, or content is swapped.", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap
                }
            },
            new DashboardContentDefinition
            {
                Id = "status", Title = "System status",
                Factory = () => new StackPanel
                {
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock { Text = "System status", FontSize = 20 }, new TextBlock { Text = "CPU utilization" }, new ProgressBar { Value = 42 },
                        new TextBlock { Text = "Drag a divider to resize the whole row or column." }
                    }
                }
            },
            new DashboardContentDefinition
            {
                Id = "events", Title = "Scrollable event log",
                Factory = () => new ListBox
                {
                    ItemsSource = Enumerable.Range(1, 100).Select(i => $"Event {i:000}: service heartbeat").ToArray(),
                    ContextMenu = new ContextMenu { Items = { new MenuItem { Header = "Hosted content menu" } } }
                }
            }
        };
        Matrix.LayoutChanging += (_, e) =>
        {
            if (ProtectContent.IsChecked == true &&
                e.Kind is DashboardMatrixChangeKind.RemoveRow or DashboardMatrixChangeKind.RemoveColumn &&
                e.AffectedCells.Any(c => c.ContentId is not null))
            {
                e.Cancel = true;
                Feedback.Text = "Removal cancelled by the host. Uncheck protection and retry to confirm.";
            }
        };
        Matrix.LayoutChanged += (_, _) => UpdateSummary();
        Reset();
    }

    public void Dispose()
    {
        Matrix.Dispose();
        GC.SuppressFinalize(this);
    }

    private void Reset()
    {
        var layout = new DashboardMatrixLayout(new[] { .6, .4 }, new[] { .28, .42, .3 }, null);
        layout = layout.WithContent(layout.Cells[0].Id, "notes")
                       .WithContent(layout.Cells[1].Id, "events")
                       .WithContent(layout.Cells[3].Id, "status");
        Matrix.RestoreLayout(layout);
        Feedback.Text = "The matrix owns fresh content instances from the supplied catalog.";
    }

    private void UpdateSummary()
    {
        var layout = Matrix.Layout;

        static string Weights(IReadOnlyList<double> values) => String.Join(", ", values.Select(v => (v / values.Sum()).ToString("P0")));

        Summary.Text = $"{layout.Rows.Count} × {layout.Columns.Count} | Rows: {Weights(layout.Rows)} | Columns: {Weights(layout.Columns)} | " +
                       String.Join(" · ", layout.Cells.Select(c => $"({c.Row},{c.Column}): {c.ContentId ?? "empty"}"));
    }

    private void Reset_Click(object? sender, RoutedEventArgs e) => Reset();

    private void SearchPicker_Changed(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkbox || Matrix == null)
            return;

        DashboardContentPicker.SetPicker(Matrix, checkbox.IsChecked == true ? new DashboardContentPicker { GroupByCategory = true } : null);
        Feedback.Text = checkbox.IsChecked == true
            ? "Searchable content picker enabled: use '+ Add content' or a cell menu's 'Change content'."
            : "Compact content menu enabled: use '+ Add content' or a cell menu's 'Change content'.";
    }

    private async void Pick_Click(object? sender, RoutedEventArgs e)
    {
        if (Matrix.IsPickingCell)
            return;

        Feedback.Text = "Choose a populated cell; Escape cancels without interacting with its content.";
        var id = await Matrix.PickCellAsync((cell, _) => cell.ContentId is not null);
        Feedback.Text = id.HasValue ? $"Selected {Matrix.Layout.GetCell(id.Value).ContentId}" : "Selection cancelled.";
    }

    private void Swap_Click(object? sender, RoutedEventArgs e) => Matrix.SwapContent(Matrix.Layout.Cells[0].Id, Matrix.Layout.Cells[^1].Id);

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        _saved = JsonSerializer.Serialize(Matrix.GetSnapshot());
        Feedback.Text = "Saved an in-memory JSON snapshot of IDs and proportions. Hosted control state is not included.";
    }

    private void Restore_Click(object? sender, RoutedEventArgs e)
    {
        if (_saved is null)
        {
            Feedback.Text = "Save a snapshot first.";

            return;
        }

        Matrix.RestoreLayout(JsonSerializer.Deserialize<DashboardMatrixLayout>(_saved)!);
        Feedback.Text = "Restored the saved matrix snapshot.";
    }
}
