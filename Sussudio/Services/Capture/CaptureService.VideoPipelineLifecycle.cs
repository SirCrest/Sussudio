using System;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private IPreviewFrameSink? _previewFrameSink
    {
        get => _videoPipeline.PreviewFrameSink;
        set => _videoPipeline.PreviewFrameSink = value;
    }

    public int GetNegotiatedVideoWidth() => _videoPipeline.NegotiatedVideoWidth;
    public int GetNegotiatedVideoHeight() => _videoPipeline.NegotiatedVideoHeight;
    public double GetNegotiatedVideoFps() => _videoPipeline.NegotiatedVideoFps;

    internal void SetPreviewFrameSink(IPreviewFrameSink? sink)
    {
        var controller = _flashbackPlaybackController;
        if (sink == null && controller is { IsDisposed: false, IsInitialized: true })
        {
            controller.PrepareForPreviewDetach();
        }

        _videoPipeline.SetPreviewFrameSink(sink);
        var unifiedVideoCapture = _unifiedVideoCapture;
        TryApplySharedPreviewDevice(unifiedVideoCapture, sink);
        // Late-initialize playback controller if it was created before the renderer
        if (controller is { IsDisposed: false, IsInitialized: false } && sink != null && unifiedVideoCapture != null)
        {
            controller.Initialize(sink, unifiedVideoCapture, _wasapiAudioPlayback, _wasapiAudioCapture);
            Logger.Log("FLASHBACK_PLAYBACK_LATE_INIT via SetPreviewFrameSink");
        }
        else if (controller is { IsDisposed: false, IsInitialized: true })
        {
            controller.UpdatePreviewComponents(sink, unifiedVideoCapture);
        }
    }

    private void CacheMjpegTimingMetrics(UnifiedVideoCapture? unifiedVideoCapture)
    {
        _videoPipeline.CacheMjpegTimingMetrics(unifiedVideoCapture);
    }

    private void ResetCachedMjpegTimingMetrics()
    {
        _videoPipeline.ResetCachedMjpegTimingMetrics();
    }

    internal ParallelMjpegDecodePipeline.PipelineTimingMetrics? GetMjpegPipelineTimingDetails()
    {
        return _videoPipeline.GetMjpegPipelineTimingDetails();
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
