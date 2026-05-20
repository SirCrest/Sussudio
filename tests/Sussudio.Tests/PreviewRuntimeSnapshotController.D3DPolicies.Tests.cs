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

    private static Task PreviewRuntimeD3DRendererStatePolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DRendererStatePolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DRendererStatePolicy.Evaluate not found.");

        var previewingState = evaluate.Invoke(null, new object[] { null!, true })
                              ?? throw new InvalidOperationException("PreviewRuntimeD3DRendererStatePolicy returned null.");
        AssertEqual("CpuSoftwareBitmap", GetStringProperty(previewingState, "RendererMode"), "null D3D previewing renderer mode");
        AssertEqual(0, GetIntProperty(previewingState, "PresentSyncInterval"), "null D3D present sync interval");
        AssertEqual(0, GetIntProperty(previewingState, "MaxFrameLatency"), "null D3D max frame latency");
        AssertEqual(0, GetIntProperty(previewingState, "SwapChainBufferCount"), "null D3D swap-chain buffer count");
        AssertEqual(string.Empty, GetStringProperty(previewingState, "SwapChainAddress"), "null D3D swap-chain address");
        AssertEqual(0L, GetLongProperty(previewingState, "RenderThreadFailureCount"), "null D3D render-thread failure count");
        AssertEqual(string.Empty, GetStringProperty(previewingState, "LastRenderThreadFailureType"), "null D3D failure type");
        AssertEqual(string.Empty, GetStringProperty(previewingState, "LastRenderThreadFailureMessage"), "null D3D failure message");
        AssertEqual(0, GetIntProperty(previewingState, "LastRenderThreadFailureHResult"), "null D3D failure HRESULT");
        AssertEqual(0, GetIntProperty(previewingState, "PendingFrameCount"), "null D3D pending frame count");
        AssertEqual("None", GetStringProperty(previewingState, "InputColorSpace"), "null D3D input color space");
        AssertEqual("None", GetStringProperty(previewingState, "OutputColorSpace"), "null D3D output color space");
        var recentSlowFrames = GetPropertyValue(previewingState, "RecentSlowFrames") as Array
                               ?? throw new InvalidOperationException("RecentSlowFrames was not an array.");
        AssertEqual(0, recentSlowFrames.Length, "null D3D recent slow-frame count");
        AssertEqual("None", GetStringProperty(previewingState, "GpuPlaybackState"), "null D3D GPU playback state");
        AssertEqual(0, GetIntProperty(previewingState, "NaturalVideoWidth"), "null D3D natural video width");
        AssertEqual(0, GetIntProperty(previewingState, "NaturalVideoHeight"), "null D3D natural video height");
        AssertEqual(0d, GetDoubleProperty(previewingState, "PositionMs"), "null D3D GPU position");

        var idleState = evaluate.Invoke(null, new object[] { null!, false })
                        ?? throw new InvalidOperationException("PreviewRuntimeD3DRendererStatePolicy returned null for idle.");
        AssertEqual("None", GetStringProperty(idleState, "RendererMode"), "null D3D idle renderer mode");

        return Task.CompletedTask;
    }

    private static Task PreviewRuntimeD3DDisplayCadencePolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DDisplayCadencePolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DDisplayCadencePolicy.Evaluate not found.");

        var displayCadence = evaluate.Invoke(null, new object[] { null!, 8.33d })
                             ?? throw new InvalidOperationException("PreviewRuntimeD3DDisplayCadencePolicy returned null.");
        AssertEqual(0, GetIntProperty(displayCadence, "SampleCount"), "null D3D display cadence sample count");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "ObservedFps"), "null D3D display cadence observed fps");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "ExpectedIntervalMs"), "null D3D display cadence expected interval");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "AverageIntervalMs"), "null D3D display cadence average interval");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "P95IntervalMs"), "null D3D display cadence p95 interval");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "P99IntervalMs"), "null D3D display cadence p99 interval");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "MaxIntervalMs"), "null D3D display cadence max interval");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "OnePercentLowFps"), "null D3D display cadence one-percent low");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "FivePercentLowFps"), "null D3D display cadence five-percent low");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "SampleDurationMs"), "null D3D display cadence sample duration");
        var recentIntervals = GetPropertyValue(displayCadence, "RecentIntervalsMs") as Array
                              ?? throw new InvalidOperationException("RecentIntervalsMs was not an array.");
        AssertEqual(0, recentIntervals.Length, "null D3D display cadence recent interval count");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "JitterStdDevMs"), "null D3D display cadence jitter");
        AssertEqual(0L, GetLongProperty(displayCadence, "SlowFrameCount"), "null D3D display cadence slow-frame count");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "SlowFramePercent"), "null D3D display cadence slow-frame percent");

        return Task.CompletedTask;
    }

    private static Task PreviewRuntimeD3DRenderCpuTimingPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DRenderCpuTimingPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DRenderCpuTimingPolicy.Evaluate not found.");

        var renderCpuTiming = evaluate.Invoke(null, new object[] { null! })
                              ?? throw new InvalidOperationException("PreviewRuntimeD3DRenderCpuTimingPolicy returned null.");
        AssertEqual(0, GetIntProperty(renderCpuTiming, "SampleCount"), "null D3D render CPU timing sample count");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "InputUploadAverageMs"), "null D3D input-upload average");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "InputUploadP95Ms"), "null D3D input-upload p95");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "InputUploadP99Ms"), "null D3D input-upload p99");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "InputUploadMaxMs"), "null D3D input-upload max");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "RenderSubmitAverageMs"), "null D3D render-submit average");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "RenderSubmitP95Ms"), "null D3D render-submit p95");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "RenderSubmitP99Ms"), "null D3D render-submit p99");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "RenderSubmitMaxMs"), "null D3D render-submit max");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "PresentCallAverageMs"), "null D3D present-call average");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "PresentCallP95Ms"), "null D3D present-call p95");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "PresentCallP99Ms"), "null D3D present-call p99");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "PresentCallMaxMs"), "null D3D present-call max");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "TotalFrameAverageMs"), "null D3D total-frame average");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "TotalFrameP95Ms"), "null D3D total-frame p95");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "TotalFrameP99Ms"), "null D3D total-frame p99");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "TotalFrameMaxMs"), "null D3D total-frame max");

        return Task.CompletedTask;
    }

    private static Task PreviewRuntimeD3DPipelineLatencyPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DPipelineLatencyPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DPipelineLatencyPolicy.Evaluate not found.");

        var pipelineLatency = evaluate.Invoke(null, new object[] { null! })
                              ?? throw new InvalidOperationException("PreviewRuntimeD3DPipelineLatencyPolicy returned null.");
        AssertEqual(0, GetIntProperty(pipelineLatency, "SampleCount"), "null D3D pipeline latency sample count");
        AssertEqual(0d, GetDoubleProperty(pipelineLatency, "AverageMs"), "null D3D pipeline latency average");
        AssertEqual(0d, GetDoubleProperty(pipelineLatency, "P95Ms"), "null D3D pipeline latency p95");
        AssertEqual(0d, GetDoubleProperty(pipelineLatency, "P99Ms"), "null D3D pipeline latency p99");
        AssertEqual(0d, GetDoubleProperty(pipelineLatency, "MaxMs"), "null D3D pipeline latency max");
        AssertEqual(0d, GetDoubleProperty(pipelineLatency, "EstimatedPipelineLatencyMs"), "null estimated pipeline latency");

        return Task.CompletedTask;
    }

    private static Task PreviewRuntimeD3DFrameStatisticsPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DFrameStatisticsPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameStatisticsPolicy.Evaluate not found.");

        var frameStatistics = evaluate.Invoke(null, new object[] { null! })
                              ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameStatisticsPolicy returned null.");
        AssertEqual(0L, GetLongProperty(frameStatistics, "SampleCount"), "null D3D frame-stat sample count");
        AssertEqual(0L, GetLongProperty(frameStatistics, "SuccessCount"), "null D3D frame-stat success count");
        AssertEqual(0L, GetLongProperty(frameStatistics, "FailureCount"), "null D3D frame-stat failure count");
        AssertEqual(string.Empty, GetStringProperty(frameStatistics, "LastError"), "null D3D frame-stat last error");
        AssertEqual(-1L, GetLongProperty(frameStatistics, "PresentCount"), "null D3D present-count sentinel");
        AssertEqual(-1L, GetLongProperty(frameStatistics, "PresentRefreshCount"), "null D3D present-refresh sentinel");
        AssertEqual(-1L, GetLongProperty(frameStatistics, "SyncRefreshCount"), "null D3D sync-refresh sentinel");
        AssertEqual(0L, GetLongProperty(frameStatistics, "SyncQpcTime"), "null D3D sync QPC time");
        AssertEqual(0L, GetLongProperty(frameStatistics, "LastPresentDelta"), "null D3D present delta");
        AssertEqual(0L, GetLongProperty(frameStatistics, "LastPresentRefreshDelta"), "null D3D present-refresh delta");
        AssertEqual(0L, GetLongProperty(frameStatistics, "LastSyncRefreshDelta"), "null D3D sync-refresh delta");
        AssertEqual(0L, GetLongProperty(frameStatistics, "MissedRefreshCount"), "null D3D missed-refresh count");

        return Task.CompletedTask;
    }

    private static Task PreviewRuntimeD3DFrameLatencyWaitPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DFrameLatencyWaitPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameLatencyWaitPolicy.Evaluate not found.");

        var frameLatencyWait = evaluate.Invoke(null, new object[] { null! })
                               ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameLatencyWaitPolicy returned null.");
        AssertEqual(false, GetBoolProperty(frameLatencyWait, "Enabled"), "null D3D frame-latency wait enabled");
        AssertEqual(false, GetBoolProperty(frameLatencyWait, "HandleActive"), "null D3D frame-latency wait handle active");
        AssertEqual(0L, GetLongProperty(frameLatencyWait, "CallCount"), "null D3D frame-latency wait call count");
        AssertEqual(0L, GetLongProperty(frameLatencyWait, "SignaledCount"), "null D3D frame-latency wait signaled count");
        AssertEqual(0L, GetLongProperty(frameLatencyWait, "TimeoutCount"), "null D3D frame-latency wait timeout count");
        AssertEqual(0L, GetLongProperty(frameLatencyWait, "UnexpectedResultCount"), "null D3D frame-latency wait unexpected-result count");
        AssertEqual(0u, GetPropertyValue(frameLatencyWait, "LastResult"), "null D3D frame-latency wait last result");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "LastWaitMs"), "null D3D frame-latency wait last wait");
        AssertEqual(0, GetIntProperty(frameLatencyWait, "SampleCount"), "null D3D frame-latency wait sample count");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "AverageMs"), "null D3D frame-latency wait average");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "P95Ms"), "null D3D frame-latency wait p95");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "P99Ms"), "null D3D frame-latency wait p99");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "MaxMs"), "null D3D frame-latency wait max");

        return Task.CompletedTask;
    }

    private static Task PreviewRuntimeD3DFrameOwnershipPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DFrameOwnershipPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameOwnershipPolicy.Evaluate not found.");

        var frameOwnership = evaluate.Invoke(null, new object[] { null! })
                             ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameOwnershipPolicy returned null.");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastSubmittedPreviewPresentId"), "null D3D submitted present id");
        AssertEqual(-1L, GetLongProperty(frameOwnership, "LastSubmittedSourceSequenceNumber"), "null D3D submitted source sequence sentinel");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastSubmittedSourcePtsTicks"), "null D3D submitted source PTS");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastSubmittedQpc"), "null D3D submitted QPC");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastSubmittedUtcUnixMs"), "null D3D submitted UTC");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastRenderedPreviewPresentId"), "null D3D rendered present id");
        AssertEqual(-1L, GetLongProperty(frameOwnership, "LastRenderedSourceSequenceNumber"), "null D3D rendered source sequence sentinel");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastRenderedSourcePtsTicks"), "null D3D rendered source PTS");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastRenderedQpc"), "null D3D rendered QPC");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastRenderedUtcUnixMs"), "null D3D rendered UTC");
        AssertEqual(0d, GetDoubleProperty(frameOwnership, "LastRenderedSchedulerToPresentMs"), "null D3D scheduler-to-present");
        AssertEqual(0d, GetDoubleProperty(frameOwnership, "LastRenderedPipelineLatencyMs"), "null D3D pipeline latency");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastDroppedPreviewPresentId"), "null D3D dropped present id");
        AssertEqual(-1L, GetLongProperty(frameOwnership, "LastDroppedSourceSequenceNumber"), "null D3D dropped source sequence sentinel");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastDroppedSourcePtsTicks"), "null D3D dropped source PTS");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastDroppedQpc"), "null D3D dropped QPC");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastDroppedUtcUnixMs"), "null D3D dropped UTC");
        AssertEqual(string.Empty, GetStringProperty(frameOwnership, "LastDropReason"), "null D3D drop reason");

        return Task.CompletedTask;
    }
}
