using System;
using System.Collections.Generic;
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

    /// <summary>
    /// A recovery-preserve marker (set unconditionally on any fatal flashback error,
    /// see CaptureService.BeginFlashbackBackendCleanup) keeps a session directory
    /// alive across startup cleanup passes, but only for this long - otherwise a
    /// preserved session would leak disk space forever.
    /// </summary>
    internal static readonly TimeSpan RecoveryPreserveRetention = TimeSpan.FromDays(7);

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

                if (IsPreserveMarkerActive(fullPath, nowUtc))
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

    /// <summary>
    /// True when <paramref name="sessionDirectory"/> carries an active (unexpired)
    /// recovery-preserve marker. Also used by <see cref="FlashbackStartupSessionCacheBudget"/>
    /// so both startup cleanup passes age preserved sessions the same way.
    /// </summary>
    internal static bool IsPreserveMarkerActive(string sessionDirectory, DateTime nowUtc)
    {
        var markerPath = Path.Combine(sessionDirectory, RecoveryPreserveMarkerFileName);
        if (!File.Exists(markerPath))
        {
            return false;
        }

        DateTime markerUtc;
        try
        {
            var text = File.ReadAllText(markerPath).Trim();
            markerUtc = DateTimeOffset.TryParse(text, out var parsed)
                ? parsed.UtcDateTime
                : File.GetLastWriteTimeUtc(markerPath);
        }
        catch
        {
            markerUtc = File.GetLastWriteTimeUtc(markerPath);
        }

        if (nowUtc - markerUtc <= RecoveryPreserveRetention)
        {
            return true;
        }

        Logger.Log($"FLASHBACK_RECOVERY_PRESERVE_EXPIRED dir='{sessionDirectory}' age_days={(nowUtc - markerUtc).TotalDays:F1}");
        return false;
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

/// <summary>
/// Startup-time budget enforcement for flashback session directories.
/// </summary>
internal static class FlashbackStartupSessionCacheBudget
{
    private const int MaxStartupCacheSessionDirectoryScansPerInit = 256;
    private const int MaxStartupCacheSessionDirectoriesPerInit = 32;
    private const long StartupCacheBudgetMultiplier = 2;

    internal record StartupCacheCleanupResult(
        long BudgetBytes,
        long RemainingBytes,
        int SessionCount,
        int DeletedSessionCount,
        long FreedBytes);

    private record StartupCacheCandidate(string Path, DateTime LastActivityUtc, long SizeBytes);

    internal static long CalculateStartupTempCacheBudgetBytes(long sessionMaxDiskBytes)
    {
        if (sessionMaxDiskBytes <= 0)
        {
            return 0;
        }

        return sessionMaxDiskBytes > long.MaxValue / StartupCacheBudgetMultiplier
            ? long.MaxValue
            : sessionMaxDiskBytes * StartupCacheBudgetMultiplier;
    }

    internal static StartupCacheCleanupResult CleanupSessionCacheBudget(string tempDirectory, string currentSessionDirectory, long maxCacheBytes)
    {
        if (maxCacheBytes <= 0)
        {
            return new StartupCacheCleanupResult(0, 0, 0, 0, 0);
        }

        try
        {
            var tempRoot = FlashbackSessionRecoveryScanner.EnsureTrailingDirectorySeparator(Path.GetFullPath(tempDirectory));
            var currentFullPath = Path.GetFullPath(currentSessionDirectory);
            var nowUtc = DateTime.UtcNow;
            var candidates = new List<StartupCacheCandidate>();
            var scannedCount = 0;
            var deletedCount = 0;
            var sessionCount = 0;
            long freedBytes = 0;
            long totalCacheBytes = TryGetFlashbackSessionDirectoryStats(
                currentFullPath,
                out _,
                out var currentBytes,
                out _)
                ? currentBytes
                : 0;
            if (currentBytes > 0)
            {
                sessionCount++;
            }

            foreach (var directory in Directory.EnumerateDirectories(tempDirectory))
            {
                if (scannedCount >= MaxStartupCacheSessionDirectoryScansPerInit)
                {
                    break;
                }

                scannedCount++;

                var fullPath = Path.GetFullPath(directory);
                if (!FlashbackSessionRecoveryScanner.IsPathUnderDirectory(fullPath, tempRoot))
                {
                    Logger.Log($"FLASHBACK_CACHE_BUDGET_SKIP reason=outside_temp dir='{fullPath}'");
                    continue;
                }

                if (string.Equals(fullPath, currentFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (FlashbackStartupCacheCleanup.IsPreserveMarkerActive(fullPath, nowUtc))
                {
                    Logger.Log($"FLASHBACK_CACHE_BUDGET_PRESERVE_SKIP dir='{fullPath}'");
                    continue;
                }

                if (!TryGetFlashbackSessionDirectoryStats(fullPath, out var latestActivityUtc, out var directoryBytes, out var hasFiles))
                {
                    continue;
                }

                if (!hasFiles || directoryBytes <= 0)
                {
                    continue;
                }

                totalCacheBytes = AddNonNegativeSaturated(totalCacheBytes, directoryBytes);
                sessionCount++;
                candidates.Add(new StartupCacheCandidate(fullPath, latestActivityUtc, directoryBytes));
            }

            if (totalCacheBytes <= maxCacheBytes)
            {
                return new StartupCacheCleanupResult(maxCacheBytes, totalCacheBytes, sessionCount, 0, 0);
            }

            foreach (var candidate in candidates.OrderBy(candidate => candidate.LastActivityUtc))
            {
                if (deletedCount >= MaxStartupCacheSessionDirectoriesPerInit || totalCacheBytes <= maxCacheBytes)
                {
                    break;
                }

                try
                {
                    Directory.Delete(candidate.Path, recursive: true);
                    deletedCount++;
                    freedBytes = AddNonNegativeSaturated(freedBytes, candidate.SizeBytes);
                    totalCacheBytes = SubtractNonNegative(totalCacheBytes, candidate.SizeBytes);
                    sessionCount = Math.Max(0, sessionCount - 1);
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_CACHE_BUDGET_DELETE_WARN dir='{candidate.Path}' type={ex.GetType().Name} msg={ex.Message}");
                }
            }

            if (deletedCount > 0)
            {
                Logger.Log($"FLASHBACK_CACHE_BUDGET_CLEANUP deleted={deletedCount} freed_bytes={freedBytes} remaining_bytes={totalCacheBytes} budget_bytes={maxCacheBytes}");
            }

            return new StartupCacheCleanupResult(maxCacheBytes, totalCacheBytes, sessionCount, deletedCount, freedBytes);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CACHE_BUDGET_CLEANUP_WARN type={ex.GetType().Name} msg={ex.Message}");
            return new StartupCacheCleanupResult(maxCacheBytes, 0, 0, 0, 0);
        }
    }

    internal static bool TryGetFlashbackSessionDirectoryStats(
        string fullPath,
        out DateTime latestActivityUtc,
        out long directoryBytes,
        out bool hasFiles)
    {
        latestActivityUtc = DateTime.MinValue;
        directoryBytes = 0;
        hasFiles = false;

        try
        {
            var info = new DirectoryInfo(fullPath);
            if (!info.Exists)
            {
                return false;
            }

            if (FlashbackSessionRecoveryScanner.IsReparsePoint(info))
            {
                Logger.Log($"FLASHBACK_SESSION_STATS_SKIP reason=reparse_point dir='{fullPath}'");
                return false;
            }

            latestActivityUtc = info.LastWriteTimeUtc;
            var looksLikeFlashbackSession = false;
            foreach (var file in info.EnumerateFiles("fb_*", SearchOption.TopDirectoryOnly))
            {
                looksLikeFlashbackSession = true;
                hasFiles = true;
                latestActivityUtc = latestActivityUtc > file.LastWriteTimeUtc
                    ? latestActivityUtc
                    : file.LastWriteTimeUtc;
                directoryBytes = AddNonNegativeSaturated(directoryBytes, file.Length);
            }

            if (!looksLikeFlashbackSession)
            {
                if (info.EnumerateFileSystemInfos().Any())
                {
                    return false;
                }

                return FlashbackSessionRecoveryScanner.IsPlausibleFlashbackSessionDirectoryName(info.Name);
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SESSION_STATS_WARN dir='{fullPath}' type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }
    }

    private static long AddNonNegativeSaturated(long left, long right)
    {
        left = Math.Max(0, left);
        right = Math.Max(0, right);
        return left > long.MaxValue - right ? long.MaxValue : left + right;
    }

    private static long SubtractNonNegative(long left, long right)
    {
        left = Math.Max(0, left);
        right = Math.Max(0, right);
        return left <= right ? 0 : left - right;
    }
}

/// <summary>
/// Session-directory naming and probing helpers used during flashback initialization
/// and recovery-directory scanning.
/// </summary>
internal static class FlashbackSessionRecoveryScanner
{
    internal static string BuildSessionDirectory(string tempDirectory, string sessionId)
    {
        if (Path.IsPathRooted(sessionId) ||
            sessionId.IndexOf(Path.DirectorySeparatorChar) >= 0 ||
            sessionId.IndexOf(Path.AltDirectorySeparatorChar) >= 0 ||
            sessionId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Session id must be a simple file-name component.", nameof(sessionId));
        }

        var tempRoot = EnsureTrailingDirectorySeparator(Path.GetFullPath(tempDirectory));
        var sessionDirectory = Path.GetFullPath(Path.Combine(tempRoot, sessionId));
        if (!IsPathUnderDirectory(sessionDirectory, tempRoot))
        {
            throw new ArgumentException("Session id must resolve inside the flashback temp directory.", nameof(sessionId));
        }

        return sessionDirectory;
    }

    internal static string NormalizeSegmentExtension(string extension)
    {
        if (string.Equals(extension, ".ts", StringComparison.OrdinalIgnoreCase))
        {
            return ".ts";
        }

        if (string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase))
        {
            return ".mp4";
        }

        throw new ArgumentException("Flashback segment extension must be .ts or .mp4.", nameof(extension));
    }

    internal static string EnsureTrailingDirectorySeparator(string path)
        => Path.EndsInDirectorySeparator(path) ? path : path + Path.DirectorySeparatorChar;

    internal static bool IsPlausibleFlashbackSessionDirectoryName(string name)
    {
        if (name.Length == 32)
        {
            return IsLowerHexString(name);
        }

        var underscore = name.IndexOf('_');
        return underscore > 0 &&
               underscore < name.Length - 1 &&
               IsLowerHexString(name.AsSpan(0, underscore)) &&
               name.AsSpan(underscore + 1).Length == 32 &&
               IsLowerHexString(name.AsSpan(underscore + 1));
    }

    internal static bool IsLowerHexString(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return false;
        }

        foreach (var ch in value)
        {
            if (!IsLowerHexDigit(ch))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsLowerHexDigit(char value)
        => value is >= '0' and <= '9' or >= 'a' and <= 'f';

    internal static bool IsPathUnderDirectory(string fullPath, string fullDirectoryRoot)
        => fullPath.StartsWith(fullDirectoryRoot, StringComparison.OrdinalIgnoreCase);

    internal static bool IsReparsePoint(FileSystemInfo info)
        => (info.Attributes & FileAttributes.ReparsePoint) != 0;
}
