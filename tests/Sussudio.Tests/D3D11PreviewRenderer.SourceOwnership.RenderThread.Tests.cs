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
        var renderThreadFailuresText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThreadFailures.cs")
            .Replace("\r\n", "\n");
        var firstFrameNotificationsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FirstFrameNotifications.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Lifecycle.cs")
            .Replace("\r\n", "\n");
        var presentText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Present.cs")
            .Replace("\r\n", "\n");

        AssertContains(renderThreadText, "private void RenderThreadMain()");
        AssertContains(renderThreadText, "MmcssThreadRegistration.TryRegister");
        AssertContains(renderThreadText, "_frameReadyEvent.Wait");
        AssertContains(renderThreadText, "WaitForFrameLatencySignal();");
        AssertContains(renderThreadText, "RenderFrame(frame);");
        AssertContains(renderThreadText, "HandleDeviceLost(ex);");
        AssertContains(renderThreadText, "FailPendingFrameCapture(\"Render thread exited before frame capture completed.\");");
        AssertContains(renderThreadText, "NotifyRenderThreadFailed(ex);");
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
