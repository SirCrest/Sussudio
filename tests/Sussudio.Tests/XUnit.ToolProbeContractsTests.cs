using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class ToolProbeContractsTests
{
    [Fact]
    public Task PresentMonParserSelectsDominantNonArtifactSwapChain()
        => global::Program.PresentMonParser_SelectsDominantNonArtifactSwapChain();

    [Fact]
    public Task PresentMonProbeSourceOwnershipIsSplit()
        => global::Program.PresentMonProbe_SourceOwnership_IsSplit();

    [Fact]
    public Task SsctlPipeTransportExposesAdvancedAutomationCommandIds()
        => global::Program.SsctlPipeTransport_ExposesAdvancedAutomationCommandIds();

    [Fact]
    public Task KsAudioNodeProbeSourceOwnershipIsSplit()
        => global::Program.KsAudioNodeProbe_SourceOwnership_IsSplit();

    [Fact]
    public Task EgavdsAudioProbeSourceOwnershipIsSplit()
        => global::Program.EgavdsAudioProbe_SourceOwnership_IsSplit();
}
