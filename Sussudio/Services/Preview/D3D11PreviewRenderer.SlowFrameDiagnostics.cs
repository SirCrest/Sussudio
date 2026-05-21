using System;
using Sussudio.Models;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private readonly object _slowFrameDiagnosticsLock = new();
    private readonly PreviewSlowFrameDiagnostic[] _slowFrameDiagnostics = new PreviewSlowFrameDiagnostic[64];
    private int _slowFrameDiagnosticsCount;
    private int _slowFrameDiagnosticsIndex;

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

}
