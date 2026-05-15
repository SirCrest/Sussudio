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

        var width = _context.Canvas.ActualWidth > 1 ? _context.Canvas.ActualWidth : 500;
        var height = _context.Canvas.ActualHeight > 1 ? _context.Canvas.ActualHeight : 92;
        for (var i = 0; i < samples.Count; i++)
        {
            var x = samples.Count == 1 ? 0 : i * width / (samples.Count - 1);
            var normalized = Math.Clamp((samples[i] - range.MinMs) / range.SpanMs, 0.0, 1.0);
            var y = height - normalized * height;
            line.Points.Add(new Point(x, y));
        }
    }

    private void UpdateExpectedLine(StatsFrameTimeRange range)
    {
        var width = _context.Canvas.ActualWidth > 1 ? _context.Canvas.ActualWidth : 500;
        var height = _context.Canvas.ActualHeight > 1 ? _context.Canvas.ActualHeight : 92;
        var normalized = Math.Clamp((range.ExpectedMs - range.MinMs) / range.SpanMs, 0.0, 1.0);
        var y = height - normalized * height;
        _context.ExpectedLine.X2 = width;
        _context.ExpectedLine.Y1 = y;
        _context.ExpectedLine.Y2 = y;
    }

    private static void SetTextIfChanged(TextBlock target, string value)
    {
        if (!string.Equals(target.Text, value, StringComparison.Ordinal))
        {
            target.Text = value;
        }
    }
}
