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
