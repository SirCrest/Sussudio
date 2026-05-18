using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddMjpegPipelineChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Unified video capture CPU MJPEG emit reports NV12",
            UnifiedVideoCapture_CpuMjpegEmitReportsNv12);
        await AddCheckAsync(results,
            "Unified video capture retains MJPEG pipeline on stop failure",
            UnifiedVideoCapture_RetainsMjpegPipeline_WhenStopFails);
        await AddCheckAsync(results,
            "MJPEG pipeline timing metrics calculate uniform samples",
            ParallelMjpegDecodePipeline_ComputeTimingMetrics_CalculatesCorrectly);
        await AddCheckAsync(results,
            "MJPEG pipeline timing metrics calculate P95 samples",
            ParallelMjpegDecodePipeline_ComputeTimingMetrics_P95Calculation);
        await AddCheckAsync(results,
            "MJPEG pipeline elapsed milliseconds uses stopwatch ticks",
            ParallelMjpegDecodePipeline_GetElapsedMilliseconds_ComputesCorrectly);
        await AddCheckAsync(results,
            "MJPEG pipeline remaining timeout clamps past deadlines",
            ParallelMjpegDecodePipeline_GetRemainingTimeout_ReturnsCorrectTimeSpan);
        await AddCheckAsync(results,
            "MJPEG pipeline lifecycle lives in focused partial",
            ParallelMjpegDecodePipeline_LifecycleLivesInFocusedPartial);
        await AddCheckAsync(results,
            "MJPEG compressed queue admission lives in focused partial",
            ParallelMjpegDecodePipeline_CompressedQueueLivesInFocusedPartial);
        await AddCheckAsync(results,
            "MJPEG pipeline reorder lives in focused partial",
            ParallelMjpegDecodePipeline_ReorderLivesInFocusedPartial);
        await AddCheckAsync(results,
            "MJPEG pipeline timing metrics expose expected properties",
            ParallelMjpegDecodePipeline_PipelineTimingMetrics_HasExpectedProperties);
        await AddCheckAsync(results,
            "Software MJPEG decoder exposes dimensions and NV12 size",
            SoftwareMjpegDecoder_Properties_ExposeCorrectDimensions);
        await AddCheckAsync(results,
            "Pooled video frame leases return buffer after final release",
            PooledVideoFrame_LeaseLifecycle_ReturnsBufferAfterLastRelease);
        await AddCheckAsync(results,
            "Pooled video frame rejects leases after return",
            PooledVideoFrame_AddLeaseAfterReturn_Throws);
        await AddCheckAsync(results,
            "Pooled video frame closes new leases after owner release",
            PooledVideoFrame_OwnerDisposeClosesNewLeasesButExistingLeaseRemainsReadable);
        await AddCheckAsync(results,
            "MJPEG pooled frame fanout exposes lease contracts",
            MjpegPooledFrameFanout_ExposesLeaseContracts);
        await AddCheckAsync(results,
            "MJPEG shared reorder does not synthesize recording skips",
            ParallelMjpegDecodePipeline_SharedReorder_DoesNotSynthesizeRecordingSkips);
        await AddCheckAsync(results,
            "MJPEG startup non-JPEG samples drop before sequencing",
            ParallelMjpegDecodePipeline_DropsStartupNonJpegBeforeSequencing);
        await AddCheckAsync(results,
            "MJPEG known losses skip instead of fataling capture",
            ParallelMjpegDecodePipeline_KnownLossSkipsInsteadOfSignalingFatal);
        await AddCheckAsync(results,
            "MJPEG packet hash current duplicate run lowers unique FPS",
            FrameFingerprintCadenceTracker_CurrentDuplicateRunLowersUniqueFps);
        await AddCheckAsync(results,
            "Decoded visual cadence samples exact crop pixels in one pass",
            VisualCadenceTracker_UsesExactCropPixelsWithOnePassDiff);
        await AddCheckAsync(results,
            "MJPEG leased video packets release queued leases",
            MjpegLeasedVideoPackets_ReleaseQueuedLeases);
        await AddCheckAsync(results,
            "MJPEG preview jitter exposes adaptive deadline policy",
            MjpegPreviewJitter_ExposesAdaptiveDeadlinePolicy);
        await AddCheckAsync(results,
            "MJPEG preview jitter emit loop lives in focused partial",
            MjpegPreviewJitter_EmitLoopLivesInFocusedPartial);
        await AddCheckAsync(results,
            "MJPEG preview jitter drops soft deadline overflow to recover latency",
            MjpegPreviewJitter_DropsSoftDeadlineOverflowToRecoverLatency);
        await AddCheckAsync(results,
            "MJPEG preview jitter drops expired frames below target depth",
            MjpegPreviewJitter_DropsExpiredFramesBelowTargetDepth);
        await AddCheckAsync(results,
            "MJPEG preview jitter skips missing preview sequence after deadline",
            MjpegPreviewJitter_SkipsMissingPreviewSequenceAfterDeadline);
        await AddCheckAsync(results,
            "MJPEG preview jitter does not count late sequence frames as queued",
            MjpegPreviewJitter_LateSequenceDoesNotCountAsQueued);
        await AddCheckAsync(results,
            "MJPEG preview jitter clear resets preview sequence",
            MjpegPreviewJitter_ClearResetsPreviewSequence);
        await AddCheckAsync(results,
            "MJPEG preview jitter reprimes after suppression resume",
            MjpegPreviewJitter_ReprimesAfterSuppressionResume);
        await AddCheckAsync(results,
            "D3D preview pending frame releases queued lease",
            D3DPreviewPendingFrame_ReleasesQueuedLease);
    }
}
