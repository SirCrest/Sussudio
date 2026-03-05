using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ElgatoCapture.Services;

internal sealed class WasapiAudioPlayback : IDisposable
{
    private const int OutputChannels = 2;
    private const int BytesPerSample = 4;
    private const int OutputBlockAlign = OutputChannels * BytesPerSample;
    private const uint WaitTimeoutMs = 100;
    private const uint WaitObject0 = 0;
    private const uint WaitTimeout = 0x00000102;

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

    [DllImport("kernel32.dll")]
    private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

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

            audioClient = ActivateAudioClient(device, out audioClient3);
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

            if (!TryInitializeSharedStreamWithAudioClient3(audioClient3, desiredFormat))
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
            Interlocked.Exchange(ref _initialized, 1);
            Logger.Log("WASAPI playback initialized (f32le 48kHz stereo).");
            return Task.CompletedTask;
        }
        catch
        {
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
        catch
        {
            Interlocked.Exchange(ref _started, 0);
            throw;
        }
    }

    public void EnqueueSamples(ReadOnlySpan<byte> f32leSamples)
    {
        if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _initialized) == 0 || f32leSamples.IsEmpty)
        {
            return;
        }

        var frameAlignedLength = f32leSamples.Length - (f32leSamples.Length % OutputBlockAlign);
        if (frameAlignedLength <= 0)
        {
            return;
        }

        var rented = ArrayPool<byte>.Shared.Rent(frameAlignedLength);
        f32leSamples[..frameAlignedLength].CopyTo(rented);
        EnqueueChunk(new PlaybackChunk(rented, frameAlignedLength, isPooled: true));
    }

    internal void EnqueuePooledSamples(byte[] pooledBuffer, int validLength)
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

        EnqueueChunk(new PlaybackChunk(pooledBuffer, safeLength, isPooled: true));
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
            thread.Join();
        }

        ReturnActiveChunk();
        while (_sampleQueue.Reader.TryRead(out var queuedChunk))
        {
            ReturnChunk(queuedChunk);
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
        if (_sampleQueue.Writer.TryWrite(chunk))
        {
            return;
        }

        if (_sampleQueue.Reader.TryRead(out var droppedChunk))
        {
            ReturnChunk(droppedChunk);
        }

        if (!_sampleQueue.Writer.TryWrite(chunk))
        {
            ReturnChunk(chunk);
        }
    }

    private void RenderThreadMain()
    {
        while (Volatile.Read(ref _started) != 0)
        {
            var renderEvent = _renderEvent;
            if (renderEvent == null)
            {
                return;
            }

            var waitResult = WaitForSingleObject(renderEvent.SafeWaitHandle.DangerousGetHandle(), WaitTimeoutMs);
            if (waitResult == WaitTimeout)
            {
                continue;
            }

            if (waitResult != WaitObject0)
            {
                continue;
            }

            if (Volatile.Read(ref _started) == 0)
            {
                return;
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

        WasapiComInterop.ThrowIfFailed(
            _audioClient.GetCurrentPadding(out var paddingFrames),
            "IAudioClient.GetCurrentPadding(render)");

        if (paddingFrames >= _bufferFrameCount)
        {
            return;
        }

        var framesToWrite = _bufferFrameCount - paddingFrames;
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
            FillRenderBuffer(destinationSpan);
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
                if (!_sampleQueue.Reader.TryRead(out _activeChunk))
                {
                    destination[written..].Clear();
                    return;
                }

                _activeChunkOffset = 0;
                _hasActiveChunk = true;
            }

            var activeBuffer = _activeChunk.Buffer;
            if (activeBuffer == null)
            {
                destination[written..].Clear();
                ReturnActiveChunk();
                return;
            }

            var available = _activeChunk.Length - _activeChunkOffset;
            var copyLength = Math.Min(destination.Length - written, available);
            activeBuffer.AsSpan(_activeChunkOffset, copyLength).CopyTo(destination[written..]);
            _activeChunkOffset += copyLength;
            written += copyLength;
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

    private static IAudioClient ActivateAudioClient(IMMDevice device, out IAudioClient3? audioClient3)
    {
        var iidAudioClient3 = typeof(IAudioClient3).GUID;
        var hr = device.Activate(ref iidAudioClient3, WasapiComInterop.CLSCTX_ALL, IntPtr.Zero, out var client3Object);
        if (hr >= 0 && client3Object is IAudioClient3 client3)
        {
            audioClient3 = client3;
            return client3;
        }

        var iidAudioClient = typeof(IAudioClient).GUID;
        WasapiComInterop.ThrowIfFailed(
            device.Activate(ref iidAudioClient, WasapiComInterop.CLSCTX_ALL, IntPtr.Zero, out var clientObject),
            "IMMDevice.Activate(IAudioClient)");

        audioClient3 = clientObject as IAudioClient3;
        return (IAudioClient)clientObject;
    }

    private static bool TryInitializeSharedStreamWithAudioClient3(IAudioClient3? audioClient3, IntPtr format)
    {
        if (audioClient3 == null)
        {
            return false;
        }

        var hr = audioClient3.GetSharedModeEnginePeriod(
            format,
            out var defaultPeriodInFrames,
            out _,
            out _,
            out _);
        if (hr < 0)
        {
            return false;
        }

        hr = audioClient3.InitializeSharedAudioStream(
            WasapiComInterop.AUDCLNT_STREAMFLAGS_EVENTCALLBACK,
            defaultPeriodInFrames,
            format,
            IntPtr.Zero);
        return hr >= 0;
    }

    private readonly struct PlaybackChunk
    {
        public PlaybackChunk(byte[] buffer, int length, bool isPooled)
        {
            Buffer = buffer;
            Length = length;
            IsPooled = isPooled;
        }

        public byte[]? Buffer { get; }

        public int Length { get; }

        public bool IsPooled { get; }
    }
}
