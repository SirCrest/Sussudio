using System;
using System.IO;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackResumeHardeningTests
{
    private static string ControllerSource() =>
        File.ReadAllText(TestPaths.Repo("Sussudio/Services/Flashback/FlashbackPlaybackController.cs"));

    [Fact]
    public void Prime_KeepsCpuFrames_InPrebufferQueue()
    {
        var method = SourceSlice.Method(ControllerSource(), "private void PrimePlaybackAudioBuffer");
        Assert.Contains("prebufferedFrames.Enqueue(", method);
        Assert.Contains("PlaybackAudioPrebufferMaxHeldFrames", method);
    }

    [Fact]
    public void Prime_SkipsRewind_WhenAllFramesKept()
    {
        var method = SourceSlice.Method(ControllerSource(), "private void PrimePlaybackAudioBuffer");
        // The rewind (and its re-decode) must only run when frames were released.
        Assert.Contains("if (releasedAnyFrame && decodedFrames > 0)", method);
    }

    [Fact]
    public void SetState_RaisesStateChangedEvent()
    {
        var source = ControllerSource();
        Assert.Contains("public event Action<FlashbackPlaybackState, FlashbackPlaybackState, string>? StateChanged;", source);
        var method = SourceSlice.Method(source, "private void SetState");
        Assert.Contains("StateChanged?.Invoke(oldState, newState, reason)", method);
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
