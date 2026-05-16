using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal enum StatsDockSimpleRowPool
{
    Decode,
    Gpu
}

internal sealed class StatsDockRowChromeControllerContext
{
    public required FrameworkElement ResourceOwner { get; init; }
}

internal sealed class StatsDockRowChromeController
{
    private readonly StatsDockRowChromeControllerContext _context;
    private readonly List<RowSlot> _decodeRowPool = new();
    private readonly List<RowSlot> _gpuRowPool = new();

    public StatsDockRowChromeController(StatsDockRowChromeControllerContext context)
    {
        _context = context;
    }

    public void CollapseSimpleRows(StatsDockSimpleRowPool poolKind)
    {
        CollapseRows(GetSimpleRowPool(poolKind));
    }

    public void UpdateSimpleRows(
        StatsDockSimpleRowPool poolKind,
        StackPanel container,
        IReadOnlyList<StatsHardwareRowPresentation> rows,
        int minimumCapacity)
    {
        var pool = GetSimpleRowPool(poolKind);
        EnsureRowPool(container, pool, Math.Max(minimumCapacity, rows.Count));
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            UpdateRowSlot(pool[i], row.Label, row.Value, alt: (i % 2) != 0);
        }

        CollapseRows(pool, startIndex: rows.Count);
    }

    private List<RowSlot> GetSimpleRowPool(StatsDockSimpleRowPool poolKind)
        => poolKind switch
        {
            StatsDockSimpleRowPool.Decode => _decodeRowPool,
            StatsDockSimpleRowPool.Gpu => _gpuRowPool,
            _ => throw new ArgumentOutOfRangeException(nameof(poolKind), poolKind, null)
        };

    private void EnsureRowPool(StackPanel container, List<RowSlot> pool, int requiredCount)
    {
        while (pool.Count < requiredCount)
        {
            var row = CreateRow("", "", alt: false);
            var grid = (Grid)row.Child;
            var labelBlock = (TextBlock)grid.Children[0];
            var valueBlock = (TextBlock)grid.Children[1];
            pool.Add(new RowSlot(row, labelBlock, valueBlock));
            container.Children.Add(row);
        }
    }

    private void UpdateRowSlot(RowSlot slot, string label, string value, bool alt)
    {
        SetTextIfChanged(slot.Label, label);
        SetTextIfChanged(slot.Value, value);
        var targetStyle = GetStyle(alt ? "DockStatsRowAltStyle" : "DockStatsRowStyle");
        if (!ReferenceEquals(slot.Row.Style, targetStyle))
        {
            slot.Row.Style = targetStyle;
        }

        SetVisibilityIfChanged(slot.Row, Visibility.Visible);
    }

    private static void CollapseRows(List<RowSlot> pool, int startIndex = 0)
    {
        for (var i = startIndex; i < pool.Count; i++)
        {
            SetVisibilityIfChanged(pool[i].Row, Visibility.Collapsed);
        }
    }

    private Border CreateRow(string label, string value, bool alt)
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

        return new Border
        {
            Style = GetStyle(alt ? "DockStatsRowAltStyle" : "DockStatsRowStyle"),
            Child = grid
        };
    }

    private Style GetStyle(string key) => (Style)_context.ResourceOwner.Resources[key];

    private static void SetTextIfChanged(TextBlock target, string value)
    {
        if (!string.Equals(target.Text, value, StringComparison.Ordinal))
        {
            target.Text = value;
        }
    }

    private static void SetVisibilityIfChanged(UIElement element, Visibility visibility)
    {
        if (element.Visibility != visibility)
        {
            element.Visibility = visibility;
        }
    }

    private sealed record RowSlot(Border Row, TextBlock Label, TextBlock Value);
}
