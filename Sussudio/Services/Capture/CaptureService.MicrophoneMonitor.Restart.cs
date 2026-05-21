using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

// Post-recording microphone monitor restart and Flashback writer reattachment.
public partial class CaptureService
{
    private async Task RestartMicrophoneMonitorAfterRecordingAsync(
        MicrophoneMonitorRestartOptions options,
        CancellationToken cancellationToken)
    {
        if (!_isVideoPreviewActive || !_micMonitorEnabled || string.IsNullOrWhiteSpace(_micMonitorDeviceId))
        {
            return;
        }

        if (options.OnlyWhenMissing && _previewAudioGraph.MicrophoneCapture != null)
        {
            return;
        }

        WasapiAudioCapture? micCapture = null;
        try
        {
            micCapture = new WasapiAudioCapture();
            await micCapture.InitializeAsync(_micMonitorDeviceId, cancellationToken).ConfigureAwait(false);
            micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
            micCapture.CaptureFailed += OnWasapiCaptureFailed;
            micCapture.Start();
            if (_flashbackBackend.Sink is { MicrophoneEnabled: true } fbSink)
            {
                micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                if (!string.IsNullOrWhiteSpace(options.FlashbackAttachReason))
                {
                    Logger.Log($"FLASHBACK_MIC_ATTACH_OK reason='{options.FlashbackAttachReason}'");
                }
            }

            _previewAudioGraph.MicrophoneCapture = micCapture;
            micCapture = null;
            if (!string.IsNullOrWhiteSpace(options.RestartLogEvent))
            {
                Logger.Log($"{options.RestartLogEvent} device='" + (_micMonitorDeviceName ?? "?") + "'");
            }
        }
        finally
        {
            if (micCapture != null)
            {
                micCapture.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                micCapture.CaptureFailed -= OnWasapiCaptureFailed;
                try { await micCapture.DisposeAsync().ConfigureAwait(false); }
                catch (Exception disposeEx) { Logger.Log($"{options.DisposeWarningEvent} type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
            }
        }
    }
}
