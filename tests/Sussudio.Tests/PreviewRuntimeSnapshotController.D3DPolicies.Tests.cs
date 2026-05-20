using System.Reflection;

static partial class Program
{
    private static Task PreviewRuntimeD3DFrameCounterPolicy_PreservesCpuFallbackCounters()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DFrameCounterPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameCounterPolicy.Evaluate not found.");

        var attachedInput = Activator.CreateInstance(inputType)
                            ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(attachedInput, "D3DRenderer", null);
        SetPropertyOrBackingField(attachedInput, "PreviewSourceAttached", true);
        SetPropertyOrBackingField(attachedInput, "FramesArrived", 31L);
        SetPropertyOrBackingField(attachedInput, "FramesDisplayed", 17L);
        SetPropertyOrBackingField(attachedInput, "FramesDropped", 4L);

        var attachedCounters = evaluate.Invoke(null, new[] { attachedInput })
                               ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameCounterPolicy returned null.");
        AssertEqual(false, GetBoolProperty(attachedCounters, "GpuActive"), "CPU fallback reports GPU inactive");
        AssertEqual(true, GetBoolProperty(attachedCounters, "RendererAttached"), "CPU fallback keeps renderer attached");
        AssertEqual(31L, GetLongProperty(attachedCounters, "FramesArrived"), "CPU fallback frames arrived");
        AssertEqual(17L, GetLongProperty(attachedCounters, "FramesDisplayed"), "CPU fallback frames displayed");
        AssertEqual(4L, GetLongProperty(attachedCounters, "FramesDropped"), "CPU fallback frames dropped");
        AssertEqual(0L, GetLongProperty(attachedCounters, "D3DFramesSubmitted"), "null D3D submitted counter");
        AssertEqual(0L, GetLongProperty(attachedCounters, "D3DFramesRendered"), "null D3D rendered counter");
        AssertEqual(0L, GetLongProperty(attachedCounters, "D3DFramesDropped"), "null D3D dropped counter");

        var detachedInput = Activator.CreateInstance(inputType)
                            ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(detachedInput, "D3DRenderer", null);
        SetPropertyOrBackingField(detachedInput, "PreviewSourceAttached", false);

        var detachedCounters = evaluate.Invoke(null, new[] { detachedInput })
                               ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameCounterPolicy returned null.");
        AssertEqual(false, GetBoolProperty(detachedCounters, "RendererAttached"), "null D3D without CPU source is detached");

        return Task.CompletedTask;
    }

    private static Task PreviewRuntimeD3DProjectionBuilder_AppliesPolicyGroups()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var projectionType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DProjection");
        var build = projectionType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException("PreviewRuntimeD3DProjection.Build not found.");

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "D3DRenderer", null);
        SetPropertyOrBackingField(input, "PreviewSourceAttached", true);
        SetPropertyOrBackingField(input, "IsPreviewing", true);
        SetPropertyOrBackingField(input, "FramesArrived", 31L);
        SetPropertyOrBackingField(input, "FramesDisplayed", 17L);
        SetPropertyOrBackingField(input, "FramesDropped", 4L);
        SetPropertyOrBackingField(input, "PreviewMinPresentationIntervalMs", 8.33d);

        var projection = build.Invoke(null, new[] { input })
                         ?? throw new InvalidOperationException("PreviewRuntimeD3DProjection.Build returned null.");
        AssertEqual(false, GetBoolProperty(projection, "GpuActive"), "builder applies frame-counter GPU state");
        AssertEqual(true, GetBoolProperty(projection, "RendererAttached"), "builder applies CPU fallback attachment");
        AssertEqual(31L, GetLongProperty(projection, "FramesArrived"), "builder applies frame-counter arrived value");
        AssertEqual("CpuSoftwareBitmap", GetStringProperty(projection, "RendererMode"), "builder applies renderer-state fallback");
        AssertEqual(0, GetIntProperty(projection, "DisplayCadenceSampleCount"), "builder applies display cadence defaults");
        AssertEqual(0d, GetDoubleProperty(projection, "D3DInputUploadCpuAvgMs"), "builder applies render CPU timing defaults");
        AssertEqual(0d, GetDoubleProperty(projection, "EstimatedPipelineLatencyMs"), "builder applies pipeline latency defaults");
        AssertEqual(false, GetBoolProperty(projection, "D3DFrameLatencyWaitEnabled"), "builder applies frame-latency wait defaults");
        AssertEqual(-1L, GetLongProperty(projection, "D3DFrameStatsPresentCount"), "builder applies frame-stat sentinels");
        AssertEqual(-1L, GetLongProperty(projection, "D3DLastSubmittedSourceSequenceNumber"), "builder applies frame-ownership sentinels");

        return Task.CompletedTask;
    }
}
