using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackResumeHardeningTests
{
    private static string ControllerSource()
        => RuntimeContractSource.ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs");

    [Fact]
    public void SetState_RaisesStateChangedEvent()
    {
        var source = ControllerSource();
        Assert.Contains("public event Action<FlashbackPlaybackState, FlashbackPlaybackState, string, bool>? StateChanged;", source);
        var method = global::Program.ExtractDeclaredMemberCode(source, "private void SetState");
        Assert.Contains("bool isInvoluntaryLiveReturn = false", method);
        Assert.Contains("StateChanged?.Invoke(oldState, newState, reason, isInvoluntaryLiveReturn)", method);
    }
}
