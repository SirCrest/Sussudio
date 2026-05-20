using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
}
