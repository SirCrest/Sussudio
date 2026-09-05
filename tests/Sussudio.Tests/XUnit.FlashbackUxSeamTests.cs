using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackUxSeamTests
{
    private static string ControllerSource()
        => RuntimeContractSource.ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs");

    private static string ThreadCommandsSource()
        => RuntimeContractSource.ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs");

    private static string PlaybackFramesSource()
        => RuntimeContractSource.ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs");

    [Fact]
    public void PauseFromLive_CallsBoundedForwardDecode_BetweenKeyframeDisplayAndPausedTransition()
    {
        var method = global::Program.ExtractDeclaredMemberCode(ThreadCommandsSource(), "private void HandlePauseCommand");

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
        var method = global::Program.ExtractDeclaredMemberCode(ThreadCommandsSource(), "private void HandlePauseCommand");
        Assert.Contains("SetState(FlashbackPlaybackState.Paused, \"user\");", method);
        Assert.Contains("frozen_frame=true", method);
        Assert.Contains("pendingExactResumeTarget = SaturatingAdd(PlaybackPosition, frozenValidStart);", method);
    }

    [Fact]
    public void DecodeForwardToPauseTarget_YieldsToQueuedCommands_AndReleasesIntermediateFrames()
    {
        var source = PlaybackFramesSource();
        Assert.DoesNotContain("SeekAndDisplayExactFrame", source);

        var method = global::Program.ExtractDeclaredMemberCode(source, "private void DecodeForwardToPauseTarget");
        Assert.Contains("commandChannel.TryPeek", method);
        Assert.Contains("ReleaseHeldFrameBestEffort(frame,", method);
        Assert.Contains("TrySubmitAndHoldFrame(frame,", method);
        Assert.Contains("maxForwardDecodeFrames", method);
    }

    [Fact]
    public void PreWarm_ExistsAndDoesNotEnqueueCommands()
    {
        var source = ControllerSource();
        Assert.Contains("public void PreWarm()", source);

        var method = global::Program.ExtractDeclaredMemberCode(source, "public void PreWarm()");
        Assert.DoesNotContain("SendCommand", method);
        Assert.Contains("EnsurePlaybackThread(", method);
        Assert.Contains("_initialized", method);
        Assert.Contains("_disposedFlag", method);
    }

    [Fact]
    public void GapFromLive_FallsBackWhenNoFrameDecodedSinceLeavingLive()
    {
        var source = ControllerSource();
        var property = global::Program.ExtractDeclaredMemberCode(source, "public TimeSpan GapFromLive");

        Assert.Contains("lastFrame == TimeSpan.Zero", property);
        Assert.Contains("_state == FlashbackPlaybackState.Live", property);
        Assert.Contains("_bufferManager.ValidStartPts", property);
        Assert.Contains("SaturatingAdd(PlaybackPosition,", property);
    }
}
