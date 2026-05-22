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

    // Public path used by normal recording-stop (UI Stop button, automation StopRecording).
    public Task StopRecordingAsync(CancellationToken cancellationToken = default)
        => StopRecordingAsync(emergency: false, cancellationToken);

    // Internal overload used by CaptureSessionCoordinator.StopRecordingForEmergencyAsync.
    // Threads `emergency` through StopAndDisposeRecordingBackendAsync to LibAvRecordingSink
    // so the sink applies EmergencyStopTimeoutMs (5s) instead of StopTimeoutMs (30s) - fits
    // inside App.TryEmergencyStopRecording's 8s wrapper (fix #12).
    internal Task StopRecordingAsync(bool emergency, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Ready, async transitionToken =>
        {
            if (!_isRecording && _recordingBackend.Sink == null && _recordingBackend.LibAvSink == null)
            {
                return;
            }

            var result = await StopAndDisposeRecordingBackendAsync("Stopped", emergency, transitionToken).ConfigureAwait(false);
            // Preview continues running on the active source-reader/WASAPI sessions - no resume needed.
            StatusChanged?.Invoke(this, result.StatusMessage);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(result.StatusMessage);
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

    // Recording finalization router: choose the active recording backend and delegate
    // backend-specific stop/dispose work to the focused finalization partials.
    private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync(string fallbackStatusMessage, bool emergency, CancellationToken cancellationToken)
    {
        if (IsFlashbackRecordingBackendActive())
        {
            return await StopAndDisposeFlashbackRecordingBackendAsync(cancellationToken).ConfigureAwait(false);
        }

        return await StopAndDisposeLibAvRecordingBackendAsync(fallbackStatusMessage, emergency, cancellationToken).ConfigureAwait(false);
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
