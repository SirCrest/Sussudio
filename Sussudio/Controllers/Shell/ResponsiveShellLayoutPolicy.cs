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
