using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Live-signal ViewModel property projection from capture runtime snapshots.
/// </summary>
public partial class MainViewModel
{
    private void UpdateLiveCaptureInfo(CaptureRuntimeSnapshot? runtimeSnapshot = null)
    {
        var runtime = runtimeSnapshot ?? _captureService.GetRuntimeSnapshot();
        IsAudioPreviewActive = runtime.IsAudioPreviewActive;

        var liveSignalText = LiveSignalTextPresentationBuilder.Build(
            runtime,
            _captureService.EncoderCodecName,
            LiveInfoUnavailable);
        LiveResolution = liveSignalText.Resolution;
        LiveFrameRate = liveSignalText.FrameRate;
        LivePixelFormat = liveSignalText.PixelFormat;
    }

    private void ResetLiveCaptureInfo()
    {
        IsAudioPreviewActive = false;
        LiveResolution = LiveInfoUnavailable;
        LiveFrameRate = LiveInfoUnavailable;
        LivePixelFormat = LiveInfoUnavailable;
    }

    partial void OnIsPreviewingChanged(bool value)
    {
        if (!value && !IsRecording)
        {
            ResetLiveCaptureInfo();
        }
    }
}
