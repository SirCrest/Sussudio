using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

// Sussudio.csproj targets net8.0-windows10.0.19041.0 while this test project
// targets net8.0 (see tests/Sussudio.Tests/MIGRATION.md); a ProjectReference
// would force a Windows/WinUI target onto the test rig, so these behavioral
// tests resolve Sussudio.Services.Flashback types via Assembly.LoadFrom against
// the staged Sussudio.dll + reflection, matching every other ported xUnit test
// in this project (e.g. FlashbackBufferManager_RejectsUnsafeSessionIds in
// XUnit.RecordingContractsTests.cs) rather than a direct `using` + `new`.
public sealed class FlashbackDiskPolicyTests
{
    [Theory]
    [InlineData((2L * 1024 * 1024 * 1024) - 1, true)]
    [InlineData(2L * 1024 * 1024 * 1024, false)]
    [InlineData((2L * 1024 * 1024 * 1024) + 1, false)]
    public void LowFreeSpace_UsesExclusiveTwoGibibyteThreshold(long freeBytes, bool expected)
    {
        var options = CreateOptions(freeDiskBytesProvider: () => freeBytes);
        using var manager = CreateManager(options);

        Assert.Equal(expected, GetBoolProperty(manager, "IsDiskSpaceLow"));
    }

    [Theory]
    [InlineData((512L * 1024 * 1024) - 1, true)]
    [InlineData(512L * 1024 * 1024, false)]
    [InlineData((512L * 1024 * 1024) + 1, false)]
    public void CriticallyLowFreeSpace_UsesExclusive512MebibyteThreshold(long freeBytes, bool expected)
    {
        var options = CreateOptions(freeDiskBytesProvider: () => freeBytes);
        using var manager = CreateManager(options);

        Assert.Equal(expected, GetBoolProperty(manager, "IsDiskCriticallyLow"));
    }

    [Fact]
    public void PreservedRecoveryDirectory_OlderThanRetention_IsDeleted()
    {
        var temp = Directory.CreateTempSubdirectory("fbtest_").FullName;
        try
        {
            var session = Path.Combine(temp, new string('a', 32));
            Directory.CreateDirectory(session);
            File.WriteAllText(Path.Combine(session, ".flashback-recovery-preserve"),
                DateTimeOffset.UtcNow.AddDays(-10).ToString("O"));
            File.WriteAllText(Path.Combine(session, "fb_dummy_0000.ts"), "x");
            File.SetLastWriteTimeUtc(Path.Combine(session, "fb_dummy_0000.ts"), DateTime.UtcNow.AddDays(-10));
            // The cleanup's staleness check maxes the directory mtime with the
            // fb_* file mtimes; creating the files above refreshed the directory,
            // so age it explicitly (must be last — file writes bump the dir).
            Directory.SetLastWriteTimeUtc(session, DateTime.UtcNow.AddDays(-10));

            InvokeCleanupStaleSessionDirectories(temp, Path.Combine(temp, "current"));

            Assert.False(Directory.Exists(session)); // expired preserve => reclaimed
        }
        finally { Directory.Delete(temp, recursive: true); }
    }

    [Fact]
    public void PreservedRecoveryDirectory_WithinRetention_IsKept()
    {
        var temp = Directory.CreateTempSubdirectory("fbtest_").FullName;
        try
        {
            var session = Path.Combine(temp, new string('b', 32));
            Directory.CreateDirectory(session);
            File.WriteAllText(Path.Combine(session, ".flashback-recovery-preserve"),
                DateTimeOffset.UtcNow.AddDays(-1).ToString("O"));
            File.WriteAllText(Path.Combine(session, "fb_dummy_0000.ts"), "x");
            File.SetLastWriteTimeUtc(Path.Combine(session, "fb_dummy_0000.ts"), DateTime.UtcNow.AddDays(-1));

            InvokeCleanupStaleSessionDirectories(temp, Path.Combine(temp, "current"));

            Assert.True(Directory.Exists(session));
        }
        finally { Directory.Delete(temp, recursive: true); }
    }

    // --- reflection helpers, private/file-local copies (do not modify shared
    // test infrastructure) -------------------------------------------------

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object CreateOptions(Func<long>? freeDiskBytesProvider = null)
    {
        var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
        var options = Activator.CreateInstance(optionsType)
            ?? throw new InvalidOperationException("Failed to create FlashbackBufferOptions.");
        if (freeDiskBytesProvider != null)
        {
            SetPropertyBackingField(options, "FreeDiskBytesProvider", freeDiskBytesProvider);
        }

        return options;
    }

    private static IDisposable CreateManager(object options)
    {
        var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        return (IDisposable)(Activator.CreateInstance(managerType, new[] { options })
            ?? throw new InvalidOperationException("Failed to create FlashbackBufferManager."));
    }

    private static void InvokeCleanupStaleSessionDirectories(string tempDirectory, string currentSessionDirectory)
    {
        var cleanupType = RequireType("Sussudio.Services.Flashback.FlashbackStartupCacheCleanup");
        var method = cleanupType.GetMethod(
            "CleanupStaleSessionDirectories",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("CleanupStaleSessionDirectories not found.");
        method.Invoke(null, new object[] { tempDirectory, currentSessionDirectory });
    }

    private static void SetPropertyBackingField(object instance, string name, object? value)
    {
        var field = instance.GetType().GetField($"<{name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{instance.GetType().Name}.{name} backing field not found.");
        field.SetValue(instance, value);
    }

    private static bool GetBoolProperty(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"{instance.GetType().Name}.{name} not found.");
        return (bool)property.GetValue(instance)!;
    }
}
