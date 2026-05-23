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
        _timelineBuffer[_timelineHead] = BuildPerformanceTimelineEntry(snapshot);
        _timelineHead = (_timelineHead + 1) % TimelineCapacity;
        if (_timelineCount < TimelineCapacity)
        {
            _timelineCount++;
        }
    }

    private static PerformanceTimelineCoreProjection BuildPerformanceTimelineCoreProjection(
        AutomationSnapshot snapshot)
        => new(
            TimestampUtc: snapshot.TimestampUtc,
            CaptureFps: snapshot.CaptureCadenceObservedFps,
            PreviewFps: snapshot.PreviewCadenceObservedFps,
            VideoQueueDepth: snapshot.FfmpegVideoQueueDepth,
            VideoDrops: snapshot.VideoDropsQueueSaturated,
            CaptureCadenceAverageMs: snapshot.CaptureCadenceAverageIntervalMs,
            CaptureCadenceP95Ms: snapshot.CaptureCadenceP95IntervalMs,
            CaptureCadenceP99Ms: snapshot.CaptureCadenceP99IntervalMs,
            CaptureCadenceMaxMs: snapshot.CaptureCadenceMaxIntervalMs,
            CaptureCadenceOnePercentLowFps: snapshot.CaptureCadenceOnePercentLowFps,
            CaptureCadenceFivePercentLowFps: snapshot.CaptureCadenceFivePercentLowFps);

    private static PerformanceTimelineEntry BuildPerformanceTimelineEntry(AutomationSnapshot snapshot)
    {
        var core = BuildPerformanceTimelineCoreProjection(snapshot);
        var preview = BuildPerformanceTimelinePreviewProjection(snapshot);
        var flashbackPlayback = BuildPerformanceTimelineFlashbackPlaybackProjection(snapshot);
        var flashbackExport = BuildPerformanceTimelineFlashbackExportProjection(snapshot);
        var system = BuildPerformanceTimelineSystemProjection(snapshot);

        return new()
        {
            TimestampUtc = core.TimestampUtc,
            CaptureFps = core.CaptureFps,
            PreviewFps = core.PreviewFps,
            VideoQueueDepth = core.VideoQueueDepth,
            VideoDrops = core.VideoDrops,
            CaptureCadenceAverageMs = core.CaptureCadenceAverageMs,
            CaptureCadenceP95Ms = core.CaptureCadenceP95Ms,
            CaptureCadenceP99Ms = core.CaptureCadenceP99Ms,
            CaptureCadenceMaxMs = core.CaptureCadenceMaxMs,
            CaptureCadenceOnePercentLowFps = core.CaptureCadenceOnePercentLowFps,
            CaptureCadenceFivePercentLowFps = core.CaptureCadenceFivePercentLowFps,
            PreviewCadenceAverageMs = preview.CadenceAverageMs,
            PreviewCadenceP95Ms = preview.CadenceP95Ms,
            PreviewCadenceP99Ms = preview.CadenceP99Ms,
            PreviewCadenceMaxMs = preview.CadenceMaxMs,
            PreviewCadenceOnePercentLowFps = preview.CadenceOnePercentLowFps,
            PreviewCadenceFivePercentLowFps = preview.CadenceFivePercentLowFps,
            PreviewCadenceSlowFramePercent = preview.CadenceSlowFramePercent,
            VisualCadenceChangeObservedFps = preview.VisualCadenceChangeObservedFps,
            VisualCadenceRepeatFramePercent = preview.VisualCadenceRepeatFramePercent,
            VisualCadenceMotionConfidence = preview.VisualCadenceMotionConfidence,
            MjpegPacketHashInputObservedFps = preview.MjpegPacketHashInputObservedFps,
            MjpegPacketHashUniqueObservedFps = preview.MjpegPacketHashUniqueObservedFps,
            MjpegPacketHashDuplicateFramePercent = preview.MjpegPacketHashDuplicateFramePercent,
            MjpegPreviewJitterEnabled = preview.MjpegPreviewJitterEnabled,
            MjpegPreviewJitterTargetDepth = preview.MjpegPreviewJitterTargetDepth,
            MjpegPreviewJitterMaxDepth = preview.MjpegPreviewJitterMaxDepth,
            MjpegPreviewJitterQueueDepth = preview.MjpegPreviewJitterQueueDepth,
            MjpegPreviewJitterTotalDropped = preview.MjpegPreviewJitterTotalDropped,
            MjpegPreviewJitterDeadlineDropCount = preview.MjpegPreviewJitterDeadlineDropCount,
            MjpegPreviewJitterClearedDropCount = preview.MjpegPreviewJitterClearedDropCount,
            MjpegPreviewJitterUnderflowCount = preview.MjpegPreviewJitterUnderflowCount,
            MjpegPreviewJitterResumeReprimeCount = preview.MjpegPreviewJitterResumeReprimeCount,
            MjpegPreviewJitterLatencyP95Ms = preview.MjpegPreviewJitterLatencyP95Ms,
            MjpegPreviewJitterLatencyMaxMs = preview.MjpegPreviewJitterLatencyMaxMs,
            MjpegPreviewJitterLastDropReason = preview.MjpegPreviewJitterLastDropReason,
            MjpegPreviewJitterLastUnderflowReason = preview.MjpegPreviewJitterLastUnderflowReason,
            MjpegPreviewJitterLastUnderflowInputAgeMs = preview.MjpegPreviewJitterLastUnderflowInputAgeMs,
            MjpegPreviewJitterLastUnderflowOutputAgeMs = preview.MjpegPreviewJitterLastUnderflowOutputAgeMs,
            MjpegPreviewJitterMaxScheduleLateMs = preview.MjpegPreviewJitterMaxScheduleLateMs,
            MjpegPreviewJitterScheduleLateCount = preview.MjpegPreviewJitterScheduleLateCount,
            PreviewD3DPendingFrameCount = preview.D3DPendingFrameCount,
            PreviewD3DPresentCallP95Ms = preview.D3DPresentCallP95Ms,
            PreviewD3DTotalFrameCpuP95Ms = preview.D3DTotalFrameCpuP95Ms,
            PreviewD3DInputUploadCpuP99Ms = preview.D3DInputUploadCpuP99Ms,
            PreviewD3DRenderSubmitCpuP99Ms = preview.D3DRenderSubmitCpuP99Ms,
            PreviewD3DPresentCallP99Ms = preview.D3DPresentCallP99Ms,
            PreviewD3DTotalFrameCpuP99Ms = preview.D3DTotalFrameCpuP99Ms,
            PreviewD3DPipelineLatencyP95Ms = preview.D3DPipelineLatencyP95Ms,
            PreviewD3DPipelineLatencyP99Ms = preview.D3DPipelineLatencyP99Ms,
            PreviewD3DPipelineLatencyMaxMs = preview.D3DPipelineLatencyMaxMs,
            PreviewD3DFrameLatencyWaitTimeoutCount = preview.D3DFrameLatencyWaitTimeoutCount,
            PreviewD3DFrameLatencyWaitP95Ms = preview.D3DFrameLatencyWaitP95Ms,
            PreviewD3DFrameLatencyWaitMaxMs = preview.D3DFrameLatencyWaitMaxMs,
            PreviewD3DFrameStatsRecentMissedRefreshCount = preview.D3DFrameStatsRecentMissedRefreshCount,
            PreviewD3DFrameStatsRecentFailureCount = preview.D3DFrameStatsRecentFailureCount,
            PreviewD3DLastRenderedSchedulerToPresentMs = preview.D3DLastRenderedSchedulerToPresentMs,
            PreviewD3DLastRenderedPipelineLatencyMs = preview.D3DLastRenderedPipelineLatencyMs,
            PreviewD3DLastDropReason = preview.D3DLastDropReason,
            PreviewPacingLikelySlowStage = preview.PacingLikelySlowStage,
            PreviewPacingSlowStageConfidence = preview.PacingSlowStageConfidence,
            PreviewPacingSlowStageEvidence = preview.PacingSlowStageEvidence,
            FlashbackPlaybackState = flashbackPlayback.State,
            FlashbackPlaybackTargetFps = flashbackPlayback.TargetFps,
            FlashbackPlaybackObservedFps = flashbackPlayback.ObservedFps,
            FlashbackPlaybackP99FrameMs = flashbackPlayback.P99FrameMs,
            FlashbackPlaybackMaxFrameMs = flashbackPlayback.MaxFrameMs,
            FlashbackPlaybackOnePercentLowFps = flashbackPlayback.OnePercentLowFps,
            FlashbackPlaybackFivePercentLowFps = flashbackPlayback.FivePercentLowFps,
            FlashbackPlaybackSlowFramePercent = flashbackPlayback.SlowFramePercent,
            FlashbackPlaybackDecodeP99Ms = flashbackPlayback.DecodeP99Ms,
            FlashbackPlaybackDecodeMaxMs = flashbackPlayback.DecodeMaxMs,
            FlashbackPlaybackMaxDecodePhase = flashbackPlayback.MaxDecodePhase,
            FlashbackPlaybackMaxDecodeReceiveMs = flashbackPlayback.MaxDecodeReceiveMs,
            FlashbackPlaybackMaxDecodeFeedMs = flashbackPlayback.MaxDecodeFeedMs,
            FlashbackPlaybackMaxDecodeReadMs = flashbackPlayback.MaxDecodeReadMs,
            FlashbackPlaybackMaxDecodeSendMs = flashbackPlayback.MaxDecodeSendMs,
            FlashbackPlaybackMaxDecodeAudioMs = flashbackPlayback.MaxDecodeAudioMs,
            FlashbackPlaybackMaxDecodeConvertMs = flashbackPlayback.MaxDecodeConvertMs,
            FlashbackPlaybackMaxDecodeUtcUnixMs = flashbackPlayback.MaxDecodeUtcUnixMs,
            FlashbackPlaybackMaxDecodePositionMs = flashbackPlayback.MaxDecodePositionMs,
            FlashbackPlaybackSeekForwardDecodeCapHits = flashbackPlayback.SeekForwardDecodeCapHits,
            FlashbackPlaybackLastSeekHitForwardDecodeCap = flashbackPlayback.LastSeekHitForwardDecodeCap,
            FlashbackPlaybackPendingCommands = flashbackPlayback.PendingCommands,
            FlashbackPlaybackMaxPendingCommands = flashbackPlayback.MaxPendingCommands,
            FlashbackPlaybackCommandsEnqueued = flashbackPlayback.CommandsEnqueued,
            FlashbackPlaybackCommandsProcessed = flashbackPlayback.CommandsProcessed,
            FlashbackPlaybackCommandsDropped = flashbackPlayback.CommandsDropped,
            FlashbackPlaybackCommandsSkippedNotReady = flashbackPlayback.CommandsSkippedNotReady,
            FlashbackPlaybackScrubUpdatesCoalesced = flashbackPlayback.ScrubUpdatesCoalesced,
            FlashbackPlaybackSeekCommandsCoalesced = flashbackPlayback.SeekCommandsCoalesced,
            FlashbackPlaybackLastCommandQueued = flashbackPlayback.LastCommandQueued,
            FlashbackPlaybackLastCommandProcessed = flashbackPlayback.LastCommandProcessed,
            FlashbackPlaybackMaxCommandQueueLatencyMs = flashbackPlayback.MaxCommandQueueLatencyMs,
            FlashbackPlaybackMaxCommandQueueLatencyCommand = flashbackPlayback.MaxCommandQueueLatencyCommand,
            FlashbackPlaybackSubmitFailures = flashbackPlayback.SubmitFailures,
            FlashbackPlaybackLastDropUtcUnixMs = flashbackPlayback.LastDropUtcUnixMs,
            FlashbackPlaybackLastDropReason = flashbackPlayback.LastDropReason,
            FlashbackPlaybackLastSubmitFailureUtcUnixMs = flashbackPlayback.LastSubmitFailureUtcUnixMs,
            FlashbackPlaybackLastSubmitFailure = flashbackPlayback.LastSubmitFailure,
            FlashbackPlaybackDroppedFrames = flashbackPlayback.DroppedFrames,
            FlashbackPlaybackAudioMasterDelayDoubles = flashbackPlayback.AudioMasterDelayDoubles,
            FlashbackPlaybackAudioMasterDelayShrinks = flashbackPlayback.AudioMasterDelayShrinks,
            FlashbackPlaybackAudioMasterFallbacks = flashbackPlayback.AudioMasterFallbacks,
            FlashbackPlaybackAudioMasterUnavailableFallbacks = flashbackPlayback.AudioMasterUnavailableFallbacks,
            FlashbackPlaybackAudioMasterStaleFallbacks = flashbackPlayback.AudioMasterStaleFallbacks,
            FlashbackPlaybackAudioMasterDriftOutlierFallbacks = flashbackPlayback.AudioMasterDriftOutlierFallbacks,
            FlashbackPlaybackAudioMasterLastFallbackReason = flashbackPlayback.AudioMasterLastFallbackReason,
            FlashbackPlaybackAudioMasterLastFallbackClockAgeMs = flashbackPlayback.AudioMasterLastFallbackClockAgeMs,
            FlashbackPlaybackSegmentSwitches = flashbackPlayback.SegmentSwitches,
            FlashbackPlaybackFmp4Reopens = flashbackPlayback.Fmp4Reopens,
            FlashbackPlaybackWriteHeadWaits = flashbackPlayback.WriteHeadWaits,
            FlashbackPlaybackNearLiveSnaps = flashbackPlayback.NearLiveSnaps,
            FlashbackPlaybackDecodeErrorSnaps = flashbackPlayback.DecodeErrorSnaps,
            FlashbackPlaybackLastWriteHeadWaitGapMs = flashbackPlayback.LastWriteHeadWaitGapMs,
            FlashbackPlaybackLastCommandFailureUtcUnixMs = flashbackPlayback.LastCommandFailureUtcUnixMs,
            FlashbackPlaybackLastCommandFailure = flashbackPlayback.LastCommandFailure,
            FlashbackBackendSettingsStale = flashbackPlayback.BackendSettingsStale,
            FlashbackBackendSettingsStaleReason = flashbackPlayback.BackendSettingsStaleReason,
            FlashbackBackendActiveFormat = flashbackPlayback.BackendActiveFormat,
            FlashbackBackendRequestedFormat = flashbackPlayback.BackendRequestedFormat,
            FlashbackBackendActivePreset = flashbackPlayback.BackendActivePreset,
            FlashbackBackendRequestedPreset = flashbackPlayback.BackendRequestedPreset,
            FlashbackVideoQueueRejectedFrames = flashbackPlayback.VideoQueueRejectedFrames,
            FlashbackVideoQueueLastRejectReason = flashbackPlayback.VideoQueueLastRejectReason,
            FlashbackGpuQueueRejectedFrames = flashbackPlayback.GpuQueueRejectedFrames,
            FlashbackGpuQueueLastRejectReason = flashbackPlayback.GpuQueueLastRejectReason,
            FatalCleanupInProgress = flashbackPlayback.FatalCleanupInProgress,
            FlashbackCleanupInProgress = flashbackPlayback.CleanupInProgress,
            FlashbackForceRotateRequested = flashbackPlayback.ForceRotateRequested,
            FlashbackForceRotateDraining = flashbackPlayback.ForceRotateDraining,
            FlashbackExportActive = flashbackExport.Active,
            FlashbackExportStatus = flashbackExport.Status,
            FlashbackExportFailureKind = flashbackExport.FailureKind,
            FlashbackExportElapsedMs = flashbackExport.ElapsedMs,
            FlashbackExportLastProgressAgeMs = flashbackExport.LastProgressAgeMs,
            FlashbackExportOutputBytes = flashbackExport.OutputBytes,
            FlashbackExportThroughputBytesPerSec = flashbackExport.ThroughputBytesPerSec,
            FlashbackExportSegmentsProcessed = flashbackExport.SegmentsProcessed,
            FlashbackExportTotalSegments = flashbackExport.TotalSegments,
            FlashbackExportPercent = flashbackExport.Percent,
            FlashbackExportInPointMs = flashbackExport.InPointMs,
            FlashbackExportOutPointMs = flashbackExport.OutPointMs,
            FlashbackExportMessage = flashbackExport.Message,
            FlashbackExportForceRotateFallbacks = flashbackExport.ForceRotateFallbacks,
            FlashbackExportLastForceRotateFallbackUtcUnixMs = flashbackExport.LastForceRotateFallbackUtcUnixMs,
            FlashbackExportLastForceRotateFallbackSegments = flashbackExport.LastForceRotateFallbackSegments,
            FlashbackExportLastForceRotateFallbackInPointMs = flashbackExport.LastForceRotateFallbackInPointMs,
            FlashbackExportLastForceRotateFallbackOutPointMs = flashbackExport.LastForceRotateFallbackOutPointMs,
            PipelineLatencyMs = system.PipelineLatencyMs,
            ProcessCpuPercent = system.ProcessCpuPercent,
            MemoryWorkingSetMb = system.MemoryWorkingSetMb,
            MemoryManagedHeapMb = system.MemoryManagedHeapMb,
            GcGen0Collections = system.GcGen0Collections,
            GcGen1Collections = system.GcGen1Collections,
            GcGen2Collections = system.GcGen2Collections,
            GcPauseTimePercent = system.GcPauseTimePercent,
            ThreadPoolWorkerAvailable = system.ThreadPoolWorkerAvailable,
            ThreadPoolIoAvailable = system.ThreadPoolIoAvailable
        };
    }

    private readonly record struct PerformanceTimelineCoreProjection(
        DateTimeOffset TimestampUtc,
        double CaptureFps,
        double PreviewFps,
        int VideoQueueDepth,
        long VideoDrops,
        double CaptureCadenceAverageMs,
        double CaptureCadenceP95Ms,
        double CaptureCadenceP99Ms,
        double CaptureCadenceMaxMs,
        double CaptureCadenceOnePercentLowFps,
        double CaptureCadenceFivePercentLowFps);

    private static PerformanceTimelinePreviewProjection BuildPerformanceTimelinePreviewProjection(
        AutomationSnapshot snapshot)
        => new(
            CadenceAverageMs: snapshot.PreviewCadenceAverageIntervalMs,
            CadenceP95Ms: snapshot.PreviewCadenceP95IntervalMs,
            CadenceP99Ms: snapshot.PreviewCadenceP99IntervalMs,
            CadenceMaxMs: snapshot.PreviewCadenceMaxIntervalMs,
            CadenceOnePercentLowFps: snapshot.PreviewCadenceOnePercentLowFps,
            CadenceFivePercentLowFps: snapshot.PreviewCadenceFivePercentLowFps,
            CadenceSlowFramePercent: snapshot.PreviewCadenceSlowFramePercent,
            VisualCadenceChangeObservedFps: snapshot.VisualCadenceChangeObservedFps,
            VisualCadenceRepeatFramePercent: snapshot.VisualCadenceRepeatFramePercent,
            VisualCadenceMotionConfidence: snapshot.VisualCadenceMotionConfidence,
            MjpegPacketHashInputObservedFps: snapshot.MjpegPacketHashInputObservedFps,
            MjpegPacketHashUniqueObservedFps: snapshot.MjpegPacketHashUniqueObservedFps,
            MjpegPacketHashDuplicateFramePercent: snapshot.MjpegPacketHashDuplicateFramePercent,
            MjpegPreviewJitterEnabled: snapshot.MjpegPreviewJitterEnabled,
            MjpegPreviewJitterTargetDepth: snapshot.MjpegPreviewJitterTargetDepth,
            MjpegPreviewJitterMaxDepth: snapshot.MjpegPreviewJitterMaxDepth,
            MjpegPreviewJitterQueueDepth: snapshot.MjpegPreviewJitterQueueDepth,
            MjpegPreviewJitterTotalDropped: snapshot.MjpegPreviewJitterTotalDropped,
            MjpegPreviewJitterDeadlineDropCount: snapshot.MjpegPreviewJitterDeadlineDropCount,
            MjpegPreviewJitterClearedDropCount: snapshot.MjpegPreviewJitterClearedDropCount,
            MjpegPreviewJitterUnderflowCount: snapshot.MjpegPreviewJitterUnderflowCount,
            MjpegPreviewJitterResumeReprimeCount: snapshot.MjpegPreviewJitterResumeReprimeCount,
            MjpegPreviewJitterLatencyP95Ms: snapshot.MjpegPreviewJitterLatencyP95Ms,
            MjpegPreviewJitterLatencyMaxMs: snapshot.MjpegPreviewJitterLatencyMaxMs,
            MjpegPreviewJitterLastDropReason: snapshot.MjpegPreviewJitterLastDropReason,
            MjpegPreviewJitterLastUnderflowReason: snapshot.MjpegPreviewJitterLastUnderflowReason,
            MjpegPreviewJitterLastUnderflowInputAgeMs: snapshot.MjpegPreviewJitterLastUnderflowInputAgeMs,
            MjpegPreviewJitterLastUnderflowOutputAgeMs: snapshot.MjpegPreviewJitterLastUnderflowOutputAgeMs,
            MjpegPreviewJitterMaxScheduleLateMs: snapshot.MjpegPreviewJitterMaxScheduleLateMs,
            MjpegPreviewJitterScheduleLateCount: snapshot.MjpegPreviewJitterScheduleLateCount,
            D3DPendingFrameCount: snapshot.PreviewD3DPendingFrameCount,
            D3DPresentCallP95Ms: snapshot.PreviewD3DPresentCallP95Ms,
            D3DTotalFrameCpuP95Ms: snapshot.PreviewD3DTotalFrameCpuP95Ms,
            D3DInputUploadCpuP99Ms: snapshot.PreviewD3DInputUploadCpuP99Ms,
            D3DRenderSubmitCpuP99Ms: snapshot.PreviewD3DRenderSubmitCpuP99Ms,
            D3DPresentCallP99Ms: snapshot.PreviewD3DPresentCallP99Ms,
            D3DTotalFrameCpuP99Ms: snapshot.PreviewD3DTotalFrameCpuP99Ms,
            D3DPipelineLatencyP95Ms: snapshot.PreviewD3DPipelineLatencyP95Ms,
            D3DPipelineLatencyP99Ms: snapshot.PreviewD3DPipelineLatencyP99Ms,
            D3DPipelineLatencyMaxMs: snapshot.PreviewD3DPipelineLatencyMaxMs,
            D3DFrameLatencyWaitTimeoutCount: snapshot.PreviewD3DFrameLatencyWaitTimeoutCount,
            D3DFrameLatencyWaitP95Ms: snapshot.PreviewD3DFrameLatencyWaitP95Ms,
            D3DFrameLatencyWaitMaxMs: snapshot.PreviewD3DFrameLatencyWaitMaxMs,
            D3DFrameStatsRecentMissedRefreshCount: snapshot.PreviewD3DFrameStatsRecentMissedRefreshCount,
            D3DFrameStatsRecentFailureCount: snapshot.PreviewD3DFrameStatsRecentFailureCount,
            D3DLastRenderedSchedulerToPresentMs: snapshot.PreviewD3DLastRenderedSchedulerToPresentMs,
            D3DLastRenderedPipelineLatencyMs: snapshot.PreviewD3DLastRenderedPipelineLatencyMs,
            D3DLastDropReason: snapshot.PreviewD3DLastDropReason,
            PacingLikelySlowStage: snapshot.PreviewPacingLikelySlowStage,
            PacingSlowStageConfidence: snapshot.PreviewPacingSlowStageConfidence,
            PacingSlowStageEvidence: snapshot.PreviewPacingSlowStageEvidence);

    private readonly record struct PerformanceTimelinePreviewProjection(
        double CadenceAverageMs,
        double CadenceP95Ms,
        double CadenceP99Ms,
        double CadenceMaxMs,
        double CadenceOnePercentLowFps,
        double CadenceFivePercentLowFps,
        double CadenceSlowFramePercent,
        double VisualCadenceChangeObservedFps,
        double VisualCadenceRepeatFramePercent,
        string VisualCadenceMotionConfidence,
        double MjpegPacketHashInputObservedFps,
        double MjpegPacketHashUniqueObservedFps,
        double MjpegPacketHashDuplicateFramePercent,
        bool MjpegPreviewJitterEnabled,
        int MjpegPreviewJitterTargetDepth,
        int MjpegPreviewJitterMaxDepth,
        int MjpegPreviewJitterQueueDepth,
        long MjpegPreviewJitterTotalDropped,
        long MjpegPreviewJitterDeadlineDropCount,
        long MjpegPreviewJitterClearedDropCount,
        long MjpegPreviewJitterUnderflowCount,
        long MjpegPreviewJitterResumeReprimeCount,
        double MjpegPreviewJitterLatencyP95Ms,
        double MjpegPreviewJitterLatencyMaxMs,
        string MjpegPreviewJitterLastDropReason,
        string MjpegPreviewJitterLastUnderflowReason,
        double MjpegPreviewJitterLastUnderflowInputAgeMs,
        double MjpegPreviewJitterLastUnderflowOutputAgeMs,
        double MjpegPreviewJitterMaxScheduleLateMs,
        long MjpegPreviewJitterScheduleLateCount,
        int D3DPendingFrameCount,
        double D3DPresentCallP95Ms,
        double D3DTotalFrameCpuP95Ms,
        double D3DInputUploadCpuP99Ms,
        double D3DRenderSubmitCpuP99Ms,
        double D3DPresentCallP99Ms,
        double D3DTotalFrameCpuP99Ms,
        double D3DPipelineLatencyP95Ms,
        double D3DPipelineLatencyP99Ms,
        double D3DPipelineLatencyMaxMs,
        long D3DFrameLatencyWaitTimeoutCount,
        double D3DFrameLatencyWaitP95Ms,
        double D3DFrameLatencyWaitMaxMs,
        long D3DFrameStatsRecentMissedRefreshCount,
        long D3DFrameStatsRecentFailureCount,
        double D3DLastRenderedSchedulerToPresentMs,
        double D3DLastRenderedPipelineLatencyMs,
        string D3DLastDropReason,
        string PacingLikelySlowStage,
        string PacingSlowStageConfidence,
        string PacingSlowStageEvidence);

    private static PerformanceTimelineFlashbackPlaybackProjection BuildPerformanceTimelineFlashbackPlaybackProjection(
        AutomationSnapshot snapshot)
    {
        var cadence = BuildPerformanceTimelineFlashbackPlaybackCadenceProjection(snapshot);
        var decode = BuildPerformanceTimelineFlashbackPlaybackDecodeProjection(snapshot);
        var commands = BuildPerformanceTimelineFlashbackPlaybackCommandsProjection(snapshot);
        var audioMaster = BuildPerformanceTimelineFlashbackPlaybackAudioMasterProjection(snapshot);
        var stages = BuildPerformanceTimelineFlashbackPlaybackStagesProjection(snapshot);
        var backend = BuildPerformanceTimelineFlashbackPlaybackBackendProjection(snapshot);

        return new(
            State: snapshot.FlashbackPlaybackState,
            TargetFps: cadence.TargetFps,
            ObservedFps: cadence.ObservedFps,
            P99FrameMs: cadence.P99FrameMs,
            MaxFrameMs: cadence.MaxFrameMs,
            OnePercentLowFps: cadence.OnePercentLowFps,
            FivePercentLowFps: cadence.FivePercentLowFps,
            SlowFramePercent: cadence.SlowFramePercent,
            DecodeP99Ms: decode.DecodeP99Ms,
            DecodeMaxMs: decode.DecodeMaxMs,
            MaxDecodePhase: decode.MaxDecodePhase,
            MaxDecodeReceiveMs: decode.MaxDecodeReceiveMs,
            MaxDecodeFeedMs: decode.MaxDecodeFeedMs,
            MaxDecodeReadMs: decode.MaxDecodeReadMs,
            MaxDecodeSendMs: decode.MaxDecodeSendMs,
            MaxDecodeAudioMs: decode.MaxDecodeAudioMs,
            MaxDecodeConvertMs: decode.MaxDecodeConvertMs,
            MaxDecodeUtcUnixMs: decode.MaxDecodeUtcUnixMs,
            MaxDecodePositionMs: decode.MaxDecodePositionMs,
            SeekForwardDecodeCapHits: decode.SeekForwardDecodeCapHits,
            LastSeekHitForwardDecodeCap: decode.LastSeekHitForwardDecodeCap,
            PendingCommands: commands.PendingCommands,
            MaxPendingCommands: commands.MaxPendingCommands,
            CommandsEnqueued: commands.CommandsEnqueued,
            CommandsProcessed: commands.CommandsProcessed,
            CommandsDropped: commands.CommandsDropped,
            CommandsSkippedNotReady: commands.CommandsSkippedNotReady,
            ScrubUpdatesCoalesced: commands.ScrubUpdatesCoalesced,
            SeekCommandsCoalesced: commands.SeekCommandsCoalesced,
            LastCommandQueued: commands.LastCommandQueued,
            LastCommandProcessed: commands.LastCommandProcessed,
            MaxCommandQueueLatencyMs: commands.MaxCommandQueueLatencyMs,
            MaxCommandQueueLatencyCommand: commands.MaxCommandQueueLatencyCommand,
            SubmitFailures: stages.SubmitFailures,
            LastDropUtcUnixMs: stages.LastDropUtcUnixMs,
            LastDropReason: stages.LastDropReason,
            LastSubmitFailureUtcUnixMs: stages.LastSubmitFailureUtcUnixMs,
            LastSubmitFailure: stages.LastSubmitFailure,
            DroppedFrames: cadence.DroppedFrames,
            AudioMasterDelayDoubles: audioMaster.DelayDoubles,
            AudioMasterDelayShrinks: audioMaster.DelayShrinks,
            AudioMasterFallbacks: audioMaster.Fallbacks,
            AudioMasterUnavailableFallbacks: audioMaster.UnavailableFallbacks,
            AudioMasterStaleFallbacks: audioMaster.StaleFallbacks,
            AudioMasterDriftOutlierFallbacks: audioMaster.DriftOutlierFallbacks,
            AudioMasterLastFallbackReason: audioMaster.LastFallbackReason,
            AudioMasterLastFallbackClockAgeMs: audioMaster.LastFallbackClockAgeMs,
            SegmentSwitches: stages.SegmentSwitches,
            Fmp4Reopens: stages.Fmp4Reopens,
            WriteHeadWaits: stages.WriteHeadWaits,
            NearLiveSnaps: stages.NearLiveSnaps,
            DecodeErrorSnaps: stages.DecodeErrorSnaps,
            LastWriteHeadWaitGapMs: stages.LastWriteHeadWaitGapMs,
            LastCommandFailureUtcUnixMs: commands.LastCommandFailureUtcUnixMs,
            LastCommandFailure: commands.LastCommandFailure,
            BackendSettingsStale: backend.SettingsStale,
            BackendSettingsStaleReason: backend.SettingsStaleReason,
            BackendActiveFormat: backend.ActiveFormat,
            BackendRequestedFormat: backend.RequestedFormat,
            BackendActivePreset: backend.ActivePreset,
            BackendRequestedPreset: backend.RequestedPreset,
            VideoQueueRejectedFrames: backend.VideoQueueRejectedFrames,
            VideoQueueLastRejectReason: backend.VideoQueueLastRejectReason,
            GpuQueueRejectedFrames: backend.GpuQueueRejectedFrames,
            GpuQueueLastRejectReason: backend.GpuQueueLastRejectReason,
            FatalCleanupInProgress: backend.FatalCleanupInProgress,
            CleanupInProgress: backend.CleanupInProgress,
            ForceRotateRequested: backend.ForceRotateRequested,
            ForceRotateDraining: backend.ForceRotateDraining);
    }

    private static PerformanceTimelineFlashbackPlaybackCadenceProjection BuildPerformanceTimelineFlashbackPlaybackCadenceProjection(
        AutomationSnapshot snapshot)
        => new(
            TargetFps: snapshot.FlashbackPlaybackTargetFps,
            ObservedFps: snapshot.FlashbackPlaybackObservedFps,
            P99FrameMs: snapshot.FlashbackPlaybackP99FrameMs,
            MaxFrameMs: snapshot.FlashbackPlaybackMaxFrameMs,
            OnePercentLowFps: snapshot.FlashbackPlaybackOnePercentLowFps,
            FivePercentLowFps: snapshot.FlashbackPlaybackFivePercentLowFps,
            SlowFramePercent: snapshot.FlashbackPlaybackSlowFramePercent,
            DroppedFrames: snapshot.FlashbackPlaybackDroppedFrames);

    private readonly record struct PerformanceTimelineFlashbackPlaybackCadenceProjection(
        double TargetFps,
        double ObservedFps,
        double P99FrameMs,
        double MaxFrameMs,
        double OnePercentLowFps,
        double FivePercentLowFps,
        double SlowFramePercent,
        long DroppedFrames);

    private static PerformanceTimelineFlashbackPlaybackDecodeProjection BuildPerformanceTimelineFlashbackPlaybackDecodeProjection(
        AutomationSnapshot snapshot)
        => new(
            DecodeP99Ms: snapshot.FlashbackPlaybackDecodeP99Ms,
            DecodeMaxMs: snapshot.FlashbackPlaybackDecodeMaxMs,
            MaxDecodePhase: snapshot.FlashbackPlaybackMaxDecodePhase,
            MaxDecodeReceiveMs: snapshot.FlashbackPlaybackMaxDecodeReceiveMs,
            MaxDecodeFeedMs: snapshot.FlashbackPlaybackMaxDecodeFeedMs,
            MaxDecodeReadMs: snapshot.FlashbackPlaybackMaxDecodeReadMs,
            MaxDecodeSendMs: snapshot.FlashbackPlaybackMaxDecodeSendMs,
            MaxDecodeAudioMs: snapshot.FlashbackPlaybackMaxDecodeAudioMs,
            MaxDecodeConvertMs: snapshot.FlashbackPlaybackMaxDecodeConvertMs,
            MaxDecodeUtcUnixMs: snapshot.FlashbackPlaybackMaxDecodeUtcUnixMs,
            MaxDecodePositionMs: snapshot.FlashbackPlaybackMaxDecodePositionMs,
            SeekForwardDecodeCapHits: snapshot.FlashbackPlaybackSeekForwardDecodeCapHits,
            LastSeekHitForwardDecodeCap: snapshot.FlashbackPlaybackLastSeekHitForwardDecodeCap);

    private readonly record struct PerformanceTimelineFlashbackPlaybackDecodeProjection(
        double DecodeP99Ms,
        double DecodeMaxMs,
        string MaxDecodePhase,
        double MaxDecodeReceiveMs,
        double MaxDecodeFeedMs,
        double MaxDecodeReadMs,
        double MaxDecodeSendMs,
        double MaxDecodeAudioMs,
        double MaxDecodeConvertMs,
        long MaxDecodeUtcUnixMs,
        long MaxDecodePositionMs,
        long SeekForwardDecodeCapHits,
        bool LastSeekHitForwardDecodeCap);

    private static PerformanceTimelineFlashbackPlaybackCommandsProjection BuildPerformanceTimelineFlashbackPlaybackCommandsProjection(
        AutomationSnapshot snapshot)
        => new(
            PendingCommands: snapshot.FlashbackPlaybackPendingCommands,
            MaxPendingCommands: snapshot.FlashbackPlaybackMaxPendingCommands,
            CommandsEnqueued: snapshot.FlashbackPlaybackCommandsEnqueued,
            CommandsProcessed: snapshot.FlashbackPlaybackCommandsProcessed,
            CommandsDropped: snapshot.FlashbackPlaybackCommandsDropped,
            CommandsSkippedNotReady: snapshot.FlashbackPlaybackCommandsSkippedNotReady,
            ScrubUpdatesCoalesced: snapshot.FlashbackPlaybackScrubUpdatesCoalesced,
            SeekCommandsCoalesced: snapshot.FlashbackPlaybackSeekCommandsCoalesced,
            LastCommandQueued: snapshot.FlashbackPlaybackLastCommandQueued,
            LastCommandProcessed: snapshot.FlashbackPlaybackLastCommandProcessed,
            MaxCommandQueueLatencyMs: snapshot.FlashbackPlaybackMaxCommandQueueLatencyMs,
            MaxCommandQueueLatencyCommand: snapshot.FlashbackPlaybackMaxCommandQueueLatencyCommand,
            LastCommandFailureUtcUnixMs: snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs,
            LastCommandFailure: snapshot.FlashbackPlaybackLastCommandFailure);

    private readonly record struct PerformanceTimelineFlashbackPlaybackCommandsProjection(
        int PendingCommands,
        int MaxPendingCommands,
        long CommandsEnqueued,
        long CommandsProcessed,
        long CommandsDropped,
        long CommandsSkippedNotReady,
        long ScrubUpdatesCoalesced,
        long SeekCommandsCoalesced,
        string LastCommandQueued,
        string LastCommandProcessed,
        long MaxCommandQueueLatencyMs,
        string MaxCommandQueueLatencyCommand,
        long LastCommandFailureUtcUnixMs,
        string LastCommandFailure);

    private static PerformanceTimelineFlashbackPlaybackAudioMasterProjection BuildPerformanceTimelineFlashbackPlaybackAudioMasterProjection(
        AutomationSnapshot snapshot)
        => new(
            DelayDoubles: snapshot.FlashbackPlaybackAudioMasterDelayDoubles,
            DelayShrinks: snapshot.FlashbackPlaybackAudioMasterDelayShrinks,
            Fallbacks: snapshot.FlashbackPlaybackAudioMasterFallbacks,
            UnavailableFallbacks: snapshot.FlashbackPlaybackAudioMasterUnavailableFallbacks,
            StaleFallbacks: snapshot.FlashbackPlaybackAudioMasterStaleFallbacks,
            DriftOutlierFallbacks: snapshot.FlashbackPlaybackAudioMasterDriftOutlierFallbacks,
            LastFallbackReason: snapshot.FlashbackPlaybackAudioMasterLastFallbackReason,
            LastFallbackClockAgeMs: snapshot.FlashbackPlaybackAudioMasterLastFallbackClockAgeMs);

    private readonly record struct PerformanceTimelineFlashbackPlaybackAudioMasterProjection(
        long DelayDoubles,
        long DelayShrinks,
        long Fallbacks,
        long UnavailableFallbacks,
        long StaleFallbacks,
        long DriftOutlierFallbacks,
        string LastFallbackReason,
        double LastFallbackClockAgeMs);

    private static PerformanceTimelineFlashbackPlaybackStagesProjection BuildPerformanceTimelineFlashbackPlaybackStagesProjection(
        AutomationSnapshot snapshot)
        => new(
            SubmitFailures: snapshot.FlashbackPlaybackSubmitFailures,
            LastDropUtcUnixMs: snapshot.FlashbackPlaybackLastDropUtcUnixMs,
            LastDropReason: snapshot.FlashbackPlaybackLastDropReason,
            LastSubmitFailureUtcUnixMs: snapshot.FlashbackPlaybackLastSubmitFailureUtcUnixMs,
            LastSubmitFailure: snapshot.FlashbackPlaybackLastSubmitFailure,
            SegmentSwitches: snapshot.FlashbackPlaybackSegmentSwitches,
            Fmp4Reopens: snapshot.FlashbackPlaybackFmp4Reopens,
            WriteHeadWaits: snapshot.FlashbackPlaybackWriteHeadWaits,
            NearLiveSnaps: snapshot.FlashbackPlaybackNearLiveSnaps,
            DecodeErrorSnaps: snapshot.FlashbackPlaybackDecodeErrorSnaps,
            LastWriteHeadWaitGapMs: snapshot.FlashbackPlaybackLastWriteHeadWaitGapMs);

    private readonly record struct PerformanceTimelineFlashbackPlaybackStagesProjection(
        long SubmitFailures,
        long LastDropUtcUnixMs,
        string LastDropReason,
        long LastSubmitFailureUtcUnixMs,
        string LastSubmitFailure,
        long SegmentSwitches,
        long Fmp4Reopens,
        long WriteHeadWaits,
        long NearLiveSnaps,
        long DecodeErrorSnaps,
        long LastWriteHeadWaitGapMs);

    private static PerformanceTimelineFlashbackPlaybackBackendProjection BuildPerformanceTimelineFlashbackPlaybackBackendProjection(
        AutomationSnapshot snapshot)
        => new(
            SettingsStale: snapshot.FlashbackBackendSettingsStale,
            SettingsStaleReason: snapshot.FlashbackBackendSettingsStaleReason,
            ActiveFormat: snapshot.FlashbackBackendActiveFormat,
            RequestedFormat: snapshot.FlashbackBackendRequestedFormat,
            ActivePreset: snapshot.FlashbackBackendActivePreset,
            RequestedPreset: snapshot.FlashbackBackendRequestedPreset,
            VideoQueueRejectedFrames: snapshot.FlashbackVideoQueueRejectedFrames,
            VideoQueueLastRejectReason: snapshot.FlashbackVideoQueueLastRejectReason,
            GpuQueueRejectedFrames: snapshot.FlashbackGpuQueueRejectedFrames,
            GpuQueueLastRejectReason: snapshot.FlashbackGpuQueueLastRejectReason,
            FatalCleanupInProgress: snapshot.FatalCleanupInProgress,
            CleanupInProgress: snapshot.FlashbackCleanupInProgress,
            ForceRotateRequested: snapshot.FlashbackForceRotateRequested,
            ForceRotateDraining: snapshot.FlashbackForceRotateDraining);

    private readonly record struct PerformanceTimelineFlashbackPlaybackBackendProjection(
        bool SettingsStale,
        string SettingsStaleReason,
        string ActiveFormat,
        string RequestedFormat,
        string ActivePreset,
        string RequestedPreset,
        long VideoQueueRejectedFrames,
        string VideoQueueLastRejectReason,
        long GpuQueueRejectedFrames,
        string GpuQueueLastRejectReason,
        bool FatalCleanupInProgress,
        bool CleanupInProgress,
        bool ForceRotateRequested,
        bool ForceRotateDraining);

    private readonly record struct PerformanceTimelineFlashbackPlaybackProjection(
        string State,
        double TargetFps,
        double ObservedFps,
        double P99FrameMs,
        double MaxFrameMs,
        double OnePercentLowFps,
        double FivePercentLowFps,
        double SlowFramePercent,
        double DecodeP99Ms,
        double DecodeMaxMs,
        string MaxDecodePhase,
        double MaxDecodeReceiveMs,
        double MaxDecodeFeedMs,
        double MaxDecodeReadMs,
        double MaxDecodeSendMs,
        double MaxDecodeAudioMs,
        double MaxDecodeConvertMs,
        long MaxDecodeUtcUnixMs,
        long MaxDecodePositionMs,
        long SeekForwardDecodeCapHits,
        bool LastSeekHitForwardDecodeCap,
        int PendingCommands,
        int MaxPendingCommands,
        long CommandsEnqueued,
        long CommandsProcessed,
        long CommandsDropped,
        long CommandsSkippedNotReady,
        long ScrubUpdatesCoalesced,
        long SeekCommandsCoalesced,
        string LastCommandQueued,
        string LastCommandProcessed,
        long MaxCommandQueueLatencyMs,
        string MaxCommandQueueLatencyCommand,
        long SubmitFailures,
        long LastDropUtcUnixMs,
        string LastDropReason,
        long LastSubmitFailureUtcUnixMs,
        string LastSubmitFailure,
        long DroppedFrames,
        long AudioMasterDelayDoubles,
        long AudioMasterDelayShrinks,
        long AudioMasterFallbacks,
        long AudioMasterUnavailableFallbacks,
        long AudioMasterStaleFallbacks,
        long AudioMasterDriftOutlierFallbacks,
        string AudioMasterLastFallbackReason,
        double AudioMasterLastFallbackClockAgeMs,
        long SegmentSwitches,
        long Fmp4Reopens,
        long WriteHeadWaits,
        long NearLiveSnaps,
        long DecodeErrorSnaps,
        long LastWriteHeadWaitGapMs,
        long LastCommandFailureUtcUnixMs,
        string LastCommandFailure,
        bool BackendSettingsStale,
        string BackendSettingsStaleReason,
        string BackendActiveFormat,
        string BackendRequestedFormat,
        string BackendActivePreset,
        string BackendRequestedPreset,
        long VideoQueueRejectedFrames,
        string VideoQueueLastRejectReason,
        long GpuQueueRejectedFrames,
        string GpuQueueLastRejectReason,
        bool FatalCleanupInProgress,
        bool CleanupInProgress,
        bool ForceRotateRequested,
        bool ForceRotateDraining);

    private static PerformanceTimelineFlashbackExportProjection BuildPerformanceTimelineFlashbackExportProjection(
        AutomationSnapshot snapshot)
        => new(
            Active: snapshot.FlashbackExportActive,
            Status: snapshot.FlashbackExportStatus,
            FailureKind: snapshot.FlashbackExportFailureKind,
            ElapsedMs: snapshot.FlashbackExportElapsedMs,
            LastProgressAgeMs: snapshot.FlashbackExportLastProgressAgeMs,
            OutputBytes: snapshot.FlashbackExportOutputBytes,
            ThroughputBytesPerSec: snapshot.FlashbackExportThroughputBytesPerSec,
            SegmentsProcessed: snapshot.FlashbackExportSegmentsProcessed,
            TotalSegments: snapshot.FlashbackExportTotalSegments,
            Percent: snapshot.FlashbackExportPercent,
            InPointMs: snapshot.FlashbackExportInPointMs,
            OutPointMs: snapshot.FlashbackExportOutPointMs,
            Message: snapshot.FlashbackExportMessage,
            ForceRotateFallbacks: snapshot.FlashbackExportForceRotateFallbacks,
            LastForceRotateFallbackUtcUnixMs: snapshot.FlashbackExportLastForceRotateFallbackUtcUnixMs,
            LastForceRotateFallbackSegments: snapshot.FlashbackExportLastForceRotateFallbackSegments,
            LastForceRotateFallbackInPointMs: snapshot.FlashbackExportLastForceRotateFallbackInPointMs,
            LastForceRotateFallbackOutPointMs: snapshot.FlashbackExportLastForceRotateFallbackOutPointMs);

    private readonly record struct PerformanceTimelineFlashbackExportProjection(
        bool Active,
        string Status,
        string FailureKind,
        long ElapsedMs,
        long LastProgressAgeMs,
        long OutputBytes,
        double ThroughputBytesPerSec,
        int SegmentsProcessed,
        int TotalSegments,
        double Percent,
        long InPointMs,
        long OutPointMs,
        string Message,
        long ForceRotateFallbacks,
        long LastForceRotateFallbackUtcUnixMs,
        int LastForceRotateFallbackSegments,
        long LastForceRotateFallbackInPointMs,
        long LastForceRotateFallbackOutPointMs);

    private static PerformanceTimelineSystemProjection BuildPerformanceTimelineSystemProjection(
        AutomationSnapshot snapshot)
        => new(
            PipelineLatencyMs: snapshot.EstimatedPipelineLatencyMs,
            ProcessCpuPercent: snapshot.ProcessCpuPercent,
            MemoryWorkingSetMb: snapshot.MemoryWorkingSetMb,
            MemoryManagedHeapMb: snapshot.MemoryManagedHeapMb,
            GcGen0Collections: snapshot.MemoryGcGen0Collections,
            GcGen1Collections: snapshot.MemoryGcGen1Collections,
            GcGen2Collections: snapshot.MemoryGcGen2Collections,
            GcPauseTimePercent: snapshot.MemoryGcPauseTimePercent,
            ThreadPoolWorkerAvailable: snapshot.ThreadPoolWorkerAvailable,
            ThreadPoolIoAvailable: snapshot.ThreadPoolIoAvailable);

    private readonly record struct PerformanceTimelineSystemProjection(
        long PipelineLatencyMs,
        double ProcessCpuPercent,
        double MemoryWorkingSetMb,
        double MemoryManagedHeapMb,
        int GcGen0Collections,
        int GcGen1Collections,
        int GcGen2Collections,
        double GcPauseTimePercent,
        int ThreadPoolWorkerAvailable,
        int ThreadPoolIoAvailable);
}
