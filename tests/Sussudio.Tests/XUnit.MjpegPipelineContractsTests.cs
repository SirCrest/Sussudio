using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

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
    public Task ParallelMjpegDecodePipelineLifecycleLivesInFocusedPartial()
        => global::Program.ParallelMjpegDecodePipeline_LifecycleLivesInFocusedPartial();

    [Fact]
    public Task ParallelMjpegDecodePipelineCompressedQueueLivesWithRoot()
        => global::Program.ParallelMjpegDecodePipeline_CompressedQueueLivesWithRoot();

    [Fact]
    public Task ParallelMjpegDecodePipelineWorkersLiveWithRoot()
        => global::Program.ParallelMjpegDecodePipeline_WorkersLiveWithRoot();

    [Fact]
    public Task ParallelMjpegDecodePipelineReorderLivesInFocusedPartial()
        => global::Program.ParallelMjpegDecodePipeline_ReorderLivesInFocusedPartial();

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
