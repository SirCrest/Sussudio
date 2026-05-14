using System;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task PreviewPacingClassifier_RequiresStableSampleUnlessHardSignal()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "PreviewCadenceSampleCount", 240);
        SetPropertyOrBackingField(input, "PreviewCadenceSampleDurationMs", 2000d);

        var result = ClassifyPreviewPacing(input);

        AssertEqual("InsufficientSample", GetStringProperty(result, "LikelySlowStage"), "Preview pacing weak sample stage");
        AssertEqual("Low", GetStringProperty(result, "Confidence"), "Preview pacing weak sample confidence");
        AssertContains(GetStringProperty(result, "Evidence"), "requiredDurationMs=30000");
        return Task.CompletedTask;
    }

    private static Task PreviewPacingClassifier_ClassifiesSourceCaptureBeforePreviewTail()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "CaptureCadenceSampleCount", 3600);
        SetPropertyOrBackingField(input, "CaptureCadenceSampleDurationMs", 30000d);
        SetPropertyOrBackingField(input, "CaptureCadenceOnePercentLowFps", 106d);
        SetPropertyOrBackingField(input, "CaptureCadenceEstimatedDroppedFrames", 3L);
        SetPropertyOrBackingField(input, "CaptureCadenceSevereGapCount", 1L);

        var result = ClassifyPreviewPacing(input);

        AssertEqual("SourceCapture", GetStringProperty(result, "LikelySlowStage"), "Preview pacing source capture stage");
        AssertEqual("High", GetStringProperty(result, "Confidence"), "Preview pacing source capture confidence");
        AssertContains(GetStringProperty(result, "Evidence"), "drops=3");
        return Task.CompletedTask;
    }

    private static Task PreviewPacingClassifier_ClassifiesCompositorMissBeforePresentBlocked()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "PreviewD3DPresentCallP99Ms", 6d);
        SetPropertyOrBackingField(input, "RecentD3DMissedRefreshes", 2L);

        var result = ClassifyPreviewPacing(input);

        AssertEqual("CompositorMiss", GetStringProperty(result, "LikelySlowStage"), "Preview pacing compositor stage");
        AssertEqual("High", GetStringProperty(result, "Confidence"), "Preview pacing compositor confidence");
        AssertContains(GetStringProperty(result, "Evidence"), "dxgiRecentMissed=2");
        return Task.CompletedTask;
    }

    private static Task PreviewPacingClassifier_ClassifiesDominantRenderUpload()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "PreviewD3DInputUploadCpuP99Ms", 5d);
        SetPropertyOrBackingField(input, "PreviewD3DRenderSubmitCpuP99Ms", 1.2d);
        SetPropertyOrBackingField(input, "PreviewD3DPresentCallP99Ms", 1.0d);
        SetPropertyOrBackingField(input, "PreviewD3DFrameLatencyWaitP95Ms", 0.5d);

        var result = ClassifyPreviewPacing(input);

        AssertEqual("RenderUpload", GetStringProperty(result, "LikelySlowStage"), "Preview pacing render upload stage");
        AssertEqual("Medium", GetStringProperty(result, "Confidence"), "Preview pacing render upload confidence");
        AssertContains(GetStringProperty(result, "Evidence"), "input=5");
        return Task.CompletedTask;
    }

    private static Task PreviewPacingClassifier_ClassifiesFrameLatencyWaitTimeout()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "PreviewD3DFrameLatencyWaitTimeoutCount", 1L);
        SetPropertyOrBackingField(input, "RecentD3DFrameLatencyWaitTimeoutCount", 1L);

        var result = ClassifyPreviewPacing(input);

        AssertEqual("PresentBlocked", GetStringProperty(result, "LikelySlowStage"), "Preview pacing wait timeout stage");
        AssertEqual("Medium", GetStringProperty(result, "Confidence"), "Preview pacing wait timeout confidence");
        AssertContains(GetStringProperty(result, "Evidence"), "waitP95");
        return Task.CompletedTask;
    }

    private static Task PreviewPacingClassifier_IgnoresStaleLifetimeSignalsWithoutRecentDeltas()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "MjpegPreviewJitterEnabled", true);
        SetPropertyOrBackingField(input, "MjpegPreviewJitterScheduleLateCount", 12L);
        SetPropertyOrBackingField(input, "MjpegPreviewJitterMaxScheduleLateMs", 20d);
        SetPropertyOrBackingField(input, "MjpegPreviewJitterLastDropReason", "submit-failed");
        SetPropertyOrBackingField(input, "PreviewD3DFrameLatencyWaitTimeoutCount", 4L);
        SetPropertyOrBackingField(input, "PreviewD3DLastDropReason", "queue-full");

        var result = ClassifyPreviewPacing(input);

        AssertEqual("Unknown", GetStringProperty(result, "LikelySlowStage"), "Preview pacing stale lifetime signals stage");
        AssertEqual("Low", GetStringProperty(result, "Confidence"), "Preview pacing stale lifetime signals confidence");
        return Task.CompletedTask;
    }

    private static Task PreviewPacingClassifier_ClassifiesRecentJitterScheduleLate()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "MjpegPreviewJitterEnabled", true);
        SetPropertyOrBackingField(input, "RecentPreviewJitterScheduleLateCount", 1L);
        SetPropertyOrBackingField(input, "RecentPreviewJitterScheduleLateMs", 5d);
        SetPropertyOrBackingField(input, "MjpegPreviewJitterScheduleLateCount", 12L);
        SetPropertyOrBackingField(input, "MjpegPreviewJitterMaxScheduleLateMs", 20d);

        var result = ClassifyPreviewPacing(input);

        AssertEqual("PreviewJitterScheduler", GetStringProperty(result, "LikelySlowStage"), "Preview pacing recent jitter schedule-late stage");
        AssertEqual("Medium", GetStringProperty(result, "Confidence"), "Preview pacing recent jitter schedule-late confidence");
        AssertContains(GetStringProperty(result, "Evidence"), "recentScheduleLate=1/5");
        return Task.CompletedTask;
    }

    private static Task PreviewPacingClassifier_ModelsLiveInFocusedFile()
    {
        var classifierText = ReadRepoFile("Sussudio/Services/Automation/PreviewPacingSlowStageClassifier.cs")
            .Replace("\r\n", "\n");
        var modelText = ReadRepoFile("Sussudio/Services/Automation/PreviewPacingClassificationModels.cs")
            .Replace("\r\n", "\n");

        AssertContains(modelText, "public sealed class PreviewPacingClassificationInput");
        AssertContains(modelText, "public readonly record struct PreviewPacingClassification(");
        AssertContains(classifierText, "public static class PreviewPacingSlowStageClassifier");
        AssertDoesNotContain(classifierText, "public sealed class PreviewPacingClassificationInput");
        AssertDoesNotContain(classifierText, "public readonly record struct PreviewPacingClassification(");

        return Task.CompletedTask;
    }
    private static Task PreviewPacingClassifier_IsWiredIntoAutomationSnapshots()
    {
        var contractsText = ReadAutomationSnapshotFamilyText();
        var diagnosticsSnapshotsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs");
        var diagnosticsSnapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs");
        var diagnosticsSnapshotProjectionSnapshotEvaluationText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs");
        var diagnosticsSnapshotProjectionCaptureCadenceText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs");
        var diagnosticsPreviewPacingText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.PreviewPacing.cs");
        var diagnosticsHubText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            + "\n" + diagnosticsSnapshotsText
            + "\n" + diagnosticsSnapshotProjectionText
            + "\n" + diagnosticsSnapshotProjectionSnapshotEvaluationText
            + "\n" + diagnosticsSnapshotProjectionCaptureCadenceText
            + "\n" + diagnosticsPreviewPacingText
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Timeline.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.cs");

        AssertContains(contractsText, "public string PreviewPacingLikelySlowStage { get; init; }");
        AssertContains(contractsText, "public string PreviewPacingSlowStageConfidence { get; init; }");
        AssertContains(contractsText, "public string PreviewPacingSlowStageEvidence { get; init; }");
        AssertContains(diagnosticsSnapshotsText, "var previewPacingClassification = ClassifyPreviewPacing(");
        AssertDoesNotContain(diagnosticsSnapshotsText, "new PreviewPacingClassificationInput");
        AssertContains(diagnosticsSnapshotProjectionText, "PreviewPacingLikelySlowStage = snapshotEvaluation.PreviewPacingLikelySlowStage");
        AssertContains(diagnosticsSnapshotProjectionSnapshotEvaluationText, "PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage");
        AssertContains(diagnosticsPreviewPacingText, "private static PreviewPacingClassification ClassifyPreviewPacing(");
        AssertContains(diagnosticsPreviewPacingText, "PreviewPacingSlowStageClassifier.Classify");
        AssertContains(diagnosticsHubText, "PreviewCadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps");
        AssertContains(diagnosticsHubText, "CaptureCadenceEstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames");
        AssertContains(diagnosticsHubText, "EstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames");
        AssertContains(diagnosticsHubText, "RecentD3DMissedRefreshes = recentD3DMissedRefreshes");
        AssertContains(diagnosticsHubText, "RecentPreviewJitterScheduleLateCount = recentPreviewJitter.ScheduleLateCount");
        AssertContains(diagnosticsHubText, "RecentD3DFrameLatencyWaitTimeoutCount = recentD3DFrameLatencyWaitTimeouts");
        AssertContains(diagnosticsHubText, "UpdateD3DFrameLatencyWaitRecentCounters");
        AssertContains(diagnosticsHubText, "PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage");
        AssertContains(diagnosticsHubText, "PreviewPacingSlowStageConfidence = previewPacingClassification.Confidence");
        AssertContains(diagnosticsHubText, "PreviewPacingSlowStageEvidence = previewPacingClassification.Evidence");
        AssertContains(diagnosticsHubText, "PreviewPacingLikelySlowStage = snapshot.PreviewPacingLikelySlowStage");
        AssertContains(diagnosticsHubText, "PreviewPacingSlowStageConfidence = snapshot.PreviewPacingSlowStageConfidence");
        AssertContains(diagnosticsHubText, "PreviewPacingSlowStageEvidence = snapshot.PreviewPacingSlowStageEvidence");
        return Task.CompletedTask;
    }

    private static object CreateBaselinePreviewPacingInput()
    {
        var input = CreateInstance("Sussudio.Services.Automation.PreviewPacingClassificationInput");
        SetPropertyOrBackingField(input, "IsPreviewing", true);
        SetPropertyOrBackingField(input, "TargetFrameRate", 120d);
        SetPropertyOrBackingField(input, "PreviewCadenceSampleCount", 3600);
        SetPropertyOrBackingField(input, "PreviewCadenceSampleDurationMs", 30000d);
        SetPropertyOrBackingField(input, "PreviewCadenceExpectedIntervalMs", 1000d / 120d);
        SetPropertyOrBackingField(input, "PreviewCadenceObservedFps", 119d);
        SetPropertyOrBackingField(input, "PreviewCadenceOnePercentLowFps", 105d);
        SetPropertyOrBackingField(input, "PreviewCadenceP99IntervalMs", 9.8d);
        SetPropertyOrBackingField(input, "CaptureExpectedFrameRate", 120d);
        return input;
    }

    private static object ClassifyPreviewPacing(object input)
    {
        var classifierType = RequireType("Sussudio.Services.Automation.PreviewPacingSlowStageClassifier");
        var classify = classifierType.GetMethod("Classify", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewPacingSlowStageClassifier.Classify was not found.");
        return classify.Invoke(null, new[] { input })
               ?? throw new InvalidOperationException("Preview pacing classifier returned null.");
    }
}
