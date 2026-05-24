using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Sussudio.ViewModels;
using Windows.Foundation;

namespace Sussudio.Controllers;

internal sealed class FrameTimeOverlayPresentationControllerContext
{
    public required TextBlock SourceValue { get; init; }
    public required TextBlock VisualValue { get; init; }
    public required TextBlock PreviewValue { get; init; }
    public required TextBlock LatencyValue { get; init; }
    public required TextBlock StatusValue { get; init; }
    public required Canvas Canvas { get; init; }
    public required Polyline VisualLine { get; init; }
    public required Polyline PreviewLine { get; init; }
    public required Line ExpectedLine { get; init; }
}

internal sealed class FrameTimeOverlayPresentationController
{
    private readonly FrameTimeOverlayPresentationControllerContext _context;

    public FrameTimeOverlayPresentationController(FrameTimeOverlayPresentationControllerContext context)
    {
        _context = context;
    }

    public void Apply(StatsSnapshot snapshot)
    {
        var presentation = StatsPresentationBuilder.BuildFrameTimePresentation(snapshot);

        SetTextIfChanged(_context.SourceValue, presentation.SourceText);
        SetTextIfChanged(_context.VisualValue, presentation.VisualText);
        SetTextIfChanged(_context.PreviewValue, presentation.PreviewText);
        SetTextIfChanged(_context.LatencyValue, presentation.LatencyText);
        SetTextIfChanged(_context.StatusValue, presentation.StatusText);

        UpdateExpectedLine(presentation.Range);
        UpdateLine(_context.VisualLine, presentation.VisualSamples, presentation.Range);
        UpdateLine(_context.PreviewLine, presentation.PreviewSamples, presentation.Range);
    }

    private void UpdateLine(
        Polyline line,
        IReadOnlyList<double> samples,
        StatsFrameTimeRange range)
    {
        line.Points.Clear();
        if (samples.Count <= 1)
        {
            return;
        }

        var canvasSize = FrameTimeOverlayGeometry.ResolveCanvasSize(
            _context.Canvas.ActualWidth,
            _context.Canvas.ActualHeight);
        for (var i = 0; i < samples.Count; i++)
        {
            line.Points.Add(FrameTimeOverlayGeometry.ProjectSample(i, samples.Count, samples[i], range, canvasSize));
        }
    }

    private void UpdateExpectedLine(StatsFrameTimeRange range)
    {
        var canvasSize = FrameTimeOverlayGeometry.ResolveCanvasSize(
            _context.Canvas.ActualWidth,
            _context.Canvas.ActualHeight);
        var line = FrameTimeOverlayGeometry.ProjectExpectedLine(range, canvasSize);
        _context.ExpectedLine.X2 = line.X2;
        _context.ExpectedLine.Y1 = line.Y;
        _context.ExpectedLine.Y2 = line.Y;
    }

    private static void SetTextIfChanged(TextBlock target, string value)
    {
        if (!string.Equals(target.Text, value, StringComparison.Ordinal))
        {
            target.Text = value;
        }
    }
}

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
