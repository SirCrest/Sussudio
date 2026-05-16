using System;
using System.Diagnostics;
using System.IO;

namespace Sussudio.ViewModels;

internal static class OutputDriveSpacePresentationBuilder
{
    internal static string Build(string outputPath)
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(outputPath) ?? "C:");
            var freeGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            return $"Free: {freeGb:F1} GB";
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Suppressed exception in MainViewModel.RefreshDiskSpace: {ex.Message}");
            return "";
        }
    }
}
