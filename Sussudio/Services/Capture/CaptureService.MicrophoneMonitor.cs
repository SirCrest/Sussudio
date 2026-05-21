using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

// Microphone monitoring and preview-time microphone writer attachment.
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
