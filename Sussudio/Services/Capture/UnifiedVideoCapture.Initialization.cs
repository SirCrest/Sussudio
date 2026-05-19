using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;

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
        var useExternalMjpegDecode = ShouldUseExternalMjpegDecode(
            useMjpegHighFrameRateMode,
            requireP010,
            requestedPixelFormat);
        var mjpegPipeline = CreateExternalMjpegPipelineIfNeeded(
            useExternalMjpegDecode,
            mjpegDecoderCount,
            width,
            height);

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
}
