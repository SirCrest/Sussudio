using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackBufferManager_CleansStaleSessionDirectories()
    {
        var bufferText = ReadFlashbackBufferManagerSource();
        var cleanupText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackStartupCacheCleanup.cs")
            .Replace("\r\n", "\n");
        var scannerText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackSessionRecoveryScanner.cs")
            .Replace("\r\n", "\n");
        var playbackSegmentEdgesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackSegmentEdges.cs")
            .Replace("\r\n", "\n");

        // Constants/definitions now live in the extracted helper classes
        AssertContains(cleanupText, "internal static readonly TimeSpan StaleSessionMinAge = TimeSpan.FromHours(12);");
        AssertContains(cleanupText, "private const int MaxStaleSessionDirectoryScansPerInit = 64;");
        AssertContains(cleanupText, "private const int MaxStartupCacheSessionDirectoryScansPerInit = 256;");
        AssertContains(cleanupText, "private const int MaxStartupCacheSessionDirectoriesPerInit = 32;");
        AssertContains(cleanupText, "private const long StartupCacheBudgetMultiplier = 2;");
        AssertContains(cleanupText, "private const int MaxStaleRootSegmentFileScansPerInit = 512;");

        // Call sites remain in the FlashbackBufferManager partial family (now qualified)
        AssertContains(bufferText, "FlashbackStartupCacheCleanup.CleanupStaleRootSegmentFiles(tempDirectory);");
        AssertContains(bufferText, "FlashbackStartupCacheCleanup.CleanupStaleSessionDirectories(tempDirectory, sessionDirectory);");
        AssertContains(bufferText, "var cacheCleanup = FlashbackStartupCacheCleanup.CleanupSessionCacheBudget(");
        AssertContains(bufferText, "FlashbackStartupCacheCleanup.CalculateStartupTempCacheBudgetBytes(_options.MaxDiskBytes));");
        AssertContains(bufferText, "var sessionDirectory = FlashbackSessionRecoveryScanner.BuildSessionDirectory(tempDirectory, sessionId);");

        // Session directory helper definitions now in FlashbackSessionRecoveryScanner
        AssertContains(scannerText, "internal static string BuildSessionDirectory(string tempDirectory, string sessionId)");
        AssertContains(scannerText, "Session id must be a simple file-name component.");
        AssertContains(scannerText, "Session id must resolve inside the flashback temp directory.");
        AssertContains(scannerText, "internal static string NormalizeSegmentExtension(string extension)");
        AssertContains(scannerText, "Flashback segment extension must be .ts or .mp4.");
        AssertContains(scannerText, "internal static bool IsPathUnderDirectory(string fullPath, string fullDirectoryRoot)");
        AssertContains(scannerText, "internal static bool IsReparsePoint(FileSystemInfo info)");
        AssertContains(scannerText, "internal static bool IsPlausibleFlashbackSessionDirectoryName(string name)");

        // NormalizeSegmentExtension call site remains in the FlashbackBufferManager partial family (now qualified)
        AssertContains(bufferText, "var normalizedExtension = FlashbackSessionRecoveryScanner.NormalizeSegmentExtension(extension);");

        // TempDriveAvailableFreeBytes property delegates to the extracted class
        AssertContains(bufferText, "public long TempDriveAvailableFreeBytes => FlashbackStartupCacheCleanup.TryGetTempDriveAvailableFreeBytes(_options.TempDirectory);");

        // Log strings remain in the cleanup class
        AssertContains(cleanupText, "FLASHBACK_STALE_SESSION_SKIP reason=reparse_point");
        AssertContains(cleanupText, "FLASHBACK_STALE_SESSION_SKIP reason=unrecognized_empty_dir");
        AssertContains(cleanupText, "FLASHBACK_CACHE_BUDGET_SKIP reason=outside_temp");
        AssertContains(cleanupText, "FLASHBACK_SESSION_STATS_SKIP reason=reparse_point");
        AssertContains(cleanupText, "if (string.Equals(fullPath, currentFullPath, StringComparison.OrdinalIgnoreCase))");
        AssertContains(cleanupText, "FLASHBACK_CACHE_BUDGET_PRESERVE_SKIP");
        AssertContains(cleanupText, "FLASHBACK_CACHE_BUDGET_CLEANUP");
        AssertContains(cleanupText, "info.EnumerateFiles(\"fb_*\", SearchOption.TopDirectoryOnly)");
        AssertContains(cleanupText, "Directory.EnumerateFiles(tempDirectory, \"fb_*\", SearchOption.TopDirectoryOnly)");
        AssertContains(cleanupText, "Directory.Delete(fullPath, recursive: true);");

        // Segment lookup helpers remain in the FlashbackBufferManager partial family
        AssertContains(bufferText, "if (IsSameSegmentPath(_activeSegmentPath, currentPath))\n                return _activeSegmentPath != null && File.Exists(_activeSegmentPath) ? _activeSegmentPath : null;");
        AssertContains(bufferText, "return GetOldestExistingSegmentPath()\n                ?? (_activeSegmentPath != null && File.Exists(_activeSegmentPath) ? _activeSegmentPath : null);");
        AssertContains(bufferText, "public TimeSpan? GetSegmentStartPts(string path)");
        AssertContains(playbackSegmentEdgesText, "var nextSegmentStart = _bufferManager.GetSegmentStartPts(nextFile);");
        AssertContains(playbackSegmentEdgesText, "if (nextSegmentStart.HasValue && segSwitchTarget < nextSegmentStart.Value)");
        AssertContains(playbackSegmentEdgesText, "var currentSegmentStart = _bufferManager.GetSegmentStartPts(currentOpenFilePath);");
        AssertContains(playbackSegmentEdgesText, "if (currentSegmentStart.HasValue && resumeTarget < currentSegmentStart.Value)");

        return Task.CompletedTask;
    }
}
