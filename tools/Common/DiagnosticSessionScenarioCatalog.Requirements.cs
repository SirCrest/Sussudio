namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioCatalog
{
    internal static bool NeedsPreview(string scenario)
        => TryGetEntry(scenario, out var entry) && entry.RequiresPreview;

    internal static bool NeedsRecording(string scenario)
        => TryGetEntry(scenario, out var entry) && entry.RequiresRecording;

    internal static bool NeedsFlashback(string scenario)
        => TryGetEntry(scenario, out var entry) && entry.RequiresFlashback;

    internal static bool TryGetFlashbackExportVerificationPath(
        string scenario,
        string outputDirectory,
        out string exportPath)
    {
        var fileName = TryGetEntry(scenario, out var entry)
            ? entry.FlashbackExportVerificationFileName
            : null;
        exportPath = fileName is null ? string.Empty : Path.Combine(outputDirectory, fileName);

        return exportPath.Length > 0;
    }
}
