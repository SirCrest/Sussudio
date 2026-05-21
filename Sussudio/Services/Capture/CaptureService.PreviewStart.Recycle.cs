using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private async Task RecyclePreviewPipelineForStartAsync(
        CaptureSettings settings,
        bool flashbackBackendSettingsChanged,
        CancellationToken transitionToken)
    {
        var unifiedVideoCapture = _videoPipeline.Capture;
        if (unifiedVideoCapture != null &&
            !_isRecording &&
            !CanReuseVideoCaptureForPreview(unifiedVideoCapture, settings))
        {
            Logger.Log("PREVIEW_START recycle_pipeline=1 reason=settings_changed");
            await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: true).ConfigureAwait(false);
        }

        unifiedVideoCapture = _videoPipeline.Capture;
        if (unifiedVideoCapture != null &&
            !_isRecording &&
            !_flashbackEnabled)
        {
            Logger.Log("PREVIEW_START recycle_pipeline=1 reason=flashback_disabled");
            await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: false).ConfigureAwait(false);
        }

        unifiedVideoCapture = _videoPipeline.Capture;
        if (unifiedVideoCapture != null &&
            !_isRecording &&
            _flashbackBackend.Sink != null &&
            flashbackBackendSettingsChanged)
        {
            Logger.Log("PREVIEW_START recycle_flashback=1 reason=flashback_settings_changed");
            await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
        }
    }
}
