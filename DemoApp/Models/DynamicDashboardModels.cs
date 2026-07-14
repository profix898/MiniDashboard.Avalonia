using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DemoApp.Models;

public abstract class DynamicTileViewModel : INotifyPropertyChanged
{
    private int _height;
    private string _title = "Tile";
    private int _width;
    private int _x;
    private int _y;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Height
    {
        get { return _height; }
        set { SetField(ref _height, value); }
    }

    public string Title
    {
        get { return _title; }
        set { SetField(ref _title, value); }
    }

    public int Width
    {
        get { return _width; }
        set { SetField(ref _width, value); }
    }

    public int X
    {
        get { return _x; }
        set { SetField(ref _x, value); }
    }

    public int Y
    {
        get { return _y; }
        set { SetField(ref _y, value); }
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public sealed class DynamicTextTileViewModel : DynamicTileViewModel
{
    private string _text = "Generated from ItemsSource";

    public string Text
    {
        get { return _text; }
        set { SetField(ref _text, value); }
    }
}

public sealed class DynamicTableTileViewModel : DynamicTileViewModel
{
    public IReadOnlyList<DashboardTableRow> Rows { get; init; } = [];
}

public sealed class DynamicScatterTileViewModel : DynamicTileViewModel
{
    public IReadOnlyList<double> Series { get; init; } = [];
}

public sealed class DynamicServicesTileViewModel : DynamicTileViewModel
{
    public IReadOnlyList<DashboardTableRow> Rows { get; init; } = [];
}

public sealed class DerivedMetricTileViewModel : DynamicTileViewModel
{
    public IReadOnlyList<double> Series { get; init; } = [];
}