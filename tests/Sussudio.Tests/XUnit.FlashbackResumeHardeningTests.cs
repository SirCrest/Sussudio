using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackResumeHardeningTests
{
    private static string ControllerSource()
        => RuntimeContractSource.ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs");

    [Fact]
    public void Prime_KeepsCpuFrames_InPrebufferQueue()
    {
        var method = global::Program.ExtractDeclaredMemberCode(ControllerSource(), "private void PrimePlaybackAudioBuffer");
        Assert.Contains("prebufferedFrames.Enqueue(", method);
        Assert.Contains("PlaybackAudioPrebufferMaxHeldFrames", method);
    }

    [Fact]
    public void Prime_SkipsRewind_WhenAllFramesKept()
    {
        var method = global::Program.ExtractDeclaredMemberCode(ControllerSource(), "private void PrimePlaybackAudioBuffer");
        // The rewind (and its re-decode) must only run when frames were released.
        Assert.Contains("if (releasedAnyFrame && decodedFrames > 0)", method);
    }

    [Fact]
    public void SetState_RaisesStateChangedEvent()
    {
        var source = ControllerSource();
        Assert.Contains("public event Action<FlashbackPlaybackState, FlashbackPlaybackState, string>? StateChanged;", source);
        var method = global::Program.ExtractDeclaredMemberCode(source, "private void SetState");
        Assert.Contains("StateChanged?.Invoke(oldState, newState, reason)", method);
    }
}
