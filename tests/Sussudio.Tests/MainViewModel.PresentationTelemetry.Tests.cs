using System;
using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task SourceTelemetryPresentationBuilder_PreservesSummaryAndTargetText()
    {
        var builderType = RequireType("Sussudio.ViewModels.SourceTelemetryPresentationBuilder");
        var snapshotType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var buildSourceSummary = builderType.GetMethod(
            "BuildSourceSummary",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SourceTelemetryPresentationBuilder.BuildSourceSummary was not found.");
        var buildAgeText = builderType.GetMethod(
            "BuildAgeText",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SourceTelemetryPresentationBuilder.BuildAgeText was not found.");
        var buildTargetSummary = builderType.GetMethod(
            "BuildTargetSummary",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SourceTelemetryPresentationBuilder.BuildTargetSummary was not found.");

        var now = new DateTimeOffset(2026, 5, 14, 22, 10, 30, TimeSpan.Zero);
        var unavailable = snapshotType.GetMethod(
            "CreateUnavailable",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string) },
            modifiers: null)!.Invoke(null, new object?[] { "telemetry-not-started", null })!;
        AssertEqual(
            "Source: waiting for signal telemetry",
            buildSourceSummary.Invoke(null, new[] { unavailable, now }),
            "Source telemetry unavailable summary");

        var full = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create SourceSignalTelemetrySnapshot.");
        SetPropertyOrBackingField(full, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Available"));
        SetPropertyOrBackingField(full, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(full, "Width", 3840);
        SetPropertyOrBackingField(full, "Height", 2160);
        SetPropertyOrBackingField(full, "FrameRateExact", 120000d / 1001d);
        SetPropertyOrBackingField(full, "FrameRateArg", "120000/1001");
        SetPropertyOrBackingField(full, "IsHdr", true);
        SetPropertyOrBackingField(full, "TimestampUtc", now.AddSeconds(-17));
        AssertEqual(
            "Source: 3840x2160 @ 120000/1001 | HDR | Available/High | updated 17s ago",
            buildSourceSummary.Invoke(null, new[] { full, now }),
            "Source telemetry full summary");

        var partial = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create partial SourceSignalTelemetrySnapshot.");
        SetPropertyOrBackingField(partial, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Stale"));
        SetPropertyOrBackingField(partial, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "Low"));
        SetPropertyOrBackingField(partial, "FrameRateExact", 59.94d);
        SetPropertyOrBackingField(partial, "TimestampUtc", now.AddSeconds(2));
        AssertEqual(
            "Source: ?x? @ 59.94 | HDR? | Stale/Low | updated now",
            buildSourceSummary.Invoke(null, new[] { partial, now }),
            "Source telemetry partial summary");

        AssertEqual(
            "updated ?",
            buildAgeText.Invoke(null, new object?[] { null, now }),
            "Source telemetry null age");
        AssertEqual(
            "Target: Auto (3840 x 2160) @ 60 (exact 60000/1001) | HDR=Ready",
            buildTargetSummary.Invoke(null, new object?[] { "Auto (3840 x 2160)", 59.94d, 60d, 60000d / 1001d, "60000/1001", "Ready" }),
            "Source telemetry target summary exact rational");
        AssertEqual(
            "Target: 1080p @ 0 (exact ?) | HDR=Unknown",
            buildTargetSummary.Invoke(null, new object?[] { "1080p", 0d, null, null, null, " " }),
            "Source telemetry target summary unknown HDR");

        return Task.CompletedTask;
    }

    private static Task SourceTelemetryPresentationBuilder_LivesInFocusedHelper()
    {
        var telemetryText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSourceTelemetryController.cs").Replace("\r\n", "\n");
        var capturePresentationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CapturePresentation.cs").Replace("\r\n", "\n");
        var builderText = ReadRepoFile("Sussudio/ViewModels/SourceTelemetryPresentationBuilder.cs").Replace("\r\n", "\n");

        AssertContains(telemetryText, "SourceTelemetryPresentationBuilder.BuildSourceSummary(_viewModel._latestSourceTelemetry, DateTimeOffset.UtcNow);");
        AssertContains(telemetryText, "SourceTelemetryPresentationBuilder.BuildSourceSummary(snapshot, DateTimeOffset.UtcNow);");
        AssertContains(telemetryText, "UpdateTargetSummary();");
        AssertDoesNotContain(telemetryText, "private void UpdateHdrRuntimeStatusFromCapture(");
        AssertContains(capturePresentationText, "private void UpdateHdrRuntimeStatusFromCapture(CaptureRuntimeSnapshot? runtimeSnapshot = null)");
        AssertContains(capturePresentationText, "HdrRuntimeState = runtime.HdrRuntimeState;");
        AssertContains(capturePresentationText, "HdrReadinessReason = runtime.HdrReadinessReason;");
        AssertContains(capturePresentationText, "UpdateTargetSummary();");
        AssertDoesNotContain(telemetryText, "private void UpdateTargetSummary()");
        AssertDoesNotContain(telemetryText, "SourceTelemetryPresentationBuilder.BuildTargetSummary(");
        AssertContains(capturePresentationText, "private void UpdateTargetSummary()");
        AssertContains(capturePresentationText, "SourceTargetSummaryText = SourceTelemetryPresentationBuilder.BuildTargetSummary(");
        AssertContains(capturePresentationText, "GetSelectedResolutionDisplayText(),");
        AssertContains(capturePresentationText, "SelectedFriendlyFrameRate,");
        AssertContains(capturePresentationText, "SelectedExactFrameRate,");
        AssertContains(capturePresentationText, "SelectedExactFrameRateArg,");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.HdrRuntimePresentation.cs")),
            "old HDR runtime presentation file removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.TargetSummaryPresentation.cs")),
            "old target summary presentation file removed");
        AssertContains(capturePresentationText, "private string GetSelectedResolutionDisplayText()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.TargetPresentation.cs")),
            "old target presentation file removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutoResolutionPresentation.cs")),
            "old auto resolution presentation file removed");
        AssertDoesNotContain(telemetryText, "private static string BuildSourceTelemetrySummaryText(");
        AssertDoesNotContain(telemetryText, "private static string BuildTelemetryAgeText(");
        AssertDoesNotContain(telemetryText, "Source: waiting for signal telemetry");
        AssertDoesNotContain(telemetryText, "Target: {GetSelectedResolutionDisplayText()}");
        AssertContains(builderText, "internal static class SourceTelemetryPresentationBuilder");
        AssertContains(builderText, "internal static string BuildSourceSummary(SourceSignalTelemetrySnapshot snapshot, DateTimeOffset nowUtc)");
        AssertContains(builderText, "internal static string BuildAgeText(DateTimeOffset? timestampUtc, DateTimeOffset nowUtc)");
        AssertContains(builderText, "TelemetryAgeHelper.ComputeAgeSeconds(timestampUtc, nowUtc)");
        AssertContains(builderText, "snapshot.FrameRateArg ??");
        AssertContains(builderText, "snapshot.FrameRateExact?.ToString(\"0.###\")");
        AssertContains(builderText, "snapshot.IsHdr.HasValue ? (snapshot.IsHdr.Value ? \"HDR\" : \"SDR\") : \"HDR?\"");
        AssertContains(builderText, "internal static string BuildTargetSummary(");
        AssertContains(builderText, "string.IsNullOrWhiteSpace(hdrRuntimeState) ? \"Unknown\" : hdrRuntimeState");
        AssertDoesNotContain(builderText, "GetSelectedResolutionDisplayText()");
        AssertDoesNotContain(builderText, "SourceTelemetrySummaryText =");

        return Task.CompletedTask;
    }

    private static Task LiveSignalTextProjection_PreservesPixelFormatFallbackOrder()
    {
        var runtimeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRuntimeLifecycleController.cs")
            .Replace("\r\n", "\n");
        var capturePresentationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CapturePresentation.cs")
            .Replace("\r\n", "\n");
        var liveSignalText = ReadRepoFile("Sussudio/ViewModels/LiveSignalTextPresentationBuilder.cs")
            .Replace("\r\n", "\n");
        var builderType = RequireType("Sussudio.ViewModels.LiveSignalTextPresentationBuilder");
        var snapshotType = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var buildMethod = builderType.GetMethod(
            "Build",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build was not found.");

        AssertContains(runtimeLifecycleControllerText, "_viewModel.UpdateLiveCaptureInfo(runtimeSnapshot);");
        AssertContains(runtimeLifecycleControllerText, "_viewModel.ResetLiveCaptureInfo();");
        AssertDoesNotContain(runtimeLifecycleControllerText, "IsAudioPreviewActive =");
        AssertDoesNotContain(runtimeLifecycleControllerText, "private void UpdateLiveCaptureInfo(");
        AssertDoesNotContain(runtimeLifecycleControllerText, "private void ResetLiveCaptureInfo()");
        AssertContains(capturePresentationText, "private void UpdateLiveCaptureInfo(CaptureRuntimeSnapshot? runtimeSnapshot = null)");
        AssertContains(capturePresentationText, "IsAudioPreviewActive = runtime.IsAudioPreviewActive;");
        AssertContains(capturePresentationText, "var liveSignalText = LiveSignalTextPresentationBuilder.Build(");
        AssertContains(capturePresentationText, "_captureService.EncoderCodecName,");
        AssertContains(capturePresentationText, "LiveInfoUnavailable);");
        AssertContains(capturePresentationText, "LiveResolution = liveSignalText.Resolution;");
        AssertContains(capturePresentationText, "LiveFrameRate = liveSignalText.FrameRate;");
        AssertContains(capturePresentationText, "LivePixelFormat = liveSignalText.PixelFormat;");
        AssertContains(capturePresentationText, "private void ResetLiveCaptureInfo()");
        AssertContains(capturePresentationText, "partial void OnIsPreviewingChanged(bool value)");
        AssertContains(capturePresentationText, "if (!value && !IsRecording)");
        AssertContains(capturePresentationText, "IsAudioPreviewActive = false;");
        AssertContains(capturePresentationText, "LiveResolution = LiveInfoUnavailable;");
        AssertContains(capturePresentationText, "LiveFrameRate = LiveInfoUnavailable;");
        AssertContains(capturePresentationText, "LivePixelFormat = LiveInfoUnavailable;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.LiveSignalPresentation.cs")),
            "old live signal presentation file removed");
        AssertDoesNotContain(runtimeLifecycleControllerText, "runtime.ReaderSourceSubtype ??");
        AssertDoesNotContain(runtimeLifecycleControllerText, "runtime.LatestObservedFramePixelFormat ??");
        AssertContains(liveSignalText, "internal static class LiveSignalTextPresentationBuilder");
        AssertContains(liveSignalText, "internal static LiveSignalTextPresentation Build(");
        AssertContains(liveSignalText, "runtime.ActualWidth ?? runtime.NegotiatedWidth ?? runtime.RequestedWidth");
        AssertContains(liveSignalText, "runtime.ActualHeight ?? runtime.NegotiatedHeight ?? runtime.RequestedHeight");
        AssertContains(liveSignalText, "runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate ?? runtime.RequestedFrameRate");
        AssertContains(liveSignalText, "frameRateValue.Value.ToString(\"0.00\")");
        AssertContains(liveSignalText, "runtime.ReaderSourceSubtype ??");
        AssertContains(liveSignalText, "runtime.LatestObservedFramePixelFormat ??");
        AssertContains(liveSignalText, "\"hevc_nvenc\" => \" / HEVC\"");
        AssertContains(liveSignalText, "\"h264_nvenc\" => \" / H264\"");
        AssertContains(liveSignalText, "\"av1_nvenc\" => \" / AV1\"");
        AssertContains(liveSignalText, "? unavailableText");

        if (liveSignalText.IndexOf("runtime.ReaderSourceSubtype ??", StringComparison.Ordinal) >
            liveSignalText.IndexOf("runtime.LatestObservedFramePixelFormat ??", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("MainViewModel.LivePixelFormat should prefer ReaderSourceSubtype before LatestObservedFramePixelFormat.");
        }

        var actualRuntime = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create CaptureRuntimeSnapshot.");
        SetPropertyOrBackingField(actualRuntime, "RequestedWidth", (uint?)1280);
        SetPropertyOrBackingField(actualRuntime, "RequestedHeight", (uint?)720);
        SetPropertyOrBackingField(actualRuntime, "NegotiatedWidth", (uint?)1920);
        SetPropertyOrBackingField(actualRuntime, "NegotiatedHeight", (uint?)1080);
        SetPropertyOrBackingField(actualRuntime, "ActualWidth", (uint?)3840);
        SetPropertyOrBackingField(actualRuntime, "ActualHeight", (uint?)2160);
        SetPropertyOrBackingField(actualRuntime, "RequestedFrameRate", 30d);
        SetPropertyOrBackingField(actualRuntime, "NegotiatedFrameRate", 59.94d);
        SetPropertyOrBackingField(actualRuntime, "ActualFrameRate", 119.88d);
        SetPropertyOrBackingField(actualRuntime, "RequestedPixelFormat", "MJPG");
        SetPropertyOrBackingField(actualRuntime, "RequestedReaderSubtype", "YUY2");
        SetPropertyOrBackingField(actualRuntime, "LatestObservedFramePixelFormat", "P010");
        SetPropertyOrBackingField(actualRuntime, "NegotiatedPixelFormat", "NV12");
        SetPropertyOrBackingField(actualRuntime, "VideoNegotiatedSubtype", "I420");
        SetPropertyOrBackingField(actualRuntime, "ReaderSourceSubtype", "RGB32");

        var actualPresentation = buildMethod.Invoke(null, new object?[] { actualRuntime, "av1_nvenc", "\u2014" })
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build returned null.");
        AssertEqual("3840x2160", GetStringProperty(actualPresentation, "Resolution"), "Live signal actual resolution");
        AssertEqual("119.88", GetStringProperty(actualPresentation, "FrameRate"), "Live signal actual frame rate");
        AssertEqual("RGB32 / AV1", GetStringProperty(actualPresentation, "PixelFormat"), "Live signal reader subtype");

        var fallbackRuntime = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create fallback CaptureRuntimeSnapshot.");
        SetPropertyOrBackingField(fallbackRuntime, "RequestedWidth", (uint?)1280);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedHeight", (uint?)720);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedFrameRate", 30d);
        SetPropertyOrBackingField(fallbackRuntime, "VideoNegotiatedSubtype", null);
        SetPropertyOrBackingField(fallbackRuntime, "NegotiatedPixelFormat", null);
        SetPropertyOrBackingField(fallbackRuntime, "LatestObservedFramePixelFormat", null);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedReaderSubtype", null);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedPixelFormat", "MJPG");
        var fallbackPresentation = buildMethod.Invoke(null, new object?[] { fallbackRuntime, "hevc_nvenc", "\u2014" })
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build returned null.");
        AssertEqual("1280x720", GetStringProperty(fallbackPresentation, "Resolution"), "Live signal requested resolution fallback");
        AssertEqual("30.00", GetStringProperty(fallbackPresentation, "FrameRate"), "Live signal requested frame-rate fallback");
        AssertEqual("MJPG / HEVC", GetStringProperty(fallbackPresentation, "PixelFormat"), "Live signal requested pixel-format fallback");

        var unavailablePresentation = buildMethod.Invoke(
                null,
                new object?[] { CreateLiveSignalUnavailableRuntime(snapshotType), "libx264", "\u2014" })
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build returned null.");
        AssertEqual("\u2014", GetStringProperty(unavailablePresentation, "Resolution"), "Live signal unavailable resolution");
        AssertEqual("\u2014", GetStringProperty(unavailablePresentation, "FrameRate"), "Live signal unavailable frame-rate");
        AssertEqual("\u2014", GetStringProperty(unavailablePresentation, "PixelFormat"), "Live signal unavailable pixel format");

        return Task.CompletedTask;
    }

    private static object CreateLiveSignalUnavailableRuntime(Type snapshotType)
    {
        var runtime = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create unavailable CaptureRuntimeSnapshot.");
        SetPropertyOrBackingField(runtime, "VideoNegotiatedSubtype", null);
        SetPropertyOrBackingField(runtime, "NegotiatedPixelFormat", null);
        SetPropertyOrBackingField(runtime, "LatestObservedFramePixelFormat", null);
        SetPropertyOrBackingField(runtime, "RequestedReaderSubtype", null);
        SetPropertyOrBackingField(runtime, "RequestedPixelFormat", null);
        return runtime;
    }
}
