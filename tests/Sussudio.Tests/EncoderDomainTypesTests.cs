using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public sealed class EncoderDomainTypesTests
{
    [Theory]
    [InlineData("NvencPreset", "NvencPresetParser")]
    [InlineData("SplitEncodeMode", "SplitEncodeModeParser")]
    public void SettingParsersKeepDefinedNamesAndNumericValues(string enumName, string parserName)
    {
        foreach (var value in Enum.GetValues(RequireType($"Sussudio.Models.{enumName}")))
        {
            var name = value.ToString()!;
            var numeric = Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            foreach (var input in new[] { name, name.ToLowerInvariant(), numeric })
            {
                Assert.Equal(value, ParseSetting(parserName, input));
            }
        }
    }

    [Theory]
    [InlineData("NvencPresetParser")]
    [InlineData("SplitEncodeModeParser")]
    public void SettingParsersUseExistingAutoDefaultForMissingOrUnknownValues(string parserName)
    {
        foreach (var input in new string?[] { null, "", "  ", "not-a-setting", "999", "-999" })
        {
            Assert.Equal("Auto", ParseSetting(parserName, input).ToString());
        }
    }

    [Theory]
    [InlineData("2-way", "TwoWay")]
    [InlineData("2-WAY", "TwoWay")]
    [InlineData("2", "TwoWay")]
    [InlineData("3-way", "ThreeWay")]
    [InlineData("3-WAY", "ThreeWay")]
    [InlineData("3", "ThreeWay")]
    public void SplitParserKeepsWireAliases(string input, string expected)
    {
        var mode = ParseSetting("SplitEncodeModeParser", input);
        Assert.Equal(expected, mode.ToString());
        var wireValue = RequireType("Sussudio.Models.SplitEncodeModeParser")
            .GetMethod("ToWireString")!.Invoke(null, new[] { mode });
        Assert.Equal(expected == "TwoWay" ? "2-way" : "3-way", wireValue);
    }

    [Theory]
    [InlineData("ParseWindowAction", "AutomationWindowAction", "action")]
    [InlineData("ParseFlashbackAction", "AutomationFlashbackAction", "action")]
    [InlineData("ParseWaitCondition", "AutomationWaitCondition", "condition")]
    public void AutomationParsersKeepDefinedNamesAndNumericValues(string parserName, string enumName, string propertyName)
    {
        foreach (var value in Enum.GetValues(RequireType($"Sussudio.Models.{enumName}")))
        {
            var name = value.ToString()!;
            var numeric = Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            foreach (var input in new[] { name, name.ToLowerInvariant(), numeric })
            {
                Assert.Equal(value, ParseAutomationValue(parserName, propertyName, input));
            }
        }
    }

    [Theory]
    [InlineData("ParseWindowAction", "action")]
    [InlineData("ParseFlashbackAction", "action")]
    [InlineData("ParseWaitCondition", "condition")]
    public void AutomationParsersRejectUndefinedValues(string parserName, string propertyName)
    {
        foreach (var input in new[] { "999", "-999", "not-a-value" })
        {
            var exception = Assert.Throws<TargetInvocationException>(() => ParseAutomationValue(parserName, propertyName, input));
            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }
    }

    [Theory]
    [InlineData("go-live", "GoLive")]
    [InlineData("GO_LIVE", "GoLive")]
    [InlineData(" begin-scrub ", "BeginScrub")]
    [InlineData("clear_in_out_points", "ClearInOutPoints")]
    public void FlashbackParserKeepsActionAliases(string input, string expected)
        => Assert.Equal(expected, ParseAutomationValue("ParseFlashbackAction", "action", input).ToString());

    [Fact]
    public void FlashbackParserStillRequiresAnAction()
    {
        using var payload = JsonDocument.Parse("{}");
        var exception = Assert.Throws<TargetInvocationException>(() => AutomationParser("ParseFlashbackAction")
            .Invoke(null, new object[] { payload.RootElement }));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Theory]
    [InlineData("Auto", 0L)]
    [InlineData("Disabled", 15L)]
    [InlineData("TwoWay", 2L)]
    [InlineData("ThreeWay", 3L)]
    public void SplitModesMapToTheirNativeValues(string modeName, long expected)
    {
        var mode = Enum.Parse(RequireType("Sussudio.Models.SplitEncodeMode"), modeName);
        var arguments = new object?[] { mode, null };
        var mapped = EncoderMethod("TryMapSplitEncodeMode").Invoke(null, arguments);
        Assert.Equal(true, mapped);
        Assert.Equal(expected, Assert.IsType<long>(arguments[1]));
    }

    [Theory]
    [InlineData("ForcedOn")]
    [InlineData("999")]
    [InlineData("-1")]
    public void UnsupportedSplitModesStayRejected(string modeName)
    {
        var mode = Enum.Parse(RequireType("Sussudio.Models.SplitEncodeMode"), modeName);
        Assert.Equal(false, EncoderMethod("TryMapSplitEncodeMode").Invoke(null, new object?[] { mode, null }));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public void UndefinedPresetsDoNotFallBackToAnotherPreset(int value)
    {
        var preset = Enum.ToObject(RequireType("Sussudio.Models.NvencPreset"), value);
        var exception = Assert.Throws<TargetInvocationException>(() => EncoderMethod("MapNvencPreset")
            .Invoke(null, new[] { preset }));
        Assert.IsType<ArgumentOutOfRangeException>(exception.InnerException);
    }

    [Theory]
    [InlineData("Sussudio.Services.Recording.LibAvEncoderOptions")]
    [InlineData("Sussudio.Models.FlashbackSessionContext")]
    public void InternalOptionsDefaultToAuto(string typeName)
    {
        var options = Create(typeName);
        AssertSettingValues(options, "Auto", "Auto");
    }

    [Theory]
    [InlineData("P5", "TwoWay")]
    [InlineData("Fast", "Disabled")]
    [InlineData("Auto", "Auto")]
    public void RecordingAndFlashbackKeepTheSelectedDomainValues(string presetName, string modeName)
    {
        var settings = Create("Sussudio.Models.CaptureSettings");
        Set(settings, "NvencPreset", Enum.Parse(RequireType("Sussudio.Models.NvencPreset"), presetName));
        Set(settings, "SplitEncodeMode", Enum.Parse(RequireType("Sussudio.Models.SplitEncodeMode"), modeName));

        var recordingContext = Create("Sussudio.Services.Contracts.RecordingContext");
        Set(recordingContext, "Settings", settings);
        Set(recordingContext, "EffectiveWidth", 1920u);
        Set(recordingContext, "EffectiveHeight", 1080u);
        Set(recordingContext, "EffectiveFrameRate", 60d);
        Set(recordingContext, "FrameRateArg", "60");
        Set(recordingContext, "VideoOutputPath", "recording.mp4");
        Set(recordingContext, "FinalOutputPath", "recording.mp4");

        // These construction methods only project options; no encoder or device is opened.
        var recordingSinkType = RequireType("Sussudio.Services.Recording.LibAvRecordingSink");
        using var recordingSink = (IDisposable)Create("Sussudio.Services.Recording.LibAvRecordingSink");
        var recordingOptions = recordingSinkType.GetMethod("CreateOptions", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(recordingSink, new[] { recordingContext })!;
        AssertSettingValues(recordingOptions, presetName, modeName);

        var flashbackSinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var flashbackContext = flashbackSinkType.GetMethod("CreateSessionContext", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new[] { recordingContext })!;
        AssertSettingValues(flashbackContext, presetName, modeName);

        var flashbackOptions = flashbackSinkType.GetMethod("CreateOptions", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { flashbackContext, "segment.ts" })!;
        AssertSettingValues(flashbackOptions, presetName, modeName);
    }

    private static void AssertSettingValues(object options, string presetName, string modeName)
    {
        Assert.Equal(Enum.Parse(RequireType("Sussudio.Models.NvencPreset"), presetName),
            options.GetType().GetProperty("NvencPreset")!.GetValue(options));
        Assert.Equal(Enum.Parse(RequireType("Sussudio.Models.SplitEncodeMode"), modeName),
            options.GetType().GetProperty("SplitEncodeMode")!.GetValue(options));
    }

    private static object ParseSetting(string parserName, string? value)
        => RequireType($"Sussudio.Models.{parserName}").GetMethod("Parse")!.Invoke(null, new object?[] { value })!;

    private static object ParseAutomationValue(string parserName, string propertyName, string value)
    {
        using var payload = JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, string> { [propertyName] = value }));
        return AutomationParser(parserName).Invoke(null, new object[] { payload.RootElement })!;
    }

    private static MethodInfo AutomationParser(string name)
        => RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher").GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;

    private static MethodInfo EncoderMethod(string name)
        => RequireType("Sussudio.Services.Recording.LibAvEncoder").GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;

    private static Type RequireType(string name)
        => SussudioAssembly.Load().GetType(name, throwOnError: true)!;

    private static object Create(string name)
        => Activator.CreateInstance(RequireType(name), nonPublic: true)!;

    private static void Set(object instance, string propertyName, object value)
        => instance.GetType().GetProperty(propertyName)!.SetValue(instance, value);
}
