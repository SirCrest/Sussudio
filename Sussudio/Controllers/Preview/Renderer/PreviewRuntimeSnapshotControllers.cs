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
        return PreviewRuntimeSnapshotController.Build(new PreviewRuntimeSnapshotInput
        {
            D3DRenderer = rendererHost.Renderer,
            IsPreviewing = _context.ViewModel.IsPreviewing,
            PreviewSourceAttached = rendererHost.IsCpuPreviewSourceAttached,
            GpuElementVisible = _context.IsGpuElementVisible(),
            CpuElementVisible = _context.IsCpuElementVisible(),
            PlaceholderVisible = _context.IsPlaceholderVisible(),
            FramesArrived = rendererHost.FramesArrived,
            FramesDisplayed = rendererHost.FramesDisplayed,
            FramesDropped = rendererHost.FramesDropped,
            LastPresentedTick = rendererHost.LastPresentedTick,
            PreviewMinPresentationIntervalMs = rendererHost.PreviewMinPresentationIntervalMs,
            StartupState = startupSession.State.ToString(),
            IsStartupWaitingForFirstVisual = startupSession.IsWaitingForFirstVisual,
            StartupAttemptId = startupSession.AttemptId,
            StartupRequestedUtc = startupSession.RequestedUtc,
            StartupTimeoutMs = _context.GetStartupVisualTimeoutMs(),
            StartupGpuSignalMediaOpened = startupSignalSnapshot.GpuSignalMediaOpened,
            StartupGpuSignalFirstFrame = startupSignalSnapshot.GpuSignalFirstFrame,
            StartupGpuSignalPlaybackAdvancing = startupSignalSnapshot.GpuSignalPlaybackAdvancing,
            StartupRequiredSignals = startupSignalSnapshot.RequiredSignals,
            StartupReceivedSignals = startupSignalSnapshot.ReceivedSignals,
            StartupStrategy = startupSignalSnapshot.Strategy,
            StartupMissingSignals = startupMissingSignals,
            StartupRecoveryAttemptCount = startupSession.RecoveryAttemptCount,
            StartupLastFailureReason = startupSession.LastFailureReason,
            FirstVisualConfirmed = startupSession.FirstVisualConfirmed,
            GpuPositionEventCount = startupSignals.PositionEventCount
        });
    }
}

internal sealed class PreviewRuntimeSnapshotInput
{
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

internal readonly record struct PreviewRuntimeSnapshotSurfaceProjection(
    bool IsPreviewing,
    bool GpuActive,
    bool PlaceholderVisible,
    bool GpuElementVisible,
    bool CpuElementVisible,
    bool RendererAttached,
    long FramesArrived,
    long FramesDisplayed,
    long FramesDropped,
    bool BlankSuspected,
    bool StallSuspected);

internal static class PreviewRuntimeSnapshotSurfaceProjectionPolicy
{
    public static PreviewRuntimeSnapshotSurfaceProjection Evaluate(
        PreviewRuntimeSnapshotInput input,
        PreviewRuntimeD3DProjection d3dProjection,
        PreviewRuntimeSnapshotHealth health)
        => new(
            IsPreviewing: input.IsPreviewing,
            GpuActive: d3dProjection.GpuActive,
            PlaceholderVisible: input.PlaceholderVisible,
            GpuElementVisible: input.GpuElementVisible,
            CpuElementVisible: input.CpuElementVisible,
            RendererAttached: d3dProjection.RendererAttached,
            FramesArrived: d3dProjection.FramesArrived,
            FramesDisplayed: d3dProjection.FramesDisplayed,
            FramesDropped: d3dProjection.FramesDropped,
            BlankSuspected: health.BlankSuspected,
            StallSuspected: health.StallSuspected);
}

internal readonly record struct PreviewRuntimeSnapshotStartupProjection(
    string State,
    string? AttemptId,
    double? ElapsedMs,
    int TimeoutMs,
    bool GpuSignalMediaOpened,
    bool GpuSignalFirstFrame,
    bool GpuSignalPlaybackAdvancing,
    PreviewStartupSignalFlags RequiredSignals,
    PreviewStartupSignalFlags ReceivedSignals,
    PreviewStartupStrategy Strategy,
    string? MissingSignals,
    int RecoveryAttemptCount,
    string? LastFailureReason,
    bool FirstVisualConfirmed);

internal static class PreviewRuntimeSnapshotStartupProjectionPolicy
{
    public static PreviewRuntimeSnapshotStartupProjection Evaluate(
        PreviewRuntimeSnapshotInput input,
        PreviewRuntimeSnapshotHealth health)
        => new(
            State: input.StartupState,
            AttemptId: input.StartupAttemptId,
            ElapsedMs: health.StartupElapsedMs,
            TimeoutMs: input.StartupTimeoutMs,
            GpuSignalMediaOpened: input.StartupGpuSignalMediaOpened,
            GpuSignalFirstFrame: input.StartupGpuSignalFirstFrame,
            GpuSignalPlaybackAdvancing: input.StartupGpuSignalPlaybackAdvancing,
            RequiredSignals: input.StartupRequiredSignals,
            ReceivedSignals: input.StartupReceivedSignals,
            Strategy: input.StartupStrategy,
            MissingSignals: input.StartupMissingSignals,
            RecoveryAttemptCount: input.StartupRecoveryAttemptCount,
            LastFailureReason: input.StartupLastFailureReason,
            FirstVisualConfirmed: input.FirstVisualConfirmed);
}

internal readonly record struct PreviewRuntimeSnapshotGpuPlaybackProjection(
    string PlaybackState,
    int NaturalVideoWidth,
    int NaturalVideoHeight,
    double PositionMs,
    long PositionEventCount);

internal static class PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy
{
    public static PreviewRuntimeSnapshotGpuPlaybackProjection Evaluate(
        PreviewRuntimeSnapshotInput input,
        PreviewRuntimeD3DProjection d3dProjection)
        => new(
            PlaybackState: d3dProjection.GpuPlaybackState,
            NaturalVideoWidth: d3dProjection.GpuNaturalVideoWidth,
            NaturalVideoHeight: d3dProjection.GpuNaturalVideoHeight,
            PositionMs: d3dProjection.GpuPositionMs,
            PositionEventCount: input.GpuPositionEventCount);
}

internal static class PreviewRuntimeSnapshotMapper
{
    public static PreviewRuntimeSnapshot Build(
        PreviewRuntimeSnapshotInput input,
        PreviewRuntimeD3DProjection d3dProjection,
        PreviewRuntimeSnapshotHealth health,
        DateTimeOffset timestampUtc)
    {
        var surface = PreviewRuntimeSnapshotSurfaceProjectionPolicy.Evaluate(input, d3dProjection, health);
        var startup = PreviewRuntimeSnapshotStartupProjectionPolicy.Evaluate(input, health);
        var gpuPlayback = PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy.Evaluate(input, d3dProjection);

        return new PreviewRuntimeSnapshot
        {
            TimestampUtc = timestampUtc,
            IsPreviewing = surface.IsPreviewing,
            GpuActive = surface.GpuActive,
            PlaceholderVisible = surface.PlaceholderVisible,
            GpuElementVisible = surface.GpuElementVisible,
            CpuElementVisible = surface.CpuElementVisible,
            RendererAttached = surface.RendererAttached,
            StartupState = startup.State,
            StartupAttemptId = startup.AttemptId,
            StartupElapsedMs = startup.ElapsedMs,
            StartupTimeoutMs = startup.TimeoutMs,
            StartupGpuSignalMediaOpened = startup.GpuSignalMediaOpened,
            StartupGpuSignalFirstFrame = startup.GpuSignalFirstFrame,
            StartupGpuSignalPlaybackAdvancing = startup.GpuSignalPlaybackAdvancing,
            StartupRequiredSignals = startup.RequiredSignals,
            StartupReceivedSignals = startup.ReceivedSignals,
            StartupStrategy = startup.Strategy,
            StartupMissingSignals = startup.MissingSignals,
            StartupRecoveryAttemptCount = startup.RecoveryAttemptCount,
            StartupLastFailureReason = startup.LastFailureReason,
            FirstVisualConfirmed = startup.FirstVisualConfirmed,
            FramesArrived = surface.FramesArrived,
            FramesDisplayed = surface.FramesDisplayed,
            FramesDropped = surface.FramesDropped,
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
            BlankSuspected = surface.BlankSuspected,
            StallSuspected = surface.StallSuspected,
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
            GpuPlaybackState = gpuPlayback.PlaybackState,
            GpuNaturalVideoWidth = gpuPlayback.NaturalVideoWidth,
            GpuNaturalVideoHeight = gpuPlayback.NaturalVideoHeight,
            GpuPositionMs = gpuPlayback.PositionMs,
            GpuPositionEventCount = gpuPlayback.PositionEventCount
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
        var displayCadence = PreviewRuntimeD3DDisplayCadencePolicy.Evaluate(d3d, input.PreviewMinPresentationIntervalMs);
        var renderCpuTiming = PreviewRuntimeD3DRenderCpuTimingPolicy.Evaluate(d3d);
        var frameOwnership = PreviewRuntimeD3DFrameOwnershipPolicy.Evaluate(d3d);
        var frameStatistics = PreviewRuntimeD3DFrameStatisticsPolicy.Evaluate(d3d);
        var frameLatencyWait = PreviewRuntimeD3DFrameLatencyWaitPolicy.Evaluate(d3d);
        var pipelineLatency = PreviewRuntimeD3DPipelineLatencyPolicy.Evaluate(d3d);

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

    private void ApplyDisplayCadence(PreviewRuntimeD3DDisplayCadence displayCadence)
    {
        DisplayCadenceSampleCount = displayCadence.SampleCount;
        DisplayCadenceObservedFps = displayCadence.ObservedFps;
        DisplayCadenceExpectedIntervalMs = displayCadence.ExpectedIntervalMs;
        DisplayCadenceAverageIntervalMs = displayCadence.AverageIntervalMs;
        DisplayCadenceP95IntervalMs = displayCadence.P95IntervalMs;
        DisplayCadenceP99IntervalMs = displayCadence.P99IntervalMs;
        DisplayCadenceMaxIntervalMs = displayCadence.MaxIntervalMs;
        DisplayCadenceOnePercentLowFps = displayCadence.OnePercentLowFps;
        DisplayCadenceFivePercentLowFps = displayCadence.FivePercentLowFps;
        DisplayCadenceSampleDurationMs = displayCadence.SampleDurationMs;
        DisplayCadenceRecentIntervalsMs = displayCadence.RecentIntervalsMs;
        DisplayCadenceJitterStdDevMs = displayCadence.JitterStdDevMs;
        DisplayCadenceSlowFrameCount = displayCadence.SlowFrameCount;
        DisplayCadenceSlowFramePercent = displayCadence.SlowFramePercent;
    }

    private void ApplyRenderCpuTiming(PreviewRuntimeD3DRenderCpuTiming renderCpuTiming)
    {
        D3DCpuTimingSampleCount = renderCpuTiming.SampleCount;
        D3DInputUploadCpuAvgMs = renderCpuTiming.InputUploadAverageMs;
        D3DInputUploadCpuP95Ms = renderCpuTiming.InputUploadP95Ms;
        D3DInputUploadCpuP99Ms = renderCpuTiming.InputUploadP99Ms;
        D3DInputUploadCpuMaxMs = renderCpuTiming.InputUploadMaxMs;
        D3DRenderSubmitCpuAvgMs = renderCpuTiming.RenderSubmitAverageMs;
        D3DRenderSubmitCpuP95Ms = renderCpuTiming.RenderSubmitP95Ms;
        D3DRenderSubmitCpuP99Ms = renderCpuTiming.RenderSubmitP99Ms;
        D3DRenderSubmitCpuMaxMs = renderCpuTiming.RenderSubmitMaxMs;
        D3DPresentCallAvgMs = renderCpuTiming.PresentCallAverageMs;
        D3DPresentCallP95Ms = renderCpuTiming.PresentCallP95Ms;
        D3DPresentCallP99Ms = renderCpuTiming.PresentCallP99Ms;
        D3DPresentCallMaxMs = renderCpuTiming.PresentCallMaxMs;
        D3DTotalFrameCpuAvgMs = renderCpuTiming.TotalFrameAverageMs;
        D3DTotalFrameCpuP95Ms = renderCpuTiming.TotalFrameP95Ms;
        D3DTotalFrameCpuP99Ms = renderCpuTiming.TotalFrameP99Ms;
        D3DTotalFrameCpuMaxMs = renderCpuTiming.TotalFrameMaxMs;
    }

    private void ApplyPipelineLatency(PreviewRuntimeD3DPipelineLatency pipelineLatency)
    {
        D3DPipelineLatencySampleCount = pipelineLatency.SampleCount;
        D3DPipelineLatencyAvgMs = pipelineLatency.AverageMs;
        D3DPipelineLatencyP95Ms = pipelineLatency.P95Ms;
        D3DPipelineLatencyP99Ms = pipelineLatency.P99Ms;
        D3DPipelineLatencyMaxMs = pipelineLatency.MaxMs;
        EstimatedPipelineLatencyMs = pipelineLatency.EstimatedPipelineLatencyMs;
    }

    private void ApplyFrameOwnership(PreviewRuntimeD3DFrameOwnership frameOwnership)
    {
        D3DLastSubmittedPreviewPresentId = frameOwnership.LastSubmittedPreviewPresentId;
        D3DLastSubmittedSourceSequenceNumber = frameOwnership.LastSubmittedSourceSequenceNumber;
        D3DLastSubmittedSourcePtsTicks = frameOwnership.LastSubmittedSourcePtsTicks;
        D3DLastSubmittedQpc = frameOwnership.LastSubmittedQpc;
        D3DLastSubmittedUtcUnixMs = frameOwnership.LastSubmittedUtcUnixMs;
        D3DLastRenderedPreviewPresentId = frameOwnership.LastRenderedPreviewPresentId;
        D3DLastRenderedSourceSequenceNumber = frameOwnership.LastRenderedSourceSequenceNumber;
        D3DLastRenderedSourcePtsTicks = frameOwnership.LastRenderedSourcePtsTicks;
        D3DLastRenderedQpc = frameOwnership.LastRenderedQpc;
        D3DLastRenderedUtcUnixMs = frameOwnership.LastRenderedUtcUnixMs;
        D3DLastRenderedSchedulerToPresentMs = frameOwnership.LastRenderedSchedulerToPresentMs;
        D3DLastRenderedPipelineLatencyMs = frameOwnership.LastRenderedPipelineLatencyMs;
        D3DLastDroppedPreviewPresentId = frameOwnership.LastDroppedPreviewPresentId;
        D3DLastDroppedSourceSequenceNumber = frameOwnership.LastDroppedSourceSequenceNumber;
        D3DLastDroppedSourcePtsTicks = frameOwnership.LastDroppedSourcePtsTicks;
        D3DLastDroppedQpc = frameOwnership.LastDroppedQpc;
        D3DLastDroppedUtcUnixMs = frameOwnership.LastDroppedUtcUnixMs;
        D3DLastDropReason = frameOwnership.LastDropReason;
    }

    private void ApplyFrameStatistics(PreviewRuntimeD3DFrameStatistics frameStatistics)
    {
        D3DFrameStatsSampleCount = frameStatistics.SampleCount;
        D3DFrameStatsSuccessCount = frameStatistics.SuccessCount;
        D3DFrameStatsFailureCount = frameStatistics.FailureCount;
        D3DFrameStatsLastError = frameStatistics.LastError;
        D3DFrameStatsPresentCount = frameStatistics.PresentCount;
        D3DFrameStatsPresentRefreshCount = frameStatistics.PresentRefreshCount;
        D3DFrameStatsSyncRefreshCount = frameStatistics.SyncRefreshCount;
        D3DFrameStatsSyncQpcTime = frameStatistics.SyncQpcTime;
        D3DFrameStatsLastPresentDelta = frameStatistics.LastPresentDelta;
        D3DFrameStatsLastPresentRefreshDelta = frameStatistics.LastPresentRefreshDelta;
        D3DFrameStatsLastSyncRefreshDelta = frameStatistics.LastSyncRefreshDelta;
        D3DFrameStatsMissedRefreshCount = frameStatistics.MissedRefreshCount;
    }

    private void ApplyFrameLatencyWait(PreviewRuntimeD3DFrameLatencyWait frameLatencyWait)
    {
        D3DFrameLatencyWaitEnabled = frameLatencyWait.Enabled;
        D3DFrameLatencyWaitHandleActive = frameLatencyWait.HandleActive;
        D3DFrameLatencyWaitCallCount = frameLatencyWait.CallCount;
        D3DFrameLatencyWaitSignaledCount = frameLatencyWait.SignaledCount;
        D3DFrameLatencyWaitTimeoutCount = frameLatencyWait.TimeoutCount;
        D3DFrameLatencyWaitUnexpectedResultCount = frameLatencyWait.UnexpectedResultCount;
        D3DFrameLatencyWaitLastResult = frameLatencyWait.LastResult;
        D3DFrameLatencyWaitLastMs = frameLatencyWait.LastWaitMs;
        D3DFrameLatencyWaitSampleCount = frameLatencyWait.SampleCount;
        D3DFrameLatencyWaitAvgMs = frameLatencyWait.AverageMs;
        D3DFrameLatencyWaitP95Ms = frameLatencyWait.P95Ms;
        D3DFrameLatencyWaitP99Ms = frameLatencyWait.P99Ms;
        D3DFrameLatencyWaitMaxMs = frameLatencyWait.MaxMs;
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

internal readonly record struct PreviewRuntimeD3DDisplayCadence(
    int SampleCount,
    double ObservedFps,
    double ExpectedIntervalMs,
    double AverageIntervalMs,
    double P95IntervalMs,
    double P99IntervalMs,
    double MaxIntervalMs,
    double OnePercentLowFps,
    double FivePercentLowFps,
    double SampleDurationMs,
    double[] RecentIntervalsMs,
    double JitterStdDevMs,
    long SlowFrameCount,
    double SlowFramePercent);

internal static class PreviewRuntimeD3DDisplayCadencePolicy
{
    public static PreviewRuntimeD3DDisplayCadence Evaluate(
        D3D11PreviewRenderer? d3d,
        double previewMinPresentationIntervalMs)
    {
        var displayCadence = d3d?.GetPresentCadenceMetrics(previewMinPresentationIntervalMs);

        return new PreviewRuntimeD3DDisplayCadence(
            SampleCount: displayCadence?.SampleCount ?? 0,
            ObservedFps: displayCadence?.ObservedFps ?? 0,
            ExpectedIntervalMs: displayCadence?.ExpectedIntervalMs ?? 0,
            AverageIntervalMs: displayCadence?.AverageIntervalMs ?? 0,
            P95IntervalMs: displayCadence?.P95IntervalMs ?? 0,
            P99IntervalMs: displayCadence?.P99IntervalMs ?? 0,
            MaxIntervalMs: displayCadence?.MaxIntervalMs ?? 0,
            OnePercentLowFps: displayCadence?.OnePercentLowFps ?? 0,
            FivePercentLowFps: displayCadence?.FivePercentLowFps ?? 0,
            SampleDurationMs: displayCadence?.SampleDurationMs ?? 0,
            RecentIntervalsMs: displayCadence?.RecentIntervalsMs ?? Array.Empty<double>(),
            JitterStdDevMs: displayCadence?.JitterStdDevMs ?? 0,
            SlowFrameCount: displayCadence?.SlowFrameCount ?? 0,
            SlowFramePercent: displayCadence?.SlowFramePercent ?? 0);
    }
}

internal readonly record struct PreviewRuntimeD3DRenderCpuTiming(
    int SampleCount,
    double InputUploadAverageMs,
    double InputUploadP95Ms,
    double InputUploadP99Ms,
    double InputUploadMaxMs,
    double RenderSubmitAverageMs,
    double RenderSubmitP95Ms,
    double RenderSubmitP99Ms,
    double RenderSubmitMaxMs,
    double PresentCallAverageMs,
    double PresentCallP95Ms,
    double PresentCallP99Ms,
    double PresentCallMaxMs,
    double TotalFrameAverageMs,
    double TotalFrameP95Ms,
    double TotalFrameP99Ms,
    double TotalFrameMaxMs);

internal static class PreviewRuntimeD3DRenderCpuTimingPolicy
{
    public static PreviewRuntimeD3DRenderCpuTiming Evaluate(D3D11PreviewRenderer? d3d)
    {
        var renderCpuTiming = d3d?.GetRenderCpuTimingMetrics();

        return new PreviewRuntimeD3DRenderCpuTiming(
            SampleCount: renderCpuTiming?.TotalFrame.SampleCount ?? 0,
            InputUploadAverageMs: renderCpuTiming?.InputUpload.AverageMs ?? 0,
            InputUploadP95Ms: renderCpuTiming?.InputUpload.P95Ms ?? 0,
            InputUploadP99Ms: renderCpuTiming?.InputUpload.P99Ms ?? 0,
            InputUploadMaxMs: renderCpuTiming?.InputUpload.MaxMs ?? 0,
            RenderSubmitAverageMs: renderCpuTiming?.RenderSubmit.AverageMs ?? 0,
            RenderSubmitP95Ms: renderCpuTiming?.RenderSubmit.P95Ms ?? 0,
            RenderSubmitP99Ms: renderCpuTiming?.RenderSubmit.P99Ms ?? 0,
            RenderSubmitMaxMs: renderCpuTiming?.RenderSubmit.MaxMs ?? 0,
            PresentCallAverageMs: renderCpuTiming?.PresentCall.AverageMs ?? 0,
            PresentCallP95Ms: renderCpuTiming?.PresentCall.P95Ms ?? 0,
            PresentCallP99Ms: renderCpuTiming?.PresentCall.P99Ms ?? 0,
            PresentCallMaxMs: renderCpuTiming?.PresentCall.MaxMs ?? 0,
            TotalFrameAverageMs: renderCpuTiming?.TotalFrame.AverageMs ?? 0,
            TotalFrameP95Ms: renderCpuTiming?.TotalFrame.P95Ms ?? 0,
            TotalFrameP99Ms: renderCpuTiming?.TotalFrame.P99Ms ?? 0,
            TotalFrameMaxMs: renderCpuTiming?.TotalFrame.MaxMs ?? 0);
    }
}

internal readonly record struct PreviewRuntimeD3DPipelineLatency(
    int SampleCount,
    double AverageMs,
    double P95Ms,
    double P99Ms,
    double MaxMs,
    double EstimatedPipelineLatencyMs);

internal static class PreviewRuntimeD3DPipelineLatencyPolicy
{
    public static PreviewRuntimeD3DPipelineLatency Evaluate(D3D11PreviewRenderer? d3d)
    {
        var pipelineLatency = d3d?.GetPipelineLatencyMetrics();

        return new PreviewRuntimeD3DPipelineLatency(
            SampleCount: pipelineLatency?.SampleCount ?? 0,
            AverageMs: pipelineLatency?.AverageMs ?? 0,
            P95Ms: pipelineLatency?.P95Ms ?? 0,
            P99Ms: pipelineLatency?.P99Ms ?? 0,
            MaxMs: pipelineLatency?.MaxMs ?? 0,
            EstimatedPipelineLatencyMs: pipelineLatency?.AverageMs ?? 0);
    }
}

internal readonly record struct PreviewRuntimeD3DFrameOwnership(
    long LastSubmittedPreviewPresentId,
    long LastSubmittedSourceSequenceNumber,
    long LastSubmittedSourcePtsTicks,
    long LastSubmittedQpc,
    long LastSubmittedUtcUnixMs,
    long LastRenderedPreviewPresentId,
    long LastRenderedSourceSequenceNumber,
    long LastRenderedSourcePtsTicks,
    long LastRenderedQpc,
    long LastRenderedUtcUnixMs,
    double LastRenderedSchedulerToPresentMs,
    double LastRenderedPipelineLatencyMs,
    long LastDroppedPreviewPresentId,
    long LastDroppedSourceSequenceNumber,
    long LastDroppedSourcePtsTicks,
    long LastDroppedQpc,
    long LastDroppedUtcUnixMs,
    string LastDropReason);

internal static class PreviewRuntimeD3DFrameOwnershipPolicy
{
    public static PreviewRuntimeD3DFrameOwnership Evaluate(D3D11PreviewRenderer? d3d)
    {
        var frameOwnership = d3d?.GetFrameOwnershipMetrics();

        return new PreviewRuntimeD3DFrameOwnership(
            LastSubmittedPreviewPresentId: frameOwnership?.LastSubmittedPreviewPresentId ?? 0,
            LastSubmittedSourceSequenceNumber: frameOwnership?.LastSubmittedSourceSequenceNumber ?? -1,
            LastSubmittedSourcePtsTicks: frameOwnership?.LastSubmittedSourcePtsTicks ?? 0,
            LastSubmittedQpc: frameOwnership?.LastSubmittedQpc ?? 0,
            LastSubmittedUtcUnixMs: frameOwnership?.LastSubmittedUtcUnixMs ?? 0,
            LastRenderedPreviewPresentId: frameOwnership?.LastRenderedPreviewPresentId ?? 0,
            LastRenderedSourceSequenceNumber: frameOwnership?.LastRenderedSourceSequenceNumber ?? -1,
            LastRenderedSourcePtsTicks: frameOwnership?.LastRenderedSourcePtsTicks ?? 0,
            LastRenderedQpc: frameOwnership?.LastRenderedQpc ?? 0,
            LastRenderedUtcUnixMs: frameOwnership?.LastRenderedUtcUnixMs ?? 0,
            LastRenderedSchedulerToPresentMs: frameOwnership?.LastRenderedSchedulerToPresentMs ?? 0,
            LastRenderedPipelineLatencyMs: frameOwnership?.LastRenderedPipelineLatencyMs ?? 0,
            LastDroppedPreviewPresentId: frameOwnership?.LastDroppedPreviewPresentId ?? 0,
            LastDroppedSourceSequenceNumber: frameOwnership?.LastDroppedSourceSequenceNumber ?? -1,
            LastDroppedSourcePtsTicks: frameOwnership?.LastDroppedSourcePtsTicks ?? 0,
            LastDroppedQpc: frameOwnership?.LastDroppedQpc ?? 0,
            LastDroppedUtcUnixMs: frameOwnership?.LastDroppedUtcUnixMs ?? 0,
            LastDropReason: frameOwnership?.LastDropReason ?? string.Empty);
    }
}

internal readonly record struct PreviewRuntimeD3DFrameStatistics(
    long SampleCount,
    long SuccessCount,
    long FailureCount,
    string LastError,
    long PresentCount,
    long PresentRefreshCount,
    long SyncRefreshCount,
    long SyncQpcTime,
    long LastPresentDelta,
    long LastPresentRefreshDelta,
    long LastSyncRefreshDelta,
    long MissedRefreshCount);

internal static class PreviewRuntimeD3DFrameStatisticsPolicy
{
    public static PreviewRuntimeD3DFrameStatistics Evaluate(D3D11PreviewRenderer? d3d)
    {
        var frameStats = d3d?.GetDxgiFrameStatisticsMetrics();

        return new PreviewRuntimeD3DFrameStatistics(
            SampleCount: frameStats?.SampleCount ?? 0,
            SuccessCount: frameStats?.SuccessCount ?? 0,
            FailureCount: frameStats?.FailureCount ?? 0,
            LastError: frameStats?.LastError ?? string.Empty,
            PresentCount: frameStats?.PresentCount ?? -1,
            PresentRefreshCount: frameStats?.PresentRefreshCount ?? -1,
            SyncRefreshCount: frameStats?.SyncRefreshCount ?? -1,
            SyncQpcTime: frameStats?.SyncQpcTime ?? 0,
            LastPresentDelta: frameStats?.LastPresentDelta ?? 0,
            LastPresentRefreshDelta: frameStats?.LastPresentRefreshDelta ?? 0,
            LastSyncRefreshDelta: frameStats?.LastSyncRefreshDelta ?? 0,
            MissedRefreshCount: frameStats?.MissedRefreshCount ?? 0);
    }
}

internal readonly record struct PreviewRuntimeD3DFrameLatencyWait(
    bool Enabled,
    bool HandleActive,
    long CallCount,
    long SignaledCount,
    long TimeoutCount,
    long UnexpectedResultCount,
    uint LastResult,
    double LastWaitMs,
    int SampleCount,
    double AverageMs,
    double P95Ms,
    double P99Ms,
    double MaxMs);

internal static class PreviewRuntimeD3DFrameLatencyWaitPolicy
{
    public static PreviewRuntimeD3DFrameLatencyWait Evaluate(D3D11PreviewRenderer? d3d)
    {
        var frameLatencyWait = d3d?.GetFrameLatencyWaitMetrics();

        return new PreviewRuntimeD3DFrameLatencyWait(
            Enabled: frameLatencyWait?.Enabled ?? false,
            HandleActive: frameLatencyWait?.HandleActive ?? false,
            CallCount: frameLatencyWait?.CallCount ?? 0,
            SignaledCount: frameLatencyWait?.SignaledCount ?? 0,
            TimeoutCount: frameLatencyWait?.TimeoutCount ?? 0,
            UnexpectedResultCount: frameLatencyWait?.UnexpectedResultCount ?? 0,
            LastResult: frameLatencyWait?.LastResult ?? 0,
            LastWaitMs: frameLatencyWait?.LastWaitMs ?? 0,
            SampleCount: frameLatencyWait?.Timing.SampleCount ?? 0,
            AverageMs: frameLatencyWait?.Timing.AverageMs ?? 0,
            P95Ms: frameLatencyWait?.Timing.P95Ms ?? 0,
            P99Ms: frameLatencyWait?.Timing.P99Ms ?? 0,
            MaxMs: frameLatencyWait?.Timing.MaxMs ?? 0);
    }
}
