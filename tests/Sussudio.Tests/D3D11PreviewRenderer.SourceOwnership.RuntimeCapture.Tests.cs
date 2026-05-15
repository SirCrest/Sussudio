using System.Threading.Tasks;

static partial class Program
{
    private static Task D3D11PreviewRenderer_SubmissionLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var submissionText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Submission.cs")
            .Replace("\r\n", "\n");

        AssertContains(submissionText, "public void SubmitRawFrame(");
        AssertContains(submissionText, "public void SubmitRawFrameLease(");
        AssertContains(submissionText, "public void SubmitTexture(");
        AssertContains(submissionText, "public void SubmitNv12PlaneTextures(");
        AssertContains(submissionText, "private void EnqueueNv12Frame(");
        AssertContains(submissionText, "EnqueuePendingFrame(frame);");
        AssertDoesNotContain(rootText, "public void SubmitRawFrame(");
        AssertDoesNotContain(rootText, "public void SubmitRawFrameLease(");
        AssertDoesNotContain(rootText, "public void SubmitTexture(");
        AssertDoesNotContain(rootText, "public void SubmitNv12PlaneTextures(");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_LifecycleLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Lifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(lifecycleText, "public void Start(int width, int height, double fps, bool isHdr)");
        AssertContains(lifecycleText, "public void StopRenderThread()");
        AssertContains(lifecycleText, "public void Stop()");
        AssertContains(lifecycleText, "private void WaitForNativeCallToDrainOrThrow(string operation)");
        AssertContains(lifecycleText, "public void Dispose()");
        AssertContains(lifecycleText, "WaitForNativeCallToDrainOrThrow(\"stop\");");
        AssertContains(lifecycleText, "FailPendingFrameCapture(\"Preview renderer stopped before frame capture completed.\");");
        AssertDoesNotContain(rootText, "public void Start(int width, int height, double fps, bool isHdr)");
        AssertDoesNotContain(rootText, "public void StopRenderThread()");
        AssertDoesNotContain(rootText, "private void WaitForNativeCallToDrainOrThrow(string operation)");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_ScreenshotEncodingLivesInFocusedPartial()
    {
        var captureText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotCapture.cs")
            .Replace("\r\n", "\n");
        var encodingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotEncoding.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureText, "private void TryCaptureFrameBeforePresent(string rendererMode)");
        AssertContains(captureText, "PreviewScreenshotCapture.CaptureFrameBufferTo16BitPng(");
        AssertContains(captureText, "private void FailPendingFrameCapture(string message)");
        AssertContains(encodingText, "private static PreviewFrameCaptureResult CaptureMappedFrameToBmp(");
        AssertContains(encodingText, "private static byte[] CopyMappedFrameToBuffer(");
        AssertContains(encodingText, "private static void WriteBitmapHeaders(");
        AssertContains(encodingText, "private static PreviewFrameCaptureResult CreateFrameCaptureError(");
        AssertDoesNotContain(captureText, "private static PreviewFrameCaptureResult CaptureMappedFrameToBmp(");
        AssertDoesNotContain(captureText, "private static void WriteBitmapHeaders(");

        return Task.CompletedTask;
    }
}
