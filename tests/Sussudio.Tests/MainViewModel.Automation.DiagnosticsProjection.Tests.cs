using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsSnapshotStatusProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var snapshotStatusProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var snapshotStatus = BuildSnapshotStatusProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var snapshotStatusFlattening = BuildSnapshotStatusFlattenedProjection(snapshotStatus);");
        AssertContains(snapshotFlatteningText, "TimestampUtc = snapshotStatusFlattening.TimestampUtc,");
        AssertContains(snapshotFlatteningText, "VerificationInProgress = snapshotStatusFlattening.VerificationInProgress,");
        AssertContains(snapshotFlatteningText, "SessionState = snapshotStatusFlattening.SessionState,");
        AssertContains(snapshotFlatteningText, "StatusText = snapshotStatusFlattening.StatusText,");
        AssertDoesNotContain(snapshotFlatteningText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertDoesNotContain(snapshotFlatteningText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertDoesNotContain(snapshotFlatteningText, "SessionState = captureRuntime.SessionState,");
        AssertDoesNotContain(snapshotFlatteningText, "StatusText = viewModelSnapshot.StatusText,");
        AssertDoesNotContain(snapshotFlatteningText, "TimestampUtc = snapshotStatus.TimestampUtc,");
        AssertDoesNotContain(snapshotFlatteningText, "StatusText = snapshotStatus.StatusText,");

        AssertContains(snapshotStatusProjectionText, "private SnapshotStatusProjection BuildSnapshotStatusProjection(");
        AssertContains(snapshotStatusProjectionText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertContains(snapshotStatusProjectionText, "IsInitialized = viewModelSnapshot.IsInitialized,");
        AssertContains(snapshotStatusProjectionText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertContains(snapshotStatusProjectionText, "SessionState = captureRuntime.SessionState,");
        AssertContains(snapshotStatusProjectionText, "StatusText = viewModelSnapshot.StatusText");
        AssertContains(snapshotStatusProjectionText, "private readonly record struct SnapshotStatusProjection");
        AssertContains(snapshotStatusProjectionText, "private static SnapshotStatusFlattenedProjection BuildSnapshotStatusFlattenedProjection(");
        AssertContains(snapshotStatusProjectionText, "TimestampUtc = snapshotStatus.TimestampUtc,");
        AssertContains(snapshotStatusProjectionText, "VerificationInProgress = snapshotStatus.VerificationInProgress,");
        AssertContains(snapshotStatusProjectionText, "SessionState = snapshotStatus.SessionState,");
        AssertContains(snapshotStatusProjectionText, "StatusText = snapshotStatus.StatusText");
        AssertContains(snapshotStatusProjectionText, "private readonly record struct SnapshotStatusFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsSnapshotEvaluationProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var snapshotEvaluationProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var snapshotEvaluation = BuildSnapshotEvaluationProjection(performance, diagnostic, previewPacingClassification);");
        AssertContains(snapshotFlatteningText, "var snapshotEvaluationFlattening = BuildSnapshotEvaluationFlattenedProjection(snapshotEvaluation);");
        AssertContains(snapshotFlatteningText, "PerformanceScore = snapshotEvaluationFlattening.PerformanceScore,");
        AssertContains(snapshotFlatteningText, "DiagnosticHealthStatus = snapshotEvaluationFlattening.DiagnosticHealthStatus,");
        AssertContains(snapshotFlatteningText, "PreviewPacingLikelySlowStage = snapshotEvaluationFlattening.PreviewPacingLikelySlowStage,");
        AssertContains(snapshotFlatteningText, "PerformanceThresholdCaptureDropPercent = snapshotEvaluationFlattening.PerformanceThresholdCaptureDropPercent,");
        AssertDoesNotContain(snapshotFlatteningText, "PerformanceScore = performance.Score,");
        AssertDoesNotContain(snapshotFlatteningText, "DiagnosticHealthStatus = diagnostic.HealthStatus,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage");
        AssertDoesNotContain(snapshotFlatteningText, "PerformanceThresholdCaptureDropPercent = _perfectionCaptureDropPercentThreshold,");
        AssertDoesNotContain(snapshotFlatteningText, "PerformanceScore = snapshotEvaluation.PerformanceScore,");
        AssertDoesNotContain(snapshotFlatteningText, "PerformanceThresholdCaptureDropPercent = snapshotEvaluation.PerformanceThresholdCaptureDropPercent,");

        AssertContains(snapshotEvaluationProjectionText, "private static SnapshotEvaluationFlattenedProjection BuildSnapshotEvaluationFlattenedProjection(");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceScore = snapshotEvaluation.PerformanceScore,");
        AssertContains(snapshotEvaluationProjectionText, "DiagnosticHealthStatus = snapshotEvaluation.DiagnosticHealthStatus,");
        AssertContains(snapshotEvaluationProjectionText, "PreviewPacingLikelySlowStage = snapshotEvaluation.PreviewPacingLikelySlowStage,");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceThresholdCaptureDropPercent = snapshotEvaluation.PerformanceThresholdCaptureDropPercent,");
        AssertContains(snapshotEvaluationProjectionText, "private readonly record struct SnapshotEvaluationFlattenedProjection");

        AssertContains(snapshotEvaluationProjectionText, "private SnapshotEvaluationProjection BuildSnapshotEvaluationProjection(");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceScore = performance.Score,");
        AssertContains(snapshotEvaluationProjectionText, "DiagnosticHealthStatus = diagnostic.HealthStatus,");
        AssertContains(snapshotEvaluationProjectionText, "DiagnosticAudioLane = diagnostic.AudioLane,");
        AssertContains(snapshotEvaluationProjectionText, "PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage,");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceThresholdCaptureDropPercent = _perfectionCaptureDropPercentThreshold,");
        AssertContains(snapshotEvaluationProjectionText, "private readonly record struct SnapshotEvaluationProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsProcessResourceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var processResourceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var processResourceProjection = BuildProcessResourceProjection(processResources);");
        AssertContains(snapshotFlatteningText, "var processResourceFlattening = BuildProcessResourceFlattenedProjection(processResourceProjection);");
        AssertContains(snapshotFlatteningText, "MemoryWorkingSetMb = processResourceFlattening.MemoryWorkingSetMb,");
        AssertContains(snapshotFlatteningText, "MemoryGcFragmentationPercent = processResourceFlattening.MemoryGcFragmentationPercent,");
        AssertContains(snapshotFlatteningText, "ThreadPoolIoMax = processResourceFlattening.ThreadPoolIoMax,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryWorkingSetMb = processResourceProjection.MemoryWorkingSetMb,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryGcFragmentationPercent = processResources.MemoryGcFragmentationPercent,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryGcFragmentationPercent = processResourceProjection.MemoryGcFragmentationPercent,");
        AssertDoesNotContain(snapshotFlatteningText, "ThreadPoolIoMax = processResources.ThreadPoolIoMax,");
        AssertDoesNotContain(snapshotFlatteningText, "ThreadPoolIoMax = processResourceProjection.ThreadPoolIoMax,");

        AssertContains(processResourceProjectionText, "private static ProcessResourceProjection BuildProcessResourceProjection(ProcessResourceSnapshot processResources)");
        AssertContains(processResourceProjectionText, "MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,");
        AssertContains(processResourceProjectionText, "MemoryGcFragmentationPercent = processResources.MemoryGcFragmentationPercent,");
        AssertContains(processResourceProjectionText, "ThreadPoolIoMax = processResources.ThreadPoolIoMax");
        AssertContains(processResourceProjectionText, "private readonly record struct ProcessResourceProjection");
        AssertContains(processResourceProjectionText, "private static ProcessResourceFlattenedProjection BuildProcessResourceFlattenedProjection(");
        AssertContains(processResourceProjectionText, "MemoryWorkingSetMb = processResourceProjection.MemoryWorkingSetMb,");
        AssertContains(processResourceProjectionText, "MemoryGcFragmentationPercent = processResourceProjection.MemoryGcFragmentationPercent,");
        AssertContains(processResourceProjectionText, "ThreadPoolIoMax = processResourceProjection.ThreadPoolIoMax");
        AssertContains(processResourceProjectionText, "private readonly record struct ProcessResourceFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsAvSyncProjection_LivesWithProjectionRoot()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var avSyncProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var avSync = BuildAvSyncProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var avSyncFlattening = BuildAvSyncFlattenedProjection(avSync);");
        AssertContains(snapshotFlatteningText, "AvSyncCaptureDriftMs = avSyncFlattening.CaptureDriftMs,");
        AssertContains(snapshotFlatteningText, "AvSyncCaptureDriftRateMsPerSec = avSyncFlattening.CaptureDriftRateMsPerSec,");
        AssertContains(snapshotFlatteningText, "AvSyncEncoderCorrectionSamples = avSyncFlattening.EncoderCorrectionSamples,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncCaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncCaptureDriftMs = avSync.CaptureDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncEncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncEncoderCorrectionSamples = avSync.EncoderCorrectionSamples,");

        AssertContains(avSyncProjectionText, "private static AvSyncFlattenedProjection BuildAvSyncFlattenedProjection(AvSyncProjection avSync)");
        AssertContains(avSyncProjectionText, "CaptureDriftMs = avSync.CaptureDriftMs,");
        AssertContains(avSyncProjectionText, "CaptureDriftRateMsPerSec = avSync.CaptureDriftRateMsPerSec,");
        AssertContains(avSyncProjectionText, "EncoderCorrectionSamples = avSync.EncoderCorrectionSamples");
        AssertContains(avSyncProjectionText, "private readonly record struct AvSyncFlattenedProjection");

        AssertContains(avSyncProjectionText, "private static AvSyncProjection BuildAvSyncProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(avSyncProjectionText, "CaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,");
        AssertContains(avSyncProjectionText, "CaptureDriftRateMsPerSec = captureRuntime.AvSyncCaptureDriftRateMsPerSec,");
        AssertContains(avSyncProjectionText, "EncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples");
        AssertContains(avSyncProjectionText, "private readonly record struct AvSyncProjection");

        return Task.CompletedTask;
    }


    internal static Task AutomationDiagnosticsSnapshotAudioProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var audioProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Audio.cs")
            .Replace("\r\n", "\n");
        var captureIngestProjectionText = audioProjectionText;
        var wasapiAudioProjectionText = audioProjectionText;

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
        AssertDoesNotContain(snapshotFlatteningText, "SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,");
        AssertDoesNotContain(snapshotFlatteningText, "WasapiCaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,");

        AssertContains(audioProjectionText, "private static AudioSignalProjection BuildAudioSignalProjection(");
        AssertContains(audioProjectionText, "Peak = viewModelSnapshot.AudioPeak,");
        AssertContains(audioProjectionText, "SignalPresent = audioSignal.SignalPresent,");
        AssertContains(audioProjectionText, "private readonly record struct AudioSignalProjection");
        AssertContains(audioProjectionText, "private static AudioSignalFlattenedProjection BuildAudioSignalFlattenedProjection(");
        AssertContains(audioProjectionText, "Peak = signal.Peak,");

        AssertContains(audioProjectionText, "private static AudioDropsProjection BuildAudioDropsProjection(CaptureHealthSnapshot health)");
        AssertContains(audioProjectionText, "QueueDropsRealtime = health.AudioDropsQueueSaturated + health.AudioDropsBacklogEviction,");
        AssertContains(audioProjectionText, "QueueDropsFileWriter = health.AudioChunksDropped");
        AssertContains(audioProjectionText, "private readonly record struct AudioDropsProjection");

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


    internal static Task AutomationDiagnosticsCaptureCommandProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var captureCommandProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureCommands = BuildCaptureCommandProjection(viewModelSnapshot);");
        AssertContains(snapshotFlatteningText, "var captureCommandFlattening = BuildCaptureCommandFlattenedProjection(captureCommands);");
        AssertContains(snapshotFlatteningText, "CaptureCommandCommandsEnqueued = captureCommandFlattening.CommandsEnqueued,");
        AssertContains(snapshotFlatteningText, "CaptureCommandMaxQueueLatencyMs = captureCommandFlattening.MaxQueueLatencyMs,");
        AssertContains(snapshotFlatteningText, "CaptureCommandLastError = captureCommandFlattening.LastError,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandCommandsEnqueued = viewModelSnapshot.CaptureCommandCommandsEnqueued,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandCommandsEnqueued = captureCommands.CommandsEnqueued,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandMaxQueueLatencyMs = viewModelSnapshot.CaptureCommandMaxQueueLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandMaxQueueLatencyMs = captureCommands.MaxQueueLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandLastError = viewModelSnapshot.CaptureCommandLastError,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandLastError = captureCommands.LastError,");

        AssertContains(captureCommandProjectionText, "private static CaptureCommandProjection BuildCaptureCommandProjection(ViewModelRuntimeSnapshot viewModelSnapshot)");
        AssertContains(captureCommandProjectionText, "CommandsEnqueued = viewModelSnapshot.CaptureCommandCommandsEnqueued,");
        AssertContains(captureCommandProjectionText, "MaxQueueLatencyMs = viewModelSnapshot.CaptureCommandMaxQueueLatencyMs,");
        AssertContains(captureCommandProjectionText, "LastError = viewModelSnapshot.CaptureCommandLastError");
        AssertContains(captureCommandProjectionText, "private readonly record struct CaptureCommandProjection");
        AssertContains(captureCommandProjectionText, "private static CaptureCommandFlattenedProjection BuildCaptureCommandFlattenedProjection(");
        AssertContains(captureCommandProjectionText, "CommandsEnqueued = captureCommands.CommandsEnqueued,");
        AssertContains(captureCommandProjectionText, "MaxQueueLatencyMs = captureCommands.MaxQueueLatencyMs,");
        AssertContains(captureCommandProjectionText, "LastError = captureCommands.LastError");
        AssertContains(captureCommandProjectionText, "private readonly record struct CaptureCommandFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsUserSettingsProjection_LivesWithSnapshotProjection()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var userSettingsProjectionText = snapshotProjectionText;

        AssertContains(snapshotProjectionText, "var userSettings = BuildUserSettingsProjection(viewModelSnapshot);");
        AssertContains(snapshotProjectionText, "var recordingSettings = BuildRecordingSettingsProjection(userSettings);");
        AssertContains(snapshotFlatteningText, "var settingsFlattening = BuildSettingsFlattenedProjection(userSettings, recordingSettings);");
        AssertContains(snapshotFlatteningText, "SelectedDeviceId = settingsFlattening.SelectedDeviceId,");
        AssertContains(snapshotFlatteningText, "SelectedFriendlyFrameRate = settingsFlattening.SelectedFriendlyFrameRate,");
        AssertContains(snapshotFlatteningText, "SelectedRecordingFormat = settingsFlattening.SelectedRecordingFormat,");
        AssertContains(snapshotFlatteningText, "CustomBitrateMbps = settingsFlattening.CustomBitrateMbps,");
        AssertContains(snapshotFlatteningText, "IsStatsVisible = settingsFlattening.IsStatsVisible,");
        AssertContains(userSettingsProjectionText, "private static SettingsFlattenedProjection BuildSettingsFlattenedProjection(");
        AssertContains(userSettingsProjectionText, "SelectedDeviceId = userSettings.SelectedDeviceId,");
        AssertContains(userSettingsProjectionText, "SelectedFriendlyFrameRate = userSettings.SelectedFriendlyFrameRate,");
        AssertContains(userSettingsProjectionText, "SelectedRecordingFormat = recordingSettings.SelectedRecordingFormat,");
        AssertContains(userSettingsProjectionText, "CustomBitrateMbps = recordingSettings.CustomBitrateMbps,");
        AssertContains(userSettingsProjectionText, "IsStatsVisible = userSettings.IsStatsVisible");
        AssertContains(userSettingsProjectionText, "private readonly record struct SettingsFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedDeviceId = viewModelSnapshot.SelectedDeviceId,");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedDeviceId = userSettings.SelectedDeviceId,");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedFriendlyFrameRate = viewModelSnapshot.SelectedFriendlyFrameRate ?? Math.Round(viewModelSnapshot.SelectedFrameRate),");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedFriendlyFrameRate = userSettings.SelectedFriendlyFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedRecordingFormat = userSettings.SelectedRecordingFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedRecordingFormat = recordingSettings.SelectedRecordingFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "CustomBitrateMbps = userSettings.CustomBitrateMbps,");
        AssertDoesNotContain(snapshotFlatteningText, "CustomBitrateMbps = recordingSettings.CustomBitrateMbps,");
        AssertDoesNotContain(snapshotFlatteningText, "IsStatsVisible = viewModelSnapshot.IsStatsVisible,");
        AssertDoesNotContain(snapshotFlatteningText, "IsStatsVisible = userSettings.IsStatsVisible,");

        AssertContains(userSettingsProjectionText, "private static UserSettingsProjection BuildUserSettingsProjection(ViewModelRuntimeSnapshot viewModelSnapshot)");
        AssertContains(userSettingsProjectionText, "SelectedDeviceId = viewModelSnapshot.SelectedDeviceId,");
        AssertContains(userSettingsProjectionText, "SelectedFriendlyFrameRate = viewModelSnapshot.SelectedFriendlyFrameRate ?? Math.Round(viewModelSnapshot.SelectedFrameRate),");
        AssertContains(userSettingsProjectionText, "SelectedRecordingFormat = viewModelSnapshot.SelectedRecordingFormat,");
        AssertContains(userSettingsProjectionText, "IsStatsVisible = viewModelSnapshot.IsStatsVisible");
        AssertContains(userSettingsProjectionText, "private readonly record struct UserSettingsProjection");
        AssertContains(userSettingsProjectionText, "private static RecordingSettingsProjection BuildRecordingSettingsProjection(UserSettingsProjection userSettings)");
        AssertContains(userSettingsProjectionText, "SelectedRecordingFormat = userSettings.SelectedRecordingFormat,");
        AssertContains(userSettingsProjectionText, "SelectedVideoFormat = userSettings.SelectedVideoFormat,");
        AssertContains(userSettingsProjectionText, "CustomBitrateMbps = userSettings.CustomBitrateMbps");
        AssertContains(userSettingsProjectionText, "private readonly record struct RecordingSettingsProjection");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs")),
            "user settings projection lives with AutomationDiagnosticsHub.SnapshotProjection.cs");

        return Task.CompletedTask;
    }


    internal static Task AutomationDiagnosticsCaptureFormatProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var captureFormatProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureFormat = BuildCaptureFormatProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var captureFormatFlattening = BuildCaptureFormatFlattenedProjection(captureFormat);");
        AssertContains(snapshotFlatteningText, "RequestedWidth = captureFormatFlattening.Requested.Width,");
        AssertContains(snapshotFlatteningText, "HdrActivationReason = captureFormatFlattening.HdrRequest.ActivationReason,");
        AssertContains(snapshotFlatteningText, "NegotiatedWidth = captureFormatFlattening.Negotiated.Width,");
        AssertContains(snapshotFlatteningText, "LatestObservedFramePixelFormat = captureFormatFlattening.ReaderObservation.LatestObservedFramePixelFormat,");
        AssertContains(snapshotFlatteningText, "EncoderVideoCodec = captureFormatFlattening.Encoder.VideoCodec,");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatFlattenedProjection BuildCaptureFormatFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "Requested = BuildCaptureFormatRequestedFlattenedProjection(captureFormat),");
        AssertContains(captureFormatProjectionText, "HdrRequest = BuildCaptureFormatHdrRequestFlattenedProjection(captureFormat),");
        AssertContains(captureFormatProjectionText, "Negotiated = BuildCaptureFormatNegotiatedFlattenedProjection(captureFormat),");
        AssertContains(captureFormatProjectionText, "ReaderObservation = BuildCaptureFormatReaderObservationFlattenedProjection(captureFormat),");
        AssertContains(captureFormatProjectionText, "Encoder = BuildCaptureFormatEncoderFlattenedProjection(captureFormat)");
        AssertContains(captureFormatProjectionText, "private readonly record struct CaptureFormatFlattenedProjection");

        AssertContains(captureFormatProjectionText, "private static CaptureFormatRequestedProjection BuildCaptureFormatRequestedProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureRuntime.RequestedWidth,");
        AssertContains(captureFormatProjectionText, "AudioEnabled = captureRuntime.RequestedAudioEnabled");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatHdrRequestProjection BuildCaptureFormatHdrRequestProjection(");
        AssertContains(captureFormatProjectionText, "ActivationReason = captureRuntime.HdrActivationReason,");
        AssertContains(captureFormatProjectionText, "RequestedButSourceNot10Bit = captureRuntime.HdrRequestedButSourceNot10Bit");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatActualProjection BuildCaptureFormatActualProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureRuntime.ActualWidth,");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatNegotiatedProjection BuildCaptureFormatNegotiatedProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,");
        AssertContains(captureFormatProjectionText, "MediaSubtypeToken = captureRuntime.NegotiatedMediaSubtypeToken");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatReaderObservationProjection BuildCaptureFormatReaderObservationProjection(");
        AssertContains(captureFormatProjectionText, "LatestObservedFramePixelFormat = captureRuntime.LatestObservedFramePixelFormat,");
        AssertContains(captureFormatProjectionText, "MfReadwriteDisableConverters = captureRuntime.MfReadwriteDisableConverters");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatEncoderProjection BuildCaptureFormatEncoderProjection(");
        AssertContains(captureFormatProjectionText, "VideoCodec = captureRuntime.EncoderVideoCodec,");
        AssertContains(captureFormatProjectionText, "TenBitPipelineConfirmed = captureRuntime.EncoderTenBitPipelineConfirmed");

        AssertContains(captureFormatProjectionText, "private static CaptureFormatRequestedFlattenedProjection BuildCaptureFormatRequestedFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureFormat.Requested.Width,");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatHdrRequestFlattenedProjection BuildCaptureFormatHdrRequestFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "ActivationReason = captureFormat.HdrRequest.ActivationReason,");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatActualFlattenedProjection BuildCaptureFormatActualFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureFormat.Actual.Width,");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatNegotiatedFlattenedProjection BuildCaptureFormatNegotiatedFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureFormat.Negotiated.Width,");
        AssertContains(captureFormatProjectionText, "MediaSubtypeToken = captureFormat.Negotiated.MediaSubtypeToken");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatReaderObservationFlattenedProjection BuildCaptureFormatReaderObservationFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "LatestObservedFramePixelFormat = captureFormat.ReaderObservation.LatestObservedFramePixelFormat,");
        AssertContains(captureFormatProjectionText, "MfReadwriteDisableConverters = captureFormat.ReaderObservation.MfReadwriteDisableConverters");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatEncoderFlattenedProjection BuildCaptureFormatEncoderFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "VideoCodec = captureFormat.Encoder.VideoCodec,");
        AssertContains(captureFormatProjectionText, "TenBitPipelineConfirmed = captureFormat.Encoder.TenBitPipelineConfirmed");
        AssertDoesNotContain(snapshotFlatteningText, "RequestedWidth = captureRuntime.RequestedWidth,");
        AssertDoesNotContain(snapshotFlatteningText, "RequestedWidth = captureFormat.RequestedWidth,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrActivationReason = captureRuntime.HdrActivationReason,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrActivationReason = captureFormat.HdrActivationReason,");
        AssertDoesNotContain(snapshotFlatteningText, "NegotiatedWidth = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,");
        AssertDoesNotContain(snapshotFlatteningText, "NegotiatedWidth = captureFormat.NegotiatedWidth,");
        AssertDoesNotContain(snapshotFlatteningText, "LatestObservedFramePixelFormat = captureRuntime.LatestObservedFramePixelFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "LatestObservedFramePixelFormat = captureFormat.LatestObservedFramePixelFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "EncoderVideoCodec = captureRuntime.EncoderVideoCodec,");
        AssertDoesNotContain(snapshotFlatteningText, "EncoderVideoCodec = captureFormat.EncoderVideoCodec,");

        AssertContains(captureFormatProjectionText, "private static CaptureFormatProjection BuildCaptureFormatProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(captureFormatProjectionText, "private readonly record struct CaptureFormatProjection");
        AssertContains(captureFormatProjectionText, "Requested = BuildCaptureFormatRequestedProjection(captureRuntime),");
        AssertContains(captureFormatProjectionText, "HdrRequest = BuildCaptureFormatHdrRequestProjection(captureRuntime),");
        AssertContains(captureFormatProjectionText, "Actual = BuildCaptureFormatActualProjection(captureRuntime),");
        AssertContains(captureFormatProjectionText, "Negotiated = BuildCaptureFormatNegotiatedProjection(captureRuntime),");
        AssertContains(captureFormatProjectionText, "ReaderObservation = BuildCaptureFormatReaderObservationProjection(captureRuntime),");
        AssertContains(captureFormatProjectionText, "Encoder = BuildCaptureFormatEncoderProjection(captureRuntime)");
        AssertContains(captureFormatProjectionText, "public CaptureFormatRequestedProjection Requested { get; init; }");
        AssertContains(captureFormatProjectionText, "public CaptureFormatEncoderProjection Encoder { get; init; }");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsCaptureTransportProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var captureTransportProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureTransport = BuildCaptureTransportProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var captureTransportFlattening = BuildCaptureTransportFlattenedProjection(captureTransport);");
        AssertContains(snapshotFlatteningText, "MemoryPreference = captureTransportFlattening.MemoryPreference,");
        AssertContains(snapshotFlatteningText, "VideoNegotiatedSubtype = captureTransportFlattening.VideoNegotiatedSubtype,");
        AssertContains(snapshotFlatteningText, "FrameLedgerRecentEvents = captureTransportFlattening.FrameLedgerRecentEvents,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryPreference = captureRuntime.MemoryPreference,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryPreference = captureTransport.MemoryPreference,");
        AssertDoesNotContain(snapshotFlatteningText, "FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents,");
        AssertDoesNotContain(snapshotFlatteningText, "FrameLedgerRecentEvents = captureTransport.FrameLedgerRecentEvents,");

        AssertContains(captureTransportProjectionText, "private static CaptureTransportProjection BuildCaptureTransportProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(captureTransportProjectionText, "MemoryPreference = captureRuntime.MemoryPreference,");
        AssertContains(captureTransportProjectionText, "VideoNegotiatedSubtype = captureRuntime.VideoNegotiatedSubtype,");
        AssertContains(captureTransportProjectionText, "FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents");
        AssertContains(captureTransportProjectionText, "private readonly record struct CaptureTransportProjection");
        AssertContains(captureTransportProjectionText, "private static CaptureTransportFlattenedProjection BuildCaptureTransportFlattenedProjection(");
        AssertContains(captureTransportProjectionText, "MemoryPreference = captureTransport.MemoryPreference,");
        AssertContains(captureTransportProjectionText, "VideoNegotiatedSubtype = captureTransport.VideoNegotiatedSubtype,");
        AssertContains(captureTransportProjectionText, "FrameLedgerRecentEvents = captureTransport.FrameLedgerRecentEvents");
        AssertContains(captureTransportProjectionText, "private readonly record struct CaptureTransportFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsHdrPipelineProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var hdrPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Hdr.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var hdrPipeline = BuildHdrPipelineProjection(viewModelSnapshot, captureRuntime, hdrTruthVerdict);");
        AssertContains(snapshotFlatteningText, "var hdrPipelineFlattening = BuildHdrPipelineFlattenedProjection(hdrPipeline);");
        AssertContains(snapshotFlatteningText, "IsHdrAvailable = hdrPipelineFlattening.IsHdrAvailable,");
        AssertContains(snapshotFlatteningText, "HdrRuntimeState = hdrPipelineFlattening.HdrRuntimeState,");
        AssertContains(snapshotFlatteningText, "HdrWarmupObservedNonP010Frames = hdrPipelineFlattening.HdrWarmupObservedNonP010Frames,");
        AssertContains(snapshotFlatteningText, "PipelineModeStatus = hdrPipelineFlattening.PipelineModeStatus,");
        AssertContains(snapshotFlatteningText, "TelemetryAlignmentReason = hdrPipelineFlattening.TelemetryAlignmentReason,");
        AssertContains(snapshotFlatteningText, "HdrTruthVerdict = hdrPipelineFlattening.TruthVerdict,");
        AssertContains(hdrPipelineProjectionText, "private static HdrPipelineFlattenedProjection BuildHdrPipelineFlattenedProjection(");
        AssertContains(hdrPipelineProjectionText, "IsHdrAvailable = hdrPipeline.IsHdrAvailable,");
        AssertContains(hdrPipelineProjectionText, "HdrRuntimeState = hdrPipeline.HdrRuntimeState,");
        AssertContains(hdrPipelineProjectionText, "HdrWarmupObservedNonP010Frames = hdrPipeline.HdrWarmupObservedNonP010Frames,");
        AssertContains(hdrPipelineProjectionText, "PipelineModeStatus = hdrPipeline.PipelineModeStatus,");
        AssertContains(hdrPipelineProjectionText, "TelemetryAlignmentReason = hdrPipeline.TelemetryAlignmentReason,");
        AssertContains(hdrPipelineProjectionText, "TruthVerdict = hdrPipeline.TruthVerdict");
        AssertContains(hdrPipelineProjectionText, "private readonly record struct HdrPipelineFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "IsHdrAvailable = viewModelSnapshot.IsHdrAvailable,");
        AssertDoesNotContain(snapshotFlatteningText, "IsHdrAvailable = hdrPipeline.IsHdrAvailable,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrRuntimeState = !string.IsNullOrWhiteSpace(viewModelSnapshot.HdrRuntimeState)");
        AssertDoesNotContain(snapshotFlatteningText, "HdrRuntimeState = hdrPipeline.HdrRuntimeState,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrWarmupObservedNonP010Frames = captureRuntime.HdrWarmupObservedNonP010Frames,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrWarmupObservedNonP010Frames = hdrPipeline.HdrWarmupObservedNonP010Frames,");
        AssertDoesNotContain(snapshotFlatteningText, "PipelineModeStatus = captureRuntime.PipelineModeStatus,");
        AssertDoesNotContain(snapshotFlatteningText, "PipelineModeStatus = hdrPipeline.PipelineModeStatus,");
        AssertDoesNotContain(snapshotFlatteningText, "TelemetryAlignmentReason = captureRuntime.TelemetryAlignmentReason,");
        AssertDoesNotContain(snapshotFlatteningText, "TelemetryAlignmentReason = hdrPipeline.TelemetryAlignmentReason,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrTruthVerdict = hdrPipeline.TruthVerdict,");

        AssertContains(hdrPipelineProjectionText, "private static HdrPipelineProjection BuildHdrPipelineProjection(");
        AssertContains(hdrPipelineProjectionText, "IsHdrAvailable = viewModelSnapshot.IsHdrAvailable,");
        AssertContains(hdrPipelineProjectionText, "HdrRuntimeState = PreferViewModelHdrText(viewModelSnapshot.HdrRuntimeState, captureRuntime.HdrRuntimeState),");
        AssertContains(hdrPipelineProjectionText, "HdrWarmupObservedNonP010Frames = captureRuntime.HdrWarmupObservedNonP010Frames,");
        AssertContains(hdrPipelineProjectionText, "PipelineModeStatus = captureRuntime.PipelineModeStatus,");
        AssertContains(hdrPipelineProjectionText, "TelemetryAlignmentReason = captureRuntime.TelemetryAlignmentReason,");
        AssertContains(hdrPipelineProjectionText, "TruthVerdict = truthVerdict");
        AssertContains(hdrPipelineProjectionText, "private static string PreferViewModelHdrText(string viewModelValue, string runtimeValue)");
        AssertContains(hdrPipelineProjectionText, "private readonly record struct HdrPipelineProjection");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs")),
            "HDR pipeline projection partial folded into HDR diagnostics owner");

        return Task.CompletedTask;
    }


    internal static Task AutomationDiagnosticsSourceTelemetryProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var sourceSignalProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs")
            .Replace("\r\n", "\n");
        var sourceTelemetryProjectionText = sourceSignalProjectionText;

        AssertContains(snapshotProjectionText, "var sourceTelemetry = BuildSourceTelemetryProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var sourceFlattening = BuildSourceFlattenedProjection(sourceSignal, sourceTelemetry);");
        AssertContains(snapshotFlatteningText, "SourceTelemetryAvailability = sourceFlattening.Telemetry.SourceTelemetryAvailability,");
        AssertContains(snapshotFlatteningText, "SourceTelemetryDetails = sourceFlattening.Telemetry.SourceTelemetryDetails,");
        AssertContains(snapshotFlatteningText, "SourceTelemetryAgeSeconds = sourceFlattening.Telemetry.SourceTelemetryAgeSeconds,");
        AssertContains(snapshotFlatteningText, "SourceTargetSummaryText = sourceFlattening.Telemetry.SourceTargetSummaryText,");
        AssertContains(sourceSignalProjectionText, "private static SourceFlattenedProjection BuildSourceFlattenedProjection(");
        AssertContains(sourceSignalProjectionText, "Telemetry = BuildSourceTelemetryFlattenedProjection(sourceTelemetry)");
        AssertContains(sourceSignalProjectionText, "private readonly record struct SourceFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAvailability = !string.IsNullOrWhiteSpace(viewModelSnapshot.SourceTelemetryAvailability)");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryDetails = captureRuntime.SourceTelemetryDetails,");

        AssertContains(sourceTelemetryProjectionText, "private static SourceTelemetryProjection BuildSourceTelemetryProjection(");
        AssertContains(sourceTelemetryProjectionText, "private static string PreferKnownTelemetryValue(string viewModelValue, string runtimeValue)");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryAvailability = PreferKnownTelemetryValue(");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryDetails = captureRuntime.SourceTelemetryDetails,");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(");
        AssertContains(sourceTelemetryProjectionText, "private readonly record struct SourceTelemetryProjection");
        AssertContains(sourceTelemetryProjectionText, "private static SourceTelemetryFlattenedProjection BuildSourceTelemetryFlattenedProjection(");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,");
        AssertContains(sourceTelemetryProjectionText, "SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText");
        AssertContains(sourceTelemetryProjectionText, "private readonly record struct SourceTelemetryFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsSourceSignalProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var sourceSignalProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var sourceSignal = BuildSourceSignalProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var sourceFlattening = BuildSourceFlattenedProjection(sourceSignal, sourceTelemetry);");
        AssertContains(snapshotFlatteningText, "DetectedSourceFrameRate = sourceFlattening.Signal.DetectedSourceFrameRate,");
        AssertContains(snapshotFlatteningText, "SourceFrameRateOrigin = sourceFlattening.Signal.SourceFrameRateOrigin,");
        AssertContains(snapshotFlatteningText, "SourceRawTimingHex = sourceFlattening.Signal.SourceRawTimingHex,");
        AssertContains(sourceSignalProjectionText, "private static SourceFlattenedProjection BuildSourceFlattenedProjection(");
        AssertContains(sourceSignalProjectionText, "Signal = BuildSourceSignalFlattenedProjection(sourceSignal),");
        AssertContains(sourceSignalProjectionText, "private static SourceSignalFlattenedProjection BuildSourceSignalFlattenedProjection(");
        AssertContains(sourceSignalProjectionText, "DetectedSourceFrameRate = sourceSignal.DetectedFrameRate,");
        AssertContains(sourceSignalProjectionText, "SourceFrameRateOrigin = sourceSignal.FrameRateOrigin,");
        AssertContains(sourceSignalProjectionText, "SourceRawTimingHex = sourceSignal.RawTimingHex");
        AssertContains(sourceSignalProjectionText, "private readonly record struct SourceSignalFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "DetectedSourceFrameRate = sourceSignal.DetectedFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceFrameRateOrigin = sourceSignal.FrameRateOrigin,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceRawTimingHex = sourceSignal.RawTimingHex,");
        AssertDoesNotContain(snapshotFlatteningText, "DetectedSourceFrameRate = viewModelSnapshot.DetectedSourceFrameRate ?? captureRuntime.DetectedSourceFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceRawTimingHex = captureRuntime.SourceRawTimingHex,");

        AssertContains(sourceSignalProjectionText, "private static SourceSignalProjection BuildSourceSignalProjection(");
        AssertContains(sourceSignalProjectionText, "DetectedFrameRate = viewModelSnapshot.DetectedSourceFrameRate ?? captureRuntime.DetectedSourceFrameRate,");
        AssertContains(sourceSignalProjectionText, "FrameRateOrigin = ResolveSourceFrameRateOrigin(viewModelSnapshot.SourceFrameRateOrigin, captureRuntime.SourceFrameRateOrigin),");
        AssertContains(sourceSignalProjectionText, "RawTimingHex = captureRuntime.SourceRawTimingHex");
        AssertContains(sourceSignalProjectionText, "private static string ResolveSourceFrameRateOrigin(string viewModelOrigin, string runtimeOrigin)");
        AssertContains(sourceSignalProjectionText, "private readonly record struct SourceSignalProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsCaptureCadenceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var captureCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs")
            .Replace("\r\n", "\n");
        var captureCadenceOnlyText = captureCadenceProjectionText[..captureCadenceProjectionText.IndexOf("private static VisualCadenceProjection", System.StringComparison.Ordinal)];

        AssertContains(snapshotProjectionText, "var captureCadence = BuildCaptureCadenceProjection(health);");
        AssertContains(snapshotFlatteningText, "var captureCadenceFlattening = BuildCaptureCadenceFlattenedProjection(captureCadence);");
        AssertContains(snapshotFlatteningText, "ExpectedCaptureFrameRate = captureCadenceFlattening.ExpectedFrameRate,");
        AssertContains(snapshotFlatteningText, "CaptureCadenceEstimatedDroppedFrames = captureCadenceFlattening.EstimatedDroppedFrames,");
        AssertDoesNotContain(snapshotFlatteningText, "ExpectedCaptureFrameRate = health.ExpectedFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCadenceEstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,");
        AssertDoesNotContain(snapshotFlatteningText, "ExpectedCaptureFrameRate = captureCadence.ExpectedFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCadenceEstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames,");

        AssertContains(captureCadenceProjectionText, "private static CaptureCadenceProjection BuildCaptureCadenceProjection(CaptureHealthSnapshot health)");
        AssertContains(captureCadenceProjectionText, "ExpectedFrameRate = health.ExpectedFrameRate,");
        AssertContains(captureCadenceProjectionText, "EstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,");
        AssertContains(captureCadenceProjectionText, "private readonly record struct CaptureCadenceProjection");
        AssertContains(captureCadenceProjectionText, "private static CaptureCadenceFlattenedProjection BuildCaptureCadenceFlattenedProjection(");
        AssertContains(captureCadenceProjectionText, "ExpectedFrameRate = captureCadence.ExpectedFrameRate,");
        AssertContains(captureCadenceProjectionText, "EstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames");
        AssertContains(captureCadenceProjectionText, "private readonly record struct CaptureCadenceFlattenedProjection");
        AssertDoesNotContain(captureCadenceOnlyText, "VisualMotionConfidence");
        AssertDoesNotContain(captureCadenceOnlyText, "VisualCenterRecentChangeIntervalsMs");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsVisualCadenceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var visualCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs")
            .Replace("\r\n", "\n");
        var captureCadenceOnlyText = visualCadenceProjectionText[..visualCadenceProjectionText.IndexOf("private static VisualCadenceProjection", System.StringComparison.Ordinal)];

        AssertContains(snapshotProjectionText, "var visualCadence = BuildVisualCadenceProjection(health);");
        AssertContains(snapshotFlatteningText, "var visualCadenceFlattening = BuildVisualCadenceFlattenedProjection(visualCadence);");
        AssertContains(snapshotFlatteningText, "VisualCadenceMotionConfidence = visualCadenceFlattening.MotionConfidence,");
        AssertContains(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = visualCadenceFlattening.CenterRecentChangeIntervalsMs,");
        AssertContains(visualCadenceProjectionText, "private static VisualCadenceFlattenedProjection BuildVisualCadenceFlattenedProjection(");
        AssertContains(visualCadenceProjectionText, "MotionConfidence = visualCadence.MotionConfidence,");
        AssertContains(visualCadenceProjectionText, "CenterRecentChangeIntervalsMs = visualCadence.CenterRecentChangeIntervalsMs");
        AssertContains(visualCadenceProjectionText, "private readonly record struct VisualCadenceFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCadenceMotionConfidence = captureCadence.VisualMotionConfidence,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = captureCadence.VisualCenterRecentChangeIntervalsMs,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCadenceMotionConfidence = health.VisualCadenceMotionConfidence,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCadenceMotionConfidence = visualCadence.MotionConfidence,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = visualCadence.CenterRecentChangeIntervalsMs,");

        AssertContains(visualCadenceProjectionText, "private static VisualCadenceProjection BuildVisualCadenceProjection(CaptureHealthSnapshot health)");
        AssertContains(visualCadenceProjectionText, "MotionConfidence = health.VisualCadenceMotionConfidence,");
        AssertContains(visualCadenceProjectionText, "CenterRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs");
        AssertContains(visualCadenceProjectionText, "private readonly record struct VisualCadenceProjection");
        AssertDoesNotContain(captureCadenceOnlyText, "VisualCadenceMotionConfidence");
        AssertDoesNotContain(captureCadenceOnlyText, "VisualCenterCadenceRecentChangeIntervalsMs");

        return Task.CompletedTask;
    }


    internal static Task AutomationDiagnosticsMjpegProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var mjpegProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs")
            .Replace("\r\n", "\n");
        var mjpegPreviewJitterProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var mjpeg = BuildMjpegProjection(health);");
        AssertContains(snapshotFlatteningText, "var mjpegFlattening = BuildMjpegFlattenedProjection(mjpeg);");
        AssertContains(snapshotFlatteningText, "MjpegTotalDecoded = mjpegFlattening.TotalDecoded,");
        AssertContains(snapshotFlatteningText, "var mjpegTimingFlattening = BuildMjpegTimingFlattenedProjection(mjpeg.Timing);");
        AssertContains(snapshotFlatteningText, "MjpegDecodeSampleCount = mjpegTimingFlattening.DecodeSampleCount,");
        AssertContains(snapshotFlatteningText, "var mjpegPreviewJitterFlattening = BuildMjpegPreviewJitterFlattenedProjection(mjpeg.PreviewJitter);");
        AssertContains(snapshotFlatteningText, "MjpegPreviewJitterLastDropReason = mjpegPreviewJitterFlattening.Events.LastDropReason,");
        AssertContains(snapshotFlatteningText, "var mjpegPacketHashFlattening = BuildMjpegPacketHashFlattenedProjection(mjpeg.PacketHash);");
        AssertContains(snapshotFlatteningText, "MjpegPacketHashPattern = mjpegPacketHashFlattening.Pattern,");
        AssertContains(snapshotFlatteningText, "MjpegPerDecoder = mjpegTimingFlattening.PerDecoder,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegDecodeSampleCount = health.MjpegDecodeSampleCount,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegTotalDecoded = mjpeg.TotalDecoded,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegCompressedQueueByteBudget = mjpeg.CompressedQueueByteBudget,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegDecodeSampleCount = mjpeg.DecodeSampleCount,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegDecodeSampleCount = mjpeg.Timing.DecodeSampleCount,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPreviewJitterLastDropReason = mjpeg.PreviewJitterLastDropReason,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPreviewJitterLastDropReason = mjpeg.PreviewJitter.LastDropReason,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPreviewJitterLastDropReason = health.MjpegPreviewJitterLastDropReason,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPacketHashPattern = mjpeg.PacketHashPattern,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPacketHashPattern = mjpeg.PacketHash.Pattern,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPacketHashPattern = health.MjpegPacketHashPattern,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPerDecoder = mjpeg.PerDecoder,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPerDecoder = mjpeg.Timing.PerDecoder,");
        AssertDoesNotContain(snapshotFlatteningText, "MjpegPerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder");

        AssertContains(mjpegProjectionText, "private static MjpegProjection BuildMjpegProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegProjectionText, "var timing = BuildMjpegTimingProjection(health);");
        AssertContains(mjpegProjectionText, "Timing = timing,");
        AssertContains(mjpegProjectionText, "var previewJitter = BuildMjpegPreviewJitterProjection(health);");
        AssertContains(mjpegProjectionText, "var packetHash = BuildMjpegPacketHashProjection(health);");
        AssertContains(mjpegProjectionText, "PreviewJitter = previewJitter,");
        AssertDoesNotContain(mjpegProjectionText, "PreviewJitterLastDropReason = health.MjpegPreviewJitterLastDropReason,");
        AssertContains(mjpegProjectionText, "PacketHash = packetHash,");
        AssertDoesNotContain(mjpegProjectionText, "PacketHashPattern = health.MjpegPacketHashPattern,");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegProjection");
        AssertContains(mjpegProjectionText, "private static MjpegFlattenedProjection BuildMjpegFlattenedProjection(");
        AssertContains(mjpegProjectionText, "TotalDecoded = mjpeg.TotalDecoded,");
        AssertContains(mjpegProjectionText, "CompressedQueueByteBudget = mjpeg.CompressedQueueByteBudget,");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegFlattenedProjection");
        AssertContains(mjpegProjectionText, "private static MjpegTimingProjection BuildMjpegTimingProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegProjectionText, "DecodeSampleCount = health.MjpegDecodeSampleCount,");
        AssertContains(mjpegProjectionText, "PipelineMaxMs = health.MjpegPipelineMaxMs,");
        AssertContains(mjpegProjectionText, "PerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegTimingProjection");
        AssertContains(mjpegProjectionText, "private static MjpegTimingFlattenedProjection BuildMjpegTimingFlattenedProjection(");
        AssertContains(mjpegProjectionText, "DecodeSampleCount = timing.DecodeSampleCount,");
        AssertContains(mjpegProjectionText, "PipelineMaxMs = timing.PipelineMaxMs,");
        AssertContains(mjpegProjectionText, "PerDecoder = timing.PerDecoder");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegTimingFlattenedProjection");

        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterProjection BuildMjpegPreviewJitterProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegPreviewJitterProjectionText, "Queue = BuildMjpegPreviewJitterQueueProjection(health),");
        AssertContains(mjpegPreviewJitterProjectionText, "Timing = BuildMjpegPreviewJitterTimingProjection(health),");
        AssertContains(mjpegPreviewJitterProjectionText, "Adaptive = BuildMjpegPreviewJitterAdaptiveProjection(health),");
        AssertContains(mjpegPreviewJitterProjectionText, "Events = BuildMjpegPreviewJitterEventProjection(health)");
        AssertContains(mjpegPreviewJitterProjectionText, "private readonly record struct MjpegPreviewJitterProjection");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterFlattenedProjection BuildMjpegPreviewJitterFlattenedProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "Queue = BuildMjpegPreviewJitterQueueFlattenedProjection(previewJitter.Queue),");
        AssertContains(mjpegPreviewJitterProjectionText, "Timing = BuildMjpegPreviewJitterTimingFlattenedProjection(previewJitter.Timing),");
        AssertContains(mjpegPreviewJitterProjectionText, "Adaptive = BuildMjpegPreviewJitterAdaptiveFlattenedProjection(previewJitter.Adaptive),");
        AssertContains(mjpegPreviewJitterProjectionText, "Events = BuildMjpegPreviewJitterEventFlattenedProjection(previewJitter.Events)");
        AssertContains(mjpegPreviewJitterProjectionText, "private readonly record struct MjpegPreviewJitterFlattenedProjection");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterQueueProjection BuildMjpegPreviewJitterQueueProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "Enabled = health.MjpegPreviewJitterEnabled,");
        AssertContains(mjpegPreviewJitterProjectionText, "ResumeReprimeCount = health.MjpegPreviewJitterResumeReprimeCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterQueueFlattenedProjection BuildMjpegPreviewJitterQueueFlattenedProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "Enabled = queue.Enabled,");
        AssertContains(mjpegPreviewJitterProjectionText, "ResumeReprimeCount = queue.ResumeReprimeCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterTimingProjection BuildMjpegPreviewJitterTimingProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "InputSampleCount = health.MjpegPreviewJitterInputSampleCount,");
        AssertContains(mjpegPreviewJitterProjectionText, "LatencyMaxMs = health.MjpegPreviewJitterLatencyMaxMs");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterTimingFlattenedProjection BuildMjpegPreviewJitterTimingFlattenedProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "InputSampleCount = timing.InputSampleCount,");
        AssertContains(mjpegPreviewJitterProjectionText, "LatencyMaxMs = timing.LatencyMaxMs");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterAdaptiveProjection BuildMjpegPreviewJitterAdaptiveProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "DeadlineDropCount = health.MjpegPreviewJitterDeadlineDropCount,");
        AssertContains(mjpegPreviewJitterProjectionText, "TargetDecreaseCount = health.MjpegPreviewJitterTargetDecreaseCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterAdaptiveFlattenedProjection BuildMjpegPreviewJitterAdaptiveFlattenedProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "DeadlineDropCount = adaptive.DeadlineDropCount,");
        AssertContains(mjpegPreviewJitterProjectionText, "TargetDecreaseCount = adaptive.TargetDecreaseCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterEventProjection BuildMjpegPreviewJitterEventProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "LastDropReason = health.MjpegPreviewJitterLastDropReason,");
        AssertContains(mjpegPreviewJitterProjectionText, "ScheduleLateCount = health.MjpegPreviewJitterScheduleLateCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterEventFlattenedProjection BuildMjpegPreviewJitterEventFlattenedProjection(");
        AssertContains(mjpegPreviewJitterProjectionText, "LastDropReason = events.LastDropReason,");
        AssertContains(mjpegPreviewJitterProjectionText, "ScheduleLateCount = events.ScheduleLateCount");

        AssertContains(mjpegProjectionText, "private static MjpegPacketHashProjection BuildMjpegPacketHashProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegProjectionText, "SampleCount = health.MjpegPacketHashSampleCount,");
        AssertContains(mjpegProjectionText, "Pattern = health.MjpegPacketHashPattern,");
        AssertContains(mjpegProjectionText, "RecentDuplicateFlags = health.MjpegPacketHashRecentDuplicateFlags");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegPacketHashProjection");
        AssertContains(mjpegProjectionText, "private static MjpegPacketHashFlattenedProjection BuildMjpegPacketHashFlattenedProjection(");
        AssertContains(mjpegProjectionText, "SampleCount = packetHash.SampleCount,");
        AssertContains(mjpegProjectionText, "Pattern = packetHash.Pattern,");
        AssertContains(mjpegProjectionText, "RecentDuplicateFlags = packetHash.RecentDuplicateFlags");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegPacketHashFlattenedProjection");

        return Task.CompletedTask;
    }


    internal static Task AutomationDiagnosticsRecordingPipelineProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var recordingPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var recordingPipeline = BuildRecordingPipelineProjection(health);");
        AssertContains(snapshotFlatteningText, "var recordingPipelineFlattening = BuildRecordingPipelineFlattenedProjection(recordingPipeline);");
        AssertContains(snapshotFlatteningText, "EncoderVideoFramesEnqueued = recordingPipelineFlattening.Encoder.VideoFramesEnqueued,");
        AssertContains(snapshotFlatteningText, "ConversionQueueDepth = recordingPipelineFlattening.Ingest.ConversionQueueDepth,");
        AssertContains(snapshotFlatteningText, "RecordingVideoQueueCapacity = recordingPipelineFlattening.VideoQueue.Capacity,");
        AssertContains(snapshotFlatteningText, "RecordingGpuFramesEnqueued = recordingPipelineFlattening.HardwareQueues.GpuFramesEnqueued,");
        AssertContains(snapshotFlatteningText, "RecordingCudaFramesDropped = recordingPipelineFlattening.HardwareQueues.CudaFramesDropped,");
        AssertDoesNotContain(snapshotFlatteningText, "EncoderVideoFramesEnqueued = health.VideoFramesEnqueued,");
        AssertDoesNotContain(snapshotFlatteningText, "ConversionQueueDepth = health.ConversionQueueDepth,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingVideoQueueCapacity = health.RecordingVideoQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingCudaFramesDropped = health.RecordingCudaFramesDropped,");
        AssertDoesNotContain(snapshotFlatteningText, "EncoderVideoFramesEnqueued = recordingPipeline.EncoderVideoFramesEnqueued,");
        AssertDoesNotContain(snapshotFlatteningText, "ConversionQueueDepth = recordingPipeline.ConversionQueueDepth,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingVideoQueueCapacity = recordingPipeline.RecordingVideoQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingCudaFramesDropped = recordingPipeline.RecordingCudaFramesDropped,");

        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineFlattenedProjection BuildRecordingPipelineFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "Encoder = BuildRecordingPipelineEncoderFlattenedProjection(recordingPipeline),");
        AssertContains(recordingPipelineProjectionText, "Ingest = BuildRecordingPipelineIngestFlattenedProjection(recordingPipeline),");
        AssertContains(recordingPipelineProjectionText, "VideoQueue = BuildRecordingPipelineVideoQueueFlattenedProjection(recordingPipeline),");
        AssertContains(recordingPipelineProjectionText, "HardwareQueues = BuildRecordingPipelineHardwareQueuesFlattenedProjection(recordingPipeline)");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingPipelineFlattenedProjection");

        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineEncoderProjection BuildRecordingPipelineEncoderProjection(");
        AssertContains(recordingPipelineProjectionText, "VideoFramesEnqueued = health.VideoFramesEnqueued,");
        AssertContains(recordingPipelineProjectionText, "EncodingFailed = health.RecordingEncodingFailed,");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineIngestProjection BuildRecordingPipelineIngestProjection(");
        AssertContains(recordingPipelineProjectionText, "ConversionQueueDepth = health.ConversionQueueDepth,");
        AssertContains(recordingPipelineProjectionText, "VideoDropsBacklogEviction = health.VideoDropsBacklogEviction");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineVideoQueueProjection BuildRecordingPipelineVideoQueueProjection(");
        AssertContains(recordingPipelineProjectionText, "Capacity = health.RecordingVideoQueueCapacity,");
        AssertContains(recordingPipelineProjectionText, "BackpressureMaxWaitMs = health.RecordingVideoBackpressureMaxWaitMs");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineHardwareQueuesProjection BuildRecordingPipelineHardwareQueuesProjection(");
        AssertContains(recordingPipelineProjectionText, "GpuFramesEnqueued = health.RecordingGpuFramesEnqueued,");
        AssertContains(recordingPipelineProjectionText, "CudaFramesDropped = health.RecordingCudaFramesDropped");

        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineEncoderFlattenedProjection BuildRecordingPipelineEncoderFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "VideoFramesEnqueued = recordingPipeline.Encoder.VideoFramesEnqueued,");
        AssertContains(recordingPipelineProjectionText, "EncodingFailed = recordingPipeline.Encoder.EncodingFailed,");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineIngestFlattenedProjection BuildRecordingPipelineIngestFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "ConversionQueueDepth = recordingPipeline.Ingest.ConversionQueueDepth,");
        AssertContains(recordingPipelineProjectionText, "VideoDropsBacklogEviction = recordingPipeline.Ingest.VideoDropsBacklogEviction");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineVideoQueueFlattenedProjection BuildRecordingPipelineVideoQueueFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "Capacity = recordingPipeline.VideoQueue.Capacity,");
        AssertContains(recordingPipelineProjectionText, "BackpressureMaxWaitMs = recordingPipeline.VideoQueue.BackpressureMaxWaitMs");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineHardwareQueuesFlattenedProjection BuildRecordingPipelineHardwareQueuesFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "GpuFramesEnqueued = recordingPipeline.HardwareQueues.GpuFramesEnqueued,");
        AssertContains(recordingPipelineProjectionText, "CudaFramesDropped = recordingPipeline.HardwareQueues.CudaFramesDropped");

        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineProjection BuildRecordingPipelineProjection(CaptureHealthSnapshot health)");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingPipelineProjection");
        AssertContains(recordingPipelineProjectionText, "Encoder = BuildRecordingPipelineEncoderProjection(health),");
        AssertContains(recordingPipelineProjectionText, "Ingest = BuildRecordingPipelineIngestProjection(health),");
        AssertContains(recordingPipelineProjectionText, "VideoQueue = BuildRecordingPipelineVideoQueueProjection(health),");
        AssertContains(recordingPipelineProjectionText, "HardwareQueues = BuildRecordingPipelineHardwareQueuesProjection(health)");
        AssertContains(recordingPipelineProjectionText, "public RecordingPipelineEncoderProjection Encoder { get; init; }");
        AssertContains(recordingPipelineProjectionText, "public RecordingPipelineHardwareQueuesProjection HardwareQueues { get; init; }");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsRecordingBackendProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var recordingPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var recordingBackend = BuildRecordingBackendProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var recordingOutputFlattening = BuildRecordingOutputFlattenedProjection(recordingBackend, recordingOutput);");
        AssertContains(snapshotFlatteningText, "RecordingBackend = recordingOutputFlattening.Backend,");
        AssertContains(snapshotFlatteningText, "AudioPathMode = recordingOutputFlattening.AudioPathMode,");
        AssertContains(snapshotFlatteningText, "MuxResult = recordingOutputFlattening.MuxResult,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingBackend = captureRuntime.RecordingBackend,");
        AssertDoesNotContain(snapshotFlatteningText, "MuxResult = captureRuntime.MuxSucceeded.HasValue");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingBackend = recordingBackend.Backend,");
        AssertDoesNotContain(snapshotFlatteningText, "MuxResult = recordingBackend.MuxResult,");

        AssertContains(recordingPipelineProjectionText, "private static RecordingBackendProjection BuildRecordingBackendProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(recordingPipelineProjectionText, "Backend = captureRuntime.RecordingBackend,");
        AssertContains(recordingPipelineProjectionText, "AudioPathMode = captureRuntime.AudioPathMode,");
        AssertContains(recordingPipelineProjectionText, "MuxResult = ResolveMuxResult(captureRuntime.MuxSucceeded)");
        AssertContains(recordingPipelineProjectionText, "private static string ResolveMuxResult(bool? muxSucceeded)");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingBackendProjection");
        AssertContains(recordingPipelineProjectionText, "private static RecordingOutputFlattenedProjection BuildRecordingOutputFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "Backend = recordingBackend.Backend,");
        AssertContains(recordingPipelineProjectionText, "AudioPathMode = recordingBackend.AudioPathMode,");
        AssertContains(recordingPipelineProjectionText, "MuxResult = recordingBackend.MuxResult,");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingOutputFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsRecordingOutputProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var recordingPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs")
            .Replace("\r\n", "\n");
        var obsoleteRecordingOutputPath = System.IO.Path.Combine(
            GetRepoRoot(),
            "Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs");

        AssertContains(snapshotProjectionText, "var recordingOutput = BuildRecordingOutputProjection(");
        AssertContains(snapshotFlatteningText, "OutputPath = recordingOutputFlattening.OutputPath,");
        AssertContains(snapshotFlatteningText, "RecordingVideoBytes = recordingOutputFlattening.RecordingVideoBytes,");
        AssertContains(snapshotFlatteningText, "LastOutputPath = recordingOutputFlattening.LastOutputPath,");
        AssertContains(snapshotFlatteningText, "LastVerification = recordingOutputFlattening.LastVerification,");
        AssertDoesNotContain(snapshotFlatteningText, "OutputPath = viewModelSnapshot.OutputPath,");
        AssertDoesNotContain(snapshotFlatteningText, "OutputPath = recordingOutput.OutputPath,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingVideoBytes = recordingStats.VideoBytes,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingVideoBytes = recordingOutput.RecordingVideoBytes,");
        AssertDoesNotContain(snapshotFlatteningText, "LastOutputPath = captureRuntime.LastOutputPath,");
        AssertDoesNotContain(snapshotFlatteningText, "LastOutputPath = recordingOutput.LastOutputPath,");
        AssertDoesNotContain(snapshotFlatteningText, "LastOutputSizeBytes = lastOutput.SizeBytes,");
        AssertDoesNotContain(snapshotFlatteningText, "LastVerification = recordingOutput.LastVerification,");

        AssertContains(recordingPipelineProjectionText, "private static RecordingOutputProjection BuildRecordingOutputProjection(");
        AssertContains(recordingPipelineProjectionText, "OutputPath = viewModelSnapshot.OutputPath,");
        AssertContains(recordingPipelineProjectionText, "RecordingVideoBytes = recordingStats.VideoBytes,");
        AssertContains(recordingPipelineProjectionText, "LastOutputPath = captureRuntime.LastOutputPath,");
        AssertContains(recordingPipelineProjectionText, "LastOutputSizeBytes = lastOutput.SizeBytes,");
        AssertContains(recordingPipelineProjectionText, "LastVerification = lastVerification");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingOutputProjection");
        AssertContains(recordingPipelineProjectionText, "OutputPath = recordingOutput.OutputPath,");
        AssertContains(recordingPipelineProjectionText, "RecordingVideoBytes = recordingOutput.RecordingVideoBytes,");
        AssertContains(recordingPipelineProjectionText, "LastOutputPath = recordingOutput.LastOutputPath,");
        AssertContains(recordingPipelineProjectionText, "LastVerification = recordingOutput.LastVerification");
        if (System.IO.File.Exists(obsoleteRecordingOutputPath))
        {
            throw new System.InvalidOperationException("Recording output projection should stay consolidated into RecordingPipeline.cs.");
        }

        return Task.CompletedTask;
    }


    internal static Task AutomationDiagnosticsPreviewRuntimeProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var previewRuntimeProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var previewSummary = BuildPreviewRuntimeProjection(previewRuntime, previewHdrState, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var previewRuntimeFlattening = BuildPreviewRuntimeFlattenedProjection(previewSummary);");
        AssertContains(snapshotFlatteningText, "PreviewFramesArrived = previewRuntimeFlattening.Frame.FramesArrived,");
        AssertContains(snapshotFlatteningText, "EstimatedPipelineLatencyMs = previewRuntimeFlattening.Frame.EstimatedPipelineLatencyMs,");
        AssertContains(snapshotFlatteningText, "PreviewCadenceOnePercentLowFps = previewRuntimeFlattening.Cadence.OnePercentLowFps,");
        AssertContains(snapshotFlatteningText, "PreviewStartupStrategy = previewRuntimeFlattening.Startup.Strategy,");
        AssertContains(snapshotFlatteningText, "PreviewRendererMode = previewRuntimeFlattening.Startup.RendererMode,");
        AssertContains(snapshotFlatteningText, "PreviewGpuPlaybackState = previewRuntimeFlattening.GpuPlayback.PlaybackState,");
        AssertContains(snapshotFlatteningText, "PreviewColorContext = previewRuntimeFlattening.Color.ColorContext,");
        AssertContains(snapshotFlatteningText, "PreviewAdapterColorMetadata = previewRuntimeFlattening.Color.AdapterColorMetadata,");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeFlattenedProjection BuildPreviewRuntimeFlattenedProjection(");
        AssertContains(previewRuntimeProjectionText, "Frame = BuildPreviewRuntimeFrameFlattenedProjection(previewSummary.Frame),");
        AssertContains(previewRuntimeProjectionText, "Cadence = BuildPreviewRuntimeCadenceFlattenedProjection(previewSummary.Cadence),");
        AssertContains(previewRuntimeProjectionText, "Surface = BuildPreviewRuntimeSurfaceFlattenedProjection(previewSummary.Surface),");
        AssertContains(previewRuntimeProjectionText, "Startup = BuildPreviewRuntimeStartupFlattenedProjection(previewSummary.Startup),");
        AssertContains(previewRuntimeProjectionText, "GpuPlayback = BuildPreviewRuntimeGpuPlaybackFlattenedProjection(previewSummary.GpuPlayback),");
        AssertContains(previewRuntimeProjectionText, "Color = BuildPreviewRuntimeColorFlattenedProjection(previewSummary.Color)");
        AssertContains(previewRuntimeProjectionText, "private readonly record struct PreviewRuntimeFlattenedProjection");

        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeFrameProjection BuildPreviewRuntimeFrameProjection(");
        AssertContains(previewRuntimeProjectionText, "FramesArrived = previewRuntime.FramesArrived,");
        AssertContains(previewRuntimeProjectionText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(");
        AssertContains(previewRuntimeProjectionText, "OnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertContains(previewRuntimeProjectionText, "RecentIntervalsMs = previewRuntime.DisplayCadenceRecentIntervalsMs,");
        AssertContains(previewRuntimeProjectionText, "SlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeSurfaceProjection BuildPreviewRuntimeSurfaceProjection(");
        AssertContains(previewRuntimeProjectionText, "RendererAttached = previewRuntime.RendererAttached");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(");
        AssertContains(previewRuntimeProjectionText, "Strategy = previewRuntime.StartupStrategy.ToString(),");
        AssertContains(previewRuntimeProjectionText, "RendererMode = previewRuntime.RendererMode");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeGpuPlaybackProjection BuildPreviewRuntimeGpuPlaybackProjection(");
        AssertContains(previewRuntimeProjectionText, "PlaybackState = previewRuntime.GpuPlaybackState,");
        AssertContains(previewRuntimeProjectionText, "PositionEventCount = previewRuntime.GpuPositionEventCount");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeColorProjection BuildPreviewRuntimeColorProjection(");
        AssertContains(previewRuntimeProjectionText, "HdrInputDetected = previewHdrState.InputDetected,");
        AssertContains(previewRuntimeProjectionText, "AdapterColorMetadata = captureRuntime.PreviewColorMetadata");

        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeFrameFlattenedProjection BuildPreviewRuntimeFrameFlattenedProjection(");
        AssertContains(previewRuntimeProjectionText, "FramesArrived = frame.FramesArrived,");
        AssertContains(previewRuntimeProjectionText, "EstimatedPipelineLatencyMs = frame.EstimatedPipelineLatencyMs");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeCadenceFlattenedProjection BuildPreviewRuntimeCadenceFlattenedProjection(");
        AssertContains(previewRuntimeProjectionText, "OnePercentLowFps = cadence.OnePercentLowFps,");
        AssertContains(previewRuntimeProjectionText, "SlowFramePercent = cadence.SlowFramePercent");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeSurfaceFlattenedProjection BuildPreviewRuntimeSurfaceFlattenedProjection(");
        AssertContains(previewRuntimeProjectionText, "RendererAttached = surface.RendererAttached");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeStartupFlattenedProjection BuildPreviewRuntimeStartupFlattenedProjection(");
        AssertContains(previewRuntimeProjectionText, "Strategy = startup.Strategy,");
        AssertContains(previewRuntimeProjectionText, "RendererMode = startup.RendererMode");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeGpuPlaybackFlattenedProjection BuildPreviewRuntimeGpuPlaybackFlattenedProjection(");
        AssertContains(previewRuntimeProjectionText, "PlaybackState = gpuPlayback.PlaybackState,");
        AssertContains(previewRuntimeProjectionText, "PositionEventCount = gpuPlayback.PositionEventCount");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeColorFlattenedProjection BuildPreviewRuntimeColorFlattenedProjection(");
        AssertContains(previewRuntimeProjectionText, "ColorContext = color.ColorContext,");
        AssertContains(previewRuntimeProjectionText, "AdapterColorMetadata = color.AdapterColorMetadata");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewFramesArrived = previewRuntime.FramesArrived,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewFramesArrived = previewSummary.FramesArrived,");
        AssertDoesNotContain(snapshotFlatteningText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "EstimatedPipelineLatencyMs = previewSummary.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewCadenceOnePercentLowFps = previewSummary.CadenceOnePercentLowFps,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewCadenceOnePercentLowFps = previewSummary.Cadence.OnePercentLowFps,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewCadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewStartupStrategy = previewSummary.StartupStrategy,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewStartupStrategy = previewSummary.Startup.Strategy,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewRendererMode = previewSummary.RendererMode,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewRendererMode = previewSummary.Startup.RendererMode,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewStartupStrategy = previewRuntime.StartupStrategy.ToString(),");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewGpuPlaybackState = previewRuntime.GpuPlaybackState,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewGpuPlaybackState = previewSummary.GpuPlaybackState,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewColorContext = captureRuntime.NegotiatedPixelFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewColorContext = previewSummary.ColorContext,");

        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeProjection BuildPreviewRuntimeProjection(");
        AssertContains(previewRuntimeProjectionText, "Frame = BuildPreviewRuntimeFrameProjection(previewRuntime),");
        AssertContains(previewRuntimeProjectionText, "Cadence = BuildPreviewRuntimeCadenceProjection(previewRuntime),");
        AssertContains(previewRuntimeProjectionText, "Surface = BuildPreviewRuntimeSurfaceProjection(previewRuntime),");
        AssertContains(previewRuntimeProjectionText, "Startup = BuildPreviewRuntimeStartupProjection(previewRuntime),");
        AssertContains(previewRuntimeProjectionText, "GpuPlayback = BuildPreviewRuntimeGpuPlaybackProjection(previewRuntime),");
        AssertContains(previewRuntimeProjectionText, "Color = BuildPreviewRuntimeColorProjection(previewHdrState, captureRuntime)");
        AssertDoesNotContain(previewRuntimeProjectionText, "CadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertDoesNotContain(previewRuntimeProjectionText, "CadenceSlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent,");
        AssertDoesNotContain(previewRuntimeProjectionText, "StartupStrategy = previewRuntime.StartupStrategy.ToString(),");
        AssertDoesNotContain(previewRuntimeProjectionText, "RendererMode = previewRuntime.RendererMode,");
        AssertDoesNotContain(previewRuntimeProjectionText, "GpuPlaybackState = previewRuntime.GpuPlaybackState,");
        AssertContains(previewRuntimeProjectionText, "private readonly record struct PreviewRuntimeProjection");
        AssertContains(previewRuntimeProjectionText, "public PreviewRuntimeFrameProjection Frame { get; init; }");
        AssertContains(previewRuntimeProjectionText, "public PreviewRuntimeColorProjection Color { get; init; }");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsPreviewD3DProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var previewD3DProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs")
            .Replace("\r\n", "\n");
        var previewD3DFrameFlowProjectionText = previewD3DProjectionText;
        var previewD3DCpuTimingProjectionText = previewD3DProjectionText;

        AssertContains(snapshotProjectionText, "var previewD3D = BuildPreviewD3DProjection(\n            previewRuntime,\n            recentD3DMissedRefreshes,\n            recentD3DStatsFailures);");
        AssertContains(snapshotFlatteningText, "var previewD3DFlattening = BuildPreviewD3DFlattenedProjection(previewD3D);");
        AssertContains(snapshotFlatteningText, "PreviewD3DPresentSyncInterval = previewD3DFlattening.PresentSyncInterval,");
        AssertContains(snapshotFlatteningText, "PreviewD3DInputUploadCpuP99Ms = previewD3DFlattening.CpuTiming.InputUploadCpuP99Ms,");
        AssertContains(snapshotFlatteningText, "PreviewD3DPipelineLatencyMaxMs = previewD3DFlattening.LatencyAndStats.PipelineLatencyMaxMs,");
        AssertContains(snapshotFlatteningText, "PreviewD3DFrameLatencyWaitTimeoutCount = previewD3DFlattening.LatencyAndStats.FrameLatencyWaitTimeoutCount,");
        AssertContains(snapshotFlatteningText, "PreviewD3DFrameStatsRecentMissedRefreshCount = previewD3DFlattening.LatencyAndStats.FrameStatsRecentMissedRefreshCount,");
        AssertContains(snapshotFlatteningText, "PreviewD3DRecentSlowFrames = previewD3DFlattening.FrameFlow.RecentSlowFrames,");
        AssertContains(snapshotFlatteningText, "PreviewD3DLastRenderedPipelineLatencyMs = previewD3DFlattening.FrameFlow.LastRenderedPipelineLatencyMs,");
        AssertContains(previewD3DProjectionText, "private static PreviewD3DFlattenedProjection BuildPreviewD3DFlattenedProjection(");
        AssertContains(previewD3DProjectionText, "CpuTiming = BuildPreviewD3DCpuTimingFlattenedProjection(previewD3D.CpuTiming),");
        AssertContains(previewD3DProjectionText, "LatencyAndStats = BuildPreviewD3DLatencyAndStatsFlattenedProjection(");
        AssertContains(previewD3DProjectionText, "FrameFlow = BuildPreviewD3DFrameFlowFlattenedProjection(previewD3D.FrameFlow)");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DFlattenedProjection");
        AssertContains(previewD3DProjectionText, "public PreviewD3DCpuTimingFlattenedProjection CpuTiming { get; init; }");
        AssertContains(previewD3DProjectionText, "public PreviewD3DLatencyAndStatsFlattenedProjection LatencyAndStats { get; init; }");
        AssertContains(previewD3DProjectionText, "public PreviewD3DFrameFlowFlattenedProjection FrameFlow { get; init; }");
        AssertContains(previewD3DCpuTimingProjectionText, "private static PreviewD3DCpuTimingFlattenedProjection BuildPreviewD3DCpuTimingFlattenedProjection(");
        AssertContains(previewD3DCpuTimingProjectionText, "InputUploadCpuP99Ms = cpuTiming.InputUploadP99Ms,");
        AssertContains(previewD3DCpuTimingProjectionText, "private readonly record struct PreviewD3DCpuTimingFlattenedProjection");
        AssertContains(previewD3DCpuTimingProjectionText, "public double InputUploadCpuP99Ms { get; init; }");
        AssertContains(previewD3DCpuTimingProjectionText, "public double RenderSubmitCpuP99Ms { get; init; }");
        AssertContains(previewD3DCpuTimingProjectionText, "public double PresentCallP99Ms { get; init; }");
        AssertContains(previewD3DCpuTimingProjectionText, "public double TotalFrameCpuP99Ms { get; init; }");
        AssertContains(previewD3DProjectionText, "private static PreviewD3DLatencyAndStatsFlattenedProjection BuildPreviewD3DLatencyAndStatsFlattenedProjection(");
        AssertContains(previewD3DProjectionText, "PipelineLatencyMaxMs = pipelineLatency.MaxMs,");
        AssertContains(previewD3DProjectionText, "FrameLatencyWaitTimeoutCount = frameLatencyWait.TimeoutCount,");
        AssertContains(previewD3DProjectionText, "FrameStatsRecentMissedRefreshCount = frameStats.RecentMissedRefreshCount,");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DLatencyAndStatsFlattenedProjection");
        AssertContains(previewD3DProjectionText, "public double PipelineLatencyP99Ms { get; init; }");
        AssertContains(previewD3DProjectionText, "public long FrameLatencyWaitTimeoutCount { get; init; }");
        AssertContains(previewD3DProjectionText, "public long FrameStatsRecentMissedRefreshCount { get; init; }");
        AssertContains(previewD3DFrameFlowProjectionText, "private static PreviewD3DFrameFlowFlattenedProjection BuildPreviewD3DFrameFlowFlattenedProjection(");
        AssertContains(previewD3DFrameFlowProjectionText, "LastRenderedPipelineLatencyMs = frameFlow.LastRenderedPipelineLatencyMs,");
        AssertContains(previewD3DFrameFlowProjectionText, "RecentSlowFrames = frameFlow.RecentSlowFrames");
        AssertContains(previewD3DFrameFlowProjectionText, "private readonly record struct PreviewD3DFrameFlowFlattenedProjection");
        AssertContains(previewD3DFrameFlowProjectionText, "public long LastSubmittedPreviewPresentId { get; init; }");
        AssertContains(previewD3DFrameFlowProjectionText, "public double LastRenderedPipelineLatencyMs { get; init; }");
        AssertContains(previewD3DFrameFlowProjectionText, "public string LastDropReason { get; init; }");
        AssertContains(previewD3DFrameFlowProjectionText, "public PreviewSlowFrameDiagnostic[] RecentSlowFrames { get; init; }");
        AssertContains(previewD3DProjectionText, "public double InputUploadCpuP99Ms { get; init; }");
        AssertContains(previewD3DProjectionText, "public long LastSubmittedPreviewPresentId { get; init; }");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DPresentSyncInterval = previewRuntime.D3DPresentSyncInterval,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DInputUploadCpuP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DInputUploadCpuP99Ms = previewD3D.InputUploadCpuP99Ms,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DPipelineLatencyMaxMs = previewD3D.PipelineLatencyMaxMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DPipelineLatencyMaxMs = previewD3D.PipelineLatency.MaxMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DPipelineLatencyMaxMs = previewD3D.CpuTiming.PipelineLatencyMaxMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameLatencyWaitTimeoutCount = previewD3D.FrameLatencyWaitTimeoutCount,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameLatencyWaitTimeoutCount = previewD3D.FrameLatencyWait.TimeoutCount,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameStatsRecentMissedRefreshCount = previewD3D.FrameStatsRecentMissedRefreshCount,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameStatsRecentMissedRefreshCount = previewD3D.FrameStats.RecentMissedRefreshCount,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameStatsRecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DRecentSlowFrames = previewD3D.RecentSlowFrames,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DRecentSlowFrames = previewD3D.FrameFlow.RecentSlowFrames,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DLastRenderedPipelineLatencyMs = previewD3D.LastRenderedPipelineLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DLastRenderedPipelineLatencyMs = previewD3D.FrameFlow.LastRenderedPipelineLatencyMs,");

        AssertContains(previewD3DProjectionText, "private static PreviewD3DProjection BuildPreviewD3DProjection(");
        AssertContains(previewD3DProjectionText, "var cpuTiming = BuildPreviewD3DCpuTimingProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "CpuTiming = cpuTiming,");
        AssertContains(previewD3DProjectionText, "var pipelineLatency = BuildPreviewD3DPipelineLatencyProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "PipelineLatency = pipelineLatency,");
        AssertContains(previewD3DProjectionText, "var frameFlow = BuildPreviewD3DFrameFlowProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "FrameFlow = frameFlow");
        AssertContains(previewD3DProjectionText, "var frameLatencyWait = BuildPreviewD3DFrameLatencyWaitProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "var frameStats = BuildPreviewD3DFrameStatsProjection(");
        AssertContains(previewD3DProjectionText, "FrameLatencyWait = frameLatencyWait,");
        AssertContains(previewD3DProjectionText, "FrameStats = frameStats,");
        AssertDoesNotContain(previewD3DProjectionText, "InputUploadCpuP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,");
        AssertDoesNotContain(previewD3DProjectionText, "PipelineLatencyMaxMs = previewRuntime.D3DPipelineLatencyMaxMs,");
        AssertDoesNotContain(previewD3DProjectionText, "LastRenderedPipelineLatencyMs = previewD3D.D3DLastRenderedPipelineLatencyMs,");
        AssertDoesNotContain(previewD3DProjectionText, "RecentSlowFrames = previewD3D.D3DRecentSlowFrames");
        AssertDoesNotContain(previewD3DProjectionText, "FrameLatencyWaitTimeoutCount = previewD3D.D3DFrameLatencyWaitTimeoutCount,");
        AssertDoesNotContain(previewD3DProjectionText, "FrameStatsRecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertContains(previewD3DCpuTimingProjectionText, "private static PreviewD3DCpuTimingProjection BuildPreviewD3DCpuTimingProjection(");
        AssertContains(previewD3DCpuTimingProjectionText, "SampleCount = previewRuntime.D3DCpuTimingSampleCount,");
        AssertContains(previewD3DCpuTimingProjectionText, "InputUploadP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,");
        AssertContains(previewD3DCpuTimingProjectionText, "private readonly record struct PreviewD3DCpuTimingProjection");
        AssertDoesNotContain(previewD3DCpuTimingProjectionText, "PipelineLatencyMaxMs = previewRuntime.D3DPipelineLatencyMaxMs");
        AssertContains(previewD3DProjectionText, "private static PreviewD3DPipelineLatencyProjection BuildPreviewD3DPipelineLatencyProjection(");
        AssertContains(previewD3DProjectionText, "SampleCount = previewRuntime.D3DPipelineLatencySampleCount,");
        AssertContains(previewD3DProjectionText, "MaxMs = previewRuntime.D3DPipelineLatencyMaxMs");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DPipelineLatencyProjection");
        AssertContains(previewD3DFrameFlowProjectionText, "private static PreviewD3DFrameFlowProjection BuildPreviewD3DFrameFlowProjection(");
        AssertContains(previewD3DFrameFlowProjectionText, "LastRenderedPipelineLatencyMs = previewRuntime.D3DLastRenderedPipelineLatencyMs,");
        AssertContains(previewD3DFrameFlowProjectionText, "RecentSlowFrames = previewRuntime.D3DRecentSlowFrames");
        AssertContains(previewD3DFrameFlowProjectionText, "private readonly record struct PreviewD3DFrameFlowProjection");
        AssertContains(previewD3DProjectionText, "private static PreviewD3DFrameFlowProjection BuildPreviewD3DFrameFlowProjection(");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DFrameFlowProjection");
        AssertContains(previewD3DProjectionText, "private static PreviewD3DFrameLatencyWaitProjection BuildPreviewD3DFrameLatencyWaitProjection(");
        AssertContains(previewD3DProjectionText, "Enabled = previewRuntime.D3DFrameLatencyWaitEnabled,");
        AssertContains(previewD3DProjectionText, "TimeoutCount = previewRuntime.D3DFrameLatencyWaitTimeoutCount,");
        AssertContains(previewD3DProjectionText, "MaxMs = previewRuntime.D3DFrameLatencyWaitMaxMs");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DFrameLatencyWaitProjection");

        AssertContains(previewD3DProjectionText, "private static PreviewD3DFrameStatsProjection BuildPreviewD3DFrameStatsProjection(");
        AssertContains(previewD3DProjectionText, "SampleCount = previewRuntime.D3DFrameStatsSampleCount,");
        AssertContains(previewD3DProjectionText, "RecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertContains(previewD3DProjectionText, "RecentFailureCount = recentD3DStatsFailures");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DFrameStatsProjection");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DProjection");

        return Task.CompletedTask;
    }


    internal static Task AutomationDiagnosticsFlashbackExportProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var flashbackExportProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var flashbackExport = BuildFlashbackExportProjection(health);");
        AssertContains(snapshotProjectionText, "var flashbackExportLastResult = BuildFlashbackExportLastResultProjection(health);");
        AssertContains(snapshotFlatteningText, "var flashbackExportFlattening = BuildFlashbackExportFlattenedProjection(");
        AssertContains(snapshotFlatteningText, "FlashbackExportActive = flashbackExportFlattening.Active,");
        AssertContains(snapshotFlatteningText, "FlashbackExportPercent = flashbackExportFlattening.Percent,");
        AssertContains(snapshotFlatteningText, "FlashbackExportLastForceRotateFallbackSegments = flashbackExportFlattening.LastForceRotateFallbackSegments,");
        AssertContains(snapshotFlatteningText, "LastExportId = flashbackExportFlattening.LastExportId,");
        AssertContains(snapshotFlatteningText, "LastExportMessage = flashbackExportFlattening.LastExportMessage");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackExportActive = health.FlashbackExportActive,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackExportActive = flashbackExport.Active,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackExportPercent = health.FlashbackExportPercent,");
        AssertDoesNotContain(snapshotFlatteningText, "LastExportId = health.LastExportId,");
        AssertDoesNotContain(snapshotFlatteningText, "LastExportId = flashbackExportLastResult.LastExportId,");
        AssertDoesNotContain(snapshotFlatteningText, "LastExportId = flashbackExport.LastExportId,");

        AssertContains(flashbackExportProjectionText, "private static FlashbackExportFlattenedProjection BuildFlashbackExportFlattenedProjection(");
        AssertContains(flashbackExportProjectionText, "Active = flashbackExport.Active,");
        AssertContains(flashbackExportProjectionText, "Percent = flashbackExport.Percent,");
        AssertContains(flashbackExportProjectionText, "LastForceRotateFallbackSegments = flashbackExport.LastForceRotateFallbackSegments,");
        AssertContains(flashbackExportProjectionText, "LastExportId = lastResult.LastExportId,");
        AssertContains(flashbackExportProjectionText, "LastExportMessage = lastResult.LastExportMessage");
        AssertContains(flashbackExportProjectionText, "private readonly record struct FlashbackExportFlattenedProjection");

        AssertContains(flashbackExportProjectionText, "private static FlashbackExportProjection BuildFlashbackExportProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackExportProjectionText, "Active = health.FlashbackExportActive,");
        AssertContains(flashbackExportProjectionText, "Percent = health.FlashbackExportPercent,");
        AssertContains(flashbackExportProjectionText, "LastForceRotateFallbackSegments = health.FlashbackExportLastForceRotateFallbackSegments,");
        AssertContains(flashbackExportProjectionText, "private readonly record struct FlashbackExportProjection");
        AssertDoesNotContain(flashbackExportProjectionText, "LastExportId = flashbackExport.LastExportId,");
        AssertContains(flashbackExportProjectionText, "private static FlashbackExportLastResultProjection BuildFlashbackExportLastResultProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackExportProjectionText, "LastExportId = health.LastExportId,");
        AssertContains(flashbackExportProjectionText, "LastExportMessage = health.LastExportMessage");
        AssertContains(flashbackExportProjectionText, "private readonly record struct FlashbackExportLastResultProjection");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs")),
            "Flashback export projection folded into Flashback projection owner");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsFlashbackRecordingProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var flashbackRecordingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingQueuesProjectionText = flashbackRecordingProjectionText;

        AssertContains(snapshotProjectionText, "var flashbackRecording = BuildFlashbackRecordingProjection(captureRuntime, health);");
        AssertContains(snapshotFlatteningText, "var flashbackRecordingFlattening = BuildFlashbackRecordingFlattenedProjection(flashbackRecording);");
        AssertContains(snapshotFlatteningText, "FlashbackEncodingFailed = flashbackRecordingFlattening.EncodingFailed,");
        AssertContains(snapshotFlatteningText, "FlashbackStartupCacheOverBudget = flashbackRecordingFlattening.StartupCache.OverBudget,");
        AssertContains(snapshotFlatteningText, "FlashbackVideoQueueCapacity = flashbackRecordingFlattening.Queues.VideoQueueCapacity,");
        AssertContains(snapshotFlatteningText, "FlashbackGpuQueueLastRejectReason = flashbackRecordingFlattening.Queues.GpuQueueLastRejectReason,");
        AssertContains(snapshotFlatteningText, "FlashbackActive = flashbackRecordingFlattening.Runtime.Active,");
        AssertContains(snapshotFlatteningText, "FlashbackBackendSettingsStale = flashbackRecordingFlattening.Backend.SettingsStale,");
        AssertContains(snapshotFlatteningText, "FlashbackExportVerificationFormat = flashbackRecordingFlattening.Backend.ExportVerificationFormat,");
        AssertContains(snapshotFlatteningText, "EncoderCodecName = flashbackRecordingFlattening.Encoder.CodecName,");
        AssertContains(snapshotFlatteningText, "FlashbackAudioQueueCapacity = flashbackRecordingFlattening.Queues.AudioQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackEncodingFailed = health.FlashbackEncodingFailed,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackVideoQueueCapacity = health.FlashbackVideoQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackGpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackActive = health.FlashbackActive,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "EncoderCodecName = health.EncoderCodecName,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackEncodingFailed = flashbackRecording.EncodingFailed,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackStartupCacheOverBudget = flashbackRecording.StartupCache.OverBudget,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackVideoQueueCapacity = flashbackRecording.Queues.VideoQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackGpuQueueLastRejectReason = flashbackRecording.Queues.GpuQueueLastRejectReason,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackActive = flashbackRecording.Active,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackAudioQueueCapacity = flashbackRecording.Queues.AudioQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackStartupCacheOverBudget = flashbackRecording.StartupCacheOverBudget,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackStartupCacheOverBudget = health.FlashbackStartupCacheOverBudget,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackVideoQueueCapacity = flashbackRecording.VideoQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackGpuQueueLastRejectReason = flashbackRecording.GpuQueueLastRejectReason,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackAudioQueueCapacity = flashbackRecording.AudioQueueCapacity,");

        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingProjection BuildFlashbackRecordingProjection(");
        AssertContains(flashbackRecordingProjectionText, "CaptureRuntimeSnapshot captureRuntime,");
        AssertContains(flashbackRecordingProjectionText, "var startupCache = BuildFlashbackRecordingStartupCacheProjection(health);");
        AssertContains(flashbackRecordingProjectionText, "var queues = BuildFlashbackRecordingQueuesProjection(health);");
        AssertContains(flashbackRecordingProjectionText, "var runtime = BuildFlashbackRecordingRuntimeProjection(health);");
        AssertContains(flashbackRecordingProjectionText, "var backend = BuildFlashbackRecordingBackendProjection(captureRuntime, health);");
        AssertContains(flashbackRecordingProjectionText, "var encoder = BuildFlashbackRecordingEncoderProjection(health);");
        AssertContains(flashbackRecordingProjectionText, "StartupCache = startupCache,");
        AssertContains(flashbackRecordingProjectionText, "Queues = queues,");
        AssertContains(flashbackRecordingProjectionText, "Runtime = runtime,");
        AssertContains(flashbackRecordingProjectionText, "Backend = backend,");
        AssertContains(flashbackRecordingProjectionText, "Encoder = encoder");
        AssertContains(flashbackRecordingProjectionText, "EncodingFailed = health.FlashbackEncodingFailed,");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingFlattenedProjection BuildFlashbackRecordingFlattenedProjection(");
        AssertContains(flashbackRecordingProjectionText, "FlashbackRecordingProjection flashbackRecording");
        AssertContains(flashbackRecordingProjectionText, "EncodingFailed = flashbackRecording.EncodingFailed,");
        AssertContains(flashbackRecordingProjectionText, "StartupCache = BuildFlashbackRecordingStartupCacheFlattenedProjection(flashbackRecording.StartupCache),");
        AssertContains(flashbackRecordingProjectionText, "Queues = BuildFlashbackRecordingQueuesFlattenedProjection(flashbackRecording.Queues),");
        AssertContains(flashbackRecordingProjectionText, "Runtime = BuildFlashbackRecordingRuntimeFlattenedProjection(flashbackRecording.Runtime),");
        AssertContains(flashbackRecordingProjectionText, "Backend = BuildFlashbackRecordingBackendFlattenedProjection(flashbackRecording.Backend),");
        AssertContains(flashbackRecordingProjectionText, "Encoder = BuildFlashbackRecordingEncoderFlattenedProjection(flashbackRecording.Encoder)");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingFlattenedProjection");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingStartupCacheFlattenedProjection StartupCache { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingQueuesFlattenedProjection Queues { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingRuntimeFlattenedProjection Runtime { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingBackendFlattenedProjection Backend { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingEncoderFlattenedProjection Encoder { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingRuntimeProjection Runtime { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingBackendProjection Backend { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingEncoderProjection Encoder { get; init; }");
        AssertDoesNotContain(flashbackRecordingProjectionText, "StartupCacheOverBudget = health.FlashbackStartupCacheOverBudget,");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingStartupCacheProjection BuildFlashbackRecordingStartupCacheProjection(");
        AssertContains(flashbackRecordingProjectionText, "TempDriveFreeBytes = health.FlashbackTempDriveFreeBytes,");
        AssertContains(flashbackRecordingProjectionText, "OverBudget = health.FlashbackStartupCacheOverBudget");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingStartupCacheProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingStartupCacheFlattenedProjection BuildFlashbackRecordingStartupCacheFlattenedProjection(");
        AssertContains(flashbackRecordingProjectionText, "OverBudget = startupCache.OverBudget");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingStartupCacheFlattenedProjection");
        AssertContains(flashbackRecordingQueuesProjectionText, "private static FlashbackRecordingQueuesProjection BuildFlashbackRecordingQueuesProjection(");
        AssertContains(flashbackRecordingQueuesProjectionText, "VideoQueueCapacity = health.FlashbackVideoQueueCapacity,");
        AssertContains(flashbackRecordingQueuesProjectionText, "GpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,");
        AssertContains(flashbackRecordingQueuesProjectionText, "AudioQueueCapacity = health.FlashbackAudioQueueCapacity");
        AssertContains(flashbackRecordingQueuesProjectionText, "private readonly record struct FlashbackRecordingQueuesProjection");
        AssertContains(flashbackRecordingQueuesProjectionText, "private static FlashbackRecordingQueuesFlattenedProjection BuildFlashbackRecordingQueuesFlattenedProjection(");
        AssertContains(flashbackRecordingQueuesProjectionText, "VideoQueueCapacity = queues.VideoQueueCapacity,");
        AssertContains(flashbackRecordingQueuesProjectionText, "GpuQueueLastRejectReason = queues.GpuQueueLastRejectReason,");
        AssertContains(flashbackRecordingQueuesProjectionText, "AudioQueueCapacity = queues.AudioQueueCapacity");
        AssertContains(flashbackRecordingQueuesProjectionText, "private readonly record struct FlashbackRecordingQueuesFlattenedProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingRuntimeProjection BuildFlashbackRecordingRuntimeProjection(");
        AssertContains(flashbackRecordingProjectionText, "Active = health.FlashbackActive,");
        AssertContains(flashbackRecordingProjectionText, "GpuEncoding = health.FlashbackGpuEncoding");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingRuntimeProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingRuntimeFlattenedProjection BuildFlashbackRecordingRuntimeFlattenedProjection(");
        AssertContains(flashbackRecordingProjectionText, "FlashbackRecordingRuntimeProjection runtime");
        AssertContains(flashbackRecordingProjectionText, "Active = runtime.Active,");
        AssertContains(flashbackRecordingProjectionText, "GpuEncoding = runtime.GpuEncoding");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingRuntimeFlattenedProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingBackendProjection BuildFlashbackRecordingBackendProjection(");
        AssertContains(flashbackRecordingProjectionText, "ExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertContains(flashbackRecordingProjectionText, "CodecDowngradeReason = captureRuntime.FlashbackCodecDowngradeReason ?? health.FlashbackCodecDowngradeReason");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingBackendProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingBackendFlattenedProjection BuildFlashbackRecordingBackendFlattenedProjection(");
        AssertContains(flashbackRecordingProjectionText, "FlashbackRecordingBackendProjection backend");
        AssertContains(flashbackRecordingProjectionText, "SettingsStale = backend.SettingsStale,");
        AssertContains(flashbackRecordingProjectionText, "ExportVerificationFormat = backend.ExportVerificationFormat,");
        AssertContains(flashbackRecordingProjectionText, "CodecDowngradeReason = backend.CodecDowngradeReason");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingBackendFlattenedProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingEncoderProjection BuildFlashbackRecordingEncoderProjection(");
        AssertContains(flashbackRecordingProjectionText, "CodecName = health.EncoderCodecName,");
        AssertContains(flashbackRecordingProjectionText, "FrameRateDenominator = health.EncoderFrameRateDenominator");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingEncoderProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingEncoderFlattenedProjection BuildFlashbackRecordingEncoderFlattenedProjection(");
        AssertContains(flashbackRecordingProjectionText, "FlashbackRecordingEncoderProjection encoder");
        AssertContains(flashbackRecordingProjectionText, "CodecName = encoder.CodecName,");
        AssertContains(flashbackRecordingProjectionText, "FrameRateDenominator = encoder.FrameRateDenominator");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingEncoderFlattenedProjection");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs")),
            "Flashback recording projection folded into Flashback projection owner");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsFlashbackPlaybackProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var flashbackPlaybackProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var flashbackPlayback = BuildFlashbackPlaybackProjection(health);");
        AssertContains(snapshotFlatteningText, "var flashbackPlaybackFlattening = BuildFlashbackPlaybackFlattenedProjection(flashbackPlayback);");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackState = flashbackPlaybackFlattening.State,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackAudioMasterFallbacks = flashbackPlaybackFlattening.AudioMaster.Fallbacks,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackTargetFps = flashbackPlaybackFlattening.Timing.TargetFps,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackMaxDecodePhase = flashbackPlaybackFlattening.Decode.MaxPhase,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackLastCommandFailure = flashbackPlaybackFlattening.Commands.LastFailure,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackState = health.FlashbackPlaybackState,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackTargetFps = health.FlashbackPlaybackTargetFps,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackLastCommandFailure = health.FlashbackPlaybackLastCommandFailure,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackState = flashbackPlayback.State,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackTargetFps = flashbackPlayback.TargetFps,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackMaxDecodePhase = flashbackPlayback.Decode.MaxPhase,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackLastCommandFailure = flashbackPlayback.Commands.LastFailure,");

        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackProjection BuildFlashbackPlaybackProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "var audioMaster = BuildFlashbackPlaybackAudioMasterProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "var timing = BuildFlashbackPlaybackTimingProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "var decode = BuildFlashbackPlaybackDecodeProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "var commands = BuildFlashbackPlaybackCommandProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "State = health.FlashbackPlaybackState,");
        AssertContains(flashbackPlaybackProjectionText, "AudioMaster = audioMaster,");
        AssertContains(flashbackPlaybackProjectionText, "Timing = timing,");
        AssertContains(flashbackPlaybackProjectionText, "Decode = decode,");
        AssertContains(flashbackPlaybackProjectionText, "Commands = commands");
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackFlattenedProjection BuildFlashbackPlaybackFlattenedProjection(");
        AssertContains(flashbackPlaybackProjectionText, "FlashbackPlaybackProjection flashbackPlayback");
        AssertContains(flashbackPlaybackProjectionText, "State = flashbackPlayback.State,");
        AssertContains(flashbackPlaybackProjectionText, "AudioMaster = BuildFlashbackPlaybackAudioMasterFlattenedProjection(flashbackPlayback.AudioMaster),");
        AssertContains(flashbackPlaybackProjectionText, "Timing = BuildFlashbackPlaybackTimingFlattenedProjection(flashbackPlayback.Timing),");
        AssertContains(flashbackPlaybackProjectionText, "Decode = BuildFlashbackPlaybackDecodeFlattenedProjection(flashbackPlayback.Decode),");
        AssertContains(flashbackPlaybackProjectionText, "Commands = BuildFlashbackPlaybackCommandFlattenedProjection(flashbackPlayback.Commands)");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackFlattenedProjection");
        AssertContains(flashbackPlaybackProjectionText, "public FlashbackPlaybackAudioMasterFlattenedProjection AudioMaster { get; init; }");
        AssertContains(flashbackPlaybackProjectionText, "public FlashbackPlaybackTimingFlattenedProjection Timing { get; init; }");
        AssertContains(flashbackPlaybackProjectionText, "public FlashbackPlaybackDecodeFlattenedProjection Decode { get; init; }");
        AssertContains(flashbackPlaybackProjectionText, "public FlashbackPlaybackCommandFlattenedProjection Commands { get; init; }");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "AudioMasterFallbacks = flashbackPlayback.AudioMaster.Fallbacks,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "MaxDecodePhase = flashbackPlayback.Decode.MaxPhase,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "LastCommandFailure = flashbackPlayback.Commands.LastFailure");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackProjection");
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackAudioMasterProjection BuildFlashbackPlaybackAudioMasterProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "Fallbacks = health.FlashbackPlaybackAudioMasterFallbacks,");
        AssertContains(flashbackPlaybackProjectionText, "LastFallbackReason = health.FlashbackPlaybackAudioMasterLastFallbackReason,");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackAudioMasterProjection");
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackAudioMasterFlattenedProjection BuildFlashbackPlaybackAudioMasterFlattenedProjection(");
        AssertContains(flashbackPlaybackProjectionText, "Fallbacks = audioMaster.Fallbacks,");
        AssertContains(flashbackPlaybackProjectionText, "LastFallbackReason = audioMaster.LastFallbackReason,");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackAudioMasterFlattenedProjection");

        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackTimingProjection BuildFlashbackPlaybackTimingProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "TargetFps = health.FlashbackPlaybackTargetFps,");
        AssertContains(flashbackPlaybackProjectionText, "PtsCadenceMismatchCount = health.FlashbackPlaybackPtsCadenceMismatchCount,");
        AssertContains(flashbackPlaybackProjectionText, "AvDriftMs = health.FlashbackAvDriftMs");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackTimingProjection");
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackTimingFlattenedProjection BuildFlashbackPlaybackTimingFlattenedProjection(");
        AssertContains(flashbackPlaybackProjectionText, "FlashbackPlaybackTimingProjection timing");
        AssertContains(flashbackPlaybackProjectionText, "TargetFps = timing.TargetFps,");
        AssertContains(flashbackPlaybackProjectionText, "PtsCadenceMismatchCount = timing.PtsCadenceMismatchCount,");
        AssertContains(flashbackPlaybackProjectionText, "AvDriftMs = timing.AvDriftMs");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackTimingFlattenedProjection");

        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackDecodeProjection BuildFlashbackPlaybackDecodeProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "SeekForwardDecodeCapHits = health.FlashbackPlaybackSeekForwardDecodeCapHits,");
        AssertContains(flashbackPlaybackProjectionText, "MaxPhase = health.FlashbackPlaybackMaxDecodePhase,");
        AssertContains(flashbackPlaybackProjectionText, "MaxPositionMs = health.FlashbackPlaybackMaxDecodePositionMs");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackDecodeProjection");
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackDecodeFlattenedProjection BuildFlashbackPlaybackDecodeFlattenedProjection(");
        AssertContains(flashbackPlaybackProjectionText, "MaxPhase = decode.MaxPhase,");
        AssertContains(flashbackPlaybackProjectionText, "MaxPositionMs = decode.MaxPositionMs");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackDecodeFlattenedProjection");

        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackCommandProjection BuildFlashbackPlaybackCommandProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "ThreadAlive = health.FlashbackPlaybackThreadAlive,");
        AssertContains(flashbackPlaybackProjectionText, "LastFailure = health.FlashbackPlaybackLastCommandFailure");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackCommandProjection");
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackCommandFlattenedProjection BuildFlashbackPlaybackCommandFlattenedProjection(");
        AssertContains(flashbackPlaybackProjectionText, "LastFailure = commands.LastFailure");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackCommandFlattenedProjection");

        return Task.CompletedTask;
    }
}
