using System.IO;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task D3D11PreviewRenderer_RenderThreadLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var renderThreadText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs")
            .Replace("\r\n", "\n");
        var diagnosticsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(renderThreadText, "private void RenderThreadMain()");
        AssertContains(renderThreadText, "MmcssThreadRegistration.TryRegister");
        AssertContains(renderThreadText, "_frameReadyEvent.Wait");
        AssertContains(renderThreadText, "HandlePendingSharedDeviceResetOnRenderThread();");
        AssertContains(renderThreadText, "TryApplyPendingCompositionTransformOnRenderThread(out var skipFrameDispatch)");
        AssertContains(renderThreadText, "if (skipFrameDispatch)");
        AssertContains(renderThreadText, "ProcessRenderThreadFrameOrIdle()");
        AssertContains(renderThreadText, "CleanupRenderThreadExit();");
        AssertContains(renderThreadText, "NotifyRenderThreadFailed(ex);");
        AssertContains(renderThreadText, "private void HandlePendingSharedDeviceResetOnRenderThread()");
        AssertContains(renderThreadText, "TrackFrameDropped(stale, \"shared-device-reset\");");
        AssertContains(renderThreadText, "UnbindSwapChainFromPanel();");
        AssertContains(renderThreadText, "InitializeD3D();");
        AssertContains(renderThreadText, "private bool TryApplyPendingCompositionTransformOnRenderThread(out bool skipFrameDispatch)");
        AssertContains(renderThreadText, "skipFrameDispatch = true;");
        AssertContains(renderThreadText, "if (Volatile.Read(ref _stopRequested) != 0)");
        AssertContains(renderThreadText, "ApplyCompositionScaleTransform(swapChain);");
        AssertContains(renderThreadText, "HandleDeviceLost(ex);");
        AssertContains(renderThreadText, "private bool ProcessRenderThreadFrameOrIdle()");
        AssertContains(renderThreadText, "WaitForFrameLatencySignal();");
        AssertContains(renderThreadText, "RenderFrame(frame);");
        AssertContains(renderThreadText, "TrackFrameDropped(frame, \"render-failed\");");
        AssertContains(renderThreadText, "SignalFrameReady(\"render_loop_drain\");");
        AssertContains(renderThreadText, "private void CleanupRenderThreadExit()");
        AssertContains(renderThreadText, "TrackFrameDropped(stale, \"renderer-exit\");");
        AssertContains(renderThreadText, "FailPendingFrameCapture(\"Render thread exited before frame capture completed.\");");
        AssertContains(agentMapText, "D3D11PreviewRenderer.RenderThread.cs");
        AssertContains(agentMapText, "shared-device reset consumption/rebind");
        AssertContains(agentMapText, "queued-frame render dispatch");
        AssertContains(cleanupPlanText, "D3D11PreviewRenderer.RenderThread.cs");
        AssertContains(cleanupPlanText, "shared-device reset/rebind consumption");
        AssertContains(cleanupPlanText, "pending-frame render dispatch");
        AssertContains(diagnosticsText, "private string _lastRenderThreadFailureType = string.Empty;");
        AssertContains(diagnosticsText, "private long _renderThreadFailureCount;");
        AssertContains(diagnosticsText, "private void NotifyRenderThreadFailed(Exception ex)");
        AssertContains(diagnosticsText, "RenderThreadFailed?.Invoke(reason)");
        AssertContains(diagnosticsText, "private int _firstFrameRaised;");
        AssertContains(diagnosticsText, "private void ResetFirstFrameNotification()");
        AssertContains(diagnosticsText, "private void NotifyFirstFrameRendered(string message)");
        AssertContains(diagnosticsText, "FirstFrameRendered?.Invoke()");
        AssertContains(rootText, "ResetFirstFrameNotification();");
        AssertContains(renderPassesText, "NotifyFirstFrameRendered(firstFrameMessage);");
        var waitIndex = renderThreadText.IndexOf("WaitForFrameLatencySignal();", StringComparison.Ordinal);
        var renderIndex = renderThreadText.IndexOf("RenderFrame(frame);", StringComparison.Ordinal);
        if (waitIndex < 0 || renderIndex < 0 || waitIndex > renderIndex)
        {
            throw new InvalidOperationException("Render thread must wait for frame-latency signal before rendering the frame.");
        }

        AssertDoesNotContain(rootText, "private int _firstFrameRaised;");
        AssertDoesNotContain(rootText, "private string _lastRenderThreadFailureType = string.Empty;");
        AssertDoesNotContain(renderThreadText, "private int _firstFrameRaised;");
        AssertDoesNotContain(renderThreadText, "private string _lastRenderThreadFailureType = string.Empty;");
        AssertDoesNotContain(renderPassesText, "private void RenderThreadMain()");
        AssertDoesNotContain(renderPassesText, "private void NotifyRenderThreadFailed(Exception ex)");
        AssertDoesNotContain(renderPassesText, "FirstFrameRendered?.Invoke()");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_IsDeviceLostException_ClassifiesCorrectly()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var method = rendererType.GetMethod(
            "IsDeviceLostException",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("IsDeviceLostException not found.");

        var regularEx = new InvalidOperationException("test");
        AssertEqual(false, (bool)method.Invoke(null, new object[] { regularEx })!, "Regular exception is not device lost");

        var deviceRemovedEx = new System.Runtime.InteropServices.COMException("Device removed", unchecked((int)0x887A0005));
        AssertEqual(true, (bool)method.Invoke(null, new object[] { deviceRemovedEx })!, "DeviceRemoved COMException is device lost");

        var deviceResetEx = new System.Runtime.InteropServices.COMException("Device reset", unchecked((int)0x887A0007));
        AssertEqual(true, (bool)method.Invoke(null, new object[] { deviceResetEx })!, "DeviceReset COMException is device lost");

        var otherComEx = new System.Runtime.InteropServices.COMException("Other", unchecked((int)0x80004005));
        AssertEqual(false, (bool)method.Invoke(null, new object[] { otherComEx })!, "Other COMException is not device lost");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_DeviceLostRecoveryLivesInFocusedPartial()
    {
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var deviceInitializationText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs")
            .Replace("\r\n", "\n");

        AssertContains(deviceInitializationText, "private void HandleDeviceLost(Exception ex)");
        AssertContains(deviceInitializationText, "private static bool IsDeviceLostException(Exception ex)");
        AssertContains(deviceInitializationText, "TrackFrameDropped(stalePending, \"device-lost\");");
        AssertContains(deviceInitializationText, "ResultCode.DeviceRemoved");
        AssertContains(deviceInitializationText, "unchecked((int)0x887A0005)");
        AssertDoesNotContain(resourcesText, "private void HandleDeviceLost(Exception ex)");
        AssertDoesNotContain(resourcesText, "private static bool IsDeviceLostException(Exception ex)");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_PresentAccountingLivesWithRenderPasses()
    {
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Present.cs")),
            "D3D11 preview present/accounting lives with render-pass execution");
        AssertContains(renderPassesText, "private void PresentAndTrackFrame(");
        AssertContains(renderPassesText, "TryCaptureFrameBeforePresent(rendererMode);");
        AssertContains(renderPassesText, "var presentResult = swapChain.Present((uint)_presentSyncInterval, PresentFlags.None);");
        AssertContains(renderPassesText, "TrackPresentCadence(frame.CountForPresentCadence);");
        AssertContains(renderPassesText, "var estimatedVisibleTick = EstimateVisibleTick(presentEnd);");
        AssertContains(renderPassesText, "RecordSlowFrameDiagnostic(frame, presentIntervalMs, inputUploadTicks, renderTicks, presentTicks, totalTicks, presentEnd, estimatedVisibleTick);");
        var captureIndex = renderPassesText.IndexOf("TryCaptureFrameBeforePresent(rendererMode);", StringComparison.Ordinal);
        var presentIndex = renderPassesText.IndexOf("var presentResult = swapChain.Present((uint)_presentSyncInterval, PresentFlags.None);", StringComparison.Ordinal);
        if (captureIndex < 0 || presentIndex < 0 || captureIndex > presentIndex)
        {
            throw new InvalidOperationException("Present transaction must capture screenshots before swap-chain Present.");
        }

        return Task.CompletedTask;
    }
}
