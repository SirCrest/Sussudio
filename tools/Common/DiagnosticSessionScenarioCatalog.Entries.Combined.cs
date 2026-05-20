namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioCatalog
{
    private static DiagnosticSessionScenarioCatalogEntry CreateCombinedScenarioEntry()
        => new(
            Combined,
            DiagnosticSessionScenarioPlan.Create(runCombined: true),
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true);
}
