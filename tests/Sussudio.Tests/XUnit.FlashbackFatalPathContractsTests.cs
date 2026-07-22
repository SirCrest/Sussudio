using System;
using System.IO;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackFatalPathContractsTests
{
    private static string ReadCaptureServiceSource() =>
        File.ReadAllText(TestPaths.Repo("Sussudio/Services/Capture/CaptureService.cs"));

    [Fact]
    public void FlashbackBackendCleanup_DoesNotPurgeSegments()
    {
        var source = ReadCaptureServiceSource();
        var cleanup = SourceSlice.Method(source, "private void BeginFlashbackBackendCleanup");
        Assert.Contains("purgeSegments: false", cleanup);
        Assert.DoesNotContain("purgeSegments: true", cleanup);
    }

    [Fact]
    public void FlashbackBackendCleanup_PreservesRecoverySegmentsForAllFatalErrors()
    {
        var source = ReadCaptureServiceSource();
        var cleanup = SourceSlice.Method(source, "private void BeginFlashbackBackendCleanup");
        // Preserve must run unconditionally, not only inside the IsGpuDeviceLost branch.
        Assert.Contains("PreserveRecoverySegments(\"backend_fatal\")", cleanup);
    }

    [Fact]
    public void FlashbackBackendCleanup_SchedulesBoundedAutoRestart()
    {
        var source = ReadCaptureServiceSource();
        var cleanup = SourceSlice.Method(source, "private void BeginFlashbackBackendCleanup");
        Assert.Contains("TryScheduleFlashbackAutoRestart", cleanup);

        var flashbackSource = File.ReadAllText(TestPaths.Repo("Sussudio/Services/Capture/CaptureService.Flashback.cs"));
        Assert.Contains("MaxFlashbackAutoRestartAttempts = 2", flashbackSource);
        Assert.Contains("FLASHBACK_AUTO_RESTART", flashbackSource);
    }
}

// TestPaths/SourceSlice do not exist as shared helpers in the test project (the
// established convention here is Assembly.LoadFrom + reflection against the
// staged Sussudio.dll — see MIGRATION.md — rather than a compile-time
// ProjectReference or a shared source-slicing utility). Per the plan's fallback
// instruction, these are private, file-scoped copies rather than edits to any
// shared test file.
file static class TestPaths
{
    public static string Repo(string relativePath) => Path.Combine(FindRepoRoot(), relativePath);

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sussudio.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate repository root from '{AppContext.BaseDirectory}'.");
    }
}

file static class SourceSlice
{
    /// <summary>
    /// Returns the source text of the method whose declaration starts with
    /// <paramref name="signaturePrefix"/> (e.g. "private void Foo"), from its
    /// signature through the matching closing brace of its body.
    /// </summary>
    public static string Method(string source, string signaturePrefix)
    {
        var start = source.IndexOf(signaturePrefix, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Could not find method starting with '{signaturePrefix}'.");
        }

        var braceOpen = source.IndexOf('{', start);
        if (braceOpen < 0)
        {
            throw new InvalidOperationException($"Could not find method body open brace for '{signaturePrefix}'.");
        }

        var depth = 0;
        for (var i = braceOpen; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(start, i - start + 1);
                }
            }
        }

        throw new InvalidOperationException($"Could not find matching closing brace for '{signaturePrefix}'.");
    }
}
