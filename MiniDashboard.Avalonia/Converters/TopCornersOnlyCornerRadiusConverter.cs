using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace MiniDashboard.Avalonia.Converters;

/// <summary>
/// Takes a CornerRadius and returns a CornerRadius that preserves only the top-left and top-right radii.
/// </summary>
public sealed class TopCornersOnlyCornerRadiusConverter : IValueConverter
{
    /// <summary>
    /// Singleton instance of the converter for use in XAML bindings.
    /// </summary>
    public static TopCornersOnlyCornerRadiusConverter Instance { get; } = new TopCornersOnlyCornerRadiusConverter();

    #region Implementation of IValueConverter

    /// <summary>
    /// Converts a CornerRadius to one that keeps only the top corners.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // If input is CornerRadius preserve top-left/top-right and zero bottom radii
        if (value is CornerRadius cr)
            return new CornerRadius(cr.TopLeft, cr.TopRight, 0, 0);

        // Fallback to empty radius
        return new CornerRadius();
    }

    /// <summary>
    /// ConvertBack is not supported and returns UnsetValue.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return AvaloniaProperty.UnsetValue;
    }

    #endregion
}
