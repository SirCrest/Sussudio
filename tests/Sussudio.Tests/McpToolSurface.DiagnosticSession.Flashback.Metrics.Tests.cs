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
        var playbackObservationOnePercentLowText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.OnePercentLow.cs")
            .Replace("\r\n", "\n");
        var playbackObservationFrameDecodeText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.FrameDecode.cs")
            .Replace("\r\n", "\n");
        var playbackObservationAudioMasterText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.AudioMaster.cs")
            .Replace("\r\n", "\n");
        var playbackResultModelText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.Model.cs")
            .Replace("\r\n", "\n");
        var playbackResultText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.cs")
            .Replace("\r\n", "\n");
        var playbackResultObservedReadsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.ObservedReads.cs")
            .Replace("\r\n", "\n");
        var playbackResultCommandsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.Commands.cs")
            .Replace("\r\n", "\n");
        var playbackResultCadenceText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.Cadence.cs")
            .Replace("\r\n", "\n");
        var playbackResultDecodeText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.Decode.cs")
            .Replace("\r\n", "\n");
        var playbackResultAudioMasterText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.AudioMaster.cs")
            .Replace("\r\n", "\n");
        var playbackResultStagesText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.Stages.cs")
            .Replace("\r\n", "\n");
        var exportText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.Export.cs")
            .Replace("\r\n", "\n");

        AssertContains(metricsText, "internal static partial class DiagnosticSessionFlashbackMetrics");
        AssertContains(recordingText, "internal sealed class FlashbackRecordingSessionMetrics");
        AssertContains(playbackSessionText, "internal sealed class FlashbackPlaybackSessionMetrics");
        AssertContains(playbackResultModelText, "internal sealed class FlashbackPlaybackResultMetrics");
        AssertContains(exportText, "internal sealed class FlashbackExportSessionMetrics");
        AssertContains(playbackSessionText, "public JsonElement BaselineSnapshot { get; init; }");
        AssertContains(playbackSessionText, "public long MinOnePercentLowAudioMasterFallbacks { get; set; }");
        AssertContains(playbackResultModelText, "public long SeekForwardDecodeCapHitsDelta { get; init; }");
        AssertContains(exportText, "public long ForceRotateFallbacksAtEnd { get; set; }");
        AssertDoesNotContain(recordingText, "internal sealed class FlashbackPlaybackSessionMetrics");
        AssertDoesNotContain(playbackSessionText, "internal sealed class FlashbackPlaybackResultMetrics");
        AssertDoesNotContain(playbackResultText, "internal sealed class FlashbackPlaybackResultMetrics");
        AssertDoesNotContain(playbackResultText, "internal sealed class FlashbackExportSessionMetrics");
        AssertContains(metricsText, "internal static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(");
        AssertContains(playbackSessionText, "internal static FlashbackPlaybackSessionMetrics BuildFlashbackPlaybackSessionMetrics(");
        AssertContains(playbackObservationText, "private static void ObservePlaybackSnapshot(");
        AssertContains(playbackObservationText, "ObservePlaybackOnePercentLow(");
        AssertContains(playbackObservationText, "ObservePlaybackFrameAndDecodeMetrics(metrics, snapshot);");
        AssertContains(playbackObservationText, "ObservePlaybackAudioMasterMetrics(metrics, snapshot);");
        AssertContains(playbackObservationOnePercentLowText, "private static void ObservePlaybackOnePercentLow(");
        AssertContains(playbackObservationOnePercentLowText, "metrics.OnePercentLowSampleWindowObserved = true;");
        AssertContains(playbackObservationFrameDecodeText, "private static void ObservePlaybackFrameAndDecodeMetrics(");
        AssertContains(playbackObservationFrameDecodeText, "metrics.MaxDecodePhaseObserved = GetString(snapshot, \"FlashbackPlaybackMaxDecodePhase\") ?? string.Empty;");
        AssertContains(playbackObservationAudioMasterText, "private static void ObservePlaybackAudioMasterMetrics(");
        AssertContains(playbackObservationAudioMasterText, "GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, \"FlashbackPlaybackAudioMasterFallbacks\")");
        AssertContains(playbackResultText, "internal static FlashbackPlaybackResultMetrics BuildFlashbackPlaybackResultMetrics(");
        AssertContains(playbackResultText, "var commands = BuildFlashbackPlaybackResultCommandMetrics(observed, endSnapshot, metrics);");
        AssertContains(playbackResultText, "PendingCommandsAtEnd = commands.PendingCommandsAtEnd,");
        AssertContains(playbackResultObservedReadsText, "private static long GetObservedLong(bool observed, JsonElement snapshot, string propertyName)");
        AssertContains(playbackResultObservedReadsText, "private static double GetObservedDouble(bool observed, JsonElement snapshot, string propertyName)");
        AssertContains(playbackResultCommandsText, "private static FlashbackPlaybackResultCommandMetrics BuildFlashbackPlaybackResultCommandMetrics(");
        AssertContains(playbackResultCommandsText, "PendingCommandsAtEnd: observed ? GetInt(endSnapshot, \"FlashbackPlaybackPendingCommands\") : 0");
        AssertContains(playbackResultCommandsText, "LastCommandFailureAtEnd: observed ? GetString(endSnapshot, \"FlashbackPlaybackLastCommandFailure\") ?? string.Empty : string.Empty");
        AssertContains(playbackResultCadenceText, "private static FlashbackPlaybackResultCadenceMetrics BuildFlashbackPlaybackResultCadenceMetrics(");
        AssertContains(playbackResultCadenceText, "DroppedFramesAtEnd: GetObservedLong(observed, endSnapshot, \"FlashbackPlaybackDroppedFrames\")");
        AssertContains(playbackResultDecodeText, "private static FlashbackPlaybackResultDecodeMetrics BuildFlashbackPlaybackResultDecodeMetrics(");
        AssertContains(playbackResultDecodeText, "MaxDecodePhaseAtEnd: observed ? GetString(endSnapshot, \"FlashbackPlaybackMaxDecodePhase\") ?? string.Empty : string.Empty");
        AssertContains(playbackResultAudioMasterText, "private static FlashbackPlaybackResultAudioMasterMetrics BuildFlashbackPlaybackResultAudioMasterMetrics(");
        AssertContains(playbackResultAudioMasterText, "AudioMasterFallbacksAtEnd: GetObservedLong(observed, endSnapshot, \"FlashbackPlaybackAudioMasterFallbacks\")");
        AssertContains(playbackResultStagesText, "private static FlashbackPlaybackResultStageMetrics BuildFlashbackPlaybackResultStageMetrics(");
        AssertContains(playbackResultStagesText, "GetCounterDelta(endSnapshot, metrics.BaselineSnapshot, \"FlashbackPlaybackSeekForwardDecodeCapHits\")");
        AssertContains(playbackResultCommandsText, "private readonly record struct FlashbackPlaybackResultCommandMetrics(");
        AssertContains(playbackResultCadenceText, "private readonly record struct FlashbackPlaybackResultCadenceMetrics(");
        AssertContains(playbackResultDecodeText, "private readonly record struct FlashbackPlaybackResultDecodeMetrics(");
        AssertContains(playbackResultAudioMasterText, "private readonly record struct FlashbackPlaybackResultAudioMasterMetrics(");
        AssertContains(playbackResultStagesText, "private readonly record struct FlashbackPlaybackResultStageMetrics(");
        AssertDoesNotContain(playbackResultText, "GetObservedLong(observed, endSnapshot, \"FlashbackPlaybackCommandsDropped\")");
        AssertDoesNotContain(playbackResultText, "GetObservedDouble(observed, endSnapshot, \"FlashbackPlaybackDecodeMaxMs\")");
        AssertDoesNotContain(playbackResultText, "GetObservedLong(observed, endSnapshot, \"FlashbackPlaybackAudioMasterFallbacks\")");
        AssertDoesNotContain(playbackResultText, "private static long GetObservedLong(");
        AssertDoesNotContain(playbackResultText, "private static double GetObservedDouble(");
        AssertDoesNotContain(playbackResultCommandsText, "FlashbackPlaybackDecodeMaxMs");
        AssertDoesNotContain(playbackResultCadenceText, "FlashbackPlaybackAudioMasterFallbacks");
        AssertDoesNotContain(playbackResultDecodeText, "FlashbackPlaybackSegmentSwitches");
        AssertDoesNotContain(playbackResultAudioMasterText, "FlashbackPlaybackSeekForwardDecodeCapHits");
        AssertContains(metricsText, "internal static FlashbackExportSessionMetrics BuildFlashbackExportSessionMetrics(");
        AssertContains(metricsText, "metrics.ForceRotateFallbacksAtEnd = GetNullableLong(lastSnapshot, \"FlashbackExportForceRotateFallbacks\") ?? 0;");
        AssertContains(metricsText, "metrics.ForceRotateFallbacksDelta = GetCounterDelta(");
        AssertContains(metricsText, "metrics.LastForceRotateFallbackSegmentsAtEnd =");
        AssertContains(playbackObservationText, "private static bool IsPlaybackSnapshotActive(");
        AssertDoesNotContain(playbackObservationText, "private static void ObservePlaybackOnePercentLow(");
        AssertDoesNotContain(playbackObservationText, "private static void ObservePlaybackFrameAndDecodeMetrics(");
        AssertDoesNotContain(playbackObservationText, "private static void ObservePlaybackAudioMasterMetrics(");
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
