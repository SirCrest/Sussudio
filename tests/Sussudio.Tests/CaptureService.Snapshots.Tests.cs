using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    // ── CaptureService.Snapshots: ResolveEncoderCodecName ──

    private static Task CaptureService_ResolveEncoderCodecName_MapsFormats()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveEncoderCodecName",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveEncoderCodecName not found.");

        var settingsType = RequireType("Sussudio.Models.CaptureSettings");
        var formatType = RequireType("Sussudio.Models.RecordingFormat");

        // HEVC → hevc_nvenc
        var hevcSettings = Activator.CreateInstance(settingsType)!;
        settingsType.GetProperty("Format")!.SetValue(hevcSettings, Enum.Parse(formatType, "HevcMp4"));
        var hevcResult = method.Invoke(null, new[] { hevcSettings })?.ToString();
        AssertContains(hevcResult ?? "", "hevc");

        // H264 → h264_nvenc (default Format is H264Mp4)
        var h264Settings = Activator.CreateInstance(settingsType)!;
        var h264Result = method.Invoke(null, new[] { h264Settings })?.ToString();
        AssertContains(h264Result ?? "", "264");

        // null → null
        var nullResult = method.Invoke(null, new object?[] { null });
        AssertEqual(true, nullResult == null, "null settings → null codec");

        return Task.CompletedTask;
    }

    // ── CaptureService.Snapshots: ResolveEncoderOutputPixelFormat ──

    private static Task CaptureService_ResolveEncoderOutputPixelFormat_DistinguishesHdr()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveEncoderOutputPixelFormat",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveEncoderOutputPixelFormat not found.");

        var contextType = RequireType("Sussudio.Services.Contracts.RecordingContext");
        var settingsType = RequireType("Sussudio.Models.CaptureSettings");

        // HDR active context → yuv420p10le
        var hdrContext = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(hdrContext, "HdrPipelineActive", true);
        var hdrSettings = RuntimeHelpers.GetUninitializedObject(settingsType);
        var hdrResult = method.Invoke(null, new[] { hdrContext, hdrSettings })?.ToString();
        AssertContains(hdrResult ?? "", "10");

        // SDR context → yuv420p
        var sdrContext = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(sdrContext, "HdrPipelineActive", false);
        var sdrResult = method.Invoke(null, new[] { sdrContext, hdrSettings })?.ToString();
        AssertEqual(true, sdrResult != null && !sdrResult.Contains("10"), "SDR → 8-bit pixel format");

        return Task.CompletedTask;
    }

    // ── TelemetryAgeHelper: shared compute-age logic used by capture/automation/view-model ──

    private static Task CaptureService_ResolveTelemetryAgeSeconds_ComputesCorrectly()
    {
        var helperType = RequireType("Sussudio.Services.Runtime.TelemetryAgeHelper");
        var method = helperType.GetMethod(
            "ComputeAgeSeconds",
            BindingFlags.Static | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(DateTimeOffset?), typeof(DateTimeOffset) },
            modifiers: null)
            ?? throw new InvalidOperationException("TelemetryAgeHelper.ComputeAgeSeconds(DateTimeOffset?, DateTimeOffset) not found.");

        var now = DateTimeOffset.UtcNow;

        // 10 seconds ago → 10
        var age10 = (int?)method.Invoke(null, new object?[] { (DateTimeOffset?)now.AddSeconds(-10), now });
        AssertEqual(10, age10!.Value, "10 seconds ago");

        // Future timestamp → clamped to 0
        var ageFuture = (int?)method.Invoke(null, new object?[] { (DateTimeOffset?)now.AddSeconds(5), now });
        AssertEqual(true, ageFuture!.Value <= 0, "Future timestamp clamps to 0");

        return Task.CompletedTask;
    }

    // ── CaptureService.Snapshots: ResolveHdrWarmupState ──

    private static Task CaptureService_ResolveHdrWarmupState_ReturnsCorrectStates()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveHdrWarmupState",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveHdrWarmupState not found.");

        // HDR not requested → NotRequested
        var notRequested = method.Invoke(null, new object[] { false, false, false, 0L })?.ToString();
        AssertEqual("NotRequested", notRequested, "HDR not requested");

        // HDR requested and active with P010 frames while recording → Satisfied
        var satisfied = method.Invoke(null, new object[] { true, true, true, 100L })?.ToString();
        AssertEqual("Satisfied", satisfied, "HDR active with P010 frames");

        // HDR requested but not active → Pending or Degraded
        var pending = method.Invoke(null, new object[] { true, false, false, 0L })?.ToString();
        AssertEqual(true, pending != "Satisfied" && pending != "NotRequested",
            $"HDR requested but not active → {pending}");

        return Task.CompletedTask;
    }

    // ── CaptureService: NormalizeObservedPixelFormat ──

    private static Task CaptureService_NormalizeObservedPixelFormat_NormalizesCorrectly()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("NormalizeObservedPixelFormat",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("NormalizeObservedPixelFormat not found.");

        // P010 case variants
        var p010Lower = method.Invoke(null, new object?[] { "p010" })?.ToString();
        AssertEqual("P010", p010Lower, "p010 → P010");

        var p010Mixed = method.Invoke(null, new object?[] { "P010" })?.ToString();
        AssertEqual("P010", p010Mixed, "P010 stays P010");

        // NV12 case variants
        var nv12Lower = method.Invoke(null, new object?[] { "nv12" })?.ToString();
        AssertEqual("NV12", nv12Lower, "nv12 → NV12");

        // Other formats → uppercase
        var bgra = method.Invoke(null, new object?[] { "bgra" })?.ToString();
        AssertEqual("BGRA", bgra, "bgra → BGRA");

        // Null → null
        var nullResult = method.Invoke(null, new object?[] { null });
        AssertEqual(true, nullResult == null, "null → null");

        return Task.CompletedTask;
    }

    // ── CaptureService.Snapshots: ResolveSourceTelemetryBackend ──

    private static Task CaptureService_ResolveSourceTelemetryBackend_MapsOrigins()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveSourceTelemetryBackend",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveSourceTelemetryBackend not found.");

        var telemetryType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var originType = RequireType("Sussudio.Models.SourceTelemetryOrigin");

        // NativeXu origin
        var nativeXuTelemetry = RuntimeHelpers.GetUninitializedObject(telemetryType);
        SetPropertyBackingField(nativeXuTelemetry, "Origin", Enum.Parse(originType, "NativeXu"));
        var nativeXuResult = method.Invoke(null, new[] { nativeXuTelemetry })?.ToString();
        AssertContains(nativeXuResult ?? "", "NativeXu");

        // DeviceFormatFallback origin
        var fallbackTelemetry = RuntimeHelpers.GetUninitializedObject(telemetryType);
        SetPropertyBackingField(fallbackTelemetry, "Origin", Enum.Parse(originType, "DeviceFormatFallback"));
        var fallbackResult = method.Invoke(null, new[] { fallbackTelemetry })?.ToString();
        AssertContains(fallbackResult ?? "", "DeviceFormat");

        return Task.CompletedTask;
    }

    // ── CaptureService.Snapshots: ResolveEncoderVideoProfile ──

    private static Task CaptureService_ResolveEncoderVideoProfile_MapsFormatsAndHdr()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveEncoderVideoProfile",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveEncoderVideoProfile not found.");

        var contextType = RequireType("Sussudio.Services.Contracts.RecordingContext");
        var settingsType = RequireType("Sussudio.Models.CaptureSettings");
        var formatType = RequireType("Sussudio.Models.RecordingFormat");

        // HDR → main10 regardless of format
        var hdrCtx = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(hdrCtx, "HdrPipelineActive", true);
        var settings = Activator.CreateInstance(settingsType)!;
        AssertEqual("main10", method.Invoke(null, new[] { hdrCtx, settings })?.ToString(), "HDR → main10");

        // H264 SDR → high
        var sdrCtx = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(sdrCtx, "HdrPipelineActive", false);
        var h264Settings = Activator.CreateInstance(settingsType)!;
        settingsType.GetProperty("Format")!.SetValue(h264Settings, Enum.Parse(formatType, "H264Mp4"));
        AssertEqual("high", method.Invoke(null, new[] { sdrCtx, h264Settings })?.ToString(), "H264 SDR → high");

        // HEVC SDR → main
        var hevcSettings = Activator.CreateInstance(settingsType)!;
        settingsType.GetProperty("Format")!.SetValue(hevcSettings, Enum.Parse(formatType, "HevcMp4"));
        AssertEqual("main", method.Invoke(null, new[] { sdrCtx, hevcSettings })?.ToString(), "HEVC SDR → main");

        // null settings → null
        AssertEqual(true, method.Invoke(null, new object?[] { sdrCtx, null }) == null, "null settings → null");

        return Task.CompletedTask;
    }

    // ── CaptureService.Snapshots: ComputeTickAge ──

    private static Task CaptureService_ComputeTickAge_ReturnsCorrectValues()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ComputeTickAge",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ComputeTickAge not found.");

        // tick=0 → -1 (sentinel for "no tick recorded")
        var zeroResult = (long)method.Invoke(null, new object[] { 0L })!;
        AssertEqual(-1L, zeroResult, "tick=0 → -1");

        // Recent tick → small positive age
        var recentTick = Environment.TickCount64 - 100; // 100ms ago
        var recentAge = (long)method.Invoke(null, new object[] { recentTick })!;
        AssertEqual(true, recentAge >= 0 && recentAge < 5000, $"Recent tick age should be 0-5000ms, got {recentAge}");

        return Task.CompletedTask;
    }

    // ── CaptureService.Snapshots: ResolveTelemetryAlignment ──

    private static Task CaptureService_ResolveTelemetryAlignment_DetectsMismatches()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveTelemetryAlignment",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveTelemetryAlignment not found.");

        var settingsType = RequireType("Sussudio.Models.CaptureSettings");
        var telemetryType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var availabilityType = RequireType("Sussudio.Models.SourceTelemetryAvailability");

        // Aligned case: telemetry matches settings
        var alignedTelemetry = RuntimeHelpers.GetUninitializedObject(telemetryType);
        SetPropertyBackingField(alignedTelemetry, "Availability", Enum.Parse(availabilityType, "Available"));
        SetPropertyBackingField(alignedTelemetry, "Width", (int?)1920);
        SetPropertyBackingField(alignedTelemetry, "Height", (int?)1080);
        SetPropertyBackingField(alignedTelemetry, "FrameRateExact", (double?)60.0);
        SetPropertyBackingField(alignedTelemetry, "IsHdr", (bool?)false);

        var settings = Activator.CreateInstance(settingsType)!;
        settingsType.GetProperty("Width")!.SetValue(settings, (uint)1920);
        settingsType.GetProperty("Height")!.SetValue(settings, (uint)1080);
        settingsType.GetProperty("FrameRate")!.SetValue(settings, 60.0);

        var alignedResult = method.Invoke(null, new object?[] { settings, alignedTelemetry, (uint?)1920, (uint?)1080, (double?)60.0, false });
        var status = alignedResult!.GetType().GetField("Item1")!.GetValue(alignedResult)?.ToString();
        AssertEqual("Aligned", status, "Matching telemetry → Aligned");

        var hdrSourceSdrCaptureTelemetry = RuntimeHelpers.GetUninitializedObject(telemetryType);
        SetPropertyBackingField(hdrSourceSdrCaptureTelemetry, "Availability", Enum.Parse(availabilityType, "Available"));
        SetPropertyBackingField(hdrSourceSdrCaptureTelemetry, "Width", (int?)1920);
        SetPropertyBackingField(hdrSourceSdrCaptureTelemetry, "Height", (int?)1080);
        SetPropertyBackingField(hdrSourceSdrCaptureTelemetry, "FrameRateExact", (double?)60.0);
        SetPropertyBackingField(hdrSourceSdrCaptureTelemetry, "IsHdr", (bool?)true);

        var hdrSourceSdrCaptureResult = method.Invoke(null, new object?[] { settings, hdrSourceSdrCaptureTelemetry, (uint?)1920, (uint?)1080, (double?)60.0, false });
        var hdrSourceSdrCaptureStatus = hdrSourceSdrCaptureResult!.GetType().GetField("Item1")!.GetValue(hdrSourceSdrCaptureResult)?.ToString();
        var hdrSourceSdrCaptureReason = hdrSourceSdrCaptureResult.GetType().GetField("Item2")!.GetValue(hdrSourceSdrCaptureResult)?.ToString() ?? string.Empty;
        AssertEqual("Aligned", hdrSourceSdrCaptureStatus, "HDR source with SDR capture request -> Aligned");
        AssertContains(hdrSourceSdrCaptureReason, "SDR capture was requested");

        // Unavailable telemetry
        var unavailTelemetry = RuntimeHelpers.GetUninitializedObject(telemetryType);
        SetPropertyBackingField(unavailTelemetry, "Availability", Enum.Parse(availabilityType, "Unavailable"));
        SetPropertyBackingField(unavailTelemetry, "DiagnosticSummary", "No device");

        var unavailResult = method.Invoke(null, new object?[] { settings, unavailTelemetry, (uint?)1920, (uint?)1080, (double?)60.0, false });
        var unavailStatus = unavailResult!.GetType().GetField("Item1")!.GetValue(unavailResult)?.ToString();
        AssertEqual("Unavailable", unavailStatus, "Unavailable telemetry → Unavailable");

        // Width mismatch
        var mismatchTelemetry = RuntimeHelpers.GetUninitializedObject(telemetryType);
        SetPropertyBackingField(mismatchTelemetry, "Availability", Enum.Parse(availabilityType, "Available"));
        SetPropertyBackingField(mismatchTelemetry, "Width", (int?)1280);
        SetPropertyBackingField(mismatchTelemetry, "Height", (int?)720);
        SetPropertyBackingField(mismatchTelemetry, "FrameRateExact", (double?)60.0);

        var mismatchResult = method.Invoke(null, new object?[] { settings, mismatchTelemetry, (uint?)1920, (uint?)1080, (double?)60.0, false });
        var mismatchStatus = mismatchResult!.GetType().GetField("Item1")!.GetValue(mismatchResult)?.ToString();
        AssertEqual("Mismatch", mismatchStatus, "Dimension mismatch → Mismatch");

        return Task.CompletedTask;
    }

    // ── CaptureService.Snapshots: ResolveSourceTelemetryCircuitState ──

    private static Task CaptureService_ResolveSourceTelemetryCircuitState_ReturnsCorrectState()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveSourceTelemetryCircuitState",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveSourceTelemetryCircuitState not found.");

        var availabilityType = RequireType("Sussudio.Models.SourceTelemetryAvailability");

        // Available + not suppressed → Closed
        var closed = method.Invoke(null, new object[] { Enum.Parse(availabilityType, "Available"), false })?.ToString();
        AssertEqual("Closed", closed, "Available + not suppressed → Closed");

        // Suppressed → Open
        var suppressed = method.Invoke(null, new object[] { Enum.Parse(availabilityType, "Available"), true })?.ToString();
        AssertEqual("Open", suppressed, "Suppressed → Open");

        // Unavailable → Open
        var unavailable = method.Invoke(null, new object[] { Enum.Parse(availabilityType, "Unavailable"), false })?.ToString();
        AssertEqual("Open", unavailable, "Unavailable → Open");

        return Task.CompletedTask;
    }
}
