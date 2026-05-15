using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class StatsDiagnosticRowsControllerContext
{
    public required FrameworkElement ResourceOwner { get; init; }
    public required StackPanel DiagnosticsContent { get; init; }
}

internal sealed class StatsDiagnosticRowsController
{
    private const int MaxExpectedDecodeRowCount = 14;
    private const int FixedGpuRowCount = 10;

    private readonly StatsDiagnosticRowsControllerContext _context;
    private readonly List<DiagnosticRowSlot> _decodeRowPool = new();
    private readonly List<DiagnosticRowSlot> _gpuRowPool = new();
    private readonly List<DiagnosticsPoolSlot> _diagnosticsRowPool = new();
    private TextBlock? _diagnosticsEmptyStateTextBlock;

    public StatsDiagnosticRowsController(StatsDiagnosticRowsControllerContext context)
    {
        _context = context;
    }

    public void CollapseDecodeRows(StackPanel container)
    {
        CollapseDiagnosticRows(_decodeRowPool);
    }

    public void UpdateDecodeRows(StackPanel container, IReadOnlyList<StatsHardwareRowPresentation> rows)
    {
        UpdateSimpleRows(container, _decodeRowPool, rows, MaxExpectedDecodeRowCount);
    }

    public void UpdateGpuRows(StackPanel container, IReadOnlyList<StatsHardwareRowPresentation> rows)
    {
        UpdateSimpleRows(container, _gpuRowPool, rows, FixedGpuRowCount);
    }

    public void UpdateDiagnostics(StatsDiagnosticRowsPresentation presentation)
    {
        EnsureDiagnosticsEmptyState();

        if (presentation.IsEmpty)
        {
            SetVisibilityIfChanged(_diagnosticsEmptyStateTextBlock!, Visibility.Visible);
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

        SetVisibilityIfChanged(_diagnosticsEmptyStateTextBlock!, Visibility.Collapsed);
        CollapseDiagnosticsPoolSlots(startIndex: slotIndex);
    }

    private void UpdateSimpleRows(
        StackPanel container,
        List<DiagnosticRowSlot> pool,
        IReadOnlyList<StatsHardwareRowPresentation> rows,
        int minimumCapacity)
    {
        EnsureDiagnosticRowPool(container, pool, Math.Max(minimumCapacity, rows.Count));
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            UpdateDiagnosticRowSlot(pool[i], row.Label, row.Value, alt: (i % 2) != 0);
        }

        CollapseDiagnosticRows(pool, startIndex: rows.Count);
    }

    private void EnsureDiagnosticRowPool(StackPanel container, List<DiagnosticRowSlot> pool, int requiredCount)
    {
        while (pool.Count < requiredCount)
        {
            var row = CreateDiagnosticRow("", "", alt: false);
            var grid = (Grid)row.Child;
            var labelBlock = (TextBlock)grid.Children[0];
            var valueBlock = (TextBlock)grid.Children[1];
            pool.Add(new DiagnosticRowSlot(row, labelBlock, valueBlock));
            container.Children.Add(row);
        }
    }

    private void UpdateDiagnosticRowSlot(DiagnosticRowSlot slot, string label, string value, bool alt)
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

    private static void CollapseDiagnosticRows(List<DiagnosticRowSlot> pool, int startIndex = 0)
    {
        for (var i = startIndex; i < pool.Count; i++)
        {
            SetVisibilityIfChanged(pool[i].Row, Visibility.Collapsed);
        }
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
            Style = GetStyle("DockStatsLabelStyle"),
            Visibility = Visibility.Collapsed
        };
        _context.DiagnosticsContent.Children.Add(_diagnosticsEmptyStateTextBlock);
    }

    private void EnsureDiagnosticsPoolCapacity(int requiredCount)
    {
        while (_diagnosticsRowPool.Count < requiredCount)
        {
            var row = CreateDiagnosticRow("", "", alt: false);
            var grid = (Grid)row.Child;
            var labelBlock = (TextBlock)grid.Children[0];
            var valueBlock = (TextBlock)grid.Children[1];
            var header = CreateDiagnosticGroupHeader("");
            header.Visibility = Visibility.Collapsed;
            _context.DiagnosticsContent.Children.Add(header);
            _context.DiagnosticsContent.Children.Add(row);
            _diagnosticsRowPool.Add(new DiagnosticsPoolSlot(row, header, labelBlock, valueBlock));
        }
    }

    private void UpdateDiagnosticsPoolSlot(
        DiagnosticsPoolSlot slot,
        string? groupHeader,
        string label,
        string value,
        bool alt)
    {
        if (slot.GroupHeader != null)
        {
            if (groupHeader != null)
            {
                SetTextIfChanged(slot.GroupHeader, groupHeader);
                SetVisibilityIfChanged(slot.GroupHeader, Visibility.Visible);
            }
            else
            {
                SetVisibilityIfChanged(slot.GroupHeader, Visibility.Collapsed);
            }
        }

        SetTextIfChanged(slot.Label, label);
        SetTextIfChanged(slot.Value, value);
        var targetStyle = GetStyle(alt ? "DockStatsRowAltStyle" : "DockStatsRowStyle");
        if (!ReferenceEquals(slot.Row.Style, targetStyle))
        {
            slot.Row.Style = targetStyle;
        }

        SetVisibilityIfChanged(slot.Row, Visibility.Visible);
    }

    private void CollapseDiagnosticsPoolSlots(int startIndex = 0)
    {
        for (var i = startIndex; i < _diagnosticsRowPool.Count; i++)
        {
            var slot = _diagnosticsRowPool[i];
            SetVisibilityIfChanged(slot.Row, Visibility.Collapsed);
            if (slot.GroupHeader != null)
            {
                SetVisibilityIfChanged(slot.GroupHeader, Visibility.Collapsed);
            }
        }
    }

    private TextBlock CreateDiagnosticGroupHeader(string title)
    {
        return new TextBlock
        {
            Text = title,
            Margin = new Thickness(0, 8, 0, 2),
            Style = GetStyle("DockStatsSectionHeaderStyle")
        };
    }

    private Border CreateDiagnosticRow(string label, string value, bool alt)
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

    private sealed record DiagnosticRowSlot(Border Row, TextBlock Label, TextBlock Value);

    private sealed record DiagnosticsPoolSlot(
        Border Row,
        TextBlock? GroupHeader,
        TextBlock Label,
        TextBlock Value);
}
