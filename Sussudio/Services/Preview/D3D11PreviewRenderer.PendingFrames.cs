using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Sussudio.Services.Preview;

internal interface IPreviewFrameQueueControl
{
    int DropPendingFrames(string reason);
}

internal sealed partial class D3D11PreviewRenderer
{
    private readonly ManualResetEventSlim _frameReadyEvent = new(false);
    private readonly ConcurrentQueue<PendingFrame> _pendingFrames = new();
    private int _pendingFrameCount;

    private void EnqueuePendingFrame(PendingFrame frame)
    {
        lock (_lifecycleLock)
        {
            if (Volatile.Read(ref _disposed) != 0 ||
                Volatile.Read(ref _stopRequested) != 0 ||
                _renderThread == null)
            {
                TrackFrameDropped(frame, "renderer-stopped");
                frame.Dispose();
                return;
            }

            frame.SubmissionGeneration = Interlocked.Read(ref _submissionGeneration);
            var pendingFrameCount = Interlocked.Increment(ref _pendingFrameCount);
            _pendingFrames.Enqueue(frame);
            TrackFrameSubmitted(frame);

            // Trim oldest frames if the queue exceeds the elastic limit.
            // Under normal operation the render thread keeps up and the queue
            // stays at 0-1 (no added latency). The extra slots only absorb
            // brief render hiccups instead of dropping frames.
            while (pendingFrameCount > _maxPendingFrames)
            {
                if (TryDequeuePendingFrame(out var oldest))
                {
                    TrackFrameDropped(oldest, "renderer-backlog");
                    oldest.Dispose();
                    pendingFrameCount = PendingFrameCount;
                }
                else
                {
                    Interlocked.Exchange(ref _pendingFrameCount, 0);
                    break;
                }
            }

            Volatile.Write(ref _naturalWidth, frame.Width);
            Volatile.Write(ref _naturalHeight, frame.Height);
            Interlocked.Increment(ref _framesSubmitted);
        }

        SignalFrameReady("pending_frame");
    }

    private void SignalFrameReady(string operation)
    {
        try
        {
            _frameReadyEvent.Set();
        }
        catch (ObjectDisposedException)
        {
            Logger.Log($"D3D11_PREVIEW_FRAME_SIGNAL_SKIPPED op={operation} reason=disposed");
        }
    }

    private void ResetFrameReady(string operation)
    {
        try
        {
            _frameReadyEvent.Reset();
        }
        catch (ObjectDisposedException)
        {
            Logger.Log($"D3D11_PREVIEW_FRAME_RESET_SKIPPED op={operation} reason=disposed");
        }
    }

    private bool TryDequeuePendingFrame(out PendingFrame frame)
    {
        if (_pendingFrames.TryDequeue(out var dequeued))
        {
            frame = dequeued;
            DecrementPendingFrameCount();
            return true;
        }

        frame = null!;
        return false;
    }

    public int DropPendingFrames(string reason)
    {
        var normalizedReason = string.IsNullOrWhiteSpace(reason)
            ? "explicit-drain"
            : reason.Trim();
        var dropped = 0;
        Volatile.Write(ref _submissionGenerationDropReason, normalizedReason);
        Interlocked.Increment(ref _submissionGeneration);
        lock (_lifecycleLock)
        {
            while (TryDequeuePendingFrame(out var stale))
            {
                TrackFrameDropped(stale, normalizedReason);
                stale.Dispose();
                dropped++;
            }
        }

        if (dropped > 0)
        {
            Logger.Log($"D3D11_PREVIEW_PENDING_DRAIN reason={normalizedReason} dropped={dropped}");
            if (_pendingFrames.IsEmpty &&
                Volatile.Read(ref _compositionTransformDirty) == 0 &&
                Volatile.Read(ref _sharedDeviceResetPending) == 0)
            {
                ResetFrameReady("pending_drain");
            }
        }

        return dropped;
    }

    private void DecrementPendingFrameCount()
    {
        while (true)
        {
            var current = Volatile.Read(ref _pendingFrameCount);
            if (current <= 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _pendingFrameCount, current - 1, current) == current)
            {
                return;
            }
        }
    }
}
