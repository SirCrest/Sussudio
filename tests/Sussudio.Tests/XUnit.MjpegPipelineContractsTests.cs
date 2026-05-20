using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class MjpegPipelineContractsTests
{
    public MjpegPipelineContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task UnifiedVideoCaptureCpuMjpegEmitReportsNv12()
        => global::Program.UnifiedVideoCapture_CpuMjpegEmitReportsNv12();

    [Fact]
    public Task UnifiedVideoCaptureRetainsMjpegPipelineWhenStopFails()
        => global::Program.UnifiedVideoCapture_RetainsMjpegPipeline_WhenStopFails();

    [Fact]
    public Task ParallelMjpegDecodePipelineLifecycleLivesInFocusedPartial()
        => global::Program.ParallelMjpegDecodePipeline_LifecycleLivesInFocusedPartial();

    [Fact]
    public Task ParallelMjpegDecodePipelineCompressedQueueLivesInFocusedPartial()
        => global::Program.ParallelMjpegDecodePipeline_CompressedQueueLivesInFocusedPartial();

    [Fact]
    public Task ParallelMjpegDecodePipelineWorkersLiveInFocusedPartial()
        => global::Program.ParallelMjpegDecodePipeline_WorkersLiveInFocusedPartial();

    [Fact]
    public Task ParallelMjpegDecodePipelineReorderLivesInFocusedPartial()
        => global::Program.ParallelMjpegDecodePipeline_ReorderLivesInFocusedPartial();

    [Fact]
    public Task PooledVideoFrameLeaseLifecycleReturnsBufferAfterLastRelease()
        => global::Program.PooledVideoFrame_LeaseLifecycle_ReturnsBufferAfterLastRelease();

    [Fact]
    public Task PooledVideoFrameAddLeaseAfterReturnThrows()
        => global::Program.PooledVideoFrame_AddLeaseAfterReturn_Throws();

    [Fact]
    public Task PooledVideoFrameOwnerDisposeClosesNewLeasesButExistingLeaseRemainsReadable()
        => global::Program.PooledVideoFrame_OwnerDisposeClosesNewLeasesButExistingLeaseRemainsReadable();

    [Fact]
    public Task MjpegPooledFrameFanoutExposesLeaseContracts()
        => global::Program.MjpegPooledFrameFanout_ExposesLeaseContracts();

    [Fact]
    public Task ParallelMjpegDecodePipelineSharedReorderDoesNotSynthesizeRecordingSkips()
        => global::Program.ParallelMjpegDecodePipeline_SharedReorder_DoesNotSynthesizeRecordingSkips();

    [Fact]
    public Task ParallelMjpegDecodePipelineDropsStartupNonJpegBeforeSequencing()
        => global::Program.ParallelMjpegDecodePipeline_DropsStartupNonJpegBeforeSequencing();

    [Fact]
    public Task ParallelMjpegDecodePipelineKnownLossSkipsInsteadOfSignalingFatal()
        => global::Program.ParallelMjpegDecodePipeline_KnownLossSkipsInsteadOfSignalingFatal();

    [Fact]
    public Task FrameFingerprintCadenceTrackerCurrentDuplicateRunLowersUniqueFps()
        => global::Program.FrameFingerprintCadenceTracker_CurrentDuplicateRunLowersUniqueFps();

    [Fact]
    public Task VisualCadenceTrackerUsesExactCropPixelsWithOnePassDiff()
        => global::Program.VisualCadenceTracker_UsesExactCropPixelsWithOnePassDiff();

    [Fact]
    public Task MjpegLeasedVideoPacketsReleaseQueuedLeases()
        => global::Program.MjpegLeasedVideoPackets_ReleaseQueuedLeases();

    [Fact]
    public Task MjpegPreviewJitterExposesAdaptiveDeadlinePolicy()
        => global::Program.MjpegPreviewJitter_ExposesAdaptiveDeadlinePolicy();

    [Fact]
    public Task MjpegPreviewJitterEmitLoopLivesInFocusedPartial()
        => global::Program.MjpegPreviewJitter_EmitLoopLivesInFocusedPartial();

    [Fact]
    public Task MjpegPreviewJitterDropsSoftDeadlineOverflowToRecoverLatency()
        => global::Program.MjpegPreviewJitter_DropsSoftDeadlineOverflowToRecoverLatency();

    [Fact]
    public Task MjpegPreviewJitterDropsExpiredFramesBelowTargetDepth()
        => global::Program.MjpegPreviewJitter_DropsExpiredFramesBelowTargetDepth();

    [Fact]
    public Task MjpegPreviewJitterSkipsMissingPreviewSequenceAfterDeadline()
        => global::Program.MjpegPreviewJitter_SkipsMissingPreviewSequenceAfterDeadline();

    [Fact]
    public Task MjpegPreviewJitterLateSequenceDoesNotCountAsQueued()
        => global::Program.MjpegPreviewJitter_LateSequenceDoesNotCountAsQueued();

    [Fact]
    public Task MjpegPreviewJitterClearResetsPreviewSequence()
        => global::Program.MjpegPreviewJitter_ClearResetsPreviewSequence();

    [Fact]
    public Task MjpegPreviewJitterReprimesAfterSuppressionResume()
        => global::Program.MjpegPreviewJitter_ReprimesAfterSuppressionResume();

    [Fact]
    public Task D3DPreviewPendingFrameReleasesQueuedLease()
        => global::Program.D3DPreviewPendingFrame_ReleasesQueuedLease();
}
