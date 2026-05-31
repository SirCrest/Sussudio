using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests
{
    public sealed class MjpegPipelineContractsTests
    {
        public MjpegPipelineContractsTests()
        {
            global::Program.EnsureTargetAssemblyLoadedForXUnit();
        }

        [Fact]
        public Task UnifiedVideoCaptureCpuMjpegEmitReportsNv12()
            => global::Program.UnifiedVideoCapture_CpuMjpegEmitReportsNv12();

        [Fact]
        public Task UnifiedVideoCaptureRetainsMjpegPipelineWhenStopFails()
            => global::Program.UnifiedVideoCapture_RetainsMjpegPipeline_WhenStopFails();

        [Fact]
        public Task ParallelMjpegDecodePipelineLifecycleLivesWithRoot()
            => global::Program.ParallelMjpegDecodePipeline_LifecycleLivesWithRoot();

        [Fact]
        public Task ParallelMjpegDecodePipelineCompressedQueueLivesWithRoot()
            => global::Program.ParallelMjpegDecodePipeline_CompressedQueueLivesWithRoot();

        [Fact]
        public Task ParallelMjpegDecodePipelineWorkersLiveWithRoot()
            => global::Program.ParallelMjpegDecodePipeline_WorkersLiveWithRoot();

        [Fact]
        public Task ParallelMjpegDecodePipelineReorderLivesWithRoot()
            => global::Program.ParallelMjpegDecodePipeline_ReorderLivesWithRoot();

        [Fact]
        public Task PooledVideoFrameLeaseLifecycleReturnsBufferAfterLastRelease()
            => global::Program.PooledVideoFrame_LeaseLifecycle_ReturnsBufferAfterLastRelease();

        [Fact]
        public Task PooledVideoFrameAddLeaseAfterReturnThrows()
            => global::Program.PooledVideoFrame_AddLeaseAfterReturn_Throws();

        [Fact]
        public Task PooledVideoFrameOwnerDisposeClosesNewLeasesButExistingLeaseRemainsReadable()
            => global::Program.PooledVideoFrame_OwnerDisposeClosesNewLeasesButExistingLeaseRemainsReadable();

        [Fact]
        public Task MjpegPooledFrameFanoutExposesLeaseContracts()
            => global::Program.MjpegPooledFrameFanout_ExposesLeaseContracts();

        [Fact]
        public Task ParallelMjpegDecodePipelineSharedReorderDoesNotSynthesizeRecordingSkips()
            => global::Program.ParallelMjpegDecodePipeline_SharedReorder_DoesNotSynthesizeRecordingSkips();

        [Fact]
        public Task ParallelMjpegDecodePipelineDropsStartupNonJpegBeforeSequencing()
            => global::Program.ParallelMjpegDecodePipeline_DropsStartupNonJpegBeforeSequencing();

        [Fact]
        public Task ParallelMjpegDecodePipelineKnownLossSkipsInsteadOfSignalingFatal()
            => global::Program.ParallelMjpegDecodePipeline_KnownLossSkipsInsteadOfSignalingFatal();

        [Fact]
        public Task FrameFingerprintCadenceTrackerCurrentDuplicateRunLowersUniqueFps()
            => global::Program.FrameFingerprintCadenceTracker_CurrentDuplicateRunLowersUniqueFps();

        [Fact]
        public Task VisualCadenceTrackerUsesExactCropPixelsWithOnePassDiff()
            => global::Program.VisualCadenceTracker_UsesExactCropPixelsWithOnePassDiff();

        [Fact]
        public Task MjpegLeasedVideoPacketsReleaseQueuedLeases()
            => global::Program.MjpegLeasedVideoPackets_ReleaseQueuedLeases();

        [Fact]
        public Task MjpegPreviewJitterExposesAdaptiveDeadlinePolicy()
            => global::Program.MjpegPreviewJitter_ExposesAdaptiveDeadlinePolicy();

        [Fact]
        public Task MjpegPreviewJitterEmitLoopLivesWithLifecycleRoot()
            => global::Program.MjpegPreviewJitter_EmitLoopLivesWithLifecycleRoot();

        [Fact]
        public Task MjpegPreviewJitterDropsSoftDeadlineOverflowToRecoverLatency()
            => global::Program.MjpegPreviewJitter_DropsSoftDeadlineOverflowToRecoverLatency();

        [Fact]
        public Task MjpegPreviewJitterDropsExpiredFramesBelowTargetDepth()
            => global::Program.MjpegPreviewJitter_DropsExpiredFramesBelowTargetDepth();

        [Fact]
        public Task MjpegPreviewJitterSkipsMissingPreviewSequenceAfterDeadline()
            => global::Program.MjpegPreviewJitter_SkipsMissingPreviewSequenceAfterDeadline();

        [Fact]
        public Task MjpegPreviewJitterLateSequenceDoesNotCountAsQueued()
            => global::Program.MjpegPreviewJitter_LateSequenceDoesNotCountAsQueued();

        [Fact]
        public Task MjpegPreviewJitterClearResetsPreviewSequence()
            => global::Program.MjpegPreviewJitter_ClearResetsPreviewSequence();

        [Fact]
        public Task MjpegPreviewJitterReprimesAfterSuppressionResume()
            => global::Program.MjpegPreviewJitter_ReprimesAfterSuppressionResume();

        [Fact]
        public Task D3DPreviewPendingFrameReleasesQueuedLease()
            => global::Program.D3DPreviewPendingFrame_ReleasesQueuedLease();

        [Fact]
        public void ParallelMjpegDecodePipelineComputeTimingMetricsCalculatesCorrectly()
        {
            var method = RequirePipelineMethod("ComputeTimingMetrics");
            var samples = new double[] { 10.0, 10.0, 10.0, 10.0, 10.0 };
            var result = method.Invoke(null, new object[] { samples });

            var resultType = result!.GetType();
            var countField = resultType.GetField("Item1")!;
            var avgField = resultType.GetField("Item2")!;
            var p95Field = resultType.GetField("Item3")!;
            var maxField = resultType.GetField("Item4")!;

            Assert.Equal(5, Convert.ToInt32(countField.GetValue(result)));
            var avg = Convert.ToDouble(avgField.GetValue(result));
            Assert.True(Math.Abs(avg - 10.0) < 0.001, $"Average should be 10.0, got {avg}");
            var p95 = Convert.ToDouble(p95Field.GetValue(result));
            Assert.True(Math.Abs(p95 - 10.0) < 0.001, $"P95 should be 10.0, got {p95}");
            var max = Convert.ToDouble(maxField.GetValue(result));
            Assert.True(Math.Abs(max - 10.0) < 0.001, $"Max should be 10.0, got {max}");
        }

        [Fact]
        public void ParallelMjpegDecodePipelineComputeTimingMetricsP95Calculation()
        {
            var method = RequirePipelineMethod("ComputeTimingMetrics");

            var samples = new double[20];
            for (var i = 0; i < 19; i++)
            {
                samples[i] = 5.0;
            }

            samples[19] = 50.0;

            var result = method.Invoke(null, new object[] { samples });
            var resultType = result!.GetType();

            var maxField = resultType.GetField("Item4")!;
            var max = Convert.ToDouble(maxField.GetValue(result));
            Assert.True(max >= 50.0, $"Max should be >= 50.0, got {max}");

            var p95Field = resultType.GetField("Item3")!;
            var p95 = Convert.ToDouble(p95Field.GetValue(result));
            Assert.True(p95 >= 5.0, $"P95 should be >= 5.0, got {p95}");
        }

        [Fact]
        public void ParallelMjpegDecodePipelineGetElapsedMillisecondsComputesCorrectly()
        {
            var method = RequirePipelineMethod("GetElapsedMilliseconds");

            long start = 0;
            long end = Stopwatch.Frequency;
            var result = (double)method.Invoke(null, new object[] { start, end })!;

            Assert.True(Math.Abs(result - 1000.0) < 0.1,
                $"1 second of ticks should be ~1000ms, got {result:F3}");

            long halfEnd = Stopwatch.Frequency / 2;
            var halfResult = (double)method.Invoke(null, new object[] { start, halfEnd })!;
            Assert.True(Math.Abs(halfResult - 500.0) < 0.1,
                $"Half second should be ~500ms, got {halfResult:F3}");
        }

        [Fact]
        public void ParallelMjpegDecodePipelineGetRemainingTimeoutReturnsCorrectTimeSpan()
        {
            var method = RequirePipelineMethod("GetRemainingTimeout");

            long futureDeadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 60;
            var result = (TimeSpan)method.Invoke(null, new object[] { futureDeadline })!;
            Assert.True(result.TotalSeconds > 30 && result.TotalSeconds <= 60,
                $"Remaining timeout for 60s future deadline should be bounded, got {result.TotalSeconds:F1}s");

            long pastDeadline = Stopwatch.GetTimestamp() - Stopwatch.Frequency;
            var pastResult = (TimeSpan)method.Invoke(null, new object[] { pastDeadline })!;
            Assert.True(pastResult.TotalMilliseconds <= 0,
                $"Past deadline should return <=0ms, got {pastResult.TotalMilliseconds:F1}");
        }

        [Fact]
        public void ParallelMjpegDecodePipelineTimingMetricsHasExpectedProperties()
        {
            var metricsType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline+PipelineTimingMetrics");

            var expectedProps = new[]
            {
                "DecoderCount", "DecodeSampleCount", "DecodeAvgMs", "DecodeP95Ms", "DecodeMaxMs",
                "ReorderSampleCount", "ReorderAvgMs", "ReorderP95Ms", "ReorderMaxMs",
                "PipelineSampleCount", "PipelineAvgMs", "PipelineP95Ms", "PipelineMaxMs",
                "TotalDecoded", "TotalEmitted", "TotalDropped", "ReorderSkips",
                "ReorderBufferDepth", "PerDecoder"
            };

            foreach (var prop in expectedProps)
            {
                var propInfo = metricsType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
                Assert.NotNull(propInfo);
            }
        }

        [Fact]
        public void SoftwareMjpegDecoderPropertiesExposeCorrectDimensions()
        {
            var rootText = ReadRepoFile("Sussudio/Services/Gpu/SoftwareMjpegDecoder.cs");
            var decoderType = RequireType("Sussudio.Services.Gpu.SoftwareMjpegDecoder");

            AssertContains(rootText, "internal sealed unsafe class SoftwareMjpegDecoder : IDisposable");
            AssertContains(rootText, "public void Initialize(int width, int height)");
            AssertContains(rootText, "public void Dispose()");
            AssertContains(rootText, "public bool DecodeToNv12(ReadOnlySpan<byte> jpegData, Span<byte> nv12Destination)");
            AssertContains(rootText, "SW_MJPEG_DECODE_DIAG");
            AssertContains(rootText, "Buffer.MemoryCopy(");
            Assert.False(
                File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "Sussudio", "Services", "Gpu", "SoftwareMjpegDecoder.Decode.cs")),
                "Software MJPEG decode path folded into decoder state/lifetime owner");

            var widthProp = decoderType.GetProperty("Width", BindingFlags.Public | BindingFlags.Instance);
            var heightProp = decoderType.GetProperty("Height", BindingFlags.Public | BindingFlags.Instance);
            var nv12SizeProp = decoderType.GetProperty("Nv12Size", BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(widthProp);
            Assert.NotNull(heightProp);
            Assert.NotNull(nv12SizeProp);
        }

        private static MethodInfo RequirePipelineMethod(string methodName)
        {
            var pipelineType = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline");
            return pipelineType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"{methodName} not found.");
        }

        private static Type RequireType(string typeName)
            => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

        private static string ReadRepoFile(string relativePath)
            => RuntimeContractSource.ReadRepoFile(relativePath).Replace("\r\n", "\n");

        private static void AssertContains(string actual, string expectedSubstring)
            => Assert.Contains(expectedSubstring, actual, StringComparison.Ordinal);
    }
}

static partial class Program
{
    internal static Task ParallelMjpegDecodePipeline_SharedReorder_DoesNotSynthesizeRecordingSkips()
    {
        var source = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs");
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

    internal static Task ParallelMjpegDecodePipeline_CompressedQueueLivesWithRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private const int WorkQueueItemCapacityPerDecoder = 8;");
        AssertContains(rootText, "private readonly Channel<MjpegWorkItem> _workQueue;");
        AssertContains(rootText, "private readonly FrameFingerprintCadenceTracker _packetHashTracker = new();");
        AssertContains(rootText, "private readonly long _compressedQueueByteBudget = DefaultCompressedQueueByteBudget;");
        AssertContains(rootText, "private readonly record struct MjpegWorkItem(");
        AssertContains(rootText, "public bool EnqueueFrame(ReadOnlySpan<byte> jpegData, int width, int height, long arrivalTick)");
        AssertContains(rootText, "private static bool HasJpegStartOfImage(ReadOnlySpan<byte> data)");
        AssertContains(rootText, "private void DecrementCompressedQueueDepth(string operation)");
        AssertContains(rootText, "FrameFingerprintCadenceTracker.ComputeHash(jpegData)");
        AssertContains(rootText, "public PipelineTimingMetrics GetTimingMetrics()");
        AssertContains(rootText, "public FrameFingerprintCadenceTracker.Metrics GetPacketHashMetrics()");
        AssertContains(rootText, "private void RecordPerDecoderTiming(int workerIndex, double valueMs)");
        AssertContains(rootText, "MJPEG_PIPELINE_COMPRESSED_DEPTH_UNDERFLOW");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Gpu", "ParallelMjpegDecodePipeline.CompressedQueue.cs")),
            "MJPEG compressed queue admission stays folded into pipeline root/channel owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Gpu", "ParallelMjpegDecodePipeline.Metrics.cs")),
            "MJPEG pipeline metrics folded into pipeline root/channel owner");

        return Task.CompletedTask;
    }

    internal static Task FrameFingerprintCadenceTracker_CurrentDuplicateRunLowersUniqueFps()
    {
        var trackerSource = ReadRepoFile("Sussudio/Services/Capture/CaptureCadenceTrackers.cs").Replace("\r\n", "\n");
        var tracker = CreateInstance("Sussudio.Services.Capture.FrameFingerprintCadenceTracker");
        var trackerType = tracker.GetType();
        var recordFrame = trackerType.GetMethod("RecordFrame", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FrameFingerprintCadenceTracker.RecordFrame not found.");
        var getMetrics = trackerType.GetMethod("GetMetrics", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FrameFingerprintCadenceTracker.GetMetrics not found.");

        var intervalTicks = Math.Max(1, Stopwatch.Frequency / 120);
        var tick = Stopwatch.Frequency;
        for (ulong hash = 1; hash <= 120; hash++)
        {
            recordFrame.Invoke(tracker, new object?[] { hash, tick });
            tick += intervalTicks;
        }

        var repeatedHash = 120UL;
        for (var i = 0; i < 90; i++)
        {
            recordFrame.Invoke(tracker, new object?[] { repeatedHash, tick });
            tick += intervalTicks;
        }

        var metrics = getMetrics.Invoke(tracker, new object?[] { 180 })
            ?? throw new InvalidOperationException("FrameFingerprintCadenceTracker.GetMetrics returned null.");

        AssertEqual("DuplicateRun", GetStringProperty(metrics, "Pattern"), "packet hash pattern during trailing duplicate run");
        AssertEqual(true, GetBoolProperty(metrics, "LastFrameDuplicate"), "packet hash last-frame duplicate state");

        var duplicatePercent = GetDoubleProperty(metrics, "DuplicateFramePercent");
        if (duplicatePercent < 40)
        {
            throw new InvalidOperationException($"Duplicate percent did not reflect recent duplicate run: {duplicatePercent:0.00}%.");
        }

        var uniqueFps = GetDoubleProperty(metrics, "UniqueObservedFps");
        if (uniqueFps >= 80)
        {
            throw new InvalidOperationException($"Unique FPS stayed stale during duplicate run: {uniqueFps:0.00} fps.");
        }

        AssertContains(trackerSource, "internal sealed class FrameFingerprintCadenceTracker");
        AssertDoesNotContain(trackerSource, "partial class FrameFingerprintCadenceTracker");
        AssertContains(trackerSource, "public void RecordFrame(ulong hash, long timestampTick = 0)");
        AssertContains(trackerSource, "public static ulong ComputeHash(ReadOnlySpan<byte> data)");
        AssertContains(trackerSource, "private static ulong HashBytes(ulong initialHash, ReadOnlySpan<byte> data)");
        AssertContains(trackerSource, "public readonly record struct Metrics(");
        AssertContains(trackerSource, "public static Metrics Empty { get; }");
        AssertContains(trackerSource, "public Metrics GetMetrics(int maxRecentSamples = 180)");
        AssertContains(trackerSource, "private static double[] BuildRecentUniqueIntervals(");
        AssertContains(trackerSource, "private static string ResolvePattern(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "FrameFingerprintCadenceTracker.cs")),
            "packet hash cadence tracker folded into CaptureCadenceTrackers.cs");

        return Task.CompletedTask;
    }

    internal static Task VisualCadenceTracker_UsesExactCropPixelsWithOnePassDiff()
    {
        var trackerSource = ReadRepoFile("Sussudio/Services/Capture/CaptureCadenceTrackers.cs").Replace("\r\n", "\n");
        var captureSource = ReadUnifiedVideoCaptureSource();

        AssertContains(trackerSource, "internal sealed class VisualCadenceTracker");
        AssertDoesNotContain(trackerSource, "partial class VisualCadenceTracker");
        AssertContains(trackerSource, "DefaultSampleColumns = 640");
        AssertContains(trackerSource, "DefaultSampleRows = 360");
        AssertContains(trackerSource, "sampleX = cropX + Math.Max(0, (cropWidth - sampleWidth) / 2)");
        AssertContains(trackerSource, "sampleY = cropY + Math.Max(0, (cropHeight - sampleHeight) / 2)");
        AssertContains(trackerSource, "var x = sampleX + col;");
        AssertContains(trackerSource, "var y = sampleY + row;");
        AssertContains(trackerSource, "SampleLumaAndCompare(");
        AssertContains(trackerSource, "destination[index] = luma;");
        AssertContains(trackerSource, "if (previous != null && previous[index] != luma)");
        AssertContains(trackerSource, "_lastSample = new byte[_sampleSize * 2]");
        AssertContains(trackerSource, "if (bytesPerLuma == 2)");
        AssertContains(trackerSource, "if (previous != null && previous[index] != secondLuma)");
        AssertContains(trackerSource, "sample.ChangedPixels");
        AssertContains(trackerSource, "PromoteCurrentSample(sampleLength, bytesPerLuma)");
        AssertContains(trackerSource, "_lastSample = _currentSample;");
        AssertContains(trackerSource, "AddValueSample(_deltaWindow, ref _deltaCount, ref _deltaIndex, delta)");
        AssertContains(trackerSource, "if (delta > 0)");
        AssertContains(trackerSource, "private readonly record struct LumaSample(int Length, double ChangedPixels)");
        AssertContains(trackerSource, "private static void AddTimingSample(double[] window, ref int count, ref int index, double value)");
        AssertContains(trackerSource, "private static void AddValueSample(double[] window, ref int count, ref int index, double value)");
        AssertContains(trackerSource, "public readonly record struct Metrics(");
        AssertContains(trackerSource, "public Metrics GetMetrics(int maxRecentIntervals = 180)");
        AssertContains(trackerSource, "var deltaStats = ComputeStats(deltas);");
        AssertContains(trackerSource, "ResolveMotionConfidence(_sampleCount, deltaStats.Average, repeatPercent, changeIntervals.Length)");
        AssertDoesNotContain(trackerSource, "ChangeThreshold");
        AssertDoesNotContain(trackerSource, "ComputeAverageDelta");
        AssertDoesNotContain(trackerSource, "Array.Copy(_currentSample, _lastSample");
        AssertDoesNotContain(trackerSource, "ComputeChangedPixelCount");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "VisualCadenceTracker.Sampling.cs")),
            "old visual cadence sampling partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "VisualCadenceTracker.Metrics.cs")),
            "old visual cadence metrics partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "VisualCadenceTracker.cs")),
            "visual cadence tracker folded into CaptureCadenceTrackers.cs");

        AssertContains(captureSource, "previewFrameProbe: null");
        AssertContains(captureSource, "frame.ArrivalTick");
        AssertContains(captureSource, "cropLeft: 0.25");
        AssertContains(captureSource, "cropWidth: 0.5");
        AssertContains(captureSource, "sampleColumns: 320");
        AssertContains(captureSource, "cropLeft: 0.375");
        AssertContains(captureSource, "cropWidth: 0.25");

        return Task.CompletedTask;
    }

    internal static Task ParallelMjpegDecodePipeline_WorkersLiveWithRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private readonly SoftwareMjpegDecoder[] _decoders;");
        AssertContains(rootText, "private readonly Thread[] _workers;");
        AssertContains(rootText, "StartDecodeWorkers(width, height);");
        AssertContains(rootText, "private void StartDecodeWorkers(int width, int height)");
        AssertContains(rootText, "Name = $\"MjpegWorker-{i}\"");
        AssertContains(rootText, "private void WorkerLoop(int workerIndex)");
        AssertContains(rootText, "private bool HasAliveWorkers()");
        AssertContains(rootText, "DecrementCompressedQueueDepth(\"dequeue\");");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Gpu", "ParallelMjpegDecodePipeline.Workers.cs")),
            "MJPEG worker execution stays folded into pipeline root/channel owner");

        return Task.CompletedTask;
    }

    internal static Task ParallelMjpegDecodePipeline_ReorderLivesWithRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs")
            .Replace("\r\n", "\n");
        var reorderText = rootText;

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
        AssertContains(reorderText, "private void EmitLoop()");
        AssertContains(reorderText, "private bool DrainReadyFrames()");
        AssertContains(reorderText, "private void NotifyPreviewFrameDecoded(PooledVideoFrame frame)");
        AssertContains(reorderText, "private void DrainRemainingFramesInOrder()");
        AssertContains(reorderText, "RecordTimingSample(_reorderLatencyMs");
        AssertContains(reorderText, "_emitCallback(frame.Frame);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Gpu", "ParallelMjpegDecodePipeline.ReorderEmission.cs")),
            "MJPEG reorder emission stays folded into decoded-frame ordering owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Gpu", "ParallelMjpegDecodePipeline.Reorder.cs")),
            "MJPEG decoded-frame ordering folded into the pipeline root");
        AssertContains(rootText, "private void EmitLoop()");
        AssertContains(rootText, "private bool DrainReadyFrames()");
        AssertContains(rootText, "private bool TryAddDecodedFrame(long seqNo, PooledVideoFrame frame, long decodedTick)");
        AssertContains(rootText, "private readonly record struct DecodedFrame(");

        return Task.CompletedTask;
    }

    internal static Task ParallelMjpegDecodePipeline_LifecycleLivesWithRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public void Dispose()");
        AssertContains(rootText, "public bool TryStop(TimeSpan timeout, out string? failureReason)");
        AssertContains(rootText, "private void BeginStop()");
        AssertContains(rootText, "private Thread? _emitThread;");
        AssertContains(rootText, "private readonly AutoResetEvent _emitSignal = new(false);");
        AssertContains(rootText, "private void StartEmitter()");
        AssertContains(rootText, "Name = \"MjpegEmitter\"");
        AssertContains(rootText, "private void SignalEmitter(string operation)");
        AssertContains(rootText, "private bool TryWaitForShutdown(TimeSpan timeout, out string? failureReason)");
        AssertContains(rootText, "private void SignalFatalError(Exception ex)");
        AssertContains(rootText, "private static TimeSpan GetRemainingTimeout(long deadlineTimestamp)");
        AssertContains(rootText, "private void CleanupResources()");
        AssertContains(rootText, "private void DiscardRemainingReorderFrames(string reason)");
        AssertContains(rootText, "private void ReturnRemainingWorkItems()");
        AssertContains(rootText, "ArrayPool<byte>.Shared.Return(item.JpegBuffer);");
        AssertContains(rootText, "_emitSignal.Dispose();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Gpu", "ParallelMjpegDecodePipeline.ResourceCleanup.cs")),
            "MJPEG pipeline resource cleanup folded into ParallelMjpegDecodePipeline root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Gpu", "ParallelMjpegDecodePipeline.Lifecycle.cs")),
            "MJPEG pipeline lifecycle folded into the root pipeline owner");

        return Task.CompletedTask;
    }

    internal static Task ParallelMjpegDecodePipeline_DropsStartupNonJpegBeforeSequencing()
    {
        var source = ReadRepoFile("Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs");
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
