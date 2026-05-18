using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Gpu;

internal sealed partial class ParallelMjpegDecodePipeline
{
    private void EmitLoop()
    {
        while (!_stopped || HasAliveWorkers() || Volatile.Read(ref _reorderBufferDepth) > 0)
        {
            _emitSignal.WaitOne(8);

            var emittedAny = DrainReadyFrames();
            if (Volatile.Read(ref _fatalErrorSignaled) != 0)
            {
                break;
            }

            DetectAndResetStall(emittedAny);
        }

        if (Volatile.Read(ref _fatalErrorSignaled) == 0)
        {
            DrainReadyFrames();
            DrainRemainingFramesInOrder();
        }
        else
        {
            DiscardRemainingReorderFrames("fatal_stop");
        }
    }

    private bool DrainReadyFrames()
    {
        var emittedAny = false;
        while (true)
        {
            DiscardStaleReorderFrames();
            emittedAny = ConsumeKnownMissingFrames() || emittedAny;
            if (!TryTakeNextDecodedFrame(out var frame))
            {
                break;
            }

            emittedAny = true;

            RecordTimingSample(_reorderLatencyMs, ref _reorderLatencyCount, ref _reorderLatencyIndex, GetElapsedMilliseconds(frame.DecodedTick, Stopwatch.GetTimestamp()));
            RecordTimingSample(_pipelineLatencyMs, ref _pipelineLatencyCount, ref _pipelineLatencyIndex, GetElapsedMilliseconds(frame.Frame.ArrivalTick, Stopwatch.GetTimestamp()));
            var emitted = false;

            try
            {
                NotifyPreviewFrameDecoded(frame.Frame);
                _emitCallback(frame.Frame);
                emitted = true;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _emitFailures);
                Interlocked.Increment(ref _totalFramesDropped);
                Logger.Log($"MJPEG_EMIT_FAIL seq={frame.SeqNo} type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                frame.Frame.Dispose();
            }

            _nextEmitSeq++;
            if (emitted)
            {
                Interlocked.Increment(ref _totalFramesEmitted);
            }
        }

        if (emittedAny)
        {
            Interlocked.Exchange(ref _missingSeqSinceTickMs, -1);
        }

        return emittedAny;
    }

    private void NotifyPreviewFrameDecoded(PooledVideoFrame frame)
    {
        var callback = _previewCallback;
        if (callback == null ||
            !frame.TryAddLease(out var lease))
        {
            return;
        }

        try
        {
            callback(lease!);
            lease = null;
        }
        catch (Exception ex)
        {
            Logger.Log(
                $"MJPEG_PREVIEW_CALLBACK_FAIL seq={frame.SequenceNumber} " +
                $"type={ex.GetType().Name} msg={ex.Message}");
        }
        finally
        {
            lease?.Dispose();
        }
    }

    private void DrainRemainingFramesInOrder()
    {
        List<DecodedFrame> remaining;
        lock (_reorderLock)
        {
            remaining = _reorderFrames.Values.ToList();
            _reorderFrames.Clear();
            _knownMissingSequences.Clear();
            Volatile.Write(ref _reorderBufferDepth, 0);
            Monitor.PulseAll(_reorderLock);
        }

        remaining.Sort((a, b) => a.SeqNo.CompareTo(b.SeqNo));

        foreach (var frame in remaining)
        {
            try
            {
                NotifyPreviewFrameDecoded(frame.Frame);
                _emitCallback(frame.Frame);
                Interlocked.Increment(ref _totalFramesEmitted);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _emitFailures);
                Interlocked.Increment(ref _totalFramesDropped);
                Logger.Log($"MJPEG_EMIT_FAIL seq={frame.SeqNo} type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                frame.Frame.Dispose();
            }
        }
    }
}
