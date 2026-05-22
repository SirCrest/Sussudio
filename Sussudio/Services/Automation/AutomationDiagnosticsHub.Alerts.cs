using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)
    {
        ObserveFlashbackExportCompletion(snapshot);
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

        UpdateSignalAlerts(
            snapshot,
            captureOnePercentLowDegraded,
            previewOnePercentLowDegraded,
            visualCadenceHealthy,
            previewSlowFrameDetail);

        var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        UpdateFlashbackRecordingAlerts(snapshot, flashbackRecordingRecent);
        UpdateFlashbackPlaybackAlerts(snapshot, nowUnixMs);

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

    private void UpdateFlashbackPlaybackAlerts(AutomationSnapshot snapshot, long nowUnixMs)
    {
        UpdateFlashbackPlaybackCommandAlerts(snapshot, nowUnixMs);
        UpdateFlashbackPlaybackPerformanceAlerts(snapshot);
    }

    private void UpdateFlashbackPlaybackCommandAlerts(AutomationSnapshot snapshot, long nowUnixMs)
    {
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
    }

    private void UpdateSignalAlerts(
        AutomationSnapshot snapshot,
        bool captureOnePercentLowDegraded,
        bool previewOnePercentLowDegraded,
        bool visualCadenceHealthy,
        string previewSlowFrameDetail)
    {
        UpdatePreviewSignalAlerts(snapshot, previewOnePercentLowDegraded, visualCadenceHealthy, previewSlowFrameDetail);
        UpdateAudioSignalAlerts(snapshot);
        UpdateRecordingGrowthAlerts(snapshot);
        UpdateCaptureSignalAlerts(snapshot, captureOnePercentLowDegraded);
    }

    private void UpdateFlashbackPlaybackPerformanceAlerts(AutomationSnapshot snapshot)
    {
        var playbackTargetFps = ResolveFlashbackPlaybackTargetFps(
            snapshot.FlashbackPlaybackTargetFps,
            snapshot.SelectedExactFrameRate.GetValueOrDefault(snapshot.SelectedFrameRate));
        var selectedCaptureFps = snapshot.SelectedExactFrameRate.GetValueOrDefault(snapshot.SelectedFrameRate);
        var playbackActive =
            string.Equals(snapshot.FlashbackPlaybackState, "Playing", StringComparison.OrdinalIgnoreCase);

        UpdateFlashbackPlaybackCadenceAlerts(
            snapshot,
            playbackTargetFps,
            selectedCaptureFps,
            playbackActive);
        UpdateFlashbackPlaybackAudioAlerts(snapshot, playbackActive);
        UpdateFlashbackPlaybackSubmitFailureAlert(snapshot);
    }

    private void UpdateFlashbackPlaybackCadenceAlerts(
        AutomationSnapshot snapshot,
        double playbackTargetFps,
        double selectedCaptureFps,
        bool playbackActive)
    {
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
    }

    private void UpdateFlashbackPlaybackSubmitFailureAlert(AutomationSnapshot snapshot)
    {
        SetAlertState(
            "flashback-playback-submit-failures",
            snapshot.FlashbackPlaybackSubmitFailures > 0,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback playback frame submission failed: submitFailures={snapshot.FlashbackPlaybackSubmitFailures} state={snapshot.FlashbackPlaybackState} " +
            $"frames={snapshot.FlashbackPlaybackFrameCount} threadAlive={snapshot.FlashbackPlaybackThreadAlive}.",
            "Flashback playback frame submission recovered.",
            throttleMs: 5000);
    }

    private void UpdateFlashbackPlaybackAudioAlerts(
        AutomationSnapshot snapshot,
        bool playbackActive)
    {
        var playbackAudioMasterFallbackDominant =
            playbackActive &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.FlashbackPlaybackAudioMasterFallbacks >= snapshot.FlashbackPlaybackFrameCount * FlashbackPlaybackAudioMasterFallbackWarningRatio;
        var playbackAudioQueueBacklog =
            playbackActive &&
            snapshot.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            snapshot.WasapiPlaybackQueueDepth >= FlashbackPlaybackAudioQueueBacklogWarningDepth;

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
    }
}
