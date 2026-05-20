using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationDiagnosticsSnapshotAudioProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var audioFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.cs")
            .Replace("\r\n", "\n");
        var audioProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Audio.cs")
            .Replace("\r\n", "\n");
        var captureIngestProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureIngest.cs")
            .Replace("\r\n", "\n");
        var wasapiAudioProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.WasapiAudio.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var audioAndIngest = BuildAudioAndIngestProjection(viewModelSnapshot, captureRuntime, audioSignal);");
        AssertContains(snapshotFlatteningText, "var audioAndIngestFlattening = BuildAudioAndIngestFlattenedProjection(audioAndIngest);");
        AssertContains(snapshotFlatteningText, "AudioPeak = audioAndIngestFlattening.AudioPeak,");
        AssertContains(snapshotFlatteningText, "AudioSignalPresent = audioAndIngestFlattening.AudioSignalPresent,");
        AssertContains(snapshotFlatteningText, "AudioFramesWrittenToSink = audioAndIngestFlattening.AudioFramesWrittenToSink,");
        AssertContains(snapshotFlatteningText, "SourceReaderReadOutstanding = audioAndIngestFlattening.SourceReaderReadOutstanding,");
        AssertContains(snapshotFlatteningText, "WasapiCaptureAudioLevelEventsFired = audioAndIngestFlattening.WasapiCaptureAudioLevelEventsFired,");
        AssertContains(snapshotFlatteningText, "WasapiPlaybackBufferedDurationMs = audioAndIngestFlattening.WasapiPlaybackBufferedDurationMs,");
        AssertDoesNotContain(snapshotFlatteningText, "AudioPeak = viewModelSnapshot.AudioPeak,");
        AssertDoesNotContain(snapshotFlatteningText, "AudioPeak = audioAndIngest.AudioPeak,");
        AssertDoesNotContain(snapshotFlatteningText, "AudioSignalPresent = audioSignal.SignalPresent,");
        AssertDoesNotContain(snapshotFlatteningText, "AudioSignalPresent = audioAndIngest.AudioSignalPresent,");
        AssertDoesNotContain(snapshotFlatteningText, "AudioFramesWrittenToSink = captureRuntime.AudioFramesWrittenToSink,");
        AssertDoesNotContain(snapshotFlatteningText, "AudioFramesWrittenToSink = audioAndIngest.AudioFramesWrittenToSink,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceReaderReadOutstanding = audioAndIngest.SourceReaderReadOutstanding,");
        AssertDoesNotContain(snapshotFlatteningText, "WasapiCaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,");
        AssertDoesNotContain(snapshotFlatteningText, "WasapiCaptureAudioLevelEventsFired = audioAndIngest.WasapiCaptureAudioLevelEventsFired,");
        AssertDoesNotContain(snapshotFlatteningText, "WasapiPlaybackBufferedDurationMs = captureRuntime.WasapiPlaybackBufferedDurationMs,");
        AssertDoesNotContain(snapshotFlatteningText, "WasapiPlaybackBufferedDurationMs = audioAndIngest.WasapiPlaybackBufferedDurationMs,");

        AssertContains(audioFlatteningText, "private static AudioAndIngestFlattenedProjection BuildAudioAndIngestFlattenedProjection(");
        AssertContains(audioFlatteningText, "AudioPeak = audioAndIngest.AudioPeak,");
        AssertContains(audioFlatteningText, "SourceReaderReadOutstanding = audioAndIngest.SourceReaderReadOutstanding,");
        AssertContains(audioFlatteningText, "WasapiPlaybackBufferedDurationMs = audioAndIngest.WasapiPlaybackBufferedDurationMs");
        AssertContains(audioFlatteningText, "private readonly record struct AudioAndIngestFlattenedProjection");

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

        AssertContains(audioProjectionText, "private static AudioSignalProjection BuildAudioSignalProjection(");
        AssertContains(audioProjectionText, "Peak = viewModelSnapshot.AudioPeak,");
        AssertContains(audioProjectionText, "SignalPresent = audioSignal.SignalPresent,");
        AssertContains(audioProjectionText, "private readonly record struct AudioSignalProjection");

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
