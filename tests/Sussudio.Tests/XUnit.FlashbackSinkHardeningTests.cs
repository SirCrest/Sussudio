using System;
using System.IO;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackSinkHardeningTests
{
    private static string Source() =>
        File.ReadAllText(TestPaths.Repo("Sussudio/Services/Flashback/FlashbackEncoderSink.cs"));

    [Fact]
    public void VideoEnqueue_DuringForceRotateDrain_UsesQueueGuardRatio()
    {
        var method = SourceSlice.Method(Source(), "private string? GetVideoEnqueueRejectReason");
        // Unconditional rejection is the bug; the guard must consider queue depth
        // exactly like the audio path (ForceRotateQueueGuardRatio).
        Assert.Contains("IsForceRotateQueueGuarded", method);
    }

    [Fact]
    public void RotateSegment_EscalatesAfterConsecutiveFailures()
    {
        var source = Source();
        Assert.Contains("MaxConsecutiveRotationFailures = 3", source);
        var method = SourceSlice.Method(source, "private bool RotateSegment");
        Assert.Contains("_consecutiveRotationFailures", method);
        Assert.Contains("FailEncoding", method);
    }

    [Fact]
    public void EndRecording_WaitsForQueueDrain_NotFixedDelay()
    {
        var method = SourceSlice.Method(Source(), "public async Task<FinalizeResult> EndRecordingAsync");
        Assert.DoesNotContain("Task.Delay(100", method);
        Assert.Contains("WaitForEncodeQueueDrainAsync", method);
    }

    [Fact]
    public void EncodingLoop_FailsFast_WhenDiskCriticallyLow()
    {
        var method = SourceSlice.Method(Source(), "private void OnVideoFrameEncoded");
        Assert.Contains("IsDiskCriticallyLow", method);
    }
}

// TestPaths/SourceSlice do not exist as shared helpers in the test project (the
// established convention here is Assembly.LoadFrom + reflection against the
// staged Sussudio.dll — see MIGRATION.md — rather than a compile-time
// ProjectReference or a shared source-slicing utility). Per the plan's fallback
// instruction, these are private, file-scoped copies rather than edits to any
// shared test file. (Mirrors the copy in XUnit.FlashbackFatalPathContractsTests.cs.)
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
