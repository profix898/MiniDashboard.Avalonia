using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;

namespace MiniDashboard.Avalonia;

// Shared picker and error presentation. Hosts retain their distinct structural menus.
internal static class DashboardContentMenu
{
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
        foreach (var definition in catalog)
            item.Items.Add(Action(owner, definition.Title, () => select(definition.Id)));
        
        item.IsEnabled = item.Items.Count > 0;
        
        return item;
    }

    public static void FillPickerFlyout(MenuFlyout flyout, Control owner, IEnumerable<DashboardContentDefinition> catalog,
                                        Func<string, bool> select)
    {
        flyout.Items.Clear();
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
