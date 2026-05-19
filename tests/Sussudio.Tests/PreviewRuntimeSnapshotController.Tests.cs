using System.Reflection;

static partial class Program
{
    private static Task PreviewRuntimeSnapshotController_PreservesNullD3dProjectionPolicy()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var controllerType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotController");
        var build = controllerType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException("PreviewRuntimeSnapshotController.Build not found.");
        var requiredSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "FirstVisual");
        var receivedSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "None");
        var startupStrategy = ParseEnum("Sussudio.Models.PreviewStartupStrategy", "D3D11VideoProcessor");

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "D3DRenderer", null);
        SetPropertyOrBackingField(input, "IsPreviewing", true);
        SetPropertyOrBackingField(input, "PreviewSourceAttached", true);
        SetPropertyOrBackingField(input, "GpuElementVisible", false);
        SetPropertyOrBackingField(input, "CpuElementVisible", true);
        SetPropertyOrBackingField(input, "PlaceholderVisible", false);
        SetPropertyOrBackingField(input, "FramesArrived", 31L);
        SetPropertyOrBackingField(input, "FramesDisplayed", 0L);
        SetPropertyOrBackingField(input, "FramesDropped", 2L);
        SetPropertyOrBackingField(input, "LastPresentedTick", Environment.TickCount64 - 4000);
        SetPropertyOrBackingField(input, "PreviewMinPresentationIntervalMs", 8.33d);
        SetPropertyOrBackingField(input, "StartupState", "WaitingForFirstVisual");
        SetPropertyOrBackingField(input, "IsStartupWaitingForFirstVisual", true);
        SetPropertyOrBackingField(input, "StartupAttemptId", "attempt-1");
        SetPropertyOrBackingField(input, "StartupRequestedUtc", DateTimeOffset.UtcNow.AddMilliseconds(-2000));
        SetPropertyOrBackingField(input, "StartupTimeoutMs", 1000);
        SetPropertyOrBackingField(input, "StartupGpuSignalMediaOpened", true);
        SetPropertyOrBackingField(input, "StartupGpuSignalFirstFrame", false);
        SetPropertyOrBackingField(input, "StartupGpuSignalPlaybackAdvancing", false);
        SetPropertyOrBackingField(input, "StartupRequiredSignals", requiredSignals);
        SetPropertyOrBackingField(input, "StartupReceivedSignals", receivedSignals);
        SetPropertyOrBackingField(input, "StartupStrategy", startupStrategy);
        SetPropertyOrBackingField(input, "StartupMissingSignals", "FirstVisual");
        SetPropertyOrBackingField(input, "StartupRecoveryAttemptCount", 3);
        SetPropertyOrBackingField(input, "StartupLastFailureReason", "timeout");
        SetPropertyOrBackingField(input, "FirstVisualConfirmed", false);
        SetPropertyOrBackingField(input, "GpuPositionEventCount", 7L);

        var snapshot = build.Invoke(null, new[] { input })
                       ?? throw new InvalidOperationException("PreviewRuntimeSnapshotController.Build returned null.");

        AssertEqual(true, GetBoolProperty(snapshot, "IsPreviewing"), "snapshot IsPreviewing");
        AssertEqual(false, GetBoolProperty(snapshot, "GpuActive"), "snapshot GpuActive");
        AssertEqual(true, GetBoolProperty(snapshot, "RendererAttached"), "snapshot RendererAttached");
        AssertEqual(false, GetBoolProperty(snapshot, "GpuElementVisible"), "snapshot GpuElementVisible");
        AssertEqual(true, GetBoolProperty(snapshot, "CpuElementVisible"), "snapshot CpuElementVisible");
        AssertEqual("CpuSoftwareBitmap", GetStringProperty(snapshot, "RendererMode"), "CPU renderer mode");
        AssertEqual("WaitingForFirstVisual", GetStringProperty(snapshot, "StartupState"), "startup state passthrough");
        AssertEqual("attempt-1", GetStringProperty(snapshot, "StartupAttemptId"), "startup attempt passthrough");
        AssertEqual("FirstVisual", GetStringProperty(snapshot, "StartupMissingSignals"), "missing signals passthrough");
        AssertEqual(requiredSignals, GetPropertyValue(snapshot, "StartupRequiredSignals"), "required startup signals");
        AssertEqual(receivedSignals, GetPropertyValue(snapshot, "StartupReceivedSignals"), "received startup signals");
        AssertEqual(startupStrategy, GetPropertyValue(snapshot, "StartupStrategy"), "startup strategy");
        AssertEqual(3, GetIntProperty(snapshot, "StartupRecoveryAttemptCount"), "startup recovery count");
        AssertEqual("timeout", GetStringProperty(snapshot, "StartupLastFailureReason"), "startup failure reason");
        AssertEqual(true, GetBoolProperty(snapshot, "StartupGpuSignalMediaOpened"), "media opened signal");
        AssertEqual(false, GetBoolProperty(snapshot, "StartupGpuSignalFirstFrame"), "first-frame signal");
        AssertEqual(false, GetBoolProperty(snapshot, "StartupGpuSignalPlaybackAdvancing"), "playback advancing signal");
        AssertEqual(true, GetDoubleProperty(snapshot, "StartupElapsedMs") >= 0, "startup elapsed is non-negative");
        AssertEqual(true, GetBoolProperty(snapshot, "BlankSuspected"), "blank suspected when CPU path receives frames but displays none");
        AssertEqual(true, GetBoolProperty(snapshot, "StallSuspected"), "stall suspected after stale last-presented tick");
        AssertEqual(31L, GetLongProperty(snapshot, "FramesArrived"), "frames arrived passthrough");
        AssertEqual(0L, GetLongProperty(snapshot, "FramesDisplayed"), "frames displayed passthrough");
        AssertEqual(2L, GetLongProperty(snapshot, "FramesDropped"), "frames dropped passthrough");
        AssertEqual(0, GetIntProperty(snapshot, "DisplayCadenceSampleCount"), "no D3D cadence samples");
        AssertEqual(-1L, GetLongProperty(snapshot, "D3DFrameStatsPresentCount"), "D3D present-count sentinel");
        AssertEqual(-1L, GetLongProperty(snapshot, "D3DFrameStatsPresentRefreshCount"), "D3D present-refresh sentinel");
        AssertEqual(-1L, GetLongProperty(snapshot, "D3DFrameStatsSyncRefreshCount"), "D3D sync-refresh sentinel");
        AssertEqual("None", GetStringProperty(snapshot, "D3DInputColorSpace"), "D3D input color fallback");
        AssertEqual("None", GetStringProperty(snapshot, "D3DOutputColorSpace"), "D3D output color fallback");
        AssertEqual("None", GetStringProperty(snapshot, "GpuPlaybackState"), "GPU playback fallback");
        AssertEqual(7L, GetLongProperty(snapshot, "GpuPositionEventCount"), "GPU position event count");

        return Task.CompletedTask;
    }

    private static Task PreviewRuntimeSnapshotHealthPolicy_PreservesSuspicionRules()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealthInput");
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealthPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthPolicy.Evaluate not found.");
        var now = DateTimeOffset.UtcNow;

        var cpuPathInput = Activator.CreateInstance(inputType)
                           ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealthInput.");
        SetPropertyOrBackingField(cpuPathInput, "IsPreviewing", true);
        SetPropertyOrBackingField(cpuPathInput, "IsStartupWaitingForFirstVisual", true);
        SetPropertyOrBackingField(cpuPathInput, "StartupRequestedUtc", now.AddMilliseconds(-2000));
        SetPropertyOrBackingField(cpuPathInput, "StartupTimeoutMs", 1000);
        SetPropertyOrBackingField(cpuPathInput, "RendererAttached", true);
        SetPropertyOrBackingField(cpuPathInput, "GpuActive", false);
        SetPropertyOrBackingField(cpuPathInput, "FramesArrived", 31L);
        SetPropertyOrBackingField(cpuPathInput, "FramesDisplayed", 0L);
        SetPropertyOrBackingField(cpuPathInput, "LastPresentedTick", 1000L);
        SetPropertyOrBackingField(cpuPathInput, "CurrentTick", 4001L);
        SetPropertyOrBackingField(cpuPathInput, "UtcNow", now);

        var cpuPathHealth = evaluate.Invoke(null, new[] { cpuPathInput })
                            ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthPolicy returned null.");
        AssertEqual(true, GetDoubleProperty(cpuPathHealth, "StartupElapsedMs") >= 2000, "startup elapsed uses supplied clock");
        AssertEqual(true, GetBoolProperty(cpuPathHealth, "BlankSuspected"), "CPU path blank suspected");
        AssertEqual(true, GetBoolProperty(cpuPathHealth, "StallSuspected"), "CPU path stall suspected");

        var gpuPathInput = Activator.CreateInstance(inputType)
                           ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealthInput.");
        SetPropertyOrBackingField(gpuPathInput, "IsPreviewing", true);
        SetPropertyOrBackingField(gpuPathInput, "RendererAttached", true);
        SetPropertyOrBackingField(gpuPathInput, "GpuActive", true);
        SetPropertyOrBackingField(gpuPathInput, "FramesArrived", 31L);
        SetPropertyOrBackingField(gpuPathInput, "FramesDisplayed", 0L);
        SetPropertyOrBackingField(gpuPathInput, "LastPresentedTick", 1000L);
        SetPropertyOrBackingField(gpuPathInput, "CurrentTick", 4001L);
        SetPropertyOrBackingField(gpuPathInput, "UtcNow", now);

        var gpuPathHealth = evaluate.Invoke(null, new[] { gpuPathInput })
                            ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthPolicy returned null.");
        AssertEqual(false, GetBoolProperty(gpuPathHealth, "BlankSuspected"), "GPU path does not use CPU blank suspicion");
        AssertEqual(false, GetBoolProperty(gpuPathHealth, "StallSuspected"), "GPU path does not use CPU stall suspicion");

        var timeoutInput = Activator.CreateInstance(inputType)
                           ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealthInput.");
        SetPropertyOrBackingField(timeoutInput, "IsPreviewing", true);
        SetPropertyOrBackingField(timeoutInput, "IsStartupWaitingForFirstVisual", true);
        SetPropertyOrBackingField(timeoutInput, "StartupRequestedUtc", now.AddMilliseconds(-1500));
        SetPropertyOrBackingField(timeoutInput, "StartupTimeoutMs", 1000);
        SetPropertyOrBackingField(timeoutInput, "RendererAttached", true);
        SetPropertyOrBackingField(timeoutInput, "GpuActive", false);
        SetPropertyOrBackingField(timeoutInput, "FramesArrived", 0L);
        SetPropertyOrBackingField(timeoutInput, "FramesDisplayed", 0L);
        SetPropertyOrBackingField(timeoutInput, "CurrentTick", 4001L);
        SetPropertyOrBackingField(timeoutInput, "UtcNow", now);

        var timeoutHealth = evaluate.Invoke(null, new[] { timeoutInput })
                            ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthPolicy returned null.");
        AssertEqual(true, GetBoolProperty(timeoutHealth, "BlankSuspected"), "startup timeout marks blank suspected");

        return Task.CompletedTask;
    }

    private static Task PreviewRuntimeSnapshotHealthInputFactory_ProjectsControllerInputs()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var projectionType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DProjection");
        var factoryType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealthInputFactory");
        var build = factoryType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthInputFactory.Build not found.");
        var now = DateTimeOffset.UtcNow;

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "IsPreviewing", true);
        SetPropertyOrBackingField(input, "IsStartupWaitingForFirstVisual", true);
        SetPropertyOrBackingField(input, "StartupRequestedUtc", now.AddMilliseconds(-2500));
        SetPropertyOrBackingField(input, "StartupTimeoutMs", 1200);
        SetPropertyOrBackingField(input, "LastPresentedTick", 42L);

        var projection = Activator.CreateInstance(projectionType)
                         ?? throw new InvalidOperationException("Failed to create PreviewRuntimeD3DProjection.");
        SetPropertyOrBackingField(projection, "RendererAttached", true);
        SetPropertyOrBackingField(projection, "GpuActive", false);
        SetPropertyOrBackingField(projection, "FramesArrived", 55L);
        SetPropertyOrBackingField(projection, "FramesDisplayed", 6L);

        var healthInput = build.Invoke(null, new object[] { input, projection, 999L, now })
                          ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthInputFactory returned null.");
        AssertEqual(true, GetBoolProperty(healthInput, "IsPreviewing"), "health input previewing");
        AssertEqual(true, GetBoolProperty(healthInput, "IsStartupWaitingForFirstVisual"), "health input waiting for first visual");
        AssertEqual(GetPropertyValue(input, "StartupRequestedUtc"), GetPropertyValue(healthInput, "StartupRequestedUtc"), "health input startup request time");
        AssertEqual(1200, GetIntProperty(healthInput, "StartupTimeoutMs"), "health input startup timeout");
        AssertEqual(true, GetBoolProperty(healthInput, "RendererAttached"), "health input renderer attached");
        AssertEqual(false, GetBoolProperty(healthInput, "GpuActive"), "health input GPU active");
        AssertEqual(55L, GetLongProperty(healthInput, "FramesArrived"), "health input frames arrived");
        AssertEqual(6L, GetLongProperty(healthInput, "FramesDisplayed"), "health input frames displayed");
        AssertEqual(42L, GetLongProperty(healthInput, "LastPresentedTick"), "health input last presented tick");
        AssertEqual(999L, GetLongProperty(healthInput, "CurrentTick"), "health input current tick");
        AssertEqual(now, GetPropertyValue(healthInput, "UtcNow"), "health input clock");

        return Task.CompletedTask;
    }

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
