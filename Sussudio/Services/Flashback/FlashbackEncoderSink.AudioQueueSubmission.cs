using System;
using System.Threading;
using System.Threading.Channels;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private static bool IsForceRotateQueueGuarded(int queueDepth, int queueCapacity)
        =>
            queueCapacity > 0 &&
            queueDepth >= Math.Ceiling(queueCapacity * ForceRotateQueueGuardRatio);

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

            // Total saturation: both eviction and re-enqueue failed.
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
