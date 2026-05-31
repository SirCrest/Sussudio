using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureService_RuntimeIngestAudioProjection_LivesWithRuntimeSnapshotSampler()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
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
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var hdrPipelineText = runtimeText;
        var sourceTelemetryText = runtimeText;
        var captureRuntimeModelText = ReadRepoFile("Sussudio/Models/Automation/AutomationRuntimeModels.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");
        var assemblerBuildText = ExtractMemberCode(assemblerText, "Build");

        AssertContains(runtimeText, "return CaptureRuntimeSnapshotAssembler.Build(new CaptureRuntimeSnapshotAssemblyFields");
        AssertContains(runtimeText, "var requestedSettings = _recordingBackend.SettingsSnapshot ?? _currentSettings;");
        AssertContains(runtimeText, "FlashbackExportVerificationFormat = ResolveFlashbackExportVerificationFormat(requestedSettings, unifiedVideoCapture),");
        AssertContains(runtimeText, "RuntimeAvSyncDriftMs = runtimeAvSyncDriftMs,");
        AssertContains(runtimeText, "HdrWarmup = hdrWarmup,");
        AssertContains(runtimeText, "return new CaptureRuntimeSnapshot");

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
        AssertContains(runtimeText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertContains(hdrPipelineText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertContains(sourceTelemetryText, "private sealed class CaptureRuntimeSnapshotAssemblyFields");
        AssertDoesNotContain(assemblerText, "bool? ObservedP010Likely8BitUpscaled) ObservedTelemetry");
        AssertContains(assemblerText, "return new CaptureRuntimeSnapshot");
        AssertContains(assemblerText, "TimestampUtc = fields.TimestampUtc,");
        AssertContains(assemblerText, "HdrWarmupObservedP010Frames = hdrWarmup.ObservedP010Frames,");
        AssertDoesNotContain(assemblerBuildText, "ResolveHdrWarmupState(");
        AssertContains(assemblerText, "SourceTelemetryAvailability = sourceTelemetry.Availability,");
        AssertContains(assemblerText, "RecordingIntegrityStatus = recordingIntegrity.Status,");
        AssertContains(assemblerText, "FlashbackCodecDowngradeReason = fields.FlashbackCodecDowngradeReason,");
        AssertContains(assemblerText, "AvSyncCaptureDriftMs = fields.RuntimeAvSyncDriftMs,");
        AssertContains(captureRuntimeModelText, "public sealed class CaptureRuntimeSnapshot");
        AssertContains(captureRuntimeModelText, "public CaptureSessionState SessionState { get; init; } = CaptureSessionState.Uninitialized;");
        AssertContains(captureRuntimeModelText, "public bool AudioReaderActive { get; init; }");
        AssertContains(captureRuntimeModelText, "public double WasapiPlaybackOutputPeak { get; init; }");
        AssertContains(captureRuntimeModelText, "public FrameLedgerEventSnapshot[] FrameLedgerRecentEvents { get; init; } = Array.Empty<FrameLedgerEventSnapshot>();");
        AssertContains(captureRuntimeModelText, "public string PreviewColorMetadata { get; init; } = \"None\";");
        AssertContains(captureRuntimeModelText, "public uint? RequestedWidth { get; init; }");
        AssertContains(captureRuntimeModelText, "public string? EncoderVideoCodec { get; init; }");
        AssertContains(captureRuntimeModelText, "public string HdrRuntimeState { get; init; } = \"Inactive\";");
        AssertContains(captureRuntimeModelText, "public string TelemetryAlignmentStatus { get; init; } = \"Unknown\";");
        AssertContains(captureRuntimeModelText, "public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails { get; init; } = Array.Empty<SourceTelemetryDetailEntry>();");
        AssertContains(captureRuntimeModelText, "public double? AvSyncCaptureDriftMs { get; init; }");
        AssertContains(captureRuntimeModelText, "public string RecordingIntegrityStatus { get; init; } = \"NotStarted\";");
        AssertContains(captureRuntimeModelText, "public string? FlashbackCodecDowngradeReason { get; init; }");
        AssertDoesNotContain(captureRuntimeModelText, "partial class CaptureRuntimeSnapshot");

        AssertContains(agentMapText, "`CaptureService.RuntimeSnapshots.cs` samples runtime snapshot inputs consumed by UI,");
        AssertContains(agentMapText, "`CaptureService.RuntimeSnapshots.cs` also owns final `CaptureRuntimeSnapshot` DTO construction");
        AssertContains(agentMapText, "from already-sampled field groups and the private runtime snapshot assembly");
        AssertContains(agentMapText, "handoff contract consumed by that map.");
        AssertContains(agentMapText, "AutomationRuntimeModels.cs");
        AssertContains(agentMapText, "owns video ingest/source-reader/WASAPI playback");
        AssertContains(agentMapText, "and reader/transport projections, recording-integrity summary projection,");
        AssertContains(agentMapText, "HDR pipeline/warmup projection, source-telemetry detail/frame-rate-origin/age/");
        AssertContains(agentMapText, "private assembly handoff models,");
        AssertContains(agentMapText, "final DTO construction.");
        AssertContains(cleanupPlanText, "`Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs` now samples");
        AssertContains(cleanupPlanText, "final `CaptureRuntimeSnapshot` DTO construction");
        AssertContains(cleanupPlanText, "private runtime snapshot assembly handoff contract");
        AssertContains(cleanupPlanText, "snapshot sampler that consumes it.");
        AssertContains(cleanupPlanText, "`AutomationRuntimeModels.cs`");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Models", "Automation", "CaptureRuntimeSnapshot.cs")),
            "capture runtime DTO folded into AutomationRuntimeModels.cs");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RuntimeSnapshotAssemblyFields.cs")),
            "old runtime snapshot assembly-fields partial removed");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RuntimeSnapshotAssembler.cs")),
            "runtime snapshot assembler folded into RuntimeSnapshots.cs");
        AssertContains(cleanupPlanText, "Video ingest, source-reader health, WASAPI capture, playback output counter,");
        AssertContains(cleanupPlanText, "requested/negotiated reader transport, memory preference, frame-ledger, preview");
        AssertContains(cleanupPlanText, "HDR pipeline");
        AssertContains(cleanupPlanText, "source telemetry");
        AssertContains(cleanupPlanText, "detail/frame-rate-origin/age/alignment projection");
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
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
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

    internal static Task CaptureService_RuntimeHdrPipelineProjection_LivesWithRuntimeSnapshotSampler()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var hdrPipelineText = runtimeText;

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

        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RuntimeSnapshotHdrPipeline.cs")),
            "HDR runtime snapshot projection folded into runtime snapshot sampler");
        AssertDoesNotContain(assemblerText, "HdrWarmupObservedP010Frames = (int)Math.Min(int.MaxValue, observedP010FrameCount),");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RuntimeSourceTelemetryProjection_LivesWithRuntimeSnapshotSampler()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var sourceTelemetryText = runtimeText;

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

        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RuntimeSnapshotSourceTelemetry.cs")),
            "source telemetry runtime snapshot projection folded into runtime snapshot sampler");
        AssertDoesNotContain(runtimeText, "SourceTelemetryDetails = _latestSourceTelemetry.DetailEntries,");
        AssertDoesNotContain(runtimeText, "ResolveSourceTelemetryCircuitState(_latestSourceTelemetry.Availability");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RuntimeRecordingIntegrityProjection_LivesWithRuntimeSnapshotSampler()
    {
        var runtimeText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        var assemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
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

    internal static Task FrameLedger_RetainsBoundedRecentEvents()
    {
        var unifiedVideoCaptureText = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs")
            .Replace("\r\n", "\n");
        AssertContains(unifiedVideoCaptureText, "internal sealed class FrameLedger");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "FrameLedger.cs")),
            "frame ledger helper folded into UnifiedVideoCapture.cs");

        var ledgerType = RequireType("Sussudio.Services.Capture.FrameLedger");
        var identityType = RequireType("Sussudio.Models.FrameIdentity");
        var stageType = RequireType("Sussudio.Models.FrameLedgerStage");
        var ledger = Activator.CreateInstance(
                ledgerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { 3 },
                culture: null)
            ?? throw new InvalidOperationException("Failed to create FrameLedger.");

        var recordCapture = ledgerType.GetMethod(
                "RecordCaptureArrived",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameLedger.RecordCaptureArrived missing.");
        var recordEvent = ledgerType.GetMethod(
                "RecordEvent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameLedger.RecordEvent missing.");
        var getSummary = ledgerType.GetMethod(
                "GetSummary",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameLedger.GetSummary missing.");

        for (var i = 0; i < 4; i++)
        {
            var identity = Activator.CreateInstance(
                    identityType,
                    (long)i,
                    1000L + i,
                    null,
                    "MJPG",
                    3840,
                    2160,
                    120.0,
                    1024 + i)
                ?? throw new InvalidOperationException("Failed to create FrameIdentity.");
            recordCapture.Invoke(ledger, new object?[] { identity, "capture" });
        }

        var recordingStage = Enum.Parse(stageType, "RecordingEnqueued");
        recordEvent.Invoke(ledger, new object?[]
        {
            4L,
            recordingStage,
            2000L,
            "recording",
            null,
            null,
            true,
            null
        });

        var summary = getSummary.Invoke(ledger, new object[] { 3 })
                      ?? throw new InvalidOperationException("FrameLedger.GetSummary returned null.");
        AssertEqual(3, GetIntProperty(summary, "Capacity"), "FrameLedger capacity");
        AssertEqual(5L, GetLongProperty(summary, "TotalEventsRecorded"), "FrameLedger total events");
        AssertEqual(2L, GetLongProperty(summary, "EventsDroppedByRetention"), "FrameLedger retained drop count");
        AssertEqual(3, GetIntProperty(summary, "RecentEventCount"), "FrameLedger recent count");
        AssertEqual(2L, Convert.ToInt64(GetPropertyValue(summary, "OldestSourceSequence")), "FrameLedger oldest sequence");
        AssertEqual(4L, Convert.ToInt64(GetPropertyValue(summary, "NewestSourceSequence")), "FrameLedger newest sequence");

        var events = (Array)(GetPropertyValue(summary, "RecentEvents")
                             ?? throw new InvalidOperationException("FrameLedger recent events missing."));
        AssertEqual(3, events.Length, "FrameLedger recent event array length");
        AssertEqual(2L, GetLongProperty(events.GetValue(0)!, "SourceSequence"), "FrameLedger first retained sequence");
        AssertEqual(4L, GetLongProperty(events.GetValue(2)!, "SourceSequence"), "FrameLedger last retained sequence");
        AssertEqual("RecordingEnqueued", GetPropertyValue(events.GetValue(2)!, "Stage")!.ToString(), "FrameLedger last retained stage");
        AssertEqual(true, GetBoolProperty(events.GetValue(2)!, "Accepted"), "FrameLedger accepted state");

        return Task.CompletedTask;
    }

    internal static Task FrameLedger_SnapshotContractExposesRecentEvents()
    {
        var captureSnapshotType = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var automationSnapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var eventSnapshotType = RequireType("Sussudio.Models.FrameLedgerEventSnapshot");
        var identityType = RequireType("Sussudio.Models.FrameIdentity");

        foreach (var snapshotType in new[] { captureSnapshotType, automationSnapshotType })
        {
            AssertNotNull(snapshotType.GetProperty("FrameLedgerCapacity"), $"{snapshotType.Name}.FrameLedgerCapacity");
            AssertNotNull(snapshotType.GetProperty("FrameLedgerEventCount"), $"{snapshotType.Name}.FrameLedgerEventCount");
            AssertNotNull(snapshotType.GetProperty("FrameLedgerDroppedEventCount"), $"{snapshotType.Name}.FrameLedgerDroppedEventCount");

            var recentEvents = snapshotType.GetProperty("FrameLedgerRecentEvents")
                ?? throw new InvalidOperationException($"{snapshotType.Name}.FrameLedgerRecentEvents missing.");
            AssertEqual(eventSnapshotType.MakeArrayType(), recentEvents.PropertyType, $"{snapshotType.Name}.FrameLedgerRecentEvents type");
        }

        foreach (var prop in new[]
                 {
                     "SourceSequence",
                     "Stage",
                     "QpcTimestamp",
                     "Subsystem",
                     "QueueDepth",
                     "ByteDepth",
                     "Accepted",
                     "Reason",
                     "Identity"
                 })
        {
            AssertNotNull(eventSnapshotType.GetProperty(prop), $"FrameLedgerEventSnapshot.{prop}");
        }

        foreach (var prop in new[]
                 {
                     "SourceSequence",
                     "CaptureArrivalQpc",
                     "DeviceTimestamp100ns",
                     "InputFormat",
                     "Width",
                     "Height",
                     "FrameRateNominal",
                     "CompressedByteLength"
                 })
        {
            AssertNotNull(identityType.GetProperty(prop), $"FrameIdentity.{prop}");
        }

        return Task.CompletedTask;
    }

    internal static async Task GetRuntimeSnapshot_UsesObservedTelemetryStateInsteadOfInferredCounts()
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

    internal static async Task GetRuntimeSnapshot_PreservesReaderSourceSubtype_WhenObservedFramesAreDecoded()
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

    internal static async Task GetRuntimeSnapshot_TelemetryAlignment_Mismatch_WhenSourceModeDiffersFromRequest()
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

    internal static async Task GetRuntimeSnapshot_TelemetryAlignment_Unavailable_WhenTelemetryUnavailable()
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

    internal static async Task GetRuntimeSnapshot_PipelineParity_Ready_WhenHdrRequestedAndIdle()
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

    internal static async Task GetRuntimeSnapshot_PipelineParity_Violation_WhenHdrRequestedButIngressIsSdr()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: true);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var recordingBackend = GetPrivateField(captureService, "_recordingBackend")
            ?? throw new InvalidOperationException("CaptureService recording backend resources were missing.");
        SetPropertyOrBackingField(recordingBackend, "SettingsSnapshot", settings);
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

    internal static async Task GetRuntimeSnapshot_ThreadHealthProbes_DefaultToZeroWhenInactive()
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
}
