using System;
using System.Diagnostics;
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
    private IRawVideoFrameEncoder? _recordingEncoder;
    private IGpuVideoFrameEncoder? _gpuRecordingEncoder;
    private ParallelMjpegDecodePipeline? _mjpegPipeline;
    private bool _started;
    private bool _recordingActive;
    private bool _disposed;
    private bool _isP010;
    private bool _isHighFrameRateMjpegMode;
    private bool _strictPreviewTextureRequired;
    private int _fatalErrorSignaled;
    private int _width;
    private int _height;
    private double _fps;
    private string _nativeInputFormat = "unknown";
    private string _negotiatedFormat = "unknown";
    private long _videoFramesArrived;
    private long _videoFramesDropped;
    private long _videoFramesWrittenToSink;
    private long _recordingFramesDelivered;
    private long _recordingFramesEnqueued;
    private long _lastVideoFrameArrivedTick;
    private Action<string>? _observedPixelFormatObserver;

    public readonly record struct MjpegPipelineTimingMetrics(
        int DecodeSampleCount,
        double DecodeAvgMs,
        double DecodeP95Ms,
        double DecodeMaxMs,
        int InteropCopySampleCount,
        double InteropCopyAvgMs,
        double InteropCopyP95Ms,
        double InteropCopyMaxMs,
        int CallbackSampleCount,
        double CallbackAvgMs,
        double CallbackP95Ms,
        double CallbackMaxMs);

    public bool IsP010 => Volatile.Read(ref _isP010);
    public int Width => Volatile.Read(ref _width);
    public int Height => Volatile.Read(ref _height);
    public double Fps => Volatile.Read(ref _fps);
    public bool IsHighFrameRateMjpegMode => Volatile.Read(ref _isHighFrameRateMjpegMode);
    public bool IsSoftwareMjpegPipelineActive => Volatile.Read(ref _mjpegPipeline) != null;
    public string NativeInputFormat => Volatile.Read(ref _nativeInputFormat);
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
    public event EventHandler<Exception>? FatalErrorOccurred;
    public bool SourceReaderReadOutstanding => _capture?.IsReadSampleOutstanding ?? false;
    public long SourceReaderReadOutstandingMs => _capture?.ReadSampleOutstandingMs ?? 0;
    public long SourceReaderLastFrameTickMs => _capture?.LastFrameDeliveredTickMs ?? 0;
    public SharedD3DDeviceManager? D3DManager => Volatile.Read(ref _d3dManager);

    public MfSourceReaderVideoCapture.SourceCadenceMetrics GetSourceCadenceMetrics()
    {
        var capture = _capture;
        return capture?.GetSourceCadenceMetrics() ?? default;
    }

    public MjpegPipelineTimingMetrics GetMjpegPipelineTimingMetrics()
    {
        var pipeline = Volatile.Read(ref _mjpegPipeline);
        if (pipeline == null)
        {
            return default;
        }

        var metrics = pipeline.GetTimingMetrics();
        return new MjpegPipelineTimingMetrics(
            DecodeSampleCount: metrics.DecodeSampleCount,
            DecodeAvgMs: metrics.DecodeAvgMs,
            DecodeP95Ms: metrics.DecodeP95Ms,
            DecodeMaxMs: metrics.DecodeMaxMs,
            InteropCopySampleCount: 0,
            InteropCopyAvgMs: 0,
            InteropCopyP95Ms: 0,
            InteropCopyMaxMs: 0,
            CallbackSampleCount: metrics.PipelineSampleCount,
            CallbackAvgMs: metrics.PipelineAvgMs,
            CallbackP95Ms: metrics.PipelineP95Ms,
            CallbackMaxMs: metrics.PipelineMaxMs);
    }

    public ParallelMjpegDecodePipeline.PipelineTimingMetrics? GetFullMjpegPipelineTimingMetrics()
    {
        return Volatile.Read(ref _mjpegPipeline)?.GetTimingMetrics();
    }

    public async Task InitializeAsync(
        string deviceSymbolicLink,
        int width,
        int height,
        double fps,
        bool requireP010,
        string? requestedPixelFormat = null,
        bool useMjpegHighFrameRateMode = false,
        int mjpegDecoderCount = 4)
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
        ParallelMjpegDecodePipeline? mjpegPipeline = null;
        var useExternalMjpegDecode =
            useMjpegHighFrameRateMode &&
            !requireP010 &&
            string.Equals(requestedPixelFormat, "MJPG", StringComparison.OrdinalIgnoreCase);

        if (useExternalMjpegDecode)
        {
            try
            {
                mjpegPipeline = new ParallelMjpegDecodePipeline(
                    mjpegDecoderCount,
                    width,
                    height,
                    OnMjpegPipelineFrameEmitted);
            }
            catch (Exception ex)
            {
                mjpegPipeline?.Dispose();
                Logger.Log($"SW_MJPEG_PIPELINE_FAIL type={ex.GetType().Name} msg={ex.Message}");
                throw new InvalidOperationException(
                    $"CPU MJPEG decode pipeline failed to initialize: {ex.Message}", ex);
            }
        }

        var capture = new MfSourceReaderVideoCapture();
        try
        {
            await capture.InitializeAsync(
                deviceSymbolicLink,
                width,
                height,
                fps,
                requireP010,
                requestedPixelFormat,
                useMjpegHighFrameRateMode,
                useExternalMjpegDecode ? IntPtr.Zero : dxgiDeviceManagerPtr,
                useExternalMjpegDecode).ConfigureAwait(false);
        }
        catch
        {
            mjpegPipeline?.Dispose();
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
            _mjpegPipeline = mjpegPipeline;
            _isP010 = capture.IsP010;
            _isHighFrameRateMjpegMode = capture.IsHighFrameRateMjpegMode;
            _strictPreviewTextureRequired =
                capture.IsHighFrameRateMjpegMode &&
                capture.IsD3DOutputEnabled &&
                !capture.IsCompressedMjpgOutput;
            _width = capture.Width;
            _height = capture.Height;
            _fps = capture.Fps;
            _nativeInputFormat = capture.NativeInputFormat;
            _negotiatedFormat = capture.NegotiatedFormat;
            Interlocked.Exchange(ref _videoFramesArrived, 0);
            Interlocked.Exchange(ref _videoFramesDropped, 0);
            Interlocked.Exchange(ref _videoFramesWrittenToSink, 0);
            Interlocked.Exchange(ref _recordingFramesDelivered, 0);
            Interlocked.Exchange(ref _recordingFramesEnqueued, 0);
            Interlocked.Exchange(ref _lastVideoFrameArrivedTick, 0);
            Interlocked.Exchange(ref _fatalErrorSignaled, 0);
        }

        Logger.Log($"MJPEG_CPU_PIPELINE_CONFIG decoders={mjpegDecoderCount} enabled={mjpegPipeline != null}");

        capture.FatalErrorOccurred += OnCaptureFatalError;
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

    public void SetObservedPixelFormatObserver(Action<string>? observer)
    {
        Volatile.Write(ref _observedPixelFormatObserver, observer);
    }

    public Task StartRecordingAsync(
        IRecordingSink sink,
        IRawVideoFrameEncoder encoder,
        IGpuVideoFrameEncoder? gpuEncoder = null)
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
            _gpuRecordingEncoder = gpuEncoder;
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
            _gpuRecordingEncoder = null;
        }

        return Task.CompletedTask;
    }

    public void SetSkipCpuReadback(bool skip)
    {
        var capture = _capture;
        if (capture != null)
        {
            capture.SkipCpuReadback = skip;
        }
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
            _gpuRecordingEncoder = null;
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
        ParallelMjpegDecodePipeline? mjpegPipeline;
        lock (_sync)
        {
            capture = _capture;
            _capture = null;
            d3dManager = _d3dManager;
            _d3dManager = null;
            mjpegPipeline = _mjpegPipeline;
            _mjpegPipeline = null;
            _isHighFrameRateMjpegMode = false;
            _strictPreviewTextureRequired = false;
            _previewSink = null;
            _observedPixelFormatObserver = null;
        }

        if (capture != null)
        {
            capture.FatalErrorOccurred -= OnCaptureFatalError;
            await capture.DisposeAsync().ConfigureAwait(false);
        }

        mjpegPipeline?.Dispose();
        d3dManager?.Dispose();
    }

    private void OnFrameArrived(ReadOnlySpan<byte> frameData, int width, int height, long arrivalTick)
    {
        Interlocked.Increment(ref _videoFramesArrived);
        Interlocked.Exchange(ref _lastVideoFrameArrivedTick, Environment.TickCount64);

        var pipeline = Volatile.Read(ref _mjpegPipeline);
        if (pipeline != null)
        {
            pipeline.EnqueueFrame(frameData, width, height, arrivalTick);
            return;
        }

        var isP010 = Volatile.Read(ref _isP010);
        Volatile.Read(ref _observedPixelFormatObserver)?.Invoke(isP010 ? "P010" : "NV12");

        EnqueueRecordingFrame(frameData, width, height, isP010);

        var previewSink = Volatile.Read(ref _previewSink);
        if (previewSink != null && !frameData.IsEmpty)
        {
            SubmitPreviewRawFrame(previewSink, frameData, width, height, isP010, arrivalTick);
        }
    }

    private void OnMjpegPipelineFrameEmitted(ReadOnlySpan<byte> nv12Data, int width, int height, long arrivalTick)
    {
        const bool isP010 = false;
        Volatile.Read(ref _observedPixelFormatObserver)?.Invoke("MJPG");

        EnqueueRecordingFrame(nv12Data, width, height, isP010);

        var previewSink = Volatile.Read(ref _previewSink);
        if (previewSink != null)
        {
            SubmitPreviewRawFrame(previewSink, nv12Data, width, height, isP010, arrivalTick);
        }
    }

    private void OnDualFrameArrived(
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
        Volatile.Read(ref _observedPixelFormatObserver)?.Invoke(isP010 ? "P010" : "NV12");

        var gpuEncoder = Volatile.Read(ref _gpuRecordingEncoder);
        if (gpuEncoder != null && gpuTexture != IntPtr.Zero)
        {
            EnqueueGpuRecordingFrame(gpuEncoder, gpuTexture, gpuSubresource);
        }
        else
        {
            EnqueueRecordingFrame(frameData, width, height, isP010);
        }

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

            if (!textureSubmitted &&
                Volatile.Read(ref _strictPreviewTextureRequired))
            {
                Interlocked.Increment(ref _videoFramesDropped);
                SignalFatalError(
                    new InvalidOperationException(
                        $"4K120 MJPG mode requires D3D preview textures, but texture delivery failed for native_input='{_nativeInputFormat}' negotiated='{_negotiatedFormat}'."),
                    "UNIFIED_VIDEO_PREVIEW_TEXTURE_REQUIRED " +
                    $"native_input='{_nativeInputFormat}' negotiated='{_negotiatedFormat}'");
            }
            else if (!textureSubmitted && !frameData.IsEmpty)
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
        IRawVideoFrameEncoder? encoder = null;
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

    private void EnqueueGpuRecordingFrame(IGpuVideoFrameEncoder encoder, IntPtr texture, int subresource)
    {
        Interlocked.Increment(ref _recordingFramesDelivered);
        try
        {
            encoder.EnqueueGpuVideoFrame(texture, subresource);
            Interlocked.Increment(ref _videoFramesWrittenToSink);
            Interlocked.Increment(ref _recordingFramesEnqueued);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _videoFramesDropped);
            Logger.Log($"UNIFIED_VIDEO_GPU_RECORDING_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);

    private void OnCaptureFatalError(object? sender, Exception ex)
    {
        SignalFatalError(
            ex,
            $"UNIFIED_VIDEO_FATAL_CAPTURE_ERROR type={ex.GetType().Name} msg={ex.Message}");
    }

    private void SignalFatalError(Exception ex, string logMessage)
    {
        Logger.Log(logMessage);

        if (Interlocked.Exchange(ref _fatalErrorSignaled, 1) != 0)
        {
            return;
        }

        try
        {
            FatalErrorOccurred?.Invoke(this, ex);
        }
        catch (Exception callbackEx)
        {
            Logger.Log($"UNIFIED_VIDEO_FATAL_CALLBACK_FAIL type={callbackEx.GetType().Name} msg={callbackEx.Message}");
        }
    }
}
