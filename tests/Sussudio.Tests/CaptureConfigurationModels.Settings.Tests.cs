using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
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

    private static Task CaptureSettings_MjpegHighFrameRateMode_RequiresSdr4k120StyleRequest()
    {
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Width", 3840u);
        SetPropertyOrBackingField(settings, "Height", 2160u);
        SetPropertyOrBackingField(settings, "FrameRate", 120d);
        SetPropertyOrBackingField(settings, "RequestedPixelFormat", "MJPG");
        SetPropertyOrBackingField(settings, "HdrEnabled", false);

        AssertEqual(true, GetBoolProperty(settings, "UseMjpegHighFrameRateMode"), "UseMjpegHighFrameRateMode");

        SetPropertyOrBackingField(settings, "HdrEnabled", true);
        AssertEqual(false, GetBoolProperty(settings, "UseMjpegHighFrameRateMode"), "UseMjpegHighFrameRateMode HDR");

        SetPropertyOrBackingField(settings, "HdrEnabled", false);
        SetPropertyOrBackingField(settings, "Width", 1920u);
        AssertEqual(false, GetBoolProperty(settings, "UseMjpegHighFrameRateMode"), "UseMjpegHighFrameRateMode non-4k");

        return Task.CompletedTask;
    }

    private static Task CaptureSettings_GetTargetBitrate_ScalesByResolutionAndFrameRate()
    {
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Width", 3840u);
        SetPropertyOrBackingField(settings, "Height", 2160u);
        SetPropertyOrBackingField(settings, "FrameRate", 60.0);
        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "H264Mp4"));
        SetPropertyOrBackingField(settings, "Quality", ParseEnum("Sussudio.Models.VideoQuality", "High"));

        var bitrate = InvokeInstanceMethod(settings, "GetTargetBitrate");
        var bps = Convert.ToUInt32(bitrate);

        if (bps < 150_000_000 || bps > 200_000_000)
        {
            throw new InvalidOperationException($"Expected 4K60 H264 High ~200 Mbps, got {bps / 1_000_000.0:F1} Mbps");
        }

        SetPropertyOrBackingField(settings, "Width", 1920u);
        SetPropertyOrBackingField(settings, "Height", 1080u);
        SetPropertyOrBackingField(settings, "FrameRate", 30.0);
        var lowBitrate = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));
        if (lowBitrate < 24_000_000 || lowBitrate > 26_000_000)
        {
            throw new InvalidOperationException($"Expected 1080p30 H264 High ~25 Mbps, got {lowBitrate / 1_000_000.0:F1} Mbps");
        }

        return Task.CompletedTask;
    }

    private static Task CaptureSettings_GetTargetBitrate_AppliesCodecEfficiency()
    {
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Width", 1920u);
        SetPropertyOrBackingField(settings, "Height", 1080u);
        SetPropertyOrBackingField(settings, "FrameRate", 60.0);
        SetPropertyOrBackingField(settings, "Quality", ParseEnum("Sussudio.Models.VideoQuality", "High"));

        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "H264Mp4"));
        var h264 = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));

        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "HevcMp4"));
        var hevc = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));

        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "Av1Mp4"));
        var av1 = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));

        if (hevc >= h264)
        {
            throw new InvalidOperationException($"HEVC ({hevc}) should be less than H264 ({h264})");
        }

        if (av1 >= hevc)
        {
            throw new InvalidOperationException($"AV1 ({av1}) should be less than HEVC ({hevc})");
        }

        return Task.CompletedTask;
    }

    private static Task CaptureSettings_GetTargetBitrate_ClampsCustomQuality()
    {
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Quality", ParseEnum("Sussudio.Models.VideoQuality", "Custom"));

        SetPropertyOrBackingField(settings, "CustomBitrateMbps", 999.0);
        var over = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));
        AssertEqual(300_000_000u, over, "CustomBitrate over-max clamp");

        SetPropertyOrBackingField(settings, "CustomBitrateMbps", 0.1);
        var under = Convert.ToUInt32(InvokeInstanceMethod(settings, "GetTargetBitrate"));
        AssertEqual(1_000_000u, under, "CustomBitrate under-min clamp");

        return Task.CompletedTask;
    }

    private static Task CaptureSettings_GetOutputFileName_IncludesFormatSuffix()
    {
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");

        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "Av1Mp4"));
        var av1Name = InvokeInstanceMethod(settings, "GetOutputFileName").ToString()!;
        AssertContains(av1Name, "_AV1.");
        AssertContains(av1Name, ".mp4");

        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "HevcMp4"));
        var hevcName = InvokeInstanceMethod(settings, "GetOutputFileName").ToString()!;
        AssertContains(hevcName, "_HEVC.");

        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "H264Mp4"));
        var h264Name = InvokeInstanceMethod(settings, "GetOutputFileName").ToString()!;
        AssertContains(h264Name, "_H264.");

        return Task.CompletedTask;
    }

    private static Task CaptureSettings_MjpegHfrMode_RequiresSdrAndMjpgPixelFormat()
    {
        var settingsType = RequireType("Sussudio.Models.CaptureSettings");
        var method = settingsType.GetMethod("IsMjpegHighFrameRateMode", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("IsMjpegHighFrameRateMode not found");

        var result1 = (bool)method.Invoke(null, new object?[] { "MJPG", 3840u, 2160u, 120.0, false, false })!;
        AssertEqual(true, result1, "SDR+MJPG+4K120 should be HFR");

        var result2 = (bool)method.Invoke(null, new object?[] { "MJPG", 3840u, 2160u, 120.0, true, false })!;
        AssertEqual(false, result2, "HDR should not be HFR");

        var result3 = (bool)method.Invoke(null, new object?[] { "NV12", 3840u, 2160u, 120.0, false, false })!;
        AssertEqual(false, result3, "NV12 should not be HFR");

        var result4 = (bool)method.Invoke(null, new object?[] { "MJPG", 1920u, 1080u, 60.0, false, false })!;
        AssertEqual(false, result4, "1080p60 should not be HFR");

        return Task.CompletedTask;
    }
}
