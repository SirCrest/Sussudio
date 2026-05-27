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
