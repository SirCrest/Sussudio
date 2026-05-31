using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sussudio.Tests;

public partial class StatsPresentationTests
{
    [Fact]
    public void FrameTimeOverlay_UsesDetectedFpsBoundedRange()
    {
        var presentationType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var resolveRange = presentationType.GetMethod("ResolveFrameTimeRange", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("ResolveFrameTimeRange was not found.");

        var range120 = resolveRange.Invoke(null, new object[] { 120.0 })
            ?? throw new InvalidOperationException("ResolveFrameTimeRange returned null for 120fps.");
        AssertNearlyEqual(1000.0 / 150.0, GetDoubleProperty(range120, "MinMs"), 0.0001);
        AssertNearlyEqual(1000.0 / 90.0, GetDoubleProperty(range120, "MaxMs"), 0.0001);
        AssertNearlyEqual(1000.0 / 120.0, GetDoubleProperty(range120, "ExpectedMs"), 0.0001);

        var normalizedExpected = (GetDoubleProperty(range120, "ExpectedMs") - GetDoubleProperty(range120, "MinMs")) /
                                 GetDoubleProperty(range120, "SpanMs");
        AssertNearlyEqual(0.375, normalizedExpected, 0.0001);

        var fallbackRange = resolveRange.Invoke(null, new object[] { 0.0 })
            ?? throw new InvalidOperationException("ResolveFrameTimeRange returned null for fallback fps.");
        AssertNearlyEqual(1000.0 / 75.0, GetDoubleProperty(fallbackRange, "MinMs"), 0.0001);
        AssertNearlyEqual(1000.0 / 45.0, GetDoubleProperty(fallbackRange, "MaxMs"), 0.0001);
        AssertNearlyEqual(1000.0 / 60.0, GetDoubleProperty(fallbackRange, "ExpectedMs"), 0.0001);
    }

    [Fact]
    public void FrameTimeOverlayGeometry_ProjectsGraphCoordinates()
    {
        var presentationType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var geometryType = RequireType("Sussudio.Controllers.FrameTimeOverlayGeometry");
        var resolveRange = presentationType.GetMethod("ResolveFrameTimeRange", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("ResolveFrameTimeRange was not found.");
        var resolveCanvasSize = geometryType.GetMethod("ResolveCanvasSize", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("ResolveCanvasSize was not found.");
        var projectSample = geometryType.GetMethod("ProjectSample", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("ProjectSample was not found.");
        var projectExpectedLine = geometryType.GetMethod("ProjectExpectedLine", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("ProjectExpectedLine was not found.");

        var range120 = resolveRange.Invoke(null, new object[] { 120.0 })
            ?? throw new InvalidOperationException("ResolveFrameTimeRange returned null for 120fps.");
        var fallbackCanvasSize = resolveCanvasSize.Invoke(null, new object[] { 1.0, 0.0 })
            ?? throw new InvalidOperationException("ResolveCanvasSize returned null for fallback dimensions.");
        AssertNearlyEqual(500, GetDoubleProperty(fallbackCanvasSize, "Width"), 0.0001);
        AssertNearlyEqual(92, GetDoubleProperty(fallbackCanvasSize, "Height"), 0.0001);

        var canvasSize = resolveCanvasSize.Invoke(null, new object[] { 300.0, 100.0 })
            ?? throw new InvalidOperationException("ResolveCanvasSize returned null for explicit dimensions.");
        var minPoint = projectSample.Invoke(null, new object[] { 0, 3, GetDoubleProperty(range120, "MinMs"), range120, canvasSize })
            ?? throw new InvalidOperationException("ProjectSample returned null for min sample.");
        var expectedPoint = projectSample.Invoke(null, new object[] { 1, 3, GetDoubleProperty(range120, "ExpectedMs"), range120, canvasSize })
            ?? throw new InvalidOperationException("ProjectSample returned null for expected sample.");
        var maxPoint = projectSample.Invoke(null, new object[] { 2, 3, GetDoubleProperty(range120, "MaxMs"), range120, canvasSize })
            ?? throw new InvalidOperationException("ProjectSample returned null for max sample.");
        var clippedLowPoint = projectSample.Invoke(null, new object[] { 1, 3, GetDoubleProperty(range120, "MinMs") - 100, range120, canvasSize })
            ?? throw new InvalidOperationException("ProjectSample returned null for clipped-low sample.");
        var clippedHighPoint = projectSample.Invoke(null, new object[] { 1, 3, GetDoubleProperty(range120, "MaxMs") + 100, range120, canvasSize })
            ?? throw new InvalidOperationException("ProjectSample returned null for clipped-high sample.");

        AssertNearlyEqual(0, GetDoubleProperty(minPoint, "X"), 0.0001);
        AssertNearlyEqual(100, GetDoubleProperty(minPoint, "Y"), 0.0001);
        AssertNearlyEqual(150, GetDoubleProperty(expectedPoint, "X"), 0.0001);
        AssertNearlyEqual(62.5, GetDoubleProperty(expectedPoint, "Y"), 0.0001);
        AssertNearlyEqual(300, GetDoubleProperty(maxPoint, "X"), 0.0001);
        AssertNearlyEqual(0, GetDoubleProperty(maxPoint, "Y"), 0.0001);
        AssertNearlyEqual(100, GetDoubleProperty(clippedLowPoint, "Y"), 0.0001);
        AssertNearlyEqual(0, GetDoubleProperty(clippedHighPoint, "Y"), 0.0001);

        var expectedLine = projectExpectedLine.Invoke(null, new[] { range120, canvasSize })
            ?? throw new InvalidOperationException("ProjectExpectedLine returned null.");
        AssertNearlyEqual(300, GetDoubleProperty(expectedLine, "X2"), 0.0001);
        AssertNearlyEqual(62.5, GetDoubleProperty(expectedLine, "Y"), 0.0001);
    }

    [Fact]
    public void DockEncoderPresentation_FormatsCodecAndBitrate()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var snapshotType = RequireType("Sussudio.StatsSnapshot");
        var buildDockPresentation = builderType.GetMethod("BuildDockPresentation", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("BuildDockPresentation was not found.");

        object Build(string? codecName, bool recording = true)
        {
            var snapshot = CreateUninitializedObject(snapshotType);
            SetPropertyBackingField(snapshot, "Recording", recording);
            SetPropertyBackingField(snapshot, "EncoderCodecName", codecName);
            SetPropertyBackingField(snapshot, "EncoderWidth", 3840);
            SetPropertyBackingField(snapshot, "EncoderHeight", 2160);
            SetPropertyBackingField(snapshot, "EncoderFrameRate", 59.94);
            SetPropertyBackingField(snapshot, "EncoderTargetBitRate", 50_000_000u);
            SetPropertyBackingField(snapshot, "AvSyncEncoderDriftMs", (double?)2.25d);
            SetPropertyBackingField(snapshot, "AvSyncEncoderCorrectionSamples", (long?)7L);
            SetPropertyBackingField(snapshot, "VisualCadenceMotionConfidence", string.Empty);

            return buildDockPresentation.Invoke(null, new[] { snapshot })
                ?? throw new InvalidOperationException("BuildDockPresentation returned null.");
        }

        var hevc = Build("hevc_nvenc");
        Assert.True(GetBoolProperty(hevc, "EncoderActive"));
        Assert.Equal("HEVC (NVENC)", GetStringProperty(hevc, "EncoderCodec"));
        Assert.Equal("3840 x 2160", GetStringProperty(hevc, "EncoderResolution"));
        Assert.Equal("59.94 fps", GetStringProperty(hevc, "EncoderFrameRate"));
        Assert.Equal("50 Mbps", GetStringProperty(hevc, "EncoderBitrate"));
        Assert.True(GetBoolProperty(hevc, "EncoderDriftVisible"));
        Assert.Equal("+2.2ms (7 corr)", GetStringProperty(hevc, "EncoderDrift"));

        var av1 = Build("av1_nvenc");
        Assert.Equal("AV1 (NVENC)", GetStringProperty(av1, "EncoderCodec"));

        var passthrough = Build("software_custom");
        Assert.Equal("software_custom", GetStringProperty(passthrough, "EncoderCodec"));

        var inactive = Build(null);
        Assert.False(GetBoolProperty(inactive, "EncoderActive"));
        Assert.Equal(string.Empty, GetStringProperty(inactive, "EncoderCodec"));

        var idleDrift = Build("h264_nvenc", recording: false);
        Assert.False(GetBoolProperty(idleDrift, "EncoderDriftVisible"));
        Assert.Equal(string.Empty, GetStringProperty(idleDrift, "EncoderDrift"));
    }

    [Fact]
    public void WindowPresentation_FormatsDetachedWindowText()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var snapshotType = RequireType("Sussudio.StatsSnapshot");
        var buildWindowPresentation = builderType.GetMethod("BuildStatsWindowPresentation", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("BuildStatsWindowPresentation was not found.");

        var snapshot = CreateUninitializedObject(snapshotType);
        SetPropertyBackingField(snapshot, "Previewing", true);
        SetPropertyBackingField(snapshot, "Recording", false);
        SetPropertyBackingField(snapshot, "DiagnosticHealthStatus", "Healthy");
        SetPropertyBackingField(snapshot, "DiagnosticLikelyStage", "none");
        SetPropertyBackingField(snapshot, "DiagnosticEvidence", string.Empty);
        SetPropertyBackingField(snapshot, "DiagnosticSummary", "All monitored frame lanes are within current thresholds.");
        SetPropertyBackingField(snapshot, "SourceWidth", (int?)3840);
        SetPropertyBackingField(snapshot, "SourceHeight", (int?)2160);
        SetPropertyBackingField(snapshot, "SourceFrameRateExact", (double?)119.88d);
        SetPropertyBackingField(snapshot, "SourceIsHdr", (bool?)true);
        SetPropertyBackingField(snapshot, "SourceColorimetry", "BT.2020");
        SetPropertyBackingField(snapshot, "SourceVideoFormat", "YCbCr422");
        SetPropertyBackingField(snapshot, "TelemetryOrigin", "NativeXu");
        SetPropertyBackingField(snapshot, "TelemetryConfidence", "High");
        SetPropertyBackingField(snapshot, "SourceObservedFps", 119.8d);
        SetPropertyBackingField(snapshot, "SourceExpectedFps", 120d);
        SetPropertyBackingField(snapshot, "SourceAvgIntervalMs", 8.333d);
        SetPropertyBackingField(snapshot, "SourceP95IntervalMs", 8.75d);
        SetPropertyBackingField(snapshot, "SourceJitterMs", 0.125d);
        SetPropertyBackingField(snapshot, "SourceSevereGaps", 2L);
        SetPropertyBackingField(snapshot, "SourceEstDrops", 3L);
        SetPropertyBackingField(snapshot, "SourceEstDropPct", 0.25d);
        SetPropertyBackingField(snapshot, "PreviewObservedFps", 118.2d);
        SetPropertyBackingField(snapshot, "PreviewAvgIntervalMs", 8.44d);
        SetPropertyBackingField(snapshot, "PreviewP95IntervalMs", 9.1d);
        SetPropertyBackingField(snapshot, "PreviewSlowFrames", 4L);
        SetPropertyBackingField(snapshot, "PreviewSlowPct", 1.5d);
        SetPropertyBackingField(snapshot, "PipelineLatencyMs", 3.4d);
        SetPropertyBackingField(snapshot, "SourceFramesDelivered", 500L);
        SetPropertyBackingField(snapshot, "SourceFramesDropped", 5L);
        SetPropertyBackingField(snapshot, "RendererFramesRendered", 490L);
        SetPropertyBackingField(snapshot, "RendererFramesDropped", 6L);
        SetPropertyBackingField(snapshot, "PerformanceScore", 98.75d);

        var presentation = buildWindowPresentation.Invoke(null, new[] { snapshot })
            ?? throw new InvalidOperationException("BuildStatsWindowPresentation returned null.");

        Assert.Equal("Previewing", GetStringProperty(presentation, "SessionState"));
        Assert.Equal("Healthy", GetStringProperty(presentation, "DiagnosticStatus"));
        Assert.Equal("All monitored frame lanes are within current thresholds.", GetStringProperty(presentation, "DiagnosticEvidence"));
        Assert.Equal("3840 x 2160", GetStringProperty(presentation, "SourceResolution"));
        Assert.Equal("119.88 fps", GetStringProperty(presentation, "SourceFrameRate"));
        Assert.Equal("On (BT.2020)", GetStringProperty(presentation, "SourceHdr"));
        Assert.Equal("YCbCr422", GetStringProperty(presentation, "SourceFormat"));
        Assert.Equal("NativeXu (High)", GetStringProperty(presentation, "TelemetryOrigin"));
        Assert.Equal("119.80", GetStringProperty(presentation, "SourceFps"));
        Assert.Equal("8.33ms avg", GetStringProperty(presentation, "SourceAvg"));
        Assert.Equal("3 drops (0.3%)", GetStringProperty(presentation, "SourceDrops"));
        Assert.Equal("4 frames (1.5%)", GetStringProperty(presentation, "PreviewSlow"));
        Assert.Equal("3.40ms avg", GetStringProperty(presentation, "PipelineLatency"));
        Assert.Equal("98.8 / 100", GetStringProperty(presentation, "PerformanceScore"));

        var telemetryDetails = GetPropertyValue(presentation, "TelemetryDetails")
            ?? throw new InvalidOperationException("StatsWindowPresentation.TelemetryDetails was null.");
        Assert.True(GetBoolProperty(telemetryDetails, "IsEmpty"));
        Assert.Equal("All monitored frame lanes are within current thresholds.", GetStringProperty(telemetryDetails, "EmptyText"));
    }

    [Fact]
    public void VisualPresentation_TreatsExpectedDisplayRepeatAsGood()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var snapshotType = RequireType("Sussudio.StatsSnapshot");
        var buildDockPresentation = builderType.GetMethod("BuildDockPresentation", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("BuildDockPresentation was not found.");

        var snapshot = CreateUninitializedObject(snapshotType);
        SetPropertyBackingField(snapshot, "Previewing", true);
        SetPropertyBackingField(snapshot, "SourceExpectedFps", 60d);
        SetPropertyBackingField(snapshot, "SourceFrameRateExact", (double?)60d);
        SetPropertyBackingField(snapshot, "VisualCadenceSamples", 120);
        SetPropertyBackingField(snapshot, "VisualCadenceOutputFps", 120d);
        SetPropertyBackingField(snapshot, "VisualCadenceChangeFps", 60d);
        SetPropertyBackingField(snapshot, "VisualCadenceRepeatPercent", 50d);
        SetPropertyBackingField(snapshot, "VisualCadenceLongestRepeatRun", 1L);
        SetPropertyBackingField(snapshot, "VisualCadenceMotionScore", 12.5d);
        SetPropertyBackingField(snapshot, "VisualCadenceMotionConfidence", "High");

        var presentation = buildDockPresentation.Invoke(null, new[] { snapshot })
            ?? throw new InvalidOperationException("BuildDockPresentation returned null.");

        Assert.Equal("120 Hz", GetStringProperty(presentation, "SummaryVisualFps"));
        Assert.Equal("crop 120 Hz", GetStringProperty(presentation, "VisualFps"));
        Assert.Equal("12.5% px / High", GetStringProperty(presentation, "VisualMotion"));
        Assert.Equal("Good", GetPropertyValue(presentation, "SummaryVisualFpsStatus")?.ToString());
        Assert.Equal("Good", GetPropertyValue(presentation, "VisualFpsStatus")?.ToString());
    }

    [Fact]
    public void CompactPreviewSummary_UsesCurrentFrameTimeAndOnePercentLow()
    {
        var statsOverlayText = Sussudio.Tests.MainWindowStatsOverlaySource.Read();
        var statsOverlayCompositionText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs");
        var dockPresentationControllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsDockRefreshController.cs");
        var statsSnapshotProviderText = statsOverlayCompositionText;
        var frameTimeOverlayControllerText = statsOverlayCompositionText;
        var frameTimeOverlayGeometryText = frameTimeOverlayControllerText;
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs");
        var statsSnapshotText = ReadRepoFile("Sussudio/ViewModels/StatsSnapshot.cs");
        var statsSnapshotBuilderText = statsSnapshotText;
        var mainWindowXaml = ReadRepoFile("Sussudio/MainWindow.xaml");
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs");

        Assert.Contains("PreviewOnePercentLowFps: presentCadence?.OnePercentLowFps ?? 0", statsSnapshotProviderText);
        Assert.Contains("PreviewOnePercentLowFps: StatsPresentationBuilder.Sanitize(renderer.PreviewOnePercentLowFps)", statsSnapshotBuilderText);
        AssertStatsPresentationPreviewFormattingLivesInBuilder(
            statsPresentationText,
            statsOverlayText,
            statsOverlayCompositionText,
            frameTimeOverlayControllerText);
        Assert.Contains("internal static class FrameTimeOverlayGeometry", frameTimeOverlayGeometryText);
        Assert.Contains("SetMetricBrush(_context.SummaryRendererFpsValue, presentation.SummaryRendererFpsStatus);", dockPresentationControllerText);
        Assert.Contains("SetTextIfChanged(_context.PreviewFpsValue, presentation.PreviewFps);", dockPresentationControllerText);
        Assert.DoesNotContain("SetMetricBrush(Stats_SummaryRendererFpsValue", statsOverlayText);
        Assert.Contains("double PreviewOnePercentLowFps", statsSnapshotText);
        Assert.DoesNotContain("double PreviewFivePercentLowFps", statsWindowText);
        Assert.Contains("x:Name=\"Stats_SummaryRendererFpsValue\"", mainWindowXaml);
        Assert.Contains("TextWrapping=\"NoWrap\"", mainWindowXaml);
        Assert.Contains("MaxLines=\"1\"", mainWindowXaml);
    }

[Fact]
    public void StatsPresentationLogic_LivesInFocusedBuilder()
    {
        var statsOverlayText = Sussudio.Tests.MainWindowStatsOverlaySource.Read();
        var statsOverlayCompositionText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs").Replace("\r\n", "\n");
        var statsDockRefreshControllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsDockRefreshController.cs").Replace("\r\n", "\n");
        var frameTimeOverlayText = statsOverlayCompositionText;
        var frameTimeOverlayControllerText = statsOverlayCompositionText;
        var frameTimeOverlayGeometryText = frameTimeOverlayControllerText;
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");
        var statsPresentationModelsText = statsPresentationText;
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs").Replace("\r\n", "\n");
        var statsWindowPresentationControllerText = statsWindowText;
        var statsWindowTelemetryDetailsControllerText = statsWindowPresentationControllerText;

        AssertContains(statsPresentationText, "internal static class StatsPresentationBuilder");
        AssertDoesNotContain(statsPresentationText, "internal static partial class StatsPresentationBuilder");
        AssertContains(statsPresentationText, "public static StatsDockPresentation BuildDockPresentation(StatsSnapshot snapshot)");
        AssertContains(statsPresentationText, "private static string ResolvePreviewResolutionText(StatsSnapshot snapshot)");
        AssertContains(statsPresentationText, "private static string ResolveCaptureSummaryText(StatsSnapshot snapshot)");
        AssertContains(statsPresentationText, "public static StatsFrameTimePresentation BuildFrameTimePresentation(StatsSnapshot snapshot)");
        AssertContains(statsPresentationText, "private static string FormatVisualRepeatSummary(StatsSnapshot snapshot)");
        AssertContains(statsPresentationText, "private static string FormatVisualCadenceSummary(StatsSnapshot snapshot)");
        AssertContains(statsPresentationText, "private static string FormatVisualMotionSummary(StatsSnapshot snapshot)");
        AssertContains(statsPresentationText, "private static string FormatHz(double value)");
        AssertContains(statsPresentationText, "private const double VisualRepeatTolerancePercent = 0.25;");
        AssertContains(statsPresentationText, "private static bool IsVisualRepeatWithinExpectedDrift(StatsSnapshot snapshot)");
        AssertContains(statsPresentationText, "private static double GetExpectedVisualRepeatPercent(StatsSnapshot snapshot)");
        AssertContains(statsPresentationText, "private static StatsEncoderPresentation BuildEncoderPresentation(StatsSnapshot snapshot)");
        AssertContains(statsPresentationText, "private static string FormatEncoderCodecName(string codecName)");
        AssertContains(statsPresentationText, "private static string FormatEncoderBitrate(uint targetBitRate)");
        AssertContains(statsPresentationText, "private static string FormatEncoderDrift(StatsSnapshot snapshot)");
        AssertContains(statsPresentationText, "public static StatsDiagnosticRowsPresentation BuildDiagnosticRows(");
        AssertContains(statsPresentationText, "private static List<(string Label, string Value)> ParseDiagnosticSummary");
        AssertContains(statsPresentationText, "public static IReadOnlyList<StatsHardwareRowPresentation> BuildHardwareDecodeRows(");
        AssertContains(statsPresentationText, "StatsHardwareDecodeRowsInput mjpeg)");
        AssertContains(statsPresentationText, "public static IReadOnlyList<StatsHardwareRowPresentation> BuildHardwareGpuRows(StatsHardwareGpuRowsInput? nvml)");
        AssertDoesNotContain(statsPresentationText, "using Sussudio.Services.Gpu;");
        AssertContains(statsPresentationText, "public static StatsDiagnosticSummary BuildStatsDiagnosticSummary(");
        AssertContains(statsPresentationText, "DiagnosticThresholds.CalculatePercent(rendererDrops, rendererSubmitted)");
        AssertContains(statsPresentationText, "private static StatsMetricStatus ResolveFrameLaneStatus(");
        AssertContains(statsPresentationText, "private static StatsMetricStatus ResolveDecodedVisualStatus(StatsSnapshot snapshot)");
        AssertContains(statsPresentationText, "public static StatsWindowPresentation BuildStatsWindowPresentation(StatsSnapshot snapshot)");
        AssertContains(statsPresentationText, "private static StatsWindowTelemetryDetailsPresentation BuildStatsWindowTelemetryDetails(");
        AssertContains(statsPresentationModelsText, "internal sealed record StatsDockPresentation(");
        AssertContains(statsPresentationModelsText, "internal sealed record StatsWindowPresentation(");
        AssertContains(statsPresentationModelsText, "internal sealed record StatsWindowTelemetryDetailsPresentation(");
        AssertContains(statsPresentationModelsText, "internal sealed record StatsFrameTimePresentation(");
        AssertContains(statsPresentationModelsText, "internal readonly record struct StatsHardwareRowPresentation(");
        AssertContains(statsPresentationModelsText, "internal readonly record struct StatsHardwareDecodeRowsInput(");
        AssertContains(statsPresentationModelsText, "internal readonly record struct StatsHardwareGpuRowsInput(");
        AssertContains(statsPresentationModelsText, "internal enum StatsMetricStatus");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "ViewModels", "StatsPresentationModels.cs")),
            "stats presentation DTOs folded into StatsPresentationBuilder.cs");
        AssertContains(statsDockRefreshControllerText, "var presentation = StatsPresentationBuilder.BuildDockPresentation(snapshot);");
        AssertContains(frameTimeOverlayText, "_frameTimeOverlayPresentationController.Apply(snapshot);");
        AssertContains(frameTimeOverlayControllerText, "internal sealed class FrameTimeOverlayPresentationController");
        Assert.False(
            File.Exists(Path.Combine(
                FindRepoRoot(),
                "Sussudio",
                "Controllers",
                "Stats",
                "FrameTimeOverlayPresentationController.cs")),
            "frame-time overlay presentation lives with stats overlay composition ownership");
        AssertContains(frameTimeOverlayControllerText, "public void Apply(StatsSnapshot snapshot)");
        AssertContains(frameTimeOverlayControllerText, "var presentation = StatsPresentationBuilder.BuildFrameTimePresentation(snapshot);");
        AssertContains(frameTimeOverlayControllerText, "UpdateExpectedLine(presentation.Range);");
        AssertContains(frameTimeOverlayControllerText, "UpdateLine(_context.VisualLine, presentation.VisualSamples, presentation.Range);");
        AssertContains(frameTimeOverlayControllerText, "FrameTimeOverlayGeometry.ProjectSample(i, samples.Count, samples[i], range, canvasSize)");
        AssertContains(frameTimeOverlayControllerText, "FrameTimeOverlayGeometry.ProjectExpectedLine(range, canvasSize)");
        AssertContains(frameTimeOverlayControllerText, "SetTextIfChanged(_context.SourceValue, presentation.SourceText);");
        AssertContains(frameTimeOverlayGeometryText, "internal static class FrameTimeOverlayGeometry");
        AssertContains(frameTimeOverlayGeometryText, "public static FrameTimeOverlayCanvasSize ResolveCanvasSize(double actualWidth, double actualHeight)");
        AssertContains(frameTimeOverlayGeometryText, "public static Point ProjectSample(");
        AssertContains(frameTimeOverlayGeometryText, "public static FrameTimeOverlayExpectedLineGeometry ProjectExpectedLine(");
        AssertContains(frameTimeOverlayGeometryText, "var normalized = Math.Clamp((frameTimeMs - range.MinMs) / range.SpanMs, 0.0, 1.0);");
        AssertDoesNotContain(frameTimeOverlayControllerText, "var normalized = Math.Clamp((samples[i] - range.MinMs) / range.SpanMs, 0.0, 1.0);");
        AssertContains(statsDockRefreshControllerText, "StatsPresentationBuilder.BuildDiagnosticRows(telemetryDetails, diagnosticSummary)");
        AssertContains(statsWindowText, "var presentation = StatsPresentationBuilder.BuildStatsWindowPresentation(snapshot);");
        AssertContains(statsWindowText, "_presentationController.Apply(presentation);");
        AssertContains(statsWindowPresentationControllerText, "internal sealed class StatsWindowPresentationController");
        AssertContains(statsWindowPresentationControllerText, "public void Apply(StatsWindowPresentation presentation)");
        AssertContains(statsWindowPresentationControllerText, "private readonly StatsWindowTelemetryDetailsController _telemetryDetailsController;");
        AssertContains(statsWindowPresentationControllerText, "_telemetryDetailsController.Apply(presentation.TelemetryDetails);");
        AssertDoesNotContain(statsWindowPresentationControllerText, "private void UpdateTelemetryDetails(StatsWindowTelemetryDetailsPresentation presentation)");
        Assert.False(
            File.Exists(Path.Combine(
                FindRepoRoot(),
                "Sussudio",
                "Controllers",
                "Stats",
                "StatsWindowPresentationController.cs")),
            "detached stats-window presentation lives with StatsWindow.xaml.cs");
        AssertContains(statsWindowTelemetryDetailsControllerText, "internal sealed class StatsWindowTelemetryDetailsController");
        AssertContains(statsWindowTelemetryDetailsControllerText, "public void Apply(StatsWindowTelemetryDetailsPresentation presentation)");
        AssertContains(statsWindowTelemetryDetailsControllerText, "_context.TelemetryDetailsContent.Children.Clear();");
        AssertContains(statsWindowTelemetryDetailsControllerText, "Text = presentation.EmptyText,");
        AssertContains(statsWindowTelemetryDetailsControllerText, "Margin = new Thickness(0, 8, 0, 2),");
        AssertContains(statsWindowTelemetryDetailsControllerText, "grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });");
        AssertContains(statsWindowTelemetryDetailsControllerText, "grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });");
        AssertContains(statsWindowTelemetryDetailsControllerText, "HorizontalAlignment = HorizontalAlignment.Right,");
        AssertContains(statsWindowTelemetryDetailsControllerText, "TextWrapping = TextWrapping.Wrap");
        AssertContains(statsWindowText, "var telemetryDetailsController = new StatsWindowTelemetryDetailsController(new StatsWindowTelemetryDetailsControllerContext");
        AssertDoesNotContain(statsWindowText, "private static string FormatFps(");
        AssertDoesNotContain(statsWindowText, "private static string FormatMs(");
        AssertDoesNotContain(statsWindowText, "private static string FormatPercent(");
        AssertDoesNotContain(statsWindowText, "private static string FormatSourceHdr(");
        AssertDoesNotContain(statsOverlayText, "BuildFrameTimePresentation(snapshot)");
        AssertDoesNotContain(statsOverlayText, "private enum MetricStatus");
        AssertDoesNotContain(statsOverlayText, "private static string ResolveCaptureSummaryText");
        AssertDoesNotContain(statsOverlayText, "private static List<(string Label, string Value)> ParseDiagnosticSummary");
        AssertDoesNotContain(statsOverlayText, "StatsPresentationBuilder.BuildDockPresentation(snapshot)");
        AssertDoesNotContain(statsOverlayText, "StatsPresentationBuilder.BuildDiagnosticRows(telemetryDetails, diagnosticSummary)");
    }

    [Fact]
    public void StatsPanels_UseSourceTelemetry_ForHdmiInput()
    {
        var statsOverlayText = Sussudio.Tests.MainWindowStatsOverlaySource.Read();
        var statsDockRefreshControllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsDockRefreshController.cs").Replace("\r\n", "\n");
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");
        var mainWindowXaml = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs").Replace("\r\n", "\n");
        var statsWindowXaml = ReadRepoFile("Sussudio/StatsWindow.xaml").Replace("\r\n", "\n");
        var nativeXuText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs").Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.RollingPoll.cs").Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs").Replace("\r\n", "\n");

        AssertContains(statsPresentationText, "var sourceHdr = FormatSourceHdr(snapshot.SourceIsHdr, snapshot.SourceColorimetry);");
        AssertContains(statsPresentationText, "var sourceFormat = snapshot.SourceVideoFormat ?? \"\\u2014\";");
        AssertDoesNotContain(statsPresentationText, "var sourceFormat =\n            snapshot.ReaderSourceSubtype ??");
        AssertContains(statsDockRefreshControllerText, "StatsPresentationBuilder.BuildDockPresentation(snapshot)");
        AssertContains(statsWindowText, "StatsPresentationBuilder.BuildStatsWindowPresentation(snapshot)");
        AssertContains(statsPresentationText, "SourceHdr: FormatSourceHdr(snapshot.SourceIsHdr, snapshot.SourceColorimetry),");
        AssertContains(statsPresentationText, "SourceFormat: snapshot.SourceVideoFormat ?? \"\\u2014\",");
        AssertContains(mainWindowXaml, "Text=\"Video Format\"");
        AssertContains(mainWindowXaml, "Text=\"Telemetry Details\"");
        AssertContains(statsWindowXaml, "Text=\"Video Format\"");
        AssertContains(statsWindowXaml, "Text=\"Telemetry Details\"");
        AssertContains(nativeXuText, "VideoFormat = aviInfo.ColorSpace,");
        AssertContains(nativeXuText, "Colorimetry = aviInfo.Colorimetry,");
        AssertContains(nativeXuText, "Quantization = aviInfo.Quantization,");
        AssertContains(nativeXuText, "HdrTransferFunction = ResolveHdrTransferFunction(hdrInfo.Eotf),");
    }

    [Fact]
    public void StatsDockPresentationApplication_LivesInController()
    {
        var statsOverlayText = Sussudio.Tests.MainWindowStatsOverlaySource.Read();
        var statsOverlayCompositionText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs").Replace("\r\n", "\n");
        var statsDockCompositionText = statsOverlayCompositionText;
        var refreshControllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsDockRefreshController.cs").Replace("\r\n", "\n");
        var controllerText = refreshControllerText;

        AssertContains(statsOverlayCompositionText, "private readonly StatsDockControllerGraph _statsDockControllerGraph;");
        AssertContains(statsOverlayCompositionText, "private StatsDockControllerGraph CreateDockControllerGraph(");
        AssertContains(statsDockCompositionText, "internal sealed class StatsDockControllerGraphContext");
        AssertContains(statsDockCompositionText, "var statsDockPresentationController = CreatePresentationController(context);");
        AssertContains(statsDockCompositionText, "_refreshController = CreateRefreshController(");
        AssertContains(statsDockCompositionText, "private static StatsDockPresentationController CreatePresentationController(");
        AssertContains(statsDockCompositionText, "private static StatsDockRefreshController CreateRefreshController(");
        AssertContains(statsDockCompositionText, "internal sealed class StatsDockControllerGraph");
        AssertContains(statsDockCompositionText, "public void RefreshDock()");
        AssertContains(statsDockCompositionText, "public void RefreshDiagnosticsSection()");
        AssertOccursBefore(statsOverlayCompositionText, "_frameTimeOverlayPresentationController = CreateFrameTimeOverlayPresentationController(context);", "_statsDockControllerGraph = CreateDockControllerGraph(context);");
        AssertOccursBefore(statsOverlayCompositionText, "_statsDockControllerGraph = CreateDockControllerGraph(context);", "_statsOverlayController = CreateOverlayController(context);");
        AssertOccursBefore(statsDockCompositionText, "var statsDockPresentationController = CreatePresentationController(context);", "var statsDockRowChromeController = CreateRowChromeController(context);");
        AssertOccursBefore(statsDockCompositionText, "var statsDockRowChromeController = CreateRowChromeController(context);", "var statsDiagnosticRowsController = CreateDiagnosticRowsController(context);");
        AssertOccursBefore(statsDockCompositionText, "var statsDiagnosticRowsController = CreateDiagnosticRowsController(context);", "var statsHardwareRowsInputProvider = CreateHardwareRowsInputProvider(context);");
        AssertOccursBefore(statsDockCompositionText, "var statsHardwareRowsInputProvider = CreateHardwareRowsInputProvider(context);", "var statsHardwareRowsController = CreateHardwareRowsController(");
        AssertOccursBefore(statsDockCompositionText, "var statsHardwareRowsController = CreateHardwareRowsController(", "_refreshController = CreateRefreshController(");
        AssertContains(refreshControllerText, "internal sealed class StatsDockRefreshControllerContext");
        AssertContains(refreshControllerText, "internal sealed class StatsDockRefreshController");
        AssertContains(refreshControllerText, "public required Func<bool> IsStatsDockVisible { get; init; }");
        AssertContains(refreshControllerText, "public required Func<bool> IsDiagnosticsSectionVisible { get; init; }");
        AssertContains(refreshControllerText, "public void RefreshDock()");
        AssertContains(refreshControllerText, "public void RefreshDiagnosticsSection()");
        AssertContains(refreshControllerText, "_context.IsWindowClosing() || !_context.IsStatsDockVisible()");
        AssertContains(refreshControllerText, "StatsPresentationBuilder.BuildDockPresentation(snapshot)");
        AssertContains(refreshControllerText, "_context.DockPresentationController.Apply(presentation);");
        AssertContains(refreshControllerText, "_context.HardwareRowsController.UpdateDecodeSection();");
        AssertContains(refreshControllerText, "_context.HardwareRowsController.UpdateGpuSection();");
        AssertContains(refreshControllerText, "StatsPresentationBuilder.BuildDiagnosticRows(telemetryDetails, diagnosticSummary)");
        AssertContains(refreshControllerText, "if (!_context.IsDiagnosticsSectionVisible())");
        AssertContains(controllerText, "internal sealed class StatsDockPresentationControllerContext");
        AssertContains(controllerText, "internal sealed class StatsDockPresentationController");
        AssertContains(controllerText, "public void Apply(StatsDockPresentation presentation)");
        AssertContains(controllerText, "SetTextIfChanged(_context.SessionStateValue, presentation.SessionState);");
        AssertContains(controllerText, "SetMetricBrush(_context.SummaryRendererFpsValue, presentation.SummaryRendererFpsStatus);");
        AssertContains(controllerText, "SetVisibilityIfChanged(_context.AvSyncEncoderRow, presentation.EncoderDriftVisible ? Visibility.Visible : Visibility.Collapsed);");
        AssertContains(controllerText, "SetVisibilityIfChanged(_context.EncoderSection, presentation.EncoderActive ? Visibility.Visible : Visibility.Collapsed);");
        AssertContains(controllerText, "MetricGoodBrush = new(Windows.UI.Color.FromArgb(0xFF, 0x70, 0xF0, 0x8B))");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Controllers", "Stats", "StatsDockPresentationController.cs")),
            "stats dock presentation application lives with stats dock refresh ownership");
        AssertDoesNotContain(statsOverlayText, "SetMetricBrush(");
        AssertDoesNotContain(statsOverlayText, "SetTextIfChanged(Stats_");
        AssertDoesNotContain(statsOverlayText, "private static readonly SolidColorBrush MetricNeutralBrush");
        AssertDoesNotContain(statsOverlayText, "StatsPresentationBuilder.BuildDockPresentation(snapshot)");
        AssertDoesNotContain(statsOverlayText, "StatsPresentationBuilder.BuildDiagnosticRows(telemetryDetails, diagnosticSummary)");
        AssertDoesNotContain(statsOverlayText, "private void UpdateStatsDock()");
        AssertDoesNotContain(statsOverlayText, "private void RefreshDiagnosticsSection()");
        AssertDoesNotContain(statsOverlayText, "private void UpdateDiagnosticsSection(");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Controllers", "Stats", "StatsDockControllerGraph.Contexts.cs")),
            "stats dock graph context folded into StatsOverlayCompositionController.cs");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Controllers", "Stats", "StatsDockControllerGraph.cs")),
            "stats dock graph folded into StatsOverlayCompositionController.cs");
    }

    [Fact]
    public void StatsDockRowChrome_LivesInFocusedController()
    {
        var statsOverlayText = Sussudio.Tests.MainWindowStatsOverlaySource.Read();
        var statsDockCompositionText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs").Replace("\r\n", "\n");
        var mainWindowText = MainWindowCompositionSource.Read();
        var statsDockRowsText = ReadRepoFile("Sussudio/Controllers/Stats/StatsDockRowsController.cs").Replace("\r\n", "\n");
        var controllerText = statsDockRowsText;
        var rowChromePresenterText = statsDockRowsText;
        var rowChromeControllerText = statsDockRowsText;
        var refreshControllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsDockRefreshController.cs").Replace("\r\n", "\n");
        var hardwareRowsControllerStart = refreshControllerText.IndexOf(
            "internal sealed class StatsHardwareRowsController\n",
            StringComparison.Ordinal);
        var hardwareRowsControllerContextText = refreshControllerText.Substring(
            refreshControllerText.IndexOf("internal sealed class StatsHardwareRowsControllerContext", StringComparison.Ordinal),
            refreshControllerText.IndexOf("internal sealed class StatsHardwareRowsInputProviderContext", StringComparison.Ordinal)
                - refreshControllerText.IndexOf("internal sealed class StatsHardwareRowsControllerContext", StringComparison.Ordinal));
        var hardwareRowsControllerText = hardwareRowsControllerContextText + refreshControllerText.Substring(hardwareRowsControllerStart);
        var hardwareRowsInputProviderText = refreshControllerText.Substring(
            refreshControllerText.IndexOf("internal sealed class StatsHardwareRowsInputProviderContext", StringComparison.Ordinal),
            hardwareRowsControllerStart
                - refreshControllerText.IndexOf("internal sealed class StatsHardwareRowsInputProviderContext", StringComparison.Ordinal));
        var hardwareRowsInputBuilderText = hardwareRowsInputProviderText;
        var hardwareRowsBuilderText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");

        AssertContains(statsDockCompositionText, "private static StatsDiagnosticRowsController CreateDiagnosticRowsController(");
        AssertContains(statsDockCompositionText, "private static StatsDockRowChromeController CreateRowChromeController(");
        AssertContains(statsDockCompositionText, "private static StatsHardwareRowsController CreateHardwareRowsController(");
        AssertContains(statsDockCompositionText, "ResourceOwner = context.StatsDockPanel");
        AssertContains(statsDockCompositionText, "DiagnosticsContent = context.DiagnosticsContent");
        AssertContains(statsDockCompositionText, "RowChromeController = statsDockRowChromeController");
        AssertContains(statsDockCompositionText, "private static StatsHardwareRowsInputProvider CreateHardwareRowsInputProvider(");
        AssertContains(statsDockCompositionText, "GetMjpegPipelineTimingDetails = context.GetMjpegPipelineTimingDetails,");
        AssertContains(statsDockCompositionText, "GetPendingPreviewFrameCount = context.GetPendingPreviewFrameCount,");
        AssertContains(statsDockCompositionText, "GetNvmlSnapshot = context.GetNvmlSnapshot");
        AssertContains(statsDockCompositionText, "InputProvider = statsHardwareRowsInputProvider");
        AssertOccursBefore(statsDockCompositionText, "var statsHardwareRowsInputProvider = CreateHardwareRowsInputProvider(context);", "var statsHardwareRowsController = CreateHardwareRowsController(");
        AssertDoesNotContain(statsDockCompositionText, "GetDecodeRowsInput = () =>");
        AssertDoesNotContain(statsDockCompositionText, "StatsHardwareRowsInputBuilder.BuildDecodeRowsInput(");
        AssertDoesNotContain(statsDockCompositionText, "StatsHardwareRowsInputBuilder.BuildGpuRowsInput(");
        AssertContains(refreshControllerText, "_context.DiagnosticRowsController.UpdateDiagnostics(presentation);");
        AssertContains(refreshControllerText, "_context.HardwareRowsController.UpdateDecodeSection();");
        AssertContains(refreshControllerText, "_context.HardwareRowsController.UpdateGpuSection();");
        AssertContains(hardwareRowsControllerText, "internal sealed class StatsHardwareRowsControllerContext");
        AssertContains(hardwareRowsControllerText, "internal sealed class StatsHardwareRowsController");
        AssertContains(hardwareRowsControllerText, "private const int MaxExpectedDecodeRowCount = 14;");
        AssertContains(hardwareRowsControllerText, "private const int FixedGpuRowCount = 10;");
        AssertContains(hardwareRowsControllerText, "public void UpdateDecodeSection()");
        AssertContains(hardwareRowsControllerText, "public void UpdateGpuSection()");
        AssertContains(hardwareRowsControllerText, "StatsPresentationBuilder.BuildHardwareDecodeRows(");
        AssertContains(hardwareRowsControllerText, "public required StatsHardwareRowsInputProvider InputProvider { get; init; }");
        AssertContains(hardwareRowsControllerText, "var input = _context.InputProvider.GetDecodeRowsInput();");
        AssertContains(hardwareRowsControllerText, "StatsPresentationBuilder.BuildHardwareDecodeRows(input.Value)");
        AssertContains(hardwareRowsControllerText, "StatsPresentationBuilder.BuildHardwareGpuRows(_context.InputProvider.GetGpuRowsInput())");
        AssertDoesNotContain(hardwareRowsControllerText, "public required Func<StatsHardwareDecodeRowsInput?> GetDecodeRowsInput { get; init; }");
        AssertDoesNotContain(hardwareRowsControllerText, "public required Func<StatsHardwareGpuRowsInput?> GetGpuRowsInput { get; init; }");
        AssertContains(hardwareRowsInputProviderText, "internal sealed class StatsHardwareRowsInputProviderContext");
        AssertContains(hardwareRowsInputProviderText, "internal sealed class StatsHardwareRowsInputProvider");
        AssertContains(hardwareRowsInputProviderText, "public required Func<ParallelMjpegDecodePipeline.PipelineTimingMetrics?> GetMjpegPipelineTimingDetails { get; init; }");
        AssertContains(hardwareRowsInputProviderText, "public required Func<int?> GetPendingPreviewFrameCount { get; init; }");
        AssertContains(hardwareRowsInputProviderText, "public required Func<NvmlSnapshot?> GetNvmlSnapshot { get; init; }");
        AssertContains(hardwareRowsInputProviderText, "var mjpegMetrics = _context.GetMjpegPipelineTimingDetails();");
        AssertContains(hardwareRowsInputProviderText, "if (!mjpegMetrics.HasValue || mjpegMetrics.Value.DecoderCount <= 0)");
        AssertContains(hardwareRowsInputProviderText, "StatsHardwareRowsInputBuilder.BuildDecodeRowsInput(");
        AssertContains(hardwareRowsInputProviderText, "_context.GetPendingPreviewFrameCount()");
        AssertContains(hardwareRowsInputProviderText, "StatsHardwareRowsInputBuilder.BuildGpuRowsInput(_context.GetNvmlSnapshot())");
        AssertContains(hardwareRowsInputBuilderText, "internal static class StatsHardwareRowsInputBuilder");
        AssertContains(hardwareRowsInputBuilderText, "public static StatsHardwareDecodeRowsInput BuildDecodeRowsInput(");
        AssertContains(hardwareRowsInputBuilderText, "ParallelMjpegDecodePipeline.PipelineTimingMetrics mjpeg,");
        AssertContains(hardwareRowsInputBuilderText, "public static StatsHardwareGpuRowsInput? BuildGpuRowsInput(NvmlSnapshot? nvml)");
        AssertContains(hardwareRowsInputBuilderText, "PcieTxMBps: nvml.PcieTxMBps,");
        AssertContains(hardwareRowsInputBuilderText, "VramUsedMB: nvml.VramUsedMB,");
        AssertContains(hardwareRowsInputBuilderText, "GpuPowerW: nvml.GpuPowerW,");
        AssertContains(hardwareRowsBuilderText, "public static IReadOnlyList<StatsHardwareRowPresentation> BuildHardwareDecodeRows(");
        AssertContains(hardwareRowsBuilderText, "StatsHardwareDecodeRowsInput mjpeg)");
        AssertContains(hardwareRowsBuilderText, "public static IReadOnlyList<StatsHardwareRowPresentation> BuildHardwareGpuRows(StatsHardwareGpuRowsInput? nvml)");
        AssertDoesNotContain(hardwareRowsBuilderText, "using Sussudio.Services.Gpu;");
        AssertContains(refreshControllerText, "using Sussudio.Services.Gpu;");
        AssertContains(hardwareRowsBuilderText, "internal readonly record struct StatsHardwareRowPresentation(string Label, string Value);");
        AssertContains(hardwareRowsBuilderText, "internal readonly record struct StatsHardwareDecodeRowsInput(");
        AssertContains(hardwareRowsBuilderText, "internal readonly record struct StatsHardwareGpuRowsInput(");
        AssertContains(hardwareRowsControllerText, "_context.RowChromeController.CollapseSimpleRows(StatsDockSimpleRowPool.Decode);");
        AssertContains(hardwareRowsControllerText, "_context.RowChromeController.UpdateSimpleRows(");
        AssertContains(hardwareRowsControllerText, "StatsDockSimpleRowPool.Decode,");
        AssertContains(hardwareRowsControllerText, "StatsDockSimpleRowPool.Gpu,");
        AssertDoesNotContain(hardwareRowsControllerText, "private static StatsHardwareDecodeRowsInput CreateDecodeRowsInput(");
        AssertDoesNotContain(hardwareRowsControllerText, "private static StatsHardwareGpuRowsInput? CreateGpuRowsInput(");
        AssertDoesNotContain(hardwareRowsControllerText, "new StatsHardwareDecodeRowsInput(");
        AssertDoesNotContain(hardwareRowsControllerText, "new StatsHardwareGpuRowsInput(");
        AssertDoesNotContain(hardwareRowsControllerText, "using Sussudio.Services.Gpu;");
        AssertDoesNotContain(hardwareRowsControllerText, "GetMjpegPipelineTimingDetails");
        AssertDoesNotContain(hardwareRowsControllerText, "GetPendingPreviewFrameCount");
        AssertDoesNotContain(hardwareRowsControllerText, "GetNvmlSnapshot");
        AssertContains(refreshControllerText, "using Sussudio.Services.Gpu;");
        AssertContains(controllerText, "internal sealed class StatsDiagnosticRowsControllerContext");
        AssertContains(controllerText, "internal sealed class StatsDiagnosticRowsController");
        AssertContains(controllerText, "public required FrameworkElement ResourceOwner { get; init; }");
        AssertContains(controllerText, "public required StackPanel DiagnosticsContent { get; init; }");
        AssertContains(controllerText, "private readonly List<DiagnosticsPoolSlot> _diagnosticsRowPool = new();");
        AssertContains(controllerText, "private TextBlock? _diagnosticsEmptyStateTextBlock;");
        AssertContains(controllerText, "private readonly StatsDockRowChromePresenter _rowChrome;");
        AssertContains(controllerText, "public void UpdateDiagnostics(StatsDiagnosticRowsPresentation presentation)");
        AssertContains(controllerText, "Text = \"No diagnostics available\",");
        AssertContains(controllerText, "private void EnsureDiagnosticsPoolCapacity(int requiredCount)");
        AssertContains(controllerText, "private void UpdateDiagnosticsPoolSlot(");
        AssertContains(controllerText, "private TextBlock CreateDiagnosticGroupHeader(string title)");
        AssertContains(controllerText, "var rowSlot = _rowChrome.CreateRowSlot();");
        AssertContains(controllerText, "_rowChrome.UpdateRowSlot(slot.RowSlot, label, value, alt);");
        AssertContains(controllerText, "StatsDockRowChromePresenter.SetVisibilityIfChanged(slot.RowSlot.Row, Visibility.Collapsed);");
        AssertDoesNotContain(controllerText, "_context.RowChromeController.UpdateDiagnosticsRows(presentation);");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Controllers", "Stats", "StatsDiagnosticRowsController.cs")),
            "diagnostic stats rows folded into StatsDockRowsController.cs");
        AssertContains(rowChromeControllerText, "internal sealed class StatsDockRowChromeControllerContext");
        AssertContains(rowChromeControllerText, "internal sealed class StatsDockRowChromeController");
        AssertContains(rowChromeControllerText, "internal enum StatsDockSimpleRowPool");
        AssertContains(rowChromeControllerText, "private readonly StatsDockRowChromePresenter _rowChrome;");
        AssertContains(rowChromeControllerText, "private readonly List<StatsDockRowChromeSlot> _decodeRowPool = new();");
        AssertContains(rowChromeControllerText, "private readonly List<StatsDockRowChromeSlot> _gpuRowPool = new();");
        AssertContains(rowChromeControllerText, "public void CollapseSimpleRows(StatsDockSimpleRowPool poolKind)");
        AssertContains(rowChromeControllerText, "public void UpdateSimpleRows(");
        AssertContains(rowChromeControllerText, "_rowChrome.UpdateRowSlot(pool[i], row.Label, row.Value, alt: (i % 2) != 0);");
        AssertContains(rowChromeControllerText, "StatsDockRowChromePresenter.CollapseRows(pool, startIndex: rows.Count);");
        AssertContains(rowChromePresenterText, "internal sealed record StatsDockRowChromeSlot(Border Row, TextBlock Label, TextBlock Value);");
        AssertContains(rowChromePresenterText, "internal sealed class StatsDockRowChromePresenter");
        AssertContains(rowChromePresenterText, "public StatsDockRowChromeSlot CreateRowSlot(string label = \"\", string value = \"\", bool alt = false)");
        AssertContains(rowChromePresenterText, "public void UpdateRowSlot(StatsDockRowChromeSlot slot, string label, string value, bool alt)");
        AssertContains(rowChromePresenterText, "Style = GetStyle(\"DockStatsLabelStyle\")");
        AssertContains(rowChromePresenterText, "Style = GetStyle(\"DockStatsValueStyle\"),");
        AssertContains(rowChromePresenterText, "=> GetStyle(alt ? \"DockStatsRowAltStyle\" : \"DockStatsRowStyle\");");
        AssertContains(rowChromePresenterText, "public static void CollapseRows(IReadOnlyList<StatsDockRowChromeSlot> pool, int startIndex = 0)");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Controllers", "Stats", "StatsDockRowChromePresenter.cs")),
            "stats dock row chrome folded into StatsDockRowsController.cs");
        AssertDoesNotContain(rowChromeControllerText, "public void UpdateDiagnosticsRows(StatsDiagnosticRowsPresentation presentation)");
        AssertDoesNotContain(rowChromeControllerText, "private Border CreateRow(");
        AssertDoesNotContain(controllerText, "private Border CreateRow(");
        AssertDoesNotContain(rowChromeControllerText, "Style = GetStyle(alt ? \"DockStatsRowAltStyle\" : \"DockStatsRowStyle\"),");
        AssertDoesNotContain(controllerText, "Style = GetStyle(alt ? \"DockStatsRowAltStyle\" : \"DockStatsRowStyle\"),");
        AssertDoesNotContain(hardwareRowsControllerText, "new List<StatsHardwareRowPresentation>");
        AssertDoesNotContain(hardwareRowsControllerText, "public static IReadOnlyList<StatsHardwareRowPresentation> BuildHardwareGpuRows(");
        AssertDoesNotContain(hardwareRowsControllerText, "StatsDiagnosticRowsController");
        AssertDoesNotContain(controllerText, "private Border CreateDiagnosticRow(");
        AssertDoesNotContain(mainWindowText, "_decodeRowPool");
        AssertDoesNotContain(mainWindowText, "_diagnosticsRowPool");
        AssertDoesNotContain(statsOverlayText, "private sealed record DiagnosticRowSlot(");
        AssertDoesNotContain(statsOverlayText, "private void EnsureDiagnosticRowPool(");
        AssertDoesNotContain(statsOverlayText, "private Border CreateDiagnosticRow(");
        AssertDoesNotContain(statsOverlayText, "private void UpdateDecodeSection()");
        AssertDoesNotContain(statsOverlayText, "private void UpdateGpuSection()");
        AssertDoesNotContain(statsOverlayText, "_statsDiagnosticRowsController.UpdateDiagnostics(presentation);");
        AssertDoesNotContain(statsOverlayText, "new List<StatsHardwareRowPresentation>");
    }

    private static void AssertStatsPresentationPreviewFormattingLivesInBuilder(
        string statsPresentationText,
        string statsOverlayText,
        string frameTimeOverlayText,
        string frameTimeOverlayControllerText)
    {
        Assert.Contains("private static string FormatPreviewCadenceSummary(StatsSnapshot snapshot)", statsPresentationText);
        Assert.Contains("private static double ResolveCurrentPreviewFrameTimeMs(StatsSnapshot snapshot)", statsPresentationText);
        Assert.Contains("ResolveCurrentPreviewFrameTimeMs(snapshot)", statsPresentationText);
        Assert.Contains("1% low {FormatFps(snapshot.PreviewOnePercentLowFps)} fps", statsPresentationText);
        Assert.Contains("return $\"{currentFrameTime} | {onePercentLow}\";", statsPresentationText);
        Assert.DoesNotContain("private static string FormatPreviewCadenceSummary(", statsOverlayText);
        Assert.DoesNotContain("private static double ResolveCurrentPreviewFrameTimeMs(", statsOverlayText);
        Assert.DoesNotContain("private static string FormatPreviewCadenceSummary(", frameTimeOverlayText);
        Assert.DoesNotContain("private static double ResolveCurrentPreviewFrameTimeMs(", frameTimeOverlayText);
        Assert.DoesNotContain("private static string FormatPreviewCadenceSummary(", frameTimeOverlayControllerText);
        Assert.DoesNotContain("private static double ResolveCurrentPreviewFrameTimeMs(", frameTimeOverlayControllerText);
    }

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static string ReadRepoFile(string relativePath)
    {
        var path = Path.Combine(FindRepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null)
        {
            var gitPath = Path.Combine(dir.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return Environment.CurrentDirectory;
    }

    private static object CreateUninitializedObject(Type type)
        => RuntimeHelpers.GetUninitializedObject(type);

    private static void SetPropertyBackingField(object instance, string propertyName, object? value)
    {
        var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", ReflectionFlags.Instance)
            ?? throw new InvalidOperationException($"Backing field for {propertyName} was not found.");
        field.SetValue(instance, value);
    }

    private static object? GetPropertyValue(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance)!.GetValue(instance);

    private static string GetStringProperty(object instance, string propertyName)
        => GetPropertyValue(instance, propertyName) as string
           ?? throw new InvalidOperationException($"{propertyName} was not a string.");

    private static bool GetBoolProperty(object instance, string propertyName)
        => (bool)(GetPropertyValue(instance, propertyName)
                  ?? throw new InvalidOperationException($"{propertyName} was not a bool."));

    private static double GetDoubleProperty(object instance, string propertyName)
        => Convert.ToDouble(GetPropertyValue(instance, propertyName), System.Globalization.CultureInfo.InvariantCulture);

    private static void AssertNearlyEqual(double expected, double actual, double tolerance)
        => Assert.True(
            Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected:0.####}, got {actual:0.####}; tolerance {tolerance:0.####}.");

    private static void AssertContains(string actual, string expectedSubstring)
        => Assert.Contains(expectedSubstring, actual, StringComparison.Ordinal);

    private static void AssertDoesNotContain(string actual, string unexpectedSubstring)
        => Assert.DoesNotContain(unexpectedSubstring, actual, StringComparison.Ordinal);

    private static void AssertOccursBefore(string actual, string first, string second)
    {
        var firstIndex = actual.IndexOf(first, StringComparison.Ordinal);
        if (firstIndex < 0)
        {
            throw new InvalidOperationException($"Assertion failed: expected source to contain '{first}'.");
        }

        var secondIndex = actual.IndexOf(second, StringComparison.Ordinal);
        if (secondIndex < 0)
        {
            throw new InvalidOperationException($"Assertion failed: expected source to contain '{second}'.");
        }

        Assert.True(firstIndex < secondIndex, $"Expected '{first}' to occur before '{second}'.");
    }
}
