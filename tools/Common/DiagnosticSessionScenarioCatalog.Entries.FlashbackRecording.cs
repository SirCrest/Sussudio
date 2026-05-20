namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioCatalog
{
    private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackRecordingScenarioEntries()
        => [
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
            DiagnosticSessionScenarioPlan.Create(runFlashbackExportRejected: true))
    ];
}
