using System;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private async Task<UnifiedVideoCapture> PrepareLibAvRecordingVideoCaptureAsync(
        CaptureSettings settings,
        RecordingStartRollbackState rollback,
        uint effectiveWidth,
        uint effectiveHeight,
        double effectiveFrameRate,
        bool requireP010,
        bool useMjpegHighFrameRateMode)
    {
        var unifiedVideoCapture = _unifiedVideoCapture;
        if (unifiedVideoCapture == null)
        {
            rollback.OwnedUnifiedVideoCapture = new UnifiedVideoCapture();
            AttachUnifiedVideoCapture(rollback.OwnedUnifiedVideoCapture);
            await rollback.OwnedUnifiedVideoCapture.InitializeAsync(
                _currentDevice!.Id,
                (int)effectiveWidth,
                (int)effectiveHeight,
                effectiveFrameRate,
                requireP010,
                settings.RequestedPixelFormat,
                useMjpegHighFrameRateMode,
                settings.MjpegDecoderCount).ConfigureAwait(false);
            rollback.OwnedUnifiedVideoCapture.SetPreviewSink(_isVideoPreviewActive ? _previewFrameSink : null);
            TryApplySharedPreviewDevice(rollback.OwnedUnifiedVideoCapture, _isVideoPreviewActive ? _previewFrameSink : null);
            unifiedVideoCapture = rollback.OwnedUnifiedVideoCapture;
            _videoPipeline.InstallCapture(rollback.OwnedUnifiedVideoCapture);
        }
        else if (unifiedVideoCapture.IsP010 != requireP010)
        {
            throw new InvalidOperationException(
                $"Recording requires {(requireP010 ? "P010" : "NV12")}, but the active source-reader session negotiated {(unifiedVideoCapture.IsP010 ? "P010" : "NV12")}.");
        }
        else if (unifiedVideoCapture.IsHighFrameRateMjpegMode != useMjpegHighFrameRateMode)
        {
            throw new InvalidOperationException(
                $"Recording requested mjpeg_hfr={useMjpegHighFrameRateMode}, but the active preview session is mjpeg_hfr={unifiedVideoCapture.IsHighFrameRateMjpegMode}.");
        }

        rollback.RecordingVideoCapture = unifiedVideoCapture;
        TryApplySharedPreviewDevice(unifiedVideoCapture, _isVideoPreviewActive ? _previewFrameSink : null);
        return unifiedVideoCapture;
    }
}
