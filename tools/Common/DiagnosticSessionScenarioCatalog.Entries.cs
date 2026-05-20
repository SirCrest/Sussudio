namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioCatalog
{
    internal static IReadOnlyList<DiagnosticSessionScenarioCatalogEntry> Entries { get; } =
    [
        .. CreateCoreScenarioEntries(),
        .. CreateFlashbackPlaybackScenarioEntries(),
        .. CreateFlashbackExportScenarioEntries(),
        .. CreateFlashbackRecordingScenarioEntries(),
        CreateCombinedScenarioEntry()
    ];
}

internal readonly record struct DiagnosticSessionScenarioCatalogEntry(
    string Name,
    DiagnosticSessionScenarioPlan Plan = default,
    bool RequiresPreview = false,
    bool RequiresRecording = false,
    bool RequiresFlashback = false,
    string? FlashbackExportVerificationFileName = null);
