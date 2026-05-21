namespace Sussudio.Tools;

internal sealed class DiagnosticSessionScenarioPhaseState
{
    internal bool StartedPreview { get; set; }

    internal bool StartedRecording { get; set; }

    internal bool EnabledFlashback { get; set; }

    internal bool DisabledFlashback { get; set; }

    internal bool StartedFlashbackPlayback { get; set; }

    internal PresentMonProbeResult? PresentMon { get; set; }

    internal FlashbackRecordingSettingsDeferredPresetState FlashbackRecordingSettingsDeferredPresetState { get; set; }

    internal DiagnosticSessionScenarioPhaseResult ToResult()
        => new(
            StartedPreview,
            StartedRecording,
            EnabledFlashback,
            DisabledFlashback,
            StartedFlashbackPlayback,
            PresentMon,
            FlashbackRecordingSettingsDeferredPresetState);
}
