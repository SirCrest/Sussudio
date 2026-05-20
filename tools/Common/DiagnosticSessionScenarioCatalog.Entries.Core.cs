namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioCatalog
{
    private static DiagnosticSessionScenarioCatalogEntry[] CreateCoreScenarioEntries()
        => [
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
            FlashbackExportVerificationFileName: "flashback-stress-export.mp4")
    ];
}
