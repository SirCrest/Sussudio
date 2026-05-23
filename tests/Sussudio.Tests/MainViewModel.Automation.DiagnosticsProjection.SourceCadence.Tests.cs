using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsSourceTelemetryProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var sourceSignalProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs")
            .Replace("\r\n", "\n");
        var sourceTelemetryProjectionText = sourceSignalProjectionText;

        AssertContains(snapshotProjectionText, "var sourceTelemetry = BuildSourceTelemetryProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var sourceFlattening = BuildSourceFlattenedProjection(sourceSignal, sourceTelemetry);");
        AssertContains(snapshotFlatteningText, "SourceTelemetryAvailability = sourceFlattening.Telemetry.SourceTelemetryAvailability,");
        AssertContains(snapshotFlatteningText, "SourceTelemetryDetails = sourceFlattening.Telemetry.SourceTelemetryDetails,");
        AssertContains(snapshotFlatteningText, "SourceTelemetryAgeSeconds = sourceFlattening.Telemetry.SourceTelemetryAgeSeconds,");
        AssertContains(snapshotFlatteningText, "SourceTargetSummaryText = sourceFlattening.Telemetry.SourceTargetSummaryText,");
        AssertContains(sourceSignalProjectionText, "private static SourceFlattenedProjection BuildSourceFlattenedProjection(");
        AssertContains(sourceSignalProjectionText, "Telemetry = BuildSourceTelemetryFlattenedProjection(sourceTelemetry)");
        AssertContains(sourceSignalProjectionText, "private readonly record struct SourceFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAvailability = !string.IsNullOrWhiteSpace(viewModelSnapshot.SourceTelemetryAvailability)");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryDetails = captureRuntime.SourceTelemetryDetails,");

        AssertContains(sourceTelemetryProjectionText, "private static SourceTelemetryProjection BuildSourceTelemetryProjection(");
        AssertContains(sourceTelemetryProjectionText, "private static string PreferKnownTelemetryValue(string viewModelValue, string runtimeValue)");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryAvailability = PreferKnownTelemetryValue(");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryDetails = captureRuntime.SourceTelemetryDetails,");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(");
        AssertContains(sourceTelemetryProjectionText, "private readonly record struct SourceTelemetryProjection");
        AssertContains(sourceTelemetryProjectionText, "private static SourceTelemetryFlattenedProjection BuildSourceTelemetryFlattenedProjection(");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,");
        AssertContains(sourceTelemetryProjectionText, "SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText");
        AssertContains(sourceTelemetryProjectionText, "private readonly record struct SourceTelemetryFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsSourceSignalProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var sourceSignalProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var sourceSignal = BuildSourceSignalProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var sourceFlattening = BuildSourceFlattenedProjection(sourceSignal, sourceTelemetry);");
        AssertContains(snapshotFlatteningText, "DetectedSourceFrameRate = sourceFlattening.Signal.DetectedSourceFrameRate,");
        AssertContains(snapshotFlatteningText, "SourceFrameRateOrigin = sourceFlattening.Signal.SourceFrameRateOrigin,");
        AssertContains(snapshotFlatteningText, "SourceRawTimingHex = sourceFlattening.Signal.SourceRawTimingHex,");
        AssertContains(sourceSignalProjectionText, "private static SourceFlattenedProjection BuildSourceFlattenedProjection(");
        AssertContains(sourceSignalProjectionText, "Signal = BuildSourceSignalFlattenedProjection(sourceSignal),");
        AssertContains(sourceSignalProjectionText, "private static SourceSignalFlattenedProjection BuildSourceSignalFlattenedProjection(");
        AssertContains(sourceSignalProjectionText, "DetectedSourceFrameRate = sourceSignal.DetectedFrameRate,");
        AssertContains(sourceSignalProjectionText, "SourceFrameRateOrigin = sourceSignal.FrameRateOrigin,");
        AssertContains(sourceSignalProjectionText, "SourceRawTimingHex = sourceSignal.RawTimingHex");
        AssertContains(sourceSignalProjectionText, "private readonly record struct SourceSignalFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "DetectedSourceFrameRate = sourceSignal.DetectedFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceFrameRateOrigin = sourceSignal.FrameRateOrigin,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceRawTimingHex = sourceSignal.RawTimingHex,");
        AssertDoesNotContain(snapshotFlatteningText, "DetectedSourceFrameRate = viewModelSnapshot.DetectedSourceFrameRate ?? captureRuntime.DetectedSourceFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceRawTimingHex = captureRuntime.SourceRawTimingHex,");

        AssertContains(sourceSignalProjectionText, "private static SourceSignalProjection BuildSourceSignalProjection(");
        AssertContains(sourceSignalProjectionText, "DetectedFrameRate = viewModelSnapshot.DetectedSourceFrameRate ?? captureRuntime.DetectedSourceFrameRate,");
        AssertContains(sourceSignalProjectionText, "FrameRateOrigin = ResolveSourceFrameRateOrigin(viewModelSnapshot.SourceFrameRateOrigin, captureRuntime.SourceFrameRateOrigin),");
        AssertContains(sourceSignalProjectionText, "RawTimingHex = captureRuntime.SourceRawTimingHex");
        AssertContains(sourceSignalProjectionText, "private static string ResolveSourceFrameRateOrigin(string viewModelOrigin, string runtimeOrigin)");
        AssertContains(sourceSignalProjectionText, "private readonly record struct SourceSignalProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsCaptureCadenceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var captureCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs")
            .Replace("\r\n", "\n");
        var captureCadenceOnlyText = captureCadenceProjectionText[..captureCadenceProjectionText.IndexOf("private static VisualCadenceProjection", System.StringComparison.Ordinal)];

        AssertContains(snapshotProjectionText, "var captureCadence = BuildCaptureCadenceProjection(health);");
        AssertContains(snapshotFlatteningText, "var captureCadenceFlattening = BuildCaptureCadenceFlattenedProjection(captureCadence);");
        AssertContains(snapshotFlatteningText, "ExpectedCaptureFrameRate = captureCadenceFlattening.ExpectedFrameRate,");
        AssertContains(snapshotFlatteningText, "CaptureCadenceEstimatedDroppedFrames = captureCadenceFlattening.EstimatedDroppedFrames,");
        AssertDoesNotContain(snapshotFlatteningText, "ExpectedCaptureFrameRate = health.ExpectedFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCadenceEstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,");
        AssertDoesNotContain(snapshotFlatteningText, "ExpectedCaptureFrameRate = captureCadence.ExpectedFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCadenceEstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames,");

        AssertContains(captureCadenceProjectionText, "private static CaptureCadenceProjection BuildCaptureCadenceProjection(CaptureHealthSnapshot health)");
        AssertContains(captureCadenceProjectionText, "ExpectedFrameRate = health.ExpectedFrameRate,");
        AssertContains(captureCadenceProjectionText, "EstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,");
        AssertContains(captureCadenceProjectionText, "private readonly record struct CaptureCadenceProjection");
        AssertContains(captureCadenceProjectionText, "private static CaptureCadenceFlattenedProjection BuildCaptureCadenceFlattenedProjection(");
        AssertContains(captureCadenceProjectionText, "ExpectedFrameRate = captureCadence.ExpectedFrameRate,");
        AssertContains(captureCadenceProjectionText, "EstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames");
        AssertContains(captureCadenceProjectionText, "private readonly record struct CaptureCadenceFlattenedProjection");
        AssertDoesNotContain(captureCadenceOnlyText, "VisualMotionConfidence");
        AssertDoesNotContain(captureCadenceOnlyText, "VisualCenterRecentChangeIntervalsMs");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsVisualCadenceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var visualCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs")
            .Replace("\r\n", "\n");
        var captureCadenceOnlyText = visualCadenceProjectionText[..visualCadenceProjectionText.IndexOf("private static VisualCadenceProjection", System.StringComparison.Ordinal)];

        AssertContains(snapshotProjectionText, "var visualCadence = BuildVisualCadenceProjection(health);");
        AssertContains(snapshotFlatteningText, "var visualCadenceFlattening = BuildVisualCadenceFlattenedProjection(visualCadence);");
        AssertContains(snapshotFlatteningText, "VisualCadenceMotionConfidence = visualCadenceFlattening.MotionConfidence,");
        AssertContains(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = visualCadenceFlattening.CenterRecentChangeIntervalsMs,");
        AssertContains(visualCadenceProjectionText, "private static VisualCadenceFlattenedProjection BuildVisualCadenceFlattenedProjection(");
        AssertContains(visualCadenceProjectionText, "MotionConfidence = visualCadence.MotionConfidence,");
        AssertContains(visualCadenceProjectionText, "CenterRecentChangeIntervalsMs = visualCadence.CenterRecentChangeIntervalsMs");
        AssertContains(visualCadenceProjectionText, "private readonly record struct VisualCadenceFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCadenceMotionConfidence = captureCadence.VisualMotionConfidence,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = captureCadence.VisualCenterRecentChangeIntervalsMs,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCadenceMotionConfidence = health.VisualCadenceMotionConfidence,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCadenceMotionConfidence = visualCadence.MotionConfidence,");
        AssertDoesNotContain(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = visualCadence.CenterRecentChangeIntervalsMs,");

        AssertContains(visualCadenceProjectionText, "private static VisualCadenceProjection BuildVisualCadenceProjection(CaptureHealthSnapshot health)");
        AssertContains(visualCadenceProjectionText, "MotionConfidence = health.VisualCadenceMotionConfidence,");
        AssertContains(visualCadenceProjectionText, "CenterRecentChangeIntervalsMs = health.VisualCenterCadenceRecentChangeIntervalsMs");
        AssertContains(visualCadenceProjectionText, "private readonly record struct VisualCadenceProjection");
        AssertDoesNotContain(captureCadenceOnlyText, "VisualCadenceMotionConfidence");
        AssertDoesNotContain(captureCadenceOnlyText, "VisualCenterCadenceRecentChangeIntervalsMs");

        return Task.CompletedTask;
    }
}
