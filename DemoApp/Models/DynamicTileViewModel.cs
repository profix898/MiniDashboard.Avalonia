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
