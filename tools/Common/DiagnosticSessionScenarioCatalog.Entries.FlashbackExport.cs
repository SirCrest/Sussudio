namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioCatalog
{
    private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackExportScenarioEntries()
        => [
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
            FlashbackExportVerificationFileName: "flashback-playback-preview-cycle.mp4")
    ];
}
