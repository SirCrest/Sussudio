using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewRuntimeProjection BuildPreviewRuntimeProjection(
        PreviewRuntimeSnapshot previewRuntime,
        PreviewHdrState previewHdrState,
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            FramesArrived = previewRuntime.FramesArrived,
            FramesDisplayed = previewRuntime.FramesDisplayed,
            FramesDropped = previewRuntime.FramesDropped,
            CadenceSampleCount = previewRuntime.DisplayCadenceSampleCount,
            CadenceObservedFps = previewRuntime.DisplayCadenceObservedFps,
            CadenceExpectedIntervalMs = previewRuntime.DisplayCadenceExpectedIntervalMs,
            CadenceAverageIntervalMs = previewRuntime.DisplayCadenceAverageIntervalMs,
            CadenceP95IntervalMs = previewRuntime.DisplayCadenceP95IntervalMs,
            CadenceP99IntervalMs = previewRuntime.DisplayCadenceP99IntervalMs,
            CadenceMaxIntervalMs = previewRuntime.DisplayCadenceMaxIntervalMs,
            CadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,
            CadenceFivePercentLowFps = previewRuntime.DisplayCadenceFivePercentLowFps,
            CadenceSampleDurationMs = previewRuntime.DisplayCadenceSampleDurationMs,
            CadenceRecentIntervalsMs = previewRuntime.DisplayCadenceRecentIntervalsMs,
            CadenceJitterStdDevMs = previewRuntime.DisplayCadenceJitterStdDevMs,
            CadenceSlowFrameCount = previewRuntime.DisplayCadenceSlowFrameCount,
            CadenceSlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent,
            GpuActive = previewRuntime.GpuActive,
            PlaceholderVisible = previewRuntime.PlaceholderVisible,
            GpuElementVisible = previewRuntime.GpuElementVisible,
            CpuElementVisible = previewRuntime.CpuElementVisible,
            RendererAttached = previewRuntime.RendererAttached,
            StartupState = previewRuntime.StartupState,
            AttemptId = previewRuntime.StartupAttemptId,
            StartupElapsedMs = previewRuntime.StartupElapsedMs,
            StartupTimeoutMs = previewRuntime.StartupTimeoutMs,
            GpuSignalMediaOpened = previewRuntime.StartupGpuSignalMediaOpened,
            GpuSignalFirstFrame = previewRuntime.StartupGpuSignalFirstFrame,
            GpuSignalPlaybackAdvancing = previewRuntime.StartupGpuSignalPlaybackAdvancing,
            StartupRequiredSignals = previewRuntime.StartupRequiredSignals,
            StartupReceivedSignals = previewRuntime.StartupReceivedSignals,
            StartupStrategy = previewRuntime.StartupStrategy.ToString(),
            StartupMissingSignals = previewRuntime.StartupMissingSignals,
            RecoveryAttemptCount = previewRuntime.StartupRecoveryAttemptCount,
            LastFailureReason = previewRuntime.StartupLastFailureReason,
            FirstVisualConfirmed = previewRuntime.FirstVisualConfirmed,
            BlankSuspected = previewRuntime.BlankSuspected,
            Stalled = previewRuntime.StallSuspected,
            RendererMode = previewRuntime.RendererMode,
            GpuPlaybackState = previewRuntime.GpuPlaybackState,
            GpuNaturalVideoWidth = previewRuntime.GpuNaturalVideoWidth,
            GpuNaturalVideoHeight = previewRuntime.GpuNaturalVideoHeight,
            GpuPositionMs = previewRuntime.GpuPositionMs,
            GpuPositionEventCount = previewRuntime.GpuPositionEventCount,
            HdrInputDetected = previewHdrState.InputDetected,
            ToneMapMode = previewHdrState.ToneMapMode,
            ColorContext = captureRuntime.NegotiatedPixelFormat,
            AdapterColorMetadata = captureRuntime.PreviewColorMetadata
        };

    private readonly record struct PreviewRuntimeProjection
    {
        public long FramesArrived { get; init; }
        public long FramesDisplayed { get; init; }
        public long FramesDropped { get; init; }
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
