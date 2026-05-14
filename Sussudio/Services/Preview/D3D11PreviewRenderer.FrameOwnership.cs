using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    public FrameOwnershipMetrics GetFrameOwnershipMetrics()
    {
        var schedulerToPresentTicks = Interlocked.Read(ref _lastRenderedSchedulerToPresentTicks);
        var pipelineLatencyTicks = Interlocked.Read(ref _lastRenderedPipelineLatencyTicks);
        return new FrameOwnershipMetrics(
            LastSubmittedPreviewPresentId: Interlocked.Read(ref _lastSubmittedPreviewPresentId),
            LastSubmittedSourceSequenceNumber: Interlocked.Read(ref _lastSubmittedSourceSequenceNumber),
            LastSubmittedSourcePtsTicks: Interlocked.Read(ref _lastSubmittedSourcePtsTicks),
            LastSubmittedQpc: Interlocked.Read(ref _lastSubmittedQpc),
            LastSubmittedUtcUnixMs: Interlocked.Read(ref _lastSubmittedUtcUnixMs),
            LastRenderedPreviewPresentId: Interlocked.Read(ref _lastRenderedPreviewPresentId),
            LastRenderedSourceSequenceNumber: Interlocked.Read(ref _lastRenderedSourceSequenceNumber),
            LastRenderedSourcePtsTicks: Interlocked.Read(ref _lastRenderedSourcePtsTicks),
            LastRenderedQpc: Interlocked.Read(ref _lastRenderedQpc),
            LastRenderedUtcUnixMs: Interlocked.Read(ref _lastRenderedUtcUnixMs),
            LastRenderedSchedulerToPresentMs: schedulerToPresentTicks > 0 ? TicksToMs(schedulerToPresentTicks) : 0,
            LastRenderedPipelineLatencyMs: pipelineLatencyTicks > 0 ? TicksToMs(pipelineLatencyTicks) : 0,
            LastDroppedPreviewPresentId: Interlocked.Read(ref _lastDroppedPreviewPresentId),
            LastDroppedSourceSequenceNumber: Interlocked.Read(ref _lastDroppedSourceSequenceNumber),
            LastDroppedSourcePtsTicks: Interlocked.Read(ref _lastDroppedSourcePtsTicks),
            LastDroppedQpc: Interlocked.Read(ref _lastDroppedQpc),
            LastDroppedUtcUnixMs: Interlocked.Read(ref _lastDroppedUtcUnixMs),
            LastDropReason: Volatile.Read(ref _lastDropReason));
    }

    private void TrackFrameSubmitted(PendingFrame frame)
    {
        Interlocked.Exchange(ref _lastSubmittedPreviewPresentId, frame.PreviewPresentId);
        Interlocked.Exchange(ref _lastSubmittedSourceSequenceNumber, frame.SourceSequenceNumber);
        Interlocked.Exchange(ref _lastSubmittedSourcePtsTicks, frame.SourcePtsTicks);
        Interlocked.Exchange(ref _lastSubmittedQpc, Stopwatch.GetTimestamp());
        Interlocked.Exchange(ref _lastSubmittedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    private void TrackFramePresented(PendingFrame frame, long presentReturnTick, long estimatedVisibleTick)
    {
        Interlocked.Exchange(ref _lastRenderedPreviewPresentId, frame.PreviewPresentId);
        Interlocked.Exchange(ref _lastRenderedSourceSequenceNumber, frame.SourceSequenceNumber);
        Interlocked.Exchange(ref _lastRenderedSourcePtsTicks, frame.SourcePtsTicks);
        Interlocked.Exchange(ref _lastRenderedQpc, presentReturnTick);
        Interlocked.Exchange(ref _lastRenderedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var schedulerToPresentTicks = frame.SchedulerSubmitTick > 0 && presentReturnTick > frame.SchedulerSubmitTick
            ? presentReturnTick - frame.SchedulerSubmitTick
            : 0;
        var pipelineLatencyTicks = frame.ArrivalTick > 0 && estimatedVisibleTick > frame.ArrivalTick
            ? estimatedVisibleTick - frame.ArrivalTick
            : 0;
        Interlocked.Exchange(ref _lastRenderedSchedulerToPresentTicks, schedulerToPresentTicks);
        Interlocked.Exchange(ref _lastRenderedPipelineLatencyTicks, pipelineLatencyTicks);
    }

    private void TrackFrameDropped(PendingFrame frame, string reason)
    {
        Interlocked.Increment(ref _framesDropped);
        Interlocked.Exchange(ref _lastDroppedPreviewPresentId, frame.PreviewPresentId);
        Interlocked.Exchange(ref _lastDroppedSourceSequenceNumber, frame.SourceSequenceNumber);
        Interlocked.Exchange(ref _lastDroppedSourcePtsTicks, frame.SourcePtsTicks);
        Interlocked.Exchange(ref _lastDroppedQpc, Stopwatch.GetTimestamp());
        Interlocked.Exchange(ref _lastDroppedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Volatile.Write(ref _lastDropReason, reason);
    }
}
