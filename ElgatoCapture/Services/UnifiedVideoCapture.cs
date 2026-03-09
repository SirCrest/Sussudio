using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FFmpeg.AutoGen;

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
    private ICudaVideoFrameEncoder? _cudaRecordingEncoder;
    private NvdecMjpegDecoder? _mjpegDecoder;
    private NvdecMjpegDecoder? _mjpegDecoder1;
    private CudaD3D11InteropBridge? _cudaD3D11Interop;
    private CudaD3D11InteropBridge? _cudaD3D11Interop1;
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
    private long _mjpegFrameSequence;
    private Task? _mjpegPreviewTask;
    private int _mjpegPreviewInFlight;
    private ChannelWriter<MjpegDecodeWorkItem>? _mjpegDecoder1WorkWriter;
    private Task? _mjpegDecoder1WorkerTask;
    private Action<string>? _observedPixelFormatObserver;
    private readonly double[] _mjpegDecodeTimeMs = new double[300];
    private int _mjpegDecodeTimeCount;
    private int _mjpegDecodeTimeIndex;
    private readonly double[] _mjpegInteropCopyTimeMs = new double[300];
    private int _mjpegInteropCopyTimeCount;
    private int _mjpegInteropCopyTimeIndex;
    private readonly double[] _mjpegCallbackTimeMs = new double[300];
    private int _mjpegCallbackTimeCount;
    private int _mjpegCallbackTimeIndex;
    private readonly object _timingLock = new();
    private int _timingDiagDone;

    public bool IsP010 => Volatile.Read(ref _isP010);
    public int Width => Volatile.Read(ref _width);
    public int Height => Volatile.Read(ref _height);
    public double Fps => Volatile.Read(ref _fps);
    public bool IsHighFrameRateMjpegMode => Volatile.Read(ref _isHighFrameRateMjpegMode);
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
    public NvdecMjpegDecoder? MjpegDecoder => Volatile.Read(ref _mjpegDecoder);
    private readonly record struct MjpegDecodeWorkItem(byte[] JpegData, int Width, int Height, long ArrivalTick);
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

    public MfSourceReaderVideoCapture.SourceCadenceMetrics GetSourceCadenceMetrics()
    {
        var capture = _capture;
        return capture?.GetSourceCadenceMetrics() ?? default;
    }

    public MjpegPipelineTimingMetrics GetMjpegPipelineTimingMetrics()
    {
        double[] decodeSamples;
        double[] interopSamples;
        double[] callbackSamples;
        lock (_timingLock)
        {
            decodeSamples = CopyTimingSamples(_mjpegDecodeTimeMs, _mjpegDecodeTimeCount, _mjpegDecodeTimeIndex);
            interopSamples = CopyTimingSamples(_mjpegInteropCopyTimeMs, _mjpegInteropCopyTimeCount, _mjpegInteropCopyTimeIndex);
            callbackSamples = CopyTimingSamples(_mjpegCallbackTimeMs, _mjpegCallbackTimeCount, _mjpegCallbackTimeIndex);
        }

        var decodeMetrics = ComputeTimingMetrics(decodeSamples);
        var interopMetrics = ComputeTimingMetrics(interopSamples);
        var callbackMetrics = ComputeTimingMetrics(callbackSamples);

        if (decodeMetrics.SampleCount > 0 && Interlocked.Exchange(ref _timingDiagDone, 1) == 0)
        {
            Logger.Log(
                $"MJPEG_TIMING_DIAG decode={decodeMetrics.SampleCount}s/{decodeMetrics.AverageMs:F2}avg/{decodeMetrics.P95Ms:F2}p95 " +
                $"interop={interopMetrics.SampleCount}s/{interopMetrics.AverageMs:F2}avg/{interopMetrics.P95Ms:F2}p95 " +
                $"callback={callbackMetrics.SampleCount}s/{callbackMetrics.AverageMs:F2}avg/{callbackMetrics.P95Ms:F2}p95");
        }

        return new MjpegPipelineTimingMetrics(
            DecodeSampleCount: decodeMetrics.SampleCount,
            DecodeAvgMs: decodeMetrics.AverageMs,
            DecodeP95Ms: decodeMetrics.P95Ms,
            DecodeMaxMs: decodeMetrics.MaxMs,
            InteropCopySampleCount: interopMetrics.SampleCount,
            InteropCopyAvgMs: interopMetrics.AverageMs,
            InteropCopyP95Ms: interopMetrics.P95Ms,
            InteropCopyMaxMs: interopMetrics.MaxMs,
            CallbackSampleCount: callbackMetrics.SampleCount,
            CallbackAvgMs: callbackMetrics.AverageMs,
            CallbackP95Ms: callbackMetrics.P95Ms,
            CallbackMaxMs: callbackMetrics.MaxMs);
    }

    public async Task InitializeAsync(
        string deviceSymbolicLink,
        int width,
        int height,
        double fps,
        bool requireP010,
        string? requestedPixelFormat = null,
        bool useMjpegHighFrameRateMode = false)
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
        NvdecMjpegDecoder? mjpegDecoder = null;
        NvdecMjpegDecoder? mjpegDecoder1 = null;
        var useExternalMjpegDecode =
            useMjpegHighFrameRateMode &&
            !requireP010 &&
            string.Equals(requestedPixelFormat, "MJPG", StringComparison.OrdinalIgnoreCase);

        if (useExternalMjpegDecode)
        {
            try
            {
                mjpegDecoder = new NvdecMjpegDecoder();
                mjpegDecoder.Initialize(width, height);
                Logger.Log($"NVDEC_MJPEG_DECODER_AVAILABLE width={width} height={height}");
            }
            catch (Exception ex)
            {
                mjpegDecoder?.Dispose();
                mjpegDecoder = null;
                useExternalMjpegDecode = false;
                Logger.Log($"NVDEC_MJPEG_DECODER_FAIL type={ex.GetType().Name} msg={ex.Message} fallback=mf_hardware_transform");
            }
        }

        if (mjpegDecoder != null)
        {
            try
            {
                mjpegDecoder1 = new NvdecMjpegDecoder();
                mjpegDecoder1.Initialize(width, height);
                Logger.Log($"NVDEC_MJPEG_DECODER1_AVAILABLE width={width} height={height}");
            }
            catch (Exception ex)
            {
                mjpegDecoder1?.Dispose();
                mjpegDecoder1 = null;
                Logger.Log($"NVDEC_MJPEG_DECODER1_FAIL type={ex.GetType().Name} msg={ex.Message}");
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
            mjpegDecoder1?.Dispose();
            mjpegDecoder?.Dispose();
            d3dManager.Dispose();
            throw;
        }

        if (!capture.IsD3DOutputEnabled)
        {
            Logger.Log("UNIFIED_VIDEO_D3D_MANAGER_INACTIVE reason=source_reader_cpu_only");
        }

        CudaD3D11InteropBridge? cudaD3D11Interop = null;
        CudaD3D11InteropBridge? cudaD3D11Interop1 = null;
        if (mjpegDecoder != null)
        {
            try
            {
                var cudaCtx = mjpegDecoder.GetCudaContext();
                cudaD3D11Interop = new CudaD3D11InteropBridge(cudaCtx, d3dManager.Device, capture.Width, capture.Height);
                Logger.Log($"CUDA_D3D11_INTEROP_OK width={capture.Width} height={capture.Height}");
            }
            catch (Exception ex)
            {
                cudaD3D11Interop?.Dispose();
                cudaD3D11Interop = null;
                Logger.Log($"CUDA_D3D11_INTEROP_FAIL fallback=cpu_download type={ex.GetType().Name} msg={ex.Message}");
            }
        }

        if (mjpegDecoder1 != null)
        {
            try
            {
                var cudaCtx1 = mjpegDecoder1.GetCudaContext();
                cudaD3D11Interop1 = new CudaD3D11InteropBridge(cudaCtx1, d3dManager.Device, capture.Width, capture.Height);
                Logger.Log($"CUDA_D3D11_INTEROP1_OK width={capture.Width} height={capture.Height}");
            }
            catch (Exception ex)
            {
                cudaD3D11Interop1?.Dispose();
                cudaD3D11Interop1 = null;
                Logger.Log($"CUDA_D3D11_INTEROP1_FAIL type={ex.GetType().Name} msg={ex.Message}");
            }
        }

        lock (_sync)
        {
            _capture = capture;
            _d3dManager = d3dManager;
            _mjpegDecoder = mjpegDecoder;
            _mjpegDecoder1 = mjpegDecoder1;
            _cudaD3D11Interop = cudaD3D11Interop;
            _cudaD3D11Interop1 = cudaD3D11Interop1;
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
            Interlocked.Exchange(ref _lastVideoFrameArrivedTick, 0);
            Interlocked.Exchange(ref _fatalErrorSignaled, 0);
            Interlocked.Exchange(ref _mjpegFrameSequence, 0);
            _mjpegDecoder1WorkWriter = null;
            _mjpegDecoder1WorkerTask = null;
            ResetMjpegTimingMetrics();
        }

        var decoderCount = 1 + (mjpegDecoder1 != null && cudaD3D11Interop1 != null ? 1 : 0);
        Logger.Log($"MJPEG_DUAL_DECODER_CONFIG count={decoderCount}");

        capture.FatalErrorOccurred += OnCaptureFatalError;
    }

    public void Start()
    {
        ThrowIfDisposed();

        MfSourceReaderVideoCapture? capture;
        CancellationTokenSource readCts;
        ChannelWriter<MjpegDecodeWorkItem>? mjpegDecoder1WorkWriter = null;
        Task? mjpegDecoder1WorkerTask = null;

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

            if (_mjpegDecoder1 != null && _cudaD3D11Interop1 != null)
            {
                var channel = Channel.CreateBounded<MjpegDecodeWorkItem>(new BoundedChannelOptions(2)
                {
                    SingleReader = true,
                    SingleWriter = true,
                    FullMode = BoundedChannelFullMode.Wait
                });

                _mjpegDecoder1WorkWriter = channel.Writer;
                _mjpegDecoder1WorkerTask = RunMjpegDecoder1WorkerAsync(channel.Reader, _mjpegDecoder1, _cudaD3D11Interop1);
                mjpegDecoder1WorkWriter = _mjpegDecoder1WorkWriter;
                mjpegDecoder1WorkerTask = _mjpegDecoder1WorkerTask;
            }
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
                _mjpegDecoder1WorkWriter = null;
                _mjpegDecoder1WorkerTask = null;
            }

            mjpegDecoder1WorkWriter?.TryComplete();
            mjpegDecoder1WorkerTask?.GetAwaiter().GetResult();
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
        IGpuVideoFrameEncoder? gpuEncoder = null,
        ICudaVideoFrameEncoder? cudaEncoder = null)
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
            _cudaRecordingEncoder = cudaEncoder;
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
            _cudaRecordingEncoder = null;
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
            _cudaRecordingEncoder = null;
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

        await DrainMjpegDecoder1WorkItemsAsync().ConfigureAwait(false);
        await DrainMjpegPreviewAsync().ConfigureAwait(false);
        ResetMjpegTimingMetrics();

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
        NvdecMjpegDecoder? mjpegDecoder;
        NvdecMjpegDecoder? mjpegDecoder1;
        CudaD3D11InteropBridge? cudaD3D11Interop;
        CudaD3D11InteropBridge? cudaD3D11Interop1;
        lock (_sync)
        {
            capture = _capture;
            _capture = null;
            d3dManager = _d3dManager;
            _d3dManager = null;
            mjpegDecoder = _mjpegDecoder;
            _mjpegDecoder = null;
            mjpegDecoder1 = _mjpegDecoder1;
            _mjpegDecoder1 = null;
            cudaD3D11Interop = _cudaD3D11Interop;
            _cudaD3D11Interop = null;
            cudaD3D11Interop1 = _cudaD3D11Interop1;
            _cudaD3D11Interop1 = null;
            _mjpegDecoder1WorkWriter = null;
            _mjpegDecoder1WorkerTask = null;
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

        cudaD3D11Interop1?.Dispose();
        cudaD3D11Interop?.Dispose();
        mjpegDecoder1?.Dispose();
        mjpegDecoder?.Dispose();
        d3dManager?.Dispose();
    }

    private unsafe void OnFrameArrived(ReadOnlySpan<byte> frameData, int width, int height, long arrivalTick)
    {
        Interlocked.Increment(ref _videoFramesArrived);
        Interlocked.Exchange(ref _lastVideoFrameArrivedTick, Environment.TickCount64);

        var decoder = Volatile.Read(ref _mjpegDecoder);
        if (decoder != null)
        {
            HandleMjpegFrame(decoder, frameData, width, height, arrivalTick);
            return;
        }

        var isP010 = Volatile.Read(ref _isP010);
        Volatile.Read(ref _observedPixelFormatObserver)?.Invoke(isP010 ? "P010" : "NV12");

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
        Volatile.Read(ref _observedPixelFormatObserver)?.Invoke(isP010 ? "P010" : "NV12");

        // Recording first keeps encoder delivery ahead of any preview-side lock contention.
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

    private unsafe void HandleMjpegFrame(
        NvdecMjpegDecoder decoder,
        ReadOnlySpan<byte> jpegData,
        int width,
        int height,
        long arrivalTick)
    {
        const bool isP010 = false;
        Volatile.Read(ref _observedPixelFormatObserver)?.Invoke("MJPG");

        var seq = Interlocked.Increment(ref _mjpegFrameSequence);
        var decoder1 = Volatile.Read(ref _mjpegDecoder1);
        var interop1 = Volatile.Read(ref _cudaD3D11Interop1);
        if (seq == 300)
        {
            Logger.Log($"MJPEG_DUAL_DECODER_SEQ seq={seq} dual={(decoder1 != null && interop1 != null ? "active" : "single")}");
        }

        if (decoder1 != null &&
            interop1 != null &&
            (seq & 1) == 1)
        {
            var workWriter = Volatile.Read(ref _mjpegDecoder1WorkWriter);
            if (workWriter != null &&
                workWriter.TryWrite(new MjpegDecodeWorkItem(
                    jpegData.ToArray(),
                    width,
                    height,
                    arrivalTick)))
            {
                return;
            }
        }

        DecodeMjpegFrame(
            decoder,
            Volatile.Read(ref _cudaD3D11Interop),
            jpegData,
            width,
            height,
            isP010,
            arrivalTick,
            Stopwatch.GetTimestamp(),
            0);
    }

    private unsafe void DecodeMjpegFrame(
        NvdecMjpegDecoder decoder,
        CudaD3D11InteropBridge? interop,
        ReadOnlySpan<byte> jpegData,
        int width,
        int height,
        bool isP010,
        long arrivalTick,
        long callbackStart,
        int slot)
    {
        AVFrame* cudaFrame;
        var decodeStart = Stopwatch.GetTimestamp();
        try
        {
            cudaFrame = decoder.DecodeFrame(jpegData);
            RecordTimingSample(
                _mjpegDecodeTimeMs,
                ref _mjpegDecodeTimeCount,
                ref _mjpegDecodeTimeIndex,
                GetElapsedMilliseconds(decodeStart, Stopwatch.GetTimestamp()));
            if (cudaFrame == null)
            {
                Interlocked.Increment(ref _videoFramesDropped);
                RecordTimingSample(
                    _mjpegCallbackTimeMs,
                    ref _mjpegCallbackTimeCount,
                    ref _mjpegCallbackTimeIndex,
                    GetElapsedMilliseconds(callbackStart, Stopwatch.GetTimestamp()));
                return;
            }
        }
        catch (Exception ex)
        {
            RecordTimingSample(
                _mjpegDecodeTimeMs,
                ref _mjpegDecodeTimeCount,
                ref _mjpegDecodeTimeIndex,
                GetElapsedMilliseconds(decodeStart, Stopwatch.GetTimestamp()));
            Interlocked.Increment(ref _videoFramesDropped);
            Logger.Log($"NVDEC_MJPEG_DECODE_FAIL slot={slot} type={ex.GetType().Name} msg={ex.Message}");
            RecordTimingSample(
                _mjpegCallbackTimeMs,
                ref _mjpegCallbackTimeCount,
                ref _mjpegCallbackTimeIndex,
                GetElapsedMilliseconds(callbackStart, Stopwatch.GetTimestamp()));
            return;
        }

        EnqueueCudaRecordingFrame(cudaFrame);

        var previewSink = Volatile.Read(ref _previewSink);
        if (interop != null && previewSink != null)
        {
            var copyStart = Stopwatch.GetTimestamp();
            try
            {
                interop.CopyFrameToTexture(cudaFrame);
            }
            finally
            {
                RecordTimingSample(
                    _mjpegInteropCopyTimeMs,
                    ref _mjpegInteropCopyTimeCount,
                    ref _mjpegInteropCopyTimeIndex,
                    GetElapsedMilliseconds(copyStart, Stopwatch.GetTimestamp()));
            }

            try
            {
                previewSink.SubmitTexture(interop.TextureNativePointer, 0, width, height, isP010, arrivalTick);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _videoFramesDropped);
                Logger.Log($"CUDA_D3D11_PREVIEW_FAIL slot={slot} type={ex.GetType().Name} msg={ex.Message}");
            }

            RecordTimingSample(
                _mjpegCallbackTimeMs,
                ref _mjpegCallbackTimeCount,
                ref _mjpegCallbackTimeIndex,
                GetElapsedMilliseconds(callbackStart, Stopwatch.GetTimestamp()));
            return;
        }

        // CPU fallback (clone → download → SubmitRawFrame) is not viable at HFR.
        // Hard-fail so the user sees an error instead of a silently broken preview.
        if (Volatile.Read(ref _isHighFrameRateMjpegMode))
        {
            Interlocked.Increment(ref _videoFramesDropped);
            SignalFatalError(
                new InvalidOperationException(
                    "CUDA-D3D11 interop unavailable — CPU preview fallback is not viable for HFR MJPG mode."),
                $"MJPEG_HFR_NO_GPU_PREVIEW slot={slot}");
            RecordTimingSample(
                _mjpegCallbackTimeMs,
                ref _mjpegCallbackTimeCount,
                ref _mjpegCallbackTimeIndex,
                GetElapsedMilliseconds(callbackStart, Stopwatch.GetTimestamp()));
            return;
        }

        AVFrame* clonedFrame = null;
        lock (_sync)
        {
            if (!_started || _disposed || _readCts == null || _previewSink == null)
            {
                RecordTimingSample(
                    _mjpegCallbackTimeMs,
                    ref _mjpegCallbackTimeCount,
                    ref _mjpegCallbackTimeIndex,
                    GetElapsedMilliseconds(callbackStart, Stopwatch.GetTimestamp()));
                return;
            }

            if (Interlocked.CompareExchange(ref _mjpegPreviewInFlight, 1, 0) != 0)
            {
                RecordTimingSample(
                    _mjpegCallbackTimeMs,
                    ref _mjpegCallbackTimeCount,
                    ref _mjpegCallbackTimeIndex,
                    GetElapsedMilliseconds(callbackStart, Stopwatch.GetTimestamp()));
                return;
            }

            clonedFrame = ffmpeg.av_frame_clone(cudaFrame);
            if (clonedFrame == null)
            {
                Interlocked.Exchange(ref _mjpegPreviewInFlight, 0);
                Interlocked.Increment(ref _videoFramesDropped);
                Logger.Log($"NVDEC_PREVIEW_CLONE_FAIL slot={slot}");
                RecordTimingSample(
                    _mjpegCallbackTimeMs,
                    ref _mjpegCallbackTimeCount,
                    ref _mjpegCallbackTimeIndex,
                    GetElapsedMilliseconds(callbackStart, Stopwatch.GetTimestamp()));
                return;
            }

            _mjpegPreviewTask = Task.Run(() =>
            {
                var frame = clonedFrame;
                try
                {
                    var currentPreviewSink = Volatile.Read(ref _previewSink);
                    if (currentPreviewSink == null)
                    {
                        return;
                    }

                    if (decoder.TryDownloadToCpu(frame, out var nv12Data, out var nv12Size))
                    {
                        currentPreviewSink.SubmitRawFrame(nv12Data, nv12Size, width, height, isP010, arrivalTick);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _videoFramesDropped);
                    Logger.Log($"NVDEC_PREVIEW_DOWNLOAD_FAIL slot={slot} type={ex.GetType().Name} msg={ex.Message}");
                }
                finally
                {
                    ffmpeg.av_frame_free(&frame);
                    Interlocked.Exchange(ref _mjpegPreviewInFlight, 0);
                }
            });
        }

        RecordTimingSample(
            _mjpegCallbackTimeMs,
            ref _mjpegCallbackTimeCount,
            ref _mjpegCallbackTimeIndex,
            GetElapsedMilliseconds(callbackStart, Stopwatch.GetTimestamp()));
    }

    private async Task RunMjpegDecoder1WorkerAsync(
        ChannelReader<MjpegDecodeWorkItem> reader,
        NvdecMjpegDecoder decoder,
        CudaD3D11InteropBridge interop)
    {
        try
        {
            while (await reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (reader.TryRead(out var workItem))
                {
                    DecodeMjpegFrame(
                        decoder,
                        interop,
                        workItem.JpegData,
                        workItem.Width,
                        workItem.Height,
                        isP010: false,
                        workItem.ArrivalTick,
                        Stopwatch.GetTimestamp(),
                        1);
                }
            }
        }
        catch (Exception ex)
        {
            SignalFatalError(
                ex,
                $"MJPEG_DUAL_DECODER_WORKER_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private async Task DrainMjpegDecoder1WorkItemsAsync()
    {
        ChannelWriter<MjpegDecodeWorkItem>? mjpegDecoder1WorkWriter;
        Task? mjpegDecoder1WorkerTask;
        lock (_sync)
        {
            mjpegDecoder1WorkWriter = _mjpegDecoder1WorkWriter;
            _mjpegDecoder1WorkWriter = null;
            mjpegDecoder1WorkerTask = _mjpegDecoder1WorkerTask;
            _mjpegDecoder1WorkerTask = null;
        }

        mjpegDecoder1WorkWriter?.TryComplete();
        if (mjpegDecoder1WorkerTask != null)
        {
            await mjpegDecoder1WorkerTask.ConfigureAwait(false);
        }
    }

    private async Task DrainMjpegPreviewAsync()
    {
        while (true)
        {
            Task? mjpegPreviewTask;
            lock (_sync)
            {
                mjpegPreviewTask = _mjpegPreviewTask;
            }

            if (mjpegPreviewTask == null)
            {
                if (Volatile.Read(ref _mjpegPreviewInFlight) == 0)
                {
                    return;
                }

                await Task.Yield();
                continue;
            }

            await mjpegPreviewTask.ConfigureAwait(false);

            lock (_sync)
            {
                if (ReferenceEquals(_mjpegPreviewTask, mjpegPreviewTask))
                {
                    _mjpegPreviewTask = null;
                }
            }
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

    private unsafe void EnqueueCudaRecordingFrame(AVFrame* cudaFrame)
    {
        ICudaVideoFrameEncoder? encoder = null;
        bool recordingActive;
        lock (_sync)
        {
            recordingActive = _recordingActive;
            if (recordingActive)
            {
                encoder = _cudaRecordingEncoder;
            }
        }

        if (!recordingActive || encoder == null)
        {
            return;
        }

        Interlocked.Increment(ref _recordingFramesDelivered);

        try
        {
            encoder.EnqueueCudaVideoFrame(cudaFrame);
            Interlocked.Increment(ref _videoFramesWrittenToSink);
            Interlocked.Increment(ref _recordingFramesEnqueued);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _videoFramesDropped);
            Logger.Log($"NVDEC_CUDA_ENQUEUE_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void RecordTimingSample(double[] window, ref int count, ref int index, double valueMs)
    {
        lock (_timingLock)
        {
            window[index] = valueMs;
            index = (index + 1) % window.Length;
            if (count < window.Length)
            {
                count++;
            }
        }
    }

    private void ResetMjpegTimingMetrics()
    {
        lock (_timingLock)
        {
            Array.Clear(_mjpegDecodeTimeMs, 0, _mjpegDecodeTimeMs.Length);
            _mjpegDecodeTimeCount = 0;
            _mjpegDecodeTimeIndex = 0;
            Array.Clear(_mjpegInteropCopyTimeMs, 0, _mjpegInteropCopyTimeMs.Length);
            _mjpegInteropCopyTimeCount = 0;
            _mjpegInteropCopyTimeIndex = 0;
            Array.Clear(_mjpegCallbackTimeMs, 0, _mjpegCallbackTimeMs.Length);
            _mjpegCallbackTimeCount = 0;
            _mjpegCallbackTimeIndex = 0;
        }
    }

    private static double[] CopyTimingSamples(double[] window, int count, int index)
    {
        var samples = new double[count];
        for (var i = 0; i < count; i++)
        {
            var ringIndex = (index - count + i + window.Length) % window.Length;
            samples[i] = window[ringIndex];
        }

        return samples;
    }

    private static (int SampleCount, double AverageMs, double P95Ms, double MaxMs) ComputeTimingMetrics(double[] samples)
    {
        var sampleCount = samples.Length;
        if (sampleCount == 0)
        {
            return (0, 0, 0, 0);
        }

        var sum = 0.0;
        var max = 0.0;
        for (var i = 0; i < sampleCount; i++)
        {
            sum += samples[i];
            if (samples[i] > max)
            {
                max = samples[i];
            }
        }

        Array.Sort(samples);
        var p95Index = Math.Min((int)(sampleCount * 0.95), sampleCount - 1);

        return (sampleCount, sum / sampleCount, samples[p95Index], max);
    }

    private static double GetElapsedMilliseconds(long startTimestamp, long endTimestamp)
        => (endTimestamp - startTimestamp) * 1000.0 / Stopwatch.Frequency;

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
