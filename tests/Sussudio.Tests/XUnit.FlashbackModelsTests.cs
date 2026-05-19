using Xunit;

namespace Sussudio.Tests;

public partial class FlashbackModelsTests
{
    [Fact]
    public void FlashbackBufferOptions_MaxDiskBytes_ScalesWithDuration()
    {
        var asm = SussudioAssembly.Load();
        var optionsType = RequireType(asm, "Sussudio.Models.FlashbackBufferOptions");
        var options = CreateInstance(optionsType);

        const long safetyBytesPerSecond = 57L * 1024L * 1024L;

        Set(options, "BufferDuration", TimeSpan.FromMinutes(5));
        var maxBytes = Get<long>(options, "MaxDiskBytes");
        Assert.Equal(300L * safetyBytesPerSecond, maxBytes);

        Set(options, "BufferDuration", TimeSpan.FromMinutes(1));
        var oneMinuteBytes = Get<long>(options, "MaxDiskBytes");
        Assert.Equal(60L * safetyBytesPerSecond, oneMinuteBytes);
        Assert.Equal(maxBytes, oneMinuteBytes * 5);

        Set(options, "BufferDuration", TimeSpan.Zero);
        Assert.Equal(0L, Get<long>(options, "MaxDiskBytes"));

        Set(options, "BufferDuration", TimeSpan.FromTicks(-1));
        Assert.Equal(0L, Get<long>(options, "MaxDiskBytes"));

        Set(options, "BufferDuration", TimeSpan.MaxValue);
        Assert.Equal(long.MaxValue, Get<long>(options, "MaxDiskBytes"));
    }

    [Fact]
    public void FlashbackModels_PreserveBufferSessionExportContracts()
    {
        var asm = SussudioAssembly.Load();
        var bufferOptionsType = RequireType(asm, "Sussudio.Models.FlashbackBufferOptions");
        var sessionContextType = RequireType(asm, "Sussudio.Models.FlashbackSessionContext");
        var playbackStateType = RequireType(asm, "Sussudio.Models.FlashbackPlaybackState");
        var exportProgressType = RequireType(asm, "Sussudio.Models.ExportProgress");
        var exportSegmentType = RequireType(asm, "Sussudio.Models.FlashbackExportSegment");
        var exportRequestType = RequireType(asm, "Sussudio.Models.FlashbackExportRequest");

        AssertEnumValues(playbackStateType, ("Disabled", 0), ("Buffering", 1), ("Live", 2), ("Scrubbing", 3), ("Playing", 4), ("Paused", 5));
        AssertDeclaredProperties(
            bufferOptionsType,
            new[]
            {
                Property("BufferDuration", typeof(TimeSpan), SetterExpectation.InitOnly),
                String("TempDirectory", SetterExpectation.InitOnly, NullabilityExpectation.NotNull),
                Property("SegmentDuration", typeof(TimeSpan), SetterExpectation.InitOnly),
                Property("MaxDiskBytes", typeof(long), SetterExpectation.None)
            });
        AssertDeclaredProperties(
            sessionContextType,
            new[]
            {
                RequiredProperty("Width", typeof(int), SetterExpectation.InitOnly),
                RequiredProperty("Height", typeof(int), SetterExpectation.InitOnly),
                RequiredProperty("FrameRate", typeof(double), SetterExpectation.InitOnly),
                Property("FrameRateNumerator", typeof(int?), SetterExpectation.InitOnly),
                Property("FrameRateDenominator", typeof(int?), SetterExpectation.InitOnly),
                RequiredProperty("BitRate", typeof(uint), SetterExpectation.InitOnly),
                RequiredProperty("IsP010", typeof(bool), SetterExpectation.InitOnly),
                RequiredString("CodecName", SetterExpectation.InitOnly),
                String("NvencPreset", SetterExpectation.InitOnly, NullabilityExpectation.Nullable),
                String("SplitEncodeMode", SetterExpectation.InitOnly, NullabilityExpectation.NotNull),
                Property("HdrEnabled", typeof(bool), SetterExpectation.InitOnly),
                Property("IsFullRangeInput", typeof(bool), SetterExpectation.InitOnly),
                String("HdrMasterDisplayMetadata", SetterExpectation.InitOnly, NullabilityExpectation.Nullable),
                Property("HdrMaxCll", typeof(int), SetterExpectation.InitOnly),
                Property("HdrMaxFall", typeof(int), SetterExpectation.InitOnly),
                Property("D3D11DevicePtr", typeof(IntPtr), SetterExpectation.InitOnly),
                Property("D3D11DeviceContextPtr", typeof(IntPtr), SetterExpectation.InitOnly),
                Property("AudioEnabled", typeof(bool), SetterExpectation.InitOnly),
                Property("MicrophoneEnabled", typeof(bool), SetterExpectation.InitOnly)
            });
        AssertDeclaredProperties(
            exportProgressType,
            new[]
            {
                Property("SegmentsProcessed", typeof(int), SetterExpectation.InitOnly),
                Property("TotalSegments", typeof(int), SetterExpectation.InitOnly),
                Property("Percent", typeof(double), SetterExpectation.InitOnly)
            });
        AssertDeclaredProperties(
            exportSegmentType,
            new[]
            {
                RequiredString("Path", SetterExpectation.InitOnly),
                Property("StartPts", typeof(TimeSpan?), SetterExpectation.InitOnly),
                Property("EndPts", typeof(TimeSpan?), SetterExpectation.InitOnly)
            });
        AssertDeclaredProperties(
            exportRequestType,
            new[]
            {
                Property(
                    "Segments",
                    typeof(IReadOnlyList<>).MakeGenericType(exportSegmentType),
                    SetterExpectation.InitOnly,
                    NullabilityExpectation.Nullable,
                    NullabilityExpectation.NotNull),
                Property(
                    "SegmentPaths",
                    typeof(IReadOnlyList<string>),
                    SetterExpectation.InitOnly,
                    NullabilityExpectation.Nullable,
                    NullabilityExpectation.NotNull),
                String("InputTsPath", SetterExpectation.InitOnly, NullabilityExpectation.Nullable),
                RequiredProperty("InPoint", typeof(TimeSpan), SetterExpectation.InitOnly),
                RequiredProperty("OutPoint", typeof(TimeSpan), SetterExpectation.InitOnly),
                RequiredString("OutputPath", SetterExpectation.InitOnly),
                Property("FastStart", typeof(bool), SetterExpectation.InitOnly),
                Property("Force", typeof(bool), SetterExpectation.InitOnly),
                Property("AdaptiveThrottleDelayMsProvider", typeof(Func<int>), SetterExpectation.InitOnly, NullabilityExpectation.Nullable)
            });

        var bufferOptions = CreateInstance(bufferOptionsType);
        Assert.Equal(TimeSpan.FromMinutes(5), Get<TimeSpan>(bufferOptions, "BufferDuration"));
        Assert.Equal(TimeSpan.FromMinutes(10), Get<TimeSpan>(bufferOptions, "SegmentDuration"));
        Assert.Contains("Sussudio", Get<string>(bufferOptions, "TempDirectory"), StringComparison.Ordinal);
        Assert.Contains("Flashback", Get<string>(bufferOptions, "TempDirectory"), StringComparison.Ordinal);
        Assert.Equal(300L * 57L * 1024L * 1024L, Get<long>(bufferOptions, "MaxDiskBytes"));

        var sessionContext = CreateInstance(sessionContextType);
        Set(sessionContext, "Width", 3840);
        Set(sessionContext, "Height", 2160);
        Set(sessionContext, "FrameRate", 119.88d);
        Set(sessionContext, "FrameRateNumerator", 120000);
        Set(sessionContext, "FrameRateDenominator", 1001);
        Set(sessionContext, "BitRate", 150_000_000u);
        Set(sessionContext, "IsP010", true);
        Set(sessionContext, "CodecName", "hevc_nvenc");
        Set(sessionContext, "NvencPreset", "P5");
        Set(sessionContext, "SplitEncodeMode", "2-way");
        Set(sessionContext, "HdrEnabled", true);
        Set(sessionContext, "IsFullRangeInput", true);
        Set(sessionContext, "HdrMasterDisplayMetadata", "G(13250,34500)");
        Set(sessionContext, "HdrMaxCll", 1000);
        Set(sessionContext, "HdrMaxFall", 400);
        Set(sessionContext, "D3D11DevicePtr", new IntPtr(123));
        Set(sessionContext, "D3D11DeviceContextPtr", new IntPtr(456));
        Set(sessionContext, "AudioEnabled", true);
        Set(sessionContext, "MicrophoneEnabled", true);
        Assert.Equal(3840, Get<int>(sessionContext, "Width"));
        Assert.Equal("hevc_nvenc", Get<string>(sessionContext, "CodecName"));
        Assert.Equal("2-way", Get<string>(sessionContext, "SplitEncodeMode"));
        Assert.Equal(new IntPtr(456), Get<IntPtr>(sessionContext, "D3D11DeviceContextPtr"));

        var progress = Activator.CreateInstance(exportProgressType, 3, 10, 30d)!;
        Assert.Equal(3, Get<int>(progress, "SegmentsProcessed"));
        Assert.Equal(10, Get<int>(progress, "TotalSegments"));
        Assert.Equal(30d, Get<double>(progress, "Percent"));

        var exportSegment = CreateInstance(exportSegmentType);
        Set(exportSegment, "Path", "segment.mp4");
        Set(exportSegment, "StartPts", TimeSpan.FromSeconds(5));
        Set(exportSegment, "EndPts", TimeSpan.FromSeconds(15));
        Assert.Equal("segment.mp4", Get<string>(exportSegment, "Path"));
        Assert.Equal(TimeSpan.FromSeconds(5), Get<TimeSpan>(exportSegment, "StartPts"));
        Assert.Equal(TimeSpan.FromSeconds(15), Get<TimeSpan>(exportSegment, "EndPts"));

        var exportRequest = CreateInstance(exportRequestType);
        Assert.True(Get<bool>(exportRequest, "FastStart"));
        Assert.False(Get<bool>(exportRequest, "Force"));
        var exportSegments = Array.CreateInstance(exportSegmentType, 1);
        exportSegments.SetValue(exportSegment, 0);
        Set(exportRequest, "Segments", exportSegments);
        Set(exportRequest, "SegmentPaths", new[] { "a.ts", "b.ts" });
        Set(exportRequest, "InputTsPath", "single.ts");
        Set(exportRequest, "InPoint", TimeSpan.FromSeconds(2));
        Set(exportRequest, "OutPoint", TimeSpan.FromSeconds(12));
        Set(exportRequest, "OutputPath", "clip.mp4");
        Set(exportRequest, "FastStart", false);
        Assert.Equal(1, Count(Get(exportRequest, "Segments")!));
        Assert.Equal(2, Count(Get(exportRequest, "SegmentPaths")!));
        Assert.Equal("single.ts", Get<string>(exportRequest, "InputTsPath"));
        Assert.Equal(TimeSpan.FromSeconds(12), Get<TimeSpan>(exportRequest, "OutPoint"));
        Assert.False(Get<bool>(exportRequest, "FastStart"));
        Assert.Null(Get(exportRequest, "AdaptiveThrottleDelayMsProvider"));
    }

    [Fact]
    public void FlashbackPlaybackState_HasAllExpectedStates()
    {
        var enumType = RequireType(SussudioAssembly.Load(), "Sussudio.Models.FlashbackPlaybackState");

        Assert.Equal(
            new[] { "Disabled", "Buffering", "Live", "Scrubbing", "Playing", "Paused" },
            Enum.GetNames(enumType));
    }

}
