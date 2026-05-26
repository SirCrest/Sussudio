using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task D3D11PreviewRenderer_DropPendingFrames_DrainsQueueAndMarksGeneration()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var pendingFrameType = rendererType.GetNestedType("PendingFrame", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PendingFrame nested type not found.");
        var queueType = typeof(System.Collections.Concurrent.ConcurrentQueue<>).MakeGenericType(pendingFrameType);
        var renderer = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(rendererType);
        SetPrivateField(renderer, "_lifecycleLock", new object());
        SetPrivateField(renderer, "_pendingFrames", Activator.CreateInstance(queueType));
        SetPrivateField(renderer, "_frameReadyEvent", new System.Threading.ManualResetEventSlim(false));
        SetPrivateField(renderer, "_renderThread", System.Threading.Thread.CurrentThread);
        SetPrivateField(renderer, "_maxPendingFrames", 4);

        InvokeNonPublicInstanceMethod(
            renderer,
            "EnqueuePendingFrame",
            new[] { CreateRawPendingD3DFrame(pendingFrameType, 101L, 1001L) });
        InvokeNonPublicInstanceMethod(
            renderer,
            "EnqueuePendingFrame",
            new[] { CreateRawPendingD3DFrame(pendingFrameType, 102L, 1002L) });

        AssertEqual(2, Convert.ToInt32(GetPropertyValue(renderer, "PendingFrameCount")), "pending frame count before drain");
        AssertEqual(2L, Convert.ToInt64(GetPropertyValue(renderer, "FramesSubmitted")), "frames submitted before drain");
        AssertEqual(0L, Convert.ToInt64(GetPropertyValue(renderer, "FramesDropped")), "frames dropped before drain");

        var dropMethod = rendererType.GetMethod("DropPendingFrames", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("DropPendingFrames method not found.");
        var dropped = Convert.ToInt32(dropMethod.Invoke(renderer, new object[] { "flashback-go-live" }));

        AssertEqual(2, dropped, "pending frames drained");
        AssertEqual(0, Convert.ToInt32(GetPropertyValue(renderer, "PendingFrameCount")), "pending frame count after drain");
        AssertEqual(2L, Convert.ToInt64(GetPropertyValue(renderer, "FramesDropped")), "frames dropped after drain");
        AssertEqual(1L, GetLongPrivateField(renderer, "_submissionGeneration"), "submission generation after drain");
        AssertEqual("flashback-go-live", GetStringPrivateField(renderer, "_submissionGenerationDropReason"), "submission generation reason");

        var ownership = rendererType.GetMethod("GetFrameOwnershipMetrics", BindingFlags.Public | BindingFlags.Instance)!
            .Invoke(renderer, Array.Empty<object>())
            ?? throw new InvalidOperationException("GetFrameOwnershipMetrics returned null.");
        AssertEqual("flashback-go-live", GetPropertyValue(ownership, "LastDropReason") as string, "last D3D drop reason");
        AssertEqual(1002L, Convert.ToInt64(GetPropertyValue(ownership, "LastDroppedPreviewPresentId")), "last dropped preview present id");
        AssertEqual(102L, Convert.ToInt64(GetPropertyValue(ownership, "LastDroppedSourceSequenceNumber")), "last dropped source sequence");

        var staleFrame = CreateRawPendingD3DFrame(pendingFrameType, 103L, 1003L);
        pendingFrameType.GetProperty("SubmissionGeneration", BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(staleFrame, 0L);
        var staleGeneration = Convert.ToInt64(pendingFrameType.GetProperty("SubmissionGeneration")!.GetValue(staleFrame));
        AssertEqual(true, staleGeneration != GetLongPrivateField(renderer, "_submissionGeneration"), "stale frame generation is rejected by render loop contract");
        ((IDisposable)staleFrame).Dispose();

        return Task.CompletedTask;

        static object CreateRawPendingD3DFrame(Type pendingFrameType, long sourceSequenceNumber, long previewPresentId)
        {
            var constructor = pendingFrameType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Single(ctor => ctor.GetParameters().Any(parameter => parameter.Name == "rawData"));
            var args = constructor.GetParameters()
                .Select(parameter =>
                {
                    if (string.Equals(parameter.Name, "rawData", StringComparison.Ordinal))
                    {
                        return null;
                    }

                    if (string.Equals(parameter.Name, "rawDataLength", StringComparison.Ordinal))
                    {
                        return 0;
                    }

                    if (string.Equals(parameter.Name, "width", StringComparison.Ordinal) ||
                        string.Equals(parameter.Name, "height", StringComparison.Ordinal))
                    {
                        return 16;
                    }

                    if (string.Equals(parameter.Name, "isHdr", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    if (string.Equals(parameter.Name, "arrivalTick", StringComparison.Ordinal) ||
                        string.Equals(parameter.Name, "schedulerSubmitTick", StringComparison.Ordinal))
                    {
                        return Stopwatch.GetTimestamp();
                    }

                    if (string.Equals(parameter.Name, "sourceSequenceNumber", StringComparison.Ordinal))
                    {
                        return sourceSequenceNumber;
                    }

                    if (string.Equals(parameter.Name, "previewPresentId", StringComparison.Ordinal))
                    {
                        return previewPresentId;
                    }

                    return parameter.ParameterType.IsValueType
                        ? Activator.CreateInstance(parameter.ParameterType)
                        : null;
                })
                .ToArray();
            return constructor.Invoke(args)
                   ?? throw new InvalidOperationException("PendingFrame constructor returned null.");
        }
    }

    internal static Task D3D11PreviewRenderer_FrameCaptureCancellationClearsPendingRequest()
    {
        var rendererText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotCapture.cs")
            .Replace("\r\n", "\n");
        var captureMethod = ExtractTextBetween(
            rendererText,
            "public Task<PreviewFrameCaptureResult> CaptureNextFrameAsync(string outputPath, CancellationToken cancellationToken)",
            "    private void TryCaptureFrameBeforePresent");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Snapshots.cs")
                .Replace("\r\n", "\n");

        AssertContains(captureMethod, "if (cancellationToken.IsCancellationRequested)");
        AssertContains(captureMethod, "Preview frame capture canceled.");
        AssertContains(captureMethod, "CancellationTokenRegistration cancellationRegistration = default;");
        AssertContains(captureMethod, "cancellationToken.Register(");
        AssertContains(captureMethod, "Interlocked.CompareExchange(ref renderer._frameCaptureRequest, null, request)");
        AssertContains(captureMethod, "Interlocked.Exchange(ref renderer._frameCaptureOutputPath, null);");
        AssertContains(captureMethod, "PREVIEW_FRAME_CAPTURE_CANCELED");
        AssertContains(captureMethod, "_ = request.Task.ContinueWith(");
        AssertContains(captureServiceText, "return await d3dSink.CaptureNextFrameAsync(outputPath, cancellationToken).ConfigureAwait(false);");
        AssertContains(captureServiceText, "while (_isVideoPreviewActive && !cancellationToken.IsCancellationRequested)");
        AssertDoesNotContain(captureServiceText, "cancellationToken.ThrowIfCancellationRequested();\n        return d3dSink.CaptureNextFrameAsync(outputPath);");

        return Task.CompletedTask;
    }

    internal static Task SharedD3DDeviceManager_DuplicatesReferencesUnderLifecycleLock()
    {
        var managerType = RequireType("Sussudio.Services.Preview.SharedD3DDeviceManager");
        AssertNotNull(
            managerType.GetMethod("TryCreateDeviceReference", BindingFlags.Public | BindingFlags.Instance),
            "SharedD3DDeviceManager.TryCreateDeviceReference");

        var managerText = ReadRepoFile("Sussudio/Services/Preview/SharedD3DDeviceManager.cs")
            .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewStart.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceRecordingFinalizationSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
                .Replace("\r\n", "\n");
        var duplicateMethod = ExtractTextBetween(
            managerText,
            "public bool TryCreateDeviceReference",
            "\n    public void Dispose()");
        var disposeMethod = ExtractTextBetween(
            managerText,
            "public void Dispose()",
            "\n    private void Initialize()");
        var applyMethod = ExtractTextBetween(
            captureServiceText,
            "private void TryApplySharedPreviewDevice",
            "\n    private async Task DisposeTransientRecordingBackendAsync");

        AssertContains(managerText, "private readonly object _sync = new();");
        AssertContains(duplicateMethod, "lock (_sync)");
        AssertContains(duplicateMethod, "if (Volatile.Read(ref _disposed) != 0)");
        AssertContains(duplicateMethod, "var nativePointer = currentDevice.NativePointer;");
        AssertContains(duplicateMethod, "Marshal.AddRef(nativePointer);");
        AssertContains(duplicateMethod, "device = new ID3D11Device(nativePointer);");
        AssertContains(disposeMethod, "lock (_sync)");
        AssertContains(applyMethod, "d3dManager.TryCreateDeviceReference(out var sharedDevice, out var reason)");
        AssertContains(applyMethod, "UNIFIED_VIDEO_SHARED_DEVICE_APPLY_SKIP reason={reason}");
        AssertContains(applyMethod, "sharedDevice.Dispose();");
        AssertDoesNotContain(applyMethod, "capture.D3DManager?.Device");

        return Task.CompletedTask;
    }
}
