using System.Threading.Tasks;

static partial class Program
{
    private static Task StatsLiveSummary_ShowsCurrentPreviewFrameTimeAndOnePercentLow()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var statsOverlayCompositionText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs").Replace("\r\n", "\n");
        var dockPresentationControllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsDockPresentationController.cs").Replace("\r\n", "\n");
        var statsSnapshotProviderText = ReadRepoFile("Sussudio/Controllers/Stats/StatsSnapshotProvider.cs").Replace("\r\n", "\n");
        var frameTimeOverlayText = statsOverlayCompositionText;
        var frameTimeOverlayControllerText = ReadRepoFile("Sussudio/Controllers/Stats/FrameTimeOverlayPresentationController.cs").Replace("\r\n", "\n");
        var frameTimeOverlayGeometryText = ReadRepoFile("Sussudio/Controllers/Stats/FrameTimeOverlayGeometry.cs").Replace("\r\n", "\n");
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");
        var statsPresentationFrameTimeText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.FrameTime.cs").Replace("\r\n", "\n");
        var statsSnapshotBuilderText = ReadRepoFile("Sussudio/ViewModels/StatsSnapshotBuilder.cs").Replace("\r\n", "\n");
        var statsSnapshotText = ReadRepoFile("Sussudio/ViewModels/StatsSnapshot.cs").Replace("\r\n", "\n");
        var mainWindowXaml = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs").Replace("\r\n", "\n");

        AssertContains(statsSnapshotProviderText, "PreviewOnePercentLowFps: presentCadence?.OnePercentLowFps ?? 0");
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

}
