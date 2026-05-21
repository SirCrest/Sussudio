using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Flashback recording-format changes and buffer-cycle rollback.
public partial class CaptureService
{
    /// <summary>
    /// Updates the recording format and cycles the flashback encoder so the buffer
    /// uses the new codec. No-op if not previewing or if a recording is active.
    /// </summary>
    public Task UpdateRecordingFormatAsync(RecordingFormat format, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CurrentSessionState, async transitionToken =>
        {
            if (_currentSettings == null || format == _currentSettings.Format)
                return;

            var previousSettings = CloneCaptureSettings(_currentSettings);
            if (_isRecording)
            {
                Logger.Log($"FLASHBACK_FORMAT_CHANGE_BLOCKED reason=recording_active format={format}");
                _currentSettings.Format = format;
                if (IsFlashbackRecordingBackendActive())
                    _pendingFlashbackSettingsChange = true;
                return;
            }

            _currentSettings.Format = format;

            var cycleFailed = false;
            if (_flashbackBackend.Sink != null)
            {
                try
                {
                    await CycleFlashbackBufferAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)
                {
                    Logger.Log($"FLASHBACK_FORMAT_CHANGE_CYCLE_CANCELLED format={format} type={ex.GetType().Name} error='{ex.Message}'");
                    throw;
                }
                catch (Exception ex)
                {
                    cycleFailed = true;
                    Logger.Log($"FLASHBACK_FORMAT_CHANGE_CYCLE_FAIL format={format} type={ex.GetType().Name} error='{ex.Message}'");
                }
            }

            if (!cycleFailed)
            {
                Logger.Log($"FLASHBACK_FORMAT_CHANGE_OK format={format}");
            }
            else
            {
                _currentSettings = previousSettings;
                Logger.Log($"FLASHBACK_FORMAT_CHANGE_ROLLBACK format={format} restored={_currentSettings.Format}");
            }
        }, cancellationToken);
}
