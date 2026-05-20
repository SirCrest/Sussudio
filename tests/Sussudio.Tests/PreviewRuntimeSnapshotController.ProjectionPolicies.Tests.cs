using System.Reflection;

static partial class Program
{
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
