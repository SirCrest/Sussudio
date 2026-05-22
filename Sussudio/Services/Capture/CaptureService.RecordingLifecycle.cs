using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Contracts;
using Sussudio.Services.Flashback;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    // Recording finalization state is intentionally retained after stop so the
    // UI, automation, and verifier can explain what happened to the last file
    // even after capture resources have been torn down.
    private string? _lastOutputPath;
    private string _lastFinalizeStatus = "None";
    private DateTimeOffset? _lastFinalizeUtc;
    private IReadOnlyList<string> _lastPreservedArtifacts = Array.Empty<string>();

    public Task StartRecordingAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Recording, async transitionToken =>
        {
            EnsureInitialized();
            if (_isRecording)
            {
                return;
            }

            if (_currentDevice == null)
            {
                throw new InvalidOperationException("No selected video device is available for recording.");
            }

            transitionToken.ThrowIfCancellationRequested();
            _currentSettings = settings;
            _micMonitorEnabled = settings.MicrophoneEnabled;
            _micMonitorDeviceId = settings.MicrophoneDeviceId;
            _micMonitorDeviceName = settings.MicrophoneDeviceName;

            var rollback = new RecordingStartRollbackState();
            _previewAudioGraph.ResetCaptureFault();
            _recordingBackend.ThrowIfPendingLibAvDrainBlocksReentry();
            try
            {
                await DisposeUnusableFlashbackRecordingBackendAsync(transitionToken).ConfigureAwait(false);

                if (_flashbackEnabled && _flashbackBackend.Sink != null)
                {
                    await StartFlashbackRecordingAsync(settings, transitionToken, rollback).ConfigureAwait(false);
                    return;
                }

                await StartLibAvRecordingAsync(settings, transitionToken, rollback).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await RollbackRecordingStartAsync(rollback, ex).ConfigureAwait(false);
                throw;
            }
        }, cancellationToken);

    private void PublishRecordingStartedOutcome(string finalOutputPath)
    {
        _lastOutputPath = finalOutputPath;
        _lastFinalizeStatus = "Recording";
        _lastFinalizeUtc = null;
        _lastPreservedArtifacts = Array.Empty<string>();
    }

    private void PublishRecordingFinalizedOutcome(FinalizeResult result, bool updateOutputPath)
    {
        if (updateOutputPath)
        {
            _lastOutputPath = result.OutputPath;
        }

        _lastFinalizeStatus = result.StatusMessage;
        _lastFinalizeUtc = DateTimeOffset.UtcNow;
        _lastPreservedArtifacts = result.PreservedArtifacts;
    }

    private sealed class RecordingStartRollbackState
    {
        public LibAvRecordingSink? LibAvSink { get; set; }

        public IRecordingSink? RecordingSink { get; set; }

        public WasapiAudioCapture? OwnedWasapiAudioCapture { get; set; }

        public UnifiedVideoCapture? OwnedUnifiedVideoCapture { get; set; }

        public RecordingContext? RecordingContext { get; set; }

        public UnifiedVideoCapture? RecordingVideoCapture { get; set; }

        public FlashbackEncoderSink? FlashbackRecordingStartedSink { get; set; }

        public bool FlashbackRecordingBackendLeaseHeld { get; set; }

        public bool SinkAttachedForAudioOnly { get; set; }
    }
}
