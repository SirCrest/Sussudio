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

    private static Task PreviewRuntimeSnapshotSurfaceProjectionPolicy_PreservesVisibilityAndHealthFields()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var projectionType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DProjection");
        var healthType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealth");
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotSurfaceProjectionPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeSnapshotSurfaceProjectionPolicy.Evaluate not found.");

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "IsPreviewing", true);
        SetPropertyOrBackingField(input, "PlaceholderVisible", false);
        SetPropertyOrBackingField(input, "GpuElementVisible", true);
        SetPropertyOrBackingField(input, "CpuElementVisible", false);

        var d3dProjection = Activator.CreateInstance(projectionType)
                            ?? throw new InvalidOperationException("Failed to create PreviewRuntimeD3DProjection.");
        SetPropertyOrBackingField(d3dProjection, "GpuActive", true);
        SetPropertyOrBackingField(d3dProjection, "RendererAttached", true);
        SetPropertyOrBackingField(d3dProjection, "FramesArrived", 101L);
        SetPropertyOrBackingField(d3dProjection, "FramesDisplayed", 99L);
        SetPropertyOrBackingField(d3dProjection, "FramesDropped", 2L);

        var health = Activator.CreateInstance(healthType, new object?[] { null, true, false })
                     ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealth.");
        var surface = evaluate.Invoke(null, new object?[] { input, d3dProjection, health })
                      ?? throw new InvalidOperationException("PreviewRuntimeSnapshotSurfaceProjectionPolicy returned null.");

        AssertEqual(true, GetBoolProperty(surface, "IsPreviewing"), "surface projection previewing");
        AssertEqual(true, GetBoolProperty(surface, "GpuActive"), "surface projection GPU active");
        AssertEqual(false, GetBoolProperty(surface, "PlaceholderVisible"), "surface projection placeholder visible");
        AssertEqual(true, GetBoolProperty(surface, "GpuElementVisible"), "surface projection GPU element visible");
        AssertEqual(false, GetBoolProperty(surface, "CpuElementVisible"), "surface projection CPU element visible");
        AssertEqual(true, GetBoolProperty(surface, "RendererAttached"), "surface projection renderer attached");
        AssertEqual(101L, GetLongProperty(surface, "FramesArrived"), "surface projection frames arrived");
        AssertEqual(99L, GetLongProperty(surface, "FramesDisplayed"), "surface projection frames displayed");
        AssertEqual(2L, GetLongProperty(surface, "FramesDropped"), "surface projection frames dropped");
        AssertEqual(true, GetBoolProperty(surface, "BlankSuspected"), "surface projection blank suspected");
        AssertEqual(false, GetBoolProperty(surface, "StallSuspected"), "surface projection stall suspected");

        return Task.CompletedTask;
    }

    private static Task PreviewRuntimeSnapshotStartupProjectionPolicy_PreservesSampledStartupFields()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var healthType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealth");
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotStartupProjectionPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeSnapshotStartupProjectionPolicy.Evaluate not found.");
        var requiredSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "FirstVisual");
        var receivedSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "MediaOpened");
        var startupStrategy = ParseEnum("Sussudio.Models.PreviewStartupStrategy", "D3D11VideoProcessor");

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "StartupState", "WaitingForFirstVisual");
        SetPropertyOrBackingField(input, "StartupAttemptId", "attempt-42");
        SetPropertyOrBackingField(input, "StartupTimeoutMs", 1250);
        SetPropertyOrBackingField(input, "StartupGpuSignalMediaOpened", true);
        SetPropertyOrBackingField(input, "StartupGpuSignalFirstFrame", false);
        SetPropertyOrBackingField(input, "StartupGpuSignalPlaybackAdvancing", true);
        SetPropertyOrBackingField(input, "StartupRequiredSignals", requiredSignals);
        SetPropertyOrBackingField(input, "StartupReceivedSignals", receivedSignals);
        SetPropertyOrBackingField(input, "StartupStrategy", startupStrategy);
        SetPropertyOrBackingField(input, "StartupMissingSignals", "FirstVisual");
        SetPropertyOrBackingField(input, "StartupRecoveryAttemptCount", 5);
        SetPropertyOrBackingField(input, "StartupLastFailureReason", "visual-timeout");
        SetPropertyOrBackingField(input, "FirstVisualConfirmed", true);

        var health = Activator.CreateInstance(healthType, new object?[] { 456.25d, true, false })
                     ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealth.");
        var startup = evaluate.Invoke(null, new object?[] { input, health })
                      ?? throw new InvalidOperationException("PreviewRuntimeSnapshotStartupProjectionPolicy returned null.");

        AssertEqual("WaitingForFirstVisual", GetStringProperty(startup, "State"), "startup projection state");
        AssertEqual("attempt-42", GetStringProperty(startup, "AttemptId"), "startup projection attempt id");
        AssertEqual(456.25d, GetDoubleProperty(startup, "ElapsedMs"), "startup projection elapsed");
        AssertEqual(1250, GetIntProperty(startup, "TimeoutMs"), "startup projection timeout");
        AssertEqual(true, GetBoolProperty(startup, "GpuSignalMediaOpened"), "startup projection media opened signal");
        AssertEqual(false, GetBoolProperty(startup, "GpuSignalFirstFrame"), "startup projection first frame signal");
        AssertEqual(true, GetBoolProperty(startup, "GpuSignalPlaybackAdvancing"), "startup projection playback signal");
        AssertEqual(requiredSignals, GetPropertyValue(startup, "RequiredSignals"), "startup projection required signals");
        AssertEqual(receivedSignals, GetPropertyValue(startup, "ReceivedSignals"), "startup projection received signals");
        AssertEqual(startupStrategy, GetPropertyValue(startup, "Strategy"), "startup projection strategy");
        AssertEqual("FirstVisual", GetStringProperty(startup, "MissingSignals"), "startup projection missing signals");
        AssertEqual(5, GetIntProperty(startup, "RecoveryAttemptCount"), "startup projection recovery count");
        AssertEqual("visual-timeout", GetStringProperty(startup, "LastFailureReason"), "startup projection failure reason");
        AssertEqual(true, GetBoolProperty(startup, "FirstVisualConfirmed"), "startup projection first visual confirmed");

        return Task.CompletedTask;
    }

    private static Task PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy_PreservesRendererAndEventFields()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var projectionType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DProjection");
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy.Evaluate not found.");

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "GpuPositionEventCount", 42L);

        var d3dProjection = Activator.CreateInstance(projectionType)
                            ?? throw new InvalidOperationException("Failed to create PreviewRuntimeD3DProjection.");
        SetPropertyOrBackingField(d3dProjection, "GpuPlaybackState", "Rendering");
        SetPropertyOrBackingField(d3dProjection, "GpuNaturalVideoWidth", 3840);
        SetPropertyOrBackingField(d3dProjection, "GpuNaturalVideoHeight", 2160);
        SetPropertyOrBackingField(d3dProjection, "GpuPositionMs", 1234.5d);

        var gpuPlayback = evaluate.Invoke(null, new object?[] { input, d3dProjection })
                          ?? throw new InvalidOperationException("PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy returned null.");

        AssertEqual("Rendering", GetStringProperty(gpuPlayback, "PlaybackState"), "GPU playback projection state");
        AssertEqual(3840, GetIntProperty(gpuPlayback, "NaturalVideoWidth"), "GPU playback projection natural width");
        AssertEqual(2160, GetIntProperty(gpuPlayback, "NaturalVideoHeight"), "GPU playback projection natural height");
        AssertEqual(1234.5d, GetDoubleProperty(gpuPlayback, "PositionMs"), "GPU playback projection position");
        AssertEqual(42L, GetLongProperty(gpuPlayback, "PositionEventCount"), "GPU playback projection event count");

        return Task.CompletedTask;
    }

}
