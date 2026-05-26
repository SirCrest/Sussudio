using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task McpPerformanceTimelineTool_ExposesD3DP99StageTiming()
    {
        var sources = ReadMcpPerformanceTimelineSources();

        AssertMcpPerformanceTimelineSourceOwnership(sources);
        AssertMcpPerformanceTimelineRenderingContracts(sources);
        AssertMcpPerformanceTimelineProjectionContracts(sources);

        return Task.CompletedTask;
    }

    private static McpPerformanceTimelineSources ReadMcpPerformanceTimelineSources()
    {
        var renderingSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTools.cs");

        return new McpPerformanceTimelineSources
        {
            RowsSource = renderingSource,
            RenderingSource = renderingSource,
            CombinedSource = renderingSource
        };
    }

    private sealed class McpPerformanceTimelineSources
    {
        public string RowsSource { get; init; } = string.Empty;
        public string RenderingSource { get; init; } = string.Empty;
        public string CombinedSource { get; init; } = string.Empty;
    }

    private static void AssertMcpPerformanceTimelineSourceOwnership(McpPerformanceTimelineSources sources)
    {
        AssertContains(sources.RowsSource, "PopulatePreviewTimelineRow(item, row);");
        AssertContains(sources.RowsSource, "PopulateFlashbackPlaybackTimelineRow(item, row);");
        AssertContains(sources.RowsSource, "PopulateFlashbackExportTimelineRow(item, row);");
        AssertContains(sources.RowsSource, "PopulateSystemTimelineRow(item, row);");
        AssertContains(sources.RowsSource, "private static void PopulatePreviewTimelineRow(JsonElement item, TimelineRow row)");
        AssertContains(sources.RowsSource, "private static void PopulateFlashbackPlaybackTimelineRow(JsonElement item, TimelineRow row)");
        AssertContains(sources.RowsSource, "private static void PopulateFlashbackExportTimelineRow(JsonElement item, TimelineRow row)");
        AssertContains(sources.RowsSource, "private static void PopulateSystemTimelineRow(JsonElement item, TimelineRow row)");
        AssertContains(sources.RowsSource, "MjpegPreviewJitterLatencyP95Ms");
        AssertContains(sources.RowsSource, "FlashbackPlaybackMaxCommandQueueLatencyCommand");
        AssertContains(sources.RowsSource, "FlashbackExportThroughputBytesPerSec");
        AssertContains(sources.RowsSource, "ThreadPoolIoAvailable");
        AssertOccursBefore(sources.RowsSource, "private static void PopulatePreviewTimelineRow", "private static void PopulateFlashbackPlaybackTimelineRow");
        AssertOccursBefore(sources.RowsSource, "private static void PopulateFlashbackPlaybackTimelineRow", "private static void PopulateFlashbackExportTimelineRow");
        AssertOccursBefore(sources.RowsSource, "private static void PopulateFlashbackExportTimelineRow", "private static void PopulateSystemTimelineRow");
        AssertContains(sources.RowsSource, "private sealed class TimelineRow");
        AssertContains(sources.RowsSource, "public double PreviewFivePercentLowFps { get; set; }");
        AssertContains(sources.RowsSource, "public string PreviewPacingSlowStageEvidence { get; set; } = string.Empty;");
        AssertContains(sources.RowsSource, "public string FlashbackPlaybackLastCommandFailure { get; set; } = string.Empty;");
        AssertContains(sources.RowsSource, "public double FlashbackExportThroughputBytesPerSec { get; set; }");
        AssertContains(sources.RowsSource, "public int IoThreads { get; set; }");
        AssertOccursBefore(sources.RowsSource, "private static void PopulateSystemTimelineRow", "private sealed class TimelineRow");
        AssertOccursBefore(sources.RowsSource, "private static List<TimelineRow> ReadTimelineRows", "private static string BuildPerformanceTimelineText");
        AssertOccursBefore(sources.RowsSource, "public double PreviewSlowPct { get; set; }", "public double VisualCadenceChangeObservedFps { get; set; }");
        AssertOccursBefore(sources.RowsSource, "public string PreviewPacingSlowStageEvidence { get; set; } = string.Empty;", "public string FlashbackPlaybackState { get; set; } = string.Empty;");
        AssertOccursBefore(sources.RowsSource, "public bool FlashbackForceRotateDraining { get; set; }", "public bool FlashbackExportActive { get; set; }");
        AssertOccursBefore(sources.RowsSource, "public string FlashbackExportMessage { get; set; } = string.Empty;", "public long LatencyMs { get; set; }");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "PerformanceTimelineTools.Rows.cs")),
            "MCP performance timeline row projection lives with the timeline renderer owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "PerformanceTimelineTools.Rendering.cs")),
            "MCP timeline rendering lives with the broader performance MCP tool owner");
        AssertContains(sources.RenderingSource, "public static async Task<CallToolResult> get_performance_timeline(");
        AssertContains(sources.RenderingSource, "var entries = ReadTimelineRows(data);");
        AssertContains(sources.RenderingSource, "McpToolResultFactory.FromResponse(response, BuildPerformanceTimelineText(entries, targetOnePercentLowFps))");
        AssertContains(sources.RenderingSource, "BuildPerformanceTimelineText");
        AssertContains(sources.RenderingSource, "AppendTrendSummary");
        AssertContains(sources.RenderingSource, "== Trend Summary");
        AssertContains(sources.RenderingSource, "FormatOptional");
        AssertContains(sources.RenderingSource, "CompactCell");
        AssertContains(sources.RenderingSource, "private static string FormatJitterDepthCell(TimelineRow row)");
        AssertContains(sources.RenderingSource, "private static string FormatD3DP99Bottleneck(TimelineRow row)");
        AssertContains(sources.RenderingSource, "private static string FormatFlashbackStageCell(TimelineRow row)");
        AssertContains(sources.RenderingSource, "private static string FormatExportFailureKind(string failureKind)");
        AssertContains(sources.RenderingSource, "private static string FormatBytesPerSecond(double bytesPerSecond)");
        AssertContains(sources.RenderingSource, "AppendPreviewTrendSummary(builder, first, last);");
        AssertContains(sources.RenderingSource, "AppendFlashbackTrendSummary(builder, first, last);");
        AssertContains(sources.RenderingSource, "private static void AppendPreviewTrendSummary(StringBuilder builder, TimelineRow first, TimelineRow last)");
        AssertContains(sources.RenderingSource, "Preview Slow Stage:");
        AssertContains(sources.RenderingSource, "D3D P99 Bottleneck:");
        AssertContains(sources.RenderingSource, "Jitter Drops:");
        AssertContains(sources.RenderingSource, "private static void AppendFlashbackTrendSummary(StringBuilder builder, TimelineRow first, TimelineRow last)");
        AssertContains(sources.RenderingSource, "Flashback Cmd Counters:");
        AssertContains(sources.RenderingSource, "AppendFlashbackExportTrendSummary(builder, first, last);");
        AssertContains(sources.RenderingSource, "private static void AppendFlashbackExportTrendSummary(StringBuilder builder, TimelineRow first, TimelineRow last)");
        AssertContains(sources.RenderingSource, "Export Output:");
        AssertOccursBefore(sources.RenderingSource, "Cleanup State:", "AppendFlashbackExportTrendSummary(builder, first, last);");
        AssertOccursBefore(sources.RenderingSource, "AppendFlashbackExportTrendSummary(builder, first, last);", "private static void AppendFlashbackExportTrendSummary");
        AssertContains(sources.RenderingSource, "AppendOnePercentLowTargetSummary");
        AssertContains(sources.RenderingSource, "private static void AppendPressureSummary(");
        AssertContains(sources.RenderingSource, "== Pressure Summary ==");
        AssertContains(sources.RenderingSource, "private static int CountOverBudget(");
    }

    private static void AssertMcpPerformanceTimelineRenderingContracts(McpPerformanceTimelineSources sources)
    {
        AssertContains(sources.CombinedSource, "PreviewD3DInputUploadCpuP99Ms");
        AssertContains(sources.CombinedSource, "targetOnePercentLowFps");
        AssertContains(sources.CombinedSource, "== 1% Low Target Summary");
        AssertContains(sources.CombinedSource, "AppendOnePercentLowTargetSummary");
        AssertContains(sources.CombinedSource, "AppendPressureSummary");
        AssertContains(sources.CombinedSource, "misses={belowTarget}/{valid.Length}");
        AssertContains(sources.CombinedSource, "Preview 5% Low:");
        AssertContains(sources.CombinedSource, "Visual Cadence:");
        AssertContains(sources.CombinedSource, "MJPEG Fingerprint:");
        AssertContains(sources.CombinedSource, "Preview P99:");
        AssertContains(sources.CombinedSource, "InP99 | RsP99 | PrP99 | TotP99");
        AssertContains(sources.CombinedSource, "Flashback target:");
        AssertContains(sources.CombinedSource, "lastSubmitFailure");
        AssertContains(sources.CombinedSource, "Flashback Enqueue Rejects");
        AssertContains(sources.CombinedSource, "JitD  | JitLat | JitDrop | JitUF | JitWhy");
        AssertContains(sources.CombinedSource, "FbState | Fb1%  | FbP99 | FbDec | FbCmd | FbFail | FbStage");
        AssertContains(sources.CombinedSource, "FormatFlashbackStageCell");
        AssertContains(sources.CombinedSource, "Cln | ExStat");
        AssertContains(sources.CombinedSource, "ExStat  | ExKind | Ex%");
        AssertContains(sources.CombinedSource, "FormatJitterDepthCell");
        AssertContains(sources.CombinedSource, "FormatExportFailureKind");
        AssertContains(sources.CombinedSource, "Jitter Depth:");
        AssertContains(sources.CombinedSource, "Jitter Latency:");
        AssertContains(sources.CombinedSource, "Jitter Drops:");
        AssertContains(sources.CombinedSource, "D3D Input P99:");
        AssertContains(sources.CombinedSource, "D3D Render P99:");
        AssertContains(sources.CombinedSource, "D3D Present P99:");
        AssertContains(sources.CombinedSource, "D3D Total P99:");
        AssertContains(sources.CombinedSource, "D3D P99 Bottleneck:");
        AssertContains(sources.CombinedSource, "Preview Slow Stage:");
        AssertContains(sources.CombinedSource, "FormatD3DP99Bottleneck");
        AssertContains(sources.CombinedSource, "== Pressure Summary ==");
        AssertContains(sources.CombinedSource, "Preview Pressure:");
        AssertContains(sources.CombinedSource, "overBudgetSamples input=");
        AssertContains(sources.CombinedSource, "dxgiMissedSamples=");
        AssertContains(sources.CombinedSource, "jitterDropsDelta=");
        AssertContains(sources.CombinedSource, "Flashback Pressure:");
        AssertContains(sources.CombinedSource, "decodeOverBudget=");
        AssertContains(sources.CombinedSource, "pendingCmdSamples=");
        AssertContains(sources.CombinedSource, "System Pressure:");
        AssertContains(sources.CombinedSource, "gcPauseSamples=");
        AssertContains(sources.CombinedSource, "CountOverBudget");
        AssertContains(sources.CombinedSource, "NonNegativeDelta");
        AssertContains(sources.CombinedSource, "Flashback P99:");
        AssertContains(sources.CombinedSource, "Flashback Decode:");
        AssertContains(sources.CombinedSource, "phase={FormatOptional(last.FlashbackPlaybackMaxDecodePhase)}");
        AssertContains(sources.CombinedSource, "send={last.FlashbackPlaybackMaxDecodeSendMs:F1}ms");
        AssertContains(sources.CombinedSource, "audio={last.FlashbackPlaybackMaxDecodeAudioMs:F1}ms");
        AssertContains(sources.CombinedSource, "Flashback Cmds:");
        AssertContains(sources.CombinedSource, "maxLatencyCommand={FormatOptional(last.FlashbackPlaybackMaxCommandQueueLatencyCommand)}");
        AssertContains(sources.CombinedSource, "Flashback Cmd Counters:");
        AssertContains(sources.CombinedSource, "lastQueued={FormatOptional(last.FlashbackPlaybackLastCommandQueued)}");
        AssertContains(sources.CombinedSource, "lastProcessed={FormatOptional(last.FlashbackPlaybackLastCommandProcessed)}");
        AssertContains(sources.CombinedSource, "Flashback Failure:");
        AssertContains(sources.CombinedSource, "Flashback Stages:");
        AssertContains(sources.CombinedSource, "failureUtc latest={last.FlashbackPlaybackLastCommandFailureUtcUnixMs}");
        AssertContains(sources.CombinedSource, "Cleanup State:");
        AssertContains(sources.CombinedSource, "forceRotateRequested={last.FlashbackForceRotateRequested}");
        AssertContains(sources.CombinedSource, "forceRotateDraining={last.FlashbackForceRotateDraining}");
        AssertContains(sources.CombinedSource, "kind={FormatOptional(last.FlashbackExportFailureKind)}");
        AssertContains(sources.CombinedSource, "Export Message:");
        AssertContains(sources.CombinedSource, "Export Progress:");
        AssertContains(sources.CombinedSource, "Export Range:");
        AssertContains(sources.CombinedSource, "FormatExportOutPoint");
        AssertContains(sources.CombinedSource, "Export Output:");
    }

    private static void AssertMcpPerformanceTimelineProjectionContracts(McpPerformanceTimelineSources sources)
    {
        var diagnosticsHubSource = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Timeline.cs");
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
                     "FlashbackPlaybackLastCommandProcessed",
                     "FlashbackExportPercent",
                     "FlashbackExportThroughputBytesPerSec",
                     "ProcessCpuPercent",
                     "ThreadPoolWorkerAvailable"
                 })
        {
            AssertNotNull(entryType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance), $"PerformanceTimelineEntry.{propertyName}");
            if (propertyName.StartsWith("FlashbackPlayback", StringComparison.Ordinal))
            {
                var projectionName = propertyName["FlashbackPlayback".Length..];
                AssertContains(diagnosticsHubSource, $"{propertyName} = flashbackPlayback.{projectionName}");
                AssertContains(diagnosticsHubSource, $"{projectionName}: snapshot.{propertyName}");
            }
            else if (propertyName.StartsWith("FlashbackExport", StringComparison.Ordinal))
            {
                var projectionName = propertyName["FlashbackExport".Length..];
                AssertContains(diagnosticsHubSource, $"{propertyName} = flashbackExport.{projectionName}");
                AssertContains(diagnosticsHubSource, $"{projectionName}: snapshot.{propertyName}");
            }
            else if (propertyName is "ProcessCpuPercent" or "ThreadPoolWorkerAvailable")
            {
                AssertContains(diagnosticsHubSource, $"{propertyName} = system.{propertyName}");
                AssertContains(diagnosticsHubSource, $"{propertyName}: snapshot.{propertyName}");
            }
            else if (propertyName.StartsWith("PreviewCadence", StringComparison.Ordinal))
            {
                var projectionName = propertyName["Preview".Length..];
                AssertContains(diagnosticsHubSource, $"{propertyName} = preview.{projectionName}");
                AssertContains(diagnosticsHubSource, $"{projectionName}: snapshot.{propertyName.Replace("Ms", "IntervalMs", StringComparison.Ordinal)}");
            }
            else if (propertyName.StartsWith("CaptureCadence", StringComparison.Ordinal))
            {
                AssertContains(diagnosticsHubSource, $"{propertyName} = core.{propertyName}");
                AssertContains(diagnosticsHubSource, $"{propertyName}: snapshot.{propertyName}");
            }
            else if (propertyName.StartsWith("PreviewPacing", StringComparison.Ordinal))
            {
                var projectionName = propertyName["Preview".Length..];
                AssertContains(diagnosticsHubSource, $"{propertyName} = preview.{projectionName}");
                AssertContains(diagnosticsHubSource, $"{projectionName}: snapshot.{propertyName}");
            }
            else if (propertyName.StartsWith("VisualCadence", StringComparison.Ordinal) ||
                     propertyName.StartsWith("MjpegPacketHash", StringComparison.Ordinal))
            {
                AssertContains(diagnosticsHubSource, $"{propertyName} = preview.{propertyName}");
                AssertContains(diagnosticsHubSource, $"{propertyName}: snapshot.{propertyName}");
            }
            else
            {
                AssertContains(diagnosticsHubSource, $"{propertyName} = snapshot.{propertyName}");
            }
        }
    }

    internal static async Task McpPerformanceTimelineTool_RendersFlashbackCommandCounters()
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


    internal static async Task McpPresentMonTools_RouteSnapshotCorrelation()
    {
        var presentMonTools = RequireMcpType("McpServer.Tools.PresentMonTools");

        var pipeName = NewMcpToolPipeName("presentmon-text");
        var pipeClient = CreateMcpPipeClient(pipeName);
        string textResult = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    textResult = await InvokeMcpToolStringAsync(
                            presentMonTools,
                            "capture_presentmon",
                            pipeClient,
                            5,
                            -1,
                            "NoSuchSussudioProcess",
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            false,
                            true)
                        .ConfigureAwait(false);
                },
                _ => PresentMonSnapshotJson("0xABCDEF", 42, 0, 1700000000000))
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "GetSnapshot");
        AssertEqual("No running process matched pid=-1 name='NoSuchSussudioProcess'.", textResult, "capture_presentmon no-process text");

        var rawPipeName = NewMcpToolPipeName("presentmon-raw");
        var rawPipeClient = CreateMcpPipeClient(rawPipeName);
        object? rawResult = null;
        var rawRequests = await CapturePipeRequestsAsync(
                rawPipeName,
                expectedCount: 1,
                async () =>
                {
                    rawResult = await InvokeMcpToolResultAsync(
                            presentMonTools,
                            "capture_presentmon_raw",
                            rawPipeClient,
                            15,
                            -1,
                            "AnotherMissingProcess",
                            "0xEXPLICIT",
                            99L,
                            1001L,
                            1700000000999L,
                            @"C:\tools\missing-presentmon.exe",
                            @"C:\captures\presentmon.csv",
                            true,
                            false)
                        .ConfigureAwait(false);
                },
                _ => PresentMonSnapshotJson("0xSHOULD_NOT_WIN", 12, 34, 1700000000123))
            .ConfigureAwait(false);

        AssertCommandRequest(rawRequests[0], "GetSnapshot");
        AssertEqual(false, GetBoolProperty(rawResult!, "Success"), "capture_presentmon_raw missing process success");
        AssertEqual("No running process matched pid=-1 name='AnotherMissingProcess'.", GetStringProperty(rawResult!, "Message"), "capture_presentmon_raw no-process message");

        AssertPresentMonOptionsFallbackAndPrecedence();

        var rootText = ReadRepoFile("tools/McpServer/Tools/PerformanceTools.cs")
            .Replace("\r\n", "\n");
        var probeText = ReadRepoFile("tools/Common/PresentMon/PresentMonProbe.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "[McpServerToolType]");
        AssertContains(rootText, "public static class PresentMonTools");
        AssertDoesNotContain(rootText, "public static partial class PresentMonTools");
        AssertContains(rootText, "public static async Task<CallToolResult> capture_presentmon");
        AssertContains(rootText, "public static async Task<object> capture_presentmon_raw");
        AssertContains(rootText, "[McpServerTool(UseStructuredContent = true)");
        AssertContains(rootText, "PresentMonProbe.Format(result)");
        AssertContains(rootText, "PresentMonProbe.RunAsync(PresentMonProbe.CreateOptions(");
        AssertContains(rootText, "correlation: resolved");
        AssertContains(rootText, "private static async Task<PresentMonProbeCorrelation> TryResolvePreviewPresentCorrelationAsync(");
        AssertContains(rootText, "SendCommandAsync(AutomationCommandKind.GetSnapshot)");
        AssertContains(rootText, "return PresentMonProbe.ReadPreviewCorrelation(snapshot);");
        AssertContains(rootText, "catch (JsonException ex)");
        AssertContains(rootText, "catch (IOException ex)");
        AssertDoesNotContain(rootText, "new PresentMonProbeOptions");
        AssertDoesNotContain(rootText, "ExpectedSwapChainAddress =");
        AssertDoesNotContain(rootText, "AppPresentId = appPresentId");
        AssertDoesNotContain(rootText, "SendCommandAsync(\"GetSnapshot\")");
        AssertDoesNotContain(rootText, "GetPositiveLong(");
        AssertDoesNotContain(rootText, "private readonly record struct PresentMonCorrelation(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "PresentMonTools.Correlation.cs")),
            "PresentMon snapshot correlation lives with the PresentMon MCP tool");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "PresentMonTools.cs")),
            "PresentMon MCP entry points live with the broader performance MCP tool owner");

        AssertContains(probeText, "public readonly record struct PresentMonProbeCorrelation(");
        AssertContains(probeText, "public static PresentMonProbeOptions CreateOptions(");
        AssertContains(probeText, "public static PresentMonProbeCorrelation ReadPreviewCorrelation(JsonElement snapshot)");
    }

    private static void AssertPresentMonOptionsFallbackAndPrecedence()
    {
        var presentMonProbe = RequireMcpType("Sussudio.Tools.PresentMonProbe");
        var createOptions = presentMonProbe.GetMethod("CreateOptions", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PresentMonProbe.CreateOptions was not found.");
        var correlationType = RequireMcpType("Sussudio.Tools.PresentMonProbeCorrelation");
        var resolved = Activator.CreateInstance(
                correlationType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { "0xSNAPSHOT", 42L, 0L, 1700000000000L },
                culture: null)
            ?? throw new InvalidOperationException("PresentMonCorrelation could not be created.");

        var fallbackOptions = createOptions.Invoke(null, new object?[]
        {
            5,
            123,
            "Sussudio",
            null,
            null,
            null,
            null,
            null,
            @"C:\tools\PresentMon.exe",
            @"C:\captures\presentmon.csv",
            true,
            false,
            resolved
        }) ?? throw new InvalidOperationException("CreatePresentMonProbeOptions returned null.");

        AssertEqual(5, GetIntProperty(fallbackOptions, "DurationSeconds"), "PresentMon fallback DurationSeconds");
        AssertEqual(123, GetIntProperty(fallbackOptions, "ProcessId"), "PresentMon fallback ProcessId");
        AssertEqual("Sussudio", GetStringProperty(fallbackOptions, "ProcessName"), "PresentMon fallback ProcessName");
        AssertEqual("0xSNAPSHOT", GetStringProperty(fallbackOptions, "ExpectedSwapChainAddress"), "PresentMon fallback swap chain");
        AssertEqual(42L, GetLongProperty(fallbackOptions, "AppPresentId"), "PresentMon fallback present id");
        AssertEqual(0L, GetLongProperty(fallbackOptions, "AppSourceSequenceNumber"), "PresentMon fallback source sequence");
        AssertEqual(1700000000000L, GetLongProperty(fallbackOptions, "AppPresentUtcUnixMs"), "PresentMon fallback present UTC");
        AssertEqual(@"C:\tools\PresentMon.exe", GetStringProperty(fallbackOptions, "PresentMonPath"), "PresentMon fallback path");
        AssertEqual(@"C:\captures\presentmon.csv", GetStringProperty(fallbackOptions, "OutputFile"), "PresentMon fallback output");
        AssertEqual(true, GetBoolProperty(fallbackOptions, "KeepCsv"), "PresentMon fallback keep CSV");
        AssertEqual(false, GetBoolProperty(fallbackOptions, "TrackGpuVideo"), "PresentMon fallback track GPU video");

        var explicitOptions = createOptions.Invoke(null, new object?[]
        {
            15,
            -1,
            "OtherProcess",
            "0xEXPLICIT",
            99L,
            1001L,
            1700000000999L,
            null,
            null,
            null,
            false,
            true,
            resolved
        }) ?? throw new InvalidOperationException("CreatePresentMonProbeOptions returned null for explicit args.");

        AssertEqual(15, GetIntProperty(explicitOptions, "DurationSeconds"), "PresentMon explicit DurationSeconds");
        AssertEqual(-1, GetIntProperty(explicitOptions, "ProcessId"), "PresentMon explicit ProcessId");
        AssertEqual("OtherProcess", GetStringProperty(explicitOptions, "ProcessName"), "PresentMon explicit ProcessName");
        AssertEqual("0xEXPLICIT", GetStringProperty(explicitOptions, "ExpectedSwapChainAddress"), "PresentMon explicit swap chain");
        AssertEqual(99L, GetLongProperty(explicitOptions, "AppPresentId"), "PresentMon explicit present id");
        AssertEqual(1001L, GetLongProperty(explicitOptions, "AppSourceSequenceNumber"), "PresentMon explicit source sequence");
        AssertEqual(1700000000999L, GetLongProperty(explicitOptions, "AppPresentUtcUnixMs"), "PresentMon explicit present UTC");
        AssertEqual(string.Empty, GetStringProperty(explicitOptions, "PresentMonPath"), "PresentMon explicit null path");
        AssertEqual(string.Empty, GetStringProperty(explicitOptions, "OutputFile"), "PresentMon explicit null output");
        AssertEqual(false, GetBoolProperty(explicitOptions, "KeepCsv"), "PresentMon explicit keep CSV");
        AssertEqual(true, GetBoolProperty(explicitOptions, "TrackGpuVideo"), "PresentMon explicit track GPU video");
    }

    private static string PresentMonSnapshotJson(
        string swapChainAddress,
        long presentId,
        long sourceSequenceNumber,
        long presentUtcUnixMs)
    {
        return $$"""
                 {
                   "Success": true,
                   "Snapshot": {
                     "PreviewD3DSwapChainAddress": "{{swapChainAddress}}",
                     "PreviewD3DLastRenderedPreviewPresentId": {{presentId}},
                     "PreviewD3DLastRenderedSourceSequenceNumber": {{sourceSequenceNumber}},
                     "PreviewD3DLastRenderedUtcUnixMs": {{presentUtcUnixMs}}
                   }
                 }
                 """;
    }
}
