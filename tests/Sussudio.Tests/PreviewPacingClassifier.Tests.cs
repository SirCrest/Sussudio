using System;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class PreviewPacingClassifierTests
{
    private const string InputTypeName = "Sussudio.Services.Automation.PreviewPacingClassificationInput";
    private const string ClassifierTypeName = "Sussudio.Services.Automation.PreviewPacingSlowStageClassifier";

    [Fact(DisplayName = "Preview pacing classifier rejects weak samples")]
    public void PreviewPacingClassifier_RequiresStableSampleUnlessHardSignal()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "PreviewCadenceSampleCount", 240);
        SetPropertyOrBackingField(input, "PreviewCadenceSampleDurationMs", 2000d);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("InsufficientSample", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("Low", GetStringProperty(result, "Confidence"));
        Assert.Contains("requiredDurationMs=30000", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier prefers source capture when source drops")]
    public void PreviewPacingClassifier_ClassifiesSourceCaptureBeforePreviewTail()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "CaptureCadenceSampleCount", 3600);
        SetPropertyOrBackingField(input, "CaptureCadenceSampleDurationMs", 30000d);
        SetPropertyOrBackingField(input, "CaptureCadenceOnePercentLowFps", 106d);
        SetPropertyOrBackingField(input, "CaptureCadenceEstimatedDroppedFrames", 3L);
        SetPropertyOrBackingField(input, "CaptureCadenceSevereGapCount", 1L);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("SourceCapture", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("High", GetStringProperty(result, "Confidence"));
        Assert.Contains("drops=3", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier flags compositor misses first")]
    public void PreviewPacingClassifier_ClassifiesCompositorMissBeforePresentBlocked()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "PreviewD3DPresentCallP99Ms", 6d);
        SetPropertyOrBackingField(input, "RecentD3DMissedRefreshes", 2L);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("CompositorMiss", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("High", GetStringProperty(result, "Confidence"));
        Assert.Contains("dxgiRecentMissed=2", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier flags dominant render upload")]
    public void PreviewPacingClassifier_ClassifiesDominantRenderUpload()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "PreviewD3DInputUploadCpuP99Ms", 5d);
        SetPropertyOrBackingField(input, "PreviewD3DRenderSubmitCpuP99Ms", 1.2d);
        SetPropertyOrBackingField(input, "PreviewD3DPresentCallP99Ms", 1.0d);
        SetPropertyOrBackingField(input, "PreviewD3DFrameLatencyWaitP95Ms", 0.5d);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("RenderUpload", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("Medium", GetStringProperty(result, "Confidence"));
        Assert.Contains("input=5", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier flags frame latency wait timeout")]
    public void PreviewPacingClassifier_ClassifiesFrameLatencyWaitTimeout()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "PreviewD3DFrameLatencyWaitTimeoutCount", 1L);
        SetPropertyOrBackingField(input, "RecentD3DFrameLatencyWaitTimeoutCount", 1L);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("PresentBlocked", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("Medium", GetStringProperty(result, "Confidence"));
        Assert.Contains("waitP95", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier ignores stale lifetime signals")]
    public void PreviewPacingClassifier_IgnoresStaleLifetimeSignalsWithoutRecentDeltas()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "MjpegPreviewJitterEnabled", true);
        SetPropertyOrBackingField(input, "MjpegPreviewJitterScheduleLateCount", 12L);
        SetPropertyOrBackingField(input, "MjpegPreviewJitterMaxScheduleLateMs", 20d);
        SetPropertyOrBackingField(input, "MjpegPreviewJitterLastDropReason", "submit-failed");
        SetPropertyOrBackingField(input, "PreviewD3DFrameLatencyWaitTimeoutCount", 4L);
        SetPropertyOrBackingField(input, "PreviewD3DLastDropReason", "queue-full");

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("Unknown", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("Low", GetStringProperty(result, "Confidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier flags recent jitter schedule-late")]
    public void PreviewPacingClassifier_ClassifiesRecentJitterScheduleLate()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "MjpegPreviewJitterEnabled", true);
        SetPropertyOrBackingField(input, "RecentPreviewJitterScheduleLateCount", 1L);
        SetPropertyOrBackingField(input, "RecentPreviewJitterScheduleLateMs", 5d);
        SetPropertyOrBackingField(input, "MjpegPreviewJitterScheduleLateCount", 12L);
        SetPropertyOrBackingField(input, "MjpegPreviewJitterMaxScheduleLateMs", 20d);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("PreviewJitterScheduler", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("Medium", GetStringProperty(result, "Confidence"));
        Assert.Contains("recentScheduleLate=1/5", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier flags visual duplicate or low motion")]
    public void PreviewPacingClassifier_ClassifiesVisualDuplicateOrLowMotion()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "VisualCadenceSampleCount", 240);
        SetPropertyOrBackingField(input, "VisualCadenceChangeObservedFps", 80d);
        SetPropertyOrBackingField(input, "VisualCadenceRepeatFramePercent", 12d);
        SetPropertyOrBackingField(input, "VisualCadenceLongestRepeatRun", 5);
        SetPropertyOrBackingField(input, "VisualCadenceMotionConfidence", "High");
        SetPropertyOrBackingField(input, "MjpegPacketHashInputObservedFps", 120d);
        SetPropertyOrBackingField(input, "MjpegPacketHashUniqueObservedFps", 120d);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("VisualDuplicateOrLowMotion", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("Medium", GetStringProperty(result, "Confidence"));
        Assert.Contains("visualChange=80", GetStringProperty(result, "Evidence"));
        Assert.Contains("confidence=High", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier flags MJPEG decode pressure")]
    public void PreviewPacingClassifier_ClassifiesMjpegDecodePressure()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "MjpegPipelineSampleCount", 240);
        SetPropertyOrBackingField(input, "MjpegDecodeP95Ms", 6d);
        SetPropertyOrBackingField(input, "MjpegPipelineP95Ms", 8d);
        SetPropertyOrBackingField(input, "MjpegPipelineMaxMs", 10d);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("MjpegDecode", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("Medium", GetStringProperty(result, "Confidence"));
        Assert.Contains("mjpegDecodeP95=6", GetStringProperty(result, "Evidence"));
        Assert.Contains("pipelineP95=8", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier flags renderer submit drops")]
    public void PreviewPacingClassifier_ClassifiesRendererSubmitDrops()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "RecentRendererDropped", 3L);
        SetPropertyOrBackingField(input, "RecentRendererSubmitted", 100L);
        SetPropertyOrBackingField(input, "PreviewD3DLastDropReason", "queue-full");

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("RenderSubmit", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("High", GetStringProperty(result, "Confidence"));
        Assert.Contains("rendererDropped=3/100", GetStringProperty(result, "Evidence"));
        Assert.Contains("lastDrop=queue-full", GetStringProperty(result, "Evidence"));
    }

    [Fact(DisplayName = "Preview pacing classifier falls back to render submit for high total D3D CPU time")]
    public void PreviewPacingClassifier_ClassifiesD3DTotalFrameCpuFallback()
    {
        var input = CreateBaselinePreviewPacingInput();
        SetPropertyOrBackingField(input, "PreviewD3DInputUploadCpuP99Ms", 0.4d);
        SetPropertyOrBackingField(input, "PreviewD3DRenderSubmitCpuP99Ms", 0.6d);
        SetPropertyOrBackingField(input, "PreviewD3DPresentCallP99Ms", 0.5d);
        SetPropertyOrBackingField(input, "PreviewD3DFrameLatencyWaitP95Ms", 0.4d);
        SetPropertyOrBackingField(input, "PreviewD3DTotalFrameCpuP99Ms", 10d);

        var result = ClassifyPreviewPacing(input);

        Assert.Equal("RenderSubmit", GetStringProperty(result, "LikelySlowStage"));
        Assert.Equal("Medium", GetStringProperty(result, "Confidence"));
        Assert.Contains("total=10", GetStringProperty(result, "Evidence"));
    }

    private static object CreateBaselinePreviewPacingInput()
    {
        var input = CreateInstance(InputTypeName);
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

    private static object CreateInstance(string typeName)
    {
        var type = SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;
        return Activator.CreateInstance(type)
               ?? throw new InvalidOperationException($"Failed to create instance of '{typeName}'.");
    }

    private static object ClassifyPreviewPacing(object input)
    {
        var classifierType = SussudioAssembly.Load().GetType(ClassifierTypeName, throwOnError: true)!;
        var classify = classifierType.GetMethod("Classify", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewPacingSlowStageClassifier.Classify was not found.");
        return classify.Invoke(null, new[] { input })
               ?? throw new InvalidOperationException("Preview pacing classifier returned null.");
    }

    private static void SetPropertyOrBackingField(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var backingField = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (backingField != null)
        {
            backingField.SetValue(instance, value);
            return;
        }

        throw new InvalidOperationException(
            $"Property '{propertyName}' is not writable and backing field was not found on '{instance.GetType().Name}'.");
    }

    private static string GetStringProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on '{instance.GetType().Name}'.");
        return property.GetValue(instance)?.ToString() ?? string.Empty;
    }
}
