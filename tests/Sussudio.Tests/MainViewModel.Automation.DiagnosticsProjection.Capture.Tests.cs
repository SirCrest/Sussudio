using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationDiagnosticsCaptureCommandProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var captureCommandProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureCommands = BuildCaptureCommandProjection(viewModelSnapshot);");
        AssertContains(snapshotFlatteningText, "CaptureCommandCommandsEnqueued = captureCommands.CommandsEnqueued,");
        AssertContains(snapshotFlatteningText, "CaptureCommandMaxQueueLatencyMs = captureCommands.MaxQueueLatencyMs,");
        AssertContains(snapshotFlatteningText, "CaptureCommandLastError = captureCommands.LastError,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandCommandsEnqueued = viewModelSnapshot.CaptureCommandCommandsEnqueued,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandMaxQueueLatencyMs = viewModelSnapshot.CaptureCommandMaxQueueLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandLastError = viewModelSnapshot.CaptureCommandLastError,");

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
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var userSettingsProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs")
            .Replace("\r\n", "\n");
        var recordingSettingsProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingSettings.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var userSettings = BuildUserSettingsProjection(viewModelSnapshot);");
        AssertContains(snapshotProjectionText, "var recordingSettings = BuildRecordingSettingsProjection(userSettings);");
        AssertContains(snapshotFlatteningText, "SelectedDeviceId = userSettings.SelectedDeviceId,");
        AssertContains(snapshotFlatteningText, "SelectedFriendlyFrameRate = userSettings.SelectedFriendlyFrameRate,");
        AssertContains(snapshotFlatteningText, "SelectedRecordingFormat = recordingSettings.SelectedRecordingFormat,");
        AssertContains(snapshotFlatteningText, "CustomBitrateMbps = recordingSettings.CustomBitrateMbps,");
        AssertContains(snapshotFlatteningText, "IsStatsVisible = userSettings.IsStatsVisible,");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedDeviceId = viewModelSnapshot.SelectedDeviceId,");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedFriendlyFrameRate = viewModelSnapshot.SelectedFriendlyFrameRate ?? Math.Round(viewModelSnapshot.SelectedFrameRate),");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedRecordingFormat = userSettings.SelectedRecordingFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "CustomBitrateMbps = userSettings.CustomBitrateMbps,");
        AssertDoesNotContain(snapshotFlatteningText, "IsStatsVisible = viewModelSnapshot.IsStatsVisible,");

        AssertContains(userSettingsProjectionText, "private static UserSettingsProjection BuildUserSettingsProjection(ViewModelRuntimeSnapshot viewModelSnapshot)");
        AssertContains(userSettingsProjectionText, "SelectedDeviceId = viewModelSnapshot.SelectedDeviceId,");
        AssertContains(userSettingsProjectionText, "SelectedFriendlyFrameRate = viewModelSnapshot.SelectedFriendlyFrameRate ?? Math.Round(viewModelSnapshot.SelectedFrameRate),");
        AssertContains(userSettingsProjectionText, "SelectedRecordingFormat = viewModelSnapshot.SelectedRecordingFormat,");
        AssertContains(userSettingsProjectionText, "IsStatsVisible = viewModelSnapshot.IsStatsVisible");
        AssertContains(userSettingsProjectionText, "private readonly record struct UserSettingsProjection");
        AssertContains(recordingSettingsProjectionText, "private static RecordingSettingsProjection BuildRecordingSettingsProjection(UserSettingsProjection userSettings)");
        AssertContains(recordingSettingsProjectionText, "SelectedRecordingFormat = userSettings.SelectedRecordingFormat,");
        AssertContains(recordingSettingsProjectionText, "SelectedVideoFormat = userSettings.SelectedVideoFormat,");
        AssertContains(recordingSettingsProjectionText, "CustomBitrateMbps = userSettings.CustomBitrateMbps");
        AssertContains(recordingSettingsProjectionText, "private readonly record struct RecordingSettingsProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsCaptureFormatProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var captureFormatProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureFormat = BuildCaptureFormatProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "RequestedWidth = captureFormat.RequestedWidth,");
        AssertContains(snapshotFlatteningText, "HdrActivationReason = captureFormat.HdrActivationReason,");
        AssertContains(snapshotFlatteningText, "NegotiatedWidth = captureFormat.NegotiatedWidth,");
        AssertContains(snapshotFlatteningText, "LatestObservedFramePixelFormat = captureFormat.LatestObservedFramePixelFormat,");
        AssertContains(snapshotFlatteningText, "EncoderVideoCodec = captureFormat.EncoderVideoCodec,");
        AssertDoesNotContain(snapshotFlatteningText, "RequestedWidth = captureRuntime.RequestedWidth,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrActivationReason = captureRuntime.HdrActivationReason,");
        AssertDoesNotContain(snapshotFlatteningText, "NegotiatedWidth = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,");
        AssertDoesNotContain(snapshotFlatteningText, "LatestObservedFramePixelFormat = captureRuntime.LatestObservedFramePixelFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "EncoderVideoCodec = captureRuntime.EncoderVideoCodec,");

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
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var captureTransportProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureTransport.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureTransport = BuildCaptureTransportProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "MemoryPreference = captureTransport.MemoryPreference,");
        AssertContains(snapshotFlatteningText, "VideoNegotiatedSubtype = captureTransport.VideoNegotiatedSubtype,");
        AssertContains(snapshotFlatteningText, "FrameLedgerRecentEvents = captureTransport.FrameLedgerRecentEvents,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryPreference = captureRuntime.MemoryPreference,");
        AssertDoesNotContain(snapshotFlatteningText, "FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents,");

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
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var hdrPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var hdrPipeline = BuildHdrPipelineProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "IsHdrAvailable = hdrPipeline.IsHdrAvailable,");
        AssertContains(snapshotFlatteningText, "HdrRuntimeState = hdrPipeline.HdrRuntimeState,");
        AssertContains(snapshotFlatteningText, "HdrWarmupObservedNonP010Frames = hdrPipeline.HdrWarmupObservedNonP010Frames,");
        AssertContains(snapshotFlatteningText, "PipelineModeStatus = hdrPipeline.PipelineModeStatus,");
        AssertContains(snapshotFlatteningText, "TelemetryAlignmentReason = hdrPipeline.TelemetryAlignmentReason,");
        AssertDoesNotContain(snapshotFlatteningText, "IsHdrAvailable = viewModelSnapshot.IsHdrAvailable,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrRuntimeState = !string.IsNullOrWhiteSpace(viewModelSnapshot.HdrRuntimeState)");
        AssertDoesNotContain(snapshotFlatteningText, "HdrWarmupObservedNonP010Frames = captureRuntime.HdrWarmupObservedNonP010Frames,");
        AssertDoesNotContain(snapshotFlatteningText, "PipelineModeStatus = captureRuntime.PipelineModeStatus,");
        AssertDoesNotContain(snapshotFlatteningText, "TelemetryAlignmentReason = captureRuntime.TelemetryAlignmentReason,");

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
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var sourceTelemetryProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceTelemetry.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var sourceTelemetry = BuildSourceTelemetryProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,");
        AssertContains(snapshotFlatteningText, "SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,");
        AssertContains(snapshotFlatteningText, "SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,");
        AssertContains(snapshotFlatteningText, "SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAvailability = !string.IsNullOrWhiteSpace(viewModelSnapshot.SourceTelemetryAvailability)");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryDetails = captureRuntime.SourceTelemetryDetails,");

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
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var sourceSignalProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var sourceSignal = BuildSourceSignalProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "DetectedSourceFrameRate = sourceSignal.DetectedFrameRate,");
        AssertContains(snapshotFlatteningText, "SourceFrameRateOrigin = sourceSignal.FrameRateOrigin,");
        AssertContains(snapshotFlatteningText, "SourceRawTimingHex = sourceSignal.RawTimingHex,");
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

    private static Task AutomationDiagnosticsCaptureCadenceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var captureCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureCadence = BuildCaptureCadenceProjection(health);");
        AssertContains(snapshotFlatteningText, "ExpectedCaptureFrameRate = captureCadence.ExpectedFrameRate,");
        AssertContains(snapshotFlatteningText, "CaptureCadenceEstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames,");
        AssertContains(snapshotFlatteningText, "VisualCadenceMotionConfidence = captureCadence.VisualMotionConfidence,");
        AssertContains(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = captureCadence.VisualCenterRecentChangeIntervalsMs,");
        AssertDoesNotContain(snapshotFlatteningText, "ExpectedCaptureFrameRate = health.ExpectedFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCadenceEstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCadenceMotionConfidence = health.VisualCadenceMotionConfidence,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs,");

        AssertContains(captureCadenceProjectionText, "private static CaptureCadenceProjection BuildCaptureCadenceProjection(CaptureHealthSnapshot health)");
        AssertContains(captureCadenceProjectionText, "ExpectedFrameRate = health.ExpectedFrameRate,");
        AssertContains(captureCadenceProjectionText, "EstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,");
        AssertContains(captureCadenceProjectionText, "VisualMotionConfidence = health.VisualCadenceMotionConfidence,");
        AssertContains(captureCadenceProjectionText, "VisualCenterRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs");
        AssertContains(captureCadenceProjectionText, "private readonly record struct CaptureCadenceProjection");

        return Task.CompletedTask;
    }

}
