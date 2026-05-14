using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

internal sealed partial class UnifiedVideoCapture
{
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

        // 4K120 MJPEG is compressed on the USB wire. In that mode the source
        // reader must hand compressed samples to our decoder instead of trying
        // to expose D3D textures directly from Media Foundation.
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
                    OnMjpegPipelineFrameEmitted,
                    OnMjpegPipelineFatalError,
                    OnMjpegPipelinePreviewFrameDecoded);
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
            var previewJitterFps = capture.Fps > 0 ? capture.Fps : fps;
            Volatile.Write(
                ref _mjpegPreviewJitterBuffer,
                new MjpegPreviewJitterBuffer(
                    previewJitterFps,
                    () => Volatile.Read(ref _previewSink),
                    () => _previewSuppressed,
                    previewFrameProbe: null,
                    targetDepth: 3));
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
                var jitterBuffer = Interlocked.Exchange(ref _mjpegPreviewJitterBuffer, null);
                jitterBuffer?.Dispose();

                if (!mjpegPipelineToStop.TryStop(TimeSpan.FromSeconds(5), out var failureReason))
                {
                    var stopException = new InvalidOperationException(
                        $"CPU MJPEG pipeline stop did not quiesce cleanly: {failureReason ?? "unknown"}");
                    SignalFatalError(
                        stopException,
                        $"UNIFIED_VIDEO_MJPEG_STOP_FAIL reason='{failureReason ?? "unknown"}'");
                    throw stopException;
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

        mjpegPipeline?.Dispose();
        jitterBuffer?.Dispose();
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

    private void OnMjpegPipelineFatalError(Exception ex)
    {
        SignalFatalError(
            ex,
            $"UNIFIED_VIDEO_FATAL_MJPEG_ERROR type={ex.GetType().Name} msg={ex.Message}");
    }
}
