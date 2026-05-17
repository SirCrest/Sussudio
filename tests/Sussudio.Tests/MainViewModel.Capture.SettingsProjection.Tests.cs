using System.Collections;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelCaptureSettings_OwnsSettingsProjection()
    {
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs")
            .Replace("\r\n", "\n");
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingTransitionControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs")
            .Replace("\r\n", "\n");
        var captureSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSettings.cs")
            .Replace("\r\n", "\n");
        var captureSettingsBuilderText = ReadRepoFile("Sussudio/ViewModels/CaptureSettingsProjectionBuilder.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureSettingsText, "private CaptureSettings BuildCaptureSettings()");
        AssertContains(captureSettingsText, "var runtime = _captureService.GetRuntimeSnapshot();");
        AssertContains(captureSettingsText, "var sourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();");
        AssertContains(captureSettingsText, "return CaptureSettingsProjectionBuilder.Build(new CaptureSettingsProjectionInput");
        AssertContains(captureSettingsText, "AvailableFrameRates = AvailableFrameRates.ToArray(),");
        AssertContains(captureSettingsText, "SelectedAudioInputDeviceId = SelectedAudioInputDevice?.Id,");
        AssertContains(captureSettingsText, "SelectedMicrophoneDeviceId = SelectedMicrophoneDevice?.Id,");
        AssertContains(captureSettingsBuilderText, "internal static class CaptureSettingsProjectionBuilder");
        AssertContains(captureSettingsBuilderText, "internal sealed class CaptureSettingsProjectionInput");
        AssertContains(captureSettingsBuilderText, "public static CaptureSettings Build(CaptureSettingsProjectionInput input)");
        AssertContains(captureSettingsBuilderText, "private static CaptureSettingsFrameRateProjection ProjectFrameRate(CaptureSettingsProjectionInput input)");
        AssertContains(captureSettingsBuilderText, "FrameRate = frameRateProjection.EffectiveFrameRate,");
        AssertContains(captureSettingsBuilderText, "RequestedFrameRateArg = frameRateProjection.RequestedFrameRateArg,");
        AssertContains(captureSettingsBuilderText, "RequestedFrameRateNumerator = frameRateProjection.RequestedFrameRateNumerator,");
        AssertContains(captureSettingsBuilderText, "RequestedFrameRateDenominator = frameRateProjection.RequestedFrameRateDenominator,");
        AssertContains(captureSettingsBuilderText, "RequestedPixelFormat = ResolveRequestedPixelFormat(input)");
        AssertContains(captureSettingsBuilderText, "ForceMjpegDecode = ShouldForceMjpegDecode(input)");
        AssertContains(captureSettingsBuilderText, "settings.UseCustomAudioInput = input.IsCustomAudioInputEnabled;");
        AssertContains(captureSettingsBuilderText, "settings.MicrophoneEnabled = input.IsMicrophoneEnabled;");
        AssertContains(captureSettingsBuilderText, "var selectedFrameRateOption = input.AvailableFrameRates");
        AssertContains(captureSettingsBuilderText, "var effectiveFrameRate = input.IsAutoResolutionSelected && input.AutoResolvedFrameRate.HasValue && input.AutoResolvedFrameRate.Value > 0");
        AssertContains(captureSettingsBuilderText, "runtimeMatchesResolution");
        AssertContains(captureSettingsBuilderText, "input.Runtime.NegotiatedFrameRateNumerator");
        AssertContains(captureSettingsBuilderText, "input.SourceTelemetry.HasFrameRate");
        AssertContains(captureSettingsBuilderText, "TryParseFrameRateRational(requestedFrameRateArg");
        AssertContains(captureSettingsBuilderText, "input.SelectedFormat?.FrameRateNumerator > 0 && input.SelectedFormat.FrameRateDenominator > 0");
        AssertContains(captureSettingsBuilderText, "requestedFrameRateArg = effectiveFrameRate.ToString(\"0.###\");");
        AssertDoesNotContain(captureSettingsText, "ProjectCaptureSettingsFrameRate");
        AssertDoesNotContain(captureSettingsText, "private string? ResolveRequestedPixelFormat()");
        AssertDoesNotContain(captureSettingsText, "private bool ShouldForceMjpegDecode()");
        AssertDoesNotContain(captureText, "private CaptureSettings BuildCaptureSettings()");
        AssertContains(previewLifecycleControllerText, "await _viewModel._sessionCoordinator.StartVideoPreviewAsync(settings, cancellationToken)");
        AssertContains(recordingTransitionControllerText, "await _viewModel._sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertDoesNotContain(recordingLifecycleText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertDoesNotContain(captureText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");

        return Task.CompletedTask;
    }

    private static Task MainViewModelCaptureSettingsFrameRate_PreservesProjectionPrecedence()
    {
        var settings = InvokeCaptureSettingsProjection(
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 1920, height: 1080, frameRate: 60, numerator: 60, denominator: 1),
            runtime: CreateRuntimeSnapshot(
                actualWidth: 1920,
                actualHeight: 1080,
                actualFrameRate: 60000d / 1001d,
                actualFrameRateArg: "60000/1001",
                negotiatedNumerator: 60000,
                negotiatedDenominator: 1001),
            sourceTelemetry: CreateSourceTelemetry(frameRateExact: 60, frameRateArg: "60/1"),
            frameRateOptions: new[] { CreateFrameRateOption(
                RequireType("Sussudio.Models.FrameRateOption"),
                60,
                60000d / 1001d,
                "60000/1001",
                isEnabled: true) });

        AssertNearlyEqual(60, GetDoubleProperty(settings, "FrameRate"), 0.001, "source-over-runtime effective frame rate");
        AssertEqual("60/1", GetStringProperty(settings, "RequestedFrameRateArg"), "source telemetry frame-rate arg wins after runtime");
        AssertEqual(60, Convert.ToInt32(GetPropertyValue(settings, "RequestedFrameRateNumerator")), "source telemetry numerator wins after runtime");
        AssertEqual(1, Convert.ToInt32(GetPropertyValue(settings, "RequestedFrameRateDenominator")), "source telemetry denominator wins after runtime");

        settings = InvokeCaptureSettingsProjection(
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 1920, height: 1080, frameRate: 59.94, numerator: 60000, denominator: 1001),
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry(),
            frameRateOptions: new[] { CreateFrameRateOption(
                RequireType("Sussudio.Models.FrameRateOption"),
                60,
                60,
                string.Empty,
                isEnabled: true) });

        AssertNearlyEqual(60, GetDoubleProperty(settings, "FrameRate"), 0.001, "selected frame-rate effective value");
        AssertEqual("60000/1001", GetStringProperty(settings, "RequestedFrameRateArg"), "selected format rational fallback");
        AssertEqual(60000, Convert.ToInt32(GetPropertyValue(settings, "RequestedFrameRateNumerator")), "selected format fallback numerator");
        AssertEqual(1001, Convert.ToInt32(GetPropertyValue(settings, "RequestedFrameRateDenominator")), "selected format fallback denominator");

        settings = InvokeCaptureSettingsProjection(
            selectedResolution: "Source",
            selectedFrameRate: 0,
            autoResolvedFrameRate: 119.88,
            selectedFormat: null,
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry());

        AssertNearlyEqual(119.88, GetDoubleProperty(settings, "FrameRate"), 0.001, "auto-resolved effective frame rate");
        AssertEqual("119.88", GetStringProperty(settings, "RequestedFrameRateArg"), "decimal frame-rate fallback");
        AssertEqual(null, GetPropertyValue(settings, "RequestedFrameRateNumerator"), "decimal fallback numerator remains unset");
        AssertEqual(null, GetPropertyValue(settings, "RequestedFrameRateDenominator"), "decimal fallback denominator remains unset");

        settings = InvokeCaptureSettingsProjection(
            selectedResolution: "3840x2160",
            selectedFrameRate: 120,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 3840, height: 2160, frameRate: 120, numerator: 120, denominator: 1, pixelFormat: "NV12"),
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry(),
            selectedVideoFormat: "Auto",
            isHdrEnabled: false,
            mjpegDecoderCount: 99);

        AssertEqual("MJPG", GetStringProperty(settings, "RequestedPixelFormat"), "auto SDR 4K HFR requests MJPG");
        AssertEqual(true, GetBoolProperty(settings, "ForceMjpegDecode"), "auto SDR 4K HFR forces MJPEG decode");
        AssertEqual(8, Convert.ToInt32(GetPropertyValue(settings, "MjpegDecoderCount")), "decoder count clamps high");

        settings = InvokeCaptureSettingsProjection(
            selectedResolution: "3840x2160",
            selectedFrameRate: 120,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 3840, height: 2160, frameRate: 120, numerator: 120, denominator: 1, pixelFormat: "P010"),
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry(),
            selectedVideoFormat: "Auto",
            isHdrEnabled: true,
            isTrueHdrPreviewEnabled: true,
            mjpegDecoderCount: 0);

        AssertEqual("P010", GetStringProperty(settings, "RequestedPixelFormat"), "HDR auto keeps selected format pixel format");
        AssertEqual(false, GetBoolProperty(settings, "ForceMjpegDecode"), "HDR auto does not force MJPEG decode");
        AssertEqual("Hdr10Pq", GetPropertyValue(settings, "HdrOutputMode")?.ToString(), "HDR output mode");
        AssertEqual("TrueHdr", GetPropertyValue(settings, "PreviewMode")?.ToString(), "true HDR preview mode");
        AssertEqual(1, Convert.ToInt32(GetPropertyValue(settings, "MjpegDecoderCount")), "decoder count clamps low");

        settings = InvokeCaptureSettingsProjection(
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 1920, height: 1080, frameRate: 60, numerator: 60, denominator: 1, pixelFormat: "NV12"),
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry(),
            selectedVideoFormat: "MJPG",
            isHdrEnabled: false,
            isCustomAudioInputEnabled: true,
            selectedAudioInputDeviceId: "audio-1",
            selectedAudioInputDeviceName: "Capture Audio",
            isMicrophoneEnabled: true,
            selectedMicrophoneDeviceId: "mic-1",
            selectedMicrophoneDeviceName: "Mic");

        AssertEqual("MJPG", GetStringProperty(settings, "RequestedPixelFormat"), "explicit MJPG requests MJPG");
        AssertEqual(true, GetBoolProperty(settings, "ForceMjpegDecode"), "explicit MJPG forces MJPEG decode");
        AssertEqual(true, GetBoolProperty(settings, "UseCustomAudioInput"), "custom audio flag copied");
        AssertEqual("audio-1", GetStringProperty(settings, "AudioDeviceId"), "custom audio id copied");
        AssertEqual("Capture Audio", GetStringProperty(settings, "AudioDeviceName"), "custom audio name copied");
        AssertEqual(true, GetBoolProperty(settings, "MicrophoneEnabled"), "microphone flag copied");
        AssertEqual("mic-1", GetStringProperty(settings, "MicrophoneDeviceId"), "microphone id copied");
        AssertEqual("Mic", GetStringProperty(settings, "MicrophoneDeviceName"), "microphone name copied");

        return Task.CompletedTask;
    }

    private static object InvokeCaptureSettingsProjection(
        string selectedResolution,
        double selectedFrameRate,
        double? autoResolvedFrameRate,
        object? selectedFormat,
        object runtime,
        object sourceTelemetry,
        string? selectedVideoFormat = "Auto",
        bool isHdrEnabled = false,
        bool isTrueHdrPreviewEnabled = false,
        int mjpegDecoderCount = 6,
        bool isCustomAudioInputEnabled = false,
        string? selectedAudioInputDeviceId = null,
        string? selectedAudioInputDeviceName = null,
        bool isMicrophoneEnabled = false,
        string? selectedMicrophoneDeviceId = null,
        string? selectedMicrophoneDeviceName = null,
        params object[] frameRateOptions)
    {
        var inputType = RequireType("Sussudio.ViewModels.CaptureSettingsProjectionInput");
        var input = CreateConfigInstance(inputType);
        var frameRateType = RequireType("Sussudio.Models.FrameRateOption");
        var availableFrameRates = Array.CreateInstance(frameRateType, frameRateOptions.Length);
        for (var i = 0; i < frameRateOptions.Length; i++)
        {
            availableFrameRates.SetValue(frameRateOptions[i], i);
        }

        SetPropertyOrBackingField(input, "EffectiveResolutionKnown", true);
        SetPropertyOrBackingField(input, "EffectiveWidth", 1920u);
        SetPropertyOrBackingField(input, "EffectiveHeight", 1080u);
        SetPropertyOrBackingField(input, "SelectedResolution", selectedResolution);
        SetPropertyOrBackingField(input, "SelectedFrameRate", selectedFrameRate);
        SetPropertyOrBackingField(input, "AutoResolvedFrameRate", autoResolvedFrameRate);
        SetPropertyOrBackingField(input, "IsAutoResolutionSelected", string.Equals(selectedResolution, "Source", StringComparison.OrdinalIgnoreCase));
        SetPropertyOrBackingField(input, "SelectedFormat", selectedFormat);
        SetPropertyOrBackingField(input, "AvailableFrameRates", availableFrameRates);
        SetPropertyOrBackingField(input, "Runtime", runtime);
        SetPropertyOrBackingField(input, "SourceTelemetry", sourceTelemetry);
        SetPropertyOrBackingField(input, "SelectedVideoFormat", selectedVideoFormat);
        SetPropertyOrBackingField(input, "IsHdrEnabled", isHdrEnabled);
        SetPropertyOrBackingField(input, "IsTrueHdrPreviewEnabled", isTrueHdrPreviewEnabled);
        SetPropertyOrBackingField(input, "MjpegDecoderCount", mjpegDecoderCount);
        SetPropertyOrBackingField(input, "SelectedRecordingFormat", "HEVC");
        SetPropertyOrBackingField(input, "SelectedQuality", "High");
        SetPropertyOrBackingField(input, "SelectedPreset", "P5");
        SetPropertyOrBackingField(input, "SelectedSplitEncodeMode", "Auto");
        SetPropertyOrBackingField(input, "CustomBitrateMbps", 42d);
        SetPropertyOrBackingField(input, "OutputPath", "C:\\Capture");
        SetPropertyOrBackingField(input, "FlashbackGpuDecode", true);
        SetPropertyOrBackingField(input, "FlashbackBufferMinutes", 5);
        SetPropertyOrBackingField(input, "IsAudioEnabled", true);
        SetPropertyOrBackingField(input, "IsCustomAudioInputEnabled", isCustomAudioInputEnabled);
        SetPropertyOrBackingField(input, "SelectedAudioInputDeviceId", selectedAudioInputDeviceId);
        SetPropertyOrBackingField(input, "SelectedAudioInputDeviceName", selectedAudioInputDeviceName);
        SetPropertyOrBackingField(input, "IsMicrophoneEnabled", isMicrophoneEnabled);
        SetPropertyOrBackingField(input, "SelectedMicrophoneDeviceId", selectedMicrophoneDeviceId);
        SetPropertyOrBackingField(input, "SelectedMicrophoneDeviceName", selectedMicrophoneDeviceName);

        var builderType = RequireType("Sussudio.ViewModels.CaptureSettingsProjectionBuilder");
        var build = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureSettingsProjectionBuilder.Build was not found.");
        return build.Invoke(null, new[] { input })
               ?? throw new InvalidOperationException("CaptureSettingsProjectionBuilder.Build returned null.");
    }

    private static object CreateRuntimeSnapshot(
        uint? actualWidth = null,
        uint? actualHeight = null,
        double? actualFrameRate = null,
        string? actualFrameRateArg = null,
        uint? negotiatedNumerator = null,
        uint? negotiatedDenominator = null)
    {
        var snapshot = CreateConfigInstance(RequireType("Sussudio.Models.CaptureRuntimeSnapshot"));
        SetPropertyOrBackingField(snapshot, "ActualWidth", actualWidth);
        SetPropertyOrBackingField(snapshot, "ActualHeight", actualHeight);
        SetPropertyOrBackingField(snapshot, "ActualFrameRate", actualFrameRate);
        SetPropertyOrBackingField(snapshot, "ActualFrameRateArg", actualFrameRateArg);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateNumerator", negotiatedNumerator);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateDenominator", negotiatedDenominator);
        return snapshot;
    }

    private static object CreateSourceTelemetry(double? frameRateExact = null, string? frameRateArg = null)
    {
        var snapshot = CreateConfigInstance(RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot"));
        SetPropertyOrBackingField(snapshot, "FrameRateExact", frameRateExact);
        SetPropertyOrBackingField(snapshot, "FrameRateArg", frameRateArg);
        return snapshot;
    }

    private static object CreateMediaFormat(
        uint width,
        uint height,
        double frameRate,
        uint numerator,
        uint denominator,
        string pixelFormat = "NV12")
    {
        var format = CreateConfigInstance(RequireType("Sussudio.Models.MediaFormat"));
        SetPropertyOrBackingField(format, "Width", width);
        SetPropertyOrBackingField(format, "Height", height);
        SetPropertyOrBackingField(format, "FrameRate", frameRate);
        SetPropertyOrBackingField(format, "FrameRateNumerator", numerator);
        SetPropertyOrBackingField(format, "FrameRateDenominator", denominator);
        SetPropertyOrBackingField(format, "PixelFormat", pixelFormat);
        return format;
    }
}
