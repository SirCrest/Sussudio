using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Sussudio.Services.Automation;
using Windows.Foundation;
using Windows.System;

namespace Sussudio.Controllers;

// What the live-tree walk learned about one element before deciding whether to emit it (cheap properties plus a
// peer-based interactivity probe).
internal sealed class UiElementProbe
{
    public required UIElement Element { get; init; }
    public required string Path { get; init; }
    public required string TypeName { get; init; }
    public string? Name { get; init; }
    public string? AutomationId { get; init; }
    public string? AutomationName { get; init; }
    public string? Text { get; init; }
    public required bool IsEnabled { get; init; }
    public string? DisabledReason { get; init; }
    public required bool IsVisible { get; init; }
    public string? HiddenReason { get; init; }
    public required bool Interactive { get; init; }

    public UiTargetCandidate ToCandidate()
        => new(Path, TypeName, Name, AutomationId, AutomationName, Text, IsEnabled, DisabledReason, IsVisible, HiddenReason, Interactive);
}

// Reads WinUI elements into the dependency-free UI automation models. UI thread only. Every read that goes through an
// automation peer is wrapped: a peer can throw ElementNotAvailable/COM errors while its element is being torn down, and a
// failed read must cost that field, not the whole tree.
internal static class UiElementReader
{
    private const int MaxTextLength = 200;
    private const int MaxTextNodes = 64;

    private static readonly PatternInterface[] InteractivePatterns =
    {
        PatternInterface.Invoke,
        PatternInterface.Toggle,
        PatternInterface.Value,
        PatternInterface.RangeValue,
        PatternInterface.SelectionItem,
        PatternInterface.Selection,
        PatternInterface.ExpandCollapse,
    };

    public static string TypeNameOf(UIElement element) => element.GetType().Name;

    public static string LabelOf(UIElement element)
    {
        var name = (element as FrameworkElement)?.Name;
        return string.IsNullOrEmpty(name) ? TypeNameOf(element) : $"{TypeNameOf(element)}#{name}";
    }

    public static string SegmentOf(UIElement element, int childIndex)
    {
        var name = (element as FrameworkElement)?.Name;
        return string.IsNullOrEmpty(name)
            ? $"{TypeNameOf(element)}[{childIndex}]"
            : $"{TypeNameOf(element)}#{name}[{childIndex}]";
    }

    public static UiVisibilityLayer LayerOf(UIElement element)
    {
        var frameworkElement = element as FrameworkElement;
        return new UiVisibilityLayer(
            LabelOf(element),
            element.Visibility == Visibility.Collapsed,
            element.Opacity,
            frameworkElement?.ActualWidth ?? 1,
            frameworkElement?.ActualHeight ?? 1);
    }

    // The root-most disabled control at or above the element's parent chain, as a label; null while every ancestor is enabled.
    public static string? DisabledByIncluding(UIElement element, string? disabledBy)
        => disabledBy ?? (element is Control { IsEnabled: false } ? LabelOf(element) : null);

    public static UiElementProbe Probe(UIElement element, string path, IReadOnlyList<UiVisibilityLayer> chain, string? disabledBy)
    {
        var enabledOwn = element is not Control control || control.IsEnabled;
        var isEnabled = disabledBy is null && enabledOwn;
        var visibility = UiVisibilityRules.Evaluate(chain);
        return new UiElementProbe
        {
            Element = element,
            Path = path,
            TypeName = TypeNameOf(element),
            Name = NullIfEmpty((element as FrameworkElement)?.Name),
            AutomationId = NullIfEmpty(AutomationProperties.GetAutomationId(element)),
            AutomationName = NullIfEmpty(AutomationProperties.GetName(element)),
            Text = ExtractText(element),
            IsEnabled = isEnabled,
            DisabledReason = isEnabled ? null : disabledBy is not null ? $"ancestor {disabledBy} is disabled" : "IsEnabled is false",
            IsVisible = visibility.IsVisible,
            HiddenReason = visibility.Reason,
            Interactive = IsInteractive(element),
        };
    }

    // Layers from the top of the element's own tree down to and including the element, for re-reading one element
    // outside a walk.
    public static (List<UiVisibilityLayer> Chain, string? DisabledBy) ChainOf(UIElement element)
    {
        var ancestors = new List<UIElement>();
        var current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(element);
        while (current is not null && ancestors.Count < 256)
        {
            if (current is UIElement ancestor)
            {
                ancestors.Add(ancestor);
            }

            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }

        ancestors.Reverse();
        string? disabledBy = null;
        var chain = new List<UiVisibilityLayer>(ancestors.Count + 1);
        foreach (var ancestor in ancestors)
        {
            chain.Add(LayerOf(ancestor));
            disabledBy = DisabledByIncluding(ancestor, disabledBy);
        }

        chain.Add(LayerOf(element));
        return (chain, disabledBy);
    }

    public static UiElementProbe ProbeStandalone(UIElement element, string path)
    {
        var (chain, disabledBy) = ChainOf(element);
        return Probe(element, path, chain, disabledBy);
    }

    public static bool IsInteractive(UIElement element)
    {
        if (IsInert(element))
        {
            return false;
        }

        // A realized list/combo item is operable through its owning selector even though its own peer carries no pattern.
        if (OwnerOf(element) is not null)
        {
            return true;
        }

        var peer = TryCreatePeer(element);
        if (peer is null)
        {
            return false;
        }

        foreach (var pattern in InteractivePatterns)
        {
            if (TryGetPattern(peer, pattern) is not null)
            {
                return true;
            }
        }

        return false;
    }

    // The owning selector and the item's index for a realized item container (ComboBoxItem, ListViewItem, ...). In WinUI
    // the SelectionItem pattern lives on the item-data peer the selector's own peer creates, not on the container's
    // peer: a container read through CreatePeerForElement reports no patterns at all (observed live on the Flashback
    // buffer combo's open dropdown). Selecting such a container therefore goes through its owner.
    public static (Selector Owner, int Index)? OwnerOf(UIElement element)
    {
        if (element is not SelectorItem item)
        {
            return null;
        }

        try
        {
            if (ItemsControl.ItemsControlFromItemContainer(item) is not Selector owner)
            {
                return null;
            }

            var index = owner.IndexFromContainer(item);
            return index >= 0 ? (owner, index) : null;
        }
        catch (Exception ex) when (IsPeerReadFailure(ex))
        {
            return null;
        }
    }

    // The item-data peer of one item, which carries the SelectionItem pattern and selects the item through the selector
    // whether or not a container exists. The selector's peer lists one ItemAutomationPeer per item; duplicates (equal
    // items) are told apart by their order. Null provider with the reason when the peer is not reachable this way.
    public static (ISelectionItemProvider? Provider, UiItemPeerLookup Lookup, string? Error) FindItemDataPeer(Selector selector, int index)
    {
        try
        {
            var ownerPeer = TryCreatePeer(selector);
            var children = ownerPeer?.GetChildren();
            if (children is null)
            {
                return (null, UiItemPeerLookup.NotFound, null);
            }

            var item = selector.Items[index];
            var occurrence = 0;
            for (var earlier = 0; earlier < index; earlier++)
            {
                if (Equals(selector.Items[earlier], item))
                {
                    occurrence++;
                }
            }

            var seen = 0;
            foreach (var child in children)
            {
                if (child is ItemAutomationPeer itemPeer && Equals(itemPeer.Item, item) && seen++ == occurrence)
                {
                    return TryGetPattern(itemPeer, PatternInterface.SelectionItem) is ISelectionItemProvider provider
                        ? (provider, UiItemPeerLookup.NotFound, null)
                        : (null, UiItemPeerLookup.NotFound, null);
                }
            }

            return (null, UiItemPeerLookup.NotFound, null);
        }
        catch (Exception ex) when (IsPeerReadFailure(ex))
        {
            return (null, UiItemPeerLookup.Failed, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    // Types whose peers carry no operable pattern of their own; skipping them keeps a full-tree walk from creating a peer
    // for every border and text run.
    private static bool IsInert(UIElement element)
        => element is Panel
            or Border
            or ContentPresenter
            or TextBlock
            or Microsoft.UI.Xaml.Shapes.Shape
            or Image
            or IconElement
            or ItemsPresenter
            or ScrollContentPresenter
            or ScrollViewer;

    public static AutomationPeer? TryCreatePeer(UIElement element)
    {
        try
        {
            return FrameworkElementAutomationPeer.CreatePeerForElement(element);
        }
        catch (Exception ex) when (IsPeerReadFailure(ex))
        {
            return null;
        }
    }

    public static object? TryGetPattern(AutomationPeer peer, PatternInterface pattern)
    {
        try
        {
            return peer.GetPattern(pattern);
        }
        catch (Exception ex) when (IsPeerReadFailure(ex))
        {
            return null;
        }
    }

    // Peers throw COMException/ElementNotAvailableException/InvalidOperationException for elements mid-teardown; those are
    // the expected failure of reading a live tree. Anything else (a bug in this reader) must not be swallowed.
    internal static bool IsPeerReadFailure(Exception ex)
        => ex is System.Runtime.InteropServices.COMException or InvalidOperationException or ElementNotAvailableException or ArgumentException;

    public static UiNode ToNode(UiElementProbe probe, UIElement? boundsRoot, IReadOnlyList<UiNode>? children)
    {
        var element = probe.Element;
        var peer = TryCreatePeer(element);
        var patterns = peer is null ? null : ListPatterns(peer);
        if (OwnerOf(element) is not null && patterns?.Contains(UiPatterns.SelectionItem) != true)
        {
            // An item container is selectable through its owner even though its own peer lists nothing.
            patterns = (patterns ?? Array.Empty<string>()).Append(UiPatterns.SelectionItem).ToList();
        }

        string? toggleState = null;
        UiRange? range = null;
        string? value = null;
        bool? valueReadOnly = null;
        string? expandState = null;
        if (peer is not null)
        {
            toggleState = Read(() => (TryGetPattern(peer, PatternInterface.Toggle) as IToggleProvider)?.ToggleState.ToString());
            range = Read(() => TryGetPattern(peer, PatternInterface.RangeValue) is IRangeValueProvider provider
                ? new UiRange(provider.Value, provider.Minimum, provider.Maximum, provider.IsReadOnly)
                : null);
            if (element is not PasswordBox && TryGetPattern(peer, PatternInterface.Value) is IValueProvider valueProvider)
            {
                value = Read(() => valueProvider.Value);
                valueReadOnly = Read(() => (bool?)valueProvider.IsReadOnly);
            }

            expandState = Read(() => (TryGetPattern(peer, PatternInterface.ExpandCollapse) as IExpandCollapseProvider)?.ExpandCollapseState.ToString());
        }

        string? selectedItem = null;
        int? selectedIndex = null;
        IReadOnlyList<string>? items = null;
        int? itemCount = null;
        if (element is Selector selector)
        {
            var count = Read(() => (int?)selector.Items.Count);
            itemCount = count;
            selectedIndex = Read(() => (int?)selector.SelectedIndex) is { } index and >= 0 ? index : null;
            selectedItem = selectedIndex is { } selected ? Read(() => ItemText(selector, selected)) : null;
            items = Read(() => (IReadOnlyList<string>?)ItemTexts(selector, UiTreeLimits.MaxListedItems));
        }

        return new UiNode(
            probe.Path,
            probe.TypeName,
            probe.Name,
            probe.AutomationId,
            probe.AutomationName,
            probe.Text,
            probe.IsEnabled,
            probe.DisabledReason,
            probe.IsVisible,
            probe.HiddenReason,
            ReadBounds(element, boundsRoot),
            probe.Interactive,
            patterns is { Count: > 0 } ? patterns : null,
            toggleState,
            range,
            value,
            valueReadOnly,
            selectedItem,
            selectedIndex,
            items is { Count: > 0 } ? items : null,
            itemCount,
            expandState,
            ReadAccelerators(element),
            children is { Count: > 0 } ? children : null);
    }

    // The peer's facts that action planning needs, read fresh from the live element.
    public static UiElementFacts ReadFacts(UIElement element)
    {
        var patterns = new HashSet<string>(StringComparer.Ordinal);
        var peer = TryCreatePeer(element);
        if (OwnerOf(element) is not null)
        {
            patterns.Add(UiPatterns.SelectionItem);
        }

        if (peer is null)
        {
            return new UiElementFacts(patterns, false, null, null, null);
        }

        foreach (var name in new[]
        {
            PatternInterface.Invoke, PatternInterface.Toggle, PatternInterface.Value, PatternInterface.RangeValue,
            PatternInterface.Selection, PatternInterface.SelectionItem, PatternInterface.ExpandCollapse, PatternInterface.ScrollItem,
        })
        {
            if (TryGetPattern(peer, name) is not null)
            {
                patterns.Add(name.ToString());
            }
        }

        var valueReadOnly = false;
        if (element is not PasswordBox && TryGetPattern(peer, PatternInterface.Value) is IValueProvider valueProvider)
        {
            valueReadOnly = Read(() => (bool?)valueProvider.IsReadOnly) ?? false;
        }
        else if (patterns.Contains(UiPatterns.Value))
        {
            // A PasswordBox's value is never exposed or settable through this surface.
            patterns.Remove(UiPatterns.Value);
        }

        double? minimum = null;
        double? maximum = null;
        if (TryGetPattern(peer, PatternInterface.RangeValue) is IRangeValueProvider rangeProvider)
        {
            minimum = Read(() => (double?)rangeProvider.Minimum);
            maximum = Read(() => (double?)rangeProvider.Maximum);
        }

        var expandState = Read(() => (TryGetPattern(peer, PatternInterface.ExpandCollapse) as IExpandCollapseProvider)?.ExpandCollapseState.ToString());
        return new UiElementFacts(patterns, valueReadOnly, minimum, maximum, expandState);
    }

    private static IReadOnlyList<string> ListPatterns(AutomationPeer peer)
    {
        var supported = new List<string>();
        foreach (var pattern in Enum.GetValues<PatternInterface>())
        {
            if (TryGetPattern(peer, pattern) is not null)
            {
                supported.Add(pattern.ToString());
            }
        }

        return supported;
    }

    private static UiBounds? ReadBounds(UIElement element, UIElement? boundsRoot)
    {
        if (element is not FrameworkElement frameworkElement)
        {
            return null;
        }

        try
        {
            var transform = element.TransformToVisual(boundsRoot);
            var bounds = transform.TransformBounds(new Rect(0, 0, frameworkElement.ActualWidth, frameworkElement.ActualHeight));
            return new UiBounds(Math.Round(bounds.X, 1), Math.Round(bounds.Y, 1), Math.Round(bounds.Width, 1), Math.Round(bounds.Height, 1));
        }
        catch (Exception ex) when (IsPeerReadFailure(ex))
        {
            return null;
        }
    }

    private static IReadOnlyList<UiAccelerator>? ReadAccelerators(UIElement element)
    {
        try
        {
            var accelerators = element.KeyboardAccelerators;
            if (accelerators.Count == 0)
            {
                return null;
            }

            return accelerators
                .Select(accelerator => new UiAccelerator(accelerator.Key.ToString(), FormatModifiers(accelerator.Modifiers)))
                .ToList();
        }
        catch (Exception ex) when (IsPeerReadFailure(ex))
        {
            return null;
        }
    }

    private static string FormatModifiers(VirtualKeyModifiers modifiers)
    {
        var parts = new List<string>(4);
        if (modifiers.HasFlag(VirtualKeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(VirtualKeyModifiers.Menu))
        {
            parts.Add("Alt");
        }

        if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (modifiers.HasFlag(VirtualKeyModifiers.Windows))
        {
            parts.Add("Win");
        }

        return parts.Count == 0 ? "None" : string.Join("+", parts);
    }

    // Item texts in list order. Strings are used as-is and ContentControl/UIElement items by their content. A view-model
    // object is read through the selector's DisplayMemberPath (what the control shows), then through a realized
    // container's visible text, and only then through ToString(), which is only as readable as the type makes it.
    // DisplayMemberPath comes first so an item has the same text whether or not its dropdown is open.
    public static IReadOnlyList<string> ItemTexts(Selector selector, int max)
    {
        var count = Math.Min(selector.Items.Count, max);
        var texts = new List<string>(count);
        for (var index = 0; index < count; index++)
        {
            texts.Add(ItemText(selector, index));
        }

        return texts;
    }

    public static string ItemText(Selector selector, int index)
    {
        var item = selector.Items[index];
        string? text = item switch
        {
            string value => value,
            ContentControl contentControl => ExtractText(contentControl),
            UIElement element => CollectText(element),
            _ => UiDisplayMemberPath.Resolve(item, Read(() => selector.DisplayMemberPath)),
        };

        if (string.IsNullOrWhiteSpace(text) && selector.ContainerFromIndex(index) is UIElement container)
        {
            text = ExtractText(container) ?? CollectText(container);
        }

        text ??= Convert.ToString(item, CultureInfo.InvariantCulture);
        return Normalize(text) ?? string.Empty;
    }

    public static string? ExtractText(UIElement element)
    {
        string? text = element switch
        {
            PasswordBox => null,
            TextBlock block => block.Text,
            MenuFlyoutItem menuItem => menuItem.Text,
            TextBox box => HeaderText(box.Header),
            NumberBox box => HeaderText(box.Header),
            ComboBox box => HeaderText(box.Header),
            Slider slider => HeaderText(slider.Header),
            ToggleSwitch toggle => HeaderText(toggle.Header),
            Expander expander => HeaderText(expander.Header),
            ContentControl control => HeaderText(control.Content),
            _ => null,
        };
        return Normalize(text);
    }

    private static string? HeaderText(object? header)
        => header switch
        {
            string text => text,
            UIElement element => CollectText(element),
            _ => null,
        };

    private static string? CollectText(UIElement root)
    {
        var builder = new StringBuilder();
        var budget = MaxTextNodes;
        CollectTextCore(root, builder, ref budget);
        return builder.Length == 0 ? null : builder.ToString();
    }

    private static void CollectTextCore(UIElement element, StringBuilder builder, ref int budget)
    {
        if (budget-- <= 0 || builder.Length >= MaxTextLength || element.Visibility == Visibility.Collapsed)
        {
            return;
        }

        if (element is TextBlock block)
        {
            if (!string.IsNullOrWhiteSpace(block.Text))
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(block.Text.Trim());
            }

            return;
        }

        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element);
        for (var index = 0; index < count; index++)
        {
            if (Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, index) is UIElement child)
            {
                CollectTextCore(child, builder, ref budget);
            }
        }
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var collapsed = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return collapsed.Length <= MaxTextLength ? collapsed : collapsed.Substring(0, MaxTextLength - 3) + "...";
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrEmpty(value) ? null : value;

    private static T? Read<T>(Func<T?> read)
    {
        try
        {
            return read();
        }
        catch (Exception ex) when (IsPeerReadFailure(ex))
        {
            return default;
        }
    }
}
