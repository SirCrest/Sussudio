using System;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private IPreviewFrameSink? _previewFrameSink;
    private UnifiedVideoCapture.MjpegPipelineTimingMetrics _lastMjpegPipelineTimingMetrics;
    private ParallelMjpegDecodePipeline.PipelineTimingMetrics? _lastFullMjpegPipelineTimingMetrics;

    public int GetNegotiatedVideoWidth() => _unifiedVideoCapture?.Width ?? 0;
    public int GetNegotiatedVideoHeight() => _unifiedVideoCapture?.Height ?? 0;
    public double GetNegotiatedVideoFps() => _unifiedVideoCapture?.Fps ?? 0;

    internal void SetPreviewFrameSink(IPreviewFrameSink? sink)
    {
        var controller = _flashbackPlaybackController;
        if (sink == null && controller is { IsDisposed: false, IsInitialized: true })
        {
            controller.PrepareForPreviewDetach();
        }

        _previewFrameSink = sink;
        _unifiedVideoCapture?.SetPreviewSink(sink);
        TryApplySharedPreviewDevice(_unifiedVideoCapture, sink);

        // Late-initialize playback controller if it was created before the renderer
        if (controller is { IsDisposed: false, IsInitialized: false } && sink != null && _unifiedVideoCapture != null)
        {
            controller.Initialize(sink, _unifiedVideoCapture, _wasapiAudioPlayback, _wasapiAudioCapture);
            Logger.Log("FLASHBACK_PLAYBACK_LATE_INIT via SetPreviewFrameSink");
        }
        else if (controller is { IsDisposed: false, IsInitialized: true })
        {
            controller.UpdatePreviewComponents(sink, _unifiedVideoCapture);
        }
    }

    private void CacheMjpegTimingMetrics(UnifiedVideoCapture? unifiedVideoCapture)
    {
        if (unifiedVideoCapture == null)
        {
            return;
        }

        var timingSnapshot = unifiedVideoCapture.GetMjpegPipelineTimingSnapshot();
        _lastMjpegPipelineTimingMetrics = timingSnapshot.Summary;
        _lastFullMjpegPipelineTimingMetrics = timingSnapshot.Details;
    }

    private void ResetCachedMjpegTimingMetrics()
    {
        _lastMjpegPipelineTimingMetrics = default;
        _lastFullMjpegPipelineTimingMetrics = null;
    }

    internal ParallelMjpegDecodePipeline.PipelineTimingMetrics? GetMjpegPipelineTimingDetails()
    {
        return _unifiedVideoCapture?.GetFullMjpegPipelineTimingMetrics() ?? _lastFullMjpegPipelineTimingMetrics;
    }

    private void AttachUnifiedVideoCapture(UnifiedVideoCapture unifiedVideoCapture)
    {
        unifiedVideoCapture.FatalErrorOccurred += OnUnifiedVideoCaptureFatalError;
        unifiedVideoCapture.SetPixelFormatDetectedCallback(fmt => RecordObservedPixelFormat(fmt));
    }

    private void DetachUnifiedVideoCapture(UnifiedVideoCapture? unifiedVideoCapture)
    {
        if (unifiedVideoCapture == null)
        {
            return;
        }

        unifiedVideoCapture.FatalErrorOccurred -= OnUnifiedVideoCaptureFatalError;
        unifiedVideoCapture.SetPixelFormatDetectedCallback(null);
    }

    private void TryApplySharedPreviewDevice(UnifiedVideoCapture? capture, IPreviewFrameSink? sink)
    {
        if (capture == null || sink is not D3D11PreviewRenderer renderer)
        {
            return;
        }

        // MJPEG (JPEG) uses full-range YCbCr (0-255). Tell the VP to use
        // YcbcrFullG22LeftP709 instead of the default studio-range color space.
        renderer.FullRangeInput = capture.IsHighFrameRateMjpegMode;

        var d3dManager = capture.D3DManager;
        if (d3dManager == null)
        {
            return;
        }

        if (!d3dManager.TryCreateDeviceReference(out var sharedDevice, out var reason) || sharedDevice == null)
        {
            Logger.Log($"UNIFIED_VIDEO_SHARED_DEVICE_APPLY_SKIP reason={reason}");
            return;
        }

        try
        {
            renderer.SetSharedDevice(sharedDevice);
        }
        catch (Exception ex)
        {
            Logger.Log($"UNIFIED_VIDEO_SHARED_DEVICE_APPLY_WARN type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
        }
        finally
        {
            sharedDevice.Dispose();
        }
    }
}
