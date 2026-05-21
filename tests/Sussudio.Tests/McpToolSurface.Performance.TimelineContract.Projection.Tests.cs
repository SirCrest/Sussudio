using System.Reflection;

static partial class Program
{
    private static void AssertMcpPerformanceTimelineProjectionContracts(McpPerformanceTimelineSources sources)
    {
        var diagnosticsHubSource = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Timeline.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.FlashbackPlayback.cs");
        var entryType = RequireType("Sussudio.Models.PerformanceTimelineEntry");

        AssertContains(sources.CombinedSource, "PreviewP99Ms = AutomationSnapshotFormatter.GetDouble(item, \"PreviewCadenceP99Ms\")");
        AssertContains(sources.CombinedSource, "PreviewFivePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, \"PreviewCadenceFivePercentLowFps\")");
        AssertContains(sources.CombinedSource, "VisualCadenceChangeObservedFps = AutomationSnapshotFormatter.GetDouble(item, \"VisualCadenceChangeObservedFps\")");
        AssertContains(sources.CombinedSource, "MjpegPacketHashUniqueObservedFps = AutomationSnapshotFormatter.GetDouble(item, \"MjpegPacketHashUniqueObservedFps\")");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackFivePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, \"FlashbackPlaybackFivePercentLowFps\")");
        AssertContains(sources.CombinedSource, "PreviewD3DRenderSubmitCpuP99Ms");
        AssertContains(sources.CombinedSource, "PreviewD3DPresentCallP99Ms");
        AssertContains(sources.CombinedSource, "PreviewD3DTotalFrameCpuP99Ms");
        AssertContains(sources.CombinedSource, "PreviewD3DFrameLatencyWaitTimeoutCount");
        AssertContains(sources.CombinedSource, "PreviewD3DFrameLatencyWaitP95Ms");
        AssertContains(sources.CombinedSource, "PreviewD3DFrameLatencyWaitMaxMs");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackP99FrameMs");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackTargetFps");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackDecodeP99Ms");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackPendingCommands");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackCommandsEnqueued");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackCommandsProcessed");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackCommandsDropped");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackCommandsSkippedNotReady");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackScrubUpdatesCoalesced");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackSeekCommandsCoalesced");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackMaxCommandQueueLatencyCommand");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastCommandQueued");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastCommandProcessed");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackSubmitFailures");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastDropUtcUnixMs");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastDropReason");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastSubmitFailureUtcUnixMs");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastSubmitFailure");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackSegmentSwitches");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackFmp4Reopens");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackWriteHeadWaits");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackNearLiveSnaps");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastCommandFailureUtcUnixMs");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastWriteHeadWaitGapMs");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastCommandFailure");
        AssertContains(sources.CombinedSource, "FlashbackVideoQueueRejectedFrames");
        AssertContains(sources.CombinedSource, "FlashbackVideoQueueLastRejectReason");
        AssertContains(sources.CombinedSource, "FlashbackGpuQueueRejectedFrames");
        AssertContains(sources.CombinedSource, "FlashbackGpuQueueLastRejectReason");
        AssertContains(sources.CombinedSource, "FatalCleanupInProgress");
        AssertContains(sources.CombinedSource, "FlashbackCleanupInProgress");
        AssertContains(sources.CombinedSource, "FlashbackForceRotateRequested");
        AssertContains(sources.CombinedSource, "FlashbackForceRotateDraining");
        AssertContains(sources.CombinedSource, "FlashbackExportFailureKind");
        AssertContains(sources.CombinedSource, "FlashbackExportPercent");
        AssertContains(sources.CombinedSource, "FlashbackExportInPointMs");
        AssertContains(sources.CombinedSource, "FlashbackExportOutPointMs");
        AssertContains(sources.CombinedSource, "FlashbackExportMessage");
        AssertContains(sources.CombinedSource, "FlashbackExportThroughputBytesPerSec");
        AssertContains(sources.CombinedSource, "FlashbackExportLastProgressAgeMs");
        AssertContains(sources.CombinedSource, "MjpegPreviewJitterLatencyP95Ms");
        AssertContains(sources.CombinedSource, "MjpegPreviewJitterDeadlineDropCount");
        AssertContains(sources.CombinedSource, "MjpegPreviewJitterClearedDropCount");
        AssertContains(sources.CombinedSource, "MjpegPreviewJitterResumeReprimeCount");
        AssertContains(sources.CombinedSource, "MjpegPreviewJitterLastDropReason");
        AssertContains(sources.CombinedSource, "PreviewPacingLikelySlowStage");

        foreach (var propertyName in new[]
                 {
                     "CaptureCadenceFivePercentLowFps",
                     "PreviewCadenceFivePercentLowFps",
                     "VisualCadenceChangeObservedFps",
                     "VisualCadenceRepeatFramePercent",
                     "VisualCadenceMotionConfidence",
                     "MjpegPacketHashInputObservedFps",
                     "MjpegPacketHashUniqueObservedFps",
                     "MjpegPacketHashDuplicateFramePercent",
                     "PreviewPacingLikelySlowStage",
                     "PreviewPacingSlowStageConfidence",
                     "PreviewPacingSlowStageEvidence",
                     "FlashbackPlaybackFivePercentLowFps",
                     "FlashbackPlaybackCommandsEnqueued",
                     "FlashbackPlaybackCommandsProcessed",
                     "FlashbackPlaybackCommandsDropped",
                     "FlashbackPlaybackCommandsSkippedNotReady",
                     "FlashbackPlaybackScrubUpdatesCoalesced",
                     "FlashbackPlaybackSeekCommandsCoalesced",
                     "FlashbackPlaybackLastCommandQueued",
                     "FlashbackPlaybackLastCommandProcessed"
                 })
        {
            AssertNotNull(entryType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance), $"PerformanceTimelineEntry.{propertyName}");
            if (propertyName.StartsWith("FlashbackPlayback", StringComparison.Ordinal))
            {
                var projectionName = propertyName["FlashbackPlayback".Length..];
                AssertContains(diagnosticsHubSource, $"{propertyName} = flashbackPlayback.{projectionName}");
                AssertContains(diagnosticsHubSource, $"{projectionName}: snapshot.{propertyName}");
            }
            else
            {
                AssertContains(diagnosticsHubSource, $"{propertyName} = snapshot.{propertyName}");
            }
        }
    }
}
