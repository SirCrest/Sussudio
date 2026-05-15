using System;

namespace Sussudio;

// Stats snapshot assembly for shell polling. UI text/brush projection stays in
// MainWindow.StatsOverlay.cs; pure snapshot shaping stays in StatsSnapshotBuilder.
public sealed partial class MainWindow
{
    private StatsSnapshot GetStatsSnapshot()
    {
        var health = ViewModel.GetCaptureHealthSnapshot();
        var d3d = _d3dRenderer;
        var presentCadence = d3d?.GetPresentCadenceMetrics(_previewMinPresentationIntervalMs);
        var renderer = new StatsSnapshotRenderMetrics(
            PreviewCadenceSamples: presentCadence?.SampleCount ?? 0,
            PreviewObservedFps: presentCadence?.ObservedFps ?? 0,
            PreviewAvgIntervalMs: presentCadence?.AverageIntervalMs ?? 0,
            PreviewP95IntervalMs: presentCadence?.P95IntervalMs ?? 0,
            PreviewP99IntervalMs: presentCadence?.P99IntervalMs ?? 0,
            PreviewOnePercentLowFps: presentCadence?.OnePercentLowFps ?? 0,
            PreviewSlowFrames: presentCadence?.SlowFrameCount ?? 0,
            PreviewSlowPercent: presentCadence?.SlowFramePercent ?? 0,
            PipelineLatencyMs: d3d?.GetEstimatedPipelineLatencyMs() ?? 0,
            FramesSubmitted: d3d?.FramesSubmitted ?? 0,
            FramesRendered: d3d?.FramesRendered ?? 0,
            FramesDropped: d3d?.FramesDropped ?? 0,
            PreviewNaturalWidth: d3d?.NaturalWidth ?? 0,
            PreviewNaturalHeight: d3d?.NaturalHeight ?? 0,
            PreviewRecentPresentIntervalsMs: d3d?.GetRecentPresentIntervalsMs(180) ?? Array.Empty<double>(),
            PreviewRecentLatencyMs: d3d?.GetRecentPipelineLatencyMs(180) ?? Array.Empty<double>());
        var viewState = new StatsSnapshotViewState(ViewModel.IsPreviewing, ViewModel.IsRecording);

        return StatsSnapshotBuilder.Build(health, renderer, viewState);
    }
}
