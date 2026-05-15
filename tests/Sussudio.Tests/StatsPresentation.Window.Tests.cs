using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task StatsWindowPresentation_FormatsDetachedWindowText()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var snapshotType = RequireType("Sussudio.StatsSnapshot");
        var buildWindowPresentation = builderType.GetMethod("BuildStatsWindowPresentation", BindingFlags.Static | BindingFlags.Public)
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

        AssertEqual("Previewing", GetStringProperty(presentation, "SessionState"), "Stats window session state");
        AssertEqual("Healthy", GetStringProperty(presentation, "DiagnosticStatus"), "Stats window diagnostic status");
        AssertEqual("All monitored frame lanes are within current thresholds.", GetStringProperty(presentation, "DiagnosticEvidence"), "Stats window diagnostic fallback");
        AssertEqual("3840 x 2160", GetStringProperty(presentation, "SourceResolution"), "Stats window source resolution");
        AssertEqual("119.88 fps", GetStringProperty(presentation, "SourceFrameRate"), "Stats window source frame rate");
        AssertEqual("On (BT.2020)", GetStringProperty(presentation, "SourceHdr"), "Stats window HDR text");
        AssertEqual("YCbCr422", GetStringProperty(presentation, "SourceFormat"), "Stats window source format");
        AssertEqual("NativeXu (High)", GetStringProperty(presentation, "TelemetryOrigin"), "Stats window telemetry origin");
        AssertEqual("119.80", GetStringProperty(presentation, "SourceFps"), "Stats window source FPS");
        AssertEqual("8.33ms avg", GetStringProperty(presentation, "SourceAvg"), "Stats window source average");
        AssertEqual("3 drops (0.3%)", GetStringProperty(presentation, "SourceDrops"), "Stats window source drops");
        AssertEqual("4 frames (1.5%)", GetStringProperty(presentation, "PreviewSlow"), "Stats window preview slow frames");
        AssertEqual("3.40ms avg", GetStringProperty(presentation, "PipelineLatency"), "Stats window latency");
        AssertEqual("98.8 / 100", GetStringProperty(presentation, "PerformanceScore"), "Stats window score");

        var telemetryDetails = GetPropertyValue(presentation, "TelemetryDetails")
            ?? throw new InvalidOperationException("StatsWindowPresentation.TelemetryDetails was null.");
        AssertEqual(true, GetBoolProperty(telemetryDetails, "IsEmpty"), "Stats window telemetry fallback state");
        AssertEqual("All monitored frame lanes are within current thresholds.", GetStringProperty(telemetryDetails, "EmptyText"), "Stats window telemetry fallback text");

        return Task.CompletedTask;
    }
}
