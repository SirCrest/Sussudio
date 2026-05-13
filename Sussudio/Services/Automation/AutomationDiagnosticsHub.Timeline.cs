using System;
using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    public IReadOnlyList<PerformanceTimelineEntry> GetPerformanceTimeline(int maxEntries = 240)
    {
        lock (_stateLock)
        {
            var count = Math.Min(_timelineCount, Math.Max(0, maxEntries));
            if (count == 0)
            {
                return Array.Empty<PerformanceTimelineEntry>();
            }

            var result = new PerformanceTimelineEntry[count];
            var oldest = (_timelineHead - _timelineCount + TimelineCapacity) % TimelineCapacity;
            var skip = _timelineCount - count;
            var readIndex = (oldest + skip) % TimelineCapacity;
            for (var i = 0; i < count; i++)
            {
                result[i] = _timelineBuffer[readIndex];
                readIndex = (readIndex + 1) % TimelineCapacity;
            }

            return result;
        }
    }

    // Caller must hold _stateLock so _latestSnapshot and timeline advance atomically.
    private void AppendPerformanceTimelineEntry(AutomationSnapshot snapshot)
    {
        _timelineBuffer[_timelineHead] = new PerformanceTimelineEntry
        {
            TimestampUtc = snapshot.TimestampUtc,
            CaptureFps = snapshot.CaptureCadenceObservedFps,
            PreviewFps = snapshot.PreviewCadenceObservedFps,
            VideoQueueDepth = snapshot.FfmpegVideoQueueDepth,
            VideoDrops = snapshot.VideoDropsQueueSaturated,
            CaptureCadenceAverageMs = snapshot.CaptureCadenceAverageIntervalMs,
            CaptureCadenceP95Ms = snapshot.CaptureCadenceP95IntervalMs,
            CaptureCadenceP99Ms = snapshot.CaptureCadenceP99IntervalMs,
            CaptureCadenceMaxMs = snapshot.CaptureCadenceMaxIntervalMs,
            CaptureCadenceOnePercentLowFps = snapshot.CaptureCadenceOnePercentLowFps,
            CaptureCadenceFivePercentLowFps = snapshot.CaptureCadenceFivePercentLowFps,
            PreviewCadenceAverageMs = snapshot.PreviewCadenceAverageIntervalMs,
            PreviewCadenceP95Ms = snapshot.PreviewCadenceP95IntervalMs,
            PreviewCadenceP99Ms = snapshot.PreviewCadenceP99IntervalMs,
            PreviewCadenceMaxMs = snapshot.PreviewCadenceMaxIntervalMs,
            PreviewCadenceOnePercentLowFps = snapshot.PreviewCadenceOnePercentLowFps,
            PreviewCadenceFivePercentLowFps = snapshot.PreviewCadenceFivePercentLowFps,
            PreviewCadenceSlowFramePercent = snapshot.PreviewCadenceSlowFramePercent,
            VisualCadenceChangeObservedFps = snapshot.VisualCadenceChangeObservedFps,
            VisualCadenceRepeatFramePercent = snapshot.VisualCadenceRepeatFramePercent,
            VisualCadenceMotionConfidence = snapshot.VisualCadenceMotionConfidence,
            MjpegPacketHashInputObservedFps = snapshot.MjpegPacketHashInputObservedFps,
            MjpegPacketHashUniqueObservedFps = snapshot.MjpegPacketHashUniqueObservedFps,
            MjpegPacketHashDuplicateFramePercent = snapshot.MjpegPacketHashDuplicateFramePercent,
            MjpegPreviewJitterEnabled = snapshot.MjpegPreviewJitterEnabled,
            MjpegPreviewJitterTargetDepth = snapshot.MjpegPreviewJitterTargetDepth,
            MjpegPreviewJitterMaxDepth = snapshot.MjpegPreviewJitterMaxDepth,
            MjpegPreviewJitterQueueDepth = snapshot.MjpegPreviewJitterQueueDepth,
            MjpegPreviewJitterTotalDropped = snapshot.MjpegPreviewJitterTotalDropped,
            MjpegPreviewJitterDeadlineDropCount = snapshot.MjpegPreviewJitterDeadlineDropCount,
            MjpegPreviewJitterClearedDropCount = snapshot.MjpegPreviewJitterClearedDropCount,
            MjpegPreviewJitterUnderflowCount = snapshot.MjpegPreviewJitterUnderflowCount,
            MjpegPreviewJitterResumeReprimeCount = snapshot.MjpegPreviewJitterResumeReprimeCount,
            MjpegPreviewJitterLatencyP95Ms = snapshot.MjpegPreviewJitterLatencyP95Ms,
            MjpegPreviewJitterLatencyMaxMs = snapshot.MjpegPreviewJitterLatencyMaxMs,
            MjpegPreviewJitterLastDropReason = snapshot.MjpegPreviewJitterLastDropReason,
            MjpegPreviewJitterLastUnderflowReason = snapshot.MjpegPreviewJitterLastUnderflowReason,
            MjpegPreviewJitterLastUnderflowInputAgeMs = snapshot.MjpegPreviewJitterLastUnderflowInputAgeMs,
            MjpegPreviewJitterLastUnderflowOutputAgeMs = snapshot.MjpegPreviewJitterLastUnderflowOutputAgeMs,
            MjpegPreviewJitterMaxScheduleLateMs = snapshot.MjpegPreviewJitterMaxScheduleLateMs,
            MjpegPreviewJitterScheduleLateCount = snapshot.MjpegPreviewJitterScheduleLateCount,
            PreviewD3DPendingFrameCount = snapshot.PreviewD3DPendingFrameCount,
            PreviewD3DPresentCallP95Ms = snapshot.PreviewD3DPresentCallP95Ms,
            PreviewD3DTotalFrameCpuP95Ms = snapshot.PreviewD3DTotalFrameCpuP95Ms,
            PreviewD3DInputUploadCpuP99Ms = snapshot.PreviewD3DInputUploadCpuP99Ms,
            PreviewD3DRenderSubmitCpuP99Ms = snapshot.PreviewD3DRenderSubmitCpuP99Ms,
            PreviewD3DPresentCallP99Ms = snapshot.PreviewD3DPresentCallP99Ms,
            PreviewD3DTotalFrameCpuP99Ms = snapshot.PreviewD3DTotalFrameCpuP99Ms,
            PreviewD3DPipelineLatencyP95Ms = snapshot.PreviewD3DPipelineLatencyP95Ms,
            PreviewD3DPipelineLatencyP99Ms = snapshot.PreviewD3DPipelineLatencyP99Ms,
            PreviewD3DPipelineLatencyMaxMs = snapshot.PreviewD3DPipelineLatencyMaxMs,
            PreviewD3DFrameLatencyWaitTimeoutCount = snapshot.PreviewD3DFrameLatencyWaitTimeoutCount,
            PreviewD3DFrameLatencyWaitP95Ms = snapshot.PreviewD3DFrameLatencyWaitP95Ms,
            PreviewD3DFrameLatencyWaitMaxMs = snapshot.PreviewD3DFrameLatencyWaitMaxMs,
            PreviewD3DFrameStatsRecentMissedRefreshCount = snapshot.PreviewD3DFrameStatsRecentMissedRefreshCount,
            PreviewD3DFrameStatsRecentFailureCount = snapshot.PreviewD3DFrameStatsRecentFailureCount,
            PreviewD3DLastRenderedSchedulerToPresentMs = snapshot.PreviewD3DLastRenderedSchedulerToPresentMs,
            PreviewD3DLastRenderedPipelineLatencyMs = snapshot.PreviewD3DLastRenderedPipelineLatencyMs,
            PreviewD3DLastDropReason = snapshot.PreviewD3DLastDropReason,
            PreviewPacingLikelySlowStage = snapshot.PreviewPacingLikelySlowStage,
            PreviewPacingSlowStageConfidence = snapshot.PreviewPacingSlowStageConfidence,
            PreviewPacingSlowStageEvidence = snapshot.PreviewPacingSlowStageEvidence,
            FlashbackPlaybackState = snapshot.FlashbackPlaybackState,
            FlashbackPlaybackTargetFps = snapshot.FlashbackPlaybackTargetFps,
            FlashbackPlaybackObservedFps = snapshot.FlashbackPlaybackObservedFps,
            FlashbackPlaybackP99FrameMs = snapshot.FlashbackPlaybackP99FrameMs,
            FlashbackPlaybackMaxFrameMs = snapshot.FlashbackPlaybackMaxFrameMs,
            FlashbackPlaybackOnePercentLowFps = snapshot.FlashbackPlaybackOnePercentLowFps,
            FlashbackPlaybackFivePercentLowFps = snapshot.FlashbackPlaybackFivePercentLowFps,
            FlashbackPlaybackSlowFramePercent = snapshot.FlashbackPlaybackSlowFramePercent,
            FlashbackPlaybackDecodeP99Ms = snapshot.FlashbackPlaybackDecodeP99Ms,
            FlashbackPlaybackDecodeMaxMs = snapshot.FlashbackPlaybackDecodeMaxMs,
            FlashbackPlaybackMaxDecodePhase = snapshot.FlashbackPlaybackMaxDecodePhase,
            FlashbackPlaybackMaxDecodeReceiveMs = snapshot.FlashbackPlaybackMaxDecodeReceiveMs,
            FlashbackPlaybackMaxDecodeFeedMs = snapshot.FlashbackPlaybackMaxDecodeFeedMs,
            FlashbackPlaybackMaxDecodeReadMs = snapshot.FlashbackPlaybackMaxDecodeReadMs,
            FlashbackPlaybackMaxDecodeSendMs = snapshot.FlashbackPlaybackMaxDecodeSendMs,
            FlashbackPlaybackMaxDecodeAudioMs = snapshot.FlashbackPlaybackMaxDecodeAudioMs,
            FlashbackPlaybackMaxDecodeConvertMs = snapshot.FlashbackPlaybackMaxDecodeConvertMs,
            FlashbackPlaybackMaxDecodeUtcUnixMs = snapshot.FlashbackPlaybackMaxDecodeUtcUnixMs,
            FlashbackPlaybackMaxDecodePositionMs = snapshot.FlashbackPlaybackMaxDecodePositionMs,
            FlashbackPlaybackSeekForwardDecodeCapHits = snapshot.FlashbackPlaybackSeekForwardDecodeCapHits,
            FlashbackPlaybackLastSeekHitForwardDecodeCap = snapshot.FlashbackPlaybackLastSeekHitForwardDecodeCap,
            FlashbackPlaybackPendingCommands = snapshot.FlashbackPlaybackPendingCommands,
            FlashbackPlaybackMaxPendingCommands = snapshot.FlashbackPlaybackMaxPendingCommands,
            FlashbackPlaybackCommandsEnqueued = snapshot.FlashbackPlaybackCommandsEnqueued,
            FlashbackPlaybackCommandsProcessed = snapshot.FlashbackPlaybackCommandsProcessed,
            FlashbackPlaybackCommandsDropped = snapshot.FlashbackPlaybackCommandsDropped,
            FlashbackPlaybackCommandsSkippedNotReady = snapshot.FlashbackPlaybackCommandsSkippedNotReady,
            FlashbackPlaybackScrubUpdatesCoalesced = snapshot.FlashbackPlaybackScrubUpdatesCoalesced,
            FlashbackPlaybackSeekCommandsCoalesced = snapshot.FlashbackPlaybackSeekCommandsCoalesced,
            FlashbackPlaybackLastCommandQueued = snapshot.FlashbackPlaybackLastCommandQueued,
            FlashbackPlaybackLastCommandProcessed = snapshot.FlashbackPlaybackLastCommandProcessed,
            FlashbackPlaybackMaxCommandQueueLatencyMs = snapshot.FlashbackPlaybackMaxCommandQueueLatencyMs,
            FlashbackPlaybackMaxCommandQueueLatencyCommand = snapshot.FlashbackPlaybackMaxCommandQueueLatencyCommand,
            FlashbackPlaybackSubmitFailures = snapshot.FlashbackPlaybackSubmitFailures,
            FlashbackPlaybackLastDropUtcUnixMs = snapshot.FlashbackPlaybackLastDropUtcUnixMs,
            FlashbackPlaybackLastDropReason = snapshot.FlashbackPlaybackLastDropReason,
            FlashbackPlaybackLastSubmitFailureUtcUnixMs = snapshot.FlashbackPlaybackLastSubmitFailureUtcUnixMs,
            FlashbackPlaybackLastSubmitFailure = snapshot.FlashbackPlaybackLastSubmitFailure,
            FlashbackPlaybackDroppedFrames = snapshot.FlashbackPlaybackDroppedFrames,
            FlashbackPlaybackAudioMasterDelayDoubles = snapshot.FlashbackPlaybackAudioMasterDelayDoubles,
            FlashbackPlaybackAudioMasterDelayShrinks = snapshot.FlashbackPlaybackAudioMasterDelayShrinks,
            FlashbackPlaybackAudioMasterFallbacks = snapshot.FlashbackPlaybackAudioMasterFallbacks,
            FlashbackPlaybackAudioMasterUnavailableFallbacks = snapshot.FlashbackPlaybackAudioMasterUnavailableFallbacks,
            FlashbackPlaybackAudioMasterStaleFallbacks = snapshot.FlashbackPlaybackAudioMasterStaleFallbacks,
            FlashbackPlaybackAudioMasterDriftOutlierFallbacks = snapshot.FlashbackPlaybackAudioMasterDriftOutlierFallbacks,
            FlashbackPlaybackAudioMasterLastFallbackReason = snapshot.FlashbackPlaybackAudioMasterLastFallbackReason,
            FlashbackPlaybackAudioMasterLastFallbackClockAgeMs = snapshot.FlashbackPlaybackAudioMasterLastFallbackClockAgeMs,
            FlashbackPlaybackSegmentSwitches = snapshot.FlashbackPlaybackSegmentSwitches,
            FlashbackPlaybackFmp4Reopens = snapshot.FlashbackPlaybackFmp4Reopens,
            FlashbackPlaybackWriteHeadWaits = snapshot.FlashbackPlaybackWriteHeadWaits,
            FlashbackPlaybackNearLiveSnaps = snapshot.FlashbackPlaybackNearLiveSnaps,
            FlashbackPlaybackDecodeErrorSnaps = snapshot.FlashbackPlaybackDecodeErrorSnaps,
            FlashbackPlaybackLastWriteHeadWaitGapMs = snapshot.FlashbackPlaybackLastWriteHeadWaitGapMs,
            FlashbackPlaybackLastCommandFailureUtcUnixMs = snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs,
            FlashbackPlaybackLastCommandFailure = snapshot.FlashbackPlaybackLastCommandFailure,
            FlashbackBackendSettingsStale = snapshot.FlashbackBackendSettingsStale,
            FlashbackBackendSettingsStaleReason = snapshot.FlashbackBackendSettingsStaleReason,
            FlashbackBackendActiveFormat = snapshot.FlashbackBackendActiveFormat,
            FlashbackBackendRequestedFormat = snapshot.FlashbackBackendRequestedFormat,
            FlashbackBackendActivePreset = snapshot.FlashbackBackendActivePreset,
            FlashbackBackendRequestedPreset = snapshot.FlashbackBackendRequestedPreset,
            FlashbackVideoQueueRejectedFrames = snapshot.FlashbackVideoQueueRejectedFrames,
            FlashbackVideoQueueLastRejectReason = snapshot.FlashbackVideoQueueLastRejectReason,
            FlashbackGpuQueueRejectedFrames = snapshot.FlashbackGpuQueueRejectedFrames,
            FlashbackGpuQueueLastRejectReason = snapshot.FlashbackGpuQueueLastRejectReason,
            FatalCleanupInProgress = snapshot.FatalCleanupInProgress,
            FlashbackCleanupInProgress = snapshot.FlashbackCleanupInProgress,
            FlashbackForceRotateRequested = snapshot.FlashbackForceRotateRequested,
            FlashbackForceRotateDraining = snapshot.FlashbackForceRotateDraining,
            FlashbackExportActive = snapshot.FlashbackExportActive,
            FlashbackExportStatus = snapshot.FlashbackExportStatus,
            FlashbackExportFailureKind = snapshot.FlashbackExportFailureKind,
            FlashbackExportElapsedMs = snapshot.FlashbackExportElapsedMs,
            FlashbackExportLastProgressAgeMs = snapshot.FlashbackExportLastProgressAgeMs,
            FlashbackExportOutputBytes = snapshot.FlashbackExportOutputBytes,
            FlashbackExportThroughputBytesPerSec = snapshot.FlashbackExportThroughputBytesPerSec,
            FlashbackExportSegmentsProcessed = snapshot.FlashbackExportSegmentsProcessed,
            FlashbackExportTotalSegments = snapshot.FlashbackExportTotalSegments,
            FlashbackExportPercent = snapshot.FlashbackExportPercent,
            FlashbackExportInPointMs = snapshot.FlashbackExportInPointMs,
            FlashbackExportOutPointMs = snapshot.FlashbackExportOutPointMs,
            FlashbackExportMessage = snapshot.FlashbackExportMessage,
            FlashbackExportForceRotateFallbacks = snapshot.FlashbackExportForceRotateFallbacks,
            FlashbackExportLastForceRotateFallbackUtcUnixMs = snapshot.FlashbackExportLastForceRotateFallbackUtcUnixMs,
            FlashbackExportLastForceRotateFallbackSegments = snapshot.FlashbackExportLastForceRotateFallbackSegments,
            FlashbackExportLastForceRotateFallbackInPointMs = snapshot.FlashbackExportLastForceRotateFallbackInPointMs,
            FlashbackExportLastForceRotateFallbackOutPointMs = snapshot.FlashbackExportLastForceRotateFallbackOutPointMs,
            PipelineLatencyMs = snapshot.EstimatedPipelineLatencyMs,
            ProcessCpuPercent = snapshot.ProcessCpuPercent,
            MemoryWorkingSetMb = snapshot.MemoryWorkingSetMb,
            MemoryManagedHeapMb = snapshot.MemoryManagedHeapMb,
            GcGen0Collections = snapshot.MemoryGcGen0Collections,
            GcGen1Collections = snapshot.MemoryGcGen1Collections,
            GcGen2Collections = snapshot.MemoryGcGen2Collections,
            GcPauseTimePercent = snapshot.MemoryGcPauseTimePercent,
            ThreadPoolWorkerAvailable = snapshot.ThreadPoolWorkerAvailable,
            ThreadPoolIoAvailable = snapshot.ThreadPoolIoAvailable
        };
        _timelineHead = (_timelineHead + 1) % TimelineCapacity;
        if (_timelineCount < TimelineCapacity)
        {
            _timelineCount++;
        }
    }
}
