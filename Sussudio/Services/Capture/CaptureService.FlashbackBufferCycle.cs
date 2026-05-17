using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Flashback;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    /// <summary>
    /// Cycles the flashback encoder sink after recording stops.
    /// Preserves the buffer manager and its segments so DVR rewind history
    /// survives across recordings. Only the encoder sink is torn down and
    /// replaced; the buffer manager continues accumulating segments.
    /// Falls back to full teardown+rebuild if sink-only cycle fails.
    /// </summary>
    private async Task CycleFlashbackBufferAsync(CancellationToken cancellationToken, bool purgeSegments = false)
    {
        await _flashbackBackendLeaseLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        var exportOperationLockHeld = false;
        try
        {
            await _flashbackExportOperationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            exportOperationLockHeld = true;

            var unifiedVideoCapture = _unifiedVideoCapture;
            var bufferManager = _flashbackBufferManager;
            var oldSink = _flashbackSink;
            var effectivePurgeSegments = _flashbackBackend.ResolveSegmentPurge(
                purgeSegments,
                "buffer_cycle");

            if (purgeSegments && !effectivePurgeSegments)
            {
                await DisposeFlashbackPreviewBackendCoreAsync(
                        cancellationToken,
                        new FlashbackPreviewBackendDisposalRequest(
                            PurgeSegments: false,
                            DetachMicrophoneWriter: true,
                            ExportOperationLockAlreadyHeld: true))
                    .ConfigureAwait(false);
                if (_flashbackEnabled && unifiedVideoCapture != null && _currentSettings != null)
                {
                    await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, cancellationToken).ConfigureAwait(false);
                    Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=preserve_rebuild new_session=true");
                }
                else
                {
                    Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=preserve_rebuild new_session=false reason='disabled_or_no_capture'");
                }
                return;
            }

            // If prerequisites are missing, fall back to full teardown
            if (!_flashbackEnabled || unifiedVideoCapture == null || _currentSettings == null || bufferManager == null || oldSink == null)
            {
                await DisposeFlashbackPreviewBackendCoreAsync(
                        cancellationToken,
                        new FlashbackPreviewBackendDisposalRequest(
                            effectivePurgeSegments,
                            DetachMicrophoneWriter: true,
                            ExportOperationLockAlreadyHeld: true))
                    .ConfigureAwait(false);
                if (_flashbackEnabled && unifiedVideoCapture != null && _currentSettings != null)
                {
                    await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, cancellationToken).ConfigureAwait(false);
                    Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=full_teardown new_session=true");
                }
                else
                {
                    Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=full_teardown new_session=false reason='disabled_or_no_capture'");
                }
                return;
            }

            // Close playback before cycling the sink so active decoders release segment files.
            var oldPlaybackController = _flashbackBackend.TakePlaybackController();
            var preservedInPoint = !effectivePurgeSegments ? oldPlaybackController?.InPoint : null;
            var preservedOutPoint = !effectivePurgeSegments ? oldPlaybackController?.OutPoint : null;
            var preservedInPointFilePts = !effectivePurgeSegments ? oldPlaybackController?.InPointFilePts : null;
            var preservedOutPointFilePts = !effectivePurgeSegments ? oldPlaybackController?.OutPointFilePts : null;
            if (oldPlaybackController != null)
            {
                try
                {
                    oldPlaybackController.GoLive();
                    oldPlaybackController.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_PLAYBACK_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
                }
            }

            _flashbackBackend.DetachProducers(
                new FlashbackProducerDetachRequest(
                    unifiedVideoCapture,
                    _wasapiAudioCapture,
                    _microphoneCapture,
                    "FLASHBACK_CYCLE_DETACH_WARN",
                    DetachMicrophoneWriter: true));
            oldSink.FrameEncoded -= OnFlashbackFrameEncoded;
            var committedCycleToken = CancellationToken.None;

            // Stop and dispose the old sink (leaves buffer manager and segments intact)
            try
            {
                await oldSink.StopAsync(committedCycleToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                Logger.Log($"FLASHBACK_CYCLE_STOP_CANCEL_DEFERRED type={ex.GetType().Name} msg={ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_CYCLE_STOP_WARN type={ex.GetType().Name} msg={ex.Message}");
            }

            try
            {
                await oldSink.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_CYCLE_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
            }

            // From this point on the old sink is no longer a usable backend. Keep
            // cancellation deferred until a replacement is attached or teardown is complete.
            _flashbackBackend.ClearSinkAndSettings();

            var oldSinkCompletionTask = oldSink.EncodingCompletionTask;
            if (!oldSinkCompletionTask.IsCompleted)
            {
                Logger.Log("FLASHBACK_CYCLE_DISPOSE_DEFERRED - falling back to full teardown");
                var oldExporter = _flashbackExporter;

                _flashbackBackend.Clear();

                ScheduleDeferredFlashbackBackendCleanup(
                    oldSinkCompletionTask,
                    new FlashbackBackendArtifactCleanupRequest(
                        bufferManager,
                        oldExporter,
                        "buffer_cycle_deferred_cleanup",
                        effectivePurgeSegments));

                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, committedCycleToken).ConfigureAwait(false);
                Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=deferred_full_rebuild");
                cancellationToken.ThrowIfCancellationRequested();
                return;
            }

            // When the codec/format changed, purge stale segments (incompatible with
            // new encoder) and reset PTS so the new encoder starts fresh from 0.
            // After stop-recording, keep everything — segments, PTS range, and
            // buffer state — so the user can immediately scrub/export DVR history.
            if (effectivePurgeSegments)
            {
                bufferManager.ResetLatestPts();
                bufferManager.PurgeCompletedSegments();

                // If some segments couldn't be deleted (e.g., playback has files locked),
                // fall back to full teardown to avoid mixed-codec segments in the buffer.
                if (bufferManager.SegmentCount > 0)
                {
                    Logger.Log($"FLASHBACK_CYCLE_PURGE_INCOMPLETE remaining={bufferManager.SegmentCount} — falling back to full teardown");
                    await DisposeFlashbackPreviewBackendCoreAsync(
                            committedCycleToken,
                            new FlashbackPreviewBackendDisposalRequest(
                                effectivePurgeSegments,
                                DetachMicrophoneWriter: true,
                                ExportOperationLockAlreadyHeld: true))
                        .ConfigureAwait(false);
                    await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, committedCycleToken).ConfigureAwait(false);
                    Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=purge_fallback_rebuild");
                    cancellationToken.ThrowIfCancellationRequested();
                    return;
                }
            }

            // Ensure the new sink gets a fresh segment file (not the old sink's active path).
            bufferManager.FinalizeActiveSegmentForCycle();

            // Create and start a new encoder sink on the same buffer manager
            var newSink = new FlashbackEncoderSink(bufferManager);
            newSink.SetFatalErrorCallback(OnFlashbackBackendFatalError);
            try
            {
                // When preserving DVR history (no purge), continue PTS from where
                // the old sink left off so new segments don't overlap existing ones.
                var ptsOffset = effectivePurgeSegments ? TimeSpan.Zero : bufferManager.LatestPts;
                await newSink.StartAsync(
                    CreateFlashbackSessionContext(unifiedVideoCapture, _currentSettings),
                    committedCycleToken,
                    ptsBaseOffset: ptsOffset).ConfigureAwait(false);

                newSink.FrameEncoded += OnFlashbackFrameEncoded;
                _flashbackSink = newSink;
                _flashbackBackendSettings = CloneCaptureSettings(_currentSettings);
                ClearLastFlashbackFailure();

                _flashbackBackend.AttachProducers(
                    new FlashbackProducerAttachRequest(
                        unifiedVideoCapture,
                        _wasapiAudioCapture,
                        _microphoneCapture,
                        "buffer_cycle"));

                var playbackController = new FlashbackPlaybackController(bufferManager);
                playbackController.GpuDecodeEnabled = _currentSettings.FlashbackGpuDecode;
                playbackController.RestoreInOutPoints(
                    preservedInPoint,
                    preservedOutPoint,
                    preservedInPointFilePts,
                    preservedOutPointFilePts);
                if (_previewFrameSink != null)
                {
                    playbackController.Initialize(_previewFrameSink, unifiedVideoCapture, _wasapiAudioPlayback, _wasapiAudioCapture);
                }
                _flashbackPlaybackController = playbackController;

                Logger.Log($"FLASHBACK_BUFFER_CYCLE_OK mode=sink_only segments={bufferManager.SegmentCount} buffered={bufferManager.BufferedDuration.TotalSeconds:F1}s");
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_FAIL type={ex.GetType().Name} error='{ex.Message}' — falling back to full teardown");
                try { newSink.FrameEncoded -= OnFlashbackFrameEncoded; }
                catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_EVENT_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}"); }
                try { unifiedVideoCapture.SetFlashbackSink(null); }
                catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}"); }
                try { _wasapiAudioCapture?.DetachFlashbackSink(); }
                catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_AUDIO_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}"); }
                try { _microphoneCapture?.SetAudioWriter(null); }
                catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_MIC_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}"); }
                try { await newSink.DisposeAsync().ConfigureAwait(false); }
                catch (Exception disposeEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
                _flashbackBackend.ClearSinkAndSettings();

                // Full teardown and rebuild
                await DisposeFlashbackPreviewBackendCoreAsync(
                        committedCycleToken,
                        new FlashbackPreviewBackendDisposalRequest(
                            effectivePurgeSegments,
                            DetachMicrophoneWriter: true,
                            ExportOperationLockAlreadyHeld: true))
                    .ConfigureAwait(false);
                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, committedCycleToken).ConfigureAwait(false);
                Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=fallback_full_rebuild");
            }

            if (cancellationToken.IsCancellationRequested)
            {
                Logger.Log("FLASHBACK_BUFFER_CYCLE_CANCEL_DEFERRED");
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        finally
        {
            ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);
            ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_buffer_cycle");
        }
    }
}
