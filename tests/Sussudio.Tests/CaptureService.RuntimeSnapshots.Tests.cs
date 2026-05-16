using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task GetRuntimeSnapshot_UsesObservedTelemetryStateInsteadOfInferredCounts()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        SetPrivateField(captureService, "_firstObservedFramePixelFormat", "NV12");
        SetPrivateField(captureService, "_latestObservedFramePixelFormat", "BGRA8");
        SetPrivateField(captureService, "_latestObservedSurfaceFormat", "BGRA8");
        SetPrivateField(captureService, "_observedP010FrameCount", 0L);
        SetPrivateField(captureService, "_observedNv12FrameCount", 2L);
        SetPrivateField(captureService, "_observedOtherFrameCount", 3L);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual(0L, GetLongProperty(snapshot, "ObservedP010FrameCount"), "ObservedP010FrameCount");
        AssertEqual(2L, GetLongProperty(snapshot, "ObservedNv12FrameCount"), "ObservedNv12FrameCount");
        AssertEqual(3L, GetLongProperty(snapshot, "ObservedOtherFrameCount"), "ObservedOtherFrameCount");
        AssertEqual("NV12", GetStringProperty(snapshot, "FirstObservedFramePixelFormat"), "FirstObservedFramePixelFormat");
        AssertEqual("BGRA8", GetStringProperty(snapshot, "LatestObservedFramePixelFormat"), "LatestObservedFramePixelFormat");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetRuntimeSnapshot_PreservesReaderSourceSubtype_WhenObservedFramesAreDecoded()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        SetPrivateField(captureService, "_actualPixelFormat", "MJPG");
        SetPrivateField(captureService, "_latestObservedFramePixelFormat", "NV12");

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("MJPG", GetStringProperty(snapshot, "ReaderSourceSubtype"), "ReaderSourceSubtype");
        AssertEqual("NV12", GetStringProperty(snapshot, "LatestObservedFramePixelFormat"), "LatestObservedFramePixelFormat");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetRuntimeSnapshot_TelemetryAlignment_Mismatch_WhenSourceModeDiffersFromRequest()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var sourceTelemetry = CreateInstance("Sussudio.Models.SourceSignalTelemetrySnapshot");
        SetPropertyOrBackingField(sourceTelemetry, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Available"));
        SetPropertyOrBackingField(sourceTelemetry, "Origin", ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"));
        SetPropertyOrBackingField(sourceTelemetry, "OriginDetail", "RegressionHarness");
        SetPropertyOrBackingField(sourceTelemetry, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(sourceTelemetry, "Width", 1280);
        SetPropertyOrBackingField(sourceTelemetry, "Height", 720);
        SetPropertyOrBackingField(sourceTelemetry, "FrameRateExact", 30d);
        SetPropertyOrBackingField(sourceTelemetry, "FrameRateArg", "30/1");
        SetPropertyOrBackingField(sourceTelemetry, "IsHdr", false);
        SetPrivateField(captureService, "_latestSourceTelemetry", sourceTelemetry);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("Mismatch", GetStringProperty(snapshot, "TelemetryAlignmentStatus"), "TelemetryAlignmentStatus");
        AssertContains(GetStringProperty(snapshot, "TelemetryAlignmentReason"), "width expected");
        AssertContains(GetStringProperty(snapshot, "TelemetryAlignmentReason"), "hdr expected");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetRuntimeSnapshot_TelemetryAlignment_Unavailable_WhenTelemetryUnavailable()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var telemetryType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var createUnavailable = telemetryType.GetMethod(
            "CreateUnavailable",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string) },
            modifiers: null);
        if (createUnavailable == null)
        {
            throw new InvalidOperationException("SourceSignalTelemetrySnapshot.CreateUnavailable not found.");
        }

        var unavailableTelemetry = createUnavailable.Invoke(null, new object?[] { "regression-harness-unavailable", null });
        SetPrivateField(captureService, "_latestSourceTelemetry", unavailableTelemetry);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("Unavailable", GetStringProperty(snapshot, "TelemetryAlignmentStatus"), "TelemetryAlignmentStatus");
        AssertContains(GetStringProperty(snapshot, "TelemetryAlignmentReason"), "unavailable");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetRuntimeSnapshot_PipelineParity_Ready_WhenHdrRequestedAndIdle()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("HDR10-PQ", GetStringProperty(snapshot, "RequestedPipelineMode"), "RequestedPipelineMode");
        AssertEqual("HDR10-PQ", GetStringProperty(snapshot, "ActivePipelineMode"), "ActivePipelineMode");
        AssertEqual(true, GetBoolProperty(snapshot, "PipelineModeMatched"), "PipelineModeMatched");
        AssertEqual("Ready", GetStringProperty(snapshot, "PipelineModeStatus"), "PipelineModeStatus");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetRuntimeSnapshot_PipelineParity_Violation_WhenHdrRequestedButIngressIsSdr()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        SetPrivateField(captureService, "_activeRecordingSettings", settings);
        SetPrivateField(captureService, "_isRecording", true);
        SetPrivateField(captureService, "_activeVideoInputPixelFormat", "nv12");

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual("HDR10-PQ", GetStringProperty(snapshot, "RequestedPipelineMode"), "RequestedPipelineMode");
        AssertEqual("SDR", GetStringProperty(snapshot, "ActivePipelineMode"), "ActivePipelineMode");
        AssertEqual(false, GetBoolProperty(snapshot, "PipelineModeMatched"), "PipelineModeMatched");
        AssertEqual("Violation", GetStringProperty(snapshot, "PipelineModeStatus"), "PipelineModeStatus");
        AssertContains(GetStringProperty(snapshot, "PipelineModeReason"), "Requested pipeline");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static async Task GetRuntimeSnapshot_ThreadHealthProbes_DefaultToZeroWhenInactive()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var snapshot = InvokeInstanceMethod(captureService, "GetRuntimeSnapshot");
        AssertEqual(false, GetBoolProperty(snapshot, "SourceReaderReadOutstanding"), "SourceReaderReadOutstanding");
        AssertEqual(0L, GetLongProperty(snapshot, "SourceReaderReadOutstandingMs"), "SourceReaderReadOutstandingMs");
        AssertEqual(0L, GetLongProperty(snapshot, "SourceReaderLastFrameTickMs"), "SourceReaderLastFrameTickMs");
        AssertEqual(0L, GetLongProperty(snapshot, "WasapiCaptureCallbackCount"), "WasapiCaptureCallbackCount");
        AssertEqual(0L, GetLongProperty(snapshot, "WasapiCaptureAudioLevelEventsFired"), "WasapiCaptureAudioLevelEventsFired");
        AssertEqual(0L, GetLongProperty(snapshot, "WasapiPlaybackRenderCallbackCount"), "WasapiPlaybackRenderCallbackCount");
        AssertEqual(0L, GetLongProperty(snapshot, "WasapiPlaybackQueueDropCount"), "WasapiPlaybackQueueDropCount");
        AssertNearlyEqual(0.0, GetDoubleProperty(snapshot, "WasapiPlaybackQueueDurationMs"), 0.0001, "WasapiPlaybackQueueDurationMs");
        AssertNearlyEqual(0.0, GetDoubleProperty(snapshot, "WasapiPlaybackActiveChunkDurationMs"), 0.0001, "WasapiPlaybackActiveChunkDurationMs");
        AssertNearlyEqual(0.0, GetDoubleProperty(snapshot, "WasapiPlaybackEndpointQueuedDurationMs"), 0.0001, "WasapiPlaybackEndpointQueuedDurationMs");
        AssertNearlyEqual(0.0, GetDoubleProperty(snapshot, "WasapiPlaybackBufferedDurationMs"), 0.0001, "WasapiPlaybackBufferedDurationMs");
        AssertNearlyEqual(0.0, GetDoubleProperty(snapshot, "WasapiPlaybackStreamLatencyMs"), 0.0001, "WasapiPlaybackStreamLatencyMs");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static Task CaptureService_RuntimeIngestAudioProjection_LivesInFocusedPartial()
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
        AssertContains(ingestAudioText, "VideoReaderActive = unifiedVideoCapture != null && (videoPreviewActive || recordingActive)");
        AssertContains(ingestAudioText, "IngestLastVideoFrameAgeMs = ComputeTickAge(unifiedVideoCapture?.LastVideoFrameArrivedTick ?? 0)");
        AssertContains(ingestAudioText, "SourceReaderFrameChannelDepth = sink?.VideoQueueCount ?? 0");
        AssertContains(ingestAudioText, "WasapiPlaybackTargetVolumePercent = (wasapiPlayback?.TargetVolume ?? 0) * 100.0");
        AssertContains(ingestAudioText, "WasapiPlaybackCurrentVolumePercent = (wasapiPlayback?.CurrentVolume ?? 0) * 100.0");

        AssertDoesNotContain(runtimeText, "SourceReaderFrameChannelDepth = sink?.VideoQueueCount ?? 0");
        AssertDoesNotContain(runtimeText, "WasapiPlaybackTargetVolumePercent = (wasapiPlayback?.TargetVolume ?? 0) * 100.0");

        return Task.CompletedTask;
    }

    private static Task CaptureService_RuntimeSnapshotAssembler_LivesInFocusedPartial()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(runtimeText, "return CaptureRuntimeSnapshotAssembler.Build(new CaptureRuntimeSnapshotAssemblyFields");
        AssertContains(runtimeText, "var requestedSettings = _activeRecordingSettings ?? _currentSettings;");
        AssertContains(runtimeText, "FlashbackExportVerificationFormat = ResolveFlashbackExportVerificationFormat(requestedSettings, unifiedVideoCapture),");
        AssertContains(runtimeText, "RuntimeAvSyncDriftMs = runtimeAvSyncDriftMs,");
        AssertDoesNotContain(runtimeText, "return new CaptureRuntimeSnapshot");

        AssertContains(assemblerText, "private static class CaptureRuntimeSnapshotAssembler");
        AssertContains(assemblerText, "public static CaptureRuntimeSnapshot Build(CaptureRuntimeSnapshotAssemblyFields fields)");
        AssertContains(assemblerText, "return new CaptureRuntimeSnapshot");
        AssertContains(assemblerText, "TimestampUtc = fields.TimestampUtc,");
        AssertContains(assemblerText, "HdrWarmupObservedP010Frames = (int)Math.Min(int.MaxValue, observedP010FrameCount),");
        AssertContains(assemblerText, "SourceTelemetryAvailability = sourceTelemetry.Availability,");
        AssertContains(assemblerText, "RecordingIntegrityStatus = recordingIntegrity.Status,");
        AssertContains(assemblerText, "FlashbackCodecDowngradeReason = fields.FlashbackCodecDowngradeReason,");
        AssertContains(assemblerText, "AvSyncCaptureDriftMs = fields.RuntimeAvSyncDriftMs,");

        AssertContains(agentMapText, "`CaptureService.RuntimeSnapshots.cs` samples runtime snapshot inputs consumed by UI,");
        AssertContains(agentMapText, "`CaptureService.RuntimeSnapshotAssembler.cs` owns final `CaptureRuntimeSnapshot` DTO construction");
        AssertContains(cleanupPlanText, "`Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs` now samples");
        AssertContains(cleanupPlanText, "`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs` owns final");

        return Task.CompletedTask;
    }

    private static Task CaptureService_RuntimeReaderTransportProjection_LivesInFocusedPartial()
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
        AssertDoesNotContain(runtimeText, "(_previewFrameSink as D3D11PreviewRenderer)?.RendererMode ?? \"None\"");
        AssertDoesNotContain(ingestAudioText, "D3DManager != null ? \"Gpu\"");

        return Task.CompletedTask;
    }

    private static Task CaptureService_RuntimeHdrPipelineProjection_LivesInFocusedPartial()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var hdrPipelineText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotHdrPipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(runtimeText, "var hdrPipeline = CaptureRuntimeHdrPipelineSnapshotFields(");
        AssertContains(runtimeText, "HdrPipeline = hdrPipeline,");
        AssertContains(assemblerText, "HdrRuntimeState = hdrPipeline.HdrRuntimeState,");
        AssertContains(assemblerText, "EncoderOutputPixelFormat = hdrPipeline.EncoderOutputPixelFormat,");
        AssertContains(assemblerText, "PipelineModeReason = hdrPipeline.PipelineModeReason,");

        AssertContains(hdrPipelineText, "private static RuntimeHdrPipelineSnapshotFields CaptureRuntimeHdrPipelineSnapshotFields(");
        AssertContains(hdrPipelineText, "ResolveEncoderOutputPixelFormat(recordingContext, requestedSettings)");
        AssertContains(hdrPipelineText, "Requested pipeline '{requestedPipelineMode}'");
        AssertContains(hdrPipelineText, "HdrDowngradeCode = hdrAutoDowngraded ? \"encoder-input-not-p010\" : string.Empty");
        AssertContains(hdrPipelineText, "HdrRequestedButSourceNot10Bit = hdrRequested && sourceTelemetry.IsHdr == false");

        AssertDoesNotContain(runtimeText, "Requested pipeline '{requestedPipelineMode}'");
        AssertDoesNotContain(runtimeText, "hdrAutoDowngraded ? \"encoder-input-not-p010\"");

        return Task.CompletedTask;
    }

    private static Task CaptureService_RuntimeSourceTelemetryProjection_LivesInFocusedPartial()
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
        AssertContains(sourceTelemetryText, "TelemetryAgeHelper.ComputeAgeSeconds(telemetryTimestampUtc, DateTimeOffset.UtcNow)");
        AssertContains(sourceTelemetryText, "ResolveTelemetryAlignment(");
        AssertContains(sourceTelemetryText, "CircuitState = ResolveSourceTelemetryCircuitState(telemetry.Availability, suppressed)");
        AssertContains(sourceTelemetryText, "SourceRawTimingHex = telemetry.RawTimingHex,");

        AssertDoesNotContain(runtimeText, "TelemetryAgeHelper.ComputeAgeSeconds(");
        AssertDoesNotContain(runtimeText, "SourceTelemetryDetails = _latestSourceTelemetry.DetailEntries,");
        AssertDoesNotContain(runtimeText, "ResolveSourceTelemetryCircuitState(_latestSourceTelemetry.Availability");

        return Task.CompletedTask;
    }

    private static Task CaptureService_RuntimeRecordingIntegrityProjection_LivesInFocusedPartial()
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
        AssertContains(recordingIntegrityText, "Status = recordingIntegrity.Status,");
        AssertContains(recordingIntegrityText, "AudioFramesWrittenToSink = recordingIntegrity.AudioFramesWrittenToSink,");
        AssertContains(recordingIntegrityText, "EncoderAvSyncDriftMs = recordingIntegrity.EncoderAvSyncDriftMs,");
        AssertContains(recordingIntegrityText, "Reason = recordingIntegrity.Reason");

        AssertDoesNotContain(runtimeText, "var recordingIntegrity = ResolveRecordingIntegritySummary(");

        return Task.CompletedTask;
    }
}
