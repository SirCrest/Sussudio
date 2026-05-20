using System.Reflection;

static partial class Program
{
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
}
