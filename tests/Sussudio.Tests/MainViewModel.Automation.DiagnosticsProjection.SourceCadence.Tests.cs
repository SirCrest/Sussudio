using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsSourceTelemetryProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningSourceText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.Source.cs")
            .Replace("\r\n", "\n");
        var sourceTelemetryProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceTelemetry.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var sourceTelemetry = BuildSourceTelemetryProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var sourceFlattening = BuildSourceFlattenedProjection(sourceSignal, sourceTelemetry);");
        AssertContains(snapshotFlatteningText, "SourceTelemetryAvailability = sourceFlattening.SourceTelemetryAvailability,");
        AssertContains(snapshotFlatteningText, "SourceTelemetryDetails = sourceFlattening.SourceTelemetryDetails,");
        AssertContains(snapshotFlatteningText, "SourceTelemetryAgeSeconds = sourceFlattening.SourceTelemetryAgeSeconds,");
        AssertContains(snapshotFlatteningText, "SourceTargetSummaryText = sourceFlattening.SourceTargetSummaryText,");
        AssertContains(snapshotFlatteningSourceText, "private static SourceFlattenedProjection BuildSourceFlattenedProjection(");
        AssertContains(snapshotFlatteningSourceText, "SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,");
        AssertContains(snapshotFlatteningSourceText, "SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,");
        AssertContains(snapshotFlatteningSourceText, "SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,");
        AssertContains(snapshotFlatteningSourceText, "SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText");
        AssertContains(snapshotFlatteningSourceText, "private readonly record struct SourceFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText,");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAvailability = !string.IsNullOrWhiteSpace(viewModelSnapshot.SourceTelemetryAvailability)");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(");
        AssertDoesNotContain(snapshotFlatteningText, "SourceTelemetryDetails = captureRuntime.SourceTelemetryDetails,");

        AssertContains(sourceTelemetryProjectionText, "private static SourceTelemetryProjection BuildSourceTelemetryProjection(");
        AssertContains(sourceTelemetryProjectionText, "private static string PreferKnownTelemetryValue(string viewModelValue, string runtimeValue)");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryAvailability = PreferKnownTelemetryValue(");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryDetails = captureRuntime.SourceTelemetryDetails,");
        AssertContains(sourceTelemetryProjectionText, "SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(");
        AssertContains(sourceTelemetryProjectionText, "private readonly record struct SourceTelemetryProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsSourceSignalProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningSourceText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.Source.cs")
            .Replace("\r\n", "\n");
        var sourceSignalProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var sourceSignal = BuildSourceSignalProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var sourceFlattening = BuildSourceFlattenedProjection(sourceSignal, sourceTelemetry);");
        AssertContains(snapshotFlatteningText, "DetectedSourceFrameRate = sourceFlattening.DetectedSourceFrameRate,");
        AssertContains(snapshotFlatteningText, "SourceFrameRateOrigin = sourceFlattening.SourceFrameRateOrigin,");
        AssertContains(snapshotFlatteningText, "SourceRawTimingHex = sourceFlattening.SourceRawTimingHex,");
        AssertContains(snapshotFlatteningSourceText, "private static SourceFlattenedProjection BuildSourceFlattenedProjection(");
        AssertContains(snapshotFlatteningSourceText, "DetectedSourceFrameRate = sourceSignal.DetectedFrameRate,");
        AssertContains(snapshotFlatteningSourceText, "SourceFrameRateOrigin = sourceSignal.FrameRateOrigin,");
        AssertContains(snapshotFlatteningSourceText, "SourceRawTimingHex = sourceSignal.RawTimingHex,");
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
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningCaptureCadenceText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureCadence.cs")
            .Replace("\r\n", "\n");
        var captureCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureCadence = BuildCaptureCadenceProjection(health);");
        AssertContains(snapshotFlatteningText, "var captureCadenceFlattening = BuildCaptureCadenceFlattenedProjection(captureCadence);");
        AssertContains(snapshotFlatteningText, "ExpectedCaptureFrameRate = captureCadenceFlattening.ExpectedFrameRate,");
        AssertContains(snapshotFlatteningText, "CaptureCadenceEstimatedDroppedFrames = captureCadenceFlattening.EstimatedDroppedFrames,");
        AssertContains(snapshotFlatteningCaptureCadenceText, "private static CaptureCadenceFlattenedProjection BuildCaptureCadenceFlattenedProjection(");
        AssertContains(snapshotFlatteningCaptureCadenceText, "ExpectedFrameRate = captureCadence.ExpectedFrameRate,");
        AssertContains(snapshotFlatteningCaptureCadenceText, "EstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames");
        AssertContains(snapshotFlatteningCaptureCadenceText, "private readonly record struct CaptureCadenceFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "ExpectedCaptureFrameRate = health.ExpectedFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCadenceEstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,");
        AssertDoesNotContain(snapshotFlatteningText, "ExpectedCaptureFrameRate = captureCadence.ExpectedFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCadenceEstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames,");

        AssertContains(captureCadenceProjectionText, "private static CaptureCadenceProjection BuildCaptureCadenceProjection(CaptureHealthSnapshot health)");
        AssertContains(captureCadenceProjectionText, "ExpectedFrameRate = health.ExpectedFrameRate,");
        AssertContains(captureCadenceProjectionText, "EstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,");
        AssertContains(captureCadenceProjectionText, "private readonly record struct CaptureCadenceProjection");
        AssertDoesNotContain(captureCadenceProjectionText, "VisualMotionConfidence");
        AssertDoesNotContain(captureCadenceProjectionText, "VisualCenterRecentChangeIntervalsMs");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsVisualCadenceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningVisualCadenceText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.VisualCadence.cs")
            .Replace("\r\n", "\n");
        var visualCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs")
            .Replace("\r\n", "\n");
        var captureCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var visualCadence = BuildVisualCadenceProjection(health);");
        AssertContains(snapshotFlatteningText, "var visualCadenceFlattening = BuildVisualCadenceFlattenedProjection(visualCadence);");
        AssertContains(snapshotFlatteningText, "VisualCadenceMotionConfidence = visualCadenceFlattening.MotionConfidence,");
        AssertContains(snapshotFlatteningText, "VisualCenterCadenceRecentChangeIntervalsMs = visualCadenceFlattening.CenterRecentChangeIntervalsMs,");
        AssertContains(snapshotFlatteningVisualCadenceText, "private static VisualCadenceFlattenedProjection BuildVisualCadenceFlattenedProjection(");
        AssertContains(snapshotFlatteningVisualCadenceText, "MotionConfidence = visualCadence.MotionConfidence,");
        AssertContains(snapshotFlatteningVisualCadenceText, "CenterRecentChangeIntervalsMs = visualCadence.CenterRecentChangeIntervalsMs");
        AssertContains(snapshotFlatteningVisualCadenceText, "private readonly record struct VisualCadenceFlattenedProjection");
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
        AssertDoesNotContain(captureCadenceProjectionText, "VisualCadenceMotionConfidence");
        AssertDoesNotContain(captureCadenceProjectionText, "VisualCenterCadenceRecentChangeIntervalsMs");

        return Task.CompletedTask;
    }
}
