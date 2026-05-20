using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
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

        SetPropertyOrBackingField(captureService, "_activeRecordingSettings", settings);
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
