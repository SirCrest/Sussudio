using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureService_SnapshotHelperPolicy_LivesInFocusedPartials()
    {
        var snapshotsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Snapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var flashbackExportText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotsText, "public CaptureDiagnosticsSnapshot GetDiagnosticsSnapshot()");
        AssertContains(snapshotsText, "return GetHealthSnapshot();");
        AssertContains(snapshotsText, "private static long ComputeTickAge(long tick)");
        AssertContains(snapshotsText, "public RecordingStats GetRecordingStats()");
        AssertContains(snapshotsText, "return new RecordingStats(_recordingBackend.LibAvSink.OutputBytes, 0);");
        AssertContains(
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs"),
            "private readonly CaptureRecordingBackendResources _recordingBackend = new();");
        AssertContains(snapshotsText, "IsFlashbackRecordingBackendActive()");
        AssertContains(snapshotsText, "bufferManager.TotalBytesWritten - _flashbackRecordingStartBytes");
        AssertContains(snapshotsText, "isFlashbackEstimate: true");
        AssertContains(snapshotsText, "new FileInfo(path).Length");
        AssertContains(snapshotsText, "catch (FileNotFoundException)");
        AssertContains(snapshotsText, "isFailure: true");

        AssertContains(snapshotsText, "private static string? ResolveEncoderCodecName(");
        AssertContains(snapshotsText, "MediaFormat.MapNvencCodecName(settings.Format)");
        AssertContains(snapshotsText, "private static string? ResolveEncoderOutputPixelFormat(");
        AssertContains(snapshotsText, "return \"yuv420p10le\";");
        AssertContains(snapshotsText, "private static string? ResolveEncoderVideoProfile(");
        AssertContains(snapshotsText, "RecordingFormat.H264Mp4 => \"high\"");
        AssertContains(snapshotsText, "private static string? ResolveRequestedFrameRateArg(");
        AssertContains(snapshotsText, "RequestedFrameRateNumerator is uint numerator");
        AssertContains(snapshotsText, "RequestedFrameRateDenominator is uint denominator");

        AssertContains(snapshotsText, "private ObservedFrameSnapshotFields ResolveObservedFrameTelemetry()");
        AssertContains(snapshotsText, "private readonly record struct ObservedFrameSnapshotFields(");
        AssertContains(snapshotsText, "return new ObservedFrameSnapshotFields(");
        AssertContains(snapshotsText, "Math.Max(0, Interlocked.Read(ref _observedP010FrameCount))");
        AssertContains(snapshotsText, "Math.Max(0, Interlocked.Read(ref _observedNv12FrameCount))");
        AssertContains(snapshotsText, "Math.Max(0, Interlocked.Read(ref _observedOtherFrameCount))");
        AssertContains(healthSnapshotText, "private static string ResolveFlashbackBackendSettingsStaleReason(");
        AssertContains(flashbackExportText, "private static long ComputeFlashbackExportElapsedMs(");
        AssertContains(flashbackExportText, "private static long ComputeFlashbackExportLastProgressAgeMs(");
        AssertContains(flashbackExportText, "private static long GetFileLengthOrZero(string? path)");

        AssertDoesNotContain(snapshotsText, "private static string ResolveFlashbackBackendSettingsStaleReason(");
        AssertDoesNotContain(snapshotsText, "private static long ComputeFlashbackExportElapsedMs(");
        AssertDoesNotContain(snapshotsText, "private static long ComputeFlashbackExportLastProgressAgeMs(");
        AssertDoesNotContain(snapshotsText, "private static long GetFileLengthOrZero(string? path)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.HealthSnapshotFlashbackBackend.cs")),
            "Flashback backend health fields folded into health snapshot sampler");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.SnapshotRecordingStats.cs")),
            "old recording stats snapshot partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.SnapshotRecordingFormat.cs")),
            "old recording format snapshot partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.SnapshotObservedFrames.cs")),
            "old observed frames snapshot partial removed");

        return Task.CompletedTask;
    }

    // ── CaptureService.Snapshots: ResolveEncoderCodecName ──

    internal static Task CaptureService_ResolveEncoderCodecName_MapsFormats()
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

    internal static Task CaptureService_ResolveEncoderOutputPixelFormat_DistinguishesHdr()
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

    // ── CaptureService.Snapshots: ResolveHdrWarmupState ──

    internal static Task CaptureService_ResolveHdrWarmupState_ReturnsCorrectStates()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveHdrWarmupState",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveHdrWarmupState not found.");
        var snapshotsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Snapshots.cs")
            .Replace("\r\n", "\n");
        var hdrPipelineText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        AssertDoesNotContain(snapshotsText, "private static string ResolveHdrWarmupState(");
        AssertContains(hdrPipelineText, "private static string ResolveHdrWarmupState(");

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

    internal static Task CaptureService_ObservedPixelTelemetry_LivesWithSourceTelemetry()
    {
        var telemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Telemetry.cs")
            .Replace("\r\n", "\n");

        AssertContains(telemetryText, "private void ResetObservedPixelTelemetry()");
        AssertContains(telemetryText, "private static string? NormalizeObservedPixelFormat(string? pixelFormat)");
        AssertContains(telemetryText, "private void RecordObservedPixelFormat(string? pixelFormat, bool incrementAsFrame = true)");
        AssertContains(telemetryText, "Interlocked.Exchange(ref _observedP010FrameCount, 0);");
        AssertContains(telemetryText, "Interlocked.Increment(ref _observedP010FrameCount);");
        AssertContains(telemetryText, "Interlocked.Increment(ref _observedNv12FrameCount);");
        AssertContains(telemetryText, "Interlocked.Increment(ref _observedOtherFrameCount);");
        AssertContains(telemetryText, "private void CaptureEncoderRuntimeTelemetry(LibAvRecordingSink? sink)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.ObservedPixelTelemetry.cs")),
            "old observed pixel telemetry partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.CaptureFormatTelemetry.cs")),
            "old capture-format telemetry partial removed");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_NormalizeObservedPixelFormat_NormalizesCorrectly()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("NormalizeObservedPixelFormat",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("NormalizeObservedPixelFormat not found.");

        var p010Lower = method.Invoke(null, new object?[] { "p010" })?.ToString();
        AssertEqual("P010", p010Lower, "p010 -> P010");

        var p010Mixed = method.Invoke(null, new object?[] { "P010" })?.ToString();
        AssertEqual("P010", p010Mixed, "P010 stays P010");

        var nv12Lower = method.Invoke(null, new object?[] { "nv12" })?.ToString();
        AssertEqual("NV12", nv12Lower, "nv12 -> NV12");

        var bgra = method.Invoke(null, new object?[] { "bgra" })?.ToString();
        AssertEqual("BGRA", bgra, "bgra -> BGRA");

        var nullResult = method.Invoke(null, new object?[] { null });
        AssertEqual(true, nullResult == null, "null -> null");

        return Task.CompletedTask;
    }

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

        var nativeXuTelemetry = RuntimeHelpers.GetUninitializedObject(telemetryType);
        SetPropertyBackingField(nativeXuTelemetry, "Origin", Enum.Parse(originType, "NativeXu"));
        var nativeXuResult = method.Invoke(null, new[] { nativeXuTelemetry })?.ToString();
        AssertContains(nativeXuResult ?? "", "NativeXu");

        var fallbackTelemetry = RuntimeHelpers.GetUninitializedObject(telemetryType);
        SetPropertyBackingField(fallbackTelemetry, "Origin", Enum.Parse(originType, "DeviceFormatFallback"));
        var fallbackResult = method.Invoke(null, new[] { fallbackTelemetry })?.ToString();
        AssertContains(fallbackResult ?? "", "DeviceFormat");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_ResolveEncoderVideoProfile_MapsFormatsAndHdr()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveEncoderVideoProfile",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveEncoderVideoProfile not found.");

        var contextType = RequireType("Sussudio.Services.Contracts.RecordingContext");
        var settingsType = RequireType("Sussudio.Models.CaptureSettings");
        var formatType = RequireType("Sussudio.Models.RecordingFormat");

        var hdrCtx = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(hdrCtx, "HdrPipelineActive", true);
        var settings = Activator.CreateInstance(settingsType)!;
        AssertEqual("main10", method.Invoke(null, new[] { hdrCtx, settings })?.ToString(), "HDR -> main10");

        var sdrCtx = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(sdrCtx, "HdrPipelineActive", false);
        var h264Settings = Activator.CreateInstance(settingsType)!;
        settingsType.GetProperty("Format")!.SetValue(h264Settings, Enum.Parse(formatType, "H264Mp4"));
        AssertEqual("high", method.Invoke(null, new[] { sdrCtx, h264Settings })?.ToString(), "H264 SDR -> high");

        var hevcSettings = Activator.CreateInstance(settingsType)!;
        settingsType.GetProperty("Format")!.SetValue(hevcSettings, Enum.Parse(formatType, "HevcMp4"));
        AssertEqual("main", method.Invoke(null, new[] { sdrCtx, hevcSettings })?.ToString(), "HEVC SDR -> main");

        AssertEqual(true, method.Invoke(null, new object?[] { sdrCtx, null }) == null, "null settings -> null");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_ComputeTickAge_ReturnsCorrectValues()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ComputeTickAge",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ComputeTickAge not found.");

        var zeroResult = (long)method.Invoke(null, new object[] { 0L })!;
        AssertEqual(-1L, zeroResult, "tick=0 -> -1");

        var recentTick = Environment.TickCount64 - 100;
        var recentAge = (long)method.Invoke(null, new object[] { recentTick })!;
        AssertEqual(true, recentAge >= 0 && recentAge < 5000, $"Recent tick age should be 0-5000ms, got {recentAge}");

        return Task.CompletedTask;
    }

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
        var runtimeSourceTelemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");
        AssertDoesNotContain(snapshotsText, "private static (string Status, string Reason) ResolveTelemetryAlignment(");
        AssertContains(runtimeSourceTelemetryText, "private static (string Status, string Reason) ResolveTelemetryAlignment(");

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
        AssertEqual("Aligned", status, "Matching telemetry -> Aligned");

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

        var unavailTelemetry = RuntimeHelpers.GetUninitializedObject(telemetryType);
        SetPropertyBackingField(unavailTelemetry, "Availability", Enum.Parse(availabilityType, "Unavailable"));
        SetPropertyBackingField(unavailTelemetry, "DiagnosticSummary", "No device");

        var unavailResult = method.Invoke(null, new object?[] { settings, unavailTelemetry, (uint?)1920, (uint?)1080, (double?)60.0, false });
        var unavailStatus = unavailResult!.GetType().GetField("Item1")!.GetValue(unavailResult)?.ToString();
        AssertEqual("Unavailable", unavailStatus, "Unavailable telemetry -> Unavailable");

        var mismatchTelemetry = RuntimeHelpers.GetUninitializedObject(telemetryType);
        SetPropertyBackingField(mismatchTelemetry, "Availability", Enum.Parse(availabilityType, "Available"));
        SetPropertyBackingField(mismatchTelemetry, "Width", (int?)1280);
        SetPropertyBackingField(mismatchTelemetry, "Height", (int?)720);
        SetPropertyBackingField(mismatchTelemetry, "FrameRateExact", (double?)60.0);

        var mismatchResult = method.Invoke(null, new object?[] { settings, mismatchTelemetry, (uint?)1920, (uint?)1080, (double?)60.0, false });
        var mismatchStatus = mismatchResult!.GetType().GetField("Item1")!.GetValue(mismatchResult)?.ToString();
        AssertEqual("Mismatch", mismatchStatus, "Dimension mismatch -> Mismatch");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_ResolveSourceTelemetryCircuitState_ReturnsCorrectState()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var method = serviceType.GetMethod("ResolveSourceTelemetryCircuitState",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveSourceTelemetryCircuitState not found.");

        var availabilityType = RequireType("Sussudio.Models.SourceTelemetryAvailability");

        var closed = method.Invoke(null, new object[] { Enum.Parse(availabilityType, "Available"), false })?.ToString();
        AssertEqual("Closed", closed, "Available + not suppressed -> Closed");

        var suppressed = method.Invoke(null, new object[] { Enum.Parse(availabilityType, "Available"), true })?.ToString();
        AssertEqual("Open", suppressed, "Suppressed -> Open");

        var unavailable = method.Invoke(null, new object[] { Enum.Parse(availabilityType, "Unavailable"), false })?.ToString();
        AssertEqual("Open", unavailable, "Unavailable -> Open");

        return Task.CompletedTask;
    }
}
