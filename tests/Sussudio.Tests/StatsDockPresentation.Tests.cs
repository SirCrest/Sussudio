using System;
using System.IO;
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
        var statsDockCompositionText = ReadRepoFile("Sussudio/Controllers/Stats/StatsDockControllerGraph.cs").Replace("\r\n", "\n");
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
            "stats dock graph context folded into StatsDockControllerGraph.cs");
    }

    [Fact]
    public void StatsDockRowChrome_LivesInFocusedController()
    {
        var statsOverlayText = Sussudio.Tests.MainWindowStatsOverlaySource.Read();
        var statsDockCompositionText = ReadRepoFile("Sussudio/Controllers/Stats/StatsDockControllerGraph.cs").Replace("\r\n", "\n");
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
        var statsPresentationModelsText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationModels.cs").Replace("\r\n", "\n");

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
        AssertContains(statsPresentationModelsText, "internal readonly record struct StatsHardwareRowPresentation(string Label, string Value);");
        AssertContains(statsPresentationModelsText, "internal readonly record struct StatsHardwareDecodeRowsInput(");
        AssertContains(statsPresentationModelsText, "internal readonly record struct StatsHardwareGpuRowsInput(");
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
}
