using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureService_SnapshotHelperPolicy_LivesInFocusedPartials()
    {
        var snapshotsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Snapshots.cs")
            .Replace("\r\n", "\n");
        var recordingStatsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.SnapshotRecordingStats.cs")
            .Replace("\r\n", "\n");
        var formatText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.SnapshotRecordingFormat.cs")
            .Replace("\r\n", "\n");
        var observedText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.SnapshotObservedFrames.cs")
            .Replace("\r\n", "\n");
        var flashbackBufferText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackBackend.cs")
            .Replace("\r\n", "\n");
        var flashbackExportText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackExport.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotsText, "public CaptureDiagnosticsSnapshot GetDiagnosticsSnapshot()");
        AssertContains(snapshotsText, "return GetHealthSnapshot();");
        AssertContains(snapshotsText, "private static long ComputeTickAge(long tick)");
        AssertContains(recordingStatsText, "public RecordingStats GetRecordingStats()");
        AssertContains(recordingStatsText, "return new RecordingStats(_recordingBackend.LibAvSink.OutputBytes, 0);");
        AssertContains(
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs"),
            "private readonly CaptureRecordingBackendResources _recordingBackend = new();");
        AssertContains(recordingStatsText, "IsFlashbackRecordingBackendActive()");
        AssertContains(recordingStatsText, "bufferManager.TotalBytesWritten - _flashbackRecordingStartBytes");
        AssertContains(recordingStatsText, "isFlashbackEstimate: true");
        AssertContains(recordingStatsText, "new FileInfo(path).Length");
        AssertContains(recordingStatsText, "catch (FileNotFoundException)");
        AssertContains(recordingStatsText, "isFailure: true");

        AssertContains(formatText, "private static string? ResolveEncoderCodecName(");
        AssertContains(formatText, "MediaFormat.MapNvencCodecName(settings.Format)");
        AssertContains(formatText, "private static string? ResolveEncoderOutputPixelFormat(");
        AssertContains(formatText, "return \"yuv420p10le\";");
        AssertContains(formatText, "private static string? ResolveEncoderVideoProfile(");
        AssertContains(formatText, "RecordingFormat.H264Mp4 => \"high\"");
        AssertContains(formatText, "private static string? ResolveRequestedFrameRateArg(");
        AssertContains(formatText, "RequestedFrameRateNumerator is uint numerator");
        AssertContains(formatText, "RequestedFrameRateDenominator is uint denominator");

        AssertContains(observedText, "private ObservedFrameSnapshotFields ResolveObservedFrameTelemetry()");
        AssertContains(observedText, "private readonly record struct ObservedFrameSnapshotFields(");
        AssertContains(observedText, "return new ObservedFrameSnapshotFields(");
        AssertContains(observedText, "Math.Max(0, Interlocked.Read(ref _observedP010FrameCount))");
        AssertContains(observedText, "Math.Max(0, Interlocked.Read(ref _observedNv12FrameCount))");
        AssertContains(observedText, "Math.Max(0, Interlocked.Read(ref _observedOtherFrameCount))");
        AssertContains(flashbackBufferText, "private static string ResolveFlashbackBackendSettingsStaleReason(");
        AssertContains(flashbackExportText, "private static long ComputeFlashbackExportElapsedMs(");
        AssertContains(flashbackExportText, "private static long ComputeFlashbackExportLastProgressAgeMs(");
        AssertContains(flashbackExportText, "private static long GetFileLengthOrZero(string? path)");

        AssertDoesNotContain(snapshotsText, "private static string? ResolveEncoderCodecName(");
        AssertDoesNotContain(snapshotsText, "private static string? ResolveEncoderOutputPixelFormat(");
        AssertDoesNotContain(snapshotsText, "private static string? ResolveEncoderVideoProfile(");
        AssertDoesNotContain(snapshotsText, "private static string? ResolveRequestedFrameRateArg(");
        AssertDoesNotContain(snapshotsText, "ResolveObservedFrameTelemetry()");
        AssertDoesNotContain(snapshotsText, "private static string ResolveFlashbackBackendSettingsStaleReason(");
        AssertDoesNotContain(snapshotsText, "private static long ComputeFlashbackExportElapsedMs(");
        AssertDoesNotContain(snapshotsText, "private static long ComputeFlashbackExportLastProgressAgeMs(");
        AssertDoesNotContain(snapshotsText, "private static long GetFileLengthOrZero(string? path)");
        AssertDoesNotContain(snapshotsText, "public RecordingStats GetRecordingStats()");
        AssertDoesNotContain(snapshotsText, "new FileInfo(path).Length");

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
        var telemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.SnapshotTelemetry.cs")
            .Replace("\r\n", "\n");
        var hdrPipelineText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshotHdrPipeline.cs")
            .Replace("\r\n", "\n");
        AssertDoesNotContain(snapshotsText, "private static string ResolveHdrWarmupState(");
        AssertDoesNotContain(telemetryText, "private static string ResolveHdrWarmupState(");
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
}
