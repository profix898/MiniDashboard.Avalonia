using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace MiniDashboard.Avalonia.Matrix;

// Invisible buttons do not intercept hosted content; keyboard focus can still reveal them.
internal sealed class DashboardMatrixBoundaryButton : Button
{
    private readonly Func<MenuFlyout> _createMenu;
    private bool _hoveredBoundary;
    private bool _touchRevealed;
    private bool _keyboardFocus;

    public bool Horizontal { get; init; }

    public int Boundary { get; init; }

    public int Segment { get; init; }

    public DashboardMatrixBoundaryButton(Func<MenuFlyout> createMenu)
    {
        _createMenu = createMenu;
        RefreshVisibility();
    }

    protected override Type StyleKeyOverride => typeof(Button);

    public void SetBoundaryHover(bool hovered)
    {
        _hoveredBoundary = hovered;
        RefreshVisibility();
    }

    public void RevealTap(bool reveal)
    {
        _touchRevealed = reveal;
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        var show = _hoveredBoundary || _touchRevealed || _keyboardFocus || IsPointerOver || Flyout?.IsOpen == true;
        Opacity = show ? 1 : 0;
        IsHitTestVisible = show;
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        RefreshVisibility();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        RefreshVisibility();
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        _keyboardFocus = e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional;
        RefreshVisibility();
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        _keyboardFocus = false;
        RefreshVisibility();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        _keyboardFocus = true;
        RefreshVisibility();
        base.OnKeyDown(e);
    }

    protected override void OnClick()
    {
        Flyout?.Hide();
        _touchRevealed = false;
        Flyout = _createMenu();
        Flyout.Closed += (_, _) => RefreshVisibility();
        base.OnClick();
        RefreshVisibility();
    }
}
