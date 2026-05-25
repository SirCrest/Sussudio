using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewStartupOwnershipContractsTests
{
    public PresentationPreviewStartupOwnershipContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewStartupSessionAndReinitOwnershipLivesInFocusedControllers()
        => global::Program.PreviewStartupSessionReinitOwnership_LivesInFocusedControllers();

    [Fact]
    public Task PreviewStartupWatchdogOwnershipLivesInFocusedController()
        => global::Program.PreviewStartupWatchdogOwnership_LivesInFocusedController();

    [Fact]
    public Task PreviewStartupSignalOwnershipLivesInFocusedControllers()
        => global::Program.PreviewStartupSignalsOwnership_LivesInFocusedControllers();

    [Fact]
    public Task PreviewStartupLifecycleEventOwnershipLivesInFocusedController()
        => global::Program.PreviewStartupLifecycleEventOwnership_LivesInFocusedController();
}

public sealed class PresentationPreviewStartupBehaviorContractsTests
{
    public PresentationPreviewStartupBehaviorContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewStartupWatchdogControllerPreservesTimeoutContracts()
        => global::Program.PreviewStartupWatchdogController_PreservesTimeoutContracts();

    [Fact]
    public Task PreviewStartupWatchdogControllerGatesFailureStopScheduling()
        => global::Program.PreviewStartupWatchdogController_GatesFailureStopScheduling();

    [Fact]
    public Task PreviewStartupSessionControllerPreservesAttemptStateContracts()
        => global::Program.PreviewStartupSessionController_PreservesAttemptStateContracts();

    [Fact]
    public Task PreviewReinitTransitionControllerPreservesTransitionStateContracts()
        => global::Program.PreviewReinitTransitionController_PreservesTransitionStateContracts();

    [Fact]
    public Task PreviewReinitializationWaitsForPendingFlashbackCycle()
        => global::Program.PreviewReinitialization_WaitsForPendingFlashbackCycle();
}

public sealed class PresentationPreviewStartupSignalContractsTests
{
    public PresentationPreviewStartupSignalContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewStartupSignalFormatterPreservesStringContracts()
        => global::Program.PreviewStartupSignalFormatter_PreservesSignalStrings();

    [Fact]
    public Task PreviewStartupReadinessSignalControllerPreservesStateContracts()
        => global::Program.PreviewStartupReadinessSignalController_PreservesSignalStateContracts();

    [Fact]
    public Task PreviewStartupFailureTextFormatterPreservesStringContracts()
        => global::Program.PreviewStartupFailureTextFormatter_PreservesFailureStrings();
}

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

public sealed class PresentationPreviewCaptureFlashbackBufferContractsTests
{
    public PresentationPreviewCaptureFlashbackBufferContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackBufferManagerCleansStaleSessionDirectories()
        => global::Program.FlashbackBufferManager_CleansStaleSessionDirectories();

    [Fact]
    public Task FlashbackBufferManagerPreservesMarkedRecoverySessions()
        => global::Program.FlashbackBufferManager_PreservesMarkedRecoverySessions();
}
