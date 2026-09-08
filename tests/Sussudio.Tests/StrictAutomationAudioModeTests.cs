using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class StrictAutomationAudioModeTests
{
    public StrictAutomationAudioModeTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Theory]
    [InlineData("HDMI", "HDMI")]
    [InlineData("hdmi", "HDMI")]
    [InlineData("hDmI", "HDMI")]
    [InlineData("Analog", "Analog")]
    [InlineData("analog", "Analog")]
    [InlineData("ANALOG", "Analog")]
    public void Parser_AcceptsOnlyCaseVariants_AndReturnsCanonicalValue(string input, string expected)
    {
        Assert.Equal(expected, Parse(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("HDMI ")]
    [InlineData("Embedded")]
    public void Parser_RejectsEmptyOrUnknownValues(string? input)
    {
        var exception = Assert.Throws<TargetInvocationException>(() => Parse(input));
        var validationException = Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("Device audio mode must be either 'HDMI' or 'Analog'.", validationException.Message);
    }

    private static string Parse(string? value)
    {
        var parserType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("Sussudio.Models.DeviceAudioModeParser", throwOnError: false))
            .FirstOrDefault(type => type != null)
            ?? throw new InvalidOperationException("DeviceAudioModeParser was not found in the staged Sussudio assembly.");
        var parse = parserType.GetMethod(
            "NormalizeOrThrow",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DeviceAudioModeParser.NormalizeOrThrow was not found.");
        return (string)parse.Invoke(null, new object?[] { value })!;
    }
}
