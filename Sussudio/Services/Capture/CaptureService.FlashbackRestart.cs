using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Flashback explicit restart transitions and committed rebuild orchestration.
public partial class CaptureService
{
    /// <summary>
    /// Tears down the running flashback encoder and buffer, then rebuilds
    /// with current settings. Purges all existing segments because encoding
    /// parameters (bitrate, codec, etc.) may have changed.
    /// </summary>
    public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(CurrentSessionState, async transitionToken =>
        {
            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                Logger.Log("FLASHBACK_RESTART_BLOCKED reason=recording_active");
                throw new InvalidOperationException("Cannot restart Flashback while Flashback recording is active.");
            }

            await RestartFlashbackCoreAsync(transitionToken).ConfigureAwait(false);
        }, cancellationToken);

    public Task RestartFlashbackAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return RunTransitionAsync(CurrentSessionState, async transitionToken =>
        {
            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                Logger.Log("FLASHBACK_RESTART_BLOCKED reason=recording_active");
                throw new InvalidOperationException("Cannot restart Flashback while Flashback recording is active.");
            }

            UpdateEncodingSettings(settings);
            await RestartFlashbackCoreAsync(transitionToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    private async Task RestartFlashbackCoreAsync(CancellationToken cancellationToken)
    {
        await DisposeFlashbackPreviewBackendAsync(cancellationToken, purgeSegments: true).ConfigureAwait(false);

        var committedRestartToken = CancellationToken.None;
        var unifiedVideoCapture = _videoPipeline.Capture;
        var settings = _currentSettings;
        if (!_flashbackEnabled || unifiedVideoCapture == null || settings == null)
        {
            Logger.Log($"FLASHBACK_RESTART_TEARDOWN_ONLY enabled={_flashbackEnabled} capture={unifiedVideoCapture != null} settings={settings != null}");
            return;
        }

        await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, committedRestartToken).ConfigureAwait(false);
        Logger.Log("FLASHBACK_RESTART_OK");
        cancellationToken.ThrowIfCancellationRequested();
    }
}
