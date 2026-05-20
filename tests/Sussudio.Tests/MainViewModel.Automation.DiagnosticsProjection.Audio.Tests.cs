using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsSnapshotAudioProjection_LivesInFocusedPartial()
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
        AssertContains(snapshotFlatteningText, "AudioPeak = audioAndIngestFlattening.Signal.Peak,");
        AssertContains(snapshotFlatteningText, "AudioSignalPresent = audioAndIngestFlattening.Signal.SignalPresent,");
        AssertContains(snapshotFlatteningText, "AudioFramesWrittenToSink = audioAndIngestFlattening.Ingest.AudioFramesWrittenToSink,");
        AssertContains(snapshotFlatteningText, "SourceReaderReadOutstanding = audioAndIngestFlattening.SourceReader.ReadOutstanding,");
        AssertContains(snapshotFlatteningText, "WasapiCaptureAudioLevelEventsFired = audioAndIngestFlattening.WasapiCapture.AudioLevelEventsFired,");
        AssertContains(snapshotFlatteningText, "WasapiPlaybackBufferedDurationMs = audioAndIngestFlattening.WasapiPlayback.BufferedDurationMs,");
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
        AssertContains(audioFlatteningText, "Signal = BuildAudioSignalFlattenedProjection(audioAndIngest),");
        AssertContains(audioFlatteningText, "Ingest = BuildCaptureIngestFlattenedProjection(audioAndIngest),");
        AssertContains(audioFlatteningText, "SourceReader = BuildSourceReaderFlattenedProjection(audioAndIngest),");
        AssertContains(audioFlatteningText, "WasapiCapture = BuildWasapiCaptureFlattenedProjection(audioAndIngest),");
        AssertContains(audioFlatteningText, "WasapiPlayback = BuildWasapiPlaybackFlattenedProjection(audioAndIngest)");
        AssertContains(audioFlatteningText, "private readonly record struct AudioAndIngestFlattenedProjection");

        var audioSignalFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.Signal.cs")
            .Replace("\r\n", "\n");
        var captureIngestFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.CaptureIngest.cs")
            .Replace("\r\n", "\n");
        var sourceReaderFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.SourceReader.cs")
            .Replace("\r\n", "\n");
        var wasapiCaptureFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.WasapiCapture.cs")
            .Replace("\r\n", "\n");
        var wasapiPlaybackFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.WasapiPlayback.cs")
            .Replace("\r\n", "\n");

        AssertContains(audioSignalFlatteningText, "private static AudioSignalFlattenedProjection BuildAudioSignalFlattenedProjection(");
        AssertContains(audioSignalFlatteningText, "Peak = audioAndIngest.AudioPeak,");
        AssertContains(captureIngestFlatteningText, "private static CaptureIngestFlattenedProjection BuildCaptureIngestFlattenedProjection(");
        AssertContains(captureIngestFlatteningText, "AudioFramesWrittenToSink = audioAndIngest.AudioFramesWrittenToSink,");
        AssertContains(sourceReaderFlatteningText, "private static SourceReaderFlattenedProjection BuildSourceReaderFlattenedProjection(");
        AssertContains(sourceReaderFlatteningText, "ReadOutstanding = audioAndIngest.SourceReaderReadOutstanding,");
        AssertContains(wasapiCaptureFlatteningText, "private static WasapiCaptureFlattenedProjection BuildWasapiCaptureFlattenedProjection(");
        AssertContains(wasapiCaptureFlatteningText, "AudioLevelEventsFired = audioAndIngest.WasapiCaptureAudioLevelEventsFired,");
        AssertContains(wasapiPlaybackFlatteningText, "private static WasapiPlaybackFlattenedProjection BuildWasapiPlaybackFlattenedProjection(");
        AssertContains(wasapiPlaybackFlatteningText, "BufferedDurationMs = audioAndIngest.WasapiPlaybackBufferedDurationMs,");

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
