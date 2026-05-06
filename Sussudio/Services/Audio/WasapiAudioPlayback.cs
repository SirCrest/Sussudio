using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sussudio.Services.Devices;
using Sussudio.Services.Recording;
using Sussudio.Services.Telemetry;

namespace Sussudio.Services.Audio;

internal sealed class WasapiAudioPlayback : IDisposable
{
    private const int OutputChannels = 2;
    private const int BytesPerSample = 4;
    private const int OutputBlockAlign = OutputChannels * BytesPerSample;
    private const int OutputSampleRate = 48000;
    private const uint MaxRenderWriteFrames = OutputSampleRate / 50; // 20ms
    private const uint WaitTimeoutMs = 100;

    private readonly object _chunkLock = new();
    private readonly Channel<PlaybackChunk> _sampleQueue = Channel.CreateBounded<PlaybackChunk>(
        new BoundedChannelOptions(128)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    private IMMDeviceEnumerator? _deviceEnumerator;
    private IMMDevice? _device;
    private IAudioClient? _audioClient;
    private IAudioClient3? _audioClient3;
    private IAudioRenderClient? _audioRenderClient;
    private AutoResetEvent? _renderEvent;
    private Thread? _renderThread;
    private uint _bufferFrameCount;
    private PlaybackChunk _activeChunk;
    private int _activeChunkOffset;
    private bool _hasActiveChunk;
    private int _initialized;
    private int _started;
    private int _disposed;
    private int _renderingPaused; // 0 = active, 1 = paused
    private volatile bool _pauseRequested;
    private volatile bool _resumeRequested;
    private volatile float _targetVolume = 1.0f;
    private float _currentVolume;
    private volatile float _lastOutputPeak;
    private volatile float _lastOutputRms;
    private long _lastOutputLevelTickMs;
    private const float VolumeRampPerFrame = 1.0f / (0.3f * OutputSampleRate); // 300ms ramp at 48kHz
    private long _renderCallbackCount;
    private int _renderSilenceCount;
    private int _playbackQueueDropCount;
    private int _playbackQueueDepth;
    private int _playbackQueueFrames;
    private int _activeChunkRemainingFrames;
    private int _endpointQueuedFrames;
    private long _lastRenderCallbackTickMs;
    private long _renderingPtsTicks; // PTS of chunk currently being rendered
    private long _streamLatencyHundredNs;

    public long RenderCallbackCount => Interlocked.Read(ref _renderCallbackCount);

    public int RenderSilenceCount => Volatile.Read(ref _renderSilenceCount);

    public int PlaybackQueueDepth => Math.Max(0, Volatile.Read(ref _playbackQueueDepth));

    public int PlaybackQueueDropCount => Volatile.Read(ref _playbackQueueDropCount);

    public double PlaybackQueueDurationMs => FramesToMilliseconds(Volatile.Read(ref _playbackQueueFrames));

    public double PlaybackActiveChunkDurationMs => FramesToMilliseconds(Volatile.Read(ref _activeChunkRemainingFrames));

    public double PlaybackEndpointQueuedDurationMs => FramesToMilliseconds(Volatile.Read(ref _endpointQueuedFrames));

    public double PlaybackStreamLatencyMs => Interlocked.Read(ref _streamLatencyHundredNs) / 10_000.0;

    public double PlaybackBufferedDurationMs =>
        PlaybackQueueDurationMs + PlaybackActiveChunkDurationMs + PlaybackEndpointQueuedDurationMs;

    public float TargetVolume => _targetVolume;

    public float CurrentVolume => _currentVolume;

    public float LastOutputPeak => _lastOutputPeak;

    public float LastOutputRms => _lastOutputRms;

    public long LastOutputLevelTickMs => Interlocked.Read(ref _lastOutputLevelTickMs);

    public long LastRenderCallbackTickMs => Interlocked.Read(ref _lastRenderCallbackTickMs);

    /// <summary>PTS (in TimeSpan ticks) of the audio chunk currently being rendered.</summary>
    public long RenderingPtsTicks => Interlocked.Read(ref _renderingPtsTicks);

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

    public void SetVolume(float volume)
    {
        _targetVolume = Math.Clamp(volume, 0f, 1f);
    }

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

    public void PauseRendering()
    {
        if (Volatile.Read(ref _started) == 0) return;
        if (Volatile.Read(ref _renderingPaused) != 0 && !_resumeRequested) return;

        _resumeRequested = false;
        _pauseRequested = true;
        _renderEvent?.Set();
    }

    public void ResumeRendering()
    {
        if (Volatile.Read(ref _started) == 0) return;
        if (Volatile.Read(ref _renderingPaused) == 0 && !_pauseRequested) return;

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

        // Queue full — evict oldest chunk to make room for the new one.
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

        // Both eviction and re-enqueue failed — drop the new chunk
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

    private void RenderThreadMain()
    {
        var renderEvent = _renderEvent;
        if (renderEvent == null)
        {
            return;
        }

        var waitHandle = renderEvent.SafeWaitHandle.DangerousGetHandle();
        while (Volatile.Read(ref _started) != 0)
        {
            var waitResult = WasapiComInterop.WaitForSingleObject(waitHandle, WaitTimeoutMs);
            if (waitResult == WasapiComInterop.WaitTimeout)
            {
                continue;
            }

            if (waitResult != WasapiComInterop.WaitObject0)
            {
                continue;
            }

            if (Volatile.Read(ref _started) == 0)
            {
                return;
            }

            // Handle pause request on the render thread to avoid cross-thread WASAPI calls
            if (_pauseRequested)
            {
                _pauseRequested = false;
                try
                {
                    _audioClient?.Stop();
                    _audioClient?.Reset();
                }
                catch (Exception ex)
                {
                    Logger.Log($"WASAPI_PAUSE_RENDER_WARN: {ex.Message}");
                }
                Flush();
                Interlocked.Exchange(ref _renderingPaused, 1);
                Logger.Log("WASAPI_PLAYBACK_RENDER_PAUSED");
                if (!_resumeRequested)
                {
                    continue;
                }
            }

            // Handle resume request on the render thread to avoid cross-thread WASAPI calls
            if (_resumeRequested)
            {
                _resumeRequested = false;
                if (Volatile.Read(ref _renderingPaused) == 0)
                {
                    Logger.Log("WASAPI_PLAYBACK_RENDER_RESUME_CANCELED_PENDING_PAUSE");
                    continue;
                }

                try { _audioClient?.Start(); }
                catch (Exception ex) { Logger.Log($"WASAPI_RESUME_RENDER_WARN: {ex.Message}"); }
                Interlocked.Exchange(ref _renderingPaused, 0);
                Logger.Log("WASAPI_PLAYBACK_RENDER_RESUMED");
                continue;
            }

            try
            {
                RenderAvailableFrames();
            }
            catch (Exception ex)
            {
                Logger.Log($"WASAPI playback render error: {ex.Message}");
            }
        }
    }

    private unsafe void RenderAvailableFrames()
    {
        if (_audioClient == null || _audioRenderClient == null || _bufferFrameCount == 0)
        {
            return;
        }

        if (Volatile.Read(ref _renderingPaused) != 0) return;

        Interlocked.Increment(ref _renderCallbackCount);
        Interlocked.Exchange(ref _lastRenderCallbackTickMs, Environment.TickCount64);

        WasapiComInterop.ThrowIfFailed(
            _audioClient.GetCurrentPadding(out var paddingFrames),
            "IAudioClient.GetCurrentPadding(render)");

        if (paddingFrames >= _bufferFrameCount)
        {
            return;
        }

        var framesToWrite = Math.Min(_bufferFrameCount - paddingFrames, MaxRenderWriteFrames);
        if (framesToWrite == 0)
        {
            return;
        }

        WasapiComInterop.ThrowIfFailed(
            _audioRenderClient.GetBuffer(framesToWrite, out var destination),
            "IAudioRenderClient.GetBuffer");

        try
        {
            var bytesToWrite = checked((int)framesToWrite * OutputBlockAlign);
            var destinationSpan = new Span<byte>((void*)destination, bytesToWrite);
            lock (_chunkLock)
            {
                FillRenderBuffer(destinationSpan);
            }
            ApplyVolume(destinationSpan);
            UpdateOutputLevel(destinationSpan);
            Volatile.Write(ref _endpointQueuedFrames, checked((int)Math.Min(int.MaxValue, paddingFrames + framesToWrite)));
        }
        finally
        {
            WasapiComInterop.ThrowIfFailed(
                _audioRenderClient.ReleaseBuffer(framesToWrite, 0),
                "IAudioRenderClient.ReleaseBuffer");
        }
    }

    private void FillRenderBuffer(Span<byte> destination)
    {
        var written = 0;
        while (written < destination.Length)
        {
            if (!_hasActiveChunk || _activeChunkOffset >= _activeChunk.Length)
            {
                ReturnActiveChunk();
                if (!TryDequeueChunk(out _activeChunk))
                {
                    Interlocked.Increment(ref _renderSilenceCount);
                    Volatile.Write(ref _activeChunkRemainingFrames, 0);
                    destination[written..].Clear();
                    return;
                }

                _activeChunkOffset = 0;
                _hasActiveChunk = true;
                if (_activeChunk.PtsTicks != 0)
                    Interlocked.Exchange(ref _renderingPtsTicks, _activeChunk.PtsTicks);
            }

            var activeBuffer = _activeChunk.Buffer;
            if (activeBuffer == null)
            {
                destination[written..].Clear();
                ReturnActiveChunk();
                Volatile.Write(ref _activeChunkRemainingFrames, 0);
                return;
            }

            var available = _activeChunk.Length - _activeChunkOffset;
            var copyLength = Math.Min(destination.Length - written, available);
            activeBuffer.AsSpan(_activeChunkOffset, copyLength).CopyTo(destination[written..]);
            _activeChunkOffset += copyLength;
            UpdateRenderingPtsForActiveChunk();
            UpdateActiveChunkRemainingFrames();
            written += copyLength;
        }
    }

    private void UpdateRenderingPtsForActiveChunk()
    {
        if (_activeChunk.PtsTicks == 0) return;

        var frameOffset = Math.Max(0, _activeChunkOffset) / OutputBlockAlign;
        var offsetTicks = frameOffset * TimeSpan.TicksPerSecond / OutputSampleRate;
        Interlocked.Exchange(ref _renderingPtsTicks, _activeChunk.PtsTicks + offsetTicks);
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

    private void ApplyVolume(Span<byte> buffer)
    {
        var floats = MemoryMarshal.Cast<byte, float>(buffer);
        var target = _targetVolume;

        // Fast path: already at target volume of 1.0
        if (_currentVolume >= 1.0f && target >= 1.0f) return;

        // Fast path: already at target, no ramp needed
        if (MathF.Abs(_currentVolume - target) < 0.0001f)
        {
            if (target < 0.0001f)
            {
                floats.Clear();
                return;
            }

            // Constant non-unity volume
            for (var i = 0; i < floats.Length; i++)
            {
                floats[i] *= _currentVolume;
            }
            return;
        }

        // Ramp toward target volume
        for (var i = 0; i < floats.Length; i += OutputChannels)
        {
            // Step current toward target
            if (_currentVolume < target)
            {
                _currentVolume = MathF.Min(_currentVolume + VolumeRampPerFrame, target);
            }
            else if (_currentVolume > target)
            {
                _currentVolume = MathF.Max(_currentVolume - VolumeRampPerFrame, target);
            }

            for (var ch = 0; ch < OutputChannels && i + ch < floats.Length; ch++)
            {
                floats[i + ch] *= _currentVolume;
            }

            // Once settled, apply rest at constant volume
            if (MathF.Abs(_currentVolume - target) < 0.0001f)
            {
                _currentVolume = target;
                if (target >= 1.0f) return; // rest is at unity, no scaling needed
                // Apply constant volume to remaining samples
                for (var j = i + OutputChannels; j < floats.Length; j++)
                {
                    floats[j] *= _currentVolume;
                }
                return;
            }
        }
    }

    private void UpdateOutputLevel(ReadOnlySpan<byte> buffer)
    {
        var floats = MemoryMarshal.Cast<byte, float>(buffer);
        if (floats.Length == 0)
        {
            _lastOutputPeak = 0;
            _lastOutputRms = 0;
            Interlocked.Exchange(ref _lastOutputLevelTickMs, Environment.TickCount64);
            return;
        }

        var peak = 0f;
        var sumSquares = 0.0;
        for (var i = 0; i < floats.Length; i++)
        {
            var sample = floats[i];
            var abs = MathF.Abs(sample);
            if (abs > peak)
            {
                peak = abs;
            }

            sumSquares += sample * sample;
        }

        _lastOutputPeak = peak;
        _lastOutputRms = (float)Math.Sqrt(sumSquares / floats.Length);
        Interlocked.Exchange(ref _lastOutputLevelTickMs, Environment.TickCount64);
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
