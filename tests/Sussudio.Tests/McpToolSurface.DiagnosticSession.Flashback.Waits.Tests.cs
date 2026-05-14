using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionFlashbackWaits_OwnsSnapshotPollingWaits()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var setupText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioSetup.cs")
            .Replace("\r\n", "\n");
        var waitsText = ReadDiagnosticSessionFlashbackWaitsSource();

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
