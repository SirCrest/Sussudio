namespace Sussudio.Tools;

internal readonly partial record struct DiagnosticSessionScenarioPlan(
    bool RunFlashbackPlayback,
    bool RunFlashbackStress,
    bool RunFlashbackScrubStress,
    bool RunFlashbackRestartCycle,
    bool RunFlashbackEncoderCycle,
    bool RunFlashbackExportPlayback,
    bool RunFlashbackSegmentPlayback,
    bool RunFlashbackRangeExport,
    bool RunFlashbackRangeExportAudioSwitch,
    bool RunFlashbackLifecycle,
    bool RunFlashbackExportConcurrent,
    bool RunFlashbackDisableDuringExport,
    bool RunFlashbackRotatedExport,
    bool RunFlashbackPreviewCycle,
    bool RunFlashbackPlaybackPreviewCycle,
    bool RunFlashbackRecording,
    bool RunFlashbackRecordingPreviewCycle,
    bool RunFlashbackRecordingSettingsDeferred,
    bool RunFlashbackRecordingExportRejected,
    bool RunFlashbackExportRejected,
    bool RunCombined)
{
    internal static DiagnosticSessionScenarioPlan Create(
        bool runFlashbackPlayback = false,
        bool runFlashbackStress = false,
        bool runFlashbackScrubStress = false,
        bool runFlashbackRestartCycle = false,
        bool runFlashbackEncoderCycle = false,
        bool runFlashbackExportPlayback = false,
        bool runFlashbackSegmentPlayback = false,
        bool runFlashbackRangeExport = false,
        bool runFlashbackRangeExportAudioSwitch = false,
        bool runFlashbackLifecycle = false,
        bool runFlashbackExportConcurrent = false,
        bool runFlashbackDisableDuringExport = false,
        bool runFlashbackRotatedExport = false,
        bool runFlashbackPreviewCycle = false,
        bool runFlashbackPlaybackPreviewCycle = false,
        bool runFlashbackRecording = false,
        bool runFlashbackRecordingPreviewCycle = false,
        bool runFlashbackRecordingSettingsDeferred = false,
        bool runFlashbackRecordingExportRejected = false,
        bool runFlashbackExportRejected = false,
        bool runCombined = false)
        => new(
            runFlashbackPlayback,
            runFlashbackStress,
            runFlashbackScrubStress,
            runFlashbackRestartCycle,
            runFlashbackEncoderCycle,
            runFlashbackExportPlayback,
            runFlashbackSegmentPlayback,
            runFlashbackRangeExport,
            runFlashbackRangeExportAudioSwitch,
            runFlashbackLifecycle,
            runFlashbackExportConcurrent,
            runFlashbackDisableDuringExport,
            runFlashbackRotatedExport,
            runFlashbackPreviewCycle,
            runFlashbackPlaybackPreviewCycle,
            runFlashbackRecording,
            runFlashbackRecordingPreviewCycle,
            runFlashbackRecordingSettingsDeferred,
            runFlashbackRecordingExportRejected,
            runFlashbackExportRejected,
            runCombined);

    internal static DiagnosticSessionScenarioPlan From(string scenario)
        => DiagnosticSessionScenarioCatalog.TryGetEntry(scenario, out var entry)
            ? entry.Plan
            : default;
}
