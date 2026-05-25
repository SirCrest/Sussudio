using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsSnapshotStatusProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var snapshotStatusProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var snapshotStatus = BuildSnapshotStatusProjection(viewModelSnapshot, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var snapshotStatusFlattening = BuildSnapshotStatusFlattenedProjection(snapshotStatus);");
        AssertContains(snapshotFlatteningText, "TimestampUtc = snapshotStatusFlattening.TimestampUtc,");
        AssertContains(snapshotFlatteningText, "VerificationInProgress = snapshotStatusFlattening.VerificationInProgress,");
        AssertContains(snapshotFlatteningText, "SessionState = snapshotStatusFlattening.SessionState,");
        AssertContains(snapshotFlatteningText, "StatusText = snapshotStatusFlattening.StatusText,");
        AssertDoesNotContain(snapshotFlatteningText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertDoesNotContain(snapshotFlatteningText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertDoesNotContain(snapshotFlatteningText, "SessionState = captureRuntime.SessionState,");
        AssertDoesNotContain(snapshotFlatteningText, "StatusText = viewModelSnapshot.StatusText,");
        AssertDoesNotContain(snapshotFlatteningText, "TimestampUtc = snapshotStatus.TimestampUtc,");
        AssertDoesNotContain(snapshotFlatteningText, "StatusText = snapshotStatus.StatusText,");

        AssertContains(snapshotStatusProjectionText, "private SnapshotStatusProjection BuildSnapshotStatusProjection(");
        AssertContains(snapshotStatusProjectionText, "TimestampUtc = DateTimeOffset.UtcNow,");
        AssertContains(snapshotStatusProjectionText, "IsInitialized = viewModelSnapshot.IsInitialized,");
        AssertContains(snapshotStatusProjectionText, "VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,");
        AssertContains(snapshotStatusProjectionText, "SessionState = captureRuntime.SessionState,");
        AssertContains(snapshotStatusProjectionText, "StatusText = viewModelSnapshot.StatusText");
        AssertContains(snapshotStatusProjectionText, "private readonly record struct SnapshotStatusProjection");
        AssertContains(snapshotStatusProjectionText, "private static SnapshotStatusFlattenedProjection BuildSnapshotStatusFlattenedProjection(");
        AssertContains(snapshotStatusProjectionText, "TimestampUtc = snapshotStatus.TimestampUtc,");
        AssertContains(snapshotStatusProjectionText, "VerificationInProgress = snapshotStatus.VerificationInProgress,");
        AssertContains(snapshotStatusProjectionText, "SessionState = snapshotStatus.SessionState,");
        AssertContains(snapshotStatusProjectionText, "StatusText = snapshotStatus.StatusText");
        AssertContains(snapshotStatusProjectionText, "private readonly record struct SnapshotStatusFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsSnapshotEvaluationProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var snapshotEvaluationProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var snapshotEvaluation = BuildSnapshotEvaluationProjection(performance, diagnostic, previewPacingClassification);");
        AssertContains(snapshotFlatteningText, "var snapshotEvaluationFlattening = BuildSnapshotEvaluationFlattenedProjection(snapshotEvaluation);");
        AssertContains(snapshotFlatteningText, "PerformanceScore = snapshotEvaluationFlattening.PerformanceScore,");
        AssertContains(snapshotFlatteningText, "DiagnosticHealthStatus = snapshotEvaluationFlattening.DiagnosticHealthStatus,");
        AssertContains(snapshotFlatteningText, "PreviewPacingLikelySlowStage = snapshotEvaluationFlattening.PreviewPacingLikelySlowStage,");
        AssertContains(snapshotFlatteningText, "PerformanceThresholdCaptureDropPercent = snapshotEvaluationFlattening.PerformanceThresholdCaptureDropPercent,");
        AssertDoesNotContain(snapshotFlatteningText, "PerformanceScore = performance.Score,");
        AssertDoesNotContain(snapshotFlatteningText, "DiagnosticHealthStatus = diagnostic.HealthStatus,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage");
        AssertDoesNotContain(snapshotFlatteningText, "PerformanceThresholdCaptureDropPercent = _perfectionCaptureDropPercentThreshold,");
        AssertDoesNotContain(snapshotFlatteningText, "PerformanceScore = snapshotEvaluation.PerformanceScore,");
        AssertDoesNotContain(snapshotFlatteningText, "PerformanceThresholdCaptureDropPercent = snapshotEvaluation.PerformanceThresholdCaptureDropPercent,");

        AssertContains(snapshotEvaluationProjectionText, "private static SnapshotEvaluationFlattenedProjection BuildSnapshotEvaluationFlattenedProjection(");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceScore = snapshotEvaluation.PerformanceScore,");
        AssertContains(snapshotEvaluationProjectionText, "DiagnosticHealthStatus = snapshotEvaluation.DiagnosticHealthStatus,");
        AssertContains(snapshotEvaluationProjectionText, "PreviewPacingLikelySlowStage = snapshotEvaluation.PreviewPacingLikelySlowStage,");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceThresholdCaptureDropPercent = snapshotEvaluation.PerformanceThresholdCaptureDropPercent,");
        AssertContains(snapshotEvaluationProjectionText, "private readonly record struct SnapshotEvaluationFlattenedProjection");

        AssertContains(snapshotEvaluationProjectionText, "private SnapshotEvaluationProjection BuildSnapshotEvaluationProjection(");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceScore = performance.Score,");
        AssertContains(snapshotEvaluationProjectionText, "DiagnosticHealthStatus = diagnostic.HealthStatus,");
        AssertContains(snapshotEvaluationProjectionText, "DiagnosticAudioLane = diagnostic.AudioLane,");
        AssertContains(snapshotEvaluationProjectionText, "PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage,");
        AssertContains(snapshotEvaluationProjectionText, "PerformanceThresholdCaptureDropPercent = _perfectionCaptureDropPercentThreshold,");
        AssertContains(snapshotEvaluationProjectionText, "private readonly record struct SnapshotEvaluationProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsProcessResourceProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var processResourceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var processResourceProjection = BuildProcessResourceProjection(processResources);");
        AssertContains(snapshotFlatteningText, "var processResourceFlattening = BuildProcessResourceFlattenedProjection(processResourceProjection);");
        AssertContains(snapshotFlatteningText, "MemoryWorkingSetMb = processResourceFlattening.MemoryWorkingSetMb,");
        AssertContains(snapshotFlatteningText, "MemoryGcFragmentationPercent = processResourceFlattening.MemoryGcFragmentationPercent,");
        AssertContains(snapshotFlatteningText, "ThreadPoolIoMax = processResourceFlattening.ThreadPoolIoMax,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryWorkingSetMb = processResourceProjection.MemoryWorkingSetMb,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryGcFragmentationPercent = processResources.MemoryGcFragmentationPercent,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryGcFragmentationPercent = processResourceProjection.MemoryGcFragmentationPercent,");
        AssertDoesNotContain(snapshotFlatteningText, "ThreadPoolIoMax = processResources.ThreadPoolIoMax,");
        AssertDoesNotContain(snapshotFlatteningText, "ThreadPoolIoMax = processResourceProjection.ThreadPoolIoMax,");

        AssertContains(processResourceProjectionText, "private static ProcessResourceProjection BuildProcessResourceProjection(ProcessResourceSnapshot processResources)");
        AssertContains(processResourceProjectionText, "MemoryWorkingSetMb = processResources.MemoryWorkingSetMb,");
        AssertContains(processResourceProjectionText, "MemoryGcFragmentationPercent = processResources.MemoryGcFragmentationPercent,");
        AssertContains(processResourceProjectionText, "ThreadPoolIoMax = processResources.ThreadPoolIoMax");
        AssertContains(processResourceProjectionText, "private readonly record struct ProcessResourceProjection");
        AssertContains(processResourceProjectionText, "private static ProcessResourceFlattenedProjection BuildProcessResourceFlattenedProjection(");
        AssertContains(processResourceProjectionText, "MemoryWorkingSetMb = processResourceProjection.MemoryWorkingSetMb,");
        AssertContains(processResourceProjectionText, "MemoryGcFragmentationPercent = processResourceProjection.MemoryGcFragmentationPercent,");
        AssertContains(processResourceProjectionText, "ThreadPoolIoMax = processResourceProjection.ThreadPoolIoMax");
        AssertContains(processResourceProjectionText, "private readonly record struct ProcessResourceFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsAvSyncProjection_LivesWithProjectionRoot()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var avSyncProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var avSync = BuildAvSyncProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var avSyncFlattening = BuildAvSyncFlattenedProjection(avSync);");
        AssertContains(snapshotFlatteningText, "AvSyncCaptureDriftMs = avSyncFlattening.CaptureDriftMs,");
        AssertContains(snapshotFlatteningText, "AvSyncCaptureDriftRateMsPerSec = avSyncFlattening.CaptureDriftRateMsPerSec,");
        AssertContains(snapshotFlatteningText, "AvSyncEncoderCorrectionSamples = avSyncFlattening.EncoderCorrectionSamples,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncCaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncCaptureDriftMs = avSync.CaptureDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncEncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples,");
        AssertDoesNotContain(snapshotFlatteningText, "AvSyncEncoderCorrectionSamples = avSync.EncoderCorrectionSamples,");

        AssertContains(avSyncProjectionText, "private static AvSyncFlattenedProjection BuildAvSyncFlattenedProjection(AvSyncProjection avSync)");
        AssertContains(avSyncProjectionText, "CaptureDriftMs = avSync.CaptureDriftMs,");
        AssertContains(avSyncProjectionText, "CaptureDriftRateMsPerSec = avSync.CaptureDriftRateMsPerSec,");
        AssertContains(avSyncProjectionText, "EncoderCorrectionSamples = avSync.EncoderCorrectionSamples");
        AssertContains(avSyncProjectionText, "private readonly record struct AvSyncFlattenedProjection");

        AssertContains(avSyncProjectionText, "private static AvSyncProjection BuildAvSyncProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(avSyncProjectionText, "CaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,");
        AssertContains(avSyncProjectionText, "CaptureDriftRateMsPerSec = captureRuntime.AvSyncCaptureDriftRateMsPerSec,");
        AssertContains(avSyncProjectionText, "EncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples");
        AssertContains(avSyncProjectionText, "private readonly record struct AvSyncProjection");

        return Task.CompletedTask;
    }
}
