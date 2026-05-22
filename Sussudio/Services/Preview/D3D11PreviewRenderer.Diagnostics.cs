using System;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private readonly object _slowFrameDiagnosticsLock = new();
    private readonly PreviewSlowFrameDiagnostic[] _slowFrameDiagnostics = new PreviewSlowFrameDiagnostic[64];
    private string _lastRenderThreadFailureType = string.Empty;
    private string _lastRenderThreadFailureMessage = string.Empty;
    private int _firstFrameRaised;
    private int _lastRenderThreadFailureHResult;
    private int _slowFrameDiagnosticsCount;
    private int _slowFrameDiagnosticsIndex;
    private long _renderThreadFailureCount;

    public PreviewSlowFrameDiagnostic[] GetRecentSlowFrameDiagnostics(int maxEntries = 16)
    {
        lock (_slowFrameDiagnosticsLock)
        {
            var take = Math.Min(Math.Max(0, maxEntries), _slowFrameDiagnosticsCount);
            if (take <= 0)
            {
                return Array.Empty<PreviewSlowFrameDiagnostic>();
            }

            var result = new PreviewSlowFrameDiagnostic[take];
            var start = (_slowFrameDiagnosticsIndex - take + _slowFrameDiagnostics.Length) % _slowFrameDiagnostics.Length;
            for (var i = 0; i < take; i++)
            {
                result[i] = _slowFrameDiagnostics[(start + i) % _slowFrameDiagnostics.Length];
            }

            return result;
        }
    }

    private void ResetFirstFrameNotification()
        => Interlocked.Exchange(ref _firstFrameRaised, 0);

    private void NotifyFirstFrameRendered(string message)
    {
        if (Interlocked.Exchange(ref _firstFrameRaised, 1) != 0)
        {
            return;
        }

        Logger.Log(message);
        if (!_dispatcherQueue.TryEnqueue(() => FirstFrameRendered?.Invoke()))
        {
            Logger.Log("D3D_FIRST_FRAME_UI_ENQUEUE_FAILED");
        }
    }

    private void NotifyRenderThreadFailed(Exception ex)
    {
        Interlocked.Increment(ref _renderThreadFailureCount);
        Volatile.Write(ref _lastRenderThreadFailureType, ex.GetType().Name);
        Volatile.Write(ref _lastRenderThreadFailureMessage, ex.Message);
        Volatile.Write(ref _lastRenderThreadFailureHResult, ex.HResult);

        var reason = $"{ex.GetType().Name}: {ex.Message}";
        if (!_dispatcherQueue.TryEnqueue(() => RenderThreadFailed?.Invoke(reason)))
        {
            Logger.Log("D3D_RENDER_THREAD_FAILURE_UI_ENQUEUE_FAILED");
        }
    }

    private void RecordSlowFrameDiagnostic(
        PendingFrame frame,
        double presentIntervalMs,
        long inputUploadTicks,
        long renderSubmitTicks,
        long presentCallTicks,
        long totalTicks,
        long presentEndTick,
        long estimatedVisibleTick)
    {
        var inputUploadMs = TicksToMs(inputUploadTicks);
        var renderSubmitMs = TicksToMs(renderSubmitTicks);
        var presentCallMs = TicksToMs(presentCallTicks);
        var totalMs = TicksToMs(totalTicks);
        if (!IsValidRenderCpuStageMs(totalMs))
        {
            return;
        }

        var expectedIntervalMs = _startupFps > 0 ? 1000.0 / _startupFps : 8.333;
        var thresholdMs = _slowFrameDiagnosticThresholdMs > 0
            ? _slowFrameDiagnosticThresholdMs
            : Math.Max(expectedIntervalMs * 1.02, expectedIntervalMs + 0.15);

        var dxgiSlip = CaptureSlowFrameDxgiSlipSnapshot();
        if ((presentIntervalMs <= 0 || presentIntervalMs < thresholdMs) &&
            totalMs < thresholdMs &&
            presentCallMs < thresholdMs &&
            !dxgiSlip.IsRefreshSlip)
        {
            return;
        }

        var schedulerToPresentMs = frame.SchedulerSubmitTick > 0 && presentEndTick > frame.SchedulerSubmitTick
            ? TicksToMs(presentEndTick - frame.SchedulerSubmitTick)
            : 0;
        var pipelineLatencyMs = frame.ArrivalTick > 0 && estimatedVisibleTick > frame.ArrivalTick
            ? TicksToMs(estimatedVisibleTick - frame.ArrivalTick)
            : 0;
        var worstObservedMs = Math.Max(
            Math.Max(presentIntervalMs > 0 ? presentIntervalMs : 0, totalMs),
            presentCallMs);
        var worstOverBudgetMs = Math.Max(0, worstObservedMs - expectedIntervalMs);
        var slowReason = BuildSlowFrameDiagnosticReason(
            presentIntervalMs,
            totalMs,
            presentCallMs,
            dxgiSlip.IsRefreshSlip,
            thresholdMs);
        var sample = new PreviewSlowFrameDiagnostic
        {
            PreviewPresentId = frame.PreviewPresentId,
            SourceSequenceNumber = frame.SourceSequenceNumber,
            QpcTimestamp = presentEndTick,
            UtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            PresentIntervalMs = presentIntervalMs,
            InputUploadCpuMs = IsValidRenderCpuStageMs(inputUploadMs) ? inputUploadMs : 0,
            RenderSubmitCpuMs = IsValidRenderCpuStageMs(renderSubmitMs) ? renderSubmitMs : 0,
            PresentCallMs = IsValidRenderCpuStageMs(presentCallMs) ? presentCallMs : 0,
            TotalFrameCpuMs = totalMs,
            SchedulerToPresentMs = schedulerToPresentMs,
            PipelineLatencyMs = pipelineLatencyMs,
            ExpectedIntervalMs = expectedIntervalMs,
            DiagnosticThresholdMs = thresholdMs,
            WorstOverBudgetMs = worstOverBudgetMs,
            SlowReason = slowReason,
            PendingFrameCount = PendingFrameCount,
            DxgiPresentDelta = dxgiSlip.PresentDelta,
            DxgiPresentRefreshDelta = dxgiSlip.PresentRefreshDelta,
            DxgiSyncRefreshDelta = dxgiSlip.SyncRefreshDelta,
            DxgiMissedRefreshCount = dxgiSlip.MissedRefreshCount
        };

        lock (_slowFrameDiagnosticsLock)
        {
            _slowFrameDiagnostics[_slowFrameDiagnosticsIndex] = sample;
            _slowFrameDiagnosticsIndex = (_slowFrameDiagnosticsIndex + 1) % _slowFrameDiagnostics.Length;
            if (_slowFrameDiagnosticsCount < _slowFrameDiagnostics.Length)
            {
                _slowFrameDiagnosticsCount++;
            }
        }
    }

    private static string BuildSlowFrameDiagnosticReason(
        double presentIntervalMs,
        double totalFrameCpuMs,
        double presentCallMs,
        bool dxgiRefreshSlip,
        double thresholdMs)
    {
        var reason = string.Empty;
        AppendSlowFrameReason(ref reason, presentIntervalMs >= thresholdMs, "present_interval");
        AppendSlowFrameReason(ref reason, totalFrameCpuMs >= thresholdMs, "total_cpu");
        AppendSlowFrameReason(ref reason, presentCallMs >= thresholdMs, "present_call");
        AppendSlowFrameReason(ref reason, dxgiRefreshSlip, "dxgi_refresh_slip");
        return reason.Length > 0 ? reason : "unknown";
    }

    private static void AppendSlowFrameReason(ref string reason, bool condition, string token)
    {
        if (!condition)
        {
            return;
        }

        reason = reason.Length == 0 ? token : $"{reason}+{token}";
    }
}
