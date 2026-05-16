using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Sussudio.ViewModels;

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
