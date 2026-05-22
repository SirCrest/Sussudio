using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class RecordingPipelineContractsTests
{
    public RecordingPipelineContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task RecordingVideoQueuesFailExplicitlyInsteadOfEvictingFrames()
        => global::Program.RecordingVideoQueues_FailExplicitlyInsteadOfEvictingFrames();

    [Fact]
    public Task RecordingBackendFinalizeAndCleanupPreservesFlashbackBoundaries()
        => global::Program.RecordingBackendFinalizeAndCleanup_PreservesFlashbackBoundaries();

    [Fact]
    public Task RecordingBackendFlashbackBufferCyclePreservesCommittedPolicies()
        => global::Program.RecordingBackendFlashbackBufferCycle_PreservesPolicies();

    [Fact]
    public Task CaptureServiceRecordingLifecycleAndBackendResourcesHaveFocusedOwners()
        => global::Program.CaptureService_RecordingLifecycleAndBackendResourcesHaveFocusedOwners();

    [Fact]
    public Task CaptureServiceRecordingRollbackLivesInFocusedPartial()
        => global::Program.CaptureService_RecordingRollbackLivesInFocusedPartial();

    [Fact]
    public Task CaptureServiceRecordingOutcomeStateLivesWithRecordingLifecycle()
        => global::Program.CaptureService_RecordingOutcomeStateLivesWithRecordingLifecycle();

    [Fact]
    public Task CaptureServiceAudioOwnershipLivesInFocusedPartials()
        => global::Program.CaptureService_AudioOwnershipLivesInFocusedPartials();

    [Fact]
    public Task CaptureServiceMicrophoneRestartAfterRecordingLivesInMicrophoneMonitorPartial()
        => global::Program.CaptureService_MicrophoneRestartAfterRecordingLivesInMicrophoneMonitorPartial();

    [Fact]
    public Task LibAvRecordingSinkStopValidatesFinalOutput()
        => global::Program.LibAvRecordingSink_StopValidatesFinalOutput();

    [Fact]
    public Task RecordingVideoTryEnqueuePathsDoNotBlockCaptureCallbacks()
        => global::Program.RecordingVideoTryEnqueuePaths_DoNotBlockCaptureCallbacks();

    [Fact]
    public Task UnifiedVideoCaptureSinkFanoutLivesInFocusedPartial()
        => global::Program.UnifiedVideoCapture_SinkFanoutLivesInFocusedPartial();

    [Fact]
    public Task UnifiedVideoCaptureFrameIngressLivesInFocusedPartial()
        => global::Program.UnifiedVideoCapture_FrameIngressLivesInFocusedPartial();

    [Fact]
    public Task UnifiedVideoCaptureLifecycleLivesInFocusedPartial()
        => global::Program.UnifiedVideoCapture_LifecycleLivesInFocusedPartial();

    [Fact]
    public Task WasapiAudioCaptureRejectsIncompleteHotAudioWrites()
        => global::Program.WasapiAudioCapture_HotAudioWritesRejectIncompleteTasks();

    [Fact]
    public Task WasapiAudioCaptureConversionLivesInFocusedPartial()
        => global::Program.WasapiAudioCapture_ConversionLivesInFocusedPartial();

    [Fact]
    public Task WasapiAudioCaptureInitializationLivesInFocusedPartial()
        => global::Program.WasapiAudioCapture_InitializationLivesInFocusedPartial();

    [Fact]
    public Task WasapiAudioPlaybackInitializationLivesInFocusedPartial()
        => global::Program.WasapiAudioPlayback_InitializationLivesInFocusedPartial();

    [Fact]
    public Task WasapiAudioCaptureDiagnosticsLivesInFocusedPartial()
        => global::Program.WasapiAudioCapture_DiagnosticsLivesInFocusedPartial();

    [Fact]
    public Task WasapiComInteropContractsLiveInFocusedFiles()
        => global::Program.WasapiComInterop_ContractsLiveInFocusedFiles();

    [Fact]
    public Task WasapiAudioCaptureStopUsesBoundedThreadJoin()
        => global::Program.WasapiAudioCapture_StopUsesBoundedThreadJoin();

    [Fact]
    public Task CaptureServiceFlashbackBackendOwnershipUsesResourceAggregate()
        => global::Program.CaptureService_FlashbackBackendOwnershipUsesResourceAggregate();

    [Fact]
    public Task CaptureServiceFlashbackOrchestrationLivesInFocusedPartials()
        => global::Program.CaptureService_FlashbackOrchestrationLivesInFocusedPartials();

    [Fact]
    public Task CaptureServiceRecordingFinalizationLivesInFocusedPartials()
        => global::Program.CaptureService_RecordingFinalizationLivesInFocusedPartials();
}
