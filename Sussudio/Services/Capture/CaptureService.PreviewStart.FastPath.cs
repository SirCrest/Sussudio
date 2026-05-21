using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private async Task<bool> TryStartPreviewFromRetainedPipelineAsync(
        CaptureSettings settings,
        CancellationToken transitionToken)
    {
        var unifiedVideoCapture = _unifiedVideoCapture;
        if (unifiedVideoCapture == null ||
            (!_isRecording && !_flashbackEnabled))
        {
            return false;
        }

        // Fast-path: the capture pipeline is already running (recording active, or
        // flashback backend kept alive across a prior preview toggle). Just reattach
        // the preview renderer: no device re-init, no flashback restart.
        if (_flashbackSink?.IsP010 is bool sinkIsP010 &&
            sinkIsP010 != unifiedVideoCapture.IsP010)
        {
            Logger.Log(
                $"FLASHBACK_FAST_PATH_FORMAT_MISMATCH " +
                $"existing_p010={sinkIsP010} requested_p010={unifiedVideoCapture.IsP010}");
            throw new InvalidOperationException(
                $"Flashback fast path: pixel-format mismatch â€” sink was built for " +
                $"{(sinkIsP010 ? "P010" : "NV12")} but UVC session negotiated " +
                $"{(unifiedVideoCapture.IsP010 ? "P010" : "NV12")}. " +
                "Rebuild the flashback backend with the correct format.");
        }

        Logger.Log($"PREVIEW_START fast_path=1 recording={_isRecording} flashback_alive={_flashbackSink != null}");
        unifiedVideoCapture.SetPreviewSink(_previewFrameSink);
        TryApplySharedPreviewDevice(unifiedVideoCapture, _previewFrameSink);
        if (!_isRecording && _flashbackEnabled && _flashbackSink == null)
        {
            await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, transitionToken).ConfigureAwait(false);
        }
        await EnsureFlashbackAudioInputsAsync(settings, transitionToken, "preview_fast_path").ConfigureAwait(false);
        _isVideoPreviewActive = true;
        // Telemetry may have been stopped via a recording-stop path while preview
        // was off; StartTelemetryPoll is idempotent (stops any prior timer first).
        StartTelemetryPoll();
        StatusChanged?.Invoke(this, "Preview started");
        return true;
    }
}
