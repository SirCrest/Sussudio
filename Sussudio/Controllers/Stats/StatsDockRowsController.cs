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
    private readonly StatsDockRowChromePresenter _rowChrome;
    private readonly List<StatsDockRowChromeSlot> _decodeRowPool = new();
    private readonly List<StatsDockRowChromeSlot> _gpuRowPool = new();

    public StatsDockRowChromeController(StatsDockRowChromeControllerContext context)
    {
        _rowChrome = new StatsDockRowChromePresenter(context.ResourceOwner);
    }

    public void CollapseSimpleRows(StatsDockSimpleRowPool poolKind)
    {
        StatsDockRowChromePresenter.CollapseRows(GetSimpleRowPool(poolKind));
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
            _rowChrome.UpdateRowSlot(pool[i], row.Label, row.Value, alt: (i % 2) != 0);
        }

        StatsDockRowChromePresenter.CollapseRows(pool, startIndex: rows.Count);
    }

    private List<StatsDockRowChromeSlot> GetSimpleRowPool(StatsDockSimpleRowPool poolKind)
        => poolKind switch
        {
            StatsDockSimpleRowPool.Decode => _decodeRowPool,
            StatsDockSimpleRowPool.Gpu => _gpuRowPool,
            _ => throw new ArgumentOutOfRangeException(nameof(poolKind), poolKind, null)
        };

    private void EnsureRowPool(StackPanel container, List<StatsDockRowChromeSlot> pool, int requiredCount)
    {
        while (pool.Count < requiredCount)
        {
            var slot = _rowChrome.CreateRowSlot();
            pool.Add(slot);
            container.Children.Add(slot.Row);
        }
    }
}

internal sealed class StatsDiagnosticRowsControllerContext
{
    public required FrameworkElement ResourceOwner { get; init; }
    public required StackPanel DiagnosticsContent { get; init; }
}

internal sealed class StatsDiagnosticRowsController
{
    private readonly StatsDiagnosticRowsControllerContext _context;
    private readonly StatsDockRowChromePresenter _rowChrome;
    private readonly List<DiagnosticsPoolSlot> _diagnosticsRowPool = new();
    private TextBlock? _diagnosticsEmptyStateTextBlock;

    public StatsDiagnosticRowsController(StatsDiagnosticRowsControllerContext context)
    {
        _context = context;
        _rowChrome = new StatsDockRowChromePresenter(context.ResourceOwner);
    }

    public void UpdateDiagnostics(StatsDiagnosticRowsPresentation presentation)
    {
        EnsureDiagnosticsEmptyState();

        if (presentation.IsEmpty)
        {
            StatsDockRowChromePresenter.SetVisibilityIfChanged(_diagnosticsEmptyStateTextBlock!, Visibility.Visible);
            CollapseDiagnosticsPoolSlots();
            return;
        }

        var slotIndex = 0;
        foreach (var row in presentation.Rows)
        {
            EnsureDiagnosticsPoolCapacity(slotIndex + 1);
            UpdateDiagnosticsPoolSlot(
                _diagnosticsRowPool[slotIndex],
                row.GroupHeader,
                row.Label,
                row.Value,
                row.IsAlternate);
            slotIndex++;
        }

        StatsDockRowChromePresenter.SetVisibilityIfChanged(_diagnosticsEmptyStateTextBlock!, Visibility.Collapsed);
        CollapseDiagnosticsPoolSlots(startIndex: slotIndex);
    }

    private void EnsureDiagnosticsEmptyState()
    {
        if (_diagnosticsEmptyStateTextBlock != null)
        {
            return;
        }

        _diagnosticsEmptyStateTextBlock = new TextBlock
        {
            Text = "No diagnostics available",
            Style = _rowChrome.GetStyle("DockStatsLabelStyle"),
            Visibility = Visibility.Collapsed
        };
        _context.DiagnosticsContent.Children.Add(_diagnosticsEmptyStateTextBlock);
    }

    private void EnsureDiagnosticsPoolCapacity(int requiredCount)
    {
        while (_diagnosticsRowPool.Count < requiredCount)
        {
            var rowSlot = _rowChrome.CreateRowSlot();
            var header = CreateDiagnosticGroupHeader("");
            header.Visibility = Visibility.Collapsed;
            _context.DiagnosticsContent.Children.Add(header);
            _context.DiagnosticsContent.Children.Add(rowSlot.Row);
            _diagnosticsRowPool.Add(new DiagnosticsPoolSlot(rowSlot, header));
        }
    }

    private void UpdateDiagnosticsPoolSlot(
        DiagnosticsPoolSlot slot,
        string? groupHeader,
        string label,
        string value,
        bool alt)
    {
        if (groupHeader != null)
        {
            StatsDockRowChromePresenter.SetTextIfChanged(slot.GroupHeader, groupHeader);
            StatsDockRowChromePresenter.SetVisibilityIfChanged(slot.GroupHeader, Visibility.Visible);
        }
        else
        {
            StatsDockRowChromePresenter.SetVisibilityIfChanged(slot.GroupHeader, Visibility.Collapsed);
        }

        _rowChrome.UpdateRowSlot(slot.RowSlot, label, value, alt);
    }

    private void CollapseDiagnosticsPoolSlots(int startIndex = 0)
    {
        for (var i = startIndex; i < _diagnosticsRowPool.Count; i++)
        {
            var slot = _diagnosticsRowPool[i];
            StatsDockRowChromePresenter.SetVisibilityIfChanged(slot.RowSlot.Row, Visibility.Collapsed);
            StatsDockRowChromePresenter.SetVisibilityIfChanged(slot.GroupHeader, Visibility.Collapsed);
        }
    }

    private TextBlock CreateDiagnosticGroupHeader(string title)
    {
        return new TextBlock
        {
            Text = title,
            Margin = new Thickness(0, 8, 0, 2),
            Style = _rowChrome.GetStyle("DockStatsSectionHeaderStyle")
        };
    }

    private sealed record DiagnosticsPoolSlot(
        StatsDockRowChromeSlot RowSlot,
        TextBlock GroupHeader);
}

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
