using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationDiagnosticsCaptureCommandProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
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
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningSettingsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.Settings.cs")
            .Replace("\r\n", "\n");
        var userSettingsProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var userSettings = BuildUserSettingsProjection(viewModelSnapshot);");
        AssertContains(snapshotProjectionText, "var recordingSettings = BuildRecordingSettingsProjection(userSettings);");
        AssertContains(snapshotFlatteningText, "var settingsFlattening = BuildSettingsFlattenedProjection(userSettings, recordingSettings);");
        AssertContains(snapshotFlatteningText, "SelectedDeviceId = settingsFlattening.SelectedDeviceId,");
        AssertContains(snapshotFlatteningText, "SelectedFriendlyFrameRate = settingsFlattening.SelectedFriendlyFrameRate,");
        AssertContains(snapshotFlatteningText, "SelectedRecordingFormat = settingsFlattening.SelectedRecordingFormat,");
        AssertContains(snapshotFlatteningText, "CustomBitrateMbps = settingsFlattening.CustomBitrateMbps,");
        AssertContains(snapshotFlatteningText, "IsStatsVisible = settingsFlattening.IsStatsVisible,");
        AssertContains(snapshotFlatteningSettingsText, "private static SettingsFlattenedProjection BuildSettingsFlattenedProjection(");
        AssertContains(snapshotFlatteningSettingsText, "SelectedDeviceId = userSettings.SelectedDeviceId,");
        AssertContains(snapshotFlatteningSettingsText, "SelectedFriendlyFrameRate = userSettings.SelectedFriendlyFrameRate,");
        AssertContains(snapshotFlatteningSettingsText, "SelectedRecordingFormat = recordingSettings.SelectedRecordingFormat,");
        AssertContains(snapshotFlatteningSettingsText, "CustomBitrateMbps = recordingSettings.CustomBitrateMbps,");
        AssertContains(snapshotFlatteningSettingsText, "IsStatsVisible = userSettings.IsStatsVisible");
        AssertContains(snapshotFlatteningSettingsText, "private readonly record struct SettingsFlattenedProjection");
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

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsCaptureFormatProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningCaptureFormatText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.cs")
            .Replace("\r\n", "\n");
        var captureFormatProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureFormat = BuildCaptureFormatProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var captureFormatFlattening = BuildCaptureFormatFlattenedProjection(captureFormat);");
        AssertContains(snapshotFlatteningText, "RequestedWidth = captureFormatFlattening.RequestedWidth,");
        AssertContains(snapshotFlatteningText, "HdrActivationReason = captureFormatFlattening.HdrActivationReason,");
        AssertContains(snapshotFlatteningText, "NegotiatedWidth = captureFormatFlattening.NegotiatedWidth,");
        AssertContains(snapshotFlatteningText, "LatestObservedFramePixelFormat = captureFormatFlattening.LatestObservedFramePixelFormat,");
        AssertContains(snapshotFlatteningText, "EncoderVideoCodec = captureFormatFlattening.EncoderVideoCodec,");
        AssertContains(snapshotFlatteningCaptureFormatText, "private static CaptureFormatFlattenedProjection BuildCaptureFormatFlattenedProjection(");
        AssertContains(snapshotFlatteningCaptureFormatText, "RequestedWidth = captureFormat.RequestedWidth,");
        AssertContains(snapshotFlatteningCaptureFormatText, "HdrActivationReason = captureFormat.HdrActivationReason,");
        AssertContains(snapshotFlatteningCaptureFormatText, "NegotiatedWidth = captureFormat.NegotiatedWidth,");
        AssertContains(snapshotFlatteningCaptureFormatText, "LatestObservedFramePixelFormat = captureFormat.LatestObservedFramePixelFormat,");
        AssertContains(snapshotFlatteningCaptureFormatText, "EncoderVideoCodec = captureFormat.EncoderVideoCodec,");
        AssertContains(snapshotFlatteningCaptureFormatText, "private readonly record struct CaptureFormatFlattenedProjection");
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
        AssertContains(captureFormatProjectionText, "RequestedWidth = captureRuntime.RequestedWidth,");
        AssertContains(captureFormatProjectionText, "HdrActivationReason = captureRuntime.HdrActivationReason,");
        AssertContains(captureFormatProjectionText, "NegotiatedWidth = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,");
        AssertContains(captureFormatProjectionText, "LatestObservedFramePixelFormat = captureRuntime.LatestObservedFramePixelFormat,");
        AssertContains(captureFormatProjectionText, "EncoderVideoCodec = captureRuntime.EncoderVideoCodec,");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsCaptureTransportProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningCaptureTransportText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureTransport.cs")
            .Replace("\r\n", "\n");
        var captureTransportProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureTransport.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureTransport = BuildCaptureTransportProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var captureTransportFlattening = BuildCaptureTransportFlattenedProjection(captureTransport);");
        AssertContains(snapshotFlatteningText, "MemoryPreference = captureTransportFlattening.MemoryPreference,");
        AssertContains(snapshotFlatteningText, "VideoNegotiatedSubtype = captureTransportFlattening.VideoNegotiatedSubtype,");
        AssertContains(snapshotFlatteningText, "FrameLedgerRecentEvents = captureTransportFlattening.FrameLedgerRecentEvents,");
        AssertContains(snapshotFlatteningCaptureTransportText, "private static CaptureTransportFlattenedProjection BuildCaptureTransportFlattenedProjection(");
        AssertContains(snapshotFlatteningCaptureTransportText, "MemoryPreference = captureTransport.MemoryPreference,");
        AssertContains(snapshotFlatteningCaptureTransportText, "VideoNegotiatedSubtype = captureTransport.VideoNegotiatedSubtype,");
        AssertContains(snapshotFlatteningCaptureTransportText, "FrameLedgerRecentEvents = captureTransport.FrameLedgerRecentEvents");
        AssertContains(snapshotFlatteningCaptureTransportText, "private readonly record struct CaptureTransportFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryPreference = captureRuntime.MemoryPreference,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryPreference = captureTransport.MemoryPreference,");
        AssertDoesNotContain(snapshotFlatteningText, "FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents,");
        AssertDoesNotContain(snapshotFlatteningText, "FrameLedgerRecentEvents = captureTransport.FrameLedgerRecentEvents,");

        AssertContains(captureTransportProjectionText, "private static CaptureTransportProjection BuildCaptureTransportProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(captureTransportProjectionText, "MemoryPreference = captureRuntime.MemoryPreference,");
        AssertContains(captureTransportProjectionText, "VideoNegotiatedSubtype = captureRuntime.VideoNegotiatedSubtype,");
        AssertContains(captureTransportProjectionText, "FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents");
        AssertContains(captureTransportProjectionText, "private readonly record struct CaptureTransportProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsHdrPipelineProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningHdrPipelineText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.HdrPipeline.cs")
            .Replace("\r\n", "\n");
        var hdrPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var hdrPipeline = BuildHdrPipelineProjection(viewModelSnapshot, captureRuntime, hdrTruthVerdict);");
        AssertContains(snapshotFlatteningText, "var hdrPipelineFlattening = BuildHdrPipelineFlattenedProjection(hdrPipeline);");
        AssertContains(snapshotFlatteningText, "IsHdrAvailable = hdrPipelineFlattening.IsHdrAvailable,");
        AssertContains(snapshotFlatteningText, "HdrRuntimeState = hdrPipelineFlattening.HdrRuntimeState,");
        AssertContains(snapshotFlatteningText, "HdrWarmupObservedNonP010Frames = hdrPipelineFlattening.HdrWarmupObservedNonP010Frames,");
        AssertContains(snapshotFlatteningText, "PipelineModeStatus = hdrPipelineFlattening.PipelineModeStatus,");
        AssertContains(snapshotFlatteningText, "TelemetryAlignmentReason = hdrPipelineFlattening.TelemetryAlignmentReason,");
        AssertContains(snapshotFlatteningText, "HdrTruthVerdict = hdrPipelineFlattening.TruthVerdict,");
        AssertContains(snapshotFlatteningHdrPipelineText, "private static HdrPipelineFlattenedProjection BuildHdrPipelineFlattenedProjection(");
        AssertContains(snapshotFlatteningHdrPipelineText, "IsHdrAvailable = hdrPipeline.IsHdrAvailable,");
        AssertContains(snapshotFlatteningHdrPipelineText, "HdrRuntimeState = hdrPipeline.HdrRuntimeState,");
        AssertContains(snapshotFlatteningHdrPipelineText, "HdrWarmupObservedNonP010Frames = hdrPipeline.HdrWarmupObservedNonP010Frames,");
        AssertContains(snapshotFlatteningHdrPipelineText, "PipelineModeStatus = hdrPipeline.PipelineModeStatus,");
        AssertContains(snapshotFlatteningHdrPipelineText, "TelemetryAlignmentReason = hdrPipeline.TelemetryAlignmentReason,");
        AssertContains(snapshotFlatteningHdrPipelineText, "TruthVerdict = hdrPipeline.TruthVerdict");
        AssertContains(snapshotFlatteningHdrPipelineText, "private readonly record struct HdrPipelineFlattenedProjection");
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

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsSourceTelemetryProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningSourceText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.Source.cs")
            .Replace("\r\n", "\n");
        var sourceTelemetryProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceTelemetry.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var sourceTelemetry = BuildSourceTelemetryProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var sourceFlattening = BuildSourceFlattenedProjection(sourceSignal, sourceTelemetry);");
        AssertContains(snapshotFlatteningText, "SourceTelemetryAvailability = sourceFlattening.SourceTelemetryAvailability,");
        AssertContains(snapshotFlatteningText, "SourceTelemetryDetails = sourceFlattening.SourceTelemetryDetails,");
        AssertContains(snapshotFlatteningText, "SourceTelemetryAgeSeconds = sourceFlattening.SourceTelemetryAgeSeconds,");
        AssertContains(snapshotFlatteningText, "SourceTargetSummaryText = sourceFlattening.SourceTargetSummaryText,");
        AssertContains(snapshotFlatteningSourceText, "private static SourceFlattenedProjection BuildSourceFlattenedProjection(");
        AssertContains(snapshotFlatteningSourceText, "SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,");
        AssertContains(snapshotFlatteningSourceText, "SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,");
        AssertContains(snapshotFlatteningSourceText, "SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,");
        AssertContains(snapshotFlatteningSourceText, "SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText");
        AssertContains(snapshotFlatteningSourceText, "private readonly record struct SourceFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText,");
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
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningSourceText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.Source.cs")
            .Replace("\r\n", "\n");
        var sourceSignalProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var sourceSignal = BuildSourceSignalProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var sourceFlattening = BuildSourceFlattenedProjection(sourceSignal, sourceTelemetry);");
        AssertContains(snapshotFlatteningText, "DetectedSourceFrameRate = sourceFlattening.DetectedSourceFrameRate,");
        AssertContains(snapshotFlatteningText, "SourceFrameRateOrigin = sourceFlattening.SourceFrameRateOrigin,");
        AssertContains(snapshotFlatteningText, "SourceRawTimingHex = sourceFlattening.SourceRawTimingHex,");
        AssertContains(snapshotFlatteningSourceText, "private static SourceFlattenedProjection BuildSourceFlattenedProjection(");
        AssertContains(snapshotFlatteningSourceText, "DetectedSourceFrameRate = sourceSignal.DetectedFrameRate,");
        AssertContains(snapshotFlatteningSourceText, "SourceFrameRateOrigin = sourceSignal.FrameRateOrigin,");
        AssertContains(snapshotFlatteningSourceText, "SourceRawTimingHex = sourceSignal.RawTimingHex,");
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

    private static Task AutomationDiagnosticsCaptureCadenceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var captureCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureCadence = BuildCaptureCadenceProjection(health);");
        AssertContains(snapshotFlatteningText, "ExpectedCaptureFrameRate = captureCadence.ExpectedFrameRate,");
        AssertContains(snapshotFlatteningText, "CaptureCadenceEstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames,");
        AssertDoesNotContain(snapshotFlatteningText, "ExpectedCaptureFrameRate = health.ExpectedFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCadenceEstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,");

        AssertContains(captureCadenceProjectionText, "private static CaptureCadenceProjection BuildCaptureCadenceProjection(CaptureHealthSnapshot health)");
        AssertContains(captureCadenceProjectionText, "ExpectedFrameRate = health.ExpectedFrameRate,");
        AssertContains(captureCadenceProjectionText, "EstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,");
        AssertContains(captureCadenceProjectionText, "private readonly record struct CaptureCadenceProjection");
        AssertDoesNotContain(captureCadenceProjectionText, "VisualMotionConfidence");
        AssertDoesNotContain(captureCadenceProjectionText, "VisualCenterRecentChangeIntervalsMs");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsVisualCadenceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var visualCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs")
            .Replace("\r\n", "\n");
        var captureCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var visualCadence = BuildVisualCadenceProjection(health);");
        AssertContains(snapshotFlatteningText, "VisualCadenceMotionConfidence = visualCadence.MotionConfidence,");
        AssertContains(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = visualCadence.CenterRecentChangeIntervalsMs,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCadenceMotionConfidence = captureCadence.VisualMotionConfidence,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = captureCadence.VisualCenterRecentChangeIntervalsMs,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCadenceMotionConfidence = health.VisualCadenceMotionConfidence,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs,");

        AssertContains(visualCadenceProjectionText, "private static VisualCadenceProjection BuildVisualCadenceProjection(CaptureHealthSnapshot health)");
        AssertContains(visualCadenceProjectionText, "MotionConfidence = health.VisualCadenceMotionConfidence,");
        AssertContains(visualCadenceProjectionText, "CenterRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs");
        AssertContains(visualCadenceProjectionText, "private readonly record struct VisualCadenceProjection");
        AssertDoesNotContain(captureCadenceProjectionText, "VisualCadenceMotionConfidence");
        AssertDoesNotContain(captureCadenceProjectionText, "VisualCenterCadenceRecentChangeIntervalsMs");

        return Task.CompletedTask;
    }

}
