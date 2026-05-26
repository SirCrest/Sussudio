using Xunit;

namespace Sussudio.Tests;

public partial class StatsPresentationTests
{
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
        var statsPresentationModelsText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationModels.cs").Replace("\r\n", "\n");
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
        AssertDoesNotContain(statsPresentationText, "internal sealed record StatsDockPresentation(");
        AssertDoesNotContain(statsPresentationText, "internal sealed record StatsWindowPresentation(");
        AssertDoesNotContain(statsPresentationText, "internal enum StatsMetricStatus");
        AssertContains(statsDockRefreshControllerText, "var presentation = StatsPresentationBuilder.BuildDockPresentation(snapshot);");
        AssertContains(frameTimeOverlayText, "_frameTimeOverlayPresentationController.Apply(snapshot);");
        AssertContains(frameTimeOverlayControllerText, "internal sealed class FrameTimeOverlayPresentationController");
        Assert.False(
            System.IO.File.Exists(System.IO.Path.Combine(
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
            System.IO.File.Exists(System.IO.Path.Combine(
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
}
