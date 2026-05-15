using System;
using Sussudio.Models;
using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

internal sealed class StatsSnapshotProviderContext
{
    public required Func<CaptureHealthSnapshot> GetCaptureHealthSnapshot { get; init; }
    public required Func<D3D11PreviewRenderer?> GetRenderer { get; init; }
    public required Func<double> GetPreviewMinPresentationIntervalMs { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsRecording { get; init; }
}

internal sealed class StatsSnapshotProvider
{
    private const int RecentSampleCount = 180;

    private readonly StatsSnapshotProviderContext _context;

    public StatsSnapshotProvider(StatsSnapshotProviderContext context)
    {
        _context = context;
    }

    public StatsSnapshot GetSnapshot()
    {
        var health = _context.GetCaptureHealthSnapshot();
        var renderer = BuildRenderMetrics(_context.GetRenderer(), _context.GetPreviewMinPresentationIntervalMs());
        var viewState = new StatsSnapshotViewState(_context.IsPreviewing(), _context.IsRecording());

        return StatsSnapshotBuilder.Build(health, renderer, viewState);
    }

    private static StatsSnapshotRenderMetrics BuildRenderMetrics(
        D3D11PreviewRenderer? renderer,
        double previewMinPresentationIntervalMs)
    {
        var presentCadence = renderer?.GetPresentCadenceMetrics(previewMinPresentationIntervalMs);
        return new StatsSnapshotRenderMetrics(
            PreviewCadenceSamples: presentCadence?.SampleCount ?? 0,
            PreviewObservedFps: presentCadence?.ObservedFps ?? 0,
            PreviewAvgIntervalMs: presentCadence?.AverageIntervalMs ?? 0,
            PreviewP95IntervalMs: presentCadence?.P95IntervalMs ?? 0,
            PreviewP99IntervalMs: presentCadence?.P99IntervalMs ?? 0,
            PreviewOnePercentLowFps: presentCadence?.OnePercentLowFps ?? 0,
            PreviewSlowFrames: presentCadence?.SlowFrameCount ?? 0,
            PreviewSlowPercent: presentCadence?.SlowFramePercent ?? 0,
            PipelineLatencyMs: renderer?.GetEstimatedPipelineLatencyMs() ?? 0,
            FramesSubmitted: renderer?.FramesSubmitted ?? 0,
            FramesRendered: renderer?.FramesRendered ?? 0,
            FramesDropped: renderer?.FramesDropped ?? 0,
            PreviewNaturalWidth: renderer?.NaturalWidth ?? 0,
            PreviewNaturalHeight: renderer?.NaturalHeight ?? 0,
            PreviewRecentPresentIntervalsMs: renderer?.GetRecentPresentIntervalsMs(RecentSampleCount) ?? Array.Empty<double>(),
            PreviewRecentLatencyMs: renderer?.GetRecentPipelineLatencyMs(RecentSampleCount) ?? Array.Empty<double>());
    }
}
