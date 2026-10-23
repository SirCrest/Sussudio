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
    public required Func<bool> IsGpuElementVisible { get; init; }
    public required Func<bool> IsCpuElementVisible { get; init; }
    public required Func<bool> IsPlaceholderVisible { get; init; }
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
        var signature = BuildSignature();
        var previewRuntimeEpoch = GetOrAdvancePreviewRuntimeSnapshotEpoch(signature);
        return PreviewRuntimeSnapshotController.Build(new PreviewRuntimeSnapshotInput
        {
            PreviewRuntimeEpoch = previewRuntimeEpoch,
            D3DRenderer = _context.RendererHostController.Renderer,
            Signature = signature
        });
    }

    private PreviewRuntimeSnapshotSignature BuildSignature()
    {
        var startupSession = _context.StartupSessionController;
        var startupSignalSnapshot = startupSession.SignalSnapshot;
        var startupMissingSignals = startupSession.MissingSignals;
        if (string.IsNullOrWhiteSpace(startupMissingSignals) &&
            startupSession.ShouldRefreshMissingSignalsForSnapshot)
        {
            startupMissingSignals = startupSession.BuildMissingSignals();
        }

        var rendererHost = _context.RendererHostController;
        return new PreviewRuntimeSnapshotSignature(
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
            startupSession.State,
            startupSession.IsWaitingForFirstVisual,
            startupSession.AttemptId,
            startupSession.RequestedUtc,
            startupSession.VisualTimeoutMs,
            startupSignalSnapshot.RequiredSignals,
            startupSignalSnapshot.ReceivedSignals,
            startupSignalSnapshot.Strategy,
            startupMissingSignals,
            startupSession.RecoveryAttemptCount,
            startupSession.LastFailureReason,
            startupSession.FirstVisualConfirmed);
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
    PreviewStartupState StartupState,
    bool IsStartupWaitingForFirstVisual,
    string? StartupAttemptId,
    DateTimeOffset? StartupRequestedUtc,
    int StartupTimeoutMs,
    PreviewStartupSignalFlags StartupRequiredSignals,
    PreviewStartupSignalFlags StartupReceivedSignals,
    PreviewStartupStrategy StartupStrategy,
    string? StartupMissingSignals,
    int StartupRecoveryAttemptCount,
    string? StartupLastFailureReason,
    bool FirstVisualConfirmed);

internal sealed class PreviewRuntimeSnapshotInput
{
    public long PreviewRuntimeEpoch { get; init; }
    public D3D11PreviewRenderer? D3DRenderer { get; init; }
    public PreviewRuntimeSnapshotSignature Signature { get; init; }
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
            IsPreviewing = input.Signature.IsPreviewing,
            GpuActive = (d3dProjection.FrameCounters.GpuActive),
            PlaceholderVisible = input.Signature.PlaceholderVisible,
            GpuElementVisible = input.Signature.GpuElementVisible,
            CpuElementVisible = input.Signature.CpuElementVisible,
            RendererAttached = (d3dProjection.FrameCounters.RendererAttached),
            StartupState = input.Signature.StartupState,
            StartupAttemptId = input.Signature.StartupAttemptId,
            StartupElapsedMs = health.StartupElapsedMs,
            StartupTimeoutMs = input.Signature.StartupTimeoutMs,
            StartupRequiredSignals = input.Signature.StartupRequiredSignals,
            StartupReceivedSignals = input.Signature.StartupReceivedSignals,
            StartupStrategy = input.Signature.StartupStrategy,
            StartupMissingSignals = input.Signature.StartupMissingSignals,
            StartupRecoveryAttemptCount = input.Signature.StartupRecoveryAttemptCount,
            StartupLastFailureReason = input.Signature.StartupLastFailureReason,
            FirstVisualConfirmed = input.Signature.FirstVisualConfirmed,
            FramesArrived = (d3dProjection.FrameCounters.FramesArrived),
            FramesDisplayed = (d3dProjection.FrameCounters.FramesDisplayed),
            FramesDropped = (d3dProjection.FrameCounters.FramesDropped),
            DisplayCadenceSampleCount = (d3dProjection.DisplayCadence?.SampleCount ?? 0),
            DisplayCadenceObservedFps = (d3dProjection.DisplayCadence?.ObservedFps ?? 0),
            DisplayCadenceExpectedIntervalMs = (d3dProjection.DisplayCadence?.ExpectedIntervalMs ?? 0),
            DisplayCadenceAverageIntervalMs = (d3dProjection.DisplayCadence?.AverageIntervalMs ?? 0),
            DisplayCadenceP95IntervalMs = (d3dProjection.DisplayCadence?.P95IntervalMs ?? 0),
            DisplayCadenceP99IntervalMs = (d3dProjection.DisplayCadence?.P99IntervalMs ?? 0),
            DisplayCadenceMaxIntervalMs = (d3dProjection.DisplayCadence?.MaxIntervalMs ?? 0),
            DisplayCadenceOnePercentLowFps = (d3dProjection.DisplayCadence?.OnePercentLowFps ?? 0),
            DisplayCadenceFivePercentLowFps = (d3dProjection.DisplayCadence?.FivePercentLowFps ?? 0),
            DisplayCadenceSampleDurationMs = (d3dProjection.DisplayCadence?.SampleDurationMs ?? 0),
            DisplayCadenceRecentIntervalsMs = (d3dProjection.DisplayCadence?.RecentIntervalsMs ?? Array.Empty<double>()),
            DisplayCadenceJitterStdDevMs = (d3dProjection.DisplayCadence?.JitterStdDevMs ?? 0),
            DisplayCadenceSlowFrameCount = (d3dProjection.DisplayCadence?.SlowFrameCount ?? 0),
            DisplayCadenceSlowFramePercent = (d3dProjection.DisplayCadence?.SlowFramePercent ?? 0),
            BlankSuspected = health.BlankSuspected,
            StallSuspected = health.StallSuspected,
            RendererMode = (d3dProjection.RendererState.RendererMode),
            D3DPresentSyncInterval = (d3dProjection.RendererState.PresentSyncInterval),
            D3DMaxFrameLatency = (d3dProjection.RendererState.MaxFrameLatency),
            D3DSwapChainBufferCount = (d3dProjection.RendererState.SwapChainBufferCount),
            D3DSwapChainAddress = (d3dProjection.RendererState.SwapChainAddress),
            D3DFramesSubmitted = (d3dProjection.FrameCounters.D3DFramesSubmitted),
            D3DFramesRendered = (d3dProjection.FrameCounters.D3DFramesRendered),
            D3DFramesDropped = (d3dProjection.FrameCounters.D3DFramesDropped),
            D3DRenderThreadFailureCount = (d3dProjection.RendererState.RenderThreadFailureCount),
            D3DLastRenderThreadFailureType = (d3dProjection.RendererState.LastRenderThreadFailureType),
            D3DLastRenderThreadFailureMessage = (d3dProjection.RendererState.LastRenderThreadFailureMessage),
            D3DLastRenderThreadFailureHResult = (d3dProjection.RendererState.LastRenderThreadFailureHResult),
            D3DPendingFrameCount = (d3dProjection.RendererState.PendingFrameCount),
            D3DInputColorSpace = (d3dProjection.RendererState.InputColorSpace),
            D3DOutputColorSpace = (d3dProjection.RendererState.OutputColorSpace),
            D3DCpuTimingSampleCount = (d3dProjection.RenderCpuTiming?.TotalFrame.SampleCount ?? 0),
            D3DInputUploadCpuAvgMs = (d3dProjection.RenderCpuTiming?.InputUpload.AverageMs ?? 0),
            D3DInputUploadCpuP95Ms = (d3dProjection.RenderCpuTiming?.InputUpload.P95Ms ?? 0),
            D3DInputUploadCpuP99Ms = (d3dProjection.RenderCpuTiming?.InputUpload.P99Ms ?? 0),
            D3DInputUploadCpuMaxMs = (d3dProjection.RenderCpuTiming?.InputUpload.MaxMs ?? 0),
            D3DRenderSubmitCpuAvgMs = (d3dProjection.RenderCpuTiming?.RenderSubmit.AverageMs ?? 0),
            D3DRenderSubmitCpuP95Ms = (d3dProjection.RenderCpuTiming?.RenderSubmit.P95Ms ?? 0),
            D3DRenderSubmitCpuP99Ms = (d3dProjection.RenderCpuTiming?.RenderSubmit.P99Ms ?? 0),
            D3DRenderSubmitCpuMaxMs = (d3dProjection.RenderCpuTiming?.RenderSubmit.MaxMs ?? 0),
            D3DPresentCallAvgMs = (d3dProjection.RenderCpuTiming?.PresentCall.AverageMs ?? 0),
            D3DPresentCallP95Ms = (d3dProjection.RenderCpuTiming?.PresentCall.P95Ms ?? 0),
            D3DPresentCallP99Ms = (d3dProjection.RenderCpuTiming?.PresentCall.P99Ms ?? 0),
            D3DPresentCallMaxMs = (d3dProjection.RenderCpuTiming?.PresentCall.MaxMs ?? 0),
            D3DTotalFrameCpuAvgMs = (d3dProjection.RenderCpuTiming?.TotalFrame.AverageMs ?? 0),
            D3DTotalFrameCpuP95Ms = (d3dProjection.RenderCpuTiming?.TotalFrame.P95Ms ?? 0),
            D3DTotalFrameCpuP99Ms = (d3dProjection.RenderCpuTiming?.TotalFrame.P99Ms ?? 0),
            D3DTotalFrameCpuMaxMs = (d3dProjection.RenderCpuTiming?.TotalFrame.MaxMs ?? 0),
            D3DPipelineLatencySampleCount = (d3dProjection.PipelineLatency?.SampleCount ?? 0),
            D3DPipelineLatencyAvgMs = (d3dProjection.PipelineLatency?.AverageMs ?? 0),
            D3DPipelineLatencyP95Ms = (d3dProjection.PipelineLatency?.P95Ms ?? 0),
            D3DPipelineLatencyP99Ms = (d3dProjection.PipelineLatency?.P99Ms ?? 0),
            D3DPipelineLatencyMaxMs = (d3dProjection.PipelineLatency?.MaxMs ?? 0),
            D3DFrameLatencyWaitEnabled = (d3dProjection.FrameLatencyWait?.Enabled ?? false),
            D3DFrameLatencyWaitHandleActive = (d3dProjection.FrameLatencyWait?.HandleActive ?? false),
            D3DFrameLatencyWaitCallCount = (d3dProjection.FrameLatencyWait?.CallCount ?? 0),
            D3DFrameLatencyWaitSignaledCount = (d3dProjection.FrameLatencyWait?.SignaledCount ?? 0),
            D3DFrameLatencyWaitTimeoutCount = (d3dProjection.FrameLatencyWait?.TimeoutCount ?? 0),
            D3DFrameLatencyWaitUnexpectedResultCount = (d3dProjection.FrameLatencyWait?.UnexpectedResultCount ?? 0),
            D3DFrameLatencyWaitLastResult = (d3dProjection.FrameLatencyWait?.LastResult ?? 0),
            D3DFrameLatencyWaitLastMs = (d3dProjection.FrameLatencyWait?.LastWaitMs ?? 0),
            D3DFrameLatencyWaitSampleCount = (d3dProjection.FrameLatencyWait?.Timing.SampleCount ?? 0),
            D3DFrameLatencyWaitAvgMs = (d3dProjection.FrameLatencyWait?.Timing.AverageMs ?? 0),
            D3DFrameLatencyWaitP95Ms = (d3dProjection.FrameLatencyWait?.Timing.P95Ms ?? 0),
            D3DFrameLatencyWaitP99Ms = (d3dProjection.FrameLatencyWait?.Timing.P99Ms ?? 0),
            D3DFrameLatencyWaitMaxMs = (d3dProjection.FrameLatencyWait?.Timing.MaxMs ?? 0),
            D3DFrameStatsSampleCount = (d3dProjection.FrameStatistics?.SampleCount ?? 0),
            D3DFrameStatsSuccessCount = (d3dProjection.FrameStatistics?.SuccessCount ?? 0),
            D3DFrameStatsFailureCount = (d3dProjection.FrameStatistics?.FailureCount ?? 0),
            D3DFrameStatsLastError = (d3dProjection.FrameStatistics?.LastError ?? string.Empty),
            D3DFrameStatsPresentCount = (d3dProjection.FrameStatistics?.PresentCount ?? -1),
            D3DFrameStatsPresentRefreshCount = (d3dProjection.FrameStatistics?.PresentRefreshCount ?? -1),
            D3DFrameStatsSyncRefreshCount = (d3dProjection.FrameStatistics?.SyncRefreshCount ?? -1),
            D3DFrameStatsSyncQpcTime = (d3dProjection.FrameStatistics?.SyncQpcTime ?? 0),
            D3DFrameStatsLastPresentDelta = (d3dProjection.FrameStatistics?.LastPresentDelta ?? 0),
            D3DFrameStatsLastPresentRefreshDelta = (d3dProjection.FrameStatistics?.LastPresentRefreshDelta ?? 0),
            D3DFrameStatsLastSyncRefreshDelta = (d3dProjection.FrameStatistics?.LastSyncRefreshDelta ?? 0),
            D3DFrameStatsMissedRefreshCount = (d3dProjection.FrameStatistics?.MissedRefreshCount ?? 0),
            D3DFrameStatsCadenceMissedRefreshCount = (d3dProjection.FrameStatistics?.CadenceMissedRefreshCount ?? 0),
            D3DLastSubmittedPreviewPresentId = (d3dProjection.FrameOwnership?.LastSubmittedPreviewPresentId ?? 0),
            D3DLastSubmittedSourceSequenceNumber = (d3dProjection.FrameOwnership?.LastSubmittedSourceSequenceNumber ?? -1),
            D3DLastSubmittedSourcePtsTicks = (d3dProjection.FrameOwnership?.LastSubmittedSourcePtsTicks ?? 0),
            D3DLastSubmittedQpc = (d3dProjection.FrameOwnership?.LastSubmittedQpc ?? 0),
            D3DLastSubmittedUtcUnixMs = (d3dProjection.FrameOwnership?.LastSubmittedUtcUnixMs ?? 0),
            D3DLastRenderedPreviewPresentId = (d3dProjection.FrameOwnership?.LastRenderedPreviewPresentId ?? 0),
            D3DLastRenderedSourceSequenceNumber = (d3dProjection.FrameOwnership?.LastRenderedSourceSequenceNumber ?? -1),
            D3DLastRenderedSourcePtsTicks = (d3dProjection.FrameOwnership?.LastRenderedSourcePtsTicks ?? 0),
            D3DLastRenderedQpc = (d3dProjection.FrameOwnership?.LastRenderedQpc ?? 0),
            D3DLastRenderedUtcUnixMs = (d3dProjection.FrameOwnership?.LastRenderedUtcUnixMs ?? 0),
            D3DLastRenderedSchedulerToPresentMs = (d3dProjection.FrameOwnership?.LastRenderedSchedulerToPresentMs ?? 0),
            D3DLastRenderedPipelineLatencyMs = (d3dProjection.FrameOwnership?.LastRenderedPipelineLatencyMs ?? 0),
            D3DLastDroppedPreviewPresentId = (d3dProjection.FrameOwnership?.LastDroppedPreviewPresentId ?? 0),
            D3DLastDroppedSourceSequenceNumber = (d3dProjection.FrameOwnership?.LastDroppedSourceSequenceNumber ?? -1),
            D3DLastDroppedSourcePtsTicks = (d3dProjection.FrameOwnership?.LastDroppedSourcePtsTicks ?? 0),
            D3DLastDroppedQpc = (d3dProjection.FrameOwnership?.LastDroppedQpc ?? 0),
            D3DLastDroppedUtcUnixMs = (d3dProjection.FrameOwnership?.LastDroppedUtcUnixMs ?? 0),
            D3DLastDropReason = (d3dProjection.FrameOwnership?.LastDropReason ?? string.Empty),
            D3DRecentSlowFrames = (d3dProjection.RendererState.RecentSlowFrames),
            EstimatedPipelineLatencyMs = (d3dProjection.PipelineLatency?.AverageMs ?? 0),
            GpuPlaybackState = (d3dProjection.RendererState.GpuPlaybackState),
            GpuNaturalVideoWidth = (d3dProjection.RendererState.NaturalVideoWidth),
            GpuNaturalVideoHeight = (d3dProjection.RendererState.NaturalVideoHeight)
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
            IsPreviewing = input.Signature.IsPreviewing,
            IsStartupWaitingForFirstVisual = input.Signature.IsStartupWaitingForFirstVisual,
            StartupRequestedUtc = input.Signature.StartupRequestedUtc,
            StartupTimeoutMs = input.Signature.StartupTimeoutMs,
            RendererAttached = d3dProjection.FrameCounters.RendererAttached,
            GpuActive = d3dProjection.FrameCounters.GpuActive,
            FramesArrived = d3dProjection.FrameCounters.FramesArrived,
            FramesDisplayed = d3dProjection.FrameCounters.FramesDisplayed,
            LastPresentedTick = input.Signature.LastPresentedTick,
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

internal readonly record struct PreviewRuntimeD3DProjection(
    PreviewRuntimeD3DFrameCounters FrameCounters,
    PreviewRuntimeD3DRendererState RendererState,
    D3D11PreviewRenderer.PresentCadenceMetrics? DisplayCadence,
    D3D11PreviewRenderer.RenderCpuTimingMetrics? RenderCpuTiming,
    D3D11PreviewRenderer.PipelineLatencyMetrics? PipelineLatency,
    D3D11PreviewRenderer.FrameLatencyWaitMetrics? FrameLatencyWait,
    D3D11PreviewRenderer.DxgiFrameStatisticsMetrics? FrameStatistics,
    D3D11PreviewRenderer.FrameOwnershipMetrics? FrameOwnership)
{
    public static PreviewRuntimeD3DProjection Build(PreviewRuntimeSnapshotInput input)
    {
        var d3d = input.D3DRenderer;
        var frameCounters = PreviewRuntimeD3DFrameCounterPolicy.Evaluate(input);
        var rendererState = PreviewRuntimeD3DRendererStatePolicy.Evaluate(d3d, input.Signature.IsPreviewing);
        var displayCadence = d3d?.GetPresentCadenceMetrics(input.Signature.PreviewMinPresentationIntervalMs);
        var renderCpuTiming = d3d?.GetRenderCpuTimingMetrics();
        var frameOwnership = d3d?.GetFrameOwnershipMetrics();
        var frameStatistics = d3d?.GetDxgiFrameStatisticsMetrics();
        var frameLatencyWait = d3d?.GetFrameLatencyWaitMetrics();
        var pipelineLatency = d3d?.GetPipelineLatencyMetrics();

        return new PreviewRuntimeD3DProjection(frameCounters, rendererState, displayCadence, renderCpuTiming, pipelineLatency, frameLatencyWait, frameStatistics, frameOwnership);
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
            RendererAttached: d3d != null || input.Signature.PreviewSourceAttached,
            FramesArrived: gpuActive ? d3dFramesSubmitted : input.Signature.FramesArrived,
            FramesDisplayed: gpuActive ? d3dFramesRendered : input.Signature.FramesDisplayed,
            FramesDropped: gpuActive ? d3dFramesDropped : input.Signature.FramesDropped,
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
    int NaturalVideoHeight);

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
            NaturalVideoHeight: d3d?.NaturalHeight ?? 0);
}
