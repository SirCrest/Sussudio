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
        var audioSignalProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Audio.Signal.cs")
            .Replace("\r\n", "\n");
        var audioDropsProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.AudioDrops.cs")
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
        AssertContains(audioFlatteningText, "Signal = BuildAudioSignalFlattenedProjection(audioAndIngest.Signal),");
        AssertContains(audioFlatteningText, "Ingest = BuildCaptureIngestFlattenedProjection(audioAndIngest.Ingest),");
        AssertContains(audioFlatteningText, "SourceReader = BuildSourceReaderFlattenedProjection(audioAndIngest.Ingest),");
        AssertContains(audioFlatteningText, "WasapiCapture = BuildWasapiCaptureFlattenedProjection(audioAndIngest.Wasapi),");
        AssertContains(audioFlatteningText, "WasapiPlayback = BuildWasapiPlaybackFlattenedProjection(audioAndIngest.Wasapi)");
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
        AssertContains(audioSignalFlatteningText, "Peak = signal.Peak,");
        AssertContains(captureIngestFlatteningText, "private static CaptureIngestFlattenedProjection BuildCaptureIngestFlattenedProjection(");
        AssertContains(captureIngestFlatteningText, "AudioFramesWrittenToSink = ingest.AudioFramesWrittenToSink,");
        AssertContains(sourceReaderFlatteningText, "private static SourceReaderFlattenedProjection BuildSourceReaderFlattenedProjection(");
        AssertContains(sourceReaderFlatteningText, "ReadOutstanding = ingest.SourceReaderReadOutstanding,");
        AssertContains(wasapiCaptureFlatteningText, "private static WasapiCaptureFlattenedProjection BuildWasapiCaptureFlattenedProjection(");
        AssertContains(wasapiCaptureFlatteningText, "AudioLevelEventsFired = wasapi.CaptureAudioLevelEventsFired,");
        AssertContains(wasapiPlaybackFlatteningText, "private static WasapiPlaybackFlattenedProjection BuildWasapiPlaybackFlattenedProjection(");
        AssertContains(wasapiPlaybackFlatteningText, "BufferedDurationMs = wasapi.PlaybackBufferedDurationMs,");

        AssertContains(audioProjectionText, "private static AudioAndIngestProjection BuildAudioAndIngestProjection(");
        AssertContains(audioProjectionText, "Signal = BuildAudioSignalProjection(viewModelSnapshot, audioSignal),");
        AssertContains(audioProjectionText, "Ingest = BuildCaptureIngestProjection(captureRuntime),");
        AssertContains(audioProjectionText, "Wasapi = BuildWasapiAudioProjection(captureRuntime)");
        AssertContains(audioProjectionText, "private readonly record struct AudioAndIngestProjection");
        AssertContains(audioProjectionText, "public AudioSignalProjection Signal { get; init; }");
        AssertContains(audioProjectionText, "public CaptureIngestProjection Ingest { get; init; }");
        AssertContains(audioProjectionText, "public WasapiAudioProjection Wasapi { get; init; }");
        AssertDoesNotContain(audioProjectionText, "AudioPeak = viewModelSnapshot.AudioPeak,");
        AssertDoesNotContain(audioProjectionText, "SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,");
        AssertDoesNotContain(audioProjectionText, "WasapiCaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,");

        AssertContains(audioSignalProjectionText, "private static AudioSignalProjection BuildAudioSignalProjection(");
        AssertContains(audioSignalProjectionText, "Peak = viewModelSnapshot.AudioPeak,");
        AssertContains(audioSignalProjectionText, "SignalPresent = audioSignal.SignalPresent,");
        AssertContains(audioSignalProjectionText, "private readonly record struct AudioSignalProjection");

        AssertContains(audioDropsProjectionText, "private static AudioDropsProjection BuildAudioDropsProjection(CaptureHealthSnapshot health)");
        AssertContains(audioDropsProjectionText, "QueueDropsRealtime = health.AudioDropsQueueSaturated + health.AudioDropsBacklogEviction,");
        AssertContains(audioDropsProjectionText, "QueueDropsFileWriter = health.AudioChunksDropped");
        AssertContains(audioDropsProjectionText, "private readonly record struct AudioDropsProjection");

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
