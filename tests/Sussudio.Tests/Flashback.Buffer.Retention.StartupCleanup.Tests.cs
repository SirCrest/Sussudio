using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackBufferManager_AbandonsStartupGeneratedSegmentPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_startup_abandon_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            SetPrivateField(manager, "_activeSegmentPath", null);
            var startingIndex = (int)GetPrivateField(manager, "_nextSegmentIndex")!;
            var getFilePath = manager.GetType().GetMethod("AcquireSegmentPath", new[] { typeof(bool).MakeByRefType() })
                ?? throw new InvalidOperationException("FlashbackBufferManager.AcquireSegmentPath(out bool) not found.");
            var abandonGenerated = manager.GetType().GetMethod("AbandonGeneratedSegmentPath")
                ?? throw new InvalidOperationException("FlashbackBufferManager.AbandonGeneratedSegmentPath not found.");

            object?[] args = { false };
            var generatedPath = (string)getFilePath.Invoke(manager, args)!;
            AssertEqual(true, (bool)args[0]!, "Fresh AcquireSegmentPath reports generated path");
            AssertEqual(generatedPath, (string)GetPrivateField(manager, "_activeSegmentPath")!, "Generated path becomes raw active segment");
            AssertEqual(startingIndex + 1, (int)GetPrivateField(manager, "_nextSegmentIndex")!, "Generated path advances segment index");

            File.WriteAllBytes(generatedPath, new byte[17]);
            abandonGenerated.Invoke(manager, new object?[] { generatedPath, null });

            AssertEqual<string?>(null, (string?)GetPrivateField(manager, "_activeSegmentPath"), "Abandon clears startup-generated active path");
            AssertEqual(false, File.Exists(generatedPath), "Abandon deletes partial startup segment file");
            AssertEqual(startingIndex, (int)GetPrivateField(manager, "_nextSegmentIndex")!, "Abandon rolls back generated segment index");

            object?[] retryArgs = { false };
            var retryPath = (string)getFilePath.Invoke(manager, retryArgs)!;
            AssertEqual(true, (bool)retryArgs[0]!, "Retry after abandon generates a fresh path");
            AssertEqual(generatedPath, retryPath, "Retry reuses the rolled-back segment slot");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_RemovesStaleLegacyRootSegments()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_legacy_cleanup_{Guid.NewGuid():N}");
        object? manager = null;
        Directory.CreateDirectory(tempDir);

        try
        {
            var staleRootSegment = Path.Combine(tempDir, "fb_legacy_0001.ts");
            var recentRootSegment = Path.Combine(tempDir, "fb_recent_0001.ts");
            var unrelatedFile = Path.Combine(tempDir, "unrelated.ts");
            File.WriteAllText(staleRootSegment, "stale");
            File.WriteAllText(recentRootSegment, "recent");
            File.WriteAllText(unrelatedFile, "keep");

            File.SetLastWriteTimeUtc(staleRootSegment, DateTime.UtcNow - TimeSpan.FromHours(13));
            File.SetLastWriteTimeUtc(recentRootSegment, DateTime.UtcNow);
            File.SetLastWriteTimeUtc(unrelatedFile, DateTime.UtcNow - TimeSpan.FromHours(13));

            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            manager = Activator.CreateInstance(managerType, new[] { options })!;
            var initialize = managerType.GetMethod("Initialize")
                ?? throw new InvalidOperationException("FlashbackBufferManager.Initialize not found.");
            initialize.Invoke(manager, new object[] { "current-session" });

            AssertEqual(false, File.Exists(staleRootSegment), "Stale root fb_* segment removed");
            AssertEqual(true, File.Exists(recentRootSegment), "Recent root fb_* segment preserved");
            AssertEqual(true, File.Exists(unrelatedFile), "Unrelated root file preserved");
            AssertEqual(true, Directory.Exists(Path.Combine(tempDir, "current-session")), "Current session directory created");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_PreservesUnrelatedEmptyTempDirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_stale_empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var currentSession = Path.Combine(tempDir, Guid.NewGuid().ToString("N"));
            var staleFlashbackSession = Path.Combine(tempDir, Guid.NewGuid().ToString("N"));
            var unrelatedEmptyDirectory = Path.Combine(tempDir, "empty-but-not-flashback");

            Directory.CreateDirectory(currentSession);
            Directory.CreateDirectory(staleFlashbackSession);
            Directory.CreateDirectory(unrelatedEmptyDirectory);

            var staleTime = DateTime.UtcNow - TimeSpan.FromHours(13);
            Directory.SetLastWriteTimeUtc(staleFlashbackSession, staleTime);
            Directory.SetLastWriteTimeUtc(unrelatedEmptyDirectory, staleTime);

            var cleanupType = RequireType("Sussudio.Services.Flashback.FlashbackStartupCacheCleanup");
            var cleanup = cleanupType.GetMethod("CleanupStaleSessionDirectories", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException("CleanupStaleSessionDirectories not found.");

            cleanup.Invoke(null, new object[] { tempDir, currentSession });

            AssertEqual(true, Directory.Exists(currentSession), "Current empty session directory preserved");
            AssertEqual(false, Directory.Exists(staleFlashbackSession), "Plausible stale empty flashback session removed");
            AssertEqual(true, Directory.Exists(unrelatedEmptyDirectory), "Unrelated stale empty directory preserved");

            var cleanupSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Sussudio", "Services", "Flashback", "FlashbackStartupCacheCleanup.cs"))
                .Replace("\r\n", "\n");
            var scannerSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Sussudio", "Services", "Flashback", "FlashbackSessionRecoveryScanner.cs"))
                .Replace("\r\n", "\n");
            AssertContains(cleanupSource, "FLASHBACK_STALE_SESSION_SKIP reason=unrecognized_empty_dir");
            AssertContains(scannerSource, "internal static bool IsPlausibleFlashbackSessionDirectoryName(string name)");
            AssertContains(scannerSource, "internal static bool IsLowerHexString(ReadOnlySpan<char> value)");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_TrimsStartupSessionCacheBudget()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_cache_budget_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var currentSession = Path.Combine(tempDir, "current-session");
            var oldSession = Path.Combine(tempDir, "old-session");
            var recentSession = Path.Combine(tempDir, "recent-session");
            var preservedSession = Path.Combine(tempDir, "preserved-session");
            var nonFlashbackDirectory = Path.Combine(tempDir, "not-flashback");

            Directory.CreateDirectory(currentSession);
            Directory.CreateDirectory(oldSession);
            Directory.CreateDirectory(recentSession);
            Directory.CreateDirectory(preservedSession);
            Directory.CreateDirectory(nonFlashbackDirectory);

            WriteSizedFile(Path.Combine(currentSession, "fb_current_0001.ts"), 1);
            WriteSizedFile(Path.Combine(oldSession, "fb_old_0001.ts"), 20);
            WriteSizedFile(Path.Combine(recentSession, "fb_recent_0001.ts"), 10);
            WriteSizedFile(Path.Combine(preservedSession, "fb_preserved_0001.ts"), 100);
            File.WriteAllText(Path.Combine(preservedSession, ".flashback-recovery-preserve"), "keep");
            File.WriteAllText(Path.Combine(nonFlashbackDirectory, "notes.txt"), "keep");

            var now = DateTime.UtcNow;
            File.SetLastWriteTimeUtc(Path.Combine(oldSession, "fb_old_0001.ts"), now - TimeSpan.FromHours(2));
            File.SetLastWriteTimeUtc(Path.Combine(recentSession, "fb_recent_0001.ts"), now - TimeSpan.FromMinutes(5));

            var cleanupType = RequireType("Sussudio.Services.Flashback.FlashbackStartupCacheCleanup");
            var cleanup = cleanupType.GetMethod("CleanupSessionCacheBudget", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException("CleanupSessionCacheBudget not found.");

            cleanup.Invoke(null, new object[] { tempDir, currentSession, 25L });

            AssertEqual(true, Directory.Exists(currentSession), "Current session preserved");
            AssertEqual(false, Directory.Exists(oldSession), "Oldest session removed to satisfy budget");
            AssertEqual(true, Directory.Exists(recentSession), "Recent session preserved once budget is satisfied");
            AssertEqual(true, Directory.Exists(preservedSession), "Recovery-preserved session skipped");
            AssertEqual(true, Directory.Exists(nonFlashbackDirectory), "Non-flashback directory preserved");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        return Task.CompletedTask;
    }
}
