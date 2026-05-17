using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionFlashbackValidation_OwnsFlashbackWarningPolicy()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var validationRootText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.cs")
            .Replace("\r\n", "\n");
        var recordingValidationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.Recording.cs")
            .Replace("\r\n", "\n");
        var playbackValidationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.Playback.cs")
            .Replace("\r\n", "\n");
        var previewValidationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.Preview.cs")
            .Replace("\r\n", "\n");

        AssertContains(validationRootText, "internal static partial class DiagnosticSessionFlashbackValidation");
        AssertDoesNotContain(validationRootText, "internal static void ValidateFlashback");
        AssertContains(recordingValidationText, "internal static void ValidateFlashbackRecordingSession(");
        AssertContains(recordingValidationText, "\"flashback recording: no Flashback video frames submitted to encoder\"");
        AssertContains(playbackValidationText, "internal static void ValidateFlashbackPlaybackSession(");
        AssertContains(playbackValidationText, "\"flashback playback: no playback frames were observed\"");
        AssertContains(playbackValidationText, "\"flashback playback: absolute A/V drift exceeded budget");
        AssertContains(previewValidationText, "internal static void ValidateFlashbackPreviewScheduler(");
        AssertContains(previewValidationText, "\"flashback preview: present/display pressure \"");
        AssertContains(previewValidationText, "latestSlowReason={FormatOptional(previewD3DMetrics.LatestSlowFrameReason)}");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackRecordingSession(");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackPlaybackSession(");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackPreviewScheduler(");

        return Task.CompletedTask;
    }
}
