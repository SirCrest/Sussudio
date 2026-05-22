using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackEncoderSinkContractsTests
{
    public FlashbackEncoderSinkContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackEncoderResolvesFractionalFrameRates()
        => global::Program.FlashbackEncoderSink_ResolveFrameRateParts_ParsesFractionalRates();

    [Fact]
    public Task FlashbackEncoderMapsCodecNames()
        => global::Program.FlashbackEncoderSink_MapCodecName_MapsFormats();

    [Fact]
    public Task FlashbackEncoderCountersDefaultToZero()
        => global::Program.FlashbackEncoderSink_CountersDefaultToZero();

    [Fact]
    public Task FlashbackEncoderBoundsHighResolutionCpuQueueCapacity()
        => global::Program.FlashbackEncoderSink_HighResolutionCpuQueueCapacityIsBounded();

    [Fact]
    public Task FlashbackExportThrottleRespondsToLiveQueuePressure()
        => global::Program.CaptureService_FlashbackExportThrottleRespondsToLiveQueuePressure();

    [Fact]
    public Task FlashbackEncoderForceRotateDrainRejectsVideoEnqueues()
        => global::Program.FlashbackEncoderSink_ForceRotateDrainingRejectsVideoAndGpuEnqueues();

    [Fact]
    public Task FlashbackEncoderStartFailureRollsBackStartedState()
        => global::Program.FlashbackEncoderSink_StartFailureRollsBackStartedState();

    [Fact]
    public Task FlashbackEncoderDisposeResetsGpuQueueDepth()
        => global::Program.FlashbackEncoderSink_DisposeResetsGpuQueueDepth();

    [Fact]
    public Task FlashbackEncoderPtsGuardsInvalidFrameRates()
        => global::Program.FlashbackEncoderSink_EncoderPtsGuardsInvalidFrameRate();

    [Fact]
    public Task FlashbackEncoderSinkRestoresActiveSegmentAfterRotationFailure()
        => global::Program.FlashbackEncoderSink_RotateFailureRestoresActiveSegment();

    [Fact]
    public Task FlashbackEncoderSinkRegistersSegmentsOnCancellationAndRotationFailure()
        => global::Program.FlashbackEncoderSink_RegistersSegmentsOnCancellationAndRotationFailure();

    [Fact]
    public Task FlashbackEncoderSinkRejectsForceRotateAfterEncoderFailure()
        => global::Program.FlashbackEncoderSink_ForceRotateRejectsFailedEncoder();

    [Fact]
    public Task FlashbackEncoderSinkSkipsCompletedForceRotateRequests()
        => global::Program.FlashbackEncoderSink_ForceRotateSkipsCompletedPendingRequest();

    [Fact]
    public Task FlashbackEncoderSinkLogsFatalSegmentRegistrationFailures()
        => global::Program.FlashbackEncoderSink_FatalSegmentRegistrationFailuresAreLogged();

    [Fact]
    public Task FlashbackEncoderSinkValidatesAudioPacketsBeforeRent()
        => global::Program.FlashbackEncoderSink_AudioPacketsAreValidatedBeforeRent();

    [Fact]
    public Task FlashbackEncoderSinkInterleavesAudioWithBoundedVideoBatches()
        => global::Program.FlashbackEncoderSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches();

    [Fact]
    public Task FlashbackEncoderSinkPacketDrainsLiveInFocusedPartial()
        => global::Program.FlashbackEncoderSink_PacketDrainLivesInFocusedPartial();

    [Fact]
    public Task FlashbackEncoderSinkQueueCleanupLivesInFocusedPartial()
        => global::Program.FlashbackEncoderSink_QueueCleanupLivesInFocusedPartial();

    [Fact]
    public Task FlashbackEncoderSinkStartupLivesInFocusedPartial()
        => global::Program.FlashbackEncoderSink_StartupLivesInFocusedPartial();

    [Fact]
    public Task FlashbackEncoderSinkRootHelpersLiveInFocusedPartials()
        => global::Program.FlashbackEncoderSink_RootHelpersLiveInFocusedPartials();

    [Fact]
    public Task FlashbackEncoderSinkForceRotateRequestsLiveInFocusedPartial()
        => global::Program.FlashbackEncoderSink_ForceRotateRequestsLiveInFocusedPartial();

    [Fact]
    public Task FlashbackEncoderSinkStopAndDisposeLifecyclesStaySplit()
        => global::Program.FlashbackEncoderSink_StopAndDisposeLifecyclesStaySplit();

    [Fact]
    public Task FlashbackEncoderSinkProducerInputsLiveInFocusedPartials()
        => global::Program.FlashbackEncoderSink_ProducerInputsLiveInFocusedPartials();

    [Fact]
    public Task FlashbackEncoderSinkRuntimeStateLivesInCohesivePartial()
        => global::Program.FlashbackEncoderSink_RuntimeStateLivesInCohesivePartial();

    [Fact]
    public Task FlashbackEncoderSinkRecordingLifecycleLivesInCohesivePartial()
        => global::Program.FlashbackEncoderSink_RecordingLifecycleLivesInCohesivePartial();

    [Fact]
    public Task FlashbackEncoderSinkOptionsHelpersLiveInFocusedPartials()
        => global::Program.FlashbackEncoderSink_OptionsHelpersLiveInFocusedPartials();
}
