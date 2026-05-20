using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewCaptureRuntimeGuardContractsTests
{
    public PresentationPreviewCaptureRuntimeGuardContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task RecordingStopPropagatesUnifiedVideoStopFailure()
        => global::Program.RecordingStop_PropagatesUnifiedVideoStopFailure();

    [Fact]
    public Task PreviewStopCompatibilityOverloadsArePreserved()
        => global::Program.PreviewStopCompatibilityOverloads_ArePreserved();

    [Fact]
    public Task PreviewStopApiSurfaceHasNoDefaultLiteralAmbiguity()
        => global::Program.PreviewStopApiSurface_HasNoDefaultLiteralAmbiguity();

    [Fact]
    public Task EmergencyRecordingStopDoesNotDispatchToBlockedUiThread()
        => global::Program.EmergencyRecordingStop_DoesNotDispatchBackToBlockedUiThread();
}
