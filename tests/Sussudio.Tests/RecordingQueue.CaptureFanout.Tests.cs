using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

// Tests for recording sink queue limits, drops, and latency accounting.
static partial class Program
{
    internal static Task UnifiedVideoCapture_SinkFanoutOwnsRecordingAndFlashbackFanout()
    {
        var rootSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs")
            .Replace("\r\n", "\n");
        var fanoutSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.FrameIngress.cs")
            .Replace("\r\n", "\n");

        AssertContains(fanoutSource, "private void EnqueueRecordingFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertContains(fanoutSource, "private void EnqueueRecordingFrame(PooledVideoFrame frame)");
        AssertContains(fanoutSource, "private void EnqueueGpuRecordingFrame(IGpuVideoFrameEncoder encoder, IntPtr texture, int subresource, long sourceSequence)");
        AssertContains(fanoutSource, "private void EnqueueFlashbackFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertContains(fanoutSource, "private void EnqueueFlashbackFrame(PooledVideoFrame frame)");
        AssertContains(fanoutSource, "private void EnqueueFlashbackGpuFrame(IntPtr texture, int subresource, long sourceSequence)");
        AssertContains(fanoutSource, "private void TrackFlashbackRecordingAcceptedSequence(long sourceSequence)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "UnifiedVideoCapture.SinkFanout.Flashback.cs")),
            "UnifiedVideoCapture Flashback fanout folded into the fanout owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "UnifiedVideoCapture.SinkFanout.cs")),
            "UnifiedVideoCapture recording/Flashback fanout folded into the frame ingress owner");
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

        AssertContains(frameIngressSource, "private void OnFrameArrived(ReadOnlySpan<byte> frameData, int width, int height, long arrivalTick)");
        AssertContains(frameIngressSource, "private void OnMjpegPipelineFrameEmitted(PooledVideoFrame frame)");
        AssertContains(frameIngressSource, "private void OnDualFrameArrived(");
        AssertContains(frameIngressSource, "private void RecordCaptureArrived(long sourceSequence, long arrivalTick, int width, int height, int compressedByteLength)");
        AssertContains(frameIngressSource, "private void FirePixelFormatObserverOnce(string format)");
        AssertContains(frameIngressSource, "private void SignalFatalError(Exception ex, string logMessage)");
        AssertContains(frameIngressSource, "private void OnMjpegPipelinePreviewFrameDecoded(PooledVideoFrameLease frame)");
        AssertContains(frameIngressSource, "private unsafe void SubmitPreviewRawFrame(");
        AssertContains(frameIngressSource, "private void TrackPreviewVisualFrame(");
        AssertContains(frameIngressSource, "private void MarkPreviewVisualCadenceUnavailable(string reason)");
        AssertContains(frameIngressSource, "private void EnqueueRecordingFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertContains(frameIngressSource, "private void EnqueueFlashbackFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertContains(frameIngressSource, "private void TrackFlashbackRecordingAcceptedSequence(long sourceSequence)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "UnifiedVideoCapture.Preview.cs")),
            "UnifiedVideoCapture preview submission folded into frame ingress owner");

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

    internal static Task UnifiedVideoCapture_LifecycleLivesWithRootState()
    {
        var rootSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs")
            .Replace("\r\n", "\n");
        var lifecycleSource = rootSource;
        var initializationSource = lifecycleSource;
        var mjpegStartupSource = lifecycleSource;
        var mjpegLifecycleSource = lifecycleSource;

        AssertContains(initializationSource, "public async Task InitializeAsync(");
        AssertContains(initializationSource, "var d3dManager = new SharedD3DDeviceManager();");
        AssertContains(initializationSource, "CreateExternalMjpegPipelineIfNeeded(");
        AssertContains(initializationSource, "InstallMjpegPreviewJitterBuffer(capture.Fps > 0 ? capture.Fps : fps);");
        AssertContains(initializationSource, "capture.FatalErrorOccurred += OnCaptureFatalError;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "UnifiedVideoCapture.Initialization.cs")),
            "UnifiedVideoCapture initialization folded into root source-session owner");
        AssertContains(lifecycleSource, "public void Start()");
        AssertContains(lifecycleSource, "public async Task StopAsync()");
        AssertContains(lifecycleSource, "public async ValueTask DisposeAsync()");
        AssertContains(lifecycleSource, "public async ValueTask DisposeForPreviewReinitAsync()");
        AssertContains(lifecycleSource, "private async ValueTask DisposeCoreAsync(bool disposeSharedD3DDeviceManager)");
        AssertContains(lifecycleSource, "private void ThrowIfDisposed()");
        AssertContains(lifecycleSource, "private void OnCaptureFatalError(object? sender, Exception ex)");
        AssertContains(mjpegStartupSource, "private static bool ShouldUseExternalMjpegDecode(");
        AssertContains(mjpegStartupSource, "private ParallelMjpegDecodePipeline? CreateExternalMjpegPipelineIfNeeded(");
        AssertContains(mjpegStartupSource, "private void InstallMjpegPreviewJitterBuffer(double fps)");
        AssertContains(mjpegLifecycleSource, "private void StopAndDisposeMjpegPipeline(ParallelMjpegDecodePipeline mjpegPipelineToStop)");
        AssertContains(mjpegLifecycleSource, "private static void DisposeMjpegPipelineResources(");
        AssertContains(mjpegLifecycleSource, "private void OnMjpegPipelineFatalError(Exception ex)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "UnifiedVideoCapture.MjpegPipelineLifecycle.cs")),
            "UnifiedVideoCapture MJPEG startup helpers folded into root source-session owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "UnifiedVideoCapture.Lifecycle.cs")),
            "UnifiedVideoCapture lifecycle folded into root source-session owner");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_FlashbackBackendOwnershipUsesResourceAggregate()
    {
        var captureSource = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource()
            + "\n" + ReadCaptureServiceRecordingFinalizationSource();
        var backendSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs")
            .Replace("\r\n", "\n");

        AssertContains(backendSource, "internal sealed class FlashbackBackendResources");
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

    internal static Task UnifiedVideoCapture_CpuMjpegEmitReportsNv12()
    {
        var unifiedVideoCapture = CreateInstance("Sussudio.Services.Capture.UnifiedVideoCapture");
        var observed = string.Empty;

        var setObserver = unifiedVideoCapture.GetType().GetMethod("SetPixelFormatDetectedCallback", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("SetPixelFormatDetectedCallback method not found.");
        setObserver.Invoke(unifiedVideoCapture, new object?[] { new Action<string>(value => observed = value) });

        var emitMethod = unifiedVideoCapture.GetType().GetMethod("OnMjpegPipelineFrameEmitted", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("OnMjpegPipelineFrameEmitted method not found.");
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var rentMethod = frameType.GetMethod("Rent", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PooledVideoFrame.Rent method not found.");
        var frame = rentMethod.Invoke(
            null,
            new object[]
            {
                0L,
                0L,
                0L,
                2,
                2,
                Enum.Parse(formatType, "Nv12"),
                6
            })
            ?? throw new InvalidOperationException("PooledVideoFrame.Rent returned null.");
        try
        {
            emitMethod.Invoke(unifiedVideoCapture, new[] { frame });
        }
        finally
        {
            ((IDisposable)frame).Dispose();
        }

        AssertEqual("NV12", observed, "UnifiedVideoCapture.OnMjpegPipelineFrameEmitted observer format");
        return Task.CompletedTask;
    }

    internal static async Task UnifiedVideoCapture_RetainsMjpegPipeline_WhenStopFails()
    {
        var unifiedVideoCapture = CreateInstance("Sussudio.Services.Capture.UnifiedVideoCapture");
        var pipelineType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline");
        var pipeline = CreateUninitializedObject(pipelineType);
        SeedPipelineStopFailureState(pipeline, pipelineType);

        SetPrivateField(unifiedVideoCapture, "_mjpegPipeline", pipeline);
        SetPrivateField(pipeline, "_emitThread", Thread.CurrentThread);

        try
        {
            var stopAsync = unifiedVideoCapture.GetType().GetMethod("StopAsync", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("UnifiedVideoCapture.StopAsync method not found.");
            if (stopAsync.Invoke(unifiedVideoCapture, null) is not Task stopTask)
            {
                throw new InvalidOperationException("UnifiedVideoCapture.StopAsync did not return a Task.");
            }

            try
            {
                await stopTask.ConfigureAwait(false);
                throw new InvalidOperationException("UnifiedVideoCapture.StopAsync unexpectedly succeeded.");
            }
            catch (InvalidOperationException ex)
            {
                AssertContains(ex.Message, "emitter_self_join");
            }

            var retainedPipeline = GetPrivateField(unifiedVideoCapture, "_mjpegPipeline");
            AssertEqual(pipeline, retainedPipeline, "UnifiedVideoCapture._mjpegPipeline retained on stop failure");
        }
        finally
        {
            SetPrivateField(pipeline, "_emitThread", null);
            SetPrivateField(unifiedVideoCapture, "_mjpegPipeline", null);

            var disposeMethod = pipelineType.GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("ParallelMjpegDecodePipeline.Dispose method not found.");
            disposeMethod.Invoke(pipeline, null);

            await DisposeValueTaskAsync(unifiedVideoCapture).ConfigureAwait(false);
        }
    }
}

static partial class Program
{
    private static readonly string[] CaptureServiceFlashbackOrchestrationFiles =
    {
        "Sussudio/Services/Capture/CaptureService.FlashbackControls.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs"
    };

    private static readonly string[] CaptureServiceRecordingFinalizationFiles =
    {
        "Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs"
    };

    private static readonly string[] CaptureServicePreviewLifecycleFiles =
    {
        "Sussudio/Services/Capture/CaptureService.PreviewStart.cs",
        "Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs",
        "Sussudio/Services/Capture/CapturePipelineResources.cs"
    };

    private static readonly string[] CaptureServiceRecordingIntegrityFiles =
    {
        "Sussudio/Services/Capture/CaptureService.RecordingIntegrity.cs"
    };

    private static string ReadCaptureServiceFlashbackOrchestrationSource()
        => string.Join(
            "\n",
            CaptureServiceFlashbackOrchestrationFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceRecordingFinalizationSource()
        => string.Join(
            "\n",
            CaptureServiceRecordingFinalizationFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServicePreviewLifecycleSource()
        => string.Join(
            "\n",
            CaptureServicePreviewLifecycleFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceRecordingIntegritySource()
        => string.Join(
            "\n",
            CaptureServiceRecordingIntegrityFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceFlashbackOrchestrationCodeWithoutCommentsOrStrings()
        => string.Join(
            "\n",
            CaptureServiceFlashbackOrchestrationFiles.Select(ReadRepoCodeWithoutCommentsOrStrings));

    private static string ReadCaptureServicePreviewLifecycleCodeWithoutCommentsOrStrings()
        => string.Join(
            "\n",
            CaptureServicePreviewLifecycleFiles.Select(ReadRepoCodeWithoutCommentsOrStrings));

    internal static Task CaptureService_FlashbackOrchestrationLivesInFocusedPartials()
    {
        var flashbackStateText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackControls.cs");
        var flashbackRecordingText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs");
        var previewBackendText = flashbackStateText;
        var settingsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackControls.cs");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");
        var backendResourcesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs");

        AssertContains(flashbackStateText, "public bool IsFlashbackActive => _flashbackBackend.Sink != null;");
        AssertContains(flashbackStateText, "internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()");
        AssertContains(flashbackStateText, "public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(flashbackStateText, "FLASHBACK_ENABLE_DEFERRED");
        AssertContains(flashbackStateText, "public Task RestartFlashbackAsync(");
        AssertContains(flashbackStateText, "private async Task RestartFlashbackCoreAsync(");
        AssertContains(flashbackStateText, "UpdateEncodingSettings(settings);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackAudioInputs.cs")),
            "Flashback audio input restoration folded into Flashback recording owner");
        AssertContains(flashbackRecordingText, "private async Task EnsureFlashbackAudioInputsAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackPreviewBackend.cs")),
            "Flashback preview backend lifecycle folded into Flashback controls owner");
        AssertContains(previewBackendText, "private async Task EnsureFlashbackPreviewBackendAsync(");
        AssertContains(previewBackendText, "private async Task DisposeFlashbackPreviewBackendAsync(");
        AssertContains(previewBackendText, "private async Task DisposeFlashbackPreviewBackendCoreAsync(");
        AssertContains(previewBackendText, "CreateFlashbackPreviewBackendDisposalRequest(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackPreviewBackendDisposal.cs")),
            "old Flashback preview backend disposal partial removed");
        AssertContains(backendResourcesText, "internal readonly record struct FlashbackPreviewBackendDisposalRequest(");
        AssertContains(backendResourcesText, "public async Task DisposePreviewBackendAsync(");
        AssertContains(settingsText, "private async Task CycleFlashbackBufferAsync(");
        AssertContains(settingsText, "_flashbackBackend.CycleSinkOnlyAsync(");
        AssertContains(settingsText, "public Task UpdateFlashbackSettingsAsync(");
        AssertContains(settingsText, "_currentSettings.FlashbackBufferMinutes = bufferMinutes;");
        AssertContains(settingsText, "_flashbackBackend.PlaybackController.GpuDecodeEnabled = gpuDecode;");
        AssertContains(settingsText, "public Task UpdateRecordingFormatAsync(");
        AssertContains(settingsText, "var previousSettings = CloneCaptureSettings(_currentSettings);");
        AssertContains(settingsText, "FLASHBACK_FORMAT_CHANGE_ROLLBACK");
        AssertContains(settingsText, "private void UpdateEncodingSettings(CaptureSettings source)");
        AssertContains(settingsText, "public Task CycleFlashbackEncoderSettingsAsync(");
        AssertContains(settingsText, "FLASHBACK_ENCODER_SETTINGS_CHANGE_ROLLBACK");
        AssertContains(agentMapText, "CaptureService.FlashbackControls.cs");
        AssertDoesNotContain(agentMapText, "CaptureService.FlashbackEnable.cs");
        AssertDoesNotContain(agentMapText, "CaptureService.FlashbackRestart.cs");
        AssertDoesNotContain(agentMapText, "CaptureService.FlashbackState.cs");
        AssertDoesNotContain(agentMapText, "CaptureService.FlashbackSettings.cs");
        AssertDoesNotContain(agentMapText, "CaptureService.FlashbackSettingsControls.cs");
        AssertContains(cleanupPlanText, "CaptureService.FlashbackControls.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureService.FlashbackEnable.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureService.FlashbackRestart.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureService.FlashbackState.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureService.FlashbackSettings.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureService.FlashbackSettingsControls.cs");
        AssertContains(agentMapText, "FlashbackBackendResources.cs");
        AssertContains(cleanupPlanText, "FlashbackBackendResources.cs");
        AssertDoesNotContain(agentMapText, "FlashbackBackendResources.BufferCycle.cs");
        AssertDoesNotContain(cleanupPlanText, "FlashbackBackendResources.BufferCycle.cs");
        AssertDoesNotContain(agentMapText, "FlashbackBackendResources.Startup.cs");
        AssertDoesNotContain(cleanupPlanText, "FlashbackBackendResources.Startup.cs");
        AssertDoesNotContain(agentMapText, "FlashbackBackendResources.Teardown.cs");
        AssertDoesNotContain(cleanupPlanText, "FlashbackBackendResources.Teardown.cs");
        AssertContains(agentMapText, "rollback cleanup");
        AssertContains(cleanupPlanText, "startup failure rollback cleanup");
        AssertDoesNotContain(agentMapText, "FlashbackBackendResources.RecordingFinalize.cs");
        AssertDoesNotContain(cleanupPlanText, "FlashbackBackendResources.RecordingFinalize.cs");
        AssertContains(agentMapText, "attach/detach request");
        AssertContains(cleanupPlanText, "attach/detach request");
        AssertContains(backendResourcesText, "private FlashbackBufferCyclePlaybackState DisposePlaybackForBufferCycle(");
        AssertContains(backendResourcesText, "private static async Task StopAndDisposeOldSinkForBufferCycleAsync(");
        AssertContains(backendResourcesText, "private async Task<bool> TryStartReplacementSinkForBufferCycleAsync(");
        AssertContains(backendResourcesText, "private static async Task CleanupFailedReplacementSinkForBufferCycleAsync(");
        AssertContains(backendResourcesText, "public async Task<FlashbackBufferCycleResult> CycleSinkOnlyAsync(");
        AssertContains(backendResourcesText, "public async Task<FlashbackPlaybackController> StartPreviewBackendAsync(");
        AssertContains(backendResourcesText, "private async Task RollBackPreviewBackendStartAsync(");
        AssertContains(backendResourcesText, "FLASHBACK_PREVIEW_ROLLBACK_DETACH_WARN");
        AssertContains(backendResourcesText, "preview_init_rollback");
        AssertContains(backendResourcesText, "public async Task<FinalizeResult> FinalizeRecordingAsync(");
        AssertContains(backendResourcesText, "private static FinalizeResult PreserveEndArtifactsOnFailure(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackBackendResources.RecordingFinalize.cs")),
            "recording finalize policy folded into FlashbackBackendResources.cs");
        AssertContains(backendResourcesText, "public void AttachProducers(FlashbackProducerAttachRequest request)");
        AssertContains(backendResourcesText, "public void DetachProducers(FlashbackProducerDetachRequest request)");
        AssertDoesNotContain(flashbackStateText, "private async Task EnsureFlashbackAudioInputsAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackState.cs")),
            "Flashback state owner folded into CaptureService.FlashbackControls.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackSettings.cs")),
            "Flashback settings owner folded into CaptureService.FlashbackControls.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackSettingsControls.cs")),
            "old broad Flashback settings controls file removed");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RecordingFinalizationLivesInFocusedPartials()
    {
        var stopLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs");
        var flashbackBackendFinalizationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs");
        var libAvBackendFinalizationText =
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs");
        var recordingLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs");

        AssertContains(stopLifecycleText, "private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync(");
        AssertContains(stopLifecycleText, "StopAndDisposeFlashbackRecordingBackendAsync(cancellationToken)");
        AssertContains(stopLifecycleText, "StopAndDisposeLibAvRecordingBackendAsync(fallbackStatusMessage, emergency, cancellationToken)");
        AssertContains(flashbackBackendFinalizationText, "private async Task<FinalizeResult> StopAndDisposeFlashbackRecordingBackendAsync(");
        AssertContains(flashbackBackendFinalizationText, "FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL");
        AssertContains(flashbackBackendFinalizationText, "ReconcileFlashbackBackendAfterRecordingFinalizeAsync(");
        AssertContains(flashbackBackendFinalizationText, "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);");
        AssertContains(flashbackBackendFinalizationText, "private async Task<OperationCanceledException?> ReconcileFlashbackBackendAfterRecordingFinalizeAsync(");
        AssertContains(flashbackBackendFinalizationText, "private async Task<FinalizeResult> FinalizeFlashbackRecordingAsync(");
        AssertContains(flashbackBackendFinalizationText, "private static bool IsFlashbackFinalizeCancellationResult(FinalizeResult result)");
        AssertContains(flashbackBackendFinalizationText, "private sealed class FlashbackRecordingBoundarySnapshot");
        AssertContains(flashbackBackendFinalizationText, "private void CaptureFlashbackRecordingBoundarySnapshot(");
        AssertContains(flashbackBackendFinalizationText, "if (recordingBoundary.Captured)");
        AssertContains(flashbackBackendFinalizationText, "flashbackVideoCapture.EndFlashbackRecordingAccounting();");
        AssertContains(flashbackBackendFinalizationText, "recordingBoundary.Counters = CaptureFlashbackRecordingIntegrityCountersSinceBaseline");
        AssertContains(flashbackBackendFinalizationText, "recordingBoundary.AudioCounters = GetRecordingAudioCountersSinceBaseline(");
        AssertContains(flashbackBackendFinalizationText, "recordingBoundary.Captured = true;");
        AssertContains(flashbackBackendFinalizationText, "_flashbackBackend.PreserveRecoverySegments(\"recording_finalize_failed\");");
        AssertContains(flashbackBackendFinalizationText, "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING_DEFERRED");
        AssertContains(flashbackBackendFinalizationText, "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING");
        AssertContains(flashbackBackendFinalizationText, "await CycleFlashbackBufferAsync(cancellationToken)");
        AssertContains(flashbackBackendFinalizationText, "FLASHBACK_BUFFER_CYCLE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(flashbackBackendFinalizationText, "BeginFlashbackBackendCleanup(ex);");
        AssertOccursBefore(flashbackBackendFinalizationText, "LogRecordingIntegritySummary(_lastRecordingIntegrity);", "ReconcileFlashbackBackendAfterRecordingFinalizeAsync(");
        AssertOccursBefore(flashbackBackendFinalizationText, "ReconcileFlashbackBackendAfterRecordingFinalizeAsync(", "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeFlashbackBackendReconcile.cs")),
            "old Flashback backend reconcile partial removed");
        AssertContains(libAvBackendFinalizationText, "private async Task<FinalizeResult> StopAndDisposeLibAvRecordingBackendAsync(");
        AssertContains(libAvBackendFinalizationText, "StopUnifiedVideoRecordingForLibAvFinalizeAsync(");
        AssertContains(libAvBackendFinalizationText, "DetachLibAvRecordingAudioBeforeSinkStopAsync(");
        AssertContains(libAvBackendFinalizationText, "StopAndDisposeLibAvSinkForFinalizeAsync(");
        AssertContains(libAvBackendFinalizationText, "DisposeIdleLibAvPreviewResourcesAfterRecordingAsync(");
        AssertContains(libAvBackendFinalizationText, "FoldLibAvAudioFaultIntoFinalizeResult(");
        AssertContains(libAvBackendFinalizationText, "PublishLibAvRecordingIntegrity(");
        AssertContains(libAvBackendFinalizationText, "CompleteLibAvRecordingFinalizeStateAsync(");
        AssertContains(libAvBackendFinalizationText, "var sinkResult = libAvSink != null");
        AssertContains(libAvBackendFinalizationText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(libAvBackendFinalizationText, "reason: \"recording_stop_deferred_drain\"");
        AssertContains(libAvBackendFinalizationText, "_previewAudioGraph.DetachCapture(");
        AssertContains(libAvBackendFinalizationText, "Recording WASAPI capture dispose failed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvResources.cs")),
            "old broad LibAv resource finalization partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvVideoBoundary.cs")),
            "old LibAv video-boundary finalization partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvSink.cs")),
            "old LibAv sink finalization partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvIdlePreview.cs")),
            "old LibAv idle-preview finalization partial removed");
        AssertContains(libAvBackendFinalizationText, "RestoreLibAvPreviewFeaturesAfterRecordingAsync(");
        AssertContains(libAvBackendFinalizationText, "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        AssertContains(libAvBackendFinalizationText, "private async Task<OperationCanceledException?> RestoreLibAvPreviewFeaturesAfterRecordingAsync(");
        AssertContains(libAvBackendFinalizationText, "private async Task<OperationCanceledException?> RestorePendingFlashbackEnableAfterLibAvRecordingAsync(");
        AssertContains(libAvBackendFinalizationText, "private async Task<OperationCanceledException?> RestartStandardMicrophoneMonitorAfterLibAvRecordingAsync(");
        AssertContains(libAvBackendFinalizationText, "if (!_pendingFlashbackEnableAfterRecording)");
        AssertContains(libAvBackendFinalizationText, "await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, cancellationToken)");
        AssertContains(libAvBackendFinalizationText, "FLASHBACK_ENABLE_AFTER_RECORDING_CANCELLED");
        AssertContains(libAvBackendFinalizationText, "FLASHBACK_ENABLE_AFTER_RECORDING_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(libAvBackendFinalizationText, "OnlyWhenMissing: false,");
        AssertContains(libAvBackendFinalizationText, "FlashbackAttachReason: \"mic_monitor_restart\",");
        AssertContains(libAvBackendFinalizationText, "RestartLogEvent: \"MIC_MONITOR_RESTART\",");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvPreviewRestore.cs")),
            "old LibAv preview-restore finalization partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeFlashback.cs")),
            "Flashback export-finalize helpers folded into CaptureService.FlashbackRecording.cs");
        AssertContains(recordingLifecycleText, "private void PublishRecordingStartedOutcome(string finalOutputPath)");
        AssertContains(recordingLifecycleText, "private void PublishRecordingFinalizedOutcome(FinalizeResult result, bool updateOutputPath)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingOutcomeState.cs")),
            "old recording outcome-state partial removed");
        AssertDoesNotContain(stopLifecycleText, "private sealed class FlashbackRecordingBoundarySnapshot");
        AssertDoesNotContain(stopLifecycleText, "private void CaptureFlashbackRecordingBoundarySnapshot(");
        AssertDoesNotContain(stopLifecycleText, "FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL");

        return Task.CompletedTask;
    }
}
