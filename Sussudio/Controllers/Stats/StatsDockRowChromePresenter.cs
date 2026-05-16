using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Sussudio.Controllers;

internal sealed record StatsDockRowChromeSlot(Border Row, TextBlock Label, TextBlock Value);

internal sealed class StatsDockRowChromePresenter
{
    private readonly FrameworkElement _resourceOwner;

    public StatsDockRowChromePresenter(FrameworkElement resourceOwner)
    {
        _resourceOwner = resourceOwner;
    }

    public StatsDockRowChromeSlot CreateRowSlot(string label = "", string value = "", bool alt = false)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelBlock = new TextBlock
        {
            Text = label,
            Style = GetStyle("DockStatsLabelStyle")
        };

        var valueBlock = new TextBlock
        {
            Text = value,
            Style = GetStyle("DockStatsValueStyle"),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(valueBlock, 1);

        grid.Children.Add(labelBlock);
        grid.Children.Add(valueBlock);

        var row = new Border
        {
            Style = GetRowStyle(alt),
            Child = grid
        };
        return new StatsDockRowChromeSlot(row, labelBlock, valueBlock);
    }

    public void UpdateRowSlot(StatsDockRowChromeSlot slot, string label, string value, bool alt)
    {
        SetTextIfChanged(slot.Label, label);
        SetTextIfChanged(slot.Value, value);
        var targetStyle = GetRowStyle(alt);
        if (!ReferenceEquals(slot.Row.Style, targetStyle))
        {
            slot.Row.Style = targetStyle;
        }

        SetVisibilityIfChanged(slot.Row, Visibility.Visible);
    }

    public Style GetStyle(string key) => (Style)_resourceOwner.Resources[key];

    private Style GetRowStyle(bool alt)
        => GetStyle(alt ? "DockStatsRowAltStyle" : "DockStatsRowStyle");

    public static void CollapseRows(IReadOnlyList<StatsDockRowChromeSlot> pool, int startIndex = 0)
    {
        for (var i = startIndex; i < pool.Count; i++)
        {
            SetVisibilityIfChanged(pool[i].Row, Visibility.Collapsed);
        }
    }

    public static void SetTextIfChanged(TextBlock target, string value)
    {
        if (!string.Equals(target.Text, value, StringComparison.Ordinal))
        {
            target.Text = value;
        }
    }

    public static void SetVisibilityIfChanged(UIElement element, Visibility visibility)
    {
        if (element.Visibility != visibility)
        {
            element.Visibility = visibility;
        }
    }
}
