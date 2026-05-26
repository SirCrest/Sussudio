using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sussudio.Tests;

public partial class CaptureConfigurationModelsTests
{
    private enum SetterExpectation
    {
        Set,
        InitOnly,
        None
    }

    private enum NullabilityExpectation
    {
        NotApplicable,
        NotNull,
        Nullable
    }

    private enum PropertyScope
    {
        Instance,
        Static
    }

    private sealed record PropertySpec(
        string Name,
        Type Type,
        SetterExpectation Setter,
        NullabilityExpectation Nullability = NullabilityExpectation.NotApplicable,
        NullabilityExpectation ElementNullability = NullabilityExpectation.NotApplicable,
        PropertyScope Scope = PropertyScope.Instance,
        bool IsRequired = false);


    private static PropertySpec Property(
        string name,
        Type type,
        SetterExpectation setter,
        NullabilityExpectation nullability = NullabilityExpectation.NotApplicable,
        NullabilityExpectation elementNullability = NullabilityExpectation.NotApplicable,
        PropertyScope scope = PropertyScope.Instance,
        bool isRequired = false)
        => new(name, type, setter, nullability, elementNullability, scope, isRequired);

    private static PropertySpec String(string name, SetterExpectation setter, NullabilityExpectation nullability)
        => Property(name, typeof(string), setter, nullability);

    private static PropertySpec RequiredString(string name, SetterExpectation setter)
        => Property(name, typeof(string), setter, NullabilityExpectation.NotNull, isRequired: true);

    private static PropertySpec RequiredProperty(string name, Type type, SetterExpectation setter)
        => Property(name, type, setter, isRequired: true);

    private static void AssertDeclaredProperties(Type type, IReadOnlyCollection<PropertySpec> expectedProperties)
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
        Assert.Equal(expectedNames, actualNames);

        foreach (var expected in expectedProperties)
        {
            var flags = expected.Scope == PropertyScope.Static ? staticFlags : instanceFlags;
            var property = type.GetProperty(expected.Name, flags);
            Assert.NotNull(property);
            Assert.Equal(expected.Type, property!.PropertyType);
            Assert.Equal(expected.IsRequired, property.GetCustomAttribute<RequiredMemberAttribute>() != null);
            Assert.NotNull(property.GetMethod);
            Assert.True(property.GetMethod!.IsPublic);

            if (expected.Setter == SetterExpectation.None)
            {
                Assert.Null(property.SetMethod);
            }
            else
            {
                Assert.NotNull(property.SetMethod);
                Assert.True(property.SetMethod!.IsPublic);
                Assert.Equal(expected.Setter == SetterExpectation.InitOnly, IsInitOnlySetter(property));
            }

            AssertNullability(type, property, expected);
        }
    }

    private static void AssertNullability(Type type, PropertyInfo property, PropertySpec expected)
    {
        if (expected.Nullability == NullabilityExpectation.NotApplicable)
        {
            return;
        }

        var nullability = new NullabilityInfoContext().Create(property);
        var expectedState = expected.Nullability == NullabilityExpectation.Nullable
            ? NullabilityState.Nullable
            : NullabilityState.NotNull;
        Assert.Equal(expectedState, nullability.ReadState);
        if (expected.Setter != SetterExpectation.None)
        {
            Assert.Equal(expectedState, nullability.WriteState);
        }

        if (expected.ElementNullability == NullabilityExpectation.NotApplicable)
        {
            return;
        }

        var elementNullability = property.PropertyType.IsArray
            ? nullability.ElementType
            : nullability.GenericTypeArguments.FirstOrDefault();
        Assert.NotNull(elementNullability);
        var expectedElementState = expected.ElementNullability == NullabilityExpectation.Nullable
            ? NullabilityState.Nullable
            : NullabilityState.NotNull;
        Assert.Equal(expectedElementState, elementNullability!.ReadState);
        Assert.Equal(expectedElementState, elementNullability.WriteState);
    }

    private static bool IsInitOnlySetter(PropertyInfo property)
        => property.SetMethod?.ReturnParameter.GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit") == true;

    private static Type RequireType(Assembly asm, string name)
        => asm.GetType(name, throwOnError: true)!;

    private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags)
    {
        var method = type.GetMethod(name, flags);
        Assert.NotNull(method);
        return method!;
    }

    private static object CreateInstance(Type type)
        => Activator.CreateInstance(type, nonPublic: true)!;

    private static object? Get(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance | ReflectionFlags.Static)!.GetValue(instance);

    private static T Get<T>(object instance, string propertyName)
        => (T)Get(instance, propertyName)!;

    private static void Set(object instance, string propertyName, object? value)
    {
        var type = instance.GetType();
        var property = type.GetProperty(propertyName, ReflectionFlags.Instance | ReflectionFlags.Static);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = type.GetField($"<{propertyName}>k__BackingField", ReflectionFlags.Instance | ReflectionFlags.Static)
            ?? type.GetField($"_{char.ToLowerInvariant(propertyName[0])}{propertyName[1..]}", ReflectionFlags.Instance | ReflectionFlags.Static)
            ?? type.GetField(propertyName, ReflectionFlags.Instance | ReflectionFlags.Static);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static object? Invoke(object instance, string methodName)
        => RequireMethod(instance.GetType(), methodName, ReflectionFlags.Instance).Invoke(instance, Array.Empty<object>());

    private static object ParseEnum(Assembly asm, string typeName, string value)
        => Enum.Parse(RequireType(asm, typeName), value);

    private static void AssertEnumValues(Type enumType, params (string Name, int Value)[] expectedValues)
    {
        Assert.Equal(expectedValues.Length, Enum.GetNames(enumType).Length);
        foreach (var (name, value) in expectedValues)
        {
            Assert.Equal(value, Convert.ToInt32(Enum.Parse(enumType, name)));
        }
    }

    [Fact]
    public void CaptureModeOptions_PreserveDisplayTextAndMetadata()
    {
        var asm = SussudioAssembly.Load();
        var resolutionType = RequireType(asm, "Sussudio.Models.ResolutionOption");
        var frameRateType = RequireType(asm, "Sussudio.Models.FrameRateOption");

        AssertDeclaredProperties(
            resolutionType,
            new[]
            {
                RequiredString("Value", SetterExpectation.InitOnly),
                RequiredProperty("Width", typeof(uint), SetterExpectation.InitOnly),
                RequiredProperty("Height", typeof(uint), SetterExpectation.InitOnly),
                Property("IsEnabled", typeof(bool), SetterExpectation.InitOnly),
                String("DisableReason", SetterExpectation.InitOnly, NullabilityExpectation.NotNull),
                String("DisplayTextOverride", SetterExpectation.InitOnly, NullabilityExpectation.Nullable),
                String("DisplayText", SetterExpectation.None, NullabilityExpectation.NotNull)
            });
        AssertDeclaredProperties(
            frameRateType,
            new[]
            {
                RequiredProperty("FriendlyValue", typeof(double), SetterExpectation.InitOnly),
                RequiredProperty("Value", typeof(double), SetterExpectation.InitOnly),
                String("Rational", SetterExpectation.InitOnly, NullabilityExpectation.NotNull),
                Property("Numerator", typeof(uint?), SetterExpectation.InitOnly),
                Property("Denominator", typeof(uint?), SetterExpectation.InitOnly),
                Property("IsEnabled", typeof(bool), SetterExpectation.InitOnly),
                String("DisableReason", SetterExpectation.InitOnly, NullabilityExpectation.NotNull),
                String("DisplayTextOverride", SetterExpectation.InitOnly, NullabilityExpectation.Nullable),
                String("DisplayText", SetterExpectation.None, NullabilityExpectation.NotNull)
            });

        var resolution = CreateInstance(resolutionType);
        Set(resolution, "Value", "3840x2160");
        Set(resolution, "Width", 3840u);
        Set(resolution, "Height", 2160u);
        Assert.Equal("3840x2160", Get<string>(resolution, "DisplayText"));
        Assert.Equal(string.Empty, Get<string>(resolution, "DisableReason"));

        Set(resolution, "DisplayTextOverride", "4K UHD");
        Assert.Equal("4K UHD", Get<string>(resolution, "DisplayText"));
        Set(resolution, "DisplayTextOverride", "   ");
        Assert.Equal("3840x2160", Get<string>(resolution, "DisplayText"));

        var frameRate = CreateInstance(frameRateType);
        Set(frameRate, "FriendlyValue", 59.94d);
        Set(frameRate, "Value", 60000d / 1001d);
        Set(frameRate, "Rational", "60000/1001");
        Set(frameRate, "Numerator", 60000u);
        Set(frameRate, "Denominator", 1001u);
        Assert.Equal("60", Get<string>(frameRate, "DisplayText"));
        Set(frameRate, "DisplayTextOverride", "59.94");
        Assert.Equal("59.94", Get<string>(frameRate, "DisplayText"));
        Assert.Equal("60000/1001", Get<string>(frameRate, "Rational"));
    }

    [Fact]
    public void CaptureModeOptionsBuilder_BuildsResolutionAndVideoFormatOptions()
    {
        var asm = SussudioAssembly.Load();
        var builderType = RequireType(asm, "Sussudio.ViewModels.CaptureModeOptionsBuilder");
        var mediaFormatType = RequireType(asm, "Sussudio.Models.MediaFormat");
        var telemetryType = RequireType(asm, "Sussudio.Models.SourceSignalTelemetrySnapshot");
        var buildResolutionOptions = RequireMethod(builderType, "BuildResolutionOptions", ReflectionFlags.Static);
        var buildVideoFormatOptions = RequireMethod(builderType, "BuildVideoFormatOptions", ReflectionFlags.Static);

        var formatsByResolution = CreateResolutionFormatDictionary(mediaFormatType);
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "3840x2160",
            CreateMediaFormat(mediaFormatType, 3840, 2160, 60, "P010", isHdr: true));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1920x1080",
            CreateMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1280x1024",
            CreateMediaFormat(mediaFormatType, 1280, 1024, 60, "P010", isHdr: true));

        var telemetry = CreateInstance(telemetryType);
        Set(telemetry, "Width", 1920);
        Set(telemetry, "Height", 1080);

        var filteredOptions = ((IEnumerable)buildResolutionOptions.Invoke(
                null,
                new object?[] { formatsByResolution, true, false, telemetry })!)
            .Cast<object>()
            .ToArray();
        Assert.Equal(2, filteredOptions.Length);
        Assert.Equal("3840x2160", Get<string>(filteredOptions[0], "Value"));
        Assert.True(Get<bool>(filteredOptions[0], "IsEnabled"));
        var sdrOnlyResolution = filteredOptions.Single(option => Get<string>(option, "Value") == "1920x1080");
        Assert.False(Get<bool>(sdrOnlyResolution, "IsEnabled"));
        Assert.Equal("HDR mode is not supported at this resolution.", Get<string>(sdrOnlyResolution, "DisableReason"));

        var unfilteredOptions = ((IEnumerable)buildResolutionOptions.Invoke(
                null,
                new object?[] { formatsByResolution, true, true, telemetry })!)
            .Cast<object>()
            .ToArray();
        Assert.Contains(unfilteredOptions, option => Get<string>(option, "Value") == "1280x1024");

        var videoFormats = CreateMediaFormatList(
            mediaFormatType,
            CreateMediaFormat(mediaFormatType, 3840, 2160, 120, "mjpg", isHdr: false),
            CreateMediaFormat(mediaFormatType, 3840, 2160, 120, "NV12", isHdr: false),
            CreateMediaFormat(mediaFormatType, 3840, 2160, 120, "nv12", isHdr: false),
            CreateMediaFormat(mediaFormatType, 3840, 2160, 60, "P010", isHdr: true),
            CreateMediaFormat(mediaFormatType, 3840, 2160, 60, " ", isHdr: false));
        var videoOptions = ((IEnumerable)buildVideoFormatOptions.Invoke(null, new object?[] { videoFormats })!)
            .Cast<string>()
            .ToArray();
        Assert.Equal(new[] { "Auto", "NV12", "MJPG", "P010" }, videoOptions);
    }

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

    private static object CreateResolutionFormatDictionary(Type mediaFormatType)
        => Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(
               typeof(string),
               typeof(List<>).MakeGenericType(mediaFormatType)))!;

    private static void AddResolutionFormats(object formatsByResolution, Type mediaFormatType, string resolutionKey, params object[] formats)
        => ((IDictionary)formatsByResolution).Add(resolutionKey, CreateMediaFormatList(mediaFormatType, formats));

    private static object CreateMediaFormatList(Type mediaFormatType, params object[] formats)
    {
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(mediaFormatType))!;
        foreach (var format in formats)
        {
            list.Add(format);
        }

        return list;
    }

    private static object CreateMediaFormat(Type mediaFormatType, uint width, uint height, double frameRate, string pixelFormat, bool isHdr)
    {
        var format = CreateInstance(mediaFormatType);
        Set(format, "Width", width);
        Set(format, "Height", height);
        Set(format, "FrameRate", frameRate);
        Set(format, "PixelFormat", pixelFormat);
        Set(format, "IsHdr", isHdr);
        return format;
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
