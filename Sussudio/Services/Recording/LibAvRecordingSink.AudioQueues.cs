using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Sussudio.Services.Recording;

public sealed partial class LibAvRecordingSink
{
    public Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)
    {
        // Hot WASAPI callback path: copy/enqueue only, never await or block.
        cancellationToken.ThrowIfCancellationRequested();
        var queue = _audioQueue;
        if (_disposed || !_started || !_audioEnabled || queue == null || samples.IsEmpty)
        {
            return Task.CompletedTask;
        }

        var buffer = GetBuffer(samples.Length);
        samples.Span.CopyTo(buffer.AsSpan(0, samples.Length));
        var packet = new AudioSamplePacket(buffer, samples.Length);
        if (TryEnqueueAudioPacket(queue, packet))
        {
            return Task.CompletedTask;
        }

        var dropped = Interlocked.Increment(ref _audioDropsQueueSaturated);
        if (dropped == 1 || dropped % 120 == 0)
        {
            Logger.Log(
                $"LIBAV_SINK_AUDIO_DROP saturated={dropped} evicted={Interlocked.Read(ref _audioDropsBacklogEviction)}");
        }

        return Task.CompletedTask;
    }

    public Task WriteMicrophoneAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)
    {
        // Hot WASAPI callback path: copy/enqueue only, never await or block.
        cancellationToken.ThrowIfCancellationRequested();
        var queue = _microphoneQueue;
        if (_disposed || !_started || !_microphoneEnabled || queue == null || samples.IsEmpty)
        {
            return Task.CompletedTask;
        }

        var buffer = GetBuffer(samples.Length);
        samples.Span.CopyTo(buffer.AsSpan(0, samples.Length));
        var packet = new AudioSamplePacket(buffer, samples.Length);
        if (TryEnqueueMicrophonePacket(queue, packet))
        {
            return Task.CompletedTask;
        }

        var dropped = Interlocked.Increment(ref _microphoneDropsQueueSaturated);
        if (dropped == 1 || dropped % 120 == 0)
        {
            Logger.Log(
                $"LIBAV_SINK_MIC_DROP saturated={dropped} evicted={Interlocked.Read(ref _microphoneDropsBacklogEviction)}");
        }

        return Task.CompletedTask;
    }

    private static void ReturnRemainingBuffers(Channel<AudioSamplePacket>? queue)
    {
        if (queue == null)
        {
            return;
        }

        while (queue.Reader.TryRead(out var packet))
        {
            ReturnBuffer(packet.Buffer);
        }
    }

    private static void ReturnRemainingBuffers(Channel<AudioSamplePacket>? queue, ref int queueDepth)
    {
        ReturnRemainingBuffers(queue);
        Interlocked.Exchange(ref queueDepth, 0);
    }

    private bool TryEnqueueAudioPacket(Channel<AudioSamplePacket> queue, AudioSamplePacket packet)
    {
        if (_cts?.IsCancellationRequested == true)
        {
            ReturnBuffer(packet.Buffer);
            return false;
        }

        if (TryWriteAudioPacket(queue, packet, ref _audioQueueDepth, "audio"))
        {
            SignalWork("audio_enqueue");
            return true;
        }

        if (queue.Reader.TryRead(out var evictedPacket))
        {
            DecrementQueueDepth(ref _audioQueueDepth, "audio_evict");
            var evicted = Interlocked.Increment(ref _audioDropsBacklogEviction);
            if (evicted == 1 || evicted % 120 == 0)
            {
                // Log evicted audio bytes so A/V drift from dropped audio is traceable.
                Logger.Log(
                    $"LIBAV_SINK_AUDIO_EVICT evicted={evicted} dropped_bytes={evictedPacket.Length} " +
                    $"queue_depth={Volatile.Read(ref _audioQueueDepth)}");
            }

            ReturnBuffer(evictedPacket.Buffer);
            if (TryWriteAudioPacket(queue, packet, ref _audioQueueDepth, "audio_after_evict"))
            {
                SignalWork("audio_after_evict");
                return true;
            }
        }

        ReturnBuffer(packet.Buffer);
        return false;
    }

    private bool TryEnqueueMicrophonePacket(Channel<AudioSamplePacket> queue, AudioSamplePacket packet)
    {
        if (_cts?.IsCancellationRequested == true)
        {
            ReturnBuffer(packet.Buffer);
            return false;
        }

        if (TryWriteAudioPacket(queue, packet, ref _microphoneQueueDepth, "microphone"))
        {
            SignalWork("microphone_enqueue");
            return true;
        }

        if (queue.Reader.TryRead(out var evictedPacket))
        {
            DecrementQueueDepth(ref _microphoneQueueDepth, "microphone_evict");
            var evicted = Interlocked.Increment(ref _microphoneDropsBacklogEviction);
            if (evicted == 1 || evicted % 120 == 0)
            {
                Logger.Log(
                    $"LIBAV_SINK_MIC_EVICT evicted={evicted} dropped_bytes={evictedPacket.Length} " +
                    $"queue_depth={Volatile.Read(ref _microphoneQueueDepth)}");
            }

            ReturnBuffer(evictedPacket.Buffer);
            if (TryWriteAudioPacket(queue, packet, ref _microphoneQueueDepth, "microphone_after_evict"))
            {
                SignalWork("microphone_after_evict");
                return true;
            }
        }

        ReturnBuffer(packet.Buffer);
        return false;
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

    private readonly record struct AudioSamplePacket(byte[] Buffer, int Length);
}
