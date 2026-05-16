using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Decides how the Flashback preview backend continues after recording finalize:
// preserve recovery state, apply deferred settings, or cycle the DVR buffer.
public partial class CaptureService
{
    private async Task<OperationCanceledException?> ReconcileFlashbackBackendAfterRecordingFinalizeAsync(
        FinalizeResult fbResult,
        OperationCanceledException? cancellationException,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!fbResult.Succeeded)
            {
                var hadPendingFlashbackSettingsChange = _pendingFlashbackSettingsChange;
                _pendingFlashbackSettingsChange = false;
                _flashbackBackend.PreserveRecoverySegments("recording_finalize_failed");
                Logger.Log(
                    "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING_DEFERRED " +
                    $"reason=recording_finalize_failed pending_settings={hadPendingFlashbackSettingsChange}");
            }
            else if (_pendingFlashbackSettingsChange)
            {
                _pendingFlashbackSettingsChange = false;
                Logger.Log("FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING");
                await DisposeFlashbackPreviewBackendAsync(cancellationToken, purgeSegments: true).ConfigureAwait(false);
                if (_flashbackEnabled && _unifiedVideoCapture != null && _currentSettings != null)
                {
                    await EnsureFlashbackPreviewBackendAsync(_unifiedVideoCapture, _currentSettings, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                await CycleFlashbackBufferAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancellationException ??= new OperationCanceledException(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_BUFFER_CYCLE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
            RecordLastFlashbackFailure(ex);
            _flashbackBackend.PreserveRecoverySegments("buffer_cycle_failed");
            BeginFlashbackBackendCleanup(ex);
        }

        return cancellationException;
    }
}
