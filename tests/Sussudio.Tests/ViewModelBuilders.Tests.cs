using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class ViewModelBuildersTests
{
    [Fact]
    public void Build_PreservesAutomationOptionsSnapshotContract()
    {
        var asm = SussudioAssembly.Load();
        var builderType = asm.GetType("Sussudio.ViewModels.AutomationOptionsSnapshotBuilder", throwOnError: true)!;
        var inputType = asm.GetType("Sussudio.ViewModels.AutomationOptionsSnapshotInput", throwOnError: true)!;
        var deviceInputType = asm.GetType("Sussudio.ViewModels.AutomationOptionsDeviceInput", throwOnError: true)!;
        var resolutionInputType = asm.GetType("Sussudio.ViewModels.AutomationOptionsResolutionInput", throwOnError: true)!;
        var frameRateInputType = asm.GetType("Sussudio.ViewModels.AutomationOptionsFrameRateInput", throwOnError: true)!;

        var timestamp = new DateTimeOffset(2026, 5, 16, 1, 2, 3, TimeSpan.Zero);
        var input = CreateInput(inputType,
            ("TimestampUtc", timestamp),
            ("Devices", InputArray(deviceInputType,
                CreateInput(deviceInputType, ("Id", "device-a"), ("Name", "Device A")),
                CreateInput(deviceInputType, ("Id", "DEVICE-B"), ("Name", "Device B")))),
            ("AudioInputDevices", InputArray(deviceInputType,
                CreateInput(deviceInputType, ("Id", "audio-a"), ("Name", "Audio A")))),
            ("Resolutions", InputArray(resolutionInputType,
                CreateInput(resolutionInputType,
                    ("Value", "1920x1080"),
                    ("Width", 1920u),
                    ("Height", 1080u),
                    ("IsEnabled", true),
                    ("DisableReason", null)),
                CreateInput(resolutionInputType,
                    ("Value", "3840x2160"),
                    ("Width", 3840u),
                    ("Height", 2160u),
                    ("IsEnabled", false),
                    ("DisableReason", "Unavailable")))),
            ("FrameRates", InputArray(frameRateInputType,
                CreateInput(frameRateInputType,
                    ("Value", 59.94d),
                    ("FriendlyValue", 60d),
                    ("ExactValueArg", "60000/1001"),
                    ("IsEnabled", true),
                    ("DisableReason", null),
                    ("IsSelected", false)),
                CreateInput(frameRateInputType,
                    ("Value", 60d),
                    ("FriendlyValue", 60d),
                    ("ExactValueArg", null),
                    ("IsEnabled", false),
                    ("DisableReason", null),
                    ("IsSelected", true)))),
            ("RecordingFormats", new[] { "H264", "AV1" }),
            ("Qualities", new[] { "High", "Medium" }),
            ("Presets", new[] { "Quality", "Speed" }),
            ("SplitEncodeModes", new[] { "Auto", "Disabled" }),
            ("VideoFormats", new[] { "Auto", "MJPG" }),
            ("SelectedDeviceId", "device-b"),
            ("SelectedAudioInputDeviceId", "AUDIO-A"),
            ("SelectedResolution", "1920X1080"),
            ("SelectedFrameRate", 60d),
            ("SelectedRecordingFormat", "av1"),
            ("SelectedQuality", "medium"),
            ("SelectedPreset", "speed"),
            ("SelectedSplitEncodeMode", "disabled"),
            ("SelectedVideoFormat", "mjpg"),
            ("MjpegDecoderCount", 99),
            ("PreviewVolume", 0.425d),
            ("IsStatsVisible", true));

        var build = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        var snapshot = build.Invoke(null, new[] { input })!;

        Assert.Equal(timestamp, Get(snapshot, "TimestampUtc"));
        Assert.Equal("device-b", Get(snapshot, "SelectedDeviceId"));
        Assert.Equal("AUDIO-A", Get(snapshot, "SelectedAudioInputDeviceId"));
        Assert.Equal("1920X1080", Get(snapshot, "SelectedResolution"));
        Assert.Equal(60d, Get(snapshot, "SelectedFrameRate"));
        Assert.Equal(8, Get(snapshot, "MjpegDecoderCount"));
        Assert.Equal(42.5d, Get(snapshot, "PreviewVolumePercent"));
        Assert.True((bool)Get(snapshot, "IsStatsVisible")!);

        var devices = (Array)Get(snapshot, "Devices")!;
        Assert.False((bool)Get(devices.GetValue(0)!, "IsSelected")!);
        Assert.True((bool)Get(devices.GetValue(1)!, "IsSelected")!);

        var audioDevices = (Array)Get(snapshot, "AudioInputDevices")!;
        Assert.True((bool)Get(audioDevices.GetValue(0)!, "IsSelected")!);

        var resolutions = (Array)Get(snapshot, "Resolutions")!;
        Assert.Equal(string.Empty, Get(resolutions.GetValue(0)!, "DisableReason"));
        Assert.True((bool)Get(resolutions.GetValue(0)!, "IsSelected")!);
        Assert.Equal(3840, Get(resolutions.GetValue(1)!, "Width"));
        Assert.Equal("Unavailable", Get(resolutions.GetValue(1)!, "DisableReason"));

        var frameRates = (Array)Get(snapshot, "FrameRates")!;
        Assert.Equal("60000/1001", Get(frameRates.GetValue(0)!, "ExactValueArg"));
        Assert.Equal(string.Empty, Get(frameRates.GetValue(0)!, "DisableReason"));
        Assert.Equal(string.Empty, Get(frameRates.GetValue(1)!, "ExactValueArg"));
        Assert.True((bool)Get(frameRates.GetValue(1)!, "IsSelected")!);

        var recordingFormats = (Array)Get(snapshot, "RecordingFormats")!;
        Assert.True((bool)Get(recordingFormats.GetValue(1)!, "IsSelected")!);
        Assert.Equal(string.Empty, Get(recordingFormats.GetValue(1)!, "DisableReason"));

        var decoderCounts = (Array)Get(snapshot, "MjpegDecoderCounts")!;
        Assert.Equal(8, decoderCounts.Length);
        for (var i = 0; i < decoderCounts.Length; i++)
        {
            var option = decoderCounts.GetValue(i)!;
            Assert.Equal(i + 1, Get(option, "Value"));
            Assert.Equal(i == 7, (bool)Get(option, "IsSelected")!);
        }
    }

    [Fact]
    public void Build_PreservesViewModelRuntimeSnapshotContract()
    {
        var asm = SussudioAssembly.Load();
        var builderType = asm.GetType("Sussudio.ViewModels.ViewModelRuntimeSnapshotBuilder", throwOnError: true)!;
        var inputType = asm.GetType("Sussudio.ViewModels.ViewModelRuntimeSnapshotInput", throwOnError: true)!;
        var sessionSnapshotType = asm.GetType("Sussudio.Services.Capture.CaptureSessionSnapshot", throwOnError: true)!;
        var commandOutcomeType = asm.GetType("Sussudio.Services.Capture.CaptureCommandOutcome", throwOnError: true)!;

        var timestamp = new DateTimeOffset(2026, 5, 16, 12, 0, 10, TimeSpan.Zero);
        var telemetryTimestamp = timestamp.AddSeconds(-12);
        var sessionSnapshot = CreateInput(sessionSnapshotType,
            ("CommandsEnqueued", 11L),
            ("CommandsCompleted", 7L),
            ("CommandsFailed", 2L),
            ("CommandsCanceled", 1L),
            ("CommandsCoalesced", 3L),
            ("PendingCommands", 4),
            ("MaxPendingCommands", 6),
            ("OldestPendingCommandAgeMs", 123L),
            ("LastCommandQueueLatencyMs", 45L),
            ("MaxCommandQueueLatencyMs", 67L),
            ("LastOutcome", Enum.Parse(commandOutcomeType, "Completed")));

        var input = CreateInput(inputType,
            ("TimestampUtc", timestamp),
            ("SessionSnapshot", sessionSnapshot),
            ("IsInitialized", true),
            ("IsPreviewing", true),
            ("IsRecording", false),
            ("IsAudioEnabled", true),
            ("IsAudioPreviewEnabled", true),
            ("IsCustomAudioInputEnabled", false),
            ("StatusText", "Ready"),
            ("SelectedDeviceId", "device-1"),
            ("SelectedDeviceName", "Device One"),
            ("SelectedAudioInputDeviceId", "audio-1"),
            ("SelectedAudioInputDeviceName", "Audio One"),
            ("SelectedResolution", "3840x2160"),
            ("SelectedFrameRate", 119.88d),
            ("SelectedFriendlyFrameRate", 120d),
            ("SelectedExactFrameRate", 119.88d),
            ("SelectedExactFrameRateArg", "120000/1001"),
            ("DisabledResolutionReason", "resolution reason"),
            ("DisabledFrameRateReason", "frame reason"),
            ("HdrResolutionSupportHint", "HDR OK"),
            ("DetectedSourceFrameRate", 119.88d),
            ("DetectedSourceFrameRateArg", "120000/1001"),
            ("SourceFrameRateOrigin", "Telemetry"),
            ("SourceWidth", 3840),
            ("SourceHeight", 2160),
            ("SourceIsHdr", true),
            ("SourceTelemetryAvailability", "Available"),
            ("SourceTelemetryOriginDetail", "Native"),
            ("SourceTelemetryConfidence", "High"),
            ("SourceTelemetryDiagnosticSummary", "clean"),
            ("SourceTelemetryTimestampUtc", telemetryTimestamp),
            ("SourceTelemetrySummaryText", "source summary"),
            ("SourceTargetSummaryText", "target summary"),
            ("SelectedRecordingFormat", "HEVC"),
            ("SelectedQuality", "High"),
            ("SelectedPreset", "P5"),
            ("SelectedSplitEncodeMode", "Auto"),
            ("SelectedVideoFormat", "MJPG"),
            ("CustomBitrateMbps", 42d),
            ("PreviewVolume", 0.375d),
            ("IsStatsVisible", true),
            ("IsHdrAvailable", true),
            ("IsHdrEnabled", true),
            ("HdrRuntimeState", "Active"),
            ("HdrReadinessReason", "ready"),
            ("LiveResolution", "3840x2160"),
            ("LiveFrameRate", "119.88"),
            ("LivePixelFormat", "P010"),
            ("OutputPath", "C:\\Capture"),
            ("RecordingTime", "00:01:02"),
            ("RecordingSizeInfo", "10 MB"),
            ("RecordingBitrateInfo", "100 Mbps"),
            ("AudioPeak", 0.75d),
            ("AudioClipping", true));

        var build = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        var snapshot = build.Invoke(null, new[] { input })!;

        Assert.Equal(timestamp, Get(snapshot, "TimestampUtc"));
        Assert.True((bool)Get(snapshot, "IsInitialized")!);
        Assert.True((bool)Get(snapshot, "IsPreviewing")!);
        Assert.Equal("Ready", Get(snapshot, "StatusText"));
        Assert.Equal("device-1", Get(snapshot, "SelectedDeviceId"));
        Assert.Equal("3840x2160", Get(snapshot, "SelectedResolution"));
        Assert.Equal(119.88d, Get(snapshot, "SelectedFrameRate"));
        Assert.Equal("120000/1001", Get(snapshot, "SelectedExactFrameRateArg"));
        Assert.Equal(true, Get(snapshot, "SourceIsHdr"));
        Assert.Equal(12, Get(snapshot, "SourceTelemetryAgeSeconds"));
        Assert.Equal("source summary", Get(snapshot, "SourceTelemetrySummaryText"));
        Assert.Equal("HEVC", Get(snapshot, "SelectedRecordingFormat"));
        Assert.Equal(37.5d, Get(snapshot, "PreviewVolumePercent"));
        Assert.True((bool)Get(snapshot, "IsStatsVisible")!);
        Assert.Equal("Active", Get(snapshot, "HdrRuntimeState"));
        Assert.Equal("P010", Get(snapshot, "LivePixelFormat"));
        Assert.Equal("C:\\Capture", Get(snapshot, "OutputPath"));
        Assert.Equal("00:01:02", Get(snapshot, "RecordingTime"));
        Assert.Equal(0.75d, Get(snapshot, "AudioPeak"));
        Assert.True((bool)Get(snapshot, "AudioClipping")!);

        Assert.Equal(11L, Get(snapshot, "CaptureCommandCommandsEnqueued"));
        Assert.Equal(7L, Get(snapshot, "CaptureCommandCommandsCompleted"));
        Assert.Equal(2L, Get(snapshot, "CaptureCommandCommandsFailed"));
        Assert.Equal(1L, Get(snapshot, "CaptureCommandCommandsCanceled"));
        Assert.Equal(3L, Get(snapshot, "CaptureCommandCommandsCoalesced"));
        Assert.Equal(4, Get(snapshot, "CaptureCommandPendingCommands"));
        Assert.Equal(6, Get(snapshot, "CaptureCommandMaxPendingCommands"));
        Assert.Equal(123L, Get(snapshot, "CaptureCommandOldestPendingCommandAgeMs"));
        Assert.Equal(45L, Get(snapshot, "CaptureCommandLastQueueLatencyMs"));
        Assert.Equal(67L, Get(snapshot, "CaptureCommandMaxQueueLatencyMs"));
        Assert.Equal("None", Get(snapshot, "CaptureCommandLastCommand"));
        Assert.Equal("Completed", Get(snapshot, "CaptureCommandLastOutcome"));
        Assert.Equal(string.Empty, Get(snapshot, "CaptureCommandLastCorrelationId"));
        Assert.Equal(string.Empty, Get(snapshot, "CaptureCommandLastError"));
    }

    [Fact]
    public void LiveSignalTextProjection_PreservesPixelFormatFallbackOrder()
    {
        var runtimeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs");
        var capturePresentationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs");
        var liveSignalText = ReadRepoFile("Sussudio/ViewModels/ViewModelBuilders.cs");
        var builderType = RequireType("Sussudio.ViewModels.LiveSignalTextPresentationBuilder");
        var snapshotType = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var buildMethod = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build was not found.");

        AssertContains(runtimeLifecycleControllerText, "_context.UpdateLiveCaptureInfo(runtimeSnapshot);");
        AssertContains(runtimeLifecycleControllerText, "_context.ResetLiveCaptureInfo();");
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
        Assert.False(
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.CapturePresentation.cs")),
            "MainViewModel.CapturePresentation.cs folded into capture state");
        Assert.False(
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
        Assert.True(
            liveSignalText.IndexOf("runtime.ReaderSourceSubtype ??", StringComparison.Ordinal) <
            liveSignalText.IndexOf("runtime.LatestObservedFramePixelFormat ??", StringComparison.Ordinal),
            "MainViewModel.LivePixelFormat should prefer ReaderSourceSubtype before LatestObservedFramePixelFormat.");

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
        Assert.Equal("3840x2160", GetStringProperty(actualPresentation, "Resolution"));
        Assert.Equal("119.88", GetStringProperty(actualPresentation, "FrameRate"));
        Assert.Equal("RGB32 / AV1", GetStringProperty(actualPresentation, "PixelFormat"));

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
        Assert.Equal("1280x720", GetStringProperty(fallbackPresentation, "Resolution"));
        Assert.Equal("30.00", GetStringProperty(fallbackPresentation, "FrameRate"));
        Assert.Equal("MJPG / HEVC", GetStringProperty(fallbackPresentation, "PixelFormat"));

        var unavailablePresentation = buildMethod.Invoke(
                null,
                new object?[] { CreateLiveSignalUnavailableRuntime(snapshotType), "libx264", "\u2014" })
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build returned null.");
        Assert.Equal("\u2014", GetStringProperty(unavailablePresentation, "Resolution"));
        Assert.Equal("\u2014", GetStringProperty(unavailablePresentation, "FrameRate"));
        Assert.Equal("\u2014", GetStringProperty(unavailablePresentation, "PixelFormat"));
    }

    [Fact]
    public void SourceTelemetryPresentationBuilder_PreservesSummaryAndTargetText()
    {
        var builderType = RequireType("Sussudio.ViewModels.SourceTelemetryPresentationBuilder");
        var snapshotType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var buildSourceSummary = builderType.GetMethod("BuildSourceSummary", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SourceTelemetryPresentationBuilder.BuildSourceSummary was not found.");
        var buildAgeText = builderType.GetMethod("BuildAgeText", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SourceTelemetryPresentationBuilder.BuildAgeText was not found.");
        var buildTargetSummary = builderType.GetMethod("BuildTargetSummary", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SourceTelemetryPresentationBuilder.BuildTargetSummary was not found.");

        var now = new DateTimeOffset(2026, 5, 14, 22, 10, 30, TimeSpan.Zero);
        var unavailable = snapshotType.GetMethod(
            "CreateUnavailable",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string) },
            modifiers: null)!.Invoke(null, new object?[] { "telemetry-not-started", null })!;
        Assert.Equal(
            "Source: waiting for signal telemetry",
            buildSourceSummary.Invoke(null, new[] { unavailable, now }));

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
        Assert.Equal(
            "Source: 3840x2160 @ 120000/1001 | HDR | Available/High | updated 17s ago",
            buildSourceSummary.Invoke(null, new[] { full, now }));

        var partial = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create partial SourceSignalTelemetrySnapshot.");
        SetPropertyOrBackingField(partial, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Stale"));
        SetPropertyOrBackingField(partial, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "Low"));
        SetPropertyOrBackingField(partial, "FrameRateExact", 59.94d);
        SetPropertyOrBackingField(partial, "TimestampUtc", now.AddSeconds(2));
        Assert.Equal(
            "Source: ?x? @ 59.94 | HDR? | Stale/Low | updated now",
            buildSourceSummary.Invoke(null, new[] { partial, now }));

        Assert.Equal("updated ?", buildAgeText.Invoke(null, new object?[] { null, now }));
        Assert.Equal(
            "Target: Auto (3840 x 2160) @ 60 (exact 60000/1001) | HDR=Ready",
            buildTargetSummary.Invoke(null, new object?[] { "Auto (3840 x 2160)", 59.94d, 60d, 60000d / 1001d, "60000/1001", "Ready" }));
        Assert.Equal(
            "Target: 1080p @ 0 (exact ?) | HDR=Unknown",
            buildTargetSummary.Invoke(null, new object?[] { "1080p", 0d, null, null, null, " " }));
    }

    [Fact]
    public void SourceTelemetryPresentationBuilder_LivesInFocusedHelper()
    {
        var telemetryText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs");
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs");
        var capturePresentationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs");
        var builderText = ReadRepoFile("Sussudio/ViewModels/ViewModelBuilders.cs");
        var sourceTelemetryBuilderText = ExtractTextBetween(
            builderText,
            "internal static class SourceTelemetryPresentationBuilder",
            "internal static class AutomationOptionsSnapshotBuilder");

        AssertContains(telemetryText, "_context.BuildSourceTelemetrySummary(_context.GetLatestSourceTelemetry(), DateTimeOffset.UtcNow);");
        AssertContains(telemetryText, "_context.SetSourceTelemetrySummaryText(_context.BuildSourceTelemetrySummary(snapshot, DateTimeOffset.UtcNow));");
        AssertContains(controllerGraphText, "BuildSourceTelemetrySummary = SourceTelemetryPresentationBuilder.BuildSourceSummary,");
        AssertContains(telemetryText, "_context.UpdateTargetSummary();");
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
        Assert.False(
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.HdrRuntimePresentation.cs")),
            "old HDR runtime presentation file removed");
        Assert.False(
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.TargetSummaryPresentation.cs")),
            "old target summary presentation file removed");
        AssertContains(capturePresentationText, "private string GetSelectedResolutionDisplayText()");
        Assert.False(
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.TargetPresentation.cs")),
            "old target presentation file removed");
        Assert.False(
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutoResolutionPresentation.cs")),
            "old auto resolution presentation file removed");
        AssertDoesNotContain(telemetryText, "private static string BuildSourceTelemetrySummaryText(");
        AssertDoesNotContain(telemetryText, "private static string BuildTelemetryAgeText(");
        AssertDoesNotContain(telemetryText, "Source: waiting for signal telemetry");
        AssertDoesNotContain(telemetryText, "Target: {GetSelectedResolutionDisplayText()}");
        AssertContains(sourceTelemetryBuilderText, "internal static class SourceTelemetryPresentationBuilder");
        AssertContains(sourceTelemetryBuilderText, "internal static string BuildSourceSummary(SourceSignalTelemetrySnapshot snapshot, DateTimeOffset nowUtc)");
        AssertContains(sourceTelemetryBuilderText, "internal static string BuildAgeText(DateTimeOffset? timestampUtc, DateTimeOffset nowUtc)");
        AssertContains(sourceTelemetryBuilderText, "TelemetryAgeHelper.ComputeAgeSeconds(timestampUtc, nowUtc)");
        AssertContains(sourceTelemetryBuilderText, "snapshot.FrameRateArg ??");
        AssertContains(sourceTelemetryBuilderText, "snapshot.FrameRateExact?.ToString(\"0.###\")");
        AssertContains(sourceTelemetryBuilderText, "snapshot.IsHdr.HasValue ? (snapshot.IsHdr.Value ? \"HDR\" : \"SDR\") : \"HDR?\"");
        AssertContains(sourceTelemetryBuilderText, "internal static string BuildTargetSummary(");
        AssertContains(sourceTelemetryBuilderText, "string.IsNullOrWhiteSpace(hdrRuntimeState) ? \"Unknown\" : hdrRuntimeState");
        AssertDoesNotContain(sourceTelemetryBuilderText, "GetSelectedResolutionDisplayText()");
        AssertDoesNotContain(sourceTelemetryBuilderText, "SourceTelemetrySummaryText =");
    }

    private static object CreateInput(Type type, params (string Property, object? Value)[] values)
    {
        var instance = Activator.CreateInstance(type)
                       ?? throw new InvalidOperationException($"Failed to create {type.FullName}.");
        foreach (var (property, value) in values)
        {
            Set(instance, property, value);
        }

        return instance;
    }

    private static Array InputArray(Type elementType, params object[] values)
    {
        var array = Array.CreateInstance(elementType, values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            array.SetValue(values[i], i);
        }

        return array;
    }

    private static void Set(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                       ?? throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} was not found.");
        property.SetValue(instance, value);
    }

    private static object? Get(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                       ?? throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} was not found.");
        return property.GetValue(instance);
    }

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object ParseEnum(string typeName, string value)
        => Enum.Parse(RequireType(typeName), value);

    private static void SetPropertyOrBackingField(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Backing field for {propertyName} was not found.");
        field.SetValue(instance, value);
    }

    private static string GetStringProperty(object instance, string propertyName)
        => Get(instance, propertyName) as string
           ?? throw new InvalidOperationException($"{propertyName} was not a string.");

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

    private static string ReadRepoFile(string relativePath)
        => RuntimeContractSource.ReadRepoFile(relativePath).Replace("\r\n", "\n");

    private static string GetRepoRoot()
        => RuntimeContractSource.GetRepoRoot();

    private static string ExtractTextBetween(string source, string startToken, string endToken)
    {
        var start = source.IndexOf(startToken, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Start token '{startToken}' was not found.");
        var end = source.IndexOf(endToken, start + startToken.Length, StringComparison.Ordinal);
        Assert.True(end >= 0, $"End token '{endToken}' was not found after '{startToken}'.");
        return source.Substring(start, end - start);
    }

    private static void AssertContains(string actual, string expectedSubstring)
        => Assert.Contains(expectedSubstring, actual, StringComparison.Ordinal);

    private static void AssertDoesNotContain(string actual, string unexpectedSubstring)
        => Assert.DoesNotContain(unexpectedSubstring, actual, StringComparison.Ordinal);
}
