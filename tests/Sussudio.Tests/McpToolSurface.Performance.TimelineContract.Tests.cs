using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task McpPerformanceTimelineTool_ExposesD3DP99StageTiming()
    {
        var source = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.cs")
            + "\n" + ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.cs")
            + "\n" + ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.cs")
            + "\n" + ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Formatting.cs")
            + "\n" + ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rendering.cs")
            + "\n" + ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Summaries.cs");
        AssertDoesNotContain(ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.cs"), "private sealed class TimelineRow");
        AssertDoesNotContain(ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.cs"), "new StringBuilder()");
        AssertDoesNotContain(ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.cs"), "== Trend Summary");
        AssertDoesNotContain(ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.cs"), "private sealed class TimelineRow");
        AssertContains(ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.cs"), "private sealed class TimelineRow");
        AssertContains(ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rendering.cs"), "BuildPerformanceTimelineText");
        AssertContains(ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Summaries.cs"), "AppendPressureSummary");
        var diagnosticsHubSource = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Timeline.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.cs");
        var entryType = RequireType("Sussudio.Models.PerformanceTimelineEntry");

        AssertContains(source, "PreviewD3DInputUploadCpuP99Ms");
        AssertContains(source, "targetOnePercentLowFps");
        AssertContains(source, "== 1% Low Target Summary");
        AssertContains(source, "AppendOnePercentLowTargetSummary");
        AssertContains(source, "AppendPressureSummary");
        AssertContains(source, "misses={belowTarget}/{valid.Length}");
        AssertContains(source, "PreviewP99Ms = AutomationSnapshotFormatter.GetDouble(item, \"PreviewCadenceP99Ms\")");
        AssertContains(source, "PreviewFivePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, \"PreviewCadenceFivePercentLowFps\")");
        AssertContains(source, "VisualCadenceChangeObservedFps = AutomationSnapshotFormatter.GetDouble(item, \"VisualCadenceChangeObservedFps\")");
        AssertContains(source, "MjpegPacketHashUniqueObservedFps = AutomationSnapshotFormatter.GetDouble(item, \"MjpegPacketHashUniqueObservedFps\")");
        AssertContains(source, "FlashbackPlaybackFivePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, \"FlashbackPlaybackFivePercentLowFps\")");
        AssertContains(source, "Preview 5% Low:");
        AssertContains(source, "Visual Cadence:");
        AssertContains(source, "MJPEG Fingerprint:");
        AssertContains(source, "Preview P99:");
        AssertContains(source, "PreviewD3DRenderSubmitCpuP99Ms");
        AssertContains(source, "PreviewD3DPresentCallP99Ms");
        AssertContains(source, "PreviewD3DTotalFrameCpuP99Ms");
        AssertContains(source, "PreviewD3DFrameLatencyWaitTimeoutCount");
        AssertContains(source, "PreviewD3DFrameLatencyWaitP95Ms");
        AssertContains(source, "PreviewD3DFrameLatencyWaitMaxMs");
        AssertContains(source, "InP99 | RsP99 | PrP99 | TotP99");
        AssertContains(source, "FlashbackPlaybackP99FrameMs");
        AssertContains(source, "FlashbackPlaybackTargetFps");
        AssertContains(source, "Flashback target:");
        AssertContains(source, "FlashbackPlaybackDecodeP99Ms");
        AssertContains(source, "FlashbackPlaybackPendingCommands");
        AssertContains(source, "FlashbackPlaybackCommandsEnqueued");
        AssertContains(source, "FlashbackPlaybackCommandsProcessed");
        AssertContains(source, "FlashbackPlaybackCommandsDropped");
        AssertContains(source, "FlashbackPlaybackCommandsSkippedNotReady");
        AssertContains(source, "FlashbackPlaybackScrubUpdatesCoalesced");
        AssertContains(source, "FlashbackPlaybackSeekCommandsCoalesced");
        AssertContains(source, "FlashbackPlaybackMaxCommandQueueLatencyCommand");
        AssertContains(source, "FlashbackPlaybackLastCommandQueued");
        AssertContains(source, "FlashbackPlaybackLastCommandProcessed");
        AssertContains(source, "FlashbackPlaybackSubmitFailures");
        AssertContains(source, "FlashbackPlaybackLastDropUtcUnixMs");
        AssertContains(source, "FlashbackPlaybackLastDropReason");
        AssertContains(source, "FlashbackPlaybackLastSubmitFailureUtcUnixMs");
        AssertContains(source, "FlashbackPlaybackLastSubmitFailure");
        AssertContains(source, "lastSubmitFailure");
        AssertContains(source, "FlashbackPlaybackSegmentSwitches");
        AssertContains(source, "FlashbackPlaybackFmp4Reopens");
        AssertContains(source, "FlashbackPlaybackWriteHeadWaits");
        AssertContains(source, "FlashbackPlaybackNearLiveSnaps");
        AssertContains(source, "FlashbackPlaybackLastCommandFailureUtcUnixMs");
        AssertContains(source, "FlashbackPlaybackLastWriteHeadWaitGapMs");
        AssertContains(source, "FlashbackPlaybackLastCommandFailure");
        AssertContains(source, "FlashbackVideoQueueRejectedFrames");
        AssertContains(source, "FlashbackVideoQueueLastRejectReason");
        AssertContains(source, "FlashbackGpuQueueRejectedFrames");
        AssertContains(source, "FlashbackGpuQueueLastRejectReason");
        AssertContains(source, "Flashback Enqueue Rejects");
        AssertContains(source, "FatalCleanupInProgress");
        AssertContains(source, "FlashbackCleanupInProgress");
        AssertContains(source, "FlashbackForceRotateRequested");
        AssertContains(source, "FlashbackForceRotateDraining");
        AssertContains(source, "FlashbackExportFailureKind");
        AssertContains(source, "FlashbackExportPercent");
        AssertContains(source, "FlashbackExportInPointMs");
        AssertContains(source, "FlashbackExportOutPointMs");
        AssertContains(source, "FlashbackExportMessage");
        AssertContains(source, "FlashbackExportThroughputBytesPerSec");
        AssertContains(source, "FlashbackExportLastProgressAgeMs");
        AssertContains(source, "MjpegPreviewJitterLatencyP95Ms");
        AssertContains(source, "MjpegPreviewJitterDeadlineDropCount");
        AssertContains(source, "MjpegPreviewJitterClearedDropCount");
        AssertContains(source, "MjpegPreviewJitterResumeReprimeCount");
        AssertContains(source, "MjpegPreviewJitterLastDropReason");
        AssertContains(source, "JitD  | JitLat | JitDrop | JitUF | JitWhy");
        AssertContains(source, "FbState | Fb1%  | FbP99 | FbDec | FbCmd | FbFail | FbStage");
        AssertContains(source, "FormatFlashbackStageCell");
        AssertContains(source, "Cln | ExStat");
        AssertContains(source, "ExStat  | ExKind | Ex%");
        AssertContains(source, "FormatJitterDepthCell");
        AssertContains(source, "FormatExportFailureKind");
        AssertContains(source, "Jitter Depth:");
        AssertContains(source, "Jitter Latency:");
        AssertContains(source, "Jitter Drops:");
        AssertContains(source, "D3D Input P99:");
        AssertContains(source, "D3D Render P99:");
        AssertContains(source, "D3D Present P99:");
        AssertContains(source, "D3D Total P99:");
        AssertContains(source, "D3D P99 Bottleneck:");
        AssertContains(source, "PreviewPacingLikelySlowStage");
        AssertContains(source, "Preview Slow Stage:");
        AssertContains(source, "FormatD3DP99Bottleneck");
        AssertContains(source, "== Pressure Summary ==");
        AssertContains(source, "Preview Pressure:");
        AssertContains(source, "overBudgetSamples input=");
        AssertContains(source, "dxgiMissedSamples=");
        AssertContains(source, "jitterDropsDelta=");
        AssertContains(source, "Flashback Pressure:");
        AssertContains(source, "decodeOverBudget=");
        AssertContains(source, "pendingCmdSamples=");
        AssertContains(source, "System Pressure:");
        AssertContains(source, "gcPauseSamples=");
        AssertContains(source, "CountOverBudget");
        AssertContains(source, "NonNegativeDelta");
        AssertContains(source, "Flashback P99:");
        AssertContains(source, "Flashback Decode:");
        AssertContains(source, "phase={FormatOptional(last.FlashbackPlaybackMaxDecodePhase)}");
        AssertContains(source, "send={last.FlashbackPlaybackMaxDecodeSendMs:F1}ms");
        AssertContains(source, "audio={last.FlashbackPlaybackMaxDecodeAudioMs:F1}ms");
        AssertContains(source, "Flashback Cmds:");
        AssertContains(source, "maxLatencyCommand={FormatOptional(last.FlashbackPlaybackMaxCommandQueueLatencyCommand)}");
        AssertContains(source, "Flashback Cmd Counters:");
        AssertContains(source, "lastQueued={FormatOptional(last.FlashbackPlaybackLastCommandQueued)}");
        AssertContains(source, "lastProcessed={FormatOptional(last.FlashbackPlaybackLastCommandProcessed)}");
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
            AssertContains(diagnosticsHubSource, $"{propertyName} = snapshot.{propertyName}");
        }
        AssertContains(source, "Flashback Failure:");
        AssertContains(source, "Flashback Stages:");
        AssertContains(source, "failureUtc latest={last.FlashbackPlaybackLastCommandFailureUtcUnixMs}");
        AssertContains(source, "Cleanup State:");
        AssertContains(source, "forceRotateRequested={last.FlashbackForceRotateRequested}");
        AssertContains(source, "forceRotateDraining={last.FlashbackForceRotateDraining}");
        AssertContains(source, "kind={FormatOptional(last.FlashbackExportFailureKind)}");
        AssertContains(source, "Export Message:");
        AssertContains(source, "Export Progress:");
        AssertContains(source, "Export Range:");
        AssertContains(source, "FormatExportOutPoint");
        AssertContains(source, "Export Output:");

        return Task.CompletedTask;
    }
}
