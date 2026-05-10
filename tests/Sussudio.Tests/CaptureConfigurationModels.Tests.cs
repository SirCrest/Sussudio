using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// Tests for capture settings, option models, and source-driven mode selection.
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

    private static Task CaptureSettings_DefaultsAndOutputContracts()
    {
        var settingsType = RequireType("Sussudio.Models.CaptureSettings");
        var recordingFormatType = RequireType("Sussudio.Models.RecordingFormat");
        var videoQualityType = RequireType("Sussudio.Models.VideoQuality");
        var hdrOutputModeType = RequireType("Sussudio.Models.HdrOutputMode");
        var previewModeType = RequireType("Sussudio.Models.PreviewMode");
        var audioPathModeType = RequireType("Sussudio.Models.AudioPathMode");
        var pipelineOptionsType = RequireType("Sussudio.Models.RecordingPipelineOptions");
        var splitEncodeSupportType = RequireType("Sussudio.Models.SplitEncodeSupport");
        var nvencPresetType = RequireType("Sussudio.Models.NvencPreset");
        var splitEncodeModeType = RequireType("Sussudio.Models.SplitEncodeMode");

        AssertEnumValues(recordingFormatType, ("H264Mp4", 0), ("HevcMp4", 1), ("Av1Mp4", 2));
        AssertEnumValues(videoQualityType, ("Auto", 0), ("Low", 1), ("Medium", 2), ("High", 3), ("SuperHigh", 4), ("Custom", 5));
        AssertEnumValues(hdrOutputModeType, ("Off", 0), ("Hdr10Pq", 1));
        AssertEnumValues(previewModeType, ("GpuFast", 0), ("TrueHdr", 1));
        AssertEnumValues(nvencPresetType, ("Auto", 0), ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4), ("P5", 5), ("P6", 6), ("P7", 7), ("Fast", 8), ("Slow", 9));
        AssertEnumValues(splitEncodeModeType, ("Auto", 0), ("Disabled", 1), ("TwoWay", 2), ("ThreeWay", 3), ("ForcedOn", 4));

        AssertDeclaredConfigProperties(
            settingsType,
            new ConfigPropertySpec[]
            {
                ConfigProperty("Width", typeof(uint), ConfigSetterExpectation.Set),
                ConfigProperty("Height", typeof(uint), ConfigSetterExpectation.Set),
                ConfigProperty("FrameRate", typeof(double), ConfigSetterExpectation.Set),
                ConfigString("RequestedFrameRateArg", ConfigSetterExpectation.Set, ConfigNullability.Nullable),
                ConfigProperty("RequestedFrameRateNumerator", typeof(uint?), ConfigSetterExpectation.Set),
                ConfigProperty("RequestedFrameRateDenominator", typeof(uint?), ConfigSetterExpectation.Set),
                ConfigString("RequestedPixelFormat", ConfigSetterExpectation.Set, ConfigNullability.Nullable),
                ConfigProperty("Format", recordingFormatType, ConfigSetterExpectation.Set),
                ConfigProperty("Quality", videoQualityType, ConfigSetterExpectation.Set),
                ConfigProperty("NvencPreset", nvencPresetType, ConfigSetterExpectation.Set),
                ConfigProperty("SplitEncodeMode", splitEncodeModeType, ConfigSetterExpectation.Set),
                ConfigProperty("CustomBitrateMbps", typeof(double), ConfigSetterExpectation.Set),
                ConfigProperty("HdrEnabled", typeof(bool), ConfigSetterExpectation.Set),
                ConfigProperty("HdrOutputMode", hdrOutputModeType, ConfigSetterExpectation.Set),
                ConfigProperty("HdrNominalPeakNits", typeof(int), ConfigSetterExpectation.Set),
                ConfigProperty("HdrMaxCll", typeof(int), ConfigSetterExpectation.Set),
                ConfigProperty("HdrMaxFall", typeof(int), ConfigSetterExpectation.Set),
                ConfigString("HdrMasterDisplayMetadata", ConfigSetterExpectation.Set, ConfigNullability.NotNull),
                ConfigProperty("PreviewMode", previewModeType, ConfigSetterExpectation.Set),
                ConfigString("OutputPath", ConfigSetterExpectation.Set, ConfigNullability.NotNull),
                ConfigProperty("AudioEnabled", typeof(bool), ConfigSetterExpectation.Set),
                ConfigProperty("UseCustomAudioInput", typeof(bool), ConfigSetterExpectation.Set),
                ConfigString("AudioDeviceId", ConfigSetterExpectation.Set, ConfigNullability.Nullable),
                ConfigString("AudioDeviceName", ConfigSetterExpectation.Set, ConfigNullability.Nullable),
                ConfigProperty("MicrophoneEnabled", typeof(bool), ConfigSetterExpectation.Set),
                ConfigString("MicrophoneDeviceId", ConfigSetterExpectation.Set, ConfigNullability.Nullable),
                ConfigString("MicrophoneDeviceName", ConfigSetterExpectation.Set, ConfigNullability.Nullable),
                ConfigProperty("AudioPathMode", audioPathModeType, ConfigSetterExpectation.Set),
                ConfigProperty("PipelineOptions", pipelineOptionsType, ConfigSetterExpectation.Set),
                ConfigProperty("ForceMjpegDecode", typeof(bool), ConfigSetterExpectation.Set),
                ConfigProperty("FlashbackGpuDecode", typeof(bool), ConfigSetterExpectation.Set),
                ConfigProperty("FlashbackBufferMinutes", typeof(int), ConfigSetterExpectation.Set),
                ConfigProperty("MjpegDecoderCount", typeof(int), ConfigSetterExpectation.Set),
                ConfigProperty("UseMjpegHighFrameRateMode", typeof(bool), ConfigSetterExpectation.None)
            });
        AssertDeclaredConfigProperties(
            splitEncodeSupportType,
            new ConfigPropertySpec[]
            {
                ConfigProperty("Supports2Way", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("Supports3Way", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("NvencUnavailable", splitEncodeSupportType, ConfigSetterExpectation.None, scope: ConfigPropertyScope.Static)
            });

        var settings = CreateConfigInstance(settingsType);
        AssertEqual(1920u, (uint)GetPropertyValue(settings, "Width")!, "CaptureSettings.Width default");
        AssertEqual(1080u, (uint)GetPropertyValue(settings, "Height")!, "CaptureSettings.Height default");
        AssertEqual(60d, GetDoubleProperty(settings, "FrameRate"), "CaptureSettings.FrameRate default");
        AssertEqual(ParseEnum("Sussudio.Models.RecordingFormat", "H264Mp4"), GetPropertyValue(settings, "Format"), "CaptureSettings.Format default");
        AssertEqual(ParseEnum("Sussudio.Models.VideoQuality", "High"), GetPropertyValue(settings, "Quality"), "CaptureSettings.Quality default");
        AssertEqual("Auto", GetStringProperty(settings, "NvencPreset"), "CaptureSettings.NvencPreset default");
        AssertEqual("Auto", GetStringProperty(settings, "SplitEncodeMode"), "CaptureSettings.SplitEncodeMode default");
        AssertEqual(50d, GetDoubleProperty(settings, "CustomBitrateMbps"), "CaptureSettings.CustomBitrateMbps default");
        AssertEqual(false, GetBoolProperty(settings, "HdrEnabled"), "CaptureSettings.HdrEnabled default");
        AssertEqual(ParseEnum("Sussudio.Models.HdrOutputMode", "Hdr10Pq"), GetPropertyValue(settings, "HdrOutputMode"), "CaptureSettings.HdrOutputMode default");
        AssertEqual(1000, GetIntProperty(settings, "HdrNominalPeakNits"), "CaptureSettings.HdrNominalPeakNits default");
        AssertEqual(string.Empty, GetStringProperty(settings, "HdrMasterDisplayMetadata"), "CaptureSettings.HdrMasterDisplayMetadata default");
        AssertEqual(ParseEnum("Sussudio.Models.PreviewMode", "GpuFast"), GetPropertyValue(settings, "PreviewMode"), "CaptureSettings.PreviewMode default");
        AssertEqual(true, GetBoolProperty(settings, "AudioEnabled"), "CaptureSettings.AudioEnabled default");
        AssertEqual(false, GetBoolProperty(settings, "UseCustomAudioInput"), "CaptureSettings.UseCustomAudioInput default");
        AssertEqual(false, GetBoolProperty(settings, "MicrophoneEnabled"), "CaptureSettings.MicrophoneEnabled default");
        AssertEqual(ParseEnum("Sussudio.Models.AudioPathMode", "PostMuxDefault"), GetPropertyValue(settings, "AudioPathMode"), "CaptureSettings.AudioPathMode default");
        AssertNotNull(GetPropertyValue(settings, "PipelineOptions"), "CaptureSettings.PipelineOptions default");
        AssertEqual(false, GetBoolProperty(settings, "ForceMjpegDecode"), "CaptureSettings.ForceMjpegDecode default");
        AssertEqual(true, GetBoolProperty(settings, "FlashbackGpuDecode"), "CaptureSettings.FlashbackGpuDecode default");
        AssertEqual(5, GetIntProperty(settings, "FlashbackBufferMinutes"), "CaptureSettings.FlashbackBufferMinutes default");
        AssertEqual(6, GetIntProperty(settings, "MjpegDecoderCount"), "CaptureSettings.MjpegDecoderCount default");
        AssertEqual(false, GetBoolProperty(settings, "UseMjpegHighFrameRateMode"), "CaptureSettings.UseMjpegHighFrameRateMode default");

        var otherSettings = CreateConfigInstance(settingsType);
        if (ReferenceEquals(GetPropertyValue(settings, "PipelineOptions"), GetPropertyValue(otherSettings, "PipelineOptions")))
        {
            throw new InvalidOperationException("CaptureSettings.PipelineOptions default should be per-instance.");
        }

        var outputDir = Path.Combine(Path.GetTempPath(), $"capture_settings_{Guid.NewGuid():N}");
        SetPropertyOrBackingField(settings, "OutputPath", outputDir);
        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "HevcMp4"));
        var fullPath = InvokeInstanceMethod(settings, "GetFullOutputPath").ToString()!;
        AssertEqual(outputDir, Path.GetDirectoryName(fullPath), "CaptureSettings.GetFullOutputPath directory");
        AssertContains(Path.GetFileName(fullPath), "_HEVC.mp4");

        var splitSupport = Activator.CreateInstance(splitEncodeSupportType, true, false)
            ?? throw new InvalidOperationException("Failed to create SplitEncodeSupport.");
        AssertEqual(true, GetBoolProperty(splitSupport, "Supports2Way"), "SplitEncodeSupport.Supports2Way");
        AssertEqual(false, GetBoolProperty(splitSupport, "Supports3Way"), "SplitEncodeSupport.Supports3Way");
        var nvencUnavailable = splitEncodeSupportType.GetProperty("NvencUnavailable", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        AssertEqual(false, GetBoolProperty(nvencUnavailable, "Supports2Way"), "SplitEncodeSupport.NvencUnavailable.Supports2Way");
        AssertEqual(false, GetBoolProperty(nvencUnavailable, "Supports3Way"), "SplitEncodeSupport.NvencUnavailable.Supports3Way");

        return Task.CompletedTask;
    }

    private static Task CaptureSettings_MjpegHighFrameRateMode_HandlesForceCaseAndInstanceState()
    {
        var settingsType = RequireType("Sussudio.Models.CaptureSettings");
        var method = settingsType.GetMethod("IsMjpegHighFrameRateMode", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("CaptureSettings.IsMjpegHighFrameRateMode not found.");

        AssertEqual(true, (bool)method.Invoke(null, new object?[] { "mjpg", 3840u, 2160u, 100d, false, false })!, "MJPG comparison is case-insensitive");
        AssertEqual(true, (bool)method.Invoke(null, new object?[] { "MJPG", 1920u, 1080u, 60d, false, true })!, "Force enables SDR MJPG below 4K100");
        AssertEqual(false, (bool)method.Invoke(null, new object?[] { "NV12", 3840u, 2160u, 120d, false, true })!, "Force does not override pixel format");
        AssertEqual(false, (bool)method.Invoke(null, new object?[] { "MJPG", 3840u, 2160u, 120d, true, true })!, "Force does not override HDR guard");

        var settings = CreateConfigInstance(settingsType);
        SetPropertyOrBackingField(settings, "RequestedPixelFormat", "MJPG");
        SetPropertyOrBackingField(settings, "Width", 3840u);
        SetPropertyOrBackingField(settings, "Height", 2160u);
        SetPropertyOrBackingField(settings, "FrameRate", 120d);
        SetPropertyOrBackingField(settings, "HdrEnabled", false);
        AssertEqual(true, GetBoolProperty(settings, "UseMjpegHighFrameRateMode"), "CaptureSettings.UseMjpegHighFrameRateMode active");
        SetPropertyOrBackingField(settings, "HdrEnabled", true);
        AssertEqual(false, GetBoolProperty(settings, "UseMjpegHighFrameRateMode"), "CaptureSettings.UseMjpegHighFrameRateMode HDR guard");
        SetPropertyOrBackingField(settings, "Width", 1920u);
        SetPropertyOrBackingField(settings, "Height", 1080u);
        SetPropertyOrBackingField(settings, "FrameRate", 60d);
        SetPropertyOrBackingField(settings, "HdrEnabled", false);
        SetPropertyOrBackingField(settings, "ForceMjpegDecode", true);
        AssertEqual(true, GetBoolProperty(settings, "UseMjpegHighFrameRateMode"), "CaptureSettings.UseMjpegHighFrameRateMode force path");
        SetPropertyOrBackingField(settings, "HdrEnabled", true);
        AssertEqual(false, GetBoolProperty(settings, "UseMjpegHighFrameRateMode"), "CaptureSettings.UseMjpegHighFrameRateMode force HDR guard");

        return Task.CompletedTask;
    }

    private static Task EncoderSupport_ComputesAvailabilityAndPreferredEncoders()
    {
        var supportType = RequireType("Sussudio.Models.EncoderSupport");
        AssertDeclaredConfigProperties(
            supportType,
            new ConfigPropertySpec[]
            {
                ConfigProperty("HasH264Nvenc", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("HasHevcNvenc", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("HasAv1Nvenc", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("HasLibX264", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("HasLibX265", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("HasLibSvtAv1", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("HasLibAomAv1", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("HasH264", typeof(bool), ConfigSetterExpectation.None),
                ConfigProperty("HasHevc", typeof(bool), ConfigSetterExpectation.None),
                ConfigProperty("HasAv1", typeof(bool), ConfigSetterExpectation.None),
                ConfigString("PreferredAv1Encoder", ConfigSetterExpectation.None, ConfigNullability.Nullable),
                ConfigProperty("Empty", supportType, ConfigSetterExpectation.None, scope: ConfigPropertyScope.Static)
            });

        var empty = supportType.GetProperty("Empty", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        AssertEqual(false, GetBoolProperty(empty, "HasH264"), "EncoderSupport.Empty.HasH264");
        AssertEqual(false, GetBoolProperty(empty, "HasHevc"), "EncoderSupport.Empty.HasHevc");
        AssertEqual(false, GetBoolProperty(empty, "HasAv1"), "EncoderSupport.Empty.HasAv1");
        AssertEqual(null, GetPropertyValue(empty, "PreferredAv1Encoder"), "EncoderSupport.Empty.PreferredAv1Encoder");

        var nvencAv1 = CreateConfigInstance(supportType);
        SetPropertyOrBackingField(nvencAv1, "HasAv1Nvenc", true);
        SetPropertyOrBackingField(nvencAv1, "HasLibSvtAv1", true);
        AssertEqual(true, GetBoolProperty(nvencAv1, "HasAv1"), "EncoderSupport.HasAv1 with NVENC");
        AssertEqual("av1_nvenc", GetStringProperty(nvencAv1, "PreferredAv1Encoder"), "EncoderSupport.PreferredAv1Encoder NVENC priority");

        var svtAv1 = CreateConfigInstance(supportType);
        SetPropertyOrBackingField(svtAv1, "HasLibSvtAv1", true);
        SetPropertyOrBackingField(svtAv1, "HasLibAomAv1", true);
        AssertEqual("libsvtav1", GetStringProperty(svtAv1, "PreferredAv1Encoder"), "EncoderSupport.PreferredAv1Encoder SVT priority");

        var softwareFallbacks = CreateConfigInstance(supportType);
        SetPropertyOrBackingField(softwareFallbacks, "HasLibX264", true);
        SetPropertyOrBackingField(softwareFallbacks, "HasLibX265", true);
        SetPropertyOrBackingField(softwareFallbacks, "HasLibAomAv1", true);
        AssertEqual(true, GetBoolProperty(softwareFallbacks, "HasH264"), "EncoderSupport.HasH264 software fallback");
        AssertEqual(true, GetBoolProperty(softwareFallbacks, "HasHevc"), "EncoderSupport.HasHevc software fallback");
        AssertEqual("libaom-av1", GetStringProperty(softwareFallbacks, "PreferredAv1Encoder"), "EncoderSupport.PreferredAv1Encoder AOM fallback");

        return Task.CompletedTask;
    }

    private static Task FlashbackModels_PreserveBufferSessionExportContracts()
    {
        var bufferOptionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
        var sessionContextType = RequireType("Sussudio.Models.FlashbackSessionContext");
        var playbackStateType = RequireType("Sussudio.Models.FlashbackPlaybackState");
        var exportProgressType = RequireType("Sussudio.Models.ExportProgress");
        var exportSegmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var exportRequestType = RequireType("Sussudio.Models.FlashbackExportRequest");

        AssertEnumValues(playbackStateType, ("Disabled", 0), ("Buffering", 1), ("Live", 2), ("Scrubbing", 3), ("Playing", 4), ("Paused", 5));
        AssertDeclaredConfigProperties(
            bufferOptionsType,
            new ConfigPropertySpec[]
            {
                ConfigProperty("BufferDuration", typeof(TimeSpan), ConfigSetterExpectation.InitOnly),
                ConfigString("TempDirectory", ConfigSetterExpectation.InitOnly, ConfigNullability.NotNull),
                ConfigProperty("SegmentDuration", typeof(TimeSpan), ConfigSetterExpectation.InitOnly),
                ConfigProperty("MaxDiskBytes", typeof(long), ConfigSetterExpectation.None)
            });
        AssertDeclaredConfigProperties(
            sessionContextType,
            new ConfigPropertySpec[]
            {
                RequiredConfigProperty("Width", typeof(int), ConfigSetterExpectation.InitOnly),
                RequiredConfigProperty("Height", typeof(int), ConfigSetterExpectation.InitOnly),
                RequiredConfigProperty("FrameRate", typeof(double), ConfigSetterExpectation.InitOnly),
                ConfigProperty("FrameRateNumerator", typeof(int?), ConfigSetterExpectation.InitOnly),
                ConfigProperty("FrameRateDenominator", typeof(int?), ConfigSetterExpectation.InitOnly),
                RequiredConfigProperty("BitRate", typeof(uint), ConfigSetterExpectation.InitOnly),
                RequiredConfigProperty("IsP010", typeof(bool), ConfigSetterExpectation.InitOnly),
                RequiredConfigString("CodecName", ConfigSetterExpectation.InitOnly),
                ConfigString("NvencPreset", ConfigSetterExpectation.InitOnly, ConfigNullability.Nullable),
                ConfigString("SplitEncodeMode", ConfigSetterExpectation.InitOnly, ConfigNullability.NotNull),
                ConfigProperty("HdrEnabled", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("IsFullRangeInput", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigString("HdrMasterDisplayMetadata", ConfigSetterExpectation.InitOnly, ConfigNullability.Nullable),
                ConfigProperty("HdrMaxCll", typeof(int), ConfigSetterExpectation.InitOnly),
                ConfigProperty("HdrMaxFall", typeof(int), ConfigSetterExpectation.InitOnly),
                ConfigProperty("D3D11DevicePtr", typeof(IntPtr), ConfigSetterExpectation.InitOnly),
                ConfigProperty("D3D11DeviceContextPtr", typeof(IntPtr), ConfigSetterExpectation.InitOnly),
                ConfigProperty("AudioEnabled", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("MicrophoneEnabled", typeof(bool), ConfigSetterExpectation.InitOnly)
            });
        AssertDeclaredConfigProperties(
            exportProgressType,
            new ConfigPropertySpec[]
            {
                ConfigProperty("SegmentsProcessed", typeof(int), ConfigSetterExpectation.InitOnly),
                ConfigProperty("TotalSegments", typeof(int), ConfigSetterExpectation.InitOnly),
                ConfigProperty("Percent", typeof(double), ConfigSetterExpectation.InitOnly)
            });
        AssertDeclaredConfigProperties(
            exportSegmentType,
            new ConfigPropertySpec[]
            {
                RequiredConfigString("Path", ConfigSetterExpectation.InitOnly),
                ConfigProperty("StartPts", typeof(TimeSpan?), ConfigSetterExpectation.InitOnly),
                ConfigProperty("EndPts", typeof(TimeSpan?), ConfigSetterExpectation.InitOnly)
            });
        AssertDeclaredConfigProperties(
            exportRequestType,
            new ConfigPropertySpec[]
            {
                ConfigProperty(
                    "Segments",
                    typeof(IReadOnlyList<>).MakeGenericType(exportSegmentType),
                    ConfigSetterExpectation.InitOnly,
                    Nullability: ConfigNullability.Nullable,
                    ElementNullability: ConfigNullability.NotNull),
                ConfigProperty(
                    "SegmentPaths",
                    typeof(IReadOnlyList<string>),
                    ConfigSetterExpectation.InitOnly,
                    Nullability: ConfigNullability.Nullable,
                    ElementNullability: ConfigNullability.NotNull),
                ConfigString("InputTsPath", ConfigSetterExpectation.InitOnly, ConfigNullability.Nullable),
                RequiredConfigProperty("InPoint", typeof(TimeSpan), ConfigSetterExpectation.InitOnly),
                RequiredConfigProperty("OutPoint", typeof(TimeSpan), ConfigSetterExpectation.InitOnly),
                RequiredConfigString("OutputPath", ConfigSetterExpectation.InitOnly),
                ConfigProperty("FastStart", typeof(bool), ConfigSetterExpectation.InitOnly),
                ConfigProperty("AdaptiveThrottleDelayMsProvider", typeof(Func<int>), ConfigSetterExpectation.InitOnly, Nullability: ConfigNullability.Nullable)
            });

        var bufferOptions = CreateConfigInstance(bufferOptionsType);
        AssertEqual(TimeSpan.FromMinutes(5), (TimeSpan)GetPropertyValue(bufferOptions, "BufferDuration")!, "FlashbackBufferOptions.BufferDuration default");
        AssertEqual(TimeSpan.FromMinutes(10), (TimeSpan)GetPropertyValue(bufferOptions, "SegmentDuration")!, "FlashbackBufferOptions.SegmentDuration default");
        AssertContains(GetStringProperty(bufferOptions, "TempDirectory"), "Sussudio");
        AssertContains(GetStringProperty(bufferOptions, "TempDirectory"), "Flashback");
        AssertEqual(300L * 57L * 1024L * 1024L, GetLongProperty(bufferOptions, "MaxDiskBytes"), "FlashbackBufferOptions.MaxDiskBytes default");

        var sessionContext = CreateConfigInstance(sessionContextType);
        SetPropertyOrBackingField(sessionContext, "Width", 3840);
        SetPropertyOrBackingField(sessionContext, "Height", 2160);
        SetPropertyOrBackingField(sessionContext, "FrameRate", 119.88d);
        SetPropertyOrBackingField(sessionContext, "FrameRateNumerator", 120000);
        SetPropertyOrBackingField(sessionContext, "FrameRateDenominator", 1001);
        SetPropertyOrBackingField(sessionContext, "BitRate", 150_000_000u);
        SetPropertyOrBackingField(sessionContext, "IsP010", true);
        SetPropertyOrBackingField(sessionContext, "CodecName", "hevc_nvenc");
        SetPropertyOrBackingField(sessionContext, "NvencPreset", "P5");
        SetPropertyOrBackingField(sessionContext, "SplitEncodeMode", "2-way");
        SetPropertyOrBackingField(sessionContext, "HdrEnabled", true);
        SetPropertyOrBackingField(sessionContext, "IsFullRangeInput", true);
        SetPropertyOrBackingField(sessionContext, "HdrMasterDisplayMetadata", "G(13250,34500)");
        SetPropertyOrBackingField(sessionContext, "HdrMaxCll", 1000);
        SetPropertyOrBackingField(sessionContext, "HdrMaxFall", 400);
        SetPropertyOrBackingField(sessionContext, "D3D11DevicePtr", new IntPtr(123));
        SetPropertyOrBackingField(sessionContext, "D3D11DeviceContextPtr", new IntPtr(456));
        SetPropertyOrBackingField(sessionContext, "AudioEnabled", true);
        SetPropertyOrBackingField(sessionContext, "MicrophoneEnabled", true);
        AssertEqual(3840, GetIntProperty(sessionContext, "Width"), "FlashbackSessionContext.Width");
        AssertEqual("hevc_nvenc", GetStringProperty(sessionContext, "CodecName"), "FlashbackSessionContext.CodecName");
        AssertEqual("2-way", GetStringProperty(sessionContext, "SplitEncodeMode"), "FlashbackSessionContext.SplitEncodeMode");
        AssertEqual(new IntPtr(456), (IntPtr)GetPropertyValue(sessionContext, "D3D11DeviceContextPtr")!, "FlashbackSessionContext.D3D11DeviceContextPtr");

        var progress = Activator.CreateInstance(exportProgressType, 3, 10, 30d)
            ?? throw new InvalidOperationException("Failed to create ExportProgress.");
        AssertEqual(3, GetIntProperty(progress, "SegmentsProcessed"), "ExportProgress.SegmentsProcessed");
        AssertEqual(10, GetIntProperty(progress, "TotalSegments"), "ExportProgress.TotalSegments");
        AssertEqual(30d, GetDoubleProperty(progress, "Percent"), "ExportProgress.Percent");

        var exportSegment = CreateConfigInstance(exportSegmentType);
        SetPropertyOrBackingField(exportSegment, "Path", "segment.mp4");
        SetPropertyOrBackingField(exportSegment, "StartPts", TimeSpan.FromSeconds(5));
        SetPropertyOrBackingField(exportSegment, "EndPts", TimeSpan.FromSeconds(15));
        AssertEqual("segment.mp4", GetStringProperty(exportSegment, "Path"), "FlashbackExportSegment.Path");
        AssertEqual(TimeSpan.FromSeconds(5), (TimeSpan)GetPropertyValue(exportSegment, "StartPts")!, "FlashbackExportSegment.StartPts");
        AssertEqual(TimeSpan.FromSeconds(15), (TimeSpan)GetPropertyValue(exportSegment, "EndPts")!, "FlashbackExportSegment.EndPts");

        var exportRequest = CreateConfigInstance(exportRequestType);
        AssertEqual(true, GetBoolProperty(exportRequest, "FastStart"), "FlashbackExportRequest.FastStart default");
        var exportSegments = Array.CreateInstance(exportSegmentType, 1);
        exportSegments.SetValue(exportSegment, 0);
        SetPropertyOrBackingField(exportRequest, "Segments", exportSegments);
        SetPropertyOrBackingField(exportRequest, "SegmentPaths", new[] { "a.ts", "b.ts" });
        SetPropertyOrBackingField(exportRequest, "InputTsPath", "single.ts");
        SetPropertyOrBackingField(exportRequest, "InPoint", TimeSpan.FromSeconds(2));
        SetPropertyOrBackingField(exportRequest, "OutPoint", TimeSpan.FromSeconds(12));
        SetPropertyOrBackingField(exportRequest, "OutputPath", "clip.mp4");
        SetPropertyOrBackingField(exportRequest, "FastStart", false);
        AssertEqual(1, GetCountProperty(GetPropertyValue(exportRequest, "Segments")!), "FlashbackExportRequest.Segments count");
        AssertEqual(2, GetCountProperty(GetPropertyValue(exportRequest, "SegmentPaths")!), "FlashbackExportRequest.SegmentPaths count");
        AssertEqual("single.ts", GetStringProperty(exportRequest, "InputTsPath"), "FlashbackExportRequest.InputTsPath");
        AssertEqual(TimeSpan.FromSeconds(12), (TimeSpan)GetPropertyValue(exportRequest, "OutPoint")!, "FlashbackExportRequest.OutPoint");
        AssertEqual(false, GetBoolProperty(exportRequest, "FastStart"), "FlashbackExportRequest.FastStart round-trip");
        AssertEqual(null, GetPropertyValue(exportRequest, "AdaptiveThrottleDelayMsProvider"), "FlashbackExportRequest.AdaptiveThrottleDelayMsProvider default");

        return Task.CompletedTask;
    }

    private static Task RecordingPipelineOptions_DefaultsAndCapacityBounds()
    {
        var optionsType = RequireType("Sussudio.Models.RecordingPipelineOptions");
        var dropPolicyType = RequireType("Sussudio.Models.VideoFrameDropPolicy");
        AssertEnumValues(dropPolicyType, ("DropOldest", 0), ("DropNewest", 1));
        AssertDeclaredConfigProperties(
            optionsType,
            new ConfigPropertySpec[]
            {
                ConfigProperty("TargetVideoLatencyMs", typeof(int), ConfigSetterExpectation.Set),
                ConfigProperty("MinBufferedVideoFrames", typeof(int), ConfigSetterExpectation.Set),
                ConfigProperty("MaxBufferedVideoFrames", typeof(int), ConfigSetterExpectation.Set),
                ConfigProperty("VideoDropPolicy", dropPolicyType, ConfigSetterExpectation.Set)
            });

        var options = CreateConfigInstance(optionsType);
        var resolve = optionsType.GetMethod("ResolveVideoQueueCapacity", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("RecordingPipelineOptions.ResolveVideoQueueCapacity not found.");
        AssertEqual(250, GetIntProperty(options, "TargetVideoLatencyMs"), "RecordingPipelineOptions.TargetVideoLatencyMs default");
        AssertEqual(4, GetIntProperty(options, "MinBufferedVideoFrames"), "RecordingPipelineOptions.MinBufferedVideoFrames default");
        AssertEqual(30, GetIntProperty(options, "MaxBufferedVideoFrames"), "RecordingPipelineOptions.MaxBufferedVideoFrames default");
        AssertEqual(ParseEnum("Sussudio.Models.VideoFrameDropPolicy", "DropOldest"), GetPropertyValue(options, "VideoDropPolicy"), "RecordingPipelineOptions.VideoDropPolicy default");
        AssertEqual(15, (int)resolve.Invoke(options, new object[] { 60d })!, "RecordingPipelineOptions 60fps default capacity");
        AssertEqual(15, (int)resolve.Invoke(options, new object[] { -1d })!, "RecordingPipelineOptions non-positive frame rate fallback");

        SetPropertyOrBackingField(options, "TargetVideoLatencyMs", 1);
        AssertEqual(4, (int)resolve.Invoke(options, new object[] { 60d })!, "RecordingPipelineOptions latency floor clamps to min");

        SetPropertyOrBackingField(options, "MinBufferedVideoFrames", 0);
        SetPropertyOrBackingField(options, "MaxBufferedVideoFrames", 2);
        AssertEqual(1, (int)resolve.Invoke(options, new object[] { 10d })!, "RecordingPipelineOptions min floor supports one-frame queue");

        SetPropertyOrBackingField(options, "TargetVideoLatencyMs", 250);
        SetPropertyOrBackingField(options, "MinBufferedVideoFrames", 8);
        SetPropertyOrBackingField(options, "MaxBufferedVideoFrames", 4);
        AssertEqual(8, (int)resolve.Invoke(options, new object[] { 120d })!, "RecordingPipelineOptions max lower than min clamps to min");

        SetPropertyOrBackingField(options, "VideoDropPolicy", ParseEnum("Sussudio.Models.VideoFrameDropPolicy", "DropNewest"));
        AssertEqual(ParseEnum("Sussudio.Models.VideoFrameDropPolicy", "DropNewest"), GetPropertyValue(options, "VideoDropPolicy"), "RecordingPipelineOptions.VideoDropPolicy round-trip");

        return Task.CompletedTask;
    }

    private enum ConfigSetterExpectation
    {
        Set,
        InitOnly,
        None
    }

    private enum ConfigNullability
    {
        NotApplicable,
        NotNull,
        Nullable
    }

    private enum ConfigPropertyScope
    {
        Instance,
        Static
    }

    private sealed record ConfigPropertySpec(
        string Name,
        Type Type,
        ConfigSetterExpectation Setter,
        ConfigNullability Nullability = ConfigNullability.NotApplicable,
        ConfigNullability ElementNullability = ConfigNullability.NotApplicable,
        ConfigPropertyScope Scope = ConfigPropertyScope.Instance,
        bool IsRequired = false);

    private static ConfigPropertySpec ConfigProperty(
        string name,
        Type type,
        ConfigSetterExpectation setter,
        ConfigNullability Nullability = ConfigNullability.NotApplicable,
        ConfigNullability ElementNullability = ConfigNullability.NotApplicable,
        ConfigPropertyScope scope = ConfigPropertyScope.Instance,
        bool isRequired = false)
        => new(name, type, setter, Nullability, ElementNullability, scope, isRequired);

    private static ConfigPropertySpec ConfigString(
        string name,
        ConfigSetterExpectation setter,
        ConfigNullability nullability)
        => ConfigProperty(name, typeof(string), setter, nullability);

    private static ConfigPropertySpec RequiredConfigString(
        string name,
        ConfigSetterExpectation setter)
        => ConfigProperty(name, typeof(string), setter, ConfigNullability.NotNull, isRequired: true);

    private static ConfigPropertySpec RequiredConfigProperty(
        string name,
        Type type,
        ConfigSetterExpectation setter)
        => ConfigProperty(name, type, setter, isRequired: true);

    private static void AssertDeclaredConfigProperties(Type type, ConfigPropertySpec[] expectedProperties)
    {
        var instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
        var staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;
        var actualNames = type.GetProperties(instanceFlags | staticFlags)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var expectedNames = expectedProperties
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (!actualNames.SequenceEqual(expectedNames))
        {
            throw new InvalidOperationException(
                $"{type.Name} public property set changed. Expected: {string.Join(", ", expectedNames)}; actual: {string.Join(", ", actualNames)}.");
        }

        foreach (var expected in expectedProperties)
        {
            var flags = expected.Scope == ConfigPropertyScope.Static ? staticFlags : instanceFlags;
            var property = type.GetProperty(expected.Name, flags)
                ?? throw new InvalidOperationException($"{type.Name}.{expected.Name} was not found.");
            AssertEqual(expected.Type, property.PropertyType, $"{type.Name}.{expected.Name} property type");
            AssertEqual(
                expected.IsRequired,
                property.GetCustomAttribute<RequiredMemberAttribute>() != null,
                $"{type.Name}.{expected.Name} required-member metadata");
            if (property.GetMethod == null || !property.GetMethod.IsPublic)
            {
                throw new InvalidOperationException($"{type.Name}.{expected.Name} must expose a public getter.");
            }

            if (expected.Setter == ConfigSetterExpectation.None)
            {
                if (property.SetMethod != null)
                {
                    throw new InvalidOperationException($"{type.Name}.{expected.Name} must not expose a setter.");
                }
            }
            else
            {
                if (property.SetMethod == null || !property.SetMethod.IsPublic)
                {
                    throw new InvalidOperationException($"{type.Name}.{expected.Name} must expose a public setter.");
                }

                var isInitOnly = IsInitOnlySetter(property);
                AssertEqual(
                    expected.Setter == ConfigSetterExpectation.InitOnly,
                    isInitOnly,
                    $"{type.Name}.{expected.Name} init-only setter");
            }

            if (expected.Nullability != ConfigNullability.NotApplicable)
            {
                var nullability = new NullabilityInfoContext().Create(property);
                var expectedState = expected.Nullability == ConfigNullability.Nullable
                    ? NullabilityState.Nullable
                    : NullabilityState.NotNull;
                AssertEqual(expectedState, nullability.ReadState, $"{type.Name}.{expected.Name} read nullability");
                if (expected.Setter != ConfigSetterExpectation.None)
                {
                    AssertEqual(expectedState, nullability.WriteState, $"{type.Name}.{expected.Name} write nullability");
                }

                if (expected.ElementNullability != ConfigNullability.NotApplicable)
                {
                    var elementNullability = property.PropertyType.IsArray
                        ? nullability.ElementType
                        : nullability.GenericTypeArguments.FirstOrDefault();
                    if (elementNullability == null)
                    {
                        throw new InvalidOperationException($"{type.Name}.{expected.Name} did not expose element nullability.");
                    }

                    var expectedElementState = expected.ElementNullability == ConfigNullability.Nullable
                        ? NullabilityState.Nullable
                        : NullabilityState.NotNull;
                    AssertEqual(expectedElementState, elementNullability.ReadState, $"{type.Name}.{expected.Name} element read nullability");
                    AssertEqual(expectedElementState, elementNullability.WriteState, $"{type.Name}.{expected.Name} element write nullability");
                }
            }
        }
    }

    private static bool IsInitOnlySetter(PropertyInfo property)
        => property.SetMethod?.ReturnParameter.GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit") == true;

    private static object CreateResolutionFormatDictionary(Type mediaFormatType)
        => Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(
               typeof(string),
               typeof(List<>).MakeGenericType(mediaFormatType)))
           ?? throw new InvalidOperationException("Failed to create resolution format dictionary.");

    private static void AddResolutionFormats(
        object formatsByResolution,
        Type mediaFormatType,
        string resolutionKey,
        params object[] formats)
        => ((IDictionary)formatsByResolution).Add(
            resolutionKey,
            CreateMediaFormatList(mediaFormatType, formats));

    private static object CreateMediaFormatList(Type mediaFormatType, params object[] formats)
    {
        var list = (IList)(Activator.CreateInstance(typeof(List<>).MakeGenericType(mediaFormatType))
                           ?? throw new InvalidOperationException("Failed to create media format list."));
        foreach (var format in formats)
        {
            list.Add(format);
        }

        return list;
    }

    private static object CreateTestMediaFormat(
        Type mediaFormatType,
        uint width,
        uint height,
        double frameRate,
        string pixelFormat,
        bool isHdr)
    {
        var format = CreateConfigInstance(mediaFormatType);
        SetPropertyOrBackingField(format, "Width", width);
        SetPropertyOrBackingField(format, "Height", height);
        SetPropertyOrBackingField(format, "FrameRate", frameRate);
        SetPropertyOrBackingField(format, "PixelFormat", pixelFormat);
        SetPropertyOrBackingField(format, "IsHdr", isHdr);
        return format;
    }

    private static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string fieldName)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}. Expected: {string.Join(", ", expected)}; actual: {string.Join(", ", actual)}.");
        }
    }

    private static object CreateConfigInstance(Type type)
        => Activator.CreateInstance(type, nonPublic: true)
           ?? throw new InvalidOperationException($"Failed to create {type.Name}.");

    private static void AssertEnumValues(Type enumType, params (string Name, int Value)[] expectedValues)
    {
        AssertEqual(expectedValues.Length, Enum.GetNames(enumType).Length, $"{enumType.Name} value count");
        foreach (var (name, value) in expectedValues)
        {
            var parsed = Enum.Parse(enumType, name);
            AssertEqual(value, Convert.ToInt32(parsed), $"{enumType.Name}.{name}");
        }
    }
}
