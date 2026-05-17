using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureModeOptions_PreserveDisplayTextAndMetadata()
    {
        var resolutionType = RequireType("Sussudio.Models.ResolutionOption");
        var frameRateType = RequireType("Sussudio.Models.FrameRateOption");

        AssertDeclaredConfigProperties(
            resolutionType,
            new ConfigPropertySpec[]
            {
                RequiredConfigString("Value", ConfigSetterExpectation.InitOnly),
                RequiredConfigProperty("Width", typeof(uint), ConfigSetterExpectation.InitOnly),
                RequiredConfigProperty("Height", typeof(uint), ConfigSetterExpectation.InitOnly),
                ConfigProperty("IsEnabled", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigString("DisableReason", ConfigSetterExpectation.InitOnly, ConfigNullability.NotNull),
                ConfigString("DisplayTextOverride", ConfigSetterExpectation.InitOnly, ConfigNullability.Nullable),
                ConfigString("DisplayText", ConfigSetterExpectation.None, ConfigNullability.NotNull)
            });
        AssertDeclaredConfigProperties(
            frameRateType,
            new ConfigPropertySpec[]
            {
                RequiredConfigProperty("FriendlyValue", typeof(double), ConfigSetterExpectation.InitOnly),
                RequiredConfigProperty("Value", typeof(double), ConfigSetterExpectation.InitOnly),
                ConfigString("Rational", ConfigSetterExpectation.InitOnly, ConfigNullability.NotNull),
                ConfigProperty("Numerator", typeof(uint?), ConfigSetterExpectation.InitOnly),
                ConfigProperty("Denominator", typeof(uint?), ConfigSetterExpectation.InitOnly),
                ConfigProperty("IsEnabled", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigString("DisableReason", ConfigSetterExpectation.InitOnly, ConfigNullability.NotNull),
                ConfigString("DisplayTextOverride", ConfigSetterExpectation.InitOnly, ConfigNullability.Nullable),
                ConfigString("DisplayText", ConfigSetterExpectation.None, ConfigNullability.NotNull)
            });

        var resolution = CreateConfigInstance(resolutionType);
        SetPropertyOrBackingField(resolution, "Value", "3840x2160");
        SetPropertyOrBackingField(resolution, "Width", 3840u);
        SetPropertyOrBackingField(resolution, "Height", 2160u);
        AssertEqual("3840x2160", GetStringProperty(resolution, "DisplayText"), "ResolutionOption.DisplayText default");
        AssertEqual(string.Empty, GetStringProperty(resolution, "DisableReason"), "ResolutionOption.DisableReason default");

        SetPropertyOrBackingField(resolution, "DisplayTextOverride", "4K UHD");
        AssertEqual("4K UHD", GetStringProperty(resolution, "DisplayText"), "ResolutionOption.DisplayText override");
        SetPropertyOrBackingField(resolution, "DisplayTextOverride", "   ");
        AssertEqual("3840x2160", GetStringProperty(resolution, "DisplayText"), "ResolutionOption.DisplayText whitespace override fallback");

        var frameRate = CreateConfigInstance(frameRateType);
        SetPropertyOrBackingField(frameRate, "FriendlyValue", 59.94d);
        SetPropertyOrBackingField(frameRate, "Value", 60000d / 1001d);
        SetPropertyOrBackingField(frameRate, "Rational", "60000/1001");
        SetPropertyOrBackingField(frameRate, "Numerator", 60000u);
        SetPropertyOrBackingField(frameRate, "Denominator", 1001u);
        AssertEqual("60", GetStringProperty(frameRate, "DisplayText"), "FrameRateOption.DisplayText rounded default");
        SetPropertyOrBackingField(frameRate, "DisplayTextOverride", "59.94");
        AssertEqual("59.94", GetStringProperty(frameRate, "DisplayText"), "FrameRateOption.DisplayText override");
        AssertEqual("60000/1001", GetStringProperty(frameRate, "Rational"), "FrameRateOption.Rational round-trip");

        return Task.CompletedTask;
    }

    private static Task CaptureModeOptionsBuilder_BuildsResolutionAndVideoFormatOptions()
    {
        var builderType = RequireType("Sussudio.ViewModels.CaptureModeOptionsBuilder");
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");
        var telemetryType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var buildResolutionOptions = builderType.GetMethod("BuildResolutionOptions", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureModeOptionsBuilder.BuildResolutionOptions missing.");
        var buildVideoFormatOptions = builderType.GetMethod("BuildVideoFormatOptions", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureModeOptionsBuilder.BuildVideoFormatOptions missing.");

        var formatsByResolution = CreateResolutionFormatDictionary(mediaFormatType);
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "3840x2160",
            CreateTestMediaFormat(mediaFormatType, 3840, 2160, 60, "P010", isHdr: true));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1920x1080",
            CreateTestMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1280x1024",
            CreateTestMediaFormat(mediaFormatType, 1280, 1024, 60, "P010", isHdr: true));

        var telemetry = CreateConfigInstance(telemetryType);
        SetPropertyOrBackingField(telemetry, "Width", 1920);
        SetPropertyOrBackingField(telemetry, "Height", 1080);

        var filteredOptions = ((IEnumerable)buildResolutionOptions.Invoke(
                null,
                new[] { formatsByResolution, true, false, telemetry })!)
            .Cast<object>()
            .ToArray();
        AssertEqual(2, filteredOptions.Length, "Source aspect-ratio filter keeps only 16:9 resolutions");
        AssertEqual("3840x2160", GetStringProperty(filteredOptions[0], "Value"), "Resolution options sort by area");
        AssertEqual(true, GetBoolProperty(filteredOptions[0], "IsEnabled"), "HDR-capable resolution remains enabled");
        var sdrOnlyResolution = filteredOptions.Single(option => GetStringProperty(option, "Value") == "1920x1080");
        AssertEqual(false, GetBoolProperty(sdrOnlyResolution, "IsEnabled"), "SDR-only resolution disables in HDR mode");
        AssertEqual(
            "HDR mode is not supported at this resolution.",
            GetStringProperty(sdrOnlyResolution, "DisableReason"),
            "SDR-only HDR disable reason");

        var unfilteredOptions = ((IEnumerable)buildResolutionOptions.Invoke(
                null,
                new[] { formatsByResolution, true, true, telemetry })!)
            .Cast<object>()
            .ToArray();
        AssertEqual(true, unfilteredOptions.Any(option => GetStringProperty(option, "Value") == "1280x1024"), "Show-all keeps source aspect-ratio mismatches");

        var videoFormats = CreateMediaFormatList(
            mediaFormatType,
            CreateTestMediaFormat(mediaFormatType, 3840, 2160, 120, "mjpg", isHdr: false),
            CreateTestMediaFormat(mediaFormatType, 3840, 2160, 120, "NV12", isHdr: false),
            CreateTestMediaFormat(mediaFormatType, 3840, 2160, 120, "nv12", isHdr: false),
            CreateTestMediaFormat(mediaFormatType, 3840, 2160, 60, "P010", isHdr: true),
            CreateTestMediaFormat(mediaFormatType, 3840, 2160, 60, " ", isHdr: false));
        var videoOptions = ((IEnumerable)buildVideoFormatOptions.Invoke(null, new[] { videoFormats })!)
            .Cast<string>()
            .ToArray();
        AssertSequenceEqual(new[] { "Auto", "NV12", "MJPG", "P010" }, videoOptions, "Video format options normalize, dedupe, and sort by pixel-format priority");

        return Task.CompletedTask;
    }

    private static Task RecordingSettingsSelectionPolicy_PreservesHdrAndSdrChoices()
    {
        AssertRecordingFormatSelection(
            "HDR filters H.264 and falls back to HEVC",
            detectedFormats: new[] { "H.264", "HEVC", "AV1" },
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: "H.264",
            isHdrEnabled: true,
            expectedFormats: new[] { "HEVC", "AV1" },
            expectedSelectedFormat: "HEVC");

        AssertRecordingFormatSelection(
            "HDR preserves existing AV1 selection",
            detectedFormats: new[] { "H.264", "HEVC", "AV1" },
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: "AV1",
            isHdrEnabled: true,
            expectedFormats: new[] { "HEVC", "AV1" },
            expectedSelectedFormat: "AV1");

        AssertRecordingFormatSelection(
            "HDR falls back to AV1 when HEVC is unavailable",
            detectedFormats: new[] { "H.264", "AV1" },
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: "H.264",
            isHdrEnabled: true,
            expectedFormats: new[] { "AV1" },
            expectedSelectedFormat: "AV1");

        AssertRecordingFormatSelection(
            "HDR preserves last known real formats when refresh has no HDR formats",
            detectedFormats: new[] { "H.264" },
            currentAvailableFormats: new[] { "HEVC", "AV1" },
            selectedFormat: "H.264",
            isHdrEnabled: true,
            expectedFormats: new[] { "HEVC", "AV1" },
            expectedSelectedFormat: "HEVC");

        AssertRecordingFormatSelection(
            "SDR preserves valid current format",
            detectedFormats: new[] { "H.264", "HEVC" },
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: "HEVC",
            isHdrEnabled: false,
            expectedFormats: new[] { "H.264", "HEVC" },
            expectedSelectedFormat: "HEVC");

        AssertRecordingFormatSelection(
            "SDR prefers H.264 when current format is unavailable",
            detectedFormats: new[] { "HEVC", "H264" },
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: "VP9",
            isHdrEnabled: false,
            expectedFormats: new[] { "HEVC", "H264" },
            expectedSelectedFormat: "H264");

        AssertRecordingFormatSelection(
            "Empty SDR capabilities fall back to default format",
            detectedFormats: Array.Empty<string>(),
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: null,
            isHdrEnabled: false,
            expectedFormats: new[] { "H.264" },
            expectedSelectedFormat: "H.264");

        return Task.CompletedTask;
    }

    private static Task RecordingSettingsSelectionPolicy_ParsesModelValues()
    {
        var policyType = RequireType("Sussudio.ViewModels.RecordingSettingsSelectionPolicy");
        var parseRecordingFormat = policyType.GetMethod("ParseRecordingFormat", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RecordingSettingsSelectionPolicy.ParseRecordingFormat missing.");
        var parseVideoQuality = policyType.GetMethod("ParseVideoQuality", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RecordingSettingsSelectionPolicy.ParseVideoQuality missing.");
        var clampCustomBitrate = policyType.GetMethod("ClampCustomBitrateMbps", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RecordingSettingsSelectionPolicy.ClampCustomBitrateMbps missing.");

        AssertEqual(
            ParseEnum("Sussudio.Models.RecordingFormat", "H264Mp4"),
            parseRecordingFormat.Invoke(null, new object?[] { "H.264" }),
            "RecordingSettingsSelectionPolicy maps H.264 to H264Mp4");
        AssertEqual(
            ParseEnum("Sussudio.Models.RecordingFormat", "HevcMp4"),
            parseRecordingFormat.Invoke(null, new object?[] { "HEVC" }),
            "RecordingSettingsSelectionPolicy maps HEVC to HevcMp4");
        AssertEqual(
            ParseEnum("Sussudio.Models.RecordingFormat", "Av1Mp4"),
            parseRecordingFormat.Invoke(null, new object?[] { "AV1" }),
            "RecordingSettingsSelectionPolicy maps AV1 to Av1Mp4");

        AssertEqual(
            ParseEnum("Sussudio.Models.VideoQuality", "Auto"),
            parseVideoQuality.Invoke(null, new object?[] { "Auto" }),
            "RecordingSettingsSelectionPolicy maps Auto quality");
        AssertEqual(
            ParseEnum("Sussudio.Models.VideoQuality", "SuperHigh"),
            parseVideoQuality.Invoke(null, new object?[] { "Super High" }),
            "RecordingSettingsSelectionPolicy maps Super High quality");
        AssertEqual(
            ParseEnum("Sussudio.Models.VideoQuality", "High"),
            parseVideoQuality.Invoke(null, new object?[] { "unexpected" }),
            "RecordingSettingsSelectionPolicy keeps unknown quality fallback");

        AssertEqual(1d, (double)clampCustomBitrate.Invoke(null, new object?[] { -5d })!, "RecordingSettingsSelectionPolicy clamps low bitrate");
        AssertEqual(42d, (double)clampCustomBitrate.Invoke(null, new object?[] { 42d })!, "RecordingSettingsSelectionPolicy keeps in-range bitrate");
        AssertEqual(300d, (double)clampCustomBitrate.Invoke(null, new object?[] { 999d })!, "RecordingSettingsSelectionPolicy clamps high bitrate");

        return Task.CompletedTask;
    }

    private static void AssertRecordingFormatSelection(
        string fieldName,
        string[] detectedFormats,
        string[] currentAvailableFormats,
        string? selectedFormat,
        bool isHdrEnabled,
        string[] expectedFormats,
        string expectedSelectedFormat)
    {
        var policyType = RequireType("Sussudio.ViewModels.RecordingSettingsSelectionPolicy");
        var select = policyType.GetMethod("Select", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RecordingSettingsSelectionPolicy.Select missing.");
        var selection = select.Invoke(
                null,
                new object?[]
                {
                    detectedFormats,
                    currentAvailableFormats,
                    selectedFormat,
                    isHdrEnabled,
                    "H.264",
                    "HEVC",
                    "AV1"
                })
            ?? throw new InvalidOperationException("RecordingSettingsSelectionPolicy.Select returned null.");
        var availableFormats = ((IEnumerable)selection.GetType().GetProperty("AvailableFormats")!.GetValue(selection)!)
            .Cast<string>()
            .ToArray();
        var selected = (string)selection.GetType().GetProperty("SelectedFormat")!.GetValue(selection)!;

        AssertSequenceEqual(expectedFormats, availableFormats, $"{fieldName} formats");
        AssertEqual(expectedSelectedFormat, selected, $"{fieldName} selected format");
    }
}
