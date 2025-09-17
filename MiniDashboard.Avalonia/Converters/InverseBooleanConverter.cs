using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace MiniDashboard.Avalonia.Converters;

/// <summary>
/// Converter that inverts boolean values for data binding.
/// Returns AvaloniaProperty.UnsetValue when input is not a boolean.
/// </summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    #region Implementation of IValueConverter

    /// <summary>
    /// Converts a boolean value to its inverse.
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // If input is a bool, return its inverse; otherwise signal no value
        if (value is bool b)
            return !b;
        return AvaloniaProperty.UnsetValue;
    }

    /// <summary>
    /// Converts back a boolean value to its inverse.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Behave symmetrically: invert boolean on ConvertBack as well
        if (value is bool b)
            return !b;
        return AvaloniaProperty.UnsetValue;
    }

    #endregion
}
