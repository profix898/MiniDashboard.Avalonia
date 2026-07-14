using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;

namespace MiniDashboard.Avalonia.TreeDataGrid;

public class CsvGridTile : TreeDataGridTile
{
    private int _loadVersion;

    public static readonly StyledProperty<string?> FilePathProperty =
        AvaloniaProperty.Register<CsvGridTile, string?>(nameof(FilePath));

    static CsvGridTile()
    {
        FilePathProperty.Changed.Subscribe(static e => _ = ((CsvGridTile) e.Sender).LoadCsvAsync());
    }

    public string? FilePath
    {
        get { return GetValue(FilePathProperty); }
        set { SetValue(FilePathProperty, value); }
    }

    private async Task LoadCsvAsync()
    {
        var loadVersion = ++_loadVersion;
        if (String.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
        {
            Source = null;
            return;
        }

        try
        {
            var fileLines = await File.ReadAllLinesAsync(FilePath!).ConfigureAwait(true);
            if (loadVersion != _loadVersion)
                return;

            var lines = new List<string[]>();
            foreach (var line in fileLines)
            {
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

            var src = new FlatTreeDataGridSource<Dictionary<string, string>>(obs);
            foreach (var header in headers)
                src.Columns.Add(new TextColumn<Dictionary<string, string>, string>(header, row => row[header]));

            Source = src;
        }
        catch
        {
            if (loadVersion == _loadVersion)
                Source = null;
        }
    }
}
