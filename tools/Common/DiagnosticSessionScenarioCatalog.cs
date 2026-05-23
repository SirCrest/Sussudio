namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioCatalog
{
    internal static string Normalize(string? scenario)
    {
        var normalized = string.IsNullOrWhiteSpace(scenario)
            ? Observe
            : scenario.Trim().ToLowerInvariant();

        if (TryGetEntry(normalized, out _))
        {
            return normalized;
        }

        throw new ArgumentException($"Unknown diagnostic session scenario '{scenario}'.", nameof(scenario));
    }

    internal static bool TryGetEntry(string scenario, out DiagnosticSessionScenarioCatalogEntry entry)
    {
        foreach (var candidate in Entries)
        {
            if (string.Equals(candidate.Name, scenario, StringComparison.Ordinal))
            {
                entry = candidate;
                return true;
            }
        }

        entry = default;
        return false;
    }

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
