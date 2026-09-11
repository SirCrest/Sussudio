using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests
{
    public sealed class MjpegPipelineContractsTests
    {
        private const string GpuNativeMjpegDecodeEnvironmentVariable = "SUSSUDIO_MJPEG_GPU_NATIVE_DECODE";
        private static readonly object GpuNativeMjpegPreferenceEnvironmentLock = new();

        public MjpegPipelineContractsTests()
        {
            global::Program.EnsureTargetAssemblyLoadedForXUnit();
        }

        [Fact]
        public Task UnifiedVideoCaptureCpuMjpegEmitReportsNv12()
            => global::Program.UnifiedVideoCapture_CpuMjpegEmitReportsNv12();

        [Fact]
        public Task UnifiedVideoCaptureNullsMjpegPipelineWhenStopFails()
            => global::Program.UnifiedVideoCapture_NullsMjpegPipeline_WhenStopFails();

        [Fact]
        public void SourceReaderGpuNativeMjpegCapabilityRequiresD3DAndDecodedOutput()
        {
            var captureType = RequireType("Sussudio.Services.Capture.MfSourceReaderVideoCapture");
            var capture = Activator.CreateInstance(captureType)!;

            SetPrivateField(captureType, capture, "_isHighFrameRateMjpegMode", false);
            SetPrivateField(captureType, capture, "_sourceReaderD3DEnabled", true);
            SetPrivateField(captureType, capture, "_isCompressedMjpgOutput", false);
            Assert.False((bool)captureType.GetProperty("IsGpuNativeMjpegDecodeActive")!.GetValue(capture)!);

            SetPrivateField(captureType, capture, "_isHighFrameRateMjpegMode", true);
            SetPrivateField(captureType, capture, "_sourceReaderD3DEnabled", true);
            SetPrivateField(captureType, capture, "_isCompressedMjpgOutput", false);
            Assert.True((bool)captureType.GetProperty("IsGpuNativeMjpegDecodeActive")!.GetValue(capture)!);

            SetPrivateField(captureType, capture, "_isCompressedMjpgOutput", true);
            Assert.False((bool)captureType.GetProperty("IsGpuNativeMjpegDecodeActive")!.GetValue(capture)!);

            SetPrivateField(captureType, capture, "_isCompressedMjpgOutput", false);
            SetPrivateField(captureType, capture, "_sourceReaderD3DEnabled", false);
            Assert.False((bool)captureType.GetProperty("IsGpuNativeMjpegDecodeActive")!.GetValue(capture)!);
        }

        [Theory]
        [InlineData(120, false)]
        [InlineData(119.88, false)]
        [InlineData(60, true)]
        [InlineData(59.94, true)]
        public void GpuNativeMjpegPreference_UsesParallelDecodeAbove60Fps(double fps, bool preferNative)
        {
            WithGpuNativeMjpegDecodeEnvironment(
                value: null,
                () => Assert.Equal(preferNative, ShouldPreferGpuNativeMjpegDecode(isMjpegHighFrameRateDecode: true, fps)));
        }

        [Fact]
        public void GpuNativeMjpegPreference_HonorsExplicitOptInAt120Fps()
        {
            WithGpuNativeMjpegDecodeEnvironment(
                value: "1",
                () => Assert.True(ShouldPreferGpuNativeMjpegDecode(isMjpegHighFrameRateDecode: true, fps: 120)));
        }

        [Fact]
        public void GpuNativeMjpegPreference_HonorsExplicitOptOut()
        {
            WithGpuNativeMjpegDecodeEnvironment(
                value: "0",
                () => Assert.False(ShouldPreferGpuNativeMjpegDecode(isMjpegHighFrameRateDecode: true, fps: 60)));
        }

        [Fact]
        public void GpuNativeMjpegPreference_RejectsNonHighFrameRateMode()
        {
            WithGpuNativeMjpegDecodeEnvironment(
                value: null,
                () =>
                {
                    var isMjpegHighFrameRateDecode = IsMjpegHighFrameRateDecode(
                        useMjpegHighFrameRateMode: false,
                        requireP010: false,
                        requestedPixelFormat: "MJPG");

                    Assert.False(isMjpegHighFrameRateDecode);
                    Assert.False(ShouldPreferGpuNativeMjpegDecode(isMjpegHighFrameRateDecode));
                });
        }

        [Fact]
        public void GpuNativeMjpegPreference_RejectsNonMjpegInput()
        {
            WithGpuNativeMjpegDecodeEnvironment(
                value: null,
                () =>
                {
                    var isMjpegHighFrameRateDecode = IsMjpegHighFrameRateDecode(
                        useMjpegHighFrameRateMode: true,
                        requireP010: false,
                        requestedPixelFormat: "NV12");

                    Assert.False(isMjpegHighFrameRateDecode);
                    Assert.False(ShouldPreferGpuNativeMjpegDecode(isMjpegHighFrameRateDecode));
                });
        }

        [Fact]
        public void GpuNativeMjpegPreference_RejectsP010Request()
        {
            WithGpuNativeMjpegDecodeEnvironment(
                value: null,
                () =>
                {
                    var isMjpegHighFrameRateDecode = IsMjpegHighFrameRateDecode(
                        useMjpegHighFrameRateMode: true,
                        requireP010: true,
                        requestedPixelFormat: "MJPG");

                    Assert.False(isMjpegHighFrameRateDecode);
                    Assert.False(ShouldPreferGpuNativeMjpegDecode(isMjpegHighFrameRateDecode));
                });
        }

        [Fact]
        public async Task GpuNativeMjpegInitialization_AttemptsNativeFirst()
        {
            var probe = await RunMjpegInitializationAsync(
                preferGpuNative: true,
                static (_, _) => null);

            Assert.False(probe.UsesExternalDecode);
            Assert.Equal(new[] { false }, probe.Attempts);
            Assert.Null(probe.ReportedNativeFailure);
        }

        [Fact]
        public async Task GpuNativeMjpegInitialization_RetriesSoftwareAfterNativeFailure()
        {
            var nativeFailure = new InvalidOperationException("native unavailable");
            var probe = await RunMjpegInitializationAsync(
                preferGpuNative: true,
                (useExternalDecode, _) => useExternalDecode ? null : nativeFailure);

            Assert.True(probe.UsesExternalDecode);
            Assert.Equal(new[] { false, true }, probe.Attempts);
            Assert.Same(nativeFailure, probe.ReportedNativeFailure);
        }

        [Fact]
        public async Task SoftwareMjpegInitialization_DoesNotAttemptNative()
        {
            var probe = await RunMjpegInitializationAsync(
                preferGpuNative: false,
                static (_, _) => null);

            Assert.True(probe.UsesExternalDecode);
            Assert.Equal(new[] { true }, probe.Attempts);
            Assert.Null(probe.ReportedNativeFailure);
        }

        [Theory]
        [InlineData("NV12", false)]
        [InlineData("P010", true)]
        public async Task UncompressedInitialization_DoesNotInstallJpegDecode(string format, bool requireP010)
        {
            var isMjpeg = IsMjpegHighFrameRateDecode(
                useMjpegHighFrameRateMode: true, requireP010, requestedPixelFormat: format);
            var attempts = new List<bool>();
            var usesExternalDecode = await InvokeMjpegInitializationAsync(
                preferGpuNative: false,
                external => { attempts.Add(external); return Task.CompletedTask; },
                _ => throw new InvalidOperationException("Uncompressed capture must not retry JPEG decode."),
                isMjpegHighFrameRateDecode: isMjpeg);

            Assert.False(usesExternalDecode);
            Assert.Equal(new[] { false }, attempts);
        }

        [Fact]
        public async Task UncompressedInitialization_DoesNotRetryFailureAsJpeg()
        {
            var failure = new InvalidOperationException("uncompressed source unavailable");
            var attempts = new List<bool>();
            var error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => InvokeMjpegInitializationAsync(
                    preferGpuNative: true,
                    external => { attempts.Add(external); return Task.FromException(failure); },
                    _ => throw new InvalidOperationException("Unexpected JPEG fallback."),
                    isMjpegHighFrameRateDecode: false));

            Assert.Same(failure, error);
            Assert.Equal(new[] { false }, attempts);
        }

        [Fact]
        public async Task GpuNativeMjpegInitialization_PropagatesSoftwareFallbackFailure()
        {
            var nativeFailure = new InvalidOperationException("native unavailable");
            var softwareFailure = new InvalidOperationException("software unavailable");
            var attempts = new List<bool>();
            Exception? reportedNativeFailure = null;

            var error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => InvokeMjpegInitializationAsync(
                    preferGpuNative: true,
                    useExternalDecode =>
                    {
                        attempts.Add(useExternalDecode);
                        return Task.FromException(useExternalDecode ? softwareFailure : nativeFailure);
                    },
                    failure => reportedNativeFailure = failure));

            Assert.Same(softwareFailure, error);
            Assert.Equal(new[] { false, true }, attempts);
            Assert.Same(nativeFailure, reportedNativeFailure);
        }

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
        public Task ParallelMjpegDecodePipelineForceDropRegistersKnownMissing()
            => global::Program.ParallelMjpegDecodePipeline_ForceDropRegistersKnownMissing();

        [Fact]
        public Task ParallelMjpegDecodePipelineNonHeadForceDropPreservesEmissionOrder()
            => global::Program.ParallelMjpegDecodePipeline_NonHeadForceDropPreservesEmissionOrder();

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task ParallelMjpegDecodePipelineCompletionPreservesFrameOwnership(bool fatal)
            => global::Program.ParallelMjpegDecodePipeline_CompletionPreservesFrameOwnership(fatal);

        [Fact]
        public Task ParallelMjpegDecodePipelineNormalCompletionConsumesFinalMissingSequences()
            => global::Program.ParallelMjpegDecodePipeline_NormalCompletionConsumesFinalMissingSequences();

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

            var samples = Enumerable.Range(1, 20).Select(static value => (double)value).ToArray();

            var result = method.Invoke(null, new object[] { samples });
            var resultType = result!.GetType();

            var maxField = resultType.GetField("Item4")!;
            var max = Convert.ToDouble(maxField.GetValue(result));
            Assert.Equal(20.0, max);

            var p95Field = resultType.GetField("Item3")!;
            var p95 = Convert.ToDouble(p95Field.GetValue(result));
            Assert.Equal(19.0, p95);
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
            var metricsType = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline+PipelineTimingMetrics");

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
        public void SoftwareMjpegDecoderLivesWithPipelineWorker()
        {
            var rootText = ReadRepoFile("Sussudio/Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs");
            var decoderType = RequireType("Sussudio.Services.Capture.Mjpeg.SoftwareMjpegDecoder");

            AssertContains(rootText, "internal sealed unsafe class SoftwareMjpegDecoder : IDisposable");
            AssertContains(rootText, "public void Initialize(int width, int height)");
            AssertContains(rootText, "public void Dispose()");
            AssertContains(rootText, "public bool DecodeToNv12(ReadOnlySpan<byte> jpegData, Span<byte> nv12Destination)");
            AssertContains(rootText, "SW_MJPEG_DECODE_DIAG");
            AssertContains(rootText, "Buffer.MemoryCopy(");
            Assert.False(
                File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "Sussudio", "Services", "Capture", "Mjpeg", "SoftwareMjpegDecoder.Decode.cs")),
                "Software MJPEG decode path folded into decoder state/lifetime owner");
            Assert.False(
                File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "Sussudio", "Services", "Capture", "Mjpeg", "SoftwareMjpegDecoder.cs")),
                "Software MJPEG decoder folded into the pipeline worker owner");

            // The policy in Sussudio-Defragmentation-Goal.md requires a written rationale
            // for any file left above 1200 lines; keep the pipeline's entry honest.
            var cleanupPlanText = RuntimeContractSource.ReadRepoFile("docs/architecture/cleanup-plan.md");
            AssertContains(cleanupPlanText, "## Retained Large Files");
            AssertContains(cleanupPlanText, "`Sussudio/Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs` (");
            AssertContains(cleanupPlanText, "one _reorderLock-guarded sequencing invariant");
            AssertContains(cleanupPlanText, "SoftwareMjpegDecoder is the per-worker leaf");

            var widthProp = decoderType.GetProperty("Width", BindingFlags.Public | BindingFlags.Instance);
            var heightProp = decoderType.GetProperty("Height", BindingFlags.Public | BindingFlags.Instance);
            var nv12SizeProp = decoderType.GetProperty("Nv12Size", BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(widthProp);
            Assert.NotNull(heightProp);
            Assert.NotNull(nv12SizeProp);
        }

        [Theory]
        [InlineData(0, 16, typeof(ArgumentOutOfRangeException))]
        [InlineData(16, 0, typeof(ArgumentOutOfRangeException))]
        [InlineData(-1, 16, typeof(ArgumentOutOfRangeException))]
        [InlineData(16, -1, typeof(ArgumentOutOfRangeException))]
        [InlineData(int.MaxValue, 2, typeof(OverflowException))]
        [InlineData(32768, 32768, typeof(OverflowException))]
        public void SoftwareMjpegDecoderRejectsInvalidDimensionsBeforeNativeAllocation(
            int width, int height, Type expectedException)
        {
            var decoderType = RequireType("Sussudio.Services.Capture.Mjpeg.SoftwareMjpegDecoder");
            var decoder = Activator.CreateInstance(decoderType, nonPublic: true)!;
            using var owner = (IDisposable)decoder;
            var initialize = decoderType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance)!;

            var error = Assert.Throws<TargetInvocationException>(() =>
                initialize.Invoke(decoder, new object[] { width, height }));

            Assert.IsType(expectedException, error.InnerException);
            AssertSoftwareMjpegDecoderHasNoNativeResources(decoder);
            Assert.False((bool)decoderType.GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(decoder)!);
            Assert.Equal(0, decoderType.GetProperty("Width")!.GetValue(decoder));
            Assert.Equal(0, decoderType.GetProperty("Height")!.GetValue(decoder));
            Assert.Equal(0, decoderType.GetProperty("Nv12Size")!.GetValue(decoder));
        }

        [Fact]
        public void SoftwareMjpegDecoderRejectsInitializationAfterDispose()
        {
            var decoderType = RequireType("Sussudio.Services.Capture.Mjpeg.SoftwareMjpegDecoder");
            var decoder = Activator.CreateInstance(decoderType, nonPublic: true)!;
            using var owner = (IDisposable)decoder;
            owner.Dispose();

            var initialize = decoderType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance)!;
            var error = Assert.Throws<TargetInvocationException>(() =>
                initialize.Invoke(decoder, new object[] { 16, 16 }));

            Assert.IsType<ObjectDisposedException>(error.InnerException);
            AssertSoftwareMjpegDecoderHasNoNativeResources(decoder);
            owner.Dispose();
            AssertSoftwareMjpegDecoderHasNoNativeResources(decoder);
        }

        private static unsafe void AssertSoftwareMjpegDecoderHasNoNativeResources(object decoder)
        {
            var decoderType = decoder.GetType();
            foreach (var fieldName in new[] { "_decoderCtx", "_decodedFrame", "_drainFrame", "_packet" })
            {
                var field = decoderType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;
                Assert.Equal(IntPtr.Zero, (IntPtr)Pointer.Unbox(field.GetValue(decoder)!));
            }

            Assert.False((bool)decoderType.GetField("_initialized", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(decoder)!);
        }

        private static MethodInfo RequirePipelineMethod(string methodName)
        {
            var pipelineType = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline");
            return pipelineType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"{methodName} not found.");
        }

        private static void SetPrivateField(Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field!.SetValue(instance, value);
        }

        private static bool IsMjpegHighFrameRateDecode(
            bool useMjpegHighFrameRateMode,
            bool requireP010,
            string? requestedPixelFormat)
            => InvokeUnifiedVideoCapturePolicy(
                "IsMjpegHighFrameRateDecode",
                useMjpegHighFrameRateMode,
                requireP010,
                requestedPixelFormat);

        private static bool ShouldPreferGpuNativeMjpegDecode(bool isMjpegHighFrameRateDecode, double fps = 120)
            => InvokeUnifiedVideoCapturePolicy(
                "ShouldPreferGpuNativeMjpegDecode",
                isMjpegHighFrameRateDecode,
                fps);

        private static bool InvokeUnifiedVideoCapturePolicy(string methodName, params object?[] arguments)
        {
            var captureType = RequireType("Sussudio.Services.Capture.UnifiedVideoCapture");
            var method = captureType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"UnifiedVideoCapture.{methodName} was not found.");
            return (bool)method.Invoke(null, arguments)!;
        }

        private static async Task<MjpegInitializationProbe> RunMjpegInitializationAsync(
            bool preferGpuNative,
            Func<bool, int, Exception?> failureFactory)
        {
            var attempts = new List<bool>();
            Exception? reportedNativeFailure = null;

            var usesExternalDecode = await InvokeMjpegInitializationAsync(
                preferGpuNative,
                useExternalDecode =>
                {
                    attempts.Add(useExternalDecode);
                    var failure = failureFactory(useExternalDecode, attempts.Count);
                    return failure == null ? Task.CompletedTask : Task.FromException(failure);
                },
                failure => reportedNativeFailure = failure);

            return new MjpegInitializationProbe(usesExternalDecode, attempts, reportedNativeFailure);
        }

        private static async Task<bool> InvokeMjpegInitializationAsync(
            bool preferGpuNative,
            Func<bool, Task> initializeAsync,
            Action<Exception> reportNativeFailure,
            bool isMjpegHighFrameRateDecode = true)
        {
            var captureType = RequireType("Sussudio.Services.Capture.UnifiedVideoCapture");
            var method = captureType.GetMethod(
                "InitializeMjpegSourceReaderWithFallbackAsync",
                BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException(
                    "UnifiedVideoCapture.InitializeMjpegSourceReaderWithFallbackAsync was not found.");
            var task = method.Invoke(
                    null,
                    new object[] { isMjpegHighFrameRateDecode, preferGpuNative, initializeAsync, reportNativeFailure }) as Task<bool>
                ?? throw new InvalidOperationException(
                    "InitializeMjpegSourceReaderWithFallbackAsync did not return Task<bool>.");
            return await task.ConfigureAwait(false);
        }

        private static void WithGpuNativeMjpegDecodeEnvironment(string? value, Action assertion)
        {
            lock (GpuNativeMjpegPreferenceEnvironmentLock)
            {
                var originalValue = Environment.GetEnvironmentVariable(GpuNativeMjpegDecodeEnvironmentVariable);
                try
                {
                    Environment.SetEnvironmentVariable(GpuNativeMjpegDecodeEnvironmentVariable, value);
                    assertion();
                }
                finally
                {
                    Environment.SetEnvironmentVariable(GpuNativeMjpegDecodeEnvironmentVariable, originalValue);
                }
            }
        }

        private static Type RequireType(string typeName)
            => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

        private sealed record MjpegInitializationProbe(
            bool UsesExternalDecode,
            IReadOnlyList<bool> Attempts,
            Exception? ReportedNativeFailure);

        private static string ReadRepoFile(string relativePath)
            => RuntimeContractSource.ReadRepoFile(relativePath).Replace("\r\n", "\n");

        private static void AssertContains(string actual, string expectedSubstring)
            => Assert.Contains(expectedSubstring, actual, StringComparison.Ordinal);
    }
}

static partial class Program
{
    private static object CreatePooledVideoFrame(
        Type frameType,
        object pixelFormat,
        long sequenceNumber,
        long arrivalTick,
        long decodedTick,
        int width,
        int height,
        int length,
        ArrayPool<byte> pool)
    {
        var constructor = frameType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[]
            {
                typeof(long),
                typeof(long),
                typeof(long),
                typeof(int),
                typeof(int),
                pixelFormat.GetType(),
                typeof(int),
                typeof(ArrayPool<byte>)
            },
            modifiers: null)
            ?? throw new InvalidOperationException("PooledVideoFrame private constructor not found.");

        return constructor.Invoke(new object[] { sequenceNumber, arrivalTick, decodedTick, width, height, pixelFormat, length, pool })
            ?? throw new InvalidOperationException("PooledVideoFrame constructor returned null.");
    }

    private static object CreateUnstartedJitterBuffer(Type jitterType, int targetDepth)
    {
        var jitter = RuntimeHelpers.GetUninitializedObject(jitterType);
        var bufferedFrameType = RequireNestedType(jitterType, "BufferedFrame");
        var listType = typeof(List<>).MakeGenericType(bufferedFrameType);

        SetPrivateField(jitter, "_sync", new object());
        SetPrivateField(jitter, "_frames", Activator.CreateInstance(listType));
        SetPrivateField(jitter, "_signal", new AutoResetEvent(false));
        SetPrivateField(jitter, "_frameIntervalTicks", Math.Max(1L, Stopwatch.Frequency / 120L));
        SetPrivateField(jitter, "_minAdaptiveTargetDepth", 2);
        SetPrivateField(jitter, "_maxAdaptiveTargetDepth", 8);
        SetPrivateField(jitter, "_targetDepth", targetDepth);
        SetPrivateField(jitter, "_maxDepth", 12);
        SetPrivateField(jitter, "_nextPreviewSequence", -1L);
        SetPrivateField(jitter, "_lastAdaptiveIssueTick", Stopwatch.GetTimestamp());
        SetPrivateField(jitter, "_lastTargetDecreaseTick", Stopwatch.GetTimestamp());
        return jitter;
    }

    private static Type RequireNestedType(Type declaringType, string nestedTypeName)
        => declaringType.GetNestedType(nestedTypeName, BindingFlags.NonPublic)
           ?? throw new InvalidOperationException($"Nested type '{nestedTypeName}' not found on '{declaringType.Name}'.");

    private static object CreateRawBufferedFrame(Type bufferedFrameType, long enqueueTick)
    {
        var constructor = bufferedFrameType.GetConstructor(
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(long), typeof(long) },
            modifiers: null)
            ?? throw new InvalidOperationException("Raw BufferedFrame constructor not found.");

        return constructor.Invoke(new object[] { ArrayPool<byte>.Shared.Rent(384), 384, 16, 16, 10L, enqueueTick })
            ?? throw new InvalidOperationException("Raw BufferedFrame constructor returned null.");
    }

    private static object CreateLeaseBufferedFrame(Type bufferedFrameType, object lease, long enqueueTick)
    {
        var constructor = bufferedFrameType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single(ctor =>
            {
                var parameters = ctor.GetParameters();
                return parameters.Length == 2 &&
                       parameters[0].ParameterType.Name == "PooledVideoFrameLease" &&
                       parameters[1].ParameterType == typeof(long);
            });

        return constructor.Invoke(new[] { lease, enqueueTick })
            ?? throw new InvalidOperationException("Lease BufferedFrame constructor returned null.");
    }

    private static object? CreatePendingFrameArgument(ParameterInfo parameter, Type leaseType, object lease)
    {
        if (parameter.ParameterType == leaseType)
        {
            return lease;
        }

        if (!parameter.ParameterType.IsValueType)
        {
            return null;
        }

        if (parameter.ParameterType == typeof(int))
        {
            return parameter.Name is "width" or "height" ? 16 : 0;
        }

        if (parameter.ParameterType == typeof(long))
        {
            return 1L;
        }

        if (parameter.ParameterType == typeof(bool))
        {
            return false;
        }

        if (parameter.ParameterType == typeof(IntPtr))
        {
            return IntPtr.Zero;
        }

        return Activator.CreateInstance(parameter.ParameterType);
    }

    private static long GetLongPrivateField(object instance, string fieldName)
        => Convert.ToInt64(GetPrivateField(instance, fieldName));

    private static int GetIntPrivateField(object instance, string fieldName)
        => Convert.ToInt32(GetPrivateField(instance, fieldName));

    private static string GetStringPrivateField(object instance, string fieldName)
        => Convert.ToString(GetPrivateField(instance, fieldName), CultureInfo.InvariantCulture) ?? string.Empty;

    private static void AssertAddLeaseThrows(MethodInfo addLeaseMethod, object frame)
    {
        try
        {
            addLeaseMethod.Invoke(frame, Array.Empty<object>());
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ObjectDisposedException)
        {
            return;
        }

        throw new InvalidOperationException("AddLease should throw ObjectDisposedException.");
    }

    private static void AssertPropertyThrowsObjectDisposed(object instance, string propertyName)
    {
        try
        {
            _ = GetPropertyValue(instance, propertyName);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ObjectDisposedException)
        {
            return;
        }

        throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} should throw ObjectDisposedException.");
    }

    private sealed class TrackingArrayPool : ArrayPool<byte>
    {
        private byte[]? _rented;

        public int RentCount { get; private set; }
        public int ReturnCount { get; private set; }

        public override byte[] Rent(int minimumLength)
        {
            RentCount++;
            _rented = new byte[Math.Max(1, minimumLength)];
            return _rented;
        }

        public override void Return(byte[] array, bool clearArray = false)
        {
            if (!ReferenceEquals(array, _rented))
            {
                throw new InvalidOperationException("Unexpected array returned to pool.");
            }

            ReturnCount++;
        }
    }

    internal static Task PooledVideoFrame_LeaseLifecycle_ReturnsBufferAfterLastRelease()
    {
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");

        var nv12 = Enum.Parse(formatType, "Nv12");
        var pool = new TrackingArrayPool();
        var frame = CreatePooledVideoFrame(frameType, nv12, 123L, 10L, 20L, 1920, 1080, 1024, pool);

        AssertEqual(123L, GetLongProperty(frame, "SequenceNumber"), "SequenceNumber");
        AssertEqual(1024, GetIntProperty(frame, "Length"), "Length");
        AssertEqual(1, GetIntProperty(frame, "LeaseCount"), "initial LeaseCount");
        AssertEqual(false, GetBoolProperty(frame, "IsReturned"), "initial IsReturned");
        AssertEqual(1, pool.RentCount, "pool rent count");

        var lease1 = addLeaseMethod.Invoke(frame, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");
        var lease2 = addLeaseMethod.Invoke(frame, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");

        AssertEqual(3, GetIntProperty(frame, "LeaseCount"), "LeaseCount after two leases");
        ((IDisposable)lease1).Dispose();
        AssertEqual(2, GetIntProperty(frame, "LeaseCount"), "LeaseCount after lease1 dispose");
        AssertEqual(false, GetBoolProperty(frame, "IsReturned"), "IsReturned with owner and lease2 alive");

        ((IDisposable)frame).Dispose();
        AssertEqual(1, GetIntProperty(frame, "LeaseCount"), "LeaseCount after owner dispose");
        AssertEqual(false, GetBoolProperty(frame, "IsReturned"), "IsReturned with lease2 alive");

        ((IDisposable)lease2).Dispose();
        AssertEqual(0, GetIntProperty(frame, "LeaseCount"), "LeaseCount after final lease dispose");
        AssertEqual(true, GetBoolProperty(frame, "IsReturned"), "IsReturned after final release");
        AssertEqual(1, pool.ReturnCount, "pool return count");

        ((IDisposable)lease2).Dispose();
        AssertEqual(0, GetIntProperty(frame, "LeaseCount"), "LeaseCount after duplicate lease dispose");
        AssertEqual(1, pool.ReturnCount, "pool return count after duplicate dispose");

        AssertPropertyThrowsObjectDisposed(frame, "Memory");

        return Task.CompletedTask;
    }

    internal static Task PooledVideoFrame_AddLeaseAfterReturn_Throws()
    {
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");

        var nv12 = Enum.Parse(formatType, "Nv12");
        var frame = CreatePooledVideoFrame(frameType, nv12, 7L, 1L, 2L, 16, 16, 384, new TrackingArrayPool());

        ((IDisposable)frame).Dispose();
        AssertEqual(true, GetBoolProperty(frame, "IsReturned"), "IsReturned after owner-only dispose");

        try
        {
            addLeaseMethod.Invoke(frame, Array.Empty<object>());
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ObjectDisposedException)
        {
            return Task.CompletedTask;
        }

        throw new InvalidOperationException("AddLease after return should throw ObjectDisposedException.");
    }

    internal static Task PooledVideoFrame_OwnerDisposeClosesNewLeasesButExistingLeaseRemainsReadable()
    {
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");

        var nv12 = Enum.Parse(formatType, "Nv12");
        var pool = new TrackingArrayPool();
        var frame = CreatePooledVideoFrame(frameType, nv12, 8L, 3L, 4L, 16, 16, 384, pool);
        var lease = addLeaseMethod.Invoke(frame, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");

        ((IDisposable)frame).Dispose();
        AssertEqual(1, GetIntProperty(frame, "LeaseCount"), "LeaseCount with existing lease after owner dispose");
        AssertEqual(false, GetBoolProperty(frame, "IsReturned"), "IsReturned with existing lease after owner dispose");
        AssertPropertyThrowsObjectDisposed(frame, "Memory");
        AssertAddLeaseThrows(addLeaseMethod, frame);

        _ = GetPropertyValue(lease, "Memory");
        AssertEqual(8L, GetLongProperty(lease, "SequenceNumber"), "lease SequenceNumber");
        AssertEqual(384, GetIntProperty(lease, "Length"), "lease Length");

        ((IDisposable)lease).Dispose();
        AssertEqual(true, GetBoolProperty(frame, "IsReturned"), "IsReturned after existing lease dispose");
        AssertEqual(1, pool.ReturnCount, "pool return count");
        AssertPropertyThrowsObjectDisposed(lease, "Memory");

        return Task.CompletedTask;
    }

    internal static Task MjpegPooledFrameFanout_ExposesLeaseContracts()
    {
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var leaseType = RequireType("Sussudio.Services.Contracts.PooledVideoFrameLease");
        var leaseEncoderType = RequireType("Sussudio.Services.Contracts.IRawVideoFrameLeaseTryEncoder");
        var leaseAdmission = leaseEncoderType.GetMethod("TryEnqueueRawVideoFrame", new[] { leaseType });
        AssertNotNull(leaseAdmission, "lease Try admission contract");
        AssertEqual(typeof(bool), leaseAdmission!.ReturnType, "lease admission returns acceptance");
        AssertEqual(null, leaseEncoderType.Assembly.GetType("Sussudio.Services.Contracts.IRawVideoFrameLeaseEncoder"), "obsolete void lease contract removed");
        var pipelineEmitCallbackType = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline+EmitFrameCallback");
        var previewSinkType = RequireType("Sussudio.Services.Contracts.IPreviewFrameSink");
        var jitterBufferType = RequireType("Sussudio.Services.Capture.MjpegPreviewJitterBuffer");
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var libAvSinkType = RequireType("Sussudio.Services.Recording.LibAvRecordingSink");
        var flashbackSinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");

        var emitInvoke = pipelineEmitCallbackType.GetMethod("Invoke")
            ?? throw new InvalidOperationException("EmitFrameCallback.Invoke not found.");
        var emitParameter = emitInvoke.GetParameters().SingleOrDefault()
            ?? throw new InvalidOperationException("EmitFrameCallback should have one parameter.");
        AssertEqual(frameType, emitParameter.ParameterType, "MJPEG emit callback frame type");

        var previewLeaseEnqueue = jitterBufferType.GetMethod(
            "Enqueue",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { leaseType },
            modifiers: null);
        AssertNotNull(previewLeaseEnqueue, "MjpegPreviewJitterBuffer.Enqueue(PooledVideoFrameLease)");

        var trackingType = previewSinkType.Assembly.GetType("Sussudio.Services.Contracts.PreviewFrameTracking", throwOnError: true)!;
        var rendererLeaseSubmit = previewSinkType.GetMethod(
            "SubmitRawFrameLease",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { leaseType, typeof(bool), trackingType },
            modifiers: null);
        AssertNotNull(rendererLeaseSubmit, "IPreviewFrameSink.SubmitRawFrameLease(PooledVideoFrameLease, bool, PreviewFrameTracking)");
        AssertEqual(true, previewSinkType.IsAssignableFrom(rendererType), "D3D11PreviewRenderer implements preview lease sink");
        AssertEqual(true, leaseEncoderType.IsAssignableFrom(libAvSinkType), "LibAvRecordingSink implements lease encoder");
        AssertEqual(true, leaseEncoderType.IsAssignableFrom(flashbackSinkType), "FlashbackEncoderSink implements lease encoder");

        return Task.CompletedTask;
    }

    internal static Task D3DPreviewPendingFrame_ReleasesQueuedLease()
    {
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var leaseType = RequireType("Sussudio.Services.Contracts.PooledVideoFrameLease");
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var pendingFrameType = RequireNestedType(rendererType, "PendingFrame");
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");

        var nv12 = Enum.Parse(formatType, "Nv12");
        var pool = new TrackingArrayPool();
        var owner = CreatePooledVideoFrame(frameType, nv12, 77L, 1L, 2L, 16, 16, 384, pool);
        var lease = addLeaseMethod.Invoke(owner, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");
        ((IDisposable)owner).Dispose();

        var constructor = pendingFrameType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single(ctor => ctor.GetParameters().Any(parameter => parameter.ParameterType == leaseType));
        var args = constructor.GetParameters()
            .Select(parameter => CreatePendingFrameArgument(parameter, leaseType, lease))
            .ToArray();
        var pendingFrame = constructor.Invoke(args)
            ?? throw new InvalidOperationException("PendingFrame constructor returned null.");

        ((IDisposable)pendingFrame).Dispose();

        AssertEqual(true, GetBoolProperty(owner, "IsReturned"), "pending preview frame lease returned");
        AssertEqual(1, pool.ReturnCount, "pending preview frame pool return count");

        return Task.CompletedTask;
    }

    internal static Task MjpegLeasedVideoPackets_ReleaseQueuedLeases()
    {
        AssertLeasedPacketReturnDisposesLease(
            sinkTypeName: "Sussudio.Services.Recording.LibAvRecordingSink",
            packetTypeName: "Sussudio.Services.Recording.LibAvRecordingSink+VideoFramePacket");
        AssertLeasedPacketReturnDisposesLease(
            sinkTypeName: "Sussudio.Services.Flashback.FlashbackEncoderSink",
            packetTypeName: "Sussudio.Services.Flashback.FlashbackEncoderSink+VideoFramePacket");

        return Task.CompletedTask;
    }

    private static void AssertLeasedPacketReturnDisposesLease(string sinkTypeName, string packetTypeName)
    {
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var leaseType = RequireType("Sussudio.Services.Contracts.PooledVideoFrameLease");
        var sinkType = RequireType(sinkTypeName);
        var packetType = RequireType(packetTypeName);
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");
        var frameFactory = packetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method =>
                method.Name == "Frame" &&
                method.GetParameters().Length == 2 &&
                method.GetParameters()[0].ParameterType == leaseType &&
                method.GetParameters()[1].ParameterType == typeof(long))
            ?? throw new InvalidOperationException($"{packetTypeName}.Frame(PooledVideoFrameLease, long) not found.");
        var returnPacket = sinkType.GetMethod("ReturnVideoPacket", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{sinkTypeName}.ReturnVideoPacket not found.");

        var nv12 = Enum.Parse(formatType, "Nv12");
        var pool = new TrackingArrayPool();
        var frame = CreatePooledVideoFrame(frameType, nv12, 55L, 11L, 12L, 16, 16, 384, pool);
        var lease = addLeaseMethod.Invoke(frame, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");

        ((IDisposable)frame).Dispose();
        AssertEqual(false, GetBoolProperty(frame, "IsReturned"), $"{sinkType.Name} owner dispose keeps queued lease alive");
        AssertEqual(0, pool.ReturnCount, $"{sinkType.Name} pool return before packet cleanup");

        var packet = frameFactory.Invoke(null, new[] { lease, Environment.TickCount64 })
            ?? throw new InvalidOperationException($"{packetTypeName}.Frame returned null.");
        returnPacket.Invoke(null, new[] { packet });

        AssertEqual(true, GetBoolProperty(frame, "IsReturned"), $"{sinkType.Name} packet cleanup returns frame");
        AssertEqual(1, pool.ReturnCount, $"{sinkType.Name} pool return after packet cleanup");
    }

    internal static Task ParallelMjpegDecodePipeline_SharedReorder_DoesNotSynthesizeRecordingSkips()
    {
        var source = ReadRepoFile("Sussudio/Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs");
        AssertContains(source, "MJPEG_PIPELINE_STARTUP_DROP");
        AssertContains(source, "HasJpegStartOfImage");
        AssertContains(source, "MJPEG_REORDER_STRICT_WAIT");
        AssertContains(source, "MJPEG_REORDER_STRICT_ADVANCE");
        AssertContains(source, "SortedDictionary<long, DecodedFrame>");
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
        var rootText = ReadRepoFile("Sussudio/Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(rootText, "partial class ParallelMjpegDecodePipeline");
        AssertContains(rootText, "private const int WorkQueueItemCapacityPerDecoder = 8;");
        AssertContains(rootText, "private readonly Channel<MjpegWorkItem> _workQueue;");
        AssertContains(rootText, "private readonly FrameFingerprintCadenceTracker _packetHashTracker = new();");
        AssertContains(rootText, "namespace Sussudio.Services.Capture.Mjpeg;");
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

        return Task.CompletedTask;
    }

    internal static Task FrameFingerprintCadenceTracker_CurrentDuplicateRunLowersUniqueFps()
    {
        var trackerSource = ReadRepoFile("Sussudio/Services/Capture/Mjpeg/FrameFingerprintCadenceTracker.cs").Replace("\r\n", "\n");
        var tracker = CreateInstance("Sussudio.Services.Capture.Mjpeg.FrameFingerprintCadenceTracker");
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
        var rootText = ReadRepoFile("Sussudio/Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private readonly SoftwareMjpegDecoder[] _decoders;");
        AssertContains(rootText, "private readonly Thread[] _workers;");
        AssertContains(rootText, "StartDecodeWorkers(width, height);");
        AssertContains(rootText, "private void StartDecodeWorkers(int width, int height)");
        AssertContains(rootText, "Name = $\"MjpegWorker-{i}\"");
        AssertContains(rootText, "private void WorkerLoop(int workerIndex)");
        AssertContains(rootText, "private bool HasAliveWorkers()");
        AssertContains(rootText, "DecrementCompressedQueueDepth(\"dequeue\");");

        return Task.CompletedTask;
    }

    internal static Task ParallelMjpegDecodePipeline_ReorderLivesWithRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs")
            .Replace("\r\n", "\n");
        var reorderText = rootText;

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
        AssertDoesNotContain(reorderText, "DrainRemainingFramesInOrder");
        AssertContains(reorderText, "RecordTimingSample(_reorderLatencyMs");
        AssertContains(reorderText, "_emitCallback(frame.Frame);");
        AssertContains(rootText, "private void EmitLoop()");
        AssertContains(rootText, "private bool DrainReadyFrames()");
        AssertContains(rootText, "private bool TryAddDecodedFrame(long seqNo, PooledVideoFrame frame, long decodedTick)");
        AssertContains(rootText, "private readonly record struct DecodedFrame(");

        return Task.CompletedTask;
    }

    internal static Task ParallelMjpegDecodePipeline_LifecycleLivesWithRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public void Dispose()");
        AssertContains(rootText, "public bool TryStop(TimeSpan timeout, [NotNullWhen(false)] out string? failureReason)");
        AssertContains(rootText, "private void BeginStop()");
        AssertContains(rootText, "private Thread? _emitThread;");
        AssertContains(rootText, "private readonly AutoResetEvent _emitSignal = new(false);");
        AssertContains(rootText, "private void StartEmitter()");
        AssertContains(rootText, "Name = \"MjpegEmitter\"");
        AssertContains(rootText, "private void SignalEmitter(string operation)");
        AssertContains(rootText, "private bool TryWaitForShutdown(TimeSpan timeout, [NotNullWhen(false)] out string? failureReason)");
        AssertContains(rootText, "private void SignalFatalError(Exception ex)");
        AssertContains(rootText, "private static TimeSpan GetRemainingTimeout(long deadlineTimestamp)");
        AssertContains(rootText, "private void CleanupResources()");
        AssertContains(rootText, "private void DiscardRemainingReorderFrames(string reason)");
        AssertContains(rootText, "private void ReturnRemainingWorkItems()");
        AssertContains(rootText, "ArrayPool<byte>.Shared.Return(item.JpegBuffer);");
        AssertContains(rootText, "_emitSignal.Dispose();");

        return Task.CompletedTask;
    }

    internal static Task ParallelMjpegDecodePipeline_DropsStartupNonJpegBeforeSequencing()
    {
        var source = ReadRepoFile("Sussudio/Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs");
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
        var pipelineType = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline");
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

    internal static Task ParallelMjpegDecodePipeline_ForceDropRegistersKnownMissing()
    {
        var pipelineType = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline");
        var pipeline = RuntimeHelpers.GetUninitializedObject(pipelineType);
        var reorderLock = new object();
        var reorderFrames = CreateSortedDictionary(pipelineType);
        var knownMissing = new SortedSet<long>();
        using var emitSignal = new AutoResetEvent(false);

        SetPrivateField(pipeline, "_reorderLock", reorderLock);
        SetPrivateField(pipeline, "_reorderFrames", reorderFrames);
        SetPrivateField(pipeline, "_knownMissingSequences", knownMissing);
        SetPrivateField(pipeline, "_emitSignal", emitSignal);
        SetPrivateField(pipeline, "_nextEmitSeq", 0L);
        SetPrivateField(pipeline, "_reorderBufferDepth", 0);

        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var nv12 = Enum.Parse(formatType, "Nv12");

        for (var sequence = 0L; sequence < 16; sequence++)
        {
            var pool = new TrackingArrayPool();
            var frame = CreatePooledVideoFrame(frameType, nv12, sequence, 100L, 200L, 16, 16, 384, pool);
            using var frameOwner = (IDisposable)frame;
            InsertDecodedFrame(pipeline, pipelineType, reorderLock, reorderFrames, sequence, frame);

            lock (reorderLock)
            {
                InvokeNonPublicInstanceMethod(pipeline, "ForceDropOldestReorderFrameUnderLock", Array.Empty<object?>());
            }

            AssertEqual(sequence, (long)GetPrivateField(pipeline, "_nextEmitSeq")!, "head eviction leaves cursor advancement to emitter");
            AssertEqual(true, knownMissing.SetEquals(new[] { sequence }), "head eviction registers exactly its missing sequence");
            AssertEqual(0, (int)GetPrivateField(pipeline, "_reorderBufferDepth")!, "head eviction empties reorder buffer");
            AssertEqual(sequence + 1, (long)GetPrivateField(pipeline, "_reorderForceDrops")!, "head eviction force-drop count");
            AssertEqual(sequence + 1, (long)GetPrivateField(pipeline, "_totalFramesDropped")!, "head eviction total-drop count");
            AssertEqual(sequence, (long)GetPrivateField(pipeline, "_reorderSkips")!, "head eviction does not count an unconsumed gap");
            AssertEqual(1, pool.ReturnCount, "head eviction returns its frame exactly once");

            var consumed = (bool)InvokeNonPublicInstanceMethod(pipeline, "ConsumeKnownMissingFrames", Array.Empty<object?>())!;
            AssertEqual(true, consumed, "head eviction gap was consumed");
            AssertEqual(sequence + 1, (long)GetPrivateField(pipeline, "_nextEmitSeq")!, "head eviction advances the cursor once");
            AssertEqual(sequence + 1, (long)GetPrivateField(pipeline, "_reorderSkips")!, "head eviction counts one reorder skip");
            AssertEqual(0, knownMissing.Count, "repeated head eviction leaves no stale missing sequences");

            var consumedAgain = (bool)InvokeNonPublicInstanceMethod(pipeline, "ConsumeKnownMissingFrames", Array.Empty<object?>())!;
            AssertEqual(false, consumedAgain, "head eviction gap is not consumed twice");
            AssertEqual(sequence + 1, (long)GetPrivateField(pipeline, "_nextEmitSeq")!, "repeated consumption preserves cursor");
            AssertEqual(sequence + 1, (long)GetPrivateField(pipeline, "_reorderSkips")!, "repeated consumption preserves skip count");
            AssertEqual(0, knownMissing.Count, "repeated consumption preserves empty gap set");
            AssertEqual(1, pool.ReturnCount, "gap consumption does not return the evicted frame again");
        }

        return Task.CompletedTask;
    }

    internal static Task ParallelMjpegDecodePipeline_NonHeadForceDropPreservesEmissionOrder()
    {
        var pipelineType = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline");
        var pipeline = RuntimeHelpers.GetUninitializedObject(pipelineType);
        var reorderLock = new object();
        var reorderFrames = CreateSortedDictionary(pipelineType);
        var knownMissing = new SortedSet<long>();
        var emitted = new List<long>();
        Action<object> collectFrame = frame => emitted.Add((long)GetPropertyValue(frame, "SequenceNumber")!);
        var callbackType = pipelineType.GetNestedType("EmitFrameCallback", BindingFlags.Public)
            ?? throw new InvalidOperationException("EmitFrameCallback not found.");

        SetPrivateField(pipeline, "_reorderLock", reorderLock);
        SetPrivateField(pipeline, "_reorderFrames", reorderFrames);
        SetPrivateField(pipeline, "_knownMissingSequences", knownMissing);
        SetPrivateField(pipeline, "_timingLock", new object());
        SetPrivateField(pipeline, "_reorderLatencyMs", new double[8]);
        SetPrivateField(pipeline, "_pipelineLatencyMs", new double[8]);
        SetPrivateField(pipeline, "_emitCallback", Delegate.CreateDelegate(callbackType, collectFrame.Target, collectFrame.Method));

        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var nv12 = Enum.Parse(formatType, "Nv12");
        var pools = Enumerable.Range(0, 4).Select(_ => new TrackingArrayPool()).ToArray();
        var frames = new List<object>();
        try
        {
            for (var sequence = 0; sequence < pools.Length; sequence++)
            {
                var timestamp = Stopwatch.GetTimestamp();
                frames.Add(CreatePooledVideoFrame(frameType, nv12, sequence, timestamp, timestamp, 16, 16, 384, pools[sequence]));
            }

            InsertDecodedFrame(pipeline, pipelineType, reorderLock, reorderFrames, 2L, frames[2]);
            InsertDecodedFrame(pipeline, pipelineType, reorderLock, reorderFrames, 3L, frames[3]);
            lock (reorderLock)
            {
                InvokeNonPublicInstanceMethod(pipeline, "ForceDropOldestReorderFrameUnderLock", Array.Empty<object?>());
            }

            AssertEqual(0L, (long)GetPrivateField(pipeline, "_nextEmitSeq")!, "non-head eviction preserves earlier pending sequence");
            AssertEqual(true, knownMissing.SetEquals(new[] { 2L }), "non-head eviction registers its later gap");
            AssertEqual(1, (int)GetPrivateField(pipeline, "_reorderBufferDepth")!, "non-head eviction retains the surviving frame");
            AssertEqual(1, pools[2].ReturnCount, "non-head eviction returns its frame exactly once");
            AssertEqual(0, pools[3].ReturnCount, "non-head eviction retains ownership of the surviving frame");

            var consumedEarly = (bool)InvokeNonPublicInstanceMethod(pipeline, "ConsumeKnownMissingFrames", Array.Empty<object?>())!;
            AssertEqual(false, consumedEarly, "later gap cannot overtake earlier pending frames");
            AssertEqual(0L, (long)GetPrivateField(pipeline, "_nextEmitSeq")!, "early gap consumption preserves cursor");
            AssertEqual(0L, (long)GetPrivateField(pipeline, "_reorderSkips")!, "early gap consumption does not count a skip");
            AssertEqual(true, knownMissing.SetEquals(new[] { 2L }), "later gap remains pending until earlier frames arrive");

            // Deliver earlier work out of order, then exercise the actual emitter drain.
            InsertDecodedFrame(pipeline, pipelineType, reorderLock, reorderFrames, 1L, frames[1]);
            InsertDecodedFrame(pipeline, pipelineType, reorderLock, reorderFrames, 0L, frames[0]);
            var drained = (bool)InvokeNonPublicInstanceMethod(pipeline, "DrainReadyFrames", Array.Empty<object?>())!;
            AssertEqual(true, drained, "drain emits surviving frames and consumes the gap");
            AssertEqual("0,1,3", string.Join(",", emitted), "non-head eviction preserves emission order");
            AssertEqual(4L, (long)GetPrivateField(pipeline, "_nextEmitSeq")!, "drain advances past exactly the emitted frames and gap");
            AssertEqual(1L, (long)GetPrivateField(pipeline, "_reorderSkips")!, "non-head eviction counts one consumed skip");
            AssertEqual(3L, (long)GetPrivateField(pipeline, "_totalFramesEmitted")!, "non-head eviction emits all surviving frames");
            AssertEqual(1L, (long)GetPrivateField(pipeline, "_totalFramesDropped")!, "non-head eviction counts one dropped frame");
            AssertEqual(1L, (long)GetPrivateField(pipeline, "_reorderForceDrops")!, "non-head eviction counts one forced drop");
            AssertEqual(0, knownMissing.Count, "drain removes the consumed non-head gap");
            AssertEqual(0, (int)GetPrivateField(pipeline, "_reorderBufferDepth")!, "drain empties the reorder buffer");

            var drainedAgain = (bool)InvokeNonPublicInstanceMethod(pipeline, "DrainReadyFrames", Array.Empty<object?>())!;
            AssertEqual(false, drainedAgain, "empty drain does no further work");
            AssertEqual("0,1,3", string.Join(",", emitted), "empty drain does not emit frames twice");
            AssertEqual(4L, (long)GetPrivateField(pipeline, "_nextEmitSeq")!, "empty drain preserves cursor");
            AssertEqual(1L, (long)GetPrivateField(pipeline, "_reorderSkips")!, "empty drain preserves skip count");
            foreach (var pool in pools)
            {
                AssertEqual(1, pool.ReturnCount, "eviction and ordered drain return each frame exactly once");
            }
        }
        finally
        {
            foreach (var frame in frames)
            {
                ((IDisposable)frame).Dispose();
            }
        }

        return Task.CompletedTask;
    }

    internal static Task ParallelMjpegDecodePipeline_CompletionPreservesFrameOwnership(bool fatal)
    {
        var pipelineType = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline");
        var pipeline = RuntimeHelpers.GetUninitializedObject(pipelineType);
        var reorderLock = new object();
        var reorderFrames = CreateSortedDictionary(pipelineType);
        var knownMissing = fatal ? new SortedSet<long> { 5 } : new SortedSet<long> { 0, 3, 5 };
        var emitted = new List<long>();
        using var emitSignal = new AutoResetEvent(true);
        Action<object> collectFrame = frame => emitted.Add((long)GetPropertyValue(frame, "SequenceNumber")!);
        var callbackType = pipelineType.GetNestedType("EmitFrameCallback", BindingFlags.Public)!;

        SetPrivateField(pipeline, "_stopped", true);
        SetPrivateField(pipeline, "_workers", Array.Empty<Thread>());
        SetPrivateField(pipeline, "_reorderLock", reorderLock);
        SetPrivateField(pipeline, "_reorderFrames", reorderFrames);
        SetPrivateField(pipeline, "_knownMissingSequences", knownMissing);
        SetPrivateField(pipeline, "_emitSignal", emitSignal);
        SetPrivateField(pipeline, "_fatalErrorSignaled", fatal ? 1 : 0);
        SetPrivateField(pipeline, "_timingLock", new object());
        SetPrivateField(pipeline, "_reorderLatencyMs", new double[8]);
        SetPrivateField(pipeline, "_pipelineLatencyMs", new double[8]);
        SetPrivateField(pipeline, "_emitCallback", Delegate.CreateDelegate(callbackType, collectFrame.Target, collectFrame.Method));

        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var nv12 = Enum.Parse(formatType, "Nv12");
        var pools = new[] { new TrackingArrayPool(), new TrackingArrayPool() };
        var frames = new List<object>();
        try
        {
            for (var index = 0; index < pools.Length; index++)
            {
                var timestamp = Stopwatch.GetTimestamp();
                frames.Add(CreatePooledVideoFrame(frameType, nv12, index + 1, timestamp, timestamp, 16, 16, 384, pools[index]));
            }

            InsertDecodedFrame(pipeline, pipelineType, reorderLock, reorderFrames, 2L, frames[1]);
            InsertDecodedFrame(pipeline, pipelineType, reorderLock, reorderFrames, 1L, frames[0]);
            InvokeNonPublicInstanceMethod(pipeline, "EmitLoop", Array.Empty<object?>());

            AssertEqual(fatal ? string.Empty : "1,2", string.Join(",", emitted), "completion emits only the surviving ordered frames");
            AssertEqual(fatal ? 0L : 4L, (long)GetPrivateField(pipeline, "_nextEmitSeq")!, "completion advances past emitted frames and contiguous gaps only");
            AssertEqual(fatal ? 0L : 2L, (long)GetPrivateField(pipeline, "_reorderSkips")!, "fatal discard does not consume missing sequences");
            AssertEqual(fatal ? 0L : 2L, (long)GetPrivateField(pipeline, "_totalFramesEmitted")!, "completion emission count");
            AssertEqual(fatal ? 2L : 0L, (long)GetPrivateField(pipeline, "_totalFramesDropped")!, "completion discard count");
            AssertEqual(0L, (long)GetPrivateField(pipeline, "_emitFailures")!, "completion callback did not fail");
            AssertEqual(0, (int)GetPrivateField(pipeline, "_reorderBufferDepth")!, "completion resets reorder depth");
            AssertEqual(0, ((IDictionary)reorderFrames).Count, "completion retains no frames");
            AssertEqual(0, knownMissing.Count, "completion clears residual missing-sequence metadata");
            foreach (var pool in pools)
            {
                AssertEqual(1, pool.ReturnCount, "completion returns every frame exactly once");
            }
        }
        finally
        {
            foreach (var frame in frames)
            {
                ((IDisposable)frame).Dispose();
            }
        }

        return Task.CompletedTask;
    }

    internal static Task ParallelMjpegDecodePipeline_NormalCompletionConsumesFinalMissingSequences()
    {
        var pipelineType = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline");
        var pipeline = RuntimeHelpers.GetUninitializedObject(pipelineType);
        var knownMissing = new SortedSet<long> { 10, 11, 14 };
        SetPrivateField(pipeline, "_stopped", true);
        SetPrivateField(pipeline, "_workers", Array.Empty<Thread>());
        SetPrivateField(pipeline, "_reorderLock", new object());
        SetPrivateField(pipeline, "_reorderFrames", CreateSortedDictionary(pipelineType));
        SetPrivateField(pipeline, "_knownMissingSequences", knownMissing);
        SetPrivateField(pipeline, "_nextEmitSeq", 10L);
        SetPrivateField(pipeline, "_missingSeqSinceTickMs", 100L);

        InvokeNonPublicInstanceMethod(pipeline, "EmitLoop", Array.Empty<object?>());

        AssertEqual(12L, (long)GetPrivateField(pipeline, "_nextEmitSeq")!, "empty normal completion consumes its contiguous missing tail");
        AssertEqual(2L, (long)GetPrivateField(pipeline, "_reorderSkips")!, "empty normal completion counts each consumed gap once");
        AssertEqual(0, knownMissing.Count, "empty normal completion clears later residual metadata");
        AssertEqual(-1L, (long)GetPrivateField(pipeline, "_missingSeqSinceTickMs")!, "consumed terminal gaps clear the pending stall timer");
        AssertEqual(0L, (long)GetPrivateField(pipeline, "_totalFramesEmitted")!, "empty normal completion does not emit");
        AssertEqual(0L, (long)GetPrivateField(pipeline, "_totalFramesDropped")!, "empty normal completion does not discard frames");
        return Task.CompletedTask;
    }

    private static object CreateSortedDictionary(Type pipelineType)
    {
        var fieldType = pipelineType.GetField("_reorderFrames", BindingFlags.NonPublic | BindingFlags.Instance)?.FieldType
            ?? throw new InvalidOperationException("_reorderFrames field not found.");
        return Activator.CreateInstance(fieldType)
            ?? throw new InvalidOperationException("Could not create SortedDictionary.");
    }

    private static void InsertDecodedFrame(
        object pipeline,
        Type pipelineType,
        object reorderLock,
        object reorderFrames,
        long seqNo,
        object frame)
    {
        var decodedFrameType = pipelineType.GetNestedType("DecodedFrame", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DecodedFrame nested type not found.");
        var decodedFrame = Activator.CreateInstance(decodedFrameType, seqNo, frame, Stopwatch.GetTimestamp())
            ?? throw new InvalidOperationException("Could not create DecodedFrame.");
        var addMethod = reorderFrames.GetType().GetMethod("Add")
            ?? throw new InvalidOperationException("SortedDictionary.Add not found.");
        lock (reorderLock)
        {
            addMethod.Invoke(reorderFrames, new object[] { seqNo, decodedFrame });
            SetPrivateField(pipeline, "_reorderBufferDepth", ((IDictionary)reorderFrames).Count);
        }
    }

    internal static Task MjpegPreviewJitter_ExposesAdaptiveDeadlinePolicy()
    {
        var source = ReadRepoFile("Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs");
        var pipelineSource = ReadRepoFile("Sussudio/Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs");
        var captureSource = ReadUnifiedVideoCaptureSource();
        AssertContains(source, "DropDeadlineExpiredFrames");
        AssertContains(source, "DropLatencyOverflowFrames");
        AssertContains(source, "SoftDeadlineExtraFrames = 2");
        AssertContains(source, "AggressiveCatchUpSurplusFrames = 4");
        AssertContains(source, "IncreaseTargetDepth");
        AssertContains(source, "MaybeDecreaseTargetDepth");
        AssertContains(source, "HasLatencyPressure");
        AssertContains(source, "GetAdjustedOutputIntervalTicks");
        AssertContains(source, "private enum DequeueMissReason");
        AssertContains(source, "TryDequeueCore(out var dequeueMissReason)");
        AssertContains(source, "dequeueMissReason == DequeueMissReason.WaitingForSequence");
        AssertContains(source, "_signal.WaitOne(1);");
        AssertContains(source, "DeadlineDropCount");
        AssertContains(source, "TargetIncreaseCount");
        AssertContains(source, "TargetDecreaseCount");
        AssertContains(source, "LastSelectedPreviewPresentId");
        AssertContains(source, "LastSelectedSourceSequenceNumber");
        AssertContains(source, "RecordSelectedFrame");
        AssertContains(source, "RecordDroppedFrame");
        AssertContains(source, "ResetForPreviewSuppression");
        AssertContains(source, "ReprimeAfterPreviewResume");
        AssertContains(source, "TryRecordResumeReprimeMiss");
        AssertContains(source, "ResumeReprimeCount");
        AssertContains(source, "if (AddFrameInOrder(frame))");
        AssertContains(source, "private bool AddFrameInOrder(BufferedFrame frame)");
        AssertContains(source, "return false;");
        AssertContains(source, "SUSSUDIO_PREVIEW_JITTER_TARGET_DEPTH");
        AssertContains(source, "SUSSUDIO_PREVIEW_JITTER_MIN_TARGET_DEPTH");
        AssertContains(source, "SUSSUDIO_PREVIEW_JITTER_MAX_TARGET_DEPTH");
        AssertContains(source, "SUSSUDIO_PREVIEW_JITTER_MAX_DEPTH");
        AssertContains(source, "SUSSUDIO_PREVIEW_DISPLAY_CLOCK_PACING\", 0");
        AssertContains(source, "SUSSUDIO_PREVIEW_JITTER_MMCSS_TASK\") ?? \"Playback\"");
        AssertContains(pipelineSource, "PreviewFrameCallback");
        AssertContains(pipelineSource, "NotifyPreviewFrameDecoded");
        AssertContains(captureSource, "OnMjpegPipelinePreviewFrameDecoded");
        AssertContains(captureSource, "Volatile.Read(ref _mjpegPreviewJitterBuffer)?.ResetForPreviewSuppression()");
        AssertContains(captureSource, "Volatile.Read(ref _mjpegPreviewJitterBuffer)?.ReprimeAfterPreviewResume()");

        return Task.CompletedTask;
    }

    internal static Task MjpegPreviewJitter_EmitLoopLivesWithLifecycleRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs")
            .Replace("\r\n", "\n");
        var queueIngressText = rootText;
        var framePacingText = rootText;
        var metricsText = rootText;

        AssertDoesNotContain(rootText, "partial class MjpegPreviewJitterBuffer");
        AssertContains(queueIngressText, "private sealed class BufferedFrame : IDisposable");
        AssertContains(queueIngressText, "public void Enqueue(ReadOnlySpan<byte> nv12Data, int width, int height, long arrivalTick)");
        AssertContains(queueIngressText, "public void Enqueue(PooledVideoFrameLease frame)");
        AssertContains(queueIngressText, "private void EnqueueBufferedFrame(BufferedFrame frame)");
        AssertContains(queueIngressText, "private bool AddFrameInOrder(BufferedFrame frame)");
        AssertContains(queueIngressText, "private BufferedFrame RemoveOldestFrame()");
        AssertContains(queueIngressText, "private bool TryRecordResumeReprimeMiss(long nowTick)");
        AssertContains(rootText, "private void EmitLoop()");
        AssertContains(rootText, "MmcssThreadRegistration.TryRegister(_mmcssTask, _mmcssPriority");
        AssertContains(framePacingText, "private long AlignDueTickToDisplayClock(IPreviewFrameSink? sink, long currentDueTick, long nowTick)");
        AssertContains(framePacingText, "private void SubmitFrame(IPreviewFrameSink sink, BufferedFrame frame)");
        AssertContains(framePacingText, "private void WaitForTicks(long ticks)");
        AssertContains(framePacingText, "private static extern uint timeBeginPeriod(uint uPeriod);");
        AssertContains(framePacingText, "private static extern uint timeEndPeriod(uint uPeriod);");
        AssertContains(framePacingText, "private void DropDeadlineExpiredFrames(long nowTick)");
        AssertContains(framePacingText, "private void IncreaseTargetDepth(long nowTick)");
        AssertContains(framePacingText, "private bool HasLatencyPressure(long nowTick)");
        AssertContains(rootText, "private long AlignDueTickToDisplayClock(");
        AssertContains(rootText, "private void SubmitFrame(IPreviewFrameSink sink, BufferedFrame frame)");
        AssertContains(metricsText, "public Metrics GetMetrics()");
        AssertContains(metricsText, "private void RecordInputInterval(long nowTick)");
        AssertContains(metricsText, "private void RecordDroppedFrame(long sourceSequenceNumber, string reason)");

        return Task.CompletedTask;
    }

    internal static Task MjpegPreviewJitter_DropsSoftDeadlineOverflowToRecoverLatency()
    {
        var jitterType = RequireType("Sussudio.Services.Capture.MjpegPreviewJitterBuffer");
        var jitter = CreateUnstartedJitterBuffer(jitterType, targetDepth: 3);
        var frames = (IList)(GetPrivateField(jitter, "_frames")
            ?? throw new InvalidOperationException("Jitter frame list missing."));
        var bufferedFrameType = RequireNestedType(jitterType, "BufferedFrame");
        var now = Stopwatch.GetTimestamp();
        var staleTick = now - Stopwatch.Frequency;

        for (var i = 0; i < 5; i++)
        {
            frames.Add(CreateRawBufferedFrame(bufferedFrameType, staleTick + i));
        }

        InvokeNonPublicInstanceMethod(jitter, "DropLatencyOverflowFrames", new object?[] { now });

        AssertEqual(3, frames.Count, "soft deadline overflow leaves target depth");
        AssertEqual(2L, GetLongPrivateField(jitter, "_deadlineDropCount"), "soft deadline overflow drops");
        AssertEqual(2L, GetLongPrivateField(jitter, "_totalDropped"), "soft deadline overflow total drops");
        AssertEqual(3, GetIntPrivateField(jitter, "_targetDepth"), "soft deadline overflow does not increase latency target");

        foreach (IDisposable frame in frames)
        {
            frame.Dispose();
        }

        frames.Clear();
        return Task.CompletedTask;
    }

    internal static Task MjpegPreviewJitter_DropsExpiredFramesBelowTargetDepth()
    {
        var jitterType = RequireType("Sussudio.Services.Capture.MjpegPreviewJitterBuffer");
        var jitter = CreateUnstartedJitterBuffer(jitterType, targetDepth: 6);
        var frames = (IList)(GetPrivateField(jitter, "_frames")
            ?? throw new InvalidOperationException("Jitter frame list missing."));
        var bufferedFrameType = RequireNestedType(jitterType, "BufferedFrame");
        var now = Stopwatch.GetTimestamp();
        var staleTick = now - Stopwatch.Frequency;

        frames.Add(CreateRawBufferedFrame(bufferedFrameType, staleTick));
        frames.Add(CreateRawBufferedFrame(bufferedFrameType, staleTick + 1));

        InvokeNonPublicInstanceMethod(jitter, "DropDeadlineExpiredFrames", new object?[] { now });

        AssertEqual(0, frames.Count, "expired preview frames below target depth");
        AssertEqual(2L, GetLongPrivateField(jitter, "_deadlineDropCount"), "deadline drops");
        AssertEqual(2L, GetLongPrivateField(jitter, "_totalDropped"), "total drops");
        AssertEqual(7, GetIntPrivateField(jitter, "_targetDepth"), "adaptive target after deadline drop");
        AssertEqual(1L, GetLongPrivateField(jitter, "_targetIncreaseCount"), "target increase count");

        return Task.CompletedTask;
    }

    internal static Task MjpegPreviewJitter_SkipsMissingPreviewSequenceAfterDeadline()
    {
        var jitterType = RequireType("Sussudio.Services.Capture.MjpegPreviewJitterBuffer");
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");

        var jitter = CreateUnstartedJitterBuffer(jitterType, targetDepth: 6);
        SetPrivateField(jitter, "_nextPreviewSequence", 10L);
        var frames = (IList)(GetPrivateField(jitter, "_frames")
            ?? throw new InvalidOperationException("Jitter frame list missing."));
        var bufferedFrameType = RequireNestedType(jitterType, "BufferedFrame");
        var nv12 = Enum.Parse(formatType, "Nv12");
        var pool = new TrackingArrayPool();
        var owner = CreatePooledVideoFrame(frameType, nv12, 12L, 100L, 200L, 16, 16, 384, pool);
        var lease = addLeaseMethod.Invoke(owner, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");
        ((IDisposable)owner).Dispose();

        frames.Add(CreateLeaseBufferedFrame(bufferedFrameType, lease, Stopwatch.GetTimestamp() - Stopwatch.Frequency));

        var dequeued = InvokeNonPublicInstanceMethod(jitter, "TryDequeue", null)
            ?? throw new InvalidOperationException("Expected jitter buffer to dequeue the first available frame after deadline.");

        AssertEqual(0, frames.Count, "remaining preview frame count");
        AssertEqual(13L, GetLongPrivateField(jitter, "_nextPreviewSequence"), "next preview sequence");
        AssertEqual(2L, GetLongPrivateField(jitter, "_deadlineDropCount"), "virtual deadline skips");
        AssertEqual(2L, GetLongPrivateField(jitter, "_totalDropped"), "total preview skips");
        AssertEqual(1L, GetLongPrivateField(jitter, "_targetIncreaseCount"), "target increase count after missing sequence");

        ((IDisposable)dequeued).Dispose();
        AssertEqual(1, pool.ReturnCount, "dequeued preview lease return count");

        return Task.CompletedTask;
    }

    internal static Task MjpegPreviewJitter_LateSequenceDoesNotCountAsQueued()
    {
        var jitterType = RequireType("Sussudio.Services.Capture.MjpegPreviewJitterBuffer");
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");

        var jitter = CreateUnstartedJitterBuffer(jitterType, targetDepth: 3);
        SetPrivateField(jitter, "_nextPreviewSequence", 10L);
        var frames = (IList)(GetPrivateField(jitter, "_frames")
            ?? throw new InvalidOperationException("Jitter frame list missing."));
        var bufferedFrameType = RequireNestedType(jitterType, "BufferedFrame");
        var nv12 = Enum.Parse(formatType, "Nv12");
        var pool = new TrackingArrayPool();
        var owner = CreatePooledVideoFrame(frameType, nv12, 9L, 100L, 200L, 16, 16, 384, pool);
        var lease = addLeaseMethod.Invoke(owner, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");
        ((IDisposable)owner).Dispose();

        var lateFrame = CreateLeaseBufferedFrame(bufferedFrameType, lease, Stopwatch.GetTimestamp());
        InvokeNonPublicInstanceMethod(jitter, "EnqueueBufferedFrame", new[] { lateFrame });

        AssertEqual(0, frames.Count, "late preview sequence was not queued");
        AssertEqual(0L, GetLongPrivateField(jitter, "_totalQueued"), "late preview sequence does not increment queued count");
        AssertEqual(1L, GetLongPrivateField(jitter, "_totalDropped"), "late preview sequence increments dropped count");
        AssertEqual(1L, GetLongPrivateField(jitter, "_deadlineDropCount"), "late preview sequence increments deadline drop count");
        AssertEqual(1, pool.ReturnCount, "late preview sequence returns frame lease");

        return Task.CompletedTask;
    }

    internal static Task MjpegPreviewJitter_ClearResetsPreviewSequence()
    {
        var jitterType = RequireType("Sussudio.Services.Capture.MjpegPreviewJitterBuffer");
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");

        var jitter = CreateUnstartedJitterBuffer(jitterType, targetDepth: 3);
        SetPrivateField(jitter, "_nextPreviewSequence", 10L);
        var frames = (IList)(GetPrivateField(jitter, "_frames")
            ?? throw new InvalidOperationException("Jitter frame list missing."));
        var bufferedFrameType = RequireNestedType(jitterType, "BufferedFrame");
        var nv12 = Enum.Parse(formatType, "Nv12");
        var pool = new TrackingArrayPool();

        var clearedOwner = CreatePooledVideoFrame(frameType, nv12, 10L, 100L, 200L, 16, 16, 384, pool);
        var clearedLease = addLeaseMethod.Invoke(clearedOwner, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");
        ((IDisposable)clearedOwner).Dispose();
        frames.Add(CreateLeaseBufferedFrame(bufferedFrameType, clearedLease, Stopwatch.GetTimestamp()));

        jitterType.GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance)!
            .Invoke(jitter, Array.Empty<object>());

        AssertEqual(0, frames.Count, "clear drains queued preview frames");
        AssertEqual(-1L, GetLongPrivateField(jitter, "_nextPreviewSequence"), "clear resets preview sequence");
        AssertEqual(1, pool.ReturnCount, "cleared preview lease return count");

        var resumedOwner = CreatePooledVideoFrame(frameType, nv12, 100L, 300L, 400L, 16, 16, 384, pool);
        var resumedLease = addLeaseMethod.Invoke(resumedOwner, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");
        ((IDisposable)resumedOwner).Dispose();
        frames.Add(CreateLeaseBufferedFrame(bufferedFrameType, resumedLease, Stopwatch.GetTimestamp()));

        var dequeued = InvokeNonPublicInstanceMethod(jitter, "TryDequeue", null)
            ?? throw new InvalidOperationException("Expected jitter buffer to accept the first frame after clear.");

        AssertEqual(0L, GetLongPrivateField(jitter, "_underflowCount"), "clear resume does not create underflows");
        AssertEqual(0L, GetLongPrivateField(jitter, "_deadlineDropCount"), "clear resume does not create deadline skips");
        AssertEqual(101L, GetLongPrivateField(jitter, "_nextPreviewSequence"), "next preview sequence after resume");

        ((IDisposable)dequeued).Dispose();
        AssertEqual(2, pool.ReturnCount, "resumed preview lease return count");

        return Task.CompletedTask;
    }

    internal static Task MjpegPreviewJitter_ReprimesAfterSuppressionResume()
    {
        var jitterType = RequireType("Sussudio.Services.Capture.MjpegPreviewJitterBuffer");
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var formatType = RequireType("Sussudio.Services.Contracts.PooledVideoPixelFormat");
        var addLeaseMethod = frameType.GetMethod("AddLease", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PooledVideoFrame.AddLease not found.");

        var jitter = CreateUnstartedJitterBuffer(jitterType, targetDepth: 3);
        SetPrivateField(jitter, "_nextPreviewSequence", 10L);
        var frames = (IList)(GetPrivateField(jitter, "_frames")
            ?? throw new InvalidOperationException("Jitter frame list missing."));
        var bufferedFrameType = RequireNestedType(jitterType, "BufferedFrame");
        var nv12 = Enum.Parse(formatType, "Nv12");
        var pool = new TrackingArrayPool();

        var staleOwner = CreatePooledVideoFrame(frameType, nv12, 10L, 100L, 200L, 16, 16, 384, pool);
        var staleLease = addLeaseMethod.Invoke(staleOwner, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");
        ((IDisposable)staleOwner).Dispose();
        frames.Add(CreateLeaseBufferedFrame(bufferedFrameType, staleLease, Stopwatch.GetTimestamp()));

        jitterType.GetMethod("ResetForPreviewSuppression", BindingFlags.Public | BindingFlags.Instance)!
            .Invoke(jitter, Array.Empty<object>());

        AssertEqual(0, frames.Count, "suppression drains queued preview frames");
        AssertEqual(-1L, GetLongPrivateField(jitter, "_nextPreviewSequence"), "suppression resets preview sequence");
        AssertEqual("suppressed", GetStringPrivateField(jitter, "_lastDropReason"), "suppression drop reason");
        AssertEqual(1, pool.ReturnCount, "suppressed preview lease return count");

        var raceOwner = CreatePooledVideoFrame(frameType, nv12, 11L, 300L, 400L, 16, 16, 384, pool);
        var raceLease = addLeaseMethod.Invoke(raceOwner, Array.Empty<object>())
            ?? throw new InvalidOperationException("AddLease returned null.");
        ((IDisposable)raceOwner).Dispose();
        jitterType.GetMethod("Enqueue", new[] { raceLease.GetType() })!
            .Invoke(jitter, new[] { raceLease });

        AssertEqual(0, frames.Count, "suppressed enqueue is rejected");
        AssertEqual(2, pool.ReturnCount, "suppressed enqueue returns raced preview lease");

        jitterType.GetMethod("ReprimeAfterPreviewResume", BindingFlags.Public | BindingFlags.Instance)!
            .Invoke(jitter, Array.Empty<object>());

        var now = Stopwatch.GetTimestamp();
        AssertEqual(
            true,
            (bool)(InvokeNonPublicInstanceMethod(jitter, "TryRecordResumeReprimeMiss", new object?[] { now })
                   ?? throw new InvalidOperationException("TryRecordResumeReprimeMiss returned null.")),
            "first resume miss is reclassified");
        AssertEqual(1L, GetLongPrivateField(jitter, "_resumeReprimeCount"), "resume reprime count");
        AssertEqual(0L, GetLongPrivateField(jitter, "_underflowCount"), "resume reprime does not increment underflows");
        AssertEqual("resume-reprime", GetStringPrivateField(jitter, "_lastUnderflowReason"), "resume reprime reason");
        AssertEqual(
            false,
            (bool)(InvokeNonPublicInstanceMethod(jitter, "TryRecordResumeReprimeMiss", new object?[] { now })
                   ?? throw new InvalidOperationException("TryRecordResumeReprimeMiss returned null.")),
            "resume reprime budget is single-use");
        SetPrivateField(jitter, "_resumeReprimeMissBudget", 1);
        SetPrivateField(jitter, "_resumeReprimeStartTick", now - Stopwatch.Frequency);
        AssertEqual(
            false,
            (bool)(InvokeNonPublicInstanceMethod(jitter, "TryRecordResumeReprimeMiss", new object?[] { now })
                   ?? throw new InvalidOperationException("TryRecordResumeReprimeMiss returned null.")),
            "stale resume reprime budget does not mask later underflows");
        AssertEqual(1L, GetLongPrivateField(jitter, "_resumeReprimeCount"), "stale reprime does not increment count");

        return Task.CompletedTask;
    }
}
