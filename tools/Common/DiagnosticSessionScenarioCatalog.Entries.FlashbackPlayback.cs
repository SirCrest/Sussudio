namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioCatalog
{
    private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackPlaybackScenarioEntries()
        => [
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
            RequiresFlashback: true)
    ];
}
