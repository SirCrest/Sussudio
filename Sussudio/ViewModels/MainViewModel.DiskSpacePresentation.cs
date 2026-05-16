using System;
using System.Diagnostics;
using System.IO;

namespace Sussudio.ViewModels;

/// <summary>
/// Output drive free-space projection for the presentation surface.
/// </summary>
public partial class MainViewModel
{
    private void UpdateDiskSpace()
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(OutputPath) ?? "C:");
            var freeGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            DiskSpaceInfo = $"Free: {freeGb:F1} GB";
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Suppressed exception in MainViewModel.RefreshDiskSpace: {ex.Message}");
            DiskSpaceInfo = "";
        }
    }
}
