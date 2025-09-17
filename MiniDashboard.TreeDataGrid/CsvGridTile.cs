using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;

namespace MiniDashboard.Avalonia.TreeDataGrid;

public class CsvGridTile : TreeDataGridTile
{
    public static readonly StyledProperty<string?> FilePathProperty =
        AvaloniaProperty.Register<CsvGridTile, string?>(nameof(FilePath));

    static CsvGridTile()
    {
        FilePathProperty.Changed.Subscribe(static e => ((CsvGridTile) e.Sender).LoadCsv());
    }

    public string? FilePath
    {
        get { return GetValue(FilePathProperty); }
        set { SetValue(FilePathProperty, value); }
    }

    private void LoadCsv()
    {
        if (String.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
        {
            Source = null;
            return;
        }

        try
        {
            using var reader = new StreamReader(FilePath!);
            var lines = new List<string[]>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line is null)
                    continue;
                var parts = line.Split(','); // simple split, no quoting support
                lines.Add(parts);
            }

            if (lines.Count < 1)
            {
                Source = null;
                return;
            }

            var headers = lines[0];
            var rows = lines.Skip(1)
                            .Select(parts =>
                            {
                                var dict = new Dictionary<string, string>();
                                for (var i = 0; i < headers.Length; i++)
                                    dict[headers[i]] = i < parts.Length ? parts[i] : "";
                                return dict;
                            })
                            .ToList();

            var obs = new ObservableCollection<Dictionary<string, string>>(rows);

            // Create columns as the generic IColumn<T> type so they can be added to the source's Columns.
            var columns = headers
                          .Select(h => (IColumn<Dictionary<string, string>>)
                                      new TextColumn<Dictionary<string, string>, string>(h, r => r.GetValueOrDefault(h, "")))
                          .ToList();

            var src = new FlatTreeDataGridSource<Dictionary<string, string>>(obs);

            // Columns is read-only; populate the ColumnList<T> collection instead of assigning it.
            src.Columns.Clear();
            foreach (var c in columns)
                src.Columns.Add(c);

            Source = src;
        }
        catch
        {
            Source = null;
        }
    }
}
