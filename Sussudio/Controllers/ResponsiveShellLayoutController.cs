using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Sussudio.Controllers;

internal sealed class ResponsiveShellLayoutControllerContext
{
    public required Border ControlBarBorder { get; init; }
    public required Grid CaptureSettingsGrid { get; init; }
    public required UIElement HdrToggleLabel { get; init; }
    public required UIElement AudioRecordToggleLabel { get; init; }
    public required UIElement PreviewButtonLabel { get; init; }
    public required UIElement HdrPreviewToggleLabel { get; init; }
    public required UIElement AudioPreviewToggleLabel { get; init; }
    public required UIElement StatsToggleLabel { get; init; }
    public required UIElement FrameTimeOverlayToggleLabel { get; init; }
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
    private const double ControlBarLabelThreshold = 900.0;
    private const double CaptureSettingsNarrowWidth = 700.0;

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
        var showLabels = controlBarWidth >= ControlBarLabelThreshold;
        if (showLabels == _toggleLabelsVisible)
        {
            return;
        }

        _toggleLabelsVisible = showLabels;
        var visibility = showLabels ? Visibility.Visible : Visibility.Collapsed;
        _context.HdrToggleLabel.Visibility = visibility;
        _context.AudioRecordToggleLabel.Visibility = visibility;
        _context.PreviewButtonLabel.Visibility = visibility;
        _context.HdrPreviewToggleLabel.Visibility = visibility;
        _context.AudioPreviewToggleLabel.Visibility = visibility;
        _context.StatsToggleLabel.Visibility = visibility;
        _context.FrameTimeOverlayToggleLabel.Visibility = visibility;
    }

    private void ApplyCaptureSettingsWidth(double width)
    {
        var narrow = width < CaptureSettingsNarrowWidth;
        if (narrow == _captureSettingsNarrow)
        {
            return;
        }

        _captureSettingsNarrow = narrow;
        if (narrow)
        {
            ApplyNarrowCaptureSettingsLayout();
        }
        else
        {
            ApplyWideCaptureSettingsLayout();
        }
    }

    private void ApplyNarrowCaptureSettingsLayout()
    {
        _context.VideoFormatColumn.Width = new GridLength(0);
        _context.PresetColumn.Width = new GridLength(0);
        _context.SplitColumn.Width = new GridLength(0);
        Grid.SetRow(_context.VideoFormatPanel, 1);
        Grid.SetColumn(_context.VideoFormatPanel, 1);
        Grid.SetRow(_context.PresetPanel, 1);
        Grid.SetColumn(_context.PresetPanel, 2);
        Grid.SetRow(_context.SplitPanel, 1);
        Grid.SetColumn(_context.SplitPanel, 3);
        Grid.SetRow(_context.CustomBitratePanel, 1);
        Grid.SetColumn(_context.CustomBitratePanel, 2);
    }

    private void ApplyWideCaptureSettingsLayout()
    {
        _context.VideoFormatColumn.Width = new GridLength(1, GridUnitType.Star);
        _context.PresetColumn.Width = new GridLength(1, GridUnitType.Star);
        _context.SplitColumn.Width = new GridLength(1, GridUnitType.Star);
        Grid.SetRow(_context.VideoFormatPanel, 0);
        Grid.SetColumn(_context.VideoFormatPanel, 0);
        Grid.SetRow(_context.PresetPanel, 0);
        Grid.SetColumn(_context.PresetPanel, 5);
        Grid.SetRow(_context.SplitPanel, 0);
        Grid.SetColumn(_context.SplitPanel, 6);
        Grid.SetRow(_context.CustomBitratePanel, 0);
        Grid.SetColumn(_context.CustomBitratePanel, 5);
    }
}
