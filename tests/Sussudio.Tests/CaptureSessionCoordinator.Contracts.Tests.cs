using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureSessionCoordinator_ModelsLiveInFocusedFile()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var modelText = rootText;

        AssertContains(modelText, "public enum CaptureCommandKind");
        AssertContains(modelText, "public enum CaptureCommandOutcome");
        AssertContains(modelText, "public readonly record struct CaptureCommand(");
        AssertContains(modelText, "public sealed class CaptureSessionSnapshot");
        AssertContains(modelText, "internal readonly record struct FlashbackPlaybackSnapshot(");
        AssertContains(modelText, "internal readonly record struct FlashbackBufferStatus(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionCoordinator.Models.cs")),
            "coordinator model surface folded into the coordinator root");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_FlashbackFacadeLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var flashbackText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.cs")
            .Replace("\r\n", "\n");
        var flashbackStatusText = flashbackText;
        var flashbackExportText = flashbackText;
        var flashbackGuardsText = flashbackText;

        AssertContains(flashbackText, "public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)");
        AssertContains(flashbackText, "public Task UpdateRecordingFormatAsync(RecordingFormat format, CancellationToken cancellationToken = default)");
        AssertContains(flashbackText, "public Task CycleFlashbackEncoderSettingsAsync(");
        AssertContains(flashbackText, "public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(flashbackStatusText, "internal FlashbackBufferStatus GetFlashbackBufferStatus()");
        AssertContains(flashbackStatusText, "internal FlashbackPlaybackSnapshot GetFlashbackPlaybackSnapshot()");
        AssertContains(flashbackExportText, "internal Task<FinalizeResult> ExportFlashbackRangeAsync(");
        AssertContains(flashbackExportText, "internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()");
        AssertContains(flashbackGuardsText, "private bool TryGetActiveFlashback(");
        AssertDoesNotContain(rootText, "public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "public Task CycleFlashbackEncoderSettingsAsync(");
        AssertDoesNotContain(rootText, "internal FlashbackPlaybackSnapshot GetFlashbackPlaybackSnapshot()");
        AssertDoesNotContain(rootText, "private bool TryGetActiveFlashback(");

        return Task.CompletedTask;
    }
}
