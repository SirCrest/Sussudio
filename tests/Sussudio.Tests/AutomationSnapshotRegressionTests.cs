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

    [Fact]
    public void PopulatedInputsExerciseComputedValuesAndDistinctCounterSources()
    {
        var snapshot = AutomationSnapshotRegressionFixture.BuildResult(SussudioAssembly.Load(), populated: true);

        Assert.True(snapshot.GetProperty("VerificationInProgress").GetBoolean());
        Assert.Equal(0.125, snapshot.GetProperty("PerformanceThresholdCaptureDropPercent").GetDouble());
        Assert.Equal(1.875, snapshot.GetProperty("PerformanceThresholdCaptureP95Multiplier").GetDouble());
        Assert.Equal(2.625, snapshot.GetProperty("PerformanceThresholdPreviewSlowPercent").GetDouble());
        Assert.Equal(3.375, snapshot.GetProperty("PerformanceThresholdVerificationDropPercent").GetDouble());
        Assert.Equal(120.0, snapshot.GetProperty("SelectedFriendlyFrameRate").GetDouble());
        Assert.Equal(119.88, snapshot.GetProperty("SelectedExactFrameRate").GetDouble());
        Assert.Equal(37, snapshot.GetProperty("SourceTelemetryAgeSeconds").GetInt32());
        Assert.Equal("captureRuntime.SourceTelemetryAvailability", snapshot.GetProperty("SourceTelemetryAvailability").GetString());
        Assert.Equal("captureRuntime.SourceTelemetryOriginDetail", snapshot.GetProperty("SourceTelemetryOriginDetail").GetString());
        Assert.Equal("captureRuntime.SourceFrameRateOrigin", snapshot.GetProperty("SourceFrameRateOrigin").GetString());
        Assert.Equal(snapshot.GetProperty("ActualWidth").GetUInt32(), snapshot.GetProperty("NegotiatedWidth").GetUInt32());
        Assert.NotEqual(snapshot.GetProperty("RequestedWidth").GetUInt32(), snapshot.GetProperty("ActualWidth").GetUInt32());
        Assert.NotEqual(snapshot.GetProperty("ActualHeight").GetUInt32(), snapshot.GetProperty("NegotiatedHeight").GetUInt32());
        Assert.Equal(25011L, snapshot.GetProperty("EncoderVideoFramesEncoded").GetInt64());
        Assert.Equal(17007L, snapshot.GetProperty("EncoderVideoFramesEnqueued").GetInt64());
        Assert.Equal(28L, snapshot.GetProperty("AudioQueueDropsRealtime").GetInt64());
        Assert.Equal(19L, snapshot.GetProperty("AudioQueueDropsFileWriter").GetInt64());
        Assert.Equal(12L, snapshot.GetProperty("EstimatedPipelineLatencyMs").GetInt64());
        Assert.Equal(33004L, snapshot.GetProperty("RecordingTotalBytes").GetInt64());
        Assert.Equal(41L, snapshot.GetProperty("PreviewD3DFrameStatsRecentMissedRefreshCount").GetInt64());
        Assert.Equal(43L, snapshot.GetProperty("PreviewD3DFrameStatsRecentFailureCount").GetInt64());
        Assert.Equal(59L, snapshot.GetProperty("PreviewD3DFrameStatsMissedRefreshCount").GetInt64());
        Assert.Equal(61L, snapshot.GetProperty("PreviewD3DFrameStatsFailureCount").GetInt64());
        Assert.Equal("NotAttempted", snapshot.GetProperty("MuxResult").GetString());
        Assert.Equal("health-export-verification", snapshot.GetProperty("FlashbackExportVerificationFormat").GetString());
        Assert.Equal(string.Empty, snapshot.GetProperty("FlashbackCodecDowngradeReason").GetString());
        Assert.False(snapshot.GetProperty("LastExportSuccess").GetBoolean());
        Assert.Equal(2, snapshot.GetProperty("MjpegPerDecoder").GetArrayLength());
        Assert.Equal(2, snapshot.GetProperty("PreviewD3DRecentSlowFrames").GetArrayLength());
        Assert.NotEqual(snapshot.GetProperty("RecordingVideoQueueLatencyP95Ms").GetDouble(),
            snapshot.GetProperty("FlashbackVideoQueueLatencyP95Ms").GetDouble());
        Assert.NotEqual(snapshot.GetProperty("VisualCadenceOutputObservedFps").GetDouble(),
            snapshot.GetProperty("VisualCenterCadenceOutputObservedFps").GetDouble());
    }

    [Fact]
    public void DefaultInputsKeepNullableResultsAndRequiredNormalization()
    {
        var snapshot = AutomationSnapshotRegressionFixture.BuildResult(SussudioAssembly.Load(), populated: false);

        Assert.False(snapshot.GetProperty("VerificationInProgress").GetBoolean());
        Assert.Equal(JsonValueKind.Null, snapshot.GetProperty("LastExportSuccess").ValueKind);
        Assert.Equal(JsonValueKind.Null, snapshot.GetProperty("LastExportPath").ValueKind);
        Assert.Equal(JsonValueKind.Null, snapshot.GetProperty("LastVerification").ValueKind);
        Assert.Equal(JsonValueKind.Null, snapshot.GetProperty("SourceTelemetryAgeSeconds").ValueKind);
        Assert.Equal(JsonValueKind.Null, snapshot.GetProperty("CaptureCadenceRecentIntervalsMs").ValueKind);
        Assert.Equal(0, snapshot.GetProperty("MjpegPerDecoder").GetArrayLength());
        Assert.Equal("NotAttempted", snapshot.GetProperty("MuxResult").GetString());
        Assert.Equal("None", snapshot.GetProperty("PreviewStartupStrategy").GetString());
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
                if (path == "$.PreviewStartupState" && expected.GetString() == "previewRuntime.StartupState")
                {
                    // The frozen fixture used a source-path sentinel because this input used to be
                    // a string. The typed runtime state now projects its legal enum wire name.
                    Assert.Equal("RendererAttaching", actual.GetString());
                    break;
                }
                Assert.True(expected.GetString() == actual.GetString(), $"{path}: expected {expected}, got {actual}");
                break;
        }
    }
}
