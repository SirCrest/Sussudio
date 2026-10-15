using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Sussudio.Services.Automation;

public sealed record UiSelectionMatch(bool Ok, int Index, string? Error);

/// <summary>
/// Picks one item of a ComboBox/ListView from what the agent typed: the item text (whole string, trimmed,
/// case-insensitive) or <c>#N</c> for a zero-based index. A bare number is text, never an index, because labels such as
/// "60" are common and a bare number silently changing meaning would select the wrong item.
/// </summary>
public static class UiSelectionMatcher
{
    private const int MaxListedItems = 20;

    public static UiSelectionMatch Match(IReadOnlyList<string> items, string value)
    {
        if (items.Count == 0)
        {
            return new UiSelectionMatch(false, -1, "The list has no items to select from.");
        }

        var wanted = value.Trim();
        if (wanted.StartsWith('#'))
        {
            if (!int.TryParse(wanted.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index >= items.Count)
            {
                return new UiSelectionMatch(false, -1, $"'{wanted}' is not a valid index: the list has {items.Count} items, so use #0 to #{items.Count - 1}.");
            }

            return new UiSelectionMatch(true, index, null);
        }

        var matches = new List<int>(1);
        for (var index = 0; index < items.Count; index++)
        {
            if (string.Equals(items[index].Trim(), wanted, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(index);
            }
        }

        if (matches.Count == 1)
        {
            return new UiSelectionMatch(true, matches[0], null);
        }

        if (matches.Count > 1)
        {
            return new UiSelectionMatch(
                false,
                -1,
                $"'{wanted}' matches {matches.Count} items ({string.Join(", ", matches.Select(index => "#" + index))}); select one by index with #N.");
        }

        var listed = new StringBuilder();
        for (var index = 0; index < Math.Min(items.Count, MaxListedItems); index++)
        {
            if (index > 0)
            {
                listed.Append(", ");
            }

            listed.Append('#').Append(index).Append(' ').Append('"').Append(items[index].Trim()).Append('"');
        }

        if (items.Count > MaxListedItems)
        {
            listed.Append(", ... (").Append(items.Count).Append(" items in total)");
        }

        return new UiSelectionMatch(
            false,
            -1,
            $"No item matches '{wanted}' ({items.Count} items; use the item text, or #N for a zero-based index). Items: {listed}.");
    }
}
