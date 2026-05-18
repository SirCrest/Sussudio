using System.Collections;
using Xunit;

namespace Sussudio.Tests;

public partial class CaptureConfigurationModelsTests
{
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

}
