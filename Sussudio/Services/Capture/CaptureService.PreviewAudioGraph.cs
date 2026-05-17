using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private async Task<WasapiAudioCapture?> StartPreviewAudioGraphAsync(
        CaptureSettings settings,
        string? audioDeviceId,
        CancellationToken transitionToken)
    {
        WasapiAudioCapture? wasapiCapture = null;
        try
        {
            if (settings.AudioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId))
            {
                wasapiCapture = new WasapiAudioCapture();
                await wasapiCapture.InitializeAsync(audioDeviceId, transitionToken).ConfigureAwait(false);
                wasapiCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                wasapiCapture.CaptureFailed += OnWasapiCaptureFailed;
                wasapiCapture.Start();
                _wasapiAudioCapture = wasapiCapture;
            }
            else if (settings.AudioEnabled)
            {
                Logger.Log("Audio preview requested but no audio capture device is available; continuing with video-only preview.");
            }

            if (_isAudioPreviewActive && _wasapiAudioCapture != null)
            {
                await StartWasapiPlaybackAsync(transitionToken).ConfigureAwait(false);
            }

            Logger.Log(
                _wasapiAudioCapture != null
                    ? "Preview backend active: IMFSourceReader video + WASAPI audio ingest."
                    : "Preview backend active: IMFSourceReader video only (no audio capture endpoint).");

            await StartPreviewMicrophoneMonitorAsync(transitionToken).ConfigureAwait(false);

            return wasapiCapture;
        }
        catch
        {
            await RollbackPreviewAudioCaptureStartupAsync(wasapiCapture).ConfigureAwait(false);
            throw;
        }
    }

    private async Task StartPreviewMicrophoneMonitorAsync(CancellationToken transitionToken)
    {
        // Start mic monitoring if enabled (metering only, no recording sink)
        if (!_micMonitorEnabled || string.IsNullOrWhiteSpace(_micMonitorDeviceId))
        {
            return;
        }

        WasapiAudioCapture? micCapture = null;
        try
        {
            micCapture = new WasapiAudioCapture();
            await micCapture.InitializeAsync(_micMonitorDeviceId, transitionToken).ConfigureAwait(false);
            micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
            micCapture.CaptureFailed += OnWasapiCaptureFailed;
            micCapture.Start();
            if (_flashbackSink is { MicrophoneEnabled: true } fbSink)
            {
                micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='preview_mic_monitor_start'");
            }

            _microphoneCapture = micCapture;
            micCapture = null;
            Logger.Log("MIC_MONITOR_START device='" + (_micMonitorDeviceName ?? "?") + "'");
        }
        catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception micEx)
        {
            Logger.Log("Mic monitor start failed (non-fatal): " + micEx.Message);
        }
        finally
        {
            if (micCapture != null)
            {
                micCapture.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                micCapture.CaptureFailed -= OnWasapiCaptureFailed;
                try
                {
                    await micCapture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception disposeEx)
                {
                    Logger.Log($"MIC_MONITOR_PREVIEW_START_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}");
                }
            }
        }
    }

    private async Task RollbackPreviewAudioCaptureStartupAsync(WasapiAudioCapture? wasapiCapture)
    {
        if (wasapiCapture != null)
        {
            wasapiCapture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
            wasapiCapture.CaptureFailed -= OnWasapiCaptureFailed;
        }

        var capture = _wasapiAudioCapture ?? wasapiCapture;
        _wasapiAudioCapture = null;
        if (capture != null)
        {
            DetachWasapiAudioCapture(capture);
            try
            {
                await capture.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception disposeEx)
            {
                Logger.Log($"WASAPI capture rollback dispose warning: {disposeEx.Message}");
            }
        }
    }
}
