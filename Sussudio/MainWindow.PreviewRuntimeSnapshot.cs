using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Sussudio.Models;

namespace Sussudio;

// UI-thread automation/runtime snapshot dispatch and read-only preview state
// projection for diagnostics and MCP/CLI callers.
public sealed partial class MainWindow
{
    private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            return GetPreviewRuntimeSnapshot();
        }

        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var completion = new TaskCompletionSource<PreviewRuntimeSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration registration = default;
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    completion.TrySetCanceled(cancellationToken);
                });
            }

            var enqueued = _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        completion.TrySetCanceled(cancellationToken);
                        return;
                    }

                    completion.TrySetResult(GetPreviewRuntimeSnapshot());
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
                finally
                {
                    registration.Dispose();
                }
            });

            if (enqueued)
            {
                return await completion.Task.ConfigureAwait(false);
            }

            registration.Dispose();
            if (attempt >= maxAttempts)
            {
                break;
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException("Failed to enqueue preview snapshot operation.");
    }

    private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()
    {
        var d3d = _d3dRenderer;
        var nowTick = Environment.TickCount64;
        var framesArrived = Interlocked.Read(ref _previewFramesArrived);
        var framesDisplayed = Interlocked.Read(ref _previewFramesDisplayed);
        var framesDropped = Interlocked.Read(ref _previewFramesDropped);
        var lastPresentedTick = Interlocked.Read(ref _previewLastPresentedTick);
        var gpuActive = d3d != null;
        var gpuElementVisible = PreviewSwapChainPanel.Visibility == Visibility.Visible;
        var cpuElementVisible = PreviewImage.Visibility == Visibility.Visible;
        var rendererAttached = d3d != null || _previewSource != null;
        var placeholderVisible = NoDevicePlaceholder.Visibility == Visibility.Visible;
        var previewPipelineActive = ViewModel.IsPreviewing && rendererAttached;
        var d3dFramesSubmitted = d3d?.FramesSubmitted ?? 0;
        var d3dFramesRendered = d3d?.FramesRendered ?? 0;
        var d3dFramesDropped = d3d?.FramesDropped ?? 0;
        var d3dRenderCpuTiming = d3d?.GetRenderCpuTimingMetrics();
        var d3dFrameOwnership = d3d?.GetFrameOwnershipMetrics();
        var d3dFrameStats = d3d?.GetDxgiFrameStatisticsMetrics();
        var d3dFrameLatencyWait = d3d?.GetFrameLatencyWaitMetrics();
        var d3dPipelineLatency = d3d?.GetPipelineLatencyMetrics();
        var d3dSlowFrames = d3d?.GetRecentSlowFrameDiagnostics() ?? Array.Empty<PreviewSlowFrameDiagnostic>();
        if (gpuActive)
        {
            framesArrived = d3dFramesSubmitted;
            framesDisplayed = d3dFramesRendered;
            framesDropped = d3dFramesDropped;
        }

        var rendererMode = d3d?.RendererMode
            ?? (ViewModel.IsPreviewing ? "CpuSoftwareBitmap" : "None");
        var gpuPlaybackState = "None";
        int gpuNaturalVideoWidth = 0, gpuNaturalVideoHeight = 0;
        double gpuPositionMs = 0;
        if (d3d != null)
        {
            gpuPlaybackState = d3d.IsRendering ? "Rendering" : "Idle";
            gpuNaturalVideoWidth = d3d.NaturalWidth;
            gpuNaturalVideoHeight = d3d.NaturalHeight;
            gpuPositionMs = 0;
        }
        var gpuPositionEventCount = Interlocked.Read(ref _previewStartupPositionEventCount);

        var startupElapsedMs = _previewStartupRequestedUtc.HasValue
            ? Math.Max(0, (DateTimeOffset.UtcNow - _previewStartupRequestedUtc.Value).TotalMilliseconds)
            : (double?)null;
        var startupMissingSignals = _previewStartupMissingSignals;
        if (string.IsNullOrWhiteSpace(startupMissingSignals) &&
            _previewStartupState is PreviewStartupState.WaitingForFirstVisual or PreviewStartupState.Failed)
        {
            startupMissingSignals = BuildPreviewStartupMissingSignals();
        }
        var startupTimedOut = ViewModel.IsPreviewing &&
                              _previewStartupState == PreviewStartupState.WaitingForFirstVisual &&
                              startupElapsedMs.GetValueOrDefault() >= PreviewStartupVisualTimeoutMs;
        var blankSuspected = !gpuActive && previewPipelineActive &&
                             framesArrived > 30 &&
                             framesDisplayed == 0;
        if (!blankSuspected && startupTimedOut)
        {
            blankSuspected = true;
        }
        var stallSuspected = !gpuActive && previewPipelineActive &&
                             lastPresentedTick > 0 &&
                             nowTick - lastPresentedTick > 3000;
        var rendererCadence = d3d?.GetPresentCadenceMetrics(_previewMinPresentationIntervalMs);

        return new PreviewRuntimeSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            IsPreviewing = ViewModel.IsPreviewing,
            GpuActive = gpuActive,
            PlaceholderVisible = placeholderVisible,
            GpuElementVisible = gpuElementVisible,
            CpuElementVisible = cpuElementVisible,
            RendererAttached = rendererAttached,
            StartupState = _previewStartupState.ToString(),
            StartupAttemptId = _previewStartupAttemptId,
            StartupElapsedMs = startupElapsedMs,
            StartupTimeoutMs = PreviewStartupVisualTimeoutMs,
            StartupGpuSignalMediaOpened = _previewGpuSignalMediaOpened,
            StartupGpuSignalFirstFrame = _previewGpuSignalFirstFrame,
            StartupGpuSignalPlaybackAdvancing = _previewGpuSignalPlaybackAdvancing,
            StartupRequiredSignals = _previewStartupRequiredSignals,
            StartupReceivedSignals = _previewStartupReceivedSignals,
            StartupStrategy = _previewStartupStrategy,
            StartupMissingSignals = startupMissingSignals,
            StartupRecoveryAttemptCount = _previewRecoveryAttemptCount,
            StartupLastFailureReason = _previewLastFailureReason,
            FirstVisualConfirmed = _previewFirstVisualConfirmed,
            FramesArrived = framesArrived,
            FramesDisplayed = framesDisplayed,
            FramesDropped = framesDropped,
            DisplayCadenceSampleCount = rendererCadence?.SampleCount ?? 0,
            DisplayCadenceObservedFps = rendererCadence?.ObservedFps ?? 0,
            DisplayCadenceExpectedIntervalMs = rendererCadence?.ExpectedIntervalMs ?? 0,
            DisplayCadenceAverageIntervalMs = rendererCadence?.AverageIntervalMs ?? 0,
            DisplayCadenceP95IntervalMs = rendererCadence?.P95IntervalMs ?? 0,
            DisplayCadenceP99IntervalMs = rendererCadence?.P99IntervalMs ?? 0,
            DisplayCadenceMaxIntervalMs = rendererCadence?.MaxIntervalMs ?? 0,
            DisplayCadenceOnePercentLowFps = rendererCadence?.OnePercentLowFps ?? 0,
            DisplayCadenceFivePercentLowFps = rendererCadence?.FivePercentLowFps ?? 0,
            DisplayCadenceSampleDurationMs = rendererCadence?.SampleDurationMs ?? 0,
            DisplayCadenceRecentIntervalsMs = rendererCadence?.RecentIntervalsMs ?? Array.Empty<double>(),
            DisplayCadenceJitterStdDevMs = rendererCadence?.JitterStdDevMs ?? 0,
            DisplayCadenceSlowFrameCount = rendererCadence?.SlowFrameCount ?? 0,
            DisplayCadenceSlowFramePercent = rendererCadence?.SlowFramePercent ?? 0,
            BlankSuspected = blankSuspected,
            StallSuspected = stallSuspected,
            RendererMode = rendererMode,
            D3DPresentSyncInterval = d3d?.PresentSyncInterval ?? 0,
            D3DMaxFrameLatency = d3d?.DxgiMaxFrameLatency ?? 0,
            D3DSwapChainBufferCount = d3d?.SwapChainBufferCount ?? 0,
            D3DSwapChainAddress = d3d?.SwapChainAddress ?? string.Empty,
            D3DFramesSubmitted = d3dFramesSubmitted,
            D3DFramesRendered = d3dFramesRendered,
            D3DFramesDropped = d3dFramesDropped,
            D3DRenderThreadFailureCount = d3d?.RenderThreadFailureCount ?? 0,
            D3DLastRenderThreadFailureType = d3d?.LastRenderThreadFailureType ?? string.Empty,
            D3DLastRenderThreadFailureMessage = d3d?.LastRenderThreadFailureMessage ?? string.Empty,
            D3DLastRenderThreadFailureHResult = d3d?.LastRenderThreadFailureHResult ?? 0,
            D3DPendingFrameCount = d3d?.PendingFrameCount ?? 0,
            D3DInputColorSpace = _d3dRenderer?.InputColorSpaceLabel ?? "None",
            D3DOutputColorSpace = _d3dRenderer?.OutputColorSpaceLabel ?? "None",
            D3DCpuTimingSampleCount = d3dRenderCpuTiming?.TotalFrame.SampleCount ?? 0,
            D3DInputUploadCpuAvgMs = d3dRenderCpuTiming?.InputUpload.AverageMs ?? 0,
            D3DInputUploadCpuP95Ms = d3dRenderCpuTiming?.InputUpload.P95Ms ?? 0,
            D3DInputUploadCpuP99Ms = d3dRenderCpuTiming?.InputUpload.P99Ms ?? 0,
            D3DInputUploadCpuMaxMs = d3dRenderCpuTiming?.InputUpload.MaxMs ?? 0,
            D3DRenderSubmitCpuAvgMs = d3dRenderCpuTiming?.RenderSubmit.AverageMs ?? 0,
            D3DRenderSubmitCpuP95Ms = d3dRenderCpuTiming?.RenderSubmit.P95Ms ?? 0,
            D3DRenderSubmitCpuP99Ms = d3dRenderCpuTiming?.RenderSubmit.P99Ms ?? 0,
            D3DRenderSubmitCpuMaxMs = d3dRenderCpuTiming?.RenderSubmit.MaxMs ?? 0,
            D3DPresentCallAvgMs = d3dRenderCpuTiming?.PresentCall.AverageMs ?? 0,
            D3DPresentCallP95Ms = d3dRenderCpuTiming?.PresentCall.P95Ms ?? 0,
            D3DPresentCallP99Ms = d3dRenderCpuTiming?.PresentCall.P99Ms ?? 0,
            D3DPresentCallMaxMs = d3dRenderCpuTiming?.PresentCall.MaxMs ?? 0,
            D3DTotalFrameCpuAvgMs = d3dRenderCpuTiming?.TotalFrame.AverageMs ?? 0,
            D3DTotalFrameCpuP95Ms = d3dRenderCpuTiming?.TotalFrame.P95Ms ?? 0,
            D3DTotalFrameCpuP99Ms = d3dRenderCpuTiming?.TotalFrame.P99Ms ?? 0,
            D3DTotalFrameCpuMaxMs = d3dRenderCpuTiming?.TotalFrame.MaxMs ?? 0,
            D3DPipelineLatencySampleCount = d3dPipelineLatency?.SampleCount ?? 0,
            D3DPipelineLatencyAvgMs = d3dPipelineLatency?.AverageMs ?? 0,
            D3DPipelineLatencyP95Ms = d3dPipelineLatency?.P95Ms ?? 0,
            D3DPipelineLatencyP99Ms = d3dPipelineLatency?.P99Ms ?? 0,
            D3DPipelineLatencyMaxMs = d3dPipelineLatency?.MaxMs ?? 0,
            D3DFrameLatencyWaitEnabled = d3dFrameLatencyWait?.Enabled ?? false,
            D3DFrameLatencyWaitHandleActive = d3dFrameLatencyWait?.HandleActive ?? false,
            D3DFrameLatencyWaitCallCount = d3dFrameLatencyWait?.CallCount ?? 0,
            D3DFrameLatencyWaitSignaledCount = d3dFrameLatencyWait?.SignaledCount ?? 0,
            D3DFrameLatencyWaitTimeoutCount = d3dFrameLatencyWait?.TimeoutCount ?? 0,
            D3DFrameLatencyWaitUnexpectedResultCount = d3dFrameLatencyWait?.UnexpectedResultCount ?? 0,
            D3DFrameLatencyWaitLastResult = d3dFrameLatencyWait?.LastResult ?? 0,
            D3DFrameLatencyWaitLastMs = d3dFrameLatencyWait?.LastWaitMs ?? 0,
            D3DFrameLatencyWaitSampleCount = d3dFrameLatencyWait?.Timing.SampleCount ?? 0,
            D3DFrameLatencyWaitAvgMs = d3dFrameLatencyWait?.Timing.AverageMs ?? 0,
            D3DFrameLatencyWaitP95Ms = d3dFrameLatencyWait?.Timing.P95Ms ?? 0,
            D3DFrameLatencyWaitP99Ms = d3dFrameLatencyWait?.Timing.P99Ms ?? 0,
            D3DFrameLatencyWaitMaxMs = d3dFrameLatencyWait?.Timing.MaxMs ?? 0,
            D3DFrameStatsSampleCount = d3dFrameStats?.SampleCount ?? 0,
            D3DFrameStatsSuccessCount = d3dFrameStats?.SuccessCount ?? 0,
            D3DFrameStatsFailureCount = d3dFrameStats?.FailureCount ?? 0,
            D3DFrameStatsLastError = d3dFrameStats?.LastError ?? string.Empty,
            D3DFrameStatsPresentCount = d3dFrameStats?.PresentCount ?? -1,
            D3DFrameStatsPresentRefreshCount = d3dFrameStats?.PresentRefreshCount ?? -1,
            D3DFrameStatsSyncRefreshCount = d3dFrameStats?.SyncRefreshCount ?? -1,
            D3DFrameStatsSyncQpcTime = d3dFrameStats?.SyncQpcTime ?? 0,
            D3DFrameStatsLastPresentDelta = d3dFrameStats?.LastPresentDelta ?? 0,
            D3DFrameStatsLastPresentRefreshDelta = d3dFrameStats?.LastPresentRefreshDelta ?? 0,
            D3DFrameStatsLastSyncRefreshDelta = d3dFrameStats?.LastSyncRefreshDelta ?? 0,
            D3DFrameStatsMissedRefreshCount = d3dFrameStats?.MissedRefreshCount ?? 0,
            D3DLastSubmittedPreviewPresentId = d3dFrameOwnership?.LastSubmittedPreviewPresentId ?? 0,
            D3DLastSubmittedSourceSequenceNumber = d3dFrameOwnership?.LastSubmittedSourceSequenceNumber ?? -1,
            D3DLastSubmittedSourcePtsTicks = d3dFrameOwnership?.LastSubmittedSourcePtsTicks ?? 0,
            D3DLastSubmittedQpc = d3dFrameOwnership?.LastSubmittedQpc ?? 0,
            D3DLastSubmittedUtcUnixMs = d3dFrameOwnership?.LastSubmittedUtcUnixMs ?? 0,
            D3DLastRenderedPreviewPresentId = d3dFrameOwnership?.LastRenderedPreviewPresentId ?? 0,
            D3DLastRenderedSourceSequenceNumber = d3dFrameOwnership?.LastRenderedSourceSequenceNumber ?? -1,
            D3DLastRenderedSourcePtsTicks = d3dFrameOwnership?.LastRenderedSourcePtsTicks ?? 0,
            D3DLastRenderedQpc = d3dFrameOwnership?.LastRenderedQpc ?? 0,
            D3DLastRenderedUtcUnixMs = d3dFrameOwnership?.LastRenderedUtcUnixMs ?? 0,
            D3DLastRenderedSchedulerToPresentMs = d3dFrameOwnership?.LastRenderedSchedulerToPresentMs ?? 0,
            D3DLastRenderedPipelineLatencyMs = d3dFrameOwnership?.LastRenderedPipelineLatencyMs ?? 0,
            D3DLastDroppedPreviewPresentId = d3dFrameOwnership?.LastDroppedPreviewPresentId ?? 0,
            D3DLastDroppedSourceSequenceNumber = d3dFrameOwnership?.LastDroppedSourceSequenceNumber ?? -1,
            D3DLastDroppedSourcePtsTicks = d3dFrameOwnership?.LastDroppedSourcePtsTicks ?? 0,
            D3DLastDroppedQpc = d3dFrameOwnership?.LastDroppedQpc ?? 0,
            D3DLastDroppedUtcUnixMs = d3dFrameOwnership?.LastDroppedUtcUnixMs ?? 0,
            D3DLastDropReason = d3dFrameOwnership?.LastDropReason ?? string.Empty,
            D3DRecentSlowFrames = d3dSlowFrames,
            EstimatedPipelineLatencyMs = d3dPipelineLatency?.AverageMs ?? 0,
            GpuPlaybackState = gpuPlaybackState,
            GpuNaturalVideoWidth = gpuNaturalVideoWidth,
            GpuNaturalVideoHeight = gpuNaturalVideoHeight,
            GpuPositionMs = gpuPositionMs,
            GpuPositionEventCount = gpuPositionEventCount
        };
    }
}
