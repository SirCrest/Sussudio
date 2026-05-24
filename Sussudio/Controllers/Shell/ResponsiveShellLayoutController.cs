using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Sussudio.Controllers;

internal enum ResponsiveCaptureSettingsLayoutKind
{
    Wide,
    Narrow,
}

internal readonly record struct ResponsiveGridSlot(int Row, int Column);

internal readonly record struct ResponsiveCaptureSettingsPlacement(
    bool CollapseCaptureOptionColumns,
    ResponsiveGridSlot VideoFormat,
    ResponsiveGridSlot Preset,
    ResponsiveGridSlot Split,
    ResponsiveGridSlot CustomBitrate);

internal static class ResponsiveShellLayoutPolicy
{
    public const double ControlBarLabelThreshold = 900.0;
    public const double CaptureSettingsNarrowWidth = 700.0;

    private static readonly ResponsiveCaptureSettingsPlacement NarrowPlacement = new(
        true,
        new ResponsiveGridSlot(1, 1),
        new ResponsiveGridSlot(1, 2),
        new ResponsiveGridSlot(1, 3),
        new ResponsiveGridSlot(1, 2));

    private static readonly ResponsiveCaptureSettingsPlacement WidePlacement = new(
        false,
        new ResponsiveGridSlot(0, 0),
        new ResponsiveGridSlot(0, 5),
        new ResponsiveGridSlot(0, 6),
        new ResponsiveGridSlot(0, 5));

    public static bool ShouldShowControlBarLabels(double controlBarWidth)
        => controlBarWidth >= ControlBarLabelThreshold;

    public static ResponsiveCaptureSettingsLayoutKind GetCaptureSettingsLayoutKind(double width)
        => width < CaptureSettingsNarrowWidth
            ? ResponsiveCaptureSettingsLayoutKind.Narrow
            : ResponsiveCaptureSettingsLayoutKind.Wide;

    public static ResponsiveCaptureSettingsPlacement GetCaptureSettingsPlacement(
        ResponsiveCaptureSettingsLayoutKind layoutKind)
        => layoutKind == ResponsiveCaptureSettingsLayoutKind.Narrow
            ? NarrowPlacement
            : WidePlacement;
}

internal sealed class ControlBarLabelVisibilityControllerContext
{
    public required Border ControlBarBorder { get; init; }
    public required UIElement[] ControlBarLabels { get; init; }
}

internal sealed class ControlBarLabelVisibilityController
{
    private readonly ControlBarLabelVisibilityControllerContext _context;
    private bool _toggleLabelsVisible;

    public ControlBarLabelVisibilityController(ControlBarLabelVisibilityControllerContext context)
    {
        _context = context;
    }

    public void Attach()
    {
        _context.ControlBarBorder.SizeChanged += (_, e) => ApplyControlBarWidth(e.NewSize.Width);
    }

    private void ApplyControlBarWidth(double controlBarWidth)
    {
        var showLabels = ResponsiveShellLayoutPolicy.ShouldShowControlBarLabels(controlBarWidth);
        if (showLabels == _toggleLabelsVisible)
        {
            return;
        }

        _toggleLabelsVisible = showLabels;
        var visibility = showLabels ? Visibility.Visible : Visibility.Collapsed;
        foreach (var label in _context.ControlBarLabels)
        {
            label.Visibility = visibility;
        }
    }
}

internal sealed class ResponsiveShellLayoutControllerContext
{
    public required Grid CaptureSettingsGrid { get; init; }
    public required ColumnDefinition VideoFormatColumn { get; init; }
    public required ColumnDefinition PresetColumn { get; init; }
    public required ColumnDefinition SplitColumn { get; init; }
    public required FrameworkElement VideoFormatPanel { get; init; }
    public required FrameworkElement PresetPanel { get; init; }
    public required FrameworkElement SplitPanel { get; init; }
    public required FrameworkElement CustomBitratePanel { get; init; }
}

internal sealed class ResponsiveShellLayoutController
{
    private readonly ResponsiveShellLayoutControllerContext _context;
    private bool _captureSettingsNarrow;

    public ResponsiveShellLayoutController(ResponsiveShellLayoutControllerContext context)
    {
        _context = context;
    }

    public void Attach()
    {
        _context.CaptureSettingsGrid.SizeChanged += (_, e) => ApplyCaptureSettingsWidth(e.NewSize.Width);
    }

    private void ApplyCaptureSettingsWidth(double width)
    {
        var layoutKind = ResponsiveShellLayoutPolicy.GetCaptureSettingsLayoutKind(width);
        var narrow = layoutKind == ResponsiveCaptureSettingsLayoutKind.Narrow;
        if (narrow == _captureSettingsNarrow)
        {
            return;
        }

        _captureSettingsNarrow = narrow;
        ApplyCaptureSettingsLayout(ResponsiveShellLayoutPolicy.GetCaptureSettingsPlacement(layoutKind));
    }

    private void ApplyCaptureSettingsLayout(ResponsiveCaptureSettingsPlacement placement)
    {
        var responsiveColumnWidth = placement.CollapseCaptureOptionColumns
            ? new GridLength(0)
            : new GridLength(1, GridUnitType.Star);
        _context.VideoFormatColumn.Width = responsiveColumnWidth;
        _context.PresetColumn.Width = responsiveColumnWidth;
        _context.SplitColumn.Width = responsiveColumnWidth;
        ApplyGridSlot(_context.VideoFormatPanel, placement.VideoFormat);
        ApplyGridSlot(_context.PresetPanel, placement.Preset);
        ApplyGridSlot(_context.SplitPanel, placement.Split);
        ApplyGridSlot(_context.CustomBitratePanel, placement.CustomBitrate);
    }

    private static void ApplyGridSlot(FrameworkElement element, ResponsiveGridSlot slot)
    {
        Grid.SetRow(element, slot.Row);
        Grid.SetColumn(element, slot.Column);
    }
}
