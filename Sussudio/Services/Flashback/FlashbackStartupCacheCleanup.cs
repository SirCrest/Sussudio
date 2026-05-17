using System;
using System.IO;
using System.Linq;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Startup-time cache-cleanup pipeline for the flashback temp directory.
/// Removes stale sessions and probes free space so the next recording session
/// starts with headroom.
/// </summary>
internal static class FlashbackStartupCacheCleanup
{
    internal static readonly TimeSpan StaleSessionMinAge = TimeSpan.FromHours(12);
    private const int MaxStaleSessionDirectoryScansPerInit = 64;
    private const int MaxStaleSessionDirectoriesPerInit = 16;
    private const int MaxStaleRootSegmentFileScansPerInit = 512;
    private const int MaxStaleRootSegmentFilesPerInit = 128;
    private const string RecoveryPreserveMarkerFileName = ".flashback-recovery-preserve";

    internal static void CleanupStaleSessionDirectories(string tempDirectory, string currentSessionDirectory)
    {
        try
        {
            var tempRoot = FlashbackSessionRecoveryScanner.EnsureTrailingDirectorySeparator(Path.GetFullPath(tempDirectory));
            var currentFullPath = Path.GetFullPath(currentSessionDirectory);
            var nowUtc = DateTime.UtcNow;
            var scannedCount = 0;
            var deletedCount = 0;
            long freedBytes = 0;

            foreach (var directory in Directory.EnumerateDirectories(tempDirectory))
            {
                if (scannedCount >= MaxStaleSessionDirectoryScansPerInit ||
                    deletedCount >= MaxStaleSessionDirectoriesPerInit)
                {
                    break;
                }

                scannedCount++;

                var fullPath = Path.GetFullPath(directory);
                if (!FlashbackSessionRecoveryScanner.IsPathUnderDirectory(fullPath, tempRoot))
                {
                    Logger.Log($"FLASHBACK_STALE_SESSION_SKIP reason=outside_temp dir='{fullPath}'");
                    continue;
                }

                if (string.Equals(fullPath, currentFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var info = new DirectoryInfo(fullPath);
                if (!info.Exists)
                {
                    continue;
                }

                if (FlashbackSessionRecoveryScanner.IsReparsePoint(info))
                {
                    Logger.Log($"FLASHBACK_STALE_SESSION_SKIP reason=reparse_point dir='{fullPath}'");
                    continue;
                }

                if (File.Exists(Path.Combine(fullPath, RecoveryPreserveMarkerFileName)))
                {
                    Logger.Log($"FLASHBACK_STALE_SESSION_PRESERVE_SKIP dir='{fullPath}'");
                    continue;
                }

                var latestActivityUtc = info.LastWriteTimeUtc;
                long directoryBytes = 0;
                var looksLikeFlashbackSession = false;
                foreach (var file in info.EnumerateFiles("fb_*", SearchOption.TopDirectoryOnly))
                {
                    looksLikeFlashbackSession = true;
                    latestActivityUtc = latestActivityUtc > file.LastWriteTimeUtc
                        ? latestActivityUtc
                        : file.LastWriteTimeUtc;
                    directoryBytes = AddNonNegativeSaturated(directoryBytes, file.Length);
                }

                if (!looksLikeFlashbackSession)
                {
                    if (info.EnumerateFileSystemInfos().Any())
                    {
                        continue;
                    }

                    if (!FlashbackSessionRecoveryScanner.IsPlausibleFlashbackSessionDirectoryName(info.Name))
                    {
                        Logger.Log($"FLASHBACK_STALE_SESSION_SKIP reason=unrecognized_empty_dir dir='{fullPath}'");
                        continue;
                    }
                }

                if (nowUtc - latestActivityUtc < StaleSessionMinAge)
                {
                    continue;
                }

                try
                {
                    Directory.Delete(fullPath, recursive: true);
                    deletedCount++;
                    freedBytes = AddNonNegativeSaturated(freedBytes, directoryBytes);
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_STALE_SESSION_DELETE_WARN dir='{fullPath}' type={ex.GetType().Name} msg={ex.Message}");
                }
            }

            if (deletedCount > 0)
            {
                Logger.Log($"FLASHBACK_STALE_SESSION_CLEANUP deleted={deletedCount} freed_bytes={freedBytes}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_STALE_SESSION_CLEANUP_WARN type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    internal static long TryGetTempDriveAvailableFreeBytes(string tempDirectory)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(tempDirectory));
            if (string.IsNullOrWhiteSpace(root))
            {
                return -1;
            }

            return new DriveInfo(root).AvailableFreeSpace;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_TEMP_DRIVE_FREE_SPACE_WARN dir='{tempDirectory}' type={ex.GetType().Name} msg={ex.Message}");
            return -1;
        }
    }

    internal static void CleanupStaleRootSegmentFiles(string tempDirectory)
    {
        try
        {
            var nowUtc = DateTime.UtcNow;
            var scannedCount = 0;
            var deletedCount = 0;
            long freedBytes = 0;

            foreach (var filePath in Directory.EnumerateFiles(tempDirectory, "fb_*", SearchOption.TopDirectoryOnly))
            {
                if (scannedCount >= MaxStaleRootSegmentFileScansPerInit ||
                    deletedCount >= MaxStaleRootSegmentFilesPerInit)
                {
                    break;
                }

                scannedCount++;

                var info = new FileInfo(filePath);
                if (!info.Exists || nowUtc - info.LastWriteTimeUtc < StaleSessionMinAge)
                {
                    continue;
                }

                try
                {
                    var length = info.Length;
                    info.Delete();
                    deletedCount++;
                    freedBytes = AddNonNegativeSaturated(freedBytes, length);
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_STALE_ROOT_SEGMENT_DELETE_WARN file='{filePath}' type={ex.GetType().Name} msg={ex.Message}");
                }
            }

            if (deletedCount > 0)
            {
                Logger.Log($"FLASHBACK_STALE_ROOT_SEGMENT_CLEANUP deleted={deletedCount} freed_bytes={freedBytes}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_STALE_ROOT_SEGMENT_CLEANUP_WARN type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private static long AddNonNegativeSaturated(long left, long right)
    {
        left = Math.Max(0, left);
        right = Math.Max(0, right);
        return left > long.MaxValue - right ? long.MaxValue : left + right;
    }
}
