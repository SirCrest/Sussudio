using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Capture;

// Restores live-preview features that standard LibAv recording temporarily
// displaced, while preserving the finalizer's delayed-cancellation contract.
public partial class CaptureService
{
    private async Task<OperationCanceledException?> RestoreLibAvPreviewFeaturesAfterRecordingAsync(
        OperationCanceledException? cancellationException,
        CancellationToken cancellationToken)
    {
        cancellationException = await RestorePendingFlashbackEnableAfterLibAvRecordingAsync(
            cancellationException,
            cancellationToken).ConfigureAwait(false);

        return await RestartStandardMicrophoneMonitorAfterLibAvRecordingAsync(
            cancellationException,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<OperationCanceledException?> RestorePendingFlashbackEnableAfterLibAvRecordingAsync(
        OperationCanceledException? cancellationException,
        CancellationToken cancellationToken)
    {
        if (!_pendingFlashbackEnableAfterRecording)
        {
            return cancellationException;
        }

        _pendingFlashbackEnableAfterRecording = false;
        var unifiedVideoCapture = _videoPipeline.Capture;
        var settings = _currentSettings;
        if (_flashbackEnabled && _isVideoPreviewActive && unifiedVideoCapture != null && settings != null)
        {
            try
            {
                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancellationException ??= new OperationCanceledException(cancellationToken);
                _flashbackEnabled = false;
                _pendingFlashbackEnableAfterRecording = false;
                if (_flashbackBackend.HasAnyResource)
                {
                    await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true).ConfigureAwait(false);
                }

                Logger.Log("FLASHBACK_ENABLE_AFTER_RECORDING_CANCELLED");
            }
            catch (Exception ex)
            {
                _flashbackEnabled = false;
                _pendingFlashbackEnableAfterRecording = false;
                if (_flashbackBackend.HasAnyResource)
                {
                    await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true).ConfigureAwait(false);
                }

                Logger.Log($"FLASHBACK_ENABLE_AFTER_RECORDING_FAIL type={ex.GetType().Name} error='{ex.Message}'");
            }
        }

        return cancellationException;
    }

    private async Task<OperationCanceledException?> RestartStandardMicrophoneMonitorAfterLibAvRecordingAsync(
        OperationCanceledException? cancellationException,
        CancellationToken cancellationToken)
    {
        try
        {
            await RestartMicrophoneMonitorAfterRecordingAsync(
                new MicrophoneMonitorRestartOptions(
                    OnlyWhenMissing: false,
                    FlashbackAttachReason: "mic_monitor_restart",
                    RestartLogEvent: "MIC_MONITOR_RESTART",
                    DisposeWarningEvent: "MIC_MONITOR_RESTART_DISPOSE_WARN"),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancellationException ??= new OperationCanceledException(cancellationToken);
        }
        catch (Exception micEx)
        {
            Logger.Log("Mic monitor restart failed (non-fatal): " + micEx.Message);
        }

        return cancellationException;
    }
}
