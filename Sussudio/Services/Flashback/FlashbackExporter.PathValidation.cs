using System;
using System.IO;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    private static bool IsSamePath(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            return string.Equals(
                Path.GetFullPath(left),
                Path.GetFullPath(right),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_PATH_COMPARE_WARN left='{left}' right='{right}' type={ex.GetType().Name} msg='{ex.Message}'");
            return false;
        }
    }

    private static bool TryValidateOutputPath(string outputPath, out string fullOutputPath, out string failureMessage)
    {
        fullOutputPath = string.Empty;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            failureMessage = "Flashback export failed: output path is required.";
            return false;
        }

        try
        {
            fullOutputPath = Path.GetFullPath(outputPath);
        }
        catch (Exception ex)
        {
            failureMessage = $"Flashback export failed: output path is invalid '{outputPath}'.";
            Logger.Log($"FLASHBACK_EXPORT_PATH_VALIDATE_WARN path='{outputPath}' type={ex.GetType().Name} msg='{ex.Message}'");
            return false;
        }

        var outputDirectory = Path.GetDirectoryName(fullOutputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
        {
            failureMessage = $"Flashback export failed: output directory does not exist for '{outputPath}'.";
            return false;
        }

        if (Directory.Exists(fullOutputPath))
        {
            failureMessage = $"Flashback export failed: output path is a directory '{outputPath}'.";
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }

}
