using System;
using System.Threading.Tasks;

// Tests for recording sink queue limits, drops, and latency accounting.
static partial class Program
{
    internal static Task UnifiedVideoCapture_SinkFanoutLivesInFocusedPartial()
    {
        var rootSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs")
            .Replace("\r\n", "\n");
        var fanoutSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.cs")
            .Replace("\r\n", "\n");
        var flashbackFanoutSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.Flashback.cs")
            .Replace("\r\n", "\n");

        AssertContains(fanoutSource, "private void EnqueueRecordingFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertContains(fanoutSource, "private void EnqueueRecordingFrame(PooledVideoFrame frame)");
        AssertContains(fanoutSource, "private void EnqueueGpuRecordingFrame(IGpuVideoFrameEncoder encoder, IntPtr texture, int subresource, long sourceSequence)");
        AssertContains(flashbackFanoutSource, "private void EnqueueFlashbackFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertContains(flashbackFanoutSource, "private void EnqueueFlashbackFrame(PooledVideoFrame frame)");
        AssertContains(flashbackFanoutSource, "private void EnqueueFlashbackGpuFrame(IntPtr texture, int subresource, long sourceSequence)");
        AssertContains(flashbackFanoutSource, "private void TrackFlashbackRecordingAcceptedSequence(long sourceSequence)");
        AssertDoesNotContain(fanoutSource, "private void EnqueueFlashbackFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertDoesNotContain(flashbackFanoutSource, "private void EnqueueRecordingFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertDoesNotContain(rootSource, "private void EnqueueRecordingFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertDoesNotContain(rootSource, "private void EnqueueFlashbackFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertDoesNotContain(rootSource, "private static bool TryLegacyRawVideoEnqueue(");

        return Task.CompletedTask;
    }

    internal static Task UnifiedVideoCapture_FrameIngressLivesInFocusedPartial()
    {
        var rootSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs")
            .Replace("\r\n", "\n");
        var frameIngressSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.FrameIngress.cs")
            .Replace("\r\n", "\n");
        var previewSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.Preview.cs")
            .Replace("\r\n", "\n");

        AssertContains(frameIngressSource, "private void OnFrameArrived(ReadOnlySpan<byte> frameData, int width, int height, long arrivalTick)");
        AssertContains(frameIngressSource, "private void OnMjpegPipelineFrameEmitted(PooledVideoFrame frame)");
        AssertContains(frameIngressSource, "private void OnDualFrameArrived(");
        AssertContains(frameIngressSource, "private void RecordCaptureArrived(long sourceSequence, long arrivalTick, int width, int height, int compressedByteLength)");
        AssertContains(frameIngressSource, "private void FirePixelFormatObserverOnce(string format)");
        AssertContains(frameIngressSource, "private void SignalFatalError(Exception ex, string logMessage)");
        AssertDoesNotContain(frameIngressSource, "private void OnMjpegPipelinePreviewFrameDecoded(PooledVideoFrameLease frame)");
        AssertContains(previewSource, "private void OnMjpegPipelinePreviewFrameDecoded(PooledVideoFrameLease frame)");

        AssertDoesNotContain(rootSource, "private void OnFrameArrived(ReadOnlySpan<byte> frameData, int width, int height, long arrivalTick)");
        AssertDoesNotContain(rootSource, "private void OnMjpegPipelineFrameEmitted(PooledVideoFrame frame)");
        AssertDoesNotContain(rootSource, "private void OnDualFrameArrived(");
        AssertDoesNotContain(rootSource, "private void RecordCaptureArrived(long sourceSequence, long arrivalTick, int width, int height, int compressedByteLength)");
        AssertDoesNotContain(rootSource, "private void FirePixelFormatObserverOnce(string format)");
        AssertDoesNotContain(rootSource, "private void SignalFatalError(Exception ex, string logMessage)");

        var rawIngress = ExtractSourceBlock(
            frameIngressSource,
            "private void OnFrameArrived(ReadOnlySpan<byte> frameData, int width, int height, long arrivalTick)",
            "private void OnMjpegPipelineFrameEmitted(PooledVideoFrame frame)");
        AssertOccursBefore(rawIngress, "Interlocked.Increment(ref _videoFramesArrived)", "Interlocked.Exchange(ref _lastVideoFrameArrivedTick");
        AssertOccursBefore(rawIngress, "Interlocked.Exchange(ref _lastVideoFrameArrivedTick", "RecordCaptureArrived(sourceSequence, arrivalTick, width, height, frameData.Length);");
        AssertOccursBefore(rawIngress, "FrameLedgerStage.CompressedQueued", "return;");
        AssertOccursBefore(rawIngress, "FirePixelFormatObserverOnce(isP010 ? \"P010\" : \"NV12\");", "EnqueueRecordingFrame(frameData, width, height, isP010, sourceSequence);");
        AssertOccursBefore(rawIngress, "EnqueueRecordingFrame(frameData, width, height, isP010, sourceSequence);", "EnqueueFlashbackFrame(frameData, width, height, isP010, sourceSequence);");
        AssertOccursBefore(rawIngress, "EnqueueFlashbackFrame(frameData, width, height, isP010, sourceSequence);", "SubmitPreviewRawFrame(previewSink, frameData, width, height, isP010, arrivalTick, sourceSequence);");

        var mjpegIngress = ExtractSourceBlock(
            frameIngressSource,
            "private void OnMjpegPipelineFrameEmitted(PooledVideoFrame frame)",
            "private void OnDualFrameArrived(");
        AssertOccursBefore(mjpegIngress, "FirePixelFormatObserverOnce(\"NV12\");", "EnqueueRecordingFrame(frame);");
        AssertOccursBefore(mjpegIngress, "EnqueueRecordingFrame(frame);", "EnqueueFlashbackFrame(frame);");

        var dualIngress = ExtractSourceBlock(
            frameIngressSource,
            "private void OnDualFrameArrived(",
            "private void RecordCaptureArrived(long sourceSequence, long arrivalTick, int width, int height, int compressedByteLength)");
        AssertOccursBefore(dualIngress, "Interlocked.Increment(ref _videoFramesArrived)", "Interlocked.Exchange(ref _lastVideoFrameArrivedTick");
        AssertOccursBefore(dualIngress, "FirePixelFormatObserverOnce(isP010 ? \"P010\" : \"NV12\");", "var gpuEncoder = Volatile.Read(ref _gpuRecordingEncoder);");
        AssertOccursBefore(dualIngress, "EnqueueGpuRecordingFrame(gpuEncoder, gpuTexture, gpuSubresource, sourceSequence);", "EnqueueFlashbackGpuFrame(gpuTexture, gpuSubresource, sourceSequence);");
        AssertOccursBefore(dualIngress, "EnqueueRecordingFrame(frameData, width, height, isP010, sourceSequence);", "EnqueueFlashbackFrame(frameData, width, height, isP010, sourceSequence);");
        AssertOccursBefore(dualIngress, "EnqueueFlashbackFrame(frameData, width, height, isP010, sourceSequence);", "previewSink.SubmitTexture(");
        AssertOccursBefore(dualIngress, "Volatile.Read(ref _strictPreviewTextureRequired)", "SignalFatalError(");
        AssertOccursBefore(dualIngress, "Volatile.Read(ref _strictPreviewTextureRequired)", "SubmitPreviewRawFrame(previewSink, frameData, width, height, isP010, arrivalTick, sourceSequence);");

        AssertOccursBefore(frameIngressSource, "Logger.Log(logMessage);", "Interlocked.Exchange(ref _fatalErrorSignaled, 1)");
        AssertOccursBefore(frameIngressSource, "Interlocked.Exchange(ref _fatalErrorSignaled, 1)", "FatalErrorOccurred?.Invoke(this, ex);");
        AssertContains(frameIngressSource, "UNIFIED_VIDEO_FATAL_CALLBACK_FAIL");

        return Task.CompletedTask;
    }

    internal static Task UnifiedVideoCapture_LifecycleLivesInFocusedPartial()
    {
        var rootSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs")
            .Replace("\r\n", "\n");
        var lifecycleSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.Lifecycle.cs")
            .Replace("\r\n", "\n");
        var initializationSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.Initialization.cs")
            .Replace("\r\n", "\n");
        var mjpegLifecycleSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.MjpegPipelineLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(initializationSource, "public async Task InitializeAsync(");
        AssertContains(initializationSource, "var d3dManager = new SharedD3DDeviceManager();");
        AssertContains(initializationSource, "CreateExternalMjpegPipelineIfNeeded(");
        AssertContains(initializationSource, "InstallMjpegPreviewJitterBuffer(capture.Fps > 0 ? capture.Fps : fps);");
        AssertContains(initializationSource, "capture.FatalErrorOccurred += OnCaptureFatalError;");
        AssertDoesNotContain(lifecycleSource, "public async Task InitializeAsync(");
        AssertContains(lifecycleSource, "public void Start()");
        AssertContains(lifecycleSource, "public async Task StopAsync()");
        AssertContains(lifecycleSource, "public async ValueTask DisposeAsync()");
        AssertContains(lifecycleSource, "public async ValueTask DisposeForPreviewReinitAsync()");
        AssertContains(lifecycleSource, "private async ValueTask DisposeCoreAsync(bool disposeSharedD3DDeviceManager)");
        AssertContains(lifecycleSource, "private void ThrowIfDisposed()");
        AssertContains(lifecycleSource, "private void OnCaptureFatalError(object? sender, Exception ex)");
        AssertContains(mjpegLifecycleSource, "private static bool ShouldUseExternalMjpegDecode(");
        AssertContains(mjpegLifecycleSource, "private ParallelMjpegDecodePipeline? CreateExternalMjpegPipelineIfNeeded(");
        AssertContains(mjpegLifecycleSource, "private void InstallMjpegPreviewJitterBuffer(double fps)");
        AssertContains(mjpegLifecycleSource, "private void StopAndDisposeMjpegPipeline(ParallelMjpegDecodePipeline mjpegPipelineToStop)");
        AssertContains(mjpegLifecycleSource, "private static void DisposeMjpegPipelineResources(");
        AssertContains(mjpegLifecycleSource, "private void OnMjpegPipelineFatalError(Exception ex)");
        AssertDoesNotContain(rootSource, "public async Task InitializeAsync(");
        AssertDoesNotContain(rootSource, "public async Task StopAsync()");
        AssertDoesNotContain(rootSource, "private async ValueTask DisposeCoreAsync(bool disposeSharedD3DDeviceManager)");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_FlashbackBackendOwnershipUsesResourceAggregate()
    {
        var captureSource = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource()
            + "\n" + ReadCaptureServiceRecordingFinalizationSource();
        var backendSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.PreviewDisposal.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.ArtifactCleanup.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.BufferCycle.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.Startup.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.Startup.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.RecordingFinalize.cs")
            .Replace("\r\n", "\n")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs")
            .Replace("\r\n", "\n");

        AssertContains(backendSource, "internal sealed partial class FlashbackBackendResources");
        AssertContains(backendSource, "public FlashbackBufferManager? BufferManager { get; set; }");
        AssertContains(backendSource, "public FlashbackEncoderSink? Sink { get; set; }");
        AssertContains(backendSource, "public FlashbackExporter? Exporter { get; set; }");
        AssertContains(backendSource, "public FlashbackPlaybackController? PlaybackController { get; set; }");
        AssertContains(backendSource, "public CaptureSettings? SettingsSnapshot { get; set; }");
        AssertContains(backendSource, "public bool HasAnyResource");
        AssertContains(backendSource, "public bool PreserveSegmentsAfterFailedRecordingFinalize { get; private set; }");
        AssertContains(backendSource, "public void Install(");
        AssertContains(backendSource, "public void ClearRecoveryPreserve()");
        AssertContains(backendSource, "public bool ResolveSegmentPurge(bool requested, string reason)");
        AssertContains(backendSource, "public void PreserveRecoverySegments(string reason)");
        AssertContains(backendSource, "internal readonly record struct FlashbackPreviewBackendStartRequest(");
        AssertContains(backendSource, "public async Task<FlashbackPlaybackController> StartPreviewBackendAsync(");
        AssertContains(backendSource, "var bufferManager = new FlashbackBufferManager(");
        AssertContains(backendSource, "flashbackSink.SetFatalErrorCallback(request.FatalErrorCallback);");
        AssertContains(backendSource, "flashbackSink.FrameEncoded += request.FrameEncodedHandler;");
        AssertContains(backendSource, "Install(");
        AssertContains(backendSource, "AttachProducers(");
        AssertContains(backendSource, "playbackController.Initialize(");
        AssertContains(backendSource, "private async Task RollBackPreviewBackendStartAsync(");
        AssertContains(backendSource, "request.ScheduleDeferredCleanup(");
        AssertContains(backendSource, "internal readonly record struct FlashbackBackendArtifactCleanupRequest(");
        AssertContains(backendSource, "public void ScheduleDeferredArtifactCleanup(");
        AssertContains(backendSource, "public async Task<bool> CleanupArtifactsAfterExportAsync(");
        AssertContains(backendSource, "Func<Task<bool>> acquireExportOperationLockAsync,");
        AssertContains(backendSource, "Action<string> releaseExportOperationLock,");
        AssertContains(backendSource, "public async Task<FinalizeResult> FinalizeRecordingAsync(");
        AssertContains(backendSource, "private static FinalizeResult PreserveEndArtifactsOnFailure(");
        AssertContains(backendSource, "public FlashbackPlaybackController? TakePlaybackController()");
        AssertContains(backendSource, "internal readonly record struct FlashbackProducerAttachRequest(");
        AssertContains(backendSource, "public void AttachProducers(FlashbackProducerAttachRequest request)");
        AssertContains(backendSource, "request.VideoCapture.SetFlashbackSink(flashbackSink);");
        AssertContains(backendSource, "private static void AttachAudioProducer(");
        AssertContains(backendSource, "FLASHBACK_AUDIO_ATTACH_SKIPPED reason='{reason}' sink_audio_enabled=false");
        AssertContains(backendSource, "private static void AttachMicrophoneProducer(");
        AssertContains(backendSource, "FLASHBACK_MIC_ATTACH_OK reason='{reason}'");
        AssertContains(backendSource, "internal readonly record struct FlashbackProducerDetachRequest(");
        AssertContains(backendSource, "UnifiedVideoCapture? VideoCapture,");
        AssertContains(backendSource, "WasapiAudioCapture? AudioCapture,");
        AssertContains(backendSource, "WasapiAudioCapture? MicrophoneCapture,");
        AssertContains(backendSource, "string WarningToken,");
        AssertContains(backendSource, "bool DetachMicrophoneWriter);");
        AssertContains(backendSource, "public void DetachProducers(FlashbackProducerDetachRequest request)");
        AssertContains(backendSource, "internal readonly record struct FlashbackBufferCycleRequest(");
        AssertContains(backendSource, "public async Task<FlashbackBufferCycleResult> CycleSinkOnlyAsync(");
        AssertContains(backendSource, "newSink.SetFatalErrorCallback(request.FatalErrorCallback);");
        AssertContains(backendSource, "newSink.FrameEncoded += request.FrameEncodedHandler;");
        AssertContains(backendSource, "SettingsSnapshot = request.SettingsSnapshot;");
        AssertContains(backendSource, "public void ClearSinkAndSettings()");
        AssertContains(backendSource, "public void Clear()");

        AssertContains(captureSource, "private readonly FlashbackBackendResources _flashbackBackend = new();");
        AssertDoesNotContain(captureSource, "_flashbackBufferManager");
        AssertDoesNotContain(captureSource, "_flashbackSink");
        AssertDoesNotContain(captureSource, "_flashbackExporter");
        AssertDoesNotContain(captureSource, "_flashbackPlaybackController");
        AssertDoesNotContain(captureSource, "_flashbackBackendSettings");
        AssertContains(captureSource, "_flashbackBackend.HasAnyResource");
        AssertContains(captureSource, "_flashbackBackend.StartPreviewBackendAsync(");
        AssertContains(backendSource, "Install(");
        AssertDoesNotContain(captureSource, "_flashbackBackend.Install(");
        AssertContains(captureSource, "_flashbackBackend.CycleSinkOnlyAsync(");
        AssertContains(backendSource, "TakePlaybackController()");
        AssertContains(backendSource, "AttachProducers(");
        AssertContains(backendSource, "new FlashbackProducerAttachRequest(");
        AssertContains(backendSource, "DetachProducers(");
        AssertContains(captureSource, "_flashbackBackend.ResolveSegmentPurge(");
        AssertContains(captureSource, "_flashbackBackend.PreserveRecoverySegments(");
        AssertContains(backendSource, "ClearRecoveryPreserve();");
        AssertContains(captureSource, "_flashbackBackend.FinalizeRecordingAsync(");
        AssertContains(backendSource, "ClearSinkAndSettings();");
        AssertContains(captureSource, "_flashbackBackend.DisposePreviewBackendAsync(request)");
        AssertContains(backendSource, "Clear();");
        AssertDoesNotContain(captureSource, "var bufferManager = new FlashbackBufferManager(");
        AssertDoesNotContain(captureSource, "FlashbackPlaybackController? playbackController = null;");

        return Task.CompletedTask;
    }

}
