using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public sealed class AutomationSnapshotRegressionTests
{
    [Theory]
    [InlineData(false, "defaults.json")]
    [InlineData(true, "populated.json")]
    public void CapturedInputsPreserveEverySerializedSnapshotField(bool populated, string fixtureName)
    {
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RuntimeContractSource.GetRepoRoot(), "tests", "Sussudio.Tests", "Fixtures", "AutomationSnapshot", fixtureName)));
        var actual = AutomationSnapshotRegressionFixture.BuildResult(SussudioAssembly.Load(), populated);

        Assert.Equal(807, expected.RootElement.EnumerateObject().Count());
        AssertJsonEquivalent(expected.RootElement, actual, "$");
    }

    [Theory]
    [InlineData("defaults.json")]
    [InlineData("populated.json")]
    public void TimelinePreservesEveryCapturedField(string fixtureName)
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RuntimeContractSource.GetRepoRoot(), "tests", "Sussudio.Tests", "Fixtures", "PerformanceTimeline", fixtureName)));
        var assembly = SussudioAssembly.Load();
        var snapshotType = assembly.GetType("Sussudio.Models.AutomationSnapshot", throwOnError: true)!;
        var timelineType = assembly.GetType("Sussudio.Models.PerformanceTimelineEntry", throwOnError: true)!;
        var builder = assembly.GetType("Sussudio.Services.Automation.AutomationDiagnosticsHub", throwOnError: true)!
            .GetMethod("BuildPerformanceTimelineEntry", BindingFlags.NonPublic | BindingFlags.Static)!;
        var snapshot = JsonSerializer.Deserialize(fixture.RootElement.GetProperty("snapshot"), snapshotType)!;
        var actual = JsonSerializer.SerializeToElement(builder.Invoke(null, new[] { snapshot }), timelineType);
        var expected = fixture.RootElement.GetProperty("timeline");

        Assert.Equal(159, expected.EnumerateObject().Count());
        AssertJsonEquivalent(expected, actual, "$.timeline");
    }

    private static void AssertJsonEquivalent(JsonElement expected, JsonElement actual, string path)
    {
        Assert.True(expected.ValueKind == actual.ValueKind,
            $"{path}: expected {expected.ValueKind}, got {actual.ValueKind}");
        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                var properties = expected.EnumerateObject().ToArray();
                Assert.Equal(properties.Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal),
                    actual.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal));
                foreach (var property in properties)
                    AssertJsonEquivalent(property.Value, actual.GetProperty(property.Name), path + "." + property.Name);
                break;
            case JsonValueKind.Array:
                Assert.Equal(expected.GetArrayLength(), actual.GetArrayLength());
                for (var i = 0; i < expected.GetArrayLength(); i++)
                    AssertJsonEquivalent(expected[i], actual[i], $"{path}[{i}]");
                break;
            case JsonValueKind.Number:
                Assert.True(expected.GetDecimal() == actual.GetDecimal(), $"{path}: expected {expected}, got {actual}");
                break;
            case JsonValueKind.String:
                Assert.True(expected.GetString() == actual.GetString(), $"{path}: expected {expected}, got {actual}");
                break;
        }
    }
}
