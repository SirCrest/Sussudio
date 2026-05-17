using System.Reflection;

static partial class Program
{
    private static Task StatsSnapshotConstruction_LivesInFocusedBuilder()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var mainWindowStatsSnapshotText = ReadRepoFile("Sussudio/MainWindow.StatsSnapshot.cs").Replace("\r\n", "\n");
        var statsSnapshotProviderText = ReadRepoFile("Sussudio/Controllers/Stats/StatsSnapshotProvider.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var statsSnapshotBuilderText = ReadRepoFile("Sussudio/ViewModels/StatsSnapshotBuilder.cs").Replace("\r\n", "\n");
        var statsSnapshotText = ReadRepoFile("Sussudio/ViewModels/StatsSnapshot.cs").Replace("\r\n", "\n");
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs").Replace("\r\n", "\n");

        AssertContains(statsSnapshotBuilderText, "internal static class StatsSnapshotBuilder");
        AssertContains(statsSnapshotBuilderText, "public static StatsSnapshot Build(");
        AssertContains(statsSnapshotBuilderText, "internal readonly record struct StatsSnapshotRenderMetrics(");
        AssertContains(statsSnapshotBuilderText, "internal readonly record struct StatsSnapshotViewState(");
        AssertContains(statsSnapshotBuilderText, "return new StatsSnapshot(");
        AssertContains(statsSnapshotText, "public sealed record StatsSnapshot(");
        AssertContains(mainWindowText, "InitializeStatsSnapshotProvider();");
        AssertContains(mainWindowStatsSnapshotText, "private StatsSnapshot GetStatsSnapshot()");
        AssertContains(mainWindowStatsSnapshotText, "private StatsSnapshotProvider _statsSnapshotProvider = null!;");
        AssertContains(mainWindowStatsSnapshotText, "GetCaptureHealthSnapshot = ViewModel.GetCaptureHealthSnapshot,");
        AssertContains(mainWindowStatsSnapshotText, "GetRenderer = () => _previewRendererHostController.Renderer,");
        AssertContains(mainWindowStatsSnapshotText, "GetPreviewMinPresentationIntervalMs = () => _previewRendererHostController.PreviewMinPresentationIntervalMs");
        AssertContains(mainWindowStatsSnapshotText, "IsPreviewing = () => ViewModel.IsPreviewing,");
        AssertContains(mainWindowStatsSnapshotText, "IsRecording = () => ViewModel.IsRecording");
        AssertContains(mainWindowStatsSnapshotText, "=> _statsSnapshotProvider.GetSnapshot();");
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
        AssertDoesNotContain(mainWindowStatsSnapshotText, "var renderer = new StatsSnapshotRenderMetrics(");
        AssertDoesNotContain(mainWindowStatsSnapshotText, "return StatsSnapshotBuilder.Build(health, renderer, viewState);");
        AssertDoesNotContain(statsOverlayText, "var renderer = new StatsSnapshotRenderMetrics(");
        AssertDoesNotContain(statsOverlayText, "return new StatsSnapshot(");
        AssertContains(statsWindowText, "private readonly Func<StatsSnapshot> _dataProvider;");
        AssertDoesNotContain(statsWindowText, "public sealed record StatsSnapshot(");

        return Task.CompletedTask;
    }

    private static Task StatsSnapshotBuilder_MapsHealthAndRendererMetrics()
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

        AssertEqual(60, GetIntProperty(snapshot, "SourceCadenceSamples"), "SourceCadenceSamples");
        AssertNearlyEqual(119.8d, GetDoubleProperty(snapshot, "SourceObservedFps"), 0.0001, "SourceObservedFps");
        AssertEqual(20, GetIntProperty(snapshot, "PreviewCadenceSamples"), "PreviewCadenceSamples");
        AssertNearlyEqual(118.2d, GetDoubleProperty(snapshot, "PreviewOnePercentLowFps"), 0.0001, "PreviewOnePercentLowFps");
        AssertNearlyEqual(0.0d, GetDoubleProperty(snapshot, "PreviewSlowPct"), 0.0001, "PreviewSlowPct sanitizes NaN");
        AssertNearlyEqual(99.5d, GetDoubleProperty(snapshot, "PerformanceScore"), 0.0001, "PerformanceScore");
        AssertEqual(true, GetBoolProperty(snapshot, "Previewing"), "Previewing");
        AssertEqual(false, GetBoolProperty(snapshot, "Recording"), "Recording");
        AssertEqual(1920, GetIntProperty(snapshot, "CaptureWidth"), "CaptureWidth");
        AssertEqual("NativeXu", GetStringProperty(snapshot, "TelemetryOrigin"), "TelemetryOrigin");
        AssertEqual("High", GetStringProperty(snapshot, "TelemetryConfidence"), "TelemetryConfidence");
        AssertEqual("Warning", GetStringProperty(snapshot, "DiagnosticHealthStatus"), "DiagnosticHealthStatus");
        AssertEqual("source_capture", GetStringProperty(snapshot, "DiagnosticLikelyStage"), "DiagnosticLikelyStage");
        AssertEqual(2, GetCountProperty(GetPropertyValue(snapshot, "SourceTelemetryDetails")), "SourceTelemetryDetails count");
        AssertEqual(2, GetCountProperty(GetPropertyValue(snapshot, "PreviewRecentPresentIntervalsMs")), "PreviewRecentPresentIntervalsMs count");

        return Task.CompletedTask;
    }
}
