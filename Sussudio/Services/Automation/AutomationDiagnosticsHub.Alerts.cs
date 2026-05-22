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
