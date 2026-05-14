using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

// WASAPI playback routing and attach/detach ordering for audio monitoring.
public partial class CaptureService
{
    private async Task StartWasapiPlaybackAsync(CancellationToken cancellationToken)
    {
        var capture = _wasapiAudioCapture;
        if (capture == null)
        {
            return;
        }

        var playback = _wasapiAudioPlayback;
        if (playback == null)
        {
            var newPlayback = new WasapiAudioPlayback();
            try
            {
                await newPlayback.InitializeAsync(cancellationToken).ConfigureAwait(false);
                newPlayback.SetVolume(0);
                newPlayback.Start();
                _wasapiAudioPlayback = newPlayback;
                Logger.Log("WASAPI audio playback started.");
                newPlayback.SetVolume(_isMonitoringMuted ? 0f : _previewVolume);
                playback = newPlayback;
            }
            catch (Exception ex)
            {
                Logger.Log($"WASAPI_PLAYBACK_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
                if (ReferenceEquals(_wasapiAudioPlayback, newPlayback))
                {
                    _wasapiAudioPlayback = null;
                }
                StopWasapiPlaybackBestEffort(newPlayback, "start_fail");
                DisposeWasapiPlaybackBestEffort(newPlayback);
                throw;
            }
        }

        try
        {
            capture.SetPlayback(playback);
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI_PLAYBACK_ATTACH_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
            StopWasapiPlayback();
            throw;
        }

        // Update flashback controller with audio components (they weren't available
        // during flashback init because WASAPI starts after flashback).
        var controller = _flashbackPlaybackController;
        controller?.UpdateAudioComponents(playback, capture);
    }

    private void StopWasapiPlayback()
    {
        var fbController = _flashbackPlaybackController;
        fbController?.UpdateAudioComponents(null, null);
        var playback = _wasapiAudioPlayback;
        _wasapiAudioPlayback = null;
        SafeClearWasapiCapturePlayback(_wasapiAudioCapture, "stop_playback");
        if (playback != null)
        {
            StopWasapiPlaybackBestEffort(playback, "stop_playback");
            DisposeWasapiPlaybackBestEffort(playback);
        }
    }

    private void DetachWasapiAudioCapture(WasapiAudioCapture? capture)
    {
        if (capture == null)
        {
            StopWasapiPlayback();
            return;
        }

        capture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
        capture.CaptureFailed -= OnWasapiCaptureFailed;
        SafeClearWasapiCapturePlayback(capture, "detach_capture");
        StopWasapiPlayback();
    }

    private static void SafeClearWasapiCapturePlayback(WasapiAudioCapture? capture, string operation)
    {
        if (capture == null)
        {
            return;
        }

        try
        {
            capture.SetPlayback(null);
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI audio playback detach warning op={operation}: {ex.Message}");
        }
    }

    private static void DisposeWasapiPlaybackBestEffort(WasapiAudioPlayback playback)
    {
        try
        {
            playback.Dispose();
            Logger.Log("WASAPI audio playback disposed.");
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI audio playback dispose warning: {ex.Message}");
        }
    }

    private static void StopWasapiPlaybackBestEffort(WasapiAudioPlayback playback, string operation)
    {
        try
        {
            playback.Stop();
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI audio playback stop warning op={operation}: {ex.Message}");
        }
    }
}
