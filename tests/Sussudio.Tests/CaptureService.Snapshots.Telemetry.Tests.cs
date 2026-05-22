using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    // CaptureService.Snapshots: observed and source telemetry helpers.

    internal static Task CaptureService_ObservedPixelTelemetry_LivesWithCaptureFormatTelemetry()
    {
        var telemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Telemetry.cs")
            .Replace("\r\n", "\n");
        var captureFormatTelemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.CaptureFormatTelemetry.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(telemetryText, "private void ResetObservedPixelTelemetry(");
        AssertDoesNotContain(telemetryText, "private static string? NormalizeObservedPixelFormat(");
        AssertDoesNotContain(telemetryText, "private void RecordObservedPixelFormat(");
        AssertContains(captureFormatTelemetryText, "private void ResetObservedPixelTelemetry()");
        AssertContains(captureFormatTelemetryText, "private static string? NormalizeObservedPixelFormat(string? pixelFormat)");
        AssertContains(captureFormatTelemetryText, "private void RecordObservedPixelFormat(string? pixelFormat, bool incrementAsFrame = true)");
        AssertContains(captureFormatTelemetryText, "Interlocked.Exchange(ref _observedP010FrameCount, 0);");
        AssertContains(captureFormatTelemetryText, "Interlocked.Increment(ref _observedP010FrameCount);");
        AssertContains(captureFormatTelemetryText, "Interlocked.Increment(ref _observedNv12FrameCount);");
        AssertContains(captureFormatTelemetryText, "Interlocked.Increment(ref _observedOtherFrameCount);");
        AssertContains(captureFormatTelemetryText, "private void CaptureEncoderRuntimeTelemetry(LibAvRecordingSink? sink)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.ObservedPixelTelemetry.cs")),
            "old observed pixel telemetry partial removed");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_NormalizeObservedPixelFormat_NormalizesCorrectly()
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

    internal static Task CaptureService_ResolveSourceTelemetryBackend_MapsOrigins()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveSourceTelemetryBackend",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveSourceTelemetryBackend not found.");

        var telemetryType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var originType = RequireType("Sussudio.Models.SourceTelemetryOrigin");
        var snapshotsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Snapshots.cs")
            .Replace("\r\n", "\n");
        AssertContains(snapshotsText, "private static string ResolveSourceTelemetryBackend(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.SnapshotTelemetry.cs")),
            "old source telemetry snapshot partial removed");

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

    internal static Task CaptureService_ResolveEncoderVideoProfile_MapsFormatsAndHdr()
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

    internal static Task CaptureService_ComputeTickAge_ReturnsCorrectValues()
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

    internal static Task CaptureService_ResolveTelemetryAlignment_DetectsMismatches()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveTelemetryAlignment",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveTelemetryAlignment not found.");

        var settingsType = RequireType("Sussudio.Models.CaptureSettings");
        var telemetryType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var availabilityType = RequireType("Sussudio.Models.SourceTelemetryAvailability");
        var snapshotsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Snapshots.cs")
            .Replace("\r\n", "\n");
        var runtimeSourceTelemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotSourceTelemetry.cs")
            .Replace("\r\n", "\n");
        AssertDoesNotContain(snapshotsText, "private static (string Status, string Reason) ResolveTelemetryAlignment(");
        AssertContains(runtimeSourceTelemetryText, "private static (string Status, string Reason) ResolveTelemetryAlignment(");

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

    internal static Task CaptureService_ResolveSourceTelemetryCircuitState_ReturnsCorrectState()
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
