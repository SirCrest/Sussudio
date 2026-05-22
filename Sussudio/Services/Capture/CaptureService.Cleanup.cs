using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Cleanup lifecycle for session teardown, app shutdown, and disposal-triggered
// cleanup. This path preserves failed Flashback recording artifacts when
// finalization cannot complete cleanly.
public partial class CaptureService
{
    public Task CleanupAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.CleaningUp, CleanupCoreAsync, cancellationToken);

    private async Task CleanupForDisposalAsync()
    {
        await _sessionTransitionLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            EnterCleanupState();
            await CleanupCoreAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            ReleaseSemaphoreBestEffort(_sessionTransitionLock, "dispose_cleanup");
        }
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;
        try
        {
            Task.Run(CleanupForDisposalAsync).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.Log($"CaptureService.Dispose cleanup warning: {ex.Message}");
        }

        DisposeCoordinationLocksBestEffort();
        EnterDisposedState();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;
        try
        {
            await CleanupForDisposalAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"CaptureService.DisposeAsync cleanup warning: {ex.Message}");
        }

        DisposeCoordinationLocksBestEffort();
        EnterDisposedState();
    }

    private async Task CleanupCoreAsync(CancellationToken transitionToken)
    {
        var cancellationRequested = false;
        var preserveFlashbackSegmentsAfterFailedRecordingFinalize = false;
        if (_isRecording || _recordingBackend.HasActiveBackend)
        {
            var stoppingFlashbackRecording = IsFlashbackRecordingBackendActive();
            try
            {
                var result = await StopAndDisposeRecordingBackendAsync(
                    "Stopped during cleanup",
                    emergency: false,
                    transitionToken).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    Logger.Log($"Cleanup stop reported issues: {result.StatusMessage}");
                    if (stoppingFlashbackRecording)
                    {
                        _flashbackBackend.PreserveRecoverySegments("cleanup_stop_failed");
                        preserveFlashbackSegmentsAfterFailedRecordingFinalize = true;
                    }
                }
            }
            catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)
            {
                cancellationRequested = true;
                if (stoppingFlashbackRecording)
                {
                    _flashbackBackend.PreserveRecoverySegments("cleanup_stop_cancelled");
                    preserveFlashbackSegmentsAfterFailedRecordingFinalize = true;
                }
            }
        }

        _recordingBackend.ClearPendingLibAvDrainIfCompletedSuccessfully();

        try
        {
            if (preserveFlashbackSegmentsAfterFailedRecordingFinalize)
            {
                Logger.Log("FLASHBACK_CLEANUP_PRESERVE_SEGMENTS reason=recording_finalize_failed");
            }

            await DisposeFlashbackPreviewBackendAsync(
                    transitionToken,
                    purgeSegments: !preserveFlashbackSegmentsAfterFailedRecordingFinalize)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CLEANUP_DISPOSE_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        }

        var pendingLibAvDrainTask = _recordingBackend.PendingLibAvDrainTask;
        var unifiedVideoCapture = _videoPipeline.TakeCapture();
        if (unifiedVideoCapture != null)
        {
            try
            {
                CacheMjpegTimingMetrics(unifiedVideoCapture);
                _lastMfSourceReaderFramesDelivered = unifiedVideoCapture.VideoFramesArrived;
                _lastMfSourceReaderFramesDropped = unifiedVideoCapture.VideoFramesDropped;
                _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
                DetachUnifiedVideoCapture(unifiedVideoCapture);
                if (pendingLibAvDrainTask is { IsCompleted: false })
                {
                    _recordingBackend.PendingLibAvDrainTask = _videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(
                        pendingLibAvDrainTask,
                        unifiedVideoCapture,
                        reason: "cleanup_after_deferred_recording");
                }
                else
                {
                    await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                    await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_CLEANUP_UNIFIED_VIDEO_WARN type={ex.GetType().Name} msg='{ex.Message}'");
            }
        }

        var wasapiCapture = _previewAudioGraph.ProgramCapture;
        _previewAudioGraph.ProgramCapture = null;
        _previewAudioGraph.DetachCapture(
            wasapiCapture,
            OnWasapiAudioLevelUpdated,
            OnWasapiCaptureFailed,
            _flashbackBackend.PlaybackController);
        if (wasapiCapture != null)
        {
            try
            {
                await wasapiCapture.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_CLEANUP_WASAPI_WARN type={ex.GetType().Name} msg='{ex.Message}'");
            }
        }

        await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

        await StopTelemetryPollAsync().ConfigureAwait(false);
        _isVideoPreviewActive = false;
        _isAudioPreviewActive = false;
        _isInitialized = false;
        _currentDevice = null;
        _currentSettings = null;
        _recordingBackend.ClearContextAndSettings();
        ResetAvSyncDriftBaseline();
        ResetSessionStateAfterCleanup();

        if (cancellationRequested || transitionToken.IsCancellationRequested)
        {
            transitionToken.ThrowIfCancellationRequested();
        }
    }
}
