using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;

namespace MiniDashboard.Avalonia.Picking;

// Shared input and lifecycle policy. Layout adapters only position overlays and supply targets.
internal sealed class DashboardTargetPicker<T>
    where T : class
{
    private readonly Control _host;
    private readonly Panel _overlayHost;
    private readonly List<Button> _buttons = new List<Button>();
    private TaskCompletionSource<T?>? _completion;

    public DashboardTargetPicker(Control host, Panel overlayHost)
    {
        _host = host;
        _overlayHost = overlayHost;
    }

    public bool IsPicking => _completion is not null;

    public void Cancel() => _completion?.TrySetResult(null);

    public async Task<T?> PickAsync(IReadOnlyList<T> targets, Func<T, bool> eligible, Func<T, string> label,
                                    Action<T, Button> place, CancellationToken cancellationToken)
    {
        if (IsPicking)
            throw new InvalidOperationException("A dashboard target selection is already pending.");
        if (cancellationToken.IsCancellationRequested)
            return null;

        var candidates = targets.Where(eligible).ToHashSet();
        if (candidates.Count == 0)
            return null;

        var completion = _completion = new TaskCompletionSource<T?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var focus = TopLevel.GetTopLevel(_host)?.FocusManager?.GetFocusedElement();
        _host.AddHandler(InputElement.KeyDownEvent, KeyDown, RoutingStrategies.Tunnel, true);
        _host.DetachedFromVisualTree += Detached;
        _host.PropertyChanged += HostChanged;

        try
        {
            foreach (var target in targets)
            {
                var allowed = candidates.Contains(target);
                var button = new Button
                {
                    ZIndex = Int32.MaxValue, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center, Focusable = allowed,
                    Content = allowed ? "Select" : "Unavailable", Tag = target
                };

                button.Classes.Set("pick-eligible", allowed);
                button.Bind(TemplatedControl.ThemeProperty, _host.GetResourceObservable("DashboardTargetPickerButtonTheme"));
                button.Bind(TemplatedControl.ForegroundProperty, _host.GetResourceObservable("TileForegroundBrush"));
                AutomationProperties.SetName(button, label(target));
                button.Click += (_, _) =>
                {
                    if (allowed)
                        completion.TrySetResult(target);
                };

                place(target, button);
                _buttons.Add(button);
                _overlayHost.Children.Add(button);
            }

            _buttons.First(b => b.Focusable).Focus();
            using var registration = cancellationToken.Register(() => Dispatcher.UIThread.Post(() =>
            {
                if (ReferenceEquals(_completion, completion))
                    Cancel();
            }));

            return await completion.Task;
        }
        finally
        {
            _host.RemoveHandler(InputElement.KeyDownEvent, KeyDown);
            _host.DetachedFromVisualTree -= Detached;
            _host.PropertyChanged -= HostChanged;

            foreach (var button in _buttons)
                _overlayHost.Children.Remove(button);

            _buttons.Clear();
            _completion = null;
            focus?.Focus();
        }
    }

    private void Detached(object? sender, VisualTreeAttachmentEventArgs e) => Cancel();

    private void HostChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Control.BoundsProperty)
            Cancel();
    }

    private void KeyDown(object? sender, KeyEventArgs e)
    {
        var focused = TopLevel.GetTopLevel(_host)?.FocusManager?.GetFocusedElement();

        if (e.Key == Key.Escape)
        {
            Cancel();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && focused is Button button && _buttons.Contains(button) && button.Tag is T target)
        {
            _completion?.TrySetResult(target);
            e.Handled = true;
        }
        else if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Tab)
        {
            var buttons = _buttons.Where(b => b.Focusable).ToArray();
            var index = Array.FindIndex(buttons, b => ReferenceEquals(b, focused));
            var direction = e.Key is Key.Left or Key.Up || (e.Key == Key.Tab && e.KeyModifiers.HasFlag(KeyModifiers.Shift)) ? -1 : 1;

            buttons[(index + direction + buttons.Length) % buttons.Length].Focus();
            e.Handled = true;
        }
    }
}
