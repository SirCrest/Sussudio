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

        AssertContains(metricsText, "internal static partial class DiagnosticSessionFlashbackMetrics");
        AssertContains(metricsText, "internal sealed class FlashbackRecordingSessionMetrics");
        AssertContains(metricsText, "internal sealed class FlashbackPlaybackSessionMetrics");
        AssertContains(metricsText, "internal sealed class FlashbackPlaybackResultMetrics");
        AssertContains(metricsText, "internal sealed class FlashbackExportSessionMetrics");
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
        AssertContains(metricsText, "internal static FlashbackPlaybackResultMetrics BuildFlashbackPlaybackResultMetrics(");
        AssertContains(metricsText, "internal static FlashbackExportSessionMetrics BuildFlashbackExportSessionMetrics(");
        AssertContains(metricsText, "public long ForceRotateFallbacksAtEnd { get; set; }");
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
