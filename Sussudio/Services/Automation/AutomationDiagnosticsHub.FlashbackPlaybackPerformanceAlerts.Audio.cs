using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
