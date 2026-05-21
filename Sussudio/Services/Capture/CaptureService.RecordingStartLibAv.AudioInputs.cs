using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private async Task StartLibAvRecordingAudioInputsAsync(
        CaptureSettings settings,
        CancellationToken transitionToken,
        RecordingStartRollbackState rollback,
        LibAvRecordingSink activeLibAvSink,
        IRecordingSink recordingSink,
        string? audioDeviceId)
    {
        if (_previewAudioGraph.ProgramCapture == null && settings.AudioEnabled)
        {
            var resolvedAudioDeviceId = audioDeviceId
                ?? throw new InvalidOperationException("Recording requires an audio capture device.");
            rollback.OwnedWasapiAudioCapture = new WasapiAudioCapture();
            await rollback.OwnedWasapiAudioCapture.InitializeAsync(resolvedAudioDeviceId, transitionToken).ConfigureAwait(false);
            rollback.OwnedWasapiAudioCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
            rollback.OwnedWasapiAudioCapture.CaptureFailed += OnWasapiCaptureFailed;
            rollback.OwnedWasapiAudioCapture.Start();
            _previewAudioGraph.ProgramCapture = rollback.OwnedWasapiAudioCapture;
        }

        if (_previewAudioGraph.ProgramCapture != null && settings.AudioEnabled)
        {
            _previewAudioGraph.ProgramCapture.AttachRecordingSink(recordingSink);
            rollback.SinkAttachedForAudioOnly = true;
            if (_isAudioPreviewActive)
            {
                await _previewAudioGraph.StartPlaybackAsync(
                    transitionToken,
                    _flashbackBackend.PlaybackController).ConfigureAwait(false);
            }
        }

        // Dispose preview-time mic monitor; recording creates its own capture wired to the active sink.
        await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

        if (settings.MicrophoneEnabled && !string.IsNullOrWhiteSpace(settings.MicrophoneDeviceId))
        {
            var micSink = activeLibAvSink; // capture stable reference - LibAv sink is nulled on success path
            var micCapture = new WasapiAudioCapture();
            await micCapture.InitializeAsync(settings.MicrophoneDeviceId, transitionToken).ConfigureAwait(false);
            micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
            micCapture.CaptureFailed += OnWasapiCaptureFailed;
            micCapture.SetAudioWriter(samples => micSink.WriteMicrophoneAudioAsync(samples));
            micCapture.Start();
            _previewAudioGraph.MicrophoneCapture = micCapture;
            Logger.Log("MICROPHONE_CAPTURE_START device='" + settings.MicrophoneDeviceName + "'");
        }
    }
}
