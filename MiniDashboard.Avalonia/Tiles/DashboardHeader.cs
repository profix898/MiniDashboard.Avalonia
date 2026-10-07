using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MiniDashboard.Avalonia.Content;

namespace MiniDashboard.Avalonia.Tiles;

/// <summary>Shared, compact title and action chrome for tiles and matrix cells.</summary>
public sealed class DashboardHeader : ContentControl
{
    /// <summary>Defines the fallback title.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<DashboardHeader, string?>(nameof(Title));

    /// <summary>Defines the optional action flyout.</summary>
    public static readonly StyledProperty<FlyoutBase?> ActionsProperty =
        AvaloniaProperty.Register<DashboardHeader, FlyoutBase?>(nameof(Actions));

    /// <summary>Defines the displayed custom content or fallback title.</summary>
    public static readonly DirectProperty<DashboardHeader, object?> DisplayContentProperty =
        AvaloniaProperty.RegisterDirect<DashboardHeader, object?>(nameof(DisplayContent), h => h.DisplayContent);

    /// <summary>Defines whether the host placement is valid.</summary>
    public static readonly StyledProperty<bool> IsPlacementValidProperty =
        AvaloniaProperty.Register<DashboardHeader, bool>(nameof(IsPlacementValid), true);

    /// <summary>Gets or sets whether to display normal placement chrome.</summary>
    public bool IsPlacementValid
    {
        get => GetValue(IsPlacementValidProperty);
        set => SetValue(IsPlacementValidProperty, value);
    }

    private object? _displayContent;

    /// <summary>Gets or sets the title used when Content is null.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Gets or sets the action menu; null hides the button.</summary>
    public FlyoutBase? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }

    /// <summary>Gets the displayed custom header or title.</summary>
    public object? DisplayContent => _displayContent;

    private Button? _titleButton;

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_titleButton != null)
            _titleButton.Click -= OnTitleClick;

        base.OnApplyTemplate(e);
        _titleButton = e.NameScope.Find<Button>("PART_TitlePicker");
        if (_titleButton != null)
            _titleButton.Click += OnTitleClick;
        UpdateTitlePicker();
    }

    private void OnTitleClick(object? sender, RoutedEventArgs e)
    {
        var tile = this.GetVisualAncestors().OfType<Tile>().FirstOrDefault();
        if (sender is Control anchor && tile != null)
            tile.GetValue(DashboardContentMenu.TitleActionProperty)?.Invoke(anchor);
    }

    private void UpdateTitlePicker() => PseudoClasses.Set(":title-picker", DashboardContentPicker.GetOpenOnTitleClick(this));

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DashboardContentPicker.OpenOnTitleClickProperty)
            UpdateTitlePicker();
        if (change.Property == TitleProperty || change.Property == ContentProperty)
            SetAndRaise(DisplayContentProperty, ref _displayContent, Content ?? Title);

        if (change.Property == ContentProperty || change.Property == ContentTemplateProperty)
            PseudoClasses.Set(":custom", Content is not null || ContentTemplate is not null);

        if (change.Property == IsPlacementValidProperty)
            PseudoClasses.Set(":invalid", !IsPlacementValid);

        if (change.Property == ActionsProperty)
            PseudoClasses.Set(":actions", Actions is not null);
    }
}
