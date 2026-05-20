using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionFlashbackWaits_OwnsSnapshotPollingWaits()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var setupText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioSetup.cs")
            .Replace("\r\n", "\n");
        var waitsText = ReadDiagnosticSessionFlashbackWaitsSource();
        var playbackStateText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackWaits.Playback.cs")
            .Replace("\r\n", "\n");
        var playbackBoundaryText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackWaits.PlaybackBoundary.cs")
            .Replace("\r\n", "\n");
        var playbackWarmSampleText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackWaits.PlaybackWarmSample.cs")
            .Replace("\r\n", "\n");
        var playbackPositionText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackWaits.PlaybackPosition.cs")
            .Replace("\r\n", "\n");

        AssertContains(waitsText, "internal static partial class DiagnosticSessionFlashbackWaits");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(waitsText, "internal static async Task<bool> WaitForFlashbackPlaybackPositionAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackActiveAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForPreviewActiveAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(");
        AssertContains(waitsText, "internal static async Task<bool> WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(waitsText, "FlashbackPlaybackPendingCommands");
        AssertContains(waitsText, "FlashbackPlaybackFrameCount");
        AssertContains(waitsText, "RecordingBackend");
        AssertContains(playbackStateText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(");
        AssertContains(playbackStateText, "string expectedState");
        AssertDoesNotContain(playbackStateText, "WaitForFlashbackPlaybackBoundaryCrossAsync(");
        AssertDoesNotContain(playbackStateText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertDoesNotContain(playbackStateText, "WaitForFlashbackPlaybackPositionAsync(");
        AssertContains(playbackBoundaryText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(");
        AssertContains(playbackBoundaryText, "positionMs >= boundaryMs + 1_500");
        AssertContains(playbackBoundaryText, "FlashbackPlaybackPendingCommands");
        AssertDoesNotContain(playbackBoundaryText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(playbackWarmSampleText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(playbackWarmSampleText, "FlashbackPlaybackTargetFps");
        AssertContains(playbackWarmSampleText, "SelectedExactFrameRate");
        AssertDoesNotContain(playbackWarmSampleText, "FlashbackPlaybackPendingCommands");
        AssertContains(playbackPositionText, "internal static async Task<bool> WaitForFlashbackPlaybackPositionAsync(");
        AssertContains(playbackPositionText, "Math.Abs(position - targetPositionMs) <= 1_500");
        AssertDoesNotContain(playbackPositionText, "WaitForFlashbackPlaybackStateAsync(");
        AssertContains(setupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> WaitForFlashbackPlaybackPositionAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackActiveAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForPreviewActiveAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> WaitForFlashbackStressBufferReadyAsync(");

        return Task.CompletedTask;
    }
}
