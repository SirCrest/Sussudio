using System.Collections;
using System.Globalization;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public class MainWindowUiContractStatsSnapshotTests
{
    [Fact]
    public void StatsSnapshotConstruction_LivesInFocusedBuilder()
    {
        var statsOverlayText = Sussudio.Tests.MainWindowStatsOverlaySource.Read();
        var statsOverlayCompositionText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs");
        var statsOverlayCompositionGraphText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.Graph.cs");
        var statsSnapshotProviderText = ReadRepoFile("Sussudio/Controllers/Stats/StatsSnapshotProvider.cs");
        var mainWindowText = MainWindowCompositionSource.Read();
        var statsSnapshotBuilderText = ReadRepoFile("Sussudio/ViewModels/StatsSnapshotBuilder.cs");
        var statsSnapshotText = ReadRepoFile("Sussudio/ViewModels/StatsSnapshot.cs");
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs");

        AssertContains(statsSnapshotBuilderText, "internal static class StatsSnapshotBuilder");
        AssertContains(statsSnapshotBuilderText, "public static StatsSnapshot Build(");
        AssertContains(statsSnapshotBuilderText, "internal readonly record struct StatsSnapshotRenderMetrics(");
        AssertContains(statsSnapshotBuilderText, "internal readonly record struct StatsSnapshotViewState(");
        AssertContains(statsSnapshotBuilderText, "return new StatsSnapshot(");
        AssertContains(statsSnapshotText, "public sealed record StatsSnapshot(");
        AssertContains(mainWindowText, "InitializeStatsOverlayCompositionController();");
        AssertContains(statsOverlayText, "private StatsSnapshot GetStatsSnapshot()");
        AssertContains(statsOverlayCompositionText, "private readonly StatsSnapshotProvider _statsSnapshotProvider;");
        AssertContains(statsOverlayText, "GetCaptureHealthSnapshot = ViewModel.GetCaptureHealthSnapshot,");
        AssertContains(statsOverlayText, "GetRenderer = () => _previewRendererHostController.Renderer,");
        AssertContains(statsOverlayText, "GetPreviewMinPresentationIntervalMs = () => _previewRendererHostController.PreviewMinPresentationIntervalMs");
        AssertContains(statsOverlayText, "IsPreviewing = () => ViewModel.IsPreviewing,");
        AssertContains(statsOverlayText, "IsRecording = () => ViewModel.IsRecording");
        AssertContains(statsOverlayText, "=> _statsOverlayCompositionController.GetStatsSnapshot();");
        AssertContains(statsOverlayCompositionGraphText, "private static StatsSnapshotProvider CreateSnapshotProvider(");
        AssertContains(statsOverlayCompositionText, "=> _statsSnapshotProvider.GetSnapshot();");
        AssertContains(statsSnapshotProviderText, "internal sealed class StatsSnapshotProvider");
        AssertDoesNotContain(statsSnapshotProviderText, "internal sealed partial class StatsSnapshotProvider");
        AssertContains(statsSnapshotProviderText, "private const int RecentSampleCount = 180;");
        AssertContains(statsSnapshotProviderText, "var health = _context.GetCaptureHealthSnapshot();");
        AssertContains(statsSnapshotProviderText, "BuildRenderMetrics(_context.GetRenderer(), _context.GetPreviewMinPresentationIntervalMs())");
        AssertContains(statsSnapshotProviderText, "new StatsSnapshotViewState(_context.IsPreviewing(), _context.IsRecording())");
        AssertContains(statsSnapshotProviderText, "return StatsSnapshotBuilder.Build(health, renderer, viewState);");
        AssertDoesNotContain(statsSnapshotProviderText, "MainViewModel ViewModel");
        AssertContains(statsSnapshotProviderText, "var presentCadence = renderer?.GetPresentCadenceMetrics(previewMinPresentationIntervalMs);");
        AssertContains(statsSnapshotProviderText, "PreviewRecentPresentIntervalsMs: renderer?.GetRecentPresentIntervalsMs(RecentSampleCount) ?? Array.Empty<double>()");
        AssertDoesNotContain(statsOverlayText, "var renderer = new StatsSnapshotRenderMetrics(");
        AssertDoesNotContain(statsOverlayText, "return new StatsSnapshot(");
        AssertDoesNotContain(statsOverlayText, "return StatsSnapshotBuilder.Build(health, renderer, viewState);");
        AssertContains(statsWindowText, "private readonly Func<StatsSnapshot> _dataProvider;");
        AssertDoesNotContain(statsWindowText, "public sealed record StatsSnapshot(");
    }

    [Fact]
    public void StatsSnapshotBuilder_MapsHealthAndRendererMetrics()
    {
        var health = CreateInstance("Sussudio.Models.CaptureHealthSnapshot");
        SetPropertyOrBackingField(health, "ExpectedFrameRate", 120d);
        SetPropertyOrBackingField(health, "NegotiatedWidth", 1920u);
        SetPropertyOrBackingField(health, "NegotiatedHeight", 1080u);
        SetPropertyOrBackingField(health, "NegotiatedFrameRate", 120d);
        SetPropertyOrBackingField(health, "ReaderSourceSubtype", "MJPG");
        SetPropertyOrBackingField(health, "CaptureCadenceSampleCount", 60);
        SetPropertyOrBackingField(health, "CaptureCadenceObservedFps", 119.8d);
        SetPropertyOrBackingField(health, "CaptureCadenceAverageIntervalMs", 8.33d);
        SetPropertyOrBackingField(health, "CaptureCadenceP95IntervalMs", 8.75d);
        SetPropertyOrBackingField(health, "CaptureCadenceJitterStdDevMs", 0.12d);
        SetPropertyOrBackingField(health, "CaptureCadenceEstimatedDropPercent", 0.5d);
        SetPropertyOrBackingField(health, "CaptureCadenceEstimatedDroppedFrames", 2L);
        SetPropertyOrBackingField(health, "VideoFramesArrived", 240L);
        SetPropertyOrBackingField(health, "VideoFramesDropped", 3L);
        SetPropertyOrBackingField(health, "VisualCadenceSampleCount", 30);
        SetPropertyOrBackingField(health, "VisualCadenceOutputObservedFps", 120d);
        SetPropertyOrBackingField(health, "VisualCadenceChangeObservedFps", 119d);
        SetPropertyOrBackingField(health, "VisualCadenceMotionConfidence", "HighMotion");
        SetPropertyOrBackingField(health, "VisualCenterCadenceMotionConfidence", "HighMotion");
        SetPropertyOrBackingField(health, "SourceTelemetryOrigin", ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"));
        SetPropertyOrBackingField(health, "SourceTelemetryConfidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(health, "SourceWidth", 3840);
        SetPropertyOrBackingField(health, "SourceHeight", 2160);
        SetPropertyOrBackingField(health, "SourceFrameRateExact", 119.88d);
        SetPropertyOrBackingField(health, "SourceIsHdr", true);
        SetPropertyOrBackingField(health, "SourceVideoFormat", "YCbCr422");
        SetPropertyOrBackingField(health, "SourceColorimetry", "BT.2020");
        SetPropertyOrBackingField(health, "AvSyncCaptureDriftMs", -1.25d);
        SetPropertyOrBackingField(health, "EncoderCodecName", "hevc_nvenc");
        SetPropertyOrBackingField(health, "EncoderWidth", 1920);
        SetPropertyOrBackingField(health, "EncoderHeight", 1080);
        SetPropertyOrBackingField(health, "EncoderFrameRate", 120d);
        SetPropertyOrBackingField(health, "EncoderTargetBitRate", 50_000_000u);

        var detailType = RequireType("Sussudio.Models.SourceTelemetryDetailEntry");
        var details = Array.CreateInstance(detailType, 1);
        details.SetValue(
            Activator.CreateInstance(detailType, "Audio / Input", "ADC (Analog)", "On", null),
            0);
        SetPropertyOrBackingField(health, "SourceTelemetryDetails", details);

        var renderMetricsType = RequireType("Sussudio.StatsSnapshotRenderMetrics");
        var renderMetrics = Activator.CreateInstance(
                renderMetricsType,
                20,
                119.7d,
                8.4d,
                9.0d,
                10.0d,
                118.2d,
                1L,
                double.NaN,
                14.5d,
                250L,
                248L,
                2L,
                1920,
                1080,
                new[] { 8.2d, 8.4d },
                new[] { 12.0d, 14.5d })
            ?? throw new InvalidOperationException("Failed to create StatsSnapshotRenderMetrics.");

        var viewStateType = RequireType("Sussudio.StatsSnapshotViewState");
        var viewState = Activator.CreateInstance(viewStateType, true, false)
            ?? throw new InvalidOperationException("Failed to create StatsSnapshotViewState.");

        var builderType = RequireType("Sussudio.StatsSnapshotBuilder");
        var build = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("StatsSnapshotBuilder.Build was not found.");
        var snapshot = build.Invoke(null, new[] { health, renderMetrics, viewState })
            ?? throw new InvalidOperationException("StatsSnapshotBuilder.Build returned null.");

        Assert.Equal(60, GetIntProperty(snapshot, "SourceCadenceSamples"));
        AssertNearlyEqual(119.8d, GetDoubleProperty(snapshot, "SourceObservedFps"), 0.0001);
        Assert.Equal(20, GetIntProperty(snapshot, "PreviewCadenceSamples"));
        AssertNearlyEqual(118.2d, GetDoubleProperty(snapshot, "PreviewOnePercentLowFps"), 0.0001);
        AssertNearlyEqual(0.0d, GetDoubleProperty(snapshot, "PreviewSlowPct"), 0.0001);
        AssertNearlyEqual(99.5d, GetDoubleProperty(snapshot, "PerformanceScore"), 0.0001);
        Assert.True(GetBoolProperty(snapshot, "Previewing"));
        Assert.False(GetBoolProperty(snapshot, "Recording"));
        Assert.Equal(1920, GetIntProperty(snapshot, "CaptureWidth"));
        Assert.Equal("NativeXu", GetStringProperty(snapshot, "TelemetryOrigin"));
        Assert.Equal("High", GetStringProperty(snapshot, "TelemetryConfidence"));
        Assert.Equal("Warning", GetStringProperty(snapshot, "DiagnosticHealthStatus"));
        Assert.Equal("source_capture", GetStringProperty(snapshot, "DiagnosticLikelyStage"));
        Assert.Equal(2, GetCountProperty(GetPropertyValue(snapshot, "SourceTelemetryDetails")));
        Assert.Equal(2, GetCountProperty(GetPropertyValue(snapshot, "PreviewRecentPresentIntervalsMs")));
    }

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object CreateInstance(string typeName)
        => Activator.CreateInstance(RequireType(typeName))
           ?? throw new InvalidOperationException($"Failed to create {typeName}.");

    private static object ParseEnum(string typeName, string value)
        => Enum.Parse(RequireType(typeName), value);

    private static void SetPropertyOrBackingField(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Backing field for {propertyName} was not found.");
        field.SetValue(instance, value);
    }

    private static object? GetPropertyValue(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(instance);

    private static int GetIntProperty(object instance, string propertyName)
        => Convert.ToInt32(GetPropertyValue(instance, propertyName), CultureInfo.InvariantCulture);

    private static double GetDoubleProperty(object instance, string propertyName)
        => Convert.ToDouble(GetPropertyValue(instance, propertyName), CultureInfo.InvariantCulture);

    private static bool GetBoolProperty(object instance, string propertyName)
        => Convert.ToBoolean(GetPropertyValue(instance, propertyName), CultureInfo.InvariantCulture);

    private static string GetStringProperty(object instance, string propertyName)
        => GetPropertyValue(instance, propertyName) as string
           ?? throw new InvalidOperationException($"{propertyName} was not a string.");

    private static int GetCountProperty(object? value)
        => value is ICollection collection
            ? collection.Count
            : value is IEnumerable enumerable
                ? enumerable.Cast<object>().Count()
                : throw new InvalidOperationException("Expected collection value.");

    private static string ReadRepoFile(string relativePath)
        => RuntimeContractSource.ReadRepoFile(relativePath).Replace("\r\n", "\n");

    private static void AssertContains(string actual, string expectedSubstring)
        => Assert.Contains(expectedSubstring, actual, StringComparison.Ordinal);

    private static void AssertDoesNotContain(string actual, string unexpectedSubstring)
        => Assert.DoesNotContain(unexpectedSubstring, actual, StringComparison.Ordinal);

    private static void AssertNearlyEqual(double expected, double actual, double tolerance)
        => Assert.True(
            Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected:0.####}, got {actual:0.####}; tolerance {tolerance:0.####}.");
}
