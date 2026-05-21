using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

// Public microphone monitor update transaction.
public partial class CaptureService
{
    public Task UpdateMicrophoneMonitorAsync(bool enabled, string? deviceId, string? deviceName, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CurrentSessionState, async transitionToken =>
        {
            var previousEnabled = _micMonitorEnabled;
            var previousDeviceId = _micMonitorDeviceId;
            var previousDeviceName = _micMonitorDeviceName;
            WasapiAudioCapture? nextMicCapture = null;
            try
            {
                transitionToken.ThrowIfCancellationRequested();
                if (_isRecording)
                {
                    _micMonitorEnabled = enabled;
                    _micMonitorDeviceId = deviceId;
                    _micMonitorDeviceName = deviceName;
                    Logger.Log("MIC_MONITOR_UPDATE_DEFERRED recording=true");
                    return;
                }

                if (enabled && !_isRecording && _isVideoPreviewActive && !string.IsNullOrWhiteSpace(deviceId))
                {
                    nextMicCapture = new WasapiAudioCapture();
                    await nextMicCapture.InitializeAsync(deviceId, transitionToken).ConfigureAwait(false);
                    nextMicCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                    nextMicCapture.CaptureFailed += OnWasapiCaptureFailed;
                    nextMicCapture.Start();
                    if (_flashbackBackend.Sink is { MicrophoneEnabled: true } fbSink)
                    {
                        nextMicCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                        Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='mic_monitor_update'");
                    }
                }

                await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

                _micMonitorEnabled = enabled;
                _micMonitorDeviceId = deviceId;
                _micMonitorDeviceName = deviceName;
                _previewAudioGraph.MicrophoneCapture = nextMicCapture;
                nextMicCapture = null;

                if (_previewAudioGraph.MicrophoneCapture != null)
                {
                    Logger.Log("MIC_MONITOR_START device='" + (deviceName ?? "?") + "'");
                }
                else
                {
                    MicrophoneAudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs(0, 0, false));
                }
            }
            catch
            {
                _micMonitorEnabled = previousEnabled;
                _micMonitorDeviceId = previousDeviceId;
                _micMonitorDeviceName = previousDeviceName;
                if (nextMicCapture != null)
                {
                    try
                    {
                        nextMicCapture.SetAudioWriter(null);
                        nextMicCapture.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                        nextMicCapture.CaptureFailed -= OnWasapiCaptureFailed;
                        await nextMicCapture.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Microphone capture rollback dispose failed: " + ex.Message);
                    }
                }

                throw;
            }
        }, cancellationToken);
}
