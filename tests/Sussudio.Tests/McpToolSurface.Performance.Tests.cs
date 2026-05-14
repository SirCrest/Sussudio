using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static Task McpPerformanceTimelineTool_ExposesD3DP99StageTiming()
    {
        var source = ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.cs")
            + "\n" + ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.cs")
            + "\n" + ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Formatting.cs")
            + "\n" + ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Summaries.cs");
        AssertDoesNotContain(ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.cs"), "private sealed class TimelineRow");
        AssertContains(ReadRepoFile("tools/McpServer/Tools/PerformanceTimelineTools.Rows.cs"), "private sealed class TimelineRow");
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

    private static async Task McpPerformanceTimelineTool_RendersFlashbackCommandCounters()
    {
        var pipeName = NewMcpToolPipeName("timeline-counters");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var timelineTools = RequireMcpType("McpServer.Tools.PerformanceTimelineTools");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    var output = await InvokeMcpToolStringAsync(
                            timelineTools,
                            "get_performance_timeline",
                            pipeClient,
                            2,
                            118d)
                        .ConfigureAwait(false);

                    AssertContains(output, "Flashback Cmd Counters: enqueued 1 -> 9, processed 0 -> 8, dropped 0 -> 2, skippedNotReady 0 -> 1, scrubCoalesced 0 -> 4, seekCoalesced 0 -> 3, lastQueued=Seek, lastProcessed=Pause");
                    AssertContains(output, "cmdDropsDelta=2");
                    AssertContains(output, "Preview Slow Stage: Unknown/None -> CompositorMiss/High evidence=dxgiRecentMissed=4");
                },
                _ => """
                     {
                       "Success": true,
                       "Data": [
                         {
                           "TimestampUtc": "2026-05-04T12:00:00Z",
                           "PreviewPacingLikelySlowStage": "Unknown",
                           "PreviewPacingSlowStageConfidence": "None",
                           "PreviewPacingSlowStageEvidence": "",
                           "FlashbackPlaybackCommandsEnqueued": 1,
                           "FlashbackPlaybackCommandsProcessed": 0,
                           "FlashbackPlaybackCommandsDropped": 0,
                           "FlashbackPlaybackCommandsSkippedNotReady": 0,
                           "FlashbackPlaybackScrubUpdatesCoalesced": 0,
                           "FlashbackPlaybackSeekCommandsCoalesced": 0,
                           "FlashbackPlaybackLastCommandQueued": "Play",
                           "FlashbackPlaybackLastCommandProcessed": "None"
                         },
                         {
                           "TimestampUtc": "2026-05-04T12:00:01Z",
                           "PreviewPacingLikelySlowStage": "CompositorMiss",
                           "PreviewPacingSlowStageConfidence": "High",
                           "PreviewPacingSlowStageEvidence": "dxgiRecentMissed=4",
                           "FlashbackPlaybackPendingCommands": 2,
                           "FlashbackPlaybackCommandsEnqueued": 9,
                           "FlashbackPlaybackCommandsProcessed": 8,
                           "FlashbackPlaybackCommandsDropped": 2,
                           "FlashbackPlaybackCommandsSkippedNotReady": 1,
                           "FlashbackPlaybackScrubUpdatesCoalesced": 4,
                           "FlashbackPlaybackSeekCommandsCoalesced": 3,
                           "FlashbackPlaybackLastCommandQueued": "Seek",
                           "FlashbackPlaybackLastCommandProcessed": "Pause"
                         }
                       ]
                     }
                     """)
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "GetPerformanceTimeline", ("maxEntries", 2));
    }

    private static async Task McpFramePacingVerdictTool_FlagsHalfRatePreviewAndPlayback()
    {
        var pipeName = NewMcpToolPipeName("frame-pacing");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var verdictTools = RequireMcpType("McpServer.Tools.FramePacingVerdictTools");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 2,
                async () =>
                {
                    var output = await InvokeMcpToolStringAsync(
                            verdictTools,
                            "get_frame_pacing_verdict",
                            pipeClient,
                            240,
                            30d,
                            120d)
                        .ConfigureAwait(false);

                    AssertContains(output, "Verdict: HalfRatePreviewAndPlaybackSuspected");
                    AssertContains(output, "SampleQuality: Ready");
                    AssertContains(output, "SourceToPreviewRatio: 0.5");
                    AssertContains(output, "SourceToPlaybackRatio: 0.5");
                    AssertContains(output, "HalfRatePreviewSuspected: true");
                    AssertContains(output, "HalfRatePlaybackSuspected: true");
                    AssertContains(output, "VisualChangeFps: 60");
                    AssertContains(output, "MjpegUniqueFps: 60");
                    AssertContains(output, "PreviewDropDelta: 4");
                    AssertContains(output, "PlaybackDropDelta: 2");
                    AssertContains(output, "PreviewPacingLikelySlowStage: VisualDuplicateOrLowMotion");
                    AssertContains(output, "PreviewPacingSlowStageConfidence: Medium");
                    AssertContains(output, "PreviewPacingSlowStageEvidence: synthetic duplicate cadence");
                },
                i => i == 0
                    ? """
                      {
                        "Success": true,
                        "Snapshot": {
                          "ExpectedCaptureFrameRate": 120,
                          "CaptureCadenceObservedFps": 120,
                          "CaptureCadenceFivePercentLowFps": 120,
                          "CaptureCadenceOnePercentLowFps": 119,
                          "CaptureCadenceSampleCount": 3600,
                          "CaptureCadenceSampleDurationMs": 30000,
                          "PreviewCadenceObservedFps": 60,
                          "PreviewCadenceFivePercentLowFps": 60,
                          "PreviewCadenceOnePercentLowFps": 58,
                          "PreviewCadenceSampleCount": 1800,
                          "PreviewCadenceSampleDurationMs": 30000,
                          "PreviewCadenceRecentIntervalsMs": [16.67, 16.67, 16.67, 16.67, 16.67, 16.67],
                          "FlashbackPlaybackTargetFps": 120,
                          "FlashbackPlaybackObservedFps": 60,
                          "FlashbackPlaybackFivePercentLowFps": 60,
                          "FlashbackPlaybackOnePercentLowFps": 58,
                          "FlashbackPlaybackCadenceSampleCount": 1800,
                          "FlashbackPlaybackSampleDurationMs": 30000,
                          "FlashbackPlaybackRecentFrameIntervalsMs": [16.67, 16.67, 16.67, 16.67, 16.67, 16.67],
                          "VisualCadenceChangeObservedFps": 60,
                          "VisualCadenceRepeatFramePercent": 50,
                          "VisualCadenceMotionConfidence": "High",
                          "MjpegPacketHashInputObservedFps": 120,
                          "MjpegPacketHashUniqueObservedFps": 60,
                          "MjpegPacketHashDuplicateFramePercent": 50,
                          "PreviewPacingLikelySlowStage": "VisualDuplicateOrLowMotion",
                          "PreviewPacingSlowStageConfidence": "Medium",
                          "PreviewPacingSlowStageEvidence": "synthetic duplicate cadence"
                        }
                      }
                      """
                    : """
                      {
                        "Success": true,
                        "Data": [
                          {
                            "PreviewD3DFrameStatsRecentMissedRefreshCount": 2,
                            "MjpegPreviewJitterTotalDropped": 1,
                            "FlashbackPlaybackDroppedFrames": 0
                          },
                          {
                            "PreviewD3DFrameStatsRecentMissedRefreshCount": 4,
                            "MjpegPreviewJitterTotalDropped": 5,
                            "FlashbackPlaybackDroppedFrames": 2
                          }
                        ]
                      }
                      """)
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "GetSnapshot");
        AssertCommandRequest(requests[1], "GetPerformanceTimeline", ("maxEntries", 240));
    }

    private static async Task McpFramePacingVerdictTool_FlagsInsufficientSampleDuration()
    {
        var pipeName = NewMcpToolPipeName("frame-pacing-short");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var verdictTools = RequireMcpType("McpServer.Tools.FramePacingVerdictTools");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 2,
                async () =>
                {
                    var output = await InvokeMcpToolStringAsync(
                            verdictTools,
                            "get_frame_pacing_verdict",
                            pipeClient,
                            240,
                            30d,
                            120d)
                        .ConfigureAwait(false);

                    AssertContains(output, "Verdict: InsufficientSample");
                    AssertContains(output, "SampleQuality: Insufficient");
                    AssertContains(output, "ready=false");
                },
                i => i == 0
                    ? """
                      {
                        "Success": true,
                        "Snapshot": {
                          "ExpectedCaptureFrameRate": 120,
                          "CaptureCadenceObservedFps": 120,
                          "CaptureCadenceFivePercentLowFps": 120,
                          "CaptureCadenceOnePercentLowFps": 119,
                          "CaptureCadenceSampleCount": 240,
                          "CaptureCadenceSampleDurationMs": 2000,
                          "PreviewCadenceObservedFps": 120,
                          "PreviewCadenceFivePercentLowFps": 120,
                          "PreviewCadenceOnePercentLowFps": 119,
                          "PreviewCadenceSampleCount": 240,
                          "PreviewCadenceSampleDurationMs": 2000
                        }
                      }
                      """
                    : """
                      {
                        "Success": true,
                        "Data": []
                      }
                      """)
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "GetSnapshot");
        AssertCommandRequest(requests[1], "GetPerformanceTimeline", ("maxEntries", 240));
    }

}
