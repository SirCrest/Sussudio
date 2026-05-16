using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Sussudio.Controllers;

internal sealed class ResponsiveShellLayoutControllerContext
{
    public required Border ControlBarBorder { get; init; }
    public required Grid CaptureSettingsGrid { get; init; }
    public required UIElement[] ControlBarLabels { get; init; }
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
    private bool _toggleLabelsVisible;
    private bool _captureSettingsNarrow;

    public ResponsiveShellLayoutController(ResponsiveShellLayoutControllerContext context)
    {
        _context = context;
    }

    public void Attach()
    {
        _context.ControlBarBorder.SizeChanged += (_, e) => ApplyControlBarWidth(e.NewSize.Width);
        _context.CaptureSettingsGrid.SizeChanged += (_, e) => ApplyCaptureSettingsWidth(e.NewSize.Width);
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
