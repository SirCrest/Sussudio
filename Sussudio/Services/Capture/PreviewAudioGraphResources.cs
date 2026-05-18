using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

// Owns the live WASAPI resources that make up preview audio. CaptureService
// still owns transition policy and event projection; this class owns the
// resource references, playback attachment order, volume application, and
// recording-fault telemetry.
internal sealed class PreviewAudioGraphResources
{
    private bool _captureFaulted;
    private string? _captureFaultMessage;

    public WasapiAudioCapture? ProgramCapture;
    public WasapiAudioCapture? MicrophoneCapture;
    public WasapiAudioPlayback? Playback;
    public float PreviewVolume = 1.0f;
    public bool IsMonitoringMuted;

    public void SetPreviewVolume(float volume)
    {
        PreviewVolume = Math.Clamp(volume, 0f, 1f);
        if (!IsMonitoringMuted)
        {
            Playback?.SetVolume(PreviewVolume);
        }
    }

    public void SetMonitoringMuted(bool muted)
    {
        IsMonitoringMuted = muted;
        Playback?.SetVolume(muted ? 0f : PreviewVolume);
    }

    public string ClassifyCaptureFailureSource(object? sender)
    {
        return ReferenceEquals(sender, ProgramCapture)
            ? "program"
            : ReferenceEquals(sender, MicrophoneCapture)
                ? "microphone"
                : "unknown";
    }

    public void RecordCaptureFault(string source, Exception ex)
    {
        Volatile.Write(ref _captureFaulted, true);
        Volatile.Write(ref _captureFaultMessage, $"{source}: {ex.Message}");
    }

    public void ResetCaptureFault()
    {
        Volatile.Write(ref _captureFaulted, false);
        Volatile.Write(ref _captureFaultMessage, null);
    }

    public PreviewAudioCaptureFaultSnapshot ConsumeCaptureFault()
    {
        var faulted = Volatile.Read(ref _captureFaulted);
        var message = Volatile.Read(ref _captureFaultMessage);
        ResetCaptureFault();
        return new PreviewAudioCaptureFaultSnapshot(faulted, message);
    }

    public async Task StartPlaybackAsync(
        CancellationToken cancellationToken,
        FlashbackPlaybackController? flashbackPlaybackController)
    {
        var capture = ProgramCapture;
        if (capture == null)
        {
            return;
        }

        var playback = Playback;
        if (playback == null)
        {
            var newPlayback = new WasapiAudioPlayback();
            try
            {
                await newPlayback.InitializeAsync(cancellationToken).ConfigureAwait(false);
                newPlayback.SetVolume(0);
                newPlayback.Start();
                Playback = newPlayback;
                Logger.Log("WASAPI audio playback started.");
                newPlayback.SetVolume(IsMonitoringMuted ? 0f : PreviewVolume);
                playback = newPlayback;
            }
            catch (Exception ex)
            {
                Logger.Log($"WASAPI_PLAYBACK_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
                if (ReferenceEquals(Playback, newPlayback))
                {
                    Playback = null;
                }

                StopPlaybackBestEffort(newPlayback, "start_fail");
                DisposePlaybackBestEffort(newPlayback);
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
            StopPlayback(flashbackPlaybackController);
            throw;
        }

        // WASAPI starts after Flashback init; keep the playback controller's
        // audio references synchronized once playback becomes available.
        flashbackPlaybackController?.UpdateAudioComponents(playback, capture);
    }

    public void StopPlayback(FlashbackPlaybackController? flashbackPlaybackController)
    {
        flashbackPlaybackController?.UpdateAudioComponents(null, null);
        var playback = Playback;
        Playback = null;
        SafeClearCapturePlayback(ProgramCapture, "stop_playback");
        if (playback != null)
        {
            StopPlaybackBestEffort(playback, "stop_playback");
            DisposePlaybackBestEffort(playback);
        }
    }

    public void DetachCapture(
        WasapiAudioCapture? capture,
        EventHandler<AudioLevelEventArgs> audioLevelUpdated,
        EventHandler<Exception> captureFailed,
        FlashbackPlaybackController? flashbackPlaybackController)
    {
        if (capture == null)
        {
            StopPlayback(flashbackPlaybackController);
            return;
        }

        capture.AudioLevelUpdated -= audioLevelUpdated;
        capture.CaptureFailed -= captureFailed;
        SafeClearCapturePlayback(capture, "detach_capture");
        StopPlayback(flashbackPlaybackController);
    }

    private static void SafeClearCapturePlayback(WasapiAudioCapture? capture, string operation)
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

    private static void DisposePlaybackBestEffort(WasapiAudioPlayback playback)
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

    private static void StopPlaybackBestEffort(WasapiAudioPlayback playback, string operation)
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

internal readonly record struct PreviewAudioCaptureFaultSnapshot(
    bool Faulted,
    string? Message);
