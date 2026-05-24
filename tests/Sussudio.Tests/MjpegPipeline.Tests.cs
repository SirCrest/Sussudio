using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task ParallelMjpegDecodePipeline_SharedReorder_DoesNotSynthesizeRecordingSkips()
    {
        var source = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Workers.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.CompressedQueue.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Reorder.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.ReorderEmission.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Lifecycle.cs");
        AssertContains(source, "MJPEG_PIPELINE_STARTUP_DROP");
        AssertContains(source, "HasJpegStartOfImage");
        AssertContains(source, "MJPEG_REORDER_STRICT_WAIT");
        AssertContains(source, "SortedDictionary<long, DecodedFrame>");
        AssertContains(source, "DefaultDecodedReorderByteBudget");
        AssertContains(source, "TryAddDecodedFrame");
        AssertContains(source, "private void DecrementCompressedQueueDepth(string operation)");
        AssertContains(source, "MJPEG_PIPELINE_COMPRESSED_DEPTH_UNDERFLOW");
        AssertContains(source, "DecrementCompressedQueueDepth(\"write_failed\");");
        AssertContains(source, "DecrementCompressedQueueDepth(\"dequeue\");");
        AssertEqual(false, source.Contains("Interlocked.Decrement(ref _compressedQueueDepth)", StringComparison.Ordinal), "compressed queue depth decrements must be guarded");
        AssertContains(source, "private void SignalEmitter(string operation)");
        AssertContains(source, "MJPEG_PIPELINE_EMIT_SIGNAL_SKIPPED");
        AssertContains(source, "SignalEmitter(\"decoded_frame\");");
        AssertContains(source, "SignalEmitter(\"stop_requested\");");
        AssertEqual(1, source.Split("_emitSignal.Set();", StringSplitOptions.None).Length - 1, "All MJPEG emit wakeups go through SignalEmitter");
        AssertContains(source, "seqNo != _nextEmitSeq");
        AssertContains(source, "MarkKnownMissing");
        AssertContains(source, "MJPEG_PIPELINE_KNOWN_MISSING");
        AssertContains(source, "ConsumeKnownMissingFrames");
        AssertContains(source, "MJPEG_PIPELINE_KNOWN_MISSING_SKIP");
        AssertEqual(false, source.Contains("_reorderRing", StringComparison.Ordinal), "shared reorder must not use a fixed modulo ring");
        AssertEqual(false, source.Contains("_reorderFlags", StringComparison.Ordinal), "shared reorder must not use fixed slot flags");
        AssertEqual(false, source.Contains("reorder_collision", StringComparison.Ordinal), "slow decoded frames must not fatal via modulo slot collision");
        AssertEqual(false, source.Contains("SkipFrameCallback", StringComparison.Ordinal), "strict MJPEG path must not expose skip callbacks");
        AssertEqual(false, source.Contains("NotifySkippedFrame", StringComparison.Ordinal), "strict MJPEG path must not synthesize skip callbacks");
        AssertEqual(false, source.Contains("reorder_missing", StringComparison.Ordinal), "shared reorder skip reason removed");
        AssertContains(source, "skippedSeq = _nextEmitSeq++");
        var duplicateBlock = ExtractTextBetween(
            source,
            "if (_reorderFrames.ContainsKey(seqNo))",
            "_reorderFrames.Add(seqNo, new DecodedFrame(seqNo, frame, decodedTick));");
        AssertDoesNotContain(duplicateBlock, "MarkKnownMissing");

        return Task.CompletedTask;
    }

    internal static Task ParallelMjpegDecodePipeline_CompressedQueueLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs")
            .Replace("\r\n", "\n");
        var compressedQueueText = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.CompressedQueue.cs")
            .Replace("\r\n", "\n");

        AssertContains(compressedQueueText, "private const int WorkQueueItemCapacityPerDecoder = 8;");
        AssertContains(compressedQueueText, "private readonly Channel<MjpegWorkItem> _workQueue;");
        AssertContains(compressedQueueText, "private readonly FrameFingerprintCadenceTracker _packetHashTracker = new();");
        AssertContains(compressedQueueText, "private readonly long _compressedQueueByteBudget = DefaultCompressedQueueByteBudget;");
        AssertContains(compressedQueueText, "private readonly record struct MjpegWorkItem(");
        AssertContains(compressedQueueText, "public bool EnqueueFrame(ReadOnlySpan<byte> jpegData, int width, int height, long arrivalTick)");
        AssertContains(compressedQueueText, "private static bool HasJpegStartOfImage(ReadOnlySpan<byte> data)");
        AssertContains(compressedQueueText, "private void DecrementCompressedQueueDepth(string operation)");
        AssertContains(compressedQueueText, "FrameFingerprintCadenceTracker.ComputeHash(jpegData)");
        AssertContains(compressedQueueText, "MJPEG_PIPELINE_COMPRESSED_DEPTH_UNDERFLOW");
        AssertDoesNotContain(rootText, "public bool EnqueueFrame(");
        AssertDoesNotContain(rootText, "private static bool HasJpegStartOfImage(");
        AssertDoesNotContain(rootText, "FrameFingerprintCadenceTracker _packetHashTracker");
        AssertDoesNotContain(rootText, "private readonly record struct MjpegWorkItem(");

        return Task.CompletedTask;
    }

    internal static Task ParallelMjpegDecodePipeline_WorkersLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs")
            .Replace("\r\n", "\n");
        var workersText = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Workers.cs")
            .Replace("\r\n", "\n");

        AssertContains(workersText, "private readonly SoftwareMjpegDecoder[] _decoders;");
        AssertContains(workersText, "private readonly Thread[] _workers;");
        AssertContains(workersText, "private void StartDecodeWorkers(int width, int height)");
        AssertContains(workersText, "Name = $\"MjpegWorker-{i}\"");
        AssertContains(workersText, "private void WorkerLoop(int workerIndex)");
        AssertContains(workersText, "private bool HasAliveWorkers()");
        AssertContains(rootText, "StartDecodeWorkers(width, height);");
        AssertDoesNotContain(rootText, "private void WorkerLoop(int workerIndex)");
        AssertDoesNotContain(rootText, "private bool HasAliveWorkers()");
        AssertDoesNotContain(rootText, "private readonly SoftwareMjpegDecoder[] _decoders;");

        return Task.CompletedTask;
    }

    internal static Task ParallelMjpegDecodePipeline_ReorderLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs")
            .Replace("\r\n", "\n");
        var reorderText = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Reorder.cs")
            .Replace("\r\n", "\n");
        var reorderEmissionText = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.ReorderEmission.cs")
            .Replace("\r\n", "\n");

        AssertContains(reorderText, "private const long DefaultDecodedReorderByteBudget = 1024L * 1024 * 1024;");
        AssertContains(reorderText, "private readonly record struct DecodedFrame(");
        AssertContains(reorderText, "private readonly SortedDictionary<long, DecodedFrame> _reorderFrames = new();");
        AssertContains(reorderText, "private readonly SortedSet<long> _knownMissingSequences = new();");
        AssertContains(reorderText, "private readonly object _reorderLock = new();");
        AssertContains(reorderText, "private static int ResolveDecodedReorderCapacity(int width, int height)");
        AssertContains(reorderText, "private void DetectAndResetStall(bool emittedAny)");
        AssertContains(reorderText, "private void MarkKnownMissing(long seqNo, string reason)");
        AssertContains(reorderText, "private bool ConsumeKnownMissingFrames()");
        AssertContains(reorderText, "private bool TryAddDecodedFrame(long seqNo, PooledVideoFrame frame, long decodedTick)");
        AssertContains(reorderEmissionText, "private void EmitLoop()");
        AssertContains(reorderEmissionText, "private bool DrainReadyFrames()");
        AssertContains(reorderEmissionText, "private void NotifyPreviewFrameDecoded(PooledVideoFrame frame)");
        AssertContains(reorderEmissionText, "private void DrainRemainingFramesInOrder()");
        AssertContains(reorderEmissionText, "RecordTimingSample(_reorderLatencyMs");
        AssertContains(reorderEmissionText, "_emitCallback(frame.Frame);");
        AssertDoesNotContain(reorderText, "private void NotifyPreviewFrameDecoded(PooledVideoFrame frame)");
        AssertDoesNotContain(reorderText, "private void DrainRemainingFramesInOrder()");
        AssertDoesNotContain(rootText, "private void EmitLoop()");
        AssertDoesNotContain(rootText, "private bool DrainReadyFrames()");
        AssertDoesNotContain(rootText, "private bool TryAddDecodedFrame(long seqNo, PooledVideoFrame frame, long decodedTick)");
        AssertDoesNotContain(rootText, "private readonly record struct DecodedFrame(");

        return Task.CompletedTask;
    }

    internal static Task ParallelMjpegDecodePipeline_LifecycleLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Lifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(lifecycleText, "public void Dispose()");
        AssertContains(lifecycleText, "public bool TryStop(TimeSpan timeout, out string? failureReason)");
        AssertContains(lifecycleText, "private void BeginStop()");
        AssertContains(lifecycleText, "private Thread? _emitThread;");
        AssertContains(lifecycleText, "private readonly AutoResetEvent _emitSignal = new(false);");
        AssertContains(lifecycleText, "private void StartEmitter()");
        AssertContains(lifecycleText, "Name = \"MjpegEmitter\"");
        AssertContains(lifecycleText, "private void SignalEmitter(string operation)");
        AssertContains(lifecycleText, "private bool TryWaitForShutdown(TimeSpan timeout, out string? failureReason)");
        AssertContains(lifecycleText, "private void SignalFatalError(Exception ex)");
        AssertContains(lifecycleText, "private static TimeSpan GetRemainingTimeout(long deadlineTimestamp)");
        AssertContains(lifecycleText, "private void CleanupResources()");
        AssertContains(lifecycleText, "private void DiscardRemainingReorderFrames(string reason)");
        AssertContains(lifecycleText, "private void ReturnRemainingWorkItems()");
        AssertContains(lifecycleText, "ArrayPool<byte>.Shared.Return(item.JpegBuffer);");
        AssertContains(lifecycleText, "_emitSignal.Dispose();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Gpu", "ParallelMjpegDecodePipeline.ResourceCleanup.cs")),
            "MJPEG pipeline resource cleanup folded into ParallelMjpegDecodePipeline.Lifecycle.cs");
        AssertDoesNotContain(rootText, "public bool TryStop(TimeSpan timeout, out string? failureReason)");
        AssertDoesNotContain(rootText, "private void BeginStop()");
        AssertDoesNotContain(rootText, "private bool TryWaitForShutdown(TimeSpan timeout, out string? failureReason)");
        AssertDoesNotContain(rootText, "private void CleanupResources()");
        AssertDoesNotContain(rootText, "private static TimeSpan GetRemainingTimeout(long deadlineTimestamp)");
        AssertDoesNotContain(rootText, "Name = \"MjpegEmitter\"");

        return Task.CompletedTask;
    }

    internal static Task ParallelMjpegDecodePipeline_DropsStartupNonJpegBeforeSequencing()
    {
        var source = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.CompressedQueue.cs");
        var guardIndex = source.IndexOf("!HasJpegStartOfImage(jpegData)", StringComparison.Ordinal);
        var sequenceIndex = source.IndexOf("Interlocked.Increment(ref _nextDispatchSeq)", StringComparison.Ordinal);

        AssertEqual(true, guardIndex >= 0, "startup non-JPEG guard exists");
        AssertEqual(true, sequenceIndex >= 0, "MJPEG sequence assignment exists");
        AssertEqual(true, guardIndex < sequenceIndex, "startup non-JPEG guard must run before sequence assignment");
        AssertContains(source, "MJPEG_PIPELINE_STARTUP_DROP");
        AssertContains(source, "return false;");

        return Task.CompletedTask;
    }

    internal static Task ParallelMjpegDecodePipeline_KnownLossSkipsInsteadOfSignalingFatal()
    {
        var pipelineType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline");
        var pipeline = RuntimeHelpers.GetUninitializedObject(pipelineType);
        using var fatalSignaled = new ManualResetEventSlim(false);
        using var emitSignal = new AutoResetEvent(false);
        Exception? fatalException = null;

        SetPrivateField(pipeline, "_workQueue", CreateUnboundedChannelFieldValue(pipelineType, "_workQueue"));
        SetPrivateField(pipeline, "_emitSignal", emitSignal);
        SetPrivateField(pipeline, "_reorderLock", new object());
        SetPrivateField(pipeline, "_knownMissingSequences", new SortedSet<long>());
        SetPrivateField(pipeline, "_fatalErrorCallback", new Action<Exception>(ex =>
        {
            fatalException = ex;
            fatalSignaled.Set();
        }));
        SetPrivateField(pipeline, "_nextEmitSeq", 0L);

        InvokeNonPublicInstanceMethod(pipeline, "MarkKnownMissing", new object?[] { 0L, "compressed_queue_full" });
        AssertEqual(true, emitSignal.WaitOne(TimeSpan.FromSeconds(2)), "known MJPEG loss wakes emitter");

        var consumed = (bool)(InvokeNonPublicInstanceMethod(pipeline, "ConsumeKnownMissingFrames", Array.Empty<object?>())
            ?? throw new InvalidOperationException("ConsumeKnownMissingFrames returned null."));
        AssertEqual(true, consumed, "known MJPEG loss was consumed");
        AssertEqual(false, fatalSignaled.Wait(TimeSpan.FromMilliseconds(50)), "known MJPEG loss must not signal fatal");
        AssertEqual(null, fatalException, "known MJPEG loss fatal exception");
        AssertEqual(0, (int)(GetPrivateField(pipeline, "_stopRequested") ?? -1), "known loss keeps pipeline running");
        AssertEqual(0, (int)(GetPrivateField(pipeline, "_fatalErrorSignaled") ?? -1), "known loss does not signal fatal");
        AssertEqual(1L, (long)(GetPrivateField(pipeline, "_nextEmitSeq") ?? -1L), "known loss advances next emit sequence");
        AssertEqual(1L, (long)(GetPrivateField(pipeline, "_reorderSkips") ?? -1L), "known loss is counted as a reorder skip");

        return Task.CompletedTask;
    }
}
