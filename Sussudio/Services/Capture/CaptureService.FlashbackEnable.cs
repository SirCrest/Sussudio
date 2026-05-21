using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Capture;

// Flashback enable/disable transition gate.
public partial class CaptureService
{
    public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CurrentSessionState, async transitionToken =>
        {
            if (_isRecording && IsFlashbackRecordingBackendActive() && !enabled)
            {
                Logger.Log("FLASHBACK_DISABLE_BLOCKED reason=recording_active");
                throw new InvalidOperationException("Cannot disable Flashback while Flashback recording is active.");
            }

            if (_flashbackEnabled == enabled)
            {
                if (enabled && (_flashbackBackend.Sink != null || _isRecording))
                {
                    return;
                }

                if (!enabled && !_flashbackBackend.HasAnyResource)
                {
                    return;
                }
            }

            _flashbackEnabled = enabled;
            if (!enabled)
            {
                _pendingFlashbackEnableAfterRecording = false;
                await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
                if (!_isVideoPreviewActive && !_isAudioPreviewActive && !_isRecording)
                {
                    await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: false).ConfigureAwait(false);
                }
                return;
            }

            if (_isRecording)
            {
                _pendingFlashbackEnableAfterRecording = true;
                Logger.Log("FLASHBACK_ENABLE_DEFERRED reason=recording_active");
                return;
            }

            _pendingFlashbackEnableAfterRecording = false;
            var unifiedVideoCapture = _videoPipeline.Capture;
            if (unifiedVideoCapture != null && _currentSettings != null)
            {
                try
                {
                    await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, transitionToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)
                {
                    _flashbackEnabled = false;
                    _pendingFlashbackEnableAfterRecording = false;
                    if (_flashbackBackend.HasAnyResource)
                    {
                        await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true).ConfigureAwait(false);
                    }
                    Logger.Log($"FLASHBACK_ENABLE_IMMEDIATE_CANCELLED type={ex.GetType().Name} error='{ex.Message}'");
                    throw;
                }
                catch (Exception ex)
                {
                    _flashbackEnabled = false;
                    _pendingFlashbackEnableAfterRecording = false;
                    if (_flashbackBackend.HasAnyResource)
                    {
                        await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true).ConfigureAwait(false);
                    }
                    Logger.Log($"FLASHBACK_ENABLE_IMMEDIATE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
                    throw;
                }
            }
        }, cancellationToken);
}
