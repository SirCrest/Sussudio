using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task D3D11PreviewRenderer_SubmissionLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var pendingFramesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PendingFrames.cs")
            .Replace("\r\n", "\n");
        var submissionText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Submission.cs")
            .Replace("\r\n", "\n");
        var nv12SubmissionText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Nv12Submission.cs")
            .Replace("\r\n", "\n");

        AssertContains(nv12SubmissionText, "private bool _loggedNv12ShaderMissing;");
        AssertContains(nv12SubmissionText, "private int _lastNv12IsHdr = -1;");
        AssertContains(pendingFramesText, "private readonly ManualResetEventSlim _frameReadyEvent = new(false);");
        AssertContains(pendingFramesText, "private readonly ConcurrentQueue<PendingFrame> _pendingFrames = new();");
        AssertContains(pendingFramesText, "private int _pendingFrameCount;");
        AssertContains(submissionText, "public void SubmitRawFrame(");
        AssertContains(submissionText, "public void SubmitRawFrameLease(");
        AssertContains(submissionText, "public void SubmitTexture(");
        AssertContains(nv12SubmissionText, "public void SubmitNv12PlaneTextures(");
        AssertContains(nv12SubmissionText, "private void EnqueueNv12Frame(");
        AssertContains(submissionText, "EnqueuePendingFrame(frame);");
        AssertDoesNotContain(submissionText, "public void SubmitNv12PlaneTextures(");
        AssertDoesNotContain(submissionText, "private void EnqueueNv12Frame(");
        AssertDoesNotContain(rootText, "public void SubmitRawFrame(");
        AssertDoesNotContain(rootText, "public void SubmitRawFrameLease(");
        AssertDoesNotContain(rootText, "public void SubmitTexture(");
        AssertDoesNotContain(rootText, "public void SubmitNv12PlaneTextures(");
        AssertDoesNotContain(rootText, "private readonly ManualResetEventSlim _frameReadyEvent = new(false);");
        AssertDoesNotContain(rootText, "private readonly ConcurrentQueue<PendingFrame> _pendingFrames = new();");
        AssertDoesNotContain(rootText, "private bool _loggedNv12ShaderMissing;");
        AssertDoesNotContain(rootText, "private int _lastNv12IsHdr = -1;");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_LifecycleLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Lifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(lifecycleText, "private readonly object _lifecycleLock = new();");
        AssertContains(lifecycleText, "private Thread? _renderThread;");
        AssertContains(lifecycleText, "private int _disposed;");
        AssertContains(lifecycleText, "private int _inNativeCall;");
        AssertContains(lifecycleText, "private double _startupFps = 60.0;");
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
        AssertDoesNotContain(rootText, "private readonly object _lifecycleLock = new();");
        AssertDoesNotContain(rootText, "private Thread? _renderThread;");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_ScreenshotEncodingLivesWithScreenshotCapture()
    {
        var captureText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotCapture.cs")
            .Replace("\r\n", "\n");
        var captureRequestsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotRequests.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var previewScreenshotCaptureText =
            ReadRepoFile("Sussudio/Services/Preview/PreviewScreenshotCapture.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/PreviewScreenshotCapture.Png.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/PreviewScreenshotCapture.Bmp.cs")
                .Replace("\r\n", "\n");
        var previewPngEncoderText = ReadRepoFile("Sussudio/Services/Preview/PreviewPng16Encoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureText, "private void TryCaptureFrameBeforePresent(string rendererMode)");
        AssertDoesNotContain(captureText, "public Task<PreviewFrameCaptureResult> CaptureNextFrameAsync(");
        AssertDoesNotContain(captureText, "private void FailPendingFrameCapture(string message)");
        AssertDoesNotContain(captureText, "private void DisposeFrameCaptureStagingResources()");
        AssertContains(captureRequestsText, "public Task<PreviewFrameCaptureResult> CaptureNextFrameAsync(string outputPath, CancellationToken cancellationToken)");
        AssertContains(captureRequestsText, "private const int FrameCaptureTimeoutMs = 5000;");
        AssertContains(captureRequestsText, "private TaskCompletionSource<PreviewFrameCaptureResult>? _frameCaptureRequest;");
        AssertContains(captureRequestsText, "private void FailPendingFrameCapture(string message)");
        AssertContains(captureRequestsText, "private void DisposeFrameCaptureStagingResources()");
        AssertContains(captureRequestsText, "private static PreviewFrameCaptureResult CreateFrameCaptureError(");
        AssertContains(captureText, "PreviewScreenshotCapture.CaptureFrameBufferTo16BitPng(");
        AssertContains(captureText, "PreviewScreenshotCapture.CopyMappedFrameToBuffer(");
        AssertContains(captureText, "PreviewScreenshotCapture.CaptureMappedFrameToBmp(");
        AssertContains(captureRequestsText, "_captureStagingTexture?.Dispose();");
        AssertContains(resourcesText, "DisposeFrameCaptureStagingResources();");
        AssertContains(previewScreenshotCaptureText, "internal static PreviewFrameCaptureResult CaptureMappedFrameToBmp(");
        AssertContains(previewScreenshotCaptureText, "internal static PreviewFrameCaptureResult CaptureFrameBufferTo16BitPng(");
        AssertContains(previewScreenshotCaptureText, "internal static byte[] CopyMappedFrameToBuffer(");
        AssertContains(previewScreenshotCaptureText, "private sealed class PreviewScreenshotPixelAnalysis");
        AssertContains(previewScreenshotCaptureText, "analysis.AnalyzePixel(");
        AssertContains(previewScreenshotCaptureText, "private static void WriteBitmapHeaders(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.ScreenshotEncoding.cs")),
            "renderer screenshot encoding partial removed");
        AssertDoesNotContain(captureText, "private static PreviewFrameCaptureResult CaptureMappedFrameToBmp(");
        AssertDoesNotContain(captureText, "private static void WriteBitmapHeaders(");
        AssertDoesNotContain(resourcesText, "_captureStagingTexture?.Dispose();");
        AssertContains(previewScreenshotCaptureText, "PreviewPng16Encoder.WriteCompressedRgb16Png(");
        AssertContains(previewPngEncoderText, "internal static class PreviewPng16Encoder");
        AssertContains(previewPngEncoderText, "internal static void WriteCompressedRgb16Png(");
        AssertContains(previewPngEncoderText, "internal static uint[] InitPngCrc32Table()");
        AssertContains(previewPngEncoderText, "private static void WritePngChunk(");
        AssertContains(previewPngEncoderText, "private static uint UpdatePngCrc32(");
        AssertDoesNotContain(previewScreenshotCaptureText, "private static void WritePngChunk(");
        AssertDoesNotContain(previewScreenshotCaptureText, "private static uint UpdatePngCrc32(");

        return Task.CompletedTask;
    }
}
