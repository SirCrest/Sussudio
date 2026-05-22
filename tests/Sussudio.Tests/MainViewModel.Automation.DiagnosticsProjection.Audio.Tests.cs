using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsSnapshotAudioProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var audioProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Audio.cs")
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

        AssertContains(audioProjectionText, "private static AudioAndIngestProjection BuildAudioAndIngestProjection(");
        AssertContains(audioProjectionText, "Signal = BuildAudioSignalProjection(viewModelSnapshot, audioSignal),");
        AssertContains(audioProjectionText, "Ingest = BuildCaptureIngestProjection(captureRuntime),");
        AssertContains(audioProjectionText, "Wasapi = BuildWasapiAudioProjection(captureRuntime)");
        AssertContains(audioProjectionText, "private readonly record struct AudioAndIngestProjection");
        AssertContains(audioProjectionText, "public AudioSignalProjection Signal { get; init; }");
        AssertContains(audioProjectionText, "public CaptureIngestProjection Ingest { get; init; }");
        AssertContains(audioProjectionText, "public WasapiAudioProjection Wasapi { get; init; }");
        AssertContains(audioProjectionText, "private static AudioAndIngestFlattenedProjection BuildAudioAndIngestFlattenedProjection(");
        AssertContains(audioProjectionText, "Signal = BuildAudioSignalFlattenedProjection(audioAndIngest.Signal),");
        AssertContains(audioProjectionText, "Ingest = BuildCaptureIngestFlattenedProjection(audioAndIngest.Ingest),");
        AssertContains(audioProjectionText, "SourceReader = BuildSourceReaderFlattenedProjection(audioAndIngest.Ingest),");
        AssertContains(audioProjectionText, "WasapiCapture = BuildWasapiCaptureFlattenedProjection(audioAndIngest.Wasapi),");
        AssertContains(audioProjectionText, "WasapiPlayback = BuildWasapiPlaybackFlattenedProjection(audioAndIngest.Wasapi)");
        AssertContains(audioProjectionText, "private readonly record struct AudioAndIngestFlattenedProjection");
        AssertDoesNotContain(audioProjectionText, "AudioPeak = viewModelSnapshot.AudioPeak,");
        AssertDoesNotContain(audioProjectionText, "SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,");
        AssertDoesNotContain(audioProjectionText, "WasapiCaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,");

        AssertContains(audioProjectionText, "private static AudioSignalProjection BuildAudioSignalProjection(");
        AssertContains(audioProjectionText, "Peak = viewModelSnapshot.AudioPeak,");
        AssertContains(audioProjectionText, "SignalPresent = audioSignal.SignalPresent,");
        AssertContains(audioProjectionText, "private readonly record struct AudioSignalProjection");
        AssertContains(audioProjectionText, "private static AudioSignalFlattenedProjection BuildAudioSignalFlattenedProjection(");
        AssertContains(audioProjectionText, "Peak = signal.Peak,");

        AssertContains(audioDropsProjectionText, "private static AudioDropsProjection BuildAudioDropsProjection(CaptureHealthSnapshot health)");
        AssertContains(audioDropsProjectionText, "QueueDropsRealtime = health.AudioDropsQueueSaturated + health.AudioDropsBacklogEviction,");
        AssertContains(audioDropsProjectionText, "QueueDropsFileWriter = health.AudioChunksDropped");
        AssertContains(audioDropsProjectionText, "private readonly record struct AudioDropsProjection");

        AssertContains(captureIngestProjectionText, "private static CaptureIngestProjection BuildCaptureIngestProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(captureIngestProjectionText, "AudioFramesWrittenToSink = captureRuntime.AudioFramesWrittenToSink,");
        AssertContains(captureIngestProjectionText, "SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,");
        AssertContains(captureIngestProjectionText, "private readonly record struct CaptureIngestProjection");
        AssertContains(captureIngestProjectionText, "private static CaptureIngestFlattenedProjection BuildCaptureIngestFlattenedProjection(");
        AssertContains(captureIngestProjectionText, "AudioFramesWrittenToSink = ingest.AudioFramesWrittenToSink,");
        AssertContains(captureIngestProjectionText, "private static SourceReaderFlattenedProjection BuildSourceReaderFlattenedProjection(");
        AssertContains(captureIngestProjectionText, "ReadOutstanding = ingest.SourceReaderReadOutstanding,");

        AssertContains(wasapiAudioProjectionText, "private static WasapiAudioProjection BuildWasapiAudioProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(wasapiAudioProjectionText, "CaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,");
        AssertContains(wasapiAudioProjectionText, "PlaybackBufferedDurationMs = captureRuntime.WasapiPlaybackBufferedDurationMs,");
        AssertContains(wasapiAudioProjectionText, "private readonly record struct WasapiAudioProjection");
        AssertContains(wasapiAudioProjectionText, "private static WasapiCaptureFlattenedProjection BuildWasapiCaptureFlattenedProjection(");
        AssertContains(wasapiAudioProjectionText, "AudioLevelEventsFired = wasapi.CaptureAudioLevelEventsFired,");
        AssertContains(wasapiAudioProjectionText, "private static WasapiPlaybackFlattenedProjection BuildWasapiPlaybackFlattenedProjection(");
        AssertContains(wasapiAudioProjectionText, "BufferedDurationMs = wasapi.PlaybackBufferedDurationMs,");

        return Task.CompletedTask;
    }

}
