using System;
using System.Collections.Generic;
using Sussudio.ViewModels;
using Windows.Foundation;

namespace Sussudio;

// Frame-time overlay presentation for the compact live graph. Stats snapshot
// construction and dock row projection stay in MainWindow.StatsOverlay.cs.
public sealed partial class MainWindow
{
    private bool IsFrameTimeOverlayVisible()
        => _statsOverlayController.IsFrameTimeOverlayVisible;

    private void UpdateFrameTimeOverlay(StatsSnapshot snapshot)
    {
        if (!IsFrameTimeOverlayVisible())
        {
            return;
        }

        var presentation = StatsPresentationBuilder.BuildFrameTimePresentation(snapshot);
        SetTextIfChanged(FrameTime_SourceValue, presentation.SourceText);
        SetTextIfChanged(FrameTime_VisualValue, presentation.VisualText);
        SetTextIfChanged(FrameTime_PreviewValue, presentation.PreviewText);
        SetTextIfChanged(FrameTime_LatencyValue, presentation.LatencyText);
        SetTextIfChanged(FrameTime_StatusValue, presentation.StatusText);

        UpdateFrameTimeExpectedLine(presentation.Range);

        UpdateFrameTimeLine(
            FrameTime_VisualLine,
            presentation.VisualSamples,
            presentation.Range);
        UpdateFrameTimeLine(
            FrameTime_PreviewLine,
            presentation.PreviewSamples,
            presentation.Range);
    }

    private void UpdateFrameTimeLine(
        Microsoft.UI.Xaml.Shapes.Polyline line,
        IReadOnlyList<double> samples,
        StatsFrameTimeRange range)
    {
        line.Points.Clear();
        if (samples.Count <= 1)
        {
            return;
        }

        var width = FrameTime_Canvas.ActualWidth > 1 ? FrameTime_Canvas.ActualWidth : 500;
        var height = FrameTime_Canvas.ActualHeight > 1 ? FrameTime_Canvas.ActualHeight : 92;
        for (var i = 0; i < samples.Count; i++)
        {
            var x = samples.Count == 1 ? 0 : i * width / (samples.Count - 1);
            var normalized = Math.Clamp((samples[i] - range.MinMs) / range.SpanMs, 0.0, 1.0);
            var y = height - normalized * height;
            line.Points.Add(new Point(x, y));
        }
    }

    private void UpdateFrameTimeExpectedLine(StatsFrameTimeRange range)
    {
        var width = FrameTime_Canvas.ActualWidth > 1 ? FrameTime_Canvas.ActualWidth : 500;
        var height = FrameTime_Canvas.ActualHeight > 1 ? FrameTime_Canvas.ActualHeight : 92;
        var normalized = Math.Clamp((range.ExpectedMs - range.MinMs) / range.SpanMs, 0.0, 1.0);
        var y = height - normalized * height;
        FrameTime_ExpectedLine.X2 = width;
        FrameTime_ExpectedLine.Y1 = y;
        FrameTime_ExpectedLine.Y2 = y;
    }
}
