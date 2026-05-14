using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationDiagnosticsSnapshotAudioProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var audioProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Audio.cs")
            .Replace("\r\n", "\n");
        var audioSignalProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.AudioSignal.cs")
            .Replace("\r\n", "\n");
        var captureIngestProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureIngest.cs")
            .Replace("\r\n", "\n");
        var wasapiAudioProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.WasapiAudio.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var audioAndIngest = BuildAudioAndIngestProjection(viewModelSnapshot, captureRuntime, audioSignal);");
        AssertContains(snapshotProjectionText, "AudioPeak = audioAndIngest.AudioPeak,");
        AssertContains(snapshotProjectionText, "AudioSignalPresent = audioAndIngest.AudioSignalPresent,");
        AssertContains(snapshotProjectionText, "AudioFramesWrittenToSink = audioAndIngest.AudioFramesWrittenToSink,");
        AssertContains(snapshotProjectionText, "SourceReaderReadOutstanding = audioAndIngest.SourceReaderReadOutstanding,");
        AssertContains(snapshotProjectionText, "WasapiCaptureAudioLevelEventsFired = audioAndIngest.WasapiCaptureAudioLevelEventsFired,");
        AssertContains(snapshotProjectionText, "WasapiPlaybackBufferedDurationMs = audioAndIngest.WasapiPlaybackBufferedDurationMs,");
        AssertDoesNotContain(snapshotProjectionText, "AudioPeak = viewModelSnapshot.AudioPeak,");
        AssertDoesNotContain(snapshotProjectionText, "AudioSignalPresent = audioSignal.SignalPresent,");
        AssertDoesNotContain(snapshotProjectionText, "AudioFramesWrittenToSink = captureRuntime.AudioFramesWrittenToSink,");
        AssertDoesNotContain(snapshotProjectionText, "SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,");
        AssertDoesNotContain(snapshotProjectionText, "WasapiCaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,");
        AssertDoesNotContain(snapshotProjectionText, "WasapiPlaybackBufferedDurationMs = captureRuntime.WasapiPlaybackBufferedDurationMs,");

        AssertContains(audioProjectionText, "private static AudioAndIngestProjection BuildAudioAndIngestProjection(");
        AssertContains(audioProjectionText, "var audioSignalProjection = BuildAudioSignalProjection(viewModelSnapshot, audioSignal);");
        AssertContains(audioProjectionText, "var captureIngest = BuildCaptureIngestProjection(captureRuntime);");
        AssertContains(audioProjectionText, "var wasapiAudio = BuildWasapiAudioProjection(captureRuntime);");
        AssertContains(audioProjectionText, "private readonly record struct AudioAndIngestProjection");
        AssertContains(audioProjectionText, "AudioPeak = audioSignalProjection.Peak,");
        AssertContains(audioProjectionText, "AudioSignalPresent = audioSignalProjection.SignalPresent,");
        AssertContains(audioProjectionText, "AudioFramesWrittenToSink = captureIngest.AudioFramesWrittenToSink,");
        AssertContains(audioProjectionText, "SourceReaderReadOutstanding = captureIngest.SourceReaderReadOutstanding,");
        AssertContains(audioProjectionText, "WasapiCaptureAudioLevelEventsFired = wasapiAudio.CaptureAudioLevelEventsFired,");
        AssertContains(audioProjectionText, "WasapiPlaybackBufferedDurationMs = wasapiAudio.PlaybackBufferedDurationMs,");
        AssertDoesNotContain(audioProjectionText, "AudioPeak = viewModelSnapshot.AudioPeak,");
        AssertDoesNotContain(audioProjectionText, "SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,");
        AssertDoesNotContain(audioProjectionText, "WasapiCaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,");

        AssertContains(audioSignalProjectionText, "private static AudioSignalProjection BuildAudioSignalProjection(");
        AssertContains(audioSignalProjectionText, "Peak = viewModelSnapshot.AudioPeak,");
        AssertContains(audioSignalProjectionText, "SignalPresent = audioSignal.SignalPresent,");
        AssertContains(audioSignalProjectionText, "private readonly record struct AudioSignalProjection");

        AssertContains(captureIngestProjectionText, "private static CaptureIngestProjection BuildCaptureIngestProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(captureIngestProjectionText, "AudioFramesWrittenToSink = captureRuntime.AudioFramesWrittenToSink,");
        AssertContains(captureIngestProjectionText, "SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,");
        AssertContains(captureIngestProjectionText, "private readonly record struct CaptureIngestProjection");

        AssertContains(wasapiAudioProjectionText, "private static WasapiAudioProjection BuildWasapiAudioProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(wasapiAudioProjectionText, "CaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,");
        AssertContains(wasapiAudioProjectionText, "PlaybackBufferedDurationMs = captureRuntime.WasapiPlaybackBufferedDurationMs,");
        AssertContains(wasapiAudioProjectionText, "private readonly record struct WasapiAudioProjection");

        return Task.CompletedTask;
    }

}
