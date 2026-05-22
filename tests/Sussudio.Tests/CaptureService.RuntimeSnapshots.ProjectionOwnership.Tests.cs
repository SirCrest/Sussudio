using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureService_RuntimeIngestAudioProjection_LivesWithRuntimeSnapshotSampler()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs")
            .Replace("\r\n", "\n");

        AssertContains(runtimeText, "var ingestAudio = CaptureRuntimeIngestAudioSnapshotFields(");
        AssertContains(runtimeText, "IngestAudio = ingestAudio,");
        AssertContains(assemblerText, "AudioReaderActive = ingestAudio.AudioReaderActive,");
        AssertContains(assemblerText, "SourceReaderFrameChannelDepth = ingestAudio.SourceReaderFrameChannelDepth,");
        AssertContains(assemblerText, "WasapiPlaybackTargetVolumePercent = ingestAudio.WasapiPlaybackTargetVolumePercent,");

        AssertContains(runtimeText, "private RuntimeIngestAudioSnapshotFields CaptureRuntimeIngestAudioSnapshotFields(");
        AssertContains(runtimeText, "private sealed class RuntimeIngestAudioSnapshotFields");
        AssertContains(runtimeText, "VideoReaderActive = unifiedVideoCapture != null && (videoPreviewActive || recordingActive)");
        AssertContains(runtimeText, "IngestLastVideoFrameAgeMs = ComputeTickAge(unifiedVideoCapture?.LastVideoFrameArrivedTick ?? 0)");
        AssertContains(runtimeText, "SourceReaderFrameChannelDepth = sink?.VideoQueueCount ?? 0");
        AssertContains(runtimeText, "WasapiPlaybackTargetVolumePercent = (wasapiPlayback?.TargetVolume ?? 0) * 100.0");
        AssertContains(runtimeText, "WasapiPlaybackCurrentVolumePercent = (wasapiPlayback?.CurrentVolume ?? 0) * 100.0");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RuntimeSnapshotAssembler_LivesInFocusedPartial()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var hdrPipelineText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotHdrPipeline.cs")
            .Replace("\r\n", "\n");
        var sourceTelemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotSourceTelemetry.cs")
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
        AssertContains(assemblerText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertContains(assemblerText, "public RuntimeHdrWarmupSnapshotFields HdrWarmup { get; init; } = new();");
        AssertContains(assemblerText, "public ObservedFrameSnapshotFields ObservedTelemetry { get; init; }");
        AssertContains(runtimeText, "private sealed class RuntimeIngestAudioSnapshotFields");
        AssertContains(runtimeText, "private sealed class RuntimeReaderTransportSnapshotFields");
        AssertContains(hdrPipelineText, "private sealed class RuntimeHdrPipelineSnapshotFields");
        AssertContains(hdrPipelineText, "private sealed class RuntimeHdrWarmupSnapshotFields");
        AssertContains(sourceTelemetryText, "private sealed class RuntimeSourceTelemetrySnapshotFields");
        AssertContains(runtimeText, "private sealed class RuntimeRecordingIntegritySnapshotFields");
        AssertDoesNotContain(runtimeText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertDoesNotContain(hdrPipelineText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertDoesNotContain(sourceTelemetryText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
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
        AssertContains(agentMapText, "from already-sampled field groups and the private runtime snapshot assembly");
        AssertContains(agentMapText, "handoff contract consumed by that map.");
        AssertContains(agentMapText, "CaptureRuntimeSnapshot*.cs");
        AssertContains(agentMapText, "owns video ingest/source-reader/WASAPI playback");
        AssertContains(agentMapText, "and reader/transport projections, recording-integrity summary projection, and");
        AssertContains(agentMapText, "and its private HDR pipeline/warmup handoff models.");
        AssertContains(agentMapText, "and its private source-telemetry handoff model.");
        AssertContains(agentMapText, "recording-integrity summary projection, and");
        AssertContains(agentMapText, "their private handoff models, then delegates final DTO construction.");
        AssertContains(cleanupPlanText, "`Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs` now samples");
        AssertContains(cleanupPlanText, "`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs` owns final");
        AssertContains(cleanupPlanText, "The private runtime snapshot assembly handoff contract lives with the assembler");
        AssertContains(cleanupPlanText, "`CaptureRuntimeSnapshot*.cs` partial family");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RuntimeSnapshotAssemblyFields.cs")),
            "old runtime snapshot assembly-fields partial removed");
        AssertContains(cleanupPlanText, "Video ingest, source-reader health, WASAPI capture, playback output counter,");
        AssertContains(cleanupPlanText, "requested/negotiated reader transport, memory preference, frame-ledger, preview");
        AssertContains(cleanupPlanText, "`Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs`,");
        AssertContains(cleanupPlanText, "HDR pipeline parity/downgrade, warmup state/count projection, and their private handoff models now live in");
        AssertContains(cleanupPlanText, "source telemetry detail/frame-rate-origin/age/alignment projection and its private handoff model now lives in");
        AssertContains(cleanupPlanText, "recording-integrity summary projection, and their");
        AssertContains(cleanupPlanText, "private handoff models now live with the runtime snapshot sampler");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RuntimeSnapshotRecordingIntegrity.cs")),
            "old runtime recording-integrity projection partial removed");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RuntimeReaderTransportProjection_LivesWithRuntimeSnapshotSampler()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs")
            .Replace("\r\n", "\n");

        AssertContains(runtimeText, "var readerTransport = CaptureRuntimeReaderTransportSnapshotFields(");
        AssertContains(runtimeText, "ReaderTransport = readerTransport,");
        AssertContains(assemblerText, "MemoryPreference = readerTransport.MemoryPreference,");
        AssertContains(assemblerText, "VideoRequestedSubtype = readerTransport.VideoRequestedSubtype,");
        AssertContains(assemblerText, "FrameLedgerRecentEvents = readerTransport.FrameLedgerRecentEvents,");
        AssertContains(assemblerText, "MfSourceReaderNegotiatedFormat = readerTransport.MfSourceReaderNegotiatedFormat,");
        AssertContains(assemblerText, "ReaderSourceSubtype = readerTransport.ReaderSourceSubtype,");

        AssertContains(runtimeText, "private static RuntimeReaderTransportSnapshotFields CaptureRuntimeReaderTransportSnapshotFields(");
        AssertContains(runtimeText, "private sealed class RuntimeReaderTransportSnapshotFields");
        AssertContains(runtimeText, "requestedSettings!.RequestedPixelFormat");
        AssertContains(runtimeText, "mfSourceReaderNegotiatedFormat.Contains(\"P010\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(runtimeText, "unifiedVideoCapture.IsHighFrameRateMjpegMode ? \"MJPG\"");
        AssertContains(runtimeText, "readerSourceStreamType = (recordingActive || videoPreviewActive) && unifiedVideoCapture != null");
        AssertContains(runtimeText, "FrameLedgerSummary.Empty");
        AssertContains(runtimeText, "MemoryPreference = unifiedVideoCapture?.D3DManager != null ? \"Gpu\" : \"Cpu\",");
        AssertContains(runtimeText, "(previewFrameSink as D3D11PreviewRenderer)?.RendererMode ?? \"None\"");
        AssertContains(runtimeText, "ReaderSourceSubtype = actualPixelFormat");

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

    internal static Task CaptureService_RuntimeRecordingIntegrityProjection_LivesWithRuntimeSnapshotSampler()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs")
            .Replace("\r\n", "\n");

        AssertContains(runtimeText, "var recordingIntegrity = CaptureRuntimeRecordingIntegritySnapshotFields(");
        AssertContains(runtimeText, "RecordingIntegrity = recordingIntegrity,");
        AssertContains(assemblerText, "RecordingIntegrityStatus = recordingIntegrity.Status,");
        AssertContains(assemblerText, "RecordingIntegrityAudioFramesWrittenToSink = recordingIntegrity.AudioFramesWrittenToSink,");
        AssertContains(assemblerText, "RecordingIntegrityEncoderAvSyncDriftMs = recordingIntegrity.EncoderAvSyncDriftMs,");

        AssertContains(runtimeText, "private static RuntimeRecordingIntegritySnapshotFields CaptureRuntimeRecordingIntegritySnapshotFields(");
        AssertContains(runtimeText, "private sealed class RuntimeRecordingIntegritySnapshotFields");
        AssertContains(runtimeText, "Status = recordingIntegrity.Status,");
        AssertContains(runtimeText, "AudioFramesWrittenToSink = recordingIntegrity.AudioFramesWrittenToSink,");
        AssertContains(runtimeText, "EncoderAvSyncDriftMs = recordingIntegrity.EncoderAvSyncDriftMs,");
        AssertContains(runtimeText, "Reason = recordingIntegrity.Reason");

        AssertDoesNotContain(runtimeText, "var recordingIntegrity = ResolveRecordingIntegritySummary(");

        return Task.CompletedTask;
    }
}
