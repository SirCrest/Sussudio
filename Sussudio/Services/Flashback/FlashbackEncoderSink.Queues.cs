using System;
using System.Threading;
using System.Threading.Channels;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private void CompleteWriter<TPacket>(Channel<TPacket>? channel)
    {
        channel?.Writer.TryComplete();
        SignalWork("complete_writer");
    }

    private void SignalWork(string operation)
    {
        try
        {
            _workAvailable.Set();
        }
        catch (ObjectDisposedException)
        {
            Logger.Log($"FLASHBACK_SINK_WORK_SIGNAL_SKIPPED op={operation} reason=disposed");
        }
    }

    private static void DecrementQueueDepth(ref int target, string queueName)
    {
        while (true)
        {
            var current = Volatile.Read(ref target);
            if (current <= 0)
            {
                Logger.Log($"FLASHBACK_SINK_QUEUE_DEPTH_UNDERFLOW queue={queueName} depth={current - 1}");
                return;
            }

            if (Interlocked.CompareExchange(ref target, current - 1, current) == current)
            {
                return;
            }
        }
    }

    private void ResetVideoDiagnostics()
    {
        _videoLatencyTracker.ResetAll();
    }

    private static bool IsForceRotateQueueGuarded(int queueDepth, int queueCapacity)
        =>
            queueCapacity > 0 &&
            queueDepth >= Math.Ceiling(queueCapacity * ForceRotateQueueGuardRatio);

    private bool WaitForCancellation(TimeSpan timeout)
    {
        var cts = _cts;
        if (cts == null)
        {
            Thread.Sleep(timeout);
            return false;
        }

        try
        {
            return cts.Token.WaitHandle.WaitOne(timeout);
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }

    private void FailEncoding(Exception ex)
    {
        var shouldNotify = false;
        lock (_sync)
        {
            if (_encodingFailure == null)
            {
                _encodingFailure = ex;
                _started = false;
                shouldNotify = true;
            }
        }

        if (!shouldNotify)
        {
            return;
        }

        Logger.Log($"FLASHBACK_SINK_FATAL type={ex.GetType().Name} msg={ex.Message}");
        CompleteWriter(_videoQueue);
        CompleteWriter(_audioQueue);
        CompleteWriter(_microphoneQueue);
        CompleteWriter(_gpuQueue);

        try
        {
            _onFatalError?.Invoke(ex);
        }
        catch (Exception callbackEx)
        {
            Logger.Log($"FLASHBACK_SINK_FATAL_CALLBACK_FAIL type={callbackEx.GetType().Name} msg={callbackEx.Message}");
        }
    }

    private bool TryEnqueueAudioPacket(
        Channel<AudioSamplePacket> queue,
        AudioSamplePacket packet,
        ref int queueDepth,
        ref long backlogEvictions)
    {
        lock (_videoQueueSync)
        {
        if (_disposed ||
            !_started ||
            _cts?.IsCancellationRequested == true ||
            (Volatile.Read(ref _forceRotateDraining) &&
             IsForceRotateQueueGuarded(Volatile.Read(ref queueDepth), AudioQueueCapacity)) ||
            Volatile.Read(ref _encodingFailure) != null)
        {
            ReturnBuffer(packet.Buffer);
            return false;
        }

        if (TryWriteAudioPacket(queue, packet, ref queueDepth, "audio"))
        {
            SignalWork("audio_enqueue");
            return true;
        }

        if (queue.Reader.TryRead(out var evictedPacket))
        {
            DecrementQueueDepth(ref queueDepth, "audio_evict");
            Interlocked.Increment(ref backlogEvictions);
            // Track dropped audio samples for A/V drift diagnostics (analogous to SkipVideoFrame for video)
            var evictedSamples = GetSampleCount(evictedPacket.Length);
            var totalDropped = Interlocked.Add(ref _droppedAudioSamplesCount, evictedSamples);
            if (totalDropped == evictedSamples || totalDropped % 48_000 < evictedSamples)
            {
                Logger.Log(
                    $"FLASHBACK_SINK_AUDIO_EVICT_PTS samples={evictedSamples} total_dropped_samples={totalDropped} " +
                    $"drift_ms={totalDropped * 1000.0 / 48_000:F1}");
            }
            ReturnBuffer(evictedPacket.Buffer);
            if (TryWriteAudioPacket(queue, packet, ref queueDepth, "audio_after_evict"))
            {
                SignalWork("audio_after_evict");
                return true;
            }
        }

        // Total saturation — both eviction and re-enqueue failed
        var saturatedSamples = GetSampleCount(packet.Length);
        Interlocked.Add(ref _droppedAudioSamplesCount, saturatedSamples);
        ReturnBuffer(packet.Buffer);
        return false;
        }
    }

    private static bool TryWriteAudioPacket(
        Channel<AudioSamplePacket> queue,
        AudioSamplePacket packet,
        ref int queueDepth,
        string queueName)
    {
        Interlocked.Increment(ref queueDepth);
        if (queue.Writer.TryWrite(packet))
        {
            return true;
        }

        DecrementQueueDepth(ref queueDepth, $"{queueName}_write_failed");
        return false;
    }

}
