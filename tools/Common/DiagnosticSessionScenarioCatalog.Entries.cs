namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioCatalog
{
    internal static IReadOnlyList<DiagnosticSessionScenarioCatalogEntry> Entries { get; } =
    [
        new(Observe),
        new(
            PreviewOnly,
            RequiresPreview: true),
        new(
            RecordingOnly,
            RequiresRecording: true),
        new(
            Flashback,
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-stress-export.mp4"),
        new(
            FlashbackPlayback,
            DiagnosticSessionScenarioPlan.Create(runFlashbackPlayback: true),
            RequiresPreview: true,
            RequiresFlashback: true),
        new(
            FlashbackStress,
            DiagnosticSessionScenarioPlan.Create(runFlashbackStress: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-stress-export.mp4"),
        new(
            FlashbackScrubStress,
            DiagnosticSessionScenarioPlan.Create(runFlashbackScrubStress: true),
            RequiresPreview: true,
            RequiresFlashback: true),
        new(
            FlashbackRestartCycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRestartCycle: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-restart-cycle-export.mp4"),
        new(
            FlashbackEncoderCycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackEncoderCycle: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-encoder-cycle-export.mp4"),
        new(
            FlashbackExportPlayback,
            DiagnosticSessionScenarioPlan.Create(runFlashbackExportPlayback: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-export-playback.mp4"),
        new(
            FlashbackSegmentPlayback,
            DiagnosticSessionScenarioPlan.Create(runFlashbackSegmentPlayback: true),
            RequiresPreview: true,
            RequiresFlashback: true),
        new(
            FlashbackRangeExport,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRangeExport: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-range-export.mp4"),
        new(
            FlashbackRangeExportAudioSwitch,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRangeExportAudioSwitch: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-range-export-audio-switch.mp4"),
        new(
            FlashbackLifecycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackLifecycle: true),
            RequiresPreview: true,
            RequiresFlashback: true),
        new(
            FlashbackExportConcurrent,
            DiagnosticSessionScenarioPlan.Create(runFlashbackExportConcurrent: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-concurrent-a.mp4"),
        new(
            FlashbackDisableDuringExport,
            DiagnosticSessionScenarioPlan.Create(runFlashbackDisableDuringExport: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-disable-during-export.mp4"),
        new(
            FlashbackRotatedExport,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRotatedExport: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-rotated-export.mp4"),
        new(
            FlashbackPreviewCycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackPreviewCycle: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-preview-off-export.mp4"),
        new(
            FlashbackPlaybackPreviewCycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackPlaybackPreviewCycle: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-playback-preview-cycle.mp4"),
        new(
            FlashbackRecording,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRecording: true),
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true),
        new(
            FlashbackRecordingPreviewCycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRecordingPreviewCycle: true),
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true),
        new(
            FlashbackRecordingSettingsDeferred,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRecordingSettingsDeferred: true),
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true),
        new(
            FlashbackRecordingExportRejected,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRecordingExportRejected: true),
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true),
        new(
            FlashbackExportRejected,
            DiagnosticSessionScenarioPlan.Create(runFlashbackExportRejected: true)),
        new(
            Combined,
            DiagnosticSessionScenarioPlan.Create(runCombined: true),
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true)
    ];
}

internal readonly record struct DiagnosticSessionScenarioCatalogEntry(
    string Name,
    DiagnosticSessionScenarioPlan Plan = default,
    bool RequiresPreview = false,
    bool RequiresRecording = false,
    bool RequiresFlashback = false,
    string? FlashbackExportVerificationFileName = null);
