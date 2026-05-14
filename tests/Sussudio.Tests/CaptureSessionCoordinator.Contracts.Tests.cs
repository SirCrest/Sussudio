using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureSessionCoordinator_ModelsLiveInFocusedFile()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var modelText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Models.cs")
            .Replace("\r\n", "\n");

        AssertContains(modelText, "public enum CaptureCommandKind");
        AssertContains(modelText, "public enum CaptureCommandOutcome");
        AssertContains(modelText, "public readonly record struct CaptureCommand(");
        AssertContains(modelText, "public sealed class CaptureSessionSnapshot");
        AssertContains(modelText, "internal readonly record struct FlashbackPlaybackSnapshot(");
        AssertContains(modelText, "internal readonly record struct FlashbackBufferStatus(");
        AssertDoesNotContain(rootText, "public enum CaptureCommandKind");
        AssertDoesNotContain(rootText, "public enum CaptureCommandOutcome");
        AssertDoesNotContain(rootText, "public sealed class CaptureSessionSnapshot");
        AssertDoesNotContain(rootText, "internal readonly record struct FlashbackPlaybackSnapshot(");
        AssertDoesNotContain(rootText, "internal readonly record struct FlashbackBufferStatus(");

        return Task.CompletedTask;
    }

    private static Task CaptureSessionCoordinator_FlashbackFacadeLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var flashbackText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.cs")
            .Replace("\r\n", "\n");

        AssertContains(flashbackText, "public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)");
        AssertContains(flashbackText, "public Task UpdateRecordingFormatAsync(RecordingFormat format, CancellationToken cancellationToken = default)");
        AssertContains(flashbackText, "public Task CycleFlashbackEncoderSettingsAsync(");
        AssertContains(flashbackText, "public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(flashbackText, "internal FlashbackBufferStatus GetFlashbackBufferStatus()");
        AssertContains(flashbackText, "internal FlashbackPlaybackSnapshot GetFlashbackPlaybackSnapshot()");
        AssertContains(flashbackText, "internal Task<FinalizeResult> ExportFlashbackRangeAsync(");
        AssertContains(flashbackText, "internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()");
        AssertContains(flashbackText, "private bool TryGetActiveFlashback(");
        AssertDoesNotContain(rootText, "public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "public Task CycleFlashbackEncoderSettingsAsync(");
        AssertDoesNotContain(rootText, "internal FlashbackPlaybackSnapshot GetFlashbackPlaybackSnapshot()");
        AssertDoesNotContain(rootText, "private bool TryGetActiveFlashback(");

        return Task.CompletedTask;
    }
}
