using System;
using System.Threading;
using System.Threading.Tasks;

namespace ElgatoCapture.Services;

internal sealed class UnifiedVideoCapture : IAsyncDisposable
{
    private readonly object _sync = new();
    private MfSourceReaderVideoCapture? _capture;
    private SharedD3DDeviceManager? _d3dManager;
    private CancellationTokenSource? _readCts;
    private IPreviewFrameSink? _previewSink;
    private IRecordingSink? _recordingSink;
    private FFmpegEncoderService? _recordingEncoder;
    private bool _started;
    private bool _recordingActive;
    private bool _disposed;
    private bool _isP010;
    private int _width;
    private int _height;
    private double _fps;
    private string _negotiatedFormat = "unknown";
    private long _videoFramesArrived;
    private long _videoFramesDropped;
    private long _videoFramesWrittenToSink;
    private long _recordingFramesDelivered;
    private long _recordingFramesEnqueued;
    private long _lastVideoFrameArrivedTick;
    private Action<bool>? _observedPixelFormatObserver;

    public bool IsP010 => Volatile.Read(ref _isP010);
    public int Width => Volatile.Read(ref _width);
    public int Height => Volatile.Read(ref _height);
    public double Fps => Volatile.Read(ref _fps);
    public string NegotiatedFormat => Volatile.Read(ref _negotiatedFormat);
    public long VideoFramesArrived => Interlocked.Read(ref _videoFramesArrived);
    public long VideoFramesDropped
    {
        get
        {
            var captureDrops = _capture?.FramesDropped ?? 0;
            return Math.Max(captureDrops, Interlocked.Read(ref _videoFramesDropped));
        }
    }
    public long VideoFramesWrittenToSink => Interlocked.Read(ref _videoFramesWrittenToSink);
    public long RecordingFramesDelivered => Interlocked.Read(ref _recordingFramesDelivered);
    public long RecordingFramesEnqueued => Interlocked.Read(ref _recordingFramesEnqueued);
    public long LastVideoFrameArrivedTick => Interlocked.Read(ref _lastVideoFrameArrivedTick);
    public bool SourceReaderReadOutstanding => _capture?.IsReadSampleOutstanding ?? false;
    public long SourceReaderReadOutstandingMs => _capture?.ReadSampleOutstandingMs ?? 0;
    public long SourceReaderLastFrameTickMs => _capture?.LastFrameDeliveredTickMs ?? 0;
    public SharedD3DDeviceManager? D3DManager => Volatile.Read(ref _d3dManager);
    public MfSourceReaderVideoCapture.SourceCadenceMetrics GetSourceCadenceMetrics()
    {
        var capture = _capture;
        return capture?.GetSourceCadenceMetrics() ?? default;
    }

    public async Task InitializeAsync(
        string deviceSymbolicLink,
        int width,
        int height,
        double fps,
        bool requireP010)
    {
        ThrowIfDisposed();

        lock (_sync)
        {
            if (_capture != null)
            {
                throw new InvalidOperationException("Unified video capture is already initialized.");
            }
        }

        var d3dManager = new SharedD3DDeviceManager();
        var dxgiDeviceManagerPtr = d3dManager.DxgiDeviceManagerPtr;

        var capture = new MfSourceReaderVideoCapture();
        try
        {
            await capture.InitializeAsync(
                deviceSymbolicLink,
                width,
                height,
                fps,
                requireP010,
                dxgiDeviceManagerPtr).ConfigureAwait(false);
        }
        catch
        {
            d3dManager.Dispose();
            throw;
        }

        if (!capture.IsD3DOutputEnabled)
        {
            Logger.Log("UNIFIED_VIDEO_D3D_MANAGER_INACTIVE reason=source_reader_cpu_only");
        }

        lock (_sync)
        {
            _capture = capture;
            _d3dManager = d3dManager;
            _isP010 = capture.IsP010;
            _width = capture.Width;
            _height = capture.Height;
            _fps = capture.Fps;
            _negotiatedFormat = capture.NegotiatedFormat;
            Interlocked.Exchange(ref _videoFramesArrived, 0);
            Interlocked.Exchange(ref _videoFramesDropped, 0);
            Interlocked.Exchange(ref _videoFramesWrittenToSink, 0);
            Interlocked.Exchange(ref _lastVideoFrameArrivedTick, 0);
        }
    }

    public void Start()
    {
        ThrowIfDisposed();

        MfSourceReaderVideoCapture? capture;
        CancellationTokenSource readCts;

        lock (_sync)
        {
            capture = _capture ?? throw new InvalidOperationException("InitializeAsync must be called before Start.");
            if (_started)
            {
                throw new InvalidOperationException("Unified video capture read loop is already running.");
            }

            _started = true;
            readCts = new CancellationTokenSource();
            _readCts = readCts;
        }

        try
        {
            var useDualCallback = capture.IsD3DOutputEnabled && Volatile.Read(ref _d3dManager) != null;
            if (useDualCallback)
            {
                capture.StartReading(OnDualFrameArrived, readCts.Token);
            }
            else
            {
                capture.StartReading(OnFrameArrived, readCts.Token);
            }
        }
        catch
        {
            lock (_sync)
            {
                _started = false;
                _readCts = null;
            }

            readCts.Dispose();
            throw;
        }
    }

    public void SetPreviewSink(IPreviewFrameSink? sink)
    {
        Volatile.Write(ref _previewSink, sink);
    }

    public void SetObservedPixelFormatObserver(Action<bool>? observer)
    {
        Volatile.Write(ref _observedPixelFormatObserver, observer);
    }

    public Task StartRecordingAsync(IRecordingSink sink, FFmpegEncoderService encoder)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(encoder);

        lock (_sync)
        {
            if (_capture == null)
            {
                throw new InvalidOperationException("Cannot start recording before capture is initialized.");
            }

            _recordingSink = sink;
            _recordingEncoder = encoder;
            Interlocked.Exchange(ref _recordingFramesDelivered, 0);
            Interlocked.Exchange(ref _recordingFramesEnqueued, 0);
            _recordingActive = true;
        }

        return Task.CompletedTask;
    }

    public Task StopRecordingAsync()
    {
        lock (_sync)
        {
            _recordingActive = false;
            _recordingSink = null;
            _recordingEncoder = null;
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? readCts;
        MfSourceReaderVideoCapture? capture;

        lock (_sync)
        {
            _started = false;
            _recordingActive = false;
            _recordingSink = null;
            _recordingEncoder = null;
            readCts = _readCts;
            _readCts = null;
            capture = _capture;
        }

        readCts?.Cancel();
        if (capture != null)
        {
            await capture.StopAsync().ConfigureAwait(false);
            Volatile.Write(ref _negotiatedFormat, capture.NegotiatedFormat);
            _isP010 = capture.IsP010;
            _width = capture.Width;
            _height = capture.Height;
            _fps = capture.Fps;
            var captureDrops = capture.FramesDropped;
            var localDrops = Interlocked.Read(ref _videoFramesDropped);
            Interlocked.Exchange(ref _videoFramesDropped, Math.Max(captureDrops, localDrops));
        }

        readCts?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopAsync().ConfigureAwait(false);

        MfSourceReaderVideoCapture? capture;
        SharedD3DDeviceManager? d3dManager;
        lock (_sync)
        {
            capture = _capture;
            _capture = null;
            d3dManager = _d3dManager;
            _d3dManager = null;
            _previewSink = null;
            _observedPixelFormatObserver = null;
        }

        if (capture != null)
        {
            await capture.DisposeAsync().ConfigureAwait(false);
        }

        d3dManager?.Dispose();
    }

    private unsafe void OnFrameArrived(ReadOnlySpan<byte> frameData, int width, int height, long arrivalTick)
    {
        Interlocked.Increment(ref _videoFramesArrived);
        Interlocked.Exchange(ref _lastVideoFrameArrivedTick, Environment.TickCount64);

        var isP010 = Volatile.Read(ref _isP010);
        Volatile.Read(ref _observedPixelFormatObserver)?.Invoke(isP010);

        // Recording first keeps encoder delivery ahead of any preview-side lock contention.
        EnqueueRecordingFrame(frameData, width, height, isP010);

        var previewSink = Volatile.Read(ref _previewSink);
        if (previewSink != null && !frameData.IsEmpty)
        {
            SubmitPreviewRawFrame(previewSink, frameData, width, height, isP010, arrivalTick);
        }
    }

    private unsafe void OnDualFrameArrived(
        IntPtr gpuTexture,
        int gpuSubresource,
        ReadOnlySpan<byte> frameData,
        int width,
        int height,
        long arrivalTick)
    {
        Interlocked.Increment(ref _videoFramesArrived);
        Interlocked.Exchange(ref _lastVideoFrameArrivedTick, Environment.TickCount64);

        var isP010 = Volatile.Read(ref _isP010);
        Volatile.Read(ref _observedPixelFormatObserver)?.Invoke(isP010);

        // Recording first keeps encoder delivery ahead of any preview-side lock contention.
        EnqueueRecordingFrame(frameData, width, height, isP010);

        var previewSink = Volatile.Read(ref _previewSink);
        if (previewSink != null)
        {
            var textureSubmitted = false;
            if (gpuTexture != IntPtr.Zero)
            {
                try
                {
                    previewSink.SubmitTexture(gpuTexture, gpuSubresource, width, height, isP010, arrivalTick);
                    textureSubmitted = true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"UNIFIED_VIDEO_PREVIEW_TEXTURE_FAIL type={ex.GetType().Name} msg={ex.Message}");
                }
            }

            if (!textureSubmitted && !frameData.IsEmpty)
            {
                SubmitPreviewRawFrame(previewSink, frameData, width, height, isP010, arrivalTick);
            }
        }
    }

    private unsafe void SubmitPreviewRawFrame(
        IPreviewFrameSink previewSink,
        ReadOnlySpan<byte> frameData,
        int width,
        int height,
        bool isP010,
        long arrivalTick)
    {
        try
        {
            fixed (byte* pointer = frameData)
            {
                previewSink.SubmitRawFrame((IntPtr)pointer, frameData.Length, width, height, isP010, arrivalTick);
            }
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _videoFramesDropped);
            Logger.Log($"UNIFIED_VIDEO_PREVIEW_FRAME_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void EnqueueRecordingFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010)
    {
        FFmpegEncoderService? encoder = null;
        bool recordingActive;
        lock (_sync)
        {
            recordingActive = _recordingActive;
            if (recordingActive)
            {
                encoder = _recordingEncoder;
            }
        }

        if (!recordingActive || encoder == null)
        {
            return;
        }

        Interlocked.Increment(ref _recordingFramesDelivered);

        try
        {
            var expectedSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(width, height, isP010);
            if (frameData.Length < expectedSize)
            {
                Interlocked.Increment(ref _videoFramesDropped);
                Logger.Log(
                    "UNIFIED_VIDEO_FRAME_SIZE_MISMATCH " +
                    $"expected={expectedSize} actual={frameData.Length} width={width} height={height} isP010={isP010}");
                return;
            }

            encoder.EnqueueRawVideoFrame(frameData, expectedSize);
            Interlocked.Increment(ref _videoFramesWrittenToSink);
            Interlocked.Increment(ref _recordingFramesEnqueued);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _videoFramesDropped);
            Logger.Log($"UNIFIED_VIDEO_RECORDING_FRAME_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(UnifiedVideoCapture));
        }
    }
}
