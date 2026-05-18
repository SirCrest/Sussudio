using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public partial class CaptureConfigurationModelsTests
{
    [Fact]
    public void CaptureSettings_DefaultsAndOutputContracts()
    {
        var asm = SussudioAssembly.Load();
        var settingsType = RequireType(asm, "Sussudio.Models.CaptureSettings");
        var recordingFormatType = RequireType(asm, "Sussudio.Models.RecordingFormat");
        var videoQualityType = RequireType(asm, "Sussudio.Models.VideoQuality");
        var hdrOutputModeType = RequireType(asm, "Sussudio.Models.HdrOutputMode");
        var previewModeType = RequireType(asm, "Sussudio.Models.PreviewMode");
        var audioPathModeType = RequireType(asm, "Sussudio.Models.AudioPathMode");
        var pipelineOptionsType = RequireType(asm, "Sussudio.Models.RecordingPipelineOptions");
        var splitEncodeSupportType = RequireType(asm, "Sussudio.Models.SplitEncodeSupport");
        var nvencPresetType = RequireType(asm, "Sussudio.Models.NvencPreset");
        var splitEncodeModeType = RequireType(asm, "Sussudio.Models.SplitEncodeMode");

        AssertEnumValues(recordingFormatType, ("H264Mp4", 0), ("HevcMp4", 1), ("Av1Mp4", 2));
        AssertEnumValues(videoQualityType, ("Auto", 0), ("Low", 1), ("Medium", 2), ("High", 3), ("SuperHigh", 4), ("Custom", 5));
        AssertEnumValues(hdrOutputModeType, ("Off", 0), ("Hdr10Pq", 1));
        AssertEnumValues(previewModeType, ("GpuFast", 0), ("TrueHdr", 1));
        AssertEnumValues(nvencPresetType, ("Auto", 0), ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4), ("P5", 5), ("P6", 6), ("P7", 7), ("Fast", 8), ("Slow", 9));
        AssertEnumValues(splitEncodeModeType, ("Auto", 0), ("Disabled", 1), ("TwoWay", 2), ("ThreeWay", 3), ("ForcedOn", 4));

        AssertDeclaredProperties(
            settingsType,
            new[]
            {
                Property("Width", typeof(uint), SetterExpectation.Set),
                Property("Height", typeof(uint), SetterExpectation.Set),
                Property("FrameRate", typeof(double), SetterExpectation.Set),
                String("RequestedFrameRateArg", SetterExpectation.Set, NullabilityExpectation.Nullable),
                Property("RequestedFrameRateNumerator", typeof(uint?), SetterExpectation.Set),
                Property("RequestedFrameRateDenominator", typeof(uint?), SetterExpectation.Set),
                String("RequestedPixelFormat", SetterExpectation.Set, NullabilityExpectation.Nullable),
                Property("Format", recordingFormatType, SetterExpectation.Set),
                Property("Quality", videoQualityType, SetterExpectation.Set),
                Property("NvencPreset", nvencPresetType, SetterExpectation.Set),
                Property("SplitEncodeMode", splitEncodeModeType, SetterExpectation.Set),
                Property("CustomBitrateMbps", typeof(double), SetterExpectation.Set),
                Property("HdrEnabled", typeof(bool), SetterExpectation.Set),
                Property("HdrOutputMode", hdrOutputModeType, SetterExpectation.Set),
                Property("HdrNominalPeakNits", typeof(int), SetterExpectation.Set),
                Property("HdrMaxCll", typeof(int), SetterExpectation.Set),
                Property("HdrMaxFall", typeof(int), SetterExpectation.Set),
                String("HdrMasterDisplayMetadata", SetterExpectation.Set, NullabilityExpectation.NotNull),
                Property("PreviewMode", previewModeType, SetterExpectation.Set),
                String("OutputPath", SetterExpectation.Set, NullabilityExpectation.NotNull),
                Property("AudioEnabled", typeof(bool), SetterExpectation.Set),
                Property("UseCustomAudioInput", typeof(bool), SetterExpectation.Set),
                String("AudioDeviceId", SetterExpectation.Set, NullabilityExpectation.Nullable),
                String("AudioDeviceName", SetterExpectation.Set, NullabilityExpectation.Nullable),
                Property("MicrophoneEnabled", typeof(bool), SetterExpectation.Set),
                String("MicrophoneDeviceId", SetterExpectation.Set, NullabilityExpectation.Nullable),
                String("MicrophoneDeviceName", SetterExpectation.Set, NullabilityExpectation.Nullable),
                Property("AudioPathMode", audioPathModeType, SetterExpectation.Set),
                Property("PipelineOptions", pipelineOptionsType, SetterExpectation.Set),
                Property("ForceMjpegDecode", typeof(bool), SetterExpectation.Set),
                Property("FlashbackGpuDecode", typeof(bool), SetterExpectation.Set),
                Property("FlashbackBufferMinutes", typeof(int), SetterExpectation.Set),
                Property("MjpegDecoderCount", typeof(int), SetterExpectation.Set),
                Property("UseMjpegHighFrameRateMode", typeof(bool), SetterExpectation.None)
            });
        AssertDeclaredProperties(
            splitEncodeSupportType,
            new[]
            {
                Property("Supports2Way", typeof(bool), SetterExpectation.InitOnly),
                Property("Supports3Way", typeof(bool), SetterExpectation.InitOnly),
                Property("NvencUnavailable", splitEncodeSupportType, SetterExpectation.None, scope: PropertyScope.Static)
            });

        var settings = CreateInstance(settingsType);
        Assert.Equal(1920u, Get<uint>(settings, "Width"));
        Assert.Equal(1080u, Get<uint>(settings, "Height"));
        Assert.Equal(60d, Get<double>(settings, "FrameRate"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.RecordingFormat", "H264Mp4"), Get(settings, "Format"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.VideoQuality", "High"), Get(settings, "Quality"));
        Assert.Equal("Auto", Get(settings, "NvencPreset")!.ToString());
        Assert.Equal("Auto", Get(settings, "SplitEncodeMode")!.ToString());
        Assert.Equal(50d, Get<double>(settings, "CustomBitrateMbps"));
        Assert.False(Get<bool>(settings, "HdrEnabled"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.HdrOutputMode", "Hdr10Pq"), Get(settings, "HdrOutputMode"));
        Assert.Equal(1000, Get<int>(settings, "HdrNominalPeakNits"));
        Assert.Equal(string.Empty, Get<string>(settings, "HdrMasterDisplayMetadata"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.PreviewMode", "GpuFast"), Get(settings, "PreviewMode"));
        Assert.True(Get<bool>(settings, "AudioEnabled"));
        Assert.False(Get<bool>(settings, "UseCustomAudioInput"));
        Assert.False(Get<bool>(settings, "MicrophoneEnabled"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.AudioPathMode", "PostMuxDefault"), Get(settings, "AudioPathMode"));
        Assert.NotNull(Get(settings, "PipelineOptions"));
        Assert.False(Get<bool>(settings, "ForceMjpegDecode"));
        Assert.True(Get<bool>(settings, "FlashbackGpuDecode"));
        Assert.Equal(5, Get<int>(settings, "FlashbackBufferMinutes"));
        Assert.Equal(6, Get<int>(settings, "MjpegDecoderCount"));
        Assert.False(Get<bool>(settings, "UseMjpegHighFrameRateMode"));

        var otherSettings = CreateInstance(settingsType);
        Assert.NotSame(Get(settings, "PipelineOptions"), Get(otherSettings, "PipelineOptions"));

        var outputDir = Path.Combine(Path.GetTempPath(), $"capture_settings_{Guid.NewGuid():N}");
        Set(settings, "OutputPath", outputDir);
        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "HevcMp4"));
        var fullPath = Invoke(settings, "GetFullOutputPath")!.ToString()!;
        Assert.Equal(outputDir, Path.GetDirectoryName(fullPath));
        Assert.Contains("_HEVC.mp4", Path.GetFileName(fullPath), StringComparison.Ordinal);

        var splitSupport = Activator.CreateInstance(splitEncodeSupportType, true, false)!;
        Assert.True(Get<bool>(splitSupport, "Supports2Way"));
        Assert.False(Get<bool>(splitSupport, "Supports3Way"));
        var nvencUnavailable = splitEncodeSupportType.GetProperty("NvencUnavailable", ReflectionFlags.Static)!.GetValue(null)!;
        Assert.False(Get<bool>(nvencUnavailable, "Supports2Way"));
        Assert.False(Get<bool>(nvencUnavailable, "Supports3Way"));
    }

    [Fact]
    public void CaptureSettings_MjpegHighFrameRateMode_HandlesForceCaseAndInstanceState()
    {
        var asm = SussudioAssembly.Load();
        var settingsType = RequireType(asm, "Sussudio.Models.CaptureSettings");
        var method = RequireMethod(settingsType, "IsMjpegHighFrameRateMode", BindingFlags.Public | BindingFlags.Static);

        Assert.True((bool)method.Invoke(null, new object?[] { "mjpg", 3840u, 2160u, 100d, false, false })!);
        Assert.True((bool)method.Invoke(null, new object?[] { "MJPG", 1920u, 1080u, 60d, false, true })!);
        Assert.False((bool)method.Invoke(null, new object?[] { "NV12", 3840u, 2160u, 120d, false, true })!);
        Assert.False((bool)method.Invoke(null, new object?[] { "MJPG", 3840u, 2160u, 120d, true, true })!);

        var settings = CreateInstance(settingsType);
        Set(settings, "RequestedPixelFormat", "MJPG");
        Set(settings, "Width", 3840u);
        Set(settings, "Height", 2160u);
        Set(settings, "FrameRate", 120d);
        Set(settings, "HdrEnabled", false);
        Assert.True(Get<bool>(settings, "UseMjpegHighFrameRateMode"));
        Set(settings, "HdrEnabled", true);
        Assert.False(Get<bool>(settings, "UseMjpegHighFrameRateMode"));
        Set(settings, "Width", 1920u);
        Set(settings, "Height", 1080u);
        Set(settings, "FrameRate", 60d);
        Set(settings, "HdrEnabled", false);
        Set(settings, "ForceMjpegDecode", true);
        Assert.True(Get<bool>(settings, "UseMjpegHighFrameRateMode"));
        Set(settings, "HdrEnabled", true);
        Assert.False(Get<bool>(settings, "UseMjpegHighFrameRateMode"));
    }

    [Fact]
    public void CaptureSettings_MjpegHighFrameRateMode_RequiresSdr4k120StyleRequest()
    {
        var settings = CreateInstance(RequireType(SussudioAssembly.Load(), "Sussudio.Models.CaptureSettings"));
        Set(settings, "Width", 3840u);
        Set(settings, "Height", 2160u);
        Set(settings, "FrameRate", 120d);
        Set(settings, "RequestedPixelFormat", "MJPG");
        Set(settings, "HdrEnabled", false);
        Assert.True(Get<bool>(settings, "UseMjpegHighFrameRateMode"));

        Set(settings, "HdrEnabled", true);
        Assert.False(Get<bool>(settings, "UseMjpegHighFrameRateMode"));

        Set(settings, "HdrEnabled", false);
        Set(settings, "Width", 1920u);
        Assert.False(Get<bool>(settings, "UseMjpegHighFrameRateMode"));
    }

    [Fact]
    public void CaptureSettings_GetTargetBitrate_ScalesByResolutionAndFrameRate()
    {
        var asm = SussudioAssembly.Load();
        var settings = CreateInstance(RequireType(asm, "Sussudio.Models.CaptureSettings"));
        Set(settings, "Width", 3840u);
        Set(settings, "Height", 2160u);
        Set(settings, "FrameRate", 60.0);
        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "H264Mp4"));
        Set(settings, "Quality", ParseEnum(asm, "Sussudio.Models.VideoQuality", "High"));

        var bps = Convert.ToUInt32(Invoke(settings, "GetTargetBitrate"));
        Assert.InRange(bps, 150_000_000u, 200_000_000u);

        Set(settings, "Width", 1920u);
        Set(settings, "Height", 1080u);
        Set(settings, "FrameRate", 30.0);
        var lowBitrate = Convert.ToUInt32(Invoke(settings, "GetTargetBitrate"));
        Assert.InRange(lowBitrate, 24_000_000u, 26_000_000u);
    }

    [Fact]
    public void CaptureSettings_GetTargetBitrate_AppliesCodecEfficiency()
    {
        var asm = SussudioAssembly.Load();
        var settings = CreateInstance(RequireType(asm, "Sussudio.Models.CaptureSettings"));
        Set(settings, "Width", 1920u);
        Set(settings, "Height", 1080u);
        Set(settings, "FrameRate", 60.0);
        Set(settings, "Quality", ParseEnum(asm, "Sussudio.Models.VideoQuality", "High"));

        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "H264Mp4"));
        var h264 = Convert.ToUInt32(Invoke(settings, "GetTargetBitrate"));
        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "HevcMp4"));
        var hevc = Convert.ToUInt32(Invoke(settings, "GetTargetBitrate"));
        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "Av1Mp4"));
        var av1 = Convert.ToUInt32(Invoke(settings, "GetTargetBitrate"));

        Assert.True(hevc < h264);
        Assert.True(av1 < hevc);
    }

    [Fact]
    public void CaptureSettings_GetTargetBitrate_ClampsCustomQuality()
    {
        var asm = SussudioAssembly.Load();
        var settings = CreateInstance(RequireType(asm, "Sussudio.Models.CaptureSettings"));
        Set(settings, "Quality", ParseEnum(asm, "Sussudio.Models.VideoQuality", "Custom"));

        Set(settings, "CustomBitrateMbps", 999.0);
        Assert.Equal(300_000_000u, Convert.ToUInt32(Invoke(settings, "GetTargetBitrate")));
        Set(settings, "CustomBitrateMbps", 0.1);
        Assert.Equal(1_000_000u, Convert.ToUInt32(Invoke(settings, "GetTargetBitrate")));
    }

    [Fact]
    public void CaptureSettings_GetOutputFileName_IncludesFormatSuffix()
    {
        var asm = SussudioAssembly.Load();
        var settings = CreateInstance(RequireType(asm, "Sussudio.Models.CaptureSettings"));

        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "Av1Mp4"));
        var av1Name = Invoke(settings, "GetOutputFileName")!.ToString()!;
        Assert.Contains("_AV1.", av1Name, StringComparison.Ordinal);
        Assert.Contains(".mp4", av1Name, StringComparison.Ordinal);

        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "HevcMp4"));
        Assert.Contains("_HEVC.", Invoke(settings, "GetOutputFileName")!.ToString()!, StringComparison.Ordinal);

        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "H264Mp4"));
        Assert.Contains("_H264.", Invoke(settings, "GetOutputFileName")!.ToString()!, StringComparison.Ordinal);
    }

    [Fact]
    public void CaptureSettings_MjpegHfrMode_RequiresSdrAndMjpgPixelFormat()
    {
        var settingsType = RequireType(SussudioAssembly.Load(), "Sussudio.Models.CaptureSettings");
        var method = RequireMethod(settingsType, "IsMjpegHighFrameRateMode", BindingFlags.Public | BindingFlags.Static);

        Assert.True((bool)method.Invoke(null, new object?[] { "MJPG", 3840u, 2160u, 120.0, false, false })!);
        Assert.False((bool)method.Invoke(null, new object?[] { "MJPG", 3840u, 2160u, 120.0, true, false })!);
        Assert.False((bool)method.Invoke(null, new object?[] { "NV12", 3840u, 2160u, 120.0, false, false })!);
        Assert.False((bool)method.Invoke(null, new object?[] { "MJPG", 1920u, 1080u, 60.0, false, false })!);
    }

}
