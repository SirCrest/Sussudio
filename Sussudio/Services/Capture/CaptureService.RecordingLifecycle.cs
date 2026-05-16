using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
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
            Volatile.Write(ref _wasapiAudioCaptureFaulted, false);
            Volatile.Write(ref _wasapiAudioCaptureFaultMessage, null);
            ThrowIfPendingLibAvDrainTaskBlocksReentry();
            try
            {
                await DisposeUnusableFlashbackRecordingBackendAsync(transitionToken).ConfigureAwait(false);

                if (_flashbackEnabled && _flashbackSink != null)
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
}
