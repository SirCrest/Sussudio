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
        var renderThreadSharedDeviceResetText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.SharedDeviceReset.cs")
            .Replace("\r\n", "\n");
        var renderThreadCompositionText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.Composition.cs")
            .Replace("\r\n", "\n");
        var renderThreadFrameDispatchText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.FrameDispatch.cs")
            .Replace("\r\n", "\n");
        var renderThreadShutdownText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.Shutdown.cs")
            .Replace("\r\n", "\n");
        var renderThreadFailuresText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThreadFailures.cs")
            .Replace("\r\n", "\n");
        var firstFrameNotificationsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FirstFrameNotifications.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Lifecycle.cs")
            .Replace("\r\n", "\n");
        var presentText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Present.cs")
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
        AssertContains(renderThreadSharedDeviceResetText, "private void HandlePendingSharedDeviceResetOnRenderThread()");
        AssertContains(renderThreadSharedDeviceResetText, "TrackFrameDropped(stale, \"shared-device-reset\");");
        AssertContains(renderThreadSharedDeviceResetText, "UnbindSwapChainFromPanel();");
        AssertContains(renderThreadSharedDeviceResetText, "InitializeD3D();");
        AssertContains(renderThreadCompositionText, "private bool TryApplyPendingCompositionTransformOnRenderThread(out bool skipFrameDispatch)");
        AssertContains(renderThreadCompositionText, "skipFrameDispatch = true;");
        AssertContains(renderThreadCompositionText, "if (Volatile.Read(ref _stopRequested) != 0)");
        AssertContains(renderThreadCompositionText, "ApplyCompositionScaleTransform(swapChain);");
        AssertContains(renderThreadCompositionText, "HandleDeviceLost(ex);");
        AssertContains(renderThreadFrameDispatchText, "private bool ProcessRenderThreadFrameOrIdle()");
        AssertContains(renderThreadFrameDispatchText, "WaitForFrameLatencySignal();");
        AssertContains(renderThreadFrameDispatchText, "RenderFrame(frame);");
        AssertContains(renderThreadFrameDispatchText, "TrackFrameDropped(frame, \"render-failed\");");
        AssertContains(renderThreadFrameDispatchText, "SignalFrameReady(\"render_loop_drain\");");
        AssertContains(renderThreadShutdownText, "private void CleanupRenderThreadExit()");
        AssertContains(renderThreadShutdownText, "TrackFrameDropped(stale, \"renderer-exit\");");
        AssertContains(renderThreadShutdownText, "FailPendingFrameCapture(\"Render thread exited before frame capture completed.\");");
        AssertContains(agentMapText, "D3D11PreviewRenderer.RenderThread.SharedDeviceReset.cs");
        AssertContains(agentMapText, "D3D11PreviewRenderer.RenderThread.Composition.cs");
        AssertContains(agentMapText, "D3D11PreviewRenderer.RenderThread.FrameDispatch.cs");
        AssertContains(agentMapText, "D3D11PreviewRenderer.RenderThread.Shutdown.cs");
        AssertContains(cleanupPlanText, "D3D11PreviewRenderer.RenderThread.SharedDeviceReset.cs");
        AssertContains(cleanupPlanText, "D3D11PreviewRenderer.RenderThread.Composition.cs");
        AssertContains(cleanupPlanText, "D3D11PreviewRenderer.RenderThread.FrameDispatch.cs");
        AssertContains(cleanupPlanText, "D3D11PreviewRenderer.RenderThread.Shutdown.cs");
        AssertContains(renderThreadFailuresText, "private string _lastRenderThreadFailureType = string.Empty;");
        AssertContains(renderThreadFailuresText, "private long _renderThreadFailureCount;");
        AssertContains(renderThreadFailuresText, "private void NotifyRenderThreadFailed(Exception ex)");
        AssertContains(renderThreadFailuresText, "RenderThreadFailed?.Invoke(reason)");
        AssertContains(firstFrameNotificationsText, "private int _firstFrameRaised;");
        AssertContains(firstFrameNotificationsText, "private void ResetFirstFrameNotification()");
        AssertContains(firstFrameNotificationsText, "private void NotifyFirstFrameRendered(string message)");
        AssertContains(firstFrameNotificationsText, "FirstFrameRendered?.Invoke()");
        AssertContains(lifecycleText, "ResetFirstFrameNotification();");
        AssertContains(presentText, "NotifyFirstFrameRendered(firstFrameMessage);");
        var waitIndex = renderThreadFrameDispatchText.IndexOf("WaitForFrameLatencySignal();", StringComparison.Ordinal);
        var renderIndex = renderThreadFrameDispatchText.IndexOf("RenderFrame(frame);", StringComparison.Ordinal);
        if (waitIndex < 0 || renderIndex < 0 || waitIndex > renderIndex)
        {
            throw new InvalidOperationException("Render thread must wait for frame-latency signal before rendering the frame.");
        }

        AssertDoesNotContain(rootText, "private int _firstFrameRaised;");
        AssertDoesNotContain(rootText, "private string _lastRenderThreadFailureType = string.Empty;");
        AssertDoesNotContain(renderThreadText, "private int _firstFrameRaised;");
        AssertDoesNotContain(renderThreadText, "private string _lastRenderThreadFailureType = string.Empty;");
        AssertDoesNotContain(renderThreadText, "private bool ProcessRenderThreadFrameOrIdle()");
        AssertDoesNotContain(renderThreadText, "private bool TryApplyPendingCompositionTransformOnRenderThread(");
        AssertDoesNotContain(renderThreadText, "private void HandlePendingSharedDeviceResetOnRenderThread()");
        AssertDoesNotContain(renderThreadText, "private void CleanupRenderThreadExit()");
        AssertDoesNotContain(renderPassesText, "private void RenderThreadMain()");
        AssertDoesNotContain(renderPassesText, "private void NotifyRenderThreadFailed(Exception ex)");
        AssertDoesNotContain(presentText, "FirstFrameRendered?.Invoke()");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_PresentAccountingLivesInFocusedPartial()
    {
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var presentText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Present.cs")
            .Replace("\r\n", "\n");

        AssertContains(presentText, "private void PresentAndTrackFrame(");
        AssertContains(presentText, "TryCaptureFrameBeforePresent(rendererMode);");
        AssertContains(presentText, "var presentResult = swapChain.Present((uint)_presentSyncInterval, PresentFlags.None);");
        AssertContains(presentText, "TrackPresentCadence(frame.CountForPresentCadence);");
        AssertContains(presentText, "var estimatedVisibleTick = EstimateVisibleTick(presentEnd);");
        AssertContains(presentText, "RecordSlowFrameDiagnostic(frame, presentIntervalMs, inputUploadTicks, renderTicks, presentTicks, totalTicks, presentEnd, estimatedVisibleTick);");
        AssertDoesNotContain(renderPassesText, "private void PresentAndTrackFrame(");
        var captureIndex = presentText.IndexOf("TryCaptureFrameBeforePresent(rendererMode);", StringComparison.Ordinal);
        var presentIndex = presentText.IndexOf("var presentResult = swapChain.Present((uint)_presentSyncInterval, PresentFlags.None);", StringComparison.Ordinal);
        if (captureIndex < 0 || presentIndex < 0 || captureIndex > presentIndex)
        {
            throw new InvalidOperationException("Present transaction must capture screenshots before swap-chain Present.");
        }

        return Task.CompletedTask;
    }
}
