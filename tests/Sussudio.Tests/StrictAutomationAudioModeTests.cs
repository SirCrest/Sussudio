using System;
using System.IO;
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

    [Fact]
    public void DispatcherAndViewModel_ValidateBeforeAnyMutationBoundary()
    {
        var dispatcher = ReadMethod(
            RepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs"),
            "private async Task<AutomationCommandResponse> ExecuteSetDeviceAudioModeCommandAsync");
        AssertOccursBefore(
            dispatcher,
            "DeviceAudioModeParser.NormalizeOrThrow(RequireString(payload, \"mode\"))",
            "_audioPort.SetDeviceAudioModeAsync(mode, cancellationToken)");
        Assert.Contains("Device audio mode changed: {mode}.", dispatcher, StringComparison.Ordinal);

        var viewModel = ReadMethod(
            RepoFile("Sussudio/ViewModels/MainViewModel.cs"),
            "public Task SetDeviceAudioModeAsync");
        AssertOccursBefore(
            viewModel,
            "var normalizedMode = DeviceAudioModeParser.NormalizeOrThrow(mode);",
            "return InvokeOnUiThreadAsync");
        AssertOccursBefore(
            viewModel,
            "return InvokeOnUiThreadAsync",
            "SelectedDeviceAudioMode = normalizedMode");
        AssertOccursBefore(
            viewModel,
            "SelectedDeviceAudioMode = normalizedMode",
            "ApplyDeviceAudioModeAsync(");
        AssertOccursBefore(viewModel, "ApplyDeviceAudioModeAsync(", "SaveSettingsOrThrow();");
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

    private static string RepoFile(string relativePath)
        => File.ReadAllText(Path.Combine(FindRepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sussudio.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not locate repository root from '{AppContext.BaseDirectory}'.");
    }

    private static string ReadMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Could not find method '{signature}'.");
        var openBrace = source.IndexOf('{', start);
        Assert.True(openBrace >= 0, $"Could not find opening brace for '{signature}'.");

        var depth = 0;
        for (var index = openBrace; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}' && --depth == 0)
            {
                return source[start..(index + 1)];
            }
        }

        throw new InvalidOperationException($"Could not find closing brace for '{signature}'.");
    }

    private static void AssertOccursBefore(string source, string first, string second)
    {
        var firstIndex = source.IndexOf(first, StringComparison.Ordinal);
        var secondIndex = source.IndexOf(second, StringComparison.Ordinal);
        Assert.True(firstIndex >= 0, $"Missing expected text: {first}");
        Assert.True(secondIndex >= 0, $"Missing expected text: {second}");
        Assert.True(firstIndex < secondIndex, $"Expected '{first}' before '{second}'.");
    }
}
