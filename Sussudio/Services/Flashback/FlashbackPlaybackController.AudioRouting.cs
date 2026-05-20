using System;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Audio routing ---

    private void ApplyAudioRoutingForState(string operation)
    {
        if (_disposedFlag != 0)
        {
            return;
        }

        switch (_state)
        {
            case FlashbackPlaybackState.Live:
                RestoreLiveAudio();
                break;
            case FlashbackPlaybackState.Playing:
                SuppressLiveAudio();
                SafeResumeRendering(operation);
                break;
            case FlashbackPlaybackState.Paused:
            case FlashbackPlaybackState.Scrubbing:
                SuppressLiveAudio();
                SafePauseRendering(operation);
                break;
        }
    }

    private void ApplyPreviewRoutingForState(string operation)
    {
        if (_disposedFlag != 0)
        {
            return;
        }

        if (_state == FlashbackPlaybackState.Live)
        {
            SafeResumePreviewSubmission(operation);
        }
        else
        {
            SafeSuppressPreviewSubmission(operation);
        }
    }

    private void SuppressLiveAudio()
    {
        try
        {
            _audioCapture?.SetPlayback(null);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=suppress_live_set_playback type={ex.GetType().Name} msg='{ex.Message}'");
        }

        SafeFlushPlayback("suppress_live_audio");
    }

    private void RestoreLiveAudio()
    {
        SafeFlushPlayback("restore_live_audio");
        // Reconnect audio feed before resuming rendering to avoid silence/stutter.
        try
        {
            if (_audioCapture != null && _audioPlayback != null)
            {
                _audioCapture.SetPlayback(_audioPlayback);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=restore_live_set_playback type={ex.GetType().Name} msg='{ex.Message}'");
        }

        SafeResumeRendering("restore_live_audio");
    }
}
