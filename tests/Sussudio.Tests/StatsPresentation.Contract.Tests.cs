using System.Reflection;

static partial class Program
{
    private static Task StatsPresentationLogic_LivesInFocusedBuilder()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var frameTimeOverlayText = ReadRepoFile("Sussudio/MainWindow.FrameTimeOverlay.cs").Replace("\r\n", "\n");
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");
        var statsPresentationDiagnosticsText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.Diagnostics.cs").Replace("\r\n", "\n");
        var statsPresentationModelsText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationModels.cs").Replace("\r\n", "\n");

        AssertContains(statsPresentationText, "internal static partial class StatsPresentationBuilder");
        AssertContains(statsPresentationText, "public static StatsDockPresentation BuildDockPresentation(StatsSnapshot snapshot)");
        AssertContains(statsPresentationText, "public static StatsFrameTimePresentation BuildFrameTimePresentation(StatsSnapshot snapshot)");
        AssertContains(statsPresentationDiagnosticsText, "internal static partial class StatsPresentationBuilder");
        AssertContains(statsPresentationDiagnosticsText, "public static StatsDiagnosticRowsPresentation BuildDiagnosticRows(");
        AssertContains(statsPresentationDiagnosticsText, "public static StatsDiagnosticSummary BuildStatsDiagnosticSummary(");
        AssertContains(statsPresentationDiagnosticsText, "private static List<(string Label, string Value)> ParseDiagnosticSummary");
        AssertDoesNotContain(statsPresentationText, "public static StatsDiagnosticRowsPresentation BuildDiagnosticRows(");
        AssertDoesNotContain(statsPresentationText, "private static List<(string Label, string Value)> ParseDiagnosticSummary");
        AssertContains(statsPresentationModelsText, "internal sealed record StatsDockPresentation(");
        AssertContains(statsPresentationModelsText, "internal sealed record StatsFrameTimePresentation(");
        AssertContains(statsPresentationModelsText, "internal enum StatsMetricStatus");
        AssertDoesNotContain(statsPresentationText, "internal sealed record StatsDockPresentation(");
        AssertDoesNotContain(statsPresentationText, "internal enum StatsMetricStatus");
        AssertContains(statsOverlayText, "var presentation = StatsPresentationBuilder.BuildDockPresentation(snapshot);");
        AssertContains(frameTimeOverlayText, "var presentation = StatsPresentationBuilder.BuildFrameTimePresentation(snapshot);");
        AssertContains(statsOverlayText, "StatsPresentationBuilder.BuildDiagnosticRows(telemetryDetails, diagnosticSummary)");
        AssertDoesNotContain(statsOverlayText, "BuildFrameTimePresentation(snapshot)");
        AssertDoesNotContain(statsOverlayText, "private enum MetricStatus");
        AssertDoesNotContain(statsOverlayText, "private static string ResolveCaptureSummaryText");
        AssertDoesNotContain(statsOverlayText, "private static List<(string Label, string Value)> ParseDiagnosticSummary");

        return Task.CompletedTask;
    }

    private static Task StatsLiveSummary_ShowsCurrentPreviewFrameTimeAndOnePercentLow()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");
        var statsSnapshotBuilderText = ReadRepoFile("Sussudio/ViewModels/StatsSnapshotBuilder.cs").Replace("\r\n", "\n");
        var statsSnapshotText = ReadRepoFile("Sussudio/ViewModels/StatsSnapshot.cs").Replace("\r\n", "\n");
        var mainWindowXaml = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs").Replace("\r\n", "\n");

        AssertContains(statsOverlayText, "PreviewOnePercentLowFps: presentCadence?.OnePercentLowFps ?? 0");
        AssertContains(statsSnapshotBuilderText, "PreviewOnePercentLowFps: StatsPresentationBuilder.Sanitize(renderer.PreviewOnePercentLowFps)");
        AssertContains(statsPresentationText, "ResolveCurrentPreviewFrameTimeMs(snapshot)");
        AssertContains(statsPresentationText, "1% low {FormatFps(snapshot.PreviewOnePercentLowFps)} fps");
        AssertContains(statsPresentationText, "return $\"{currentFrameTime} | {onePercentLow}\";");
        AssertContains(statsOverlayText, "SetMetricBrush(Stats_SummaryRendererFpsValue, presentation.SummaryRendererFpsStatus);");
        AssertContains(statsSnapshotText, "double PreviewOnePercentLowFps");
        AssertDoesNotContain(statsWindowText, "double PreviewFivePercentLowFps");
        AssertContains(mainWindowXaml, "x:Name=\"Stats_SummaryRendererFpsValue\"");
        AssertContains(mainWindowXaml, "TextWrapping=\"NoWrap\"");
        AssertContains(mainWindowXaml, "MaxLines=\"1\"");

        return Task.CompletedTask;
    }

    private static Task FrameTimeOverlay_UsesDetectedFpsBoundedRange()
    {
        var frameTimeOverlayText = ReadRepoFile("Sussudio/MainWindow.FrameTimeOverlay.cs").Replace("\r\n", "\n");
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");
        var mainWindowXaml = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");

        AssertContains(statsPresentationText, "ResolveFrameTimeRange(snapshot.SourceExpectedFps)");
        AssertContains(statsPresentationText, "fps * 0.75");
        AssertContains(statsPresentationText, "fps * 1.25");
        AssertContains(statsPresentationText, "Target {FormatMs(range.ExpectedMs)}");
        AssertContains(statsPresentationText, "range {FormatMs(range.MinMs)}-{FormatMs(range.MaxMs)}");
        AssertDoesNotContain(frameTimeOverlayText, "LowerFpsLabel");
        AssertDoesNotContain(frameTimeOverlayText, "UpperFpsLabel");
        AssertContains(frameTimeOverlayText, "(samples[i] - range.MinMs) / range.SpanMs");
        AssertContains(frameTimeOverlayText, "UpdateFrameTimeExpectedLine");
        AssertContains(mainWindowXaml, "x:Name=\"FrameTime_ExpectedLine\"");

        var presentationType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var resolveRange = presentationType.GetMethod("ResolveFrameTimeRange", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("ResolveFrameTimeRange was not found.");

        var range120 = resolveRange.Invoke(null, new object[] { 120.0 })
            ?? throw new InvalidOperationException("ResolveFrameTimeRange returned null for 120fps.");
        AssertNearlyEqual(1000.0 / 150.0, GetDoubleProperty(range120, "MinMs"), 0.0001, "120fps MinMs");
        AssertNearlyEqual(1000.0 / 90.0, GetDoubleProperty(range120, "MaxMs"), 0.0001, "120fps MaxMs");
        AssertNearlyEqual(1000.0 / 120.0, GetDoubleProperty(range120, "ExpectedMs"), 0.0001, "120fps ExpectedMs");

        var normalizedExpected = (GetDoubleProperty(range120, "ExpectedMs") - GetDoubleProperty(range120, "MinMs")) /
                                 GetDoubleProperty(range120, "SpanMs");
        AssertNearlyEqual(0.375, normalizedExpected, 0.0001, "120fps expected-line normalization");

        var fallbackRange = resolveRange.Invoke(null, new object[] { 0.0 })
            ?? throw new InvalidOperationException("ResolveFrameTimeRange returned null for fallback fps.");
        AssertNearlyEqual(1000.0 / 75.0, GetDoubleProperty(fallbackRange, "MinMs"), 0.0001, "fallback MinMs");
        AssertNearlyEqual(1000.0 / 45.0, GetDoubleProperty(fallbackRange, "MaxMs"), 0.0001, "fallback MaxMs");
        AssertNearlyEqual(1000.0 / 60.0, GetDoubleProperty(fallbackRange, "ExpectedMs"), 0.0001, "fallback ExpectedMs");

        return Task.CompletedTask;
    }
}
