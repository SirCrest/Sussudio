using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureService_RuntimeIngestAudioProjection_LivesInFocusedPartial()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var ingestAudioText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotIngestAudio.cs")
            .Replace("\r\n", "\n");

        AssertContains(runtimeText, "var ingestAudio = CaptureRuntimeIngestAudioSnapshotFields(");
        AssertContains(runtimeText, "IngestAudio = ingestAudio,");
        AssertContains(assemblerText, "AudioReaderActive = ingestAudio.AudioReaderActive,");
        AssertContains(assemblerText, "SourceReaderFrameChannelDepth = ingestAudio.SourceReaderFrameChannelDepth,");
        AssertContains(assemblerText, "WasapiPlaybackTargetVolumePercent = ingestAudio.WasapiPlaybackTargetVolumePercent,");

        AssertContains(ingestAudioText, "private RuntimeIngestAudioSnapshotFields CaptureRuntimeIngestAudioSnapshotFields(");
        AssertContains(ingestAudioText, "private sealed class RuntimeIngestAudioSnapshotFields");
        AssertContains(ingestAudioText, "VideoReaderActive = unifiedVideoCapture != null && (videoPreviewActive || recordingActive)");
        AssertContains(ingestAudioText, "IngestLastVideoFrameAgeMs = ComputeTickAge(unifiedVideoCapture?.LastVideoFrameArrivedTick ?? 0)");
        AssertContains(ingestAudioText, "SourceReaderFrameChannelDepth = sink?.VideoQueueCount ?? 0");
        AssertContains(ingestAudioText, "WasapiPlaybackTargetVolumePercent = (wasapiPlayback?.TargetVolume ?? 0) * 100.0");
        AssertContains(ingestAudioText, "WasapiPlaybackCurrentVolumePercent = (wasapiPlayback?.CurrentVolume ?? 0) * 100.0");

        AssertDoesNotContain(runtimeText, "SourceReaderFrameChannelDepth = sink?.VideoQueueCount ?? 0");
        AssertDoesNotContain(runtimeText, "WasapiPlaybackTargetVolumePercent = (wasapiPlayback?.TargetVolume ?? 0) * 100.0");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RuntimeSnapshotAssembler_LivesInFocusedPartial()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var assemblyFieldsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssemblyFields.cs")
            .Replace("\r\n", "\n");
        var ingestAudioText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotIngestAudio.cs")
            .Replace("\r\n", "\n");
        var readerTransportText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotReaderTransport.cs")
            .Replace("\r\n", "\n");
        var hdrPipelineText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotHdrPipeline.cs")
            .Replace("\r\n", "\n");
        var sourceTelemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotSourceTelemetry.cs")
            .Replace("\r\n", "\n");
        var recordingIntegrityText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotRecordingIntegrity.cs")
            .Replace("\r\n", "\n");
        var captureRuntimeModelText = ReadRepoFile("Sussudio/Models/Automation/CaptureRuntimeSnapshot.cs")
            .Replace("\r\n", "\n");
        var captureRuntimeIngestAudioModelText = ReadRepoFile("Sussudio/Models/Automation/CaptureRuntimeSnapshot.IngestAudio.cs")
            .Replace("\r\n", "\n");
        var captureRuntimeTransportModelText = ReadRepoFile("Sussudio/Models/Automation/CaptureRuntimeSnapshot.Transport.cs")
            .Replace("\r\n", "\n");
        var captureRuntimeCaptureFormatModelText = ReadRepoFile("Sussudio/Models/Automation/CaptureRuntimeSnapshot.CaptureFormat.cs")
            .Replace("\r\n", "\n");
        var captureRuntimeHdrModelText = ReadRepoFile("Sussudio/Models/Automation/CaptureRuntimeSnapshot.Hdr.cs")
            .Replace("\r\n", "\n");
        var captureRuntimeSourceTelemetryModelText = ReadRepoFile("Sussudio/Models/Automation/CaptureRuntimeSnapshot.SourceTelemetry.cs")
            .Replace("\r\n", "\n");
        var captureRuntimeAvSyncModelText = ReadRepoFile("Sussudio/Models/Automation/CaptureRuntimeSnapshot.AvSync.cs")
            .Replace("\r\n", "\n");
        var captureRuntimeRecordingModelText = ReadRepoFile("Sussudio/Models/Automation/CaptureRuntimeSnapshot.Recording.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(runtimeText, "return CaptureRuntimeSnapshotAssembler.Build(new CaptureRuntimeSnapshotAssemblyFields");
        AssertContains(runtimeText, "var requestedSettings = _recordingBackend.SettingsSnapshot ?? _currentSettings;");
        AssertContains(runtimeText, "FlashbackExportVerificationFormat = ResolveFlashbackExportVerificationFormat(requestedSettings, unifiedVideoCapture),");
        AssertContains(runtimeText, "RuntimeAvSyncDriftMs = runtimeAvSyncDriftMs,");
        AssertContains(runtimeText, "HdrWarmup = hdrWarmup,");
        AssertDoesNotContain(runtimeText, "return new CaptureRuntimeSnapshot");

        AssertContains(assemblerText, "private static class CaptureRuntimeSnapshotAssembler");
        AssertContains(assemblerText, "public static CaptureRuntimeSnapshot Build(CaptureRuntimeSnapshotAssemblyFields fields)");
        AssertContains(assemblyFieldsText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertContains(assemblyFieldsText, "public RuntimeHdrWarmupSnapshotFields HdrWarmup { get; init; } = new();");
        AssertContains(assemblyFieldsText, "public ObservedFrameSnapshotFields ObservedTelemetry { get; init; }");
        AssertContains(ingestAudioText, "private sealed class RuntimeIngestAudioSnapshotFields");
        AssertContains(readerTransportText, "private sealed class RuntimeReaderTransportSnapshotFields");
        AssertContains(hdrPipelineText, "private sealed class RuntimeHdrPipelineSnapshotFields");
        AssertContains(hdrPipelineText, "private sealed class RuntimeHdrWarmupSnapshotFields");
        AssertContains(sourceTelemetryText, "private sealed class RuntimeSourceTelemetrySnapshotFields");
        AssertContains(recordingIntegrityText, "private sealed class RuntimeRecordingIntegritySnapshotFields");
        AssertDoesNotContain(assemblerText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertDoesNotContain(ingestAudioText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertDoesNotContain(readerTransportText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertDoesNotContain(hdrPipelineText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertDoesNotContain(sourceTelemetryText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertDoesNotContain(recordingIntegrityText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertDoesNotContain(assemblerText, "bool? ObservedP010Likely8BitUpscaled) ObservedTelemetry");
        AssertContains(assemblerText, "return new CaptureRuntimeSnapshot");
        AssertContains(assemblerText, "TimestampUtc = fields.TimestampUtc,");
        AssertContains(assemblerText, "HdrWarmupObservedP010Frames = hdrWarmup.ObservedP010Frames,");
        AssertDoesNotContain(assemblerText, "ResolveHdrWarmupState(");
        AssertContains(assemblerText, "SourceTelemetryAvailability = sourceTelemetry.Availability,");
        AssertContains(assemblerText, "RecordingIntegrityStatus = recordingIntegrity.Status,");
        AssertContains(assemblerText, "FlashbackCodecDowngradeReason = fields.FlashbackCodecDowngradeReason,");
        AssertContains(assemblerText, "AvSyncCaptureDriftMs = fields.RuntimeAvSyncDriftMs,");
        AssertContains(captureRuntimeModelText, "public sealed partial class CaptureRuntimeSnapshot");
        AssertContains(captureRuntimeModelText, "public CaptureSessionState SessionState { get; init; } = CaptureSessionState.Uninitialized;");
        AssertContains(captureRuntimeIngestAudioModelText, "public bool AudioReaderActive { get; init; }");
        AssertContains(captureRuntimeIngestAudioModelText, "public double WasapiPlaybackOutputPeak { get; init; }");
        AssertContains(captureRuntimeTransportModelText, "public FrameLedgerEventSnapshot[] FrameLedgerRecentEvents { get; init; } = Array.Empty<FrameLedgerEventSnapshot>();");
        AssertContains(captureRuntimeTransportModelText, "public string PreviewColorMetadata { get; init; } = \"None\";");
        AssertContains(captureRuntimeCaptureFormatModelText, "public uint? RequestedWidth { get; init; }");
        AssertContains(captureRuntimeCaptureFormatModelText, "public string? EncoderVideoCodec { get; init; }");
        AssertContains(captureRuntimeHdrModelText, "public string HdrRuntimeState { get; init; } = \"Inactive\";");
        AssertContains(captureRuntimeHdrModelText, "public string TelemetryAlignmentStatus { get; init; } = \"Unknown\";");
        AssertContains(captureRuntimeSourceTelemetryModelText, "public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails { get; init; } = Array.Empty<SourceTelemetryDetailEntry>();");
        AssertContains(captureRuntimeAvSyncModelText, "public double? AvSyncCaptureDriftMs { get; init; }");
        AssertContains(captureRuntimeRecordingModelText, "public string RecordingIntegrityStatus { get; init; } = \"NotStarted\";");
        AssertContains(captureRuntimeRecordingModelText, "public string? FlashbackCodecDowngradeReason { get; init; }");
        AssertDoesNotContain(captureRuntimeModelText, "AudioReaderActive");
        AssertDoesNotContain(captureRuntimeModelText, "RequestedWidth");
        AssertDoesNotContain(captureRuntimeModelText, "HdrRuntimeState");
        AssertDoesNotContain(captureRuntimeModelText, "SourceTelemetryDetails");
        AssertDoesNotContain(captureRuntimeModelText, "RecordingIntegrityStatus");

        AssertContains(agentMapText, "`CaptureService.RuntimeSnapshots.cs` samples runtime snapshot inputs consumed by UI,");
        AssertContains(agentMapText, "`CaptureService.RuntimeSnapshotAssembler.cs` owns final `CaptureRuntimeSnapshot` DTO construction");
        AssertContains(agentMapText, "`CaptureService.RuntimeSnapshotAssemblyFields.cs` owns the private runtime snapshot assembly handoff contract");
        AssertContains(agentMapText, "CaptureRuntimeSnapshot*.cs");
        AssertContains(agentMapText, "and its private ingest/audio handoff model.");
        AssertContains(agentMapText, "and its private reader/transport handoff model.");
        AssertContains(agentMapText, "and its private HDR pipeline/warmup handoff models.");
        AssertContains(agentMapText, "and its private source-telemetry handoff model.");
        AssertContains(agentMapText, "and its private recording-integrity handoff model.");
        AssertContains(cleanupPlanText, "`Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs` now samples");
        AssertContains(cleanupPlanText, "`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs` owns final");
        AssertContains(cleanupPlanText, "`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssemblyFields.cs` owns the");
        AssertContains(cleanupPlanText, "`CaptureRuntimeSnapshot*.cs` partial family");
        AssertContains(cleanupPlanText, "projection and its private ingest/audio handoff model lives in");
        AssertContains(cleanupPlanText, "preview renderer-mode projection and its private reader/transport handoff model now lives in");
        AssertContains(cleanupPlanText, "HDR pipeline parity/downgrade, warmup state/count projection, and their private handoff models now live in");
        AssertContains(cleanupPlanText, "source telemetry detail/frame-rate-origin/age/alignment projection and its private handoff model now lives in");
        AssertContains(cleanupPlanText, "and recording-integrity summary projection and its private handoff model now lives in");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RuntimeReaderTransportProjection_LivesInFocusedPartial()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var ingestAudioText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotIngestAudio.cs")
            .Replace("\r\n", "\n");
        var readerTransportText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotReaderTransport.cs")
            .Replace("\r\n", "\n");

        AssertContains(runtimeText, "var readerTransport = CaptureRuntimeReaderTransportSnapshotFields(");
        AssertContains(runtimeText, "ReaderTransport = readerTransport,");
        AssertContains(assemblerText, "MemoryPreference = readerTransport.MemoryPreference,");
        AssertContains(assemblerText, "VideoRequestedSubtype = readerTransport.VideoRequestedSubtype,");
        AssertContains(assemblerText, "FrameLedgerRecentEvents = readerTransport.FrameLedgerRecentEvents,");
        AssertContains(assemblerText, "MfSourceReaderNegotiatedFormat = readerTransport.MfSourceReaderNegotiatedFormat,");
        AssertContains(assemblerText, "ReaderSourceSubtype = readerTransport.ReaderSourceSubtype,");

        AssertContains(readerTransportText, "private static RuntimeReaderTransportSnapshotFields CaptureRuntimeReaderTransportSnapshotFields(");
        AssertContains(readerTransportText, "private sealed class RuntimeReaderTransportSnapshotFields");
        AssertContains(readerTransportText, "requestedSettings!.RequestedPixelFormat");
        AssertContains(readerTransportText, "mfSourceReaderNegotiatedFormat.Contains(\"P010\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(readerTransportText, "unifiedVideoCapture.IsHighFrameRateMjpegMode ? \"MJPG\"");
        AssertContains(readerTransportText, "readerSourceStreamType = (recordingActive || videoPreviewActive) && unifiedVideoCapture != null");
        AssertContains(readerTransportText, "FrameLedgerSummary.Empty");
        AssertContains(readerTransportText, "MemoryPreference = unifiedVideoCapture?.D3DManager != null ? \"Gpu\" : \"Cpu\",");
        AssertContains(readerTransportText, "(previewFrameSink as D3D11PreviewRenderer)?.RendererMode ?? \"None\"");
        AssertContains(readerTransportText, "ReaderSourceSubtype = actualPixelFormat");

        AssertDoesNotContain(runtimeText, "var negotiatedSubtypeFromSourceReader");
        AssertDoesNotContain(runtimeText, "unifiedVideoCapture.IsHighFrameRateMjpegMode ? \"MJPG\"");
        AssertDoesNotContain(runtimeText, "GetFrameLedgerSummary() ?? FrameLedgerSummary.Empty");
        AssertDoesNotContain(runtimeText, "(_videoPipeline.PreviewFrameSink as D3D11PreviewRenderer)?.RendererMode ?? \"None\"");
        AssertDoesNotContain(ingestAudioText, "D3DManager != null ? \"Gpu\"");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RuntimeHdrPipelineProjection_LivesInFocusedPartial()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var hdrPipelineText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotHdrPipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(runtimeText, "var hdrPipeline = CaptureRuntimeHdrPipelineSnapshotFields(");
        AssertContains(runtimeText, "var hdrWarmup = CaptureRuntimeHdrWarmupSnapshotFields(");
        AssertContains(runtimeText, "HdrPipeline = hdrPipeline,");
        AssertContains(runtimeText, "HdrWarmup = hdrWarmup,");
        AssertContains(assemblerText, "HdrRuntimeState = hdrPipeline.HdrRuntimeState,");
        AssertContains(assemblerText, "HdrWarmupState = hdrWarmup.State,");
        AssertContains(assemblerText, "EncoderOutputPixelFormat = hdrPipeline.EncoderOutputPixelFormat,");
        AssertContains(assemblerText, "PipelineModeReason = hdrPipeline.PipelineModeReason,");

        AssertContains(hdrPipelineText, "private static RuntimeHdrPipelineSnapshotFields CaptureRuntimeHdrPipelineSnapshotFields(");
        AssertContains(hdrPipelineText, "private static RuntimeHdrWarmupSnapshotFields CaptureRuntimeHdrWarmupSnapshotFields(");
        AssertContains(hdrPipelineText, "private sealed class RuntimeHdrPipelineSnapshotFields");
        AssertContains(hdrPipelineText, "private sealed class RuntimeHdrWarmupSnapshotFields");
        AssertContains(hdrPipelineText, "ResolveEncoderOutputPixelFormat(recordingContext, requestedSettings)");
        AssertContains(hdrPipelineText, "Requested pipeline '{requestedPipelineMode}'");
        AssertContains(hdrPipelineText, "HdrDowngradeCode = hdrAutoDowngraded ? \"encoder-input-not-p010\" : string.Empty");
        AssertContains(hdrPipelineText, "HdrRequestedButSourceNot10Bit = hdrRequested && sourceTelemetry.IsHdr == false");
        AssertContains(hdrPipelineText, "ResolveHdrWarmupState(");
        AssertContains(hdrPipelineText, "ObservedNonP010Frames = (int)Math.Min(int.MaxValue, Math.Max(0L, observedNonP010FrameCount))");

        AssertDoesNotContain(runtimeText, "Requested pipeline '{requestedPipelineMode}'");
        AssertDoesNotContain(runtimeText, "hdrAutoDowngraded ? \"encoder-input-not-p010\"");
        AssertDoesNotContain(assemblerText, "HdrWarmupObservedP010Frames = (int)Math.Min(int.MaxValue, observedP010FrameCount),");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RuntimeSourceTelemetryProjection_LivesInFocusedPartial()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var sourceTelemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotSourceTelemetry.cs")
            .Replace("\r\n", "\n");

        AssertContains(runtimeText, "var sourceTelemetry = CaptureRuntimeSourceTelemetrySnapshotFields(");
        AssertContains(runtimeText, "SourceTelemetry = sourceTelemetry,");
        AssertContains(assemblerText, "DetectedSourceFrameRate = sourceTelemetry.DetectedSourceFrameRate,");
        AssertContains(assemblerText, "SourceTelemetryAgeSeconds = sourceTelemetry.AgeSeconds,");
        AssertContains(assemblerText, "TelemetryAlignmentStatus = sourceTelemetry.AlignmentStatus,");

        AssertContains(sourceTelemetryText, "private static RuntimeSourceTelemetrySnapshotFields CaptureRuntimeSourceTelemetrySnapshotFields(");
        AssertContains(sourceTelemetryText, "private sealed class RuntimeSourceTelemetrySnapshotFields");
        AssertContains(sourceTelemetryText, "TelemetryAgeHelper.ComputeAgeSeconds(telemetryTimestampUtc, DateTimeOffset.UtcNow)");
        AssertContains(sourceTelemetryText, "ResolveTelemetryAlignment(");
        AssertContains(sourceTelemetryText, "CircuitState = ResolveSourceTelemetryCircuitState(telemetry.Availability, suppressed)");
        AssertContains(sourceTelemetryText, "SourceRawTimingHex = telemetry.RawTimingHex,");

        AssertDoesNotContain(runtimeText, "TelemetryAgeHelper.ComputeAgeSeconds(");
        AssertDoesNotContain(runtimeText, "SourceTelemetryDetails = _latestSourceTelemetry.DetailEntries,");
        AssertDoesNotContain(runtimeText, "ResolveSourceTelemetryCircuitState(_latestSourceTelemetry.Availability");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RuntimeRecordingIntegrityProjection_LivesInFocusedPartial()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var recordingIntegrityText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotRecordingIntegrity.cs")
            .Replace("\r\n", "\n");

        AssertContains(runtimeText, "var recordingIntegrity = CaptureRuntimeRecordingIntegritySnapshotFields(");
        AssertContains(runtimeText, "RecordingIntegrity = recordingIntegrity,");
        AssertContains(assemblerText, "RecordingIntegrityStatus = recordingIntegrity.Status,");
        AssertContains(assemblerText, "RecordingIntegrityAudioFramesWrittenToSink = recordingIntegrity.AudioFramesWrittenToSink,");
        AssertContains(assemblerText, "RecordingIntegrityEncoderAvSyncDriftMs = recordingIntegrity.EncoderAvSyncDriftMs,");

        AssertContains(recordingIntegrityText, "private static RuntimeRecordingIntegritySnapshotFields CaptureRuntimeRecordingIntegritySnapshotFields(");
        AssertContains(recordingIntegrityText, "private sealed class RuntimeRecordingIntegritySnapshotFields");
        AssertContains(recordingIntegrityText, "Status = recordingIntegrity.Status,");
        AssertContains(recordingIntegrityText, "AudioFramesWrittenToSink = recordingIntegrity.AudioFramesWrittenToSink,");
        AssertContains(recordingIntegrityText, "EncoderAvSyncDriftMs = recordingIntegrity.EncoderAvSyncDriftMs,");
        AssertContains(recordingIntegrityText, "Reason = recordingIntegrity.Reason");

        AssertDoesNotContain(runtimeText, "var recordingIntegrity = ResolveRecordingIntegritySummary(");

        return Task.CompletedTask;
    }
}
