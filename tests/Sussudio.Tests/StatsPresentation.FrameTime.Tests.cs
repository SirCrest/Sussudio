using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task StatsLiveSummary_ShowsCurrentPreviewFrameTimeAndOnePercentLow()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var dockPresentationControllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsDockPresentationController.cs").Replace("\r\n", "\n");
        var statsSnapshotProviderRenderMetricsText = ReadRepoFile("Sussudio/Controllers/Stats/StatsSnapshotProvider.RenderMetrics.cs").Replace("\r\n", "\n");
        var frameTimeOverlayText = ReadRepoFile("Sussudio/MainWindow.FrameTimeOverlay.cs").Replace("\r\n", "\n");
        var frameTimeOverlayControllerText = ReadRepoFile("Sussudio/Controllers/Stats/FrameTimeOverlayPresentationController.cs").Replace("\r\n", "\n");
        var frameTimeOverlayGeometryText = ReadRepoFile("Sussudio/Controllers/Stats/FrameTimeOverlayGeometry.cs").Replace("\r\n", "\n");
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");
        var statsPresentationFrameTimeText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.FrameTime.cs").Replace("\r\n", "\n");
        var statsSnapshotBuilderText = ReadRepoFile("Sussudio/ViewModels/StatsSnapshotBuilder.cs").Replace("\r\n", "\n");
        var statsSnapshotText = ReadRepoFile("Sussudio/ViewModels/StatsSnapshot.cs").Replace("\r\n", "\n");
        var mainWindowXaml = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs").Replace("\r\n", "\n");

        AssertContains(statsSnapshotProviderRenderMetricsText, "PreviewOnePercentLowFps: presentCadence?.OnePercentLowFps ?? 0");
        AssertContains(statsSnapshotBuilderText, "PreviewOnePercentLowFps: StatsPresentationBuilder.Sanitize(renderer.PreviewOnePercentLowFps)");
        AssertStatsPresentationPreviewFormattingLivesInBuilder(statsPresentationFrameTimeText, statsPresentationText, statsOverlayText, frameTimeOverlayText, frameTimeOverlayControllerText);
        AssertContains(frameTimeOverlayGeometryText, "internal static class FrameTimeOverlayGeometry");
        AssertContains(dockPresentationControllerText, "SetMetricBrush(_context.SummaryRendererFpsValue, presentation.SummaryRendererFpsStatus);");
        AssertContains(dockPresentationControllerText, "SetTextIfChanged(_context.PreviewFpsValue, presentation.PreviewFps);");
        AssertDoesNotContain(statsOverlayText, "SetMetricBrush(Stats_SummaryRendererFpsValue");
        AssertContains(statsSnapshotText, "double PreviewOnePercentLowFps");
        AssertDoesNotContain(statsWindowText, "double PreviewFivePercentLowFps");
        AssertContains(mainWindowXaml, "x:Name=\"Stats_SummaryRendererFpsValue\"");
        AssertContains(mainWindowXaml, "TextWrapping=\"NoWrap\"");
        AssertContains(mainWindowXaml, "MaxLines=\"1\"");

        return Task.CompletedTask;
    }

    private static void AssertStatsPresentationPreviewFormattingLivesInBuilder(
        string statsPresentationFrameTimeText,
        string statsPresentationText,
        string statsOverlayText,
        string frameTimeOverlayText,
        string frameTimeOverlayControllerText)
    {
        AssertContains(statsPresentationFrameTimeText, "private static string FormatPreviewCadenceSummary(StatsSnapshot snapshot)");
        AssertContains(statsPresentationFrameTimeText, "private static double ResolveCurrentPreviewFrameTimeMs(StatsSnapshot snapshot)");
        AssertContains(statsPresentationFrameTimeText, "ResolveCurrentPreviewFrameTimeMs(snapshot)");
        AssertContains(statsPresentationFrameTimeText, "1% low {FormatFps(snapshot.PreviewOnePercentLowFps)} fps");
        AssertContains(statsPresentationFrameTimeText, "return $\"{currentFrameTime} | {onePercentLow}\";");
        AssertDoesNotContain(statsPresentationText, "private static string FormatPreviewCadenceSummary(");
        AssertDoesNotContain(statsPresentationText, "private static double ResolveCurrentPreviewFrameTimeMs(");
        AssertDoesNotContain(statsOverlayText, "private static string FormatPreviewCadenceSummary(");
        AssertDoesNotContain(statsOverlayText, "private static double ResolveCurrentPreviewFrameTimeMs(");
        AssertDoesNotContain(frameTimeOverlayText, "private static string FormatPreviewCadenceSummary(");
        AssertDoesNotContain(frameTimeOverlayText, "private static double ResolveCurrentPreviewFrameTimeMs(");
        AssertDoesNotContain(frameTimeOverlayControllerText, "private static string FormatPreviewCadenceSummary(");
        AssertDoesNotContain(frameTimeOverlayControllerText, "private static double ResolveCurrentPreviewFrameTimeMs(");
    }

    private static Task FrameTimeOverlay_UsesDetectedFpsBoundedRange()
    {
        var frameTimeOverlayText = ReadRepoFile("Sussudio/MainWindow.FrameTimeOverlay.cs").Replace("\r\n", "\n");
        var frameTimeOverlayControllerText = ReadRepoFile("Sussudio/Controllers/Stats/FrameTimeOverlayPresentationController.cs").Replace("\r\n", "\n");
        var frameTimeOverlayGeometryText = ReadRepoFile("Sussudio/Controllers/Stats/FrameTimeOverlayGeometry.cs").Replace("\r\n", "\n");
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");
        var statsPresentationFrameTimeText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.FrameTime.cs").Replace("\r\n", "\n");
        var mainWindowXaml = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");

        AssertContains(statsPresentationFrameTimeText, "ResolveFrameTimeRange(snapshot.SourceExpectedFps)");
        AssertContains(statsPresentationFrameTimeText, "fps * 0.75");
        AssertContains(statsPresentationFrameTimeText, "fps * 1.25");
        AssertContains(statsPresentationFrameTimeText, "Target {FormatMs(range.ExpectedMs)}");
        AssertContains(statsPresentationFrameTimeText, "range {FormatMs(range.MinMs)}-{FormatMs(range.MaxMs)}");
        AssertDoesNotContain(statsPresentationText, "public static StatsFrameTimePresentation BuildFrameTimePresentation(StatsSnapshot snapshot)");
        AssertDoesNotContain(frameTimeOverlayText, "LowerFpsLabel");
        AssertDoesNotContain(frameTimeOverlayText, "UpperFpsLabel");
        AssertContains(frameTimeOverlayText, "_frameTimeOverlayPresentationController.Apply(snapshot);");
        AssertContains(frameTimeOverlayControllerText, "FrameTimeOverlayGeometry.ResolveCanvasSize(");
        AssertContains(frameTimeOverlayControllerText, "FrameTimeOverlayGeometry.ProjectSample(i, samples.Count, samples[i], range, canvasSize)");
        AssertContains(frameTimeOverlayControllerText, "FrameTimeOverlayGeometry.ProjectExpectedLine(range, canvasSize)");
        AssertDoesNotContain(frameTimeOverlayControllerText, "(samples[i] - range.MinMs) / range.SpanMs");
        AssertContains(frameTimeOverlayGeometryText, "(frameTimeMs - range.MinMs) / range.SpanMs");
        AssertContains(frameTimeOverlayGeometryText, "public const double FallbackWidth = 500;");
        AssertContains(frameTimeOverlayGeometryText, "public const double FallbackHeight = 92;");
        AssertContains(frameTimeOverlayControllerText, "UpdateExpectedLine");
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

    private static Task FrameTimeOverlayGeometry_ProjectsGraphCoordinates()
    {
        var presentationType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var geometryType = RequireType("Sussudio.Controllers.FrameTimeOverlayGeometry");
        var resolveRange = presentationType.GetMethod("ResolveFrameTimeRange", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("ResolveFrameTimeRange was not found.");
        var resolveCanvasSize = geometryType.GetMethod("ResolveCanvasSize", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("ResolveCanvasSize was not found.");
        var projectSample = geometryType.GetMethod("ProjectSample", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("ProjectSample was not found.");
        var projectExpectedLine = geometryType.GetMethod("ProjectExpectedLine", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("ProjectExpectedLine was not found.");

        var range120 = resolveRange.Invoke(null, new object[] { 120.0 })
            ?? throw new InvalidOperationException("ResolveFrameTimeRange returned null for 120fps.");
        var fallbackCanvasSize = resolveCanvasSize.Invoke(null, new object[] { 1.0, 0.0 })
            ?? throw new InvalidOperationException("ResolveCanvasSize returned null for fallback dimensions.");
        AssertNearlyEqual(500, GetDoubleProperty(fallbackCanvasSize, "Width"), 0.0001, "fallback canvas width");
        AssertNearlyEqual(92, GetDoubleProperty(fallbackCanvasSize, "Height"), 0.0001, "fallback canvas height");

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

        AssertNearlyEqual(0, GetDoubleProperty(minPoint, "X"), 0.0001, "min sample x");
        AssertNearlyEqual(100, GetDoubleProperty(minPoint, "Y"), 0.0001, "min sample y");
        AssertNearlyEqual(150, GetDoubleProperty(expectedPoint, "X"), 0.0001, "expected sample x");
        AssertNearlyEqual(62.5, GetDoubleProperty(expectedPoint, "Y"), 0.0001, "expected sample y");
        AssertNearlyEqual(300, GetDoubleProperty(maxPoint, "X"), 0.0001, "max sample x");
        AssertNearlyEqual(0, GetDoubleProperty(maxPoint, "Y"), 0.0001, "max sample y");
        AssertNearlyEqual(100, GetDoubleProperty(clippedLowPoint, "Y"), 0.0001, "clipped low sample y");
        AssertNearlyEqual(0, GetDoubleProperty(clippedHighPoint, "Y"), 0.0001, "clipped high sample y");

        var expectedLine = projectExpectedLine.Invoke(null, new[] { range120, canvasSize })
            ?? throw new InvalidOperationException("ProjectExpectedLine returned null.");
        AssertNearlyEqual(300, GetDoubleProperty(expectedLine, "X2"), 0.0001, "expected line x2");
        AssertNearlyEqual(62.5, GetDoubleProperty(expectedLine, "Y"), 0.0001, "expected line y");

        return Task.CompletedTask;
    }
}
