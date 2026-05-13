using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)
    {
        var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var flashbackRecordingRecentBackpressure =
            flashbackRecordingRecent.BackpressureEvents > 0 &&
            snapshot.FlashbackVideoBackpressureLastWaitMs >= FlashbackRecordingBackpressureWarningMs;
        var flashbackRecordingQueueBacklog =
            IsFlashbackRecordingQueueBackedUp(
                snapshot.FlashbackVideoQueueDepth,
                snapshot.FlashbackVideoQueueCapacity,
                snapshot.FlashbackVideoQueueOldestFrameAgeMs);
        var flashbackAudioQueueBacklog =
            IsFlashbackAudioQueueBackedUp(
                snapshot.FlashbackAudioQueueDepth,
                snapshot.FlashbackAudioQueueCapacity);
        var flashbackRecordingRecentForceRotateGap =
            snapshot.FlashbackActive &&
            flashbackRecordingRecent.SequenceGaps > 0 &&
            snapshot.FlashbackVideoQueueRejectedFrames > 0 &&
            IsFlashbackForceRotateRejectReason(snapshot.FlashbackVideoQueueLastRejectReason);
        ObserveFlashbackExportCompletion(snapshot);
        var exportLastProgressAgeMs = snapshot.FlashbackExportActive
            ? Math.Max(0, snapshot.FlashbackExportLastProgressAgeMs)
            : 0;
        var playbackCommandQueueAgeMs =
            snapshot.FlashbackPlaybackPendingCommands > 0 &&
            snapshot.FlashbackPlaybackLastCommandQueuedUtcUnixMs > 0 &&
            snapshot.FlashbackPlaybackLastCommandQueuedUtcUnixMs > snapshot.FlashbackPlaybackLastCommandProcessedUtcUnixMs
                ? Math.Max(0, nowUnixMs - snapshot.FlashbackPlaybackLastCommandQueuedUtcUnixMs)
                : 0;
        var playbackCommandFailureAgeMs = snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs > 0
            ? Math.Max(0, nowUnixMs - snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs)
            : 0;
        var playbackCommandFailure = string.IsNullOrWhiteSpace(snapshot.FlashbackPlaybackLastCommandFailure)
            ? "None"
            : snapshot.FlashbackPlaybackLastCommandFailure;
        var playbackCommandFailedRecently =
            playbackCommandFailureAgeMs > 0 &&
            playbackCommandFailureAgeMs <= FlashbackPlaybackCommandFailureRecentMs;
        var playbackTargetFps = ResolveFlashbackPlaybackTargetFps(
            snapshot.FlashbackPlaybackTargetFps,
            snapshot.SelectedExactFrameRate.GetValueOrDefault(snapshot.SelectedFrameRate));
        var selectedCaptureFps = snapshot.SelectedExactFrameRate.GetValueOrDefault(snapshot.SelectedFrameRate);
        var playbackActive =
            string.Equals(snapshot.FlashbackPlaybackState, "Playing", StringComparison.OrdinalIgnoreCase);
        var playbackTargetBelowSelection =
            playbackActive &&
            selectedCaptureFps >= 90 &&
            snapshot.FlashbackPlaybackTargetFps > 0 &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.FlashbackPlaybackTargetFps <= selectedCaptureFps * FlashbackPlaybackSlowFpsRatio;
        var playbackPresentCadenceCapped =
            playbackActive &&
            snapshot.FlashbackPlaybackTargetFps >= 90 &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.PreviewCadenceSampleCount >= PreviewPerfectionMinSamples &&
            snapshot.PreviewCadenceObservedFps > 0 &&
            snapshot.PreviewCadenceObservedFps <= snapshot.FlashbackPlaybackTargetFps * FlashbackPlaybackSlowFpsRatio;
        var playbackSlow =
            playbackActive &&
            playbackTargetFps > 0 &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.FlashbackPlaybackObservedFps > 0 &&
            snapshot.FlashbackPlaybackObservedFps < playbackTargetFps * FlashbackPlaybackSlowFpsRatio;
        var playbackFrametimeDegraded =
            IsFlashbackPlaybackFrametimeDegraded(
                snapshot.FlashbackPlaybackState,
                playbackTargetFps,
                snapshot.FlashbackPlaybackFrameCount,
                snapshot.FlashbackPlaybackCadenceSampleCount,
                snapshot.FlashbackPlaybackOnePercentLowFps);
        var playbackAudioMasterFallbackDominant =
            playbackActive &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.FlashbackPlaybackAudioMasterFallbacks >= snapshot.FlashbackPlaybackFrameCount * FlashbackPlaybackAudioMasterFallbackWarningRatio;
        var playbackAudioQueueBacklog =
            playbackActive &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.WasapiPlaybackQueueDepth >= FlashbackPlaybackAudioQueueBacklogWarningDepth;
        var captureOnePercentLowDegraded =
            IsCaptureOnePercentLowDegraded(
                snapshot.ExpectedCaptureFrameRate,
                snapshot.CaptureCadenceSampleCount,
                snapshot.CaptureCadenceOnePercentLowFps);
        var previewOnePercentLowDegraded =
            IsPreviewOnePercentLowDegraded(
                snapshot.PreviewCadenceExpectedIntervalMs,
                snapshot.PreviewCadenceSampleCount,
                snapshot.PreviewCadenceOnePercentLowFps);
        var visualCadenceHealthy =
            IsVisualCadenceHealthy(
                snapshot.SelectedFrameRate,
                snapshot.VisualCadenceSampleCount,
                snapshot.VisualCadenceChangeObservedFps,
                snapshot.VisualCadenceRepeatFramePercent,
                snapshot.VisualCadenceLongestRepeatRun);
        var previewSlowFrameDetail = FormatPreviewSlowFrameAlertDetail(snapshot);

        SetAlertState(
            "preview-blank",
            snapshot.PreviewBlankSuspected,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Preview,
            "Preview appears active but no frames are being displayed.",
            "Preview blank condition cleared.");

        SetAlertState(
            "preview-stall",
            snapshot.PreviewStalled,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Preview,
            "Preview frame flow appears stalled.",
            "Preview stall condition cleared.");

        var startupTimeoutMs = snapshot.PreviewStartupTimeoutMs > 0 ? snapshot.PreviewStartupTimeoutMs : 2000;
        SetAlertState(
            "preview-startup-timeout",
            snapshot.IsPreviewing &&
            !snapshot.PreviewFirstVisualConfirmed &&
            string.Equals(snapshot.PreviewStartupState, "WaitingForFirstVisual", StringComparison.OrdinalIgnoreCase) &&
            snapshot.PreviewStartupElapsedMs.GetValueOrDefault() >= startupTimeoutMs,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Preview,
            string.IsNullOrWhiteSpace(snapshot.PreviewStartupMissingSignals)
                ? $"Preview startup waiting for first visual beyond {startupTimeoutMs}ms (attempt={snapshot.PreviewAttemptId ?? "none"})."
                : $"Preview startup waiting for first visual beyond {startupTimeoutMs}ms (attempt={snapshot.PreviewAttemptId ?? "none"}, missing={snapshot.PreviewStartupMissingSignals}).",
            "Preview startup visual confirmation recovered.");

        SetAlertState(
            "preview-startup-failed",
            string.Equals(snapshot.PreviewStartupState, "Failed", StringComparison.OrdinalIgnoreCase),
            DiagnosticsSeverity.Error,
            DiagnosticsCategory.Preview,
            string.IsNullOrWhiteSpace(snapshot.PreviewLastFailureReason)
                ? string.IsNullOrWhiteSpace(snapshot.PreviewStartupMissingSignals)
                    ? "Preview startup failed before first visual confirmation."
                    : $"Preview startup failed (missing={snapshot.PreviewStartupMissingSignals})."
                : $"Preview startup failed: {snapshot.PreviewLastFailureReason}",
            "Preview startup failure cleared.");

        SetAlertState(
            "preview-cadence-slow",
            snapshot.PreviewCadenceSampleCount >= 60 && snapshot.PreviewCadenceSlowFramePercent >= 8.0,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Preview,
            $"Preview cadence degraded: slowFrames={snapshot.PreviewCadenceSlowFramePercent:0.##}% " +
            $"p95={snapshot.PreviewCadenceP95IntervalMs:0.##}ms expected={snapshot.PreviewCadenceExpectedIntervalMs:0.##}ms{previewSlowFrameDetail}.",
            "Preview cadence returned to healthy range.");

        SetAlertState(
            "audio-muted-suspect",
            snapshot.AudioMutedSuspected,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Audio,
            "Audio is enabled but sustained low signal suggests muted or disconnected input.",
            "Audio signal recovered.");

        SetAlertState(
            "recording-not-growing",
            snapshot.IsRecording && !snapshot.RecordingFileGrowing,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Recording,
            "Recording is active but output bytes are not increasing.",
            "Recording output growth resumed.");

        SetAlertState(
            "capture-cadence-drop",
            snapshot.CaptureCadenceSampleCount >= 120 && snapshot.CaptureCadenceEstimatedDropPercent >= 1.0,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Capture,
            $"Capture cadence drop estimate={snapshot.CaptureCadenceEstimatedDropPercent:0.##}% " +
            $"(estDropped={snapshot.CaptureCadenceEstimatedDroppedFrames}, severeGaps={snapshot.CaptureCadenceSevereGapCount}).",
            "Capture cadence drop estimate returned to healthy range.");

        SetAlertState(
            "capture-cadence-low-1pct",
            captureOnePercentLowDegraded,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Capture,
            $"Capture cadence 1% low is below target: onePercentLow={snapshot.CaptureCadenceOnePercentLowFps:0.##}fps " +
            $"target={snapshot.ExpectedCaptureFrameRate:0.##}fps avg={snapshot.CaptureCadenceObservedFps:0.##}fps " +
            $"p95={snapshot.CaptureCadenceP95IntervalMs:0.##}ms p99={snapshot.CaptureCadenceP99IntervalMs:0.##}ms max={snapshot.CaptureCadenceMaxIntervalMs:0.##}ms.",
            "Capture cadence 1% low returned to target range.",
            throttleMs: 5000);

        SetAlertState(
            "preview-display-low-1pct",
            previewOnePercentLowDegraded && !visualCadenceHealthy,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Preview,
            $"Preview/display 1% low is below target: onePercentLow={snapshot.PreviewCadenceOnePercentLowFps:0.##}fps " +
            $"target={(snapshot.PreviewCadenceExpectedIntervalMs > 0 ? 1000.0 / snapshot.PreviewCadenceExpectedIntervalMs : 0):0.##}fps " +
            $"avg={snapshot.PreviewCadenceObservedFps:0.##}fps p95={snapshot.PreviewCadenceP95IntervalMs:0.##}ms " +
            $"p99={snapshot.PreviewCadenceP99IntervalMs:0.##}ms max={snapshot.PreviewCadenceMaxIntervalMs:0.##}ms{previewSlowFrameDetail}{FormatVisualCadenceAlertDetail(snapshot)}.",
            "Preview/display 1% low returned to target range.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-export-stalled",
            snapshot.FlashbackExportActive &&
            exportLastProgressAgeMs >= FlashbackExportStallThresholdMs,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback export has not reported progress for {exportLastProgressAgeMs}ms " +
            $"(id={snapshot.FlashbackExportId}, status={snapshot.FlashbackExportStatus}, " +
            $"progress={snapshot.FlashbackExportPercent:0.##}%, segments={snapshot.FlashbackExportSegmentsProcessed}/{snapshot.FlashbackExportTotalSegments}).",
            "Flashback export progress resumed.",
            throttleMs: 10000);

        SetAlertState(
            "flashback-temp-cache-pressure",
            snapshot.FlashbackActive &&
            (snapshot.FlashbackStartupCacheOverBudget ||
             (snapshot.FlashbackTempDriveFreeBytes >= 0 && snapshot.FlashbackTempDriveFreeBytes < FlashbackTempDriveLowFreeBytes)),
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback temp storage is under pressure: freeBytes={snapshot.FlashbackTempDriveFreeBytes} " +
            $"cacheBytes={snapshot.FlashbackStartupCacheBytes} budgetBytes={snapshot.FlashbackStartupCacheBudgetBytes} " +
            $"sessions={snapshot.FlashbackStartupCacheSessionCount} deleted={snapshot.FlashbackStartupCacheDeletedSessionCount} " +
            $"freedBytes={snapshot.FlashbackStartupCacheFreedBytes} overBudget={snapshot.FlashbackStartupCacheOverBudget}.",
            "Flashback temp storage returned to healthy range.",
            throttleMs: 10000);

        SetAlertState(
            "flashback-encoding-failed",
            snapshot.FlashbackEncodingFailed,
            DiagnosticsSeverity.Error,
            DiagnosticsCategory.Flashback,
            string.IsNullOrWhiteSpace(snapshot.FlashbackEncodingFailureMessage)
                ? $"Flashback encoder failed: type={snapshot.FlashbackEncodingFailureType ?? "Unknown"}."
                : $"Flashback encoder failed: type={snapshot.FlashbackEncodingFailureType ?? "Unknown"} message={snapshot.FlashbackEncodingFailureMessage}.",
            "Flashback encoder failure cleared.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-recording-degraded",
            snapshot.FlashbackActive &&
            (flashbackRecordingRecent.DroppedFrames > 0 ||
             flashbackRecordingRecent.EncoderDroppedFrames > 0 ||
             (flashbackRecordingRecent.SequenceGaps > 0 && !flashbackRecordingRecentForceRotateGap) ||
             flashbackRecordingRecent.GpuFramesDropped > 0 ||
             flashbackRecordingRecentBackpressure ||
             flashbackRecordingQueueBacklog ||
             flashbackAudioQueueBacklog),
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback recording path degraded: recentDropped={flashbackRecordingRecent.DroppedFrames} recentEncoderDrops={flashbackRecordingRecent.EncoderDroppedFrames} " +
            $"recentSeqGaps={flashbackRecordingRecent.SequenceGaps} recentGpuOverloads={flashbackRecordingRecent.GpuFramesDropped} " +
            $"recentBackpressureEvents={flashbackRecordingRecent.BackpressureEvents} " +
            $"totals=dropped:{snapshot.FlashbackDroppedFrames},encoderDrops:{snapshot.FlashbackVideoEncoderDroppedFrames},seqGaps:{snapshot.FlashbackVideoSequenceGaps},gpuOverloads:{snapshot.FlashbackGpuFramesDropped} " +
            $"forceRotate={snapshot.FlashbackForceRotateActive} requested={snapshot.FlashbackForceRotateRequested} draining={snapshot.FlashbackForceRotateDraining} " +
            $"queue={snapshot.FlashbackVideoQueueDepth}/{snapshot.FlashbackVideoQueueCapacity} maxQueue={snapshot.FlashbackVideoQueueMaxDepth} " +
            $"audioQueue={snapshot.FlashbackAudioQueueDepth}/{snapshot.FlashbackAudioQueueCapacity} " +
            $"backpressure={snapshot.FlashbackVideoBackpressureWaitMs}ms/{snapshot.FlashbackVideoBackpressureEvents} last={snapshot.FlashbackVideoBackpressureLastWaitMs}ms max={snapshot.FlashbackVideoBackpressureMaxWaitMs}ms.",
            "Flashback recording path returned to healthy range.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-export-rotation-gap",
            flashbackRecordingRecentForceRotateGap,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback export rotation skipped live-edge frames: recentSeqGaps={flashbackRecordingRecent.SequenceGaps} " +
            $"queueRejects={snapshot.FlashbackVideoQueueRejectedFrames} lastReject={snapshot.FlashbackVideoQueueLastRejectReason} " +
            $"exportStatus={snapshot.FlashbackExportStatus} exportId={snapshot.FlashbackExportId}.",
            "Flashback export rotation is no longer skipping live-edge frames.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-command-stalled",
            playbackCommandQueueAgeMs >= FlashbackPlaybackCommandStallThresholdMs,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback command queue has not drained for {playbackCommandQueueAgeMs}ms " +
            $"(pending={snapshot.FlashbackPlaybackPendingCommands}/{snapshot.FlashbackPlaybackCommandQueueCapacity}, maxPending={snapshot.FlashbackPlaybackMaxPendingCommands}, " +
            $"lastLatency={snapshot.FlashbackPlaybackLastCommandQueueLatencyMs}ms, maxLatency={snapshot.FlashbackPlaybackMaxCommandQueueLatencyMs}ms maxLatencyCommand={snapshot.FlashbackPlaybackMaxCommandQueueLatencyCommand}, " +
            $"lastQueued={snapshot.FlashbackPlaybackLastCommandQueued}, lastProcessed={snapshot.FlashbackPlaybackLastCommandProcessed}, " +
            $"lastFailure={playbackCommandFailure} failureAgeMs={playbackCommandFailureAgeMs}, threadAlive={snapshot.FlashbackPlaybackThreadAlive}).",
            "Flashback playback command queue drained.",
            throttleMs: 1000);

        SetAlertState(
            "flashback-playback-command-failed",
            playbackCommandFailedRecently,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback command failed recently: lastFailure={playbackCommandFailure} failureAgeMs={playbackCommandFailureAgeMs} " +
            $"pending={snapshot.FlashbackPlaybackPendingCommands}/{snapshot.FlashbackPlaybackCommandQueueCapacity} " +
            $"lastQueued={snapshot.FlashbackPlaybackLastCommandQueued} lastProcessed={snapshot.FlashbackPlaybackLastCommandProcessed} " +
            $"threadAlive={snapshot.FlashbackPlaybackThreadAlive} state={snapshot.FlashbackPlaybackState}.",
            "Flashback playback command failures cleared.",
            throttleMs: 1000);

        SetAlertState(
            "flashback-playback-target-below-selection",
            playbackTargetBelowSelection,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback target is below the selected capture rate: playbackTarget={snapshot.FlashbackPlaybackTargetFps:0.##}fps " +
            $"selected={selectedCaptureFps:0.##}fps encoder={snapshot.EncoderFrameRate:0.##}fps expected={snapshot.ExpectedCaptureFrameRate:0.##}fps " +
            $"source={(snapshot.DetectedSourceFrameRate ?? 0):0.##}fps observed={snapshot.FlashbackPlaybackObservedFps:0.##}fps frames={snapshot.FlashbackPlaybackFrameCount}.",
            "Flashback playback target matches the selected capture rate.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-present-capped",
            playbackPresentCadenceCapped,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback is targeting HFR but D3D present cadence is below target: target={snapshot.FlashbackPlaybackTargetFps:0.##}fps " +
            $"playbackObserved={snapshot.FlashbackPlaybackObservedFps:0.##}fps presentObserved={snapshot.PreviewCadenceObservedFps:0.##}fps " +
            $"present1pctLow={snapshot.PreviewCadenceOnePercentLowFps:0.##}fps sync={snapshot.PreviewD3DPresentSyncInterval} " +
            $"latency={snapshot.PreviewD3DMaxFrameLatency} buffers={snapshot.PreviewD3DSwapChainBufferCount} " +
            $"renderDrops={snapshot.PreviewD3DFramesDropped} lastDrop={snapshot.PreviewD3DLastDropReason}.",
            "Flashback playback present cadence returned to the HFR target range.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-slow",
            playbackSlow,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback is below target rate: observed={snapshot.FlashbackPlaybackObservedFps:0.##}fps target={playbackTargetFps:0.##}fps " +
            $"selected={selectedCaptureFps:0.##}fps encoder={snapshot.EncoderFrameRate:0.##}fps present={snapshot.PreviewCadenceObservedFps:0.##}fps " +
            $"frames={snapshot.FlashbackPlaybackFrameCount} late={snapshot.FlashbackPlaybackLateFrames} dropped={snapshot.FlashbackPlaybackDroppedFrames} submitFailures={snapshot.FlashbackPlaybackSubmitFailures} " +
            $"audioMasterDouble={snapshot.FlashbackPlaybackAudioMasterDelayDoubles} audioMasterShrink={snapshot.FlashbackPlaybackAudioMasterDelayShrinks} audioMasterFallback={snapshot.FlashbackPlaybackAudioMasterFallbacks} " +
            $"switches={snapshot.FlashbackPlaybackSegmentSwitches} fmp4Reopens={snapshot.FlashbackPlaybackFmp4Reopens} writeHeadWaits={snapshot.FlashbackPlaybackWriteHeadWaits}.",
            "Flashback playback returned to target rate.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-frametime-degraded",
            playbackFrametimeDegraded,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback frametime degraded: onePercentLow={snapshot.FlashbackPlaybackOnePercentLowFps:0.##}fps target={playbackTargetFps:0.##}fps " +
            $"p99={snapshot.FlashbackPlaybackP99FrameMs:0.##}ms max={snapshot.FlashbackPlaybackMaxFrameMs:0.##}ms slow={snapshot.FlashbackPlaybackSlowFramePercent:0.##}% " +
            $"ptsMismatch={snapshot.FlashbackPlaybackPtsCadenceMismatchCount} ptsDelta={snapshot.FlashbackPlaybackLastPtsCadenceDeltaMs:0.##}/{snapshot.FlashbackPlaybackLastPtsCadenceExpectedMs:0.##}ms seekCapHits={snapshot.FlashbackPlaybackSeekForwardDecodeCapHits} lastSeekCap={snapshot.FlashbackPlaybackLastSeekHitForwardDecodeCap} " +
            $"decodeP99={snapshot.FlashbackPlaybackDecodeP99Ms:0.##}ms decodeMax={snapshot.FlashbackPlaybackDecodeMaxMs:0.##}ms " +
            $"decodePhase={snapshot.FlashbackPlaybackMaxDecodePhase} decodeReceive={snapshot.FlashbackPlaybackMaxDecodeReceiveMs:0.##}ms " +
            $"decodeFeed={snapshot.FlashbackPlaybackMaxDecodeFeedMs:0.##}ms decodeRead={snapshot.FlashbackPlaybackMaxDecodeReadMs:0.##}ms decodeSend={snapshot.FlashbackPlaybackMaxDecodeSendMs:0.##}ms " +
            $"decodeAudio={snapshot.FlashbackPlaybackMaxDecodeAudioMs:0.##}ms decodeConvert={snapshot.FlashbackPlaybackMaxDecodeConvertMs:0.##}ms decodeMaxPos={snapshot.FlashbackPlaybackMaxDecodePositionMs}ms " +
            $"samples={snapshot.FlashbackPlaybackCadenceSampleCount} " +
            $"audioMasterDouble={snapshot.FlashbackPlaybackAudioMasterDelayDoubles} audioMasterShrink={snapshot.FlashbackPlaybackAudioMasterDelayShrinks} audioMasterFallback={snapshot.FlashbackPlaybackAudioMasterFallbacks}.",
            "Flashback playback frametime returned to target range.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-audio-master-fallback",
            playbackAudioMasterFallbackDominant,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback is using wall-clock pacing instead of audio-master pacing: " +
            $"fallbacks={snapshot.FlashbackPlaybackAudioMasterFallbacks} frames={snapshot.FlashbackPlaybackFrameCount} " +
            $"target={snapshot.FlashbackPlaybackTargetFps:0.##}fps observed={snapshot.FlashbackPlaybackObservedFps:0.##}fps " +
            $"avDrift={snapshot.FlashbackAvDriftMs:0.##}ms renderCallbacks={snapshot.WasapiPlaybackRenderCallbackCount} " +
            $"renderSilence={snapshot.WasapiPlaybackRenderSilenceCount} queueDepth={snapshot.WasapiPlaybackQueueDepth}.",
            "Flashback playback returned to audio-master pacing.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-audio-queue-backlog",
            playbackAudioQueueBacklog,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback audio queue is backing up: queueDepth={snapshot.WasapiPlaybackQueueDepth} " +
            $"drops={snapshot.WasapiPlaybackQueueDropCount} renderSilence={snapshot.WasapiPlaybackRenderSilenceCount} " +
            $"avDrift={snapshot.FlashbackAvDriftMs:0.##}ms target={snapshot.FlashbackPlaybackTargetFps:0.##}fps " +
            $"observed={snapshot.FlashbackPlaybackObservedFps:0.##}fps audioMasterFallback={snapshot.FlashbackPlaybackAudioMasterFallbacks}.",
            "Flashback playback audio queue returned to healthy depth.",
            throttleMs: 5000);

        SetAlertState(
            "flashback-playback-submit-failures",
            snapshot.FlashbackPlaybackSubmitFailures > 0,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback frame submission failed: submitFailures={snapshot.FlashbackPlaybackSubmitFailures} state={snapshot.FlashbackPlaybackState} " +
            $"frames={snapshot.FlashbackPlaybackFrameCount} threadAlive={snapshot.FlashbackPlaybackThreadAlive}.",
            "Flashback playback frame submission recovered.",
            throttleMs: 5000);

        SetAlertState(
            "hdr-parity-mismatch",
            snapshot.LastVerification?.HdrParity is { Requested: true, Verified: false },
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Verification,
            $"HDR parity mismatch: {snapshot.LastVerification?.HdrParity?.Status ?? "Unknown"}",
            "HDR parity mismatch cleared.");

        SetAlertState(
            "pipeline-mode-violation",
            snapshot.IsRecording && !snapshot.PipelineModeMatched,
            DiagnosticsSeverity.Error,
            DiagnosticsCategory.Capture,
            string.IsNullOrWhiteSpace(snapshot.PipelineModeReason)
                ? $"Pipeline mode violation: requested={snapshot.RequestedPipelineMode}, active={snapshot.ActivePipelineMode}."
                : $"Pipeline mode violation: {snapshot.PipelineModeReason}",
            "Pipeline mode contract restored.");

        if (!snapshot.IsRecording && _wasRecording)
        {
            AddEvent(
                DiagnosticsSeverity.Info,
                DiagnosticsCategory.Recording,
                $"Recording stopped with status: {snapshot.LastFinalizeStatus}");
        }

        SetAlertState(
            "performance-perfection-not-met",
            !snapshot.PerformancePerfectionMet,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.System,
            $"Performance below perfection threshold (score={snapshot.PerformanceScore:0.##}): {snapshot.PerformanceSummary}",
            "Performance returned to perfection threshold.",
            throttleMs: 5000);
    }
}
