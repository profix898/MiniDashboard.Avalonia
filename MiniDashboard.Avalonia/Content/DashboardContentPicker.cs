using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace MiniDashboard.Avalonia.Content;

/// <summary>Shared picker configuration and a searchable default implementation.</summary>
public sealed class DashboardContentPicker : AvaloniaObject, IDashboardContentPicker
{
    /// <summary>Defines an inherited picker override. Null retains the compact menu picker.</summary>
    public static readonly AttachedProperty<IDashboardContentPicker?> PickerProperty =
        AvaloniaProperty.RegisterAttached<DashboardContentPicker, Control, IDashboardContentPicker?>("Picker", inherits: true);

    /// <summary>Enables a clickable plain header title that opens content selection. Defaults to false; custom headers retain their own input.</summary>
    public static readonly AttachedProperty<bool> OpenOnTitleClickProperty =
        AvaloniaProperty.RegisterAttached<DashboardContentPicker, Control, bool>("OpenOnTitleClick", inherits: true);

    /// <summary>Gets whether plain header titles open the content chooser.</summary>
    public static bool GetOpenOnTitleClick(Control control) => control.GetValue(OpenOnTitleClickProperty);

    /// <summary>Sets whether plain header titles open the content chooser on a dashboard or content tile.</summary>
    public static void SetOpenOnTitleClick(Control control, bool enabled) => control.SetValue(OpenOnTitleClickProperty, enabled);

    /// <summary>Gets or sets whether matching definitions appear under category headings.</summary>
    public bool GroupByCategory { get; set; }

    /// <summary>Gets a picker override.</summary>
    public static IDashboardContentPicker? GetPicker(Control control) => control.GetValue(PickerProperty);

    /// <summary>Sets a picker override on a dashboard or content tile; null restores compact menus.</summary>
    /// <remarks>The setting inherits through tile grids and is propagated to matrix cells.
    /// Assignment refreshes content-selection menus without recreating hosted content.</remarks>
    public static void SetPicker(Control control, IDashboardContentPicker? picker) => control.SetValue(PickerProperty, picker);

    /// <inheritdoc />
    public async Task<string?> PickAsync(Control anchor, IReadOnlyList<DashboardContentDefinition> definitions, string? currentId)
    {
        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var search = new TextBox { PlaceholderText = "Search content", MinWidth = 280 };
        var list = new ListBox { MaxHeight = 360, MinHeight = 80 };

        void Filter()
        {
            var text = search.Text ?? String.Empty;
            var filtered = definitions.Where(d => $"{d.Title} {d.Category} {d.Description}".Contains(text, StringComparison.OrdinalIgnoreCase))
                                      .OrderBy(d => d.Category).ThenBy(d => d.Title).ToArray();

            var items = new List<ListBoxItem>();
            foreach (var group in filtered.GroupBy(d => GroupByCategory ? d.Category ?? String.Empty : String.Empty))
            {
                if (GroupByCategory && !String.IsNullOrWhiteSpace(group.Key))
                    items.Add(new ListBoxItem { Content = new TextBlock { Text = group.Key, FontWeight = FontWeight.SemiBold }, IsEnabled = false, Focusable = false });
                foreach (var definition in group)
                {
                    items.Add(new ListBoxItem
                    {
                        Content = GroupByCategory || String.IsNullOrEmpty(definition.Category) ? definition.Title : $"{definition.Category} · {definition.Title}",
                        Tag = definition.Id
                    });
                }
            }

            list.ItemsSource = items;
            list.SelectedItem = items.FirstOrDefault(item => Equals(item.Tag, currentId));
        }

        Filter();
        search.TextChanged += (_, _) => Filter();

        var choose = new Button { Content = "Select", HorizontalAlignment = HorizontalAlignment.Right };
        var flyout = new Flyout { Placement = PlacementMode.Bottom, Content = new StackPanel { Spacing = 8, Children = { search, list, choose } } };

        var previousFlyout = FlyoutBase.GetAttachedFlyout(anchor);
        FlyoutBase.SetAttachedFlyout(anchor, flyout);

        choose.Click += (_, _) =>
        {
            if (list.SelectedItem is ListBoxItem { Tag: string id })
                completion.TrySetResult(id);
        };

        flyout.Closed += (_, _) => completion.TrySetResult(null);
        flyout.Opened += (_, _) => Dispatcher.UIThread.Post(() => search.Focus());
        list.DoubleTapped += (_, _) =>
        {
            if (list.SelectedItem is ListBoxItem { Tag: string id })
                completion.TrySetResult(id);
        };

        // Accept only an explicit item tap, not keyboard selection or initial/current-item restoration.
        list.AddHandler(InputElement.TappedEvent, (_, e) =>
        {
            var item = (e.Source as Visual)?.GetVisualAncestors().OfType<ListBoxItem>().FirstOrDefault();
            if (e.Source is ListBoxItem sourceItem)
                item = sourceItem;
            if (item is { IsEnabled: true, Tag: string id })
                completion.TrySetResult(id);
        }, RoutingStrategies.Bubble, true);

        list.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && list.SelectedItem is ListBoxItem { Tag: string id })
            {
                completion.TrySetResult(id);
                e.Handled = true;
            }
        };

        anchor.DetachedFromVisualTree += Detached;

        void Detached(object? sender, VisualTreeAttachmentEventArgs e) => completion.TrySetResult(null);

        flyout.ShowAt(anchor);

        try
        {
            return await completion.Task;
        }
        finally
        {
            flyout.Hide();

            if (ReferenceEquals(FlyoutBase.GetAttachedFlyout(anchor), flyout))
                FlyoutBase.SetAttachedFlyout(anchor, previousFlyout);

            anchor.DetachedFromVisualTree -= Detached;
        }
    }
}
