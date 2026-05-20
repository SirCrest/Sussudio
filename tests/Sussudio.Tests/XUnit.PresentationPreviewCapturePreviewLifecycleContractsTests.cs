using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewCapturePreviewLifecycleContractsTests
{
    public PresentationPreviewCapturePreviewLifecycleContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewStartupToleratesMissingAudioCaptureDevices()
        => global::Program.PreviewStartup_ToleratesMissingAudioCaptureDevices();

    [Fact]
    public Task CaptureServicePreviewLifecycleLivesInFocusedPartials()
        => global::Program.CaptureService_PreviewLifecycleLivesInFocusedPartials();

    [Fact]
    public Task AudioPreviewRemainsInactiveWhenNoAudioCaptureDeviceExists()
        => global::Program.AudioPreview_RemainsInactive_WhenNoAudioCaptureDeviceExists();

    [Fact]
    public Task AudioMonitoringVisualsFollowRuntimePreviewActivity()
        => global::Program.AudioMonitoringVisuals_FollowRuntimePreviewActivity();

    [Fact]
    public Task PreviewBackendLogReflectsVideoOnlyFallback()
        => global::Program.PreviewBackendLog_ReflectsVideoOnlyFallback();
}
