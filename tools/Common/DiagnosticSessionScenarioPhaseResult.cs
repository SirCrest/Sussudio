namespace Sussudio.Tools;

internal sealed record DiagnosticSessionScenarioPhaseResult(
    bool StartedPreview,
    bool StartedRecording,
    bool EnabledFlashback,
    bool DisabledFlashback,
    bool StartedFlashbackPlayback,
    PresentMonProbeResult? PresentMon,
    FlashbackRecordingSettingsDeferredPresetState FlashbackRecordingSettingsDeferredPresetState)
{
    internal static readonly DiagnosticSessionScenarioPhaseResult Empty = new(
        StartedPreview: false,
        StartedRecording: false,
        EnabledFlashback: false,
        DisabledFlashback: false,
        StartedFlashbackPlayback: false,
        PresentMon: null,
        FlashbackRecordingSettingsDeferredPresetState: default);
}
