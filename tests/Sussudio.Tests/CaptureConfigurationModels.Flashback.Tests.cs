using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
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
                ConfigProperty("Force", typeof(bool), ConfigSetterExpectation.InitOnly),
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
        AssertEqual(false, GetBoolProperty(exportRequest, "Force"), "FlashbackExportRequest.Force default");
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
}
