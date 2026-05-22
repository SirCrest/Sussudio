using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionFlashbackMetrics_OwnsFlashbackSessionMetricProjection()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var metricsText = ReadDiagnosticSessionFlashbackMetricsSource();
        var recordingText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.Recording.cs")
            .Replace("\r\n", "\n");
        var playbackSessionText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackSession.cs")
            .Replace("\r\n", "\n");
        var playbackObservationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.cs")
            .Replace("\r\n", "\n");
        var playbackResultText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.cs")
            .Replace("\r\n", "\n");
        var exportText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.Export.cs")
            .Replace("\r\n", "\n");

        AssertContains(metricsText, "internal static partial class DiagnosticSessionFlashbackMetrics");
        AssertContains(recordingText, "internal sealed class FlashbackRecordingSessionMetrics");
        AssertContains(playbackSessionText, "internal sealed class FlashbackPlaybackSessionMetrics");
        AssertContains(playbackResultText, "internal sealed class FlashbackPlaybackResultMetrics");
        AssertContains(exportText, "internal sealed class FlashbackExportSessionMetrics");
        AssertContains(playbackSessionText, "public JsonElement BaselineSnapshot { get; init; }");
        AssertContains(playbackSessionText, "public int MaxCommandQueueLatencyMsObserved { get; set; }");
        AssertContains(playbackSessionText, "public double MaxSlowFramePercentObserved { get; set; }");
        AssertContains(playbackSessionText, "public long MinOnePercentLowAudioMasterFallbacks { get; set; }");
        AssertContains(playbackSessionText, "public string MaxDecodePhaseObserved { get; set; } = string.Empty;");
        AssertContains(playbackSessionText, "public double MaxAbsAvDriftMsObserved { get; set; }");
        AssertContains(playbackSessionText, "public long SubmitFailuresDelta { get; set; }");
        AssertContains(playbackResultText, "public JsonElement EndSnapshot { get; init; }");
        AssertContains(playbackResultText, "public int PendingCommandsAtEnd { get; init; }");
        AssertContains(playbackResultText, "public double OnePercentLowFpsAtEnd { get; init; }");
        AssertContains(playbackResultText, "public string MaxDecodePhaseAtEnd { get; init; } = string.Empty;");
        AssertContains(playbackResultText, "public long AudioMasterFallbacksAtEnd { get; init; }");
        AssertContains(playbackResultText, "public long SeekForwardDecodeCapHitsDelta { get; init; }");
        AssertContains(exportText, "public long ForceRotateFallbacksAtEnd { get; set; }");
        AssertDoesNotContain(recordingText, "internal sealed class FlashbackPlaybackSessionMetrics");
        AssertDoesNotContain(playbackSessionText, "internal sealed class FlashbackPlaybackResultMetrics");
        AssertDoesNotContain(playbackResultText, "internal sealed class FlashbackExportSessionMetrics");
        AssertContains(metricsText, "internal static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(");
        AssertContains(playbackSessionText, "internal static FlashbackPlaybackSessionMetrics BuildFlashbackPlaybackSessionMetrics(");
        AssertContains(playbackObservationText, "private static void ObservePlaybackSnapshot(");
        AssertContains(playbackObservationText, "var relevance = BuildPlaybackSnapshotRelevance(");
        AssertContains(playbackObservationText, "private readonly record struct FlashbackPlaybackSnapshotRelevance(");
        AssertContains(playbackObservationText, "private static FlashbackPlaybackSnapshotRelevance BuildPlaybackSnapshotRelevance(");
        AssertContains(playbackObservationText, "private static bool IsPlaybackSnapshotActive(");
        AssertContains(playbackObservationText, "GetInt(snapshot, \"FlashbackPlaybackPendingCommands\") > 0");
        AssertContains(playbackObservationText, "ObservePlaybackOnePercentLow(");
        AssertContains(playbackObservationText, "ObservePlaybackFrameAndDecodeMetrics(metrics, snapshot);");
        AssertContains(playbackObservationText, "ObservePlaybackAudioMasterMetrics(metrics, snapshot);");
        AssertContains(playbackObservationText, "private static void ObservePlaybackOnePercentLow(");
        AssertContains(playbackObservationText, "metrics.OnePercentLowSampleWindowObserved = true;");
        AssertContains(playbackObservationText, "private static void ObservePlaybackFrameAndDecodeMetrics(");
        AssertContains(playbackObservationText, "metrics.MaxDecodePhaseObserved = GetString(snapshot, \"FlashbackPlaybackMaxDecodePhase\") ?? string.Empty;");
        AssertContains(playbackObservationText, "private static void ObservePlaybackAudioMasterMetrics(");
        AssertContains(playbackObservationText, "GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, \"FlashbackPlaybackAudioMasterFallbacks\")");
        AssertContains(playbackResultText, "internal static FlashbackPlaybackResultMetrics BuildFlashbackPlaybackResultMetrics(");
        AssertContains(playbackResultText, "var commands = BuildFlashbackPlaybackResultCommandMetrics(observed, endSnapshot, metrics);");
        AssertContains(playbackResultText, "PendingCommandsAtEnd = commands.PendingCommandsAtEnd,");
        AssertContains(playbackResultText, "private static long GetObservedLong(bool observed, JsonElement snapshot, string propertyName)");
        AssertContains(playbackResultText, "private static double GetObservedDouble(bool observed, JsonElement snapshot, string propertyName)");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultCommandMetrics BuildFlashbackPlaybackResultCommandMetrics(");
        AssertContains(playbackResultText, "PendingCommandsAtEnd: observed ? GetInt(endSnapshot, \"FlashbackPlaybackPendingCommands\") : 0");
        AssertContains(playbackResultText, "LastCommandFailureAtEnd: observed ? GetString(endSnapshot, \"FlashbackPlaybackLastCommandFailure\") ?? string.Empty : string.Empty");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultCadenceMetrics BuildFlashbackPlaybackResultCadenceMetrics(");
        AssertContains(playbackResultText, "DroppedFramesAtEnd: GetObservedLong(observed, endSnapshot, \"FlashbackPlaybackDroppedFrames\")");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultDecodeMetrics BuildFlashbackPlaybackResultDecodeMetrics(");
        AssertContains(playbackResultText, "MaxDecodePhaseAtEnd: observed ? GetString(endSnapshot, \"FlashbackPlaybackMaxDecodePhase\") ?? string.Empty : string.Empty");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultAudioMasterMetrics BuildFlashbackPlaybackResultAudioMasterMetrics(");
        AssertContains(playbackResultText, "AudioMasterFallbacksAtEnd: GetObservedLong(observed, endSnapshot, \"FlashbackPlaybackAudioMasterFallbacks\")");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultStageMetrics BuildFlashbackPlaybackResultStageMetrics(");
        AssertContains(playbackResultText, "GetCounterDelta(endSnapshot, metrics.BaselineSnapshot, \"FlashbackPlaybackSeekForwardDecodeCapHits\")");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultCommandMetrics(");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultCadenceMetrics(");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultDecodeMetrics(");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultAudioMasterMetrics(");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultStageMetrics(");
        AssertContains(metricsText, "internal static FlashbackExportSessionMetrics BuildFlashbackExportSessionMetrics(");
        AssertContains(metricsText, "metrics.ForceRotateFallbacksAtEnd = GetNullableLong(lastSnapshot, \"FlashbackExportForceRotateFallbacks\") ?? 0;");
        AssertContains(metricsText, "metrics.ForceRotateFallbacksDelta = GetCounterDelta(");
        AssertContains(metricsText, "metrics.LastForceRotateFallbackSegmentsAtEnd =");
        AssertContains(exportText, "private static void ObserveExportSnapshot(");
        AssertContains(exportText, "var relevantToSession =");
        AssertContains(exportText, "metrics.MaxThroughputBytesPerSecObserved = Math.Max(");
        AssertDoesNotContain(playbackSessionText, "private static void ObservePlaybackOnePercentLow(");
        AssertDoesNotContain(playbackSessionText, "private static void ObservePlaybackFrameAndDecodeMetrics(");
        AssertDoesNotContain(playbackSessionText, "private static void ObservePlaybackAudioMasterMetrics(");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;");
        AssertContains(builderText, "var playbackResultMetrics = BuildFlashbackPlaybackResultMetrics(playbackSessionMetrics);");
        AssertDoesNotContain(runnerText, "private sealed class FlashbackPlaybackSessionMetrics");
        AssertDoesNotContain(runnerText, "GetString(playbackEndSnapshot,");
        AssertDoesNotContain(runnerText, "private sealed class FlashbackExportSessionMetrics");
        AssertDoesNotContain(runnerText, "private static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(");
        AssertDoesNotContain(runnerText, "private static bool IsPlaybackSnapshotActive(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackMetrics_ExportForceRotateCountersIgnoreRelevanceGate()
    {
        var assembly = LoadToolAssemblyIsolated(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var metricsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionFlashbackMetrics")
            ?? throw new InvalidOperationException("Sussudio.Tools.DiagnosticSessionFlashbackMetrics was not found.");
        var sampleType = assembly.GetType("Sussudio.Tools.DiagnosticSessionSample")
            ?? throw new InvalidOperationException("Sussudio.Tools.DiagnosticSessionSample was not found.");
        var buildMetrics = metricsType.GetMethod(
            "BuildFlashbackExportSessionMetrics",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildFlashbackExportSessionMetrics was not found.");

        using var initialDocument = JsonDocument.Parse(
            """
            {
              "FlashbackExportId": 0,
              "FlashbackExportActive": false,
              "FlashbackExportStatus": "NotStarted",
              "FlashbackExportForceRotateFallbacks": 1,
              "FlashbackExportLastForceRotateFallbackSegments": 0
            }
            """);
        using var lastDocument = JsonDocument.Parse(
            """
            {
              "FlashbackExportId": 0,
              "FlashbackExportActive": false,
              "FlashbackExportStatus": "NotStarted",
              "FlashbackExportForceRotateFallbacks": 3,
              "FlashbackExportLastForceRotateFallbackSegments": 2
            }
            """);

        var samples = Array.CreateInstance(sampleType, 0);
        var metrics = buildMetrics.Invoke(
            null,
            new object?[] { initialDocument.RootElement, samples, lastDocument.RootElement })
            ?? throw new InvalidOperationException("BuildFlashbackExportSessionMetrics returned null.");

        AssertEqual(false, (bool)GetPropertyValue(metrics, "Observed")!, "Non-relevant export remains unobserved");
        AssertEqual(3L, Convert.ToInt64(GetPropertyValue(metrics, "ForceRotateFallbacksAtEnd")), "ForceRotateFallbacksAtEnd");
        AssertEqual(2L, Convert.ToInt64(GetPropertyValue(metrics, "ForceRotateFallbacksDelta")), "ForceRotateFallbacksDelta");
        AssertEqual(2, Convert.ToInt32(GetPropertyValue(metrics, "LastForceRotateFallbackSegmentsAtEnd")), "LastForceRotateFallbackSegmentsAtEnd");

        return Task.CompletedTask;
    }
}
