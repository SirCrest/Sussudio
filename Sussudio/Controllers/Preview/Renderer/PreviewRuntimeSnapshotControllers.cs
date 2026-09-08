using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Preview;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class PreviewRuntimeSnapshotSamplingControllerContext
{
    public required WindowUiDispatchController UiDispatchController { get; init; }
    public required MainViewModel ViewModel { get; init; }
    public required PreviewRendererHostController RendererHostController { get; init; }
    public required PreviewStartupSessionController StartupSessionController { get; init; }
    public required PreviewStartupSignalCoordinator StartupSignalCoordinator { get; init; }
    public required Func<bool> IsGpuElementVisible { get; init; }
    public required Func<bool> IsCpuElementVisible { get; init; }
    public required Func<bool> IsPlaceholderVisible { get; init; }
    public required Func<int> GetStartupVisualTimeoutMs { get; init; }
}

internal sealed class PreviewRuntimeSnapshotSamplingController
{
    private readonly PreviewRuntimeSnapshotSamplingControllerContext _context;
    private readonly object _previewRuntimeSnapshotEpochLock = new();
    private long _previewRuntimeSnapshotEpoch;
    private PreviewRuntimeSnapshotSignature _lastPreviewRuntimeSnapshotSignature;

    public PreviewRuntimeSnapshotSamplingController(PreviewRuntimeSnapshotSamplingControllerContext context)
    {
        _context = context;
    }

    public Task<PreviewRuntimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
        => _context.UiDispatchController.InvokeWithRetryAsync(
            BuildSnapshot,
            "Failed to enqueue preview snapshot operation.",
            cancellationToken);

    private PreviewRuntimeSnapshot BuildSnapshot()
    {
        var startupSession = _context.StartupSessionController;
        var startupSignals = _context.StartupSignalCoordinator;
        var startupSignalSnapshot = startupSignals.Snapshot;
        var startupMissingSignals = startupSession.MissingSignals;
        if (string.IsNullOrWhiteSpace(startupMissingSignals) &&
            startupSession.ShouldRefreshMissingSignalsForSnapshot)
        {
            startupMissingSignals = startupSignals.BuildMissingSignals();
        }

        var rendererHost = _context.RendererHostController;
        var signature = new PreviewRuntimeSnapshotSignature(
            _context.ViewModel.IsPreviewing,
            rendererHost.IsCpuPreviewSourceAttached,
            _context.IsGpuElementVisible(),
            _context.IsCpuElementVisible(),
            _context.IsPlaceholderVisible(),
            rendererHost.FramesArrived,
            rendererHost.FramesDisplayed,
            rendererHost.FramesDropped,
            rendererHost.LastPresentedTick,
            rendererHost.PreviewMinPresentationIntervalMs,
            startupSession.State.ToString(),
            startupSession.IsWaitingForFirstVisual,
            startupSession.AttemptId,
            startupSession.RequestedUtc,
            _context.GetStartupVisualTimeoutMs(),
            startupSignalSnapshot.GpuSignalMediaOpened,
            startupSignalSnapshot.GpuSignalFirstFrame,
            startupSignalSnapshot.GpuSignalPlaybackAdvancing,
            startupSignalSnapshot.RequiredSignals,
            startupSignalSnapshot.ReceivedSignals,
            startupSignalSnapshot.Strategy,
            startupMissingSignals,
            startupSession.RecoveryAttemptCount,
            startupSession.LastFailureReason,
            startupSession.FirstVisualConfirmed,
            startupSignals.PositionEventCount);
        var previewRuntimeEpoch = GetOrAdvancePreviewRuntimeSnapshotEpoch(signature);
        return PreviewRuntimeSnapshotController.Build(new PreviewRuntimeSnapshotInput
        {
            PreviewRuntimeEpoch = previewRuntimeEpoch,
            D3DRenderer = rendererHost.Renderer,
            IsPreviewing = signature.IsPreviewing,
            PreviewSourceAttached = signature.PreviewSourceAttached,
            GpuElementVisible = signature.GpuElementVisible,
            CpuElementVisible = signature.CpuElementVisible,
            PlaceholderVisible = signature.PlaceholderVisible,
            FramesArrived = signature.FramesArrived,
            FramesDisplayed = signature.FramesDisplayed,
            FramesDropped = signature.FramesDropped,
            LastPresentedTick = signature.LastPresentedTick,
            PreviewMinPresentationIntervalMs = signature.PreviewMinPresentationIntervalMs,
            StartupState = signature.StartupState,
            IsStartupWaitingForFirstVisual = signature.IsStartupWaitingForFirstVisual,
            StartupAttemptId = signature.StartupAttemptId,
            StartupRequestedUtc = signature.StartupRequestedUtc,
            StartupTimeoutMs = signature.StartupTimeoutMs,
            StartupGpuSignalMediaOpened = signature.StartupGpuSignalMediaOpened,
            StartupGpuSignalFirstFrame = signature.StartupGpuSignalFirstFrame,
            StartupGpuSignalPlaybackAdvancing = signature.StartupGpuSignalPlaybackAdvancing,
            StartupRequiredSignals = signature.StartupRequiredSignals,
            StartupReceivedSignals = signature.StartupReceivedSignals,
            StartupStrategy = signature.StartupStrategy,
            StartupMissingSignals = signature.StartupMissingSignals,
            StartupRecoveryAttemptCount = signature.StartupRecoveryAttemptCount,
            StartupLastFailureReason = signature.StartupLastFailureReason,
            FirstVisualConfirmed = signature.FirstVisualConfirmed,
            GpuPositionEventCount = signature.GpuPositionEventCount
        });
    }

    private long GetOrAdvancePreviewRuntimeSnapshotEpoch(PreviewRuntimeSnapshotSignature signature)
    {
        lock (_previewRuntimeSnapshotEpochLock)
        {
            if (!_lastPreviewRuntimeSnapshotSignature.Equals(signature))
            {
                _lastPreviewRuntimeSnapshotSignature = signature;
                Interlocked.Increment(ref _previewRuntimeSnapshotEpoch);
            }

            return Interlocked.Read(ref _previewRuntimeSnapshotEpoch);
        }
    }
}

internal readonly record struct PreviewRuntimeSnapshotSignature(
    bool IsPreviewing,
    bool PreviewSourceAttached,
    bool GpuElementVisible,
    bool CpuElementVisible,
    bool PlaceholderVisible,
    long FramesArrived,
    long FramesDisplayed,
    long FramesDropped,
    long LastPresentedTick,
    double PreviewMinPresentationIntervalMs,
    string StartupState,
    bool IsStartupWaitingForFirstVisual,
    string? StartupAttemptId,
    DateTimeOffset? StartupRequestedUtc,
    int StartupTimeoutMs,
    bool StartupGpuSignalMediaOpened,
    bool StartupGpuSignalFirstFrame,
    bool StartupGpuSignalPlaybackAdvancing,
    PreviewStartupSignalFlags StartupRequiredSignals,
    PreviewStartupSignalFlags StartupReceivedSignals,
    PreviewStartupStrategy StartupStrategy,
    string? StartupMissingSignals,
    int StartupRecoveryAttemptCount,
    string? StartupLastFailureReason,
    bool FirstVisualConfirmed,
    long GpuPositionEventCount);

internal sealed class PreviewRuntimeSnapshotInput
{
    public long PreviewRuntimeEpoch { get; init; }
    public D3D11PreviewRenderer? D3DRenderer { get; init; }
    public bool IsPreviewing { get; init; }
    public bool PreviewSourceAttached { get; init; }
    public bool GpuElementVisible { get; init; }
    public bool CpuElementVisible { get; init; }
    public bool PlaceholderVisible { get; init; }
    public long FramesArrived { get; init; }
    public long FramesDisplayed { get; init; }
    public long FramesDropped { get; init; }
    public long LastPresentedTick { get; init; }
    public double PreviewMinPresentationIntervalMs { get; init; }
    public string StartupState { get; init; } = "Idle";
    public bool IsStartupWaitingForFirstVisual { get; init; }
    public string? StartupAttemptId { get; init; }
    public DateTimeOffset? StartupRequestedUtc { get; init; }
    public int StartupTimeoutMs { get; init; }
    public bool StartupGpuSignalMediaOpened { get; init; }
    public bool StartupGpuSignalFirstFrame { get; init; }
    public bool StartupGpuSignalPlaybackAdvancing { get; init; }
    public PreviewStartupSignalFlags StartupRequiredSignals { get; init; }
    public PreviewStartupSignalFlags StartupReceivedSignals { get; init; }
    public PreviewStartupStrategy StartupStrategy { get; init; }
    public string? StartupMissingSignals { get; init; }
    public int StartupRecoveryAttemptCount { get; init; }
    public string? StartupLastFailureReason { get; init; }
    public bool FirstVisualConfirmed { get; init; }
    public long GpuPositionEventCount { get; init; }
}

internal static class PreviewRuntimeSnapshotController
{
    public static PreviewRuntimeSnapshot Build(PreviewRuntimeSnapshotInput input)
    {
        var d3dProjection = PreviewRuntimeD3DProjection.Build(input);
        var healthInput = PreviewRuntimeSnapshotHealthInputFactory.Build(
            input,
            d3dProjection,
            Environment.TickCount64,
            DateTimeOffset.UtcNow);
        var health = PreviewRuntimeSnapshotHealthPolicy.Evaluate(healthInput);

        return PreviewRuntimeSnapshotMapper.Build(input, d3dProjection, health, DateTimeOffset.UtcNow);
    }
}

internal static class PreviewRuntimeSnapshotMapper
{
    public static PreviewRuntimeSnapshot Build(
        PreviewRuntimeSnapshotInput input,
        PreviewRuntimeD3DProjection d3dProjection,
        PreviewRuntimeSnapshotHealth health,
        DateTimeOffset timestampUtc)
    {
        return new PreviewRuntimeSnapshot
        {
            TimestampUtc = timestampUtc,
            PreviewRuntimeEpoch = input.PreviewRuntimeEpoch,
            IsPreviewing = input.IsPreviewing,
            GpuActive = d3dProjection.GpuActive,
            PlaceholderVisible = input.PlaceholderVisible,
            GpuElementVisible = input.GpuElementVisible,
            CpuElementVisible = input.CpuElementVisible,
            RendererAttached = d3dProjection.RendererAttached,
            StartupState = input.StartupState,
            StartupAttemptId = input.StartupAttemptId,
            StartupElapsedMs = health.StartupElapsedMs,
            StartupTimeoutMs = input.StartupTimeoutMs,
            StartupGpuSignalMediaOpened = input.StartupGpuSignalMediaOpened,
            StartupGpuSignalFirstFrame = input.StartupGpuSignalFirstFrame,
            StartupGpuSignalPlaybackAdvancing = input.StartupGpuSignalPlaybackAdvancing,
            StartupRequiredSignals = input.StartupRequiredSignals,
            StartupReceivedSignals = input.StartupReceivedSignals,
            StartupStrategy = input.StartupStrategy,
            StartupMissingSignals = input.StartupMissingSignals,
            StartupRecoveryAttemptCount = input.StartupRecoveryAttemptCount,
            StartupLastFailureReason = input.StartupLastFailureReason,
            FirstVisualConfirmed = input.FirstVisualConfirmed,
            FramesArrived = d3dProjection.FramesArrived,
            FramesDisplayed = d3dProjection.FramesDisplayed,
            FramesDropped = d3dProjection.FramesDropped,
            DisplayCadenceSampleCount = d3dProjection.DisplayCadenceSampleCount,
            DisplayCadenceObservedFps = d3dProjection.DisplayCadenceObservedFps,
            DisplayCadenceExpectedIntervalMs = d3dProjection.DisplayCadenceExpectedIntervalMs,
            DisplayCadenceAverageIntervalMs = d3dProjection.DisplayCadenceAverageIntervalMs,
            DisplayCadenceP95IntervalMs = d3dProjection.DisplayCadenceP95IntervalMs,
            DisplayCadenceP99IntervalMs = d3dProjection.DisplayCadenceP99IntervalMs,
            DisplayCadenceMaxIntervalMs = d3dProjection.DisplayCadenceMaxIntervalMs,
            DisplayCadenceOnePercentLowFps = d3dProjection.DisplayCadenceOnePercentLowFps,
            DisplayCadenceFivePercentLowFps = d3dProjection.DisplayCadenceFivePercentLowFps,
            DisplayCadenceSampleDurationMs = d3dProjection.DisplayCadenceSampleDurationMs,
            DisplayCadenceRecentIntervalsMs = d3dProjection.DisplayCadenceRecentIntervalsMs,
            DisplayCadenceJitterStdDevMs = d3dProjection.DisplayCadenceJitterStdDevMs,
            DisplayCadenceSlowFrameCount = d3dProjection.DisplayCadenceSlowFrameCount,
            DisplayCadenceSlowFramePercent = d3dProjection.DisplayCadenceSlowFramePercent,
            BlankSuspected = health.BlankSuspected,
            StallSuspected = health.StallSuspected,
            RendererMode = d3dProjection.RendererMode,
            D3DPresentSyncInterval = d3dProjection.D3DPresentSyncInterval,
            D3DMaxFrameLatency = d3dProjection.D3DMaxFrameLatency,
            D3DSwapChainBufferCount = d3dProjection.D3DSwapChainBufferCount,
            D3DSwapChainAddress = d3dProjection.D3DSwapChainAddress,
            D3DFramesSubmitted = d3dProjection.D3DFramesSubmitted,
            D3DFramesRendered = d3dProjection.D3DFramesRendered,
            D3DFramesDropped = d3dProjection.D3DFramesDropped,
            D3DRenderThreadFailureCount = d3dProjection.D3DRenderThreadFailureCount,
            D3DLastRenderThreadFailureType = d3dProjection.D3DLastRenderThreadFailureType,
            D3DLastRenderThreadFailureMessage = d3dProjection.D3DLastRenderThreadFailureMessage,
            D3DLastRenderThreadFailureHResult = d3dProjection.D3DLastRenderThreadFailureHResult,
            D3DPendingFrameCount = d3dProjection.D3DPendingFrameCount,
            D3DInputColorSpace = d3dProjection.D3DInputColorSpace,
            D3DOutputColorSpace = d3dProjection.D3DOutputColorSpace,
            D3DCpuTimingSampleCount = d3dProjection.D3DCpuTimingSampleCount,
            D3DInputUploadCpuAvgMs = d3dProjection.D3DInputUploadCpuAvgMs,
            D3DInputUploadCpuP95Ms = d3dProjection.D3DInputUploadCpuP95Ms,
            D3DInputUploadCpuP99Ms = d3dProjection.D3DInputUploadCpuP99Ms,
            D3DInputUploadCpuMaxMs = d3dProjection.D3DInputUploadCpuMaxMs,
            D3DRenderSubmitCpuAvgMs = d3dProjection.D3DRenderSubmitCpuAvgMs,
            D3DRenderSubmitCpuP95Ms = d3dProjection.D3DRenderSubmitCpuP95Ms,
            D3DRenderSubmitCpuP99Ms = d3dProjection.D3DRenderSubmitCpuP99Ms,
            D3DRenderSubmitCpuMaxMs = d3dProjection.D3DRenderSubmitCpuMaxMs,
            D3DPresentCallAvgMs = d3dProjection.D3DPresentCallAvgMs,
            D3DPresentCallP95Ms = d3dProjection.D3DPresentCallP95Ms,
            D3DPresentCallP99Ms = d3dProjection.D3DPresentCallP99Ms,
            D3DPresentCallMaxMs = d3dProjection.D3DPresentCallMaxMs,
            D3DTotalFrameCpuAvgMs = d3dProjection.D3DTotalFrameCpuAvgMs,
            D3DTotalFrameCpuP95Ms = d3dProjection.D3DTotalFrameCpuP95Ms,
            D3DTotalFrameCpuP99Ms = d3dProjection.D3DTotalFrameCpuP99Ms,
            D3DTotalFrameCpuMaxMs = d3dProjection.D3DTotalFrameCpuMaxMs,
            D3DPipelineLatencySampleCount = d3dProjection.D3DPipelineLatencySampleCount,
            D3DPipelineLatencyAvgMs = d3dProjection.D3DPipelineLatencyAvgMs,
            D3DPipelineLatencyP95Ms = d3dProjection.D3DPipelineLatencyP95Ms,
            D3DPipelineLatencyP99Ms = d3dProjection.D3DPipelineLatencyP99Ms,
            D3DPipelineLatencyMaxMs = d3dProjection.D3DPipelineLatencyMaxMs,
            D3DFrameLatencyWaitEnabled = d3dProjection.D3DFrameLatencyWaitEnabled,
            D3DFrameLatencyWaitHandleActive = d3dProjection.D3DFrameLatencyWaitHandleActive,
            D3DFrameLatencyWaitCallCount = d3dProjection.D3DFrameLatencyWaitCallCount,
            D3DFrameLatencyWaitSignaledCount = d3dProjection.D3DFrameLatencyWaitSignaledCount,
            D3DFrameLatencyWaitTimeoutCount = d3dProjection.D3DFrameLatencyWaitTimeoutCount,
            D3DFrameLatencyWaitUnexpectedResultCount = d3dProjection.D3DFrameLatencyWaitUnexpectedResultCount,
            D3DFrameLatencyWaitLastResult = d3dProjection.D3DFrameLatencyWaitLastResult,
            D3DFrameLatencyWaitLastMs = d3dProjection.D3DFrameLatencyWaitLastMs,
            D3DFrameLatencyWaitSampleCount = d3dProjection.D3DFrameLatencyWaitSampleCount,
            D3DFrameLatencyWaitAvgMs = d3dProjection.D3DFrameLatencyWaitAvgMs,
            D3DFrameLatencyWaitP95Ms = d3dProjection.D3DFrameLatencyWaitP95Ms,
            D3DFrameLatencyWaitP99Ms = d3dProjection.D3DFrameLatencyWaitP99Ms,
            D3DFrameLatencyWaitMaxMs = d3dProjection.D3DFrameLatencyWaitMaxMs,
            D3DFrameStatsSampleCount = d3dProjection.D3DFrameStatsSampleCount,
            D3DFrameStatsSuccessCount = d3dProjection.D3DFrameStatsSuccessCount,
            D3DFrameStatsFailureCount = d3dProjection.D3DFrameStatsFailureCount,
            D3DFrameStatsLastError = d3dProjection.D3DFrameStatsLastError,
            D3DFrameStatsPresentCount = d3dProjection.D3DFrameStatsPresentCount,
            D3DFrameStatsPresentRefreshCount = d3dProjection.D3DFrameStatsPresentRefreshCount,
            D3DFrameStatsSyncRefreshCount = d3dProjection.D3DFrameStatsSyncRefreshCount,
            D3DFrameStatsSyncQpcTime = d3dProjection.D3DFrameStatsSyncQpcTime,
            D3DFrameStatsLastPresentDelta = d3dProjection.D3DFrameStatsLastPresentDelta,
            D3DFrameStatsLastPresentRefreshDelta = d3dProjection.D3DFrameStatsLastPresentRefreshDelta,
            D3DFrameStatsLastSyncRefreshDelta = d3dProjection.D3DFrameStatsLastSyncRefreshDelta,
            D3DFrameStatsMissedRefreshCount = d3dProjection.D3DFrameStatsMissedRefreshCount,
            D3DLastSubmittedPreviewPresentId = d3dProjection.D3DLastSubmittedPreviewPresentId,
            D3DLastSubmittedSourceSequenceNumber = d3dProjection.D3DLastSubmittedSourceSequenceNumber,
            D3DLastSubmittedSourcePtsTicks = d3dProjection.D3DLastSubmittedSourcePtsTicks,
            D3DLastSubmittedQpc = d3dProjection.D3DLastSubmittedQpc,
            D3DLastSubmittedUtcUnixMs = d3dProjection.D3DLastSubmittedUtcUnixMs,
            D3DLastRenderedPreviewPresentId = d3dProjection.D3DLastRenderedPreviewPresentId,
            D3DLastRenderedSourceSequenceNumber = d3dProjection.D3DLastRenderedSourceSequenceNumber,
            D3DLastRenderedSourcePtsTicks = d3dProjection.D3DLastRenderedSourcePtsTicks,
            D3DLastRenderedQpc = d3dProjection.D3DLastRenderedQpc,
            D3DLastRenderedUtcUnixMs = d3dProjection.D3DLastRenderedUtcUnixMs,
            D3DLastRenderedSchedulerToPresentMs = d3dProjection.D3DLastRenderedSchedulerToPresentMs,
            D3DLastRenderedPipelineLatencyMs = d3dProjection.D3DLastRenderedPipelineLatencyMs,
            D3DLastDroppedPreviewPresentId = d3dProjection.D3DLastDroppedPreviewPresentId,
            D3DLastDroppedSourceSequenceNumber = d3dProjection.D3DLastDroppedSourceSequenceNumber,
            D3DLastDroppedSourcePtsTicks = d3dProjection.D3DLastDroppedSourcePtsTicks,
            D3DLastDroppedQpc = d3dProjection.D3DLastDroppedQpc,
            D3DLastDroppedUtcUnixMs = d3dProjection.D3DLastDroppedUtcUnixMs,
            D3DLastDropReason = d3dProjection.D3DLastDropReason,
            D3DRecentSlowFrames = d3dProjection.D3DRecentSlowFrames,
            EstimatedPipelineLatencyMs = d3dProjection.EstimatedPipelineLatencyMs,
            GpuPlaybackState = d3dProjection.GpuPlaybackState,
            GpuNaturalVideoWidth = d3dProjection.GpuNaturalVideoWidth,
            GpuNaturalVideoHeight = d3dProjection.GpuNaturalVideoHeight,
            GpuPositionMs = d3dProjection.GpuPositionMs,
            GpuPositionEventCount = input.GpuPositionEventCount
        };
    }
}

internal sealed class PreviewRuntimeSnapshotHealthInput
{
    public bool IsPreviewing { get; init; }
    public bool IsStartupWaitingForFirstVisual { get; init; }
    public DateTimeOffset? StartupRequestedUtc { get; init; }
    public int StartupTimeoutMs { get; init; }
    public bool RendererAttached { get; init; }
    public bool GpuActive { get; init; }
    public long FramesArrived { get; init; }
    public long FramesDisplayed { get; init; }
    public long LastPresentedTick { get; init; }
    public long CurrentTick { get; init; }
    public DateTimeOffset UtcNow { get; init; }
}

internal readonly record struct PreviewRuntimeSnapshotHealth(
    double? StartupElapsedMs,
    bool BlankSuspected,
    bool StallSuspected);

internal static class PreviewRuntimeSnapshotHealthInputFactory
{
    public static PreviewRuntimeSnapshotHealthInput Build(
        PreviewRuntimeSnapshotInput input,
        PreviewRuntimeD3DProjection d3dProjection,
        long currentTick,
        DateTimeOffset utcNow)
    {
        return new PreviewRuntimeSnapshotHealthInput
        {
            IsPreviewing = input.IsPreviewing,
            IsStartupWaitingForFirstVisual = input.IsStartupWaitingForFirstVisual,
            StartupRequestedUtc = input.StartupRequestedUtc,
            StartupTimeoutMs = input.StartupTimeoutMs,
            RendererAttached = d3dProjection.RendererAttached,
            GpuActive = d3dProjection.GpuActive,
            FramesArrived = d3dProjection.FramesArrived,
            FramesDisplayed = d3dProjection.FramesDisplayed,
            LastPresentedTick = input.LastPresentedTick,
            CurrentTick = currentTick,
            UtcNow = utcNow
        };
    }
}

internal static class PreviewRuntimeSnapshotHealthPolicy
{
    public static PreviewRuntimeSnapshotHealth Evaluate(PreviewRuntimeSnapshotHealthInput input)
    {
        var previewPipelineActive = input.IsPreviewing && input.RendererAttached;
        var startupElapsedMs = input.StartupRequestedUtc.HasValue
            ? Math.Max(0, (input.UtcNow - input.StartupRequestedUtc.Value).TotalMilliseconds)
            : (double?)null;
        var startupTimedOut = input.IsPreviewing &&
                              input.IsStartupWaitingForFirstVisual &&
                              startupElapsedMs.GetValueOrDefault() >= input.StartupTimeoutMs;
        var blankSuspected = !input.GpuActive && previewPipelineActive &&
                             input.FramesArrived > 30 &&
                             input.FramesDisplayed == 0;
        if (!blankSuspected && startupTimedOut)
        {
            blankSuspected = true;
        }

        var stallSuspected = !input.GpuActive && previewPipelineActive &&
                             input.LastPresentedTick > 0 &&
                             input.CurrentTick - input.LastPresentedTick > 3000;

        return new PreviewRuntimeSnapshotHealth(startupElapsedMs, blankSuspected, stallSuspected);
    }
}

internal sealed class PreviewRuntimeD3DProjection
{
    public bool GpuActive { get; private set; }
    public bool RendererAttached { get; private set; }
    public long FramesArrived { get; private set; }
    public long FramesDisplayed { get; private set; }
    public long FramesDropped { get; private set; }
    public long D3DFramesSubmitted { get; private set; }
    public long D3DFramesRendered { get; private set; }
    public long D3DFramesDropped { get; private set; }
    public string RendererMode { get; private set; } = "None";
    public int D3DPresentSyncInterval { get; private set; }
    public int D3DMaxFrameLatency { get; private set; }
    public int D3DSwapChainBufferCount { get; private set; }
    public string D3DSwapChainAddress { get; private set; } = string.Empty;
    public long D3DRenderThreadFailureCount { get; private set; }
    public string D3DLastRenderThreadFailureType { get; private set; } = string.Empty;
    public string D3DLastRenderThreadFailureMessage { get; private set; } = string.Empty;
    public int D3DLastRenderThreadFailureHResult { get; private set; }
    public int D3DPendingFrameCount { get; private set; }
    public string D3DInputColorSpace { get; private set; } = "None";
    public string D3DOutputColorSpace { get; private set; } = "None";
    public PreviewSlowFrameDiagnostic[] D3DRecentSlowFrames { get; private set; } = Array.Empty<PreviewSlowFrameDiagnostic>();
    public string GpuPlaybackState { get; private set; } = "None";
    public int GpuNaturalVideoWidth { get; private set; }
    public int GpuNaturalVideoHeight { get; private set; }
    public double GpuPositionMs { get; private set; }
    public int DisplayCadenceSampleCount { get; private set; }
    public double DisplayCadenceObservedFps { get; private set; }
    public double DisplayCadenceExpectedIntervalMs { get; private set; }
    public double DisplayCadenceAverageIntervalMs { get; private set; }
    public double DisplayCadenceP95IntervalMs { get; private set; }
    public double DisplayCadenceP99IntervalMs { get; private set; }
    public double DisplayCadenceMaxIntervalMs { get; private set; }
    public double DisplayCadenceOnePercentLowFps { get; private set; }
    public double DisplayCadenceFivePercentLowFps { get; private set; }
    public double DisplayCadenceSampleDurationMs { get; private set; }
    public double[] DisplayCadenceRecentIntervalsMs { get; private set; } = Array.Empty<double>();
    public double DisplayCadenceJitterStdDevMs { get; private set; }
    public long DisplayCadenceSlowFrameCount { get; private set; }
    public double DisplayCadenceSlowFramePercent { get; private set; }
    public int D3DCpuTimingSampleCount { get; private set; }
    public double D3DInputUploadCpuAvgMs { get; private set; }
    public double D3DInputUploadCpuP95Ms { get; private set; }
    public double D3DInputUploadCpuP99Ms { get; private set; }
    public double D3DInputUploadCpuMaxMs { get; private set; }
    public double D3DRenderSubmitCpuAvgMs { get; private set; }
    public double D3DRenderSubmitCpuP95Ms { get; private set; }
    public double D3DRenderSubmitCpuP99Ms { get; private set; }
    public double D3DRenderSubmitCpuMaxMs { get; private set; }
    public double D3DPresentCallAvgMs { get; private set; }
    public double D3DPresentCallP95Ms { get; private set; }
    public double D3DPresentCallP99Ms { get; private set; }
    public double D3DPresentCallMaxMs { get; private set; }
    public double D3DTotalFrameCpuAvgMs { get; private set; }
    public double D3DTotalFrameCpuP95Ms { get; private set; }
    public double D3DTotalFrameCpuP99Ms { get; private set; }
    public double D3DTotalFrameCpuMaxMs { get; private set; }
    public int D3DPipelineLatencySampleCount { get; private set; }
    public double D3DPipelineLatencyAvgMs { get; private set; }
    public double D3DPipelineLatencyP95Ms { get; private set; }
    public double D3DPipelineLatencyP99Ms { get; private set; }
    public double D3DPipelineLatencyMaxMs { get; private set; }
    public double EstimatedPipelineLatencyMs { get; private set; }
    public long D3DLastSubmittedPreviewPresentId { get; private set; }
    public long D3DLastSubmittedSourceSequenceNumber { get; private set; }
    public long D3DLastSubmittedSourcePtsTicks { get; private set; }
    public long D3DLastSubmittedQpc { get; private set; }
    public long D3DLastSubmittedUtcUnixMs { get; private set; }
    public long D3DLastRenderedPreviewPresentId { get; private set; }
    public long D3DLastRenderedSourceSequenceNumber { get; private set; }
    public long D3DLastRenderedSourcePtsTicks { get; private set; }
    public long D3DLastRenderedQpc { get; private set; }
    public long D3DLastRenderedUtcUnixMs { get; private set; }
    public double D3DLastRenderedSchedulerToPresentMs { get; private set; }
    public double D3DLastRenderedPipelineLatencyMs { get; private set; }
    public long D3DLastDroppedPreviewPresentId { get; private set; }
    public long D3DLastDroppedSourceSequenceNumber { get; private set; }
    public long D3DLastDroppedSourcePtsTicks { get; private set; }
    public long D3DLastDroppedQpc { get; private set; }
    public long D3DLastDroppedUtcUnixMs { get; private set; }
    public string D3DLastDropReason { get; private set; } = string.Empty;
    public long D3DFrameStatsSampleCount { get; private set; }
    public long D3DFrameStatsSuccessCount { get; private set; }
    public long D3DFrameStatsFailureCount { get; private set; }
    public string D3DFrameStatsLastError { get; private set; } = string.Empty;
    public long D3DFrameStatsPresentCount { get; private set; }
    public long D3DFrameStatsPresentRefreshCount { get; private set; }
    public long D3DFrameStatsSyncRefreshCount { get; private set; }
    public long D3DFrameStatsSyncQpcTime { get; private set; }
    public long D3DFrameStatsLastPresentDelta { get; private set; }
    public long D3DFrameStatsLastPresentRefreshDelta { get; private set; }
    public long D3DFrameStatsLastSyncRefreshDelta { get; private set; }
    public long D3DFrameStatsMissedRefreshCount { get; private set; }
    public bool D3DFrameLatencyWaitEnabled { get; private set; }
    public bool D3DFrameLatencyWaitHandleActive { get; private set; }
    public long D3DFrameLatencyWaitCallCount { get; private set; }
    public long D3DFrameLatencyWaitSignaledCount { get; private set; }
    public long D3DFrameLatencyWaitTimeoutCount { get; private set; }
    public long D3DFrameLatencyWaitUnexpectedResultCount { get; private set; }
    public uint D3DFrameLatencyWaitLastResult { get; private set; }
    public double D3DFrameLatencyWaitLastMs { get; private set; }
    public int D3DFrameLatencyWaitSampleCount { get; private set; }
    public double D3DFrameLatencyWaitAvgMs { get; private set; }
    public double D3DFrameLatencyWaitP95Ms { get; private set; }
    public double D3DFrameLatencyWaitP99Ms { get; private set; }
    public double D3DFrameLatencyWaitMaxMs { get; private set; }

    public static PreviewRuntimeD3DProjection Build(PreviewRuntimeSnapshotInput input)
    {
        var d3d = input.D3DRenderer;
        var frameCounters = PreviewRuntimeD3DFrameCounterPolicy.Evaluate(input);
        var rendererState = PreviewRuntimeD3DRendererStatePolicy.Evaluate(d3d, input.IsPreviewing);
        var displayCadence = d3d?.GetPresentCadenceMetrics(input.PreviewMinPresentationIntervalMs);
        var renderCpuTiming = d3d?.GetRenderCpuTimingMetrics();
        var frameOwnership = d3d?.GetFrameOwnershipMetrics();
        var frameStatistics = d3d?.GetDxgiFrameStatisticsMetrics();
        var frameLatencyWait = d3d?.GetFrameLatencyWaitMetrics();
        var pipelineLatency = d3d?.GetPipelineLatencyMetrics();

        var projection = new PreviewRuntimeD3DProjection();
        projection.ApplyFrameCounters(frameCounters);
        projection.ApplyRendererState(rendererState);
        projection.ApplyDisplayCadence(displayCadence);
        projection.ApplyRenderCpuTiming(renderCpuTiming);
        projection.ApplyPipelineLatency(pipelineLatency);
        projection.ApplyFrameLatencyWait(frameLatencyWait);
        projection.ApplyFrameStatistics(frameStatistics);
        projection.ApplyFrameOwnership(frameOwnership);
        return projection;
    }

    private void ApplyFrameCounters(PreviewRuntimeD3DFrameCounters frameCounters)
    {
        GpuActive = frameCounters.GpuActive;
        RendererAttached = frameCounters.RendererAttached;
        FramesArrived = frameCounters.FramesArrived;
        FramesDisplayed = frameCounters.FramesDisplayed;
        FramesDropped = frameCounters.FramesDropped;
        D3DFramesSubmitted = frameCounters.D3DFramesSubmitted;
        D3DFramesRendered = frameCounters.D3DFramesRendered;
        D3DFramesDropped = frameCounters.D3DFramesDropped;
    }

    private void ApplyRendererState(PreviewRuntimeD3DRendererState rendererState)
    {
        RendererMode = rendererState.RendererMode;
        D3DPresentSyncInterval = rendererState.PresentSyncInterval;
        D3DMaxFrameLatency = rendererState.MaxFrameLatency;
        D3DSwapChainBufferCount = rendererState.SwapChainBufferCount;
        D3DSwapChainAddress = rendererState.SwapChainAddress;
        D3DRenderThreadFailureCount = rendererState.RenderThreadFailureCount;
        D3DLastRenderThreadFailureType = rendererState.LastRenderThreadFailureType;
        D3DLastRenderThreadFailureMessage = rendererState.LastRenderThreadFailureMessage;
        D3DLastRenderThreadFailureHResult = rendererState.LastRenderThreadFailureHResult;
        D3DPendingFrameCount = rendererState.PendingFrameCount;
        D3DInputColorSpace = rendererState.InputColorSpace;
        D3DOutputColorSpace = rendererState.OutputColorSpace;
        D3DRecentSlowFrames = rendererState.RecentSlowFrames;
        GpuPlaybackState = rendererState.GpuPlaybackState;
        GpuNaturalVideoWidth = rendererState.NaturalVideoWidth;
        GpuNaturalVideoHeight = rendererState.NaturalVideoHeight;
        GpuPositionMs = rendererState.PositionMs;
    }

    private void ApplyDisplayCadence(D3D11PreviewRenderer.PresentCadenceMetrics? displayCadence)
    {
        DisplayCadenceSampleCount = displayCadence?.SampleCount ?? 0;
        DisplayCadenceObservedFps = displayCadence?.ObservedFps ?? 0;
        DisplayCadenceExpectedIntervalMs = displayCadence?.ExpectedIntervalMs ?? 0;
        DisplayCadenceAverageIntervalMs = displayCadence?.AverageIntervalMs ?? 0;
        DisplayCadenceP95IntervalMs = displayCadence?.P95IntervalMs ?? 0;
        DisplayCadenceP99IntervalMs = displayCadence?.P99IntervalMs ?? 0;
        DisplayCadenceMaxIntervalMs = displayCadence?.MaxIntervalMs ?? 0;
        DisplayCadenceOnePercentLowFps = displayCadence?.OnePercentLowFps ?? 0;
        DisplayCadenceFivePercentLowFps = displayCadence?.FivePercentLowFps ?? 0;
        DisplayCadenceSampleDurationMs = displayCadence?.SampleDurationMs ?? 0;
        DisplayCadenceRecentIntervalsMs = displayCadence?.RecentIntervalsMs ?? Array.Empty<double>();
        DisplayCadenceJitterStdDevMs = displayCadence?.JitterStdDevMs ?? 0;
        DisplayCadenceSlowFrameCount = displayCadence?.SlowFrameCount ?? 0;
        DisplayCadenceSlowFramePercent = displayCadence?.SlowFramePercent ?? 0;
    }

    private void ApplyRenderCpuTiming(D3D11PreviewRenderer.RenderCpuTimingMetrics? renderCpuTiming)
    {
        D3DCpuTimingSampleCount = renderCpuTiming?.TotalFrame.SampleCount ?? 0;
        D3DInputUploadCpuAvgMs = renderCpuTiming?.InputUpload.AverageMs ?? 0;
        D3DInputUploadCpuP95Ms = renderCpuTiming?.InputUpload.P95Ms ?? 0;
        D3DInputUploadCpuP99Ms = renderCpuTiming?.InputUpload.P99Ms ?? 0;
        D3DInputUploadCpuMaxMs = renderCpuTiming?.InputUpload.MaxMs ?? 0;
        D3DRenderSubmitCpuAvgMs = renderCpuTiming?.RenderSubmit.AverageMs ?? 0;
        D3DRenderSubmitCpuP95Ms = renderCpuTiming?.RenderSubmit.P95Ms ?? 0;
        D3DRenderSubmitCpuP99Ms = renderCpuTiming?.RenderSubmit.P99Ms ?? 0;
        D3DRenderSubmitCpuMaxMs = renderCpuTiming?.RenderSubmit.MaxMs ?? 0;
        D3DPresentCallAvgMs = renderCpuTiming?.PresentCall.AverageMs ?? 0;
        D3DPresentCallP95Ms = renderCpuTiming?.PresentCall.P95Ms ?? 0;
        D3DPresentCallP99Ms = renderCpuTiming?.PresentCall.P99Ms ?? 0;
        D3DPresentCallMaxMs = renderCpuTiming?.PresentCall.MaxMs ?? 0;
        D3DTotalFrameCpuAvgMs = renderCpuTiming?.TotalFrame.AverageMs ?? 0;
        D3DTotalFrameCpuP95Ms = renderCpuTiming?.TotalFrame.P95Ms ?? 0;
        D3DTotalFrameCpuP99Ms = renderCpuTiming?.TotalFrame.P99Ms ?? 0;
        D3DTotalFrameCpuMaxMs = renderCpuTiming?.TotalFrame.MaxMs ?? 0;
    }

    private void ApplyPipelineLatency(D3D11PreviewRenderer.PipelineLatencyMetrics? pipelineLatency)
    {
        D3DPipelineLatencySampleCount = pipelineLatency?.SampleCount ?? 0;
        D3DPipelineLatencyAvgMs = pipelineLatency?.AverageMs ?? 0;
        D3DPipelineLatencyP95Ms = pipelineLatency?.P95Ms ?? 0;
        D3DPipelineLatencyP99Ms = pipelineLatency?.P99Ms ?? 0;
        D3DPipelineLatencyMaxMs = pipelineLatency?.MaxMs ?? 0;
        EstimatedPipelineLatencyMs = pipelineLatency?.AverageMs ?? 0;
    }

    private void ApplyFrameOwnership(D3D11PreviewRenderer.FrameOwnershipMetrics? frameOwnership)
    {
        D3DLastSubmittedPreviewPresentId = frameOwnership?.LastSubmittedPreviewPresentId ?? 0;
        D3DLastSubmittedSourceSequenceNumber = frameOwnership?.LastSubmittedSourceSequenceNumber ?? -1;
        D3DLastSubmittedSourcePtsTicks = frameOwnership?.LastSubmittedSourcePtsTicks ?? 0;
        D3DLastSubmittedQpc = frameOwnership?.LastSubmittedQpc ?? 0;
        D3DLastSubmittedUtcUnixMs = frameOwnership?.LastSubmittedUtcUnixMs ?? 0;
        D3DLastRenderedPreviewPresentId = frameOwnership?.LastRenderedPreviewPresentId ?? 0;
        D3DLastRenderedSourceSequenceNumber = frameOwnership?.LastRenderedSourceSequenceNumber ?? -1;
        D3DLastRenderedSourcePtsTicks = frameOwnership?.LastRenderedSourcePtsTicks ?? 0;
        D3DLastRenderedQpc = frameOwnership?.LastRenderedQpc ?? 0;
        D3DLastRenderedUtcUnixMs = frameOwnership?.LastRenderedUtcUnixMs ?? 0;
        D3DLastRenderedSchedulerToPresentMs = frameOwnership?.LastRenderedSchedulerToPresentMs ?? 0;
        D3DLastRenderedPipelineLatencyMs = frameOwnership?.LastRenderedPipelineLatencyMs ?? 0;
        D3DLastDroppedPreviewPresentId = frameOwnership?.LastDroppedPreviewPresentId ?? 0;
        D3DLastDroppedSourceSequenceNumber = frameOwnership?.LastDroppedSourceSequenceNumber ?? -1;
        D3DLastDroppedSourcePtsTicks = frameOwnership?.LastDroppedSourcePtsTicks ?? 0;
        D3DLastDroppedQpc = frameOwnership?.LastDroppedQpc ?? 0;
        D3DLastDroppedUtcUnixMs = frameOwnership?.LastDroppedUtcUnixMs ?? 0;
        D3DLastDropReason = frameOwnership?.LastDropReason ?? string.Empty;
    }

    private void ApplyFrameStatistics(D3D11PreviewRenderer.DxgiFrameStatisticsMetrics? frameStatistics)
    {
        D3DFrameStatsSampleCount = frameStatistics?.SampleCount ?? 0;
        D3DFrameStatsSuccessCount = frameStatistics?.SuccessCount ?? 0;
        D3DFrameStatsFailureCount = frameStatistics?.FailureCount ?? 0;
        D3DFrameStatsLastError = frameStatistics?.LastError ?? string.Empty;
        D3DFrameStatsPresentCount = frameStatistics?.PresentCount ?? -1;
        D3DFrameStatsPresentRefreshCount = frameStatistics?.PresentRefreshCount ?? -1;
        D3DFrameStatsSyncRefreshCount = frameStatistics?.SyncRefreshCount ?? -1;
        D3DFrameStatsSyncQpcTime = frameStatistics?.SyncQpcTime ?? 0;
        D3DFrameStatsLastPresentDelta = frameStatistics?.LastPresentDelta ?? 0;
        D3DFrameStatsLastPresentRefreshDelta = frameStatistics?.LastPresentRefreshDelta ?? 0;
        D3DFrameStatsLastSyncRefreshDelta = frameStatistics?.LastSyncRefreshDelta ?? 0;
        D3DFrameStatsMissedRefreshCount = frameStatistics?.MissedRefreshCount ?? 0;
    }

    private void ApplyFrameLatencyWait(D3D11PreviewRenderer.FrameLatencyWaitMetrics? frameLatencyWait)
    {
        D3DFrameLatencyWaitEnabled = frameLatencyWait?.Enabled ?? false;
        D3DFrameLatencyWaitHandleActive = frameLatencyWait?.HandleActive ?? false;
        D3DFrameLatencyWaitCallCount = frameLatencyWait?.CallCount ?? 0;
        D3DFrameLatencyWaitSignaledCount = frameLatencyWait?.SignaledCount ?? 0;
        D3DFrameLatencyWaitTimeoutCount = frameLatencyWait?.TimeoutCount ?? 0;
        D3DFrameLatencyWaitUnexpectedResultCount = frameLatencyWait?.UnexpectedResultCount ?? 0;
        D3DFrameLatencyWaitLastResult = frameLatencyWait?.LastResult ?? 0;
        D3DFrameLatencyWaitLastMs = frameLatencyWait?.LastWaitMs ?? 0;
        D3DFrameLatencyWaitSampleCount = frameLatencyWait?.Timing.SampleCount ?? 0;
        D3DFrameLatencyWaitAvgMs = frameLatencyWait?.Timing.AverageMs ?? 0;
        D3DFrameLatencyWaitP95Ms = frameLatencyWait?.Timing.P95Ms ?? 0;
        D3DFrameLatencyWaitP99Ms = frameLatencyWait?.Timing.P99Ms ?? 0;
        D3DFrameLatencyWaitMaxMs = frameLatencyWait?.Timing.MaxMs ?? 0;
    }
}

internal readonly record struct PreviewRuntimeD3DFrameCounters(
    bool GpuActive,
    bool RendererAttached,
    long FramesArrived,
    long FramesDisplayed,
    long FramesDropped,
    long D3DFramesSubmitted,
    long D3DFramesRendered,
    long D3DFramesDropped);

internal static class PreviewRuntimeD3DFrameCounterPolicy
{
    public static PreviewRuntimeD3DFrameCounters Evaluate(PreviewRuntimeSnapshotInput input)
    {
        var d3d = input.D3DRenderer;
        var gpuActive = d3d != null;
        var d3dFramesSubmitted = d3d?.FramesSubmitted ?? 0;
        var d3dFramesRendered = d3d?.FramesRendered ?? 0;
        var d3dFramesDropped = d3d?.FramesDropped ?? 0;

        return new PreviewRuntimeD3DFrameCounters(
            GpuActive: gpuActive,
            RendererAttached: d3d != null || input.PreviewSourceAttached,
            FramesArrived: gpuActive ? d3dFramesSubmitted : input.FramesArrived,
            FramesDisplayed: gpuActive ? d3dFramesRendered : input.FramesDisplayed,
            FramesDropped: gpuActive ? d3dFramesDropped : input.FramesDropped,
            D3DFramesSubmitted: d3dFramesSubmitted,
            D3DFramesRendered: d3dFramesRendered,
            D3DFramesDropped: d3dFramesDropped);
    }
}

internal readonly record struct PreviewRuntimeD3DRendererState(
    string RendererMode,
    int PresentSyncInterval,
    int MaxFrameLatency,
    int SwapChainBufferCount,
    string SwapChainAddress,
    long RenderThreadFailureCount,
    string LastRenderThreadFailureType,
    string LastRenderThreadFailureMessage,
    int LastRenderThreadFailureHResult,
    int PendingFrameCount,
    string InputColorSpace,
    string OutputColorSpace,
    PreviewSlowFrameDiagnostic[] RecentSlowFrames,
    string GpuPlaybackState,
    int NaturalVideoWidth,
    int NaturalVideoHeight,
    double PositionMs);

internal static class PreviewRuntimeD3DRendererStatePolicy
{
    public static PreviewRuntimeD3DRendererState Evaluate(D3D11PreviewRenderer? d3d, bool isPreviewing)
        => new(
            RendererMode: d3d?.RendererMode ?? (isPreviewing ? "CpuSoftwareBitmap" : "None"),
            PresentSyncInterval: d3d?.PresentSyncInterval ?? 0,
            MaxFrameLatency: d3d?.DxgiMaxFrameLatency ?? 0,
            SwapChainBufferCount: d3d?.SwapChainBufferCount ?? 0,
            SwapChainAddress: d3d?.SwapChainAddress ?? string.Empty,
            RenderThreadFailureCount: d3d?.RenderThreadFailureCount ?? 0,
            LastRenderThreadFailureType: d3d?.LastRenderThreadFailureType ?? string.Empty,
            LastRenderThreadFailureMessage: d3d?.LastRenderThreadFailureMessage ?? string.Empty,
            LastRenderThreadFailureHResult: d3d?.LastRenderThreadFailureHResult ?? 0,
            PendingFrameCount: d3d?.PendingFrameCount ?? 0,
            InputColorSpace: d3d?.InputColorSpaceLabel ?? "None",
            OutputColorSpace: d3d?.OutputColorSpaceLabel ?? "None",
            RecentSlowFrames: d3d?.GetRecentSlowFrameDiagnostics() ?? Array.Empty<PreviewSlowFrameDiagnostic>(),
            GpuPlaybackState: d3d == null ? "None" : (d3d.IsRendering ? "Rendering" : "Idle"),
            NaturalVideoWidth: d3d?.NaturalWidth ?? 0,
            NaturalVideoHeight: d3d?.NaturalHeight ?? 0,
            PositionMs: 0);
}
