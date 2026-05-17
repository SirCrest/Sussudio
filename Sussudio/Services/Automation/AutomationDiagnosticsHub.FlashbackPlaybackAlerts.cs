using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
}
