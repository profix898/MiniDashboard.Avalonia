using System;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace MiniDashboard.Avalonia;

/// <summary>
/// Tile that displays an image. Bind either <see cref="ImageSource" /> directly or set a <see cref="SourceUri" />.
/// </summary>
public class ImageTile : Tile
{
    // You can bind either ImageSource or SourceUri.

    /// <summary>
    /// Direct image source (bitmap) to display.
    /// </summary>
    public static readonly StyledProperty<IImage?> ImageSourceProperty =
        AvaloniaProperty.Register<ImageTile, IImage?>(nameof(ImageSource));

    /// <summary>
    /// URI or file path used to load the image when <see cref="ImageSource" /> is not provided.
    /// </summary>
    public static readonly StyledProperty<string?> SourceUriProperty =
        AvaloniaProperty.Register<ImageTile, string?>(nameof(SourceUri));

    /// <summary>
    /// Stretch direction used to render the image.
    /// </summary>
    public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
        AvaloniaProperty.Register<ImageTile, StretchDirection>(nameof(StretchDirection), StretchDirection.Both);

    /// <summary>
    /// Stretch mode used to render the image inside the tile.
    /// </summary>
    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<ImageTile, Stretch>(nameof(Stretch), Stretch.Uniform);

    static ImageTile()
    {
        // Reload image when the SourceUri changes
        SourceUriProperty.Changed.Subscribe(static e => ((ImageTile) e.Sender).LoadFromUri());
    }

    /// <summary>
    /// Gets or sets the image to display.
    /// </summary>
    public IImage? ImageSource
    {
        get { return GetValue(ImageSourceProperty); }
        set { SetValue(ImageSourceProperty, value); }
    }

    /// <summary>
    /// Gets or sets the URI or file path used to load the image.
    /// </summary>
    public string? SourceUri
    {
        get { return GetValue(SourceUriProperty); }
        set { SetValue(SourceUriProperty, value); }
    }

    /// <summary>
    /// Gets or sets the Stretch mode for the image.
    /// </summary>
    public Stretch Stretch
    {
        get { return GetValue(StretchProperty); }
        set { SetValue(StretchProperty, value); }
    }

    /// <summary>
    /// Gets or sets the StretchDirection for the image.
    /// </summary>
    public StretchDirection StretchDirection
    {
        get { return GetValue(StretchDirectionProperty); }
        set { SetValue(StretchDirectionProperty, value); }
    }

    /// <summary>
    /// Try to load the image from <see cref="SourceUri" /> into <see cref="ImageSource" />.
    /// Supports avares:// and resm:// embedded assets and local file paths. HTTP/HTTPS is skipped.
    /// </summary>
    private void LoadFromUri()
    {
        if (String.IsNullOrWhiteSpace(SourceUri))
        {
            // Clear image if no URI provided
            ImageSource = null;
            return;
        }

        try
        {
            var uri = new Uri(SourceUri!, UriKind.RelativeOrAbsolute);

            // avares:// or resm:// embedded assets
            if (uri.IsAbsoluteUri && (uri.Scheme == "avares" || uri.Scheme == "resm"))
            {
                using var s = AssetLoader.Open(uri);
                ImageSource = new Bitmap(s);
                return;
            }

            // file path or absolute file://
            if (!uri.IsAbsoluteUri || uri.Scheme == Uri.UriSchemeFile)
            {
                var path = uri.IsAbsoluteUri ? uri.LocalPath : SourceUri!;
                if (File.Exists(path))
                {
                    // load from local file stream
                    using var fs = File.OpenRead(path);
                    ImageSource = new Bitmap(fs);
                    return;
                }
            }

            // (Optional) http/https: skip by default to avoid network IO in control code.
            ImageSource = null;
        }
        catch
        {
            // on any failure, clear image (best-effort, avoid throwing from control code)
            ImageSource = null;
        }
    }
}
