using System;
using Sussudio.ViewModels;
using Windows.Foundation;

namespace Sussudio.Controllers;

internal readonly record struct FrameTimeOverlayCanvasSize(double Width, double Height);

internal readonly record struct FrameTimeOverlayExpectedLineGeometry(double X2, double Y);

internal static class FrameTimeOverlayGeometry
{
    public const double FallbackWidth = 500;
    public const double FallbackHeight = 92;

    public static FrameTimeOverlayCanvasSize ResolveCanvasSize(double actualWidth, double actualHeight)
    {
        var width = actualWidth > 1 ? actualWidth : FallbackWidth;
        var height = actualHeight > 1 ? actualHeight : FallbackHeight;
        return new FrameTimeOverlayCanvasSize(width, height);
    }

    public static Point ProjectSample(
        int sampleIndex,
        int sampleCount,
        double sampleMs,
        StatsFrameTimeRange range,
        FrameTimeOverlayCanvasSize canvasSize)
    {
        var x = sampleCount <= 1 ? 0 : sampleIndex * canvasSize.Width / (sampleCount - 1);
        var y = ProjectY(sampleMs, range, canvasSize.Height);
        return new Point(x, y);
    }

    public static FrameTimeOverlayExpectedLineGeometry ProjectExpectedLine(
        StatsFrameTimeRange range,
        FrameTimeOverlayCanvasSize canvasSize)
    {
        var y = ProjectY(range.ExpectedMs, range, canvasSize.Height);
        return new FrameTimeOverlayExpectedLineGeometry(canvasSize.Width, y);
    }

    private static double ProjectY(double frameTimeMs, StatsFrameTimeRange range, double height)
    {
        var normalized = Math.Clamp((frameTimeMs - range.MinMs) / range.SpanMs, 0.0, 1.0);
        return height - normalized * height;
    }
}
