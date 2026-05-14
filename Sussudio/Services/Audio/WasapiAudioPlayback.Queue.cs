using System;
using System.Buffers;
using System.Threading;
using System.Threading.Channels;

namespace Sussudio.Services.Audio;

internal sealed partial class WasapiAudioPlayback
{
    private readonly object _chunkLock = new();
    private readonly Channel<PlaybackChunk> _sampleQueue = Channel.CreateBounded<PlaybackChunk>(
        new BoundedChannelOptions(128)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    private PlaybackChunk _activeChunk;
    private int _activeChunkOffset;
    private bool _hasActiveChunk;
    private int _playbackQueueDropCount;
    private int _playbackQueueDepth;
    private int _playbackQueueFrames;
    private int _activeChunkRemainingFrames;
    private int _endpointQueuedFrames;
    private long _streamLatencyHundredNs;

    public int PlaybackQueueDepth => Math.Max(0, Volatile.Read(ref _playbackQueueDepth));

    public int PlaybackQueueDropCount => Volatile.Read(ref _playbackQueueDropCount);

    public double PlaybackQueueDurationMs => FramesToMilliseconds(Volatile.Read(ref _playbackQueueFrames));

    public double PlaybackActiveChunkDurationMs => FramesToMilliseconds(Volatile.Read(ref _activeChunkRemainingFrames));

    public double PlaybackEndpointQueuedDurationMs => FramesToMilliseconds(Volatile.Read(ref _endpointQueuedFrames));

    public double PlaybackStreamLatencyMs => Interlocked.Read(ref _streamLatencyHundredNs) / 10_000.0;

    public double PlaybackBufferedDurationMs =>
        PlaybackQueueDurationMs + PlaybackActiveChunkDurationMs + PlaybackEndpointQueuedDurationMs;

    internal void EnqueuePooledSamples(byte[] pooledBuffer, int validLength, long ptsTicks = 0)
    {
        if (pooledBuffer == null)
        {
            return;
        }

        if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _initialized) == 0 || validLength <= 0)
        {
            ArrayPool<byte>.Shared.Return(pooledBuffer);
            return;
        }

        var safeLength = Math.Min(validLength, pooledBuffer.Length);
        safeLength -= safeLength % OutputBlockAlign;
        if (safeLength <= 0)
        {
            ArrayPool<byte>.Shared.Return(pooledBuffer);
            return;
        }

        EnqueueChunk(new PlaybackChunk(pooledBuffer, safeLength, IsPooled: true, PtsTicks: ptsTicks));
    }

    private void EnqueueChunk(PlaybackChunk chunk)
    {
        if (TryWriteChunk(chunk)) return;

        // Queue full - evict oldest chunk to make room for the new one.
        // The evicted chunk is the real drop; the new chunk replaces it.
        if (TryDequeueChunk(out var droppedChunk))
        {
            ReturnChunk(droppedChunk);
            if (TryWriteChunk(chunk))
            {
                Interlocked.Increment(ref _playbackQueueDropCount);
                return;
            }
        }

        // Both eviction and re-enqueue failed - drop the new chunk.
        Interlocked.Increment(ref _playbackQueueDropCount);
        ReturnChunk(chunk);
    }

    private bool TryWriteChunk(PlaybackChunk chunk)
    {
        Interlocked.Increment(ref _playbackQueueDepth);
        if (_sampleQueue.Writer.TryWrite(chunk))
        {
            Interlocked.Add(ref _playbackQueueFrames, GetFrameCount(chunk));
            return true;
        }

        DecrementPlaybackQueueDepth();
        return false;
    }

    private bool TryDequeueChunk(out PlaybackChunk chunk)
    {
        if (_sampleQueue.Reader.TryRead(out chunk))
        {
            DecrementPlaybackQueueDepth();
            DecrementPlaybackQueueFrames(GetFrameCount(chunk));
            return true;
        }

        return false;
    }

    private void UpdateActiveChunkRemainingFrames()
    {
        if (!_hasActiveChunk)
        {
            Volatile.Write(ref _activeChunkRemainingFrames, 0);
            return;
        }

        var remainingBytes = Math.Max(0, _activeChunk.Length - _activeChunkOffset);
        Volatile.Write(ref _activeChunkRemainingFrames, remainingBytes / OutputBlockAlign);
    }

    private static int GetFrameCount(PlaybackChunk chunk) => Math.Max(0, chunk.Length) / OutputBlockAlign;

    private static double FramesToMilliseconds(int frames) =>
        frames <= 0 ? 0 : frames * 1000.0 / OutputSampleRate;

    private void DecrementPlaybackQueueDepth()
    {
        while (true)
        {
            var current = Volatile.Read(ref _playbackQueueDepth);
            if (current <= 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _playbackQueueDepth, current - 1, current) == current)
            {
                return;
            }
        }
    }

    private void DecrementPlaybackQueueFrames(int frames)
    {
        if (frames <= 0)
        {
            return;
        }

        while (true)
        {
            var current = Volatile.Read(ref _playbackQueueFrames);
            if (current <= 0)
            {
                return;
            }

            var next = Math.Max(0, current - frames);
            if (Interlocked.CompareExchange(ref _playbackQueueFrames, next, current) == current)
            {
                return;
            }
        }
    }

    private void ReturnActiveChunk()
    {
        if (!_hasActiveChunk)
        {
            return;
        }

        ReturnChunk(_activeChunk);
        _activeChunk = default;
        _activeChunkOffset = 0;
        _hasActiveChunk = false;
    }

    private static void ReturnChunk(PlaybackChunk chunk)
    {
        if (!chunk.IsPooled || chunk.Buffer == null)
        {
            return;
        }

        ArrayPool<byte>.Shared.Return(chunk.Buffer);
    }

    private readonly record struct PlaybackChunk(byte[]? Buffer, int Length, bool IsPooled, long PtsTicks = 0);
}
