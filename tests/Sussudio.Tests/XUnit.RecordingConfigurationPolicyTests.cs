using System.Collections;
using Xunit;

namespace Sussudio.Tests;

public partial class CaptureConfigurationModelsTests
{
    [Fact]
    public void RecordingSettingsSelectionPolicy_PreservesHdrAndSdrChoices()
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
    }

    [Fact]
    public void RecordingSettingsSelectionPolicy_ParsesModelValues()
    {
        var asm = SussudioAssembly.Load();
        var policyType = RequireType(asm, "Sussudio.ViewModels.RecordingSettingsSelectionPolicy");
        var parseRecordingFormat = RequireMethod(policyType, "ParseRecordingFormat", ReflectionFlags.Static);
        var parseVideoQuality = RequireMethod(policyType, "ParseVideoQuality", ReflectionFlags.Static);
        var clampCustomBitrate = RequireMethod(policyType, "ClampCustomBitrateMbps", ReflectionFlags.Static);

        Assert.Equal(ParseEnum(asm, "Sussudio.Models.RecordingFormat", "H264Mp4"), parseRecordingFormat.Invoke(null, new object?[] { "H.264" }));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.RecordingFormat", "HevcMp4"), parseRecordingFormat.Invoke(null, new object?[] { "HEVC" }));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.RecordingFormat", "Av1Mp4"), parseRecordingFormat.Invoke(null, new object?[] { "AV1" }));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.VideoQuality", "Auto"), parseVideoQuality.Invoke(null, new object?[] { "Auto" }));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.VideoQuality", "SuperHigh"), parseVideoQuality.Invoke(null, new object?[] { "Super High" }));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.VideoQuality", "High"), parseVideoQuality.Invoke(null, new object?[] { "unexpected" }));
        Assert.Equal(1d, (double)clampCustomBitrate.Invoke(null, new object?[] { -5d })!);
        Assert.Equal(42d, (double)clampCustomBitrate.Invoke(null, new object?[] { 42d })!);
        Assert.Equal(300d, (double)clampCustomBitrate.Invoke(null, new object?[] { 999d })!);
    }

    [Fact]
    public void EncoderSupport_ComputesAvailabilityAndPreferredEncoders()
    {
        var supportType = RequireType(SussudioAssembly.Load(), "Sussudio.Models.EncoderSupport");
        AssertDeclaredProperties(
            supportType,
            new[]
            {
                Property("HasH264Nvenc", typeof(bool), SetterExpectation.InitOnly),
                Property("HasHevcNvenc", typeof(bool), SetterExpectation.InitOnly),
                Property("HasAv1Nvenc", typeof(bool), SetterExpectation.InitOnly),
                Property("HasLibX264", typeof(bool), SetterExpectation.InitOnly),
                Property("HasLibX265", typeof(bool), SetterExpectation.InitOnly),
                Property("HasLibSvtAv1", typeof(bool), SetterExpectation.InitOnly),
                Property("HasLibAomAv1", typeof(bool), SetterExpectation.InitOnly),
                Property("HasH264", typeof(bool), SetterExpectation.None),
                Property("HasHevc", typeof(bool), SetterExpectation.None),
                Property("HasAv1", typeof(bool), SetterExpectation.None),
                String("PreferredAv1Encoder", SetterExpectation.None, NullabilityExpectation.Nullable),
                Property("Empty", supportType, SetterExpectation.None, scope: PropertyScope.Static)
            });

        var empty = supportType.GetProperty("Empty", ReflectionFlags.Static)!.GetValue(null)!;
        Assert.False(Get<bool>(empty, "HasH264"));
        Assert.False(Get<bool>(empty, "HasHevc"));
        Assert.False(Get<bool>(empty, "HasAv1"));
        Assert.Null(Get(empty, "PreferredAv1Encoder"));

        var nvencAv1 = CreateInstance(supportType);
        Set(nvencAv1, "HasAv1Nvenc", true);
        Set(nvencAv1, "HasLibSvtAv1", true);
        Assert.True(Get<bool>(nvencAv1, "HasAv1"));
        Assert.Equal("av1_nvenc", Get<string>(nvencAv1, "PreferredAv1Encoder"));

        var svtAv1 = CreateInstance(supportType);
        Set(svtAv1, "HasLibSvtAv1", true);
        Set(svtAv1, "HasLibAomAv1", true);
        Assert.Equal("libsvtav1", Get<string>(svtAv1, "PreferredAv1Encoder"));

        var softwareFallbacks = CreateInstance(supportType);
        Set(softwareFallbacks, "HasLibX264", true);
        Set(softwareFallbacks, "HasLibX265", true);
        Set(softwareFallbacks, "HasLibAomAv1", true);
        Assert.True(Get<bool>(softwareFallbacks, "HasH264"));
        Assert.True(Get<bool>(softwareFallbacks, "HasHevc"));
        Assert.Equal("libaom-av1", Get<string>(softwareFallbacks, "PreferredAv1Encoder"));
    }

    [Fact]
    public void RecordingPipelineOptions_DefaultsAndCapacityBounds()
    {
        var asm = SussudioAssembly.Load();
        var optionsType = RequireType(asm, "Sussudio.Models.RecordingPipelineOptions");
        var dropPolicyType = RequireType(asm, "Sussudio.Models.VideoFrameDropPolicy");
        AssertEnumValues(dropPolicyType, ("DropOldest", 0), ("DropNewest", 1));
        AssertDeclaredProperties(
            optionsType,
            new[]
            {
                Property("TargetVideoLatencyMs", typeof(int), SetterExpectation.Set),
                Property("MinBufferedVideoFrames", typeof(int), SetterExpectation.Set),
                Property("MaxBufferedVideoFrames", typeof(int), SetterExpectation.Set),
                Property("VideoDropPolicy", dropPolicyType, SetterExpectation.Set)
            });

        var options = CreateInstance(optionsType);
        var resolve = RequireMethod(optionsType, "ResolveVideoQueueCapacity", ReflectionFlags.Instance);
        Assert.Equal(250, Get<int>(options, "TargetVideoLatencyMs"));
        Assert.Equal(4, Get<int>(options, "MinBufferedVideoFrames"));
        Assert.Equal(30, Get<int>(options, "MaxBufferedVideoFrames"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.VideoFrameDropPolicy", "DropOldest"), Get(options, "VideoDropPolicy"));
        Assert.Equal(15, (int)resolve.Invoke(options, new object[] { 60d })!);
        Assert.Equal(15, (int)resolve.Invoke(options, new object[] { -1d })!);

        Set(options, "TargetVideoLatencyMs", 1);
        Assert.Equal(4, (int)resolve.Invoke(options, new object[] { 60d })!);

        Set(options, "MinBufferedVideoFrames", 0);
        Set(options, "MaxBufferedVideoFrames", 2);
        Assert.Equal(1, (int)resolve.Invoke(options, new object[] { 10d })!);

        Set(options, "TargetVideoLatencyMs", 250);
        Set(options, "MinBufferedVideoFrames", 8);
        Set(options, "MaxBufferedVideoFrames", 4);
        Assert.Equal(8, (int)resolve.Invoke(options, new object[] { 120d })!);

        Set(options, "VideoDropPolicy", ParseEnum(asm, "Sussudio.Models.VideoFrameDropPolicy", "DropNewest"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.VideoFrameDropPolicy", "DropNewest"), Get(options, "VideoDropPolicy"));
    }

    [Fact]
    public void RecordingPipelineOptions_ResolvesVideoQueueCapacity()
    {
        var options = CreateInstance(RequireType(SussudioAssembly.Load(), "Sussudio.Models.RecordingPipelineOptions"));
        var resolve = RequireMethod(options.GetType(), "ResolveVideoQueueCapacity", ReflectionFlags.Instance);

        Assert.Equal(15, (int)resolve.Invoke(options, new object[] { 60.0 })!);
        Assert.Equal(30, (int)resolve.Invoke(options, new object[] { 120.0 })!);
        Assert.Equal(8, (int)resolve.Invoke(options, new object[] { 30.0 })!);
        Assert.Equal(15, (int)resolve.Invoke(options, new object[] { 0.0 })!);
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
        var asm = SussudioAssembly.Load();
        var policyType = RequireType(asm, "Sussudio.ViewModels.RecordingSettingsSelectionPolicy");
        var select = RequireMethod(policyType, "Select", ReflectionFlags.Static);
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
                })!;

        var availableFormats = ((IEnumerable)Get(selection, "AvailableFormats")!)
            .Cast<string>()
            .ToArray();
        var selected = Get<string>(selection, "SelectedFormat");
        Assert.Equal(expectedFormats, availableFormats);
        Assert.Equal(expectedSelectedFormat, selected);
    }
}
