using System.Reflection;

static partial class Program
{
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
}
