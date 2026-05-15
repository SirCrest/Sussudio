using System.Reflection;

static partial class Program
{
    private static Task StatsPresentationLogic_LivesInFocusedBuilder()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var frameTimeOverlayText = ReadRepoFile("Sussudio/MainWindow.FrameTimeOverlay.cs").Replace("\r\n", "\n");
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");
        var statsPresentationFrameTimeText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.FrameTime.cs").Replace("\r\n", "\n");
        var statsPresentationVisualText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.Visual.cs").Replace("\r\n", "\n");
        var statsPresentationEncoderText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.Encoder.cs").Replace("\r\n", "\n");
        var statsPresentationDiagnosticsText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.Diagnostics.cs").Replace("\r\n", "\n");
        var statsPresentationStatusText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.Status.cs").Replace("\r\n", "\n");
        var statsPresentationModelsText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationModels.cs").Replace("\r\n", "\n");

        AssertContains(statsPresentationText, "internal static partial class StatsPresentationBuilder");
        AssertContains(statsPresentationText, "public static StatsDockPresentation BuildDockPresentation(StatsSnapshot snapshot)");
        AssertContains(statsPresentationFrameTimeText, "internal static partial class StatsPresentationBuilder");
        AssertContains(statsPresentationFrameTimeText, "public static StatsFrameTimePresentation BuildFrameTimePresentation(StatsSnapshot snapshot)");
        AssertContains(statsPresentationVisualText, "internal static partial class StatsPresentationBuilder");
        AssertContains(statsPresentationVisualText, "private static string FormatVisualRepeatSummary(StatsSnapshot snapshot)");
        AssertContains(statsPresentationVisualText, "private static string FormatVisualCadenceSummary(StatsSnapshot snapshot)");
        AssertContains(statsPresentationVisualText, "private static string FormatVisualMotionSummary(StatsSnapshot snapshot)");
        AssertContains(statsPresentationVisualText, "private static string FormatHz(double value)");
        AssertContains(statsPresentationEncoderText, "internal static partial class StatsPresentationBuilder");
        AssertContains(statsPresentationEncoderText, "private static StatsEncoderPresentation BuildEncoderPresentation(StatsSnapshot snapshot)");
        AssertContains(statsPresentationEncoderText, "private static string FormatEncoderCodecName(string codecName)");
        AssertContains(statsPresentationEncoderText, "private static string FormatEncoderBitrate(uint targetBitRate)");
        AssertContains(statsPresentationEncoderText, "private static string FormatEncoderDrift(StatsSnapshot snapshot)");
        AssertContains(statsPresentationDiagnosticsText, "internal static partial class StatsPresentationBuilder");
        AssertContains(statsPresentationDiagnosticsText, "public static StatsDiagnosticRowsPresentation BuildDiagnosticRows(");
        AssertContains(statsPresentationDiagnosticsText, "public static StatsDiagnosticSummary BuildStatsDiagnosticSummary(");
        AssertContains(statsPresentationDiagnosticsText, "private static List<(string Label, string Value)> ParseDiagnosticSummary");
        AssertContains(statsPresentationStatusText, "internal static partial class StatsPresentationBuilder");
        AssertContains(statsPresentationStatusText, "private static StatsMetricStatus ResolveFrameLaneStatus(");
        AssertContains(statsPresentationStatusText, "private static StatsMetricStatus ResolveDecodedVisualStatus(StatsSnapshot snapshot)");
        AssertContains(statsPresentationStatusText, "private static bool IsVisualRepeatWithinExpectedDrift(StatsSnapshot snapshot)");
        AssertDoesNotContain(statsPresentationText, "public static StatsFrameTimePresentation BuildFrameTimePresentation(StatsSnapshot snapshot)");
        AssertDoesNotContain(statsPresentationText, "private static string FormatVisualRepeatSummary(");
        AssertDoesNotContain(statsPresentationText, "private static string FormatVisualCadenceSummary(");
        AssertDoesNotContain(statsPresentationText, "private static string FormatVisualMotionSummary(");
        AssertDoesNotContain(statsPresentationText, "snapshot.EncoderCodecName switch");
        AssertDoesNotContain(statsPresentationText, "snapshot.EncoderTargetBitRate / 1_000_000.0");
        AssertDoesNotContain(statsPresentationText, "public static StatsDiagnosticRowsPresentation BuildDiagnosticRows(");
        AssertDoesNotContain(statsPresentationText, "private static List<(string Label, string Value)> ParseDiagnosticSummary");
        AssertDoesNotContain(statsPresentationText, "private static StatsMetricStatus ResolveFrameLaneStatus(");
        AssertDoesNotContain(statsPresentationText, "private static bool IsVisualRepeatWithinExpectedDrift(StatsSnapshot snapshot)");
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

    private static Task StatsDockEncoderPresentation_FormatsCodecAndBitrate()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var snapshotType = RequireType("Sussudio.StatsSnapshot");
        var buildDockPresentation = builderType.GetMethod("BuildDockPresentation", BindingFlags.Static | BindingFlags.Public)
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
        AssertEqual(true, GetBoolProperty(hevc, "EncoderActive"), "HEVC encoder active");
        AssertEqual("HEVC (NVENC)", GetStringProperty(hevc, "EncoderCodec"), "HEVC encoder label");
        AssertEqual("3840 x 2160", GetStringProperty(hevc, "EncoderResolution"), "HEVC encoder resolution");
        AssertEqual("59.94 fps", GetStringProperty(hevc, "EncoderFrameRate"), "HEVC encoder frame rate");
        AssertEqual("50 Mbps", GetStringProperty(hevc, "EncoderBitrate"), "HEVC encoder bitrate");
        AssertEqual(true, GetBoolProperty(hevc, "EncoderDriftVisible"), "encoder drift visible while recording");
        AssertEqual("+2.2ms (7 corr)", GetStringProperty(hevc, "EncoderDrift"), "encoder drift text");

        var av1 = Build("av1_nvenc");
        AssertEqual("AV1 (NVENC)", GetStringProperty(av1, "EncoderCodec"), "AV1 encoder label");

        var passthrough = Build("software_custom");
        AssertEqual("software_custom", GetStringProperty(passthrough, "EncoderCodec"), "unknown encoder label passthrough");

        var inactive = Build(null);
        AssertEqual(false, GetBoolProperty(inactive, "EncoderActive"), "inactive encoder hidden");
        AssertEqual(string.Empty, GetStringProperty(inactive, "EncoderCodec"), "inactive encoder codec");

        var idleDrift = Build("h264_nvenc", recording: false);
        AssertEqual(false, GetBoolProperty(idleDrift, "EncoderDriftVisible"), "encoder drift hidden while idle");
        AssertEqual(string.Empty, GetStringProperty(idleDrift, "EncoderDrift"), "idle encoder drift text");

        return Task.CompletedTask;
    }

    private static Task StatsLiveSummary_ShowsCurrentPreviewFrameTimeAndOnePercentLow()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var dockPresentationControllerText = ReadRepoFile("Sussudio/Controllers/StatsDockPresentationController.cs").Replace("\r\n", "\n");
        var mainWindowStatsSnapshotText = ReadRepoFile("Sussudio/MainWindow.StatsSnapshot.cs").Replace("\r\n", "\n");
        var frameTimeOverlayText = ReadRepoFile("Sussudio/MainWindow.FrameTimeOverlay.cs").Replace("\r\n", "\n");
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");
        var statsPresentationFrameTimeText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.FrameTime.cs").Replace("\r\n", "\n");
        var statsSnapshotBuilderText = ReadRepoFile("Sussudio/ViewModels/StatsSnapshotBuilder.cs").Replace("\r\n", "\n");
        var statsSnapshotText = ReadRepoFile("Sussudio/ViewModels/StatsSnapshot.cs").Replace("\r\n", "\n");
        var mainWindowXaml = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs").Replace("\r\n", "\n");

        AssertContains(mainWindowStatsSnapshotText, "PreviewOnePercentLowFps: presentCadence?.OnePercentLowFps ?? 0");
        AssertContains(statsSnapshotBuilderText, "PreviewOnePercentLowFps: StatsPresentationBuilder.Sanitize(renderer.PreviewOnePercentLowFps)");
        AssertStatsPresentationPreviewFormattingLivesInBuilder(statsPresentationFrameTimeText, statsPresentationText, statsOverlayText, frameTimeOverlayText);
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
        string frameTimeOverlayText)
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
    }

    private static Task FrameTimeOverlay_UsesDetectedFpsBoundedRange()
    {
        var frameTimeOverlayText = ReadRepoFile("Sussudio/MainWindow.FrameTimeOverlay.cs").Replace("\r\n", "\n");
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
