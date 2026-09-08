using System.Collections;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public sealed class DiagnosticCompositionTests
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    [Fact]
    public void RetainedAnalysisPreservesEverySerializedResultField()
    {
        using var expected = ReadFixture("retained-analysis.json");
        var actual = DiagnosticCompositionFixture.BuildResult(LoadAssembly());

        Assert.Equal(197, expected.RootElement.EnumerateObject().Count());
        AssertJsonEquivalent(expected.RootElement, actual);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PlaybackMetricExtractionPreservesObservedAndUnobservedResults(bool observed)
    {
        using var fixture = ReadFixture("playback-metrics.json");
        var actual = DiagnosticCompositionFixture.BuildPlaybackMetrics(LoadAssembly(), observed);

        AssertJsonEquivalent(fixture.RootElement.GetProperty(observed ? "observed" : "unobserved"), actual);
    }

    [Fact]
    public void EveryScenarioPreservesItsNameRequirementsAndWarningPolicies()
    {
        var assembly = LoadAssembly();
        var catalogType = assembly.GetType("Sussudio.Tools.DiagnosticSessionScenarioCatalog", throwOnError: true)!;
        var planType = assembly.GetType("Sussudio.Tools.DiagnosticSessionScenarioPlan", throwOnError: true)!;
        var entries = ((IEnumerable)catalogType.GetProperty("Entries", Members)!.GetValue(null)!).Cast<object>().ToArray();
        using var fixture = ReadFixture("scenarios.json");
        var expectedEntries = fixture.RootElement.EnumerateArray().ToArray();
        Assert.Equal(25, entries.Length);
        Assert.Equal(expectedEntries.Length, entries.Length);
        var constructor = Assert.Single(planType.GetConstructors(Members));
        var identity = Assert.Single(constructor.GetParameters());
        Assert.True(identity.ParameterType.IsEnum);

        for (var i = 0; i < expectedEntries.Length; i++)
        {
            var expected = expectedEntries[i];
            var entry = entries[i];
            var plan = entry.GetType().GetProperty("Plan", Members)!.GetValue(entry)!;
            var actual = new Dictionary<string, object?>();
            foreach (var property in expected.EnumerateObject())
            {
                var entryProperty = entry.GetType().GetProperty(property.Name, Members);
                actual[property.Name] = entryProperty != null
                    ? entryProperty.GetValue(entry)
                    : planType.GetProperty(property.Name, Members)!.GetValue(plan);
            }
            AssertJsonEquivalent(expected, JsonSerializer.SerializeToElement(actual), $"scenario[{i}]");

            var name = expected.GetProperty("Name").GetString()!;
            var expectedKind = string.Concat(name.Split('-').Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
            var parsedPlan = planType.GetMethod("From", Members)!.Invoke(null, new object[] { name })!;
            Assert.Equal(expectedKind, planType.GetProperty("Kind", Members)!.GetValue(parsedPlan)!.ToString());
        }
    }

    private static Assembly LoadAssembly()
        => ToolFormatterTestAssembly.Load(global::Program.SsctlAssemblyRelativePath);

    private static JsonDocument ReadFixture(string name)
        => JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RuntimeContractSource.GetRepoRoot(), "tests", "Sussudio.Tests", "Fixtures", "DiagnosticComposition", name)));

    private static void AssertJsonEquivalent(JsonElement expected, JsonElement actual, string path = "$")
    {
        Assert.True(expected.ValueKind == actual.ValueKind, $"{path}: expected {expected.ValueKind}, got {actual.ValueKind}");
        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                var expectedProperties = expected.EnumerateObject().ToArray();
                var actualProperties = actual.EnumerateObject().ToArray();
                Assert.Equal(expectedProperties.Select(p => p.Name).OrderBy(n => n), actualProperties.Select(p => p.Name).OrderBy(n => n));
                foreach (var property in expectedProperties)
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