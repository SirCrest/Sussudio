using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewStartupOrderingContractsTests
{
    public PresentationPreviewStartupOrderingContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewStartupBeginsDeviceDiscoveryBeforeRecordingCapabilityProbesFinish()
        => global::Program.PreviewStartup_BeginsDeviceDiscoveryBeforeRecordingCapabilityProbesFinish();

    [Fact]
    public Task PreviewStartupPrimesUiAndAudioBeforePreviewReveal()
        => global::Program.PreviewStartup_PrimesUiAndAudioBeforePreviewReveal();

    [Fact]
    public Task PreviewStopRampsAudioDownBeforePreviewTeardown()
        => global::Program.PreviewStop_RampsAudioDownBeforePreviewTeardown();
}
