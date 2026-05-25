using System;
using System.Buffers;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Sussudio.Services.Audio;

// Low-latency monitoring renderer for live capture and Flashback playback.
// Producers enqueue normalized f32le 48 kHz stereo chunks; a single WASAPI
// render thread applies volume ramps, tracks queue depth, and writes to the
// default output endpoint.
internal sealed partial class WasapiAudioPlayback : IDisposable
{
    private const int OutputChannels = 2;
    private const int BytesPerSample = 4;
    private const int OutputBlockAlign = OutputChannels * BytesPerSample;
    private const int OutputSampleRate = 48000;
    private const uint MaxRenderWriteFrames = OutputSampleRate / 50; // 20ms
    private const uint WaitTimeoutMs = 100;

    private IMMDeviceEnumerator? _deviceEnumerator;
    private IMMDevice? _device;
    private IAudioClient? _audioClient;
    private IAudioClient3? _audioClient3;
    private IAudioRenderClient? _audioRenderClient;
    private AutoResetEvent? _renderEvent;
    private Thread? _renderThread;
    private uint _bufferFrameCount;
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
    private int _initialized;
    private int _started;
    private int _disposed;
    private int _renderingPaused; // 0 = active, 1 = paused
    private volatile bool _pauseRequested;
    private volatile bool _resumeRequested;
    private int _resumePrebufferFrames;
    private int _resumePrebufferTimeoutMs;
    private int _playbackQueueDropCount;
    private int _playbackQueueDepth;
    private int _playbackQueueFrames;
    private int _activeChunkRemainingFrames;
    private int _endpointQueuedFrames;
    private long _streamLatencyHundredNs;

    // Flashback seeks can restart audio mid-timeline. Resume prebuffering gives
    // the playback thread enough audio to avoid a dry render callback while
    // video cadence is being re-established.
    private long _renderCallbackCount;
    private int _renderSilenceCount;
    private long _lastRenderCallbackTickMs;
    private long _renderingPtsTicks; // PTS of chunk currently being rendered

    public int PlaybackQueueDepth => Math.Max(0, Volatile.Read(ref _playbackQueueDepth));

    public int PlaybackQueueDropCount => Volatile.Read(ref _playbackQueueDropCount);

    public double PlaybackQueueDurationMs => FramesToMilliseconds(Volatile.Read(ref _playbackQueueFrames));

    public double PlaybackActiveChunkDurationMs => FramesToMilliseconds(Volatile.Read(ref _activeChunkRemainingFrames));

    public double PlaybackEndpointQueuedDurationMs => FramesToMilliseconds(Volatile.Read(ref _endpointQueuedFrames));

    public double PlaybackStreamLatencyMs => Interlocked.Read(ref _streamLatencyHundredNs) / 10_000.0;

    public double PlaybackBufferedDurationMs =>
        PlaybackQueueDurationMs + PlaybackActiveChunkDurationMs + PlaybackEndpointQueuedDurationMs;

    public long RenderCallbackCount => Interlocked.Read(ref _renderCallbackCount);

    public int RenderSilenceCount => Volatile.Read(ref _renderSilenceCount);

    public long LastRenderCallbackTickMs => Interlocked.Read(ref _lastRenderCallbackTickMs);

    /// <summary>PTS (in TimeSpan ticks) of the audio chunk currently being rendered.</summary>
    public long RenderingPtsTicks => Interlocked.Read(ref _renderingPtsTicks);

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

    public Task InitializeAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Volatile.Read(ref _initialized) != 0)
        {
            return Task.CompletedTask;
        }

        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;
        IAudioClient? audioClient = null;
        IAudioClient3? audioClient3 = null;
        IAudioRenderClient? audioRenderClient = null;
        AutoResetEvent? renderEvent = null;
        IntPtr desiredFormat = IntPtr.Zero;

        try
        {
            enumerator = WasapiComInterop.CreateDeviceEnumerator();
            WasapiComInterop.ThrowIfFailed(
                enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, out device),
                "IMMDeviceEnumerator.GetDefaultAudioEndpoint");

            audioClient = WasapiComInterop.ActivateAudioClient(device, out audioClient3);
            desiredFormat = WasapiComInterop.AllocFloatStereo48kFormat();

            var hr = audioClient.IsFormatSupported(
                WasapiComInterop.AUDCLNT_SHAREMODE_SHARED,
                desiredFormat,
                out var closestMatch);
            if (closestMatch != IntPtr.Zero)
            {
                WasapiComInterop.CoTaskMemFree(closestMatch);
            }

            if (hr != WasapiComInterop.S_OK)
            {
                throw new InvalidOperationException(
                    "Default render endpoint does not support f32le 48kHz stereo monitoring playback.");
            }

            if (!WasapiComInterop.TryInitializeSharedStreamWithAudioClient3(audioClient3, desiredFormat))
            {
                WasapiComInterop.ThrowIfFailed(
                    audioClient.Initialize(
                        WasapiComInterop.AUDCLNT_SHAREMODE_SHARED,
                        WasapiComInterop.AUDCLNT_STREAMFLAGS_EVENTCALLBACK,
                        0,
                        0,
                        desiredFormat,
                        IntPtr.Zero),
                    "IAudioClient.Initialize(render)");
            }

            WasapiComInterop.ThrowIfFailed(
                audioClient.GetBufferSize(out _bufferFrameCount),
                "IAudioClient.GetBufferSize(render)");
            if (audioClient.GetStreamLatency(out var streamLatencyHundredNs) >= 0)
            {
                Interlocked.Exchange(ref _streamLatencyHundredNs, streamLatencyHundredNs);
            }

            renderEvent = new AutoResetEvent(false);
            WasapiComInterop.ThrowIfFailed(
                audioClient.SetEventHandle(renderEvent.SafeWaitHandle.DangerousGetHandle()),
                "IAudioClient.SetEventHandle(render)");

            var iidRenderClient = WasapiComInterop.IID_IAudioRenderClient;
            WasapiComInterop.ThrowIfFailed(
                audioClient.GetService(ref iidRenderClient, out var renderClientObject),
                "IAudioClient.GetService(IAudioRenderClient)");

            audioRenderClient = (IAudioRenderClient)renderClientObject;
            _deviceEnumerator = enumerator;
            _device = device;
            _audioClient = audioClient;
            _audioClient3 = audioClient3;
            _audioRenderClient = audioRenderClient;
            _renderEvent = renderEvent;
            Interlocked.Exchange(ref _renderCallbackCount, 0);
            Volatile.Write(ref _renderSilenceCount, 0);
            Volatile.Write(ref _playbackQueueDropCount, 0);
            Volatile.Write(ref _playbackQueueDepth, 0);
            Volatile.Write(ref _playbackQueueFrames, 0);
            Volatile.Write(ref _activeChunkRemainingFrames, 0);
            Volatile.Write(ref _endpointQueuedFrames, 0);
            Interlocked.Exchange(ref _lastRenderCallbackTickMs, 0);
            Interlocked.Exchange(ref _initialized, 1);
            Logger.Log("WASAPI playback initialized (f32le 48kHz stereo).");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in WasapiAudioPlayback.InitializeAsync: {ex.Message}");
            renderEvent?.Dispose();
            WasapiComInterop.ReleaseComObject(ref audioRenderClient);
            WasapiComInterop.ReleaseComObject(ref audioClient3);
            WasapiComInterop.ReleaseComObject(ref audioClient);
            WasapiComInterop.ReleaseComObject(ref device);
            WasapiComInterop.ReleaseComObject(ref enumerator);
            throw;
        }
        finally
        {
            if (desiredFormat != IntPtr.Zero)
            {
                WasapiComInterop.CoTaskMemFree(desiredFormat);
            }
        }
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Volatile.Read(ref _initialized) == 0)
        {
            throw new InvalidOperationException("WASAPI playback must be initialized before start.");
        }

        if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
        {
            return;
        }

        try
        {
            _currentVolume = _targetVolume;
            WasapiComInterop.ThrowIfFailed(_audioClient!.Start(), "IAudioClient.Start(render)");

            _renderThread = new Thread(RenderThreadMain)
            {
                IsBackground = true,
                Name = "WASAPI Render",
                Priority = ThreadPriority.AboveNormal
            };
            _renderThread.Start();
            Logger.Log("WASAPI playback started.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in WasapiAudioPlayback.Start: {ex.Message}");
            Interlocked.Exchange(ref _started, 0);
            throw;
        }
    }

    public void PauseRendering()
    {
        if (Volatile.Read(ref _started) == 0) return;
        if (Volatile.Read(ref _renderingPaused) != 0 && !_resumeRequested) return;

        _resumeRequested = false;
        _pauseRequested = true;
        _renderEvent?.Set();
    }

    public void ResumeRendering(double prebufferMs = 0, int prebufferTimeoutMs = 0)
    {
        if (Volatile.Read(ref _started) == 0) return;
        if (Volatile.Read(ref _renderingPaused) == 0 && !_pauseRequested) return;

        var prebufferFrames = prebufferMs > 0
            ? (int)Math.Ceiling(prebufferMs * OutputSampleRate / 1000.0)
            : 0;
        Volatile.Write(ref _resumePrebufferFrames, Math.Max(0, prebufferFrames));
        Volatile.Write(ref _resumePrebufferTimeoutMs, Math.Max(0, prebufferTimeoutMs));
        _resumeRequested = true;
        _renderEvent?.Set();
    }

    /// <summary>
    /// Drains all queued audio chunks and resets the active chunk state.
    /// Call this when transitioning between live and playback audio to prevent
    /// stale samples from bleeding across the handoff boundary.
    /// </summary>
    public void Flush()
    {
        lock (_chunkLock)
        {
            ReturnActiveChunk();
            while (TryDequeueChunk(out var queuedChunk))
            {
                ReturnChunk(queuedChunk);
            }
            _activeChunkOffset = 0;
            Volatile.Write(ref _activeChunkRemainingFrames, 0);
            Volatile.Write(ref _endpointQueuedFrames, 0);
        }
        Interlocked.Exchange(ref _renderingPtsTicks, 0);
    }

    public void Stop()
    {
        if (Interlocked.CompareExchange(ref _started, 0, 1) != 1)
        {
            return;
        }

        try
        {
            _renderEvent?.Set();
            _audioClient?.Stop();
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI playback stop warning: {ex.Message}");
        }

        var thread = _renderThread;
        _renderThread = null;
        if (thread != null && thread.IsAlive)
        {
            if (!thread.Join(TimeSpan.FromSeconds(3)))
            {
                Logger.Log("WASAPI_PLAYBACK_THREAD_JOIN_TIMEOUT");
            }
        }

        lock (_chunkLock)
        {
            ReturnActiveChunk();
            while (TryDequeueChunk(out var queuedChunk))
            {
                ReturnChunk(queuedChunk);
            }
        }

        Logger.Log("WASAPI playback stopped.");
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        Stop();
        _renderEvent?.Dispose();
        _renderEvent = null;
        WasapiComInterop.ReleaseComObject(ref _audioRenderClient);
        WasapiComInterop.ReleaseComObject(ref _audioClient3);
        WasapiComInterop.ReleaseComObject(ref _audioClient);
        WasapiComInterop.ReleaseComObject(ref _device);
        WasapiComInterop.ReleaseComObject(ref _deviceEnumerator);
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
