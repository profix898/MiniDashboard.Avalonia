using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MiniDashboard.Avalonia.Tiles;

namespace MiniDashboard.Avalonia.Content;

// Shared picker and error presentation. Hosts retain their distinct structural menus.
internal static class DashboardContentMenu
{
    internal static readonly AttachedProperty<Action<Control>?> DirectAddActionProperty =
        AvaloniaProperty.RegisterAttached<Tile, Control, Action<Control>?>("DirectAddAction");

    internal static readonly AttachedProperty<Action<Control>?> TitleActionProperty =
        AvaloniaProperty.RegisterAttached<Tile, Control, Action<Control>?>("TitleAction");

    internal static void ConfigureTitleAction(Tile tile, IEnumerable<DashboardContentDefinition> catalog,
                                              string? selected, Func<string, bool> select, MenuFlyout compactMenu)
    {
        var picker = DashboardContentPicker.GetPicker(tile);
        tile.SetValue(TitleActionProperty, picker != null
                          ? anchor => OpenPicker(tile, anchor, picker, catalog, selected, select)
                          : anchor => compactMenu.ShowAt(anchor));
    }

    private static void OpenPicker(Control owner, Control anchor, IDashboardContentPicker picker,
                                   IEnumerable<DashboardContentDefinition> catalog, string? selected, Func<string, bool> select)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var id = await picker.PickAsync(anchor, catalog.ToArray(), selected);
                if (id is not null && owner.IsAttachedToVisualTree())
                    select(id);
            }
            catch (Exception error)
            {
                new Flyout { Content = new TextBlock { Text = error.Message } }.ShowAt(anchor);
            }
        });
    }

    public static string Text(Control owner, string key, string fallback) => owner.TryFindResource(key, out var value) && value is string text ? text : fallback;

    public static MenuItem CreatePicker(Control owner, IEnumerable<DashboardContentDefinition> catalog,
                                        string? selected, Func<string, bool> select)
    {
        var item = new MenuItem
        {
            Header = selected is null
                ? Text(owner, "DashboardAddContent", "Add content")
                : Text(owner, "DashboardChangeContent", "Change content")
        };
        if (DashboardContentPicker.GetPicker(owner) is { } picker)
        {
            item.Click += (_, _) =>
            {
                var anchor = (Control?) owner.GetVisualDescendants().OfType<Button>()
                                             .FirstOrDefault(button => button.IsVisible && button.Name is "PART_Actions" or "PART_OverlayActions") ?? owner;
                OpenPicker(owner, anchor, picker, catalog, selected, select);
            };
            return item;
        }
        foreach (var definition in catalog)
            item.Items.Add(Action(owner, definition.Title, () => select(definition.Id)));

        item.IsEnabled = item.Items.Count > 0;

        return item;
    }

    public static void FillPickerFlyout(MenuFlyout flyout, Control owner, IEnumerable<DashboardContentDefinition> catalog,
                                        Func<string, bool> select)
    {
        flyout.Items.Clear();
        owner.SetValue(DirectAddActionProperty, null);
        if (DashboardContentPicker.GetPicker(owner) is { } picker)
        {
            var action = CreatePicker(owner, catalog, null, select);
            owner.SetValue(DirectAddActionProperty, anchor => OpenPicker(owner, anchor, picker, catalog, null, select));
            flyout.Items.Add(action);
            return;
        }
        foreach (var definition in catalog)
            flyout.Items.Add(Action(owner, definition.Title, () => select(definition.Id)));

        if (flyout.Items.Count == 0)
            flyout.Items.Add(new MenuItem { Header = Text(owner, "DashboardAddContent", "Add content"), IsEnabled = false });
    }

    public static MenuItem Action(Control owner, string title, Func<bool> execute, bool enabled = true)
    {
        var item = new MenuItem { Header = title, IsEnabled = enabled };
        item.Click += (_, _) =>
        {
            try
            {
                execute();
            }
            catch (Exception error)
            {
                new Flyout { Content = new TextBlock { Text = error.Message, MaxWidth = 320, TextWrapping = TextWrapping.Wrap } }.ShowAt(owner);
            }
        };

        return item;
    }
}
