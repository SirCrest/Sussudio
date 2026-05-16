using System.Threading.Tasks;

static partial class Program
{
    private static Task D3D11PreviewRenderer_RenderThreadLivesInFocusedPartial()
    {
        var renderingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Rendering.cs")
            .Replace("\r\n", "\n");
        var renderThreadText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs")
            .Replace("\r\n", "\n");

        AssertContains(renderThreadText, "private void RenderThreadMain()");
        AssertContains(renderThreadText, "private void NotifyRenderThreadFailed(Exception ex)");
        AssertContains(renderThreadText, "MmcssThreadRegistration.TryRegister");
        AssertContains(renderThreadText, "_frameReadyEvent.Wait");
        AssertContains(renderThreadText, "WaitForFrameLatencySignal();");
        AssertContains(renderThreadText, "RenderFrame(frame);");
        AssertContains(renderThreadText, "HandleDeviceLost(ex);");
        AssertContains(renderThreadText, "FailPendingFrameCapture(\"Render thread exited before frame capture completed.\");");
        var waitIndex = renderThreadText.IndexOf("WaitForFrameLatencySignal();", StringComparison.Ordinal);
        var renderIndex = renderThreadText.IndexOf("RenderFrame(frame);", StringComparison.Ordinal);
        if (waitIndex < 0 || renderIndex < 0 || waitIndex > renderIndex)
        {
            throw new InvalidOperationException("Render thread must wait for frame-latency signal before rendering the frame.");
        }

        AssertDoesNotContain(renderingText, "private void RenderThreadMain()");
        AssertDoesNotContain(renderingText, "private void NotifyRenderThreadFailed(Exception ex)");
        AssertContains(renderingText, "private void PresentAndTrackFrame(");
        AssertContains(renderingText, "TrackPresentCadence(frame.CountForPresentCadence);");

        return Task.CompletedTask;
    }
}
