using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Contracts;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio.Services.Capture;

// Owns the single source-reader session used by both preview and recording.
// The important contract is fan-out: capture frames arrive once, then this
// class routes them to the live preview sink, optional Flashback sink, and
// optional user recording sink without starting a second device session.
internal sealed class UnifiedVideoCapture : IAsyncDisposable, ILiveVideoSource
{
    private readonly object _sync = new();
    private MfSourceReaderVideoCapture? _capture;
    private SharedD3DDeviceManager? _d3dManager;
    private CancellationTokenSource? _readCts;
    private IPreviewFrameSink? _previewSink;
    private IRecordingSink? _recordingSink;
    private IRawVideoFrameEncoder? _recordingEncoder;
    private IGpuVideoFrameEncoder? _gpuRecordingEncoder;
    private FlashbackEncoderSink? _flashbackSink;
    private ParallelMjpegDecodePipeline? _mjpegPipeline;
    private MjpegPreviewJitterBuffer? _mjpegPreviewJitterBuffer;
    private readonly FrameLedger _frameLedger = new();
    private readonly VisualCadenceTracker _visualCadenceTracker = new(cropLeft: 0.25, cropTop: 0.25, cropWidth: 0.5, cropHeight: 0.5);
    private readonly VisualCadenceTracker _visualCenterCadenceTracker = new(
        sampleColumns: 320,
        sampleRows: 180,
        cropLeft: 0.375,
        cropTop: 0.375,
        cropWidth: 0.25,
        cropHeight: 0.25);
    private bool _started;
    private bool _recordingActive;
    private bool _flashbackRecordingAccountingActive;
    private long _flashbackRecordingLastAcceptedSequence = -1;
    private long _flashbackRecordingSequenceGaps;
    private bool _disposed;
    private int _disposeStarted;
    private bool _isP010;
    private bool _isHighFrameRateMjpegMode;
    private bool _strictPreviewTextureRequired;
    private int _fatalErrorSignaled;
    private int _consecutiveTextureFailures;
    private int _visualCadenceCpuDataUnavailable;
    private const int MaxConsecutiveTextureFailures = 5;
    private int _width;
    private int _height;
    private double _fps;
    private string _nativeInputFormat = "unknown";
    private string _negotiatedFormat = "unknown";
    private long _videoFramesArrived;
    private long _videoFramesDropped;
    private long _livePreviewPresentId;
    private long _videoFramesWrittenToSink;
    private long _recordingFramesDelivered;
    private long _recordingFramesRejected;
    private long _recordingQueueRejectedFrames;
    private long _lastVideoFrameArrivedTick;
    private Action<string>? _pixelFormatDetectedCallback;
    private int _pixelFormatObserverFired;
    private volatile bool _previewSuppressed;
    private readonly bool _pooledCpuFanoutEnabled =
        EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_CAPTURE_POOLED_FANOUT", 1, 0, 1) != 0;

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
    public long RecordingFramesRejected => Interlocked.Read(ref _recordingFramesRejected);
    public long RecordingQueueRejectedFrames => Interlocked.Read(ref _recordingQueueRejectedFrames);
    public long FlashbackRecordingSequenceGaps => Interlocked.Read(ref _flashbackRecordingSequenceGaps);
    public long LastVideoFrameArrivedTick => Interlocked.Read(ref _lastVideoFrameArrivedTick);
    public event EventHandler<Exception>? FatalErrorOccurred;
    public bool SourceReaderReadOutstanding => _capture?.IsReadSampleOutstanding ?? false;
    public long SourceReaderReadOutstandingMs => _capture?.ReadSampleOutstandingMs ?? 0;
    public long SourceReaderLastFrameTickMs => _capture?.LastFrameDeliveredTickMs ?? 0;
    public SharedD3DDeviceManager? D3DManager => Volatile.Read(ref _d3dManager);

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

    public readonly record struct MjpegPipelineTimingSnapshot(
        MjpegPipelineTimingMetrics Summary,
        ParallelMjpegDecodePipeline.PipelineTimingMetrics? Details);

    public void SetFlashbackSink(FlashbackEncoderSink? sink)
    {
        Volatile.Write(ref _flashbackSink, sink);
    }

    public void SetPixelFormatDetectedCallback(Action<string>? observer)
    {
        Volatile.Write(ref _pixelFormatDetectedCallback, observer);
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
            Volatile.Write(ref _recordingEncoder, encoder);
            Volatile.Write(ref _gpuRecordingEncoder, gpuEncoder);
            Interlocked.Exchange(ref _videoFramesWrittenToSink, 0);
            Interlocked.Exchange(ref _recordingFramesDelivered, 0);
            Interlocked.Exchange(ref _recordingFramesRejected, 0);
            Interlocked.Exchange(ref _recordingQueueRejectedFrames, 0);
            Volatile.Write(ref _recordingActive, true);
        }

        return Task.CompletedTask;
    }

    public void BeginFlashbackRecordingAccounting()
    {
        Interlocked.Exchange(ref _videoFramesWrittenToSink, 0);
        Interlocked.Exchange(ref _recordingFramesDelivered, 0);
        Interlocked.Exchange(ref _recordingFramesRejected, 0);
        Interlocked.Exchange(ref _recordingQueueRejectedFrames, 0);
        Interlocked.Exchange(ref _flashbackRecordingLastAcceptedSequence, -1);
        Interlocked.Exchange(ref _flashbackRecordingSequenceGaps, 0);
        Volatile.Write(ref _flashbackRecordingAccountingActive, true);
    }

    public void EndFlashbackRecordingAccounting()
    {
        Volatile.Write(ref _flashbackRecordingAccountingActive, false);
    }

    public Task StopRecordingAsync()
    {
        lock (_sync)
        {
            Volatile.Write(ref _recordingActive, false);
            Volatile.Write(ref _flashbackRecordingAccountingActive, false);
            _recordingSink = null;
            Volatile.Write(ref _recordingEncoder, null);
            Volatile.Write(ref _gpuRecordingEncoder, null);
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

    public MfSourceReaderVideoCapture.SourceCadenceMetrics GetSourceCadenceMetrics()
    {
        var capture = _capture;
        return capture?.GetSourceCadenceMetrics() ?? default;
    }

    public MjpegPipelineTimingMetrics GetMjpegPipelineTimingMetrics()
        => GetMjpegPipelineTimingSnapshot().Summary;

    public MjpegPipelineTimingSnapshot GetMjpegPipelineTimingSnapshot()
    {
        var pipeline = Volatile.Read(ref _mjpegPipeline);
        if (pipeline == null)
        {
            return default;
        }

        var metrics = pipeline.GetTimingMetrics();
        return new MjpegPipelineTimingSnapshot(
            Summary: CreateMjpegPipelineTimingSummary(metrics),
            Details: metrics);
    }

    public ParallelMjpegDecodePipeline.PipelineTimingMetrics? GetFullMjpegPipelineTimingMetrics()
    {
        return Volatile.Read(ref _mjpegPipeline)?.GetTimingMetrics();
    }

    private static MjpegPipelineTimingMetrics CreateMjpegPipelineTimingSummary(ParallelMjpegDecodePipeline.PipelineTimingMetrics metrics)
    {
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

    public MjpegPreviewJitterBuffer.Metrics GetMjpegPreviewJitterMetrics()
    {
        return Volatile.Read(ref _mjpegPreviewJitterBuffer)?.GetMetrics() ?? default;
    }

    public VisualCadenceTracker.Metrics GetPreviewVisualCadenceMetrics()
    {
        return _visualCadenceTracker.GetMetrics();
    }

    public VisualCadenceTracker.Metrics GetPreviewVisualCenterCadenceMetrics()
    {
        return _visualCenterCadenceTracker.GetMetrics();
    }

    public FrameFingerprintCadenceTracker.Metrics GetMjpegPacketHashMetrics()
    {
        return Volatile.Read(ref _mjpegPipeline)?.GetPacketHashMetrics()
            ?? FrameFingerprintCadenceTracker.Empty;
    }

    public FrameLedgerSummary GetFrameLedgerSummary(int maxEvents = 64)
        => _frameLedger.GetSummary(maxEvents);

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
        var useExternalMjpegDecode = ShouldUseExternalMjpegDecode(
            useMjpegHighFrameRateMode,
            requireP010,
            requestedPixelFormat);
        var mjpegPipeline = CreateExternalMjpegPipelineIfNeeded(
            useExternalMjpegDecode,
            mjpegDecoderCount,
            width,
            height,
            fps);

        var capture = new MfSourceReaderVideoCapture();
        try
        {
            await capture.InitializeAsync(
                deviceSymbolicLink,
                new VideoCaptureNegotiationOptions(
                    Width: width,
                    Height: height,
                    Fps: fps,
                    RequireP010: requireP010,
                    RequestedPixelFormat: requestedPixelFormat,
                    UseMjpegHighFrameRateMode: useMjpegHighFrameRateMode,
                    DxgiDeviceManager: useExternalMjpegDecode ? IntPtr.Zero : dxgiDeviceManagerPtr,
                    UseExternalMjpegDecode: useExternalMjpegDecode))
                .ConfigureAwait(false);
        }
        catch
        {
            mjpegPipeline?.Dispose();
            d3dManager.Dispose();
            throw;
        }

        if (mjpegPipeline != null)
        {
            InstallMjpegPreviewJitterBuffer(capture.Fps > 0 ? capture.Fps : fps);
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
            _visualCadenceTracker.Reset();
            _visualCenterCadenceTracker.Reset();
            Interlocked.Exchange(ref _videoFramesArrived, 0);
            Interlocked.Exchange(ref _videoFramesDropped, 0);
            Interlocked.Exchange(ref _videoFramesWrittenToSink, 0);
            Interlocked.Exchange(ref _recordingFramesDelivered, 0);
            Interlocked.Exchange(ref _recordingFramesRejected, 0);
            Interlocked.Exchange(ref _recordingQueueRejectedFrames, 0);
            Interlocked.Exchange(ref _lastVideoFrameArrivedTick, 0);
            Interlocked.Exchange(ref _fatalErrorSignaled, 0);
            Interlocked.Exchange(ref _consecutiveTextureFailures, 0);
            Interlocked.Exchange(ref _visualCadenceCpuDataUnavailable, 0);
            Interlocked.Exchange(ref _pixelFormatObserverFired, 0);
            _frameLedger.Reset();
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
            // D3D output and CPU/MJPEG output are mutually exclusive at source
            // reader initialization time; the callback shape is chosen from the
            // negotiated capture path and stays stable until StopAsync.
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

    public async Task StopAsync()
    {
        CancellationTokenSource? readCts;
        MfSourceReaderVideoCapture? capture;
        ParallelMjpegDecodePipeline? mjpegPipelineToStop = null;

        lock (_sync)
        {
            _started = false;
            _recordingActive = false;
            _flashbackRecordingAccountingActive = false;
            _recordingSink = null;
            _recordingEncoder = null;
            _gpuRecordingEncoder = null;
            Volatile.Write(ref _flashbackSink, null);
            readCts = _readCts;
            _readCts = null;
            capture = _capture;
        }

        readCts?.Cancel();
        try
        {
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

            lock (_sync)
            {
                mjpegPipelineToStop = _mjpegPipeline;
                if (mjpegPipelineToStop != null)
                {
                    _previewSink = null;
                    _pixelFormatDetectedCallback = null;
                }
            }

            if (mjpegPipelineToStop != null)
            {
                StopAndDisposeMjpegPipeline(mjpegPipelineToStop);
            }
        }
        finally
        {
            readCts?.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
        => await DisposeCoreAsync(disposeSharedD3DDeviceManager: true).ConfigureAwait(false);

    public async ValueTask DisposeForPreviewReinitAsync()
        => await DisposeCoreAsync(disposeSharedD3DDeviceManager: false).ConfigureAwait(false);

    private async ValueTask DisposeCoreAsync(bool disposeSharedD3DDeviceManager)
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
        {
            return;
        }

        Exception? stopException = null;
        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            stopException = ex;
        }

        MfSourceReaderVideoCapture? capture;
        SharedD3DDeviceManager? d3dManager;
        ParallelMjpegDecodePipeline? mjpegPipeline;
        MjpegPreviewJitterBuffer? jitterBuffer;
        lock (_sync)
        {
            capture = _capture;
            _capture = null;
            d3dManager = _d3dManager;
            _d3dManager = null;
            mjpegPipeline = _mjpegPipeline;
            _mjpegPipeline = null;
            jitterBuffer = _mjpegPreviewJitterBuffer;
            _mjpegPreviewJitterBuffer = null;
            _isHighFrameRateMjpegMode = false;
            _strictPreviewTextureRequired = false;
            _previewSink = null;
            _pixelFormatDetectedCallback = null;
            Volatile.Write(ref _flashbackSink, null);
            _disposed = true;
        }

        if (capture != null)
        {
            capture.FatalErrorOccurred -= OnCaptureFatalError;
            await capture.DisposeAsync().ConfigureAwait(false);
        }

        DisposeMjpegPipelineResources(mjpegPipeline, jitterBuffer);
        if (disposeSharedD3DDeviceManager)
        {
            d3dManager?.Dispose();
        }
        else if (d3dManager != null)
        {
            Logger.Log("UNIFIED_VIDEO_REINIT_RETIRE_SHARED_D3D_MANAGER skip_dispose=true");
        }

        if (stopException != null)
        {
            throw stopException;
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

    private static bool ShouldUseExternalMjpegDecode(
        bool useMjpegHighFrameRateMode,
        bool requireP010,
        string? requestedPixelFormat)
    {
        // 4K120 MJPEG is compressed on the USB wire. In that mode the source
        // reader must hand compressed samples to our decoder instead of trying
        // to expose D3D textures directly from Media Foundation.
        return useMjpegHighFrameRateMode &&
            !requireP010 &&
            string.Equals(requestedPixelFormat, "MJPG", StringComparison.OrdinalIgnoreCase);
    }

    private ParallelMjpegDecodePipeline? CreateExternalMjpegPipelineIfNeeded(
        bool useExternalMjpegDecode,
        int mjpegDecoderCount,
        int width,
        int height,
        double fps)
    {
        if (!useExternalMjpegDecode)
        {
            return null;
        }

        ParallelMjpegDecodePipeline? mjpegPipeline = null;
        try
        {
            mjpegPipeline = new ParallelMjpegDecodePipeline(
                mjpegDecoderCount,
                width,
                height,
                OnMjpegPipelineFrameEmitted,
                OnMjpegPipelineFatalError,
                OnMjpegPipelinePreviewFrameDecoded,
                fps,
                strictFrameConsumerActive: () =>
                    Volatile.Read(ref _recordingActive) ||
                    Volatile.Read(ref _flashbackSink) != null);
            return mjpegPipeline;
        }
        catch (Exception ex)
        {
            mjpegPipeline?.Dispose();
            Logger.Log($"SW_MJPEG_PIPELINE_FAIL type={ex.GetType().Name} msg={ex.Message}");
            throw new InvalidOperationException(
                $"CPU MJPEG decode pipeline failed to initialize: {ex.Message}", ex);
        }
    }

    private void InstallMjpegPreviewJitterBuffer(double fps)
    {
        Volatile.Write(
            ref _mjpegPreviewJitterBuffer,
            new MjpegPreviewJitterBuffer(
                fps,
                () => Volatile.Read(ref _previewSink),
                () => _previewSuppressed,
                previewFrameProbe: null,
                targetDepth: 3));
    }

    private void StopAndDisposeMjpegPipeline(ParallelMjpegDecodePipeline mjpegPipelineToStop)
    {
        var jitterBuffer = Interlocked.Exchange(ref _mjpegPreviewJitterBuffer, null);
        jitterBuffer?.Dispose();

        if (!mjpegPipelineToStop.TryStop(TimeSpan.FromSeconds(5), out var failureReason))
        {
            // Stop must never throw — a wedged emitter must not block reinit or preview-stop.
            // Log and proceed; Dispose drains the ring with its own bounded timeout and does not throw.
            Logger.Log(
                $"UNIFIED_VIDEO_MJPEG_STOP_TIMEOUT reason='{failureReason ?? "unknown"}' — forcing cleanup");
        }

        lock (_sync)
        {
            if (ReferenceEquals(_mjpegPipeline, mjpegPipelineToStop))
            {
                _mjpegPipeline = null;
            }
        }

        mjpegPipelineToStop.Dispose();
    }

    private static void DisposeMjpegPipelineResources(
        ParallelMjpegDecodePipeline? mjpegPipeline,
        MjpegPreviewJitterBuffer? jitterBuffer)
    {
        mjpegPipeline?.Dispose();
        jitterBuffer?.Dispose();
    }

    private void OnMjpegPipelineFatalError(Exception ex)
    {
        SignalFatalError(
            ex,
            $"UNIFIED_VIDEO_FATAL_MJPEG_ERROR type={ex.GetType().Name} msg={ex.Message}");
    }

    private void OnFrameArrived(ReadOnlySpan<byte> frameData, int width, int height, long arrivalTick)
    {
        var sourceSequence = Interlocked.Increment(ref _videoFramesArrived) - 1;
        Interlocked.Exchange(ref _lastVideoFrameArrivedTick, Environment.TickCount64);
        RecordCaptureArrived(sourceSequence, arrivalTick, width, height, frameData.Length);

        var pipeline = Volatile.Read(ref _mjpegPipeline);
        if (pipeline != null)
        {
            var accepted = pipeline.EnqueueFrame(frameData, width, height, arrivalTick);
            _frameLedger.RecordEvent(
                sourceSequence,
                FrameLedgerStage.CompressedQueued,
                subsystem: "mjpeg",
                byteDepth: frameData.Length,
                accepted: accepted,
                reason: accepted ? null : "mjpeg_queue_rejected");
            return;
        }

        var isP010 = Volatile.Read(ref _isP010);
        FirePixelFormatObserverOnce(isP010 ? "P010" : "NV12");

        var previewSink = Volatile.Read(ref _previewSink);
        var previewWanted = !_previewSuppressed && previewSink != null && !frameData.IsEmpty;

        if (_pooledCpuFanoutEnabled && !frameData.IsEmpty)
        {
            FanOutPooledCpuFrame(
                frameData,
                width,
                height,
                isP010,
                arrivalTick,
                sourceSequence,
                previewWanted ? previewSink : null);
            return;
        }

        EnqueueRecordingFrame(frameData, width, height, isP010, sourceSequence);
        EnqueueFlashbackFrame(frameData, width, height, isP010, sourceSequence);

        if (previewWanted)
        {
            SubmitPreviewRawFrame(previewSink!, frameData, width, height, isP010, arrivalTick, sourceSequence);
        }
    }

    // Single-copy fan-out for uncompressed CPU frames: the MF buffer is only
    // valid while this callback runs, so each consumer otherwise copies the
    // full frame inline on the capture read loop (~12 MB per consumer per
    // frame at 4K NV12, ~25 MB at P010). Copy once into a pooled frame and
    // hand refcounted leases to recording, Flashback, and preview instead.
    private void FanOutPooledCpuFrame(
        ReadOnlySpan<byte> frameData,
        int width,
        int height,
        bool isP010,
        long arrivalTick,
        long sourceSequence,
        IPreviewFrameSink? previewSink)
    {
        var recordingWanted = Volatile.Read(ref _recordingActive) && Volatile.Read(ref _recordingEncoder) != null;
        var flashbackWanted = Volatile.Read(ref _flashbackSink) != null;
        if (!recordingWanted && !flashbackWanted && previewSink == null)
        {
            return;
        }

        var frame = PooledVideoFrame.Rent(
            sourceSequence,
            arrivalTick,
            decodedTick: arrivalTick,
            width,
            height,
            isP010 ? PooledVideoPixelFormat.P010 : PooledVideoPixelFormat.Nv12,
            frameData.Length);
        try
        {
            frameData.CopyTo(frame.Span);

            if (recordingWanted)
            {
                EnqueueRecordingFrame(frame);
            }

            if (flashbackWanted)
            {
                EnqueueFlashbackFrame(frame);
            }

            if (previewSink != null)
            {
                SubmitPreviewFrameLease(previewSink, frame, isP010, sourceSequence);
            }
        }
        finally
        {
            frame.Dispose();
        }
    }

    private void OnMjpegPipelineFrameEmitted(PooledVideoFrame frame)
    {
        FirePixelFormatObserverOnce("NV12");
        _frameLedger.RecordEvent(
            frame.SequenceNumber,
            FrameLedgerStage.StrictOrderReleased,
            subsystem: "mjpeg");

        // Visual cadence tracking compares consecutive frames, so it must run
        // on this strictly ordered single-threaded path — the preview fork can
        // deliver frames out of order from multiple decode workers.
        TrackPreviewVisualFrame(
            frame.Span,
            frame.Width,
            frame.Height,
            frame.PixelFormat,
            frame.ArrivalTick,
            frame.SequenceNumber);

        EnqueueRecordingFrame(frame);
        EnqueueFlashbackFrame(frame);
    }

    private void OnDualFrameArrived(
        IntPtr gpuTexture,
        int gpuSubresource,
        ReadOnlySpan<byte> frameData,
        int width,
        int height,
        long arrivalTick)
    {
        var sourceSequence = Interlocked.Increment(ref _videoFramesArrived) - 1;
        Interlocked.Exchange(ref _lastVideoFrameArrivedTick, Environment.TickCount64);
        RecordCaptureArrived(sourceSequence, arrivalTick, width, height, frameData.Length);

        var isP010 = Volatile.Read(ref _isP010);
        FirePixelFormatObserverOnce(isP010 ? "P010" : "NV12");

        var gpuEncoder = Volatile.Read(ref _gpuRecordingEncoder);
        if (gpuEncoder != null && gpuTexture != IntPtr.Zero)
        {
            EnqueueGpuRecordingFrame(gpuEncoder, gpuTexture, gpuSubresource, sourceSequence);
        }
        else
        {
            EnqueueRecordingFrame(frameData, width, height, isP010, sourceSequence);
        }

        if (gpuTexture != IntPtr.Zero)
        {
            EnqueueFlashbackGpuFrame(gpuTexture, gpuSubresource, sourceSequence);
        }
        else
        {
            EnqueueFlashbackFrame(frameData, width, height, isP010, sourceSequence);
        }

        var previewSink = Volatile.Read(ref _previewSink);
        if (!_previewSuppressed && previewSink != null)
        {
            var textureSubmitted = false;
            if (gpuTexture != IntPtr.Zero)
            {
                try
                {
                    var previewPresentId = Interlocked.Increment(ref _livePreviewPresentId);
                    var submitTick = Stopwatch.GetTimestamp();
                    previewSink.SubmitTexture(
                        gpuTexture,
                        gpuSubresource,
                        width,
                        height,
                        isP010,
                        PreviewFrameTracking.Default with
                        {
                            ArrivalTick = arrivalTick,
                            SourceSequenceNumber = sourceSequence,
                            PreviewPresentId = previewPresentId,
                            SchedulerSubmitTick = submitTick,
                        });
                    _frameLedger.RecordEvent(
                        sourceSequence,
                        FrameLedgerStage.PreviewEnqueued,
                        subsystem: "preview",
                        accepted: true);
                    textureSubmitted = true;
                    Interlocked.Exchange(ref _consecutiveTextureFailures, 0);
                    if (!frameData.IsEmpty)
                    {
                        TrackPreviewVisualFrame(
                            frameData,
                            width,
                            height,
                            isP010 ? PooledVideoPixelFormat.P010 : PooledVideoPixelFormat.Nv12,
                            arrivalTick,
                            sequenceNumber: sourceSequence);
                    }
                    else
                    {
                        MarkPreviewVisualCadenceUnavailable("d3d_texture_only");
                    }
                }
                catch (Exception ex)
                {
                    _frameLedger.RecordEvent(
                        sourceSequence,
                        FrameLedgerStage.PreviewEnqueued,
                        subsystem: "preview",
                        accepted: false,
                        reason: "texture_submit_exception");
                    Logger.Log($"UNIFIED_VIDEO_PREVIEW_TEXTURE_FAIL type={ex.GetType().Name} msg={ex.Message}");
                }
            }

            if (!textureSubmitted &&
                Volatile.Read(ref _strictPreviewTextureRequired))
            {
                var failures = Interlocked.Increment(ref _consecutiveTextureFailures);
                Interlocked.Increment(ref _videoFramesDropped);

                if (failures >= MaxConsecutiveTextureFailures)
                {
                    SignalFatalError(
                        new InvalidOperationException(
                            $"4K120 MJPG mode requires D3D preview textures, but texture delivery failed {failures} consecutive times for native_input='{_nativeInputFormat}' negotiated='{_negotiatedFormat}'."),
                        $"UNIFIED_VIDEO_PREVIEW_TEXTURE_REQUIRED consecutive={failures} " +
                        $"native_input='{_nativeInputFormat}' negotiated='{_negotiatedFormat}'");
                }
                else
                {
                    Logger.Log($"UNIFIED_VIDEO_PREVIEW_TEXTURE_GRACE consecutive={failures}/{MaxConsecutiveTextureFailures}");
                }
            }
            else if (!textureSubmitted && !frameData.IsEmpty)
            {
                SubmitPreviewRawFrame(previewSink, frameData, width, height, isP010, arrivalTick, sourceSequence);
            }
        }
    }

    private void RecordCaptureArrived(long sourceSequence, long arrivalTick, int width, int height, int compressedByteLength)
    {
        _frameLedger.RecordCaptureArrived(new FrameIdentity(
            SourceSequence: sourceSequence,
            CaptureArrivalQpc: arrivalTick,
            DeviceTimestamp100ns: null,
            InputFormat: Volatile.Read(ref _nativeInputFormat),
            Width: width,
            Height: height,
            FrameRateNominal: Volatile.Read(ref _fps),
            CompressedByteLength: compressedByteLength));
    }

    private void FirePixelFormatObserverOnce(string format)
    {
        if (Interlocked.CompareExchange(ref _pixelFormatObserverFired, 1, 0) != 0)
        {
            return;
        }

        Volatile.Read(ref _pixelFormatDetectedCallback)?.Invoke(format);
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

    public void SetPreviewSink(IPreviewFrameSink? sink)
    {
        Volatile.Write(ref _previewSink, sink);
    }

    public void SuppressPreviewSubmission()
    {
        // Flashback playback temporarily owns the preview renderer. Drain
        // pending live frames so an old live texture cannot flash over a
        // scrub/playback frame when presentation mode changes.
        _previewSuppressed = true;
        Volatile.Read(ref _mjpegPreviewJitterBuffer)?.ResetForPreviewSuppression();
        DropPendingPreviewFrames("live-preview-suppressed");
    }

    public void ResumePreviewSubmission()
    {
        // Drop before clearing suppression so the first resumed frame is a new
        // live source frame, not stale queue residue from the playback period.
        DropPendingPreviewFrames("live-preview-resumed");
        _previewSuppressed = false;
        Volatile.Read(ref _mjpegPreviewJitterBuffer)?.ReprimeAfterPreviewResume();
    }

    private void DropPendingPreviewFrames(string reason)
    {
        if (Volatile.Read(ref _previewSink) is not IPreviewFrameQueueControl queueControl)
        {
            return;
        }

        try
        {
            var dropped = queueControl.DropPendingFrames(reason);
            if (dropped > 0)
            {
                Logger.Log($"UNIFIED_VIDEO_PREVIEW_PENDING_DRAIN reason={reason} dropped={dropped}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"UNIFIED_VIDEO_PREVIEW_PENDING_DRAIN_WARN reason={reason} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void OnMjpegPipelinePreviewFrameDecoded(PooledVideoFrameLease frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        FirePixelFormatObserverOnce("NV12");
        _frameLedger.RecordEvent(
            frame.SequenceNumber,
            FrameLedgerStage.PreviewEnqueued,
            subsystem: "preview",
            accepted: true);

        var previewSink = Volatile.Read(ref _previewSink);
        var jitterBuffer = Volatile.Read(ref _mjpegPreviewJitterBuffer);
        if (_previewSuppressed || previewSink == null)
        {
            jitterBuffer?.Clear();
            frame.Dispose();
            return;
        }

        if (jitterBuffer != null)
        {
            jitterBuffer.Enqueue(frame);
            return;
        }

        PooledVideoFrameLease? ownedFrame = frame;
        try
        {
            var previewPresentId = Interlocked.Increment(ref _livePreviewPresentId);
            var submitTick = Stopwatch.GetTimestamp();
            previewSink.SubmitRawFrameLease(
                ownedFrame,
                isHdr: false,
                PreviewFrameTracking.Default with
                {
                    PreviewPresentId = previewPresentId,
                    SchedulerSubmitTick = submitTick,
                });
            ownedFrame = null;
        }
        finally
        {
            ownedFrame?.Dispose();
        }
    }

    private void SubmitPreviewFrameLease(
        IPreviewFrameSink previewSink,
        PooledVideoFrame frame,
        bool isP010,
        long sourceSequence)
    {
        try
        {
            // Visual cadence tracking compares consecutive frames; this runs on
            // the single-threaded capture read loop, so ordering holds.
            TrackPreviewVisualFrame(
                frame.Span,
                frame.Width,
                frame.Height,
                frame.PixelFormat,
                frame.ArrivalTick,
                sequenceNumber: sourceSequence);

            if (!frame.TryAddLease(out var lease))
            {
                Interlocked.Increment(ref _videoFramesDropped);
                _frameLedger.RecordEvent(
                    sourceSequence,
                    FrameLedgerStage.PreviewEnqueued,
                    subsystem: "preview",
                    byteDepth: frame.Length,
                    accepted: false,
                    reason: "lease_unavailable");
                return;
            }

            var ownedLease = lease;
            try
            {
                var previewPresentId = Interlocked.Increment(ref _livePreviewPresentId);
                var submitTick = Stopwatch.GetTimestamp();
                previewSink.SubmitRawFrameLease(
                    ownedLease!,
                    isHdr: isP010,
                    PreviewFrameTracking.Default with
                    {
                        ArrivalTick = frame.ArrivalTick,
                        SourceSequenceNumber = sourceSequence,
                        PreviewPresentId = previewPresentId,
                        SchedulerSubmitTick = submitTick,
                    });
                ownedLease = null;
            }
            finally
            {
                ownedLease?.Dispose();
            }

            _frameLedger.RecordEvent(
                sourceSequence,
                FrameLedgerStage.PreviewEnqueued,
                subsystem: "preview",
                byteDepth: frame.Length,
                accepted: true);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _videoFramesDropped);
            _frameLedger.RecordEvent(
                sourceSequence,
                FrameLedgerStage.PreviewEnqueued,
                subsystem: "preview",
                byteDepth: frame.Length,
                accepted: false,
                reason: "exception");
            Logger.Log($"UNIFIED_VIDEO_PREVIEW_FRAME_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private unsafe void SubmitPreviewRawFrame(
        IPreviewFrameSink previewSink,
        ReadOnlySpan<byte> frameData,
        int width,
        int height,
        bool isP010,
        long arrivalTick,
        long sourceSequence)
    {
        try
        {
            TrackPreviewVisualFrame(
                frameData,
                width,
                height,
                isP010 ? PooledVideoPixelFormat.P010 : PooledVideoPixelFormat.Nv12,
                arrivalTick,
                sequenceNumber: sourceSequence);
            fixed (byte* pointer = frameData)
            {
                var previewPresentId = Interlocked.Increment(ref _livePreviewPresentId);
                var submitTick = Stopwatch.GetTimestamp();
                previewSink.SubmitRawFrame(
                    (IntPtr)pointer,
                    frameData.Length,
                    width,
                    height,
                    isP010,
                    PreviewFrameTracking.Default with
                    {
                        ArrivalTick = arrivalTick,
                        SourceSequenceNumber = sourceSequence,
                        PreviewPresentId = previewPresentId,
                        SchedulerSubmitTick = submitTick,
                    });
            }
            _frameLedger.RecordEvent(
                sourceSequence,
                FrameLedgerStage.PreviewEnqueued,
                subsystem: "preview",
                byteDepth: frameData.Length,
                accepted: true);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _videoFramesDropped);
            _frameLedger.RecordEvent(
                sourceSequence,
                FrameLedgerStage.PreviewEnqueued,
                subsystem: "preview",
                byteDepth: frameData.Length,
                accepted: false,
                reason: "exception");
            Logger.Log($"UNIFIED_VIDEO_PREVIEW_FRAME_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void TrackPreviewVisualFrame(
        ReadOnlySpan<byte> frameData,
        int width,
        int height,
        PooledVideoPixelFormat pixelFormat,
        long arrivalTick,
        long sequenceNumber)
    {
        try
        {
            Interlocked.Exchange(ref _visualCadenceCpuDataUnavailable, 0);
            _visualCadenceTracker.RecordFrame(
                frameData,
                width,
                height,
                pixelFormat,
                timestampTick: arrivalTick);
            _visualCenterCadenceTracker.RecordFrame(
                frameData,
                width,
                height,
                pixelFormat,
                timestampTick: arrivalTick);
        }
        catch (Exception ex)
        {
            Logger.Log(
                $"UNIFIED_VIDEO_VISUAL_CADENCE_FAIL type={ex.GetType().Name} " +
                $"msg={ex.Message} width={width} height={height} fmt={pixelFormat} seq={sequenceNumber}");
        }
    }

    private void MarkPreviewVisualCadenceUnavailable(string reason)
    {
        if (Interlocked.CompareExchange(ref _visualCadenceCpuDataUnavailable, 1, 0) != 0)
        {
            return;
        }

        _visualCadenceTracker.Reset();
        _visualCenterCadenceTracker.Reset();
        Logger.Log($"UNIFIED_VIDEO_VISUAL_CADENCE_UNAVAILABLE reason={reason}");
    }

    private void RecordRecordingEnqueue(long sourceSequence, bool accepted, string? reason)
    {
        if (!accepted)
        {
            Interlocked.Increment(ref _recordingFramesRejected);
            if (string.Equals(reason, "queue_rejected", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref _recordingQueueRejectedFrames);
            }
        }

        _frameLedger.RecordEvent(
            sourceSequence,
            FrameLedgerStage.RecordingEnqueued,
            subsystem: "recording",
            accepted: accepted,
            reason: reason);
    }

    private void EnqueueRecordingFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)
    {
        if (!Volatile.Read(ref _recordingActive))
        {
            return;
        }

        var encoder = Volatile.Read(ref _recordingEncoder);
        if (encoder == null)
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
                RecordRecordingEnqueue(sourceSequence, accepted: false, reason: "frame_size_mismatch");
                Logger.Log(
                    "UNIFIED_VIDEO_FRAME_SIZE_MISMATCH " +
                    $"expected={expectedSize} actual={frameData.Length} width={width} height={height} isP010={isP010}");
                return;
            }

            var accepted = encoder is IRawVideoFrameTryEncoder tryEncoder
                ? tryEncoder.TryEnqueueRawVideoFrame(frameData, expectedSize)
                : TryLegacyRawVideoEnqueue(encoder, frameData, expectedSize);
            if (accepted)
            {
                Interlocked.Increment(ref _videoFramesWrittenToSink);
            }
            RecordRecordingEnqueue(sourceSequence, accepted, accepted ? null : "queue_rejected");
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _videoFramesDropped);
            RecordRecordingEnqueue(sourceSequence, accepted: false, reason: "exception");
            Logger.Log($"UNIFIED_VIDEO_RECORDING_FRAME_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void EnqueueRecordingFrame(PooledVideoFrame frame)
    {
        if (!Volatile.Read(ref _recordingActive))
        {
            return;
        }

        var encoder = Volatile.Read(ref _recordingEncoder);
        if (encoder == null)
        {
            return;
        }

        Interlocked.Increment(ref _recordingFramesDelivered);

        try
        {
            var isP010 = frame.PixelFormat == PooledVideoPixelFormat.P010;
            var expectedSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(frame.Width, frame.Height, isP010);
            if (frame.Length < expectedSize)
            {
                Interlocked.Increment(ref _videoFramesDropped);
                RecordRecordingEnqueue(frame.SequenceNumber, accepted: false, reason: "frame_size_mismatch");
                Logger.Log(
                    "UNIFIED_VIDEO_FRAME_SIZE_MISMATCH " +
                    $"expected={expectedSize} actual={frame.Length} width={frame.Width} height={frame.Height} isP010={isP010}");
                return;
            }

            if (encoder is IRawVideoFrameLeaseEncoder leaseEncoder &&
                frame.TryAddLease(out var lease))
            {
                try
                {
                    var accepted = leaseEncoder is IRawVideoFrameLeaseTryEncoder leaseTryEncoder
                        ? leaseTryEncoder.TryEnqueueRawVideoFrame(lease!)
                        : TryLegacyLeaseVideoEnqueue(leaseEncoder, lease!);
                    lease = null;
                    if (accepted)
                    {
                        Interlocked.Increment(ref _videoFramesWrittenToSink);
                    }
                    RecordRecordingEnqueue(frame.SequenceNumber, accepted, accepted ? null : "queue_rejected");
                }
                finally
                {
                    lease?.Dispose();
                }
            }
            else
            {
                var accepted = encoder is IRawVideoFrameTryEncoder tryEncoder
                    ? tryEncoder.TryEnqueueRawVideoFrame(frame.Memory.Span, expectedSize)
                    : TryLegacyRawVideoEnqueue(encoder, frame.Memory.Span, expectedSize);
                if (accepted)
                {
                    Interlocked.Increment(ref _videoFramesWrittenToSink);
                }
                RecordRecordingEnqueue(frame.SequenceNumber, accepted, accepted ? null : "queue_rejected");
            }
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _videoFramesDropped);
            RecordRecordingEnqueue(frame.SequenceNumber, accepted: false, reason: "exception");
            Logger.Log($"UNIFIED_VIDEO_RECORDING_FRAME_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void EnqueueGpuRecordingFrame(IGpuVideoFrameEncoder encoder, IntPtr texture, int subresource, long sourceSequence)
    {
        Interlocked.Increment(ref _recordingFramesDelivered);
        try
        {
            var accepted = encoder is IGpuVideoFrameTryEncoder tryEncoder
                ? tryEncoder.TryEnqueueGpuVideoFrame(texture, subresource)
                : TryLegacyGpuVideoEnqueue(encoder, texture, subresource);
            if (accepted)
            {
                Interlocked.Increment(ref _videoFramesWrittenToSink);
            }
            RecordRecordingEnqueue(sourceSequence, accepted, accepted ? null : "queue_rejected");
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _videoFramesDropped);
            RecordRecordingEnqueue(sourceSequence, accepted: false, reason: "exception");
            Logger.Log($"UNIFIED_VIDEO_GPU_RECORDING_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private static bool TryLegacyRawVideoEnqueue(IRawVideoFrameEncoder encoder, ReadOnlySpan<byte> frameData, int expectedSize)
    {
        encoder.EnqueueRawVideoFrame(frameData, expectedSize);
        return true;
    }

    private static bool TryLegacyLeaseVideoEnqueue(IRawVideoFrameLeaseEncoder encoder, PooledVideoFrameLease frame)
    {
        encoder.EnqueueRawVideoFrame(frame);
        return true;
    }

    private static bool TryLegacyGpuVideoEnqueue(IGpuVideoFrameEncoder encoder, IntPtr texture, int subresource)
    {
        encoder.EnqueueGpuVideoFrame(texture, subresource);
        return true;
    }

    private void RecordFlashbackEnqueue(long sourceSequence, bool accepted, string? reason)
    {
        _frameLedger.RecordEvent(
            sourceSequence,
            FrameLedgerStage.FlashbackEnqueued,
            subsystem: "flashback",
            accepted: accepted,
            reason: reason);
    }

    private void EnqueueFlashbackFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)
    {
        var sink = Volatile.Read(ref _flashbackSink);
        if (sink == null)
        {
            return;
        }

        try
        {
            var expectedSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(width, height, isP010);
            if (frameData.Length < expectedSize)
            {
                RecordFlashbackRecordingAccounting(sink, accepted: false, sourceSequence, "frame_size_mismatch");
                RecordFlashbackEnqueue(sourceSequence, accepted: false, reason: "frame_size_mismatch");
                return;
            }

            var accepted = sink.TryEnqueueRawVideoFrame(frameData, expectedSize);
            RecordFlashbackRecordingAccounting(sink, accepted, sourceSequence, accepted ? null : "queue_rejected");
            RecordFlashbackEnqueue(sourceSequence, accepted, accepted ? null : "queue_rejected");
        }
        catch (Exception ex)
        {
            RecordFlashbackRecordingAccounting(sink, accepted: false, sourceSequence, "exception");
            RecordFlashbackEnqueue(sourceSequence, accepted: false, reason: "exception");
            Logger.Log($"UNIFIED_VIDEO_FLASHBACK_FRAME_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void EnqueueFlashbackFrame(PooledVideoFrame frame)
    {
        var sink = Volatile.Read(ref _flashbackSink);
        if (sink == null)
        {
            return;
        }

        try
        {
            if (sink is IRawVideoFrameLeaseEncoder leaseEncoder &&
                frame.TryAddLease(out var lease))
            {
                try
                {
                    var accepted = leaseEncoder is IRawVideoFrameLeaseTryEncoder leaseTryEncoder
                        ? leaseTryEncoder.TryEnqueueRawVideoFrame(lease!)
                        : TryLegacyLeaseVideoEnqueue(leaseEncoder, lease!);
                    lease = null;
                    RecordFlashbackRecordingAccounting(sink, accepted, frame.SequenceNumber, accepted ? null : "queue_rejected");
                    RecordFlashbackEnqueue(frame.SequenceNumber, accepted, accepted ? null : "queue_rejected");
                }
                finally
                {
                    lease?.Dispose();
                }

                return;
            }

            var expectedSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(
                frame.Width,
                frame.Height,
                frame.PixelFormat == PooledVideoPixelFormat.P010);
            if (frame.Length < expectedSize)
            {
                RecordFlashbackRecordingAccounting(sink, accepted: false, frame.SequenceNumber, "frame_size_mismatch");
                RecordFlashbackEnqueue(frame.SequenceNumber, accepted: false, reason: "frame_size_mismatch");
                return;
            }

            var rawAccepted = sink.TryEnqueueRawVideoFrame(frame.Memory.Span, expectedSize);
            RecordFlashbackRecordingAccounting(sink, rawAccepted, frame.SequenceNumber, rawAccepted ? null : "queue_rejected");
            RecordFlashbackEnqueue(frame.SequenceNumber, rawAccepted, rawAccepted ? null : "queue_rejected");
        }
        catch (Exception ex)
        {
            RecordFlashbackRecordingAccounting(sink, accepted: false, frame.SequenceNumber, "exception");
            RecordFlashbackEnqueue(frame.SequenceNumber, accepted: false, reason: "exception");
            Logger.Log($"UNIFIED_VIDEO_FLASHBACK_FRAME_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void EnqueueFlashbackGpuFrame(IntPtr texture, int subresource, long sourceSequence)
    {
        var sink = Volatile.Read(ref _flashbackSink);
        if (sink == null)
        {
            return;
        }

        try
        {
            var accepted = sink.TryEnqueueGpuVideoFrame(texture, subresource);
            RecordFlashbackRecordingAccounting(sink, accepted, sourceSequence, accepted ? null : "queue_rejected");
            RecordFlashbackEnqueue(sourceSequence, accepted, accepted ? null : "queue_rejected");
        }
        catch (Exception ex)
        {
            RecordFlashbackRecordingAccounting(sink, accepted: false, sourceSequence, "exception");
            RecordFlashbackEnqueue(sourceSequence, accepted: false, reason: "exception");
            Logger.Log($"UNIFIED_VIDEO_FLASHBACK_GPU_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void RecordFlashbackRecordingAccounting(
        FlashbackEncoderSink sink,
        bool accepted,
        long sourceSequence,
        string? rejectedReason)
    {
        if (!Volatile.Read(ref _flashbackRecordingAccountingActive) ||
            !sink.IsRecordingActive)
        {
            return;
        }

        Interlocked.Increment(ref _recordingFramesDelivered);
        if (accepted)
        {
            TrackFlashbackRecordingAcceptedSequence(sourceSequence);
            Interlocked.Increment(ref _videoFramesWrittenToSink);
            return;
        }

        Interlocked.Increment(ref _recordingFramesRejected);
        if (string.Equals(rejectedReason, "queue_rejected", StringComparison.Ordinal))
        {
            Interlocked.Increment(ref _recordingQueueRejectedFrames);
        }
    }

    private void TrackFlashbackRecordingAcceptedSequence(long sourceSequence)
    {
        while (true)
        {
            var last = Interlocked.Read(ref _flashbackRecordingLastAcceptedSequence);
            if (sourceSequence <= last)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _flashbackRecordingLastAcceptedSequence, sourceSequence, last) == last)
            {
                if (last >= 0 && sourceSequence > last + 1)
                {
                    Interlocked.Add(ref _flashbackRecordingSequenceGaps, sourceSequence - last - 1);
                }

                return;
            }
        }
    }

}

// Ring buffer of recent per-frame routing decisions. It gives automation a
// compact "flight recorder" for one source sequence across capture, preview,
// Flashback, and recording without writing a log line for every frame forever.
internal sealed class FrameLedger
{
    public const int DefaultCapacity = 4096;

    private readonly object _sync = new();
    private readonly EventEntry[] _entries;
    private int _nextIndex;
    private int _count;
    private long _totalEventsRecorded;
    private long _eventsDroppedByRetention;

    public FrameLedger(int capacity = DefaultCapacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Frame ledger capacity must be positive.");
        }

        _entries = new EventEntry[capacity];
    }

    public int Capacity => _entries.Length;

    public void Reset()
    {
        lock (_sync)
        {
            Array.Clear(_entries);
            _nextIndex = 0;
            _count = 0;
            _totalEventsRecorded = 0;
            _eventsDroppedByRetention = 0;
        }
    }

    public void RecordCaptureArrived(FrameIdentity identity, string subsystem = "capture")
    {
        Record(
            sourceSequence: identity.SourceSequence,
            stage: FrameLedgerStage.CaptureArrived,
            qpcTimestamp: identity.CaptureArrivalQpc,
            subsystem: subsystem,
            queueDepth: null,
            byteDepth: identity.CompressedByteLength > 0 ? (long?)identity.CompressedByteLength : null,
            accepted: true,
            reason: null,
            identity: identity);
    }

    public void RecordEvent(
        long sourceSequence,
        FrameLedgerStage stage,
        long qpcTimestamp = 0,
        string subsystem = "",
        int? queueDepth = null,
        long? byteDepth = null,
        bool? accepted = null,
        string? reason = null)
    {
        Record(
            sourceSequence,
            stage,
            qpcTimestamp == 0 ? Stopwatch.GetTimestamp() : qpcTimestamp,
            string.IsNullOrWhiteSpace(subsystem) ? stage.ToString() : subsystem,
            queueDepth,
            byteDepth,
            accepted,
            reason,
            identity: null);
    }

    public FrameLedgerSummary GetSummary(int maxEvents = 64)
    {
        if (maxEvents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEvents), "Frame ledger max event count cannot be negative.");
        }

        lock (_sync)
        {
            if (_count == 0 || maxEvents == 0)
            {
                return new FrameLedgerSummary(
                    Capacity,
                    _totalEventsRecorded,
                    _eventsDroppedByRetention,
                    RecentEventCount: 0,
                    OldestSourceSequence: null,
                    NewestSourceSequence: null,
                    RecentEvents: Array.Empty<FrameLedgerEventSnapshot>());
            }

            var eventCount = Math.Min(_count, maxEvents);
            var firstIndex = GetChronologicalIndex(_count - eventCount);
            var events = new FrameLedgerEventSnapshot[eventCount];
            long? oldest = null;
            long? newest = null;
            for (var i = 0; i < eventCount; i++)
            {
                var entry = _entries[(firstIndex + i) % Capacity];
                events[i] = entry.ToSnapshot();
                oldest ??= entry.SourceSequence;
                newest = entry.SourceSequence;
            }

            return new FrameLedgerSummary(
                Capacity,
                _totalEventsRecorded,
                _eventsDroppedByRetention,
                eventCount,
                oldest,
                newest,
                events);
        }
    }

    private void Record(
        long sourceSequence,
        FrameLedgerStage stage,
        long qpcTimestamp,
        string subsystem,
        int? queueDepth,
        long? byteDepth,
        bool? accepted,
        string? reason,
        FrameIdentity? identity)
    {
        lock (_sync)
        {
            _entries[_nextIndex] = new EventEntry(
                sourceSequence,
                stage,
                qpcTimestamp,
                subsystem,
                queueDepth,
                byteDepth,
                accepted,
                reason,
                identity);

            _nextIndex = (_nextIndex + 1) % Capacity;
            _totalEventsRecorded++;
            if (_count < Capacity)
            {
                _count++;
            }
            else
            {
                _eventsDroppedByRetention++;
            }
        }
    }

    private int GetChronologicalIndex(int offset)
    {
        var start = _count == Capacity ? _nextIndex : 0;
        return (start + offset) % Capacity;
    }

    private readonly struct EventEntry
    {
        public EventEntry(
            long sourceSequence,
            FrameLedgerStage stage,
            long qpcTimestamp,
            string subsystem,
            int? queueDepth,
            long? byteDepth,
            bool? accepted,
            string? reason,
            FrameIdentity? identity)
        {
            SourceSequence = sourceSequence;
            Stage = stage;
            QpcTimestamp = qpcTimestamp;
            Subsystem = subsystem;
            QueueDepth = queueDepth;
            ByteDepth = byteDepth;
            Accepted = accepted;
            Reason = reason;
            Identity = identity;
        }

        public long SourceSequence { get; }
        public FrameLedgerStage Stage { get; }
        public long QpcTimestamp { get; }
        public string Subsystem { get; }
        public int? QueueDepth { get; }
        public long? ByteDepth { get; }
        public bool? Accepted { get; }
        public string? Reason { get; }
        public FrameIdentity? Identity { get; }

        public FrameLedgerEventSnapshot ToSnapshot()
            => new(
                SourceSequence,
                Stage,
                QpcTimestamp,
                Subsystem,
                QueueDepth,
                ByteDepth,
                Accepted,
                Reason,
                Identity);
    }
}
