using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewRuntimeFlattenedProjection BuildPreviewRuntimeFlattenedProjection(
        PreviewRuntimeProjection previewSummary)
        => new()
        {
            FramesArrived = previewSummary.FramesArrived,
            FramesDisplayed = previewSummary.FramesDisplayed,
            FramesDropped = previewSummary.FramesDropped,
            EstimatedPipelineLatencyMs = previewSummary.EstimatedPipelineLatencyMs,
            CadenceSampleCount = previewSummary.Cadence.SampleCount,
            CadenceObservedFps = previewSummary.Cadence.ObservedFps,
            CadenceExpectedIntervalMs = previewSummary.Cadence.ExpectedIntervalMs,
            CadenceAverageIntervalMs = previewSummary.Cadence.AverageIntervalMs,
            CadenceP95IntervalMs = previewSummary.Cadence.P95IntervalMs,
            CadenceP99IntervalMs = previewSummary.Cadence.P99IntervalMs,
            CadenceMaxIntervalMs = previewSummary.Cadence.MaxIntervalMs,
            CadenceOnePercentLowFps = previewSummary.Cadence.OnePercentLowFps,
            CadenceFivePercentLowFps = previewSummary.Cadence.FivePercentLowFps,
            CadenceSampleDurationMs = previewSummary.Cadence.SampleDurationMs,
            CadenceRecentIntervalsMs = previewSummary.Cadence.RecentIntervalsMs,
            CadenceJitterStdDevMs = previewSummary.Cadence.JitterStdDevMs,
            CadenceSlowFrameCount = previewSummary.Cadence.SlowFrameCount,
            CadenceSlowFramePercent = previewSummary.Cadence.SlowFramePercent,
            GpuActive = previewSummary.GpuActive,
            PlaceholderVisible = previewSummary.PlaceholderVisible,
            GpuElementVisible = previewSummary.GpuElementVisible,
            CpuElementVisible = previewSummary.CpuElementVisible,
            RendererAttached = previewSummary.RendererAttached,
            StartupState = previewSummary.Startup.State,
            AttemptId = previewSummary.Startup.AttemptId,
            StartupElapsedMs = previewSummary.Startup.ElapsedMs,
            StartupTimeoutMs = previewSummary.Startup.TimeoutMs,
            GpuSignalMediaOpened = previewSummary.Startup.GpuSignalMediaOpened,
            GpuSignalFirstFrame = previewSummary.Startup.GpuSignalFirstFrame,
            GpuSignalPlaybackAdvancing = previewSummary.Startup.GpuSignalPlaybackAdvancing,
            StartupRequiredSignals = previewSummary.Startup.RequiredSignals,
            StartupReceivedSignals = previewSummary.Startup.ReceivedSignals,
            StartupStrategy = previewSummary.Startup.Strategy,
            StartupMissingSignals = previewSummary.Startup.MissingSignals,
            RecoveryAttemptCount = previewSummary.Startup.RecoveryAttemptCount,
            LastFailureReason = previewSummary.Startup.LastFailureReason,
            FirstVisualConfirmed = previewSummary.Startup.FirstVisualConfirmed,
            BlankSuspected = previewSummary.Startup.BlankSuspected,
            Stalled = previewSummary.Startup.Stalled,
            RendererMode = previewSummary.Startup.RendererMode,
            GpuPlaybackState = previewSummary.GpuPlaybackState,
            GpuNaturalVideoWidth = previewSummary.GpuNaturalVideoWidth,
            GpuNaturalVideoHeight = previewSummary.GpuNaturalVideoHeight,
            GpuPositionMs = previewSummary.GpuPositionMs,
            GpuPositionEventCount = previewSummary.GpuPositionEventCount,
            HdrInputDetected = previewSummary.HdrInputDetected,
            ToneMapMode = previewSummary.ToneMapMode,
            ColorContext = previewSummary.ColorContext,
            AdapterColorMetadata = previewSummary.AdapterColorMetadata
        };

    private readonly record struct PreviewRuntimeFlattenedProjection
    {
        public long FramesArrived { get; init; }
        public long FramesDisplayed { get; init; }
        public long FramesDropped { get; init; }
        public long EstimatedPipelineLatencyMs { get; init; }
        public int CadenceSampleCount { get; init; }
        public double CadenceObservedFps { get; init; }
        public double CadenceExpectedIntervalMs { get; init; }
        public double CadenceAverageIntervalMs { get; init; }
        public double CadenceP95IntervalMs { get; init; }
        public double CadenceP99IntervalMs { get; init; }
        public double CadenceMaxIntervalMs { get; init; }
        public double CadenceOnePercentLowFps { get; init; }
        public double CadenceFivePercentLowFps { get; init; }
        public double CadenceSampleDurationMs { get; init; }
        public double[] CadenceRecentIntervalsMs { get; init; }
        public double CadenceJitterStdDevMs { get; init; }
        public long CadenceSlowFrameCount { get; init; }
        public double CadenceSlowFramePercent { get; init; }
        public bool GpuActive { get; init; }
        public bool PlaceholderVisible { get; init; }
        public bool GpuElementVisible { get; init; }
        public bool CpuElementVisible { get; init; }
        public bool RendererAttached { get; init; }
        public string StartupState { get; init; }
        public string? AttemptId { get; init; }
        public double? StartupElapsedMs { get; init; }
        public int StartupTimeoutMs { get; init; }
        public bool GpuSignalMediaOpened { get; init; }
        public bool GpuSignalFirstFrame { get; init; }
        public bool GpuSignalPlaybackAdvancing { get; init; }
        public PreviewStartupSignalFlags StartupRequiredSignals { get; init; }
        public PreviewStartupSignalFlags StartupReceivedSignals { get; init; }
        public string StartupStrategy { get; init; }
        public string? StartupMissingSignals { get; init; }
        public int RecoveryAttemptCount { get; init; }
        public string? LastFailureReason { get; init; }
        public bool FirstVisualConfirmed { get; init; }
        public bool BlankSuspected { get; init; }
        public bool Stalled { get; init; }
        public string RendererMode { get; init; }
        public string GpuPlaybackState { get; init; }
        public int GpuNaturalVideoWidth { get; init; }
        public int GpuNaturalVideoHeight { get; init; }
        public double GpuPositionMs { get; init; }
        public long GpuPositionEventCount { get; init; }
        public bool HdrInputDetected { get; init; }
        public string ToneMapMode { get; init; }
        public string? ColorContext { get; init; }
        public string AdapterColorMetadata { get; init; }
    }
}
