using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

// Microphone monitoring and preview-time microphone writer attachment.
public partial class CaptureService
{
    private void OnMicrophoneAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        MicrophoneAudioLevelUpdated?.Invoke(this, e);
    }

    private async Task DisposeMicrophoneCaptureAsync()
    {
        var mic = _microphoneCapture;
        _microphoneCapture = null;
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

    public Task UpdateMicrophoneMonitorAsync(bool enabled, string? deviceId, string? deviceName, CancellationToken cancellationToken = default)
        => RunTransitionAsync(_sessionState, async transitionToken =>
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
                    if (_flashbackSink is { MicrophoneEnabled: true } fbSink)
                    {
                        nextMicCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                        Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='mic_monitor_update'");
                    }
                }

                await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

                _micMonitorEnabled = enabled;
                _micMonitorDeviceId = deviceId;
                _micMonitorDeviceName = deviceName;
                _microphoneCapture = nextMicCapture;
                nextMicCapture = null;

                if (_microphoneCapture != null)
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
