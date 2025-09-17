using Avalonia;

namespace MiniDashboard.Avalonia;

/// <summary>
/// Simple tile that displays a single block of text.
/// </summary>
public class TextTile : Tile
{
    /// <summary>
    /// Styled property that holds the tile text content.
    /// </summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<TextTile, string?>(nameof(Text));

    /// <summary>
    /// The text displayed inside the tile.
    /// </summary>
    public string? Text
    {
        get { return GetValue(TextProperty); }
        set { SetValue(TextProperty, value); }
    }
}
