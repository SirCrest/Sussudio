using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationDiagnosticsSnapshotStatusProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotStatusProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SnapshotStatus.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var snapshotStatus = BuildSnapshotStatusProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotProjectionText, "TimestampUtc = snapshotStatus.TimestampUtc,");
        AssertContains(snapshotProjectionText, "VerificationInProgress = snapshotStatus.VerificationInProgress,");
        AssertContains(snapshotProjectionText, "SessionState = snapshotStatus.SessionState,");
        AssertContains(snapshotProjectionText, "StatusText = snapshotStatus.StatusText,");
        AssertDoesNotContain(snapshotProjectionText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertDoesNotContain(snapshotProjectionText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertDoesNotContain(snapshotProjectionText, "SessionState = captureRuntime.SessionState,");
        AssertDoesNotContain(snapshotProjectionText, "StatusText = viewModelSnapshot.StatusText,");

        AssertContains(snapshotStatusProjectionText, "private SnapshotStatusProjection BuildSnapshotStatusProjection(");
        AssertContains(snapshotStatusProjectionText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertContains(snapshotStatusProjectionText, "IsInitialized = viewModelSnapshot.IsInitialized,");
        AssertContains(snapshotStatusProjectionText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertContains(snapshotStatusProjectionText, "SessionState = captureRuntime.SessionState,");
        AssertContains(snapshotStatusProjectionText, "StatusText = viewModelSnapshot.StatusText");
        AssertContains(snapshotStatusProjectionText, "private readonly record struct SnapshotStatusProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsSnapshotEvaluationProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotEvaluationProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var snapshotEvaluation = BuildSnapshotEvaluationProjection(performance, diagnostic, previewPacingClassification);");
        AssertContains(snapshotProjectionText, "PerformanceScore = snapshotEvaluation.PerformanceScore,");
        AssertContains(snapshotProjectionText, "DiagnosticHealthStatus = snapshotEvaluation.DiagnosticHealthStatus,");
        AssertContains(snapshotProjectionText, "PreviewPacingLikelySlowStage = snapshotEvaluation.PreviewPacingLikelySlowStage,");
        AssertContains(snapshotProjectionText, "PerformanceThresholdCaptureDropPercent = snapshotEvaluation.PerformanceThresholdCaptureDropPercent,");
        AssertDoesNotContain(snapshotProjectionText, "PerformanceScore = performance.Score,");
        AssertDoesNotContain(snapshotProjectionText, "DiagnosticHealthStatus = diagnostic.HealthStatus,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage");
        AssertDoesNotContain(snapshotProjectionText, "PerformanceThresholdCaptureDropPercent = _perfectionCaptureDropPercentThreshold,");

        AssertContains(snapshotEvaluationProjectionText, "private SnapshotEvaluationProjection BuildSnapshotEvaluationProjection(");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceScore = performance.Score,");
        AssertContains(snapshotEvaluationProjectionText, "DiagnosticHealthStatus = diagnostic.HealthStatus,");
        AssertContains(snapshotEvaluationProjectionText, "DiagnosticAudioLane = diagnostic.AudioLane,");
        AssertContains(snapshotEvaluationProjectionText, "PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage,");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceThresholdCaptureDropPercent = _perfectionCaptureDropPercentThreshold,");
        AssertContains(snapshotEvaluationProjectionText, "private readonly record struct SnapshotEvaluationProjection");

        return Task.CompletedTask;
    }

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

    private static Task AutomationDiagnosticsCaptureCommandProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var captureCommandProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureCommands = BuildCaptureCommandProjection(viewModelSnapshot);");
        AssertContains(snapshotProjectionText, "CaptureCommandCommandsEnqueued = captureCommands.CommandsEnqueued,");
        AssertContains(snapshotProjectionText, "CaptureCommandMaxQueueLatencyMs = captureCommands.MaxQueueLatencyMs,");
        AssertContains(snapshotProjectionText, "CaptureCommandLastError = captureCommands.LastError,");
        AssertDoesNotContain(snapshotProjectionText, "CaptureCommandCommandsEnqueued = viewModelSnapshot.CaptureCommandCommandsEnqueued,");
        AssertDoesNotContain(snapshotProjectionText, "CaptureCommandMaxQueueLatencyMs = viewModelSnapshot.CaptureCommandMaxQueueLatencyMs,");
        AssertDoesNotContain(snapshotProjectionText, "CaptureCommandLastError = viewModelSnapshot.CaptureCommandLastError,");

        AssertContains(captureCommandProjectionText, "private static CaptureCommandProjection BuildCaptureCommandProjection(ViewModelRuntimeSnapshot viewModelSnapshot)");
        AssertContains(captureCommandProjectionText, "CommandsEnqueued = viewModelSnapshot.CaptureCommandCommandsEnqueued,");
        AssertContains(captureCommandProjectionText, "MaxQueueLatencyMs = viewModelSnapshot.CaptureCommandMaxQueueLatencyMs,");
        AssertContains(captureCommandProjectionText, "LastError = viewModelSnapshot.CaptureCommandLastError");
        AssertContains(captureCommandProjectionText, "private readonly record struct CaptureCommandProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsUserSettingsProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var userSettingsProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var userSettings = BuildUserSettingsProjection(viewModelSnapshot);");
        AssertContains(snapshotProjectionText, "SelectedDeviceId = userSettings.SelectedDeviceId,");
        AssertContains(snapshotProjectionText, "SelectedFriendlyFrameRate = userSettings.SelectedFriendlyFrameRate,");
        AssertContains(snapshotProjectionText, "SelectedRecordingFormat = userSettings.SelectedRecordingFormat,");
        AssertContains(snapshotProjectionText, "IsStatsVisible = userSettings.IsStatsVisible,");
        AssertDoesNotContain(snapshotProjectionText, "SelectedDeviceId = viewModelSnapshot.SelectedDeviceId,");
        AssertDoesNotContain(snapshotProjectionText, "SelectedFriendlyFrameRate = viewModelSnapshot.SelectedFriendlyFrameRate ?? Math.Round(viewModelSnapshot.SelectedFrameRate),");
        AssertDoesNotContain(snapshotProjectionText, "IsStatsVisible = viewModelSnapshot.IsStatsVisible,");

        AssertContains(userSettingsProjectionText, "private static UserSettingsProjection BuildUserSettingsProjection(ViewModelRuntimeSnapshot viewModelSnapshot)");
        AssertContains(userSettingsProjectionText, "SelectedDeviceId = viewModelSnapshot.SelectedDeviceId,");
        AssertContains(userSettingsProjectionText, "SelectedFriendlyFrameRate = viewModelSnapshot.SelectedFriendlyFrameRate ?? Math.Round(viewModelSnapshot.SelectedFrameRate),");
        AssertContains(userSettingsProjectionText, "SelectedRecordingFormat = viewModelSnapshot.SelectedRecordingFormat,");
        AssertContains(userSettingsProjectionText, "IsStatsVisible = viewModelSnapshot.IsStatsVisible");
        AssertContains(userSettingsProjectionText, "private readonly record struct UserSettingsProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsCaptureFormatProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var captureFormatProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureFormat = BuildCaptureFormatProjection(captureRuntime);");
        AssertContains(snapshotProjectionText, "RequestedWidth = captureFormat.RequestedWidth,");
        AssertContains(snapshotProjectionText, "HdrActivationReason = captureFormat.HdrActivationReason,");
        AssertContains(snapshotProjectionText, "NegotiatedWidth = captureFormat.NegotiatedWidth,");
        AssertContains(snapshotProjectionText, "LatestObservedFramePixelFormat = captureFormat.LatestObservedFramePixelFormat,");
        AssertContains(snapshotProjectionText, "EncoderVideoCodec = captureFormat.EncoderVideoCodec,");
        AssertDoesNotContain(snapshotProjectionText, "RequestedWidth = captureRuntime.RequestedWidth,");
        AssertDoesNotContain(snapshotProjectionText, "HdrActivationReason = captureRuntime.HdrActivationReason,");
        AssertDoesNotContain(snapshotProjectionText, "NegotiatedWidth = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,");
        AssertDoesNotContain(snapshotProjectionText, "LatestObservedFramePixelFormat = captureRuntime.LatestObservedFramePixelFormat,");
        AssertDoesNotContain(snapshotProjectionText, "EncoderVideoCodec = captureRuntime.EncoderVideoCodec,");

        AssertContains(captureFormatProjectionText, "private static CaptureFormatProjection BuildCaptureFormatProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(captureFormatProjectionText, "private readonly record struct CaptureFormatProjection");
        AssertContains(captureFormatProjectionText, "RequestedWidth = captureRuntime.RequestedWidth,");
        AssertContains(captureFormatProjectionText, "HdrActivationReason = captureRuntime.HdrActivationReason,");
        AssertContains(captureFormatProjectionText, "NegotiatedWidth = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,");
        AssertContains(captureFormatProjectionText, "LatestObservedFramePixelFormat = captureRuntime.LatestObservedFramePixelFormat,");
        AssertContains(captureFormatProjectionText, "EncoderVideoCodec = captureRuntime.EncoderVideoCodec,");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsCaptureTransportProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var captureTransportProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureTransport.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureTransport = BuildCaptureTransportProjection(captureRuntime);");
        AssertContains(snapshotProjectionText, "MemoryPreference = captureTransport.MemoryPreference,");
        AssertContains(snapshotProjectionText, "VideoNegotiatedSubtype = captureTransport.VideoNegotiatedSubtype,");
        AssertContains(snapshotProjectionText, "FrameLedgerRecentEvents = captureTransport.FrameLedgerRecentEvents,");
        AssertDoesNotContain(snapshotProjectionText, "MemoryPreference = captureRuntime.MemoryPreference,");
        AssertDoesNotContain(snapshotProjectionText, "FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents,");

        AssertContains(captureTransportProjectionText, "private static CaptureTransportProjection BuildCaptureTransportProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(captureTransportProjectionText, "MemoryPreference = captureRuntime.MemoryPreference,");
        AssertContains(captureTransportProjectionText, "VideoNegotiatedSubtype = captureRuntime.VideoNegotiatedSubtype,");
        AssertContains(captureTransportProjectionText, "FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents");
        AssertContains(captureTransportProjectionText, "private readonly record struct CaptureTransportProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsHdrPipelineProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var hdrPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var hdrPipeline = BuildHdrPipelineProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotProjectionText, "IsHdrAvailable = hdrPipeline.IsHdrAvailable,");
        AssertContains(snapshotProjectionText, "HdrRuntimeState = hdrPipeline.HdrRuntimeState,");
        AssertContains(snapshotProjectionText, "HdrWarmupObservedNonP010Frames = hdrPipeline.HdrWarmupObservedNonP010Frames,");
        AssertContains(snapshotProjectionText, "PipelineModeStatus = hdrPipeline.PipelineModeStatus,");
        AssertContains(snapshotProjectionText, "TelemetryAlignmentReason = hdrPipeline.TelemetryAlignmentReason,");
        AssertDoesNotContain(snapshotProjectionText, "IsHdrAvailable = viewModelSnapshot.IsHdrAvailable,");
        AssertDoesNotContain(snapshotProjectionText, "HdrRuntimeState = !string.IsNullOrWhiteSpace(viewModelSnapshot.HdrRuntimeState)");
        AssertDoesNotContain(snapshotProjectionText, "HdrWarmupObservedNonP010Frames = captureRuntime.HdrWarmupObservedNonP010Frames,");
        AssertDoesNotContain(snapshotProjectionText, "PipelineModeStatus = captureRuntime.PipelineModeStatus,");
        AssertDoesNotContain(snapshotProjectionText, "TelemetryAlignmentReason = captureRuntime.TelemetryAlignmentReason,");

        AssertContains(hdrPipelineProjectionText, "private static HdrPipelineProjection BuildHdrPipelineProjection(");
        AssertContains(hdrPipelineProjectionText, "IsHdrAvailable = viewModelSnapshot.IsHdrAvailable,");
        AssertContains(hdrPipelineProjectionText, "HdrRuntimeState = PreferViewModelHdrText(viewModelSnapshot.HdrRuntimeState, captureRuntime.HdrRuntimeState),");
        AssertContains(hdrPipelineProjectionText, "HdrWarmupObservedNonP010Frames = captureRuntime.HdrWarmupObservedNonP010Frames,");
        AssertContains(hdrPipelineProjectionText, "PipelineModeStatus = captureRuntime.PipelineModeStatus,");
        AssertContains(hdrPipelineProjectionText, "TelemetryAlignmentReason = captureRuntime.TelemetryAlignmentReason");
        AssertContains(hdrPipelineProjectionText, "private static string PreferViewModelHdrText(string viewModelValue, string runtimeValue)");
        AssertContains(hdrPipelineProjectionText, "private readonly record struct HdrPipelineProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsSourceTelemetryProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var sourceTelemetryProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceTelemetry.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var sourceTelemetry = BuildSourceTelemetryProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotProjectionText, "SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,");
        AssertContains(snapshotProjectionText, "SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,");
        AssertContains(snapshotProjectionText, "SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,");
        AssertContains(snapshotProjectionText, "SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText,");
        AssertDoesNotContain(snapshotProjectionText, "SourceTelemetryAvailability = !string.IsNullOrWhiteSpace(viewModelSnapshot.SourceTelemetryAvailability)");
        AssertDoesNotContain(snapshotProjectionText, "SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(");
        AssertDoesNotContain(snapshotProjectionText, "SourceTelemetryDetails = captureRuntime.SourceTelemetryDetails,");

        AssertContains(sourceTelemetryProjectionText, "private static SourceTelemetryProjection BuildSourceTelemetryProjection(");
        AssertContains(sourceTelemetryProjectionText, "private static string PreferKnownTelemetryValue(string viewModelValue, string runtimeValue)");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryAvailability = PreferKnownTelemetryValue(");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryDetails = captureRuntime.SourceTelemetryDetails,");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(");
        AssertContains(sourceTelemetryProjectionText, "private readonly record struct SourceTelemetryProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsSourceSignalProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var sourceSignalProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var sourceSignal = BuildSourceSignalProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotProjectionText, "DetectedSourceFrameRate = sourceSignal.DetectedFrameRate,");
        AssertContains(snapshotProjectionText, "SourceFrameRateOrigin = sourceSignal.FrameRateOrigin,");
        AssertContains(snapshotProjectionText, "SourceRawTimingHex = sourceSignal.RawTimingHex,");
        AssertDoesNotContain(snapshotProjectionText, "DetectedSourceFrameRate = viewModelSnapshot.DetectedSourceFrameRate ?? captureRuntime.DetectedSourceFrameRate,");
        AssertDoesNotContain(snapshotProjectionText, "SourceRawTimingHex = captureRuntime.SourceRawTimingHex,");

        AssertContains(sourceSignalProjectionText, "private static SourceSignalProjection BuildSourceSignalProjection(");
        AssertContains(sourceSignalProjectionText, "DetectedFrameRate = viewModelSnapshot.DetectedSourceFrameRate ?? captureRuntime.DetectedSourceFrameRate,");
        AssertContains(sourceSignalProjectionText, "FrameRateOrigin = ResolveSourceFrameRateOrigin(viewModelSnapshot.SourceFrameRateOrigin, captureRuntime.SourceFrameRateOrigin),");
        AssertContains(sourceSignalProjectionText, "RawTimingHex = captureRuntime.SourceRawTimingHex");
        AssertContains(sourceSignalProjectionText, "private static string ResolveSourceFrameRateOrigin(string viewModelOrigin, string runtimeOrigin)");
        AssertContains(sourceSignalProjectionText, "private readonly record struct SourceSignalProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsCaptureCadenceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var captureCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureCadence = BuildCaptureCadenceProjection(health);");
        AssertContains(snapshotProjectionText, "ExpectedCaptureFrameRate = captureCadence.ExpectedFrameRate,");
        AssertContains(snapshotProjectionText, "CaptureCadenceEstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames,");
        AssertContains(snapshotProjectionText, "VisualCadenceMotionConfidence = captureCadence.VisualMotionConfidence,");
        AssertContains(snapshotProjectionText, "VisualCenterCadenceRecentChangeIntervalsMs = captureCadence.VisualCenterRecentChangeIntervalsMs,");
        AssertDoesNotContain(snapshotProjectionText, "ExpectedCaptureFrameRate = health.ExpectedFrameRate,");
        AssertDoesNotContain(snapshotProjectionText, "CaptureCadenceEstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,");
        AssertDoesNotContain(snapshotProjectionText, "VisualCadenceMotionConfidence = health.VisualCadenceMotionConfidence,");
        AssertDoesNotContain(snapshotProjectionText, "VisualCenterCadenceRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs,");

        AssertContains(captureCadenceProjectionText, "private static CaptureCadenceProjection BuildCaptureCadenceProjection(CaptureHealthSnapshot health)");
        AssertContains(captureCadenceProjectionText, "ExpectedFrameRate = health.ExpectedFrameRate,");
        AssertContains(captureCadenceProjectionText, "EstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,");
        AssertContains(captureCadenceProjectionText, "VisualMotionConfidence = health.VisualCadenceMotionConfidence,");
        AssertContains(captureCadenceProjectionText, "VisualCenterRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs");
        AssertContains(captureCadenceProjectionText, "private readonly record struct CaptureCadenceProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsMjpegProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var mjpegProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs")
            .Replace("\r\n", "\n");
        var mjpegTimingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegTiming.cs")
            .Replace("\r\n", "\n");
        var mjpegPreviewJitterProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs")
            .Replace("\r\n", "\n");
        var mjpegPacketHashProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPacketHash.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var mjpeg = BuildMjpegProjection(health);");
        AssertContains(snapshotProjectionText, "MjpegDecodeSampleCount = mjpeg.Timing.DecodeSampleCount,");
        AssertContains(snapshotProjectionText, "MjpegPreviewJitterLastDropReason = mjpeg.PreviewJitter.LastDropReason,");
        AssertContains(snapshotProjectionText, "MjpegPacketHashPattern = mjpeg.PacketHash.Pattern,");
        AssertContains(snapshotProjectionText, "MjpegPerDecoder = mjpeg.Timing.PerDecoder,");
        AssertDoesNotContain(snapshotProjectionText, "MjpegDecodeSampleCount = health.MjpegDecodeSampleCount,");
        AssertDoesNotContain(snapshotProjectionText, "MjpegDecodeSampleCount = mjpeg.DecodeSampleCount,");
        AssertDoesNotContain(snapshotProjectionText, "MjpegPreviewJitterLastDropReason = mjpeg.PreviewJitterLastDropReason,");
        AssertDoesNotContain(snapshotProjectionText, "MjpegPreviewJitterLastDropReason = health.MjpegPreviewJitterLastDropReason,");
        AssertDoesNotContain(snapshotProjectionText, "MjpegPacketHashPattern = mjpeg.PacketHashPattern,");
        AssertDoesNotContain(snapshotProjectionText, "MjpegPacketHashPattern = health.MjpegPacketHashPattern,");
        AssertDoesNotContain(snapshotProjectionText, "MjpegPerDecoder = mjpeg.PerDecoder,");
        AssertDoesNotContain(snapshotProjectionText, "MjpegPerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder");

        AssertContains(mjpegProjectionText, "private static MjpegProjection BuildMjpegProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegProjectionText, "var timing = BuildMjpegTimingProjection(health);");
        AssertContains(mjpegProjectionText, "Timing = timing,");
        AssertContains(mjpegProjectionText, "var previewJitter = BuildMjpegPreviewJitterProjection(health);");
        AssertContains(mjpegProjectionText, "var packetHash = BuildMjpegPacketHashProjection(health);");
        AssertContains(mjpegProjectionText, "PreviewJitter = previewJitter,");
        AssertDoesNotContain(mjpegProjectionText, "PreviewJitterLastDropReason = health.MjpegPreviewJitterLastDropReason,");
        AssertContains(mjpegProjectionText, "PacketHash = packetHash,");
        AssertDoesNotContain(mjpegProjectionText, "PacketHashPattern = health.MjpegPacketHashPattern,");
        AssertDoesNotContain(mjpegProjectionText, "DecodeSampleCount = health.MjpegDecodeSampleCount,");
        AssertDoesNotContain(mjpegProjectionText, "PerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder");
        AssertContains(mjpegProjectionText, "private readonly record struct MjpegProjection");
        AssertContains(mjpegTimingProjectionText, "private static MjpegTimingProjection BuildMjpegTimingProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegTimingProjectionText, "DecodeSampleCount = health.MjpegDecodeSampleCount,");
        AssertContains(mjpegTimingProjectionText, "PipelineMaxMs = health.MjpegPipelineMaxMs,");
        AssertContains(mjpegTimingProjectionText, "PerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder");
        AssertContains(mjpegTimingProjectionText, "private readonly record struct MjpegTimingProjection");

        AssertContains(mjpegPreviewJitterProjectionText, "private static MjpegPreviewJitterProjection BuildMjpegPreviewJitterProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegPreviewJitterProjectionText, "Enabled = health.MjpegPreviewJitterEnabled,");
        AssertContains(mjpegPreviewJitterProjectionText, "LastDropReason = health.MjpegPreviewJitterLastDropReason,");
        AssertContains(mjpegPreviewJitterProjectionText, "ScheduleLateCount = health.MjpegPreviewJitterScheduleLateCount");
        AssertContains(mjpegPreviewJitterProjectionText, "private readonly record struct MjpegPreviewJitterProjection");

        AssertContains(mjpegPacketHashProjectionText, "private static MjpegPacketHashProjection BuildMjpegPacketHashProjection(CaptureHealthSnapshot health)");
        AssertContains(mjpegPacketHashProjectionText, "SampleCount = health.MjpegPacketHashSampleCount,");
        AssertContains(mjpegPacketHashProjectionText, "Pattern = health.MjpegPacketHashPattern,");
        AssertContains(mjpegPacketHashProjectionText, "RecentDuplicateFlags = health.MjpegPacketHashRecentDuplicateFlags");
        AssertContains(mjpegPacketHashProjectionText, "private readonly record struct MjpegPacketHashProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsRecordingPipelineProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var recordingPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var recordingPipeline = BuildRecordingPipelineProjection(health);");
        AssertContains(snapshotProjectionText, "EncoderVideoFramesEnqueued = recordingPipeline.EncoderVideoFramesEnqueued,");
        AssertContains(snapshotProjectionText, "ConversionQueueDepth = recordingPipeline.ConversionQueueDepth,");
        AssertContains(snapshotProjectionText, "RecordingVideoQueueCapacity = recordingPipeline.RecordingVideoQueueCapacity,");
        AssertContains(snapshotProjectionText, "RecordingGpuFramesEnqueued = recordingPipeline.RecordingGpuFramesEnqueued,");
        AssertContains(snapshotProjectionText, "RecordingCudaFramesDropped = recordingPipeline.RecordingCudaFramesDropped,");
        AssertDoesNotContain(snapshotProjectionText, "EncoderVideoFramesEnqueued = health.VideoFramesEnqueued,");
        AssertDoesNotContain(snapshotProjectionText, "ConversionQueueDepth = health.ConversionQueueDepth,");
        AssertDoesNotContain(snapshotProjectionText, "RecordingVideoQueueCapacity = health.RecordingVideoQueueCapacity,");
        AssertDoesNotContain(snapshotProjectionText, "RecordingCudaFramesDropped = health.RecordingCudaFramesDropped,");

        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineProjection BuildRecordingPipelineProjection(CaptureHealthSnapshot health)");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingPipelineProjection");
        AssertContains(recordingPipelineProjectionText, "EncoderVideoFramesEnqueued = health.VideoFramesEnqueued,");
        AssertContains(recordingPipelineProjectionText, "ConversionQueueDepth = health.ConversionQueueDepth,");
        AssertContains(recordingPipelineProjectionText, "RecordingVideoQueueCapacity = health.RecordingVideoQueueCapacity,");
        AssertContains(recordingPipelineProjectionText, "RecordingCudaFramesDropped = health.RecordingCudaFramesDropped");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsRecordingBackendProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var recordingBackendProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingBackend.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var recordingBackend = BuildRecordingBackendProjection(captureRuntime);");
        AssertContains(snapshotProjectionText, "RecordingBackend = recordingBackend.Backend,");
        AssertContains(snapshotProjectionText, "AudioPathMode = recordingBackend.AudioPathMode,");
        AssertContains(snapshotProjectionText, "MuxResult = recordingBackend.MuxResult,");
        AssertDoesNotContain(snapshotProjectionText, "RecordingBackend = captureRuntime.RecordingBackend,");
        AssertDoesNotContain(snapshotProjectionText, "MuxResult = captureRuntime.MuxSucceeded.HasValue");

        AssertContains(recordingBackendProjectionText, "private static RecordingBackendProjection BuildRecordingBackendProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(recordingBackendProjectionText, "Backend = captureRuntime.RecordingBackend,");
        AssertContains(recordingBackendProjectionText, "AudioPathMode = captureRuntime.AudioPathMode,");
        AssertContains(recordingBackendProjectionText, "MuxResult = ResolveMuxResult(captureRuntime.MuxSucceeded)");
        AssertContains(recordingBackendProjectionText, "private static string ResolveMuxResult(bool? muxSucceeded)");
        AssertContains(recordingBackendProjectionText, "private readonly record struct RecordingBackendProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsRecordingOutputProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var recordingOutputProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var recordingOutput = BuildRecordingOutputProjection(");
        AssertContains(snapshotProjectionText, "OutputPath = recordingOutput.OutputPath,");
        AssertContains(snapshotProjectionText, "RecordingVideoBytes = recordingOutput.RecordingVideoBytes,");
        AssertContains(snapshotProjectionText, "LastOutputPath = recordingOutput.LastOutputPath,");
        AssertContains(snapshotProjectionText, "LastVerification = recordingOutput.LastVerification,");
        AssertDoesNotContain(snapshotProjectionText, "OutputPath = viewModelSnapshot.OutputPath,");
        AssertDoesNotContain(snapshotProjectionText, "RecordingVideoBytes = recordingStats.VideoBytes,");
        AssertDoesNotContain(snapshotProjectionText, "LastOutputPath = captureRuntime.LastOutputPath,");
        AssertDoesNotContain(snapshotProjectionText, "LastOutputSizeBytes = lastOutput.SizeBytes,");

        AssertContains(recordingOutputProjectionText, "private static RecordingOutputProjection BuildRecordingOutputProjection(");
        AssertContains(recordingOutputProjectionText, "OutputPath = viewModelSnapshot.OutputPath,");
        AssertContains(recordingOutputProjectionText, "RecordingVideoBytes = recordingStats.VideoBytes,");
        AssertContains(recordingOutputProjectionText, "LastOutputPath = captureRuntime.LastOutputPath,");
        AssertContains(recordingOutputProjectionText, "LastOutputSizeBytes = lastOutput.SizeBytes,");
        AssertContains(recordingOutputProjectionText, "LastVerification = lastVerification");
        AssertContains(recordingOutputProjectionText, "private readonly record struct RecordingOutputProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsProcessResourceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var processResourceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.ProcessResources.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var processResourceProjection = BuildProcessResourceProjection(processResources);");
        AssertContains(snapshotProjectionText, "MemoryWorkingSetMb = processResourceProjection.MemoryWorkingSetMb,");
        AssertContains(snapshotProjectionText, "MemoryGcFragmentationPercent = processResourceProjection.MemoryGcFragmentationPercent,");
        AssertContains(snapshotProjectionText, "ThreadPoolIoMax = processResourceProjection.ThreadPoolIoMax,");
        AssertDoesNotContain(snapshotProjectionText, "MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,");
        AssertDoesNotContain(snapshotProjectionText, "MemoryGcFragmentationPercent = processResources.MemoryGcFragmentationPercent,");
        AssertDoesNotContain(snapshotProjectionText, "ThreadPoolIoMax = processResources.ThreadPoolIoMax,");

        AssertContains(processResourceProjectionText, "private static ProcessResourceProjection BuildProcessResourceProjection(ProcessResourceSnapshot processResources)");
        AssertContains(processResourceProjectionText, "MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,");
        AssertContains(processResourceProjectionText, "MemoryGcFragmentationPercent = processResources.MemoryGcFragmentationPercent,");
        AssertContains(processResourceProjectionText, "ThreadPoolIoMax = processResources.ThreadPoolIoMax");
        AssertContains(processResourceProjectionText, "private readonly record struct ProcessResourceProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsAvSyncProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var avSyncProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.AvSync.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var avSync = BuildAvSyncProjection(captureRuntime);");
        AssertContains(snapshotProjectionText, "AvSyncCaptureDriftMs = avSync.CaptureDriftMs,");
        AssertContains(snapshotProjectionText, "AvSyncCaptureDriftRateMsPerSec = avSync.CaptureDriftRateMsPerSec,");
        AssertContains(snapshotProjectionText, "AvSyncEncoderCorrectionSamples = avSync.EncoderCorrectionSamples,");
        AssertDoesNotContain(snapshotProjectionText, "AvSyncCaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,");
        AssertDoesNotContain(snapshotProjectionText, "AvSyncEncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples,");

        AssertContains(avSyncProjectionText, "private static AvSyncProjection BuildAvSyncProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(avSyncProjectionText, "CaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,");
        AssertContains(avSyncProjectionText, "CaptureDriftRateMsPerSec = captureRuntime.AvSyncCaptureDriftRateMsPerSec,");
        AssertContains(avSyncProjectionText, "EncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples");
        AssertContains(avSyncProjectionText, "private readonly record struct AvSyncProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsPreviewRuntimeProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var previewRuntimeProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs")
            .Replace("\r\n", "\n");
        var previewRuntimeCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntimeCadence.cs")
            .Replace("\r\n", "\n");
        var previewRuntimeStartupProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntimeStartup.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var previewSummary = BuildPreviewRuntimeProjection(previewRuntime, previewHdrState, captureRuntime);");
        AssertContains(snapshotProjectionText, "PreviewFramesArrived = previewSummary.FramesArrived,");
        AssertContains(snapshotProjectionText, "PreviewCadenceOnePercentLowFps = previewSummary.Cadence.OnePercentLowFps,");
        AssertContains(snapshotProjectionText, "PreviewStartupStrategy = previewSummary.Startup.Strategy,");
        AssertContains(snapshotProjectionText, "PreviewRendererMode = previewSummary.Startup.RendererMode,");
        AssertContains(snapshotProjectionText, "PreviewGpuPlaybackState = previewSummary.GpuPlaybackState,");
        AssertContains(snapshotProjectionText, "PreviewColorContext = previewSummary.ColorContext,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewFramesArrived = previewRuntime.FramesArrived,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewCadenceOnePercentLowFps = previewSummary.CadenceOnePercentLowFps,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewCadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewStartupStrategy = previewSummary.StartupStrategy,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewRendererMode = previewSummary.RendererMode,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewStartupStrategy = previewRuntime.StartupStrategy.ToString(),");
        AssertDoesNotContain(snapshotProjectionText, "PreviewGpuPlaybackState = previewRuntime.GpuPlaybackState,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewColorContext = captureRuntime.NegotiatedPixelFormat,");

        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeProjection BuildPreviewRuntimeProjection(");
        AssertContains(previewRuntimeProjectionText, "var cadence = BuildPreviewRuntimeCadenceProjection(previewRuntime);");
        AssertContains(previewRuntimeProjectionText, "Cadence = cadence,");
        AssertContains(previewRuntimeProjectionText, "var startup = BuildPreviewRuntimeStartupProjection(previewRuntime);");
        AssertContains(previewRuntimeProjectionText, "Startup = startup,");
        AssertContains(previewRuntimeProjectionText, "FramesArrived = previewRuntime.FramesArrived,");
        AssertDoesNotContain(previewRuntimeProjectionText, "CadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertDoesNotContain(previewRuntimeProjectionText, "CadenceSlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent,");
        AssertDoesNotContain(previewRuntimeProjectionText, "StartupStrategy = previewRuntime.StartupStrategy.ToString(),");
        AssertDoesNotContain(previewRuntimeProjectionText, "RendererMode = previewRuntime.RendererMode,");
        AssertContains(previewRuntimeProjectionText, "GpuPlaybackState = previewRuntime.GpuPlaybackState,");
        AssertContains(previewRuntimeProjectionText, "HdrInputDetected = previewHdrState.InputDetected,");
        AssertContains(previewRuntimeProjectionText, "ColorContext = captureRuntime.NegotiatedPixelFormat,");
        AssertContains(previewRuntimeProjectionText, "private readonly record struct PreviewRuntimeProjection");
        AssertContains(previewRuntimeCadenceProjectionText, "private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(");
        AssertContains(previewRuntimeCadenceProjectionText, "OnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertContains(previewRuntimeCadenceProjectionText, "RecentIntervalsMs = previewRuntime.DisplayCadenceRecentIntervalsMs,");
        AssertContains(previewRuntimeCadenceProjectionText, "SlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent");
        AssertContains(previewRuntimeCadenceProjectionText, "private readonly record struct PreviewRuntimeCadenceProjection");
        AssertContains(previewRuntimeStartupProjectionText, "private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(");
        AssertContains(previewRuntimeStartupProjectionText, "Strategy = previewRuntime.StartupStrategy.ToString(),");
        AssertContains(previewRuntimeStartupProjectionText, "FirstVisualConfirmed = previewRuntime.FirstVisualConfirmed,");
        AssertContains(previewRuntimeStartupProjectionText, "RendererMode = previewRuntime.RendererMode");
        AssertContains(previewRuntimeStartupProjectionText, "private readonly record struct PreviewRuntimeStartupProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsPreviewD3DProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var previewD3DProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs")
            .Replace("\r\n", "\n");
        var previewD3DCpuTimingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DCpuTiming.cs")
            .Replace("\r\n", "\n");
        var previewD3DFrameFlowProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameFlow.cs")
            .Replace("\r\n", "\n");
        var previewD3DFrameLatencyWaitProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameLatencyWait.cs")
            .Replace("\r\n", "\n");
        var previewD3DFrameStatsProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameStats.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var previewD3D = BuildPreviewD3DProjection(\n            previewRuntime,\n            recentD3DMissedRefreshes,\n            recentD3DStatsFailures);");
        AssertContains(snapshotProjectionText, "PreviewD3DPresentSyncInterval = previewD3D.PresentSyncInterval,");
        AssertContains(snapshotProjectionText, "PreviewD3DInputUploadCpuP99Ms = previewD3D.CpuTiming.InputUploadP99Ms,");
        AssertContains(snapshotProjectionText, "PreviewD3DPipelineLatencyMaxMs = previewD3D.CpuTiming.PipelineLatencyMaxMs,");
        AssertContains(snapshotProjectionText, "PreviewD3DFrameLatencyWaitTimeoutCount = previewD3D.FrameLatencyWait.TimeoutCount,");
        AssertContains(snapshotProjectionText, "PreviewD3DFrameStatsRecentMissedRefreshCount = previewD3D.FrameStats.RecentMissedRefreshCount,");
        AssertContains(snapshotProjectionText, "PreviewD3DRecentSlowFrames = previewD3D.FrameFlow.RecentSlowFrames,");
        AssertContains(snapshotProjectionText, "PreviewD3DLastRenderedPipelineLatencyMs = previewD3D.FrameFlow.LastRenderedPipelineLatencyMs,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewD3DPresentSyncInterval = previewRuntime.D3DPresentSyncInterval,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewD3DInputUploadCpuP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewD3DInputUploadCpuP99Ms = previewD3D.InputUploadCpuP99Ms,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewD3DPipelineLatencyMaxMs = previewD3D.PipelineLatencyMaxMs,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewD3DFrameLatencyWaitTimeoutCount = previewD3D.FrameLatencyWaitTimeoutCount,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewD3DFrameStatsRecentMissedRefreshCount = previewD3D.FrameStatsRecentMissedRefreshCount,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewD3DFrameStatsRecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewD3DRecentSlowFrames = previewD3D.RecentSlowFrames,");
        AssertDoesNotContain(snapshotProjectionText, "PreviewD3DLastRenderedPipelineLatencyMs = previewD3D.LastRenderedPipelineLatencyMs,");

        AssertContains(previewD3DProjectionText, "private static PreviewD3DProjection BuildPreviewD3DProjection(");
        AssertContains(previewD3DProjectionText, "var cpuTiming = BuildPreviewD3DCpuTimingProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "CpuTiming = cpuTiming,");
        AssertContains(previewD3DProjectionText, "var frameFlow = BuildPreviewD3DFrameFlowProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "FrameFlow = frameFlow");
        AssertContains(previewD3DProjectionText, "var frameLatencyWait = BuildPreviewD3DFrameLatencyWaitProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "var frameStats = BuildPreviewD3DFrameStatsProjection(");
        AssertContains(previewD3DProjectionText, "FrameLatencyWait = frameLatencyWait,");
        AssertContains(previewD3DProjectionText, "FrameStats = frameStats,");
        AssertDoesNotContain(previewD3DProjectionText, "InputUploadCpuP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,");
        AssertDoesNotContain(previewD3DProjectionText, "PipelineLatencyMaxMs = previewRuntime.D3DPipelineLatencyMaxMs,");
        AssertDoesNotContain(previewD3DProjectionText, "LastRenderedPipelineLatencyMs = previewRuntime.D3DLastRenderedPipelineLatencyMs,");
        AssertDoesNotContain(previewD3DProjectionText, "RecentSlowFrames = previewRuntime.D3DRecentSlowFrames");
        AssertDoesNotContain(previewD3DProjectionText, "FrameLatencyWaitTimeoutCount = previewRuntime.D3DFrameLatencyWaitTimeoutCount,");
        AssertDoesNotContain(previewD3DProjectionText, "FrameStatsRecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertContains(previewD3DCpuTimingProjectionText, "private static PreviewD3DCpuTimingProjection BuildPreviewD3DCpuTimingProjection(");
        AssertContains(previewD3DCpuTimingProjectionText, "SampleCount = previewRuntime.D3DCpuTimingSampleCount,");
        AssertContains(previewD3DCpuTimingProjectionText, "InputUploadP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,");
        AssertContains(previewD3DCpuTimingProjectionText, "PipelineLatencyMaxMs = previewRuntime.D3DPipelineLatencyMaxMs");
        AssertContains(previewD3DCpuTimingProjectionText, "private readonly record struct PreviewD3DCpuTimingProjection");
        AssertContains(previewD3DFrameFlowProjectionText, "private static PreviewD3DFrameFlowProjection BuildPreviewD3DFrameFlowProjection(");
        AssertContains(previewD3DFrameFlowProjectionText, "LastRenderedPipelineLatencyMs = previewRuntime.D3DLastRenderedPipelineLatencyMs,");
        AssertContains(previewD3DFrameFlowProjectionText, "RecentSlowFrames = previewRuntime.D3DRecentSlowFrames");
        AssertContains(previewD3DFrameFlowProjectionText, "private readonly record struct PreviewD3DFrameFlowProjection");
        AssertContains(previewD3DFrameLatencyWaitProjectionText, "private static PreviewD3DFrameLatencyWaitProjection BuildPreviewD3DFrameLatencyWaitProjection(");
        AssertContains(previewD3DFrameLatencyWaitProjectionText, "Enabled = previewRuntime.D3DFrameLatencyWaitEnabled,");
        AssertContains(previewD3DFrameLatencyWaitProjectionText, "TimeoutCount = previewRuntime.D3DFrameLatencyWaitTimeoutCount,");
        AssertContains(previewD3DFrameLatencyWaitProjectionText, "MaxMs = previewRuntime.D3DFrameLatencyWaitMaxMs");
        AssertContains(previewD3DFrameLatencyWaitProjectionText, "private readonly record struct PreviewD3DFrameLatencyWaitProjection");

        AssertContains(previewD3DFrameStatsProjectionText, "private static PreviewD3DFrameStatsProjection BuildPreviewD3DFrameStatsProjection(");
        AssertContains(previewD3DFrameStatsProjectionText, "SampleCount = previewRuntime.D3DFrameStatsSampleCount,");
        AssertContains(previewD3DFrameStatsProjectionText, "RecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertContains(previewD3DFrameStatsProjectionText, "RecentFailureCount = recentD3DStatsFailures");
        AssertContains(previewD3DFrameStatsProjectionText, "private readonly record struct PreviewD3DFrameStatsProjection");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsFlashbackExportProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var flashbackExportProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var flashbackExport = BuildFlashbackExportProjection(health);");
        AssertContains(snapshotProjectionText, "FlashbackExportActive = flashbackExport.Active,");
        AssertContains(snapshotProjectionText, "FlashbackExportPercent = flashbackExport.Percent,");
        AssertContains(snapshotProjectionText, "FlashbackExportLastForceRotateFallbackSegments = flashbackExport.LastForceRotateFallbackSegments,");
        AssertContains(snapshotProjectionText, "LastExportId = flashbackExport.LastExportId,");
        AssertContains(snapshotProjectionText, "LastExportMessage = flashbackExport.LastExportMessage");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackExportActive = health.FlashbackExportActive,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackExportPercent = health.FlashbackExportPercent,");
        AssertDoesNotContain(snapshotProjectionText, "LastExportId = health.LastExportId,");

        AssertContains(flashbackExportProjectionText, "private static FlashbackExportProjection BuildFlashbackExportProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackExportProjectionText, "Active = health.FlashbackExportActive,");
        AssertContains(flashbackExportProjectionText, "Percent = health.FlashbackExportPercent,");
        AssertContains(flashbackExportProjectionText, "LastForceRotateFallbackSegments = health.FlashbackExportLastForceRotateFallbackSegments,");
        AssertContains(flashbackExportProjectionText, "LastExportId = health.LastExportId,");
        AssertContains(flashbackExportProjectionText, "private readonly record struct FlashbackExportProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsFlashbackRecordingProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingStartupCacheProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingStartupCache.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingQueuesProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingQueues.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var flashbackRecording = BuildFlashbackRecordingProjection(captureRuntime, health);");
        AssertContains(snapshotProjectionText, "FlashbackEncodingFailed = flashbackRecording.EncodingFailed,");
        AssertContains(snapshotProjectionText, "FlashbackStartupCacheOverBudget = flashbackRecording.StartupCache.OverBudget,");
        AssertContains(snapshotProjectionText, "FlashbackVideoQueueCapacity = flashbackRecording.Queues.VideoQueueCapacity,");
        AssertContains(snapshotProjectionText, "FlashbackGpuQueueLastRejectReason = flashbackRecording.Queues.GpuQueueLastRejectReason,");
        AssertContains(snapshotProjectionText, "FlashbackActive = flashbackRecording.Active,");
        AssertContains(snapshotProjectionText, "FlashbackBackendSettingsStale = flashbackRecording.BackendSettingsStale,");
        AssertContains(snapshotProjectionText, "FlashbackExportVerificationFormat = flashbackRecording.ExportVerificationFormat,");
        AssertContains(snapshotProjectionText, "EncoderCodecName = flashbackRecording.EncoderCodecName,");
        AssertContains(snapshotProjectionText, "FlashbackAudioQueueCapacity = flashbackRecording.Queues.AudioQueueCapacity,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackEncodingFailed = health.FlashbackEncodingFailed,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackVideoQueueCapacity = health.FlashbackVideoQueueCapacity,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackGpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackActive = health.FlashbackActive,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertDoesNotContain(snapshotProjectionText, "EncoderCodecName = health.EncoderCodecName,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackStartupCacheOverBudget = flashbackRecording.StartupCacheOverBudget,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackStartupCacheOverBudget = health.FlashbackStartupCacheOverBudget,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackVideoQueueCapacity = flashbackRecording.VideoQueueCapacity,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackGpuQueueLastRejectReason = flashbackRecording.GpuQueueLastRejectReason,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackAudioQueueCapacity = flashbackRecording.AudioQueueCapacity,");

        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingProjection BuildFlashbackRecordingProjection(");
        AssertContains(flashbackRecordingProjectionText, "CaptureRuntimeSnapshot captureRuntime,");
        AssertContains(flashbackRecordingProjectionText, "var startupCache = BuildFlashbackRecordingStartupCacheProjection(health);");
        AssertContains(flashbackRecordingProjectionText, "StartupCache = startupCache,");
        AssertContains(flashbackRecordingProjectionText, "var queues = BuildFlashbackRecordingQueuesProjection(health);");
        AssertContains(flashbackRecordingProjectionText, "Queues = queues,");
        AssertContains(flashbackRecordingProjectionText, "EncodingFailed = health.FlashbackEncodingFailed,");
        AssertContains(flashbackRecordingProjectionText, "ExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertContains(flashbackRecordingProjectionText, "CodecDowngradeReason = captureRuntime.FlashbackCodecDowngradeReason ?? health.FlashbackCodecDowngradeReason,");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingProjection");
        AssertDoesNotContain(flashbackRecordingProjectionText, "StartupCacheOverBudget = health.FlashbackStartupCacheOverBudget,");
        AssertDoesNotContain(flashbackRecordingProjectionText, "VideoQueueCapacity = health.FlashbackVideoQueueCapacity,");
        AssertDoesNotContain(flashbackRecordingProjectionText, "GpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,");
        AssertDoesNotContain(flashbackRecordingProjectionText, "AudioQueueCapacity = health.FlashbackAudioQueueCapacity");
        AssertContains(flashbackRecordingStartupCacheProjectionText, "private static FlashbackRecordingStartupCacheProjection BuildFlashbackRecordingStartupCacheProjection(");
        AssertContains(flashbackRecordingStartupCacheProjectionText, "TempDriveFreeBytes = health.FlashbackTempDriveFreeBytes,");
        AssertContains(flashbackRecordingStartupCacheProjectionText, "OverBudget = health.FlashbackStartupCacheOverBudget");
        AssertContains(flashbackRecordingStartupCacheProjectionText, "private readonly record struct FlashbackRecordingStartupCacheProjection");
        AssertContains(flashbackRecordingQueuesProjectionText, "private static FlashbackRecordingQueuesProjection BuildFlashbackRecordingQueuesProjection(");
        AssertContains(flashbackRecordingQueuesProjectionText, "VideoQueueCapacity = health.FlashbackVideoQueueCapacity,");
        AssertContains(flashbackRecordingQueuesProjectionText, "GpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,");
        AssertContains(flashbackRecordingQueuesProjectionText, "AudioQueueCapacity = health.FlashbackAudioQueueCapacity");
        AssertContains(flashbackRecordingQueuesProjectionText, "private readonly record struct FlashbackRecordingQueuesProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsFlashbackPlaybackProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackAudioMasterProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackAudioMaster.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackDecodeProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackDecode.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackCommandsProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var flashbackPlayback = BuildFlashbackPlaybackProjection(health);");
        AssertContains(snapshotProjectionText, "FlashbackPlaybackState = flashbackPlayback.State,");
        AssertContains(snapshotProjectionText, "FlashbackPlaybackTargetFps = flashbackPlayback.TargetFps,");
        AssertContains(snapshotProjectionText, "FlashbackPlaybackMaxDecodePhase = flashbackPlayback.Decode.MaxPhase,");
        AssertContains(snapshotProjectionText, "FlashbackPlaybackLastCommandFailure = flashbackPlayback.Commands.LastFailure,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackPlaybackState = health.FlashbackPlaybackState,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackPlaybackTargetFps = health.FlashbackPlaybackTargetFps,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackPlaybackLastCommandFailure = health.FlashbackPlaybackLastCommandFailure,");

        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackProjection BuildFlashbackPlaybackProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "var audioMaster = BuildFlashbackPlaybackAudioMasterProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "var decode = BuildFlashbackPlaybackDecodeProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "var commands = BuildFlashbackPlaybackCommandProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "State = health.FlashbackPlaybackState,");
        AssertContains(flashbackPlaybackProjectionText, "AudioMaster = audioMaster,");
        AssertContains(flashbackPlaybackProjectionText, "TargetFps = health.FlashbackPlaybackTargetFps,");
        AssertContains(flashbackPlaybackProjectionText, "Decode = decode,");
        AssertContains(flashbackPlaybackProjectionText, "Commands = commands");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "AudioMasterFallbacks = audioMaster.Fallbacks,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "AudioMasterFallbacks = health.FlashbackPlaybackAudioMasterFallbacks,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "MaxDecodePhase = decode.MaxPhase,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "MaxDecodePhase = health.FlashbackPlaybackMaxDecodePhase,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "LastCommandFailure = commands.LastFailure");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "LastCommandFailure = health.FlashbackPlaybackLastCommandFailure");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackProjection");

        AssertContains(flashbackPlaybackAudioMasterProjectionText, "private static FlashbackPlaybackAudioMasterProjection BuildFlashbackPlaybackAudioMasterProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackAudioMasterProjectionText, "Fallbacks = health.FlashbackPlaybackAudioMasterFallbacks,");
        AssertContains(flashbackPlaybackAudioMasterProjectionText, "LastFallbackReason = health.FlashbackPlaybackAudioMasterLastFallbackReason,");
        AssertContains(flashbackPlaybackAudioMasterProjectionText, "private readonly record struct FlashbackPlaybackAudioMasterProjection");

        AssertContains(flashbackPlaybackDecodeProjectionText, "private static FlashbackPlaybackDecodeProjection BuildFlashbackPlaybackDecodeProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackDecodeProjectionText, "SeekForwardDecodeCapHits = health.FlashbackPlaybackSeekForwardDecodeCapHits,");
        AssertContains(flashbackPlaybackDecodeProjectionText, "MaxPhase = health.FlashbackPlaybackMaxDecodePhase,");
        AssertContains(flashbackPlaybackDecodeProjectionText, "MaxPositionMs = health.FlashbackPlaybackMaxDecodePositionMs");
        AssertContains(flashbackPlaybackDecodeProjectionText, "private readonly record struct FlashbackPlaybackDecodeProjection");

        AssertContains(flashbackPlaybackCommandsProjectionText, "private static FlashbackPlaybackCommandProjection BuildFlashbackPlaybackCommandProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackCommandsProjectionText, "ThreadAlive = health.FlashbackPlaybackThreadAlive,");
        AssertContains(flashbackPlaybackCommandsProjectionText, "LastFailure = health.FlashbackPlaybackLastCommandFailure");
        AssertContains(flashbackPlaybackCommandsProjectionText, "private readonly record struct FlashbackPlaybackCommandProjection");

        return Task.CompletedTask;
    }

}
