using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task D3D11PreviewRenderer_SubmissionLivesInFocusedPartial()
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

    internal static Task D3D11PreviewRenderer_LifecycleLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Lifecycle.cs")
            .Replace("\r\n", "\n");
        var stopLifecycleText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.StopLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(lifecycleText, "private readonly object _lifecycleLock = new();");
        AssertContains(lifecycleText, "private Thread? _renderThread;");
        AssertContains(lifecycleText, "private int _disposed;");
        AssertContains(lifecycleText, "private double _startupFps = 60.0;");
        AssertContains(lifecycleText, "public void Start(int width, int height, double fps, bool isHdr)");
        AssertContains(lifecycleText, "public void Dispose()");
        AssertContains(stopLifecycleText, "private int _stopRequested;");
        AssertContains(stopLifecycleText, "private int _inNativeCall;");
        AssertContains(stopLifecycleText, "public void StopRenderThread()");
        AssertContains(stopLifecycleText, "public void Stop()");
        AssertContains(stopLifecycleText, "private void WaitForNativeCallToDrainOrThrow(string operation)");
        AssertContains(stopLifecycleText, "WaitForNativeCallToDrainOrThrow(\"stop\");");
        AssertContains(stopLifecycleText, "FailPendingFrameCapture(\"Preview renderer stopped before frame capture completed.\");");
        AssertContains(stopLifecycleText, "WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel)");
        AssertDoesNotContain(lifecycleText, "public void StopRenderThread()");
        AssertDoesNotContain(lifecycleText, "public void Stop()");
        AssertDoesNotContain(lifecycleText, "private void WaitForNativeCallToDrainOrThrow(string operation)");
        AssertDoesNotContain(rootText, "public void Start(int width, int height, double fps, bool isHdr)");
        AssertDoesNotContain(rootText, "public void StopRenderThread()");
        AssertDoesNotContain(rootText, "private void WaitForNativeCallToDrainOrThrow(string operation)");
        AssertDoesNotContain(rootText, "private readonly object _lifecycleLock = new();");
        AssertDoesNotContain(rootText, "private Thread? _renderThread;");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_ScreenshotEncodingLivesWithScreenshotCapture()
    {
        var captureText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotCapture.cs")
            .Replace("\r\n", "\n");
        var pngCompletionText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotPngCompletion.cs")
            .Replace("\r\n", "\n");
        var stagingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotStaging.cs")
            .Replace("\r\n", "\n");
        var resultsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotResults.cs")
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
        AssertContains(captureRequestsText, "if (IsPngFrameCaptureCompletionInProgress())");
        AssertDoesNotContain(captureRequestsText, "private void DisposeFrameCaptureStagingResources()");
        AssertDoesNotContain(captureRequestsText, "private static PreviewFrameCaptureResult CreateFrameCaptureError(");
        AssertContains(captureText, "EnsureFrameCaptureStagingTexture(backBufferDescription, width, height)");
        AssertContains(captureText, "BeginPngFrameCaptureCompletion(");
        AssertContains(captureText, "TryBeginPngFrameCaptureCompletion()");
        AssertContains(captureText, "EndPngFrameCaptureCompletion();");
        AssertContains(captureText, "LogFrameCaptureResult(captureResult);");
        AssertContains(captureText, "LogFrameCaptureFailure(ex, rendererMode);");
        AssertContains(captureText, "PreviewScreenshotCapture.CopyMappedFrameToBuffer(");
        AssertContains(captureText, "PreviewScreenshotCapture.CaptureMappedFrameToBmp(");
        AssertContains(stagingText, "private ID3D11Texture2D? _captureStagingTexture;");
        AssertContains(stagingText, "private ID3D11Texture2D EnsureFrameCaptureStagingTexture(");
        AssertContains(stagingText, "_captureStagingTexture = _device!.CreateTexture2D(");
        AssertContains(stagingText, "private void DisposeFrameCaptureStagingResources()");
        AssertContains(stagingText, "_captureStagingTexture?.Dispose();");
        AssertContains(pngCompletionText, "private void BeginPngFrameCaptureCompletion(");
        AssertContains(pngCompletionText, "private int _frameCaptureEncodeInProgress;");
        AssertContains(pngCompletionText, "private bool IsPngFrameCaptureCompletionInProgress()");
        AssertContains(pngCompletionText, "private bool TryBeginPngFrameCaptureCompletion()");
        AssertContains(pngCompletionText, "private void EndPngFrameCaptureCompletion()");
        AssertContains(pngCompletionText, "PreviewScreenshotCapture.CaptureFrameBufferTo16BitPng(");
        AssertContains(pngCompletionText, "Interlocked.Exchange(ref _frameCaptureEncodeInProgress, 0);");
        AssertContains(pngCompletionText, "LogFrameCaptureResult(captureResult);");
        AssertContains(pngCompletionText, "LogFrameCaptureFailure(ex, rendererMode);");
        AssertContains(resultsText, "private static PreviewFrameCaptureResult CreateFrameCaptureError(");
        AssertContains(resultsText, "private static void LogFrameCaptureResult(PreviewFrameCaptureResult captureResult)");
        AssertContains(resultsText, "private static void LogFrameCaptureFailure(Exception ex, string rendererMode)");
        AssertContains(resultsText, "LuminanceHistogram = new int[16]");
        AssertDoesNotContain(captureRequestsText, "_captureStagingTexture?.Dispose();");
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
        AssertDoesNotContain(captureText, "PreviewScreenshotCapture.CaptureFrameBufferTo16BitPng(");
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
