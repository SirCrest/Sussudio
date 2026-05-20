using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewDeviceFormatProbeRetargetContractsTests
{
    public PresentationPreviewDeviceFormatProbeRetargetContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task DeviceFormatProbeRetargetPolicyLivesInFocusedHelper()
        => global::Program.DeviceFormatProbeRetargetPolicy_LivesInFocusedHelper();

    [Fact]
    public Task DeviceFormatProbeRetargetPolicyPreservesRetargetDecisionBehavior()
        => global::Program.DeviceFormatProbeRetargetPolicy_PreservesRetargetDecisionBehavior();

    [Fact]
    public Task DeviceFormatProbeRetargetApplicationLivesInFocusedPartial()
        => global::Program.DeviceFormatProbeRetargetApplication_LivesInFocusedPartial();
}
