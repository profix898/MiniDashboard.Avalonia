using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace MiniDashboard.Avalonia;

/// <summary>
/// Behavior that enables dragging of controls (tiles) inside a DashboardPanel via attached property.
/// </summary>
public class DraggableBehavior
{
    // Per-control drag state stored as an attached property
    private static readonly AttachedProperty<DragState?> DragStateProperty =
        AvaloniaProperty.RegisterAttached<DraggableBehavior, Control, DragState?>("DragState");

    /// <summary>
    /// Attached property that enables or disables dragging on a control.
    /// </summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<DraggableBehavior, Control, bool>("IsEnabled");

    static DraggableBehavior()
    {
        // When IsEnabled changes, attach or detach pointer handlers on the control
        IsEnabledProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is Control control)
            {
                if (args.NewValue.Value)
                {
                    control.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel);
                    control.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel);
                    control.AddHandler(InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel);
                }
                else
                {
                    control.RemoveHandler(InputElement.PointerPressedEvent, OnPressed);
                    control.RemoveHandler(InputElement.PointerMovedEvent, OnMoved);
                    control.RemoveHandler(InputElement.PointerReleasedEvent, OnReleased);
                }
            }
        });
    }

    /// <summary>
    /// Gets whether dragging is enabled for the provided control.
    /// </summary>
    public static bool GetIsEnabled(Control control)
    {
        return control.GetValue(IsEnabledProperty);
    }

    /// <summary>
    /// Sets whether dragging is enabled for the provided control.
    /// </summary>
    public static void SetIsEnabled(Control control, bool isEnabled)
    {
        control.SetValue(IsEnabledProperty, isEnabled);
    }

    #region Handlers

    // Cancel an active drag and clean up handlers/capture
    private static void CancelDrag(Control tile, DragState state, bool markInvalid)
    {
        if (!state.dragging)
            return;

        state.dragging = false;

        try
        {
            if (state.capturedPointer is not null)
            {
                // Release capture (Capture(null) releases)
                state.capturedPointer.Capture(null);
            }
        }
        catch
        {
            // Ignore
        }
        finally
        {
            state.capturedPointer = null;
        }

        // Remove any panel handlers we added
        if (state.panel is not null)
        {
            if (state.panelMovedHandler is not null)
                state.panel.RemoveHandler(InputElement.PointerMovedEvent, state.panelMovedHandler);
            if (state.panelReleasedHandler is not null)
                state.panel.RemoveHandler(InputElement.PointerReleasedEvent, state.panelReleasedHandler);
        }

        // Remove capture-lost handler from tile and panel (best-effort)
        if (state.captureLostHandler is not null)
        {
            try
            {
                tile.RemoveHandler(InputElement.PointerCaptureLostEvent, state.captureLostHandler);
            }
            catch
            {
                // Ignore
            }
            try
            {
                state.panel?.RemoveHandler(InputElement.PointerCaptureLostEvent, state.captureLostHandler);
            }
            catch
            {
                // Ignore
            }
            state.captureLostHandler = null;
        }

        // Hide preview and mark placement invalid if requested
        state.panel?.HideSnapPreview();
        if (markInvalid && tile is Tile tb)
            tb.IsPlacementValid = false;

        // Clear panel reference but keep the DragState attached (creates new on next drag)
        state.panel = null;
    }

    // Pointer pressed: begin drag if left button and not originating from an interactive tile control
    private static void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control source)
            return;
        if (!e.GetCurrentPoint(source).Properties.IsLeftButtonPressed)
            return;

        // Ignore clicks that originate on interactive controls (buttons, inputs, etc.)
        // This prevents interactive header controls from starting tile drag / preview.
        if (e.Source is Visual srcVisual)
        {
            // Build sequence: the original source control first, then its visual ancestors
            var controlsSeq = srcVisual.GetVisualAncestors().OfType<Control>().ToList();
            if (srcVisual is Control srcAsControl)
                controlsSeq.Insert(0, srcAsControl);

            // Common interactive control types to ignore
            if (controlsSeq.Any(c => c is Button || c is ToggleButton || c is TextBox || c is ComboBox || c is Slider || c is ScrollBar || c is RepeatButton))
                return;
        }

        var panel = source.GetVisualAncestors().OfType<DashboardPanel>().FirstOrDefault();
        if (panel is null)
            return;

        // If the clicked element is a sub-element (e.g. header) of the tile, find the actual tile
        // which is the direct tile of the panel. Use TakeWhile/LastOrDefault to avoid accessing VisualParent.
        var tile = source.GetVisualAncestors().OfType<Control>().TakeWhile(c => c != panel).LastOrDefault() ?? source;

        var state = GetOrCreateState(tile);
        state.dragging = true;
        state.panel = panel;
        state.startPointer = e.GetPosition(panel);
        state.startCell = (DashboardPanel.GetX(tile), DashboardPanel.GetY(tile));

        // Track pointer and attempt capture: try the tile first, then panel
        state.capturedPointer = e.Pointer;

        if (state.capturedPointer is not null)
        {
            try
            {
                state.capturedPointer.Capture(tile);
            }
            catch
            {
                try
                {
                    state.capturedPointer.Capture(panel);
                }
                catch
                {
                    // Give up capturing - continue without tracked capture
                    state.capturedPointer = null;
                }
            }
        }

        // Add capture-lost handler so we cancel if capture is stolen/lost
        state.captureLostHandler = (_, _) =>
        {
            // Ensure we cancel the specific control's drag
            CancelDrag(tile, state, true);
        };

        // Register on tile and panel (best-effort)
        tile.AddHandler(InputElement.PointerCaptureLostEvent, state.captureLostHandler, RoutingStrategies.Tunnel);
        panel.AddHandler(InputElement.PointerCaptureLostEvent, state.captureLostHandler, RoutingStrategies.Tunnel);

        // Ensure we still receive pointer events while dragging even if the panel is the capture target:
        state.panelMovedHandler = (_, args) => OnMoved(tile, args);
        state.panelReleasedHandler = (_, args) => OnReleased(tile, args);
        panel.AddHandler(InputElement.PointerMovedEvent, state.panelMovedHandler, RoutingStrategies.Tunnel);
        panel.AddHandler(InputElement.PointerReleasedEvent, state.panelReleasedHandler, RoutingStrategies.Tunnel);

        // Start preview at current spot using the tile's size
        var w = Math.Max(1, DashboardPanel.GetW(tile));
        var h = Math.Max(1, DashboardPanel.GetH(tile));
        panel.ShowSnapPreview(state.startCell.x, state.startCell.y, w, h, true);

        if (tile is Tile tb)
            tb.IsPlacementValid = true;

        e.Handled = true;
    }

    // Pointer moved: update preview and validity while dragging
    private static void OnMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Control source)
            return;
        var state = source.GetValue(DragStateProperty);
        if (state is null || !state.dragging || state.panel is null)
            return;

        // Cancel if pointer left panel bounds
        var posOnPanel = e.GetPosition(state.panel);
        if (posOnPanel.X < 0 || posOnPanel.Y < 0 || posOnPanel.X > state.panel.Bounds.Width || posOnPanel.Y > state.panel.Bounds.Height)
        {
            CancelDrag(source, state, true);
            e.Handled = true;
            return;
        }

        var dx = posOnPanel.X - state.startPointer.X;
        var dy = posOnPanel.Y - state.startPointer.Y;

        var (cw, ch) = state.panel.GetCellSize(state.panel.Bounds.Size);
        var targetX = state.startCell.x + (int) Math.Round(dx / cw);
        var targetY = state.startCell.y + (int) Math.Round(dy / ch);

        if (state.panel.TryResolveMove(source, targetX, targetY, out var nx, out var ny))
        {
            state.panel.ShowSnapPreview(nx, ny, Math.Max(1, DashboardPanel.GetW(source)), Math.Max(1, DashboardPanel.GetH(source)), true);
            if (source is Tile tb)
                tb.IsPlacementValid = true;
        }
        else
        {
            state.panel.ShowSnapPreview(nx, ny, Math.Max(1, DashboardPanel.GetW(source)), Math.Max(1, DashboardPanel.GetH(source)), false);
            if (source is Tile tb)
                tb.IsPlacementValid = false;
        }

        e.Handled = true;
    }

    // Pointer released: finalize move and clean up handlers/capture
    private static void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Control source)
            return;
        var state = source.GetValue(DragStateProperty);
        if (state is null || !state.dragging || state.panel is null)
            return;

        // Compute final desired position before cleanup
        var pos = e.GetPosition(state.panel);
        var dx = pos.X - state.startPointer.X;
        var dy = pos.Y - state.startPointer.Y;

        var (cw, ch) = state.panel.GetCellSize(state.panel.Bounds.Size);
        var targetX = state.startCell.x + (int) Math.Round(dx / cw);
        var targetY = state.startCell.y + (int) Math.Round(dy / ch);

        var panel = state.panel; // Capture to avoid null after CancelDrag

        // Release capture & remove handlers
        CancelDrag(source, state, false);

        if (panel.TryResolveMove(source, targetX, targetY, out var nx, out var ny))
        {
            DashboardPanel.SetX(source, nx);
            DashboardPanel.SetY(source, ny);

            // Force panel to re-layout immediately so the tile moves right away
            panel.InvalidateArrange();

            if (source is Tile tb)
                tb.IsPlacementValid = true;
        }
        else
        {
            if (source is Tile tb)
                tb.IsPlacementValid = false;
        }

        e.Handled = true;
    }

    #endregion

    #region State

    // Get or create the DragState attached to a control
    private static DragState GetOrCreateState(Control control)
    {
        var s = control.GetValue(DragStateProperty);
        if (s is null)
        {
            s = new DragState();
            control.SetValue(DragStateProperty, s);
        }
        return s;
    }

    // Clear the attached drag state
    private static void ClearState(Control control)
    {
        control.SetValue(DragStateProperty, null);
    }

    #region Nested Type: DragState

    // Drag state per-control (keeps multiple concurrent drags separate)
    private sealed class DragState
    {
        public IPointer? capturedPointer;
        public EventHandler<PointerCaptureLostEventArgs>? captureLostHandler;
        public bool dragging;
        public DashboardPanel? panel;

        // Delegates we register on the panel so we can remove them later
        public EventHandler<PointerEventArgs>? panelMovedHandler;
        public EventHandler<PointerReleasedEventArgs>? panelReleasedHandler;
        public (int x, int y) startCell;
        public Point startPointer;
    }

    #endregion

    #endregion
}
