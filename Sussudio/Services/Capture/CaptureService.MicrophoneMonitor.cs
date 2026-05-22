using System;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

// Microphone monitoring state model and mic-level event forwarding.
public partial class CaptureService
{
    private readonly record struct MicrophoneMonitorRestartOptions(
        bool OnlyWhenMissing,
        string? FlashbackAttachReason,
        string? RestartLogEvent,
        string DisposeWarningEvent);

    private void OnMicrophoneAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        MicrophoneAudioLevelUpdated?.Invoke(this, e);
    }

    private async Task DisposeMicrophoneCaptureAsync()
    {
        var mic = _previewAudioGraph.MicrophoneCapture;
        _previewAudioGraph.MicrophoneCapture = null;
        if (mic != null)
        {
            try
            {
                try
                {
                    mic.SetAudioWriter(null);
                }
                catch (Exception detachEx)
                {
                    Logger.Log($"MIC_MONITOR_WRITER_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}");
                }

                mic.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                mic.CaptureFailed -= OnWasapiCaptureFailed;
                await mic.DisposeAsync().ConfigureAwait(false);
                Logger.Log("MIC_MONITOR_STOP");
            }
            catch (Exception ex)
            {
                Logger.Log("Microphone capture dispose failed: " + ex.Message);
            }
        }
    }
}
