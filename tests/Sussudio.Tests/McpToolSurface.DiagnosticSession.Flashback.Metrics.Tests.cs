using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionFlashbackMetrics_OwnsFlashbackSessionMetricProjection()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var metricsText = ReadDiagnosticSessionFlashbackMetricsSource();

        AssertContains(metricsText, "internal static partial class DiagnosticSessionFlashbackMetrics");
        AssertContains(metricsText, "internal sealed class FlashbackRecordingSessionMetrics");
        AssertContains(metricsText, "internal sealed class FlashbackPlaybackSessionMetrics");
        AssertContains(metricsText, "internal sealed class FlashbackPlaybackResultMetrics");
        AssertContains(metricsText, "internal sealed class FlashbackExportSessionMetrics");
        AssertContains(metricsText, "internal static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(");
        AssertContains(metricsText, "internal static FlashbackPlaybackSessionMetrics BuildFlashbackPlaybackSessionMetrics(");
        AssertContains(metricsText, "internal static FlashbackPlaybackResultMetrics BuildFlashbackPlaybackResultMetrics(");
        AssertContains(metricsText, "internal static FlashbackExportSessionMetrics BuildFlashbackExportSessionMetrics(");
        AssertContains(metricsText, "private static bool IsPlaybackSnapshotActive(");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;");
        AssertContains(builderText, "var playbackResultMetrics = BuildFlashbackPlaybackResultMetrics(playbackSessionMetrics);");
        AssertDoesNotContain(runnerText, "private sealed class FlashbackPlaybackSessionMetrics");
        AssertDoesNotContain(runnerText, "GetString(playbackEndSnapshot,");
        AssertDoesNotContain(runnerText, "private sealed class FlashbackExportSessionMetrics");
        AssertDoesNotContain(runnerText, "private static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(");
        AssertDoesNotContain(runnerText, "private static bool IsPlaybackSnapshotActive(");

        return Task.CompletedTask;
    }
}
