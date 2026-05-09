using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Flashback;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Recording finalization and disposal lifecycle: stop-and-dispose the recording
// backend (both the flashback and LibAv paths), finalize the flashback recording
// export, and roll back transient backends on failed recording starts.
public partial class CaptureService
{
    private async Task<FinalizeResult> FinalizeFlashbackRecordingAsync(
        RecordingContext? recordingContext,
        FlashbackRecordingBoundarySnapshot recordingBoundary,
        CancellationToken cancellationToken)
    {
        var outputPath = recordingContext?.FinalOutputPath ?? string.Empty;

        // H3: Pause eviction BEFORE EndRecordingAsync to close the window where
        // eviction could delete segments between EndRecording (which resumes eviction
        // internally) and ExportFlashbackCoreAsync (which pauses it again).
        // With ref-counted eviction, the nested Pause from ExportFlashbackCoreAsync is safe.
        var backendLeaseHeld = false;
        try
        {
            await _flashbackBackendLeaseLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            backendLeaseHeld = true;

            return await _flashbackBackend.FinalizeRecordingAsync(
                    outputPath,
                    captureBoundarySnapshot: sink => CaptureFlashbackRecordingBoundarySnapshot(sink, recordingBoundary),
                    exportRecordingAsync: (startPts, endPts, exportOutputPath, ct) =>
                        ExportFlashbackCoreAsync(
                            startPts,
                            endPts,
                            exportOutputPath,
                            progress: null,
                            ct: ct,
                            requireCompleteLiveEdge: true,
                            throttleHighResolutionBaseline: false),
                    resumeEvictionBestEffort: ResumeFlashbackEvictionBestEffort,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
        }
    }

    private sealed class FlashbackRecordingBoundarySnapshot
    {
        public bool Captured { get; set; }
        public long RecordingFramesDelivered { get; set; }
        public long RecordingFramesEnqueued { get; set; }
        public RecordingIntegrityCounterSnapshot? Counters { get; set; }
        public RecordingAudioIntegrityCounterSnapshot? AudioCounters { get; set; }
    }

    private void CaptureFlashbackRecordingBoundarySnapshot(
        FlashbackEncoderSink flashbackSink,
        FlashbackRecordingBoundarySnapshot recordingBoundary)
    {
        if (recordingBoundary.Captured)
        {
            return;
        }

        var flashbackVideoCapture = _unifiedVideoCapture;
        if (flashbackVideoCapture != null)
        {
            flashbackVideoCapture.EndFlashbackRecordingAccounting();
            _lastMfSourceReaderFramesDelivered = flashbackVideoCapture.VideoFramesArrived;
            _lastMfSourceReaderFramesDropped = flashbackVideoCapture.VideoFramesDropped;
            _lastMfSourceReaderNegotiatedFormat = flashbackVideoCapture.NegotiatedFormat;
            recordingBoundary.RecordingFramesDelivered = flashbackVideoCapture.RecordingFramesDelivered;
            recordingBoundary.RecordingFramesEnqueued = flashbackVideoCapture.VideoFramesWrittenToSink;
            Logger.Log(
                "VIDEO_DIAG flashback_recording_pipeline " +
                $"source_frames_during_recording={recordingBoundary.RecordingFramesDelivered} " +
                $"frames_accepted_by_flashback={recordingBoundary.RecordingFramesEnqueued} " +
                $"pipeline_drops={recordingBoundary.RecordingFramesDelivered - recordingBoundary.RecordingFramesEnqueued}");
        }

        recordingBoundary.Counters = CaptureFlashbackRecordingIntegrityCountersSinceBaseline(flashbackSink, flashbackVideoCapture);
        recordingBoundary.AudioCounters = GetRecordingAudioCountersSinceBaseline(
            CaptureRecordingAudioCounters(_wasapiAudioCapture, flashbackSink, _activeRecordingSettings));
        recordingBoundary.Captured = true;
    }

    private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync(string fallbackStatusMessage, CancellationToken cancellationToken)
    {
        // --- Unified flashback recording path: remux from .ts, cycle buffer ---
        if (IsFlashbackRecordingBackendActive())
        {
            var flashbackSink = _flashbackSink!;
            var fbRecordingContext = _recordingContext;
            var fbOutputPath = fbRecordingContext?.FinalOutputPath ?? (_lastOutputPath ?? string.Empty);
            var recordingBoundary = new FlashbackRecordingBoundarySnapshot();

            Volatile.Write(ref _flashbackRecordingFinalizeInProgress, 1);
            _recordingSink = null;
            // Don't null _flashbackSink — it continues for the buffer

            FinalizeResult fbResult;
            OperationCanceledException? flashbackCancellationException = null;
            try
            {
                try
                {
                    fbResult = await FinalizeFlashbackRecordingAsync(
                            fbRecordingContext,
                            recordingBoundary,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                finally
                {
                    Volatile.Write(ref _flashbackRecordingFinalizeInProgress, 0);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                flashbackCancellationException = new OperationCanceledException(cancellationToken);
                fbResult = FinalizeResult.Failure(fbOutputPath, "Flashback recording finalize cancelled.");
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
                fbResult = FinalizeResult.Failure(fbOutputPath, $"Flashback recording finalize failed: {ex.Message}");
            }

            CaptureFlashbackRecordingBoundarySnapshot(flashbackSink, recordingBoundary);

            if (cancellationToken.IsCancellationRequested && IsFlashbackFinalizeCancellationResult(fbResult))
            {
                flashbackCancellationException ??= new OperationCanceledException(cancellationToken);
            }

            _lastRecordingIntegrity = BuildRecordingIntegritySummary(
                backend: "Flashback",
                recordingActive: false,
                finalizeSucceeded: fbResult.Succeeded,
                finalizeStatus: fbResult.StatusMessage,
                completedUtc: DateTimeOffset.UtcNow,
                sourceFrames: recordingBoundary.RecordingFramesDelivered,
                acceptedFrames: recordingBoundary.RecordingFramesEnqueued,
                counters: recordingBoundary.Counters ?? CaptureFlashbackRecordingIntegrityCountersSinceBaseline(flashbackSink, _unifiedVideoCapture),
                audioCounters: recordingBoundary.AudioCounters ?? GetRecordingAudioCountersSinceBaseline(
                    CaptureRecordingAudioCounters(_wasapiAudioCapture, flashbackSink, _activeRecordingSettings)));
            _recordingIntegrityCounterBaseline = null;
            _recordingIntegrityAudioBaseline = null;
            LogRecordingIntegritySummary(_lastRecordingIntegrity);

            // If settings changed during recording (format, buffer duration, etc.),
            // do a full restart to apply them. Otherwise just cycle the sink to
            // preserve DVR history.
            try
            {
                if (!fbResult.Succeeded)
                {
                    var hadPendingFlashbackSettingsChange = _pendingFlashbackSettingsChange;
                    _pendingFlashbackSettingsChange = false;
                    _flashbackBackend.PreserveRecoverySegments("recording_finalize_failed");
                    Logger.Log(
                        "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING_DEFERRED " +
                        $"reason=recording_finalize_failed pending_settings={hadPendingFlashbackSettingsChange}");
                }
                else if (_pendingFlashbackSettingsChange)
                {
                    _pendingFlashbackSettingsChange = false;
                    Logger.Log("FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING");
                    await DisposeFlashbackPreviewBackendAsync(cancellationToken, purgeSegments: true).ConfigureAwait(false);
                    if (_flashbackEnabled && _unifiedVideoCapture != null && _currentSettings != null)
                        await EnsureFlashbackPreviewBackendAsync(_unifiedVideoCapture, _currentSettings, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await CycleFlashbackBufferAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                flashbackCancellationException ??= new OperationCanceledException(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_BUFFER_CYCLE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
                RecordLastFlashbackFailure(ex);
                _flashbackBackend.PreserveRecoverySegments("buffer_cycle_failed");
                BeginFlashbackBackendCleanup(ex);
            }

            _recordingStopwatch.Stop();
            _isRecording = false;
            if (!_isVideoPreviewActive) await StopTelemetryPollAsync().ConfigureAwait(false);
            _recordingContext = null;
            _activeRecordingSettings = null;
            _lastFinalizeStatus = fbResult.StatusMessage;
            _lastFinalizeUtc = DateTimeOffset.UtcNow;
            _lastPreservedArtifacts = fbResult.PreservedArtifacts;

            // Restart mic monitoring if preview is still active
            if (_isVideoPreviewActive && _micMonitorEnabled && !string.IsNullOrWhiteSpace(_micMonitorDeviceId))
            {
                WasapiAudioCapture? micCapture = null;
                try
                {
                    if (_microphoneCapture == null)
                    {
                        micCapture = new WasapiAudioCapture();
                        await micCapture.InitializeAsync(_micMonitorDeviceId, cancellationToken).ConfigureAwait(false);
                        micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                        micCapture.CaptureFailed += OnWasapiCaptureFailed;
                        micCapture.Start();
                        if (_flashbackSink is { MicrophoneEnabled: true } fbSink)
                        {
                            micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                        }
                        _microphoneCapture = micCapture;
                        micCapture = null;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    flashbackCancellationException ??= new OperationCanceledException(cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_MIC_RESTART_WARN type={ex.GetType().Name} error='{ex.Message}'");
                }
                finally
                {
                    if (micCapture != null)
                    {
                        micCapture.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                        micCapture.CaptureFailed -= OnWasapiCaptureFailed;
                        try { await micCapture.DisposeAsync().ConfigureAwait(false); }
                        catch (Exception disposeEx) { Logger.Log($"FLASHBACK_MIC_RESTART_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
                    }
                }
            }

            if (fbResult.Succeeded)
            {
                Logger.Log($"FLASHBACK_UNIFIED_RECORDING_STOP_OK output='{fbResult.OutputPath}'");
            }
            else
            {
                Logger.Log($"FLASHBACK_UNIFIED_RECORDING_STOP_FAIL output='{fbResult.OutputPath}'");
            }
            if (flashbackCancellationException != null)
            {
                throw flashbackCancellationException;
            }

            return fbResult;
        }

        // --- Standard LibAvRecordingSink path ---
        var sink = _recordingSink;
        var libAvSink = _libavSink;
        var recordingContext = _recordingContext;
        var fallbackOutputPath = recordingContext?.FinalOutputPath ?? (_lastOutputPath ?? string.Empty);

        _recordingSink = null;
        _libavSink = null;
        _pendingLibAvDrainTask = null;

        var result = FinalizeResult.Success(fallbackOutputPath, fallbackStatusMessage);
        OperationCanceledException? cancellationException = null;

        var unifiedVideoCapture = _unifiedVideoCapture;
        var recordingFramesDeliveredToBoundary = 0L;
        var recordingFramesAcceptedByBoundary = 0L;
        if (unifiedVideoCapture != null)
        {
            try
            {
                await unifiedVideoCapture.StopRecordingAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancellationException = new OperationCanceledException(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Log($"Unified video recording stop failed: {ex.Message}");
                if (cancellationException == null && result.Succeeded)
                {
                    result = FinalizeResult.Failure(fallbackOutputPath, $"Unified video recording stop failed: {ex.Message}");
                }
            }
            finally
            {
                // Keep SkipCpuReadback=true — preview uses GPU textures, not CPU bytes.
                // Lock2D is never needed while D3D shared device is active.
            }

            _lastMfSourceReaderFramesDelivered = unifiedVideoCapture.VideoFramesArrived;
            _lastMfSourceReaderFramesDropped = unifiedVideoCapture.VideoFramesDropped;
            _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
            recordingFramesDeliveredToBoundary = unifiedVideoCapture.RecordingFramesDelivered;
            recordingFramesAcceptedByBoundary = unifiedVideoCapture.VideoFramesWrittenToSink;
            Logger.Log(
                "VIDEO_DIAG mf_source_reader " +
                $"frames_delivered={_lastMfSourceReaderFramesDelivered} " +
                $"frames_dropped={_lastMfSourceReaderFramesDropped} " +
                $"negotiated_format='{_lastMfSourceReaderNegotiatedFormat ?? "unknown"}'");
            Logger.Log(
                "VIDEO_DIAG recording_pipeline " +
                $"source_frames_during_recording={recordingFramesDeliveredToBoundary} " +
                $"frames_enqueued_to_encoder={recordingFramesAcceptedByBoundary} " +
                $"pipeline_drops={recordingFramesDeliveredToBoundary - recordingFramesAcceptedByBoundary}");
        }

        if (_wasapiAudioCapture != null)
        {
            try
            {
                _wasapiAudioCapture.DetachRecordingSink();
            }
            catch (Exception ex)
            {
                Logger.Log($"Audio recording sink detach failed: {ex.Message}");
            }
        }

        await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

        if (sink != null)
        {
            try
            {
                var sinkResult = await sink.StopAsync(cancellationToken).ConfigureAwait(false);
                if (result.Succeeded)
                {
                    result = sinkResult;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancellationException = new OperationCanceledException(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Log($"Recording sink stop failed: {ex.Message}");
                if (result.Succeeded)
                {
                    result = FinalizeResult.Failure(fallbackOutputPath, $"Recording stop failed: {ex.Message}");
                }
            }
            finally
            {
                try
                {
                    await sink.DisposeAsync().ConfigureAwait(false);
                    if (libAvSink != null)
                    {
                        var libAvDrainTask = libAvSink.EncodingCompletionTask;
                        if (!libAvDrainTask.IsCompleted)
                        {
                            _pendingLibAvDrainTask = libAvDrainTask;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Recording sink dispose failed: {ex.Message}");
                    if (cancellationException == null && result.Succeeded)
                    {
                        result = FinalizeResult.Failure(fallbackOutputPath, $"Recording dispose failed: {ex.Message}");
                    }
                }
            }

        }

        var libAvFinalAudioCounters = libAvSink != null
            ? GetRecordingAudioCountersSinceBaseline(
                CaptureRecordingAudioCounters(_wasapiAudioCapture, libAvSink, _activeRecordingSettings))
            : RecordingAudioIntegrityCounterSnapshot.Disabled;

        if (!_isVideoPreviewActive)
        {
            _unifiedVideoCapture = null;
            if (unifiedVideoCapture != null)
            {
                try
                {
                    CacheMjpegTimingMetrics(unifiedVideoCapture);
                    DetachUnifiedVideoCapture(unifiedVideoCapture);
                    if (_pendingLibAvDrainTask is { IsCompleted: false } pendingLibAvDrainTask)
                    {
                        _pendingLibAvDrainTask = ScheduleDeferredUnifiedVideoCaptureCleanup(
                            pendingLibAvDrainTask,
                            unifiedVideoCapture,
                            reason: "recording_stop_deferred_drain");
                    }
                    else
                    {
                        await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                        await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Unified video capture dispose failed: {ex.Message}");
                    if (cancellationException == null && result.Succeeded)
                    {
                        result = FinalizeResult.Failure(fallbackOutputPath, $"Unified video capture dispose failed: {ex.Message}");
                    }
                }
            }

            var capture = _wasapiAudioCapture;
            _wasapiAudioCapture = null;
            DetachWasapiAudioCapture(capture);
            if (capture != null)
            {
                try
                {
                    await capture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Recording WASAPI capture dispose failed: {ex.Message}");
                    if (cancellationException == null && result.Succeeded)
                    {
                        result = FinalizeResult.Failure(fallbackOutputPath, $"Recording WASAPI capture dispose failed: {ex.Message}");
                    }
                }
            }
        }

        var wasapiAudioCaptureFaulted = Volatile.Read(ref _wasapiAudioCaptureFaulted);
        var wasapiAudioCaptureFaultMessage = Volatile.Read(ref _wasapiAudioCaptureFaultMessage);
        Volatile.Write(ref _wasapiAudioCaptureFaulted, false);
        Volatile.Write(ref _wasapiAudioCaptureFaultMessage, null);
        if (wasapiAudioCaptureFaulted && cancellationException == null && result.Succeeded)
        {
            var statusMessage = string.IsNullOrWhiteSpace(wasapiAudioCaptureFaultMessage)
                ? "Recording failed (WASAPI audio capture faulted)."
                : $"Recording failed (WASAPI audio capture faulted: {wasapiAudioCaptureFaultMessage})";
            Logger.Log($"RECORDING_AUDIO_FAULT status='{statusMessage}'");
            result = FinalizeResult.Failure(result.OutputPath, statusMessage);
        }

        if (libAvSink != null)
        {
            CaptureEncoderRuntimeTelemetry(libAvSink);
            _lastRecordingIntegrity = BuildRecordingIntegritySummary(
                backend: "LibAv",
                recordingActive: false,
                finalizeSucceeded: result.Succeeded,
                finalizeStatus: result.StatusMessage,
                completedUtc: DateTimeOffset.UtcNow,
                sourceFrames: recordingFramesDeliveredToBoundary,
                acceptedFrames: recordingFramesAcceptedByBoundary,
                counters: GetRecordingIntegrityCountersSinceBaseline(CaptureRecordingIntegrityCounters(libAvSink)),
                audioCounters: libAvFinalAudioCounters);
            _recordingIntegrityCounterBaseline = null;
            _recordingIntegrityAudioBaseline = null;
            LogRecordingIntegritySummary(_lastRecordingIntegrity);
        }

        _recordingStopwatch.Stop();
        _isRecording = false;
        if (!_isVideoPreviewActive) await StopTelemetryPollAsync().ConfigureAwait(false);
        _recordingContext = null;
        _activeRecordingSettings = null;
        _mfConvertersDisabled = false;

        if (_pendingFlashbackEnableAfterRecording)
        {
            _pendingFlashbackEnableAfterRecording = false;
            if (_flashbackEnabled && _isVideoPreviewActive && _unifiedVideoCapture != null && _currentSettings != null)
            {
                try
                {
                    await EnsureFlashbackPreviewBackendAsync(_unifiedVideoCapture, _currentSettings, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    cancellationException ??= new OperationCanceledException(cancellationToken);
                    _flashbackEnabled = false;
                    _pendingFlashbackEnableAfterRecording = false;
                    if (_flashbackBackend.HasAnyResource)
                    {
                        await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true).ConfigureAwait(false);
                    }
                    Logger.Log("FLASHBACK_ENABLE_AFTER_RECORDING_CANCELLED");
                }
                catch (Exception ex)
                {
                    _flashbackEnabled = false;
                    _pendingFlashbackEnableAfterRecording = false;
                    if (_flashbackBackend.HasAnyResource)
                    {
                        await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true).ConfigureAwait(false);
                    }
                    Logger.Log($"FLASHBACK_ENABLE_AFTER_RECORDING_FAIL type={ex.GetType().Name} error='{ex.Message}'");
                }
            }
        }

        // Restart mic monitoring if preview is still active
        if (_isVideoPreviewActive && _micMonitorEnabled && !string.IsNullOrWhiteSpace(_micMonitorDeviceId))
        {
            WasapiAudioCapture? micCapture = null;
            try
            {
                micCapture = new WasapiAudioCapture();
                await micCapture.InitializeAsync(_micMonitorDeviceId, cancellationToken).ConfigureAwait(false);
                micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                micCapture.CaptureFailed += OnWasapiCaptureFailed;
                micCapture.Start();
                if (_flashbackSink is { MicrophoneEnabled: true } fbSink)
                {
                    micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                    Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='mic_monitor_restart'");
                }
                _microphoneCapture = micCapture;
                micCapture = null;
                Logger.Log("MIC_MONITOR_RESTART device='" + (_micMonitorDeviceName ?? "?") + "'");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancellationException ??= new OperationCanceledException(cancellationToken);
            }
            catch (Exception micEx)
            {
                Logger.Log("Mic monitor restart failed (non-fatal): " + micEx.Message);
            }
            finally
            {
                if (micCapture != null)
                {
                    micCapture.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                    micCapture.CaptureFailed -= OnWasapiCaptureFailed;
                    try { await micCapture.DisposeAsync().ConfigureAwait(false); }
                    catch (Exception disposeEx) { Logger.Log($"MIC_MONITOR_RESTART_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
                }
            }
        }

        _lastOutputPath = result.OutputPath;
        _lastFinalizeStatus = result.StatusMessage;
        _lastFinalizeUtc = DateTimeOffset.UtcNow;
        _lastPreservedArtifacts = result.PreservedArtifacts;

        if (cancellationException != null)
        {
            throw cancellationException;
        }

        return result;
    }

    private static bool IsFlashbackFinalizeCancellationResult(FinalizeResult result)
        => !result.Succeeded &&
           (string.Equals(result.StatusMessage, "Flashback export cancelled.", StringComparison.Ordinal) ||
            string.Equals(result.StatusMessage, "Flashback recording finalize cancelled.", StringComparison.Ordinal));

    private async Task DisposeTransientRecordingBackendAsync(
        IRecordingSink? sink,
        WasapiAudioCapture? wasapiCapture,
        UnifiedVideoCapture? unifiedVideoCapture)
    {
        if (unifiedVideoCapture != null)
        {
            try
            {
                await unifiedVideoCapture.StopRecordingAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient unified video recording stop failed during rollback: {ex.Message}");
            }
        }

        if (sink != null)
        {
            try
            {
                await sink.StopAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient recording sink stop failed during rollback: {ex.Message}");
            }

            try
            {
                await sink.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient recording sink dispose failed during rollback: {ex.Message}");
            }
        }

        if (unifiedVideoCapture != null)
        {
            if (sink is LibAvRecordingSink libAvSink)
            {
                var libAvDrainTask = libAvSink.EncodingCompletionTask;
                if (!libAvDrainTask.IsCompleted)
                {
                    _pendingLibAvDrainTask = ScheduleDeferredUnifiedVideoCaptureCleanup(
                        libAvDrainTask,
                        unifiedVideoCapture,
                        reason: "recording_start_rollback");
                    unifiedVideoCapture = null;
                }
            }

            try
            {
                if (unifiedVideoCapture != null)
                {
                    await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient unified video stop failed during rollback: {ex.Message}");
            }

            try
            {
                if (unifiedVideoCapture != null)
                {
                    await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient unified video dispose failed during rollback: {ex.Message}");
            }
        }

        if (wasapiCapture != null)
        {
            try
            {
                await wasapiCapture.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient WASAPI capture dispose failed during rollback: {ex.Message}");
            }
        }

    }
}
