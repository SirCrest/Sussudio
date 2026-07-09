using System;
using System.IO;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackUxSeamTests
{
    private static string ControllerSource() =>
        File.ReadAllText(TestPaths.Repo("Sussudio/Services/Flashback/FlashbackPlaybackController.cs"));

    private static string ThreadCommandsSource() =>
        File.ReadAllText(TestPaths.Repo("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs"));

    private static string PlaybackFramesSource() =>
        File.ReadAllText(TestPaths.Repo("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs"));

    [Fact]
    public void PauseFromLive_CallsBoundedForwardDecode_BetweenKeyframeDisplayAndPausedTransition()
    {
        var method = SourceSlice.Method(ThreadCommandsSource(), "private void HandlePauseCommand");

        var seekIndex = method.IndexOf("SeekAndDisplayKeyframe(decoder, ref fileOpen, pausePos", StringComparison.Ordinal);
        var forwardDecodeIndex = method.IndexOf("DecodeForwardToPauseTarget(", StringComparison.Ordinal);
        var pausedIndex = method.LastIndexOf("SetState(FlashbackPlaybackState.Paused, \"user\");", StringComparison.Ordinal);

        Assert.True(seekIndex >= 0, "Expected the pause-from-live keyframe seek call.");
        Assert.True(forwardDecodeIndex > seekIndex, "Forward-decode must run after the keyframe display.");
        Assert.True(pausedIndex > forwardDecodeIndex, "Forward-decode must run before the Paused state transition.");
        Assert.Contains("PauseFromLiveMaxForwardDecodeFrames", method);

        // Naming constraint: this exact name must never exist anywhere in the file.
        Assert.DoesNotContain("SeekAndDisplayExactFrame", ThreadCommandsSource());
    }

    [Fact]
    public void HandlePauseCommand_PreservesExistingContractPins()
    {
        var method = SourceSlice.Method(ThreadCommandsSource(), "private void HandlePauseCommand");
        Assert.Contains("SetState(FlashbackPlaybackState.Paused, \"user\");", method);
        Assert.Contains("frozen_frame=true", method);
        Assert.Contains("pendingExactResumeTarget = SaturatingAdd(PlaybackPosition, frozenValidStart);", method);
    }

    [Fact]
    public void DecodeForwardToPauseTarget_YieldsToQueuedCommands_AndReleasesIntermediateFrames()
    {
        var source = PlaybackFramesSource();
        Assert.DoesNotContain("SeekAndDisplayExactFrame", source);

        var method = SourceSlice.Method(source, "private void DecodeForwardToPauseTarget");
        Assert.Contains("commandChannel.Reader.TryPeek", method);
        Assert.Contains("ReleaseHeldFrameBestEffort(frame,", method);
        Assert.Contains("TrySubmitAndHoldFrame(frame,", method);
        Assert.Contains("maxForwardDecodeFrames", method);
    }

    [Fact]
    public void PreWarm_ExistsAndDoesNotEnqueueCommands()
    {
        var source = ControllerSource();
        Assert.Contains("public void PreWarm()", source);

        var method = SourceSlice.Method(source, "public void PreWarm()");
        Assert.DoesNotContain("SendCommand", method);
        Assert.Contains("EnsurePlaybackThread(", method);
        Assert.Contains("_initialized", method);
        Assert.Contains("_disposedFlag", method);
    }

    [Fact]
    public void GapFromLive_FallsBackWhenNoFrameDecodedSinceLeavingLive()
    {
        var source = ControllerSource();
        var property = SourceSlice.Method(source, "public TimeSpan GapFromLive");

        Assert.Contains("lastFrame == TimeSpan.Zero", property);
        Assert.Contains("_state == FlashbackPlaybackState.Live", property);
        Assert.Contains("_bufferManager.ValidStartPts", property);
        Assert.Contains("SaturatingAdd(PlaybackPosition,", property);
    }
}

// TestPaths/SourceSlice do not exist as shared helpers in the test project (the
// established convention here is Assembly.LoadFrom + reflection against the
// staged Sussudio.dll -- see MIGRATION.md -- rather than a compile-time
// ProjectReference or a shared source-slicing utility). Per the plan's fallback
// instruction, these are private, file-scoped copies rather than edits to any
// shared test file. (Mirrors the copy in XUnit.FlashbackResumeHardeningTests.cs.)
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
    /// Returns the source text of the method/property whose declaration starts
    /// with <paramref name="signaturePrefix"/> (e.g. "private void Foo"), from its
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
