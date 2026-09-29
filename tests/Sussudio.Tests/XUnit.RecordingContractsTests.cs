using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

using System.Text.Json;

namespace Sussudio.Tests
{

public sealed class RecordingPipelineContractsTests
{
    public RecordingPipelineContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task RecordingVideoTryEnqueuePathsDoNotBlockCaptureCallbacks()
        => global::Program.RecordingVideoTryEnqueuePaths_DoNotBlockCaptureCallbacks();

    [Fact]
    public Task WasapiAudioCaptureRejectsIncompleteHotAudioWrites()
        => global::Program.WasapiAudioCapture_HotAudioWritesRejectIncompleteTasks();
}

public sealed class LibAvRecordingResetTests
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Fact]
    public async Task SessionResetClearsSeededVideoMetricsAndPreservesUnrelatedAudioState()
    {
        var assembly = SussudioAssembly.Load();
        var sinkType = assembly.GetType("Sussudio.Services.Recording.LibAvRecordingSink", throwOnError: true)!;
        await using var sink = (IAsyncDisposable)Activator.CreateInstance(sinkType)!;
        var fields = new[]
        {
            "_droppedVideoFrames", "_encodedVideoFrames", "_videoFramesEnqueued",
            "_videoFramesSubmittedToEncoder", "_videoDropsQueueSaturated", "_videoDropsBacklogEviction",
            "_gpuFramesEnqueued", "_gpuFramesDropped", "_cudaFramesEnqueued", "_cudaFramesDropped",
            "_videoQueueMaxDepth", "_gpuQueueMaxDepth", "_cudaQueueMaxDepth",
            "_videoQueueDepth", "_gpuQueueDepth", "_cudaQueueDepth",
            "_lastVideoEnqueueTick", "_lastVideoWriteTick"
        };
        foreach (var name in fields)
        {
            var field = Field(sink, name);
            field.SetValue(sink, Convert.ChangeType(17, field.FieldType));
            Assert.Equal(17L, Convert.ToInt64(field.GetValue(sink)));
        }
        Field(sink, "_audioDropsQueueSaturated").SetValue(sink, 91L);

        var tracker = Field(sink, "_videoLatencyTracker").GetValue(sink)!;
        var queuedTicks = (ICollection)Field(tracker, "_enqueueTicks").GetValue(tracker)!;
        var tick = Environment.TickCount64;
        lock (Field(sink, "_videoQueueSync").GetValue(sink)!)
        {
            Invoke(tracker, "TrackEnqueueUnderLock", tick - 50);
        }
        Invoke(tracker, "RecordPacketDequeued", tick - 30, (long?)10);
        Invoke(tracker, "RecordPacketDequeued", tick - 20, (long?)14);
        Invoke(tracker, "RecordBackpressure", 100L, 109L);
        Assert.Single(queuedTicks);
        Assert.Equal(3L, Property(sink, "VideoSequenceGaps"));
        Assert.Equal(1L, Property(sink, "VideoBackpressureEvents"));
        Assert.Equal(2, ((ValueTuple<int, double, double, double, double>)Property(sink, "VideoQueueLatencyMetrics")).Item1);

        var contextType = assembly.GetType("Sussudio.Services.Contracts.RecordingContext", throwOnError: true)!;
        var context = Activator.CreateInstance(contextType)!;
        contextType.GetProperty("EffectiveWidth")!.SetValue(context, 640u);
        contextType.GetProperty("EffectiveHeight")!.SetValue(context, 360u);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            Invoke(sink, "ResetVideoSessionState", context);

            Assert.Equal(640, Field(sink, "_width").GetValue(sink));
            Assert.Equal(360, Field(sink, "_height").GetValue(sink));
            foreach (var name in fields)
            {
                Assert.Equal(0L, Convert.ToInt64(Field(sink, name).GetValue(sink)));
            }
            foreach (var name in new[]
            {
                "DroppedVideoFrames", "EncodedVideoFrames", "VideoFramesEnqueuedCount", "VideoFramesSubmittedToEncoder",
                "VideoQueueCount", "VideoQueueMaxDepth", "GpuQueueCount", "GpuQueueMaxDepth", "CudaQueueCount", "CudaQueueMaxDepth",
                "GpuFramesEnqueued", "GpuFramesDropped", "CudaFramesEnqueued", "CudaFramesDropped",
                "LastVideoEnqueueTick", "LastVideoWriteTick", "LastVideoQueueLatencyMs", "VideoSequenceGaps",
                "VideoBackpressureWaitMs", "VideoBackpressureEvents", "LastVideoBackpressureWaitMs", "MaxVideoBackpressureWaitMs"
            })
            {
                Assert.Equal(0L, Convert.ToInt64(Property(sink, name)));
            }
            Assert.Equal((0, 0d, 0d, 0d, 0d), Property(sink, "VideoQueueLatencyMetrics"));
            Assert.Empty(queuedTicks);
            Assert.Equal(-1L, Property(tracker, "LastSequenceNumber"));
            Assert.Equal(91L, Property(sink, "AudioDropsQueueSaturated"));
        }

        Invoke(tracker, "RecordPacketDequeued", Environment.TickCount64, (long?)100);
        Assert.Equal(0L, Property(sink, "VideoSequenceGaps"));
        Assert.Equal(100L, Property(tracker, "LastSequenceNumber"));
    }

    private static FieldInfo Field(object target, string name)
        => target.GetType().GetField(name, InstanceFlags)
            ?? throw new InvalidOperationException($"{target.GetType().Name}.{name} was not found.");

    private static object Property(object target, string name)
        => target.GetType().GetProperty(name, InstanceFlags)!.GetValue(target)!;

    private static void Invoke(object target, string name, params object?[] arguments)
        => target.GetType().GetMethod(name, InstanceFlags)!.Invoke(target, arguments);
}

public sealed class CoreRuntimeRecordingContractsTests
{
    public CoreRuntimeRecordingContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public async Task RecordingVerifierFailsWhenOutputFileIsMissing()
    {
        var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
        var verifier = Activator.CreateInstance(verifierType)!;

        var result = await VerifyAsync(verifier, "/nonexistent/file.mp4", BuildRuntimeSnapshotForVerification());

        Assert.False(GetBoolProperty(result, "Succeeded"));
        Assert.False(GetBoolProperty(result, "FileExists"));
        Assert.Contains("does not exist", GetStringProperty(result, "Message"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordingVerifierFailsWhenOutputFileIsEmpty()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_test_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, Array.Empty<byte>());
        try
        {
            var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
            var verifier = Activator.CreateInstance(verifierType)!;

            var result = await VerifyAsync(verifier, tempFile, BuildRuntimeSnapshotForVerification());

            Assert.False(GetBoolProperty(result, "Succeeded"));
            Assert.Contains("output-empty", GetStringProperty(result, "PrimaryMismatchCode"), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            global::Program.TryDeleteFile(tempFile);
        }
    }

    [Fact]
    public async Task RecordingVerifierFailsWhenOutputPathIsNull()
    {
        var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
        var verifier = Activator.CreateInstance(verifierType)!;

        var result = await VerifyAsync(verifier, null, BuildRuntimeSnapshotForVerification());

        Assert.False(GetBoolProperty(result, "Succeeded"));
    }

    [Fact]
    public void RecordingVerifierIsTheConcreteDiagnosticsHubDependency()
    {
        var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");

        var verifyAsync = verifierType.GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(verifyAsync);

        var parameters = verifyAsync!.GetParameters();
        Assert.Equal(3, parameters.Length);

        var resultType = RequireType("Sussudio.Models.RecordingVerificationResult");
        Assert.True(verifyAsync.ReturnType.IsGenericType);
        Assert.Equal(resultType, verifyAsync.ReturnType.GetGenericArguments()[0]);

        var diagnosticsHubType = RequireType("Sussudio.Services.Automation.AutomationDiagnosticsHub");
        var diagnosticsHubConstructors = diagnosticsHubType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var diagnosticsHubConstructor = Array.Find(
            diagnosticsHubConstructors,
            constructor => Array.Exists(
                constructor.GetParameters(),
                parameter => parameter.Name == "recordingVerifier"));
        Assert.NotNull(diagnosticsHubConstructor);

        var verifierParameter = Array.Find(
            diagnosticsHubConstructor!.GetParameters(),
            parameter => parameter.Name == "recordingVerifier");
        Assert.NotNull(verifierParameter);
        Assert.Equal(verifierType, verifierParameter!.ParameterType);

        var verifierField = diagnosticsHubType.GetField("_recordingVerifier", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(verifierField);
        Assert.Equal(verifierType, verifierField!.FieldType);
    }

    [Fact]
    public Task RecordingVerificationResultExposesExpectedProperties()
        => global::Program.RecordingVerificationResult_HasExpectedProperties();

    [Fact]
    public Task RecordingVerifierFailsWhenFfprobeIsUnavailable()
        => global::Program.RecordingVerifier_ReturnsFailure_WhenFfprobeUnavailable();

    [Fact]
    public Task RecordingVerifierRunsFfprobeBelowNormalPriority()
        => global::Program.RecordingVerifier_RunsFfprobeBelowNormalPriority();

    [Fact]
    public Task RecordingVerifierPassesHevcWhenAllFieldsMatch()
        => global::Program.RecordingVerifier_PassesVerification_WhenAllFieldsMatch_Hevc();

    [Fact]
    public Task RecordingVerifierDetectsH264CodecWhenHevcIsExpected()
        => global::Program.RecordingVerifier_DetectsCodecMismatch_WhenH264InsteadOfHevc();

    [Fact]
    public Task RecordingVerifierUsesFlashbackExportVerificationFormat()
        => global::Program.RecordingVerifier_UsesFlashbackExportVerificationFormat();

    [Fact]
    public Task RecordingVerifierUsesFlashbackRecordingVerificationFormat()
        => global::Program.RecordingVerifier_UsesFlashbackRecordingVerificationFormat();

    [Fact]
    public Task RecordingVerifierDetectsResolutionMismatch()
        => global::Program.RecordingVerifier_DetectsResolutionMismatch();

    [Fact]
    public Task RecordingVerifierDetectsFrameRateMismatch()
        => global::Program.RecordingVerifier_DetectsFrameRateMismatch();

    [Fact]
    public Task RecordingVerifierPassesHdrValidationWhenMetadataIsPresent()
        => global::Program.RecordingVerifier_PassesHdrValidation_WhenAllHdrFieldsPresent();

    [Fact]
    public Task RecordingVerifierDetectsHdrColorimetryMismatch()
        => global::Program.RecordingVerifier_DetectsHdrColorimetryMismatch();

    [Fact]
    public Task RecordingVerifierPassesH264Format()
        => global::Program.RecordingVerifier_PassesVerification_ForH264Format();

    [Fact]
    public Task RecordingVerifierToleratesNtscFrameRateDrift()
        => global::Program.RecordingVerifier_PassesNtscFrameRateWithinTolerance();

    [Fact]
    public Task RecordingVerifierFailsWhenFfprobeExitsNonzero()
        => global::Program.RecordingVerifier_ReturnsFailure_WhenFfprobeExitsNonZero();

    [Fact]
    public Task LibAvEncoderHdrBitstreamFiltersMapCodecs()
        => global::Program.LibAvEncoder_GetHdrBitstreamFilterName_MapsCodecs();

    [Fact]
    public Task LibAvEncoderChainsHdrAndMpegTsBitstreamFilters()
        => global::Program.LibAvEncoder_VideoBitstreamFilterSpec_ChainsHdrAndMpegTsFilters();

    [Fact]
    public Task PooledVideoFrameSizesMatchPixelFormats()
        => global::Program.PooledVideoFrame_GetFrameSizeBytes_CalculatesCorrectly();

    [Fact]
    public Task LibAvEncoderNvencPresetsMapCorrectly()
        => global::Program.LibAvEncoder_MapNvencPreset_MapsCorrectly();

    [Fact]
    public Task LibAvEncoderRetainsAudioEvidenceAfterNativeCleanup()
        => global::Program.LibAvEncoder_RetainsAudioEvidenceAfterNativeCleanup();

    [Fact]
    public Task LibAvEncoderThrowsOnNegativeNativeErrors()
        => global::Program.LibAvEncoder_ThrowIfError_ThrowsOnNegative();

    [Fact]
    public Task LibAvEncoderRationalInversionSwapsNumeratorAndDenominator()
        => global::Program.LibAvEncoder_Invert_SwapsNumeratorDenominator();

    [Fact]
    public Task LibAvEncoderHdrRationalsParseCorrectly()
        => global::Program.LibAvEncoder_ChromaticityAndLuminanceRationals_ParseCorrectly();

    [Fact]
    public Task LibAvEncoderAcceptsValidOptions()
        => global::Program.LibAvEncoder_ValidateOptions_AcceptsValidOptions();

    [Fact]
    public Task LibAvEncoderRejectsEmptyOutputPath()
        => global::Program.LibAvEncoder_ValidateOptions_RejectsEmptyOutputPath();

    [Fact]
    public Task LibAvEncoderRejectsZeroDimensions()
        => global::Program.LibAvEncoder_ValidateOptions_RejectsZeroDimensions();

    [Fact]
    public Task LibAvEncoderRejectsHdrWithH264()
        => global::Program.LibAvEncoder_ValidateOptions_RejectsHdrWithH264();

    [Fact]
    public Task LibAvEncoderRejectsHdrWithoutP010()
        => global::Program.LibAvEncoder_ValidateOptions_RejectsHdrWithoutP010();

    [Fact]
    public Task LibAvEncoderRejectsMismatchedFrameRateParts()
        => global::Program.LibAvEncoder_ValidateOptions_RejectsMismatchedFrameRateParts();

    [Fact]
    public Task LibAvEncoderPublishesSuppliedMuxPhaseTimingSnapshotValues()
        => global::Program.LibAvEncoder_MuxPhaseTimingSnapshot_PublishesSuppliedValues();

    [Fact]
    public Task SharedFormatterRendersRecordingIntegrity()
        => global::Program.SharedFormatter_RendersRecordingIntegrity();

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static async Task<object> VerifyAsync(object verifier, string? outputPath, object snapshot)
    {
        var verifyAsync = verifier.GetType().GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("VerifyAsync not found.");
        var task = verifyAsync.Invoke(verifier, new object?[] { outputPath, snapshot, CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("VerifyAsync did not return Task.");

        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static object BuildRuntimeSnapshotForVerification()
    {
        var snapshot = RuntimeHelpers.GetUninitializedObject(RequireType("Sussudio.Models.CaptureRuntimeSnapshot"));
        SetPropertyOrBackingField(snapshot, "RequestedFormat", "HevcMp4");
        SetPropertyOrBackingField(snapshot, "RequestedHdrEnabled", (bool?)false);
        SetPropertyOrBackingField(snapshot, "NegotiatedWidth", (uint?)1920);
        SetPropertyOrBackingField(snapshot, "NegotiatedHeight", (uint?)1080);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateNumerator", (uint?)60);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateDenominator", (uint?)1);
        return snapshot;
    }

    private static void SetPropertyOrBackingField(object instance, string name, object? value)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = instance.GetType().GetField($"<{name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{instance.GetType().Name}.{name} backing field not found.");
        field.SetValue(instance, value);
    }

    private static object? GetPropertyValue(object instance, string name)
        => instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!.GetValue(instance);

    private static bool GetBoolProperty(object instance, string name)
        => (bool)GetPropertyValue(instance, name)!;

    private static string GetStringProperty(object instance, string name)
        => (string)GetPropertyValue(instance, name)!;
}

public sealed class RecordingLifecycleBehaviorTests
{
    public RecordingLifecycleBehaviorTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [HevcP010Fact]
    [Trait("Hardware", "NVENC")]
    public Task CaptureServiceRecordsAndFinalizesP010()
    {
        HevcP010Capability.RequireAvailable();
        return LibAvRecordingDrainBehaviorTests.VerifyCaptureServiceP010LifecycleAsync();
    }

    [Fact]
    public Task CaptureServiceRejectsP010NegotiationMismatchAndRollsBack()
        => LibAvRecordingDrainBehaviorTests.VerifyCaptureServiceP010MismatchRollbackAsync();
}

public sealed class RecordingModelContractsTests
{
    public RecordingModelContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task LibAvRecordingDrainLoopInterleavesAudioWithBoundedVideoBatches()
        => global::Program.LibAvRecordingSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches();

    [HevcP010Fact]
    [Trait("Hardware", "NVENC")]
    public Task LibAvRecordingP010EncodeRoundTrip()
    {
        HevcP010Capability.RequireAvailable();
        return global::Program.LibAvRecordingSink_P010EncodeRoundTrip();
    }

    [Fact]
    public Task StrictHfrFatalHandlerClearsActiveSessionState()
        => global::Program.CaptureService_StrictHfrFatalHandler_ClearsActiveSessionState();

    [Fact]
    public Task FlashbackBufferManagerInitializeClearsRecordingPts()
        => global::Program.FlashbackBufferManager_InitializeClearsRecordingPts();

    [Fact]
    public void FlashbackBufferManagerInitializationDeletesStaleExportTemps()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_init_cleanup_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? manager = null;

        try
        {
            var staleTemp = Path.Combine(tempDir, "abandoned.mp4.tmp");
            File.WriteAllText(staleTemp, "stale");
            File.SetLastWriteTimeUtc(staleTemp, DateTime.UtcNow - TimeSpan.FromHours(1));

            var assembly = SussudioAssembly.Load();
            var optionsType = assembly.GetType("Sussudio.Models.FlashbackBufferOptions", throwOnError: true)!;
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetBackingField(options, "TempDirectory", tempDir);
            SetBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = assembly.GetType(
                "Sussudio.Services.Flashback.FlashbackBufferManager",
                throwOnError: true)!;
            manager = Activator.CreateInstance(managerType, new[] { options })
                ?? throw new InvalidOperationException("FlashbackBufferManager construction failed.");
            managerType.GetMethod("Initialize")!.Invoke(manager, new object[] { "orphan-cleanup" });

            Assert.False(File.Exists(staleTemp));
        }
        finally
        {
            (manager as IDisposable)?.Dispose();
            global::Program.TryDeleteDirectory(tempDir);
        }

        static void SetBackingField(object instance, string name, object value)
        {
            var field = instance.GetType().GetField(
                $"<{name}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"{instance.GetType().Name}.{name} backing field not found.");
            field.SetValue(instance, value);
        }
    }

    [Fact]
    public Task FlashbackBufferManagerSegmentLookupReturnsCorrectFileForPosition()
        => global::Program.FlashbackBufferManager_GetSegmentFileForPosition_ReturnsCorrectSegment();

    [Fact]
    public Task FlashbackBufferManagerSegmentCompletionRejectsInvalidMetadata()
        => global::Program.FlashbackBufferManager_SegmentCompletionRejectsInvalidMetadata();

    [Fact]
    public Task FlashbackBufferManagerSegmentCompletionRejectsOutsidePaths()
        => global::Program.FlashbackBufferManager_SegmentCompletionRejectsOutsidePaths();

    [Fact]
    public Task FlashbackBufferManagerDeleteHelperRejectsOutsidePaths()
        => global::Program.FlashbackBufferManager_TryDeleteFileRejectsOutsidePaths();

    [Fact]
    public Task FlashbackBufferManagerSegmentRotationKeepsTotalBytesWrittenMonotonic()
        => global::Program.FlashbackBufferManager_SegmentRotationKeepsTotalBytesWrittenMonotonic();

    [Fact]
    public Task FlashbackBufferManagerSamePathCompletionExtendsLatestSegment()
        => global::Program.FlashbackBufferManager_SamePathCompletionExtendsLatestSegment();

    [Fact]
    public Task FlashbackBufferManagerIgnoresUpdatesAfterDispose()
        => global::Program.FlashbackBufferManager_IgnoresUpdatesAfterDispose();

    [Fact]
    public Task FlashbackBufferManagerIgnoresDestructiveOperationsAfterDispose()
        => global::Program.FlashbackBufferManager_IgnoresDestructiveOperationsAfterDispose();

    [Fact]
    public Task FlashbackBufferManagerValidSegmentLookupSkipsMissingFiles()
        => global::Program.FlashbackBufferManager_ResolvePlaybackSegmentPathWithFallback_SkipsMissingFiles();

    [Fact]
    public Task FlashbackBufferManagerStaleLeftEdgeLookupUsesOldestSegment()
        => global::Program.FlashbackBufferManager_ResolvePlaybackSegmentPathWithFallback_StaleLeftEdgeUsesOldest();

    [Fact]
    public Task FlashbackBufferManagerGetNextSegmentFileWalksForwardThroughSegments()
        => global::Program.FlashbackBufferManager_GetNextSegmentFile_WalksForward();

    [Fact]
    public Task FlashbackBufferManagerSegmentPathLookupsNormalizeEquivalentPaths()
        => global::Program.FlashbackBufferManager_SegmentPathLookupsNormalizeEquivalentPaths();

    [Fact]
    public Task FlashbackBufferManagerSegmentStartPtsSkipsMissingFiles()
        => global::Program.FlashbackBufferManager_GetSegmentStartPts_SkipsMissingFiles();

    [Fact]
    public Task FlashbackBufferManagerGetNextSegmentFileSkipsMissingIndexedSegments()
        => global::Program.FlashbackBufferManager_GetNextSegmentFile_SkipsMissingIndexedSegments();

    [Fact]
    public Task FlashbackBufferManagerGetExistingCompletedSegmentPathsInRangeReturnsOverlappingSegments()
        => global::Program.FlashbackBufferManager_GetExistingCompletedSegmentPathsInRange_ReturnsOverlapping();

    [Fact]
    public Task FlashbackBufferManagerSegmentInfoSkipsMissingFiles()
        => global::Program.FlashbackBufferManager_GetSegmentInfoList_SkipsMissingFiles();

    [Fact]
    public Task FlashbackBufferManagerActiveFilePathRequiresExistingFile()
        => global::Program.FlashbackBufferManager_ActiveFilePath_RequiresExistingFile();

    [Fact]
    public Task FlashbackBufferManagerSegmentCountSkipsMissingFiles()
        => global::Program.FlashbackBufferManager_SegmentCount_SkipsMissingFiles();

    [Fact]
    public Task FlashbackBufferManagerEvictionUpdatesDiskByteTotals()
        => global::Program.FlashbackBufferManager_EvictOldestSegments_UpdatesTotalDiskBytes();

    [Fact]
    public Task FlashbackBufferManagerEvictionKeepsRejectedSegmentsAccounted()
        => global::Program.FlashbackBufferManager_EvictOldestSegments_KeepsRejectedSegmentsAccounted();

    [Fact]
    public Task FlashbackBufferManagerEvictionPauseAndResumeAreBalanced()
        => global::Program.FlashbackBufferManager_EvictionPauseResume_Balanced();

    [Fact]
    public Task FlashbackBufferManagerAbandonsStartupGeneratedSegmentPaths()
        => global::Program.FlashbackBufferManager_AbandonsStartupGeneratedSegmentPath();

    [Fact]
    public Task FlashbackBufferManagerPurgesRetainLockedActiveSegmentPath()
        => global::Program.FlashbackBufferManager_PurgesRetainLockedActivePath();

    [Fact]
    public Task FlashbackBufferManagerPartialPurgeAccountsForDeletedActiveSegment()
        => global::Program.FlashbackBufferManager_PurgeCompletedSegments_AccountsForActiveBytesOnPartialPurge();

    [Fact]
    public Task FlashbackBufferManagerFullPurgeReportsActiveBytesOnce()
        => global::Program.FlashbackBufferManager_PurgeAllSegmentsCore_ReportsActiveBytesOnce();

    [Fact]
    public Task FlashbackBufferManagerRemovesStaleLegacyRootSegments()
        => global::Program.FlashbackBufferManager_RemovesStaleLegacyRootSegments();

    [Fact]
    public Task FlashbackBufferManagerPreservesUnrelatedEmptyTempDirectories()
        => global::Program.FlashbackBufferManager_PreservesUnrelatedEmptyTempDirectories();

    [Fact]
    public Task FlashbackBufferManagerTrimsStartupSessionCacheBudget()
        => global::Program.FlashbackBufferManager_TrimsStartupSessionCacheBudget();

    [Fact]
    public Task FlashbackBufferManagerRejectsUnsafeSessionIds()
        => global::Program.FlashbackBufferManager_RejectsUnsafeSessionIds();

    [Fact]
    public Task FlashbackBufferManagerValidatesSegmentExtensions()
        => global::Program.FlashbackBufferManager_ValidatesSegmentExtensions();
}

public class RecordingArtifactManagerTests
{
    [Fact]
    public async Task ArtifactManager_RollbackAsync_DeletesOnlyTheCurrentOutput()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"sussudio-rollback-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var outputPath = Path.Combine(tempDir, "current.mp4");
            var unrelatedPath = Path.Combine(tempDir, "existing-audio.m4a");
            File.WriteAllBytes(outputPath, new byte[] { 1, 2 });
            var unrelatedBytes = new byte[] { 3, 4 };
            File.WriteAllBytes(unrelatedPath, unrelatedBytes);

            var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
            var context = BuildRecordingContext(outputPath);
            var rollback = manager.GetType().GetMethod("RollbackAsync")!;
            await (Task)rollback.Invoke(manager, new object?[] { context, CancellationToken.None })!;

            Assert.False(File.Exists(outputPath));
            Assert.Equal(unrelatedBytes, File.ReadAllBytes(unrelatedPath));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ArtifactManager_RollbackAsync_SafeWithNullContext()
    {
        var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
        var rollbackMethod = manager.GetType().GetMethod("RollbackAsync")
            ?? throw new InvalidOperationException("RollbackAsync not found");

        _ = RequireType("Sussudio.Services.Contracts.RecordingContext");
        var task = rollbackMethod.Invoke(manager, new object?[] { null, CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("RollbackAsync did not return Task");
        await task;
    }

    private static object BuildRecordingContext(string outputPath)
    {
        var contextType = RequireType("Sussudio.Services.Contracts.RecordingContext");
        var context = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(context, "Settings", BuildSettings());
        SetPropertyBackingField(context, "EffectiveFrameRate", 60.0);
        SetPropertyBackingField(context, "FrameRateArg", "60");
        SetPropertyBackingField(context, "EffectiveWidth", 1920u);
        SetPropertyBackingField(context, "EffectiveHeight", 1080u);
        SetPropertyBackingField(context, "VideoInputPixelFormat", "nv12");
        SetPropertyBackingField(context, "VideoOutputPath", outputPath);
        SetPropertyBackingField(context, "FinalOutputPath", outputPath);
        SetPropertyBackingField(context, "HdrPipelineActive", false);
        return context;
    }

    private static object BuildSettings()
    {
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Width", 1920u);
        SetPropertyOrBackingField(settings, "Height", 1080u);
        SetPropertyOrBackingField(settings, "FrameRate", 60d);
        SetPropertyOrBackingField(settings, "RequestedFrameRateArg", "60/1");
        SetPropertyOrBackingField(settings, "RequestedFrameRateNumerator", 60u);
        SetPropertyOrBackingField(settings, "RequestedFrameRateDenominator", 1u);
        SetPropertyOrBackingField(settings, "RequestedPixelFormat", "NV12");
        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "HevcMp4"));
        SetPropertyOrBackingField(settings, "Quality", ParseEnum("Sussudio.Models.VideoQuality", "High"));
        SetPropertyOrBackingField(settings, "HdrEnabled", false);
        SetPropertyOrBackingField(settings, "HdrOutputMode", ParseEnum("Sussudio.Models.HdrOutputMode", "Hdr10Pq"));
        SetPropertyOrBackingField(settings, "AudioEnabled", true);
        SetPropertyOrBackingField(settings, "OutputPath", Path.GetTempPath());
        return settings;
    }

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object CreateInstance(string typeName)
        => Activator.CreateInstance(RequireType(typeName))
           ?? throw new InvalidOperationException($"Failed to create {typeName}.");

    private static object ParseEnum(string typeName, string value)
        => Enum.Parse(RequireType(typeName), value);

    private static void SetPropertyOrBackingField(object instance, string name, object? value)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        SetPropertyBackingField(instance, name, value);
    }

    private static void SetPropertyBackingField(object instance, string name, object? value)
    {
        var field = instance.GetType().GetField($"<{name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{instance.GetType().Name}.{name} backing field not found.");
        field.SetValue(instance, value);
    }

    private static object? GetPropertyValue(object instance, string name)
        => instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!.GetValue(instance);

    private static bool GetBoolProperty(object instance, string name)
        => (bool)GetPropertyValue(instance, name)!;

    private static string GetStringProperty(object instance, string name)
        => (string)GetPropertyValue(instance, name)!;

    private static int GetCountProperty(object? value)
        => value is ICollection collection
            ? collection.Count
            : throw new InvalidOperationException("Expected collection value.");
}

// Representative xUnit slice moved out of the shared Program helper namespace.
//
// The test project targets net8.0 while Sussudio targets
// net8.0-windows10.0.19041.0, so a ProjectReference would force a Windows
// target onto the test rig and pull WinUI deps into discovery. xUnit tests
// therefore reach the assembly the same way the smoke entry point does:
// Assembly.LoadFrom against the staged Sussudio.dll. The
// [assembly: InternalsVisibleTo("Sussudio.Tests")] attributes on Sussudio
// and ssctl mean reflection no longer needs to crack open private members,
// just resolve the type via its public/internal name.
//
// SussudioAssembly resolves the staged assembly independently for xUnit.
[Collection(RecoveryEnvironmentCollection.Name)]
public class RecordingContractsTests
{
    [Fact]
    public void GpuPipelineHandles_None_IsZeroed()
    {
        var asm = SussudioAssembly.Load();
        var handlesType = asm.GetType("Sussudio.Services.Contracts.GpuPipelineHandles", throwOnError: true)!;

        var none = handlesType.GetProperty("None", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

        Assert.Equal(System.IntPtr.Zero, (System.IntPtr)handlesType.GetProperty("D3D11DevicePtr")!.GetValue(none)!);
        Assert.Equal(System.IntPtr.Zero, (System.IntPtr)handlesType.GetProperty("D3D11DeviceContextPtr")!.GetValue(none)!);
        Assert.Equal(System.IntPtr.Zero, (System.IntPtr)handlesType.GetProperty("CudaHwDeviceCtxPtr")!.GetValue(none)!);
        Assert.Equal(System.IntPtr.Zero, (System.IntPtr)handlesType.GetProperty("CudaHwFramesCtxPtr")!.GetValue(none)!);
    }

    [Fact]
    public void FinalizeResult_Success_HasEmptyPreservedArtifacts()
    {
        var asm = SussudioAssembly.Load();
        var resultType = asm.GetType("Sussudio.Services.Contracts.FinalizeResult", throwOnError: true)!;

        var success = resultType.GetMethod(
                "Success",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(string), typeof(string) },
                modifiers: null)!
            .Invoke(null, new object[] { "/tmp/out.mp4", "Stopped" })!;

        Assert.True((bool)resultType.GetProperty("Succeeded")!.GetValue(success)!);
        Assert.Equal("/tmp/out.mp4", (string)resultType.GetProperty("OutputPath")!.GetValue(success)!);
        Assert.Equal("Stopped", (string)resultType.GetProperty("StatusMessage")!.GetValue(success)!);

        var artifacts = (System.Collections.IEnumerable)resultType.GetProperty("PreservedArtifacts")!.GetValue(success)!;
        Assert.Empty(artifacts.Cast<object>());
    }

    [Fact]
    public void RecordingContextRequest_Defaults_MatchRecordingContextDefaults()
    {
        var asm = SussudioAssembly.Load();
        var requestType = asm.GetType("Sussudio.Services.Contracts.RecordingContextRequest", throwOnError: true)!;

        var request = Activator.CreateInstance(requestType)!;

        Assert.Equal("30", (string)requestType.GetProperty("FrameRateArg")!.GetValue(request)!);
        Assert.Equal("nv12", (string)requestType.GetProperty("VideoInputPixelFormat")!.GetValue(request)!);
        Assert.False((bool)requestType.GetProperty("IsFullRangeInput")!.GetValue(request)!);
        Assert.True((bool)requestType.GetProperty("ReserveFinalOutputFile")!.GetValue(request)!);
    }

    [Fact]
    public void RecordingStats_ComputesTotalBytes()
    {
        var asm = SussudioAssembly.Load();
        var statsType = asm.GetType("Sussudio.Models.RecordingStats", throwOnError: true)!;

        var ctor = statsType.GetConstructor(new[] { typeof(long), typeof(long), typeof(bool), typeof(bool) });
        Assert.NotNull(ctor);

        var finalStats = ctor!.Invoke(new object[] { 123L, 456L, false, false });
        Assert.Equal(579L, GetLongProperty(finalStats, "TotalBytes"));
    }

    [Fact]
    public void RecordingStats_PreservesEstimateFlag()
    {
        var statsType = SussudioAssembly.Load().GetType("Sussudio.Models.RecordingStats", throwOnError: true)!;
        var ctor = statsType.GetConstructor(new[] { typeof(long), typeof(long), typeof(bool), typeof(bool) })!;
        var flashbackStats = ctor.Invoke(new object[] { 10L, 5L, true, false });

        Assert.True(GetBoolProperty(flashbackStats, "IsFlashbackEstimate"));
    }

    [Fact]
    public void RecordingStats_PreservesFailureFlag()
    {
        var statsType = SussudioAssembly.Load().GetType("Sussudio.Models.RecordingStats", throwOnError: true)!;
        var ctor = statsType.GetConstructor(new[] { typeof(long), typeof(long), typeof(bool), typeof(bool) })!;
        var failureStats = ctor.Invoke(new object[] { 0L, 0L, false, true });

        Assert.True(GetBoolProperty(failureStats, "IsFailure"));
    }

    [Fact]
    public void RecordingStats_StoresTimestampAndCaptureSessionEpoch()
    {
        var statsType = SussudioAssembly.Load().GetType("Sussudio.Models.RecordingStats", throwOnError: true)!;
        var stampedCtor = statsType.GetConstructor(new[] { typeof(long), typeof(long), typeof(bool), typeof(bool), typeof(DateTimeOffset), typeof(long) });
        Assert.NotNull(stampedCtor);

        var timestamp = new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);
        var stampedStats = stampedCtor!.Invoke(new object[] { 1L, 2L, false, false, timestamp, 42L });
        Assert.Equal(timestamp, (DateTimeOffset)statsType.GetProperty("TimestampUtc")!.GetValue(stampedStats)!);
        Assert.Equal(42L, GetLongProperty(stampedStats, "CaptureSessionEpoch"));
    }

    [Fact]
    public void RecordingStats_ComputesTotalBytesWithSignedAudioCorrection()
    {
        var statsType = SussudioAssembly.Load().GetType("Sussudio.Models.RecordingStats", throwOnError: true)!;
        var ctor = statsType.GetConstructor(new[] { typeof(long), typeof(long), typeof(bool), typeof(bool) })!;
        var correctedStats = ctor.Invoke(new object[] { 100L, -20L, false, false });

        Assert.Equal(80L, GetLongProperty(correctedStats, "TotalBytes"));
    }

    [Fact]
    public void FinalizeResult_Failure_DeduplicatesAndFiltersArtifacts()
    {
        var asm = SussudioAssembly.Load();
        var resultType = asm.GetType("Sussudio.Services.Contracts.FinalizeResult", throwOnError: true)!;

        var artifacts = new List<string> { "/path/a.mp4", "/path/A.mp4", null!, string.Empty, " ", "/path/b.m4a" };
        var failure = resultType.GetMethod(
                "Failure",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(string), typeof(string), typeof(IEnumerable<string>) },
                modifiers: null)!
            .Invoke(null, new object?[] { "/output.mp4", "mux failed", artifacts })!;

        Assert.False((bool)resultType.GetProperty("Succeeded")!.GetValue(failure)!);
        Assert.Equal("/output.mp4", (string)resultType.GetProperty("OutputPath")!.GetValue(failure)!);
        Assert.Equal("mux failed", (string)resultType.GetProperty("StatusMessage")!.GetValue(failure)!);

        var preserved = (System.Collections.IEnumerable)resultType.GetProperty("PreservedArtifacts")!.GetValue(failure)!;
        Assert.Equal(new[] { "/path/a.mp4", "/path/b.m4a" }, preserved.Cast<object>().Select(value => (string)value).ToArray());
    }

    [Fact]
    public void RecordingFinalizationRecoveryArtifacts_PreservesFallbackOutputWithoutContext()
    {
        var asm = SussudioAssembly.Load();
        var helperType = asm.GetType("Sussudio.Services.Recording.RecordingFinalizationRecoveryArtifacts", throwOnError: true)!;
        var preserveUnresolved = helperType.GetMethod("PreserveUnresolved", BindingFlags.Public | BindingFlags.Static)!;

        var tempDirectory = Path.Combine(Path.GetTempPath(), "sussudio-recording-recovery-" + Guid.NewGuid().ToString("N"));
        var previousRecoveryDirectory = Environment.GetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY");
        var stableRecoveryDirectory = Path.Combine(tempDirectory, "stable");
        Environment.SetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY", stableRecoveryDirectory);
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var outputPath = Path.Combine(tempDirectory, "capture.mp4");
            File.WriteAllBytes(outputPath, new byte[] { 1, 2, 3 });

            var result = (IEnumerable)preserveUnresolved.Invoke(
                null,
                new object?[] { null, outputPath, "timeout" })!;
            var artifacts = result.Cast<object>().Select(value => (string)value).ToArray();
            var markerPath = outputPath + ".recording-finalization-unresolved.txt";

            Assert.Contains(outputPath, artifacts);
            Assert.Contains(markerPath, artifacts);
            Assert.True(File.Exists(markerPath));
            Assert.Contains("reason=timeout", File.ReadAllText(markerPath));
            var stableMarkerPath = Path.Combine(
                stableRecoveryDirectory,
                "capture.mp4.recording-finalization-unresolved.txt");
            Assert.Contains(stableMarkerPath, artifacts);
            Assert.True(File.Exists(stableMarkerPath));
            Assert.Contains("reason=timeout", File.ReadAllText(stableMarkerPath));
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY", previousRecoveryDirectory);
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static bool GetBoolProperty(object instance, string propertyName)
        => (bool)instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!.GetValue(instance)!;

    private static long GetLongProperty(object instance, string propertyName)
        => (long)instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!.GetValue(instance)!;
}

// Resolves the staged Sussudio.dll the same way the smoke entry point does.
// Lives next to the xUnit slice so a future cleanup can lift it into an
// IClassFixture without coupling regression execution to Program.Main.
internal static class SussudioAssembly
{
    private static Assembly? _cached;

    public static Assembly Load()
    {
        if (_cached != null) return _cached;
        var path = System.Environment.GetEnvironmentVariable("SUSSUDIO_TEST_ASSEMBLY")
            ?? global::Program.SussudioAppAssemblyRelativePath;
        if (!System.IO.Path.IsPathRooted(path))
        {
            var repoRoot = RuntimeContractSource.GetRepoRoot();
            path = System.IO.Path.Combine(repoRoot, path.Replace('/', System.IO.Path.DirectorySeparatorChar));
        }

        path = System.IO.Path.GetFullPath(path);
        global::Program.RequireFreshSussudioAssembly(path);
        _cached = Assembly.LoadFrom(path);
        return _cached;
    }
}

internal static class LinqShim
{
    public static System.Collections.Generic.IEnumerable<T> Cast<T>(this System.Collections.IEnumerable source)
    {
        foreach (var item in source) yield return (T)item;
    }
}

internal static class ReflectionFlags
{
    public const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    public const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
}

internal static class EnvVarScope
{
    public static IDisposable Push(string name, string? value)
    {
        var previous = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
        return new Restore(name, previous);
    }

    private sealed class Restore : IDisposable
    {
        private readonly string _name;
        private readonly string? _previous;
        public Restore(string name, string? previous) { _name = name; _previous = previous; }
        public void Dispose() => Environment.SetEnvironmentVariable(_name, _previous);
    }
}
}

static partial class Program
{
    private static string ReadLibAvRecordingSinkSource()
        => ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.cs")
            .Replace("\r\n", "\n");

    private static string ReadUnifiedVideoCaptureSource()
        => ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs")
            .Replace("\r\n", "\n");

    private static string ExtractSourceBlock(string source, string startToken, string endToken)
    {
        var normalizedSource = NormalizeLineEndings(source);
        var normalizedStartToken = NormalizeLineEndings(startToken);
        var normalizedEndToken = NormalizeLineEndings(endToken);
        var start = normalizedSource.IndexOf(normalizedStartToken, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Assertion failed: expected source to contain '{startToken}'.");
        }

        var end = normalizedSource.IndexOf(normalizedEndToken, start + normalizedStartToken.Length, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidOperationException($"Assertion failed: expected source after '{startToken}' to contain '{endToken}'.");
        }

        return normalizedSource[start..end];
    }

    internal static Task RecordingVideoTryEnqueuePaths_DoNotBlockCaptureCallbacks()
    {
        var libAvSource = ReadLibAvRecordingSinkSource();
        var flashbackSource = ReadFlashbackEncoderSinkSource();

        var libAvVideoEnqueue = ExtractSourceBlock(
            libAvSource,
            "private VideoEnqueueResult TryEnqueueVideoPacket",
            "private VideoEnqueueResult TryEnqueueGpuPacket");
        var libAvGpuEnqueue = ExtractSourceBlock(
            libAvSource,
            "private VideoEnqueueResult TryEnqueueGpuPacket",
            "private unsafe VideoEnqueueResult TryEnqueueCudaPacket");
        var libAvCudaEnqueue = ExtractSourceBlock(
            libAvSource,
            "private unsafe VideoEnqueueResult TryEnqueueCudaPacket",
            "private bool TryWriteVideoPacket");
        var flashbackVideoEnqueue = ExtractSourceBlock(
            flashbackSource,
            "private VideoEnqueueResult TryEnqueueVideoPacket",
            "private VideoEnqueueResult TryEnqueueGpuPacket");
        var flashbackGpuEnqueue = ExtractSourceBlock(
            flashbackSource,
            "private VideoEnqueueResult TryEnqueueGpuPacket",
            "private string? GetVideoEnqueueRejectReason");

        AssertDoesNotContain(libAvVideoEnqueue, "while (true)");
        AssertDoesNotContain(libAvGpuEnqueue, "while (true)");
        AssertDoesNotContain(libAvCudaEnqueue, "while (true)");
        AssertDoesNotContain(flashbackVideoEnqueue, "while (true)");
        AssertDoesNotContain(flashbackGpuEnqueue, "while (true)");
        AssertDoesNotContain(libAvSource, "Thread.Sleep(");
        AssertDoesNotContain(libAvSource, "backpressure_retry");
        AssertDoesNotContain(flashbackSource, "WaitForBackpressureRetryCancellation");
        AssertDoesNotContain(flashbackVideoEnqueue, "TimeSpan.FromMilliseconds(1)");
        AssertDoesNotContain(flashbackGpuEnqueue, "TimeSpan.FromMilliseconds(1)");

        AssertContains(libAvVideoEnqueue, "FailEncoding(overloadFailure);");
        AssertContains(libAvVideoEnqueue, "ReturnVideoPacket(packet);");
        AssertContains(libAvGpuEnqueue, "FailEncoding(new InvalidOperationException(");
        AssertContains(libAvGpuEnqueue, "Marshal.Release(packet.Texture);");
        AssertContains(libAvCudaEnqueue, "FailEncoding(new InvalidOperationException(");
        AssertContains(libAvCudaEnqueue, "ffmpeg.av_frame_free(&overloadedFrame);");
        AssertContains(flashbackVideoEnqueue, "TrackVideoQueueRejected(\"queue_full\");");
        AssertContains(flashbackGpuEnqueue, "TrackGpuQueueRejected(\"queue_full\");");

        return Task.CompletedTask;
    }

    internal static Task LibAvRecordingSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches()
        => Sussudio.Tests.LibAvRecordingDrainBehaviorTests.VerifyAudioInterleavingAsync();

    internal static Task LibAvRecordingSink_P010EncodeRoundTrip()
        => Sussudio.Tests.LibAvRecordingDrainBehaviorTests.VerifyP010EncodeRoundTripAsync();

    /// <summary>
    /// A real IProcessSupervisor fake that returns crafted ffprobe output.
    /// This is the test seam that reviewers flagged as missing.
    /// </summary>
    private sealed class FakeProcessSupervisorImpl
    {
        private readonly List<(string FileName, string Arguments, string? PriorityClass)> _calls = new();
        private string _streamInfoOutput = string.Empty;
        private string _cadenceOutput = string.Empty;
        private string _hdrSideDataOutput = string.Empty;
        private bool _ffprobeVersionSucceeds = true;
        private int _exitCode;

        public IReadOnlyList<(string FileName, string Arguments, string? PriorityClass)> Calls => _calls;

        public FakeProcessSupervisorImpl WithStreamInfo(string output)
        {
            _streamInfoOutput = output;
            return this;
        }

        public FakeProcessSupervisorImpl WithCadenceJson(string json)
        {
            _cadenceOutput = json;
            return this;
        }

        public FakeProcessSupervisorImpl WithHdrSideDataJson(string json)
        {
            _hdrSideDataOutput = json;
            return this;
        }

        public FakeProcessSupervisorImpl WithFfprobeUnavailable()
        {
            _ffprobeVersionSucceeds = false;
            return this;
        }

        public FakeProcessSupervisorImpl WithExitCode(int code)
        {
            _exitCode = code;
            return this;
        }

        /// <summary>
        /// Creates an instance that implements IProcessSupervisor via a DispatchProxy.
        /// </summary>
        public object CreateProxy()
        {
            var supervisorType = RequireType("Sussudio.Services.Runtime.IProcessSupervisor");
            var specType = RequireType("Sussudio.Services.Runtime.ProcessSpec");

            // Use the generic DispatchProxy.Create<T, TProxy>() method
            var createMethod = typeof(DispatchProxy).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "Create" && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 2)
                .MakeGenericMethod(supervisorType, typeof(FakeSupervisorProxy));

            var proxy = createMethod.Invoke(null, null)!;

            // Set the callback on our proxy
            ((FakeSupervisorProxy)proxy).SetHandler(async (method, args) =>
            {
                var spec = args[0];
                var fileName = (string)specType.GetProperty("FileName")!.GetValue(spec)!;
                var arguments = (string)specType.GetProperty("Arguments")!.GetValue(spec)!;
                var priorityClass = specType.GetProperty("PriorityClass")!.GetValue(spec)?.ToString();
                _calls.Add((fileName, arguments, priorityClass));

                // Determine which probe this is based on arguments
                string stdout;
                if (arguments.Contains("-version"))
                {
                    return CreateProcessRunResult(
                        _ffprobeVersionSucceeds,
                        _ffprobeVersionSucceeds ? 0 : 1,
                        "ffprobe version N/A");
                }
                else if (arguments.Contains("-show_frames"))
                {
                    stdout = _cadenceOutput;
                }
                else if (arguments.Contains("side_data_list"))
                {
                    stdout = _hdrSideDataOutput;
                }
                else
                {
                    stdout = _streamInfoOutput;
                }

                return CreateProcessRunResult(true, _exitCode, stdout);
            });

            return proxy;
        }

        private static object CreateProcessRunResult(bool started, int exitCode, string stdOut)
        {
            var resultType = RequireType("Sussudio.Services.Runtime.ProcessRunResult");
            var result = RuntimeHelpers.GetUninitializedObject(resultType);
            SetPropertyBackingField(result, "Started", started);
            SetPropertyBackingField(result, "TimedOut", false);
            SetPropertyBackingField(result, "ExitConfirmed", true);
            SetPropertyBackingField(result, "ExitCode", (int?)exitCode);
            SetPropertyBackingField(result, "StdOut", stdOut);
            SetPropertyBackingField(result, "StdErr", string.Empty);
            return result;
        }
    }

    /// <summary>
    /// DispatchProxy implementation for IProcessSupervisor.
    /// The key challenge: Invoke must return Task&lt;ProcessRunResult&gt;, not Task&lt;object&gt;.
    /// We use a helper to wrap the result in the correctly-typed Task.
    /// </summary>
    public class FakeSupervisorProxy : DispatchProxy
    {
        private Func<MethodInfo, object?[], Task<object>>? _handler;

        public void SetHandler(Func<MethodInfo, object?[], Task<object>> handler)
        {
            _handler = handler;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (_handler == null)
                throw new InvalidOperationException("Handler not set on FakeSupervisorProxy");

            // RunAsync returns Task<ProcessRunResult>. We must return that exact type,
            // not Task<object>. Use reflection to create a typed Task wrapper.
            var resultType = targetMethod!.ReturnType; // Task<ProcessRunResult>
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var innerType = resultType.GetGenericArguments()[0]; // ProcessRunResult
                return WrapAsTypedTask(_handler(targetMethod, args!), innerType);
            }

            return _handler(targetMethod, args!);
        }

        private static object WrapAsTypedTask(Task<object> objectTask, Type targetType)
        {
            // Create a TaskCompletionSource<ProcessRunResult> and wire it to our Task<object>
            var tcsType = typeof(TaskCompletionSource<>).MakeGenericType(targetType);
            var tcs = Activator.CreateInstance(tcsType)!;
            var setResultMethod = tcsType.GetMethod("SetResult")!;
            var setExceptionMethod = tcsType.GetMethod("SetException", new[] { typeof(Exception) })!;
            var taskProp = tcsType.GetProperty("Task")!;

            objectTask.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    setExceptionMethod.Invoke(tcs, new object[] { t.Exception!.InnerException! });
                else if (t.IsCanceled)
                    tcsType.GetMethod("SetCanceled", Type.EmptyTypes)!.Invoke(tcs, null);
                else
                    setResultMethod.Invoke(tcs, new[] { t.Result });
            }, TaskScheduler.Default);

            return taskProp.GetValue(tcs)!;
        }
    }

    private static object BuildRuntimeSnapshotForVerificationEx(
        string? requestedFormat = "HevcMp4",
        bool requestedHdrEnabled = false,
        bool hdrOutputActive = false,
        bool requestedHdrMasteringMetadata = false,
        uint? negotiatedWidth = 1920,
        uint? negotiatedHeight = 1080,
        uint? negotiatedFrameRateNumerator = 60,
        uint? negotiatedFrameRateDenominator = 1,
        string? flashbackExportOutputPath = null,
        string? flashbackExportVerificationFormat = null,
        string? lastOutputPath = null,
        string? recordingBackend = null,
        string? recordingIntegrityBackend = null)
    {
        var type = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var snapshot = RuntimeHelpers.GetUninitializedObject(type);
        SetPropertyOrBackingField(snapshot, "RequestedFormat", requestedFormat);
        SetPropertyOrBackingField(snapshot, "RequestedHdrEnabled", (bool?)requestedHdrEnabled);
        SetPropertyOrBackingField(snapshot, "HdrOutputActive", hdrOutputActive);
        SetPropertyOrBackingField(snapshot, "RequestedHdrMasteringMetadata", (bool?)requestedHdrMasteringMetadata);
        SetPropertyOrBackingField(snapshot, "NegotiatedWidth", negotiatedWidth);
        SetPropertyOrBackingField(snapshot, "NegotiatedHeight", negotiatedHeight);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateNumerator", negotiatedFrameRateNumerator);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateDenominator", negotiatedFrameRateDenominator);
        SetPropertyOrBackingField(snapshot, "FlashbackExportOutputPath", flashbackExportOutputPath);
        SetPropertyOrBackingField(snapshot, "FlashbackExportVerificationFormat", flashbackExportVerificationFormat);
        SetPropertyOrBackingField(snapshot, "LastOutputPath", lastOutputPath);
        SetPropertyOrBackingField(snapshot, "RecordingBackend", recordingBackend);
        SetPropertyOrBackingField(snapshot, "RecordingIntegrityBackend", recordingIntegrityBackend);
        return snapshot;
    }

    private static object CreateVerifierWithFake(object fakeSupervisor)
    {
        var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
        var supervisorType = RequireType("Sussudio.Services.Runtime.IProcessSupervisor");
        var ctor = verifierType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { supervisorType, typeof(string) },
            modifiers: null)
            ?? throw new InvalidOperationException("RecordingVerifier internal constructor not found.");
        return ctor.Invoke(new object[] { fakeSupervisor, "ffprobe.exe" });
    }

    private static async Task<object> RunVerifyAsync(object verifier, string? outputPath, object snapshot)
    {
        var verifierType = verifier.GetType();
        var verifyAsync = verifierType.GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("VerifyAsync not found.");
        var task = verifyAsync.Invoke(verifier, new object?[] { outputPath, snapshot, CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("VerifyAsync did not return Task.");
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }



    internal static Task RecordingVerificationResult_HasExpectedProperties()
    {
        var resultType = RequireType("Sussudio.Models.RecordingVerificationResult");

        var expectedProps = new[]
        {
            "TimestampUtc", "Succeeded", "Message", "OutputPath", "FileExists", "FileSizeBytes",
            "VerificationMode", "DetectedContainer", "DetectedVideoCodec", "DetectedPixelFormat",
            "DetectedColorPrimaries", "DetectedColorTransfer", "DetectedColorSpace",
            "DetectedHdrSideDataTypes", "HdrMetadataPresent", "HdrColorimetryValid",
            "HdrMasteringMetadataPresent", "HdrVerificationLevel",
            "DetectedWidth", "DetectedHeight", "DetectedFrameRate",
            "CadenceSampleCount", "CadenceObservedFps", "CadenceExpectedIntervalMs",
            "CadenceAverageIntervalMs", "CadenceP95IntervalMs", "CadenceMaxIntervalMs",
            "CadenceJitterStdDevMs", "CadenceSevereGapCount", "CadenceSevereGapPercent",
            "CadenceEstimatedDroppedFrames", "CadenceEstimatedDropPercent",
            "PrimaryMismatchCode", "PrimaryMismatchExpected", "PrimaryMismatchActual",
            "Mismatches", "HdrParity"
        };

        foreach (var prop in expectedProps)
        {
            AssertNotNull(
                resultType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance),
                $"RecordingVerificationResult.{prop}");
        }

        return Task.CompletedTask;
    }

    internal static async Task RecordingVerifier_ReturnsFailure_WhenFfprobeUnavailable()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_ffprobe_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 }); // minimal mp4 header
        try
        {
            var fake = new FakeProcessSupervisorImpl().WithFfprobeUnavailable();
            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx();
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "ffprobe");
        }
        finally
        {
            global::Program.TryDeleteFile(tempFile);
        }
    }

    // ── Integration test: ffprobe exit code failure ──

    internal static async Task RecordingVerifier_ReturnsFailure_WhenFfprobeExitsNonZero()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_exit_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithExitCode(1)
                .WithStreamInfo("");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx();
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "ffprobe-failed");
        }
        finally
        {
            global::Program.TryDeleteFile(tempFile);
        }
    }

    // ── Integration test: codec match (HEVC) ──

    internal static async Task RecordingVerifier_RunsFfprobeBelowNormalPriority()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_priority_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=h264\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(requestedFormat: "H264Mp4");
            _ = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, fake.Calls.Count >= 2, "ffprobe calls recorded");
            foreach (var call in fake.Calls)
            {
                AssertEqual("BelowNormal", call.PriorityClass, $"ffprobe priority for {call.Arguments}");
            }
        }
        finally
        {
            global::Program.TryDeleteFile(tempFile);
        }
    }

    internal static async Task RecordingVerifier_PassesVerification_WhenAllFieldsMatch_Hevc()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_hevc_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n")
                ;

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(requestedFormat: "HevcMp4");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("hevc", GetStringProperty(result, "DetectedVideoCodec"), "DetectedVideoCodec");
            AssertEqual((uint)1920, (uint)Convert.ToInt64(GetPropertyValue(result, "DetectedWidth")), "DetectedWidth");
            AssertEqual((uint)1080, (uint)Convert.ToInt64(GetPropertyValue(result, "DetectedHeight")), "DetectedHeight");
        }
        finally
        {
            global::Program.TryDeleteFile(tempFile);
        }
    }

    // ── Integration test: codec mismatch ──

    internal static async Task RecordingVerifier_DetectsCodecMismatch_WhenH264InsteadOfHevc()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_codec_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=h264\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n")
                ;

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(requestedFormat: "HevcMp4");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "codec-mismatch");
        }
        finally
        {
            global::Program.TryDeleteFile(tempFile);
        }
    }

    // ── Integration test: H264 codec match ──

    internal static async Task RecordingVerifier_PassesVerification_ForH264Format()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_h264_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=h264\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n")
                ;

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(requestedFormat: "H264Mp4");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("h264", GetStringProperty(result, "DetectedVideoCodec"), "DetectedVideoCodec");
        }
        finally
        {
            global::Program.TryDeleteFile(tempFile);
        }
    }

    // ── Integration test: resolution mismatch ──

    internal static async Task RecordingVerifier_UsesFlashbackExportVerificationFormat()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_flashback_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                requestedFormat: "Av1Mp4",
                flashbackExportOutputPath: tempFile,
                flashbackExportVerificationFormat: "HevcMp4");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("hevc", GetStringProperty(result, "DetectedVideoCodec"), "DetectedVideoCodec");
        }
        finally
        {
            global::Program.TryDeleteFile(tempFile);
        }
    }

    internal static async Task RecordingVerifier_UsesFlashbackRecordingVerificationFormat()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_flashback_recording_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                requestedFormat: "Av1Mp4",
                flashbackExportVerificationFormat: "HevcMp4",
                lastOutputPath: tempFile,
                recordingIntegrityBackend: "Flashback");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("hevc", GetStringProperty(result, "DetectedVideoCodec"), "DetectedVideoCodec");
        }
        finally
        {
            global::Program.TryDeleteFile(tempFile);
        }
    }

    internal static async Task RecordingVerifier_DetectsResolutionMismatch()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_res_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1280\n" +
                    "height=720\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n")
                ;

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                negotiatedWidth: 1920, negotiatedHeight: 1080);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "resolution-mismatch");
        }
        finally
        {
            global::Program.TryDeleteFile(tempFile);
        }
    }

    // ── Integration test: frame rate mismatch ──

    internal static async Task RecordingVerifier_DetectsFrameRateMismatch()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_fps_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=30/1\n" +
                    "r_frame_rate=30/1\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                negotiatedFrameRateNumerator: 60, negotiatedFrameRateDenominator: 1);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "fps-mismatch");
        }
        finally
        {
            global::Program.TryDeleteFile(tempFile);
        }
    }

    // ── Integration test: HDR validation passes with correct metadata ──

    internal static async Task RecordingVerifier_PassesHdrValidation_WhenAllHdrFieldsPresent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_hdr_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            // Use hdrOutputActive=true (not requestedHdrEnabled) to trigger HDR validation
            // without the ProbeHdrSideDataAsync JSON path (avoids System.Text.Json version mismatch)
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=3840\n" +
                    "height=2160\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=p010le\n" +
                    "color_primaries=bt2020\n" +
                    "color_transfer=smpte2084\n" +
                    "color_space=bt2020nc\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                requestedFormat: "HevcMp4",
                requestedHdrEnabled: false,
                hdrOutputActive: true,
                negotiatedWidth: 3840,
                negotiatedHeight: 2160);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("p010le", GetStringProperty(result, "DetectedPixelFormat"), "DetectedPixelFormat");
            AssertEqual(true, GetPropertyValue(result, "HdrMetadataPresent"), "HdrMetadataPresent");
            AssertEqual(true, GetPropertyValue(result, "HdrColorimetryValid"), "HdrColorimetryValid");
            AssertEqual("ColorimetryOnly", GetStringProperty(result, "HdrVerificationLevel"), "HdrVerificationLevel");

            var hdrParity = GetPropertyValue(result, "HdrParity")!;
            AssertEqual("Verified", GetStringProperty(hdrParity, "Status"), "HdrParity.Status");
            AssertEqual(true, GetBoolProperty(hdrParity, "Verified"), "HdrParity.Verified");
            AssertEqual("ColorimetryOnly", GetStringProperty(hdrParity, "VerificationLevel"), "HdrParity.VerificationLevel");
        }
        finally
        {
            global::Program.TryDeleteFile(tempFile);
        }
    }

    // ── Integration test: HDR colorimetry mismatch ──

    internal static async Task RecordingVerifier_DetectsHdrColorimetryMismatch()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_hdr_bad_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            // SDR colorimetry on an HDR-active recording (use hdrOutputActive, not requestedHdrEnabled
            // to avoid ProbeHdrSideDataAsync JSON path)
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=3840\n" +
                    "height=2160\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n" +
                    "color_primaries=bt709\n" +
                    "color_transfer=bt709\n" +
                    "color_space=bt709\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                requestedFormat: "HevcMp4",
                requestedHdrEnabled: false,
                hdrOutputActive: true,
                negotiatedWidth: 3840,
                negotiatedHeight: 2160);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            // Should have multiple HDR-related mismatches
            var mismatches = GetPropertyValue(result, "Mismatches") as System.Collections.IEnumerable;
            var mismatchList = new List<string>();
            foreach (var m in mismatches!) mismatchList.Add(m?.ToString() ?? "");
            var hasPixfmtMismatch = mismatchList.Any(m => m.Contains("pixfmt-not-10bit"));
            var hasColorimetryMismatch = mismatchList.Any(m => m.Contains("colorimetry-mismatch"));
            AssertEqual(true, hasPixfmtMismatch, "Has pixfmt-not-10bit mismatch");
            AssertEqual(true, hasColorimetryMismatch, "Has colorimetry-mismatch");

            AssertEqual(false, GetPropertyValue(result, "HdrMetadataPresent"), "HdrMetadataPresent");
            AssertEqual(false, GetPropertyValue(result, "HdrColorimetryValid"), "HdrColorimetryValid");
            AssertEqual("ColorimetryOnly", GetStringProperty(result, "HdrVerificationLevel"), "HdrVerificationLevel");

            var hdrParity = GetPropertyValue(result, "HdrParity")!;
            AssertEqual("Mismatch", GetStringProperty(hdrParity, "Status"), "HdrParity.Status");
            AssertEqual(false, GetBoolProperty(hdrParity, "Verified"), "HdrParity.Verified");

            var taxonomy = GetPropertyValue(hdrParity, "MismatchTaxonomy") as System.Collections.IEnumerable;
            var taxonomyEntries = new List<object>();
            foreach (var entry in taxonomy!) taxonomyEntries.Add(entry!);
            var hasHdrError = taxonomyEntries.Any(entry =>
                GetStringProperty(entry, "Category") == "HDR" &&
                GetStringProperty(entry, "Code") == "pixfmt-not-10bit" &&
                GetStringProperty(entry, "Severity") == "Error");
            var hasColorimetryError = taxonomyEntries.Any(entry =>
                GetStringProperty(entry, "Category") == "Colorimetry" &&
                GetStringProperty(entry, "Code") == "colorimetry-mismatch" &&
                GetStringProperty(entry, "Severity") == "Error");
            AssertEqual(true, hasHdrError, "HDR mismatch taxonomy is Error severity");
            AssertEqual(true, hasColorimetryError, "Colorimetry mismatch taxonomy is Error severity");
        }
        finally
        {
            global::Program.TryDeleteFile(tempFile);
        }
    }

    // ── Integration test: NTSC frame rate tolerance ──

    internal static async Task RecordingVerifier_PassesNtscFrameRateWithinTolerance()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_ntsc_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            // 59.94 fps (60000/1001) vs expected 60 fps — within 0.75 tolerance
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60000/1001\n" +
                    "r_frame_rate=60000/1001\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                negotiatedFrameRateNumerator: 60, negotiatedFrameRateDenominator: 1);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            // 60 - 59.94 = 0.06 which is within 0.75 tolerance
            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
        }
        finally
        {
            global::Program.TryDeleteFile(tempFile);
        }
    }

    internal static Task WasapiAudioCapture_HotAudioWritesRejectIncompleteTasks()
    {
        var wasapiSource = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioCapture.cs")
            .Replace("\r\n", "\n");
        var contractsSource = ReadRepoFile("Sussudio/Services/Contracts/ServiceContracts.cs")
            .Replace("\r\n", "\n");
        var libAvSource = ReadLibAvRecordingSinkSource();
        var flashbackSource = ReadFlashbackEncoderSinkSource();

        var drainBlock = ExtractSourceBlock(
            wasapiSource,
            "private void DrainCapturePackets()",
            "private void OnCaptureFailed");
        AssertContains(drainBlock, "handoffToPlayback = DispatchConvertedAudioPacket(converted);");
        AssertDoesNotContain(drainBlock, "InvokeHotAudioWriter(");
        AssertDoesNotContain(drainBlock, "WriteAudioToSinkOnCaptureThread(");
        AssertDoesNotContain(drainBlock, ".GetAwaiter()");
        AssertDoesNotContain(drainBlock, "Volatile.Read(ref _recordingSink)");
        AssertContains(wasapiSource, "private void CaptureThreadMain()");
        AssertContains(wasapiSource, "private bool DispatchConvertedAudioPacket(ConvertedAudioPacket converted)");
        AssertContains(wasapiSource, "var audioWriter = Volatile.Read(ref _audioWriter);");
        AssertContains(wasapiSource, "var sink = Volatile.Read(ref _recordingSink);");
        AssertContains(wasapiSource, "var flashbackSink = Volatile.Read(ref _flashbackSink);");
        AssertContains(wasapiSource, "var playback = Volatile.Read(ref _playback);");
        AssertContains(wasapiSource, "playback.EnqueuePooledSamples(convertedBuffer, converted.Length);");
        AssertContains(wasapiSource, "private static void InvokeHotAudioWriter(");
        AssertContains(wasapiSource, "private static void WriteAudioToSinkOnCaptureThread(");
        AssertContains(wasapiSource, "private static void CompleteHotAudioWrite(Task task, string target)");
        AssertContains(wasapiSource, "if (!task.IsCompleted)");
        AssertContains(wasapiSource, "Audio writers must copy/enqueue synchronously and return Task.CompletedTask.");
        AssertContains(contractsSource, "Hot WASAPI callback write.");
        AssertContains(contractsSource, "must not do blocking/async work");

        var libAvAudioWrite = ExtractSourceBlock(
            libAvSource,
            "public Task WriteAudioAsync",
            "public Task WriteMicrophoneAudioAsync");
        var flashbackAudioWrite = ExtractSourceBlock(
            flashbackSource,
            "public Task WriteAudioAsync",
            "public Task WriteMicrophoneAudioAsync");
        AssertContains(libAvAudioWrite, "Hot WASAPI callback path: copy/enqueue only, never await or block.");
        AssertContains(libAvAudioWrite, "return Task.CompletedTask;");
        AssertContains(flashbackAudioWrite, "Hot WASAPI callback path: copy/enqueue only, never await or block.");
        AssertContains(flashbackAudioWrite, "return Task.CompletedTask;");

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

    internal static async Task UnifiedVideoCapture_NullsMjpegPipeline_WhenStopFails()
    {
        // Stop must never throw — a wedged or self-join pipeline must not block reinit/preview-stop.
        // StopAndDisposeMjpegPipeline logs UNIFIED_VIDEO_MJPEG_STOP_TIMEOUT and forces cleanup.
        var unifiedVideoCapture = CreateInstance("Sussudio.Services.Capture.UnifiedVideoCapture");
        var pipelineType = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline");
        var pipeline = CreateUninitializedObject(pipelineType);
        SeedPipelineStopFailureState(pipeline, pipelineType);

        SetPrivateField(unifiedVideoCapture, "_mjpegPipeline", pipeline);
        SetPrivateField(pipeline, "_emitThread", Thread.CurrentThread);

        var stopTask = (Task)InvokeInstanceMethod(unifiedVideoCapture, "StopAsync");

        // StopAsync must complete without throwing even when TryStop returns false.
        await stopTask.ConfigureAwait(false);

        // Pipeline must be nulled out — forcing cleanup means the reference is released.
        var pipelineAfterStop = GetPrivateField(unifiedVideoCapture, "_mjpegPipeline");
        AssertEqual(null, pipelineAfterStop, "UnifiedVideoCapture._mjpegPipeline nulled on forced-cleanup stop");

        await DisposeValueTaskAsync(unifiedVideoCapture).ConfigureAwait(false);
    }

    // Shared source readers: still used by architecture/contract tests owned by
    // other xUnit slices (XUnit.AutomationContractsTests.cs, XUnit.CoreRuntimeContractsTests.cs).
    private static readonly string[] CaptureServiceFlashbackOrchestrationFiles =
    {
        "Sussudio/Services/Capture/CaptureService.Flashback.cs"
    };

    private static readonly string[] CaptureServiceRecordingFinalizationFiles =
    {
        "Sussudio/Services/Capture/CaptureService.Flashback.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs"
    };

    private static readonly string[] CaptureServicePreviewLifecycleFiles =
    {
        "Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs",
        "Sussudio/Services/Capture/CaptureService.cs"
    };

    private static readonly string[] CaptureServiceRecordingIntegrityFiles =
    {
        "Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs"
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

    internal static Task RecordingStop_PropagatesUnifiedVideoStopFailure()
    {
        var captureServiceText = (
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs"))
            .Replace("\r\n", "\n");

        AssertContains(captureServiceText, "Unified video recording stop failed");
        AssertContains(captureServiceText, "$\"Unified video recording stop failed: {ex.Message}\",");
        AssertContains(captureServiceText, "RecordingFailureCodes.UnifiedStopFailed);");
        AssertContains(captureServiceText, "StopUnifiedVideoRecordingForLibAvFinalizeAsync(");
        AssertContains(captureServiceText, "StopAndDisposeLibAvSinkForFinalizeAsync(");
        AssertContains(captureServiceText, "DisposeIdleLibAvPreviewResourcesAfterRecordingAsync(");
        AssertContains(captureServiceText, "result = ApplyRecordingFinalizePolicies(");
        AssertContains(captureServiceText, "PublishLibAvRecordingIntegrity(");
        // Fix #12: sink dispatch became a ternary so the emergency flag can route to libAvSink.StopAsync(emergency, ct).
        AssertContains(captureServiceText, "var sinkResult = libAvSink != null");
        AssertContains(captureServiceText, "? await libAvSink.StopAsync(emergency, cancellationToken).ConfigureAwait(false)");
        AssertContains(captureServiceText, ": await sink.StopAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(captureServiceText, "if (result.Succeeded)\n            {\n                result = sinkResult;");
        AssertContains(captureServiceText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(captureServiceText, "_previewAudioGraph.DetachCapture(");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_VideoBitstreamFilterSpec_ChainsHdrAndMpegTsFilters()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("GetVideoBitstreamFilterSpec",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetVideoBitstreamFilterSpec not found.");

        var hdrHevcTs = CreateValidEncoderOptions();
        SetPropertyBackingField(hdrHevcTs, "CodecName", "hevc_nvenc");
        SetPropertyBackingField(hdrHevcTs, "ContainerFormat", "mpegts");
        SetPropertyBackingField(hdrHevcTs, "HdrEnabled", true);
        SetPropertyBackingField(hdrHevcTs, "IsP010", true);
        AssertEqual(
            "hevc_metadata=colour_primaries=9:transfer_characteristics=16:matrix_coefficients=9,dump_extra",
            method.Invoke(null, new[] { hdrHevcTs })?.ToString(),
            "HDR HEVC MPEG-TS chains HDR metadata and parameter-set filters");

        var sdrHevcTs = CreateValidEncoderOptions();
        SetPropertyBackingField(sdrHevcTs, "CodecName", "hevc_nvenc");
        SetPropertyBackingField(sdrHevcTs, "ContainerFormat", "mpegts");
        SetPropertyBackingField(sdrHevcTs, "HdrEnabled", false);
        AssertEqual("dump_extra", method.Invoke(null, new[] { sdrHevcTs })?.ToString(), "SDR HEVC MPEG-TS dumps parameter sets");

        var hdrHevcMp4 = CreateValidEncoderOptions();
        SetPropertyBackingField(hdrHevcMp4, "CodecName", "hevc_nvenc");
        SetPropertyBackingField(hdrHevcMp4, "ContainerFormat", "mp4");
        SetPropertyBackingField(hdrHevcMp4, "HdrEnabled", true);
        SetPropertyBackingField(hdrHevcMp4, "IsP010", true);
        AssertEqual(
            "hevc_metadata=colour_primaries=9:transfer_characteristics=16:matrix_coefficients=9",
            method.Invoke(null, new[] { hdrHevcMp4 })?.ToString(),
            "HDR HEVC MP4 keeps HDR metadata filter");

        var hdrAv1Mp4 = CreateValidEncoderOptions();
        SetPropertyBackingField(hdrAv1Mp4, "CodecName", "av1_nvenc");
        SetPropertyBackingField(hdrAv1Mp4, "ContainerFormat", "mp4");
        SetPropertyBackingField(hdrAv1Mp4, "HdrEnabled", true);
        SetPropertyBackingField(hdrAv1Mp4, "IsP010", true);
        AssertEqual(
            "av1_metadata=color_primaries=9:transfer_characteristics=16:matrix_coefficients=9",
            method.Invoke(null, new[] { hdrAv1Mp4 })?.ToString(),
            "HDR AV1 MP4 keeps AV1 metadata filter");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_MapNvencPreset_MapsCorrectly()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("MapNvencPreset",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("MapNvencPreset not found.");

        var presetType = RequireType("Sussudio.Models.NvencPreset");
        foreach (var (name, expected) in new[]
        {
            ("Auto", "p4"), ("P1", "p1"), ("P2", "p2"), ("P3", "p3"),
            ("P4", "p4"), ("P5", "p5"), ("P6", "p6"), ("P7", "p7"),
            ("Fast", "p1"), ("Slow", "p7")
        })
        {
            var preset = Enum.Parse(presetType, name);
            AssertEqual(expected, method.Invoke(null, new[] { preset })!.ToString(), $"{name} maps to {expected}");
        }

        return Task.CompletedTask;
    }

    internal static Task PooledVideoFrame_GetFrameSizeBytes_CalculatesCorrectly()
    {
        var frameType = RequireType("Sussudio.Services.Contracts.PooledVideoFrame");
        var method = frameType.GetMethod("GetFrameSizeBytes",
            BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("GetFrameSizeBytes not found.");

        // NV12: width * height * 3 / 2
        var nv12_1080 = (int)method.Invoke(null, new object[] { 1920, 1080, false })!;
        AssertEqual(1920 * 1080 * 3 / 2, nv12_1080, "NV12 1080p");

        // P010: width * height * 3
        var p010_1080 = (int)method.Invoke(null, new object[] { 1920, 1080, true })!;
        AssertEqual(1920 * 1080 * 3, p010_1080, "P010 1080p");

        // P010 is exactly 2x NV12
        AssertEqual(nv12_1080 * 2, p010_1080, "P010 is 2x NV12");

        // 4K
        var nv12_4k = (int)method.Invoke(null, new object[] { 3840, 2160, false })!;
        AssertEqual(3840 * 2160 * 3 / 2, nv12_4k, "NV12 4K");

        AssertEqual(13, (int)method.Invoke(null, new object[] { 3, 3, false })!, "Odd NV12 dimensions retain multiply-before-divide order");
        AssertEqual(27, (int)method.Invoke(null, new object[] { 3, 3, true })!, "Odd P010 dimensions");
        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_ThrowIfError_ThrowsOnNegative()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ThrowIfError",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ThrowIfError not found.");

        // Non-negative should not throw
        method.Invoke(null, new object[] { 0, "test" });
        method.Invoke(null, new object[] { 1, "test" });

        // Negative should throw (may throw InvalidOperationException or
        // DllNotFoundException if FFmpeg runtime isn't loaded for GetErrorString)
        var threw = false;
        try
        {
            method.Invoke(null, new object[] { -1, "test operation" });
        }
        catch (TargetInvocationException)
        {
            threw = true;
        }
        AssertEqual(true, threw, "ThrowIfError throws on negative error code");

        return Task.CompletedTask;
    }



    internal static Task LibAvEncoder_GetHdrBitstreamFilterName_MapsCodecs()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("GetHdrBitstreamFilterName",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetHdrBitstreamFilterName not found.");

        // HEVC variants → "hevc_metadata"
        var hevc1 = method.Invoke(null, new object[] { "hevc_nvenc" })?.ToString();
        AssertEqual("hevc_metadata", hevc1!, "hevc_nvenc → hevc_metadata");

        var hevc2 = method.Invoke(null, new object[] { "libx265" })?.ToString();
        // libx265 doesn't contain "hevc" so should return null
        AssertEqual(true, hevc2 == null, "libx265 → null (no hevc substring)");

        // AV1 → "av1_metadata"
        var av1 = method.Invoke(null, new object[] { "av1_nvenc" })?.ToString();
        AssertEqual("av1_metadata", av1!, "av1_nvenc → av1_metadata");

        // H264 → null (no HDR bitstream filter)
        var h264 = method.Invoke(null, new object?[] { "h264_nvenc" });
        AssertEqual(true, h264 == null, "h264 → null");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_Invert_SwapsNumeratorDenominator()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("Invert",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Invert not found.");

        // The method takes AVRational which is a struct from FFmpeg.AutoGen
        // AVRational has fields: int num, int den
        var avRationalType = method.GetParameters()[0].ParameterType;
        var input = Activator.CreateInstance(avRationalType)!;
        avRationalType.GetField("num")!.SetValue(input, 60);
        avRationalType.GetField("den")!.SetValue(input, 1);

        var result = method.Invoke(null, new[] { input })!;
        var resultNum = (int)avRationalType.GetField("num")!.GetValue(result)!;
        var resultDen = (int)avRationalType.GetField("den")!.GetValue(result)!;

        AssertEqual(1, resultNum, "Inverted numerator");
        AssertEqual(60, resultDen, "Inverted denominator");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_ChromaticityAndLuminanceRationals_ParseCorrectly()
    {
        var hdrType = RequireType("Sussudio.Services.Recording.HdrMasterDisplayMetadata");

        var chromaMethod = hdrType.GetMethod("ToChromaticityRational",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ToChromaticityRational not found.");
        var lumaMethod = hdrType.GetMethod("ToLuminanceRational",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ToLuminanceRational not found.");

        var avRationalType = chromaMethod.ReturnType;

        // ToChromaticityRational: int.Parse(value) / 50000
        var chromaResult = chromaMethod.Invoke(null, new object[] { "13250" })!;
        var chromaNum = (int)avRationalType.GetField("num")!.GetValue(chromaResult)!;
        var chromaDen = (int)avRationalType.GetField("den")!.GetValue(chromaResult)!;
        AssertEqual(13250, chromaNum, "Chromaticity numerator");
        AssertEqual(50000, chromaDen, "Chromaticity denominator");

        // ToLuminanceRational: int.Parse(value) / 10000
        var lumaResult = lumaMethod.Invoke(null, new object[] { "10000" })!;
        var lumaNum = (int)avRationalType.GetField("num")!.GetValue(lumaResult)!;
        var lumaDen = (int)avRationalType.GetField("den")!.GetValue(lumaResult)!;
        AssertEqual(10000, lumaNum, "Luminance numerator");
        AssertEqual(10000, lumaDen, "Luminance denominator");

        return Task.CompletedTask;
    }


    // LibAvEncoder: ValidateOptions

    internal static Task LibAvEncoder_ValidateOptions_AcceptsValidOptions()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ValidateOptions not found.");
        var options = CreateValidEncoderOptions();
        method.Invoke(null, new[] { options });
        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_ValidateOptions_RejectsEmptyOutputPath()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
        var options = CreateValidEncoderOptions();
        SetPropertyBackingField(options, "OutputPath", "");
        var threw = false;
        try { method.Invoke(null, new[] { options }); }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException) { threw = true; }
        AssertEqual(true, threw, "Empty OutputPath throws ArgumentException");
        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_ValidateOptions_RejectsZeroDimensions()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
        var options = CreateValidEncoderOptions();
        SetPropertyBackingField(options, "Width", 0);
        var threw = false;
        try { method.Invoke(null, new[] { options }); }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException) { threw = true; }
        AssertEqual(true, threw, "Width=0 throws ArgumentOutOfRangeException");
        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_ValidateOptions_RejectsHdrWithH264()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
        var options = CreateValidEncoderOptions();
        SetPropertyBackingField(options, "HdrEnabled", true);
        SetPropertyBackingField(options, "IsP010", true);
        SetPropertyBackingField(options, "CodecName", "h264_nvenc");
        var threw = false;
        try { method.Invoke(null, new[] { options }); }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) { threw = true; }
        AssertEqual(true, threw, "HDR with H264 throws InvalidOperationException");
        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_ValidateOptions_RejectsHdrWithoutP010()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
        var options = CreateValidEncoderOptions();
        SetPropertyBackingField(options, "HdrEnabled", true);
        SetPropertyBackingField(options, "IsP010", false);
        SetPropertyBackingField(options, "CodecName", "hevc_nvenc");
        var threw = false;
        try { method.Invoke(null, new[] { options }); }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) { threw = true; }
        AssertEqual(true, threw, "HDR without P010 throws InvalidOperationException");
        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_ValidateOptions_RejectsMismatchedFrameRateParts()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
        var options = CreateValidEncoderOptions();
        SetPropertyBackingField(options, "FrameRateNumerator", (int?)60000);
        SetPropertyBackingField(options, "FrameRateDenominator", (int?)null);
        var threw = false;
        try { method.Invoke(null, new[] { options }); }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException) { threw = true; }
        AssertEqual(true, threw, "Mismatched FrameRate parts throws ArgumentException");
        return Task.CompletedTask;
    }




    private static string ReadLibAvEncoderSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.Audio.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static object CreateValidEncoderOptions()
    {
        var optionsType = RequireType("Sussudio.Services.Recording.LibAvEncoderOptions");
        var options = RuntimeHelpers.GetUninitializedObject(optionsType);
        SetPropertyBackingField(options, "OutputPath", "/output/test.mp4");
        SetPropertyBackingField(options, "CodecName", "hevc_nvenc");
        SetPropertyBackingField(options, "Width", 1920);
        SetPropertyBackingField(options, "Height", 1080);
        SetPropertyBackingField(options, "FrameRate", 60.0);
        SetPropertyBackingField(options, "BitRate", (uint)50_000_000);
        SetPropertyBackingField(options, "AudioEnabled", false);
        SetPropertyBackingField(options, "HdrEnabled", false);
        return options;
    }

    internal static Task LibAvEncoder_RetainsAudioEvidenceAfterNativeCleanup()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var encoder = RuntimeHelpers.GetUninitializedObject(encoderType);
        encoderType.GetField("_audioSamplesReceived", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(encoder, 576000L);
        encoderType.GetField("_micSamplesReceived", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(encoder, 575520L);
        var cleanup = encoderType.GetMethod("ReleaseNativeResources", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var args = new object?[] { false, null };
        AssertEqual(575520L, (long)cleanup.Invoke(encoder, args)!, "Cleanup microphone evidence");
        Assert.Null(args[1]);
        AssertEqual(576000L, (long)encoderType.GetProperty("AudioSamplesReceived")!.GetValue(encoder)!, "Final game-audio samples");
        AssertEqual(575520L, (long)encoderType.GetProperty("MicrophoneSamplesReceived")!.GetValue(encoder)!, "Final microphone samples");
        cleanup.Invoke(encoder, args);
        AssertEqual(575520L, (long)encoderType.GetProperty("MicrophoneSamplesReceived")!.GetValue(encoder)!, "Repeated cleanup preserves evidence");

        var source = ReadLibAvEncoderSource();
        AssertContains(source, "if (options.ContainerFormat == \"mpegts\" || options.FragmentedMp4)");
        AssertContains(source, "av_opt_set_int(codecContext->priv_data, \"delay\", 0, 0)");
        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_MuxPhaseTimingSnapshot_PublishesSuppliedValues()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var encoder = RuntimeHelpers.GetUninitializedObject(encoderType);
        var publishMethod = encoderType.GetMethod("PublishMuxPhaseTiming", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("LibAvEncoder.PublishMuxPhaseTiming not found.");
        var operationStartedAt = System.Diagnostics.Stopwatch.GetTimestamp();

        publishMethod.Invoke(encoder, new object[]
        {
            "unit_test",
            1.5d,
            2.5d,
            3.5d,
            4.5d,
            5.5d,
            operationStartedAt
        });

        var timing = encoderType.GetProperty("LastMuxPhaseTiming", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(encoder)!;
        AssertEqual("unit_test", GetStringProperty(timing, "Operation"), "Mux timing operation");
        AssertEqual(1.5d, (double)GetPropertyValue(timing, "DrainMs")!, "Mux timing drain phase");
        AssertEqual(2.5d, (double)GetPropertyValue(timing, "TrailerMs")!, "Mux timing trailer phase");
        AssertEqual(3.5d, (double)GetPropertyValue(timing, "CloseIoMs")!, "Mux timing close-I/O phase");
        AssertEqual(4.5d, (double)GetPropertyValue(timing, "OpenOutputMs")!, "Mux timing open-output phase");
        AssertEqual(5.5d, (double)GetPropertyValue(timing, "HeaderMs")!, "Mux timing header phase");
        Assert.True((double)GetPropertyValue(timing, "TotalMs")! >= 0, "Mux timing total phase");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_SegmentRotationKeepsTotalBytesWrittenMonotonic()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
            ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
        var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
            ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");

        updateDiskBytes.Invoke(manager, new object[] { 1000L });
        var completedPath = Path.Combine(tempDir, "completed-0.ts");
        File.WriteAllBytes(completedPath, new byte[] { 0x47 });
        onSegmentCompleted.Invoke(manager, new object[]
        {
            completedPath,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            1200L
        });
        AssertEqual(1200L, GetLongProperty(manager, "TotalBytesWritten"), "Final segment bytes counted at rotation");

        updateDiskBytes.Invoke(manager, new object[] { 100L });
        AssertEqual(1300L, GetLongProperty(manager, "TotalBytesWritten"), "First bytes from next segment counted after rotation");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_SamePathCompletionExtendsLatestSegment()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");
            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
            var getCompletedSegmentPaths = manager.GetType().GetMethod("GetExistingCompletedSegmentPathsInRange")
                ?? throw new InvalidOperationException("FlashbackBufferManager.GetExistingCompletedSegmentPathsInRange not found.");
            var getSegmentInfoList = manager.GetType().GetMethod("GetSegmentInfoList")
                ?? throw new InvalidOperationException("FlashbackBufferManager.GetSegmentInfoList not found.");

            var activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(activePath, new byte[] { 0x47 });

            updateDiskBytes.Invoke(manager, new object[] { 1000L });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                activePath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(10),
                1000L
            });
            AssertEqual(1000L, GetLongProperty(manager, "TotalDiskBytes"), "Initial same-path completion tracks one physical active file");
            AssertEqual(1000L, GetLongProperty(manager, "TotalBytesWritten"), "Initial same-path completion does not double count active bytes");

            updateDiskBytes.Invoke(manager, new object[] { 1500L });
            AssertEqual(1500L, GetLongProperty(manager, "TotalDiskBytes"), "Same active file growth is counted as a delta after completion");
            AssertEqual(1500L, GetLongProperty(manager, "TotalBytesWritten"), "Same active file growth advances monotonic bytes by delta");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                Path.Combine(tempDir, ".", Path.GetFileName(activePath)),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(20),
                2000L
            });

            var paths = ((IEnumerable<string>)getCompletedSegmentPaths.Invoke(manager, new object[]
            {
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(19)
            })!).ToArray();
            AssertEqual(1, paths.Length, "Extended same-path segment remains exportable for tail range");
            AssertEqual(activePath, paths[0], "Extended same-path segment export path");
            AssertEqual(1, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Same-path extension keeps original segment sequence");
            AssertEqual(2000L, GetLongProperty(manager, "TotalDiskBytes"), "Extended same-path completion updates completed disk bytes");
            AssertEqual(2000L, GetLongProperty(manager, "TotalBytesWritten"), "Extended same-path completion advances monotonic bytes by growth delta");

            var infos = ((System.Collections.IEnumerable)getSegmentInfoList.Invoke(manager, Array.Empty<object>())!)
                .Cast<object>()
                .ToArray();
            var completedInfo = infos.First(info => GetPropertyValue(info, "IsActive") is false);
            AssertEqual(0L, (long)GetPropertyValue(completedInfo, "StartPtsMs")!, "Extended segment keeps original start");
            AssertEqual(20_000L, (long)GetPropertyValue(completedInfo, "EndPtsMs")!, "Extended segment updates end");
            AssertEqual(2000L, (long)GetPropertyValue(completedInfo, "SizeBytes")!, "Extended segment updates size");
        }
        finally
        {
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_SegmentCompletionRejectsInvalidMetadata()
    {
        var source = ReadFlashbackBufferManagerSource();

        AssertContains(source, "if (string.IsNullOrWhiteSpace(path))\n        {\n            Logger.Log(\"FLASHBACK_BUFFER_SEGMENT_SKIP reason=empty_path\");\n            return;\n        }");
        AssertContains(source, "if (endPts <= startPts)\n        {\n            Logger.Log($\"FLASHBACK_BUFFER_SEGMENT_SKIP reason=invalid_range path='{Path.GetFileName(path)}' start_ms={(long)startPts.TotalMilliseconds} end_ms={(long)endPts.TotalMilliseconds}\");\n            return;\n        }");
        AssertContains(source, "if (!IsPathInSessionDirectory(path))");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_SKIP reason=outside_session");
        AssertContains(source, "if (!File.Exists(path))");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_SKIP reason=missing_file");
        AssertContains(source, "var existingIndex = _completedSegments.FindIndex(seg => IsSameSegmentPath(seg.Path, path));");
        AssertContains(source, "if (existingIndex >= 0)");
        AssertContains(source, "if (!TryExtendCompletedSegment(existingIndex, path, startPts, endPts, safeSizeBytes, pathIsActiveSegment, ref pendingDeletes))");
        AssertContains(source, "private bool TryExtendCompletedSegment(");
        AssertContains(source, "if (!pathIsActiveSegment && !existing.AllowSamePathExtension)");
        AssertContains(source, "AllowSamePathExtension = pathIsActiveSegment");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_EXTEND");
        AssertContains(source, "if (_completedSegments.Count > 0 && startPts < _completedSegments[^1].EndPts)");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_SKIP reason=non_monotonic");
        AssertContains(source, "private bool IsPathInSessionDirectory(string path)");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_PATH_WARN");
        AssertContains(source, "var safeSizeBytes = Math.Max(0, sizeBytes);");
        AssertContains(source, "private int _completedSegmentSequence;");
        AssertContains(source, "var sequenceNumber = _completedSegmentSequence++;");
        AssertContains(source, "_completedSegments.Add(new CompletedSegment(path, sequenceNumber, startPts, endPts, safeSizeBytes)");
        AssertContains(source, "AllowSamePathExtension = pathIsActiveSegment");
        AssertContains(source, "_completedSegmentBytes = AddNonNegativeSaturated(_completedSegmentBytes, safeSizeBytes);");
        AssertContains(source, "_previousActiveSegmentBytes = pathIsActiveSegment ? safeSizeBytes : 0;");
        AssertContains(source, "_completedSegmentSequence = 0;");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var manager = CreateInitializedBufferManager(tempDir);
            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");

            var missingSegmentPath = Path.Combine(tempDir, "segment-missing.ts");
            onSegmentCompleted.Invoke(manager, new object[]
            {
                missingSegmentPath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5),
                1000L
            });

            AssertEqual(0, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Missing segment should not allocate sequence");
            AssertEqual(0L, GetLongProperty(manager, "TotalBytesWritten"), "Missing segment should not update bytes");

            var segment0Path = Path.Combine(tempDir, "segment-0.ts");
            File.WriteAllBytes(segment0Path, new byte[] { 0x47 });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                segment0Path,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5),
                1000L
            });
            var overlappingSegmentPath = Path.Combine(tempDir, "segment-overlap.ts");
            File.WriteAllBytes(overlappingSegmentPath, new byte[] { 0x47 });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                Path.Combine(tempDir, ".", "segment-0.ts"),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(6),
                1000L
            });

            AssertEqual(1, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Duplicate segment path should not allocate sequence");
            AssertEqual(1000L, GetLongProperty(manager, "TotalBytesWritten"), "Duplicate segment path should not update bytes");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                Path.Combine(tempDir, ".", "segment-0.ts"),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(8),
                1500L
            });

            AssertEqual(1, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Non-active duplicate segment growth should not allocate sequence");
            AssertEqual(1000L, GetLongProperty(manager, "TotalBytesWritten"), "Non-active duplicate segment growth should not update bytes");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                overlappingSegmentPath,
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(7),
                1000L
            });

            AssertEqual(1, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Overlapping segment should not allocate sequence");
            AssertEqual(1000L, GetLongProperty(manager, "TotalBytesWritten"), "Overlapping segment should not update bytes");
        }
        finally
        {
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_SegmentCompletionRejectsOutsidePaths()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        var outsideDir = Path.Combine(Path.GetTempPath(), $"fbtest_outside_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(outsideDir);

        try
        {
            var manager = CreateInitializedBufferManager(tempDir);
            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");

            var outsidePath = Path.Combine(outsideDir, "outside.ts");
            onSegmentCompleted.Invoke(manager, new object[]
            {
                outsidePath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                1200L
            });

            AssertEqual(0L, GetLongProperty(manager, "TotalBytesWritten"), "Outside segment path should not update bytes");
            AssertEqual(0, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Outside segment path should not allocate sequence");
        }
        finally
        {
            global::Program.TryDeleteDirectory(tempDir);
            global::Program.TryDeleteDirectory(outsideDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_TryDeleteFileRejectsOutsidePaths()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        var outsideDir = Path.Combine(Path.GetTempPath(), $"fbdelete_outside_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(outsideDir);

        try
        {
            var manager = CreateInitializedBufferManager(tempDir);
            var tryDeleteFile = manager.GetType().GetMethod("TryDeleteFile", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackBufferManager.TryDeleteFile not found.");

            var outsidePath = Path.Combine(outsideDir, "outside.ts");
            File.WriteAllText(outsidePath, "keep");

            var result = (bool)tryDeleteFile.Invoke(manager, new object[] { outsidePath })!;
            AssertEqual(false, result, "Outside delete should be rejected");
            AssertEqual(true, File.Exists(outsidePath), "Outside delete should preserve file");

            var source = ReadFlashbackBufferManagerSource();
            AssertContains(source, "FLASHBACK_BUFFER_DELETE_SKIP reason=outside_session");
            AssertOccursBefore(source, "FLASHBACK_BUFFER_DELETE_SKIP reason=outside_session", "File.Delete(filePath);");
        }
        finally
        {
            global::Program.TryDeleteDirectory(tempDir);
            global::Program.TryDeleteDirectory(outsideDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_IgnoresUpdatesAfterDispose()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_disposed_{Guid.NewGuid():N}");
        var manager = CreateInitializedBufferManager(tempDir);

        var updateLatestPts = manager.GetType().GetMethod("UpdateLatestPts")
            ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateLatestPts not found.");
        var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
            ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
        var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
            ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");

        ((IDisposable)manager).Dispose();

        updateLatestPts.Invoke(manager, new object[] { TimeSpan.FromSeconds(5) });
        updateDiskBytes.Invoke(manager, new object[] { 4096L });
        onSegmentCompleted.Invoke(manager, new object[]
        {
            Path.Combine(tempDir, "completed-after-dispose.ts"),
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            1200L
        });

        AssertEqual(TimeSpan.Zero, (TimeSpan)GetPropertyValue(manager, "LatestPts")!, "Disposed manager ignores latest PTS updates");
        AssertEqual(0L, GetLongProperty(manager, "TotalBytesWritten"), "Disposed manager ignores disk and segment byte updates");
        AssertEqual(0, (int)GetPrivateField(manager, "_completedSegmentSequence")!, "Disposed manager does not allocate segment sequence");

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "private volatile bool _disposed;");
        AssertContains(source, "FLASHBACK_BUFFER_SEGMENT_SKIP reason=disposed");
        AssertContains(source, "public void UpdateLatestPts(TimeSpan pts)\n    {\n        if (_disposed)\n        {\n            return;\n        }");
        AssertContains(source, "public void UpdateDiskBytes(long activeSegmentBytes)\n    {\n        if (_disposed)\n        {\n            return;\n        }");

        global::Program.TryDeleteDirectory(tempDir);
        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_IgnoresDestructiveOperationsAfterDispose()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_disposed_purge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var completedPath = Path.Combine(tempDir, "segment-0.ts");
        var activePath = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(completedPath, "segment");
        File.WriteAllText(activePath, "active");
        AddCompletedSegment(manager, completedPath, TimeSpan.Zero, TimeSpan.FromSeconds(1), 7);

        var purgeCompleted = manager.GetType().GetMethod("PurgeCompletedSegments")
            ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeCompletedSegments not found.");
        var purgeAll = manager.GetType().GetMethod("PurgeAllSegments")
            ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeAllSegments not found.");
        var abandonGenerated = manager.GetType().GetMethod("AbandonGeneratedSegmentPath")
            ?? throw new InvalidOperationException("FlashbackBufferManager.AbandonGeneratedSegmentPath not found.");
        var finalizeCycle = manager.GetType().GetMethod("FinalizeActiveSegmentForCycle")
            ?? throw new InvalidOperationException("FlashbackBufferManager.FinalizeActiveSegmentForCycle not found.");

        ((IDisposable)manager).Dispose();

        purgeCompleted.Invoke(manager, null);
        purgeAll.Invoke(manager, null);
        abandonGenerated.Invoke(manager, new object?[] { activePath, null });
        finalizeCycle.Invoke(manager, null);

        AssertEqual(false, File.Exists(completedPath), "Dispose purges completed segment before post-dispose purge attempts");
        AssertEqual(false, File.Exists(activePath), "Dispose purges active segment before post-dispose purge attempts");
        AssertEqual(0, GetIntProperty(manager, "SegmentCount"), "Disposed destructive operations keep the disposed empty index stable");
        AssertEqual(string.Empty, GetStringProperty(manager, "ActiveFilePath"), "Disposed destructive operations keep active path cleared");

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "FLASHBACK_PURGE_SKIP reason=disposed");
        AssertContains(source, "FLASHBACK_BUFFER_PURGE_SKIP reason=disposed");

        global::Program.TryDeleteDirectory(tempDir);
        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_PreservesMarkedRecoverySessions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_recovery_preserve_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var completedPath = Path.Combine(tempDir, "segment-0.ts");
        var activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
        File.WriteAllText(completedPath, "segment");
        File.WriteAllText(activePath, "active");
        AddCompletedSegment(manager, completedPath, TimeSpan.Zero, TimeSpan.FromSeconds(1), 7);

        var markPreserved = manager.GetType().GetMethod("MarkSessionPreservedForRecovery")
            ?? throw new InvalidOperationException("FlashbackBufferManager.MarkSessionPreservedForRecovery not found.");
        var purgeAll = manager.GetType().GetMethod("PurgeAllSegments")
            ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeAllSegments not found.");

        markPreserved.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "IsSessionPreservedForRecovery"), "Recovery-preserved manager exposes preserved state");
        SetPrivateField(manager, "_validStartPtsTicks", TimeSpan.FromSeconds(2).Ticks);
        InvokeNonPublicInstanceMethod(manager, "EvictOldestSegments", null);

        AssertEqual(true, File.Exists(completedPath), "Recovery-preserved completed segment survives normal eviction");

        purgeAll.Invoke(manager, null);

        AssertEqual(true, File.Exists(completedPath), "Recovery-preserved completed segment survives explicit purge");
        AssertEqual(true, File.Exists(activePath), "Recovery-preserved active segment survives explicit purge");

        ((IDisposable)manager).Dispose();

        AssertEqual(true, Directory.Exists(tempDir), "Recovery-preserved session directory survives dispose");
        AssertEqual(true, File.Exists(Path.Combine(tempDir, ".flashback-recovery-preserve")), "Recovery marker survives dispose");
        AssertEqual(true, File.Exists(completedPath), "Recovery-preserved completed segment survives dispose");
        AssertEqual(true, File.Exists(activePath), "Recovery-preserved active segment survives dispose");

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "private bool _preserveSessionForRecovery;");
        AssertContains(source, "private bool IsSessionPreservedForRecoveryUnsafe()");
        AssertContains(source, "FLASHBACK_BUFFER_PURGE_SKIP reason=recovery_preserved");
        AssertContains(source, "FLASHBACK_BUFFER_EVICT_SKIP reason=recovery_preserved");
        AssertContains(source, "FLASHBACK_BUFFER_DISPOSE_PRESERVE_RECOVERY");

        global::Program.TryDeleteDirectory(tempDir);
        return Task.CompletedTask;
    }



    internal static Task FlashbackBufferManager_GetSegmentFileForPosition_ReturnsCorrectSegment()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "public string? GetSegmentFileForPosition(TimeSpan absolutePts)\n        => ResolvePlaybackSegmentPathWithFallback(absolutePts);");

        // Add 3 segments: 0-5s, 5-10s, 10-15s
        var seg0 = Path.Combine(tempDir, "seg0.ts");
        var seg1 = Path.Combine(tempDir, "seg1.ts");
        var seg2 = Path.Combine(tempDir, "seg2.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(seg0, "segment");
        File.WriteAllText(seg1, "segment");
        File.WriteAllText(seg2, "segment");
        File.WriteAllText(active, "active");
        AddCompletedSegment(manager, seg0, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 1000);
        AddCompletedSegment(manager, seg1, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 1000);
        AddCompletedSegment(manager, seg2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 1000);

        var method = manager.GetType().GetMethod("GetSegmentFileForPosition")!;

        // Position 7s → segment 1 (5-10s)
        var result1 = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(7) }) as string;
        AssertEqual(seg1, result1!, "Position 7s");

        // Position 0s → segment 0 (0-5s)
        var result2 = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(0) }) as string;
        AssertEqual(seg0, result2!, "Position 0s");

        // Position 20s → not in any completed segment → falls back to active
        var result3 = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(20) }) as string;
        AssertContains(result3!, "fb_test_0003.ts");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_ResolvePlaybackSegmentPathWithFallback_SkipsMissingFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var missingOldest = Path.Combine(tempDir, "missing-oldest.ts");
        var existingFallback = Path.Combine(tempDir, "existing-fallback.ts");
        File.WriteAllText(existingFallback, "segment");

        AddCompletedSegment(manager, missingOldest, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, existingFallback, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var method = manager.GetType().GetMethod("ResolvePlaybackSegmentPathWithFallback")!;

        var fallback = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(2) }) as string;
        AssertEqual(existingFallback, fallback!, "Missing target should fall back to first existing completed segment");

        File.Delete(existingFallback);
        var missingAll = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(2) }) as string;
        AssertEqual(null, missingAll, "Missing completed and active segments should return null");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_ResolvePlaybackSegmentPathWithFallback_StaleLeftEdgeUsesOldest()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var oldest = Path.Combine(tempDir, "oldest.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(oldest, "oldest");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, oldest, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var method = manager.GetType().GetMethod("ResolvePlaybackSegmentPathWithFallback")!;
        var fallback = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(1) }) as string;

        AssertEqual(oldest, fallback!, "Position before first segment should use oldest existing segment, not active");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_GetNextSegmentFile_WalksForward()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var a = Path.Combine(tempDir, "a.ts");
        var b = Path.Combine(tempDir, "b.ts");
        var c = Path.Combine(tempDir, "c.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(a, "a");
        File.WriteAllText(b, "b");
        File.WriteAllText(c, "c");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, a, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, b, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);
        AddCompletedSegment(manager, c, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 500);

        var method = manager.GetType().GetMethod("GetNextSegmentFile")!;

        var nextA = method.Invoke(manager, new object[] { a }) as string;
        AssertEqual(b, nextA!, "a to b");

        var nextB = method.Invoke(manager, new object[] { b }) as string;
        AssertEqual(c, nextB!, "b to c");

        var nextC = method.Invoke(manager, new object[] { c }) as string;
        AssertContains(nextC!, "fb_test_0003.ts");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_SegmentPathLookupsNormalizeEquivalentPaths()
    {
        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "private static bool IsSameSegmentPath(string? left, string? right)");
        AssertContains(source, "Path.GetFullPath(left)");
        AssertContains(source, "Path.GetFullPath(right)");
        AssertContains(source, "FLASHBACK_BUFFER_PATH_COMPARE_WARN");
        AssertContains(source, "if (IsSameSegmentPath(completedPaths[i], currentPath))");
        AssertContains(source, "if (IsSameSegmentPath(seg.Path, path))");
        AssertContains(source, "IsSameSegmentPath(_activeSegmentPath, path) &&");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var a = Path.Combine(tempDir, "a.ts");
        var b = Path.Combine(tempDir, "b.ts");
        File.WriteAllText(a, "a");
        File.WriteAllText(b, "b");

        AddCompletedSegment(manager, a, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, b, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var equivalentA = Path.Combine(tempDir, ".", "a.ts");
        var nextMethod = manager.GetType().GetMethod("GetNextSegmentFile")!;
        var next = nextMethod.Invoke(manager, new object[] { equivalentA }) as string;
        AssertEqual(b, next!, "Equivalent completed segment path should walk to next segment");

        var startMethod = manager.GetType().GetMethod("GetSegmentStartPts")!;
        var start = (TimeSpan?)startMethod.Invoke(manager, new object[] { equivalentA });
        AssertEqual(TimeSpan.Zero, start!.Value, "Equivalent completed segment path should resolve start PTS");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_GetSegmentStartPts_SkipsMissingFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var missingCompleted = Path.Combine(tempDir, "missing-completed.ts");
        var existingCompleted = Path.Combine(tempDir, "existing-completed.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(existingCompleted, "segment");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, missingCompleted, TimeSpan.Zero, TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, existingCompleted, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var method = manager.GetType().GetMethod("GetSegmentStartPts")!;

        var missingStart = (TimeSpan?)method.Invoke(manager, new object[] { missingCompleted });
        AssertEqual(null, missingStart, "Missing completed segment should not expose start PTS");

        var existingStart = (TimeSpan?)method.Invoke(manager, new object[] { existingCompleted });
        AssertEqual(TimeSpan.FromSeconds(5), existingStart!.Value, "Existing completed segment should expose start PTS");

        manager.GetType().GetMethod("MarkActiveSegmentStart")!
            .Invoke(manager, new object[] { active, TimeSpan.FromSeconds(12) });
        var activeStart = (TimeSpan?)method.Invoke(manager, new object[] { active });
        AssertEqual(TimeSpan.FromSeconds(12), activeStart!.Value, "Active segment should expose marked encoder start PTS");

        File.Delete(active);
        var missingActiveStart = (TimeSpan?)method.Invoke(manager, new object[] { active });
        AssertEqual(null, missingActiveStart, "Missing active segment should not expose start PTS");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_GetNextSegmentFile_SkipsMissingIndexedSegments()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var current = Path.Combine(tempDir, "current.ts");
        var missingNext = Path.Combine(tempDir, "missing-next.ts");
        var existingNext = Path.Combine(tempDir, "existing-next.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(current, "current");
        File.WriteAllText(existingNext, "next");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, current, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, missingNext, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);
        AddCompletedSegment(manager, existingNext, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 500);

        var method = manager.GetType().GetMethod("GetNextSegmentFile")!;
        var next = method.Invoke(manager, new object[] { current }) as string;

        AssertEqual(existingNext, next!, "Next segment lookup should skip missing indexed segment");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_GetExistingCompletedSegmentPathsInRange_ReturnsOverlapping()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var s0 = Path.Combine(tempDir, "s0.ts");
        var s1 = Path.Combine(tempDir, "s1.ts");
        var s2 = Path.Combine(tempDir, "s2.ts");
        var s3 = Path.Combine(tempDir, "s3.ts");
        File.WriteAllText(s0, "segment");
        File.WriteAllText(s1, "segment");
        File.WriteAllText(s2, "segment");
        File.WriteAllText(s3, "segment");

        AddCompletedSegment(manager, s0, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, s1, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);
        AddCompletedSegment(manager, s2, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 500);
        AddCompletedSegment(manager, s3, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20), 500);

        var method = manager.GetType().GetMethod("GetExistingCompletedSegmentPathsInRange")!;

        var result = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(12) })!;
        AssertEqual(3, GetCountProperty(result), "3s-12s should span 3 segments");

        var narrow = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5.5) })!;
        AssertEqual(1, GetCountProperty(narrow), "5s-5.5s should be 1 segment");

        File.Delete(s1);
        var missing = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5.5) })!;
        AssertEqual(0, GetCountProperty(missing), "Missing overlapping file should not be returned");

        var emptyRange = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(8) })!;
        AssertEqual(0, GetCountProperty(emptyRange), "Empty range should not return segments");

        var invertedRange = method.Invoke(manager, new object[] { TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(3) })!;
        AssertEqual(0, GetCountProperty(invertedRange), "Inverted range should not return segments");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_GetSegmentInfoList_SkipsMissingFiles()
    {
        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "if (_activeSegmentPath is { } activePath)");
        AssertContains(source, "return result\n            .Where(segment => File.Exists(segment.Path))\n            .ToList();");
        AssertContains(source, "SequenceNumber = Math.Max(0, _nextSegmentIndex - 1),");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var missingCompleted = Path.Combine(tempDir, "missing-completed.ts");
        var existingCompleted = Path.Combine(tempDir, "existing-completed.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(existingCompleted, "segment");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, missingCompleted, TimeSpan.Zero, TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, existingCompleted, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        var method = manager.GetType().GetMethod("GetSegmentInfoList")!;
        var result = method.Invoke(manager, null)!;

        AssertEqual(2, GetCountProperty(result), "Segment info should include existing completed plus active");
        var infos = ((System.Collections.IEnumerable)result).Cast<object>().ToArray();
        var activeInfo = infos.Single(info => GetBoolProperty(info, "IsActive"));
        AssertEqual(3, GetIntProperty(activeInfo, "SequenceNumber"), "Active segment sequence should match current generated segment index");
        AssertEqual(10_000L, GetLongProperty(activeInfo, "StartPtsMs"), "Unmarked active segment start should fall back to completed end");

        manager.GetType().GetMethod("MarkActiveSegmentStart")!
            .Invoke(manager, new object[] { active, TimeSpan.FromSeconds(12) });
        var markedResult = method.Invoke(manager, null)!;
        var markedActiveInfo = ((System.Collections.IEnumerable)markedResult)
            .Cast<object>()
            .Single(info => GetBoolProperty(info, "IsActive"));
        AssertEqual(12_000L, GetLongProperty(markedActiveInfo, "StartPtsMs"), "Marked active segment start should follow encoder PTS");

        File.Delete(active);
        var withoutActive = method.Invoke(manager, null)!;

        AssertEqual(1, GetCountProperty(withoutActive), "Segment info should omit missing active file");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_ActiveFilePath_RequiresExistingFile()
    {
        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "return IsExistingPath(activePath)\n                ? activePath\n                : null;");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        AssertEqual(null, GetPropertyValue(manager, "ActiveFilePath"), "Missing active file should not be exposed");

        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(active, "active");

        AssertEqual(active, (string)GetPropertyValue(manager, "ActiveFilePath")!, "Existing active file should be exposed");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_SegmentCount_SkipsMissingFiles()
    {
        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "completedPaths = _completedSegments.Select(seg => seg.Path).ToList();");
        AssertContains(source, "return completedPaths.Count(File.Exists) +\n                (IsExistingPath(activePath) ? 1 : 0);");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        var missingCompleted = Path.Combine(tempDir, "missing-completed.ts");
        var existingCompleted = Path.Combine(tempDir, "existing-completed.ts");
        var active = Path.Combine(tempDir, "fb_test_0003.ts");
        File.WriteAllText(existingCompleted, "segment");
        File.WriteAllText(active, "active");

        AddCompletedSegment(manager, missingCompleted, TimeSpan.Zero, TimeSpan.FromSeconds(5), 500);
        AddCompletedSegment(manager, existingCompleted, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 500);

        AssertEqual(2, GetIntProperty(manager, "SegmentCount"), "Segment count should include existing completed plus active");

        File.Delete(active);

        AssertEqual(1, GetIntProperty(manager, "SegmentCount"), "Segment count should omit missing active file");

        return Task.CompletedTask;
    }
    internal static Task FlashbackBufferManager_CleansStaleSessionDirectories()
    {
        var bufferText = ReadFlashbackBufferManagerSource();
        var cleanupText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackStartupCacheCleanup.cs")
            .Replace("\r\n", "\n");
        var budgetText = cleanupText;
        var scannerText = cleanupText
            .Replace("\r\n", "\n");
        var playbackSegmentEdgesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var playbackSegmentSwitchText = playbackSegmentEdgesText;
        var decoderSegmentReopenText = playbackSegmentEdgesText;

        AssertContains(cleanupText, "internal static readonly TimeSpan StaleSessionMinAge = TimeSpan.FromHours(12);");
        AssertContains(cleanupText, "private const int MaxStaleSessionDirectoryScansPerInit = 64;");
        AssertContains(budgetText, "private const int MaxStartupCacheSessionDirectoryScansPerInit = 256;");
        AssertContains(budgetText, "private const int MaxStartupCacheSessionDirectoriesPerInit = 32;");
        AssertContains(budgetText, "private const long StartupCacheBudgetMultiplier = 2;");
        AssertContains(cleanupText, "private const int MaxStaleRootSegmentFileScansPerInit = 512;");

        AssertContains(bufferText, "FlashbackStartupCacheCleanup.CleanupStaleRootSegmentFiles(tempDirectory);");
        AssertContains(bufferText, "FlashbackStartupCacheCleanup.CleanupStaleSessionDirectories(tempDirectory, sessionDirectory);");
        AssertContains(bufferText, "var cacheCleanup = FlashbackStartupSessionCacheBudget.CleanupSessionCacheBudget(");
        AssertContains(bufferText, "FlashbackStartupSessionCacheBudget.CalculateStartupTempCacheBudgetBytes(_options.MaxDiskBytes));");
        AssertContains(bufferText, "var sessionDirectory = FlashbackSessionRecoveryScanner.BuildSessionDirectory(tempDirectory, sessionId);");

        AssertContains(scannerText, "internal static string BuildSessionDirectory(string tempDirectory, string sessionId)");
        AssertContains(scannerText, "Session id must be a simple file-name component.");
        AssertContains(scannerText, "Session id must resolve inside the flashback temp directory.");
        AssertContains(scannerText, "internal static string NormalizeSegmentExtension(string extension)");
        AssertContains(scannerText, "Flashback segment extension must be .ts or .mp4.");
        AssertContains(scannerText, "internal static bool IsPathUnderDirectory(string fullPath, string fullDirectoryRoot)");
        AssertContains(scannerText, "internal static bool IsReparsePoint(FileSystemInfo info)");
        AssertContains(scannerText, "internal static bool IsPlausibleFlashbackSessionDirectoryName(string name)");

        AssertContains(bufferText, "var normalizedExtension = FlashbackSessionRecoveryScanner.NormalizeSegmentExtension(extension);");
        AssertContains(bufferText, "public long TempDriveAvailableFreeBytes => ProbeFreeDiskBytes();");

        AssertContains(cleanupText, "FLASHBACK_STALE_SESSION_SKIP reason=reparse_point");
        AssertContains(cleanupText, "FLASHBACK_STALE_SESSION_SKIP reason=unrecognized_empty_dir");
        AssertContains(budgetText, "FLASHBACK_CACHE_BUDGET_SKIP reason=outside_temp");
        AssertContains(budgetText, "FLASHBACK_SESSION_STATS_SKIP reason=reparse_point");
        AssertContains(cleanupText, "if (string.Equals(fullPath, currentFullPath, StringComparison.OrdinalIgnoreCase))");
        AssertContains(budgetText, "FLASHBACK_CACHE_BUDGET_PRESERVE_SKIP");
        AssertContains(budgetText, "FLASHBACK_CACHE_BUDGET_CLEANUP");
        AssertContains(cleanupText, "info.EnumerateFiles(\"fb_*\", SearchOption.TopDirectoryOnly)");
        AssertContains(cleanupText, "Directory.EnumerateFiles(tempDirectory, \"fb_*\", SearchOption.TopDirectoryOnly)");
        AssertContains(cleanupText, "Directory.Delete(fullPath, recursive: true);");

        AssertContains(bufferText, "if (IsSameSegmentPath(activePath, currentPath))\n            return IsExistingPath(activePath) ? activePath : null;");
        AssertContains(bufferText, "return GetOldestExistingSegmentPath(completedPaths)\n            ?? (IsExistingPath(activePath) ? activePath : null);");
        AssertContains(bufferText, "public TimeSpan? GetSegmentStartPts(string path)");
        AssertContains(playbackSegmentEdgesText, "TrySwitchToNextSegment(");
        AssertContains(playbackSegmentSwitchText, "var nextSegmentStart = _bufferManager.GetSegmentStartPts(nextFile);");
        AssertContains(playbackSegmentSwitchText, "if (nextSegmentStart.HasValue && segSwitchTarget < nextSegmentStart.Value)");
        AssertContains(decoderSegmentReopenText, "var currentSegmentStart = _bufferManager.GetSegmentStartPts(currentOpenFilePath);");
        AssertContains(decoderSegmentReopenText, "if (currentSegmentStart.HasValue && resumeTarget < currentSegmentStart.Value)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_AbandonsStartupGeneratedSegmentPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_startup_abandon_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            SetPrivateField(manager, "_activeSegmentPath", null);
            var startingIndex = (int)GetPrivateField(manager, "_nextSegmentIndex")!;
            var getFilePath = manager.GetType().GetMethod("AcquireSegmentPath", new[] { typeof(bool).MakeByRefType() })
                ?? throw new InvalidOperationException("FlashbackBufferManager.AcquireSegmentPath(out bool) not found.");
            var abandonGenerated = manager.GetType().GetMethod("AbandonGeneratedSegmentPath")
                ?? throw new InvalidOperationException("FlashbackBufferManager.AbandonGeneratedSegmentPath not found.");

            object?[] args = { false };
            var generatedPath = (string)getFilePath.Invoke(manager, args)!;
            AssertEqual(true, (bool)args[0]!, "Fresh AcquireSegmentPath reports generated path");
            AssertEqual(generatedPath, (string)GetPrivateField(manager, "_activeSegmentPath")!, "Generated path becomes raw active segment");
            AssertEqual(startingIndex + 1, (int)GetPrivateField(manager, "_nextSegmentIndex")!, "Generated path advances segment index");

            File.WriteAllBytes(generatedPath, new byte[17]);
            abandonGenerated.Invoke(manager, new object?[] { generatedPath, null });

            AssertEqual<string?>(null, (string?)GetPrivateField(manager, "_activeSegmentPath"), "Abandon clears startup-generated active path");
            AssertEqual(false, File.Exists(generatedPath), "Abandon deletes partial startup segment file");
            AssertEqual(startingIndex, (int)GetPrivateField(manager, "_nextSegmentIndex")!, "Abandon rolls back generated segment index");

            object?[] retryArgs = { false };
            var retryPath = (string)getFilePath.Invoke(manager, retryArgs)!;
            AssertEqual(true, (bool)retryArgs[0]!, "Retry after abandon generates a fresh path");
            AssertEqual(generatedPath, retryPath, "Retry reuses the rolled-back segment slot");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_RemovesStaleLegacyRootSegments()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_legacy_cleanup_{Guid.NewGuid():N}");
        object? manager = null;
        Directory.CreateDirectory(tempDir);

        try
        {
            var staleRootSegment = Path.Combine(tempDir, "fb_legacy_0001.ts");
            var recentRootSegment = Path.Combine(tempDir, "fb_recent_0001.ts");
            var unrelatedFile = Path.Combine(tempDir, "unrelated.ts");
            File.WriteAllText(staleRootSegment, "stale");
            File.WriteAllText(recentRootSegment, "recent");
            File.WriteAllText(unrelatedFile, "keep");

            File.SetLastWriteTimeUtc(staleRootSegment, DateTime.UtcNow - TimeSpan.FromHours(13));
            File.SetLastWriteTimeUtc(recentRootSegment, DateTime.UtcNow);
            File.SetLastWriteTimeUtc(unrelatedFile, DateTime.UtcNow - TimeSpan.FromHours(13));

            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            manager = Activator.CreateInstance(managerType, new[] { options })!;
            var initialize = managerType.GetMethod("Initialize")
                ?? throw new InvalidOperationException("FlashbackBufferManager.Initialize not found.");
            initialize.Invoke(manager, new object[] { "current-session" });

            AssertEqual(false, File.Exists(staleRootSegment), "Stale root fb_* segment removed");
            AssertEqual(true, File.Exists(recentRootSegment), "Recent root fb_* segment preserved");
            AssertEqual(true, File.Exists(unrelatedFile), "Unrelated root file preserved");
            AssertEqual(true, Directory.Exists(Path.Combine(tempDir, "current-session")), "Current session directory created");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_PreservesUnrelatedEmptyTempDirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_stale_empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var currentSession = Path.Combine(tempDir, Guid.NewGuid().ToString("N"));
            var staleFlashbackSession = Path.Combine(tempDir, Guid.NewGuid().ToString("N"));
            var retiredFlashbackSession = Path.Combine(tempDir, Guid.NewGuid().ToString("N"));
            var unrelatedEmptyDirectory = Path.Combine(tempDir, "empty-but-not-flashback");

            Directory.CreateDirectory(currentSession);
            Directory.CreateDirectory(staleFlashbackSession);
            Directory.CreateDirectory(retiredFlashbackSession);
            Directory.CreateDirectory(unrelatedEmptyDirectory);
            var retiredSegment = Path.Combine(retiredFlashbackSession, "fb_retired_0001.ts");
            File.WriteAllText(retiredSegment, "retired");
            File.WriteAllText(Path.Combine(retiredFlashbackSession, ".flashback-retired-session"), "retired");

            var staleTime = DateTime.UtcNow - TimeSpan.FromHours(13);
            Directory.SetLastWriteTimeUtc(staleFlashbackSession, staleTime);
            File.SetLastWriteTimeUtc(retiredSegment, staleTime);
            Directory.SetLastWriteTimeUtc(retiredFlashbackSession, staleTime);
            Directory.SetLastWriteTimeUtc(unrelatedEmptyDirectory, staleTime);

            var cleanupType = RequireType("Sussudio.Services.Flashback.FlashbackStartupCacheCleanup");
            var cleanup = cleanupType.GetMethod("CleanupStaleSessionDirectories", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException("CleanupStaleSessionDirectories not found.");

            cleanup.Invoke(null, new object[] { tempDir, currentSession });

            AssertEqual(true, Directory.Exists(currentSession), "Current empty session directory preserved");
            AssertEqual(false, Directory.Exists(staleFlashbackSession), "Plausible stale empty flashback session removed");
            AssertEqual(false, Directory.Exists(retiredFlashbackSession), "Retired stale flashback session removed");
            AssertEqual(true, Directory.Exists(unrelatedEmptyDirectory), "Unrelated stale empty directory preserved");

            var cleanupSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackStartupCacheCleanup.cs")
                .Replace("\r\n", "\n");
            var scannerSource = cleanupSource;
            AssertContains(cleanupSource, "FLASHBACK_STALE_SESSION_SKIP reason=unrecognized_empty_dir");
            AssertContains(scannerSource, "internal static bool IsPlausibleFlashbackSessionDirectoryName(string name)");
            AssertContains(scannerSource, "internal static bool IsLowerHexString(ReadOnlySpan<char> value)");
        }
        finally
        {
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_TrimsStartupSessionCacheBudget()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_cache_budget_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var currentSession = Path.Combine(tempDir, "current-session");
            var oldSession = Path.Combine(tempDir, "old-session");
            var recentSession = Path.Combine(tempDir, "recent-session");
            var retiredSession = Path.Combine(tempDir, "retired-session");
            var preservedSession = Path.Combine(tempDir, "preserved-session");
            var nonFlashbackDirectory = Path.Combine(tempDir, "not-flashback");

            Directory.CreateDirectory(currentSession);
            Directory.CreateDirectory(oldSession);
            Directory.CreateDirectory(recentSession);
            Directory.CreateDirectory(retiredSession);
            Directory.CreateDirectory(preservedSession);
            Directory.CreateDirectory(nonFlashbackDirectory);

            WriteSizedFile(Path.Combine(currentSession, "fb_current_0001.ts"), 1);
            WriteSizedFile(Path.Combine(oldSession, "fb_old_0001.ts"), 20);
            WriteSizedFile(Path.Combine(recentSession, "fb_recent_0001.ts"), 10);
            WriteSizedFile(Path.Combine(retiredSession, "fb_retired_0001.ts"), 30);
            File.WriteAllText(Path.Combine(retiredSession, ".flashback-retired-session"), "retired");
            WriteSizedFile(Path.Combine(preservedSession, "fb_preserved_0001.ts"), 100);
            File.WriteAllText(Path.Combine(preservedSession, ".flashback-recovery-preserve"), "keep");
            File.WriteAllText(Path.Combine(nonFlashbackDirectory, "notes.txt"), "keep");

            var now = DateTime.UtcNow;
            File.SetLastWriteTimeUtc(Path.Combine(oldSession, "fb_old_0001.ts"), now - TimeSpan.FromHours(2));
            Directory.SetLastWriteTimeUtc(oldSession, now - TimeSpan.FromHours(2));
            File.SetLastWriteTimeUtc(Path.Combine(recentSession, "fb_recent_0001.ts"), now - TimeSpan.FromMinutes(5));
            Directory.SetLastWriteTimeUtc(recentSession, now - TimeSpan.FromMinutes(5));
            File.SetLastWriteTimeUtc(Path.Combine(retiredSession, "fb_retired_0001.ts"), now - TimeSpan.FromHours(3));
            Directory.SetLastWriteTimeUtc(retiredSession, now - TimeSpan.FromHours(3));

            var cleanupType = RequireType("Sussudio.Services.Flashback.FlashbackStartupSessionCacheBudget");
            var cleanup = cleanupType.GetMethod("CleanupSessionCacheBudget", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException("CleanupSessionCacheBudget not found.");

            cleanup.Invoke(null, new object[] { tempDir, currentSession, 25L });

            AssertEqual(true, Directory.Exists(currentSession), "Current session preserved");
            AssertEqual(false, Directory.Exists(oldSession), "Oldest session removed to satisfy budget");
            AssertEqual(false, Directory.Exists(retiredSession), "Retired session participates in startup cache budget");
            AssertEqual(true, Directory.Exists(recentSession), "Recent session preserved once budget is satisfied");
            AssertEqual(true, Directory.Exists(preservedSession), "Recovery-preserved session skipped");
            AssertEqual(true, Directory.Exists(nonFlashbackDirectory), "Non-flashback directory preserved");
        }
        finally
        {
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_RejectsUnsafeSessionIds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_session_id_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            using var manager = (IDisposable)Activator.CreateInstance(managerType, new[] { options })!;
            var initialize = managerType.GetMethod("Initialize")
                ?? throw new InvalidOperationException("FlashbackBufferManager.Initialize not found.");

            var invocation = Assert.Throws<TargetInvocationException>(
                () => initialize.Invoke(manager, new object[] { "..\\outside-session" }));
            Assert.IsType<ArgumentException>(invocation.InnerException);

            AssertEqual(false, Directory.Exists(Path.Combine(Directory.GetParent(tempDir)!.FullName, "outside-session")), "Unsafe session id must not create outside directory");
        }
        finally
        {
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_ValidatesSegmentExtensions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_segment_ext_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            using var manager = (IDisposable)Activator.CreateInstance(managerType, new[] { options })!;
            managerType.GetMethod("Initialize")!.Invoke(manager, new object[] { "safe-session" });

            var setExtension = managerType.GetMethod("SetSegmentExtension")
                ?? throw new InvalidOperationException("SetSegmentExtension not found.");
            var generatePath = managerType.GetMethod("GenerateSegmentPath")
                ?? throw new InvalidOperationException("GenerateSegmentPath not found.");

            setExtension.Invoke(manager, new object[] { ".TS" });
            var transportPath = (string)generatePath.Invoke(manager, null)!;
            AssertEqual(true, transportPath.EndsWith(".ts", StringComparison.Ordinal), "Transport stream extension normalized");

            setExtension.Invoke(manager, new object[] { ".Mp4" });
            var mp4Path = (string)generatePath.Invoke(manager, null)!;
            AssertEqual(true, mp4Path.EndsWith(".mp4", StringComparison.Ordinal), "MP4 extension normalized");

            var invocation = Assert.Throws<TargetInvocationException>(
                () => setExtension.Invoke(manager, new object[] { "..\\escape.ts" }));
            Assert.IsType<ArgumentException>(invocation.InnerException);
        }
        finally
        {
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_InitializeClearsRecordingPts()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_init_pts_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
            var options = RuntimeHelpers.GetUninitializedObject(optionsType);
            SetPropertyBackingField(options, "BufferDuration", TimeSpan.FromMinutes(5));
            SetPropertyBackingField(options, "TempDirectory", tempDir);
            SetPropertyBackingField(options, "SegmentDuration", TimeSpan.FromMinutes(10));

            var managerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
            var manager = Activator.CreateInstance(managerType, new[] { options })
                ?? throw new InvalidOperationException("FlashbackBufferManager construction failed.");
            using var disposableManager = manager as IDisposable;

            managerType.GetMethod("Initialize")!.Invoke(manager, new object[] { "session-a" });
            managerType.GetMethod("UpdateLatestPts")!.Invoke(manager, new object[] { TimeSpan.FromSeconds(10) });
            managerType.GetMethod("PauseEviction")!.Invoke(manager, null);
            managerType.GetMethod("UpdateLatestPts")!.Invoke(manager, new object[] { TimeSpan.FromSeconds(20) });
            managerType.GetMethod("ResumeEviction")!.Invoke(manager, null);

            AssertEqual(TimeSpan.FromSeconds(10), (TimeSpan)GetPropertyValue(manager, "RecordingStartPts")!, "RecordingStartPts before reinitialize");
            AssertEqual(TimeSpan.FromSeconds(20), (TimeSpan)GetPropertyValue(manager, "RecordingEndPts")!, "RecordingEndPts before reinitialize");

            managerType.GetMethod("Initialize")!.Invoke(manager, new object[] { "session-b" });
            AssertEqual(TimeSpan.Zero, (TimeSpan)GetPropertyValue(manager, "RecordingStartPts")!, "RecordingStartPts resets on Initialize");
            AssertEqual(TimeSpan.Zero, (TimeSpan)GetPropertyValue(manager, "RecordingEndPts")!, "RecordingEndPts resets on Initialize");

            var activePath = (string)managerType.GetMethod("AcquireSegmentPath", Type.EmptyTypes)!.Invoke(manager, null)!;
            File.WriteAllBytes(activePath, new byte[] { 1, 2, 3, 4 });
            var segmentInfo = (System.Collections.IEnumerable)managerType.GetMethod("GetSegmentInfoList")!.Invoke(manager, null)!;
            var activeInfo = segmentInfo.Cast<object>().Single(info => (bool)GetPropertyValue(info, "IsActive")!);
            AssertEqual(0L, (long)GetPropertyValue(activeInfo, "StartPtsMs")!, "Active segment start PTS resets on Initialize");
            AssertEqual(0L, (long)GetPropertyValue(activeInfo, "EndPtsMs")!, "Active segment end PTS resets on Initialize");
        }
        finally
        {
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_EvictOldestSegments_UpdatesTotalDiskBytes()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_evict_bytes_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            var firstSegment = Path.Combine(tempDir, "seg0.ts");
            var secondSegment = Path.Combine(tempDir, "seg1.ts");
            var activeSegment = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(firstSegment, new byte[100]);
            File.WriteAllBytes(secondSegment, new byte[200]);
            File.WriteAllBytes(activeSegment, new byte[50]);

            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");
            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                firstSegment,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                100L
            });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                secondSegment,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                200L
            });
            updateDiskBytes.Invoke(manager, new object[] { 50L });

            AssertEqual(350L, GetLongProperty(manager, "TotalDiskBytes"), "Setup should track completed and active bytes");

            SetPrivateField(manager, "_validStartPtsTicks", TimeSpan.FromSeconds(1).Ticks);
            InvokeNonPublicInstanceMethod(manager, "EvictOldestSegments", null);

            var deleteDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (File.Exists(firstSegment) && DateTime.UtcNow < deleteDeadline)
            {
                Thread.Sleep(25);
            }

            AssertEqual(false, File.Exists(firstSegment), "Eviction should delete the expired completed segment");
            AssertEqual(true, File.Exists(secondSegment), "Eviction should retain overlapping completed segment");
            AssertEqual(250L, GetLongProperty(manager, "TotalDiskBytes"), "Eviction subtracts deleted completed segment bytes");
            AssertEqual(200L, (long)GetPrivateField(manager, "_completedSegmentBytes")!, "Completed byte cache matches retained segment");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_EvictOldestSegments_KeepsRejectedSegmentsAccounted()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_evict_locked_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            var firstSegment = Path.Combine(tempDir, "seg0.ts");
            var secondSegment = Path.Combine(tempDir, "seg1.ts");
            var activeSegment = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(firstSegment, new byte[100]);
            File.WriteAllBytes(secondSegment, new byte[200]);
            File.WriteAllBytes(activeSegment, new byte[50]);

            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");
            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                firstSegment,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                100L
            });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                secondSegment,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                200L
            });
            updateDiskBytes.Invoke(manager, new object[] { 50L });

            SetPrivateField(manager, "_sessionDirectory", Path.Combine(tempDir, "different-session"));
            SetPrivateField(manager, "_validStartPtsTicks", TimeSpan.FromSeconds(1).Ticks);
            InvokeNonPublicInstanceMethod(manager, "EvictOldestSegments", null);

            AssertEqual(true, File.Exists(firstSegment), "Rejected expired segment remains on disk");
            AssertEqual(true, File.Exists(secondSegment), "Later segment is not evicted past a rejected predecessor");
            AssertEqual(3, GetIntProperty(manager, "SegmentCount"), "Rejected completed segments remain tracked with active segment");
            AssertEqual(350L, GetLongProperty(manager, "TotalDiskBytes"), "Rejected segment bytes stay in disk accounting");
            AssertEqual(300L, (long)GetPrivateField(manager, "_completedSegmentBytes")!, "Completed byte cache retains rejected segment");

            var source = ReadFlashbackBufferManagerSource();
            AssertContains(source, "if (TryCreatePendingEvictionDelete(oldest.Path, oldest.SizeBytes, \"valid_window\", out var pendingDelete))");
            AssertContains(source, "private static bool DeleteEvictedFile");
            AssertContains(source, "FLASHBACK_BUFFER_EVICT_DELETE_WARN");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_EvictionPauseResume_Balanced()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_{Guid.NewGuid():N}");
        var manager = CreateInitializedBufferManager(tempDir);

        var pauseMethod = manager.GetType().GetMethod("PauseEviction")!;
        var resumeMethod = manager.GetType().GetMethod("ResumeEviction")!;

        // Initially not paused
        AssertEqual(false, GetBoolProperty(manager, "EvictionPaused"), "Initial EvictionPaused");

        // Pause -> paused
        pauseMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 1 pause");

        // Double-pause -> still paused (count-based)
        pauseMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 2 pauses");

        // Resume once -> still paused (count = 1)
        resumeMethod.Invoke(manager, null);
        AssertEqual(true, GetBoolProperty(manager, "EvictionPaused"), "After 1 resume (count=1)");

        // Resume again -> unpaused (count = 0)
        resumeMethod.Invoke(manager, null);
        AssertEqual(false, GetBoolProperty(manager, "EvictionPaused"), "After 2 resumes (count=0)");

        // Extra resume -> remains unpaused and must not underflow the pause counter.
        resumeMethod.Invoke(manager, null);
        AssertEqual(false, GetBoolProperty(manager, "EvictionPaused"), "After unbalanced resume");

        var source = ReadFlashbackBufferManagerSource();
        AssertContains(source, "FLASHBACK_BUFFER_EVICTION_RESUME_UNBALANCED");
        AssertContains(source, "var unbalancedEndPts = ClampEndPtsToStart(_recordingStartPts, _recordingEndPts);");
        AssertContains(source, "_recordingEndPts = ClampEndPtsToStart(\n                    _recordingStartPts,\n                    TimeSpan.FromTicks(Interlocked.Read(ref _latestPtsTicks)));");
        AssertContains(source, "var rangeSeconds = TimeSpan.FromTicks(NonNegativeDeltaTicks(_recordingEndPts.Ticks, _recordingStartPts.Ticks)).TotalSeconds;");
        AssertContains(source, "private static TimeSpan ClampEndPtsToStart(TimeSpan startPts, TimeSpan endPts)");
        AssertDoesNotContain(source, "range_s={(_recordingEndPts - _recordingStartPts).TotalSeconds:F1}");

        return Task.CompletedTask;
    }


    internal static Task FlashbackBufferManager_PurgesRetainLockedActivePath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_locked_active_purge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);
        string? activePath = null;

        try
        {
            activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(activePath, new byte[50]);
            File.SetAttributes(activePath, File.GetAttributes(activePath) | FileAttributes.ReadOnly);

            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
            var purgeCompleted = manager.GetType().GetMethod("PurgeCompletedSegments")
                ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeCompletedSegments not found.");
            var purgeAll = manager.GetType().GetMethod("PurgeAllSegments")
                ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeAllSegments not found.");

            updateDiskBytes.Invoke(manager, new object[] { 50L });
            AssertEqual(50L, GetLongProperty(manager, "TotalDiskBytes"), "Setup tracks active bytes");
            AssertEqual(50L, GetLongProperty(manager, "TotalBytesWritten"), "Setup tracks active bytes written");

            purgeCompleted.Invoke(manager, null);

            AssertEqual(true, File.Exists(activePath), "Read-only active file remains on disk");
            AssertEqual(activePath, (string)GetPrivateField(manager, "_activeSegmentPath")!, "Read-only active path remains tracked");
            AssertEqual(activePath, GetStringProperty(manager, "ActiveFilePath"), "ActiveFilePath still reports read-only active segment");
            AssertEqual(1, GetIntProperty(manager, "SegmentCount"), "Segment count still includes read-only active segment");
            AssertEqual(50L, GetLongProperty(manager, "TotalDiskBytes"), "Read-only active bytes remain in disk accounting");
            AssertEqual(50L, (long)GetPrivateField(manager, "_previousActiveSegmentBytes")!, "Read-only active byte baseline is preserved");

            updateDiskBytes.Invoke(manager, new object[] { 50L });
            AssertEqual(50L, GetLongProperty(manager, "TotalBytesWritten"), "Same active bytes are not double-counted after failed purge");

            purgeAll.Invoke(manager, null);
            AssertEqual(true, File.Exists(activePath), "Read-only active file remains after full purge attempt");
            AssertEqual(activePath, (string)GetPrivateField(manager, "_activeSegmentPath")!, "Full purge keeps read-only active path tracked");
            AssertEqual(1, GetIntProperty(manager, "SegmentCount"), "Full purge segment count still includes read-only active segment");
            AssertEqual(50L, GetLongProperty(manager, "TotalDiskBytes"), "Full purge keeps read-only active bytes in disk accounting");
            AssertEqual(50L, (long)GetPrivateField(manager, "_previousActiveSegmentBytes")!, "Full purge keeps read-only active byte baseline");
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(activePath) && File.Exists(activePath))
            {
                global::Program.TryClearReadOnlyAttribute(activePath);
            }

            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_PurgeAllSegmentsCore_ReportsActiveBytesOnce()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_full_purge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);

        try
        {
            var completedPath = Path.Combine(tempDir, "completed.ts");
            var activePath = (string)GetPrivateField(manager, "_activeSegmentPath")!;
            File.WriteAllBytes(completedPath, new byte[300]);
            File.WriteAllBytes(activePath, new byte[50]);
            AddCompletedSegment(manager, completedPath, TimeSpan.Zero, TimeSpan.FromSeconds(1), 300L);
            SetPrivateField(manager, "_completedSegmentBytes", 300L);
            SetPrivateField(manager, "_totalDiskBytes", 350L);

            var purgeCore = manager.GetType().GetMethod("PurgeAllSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeAllSegmentsCore not found.");
            var result = purgeCore.Invoke(manager, null)!;
            var segments = Convert.ToInt32(result.GetType().GetField("Item1")!.GetValue(result));
            var freedBytes = Convert.ToInt64(result.GetType().GetField("Item2")!.GetValue(result));

            AssertEqual(2, segments, "Full purge reports completed plus active segment");
            AssertEqual(350L, freedBytes, "Full purge reports completed plus active bytes exactly once");
            AssertEqual(false, File.Exists(completedPath), "Full purge deletes completed segment");
            AssertEqual(false, File.Exists(activePath), "Full purge deletes active segment");
            AssertEqual(0L, GetLongProperty(manager, "TotalDiskBytes"), "Full purge resets total disk bytes");
            AssertEqual(0L, GetLongProperty(manager, "TotalBytesWritten"), "Full purge resets monotonic bytes for a new buffer session");

            var source = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBufferManager.cs")
                .Replace("\r\n", "\n");
            var purgeCoreBlock = ExtractTextBetween(
                source,
                "private (int Segments, long FreedBytes) PurgeAllSegmentsCore()",
                "    private bool TryDeleteFile(string filePath)");
            AssertOccursBefore(purgeCoreBlock, "var activeBytes = _activeSegmentPath != null", "if (_activeSegmentPath != null)");
            AssertContains(purgeCoreBlock, "_completedSegmentBytes = GetCompletedSegmentBytesSaturated();");
            AssertContains(purgeCoreBlock, "var retainedActiveBytes = _activeSegmentPath != null ? activeBytes : 0;");
        }
        finally
        {
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackBufferManager_PurgeCompletedSegments_AccountsForActiveBytesOnPartialPurge()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fbtest_partial_purge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var manager = CreateInitializedBufferManager(tempDir);
        FileStream? lockedCompleted = null;

        try
        {
            var completedPath = Path.Combine(tempDir, "completed-locked.ts");
            var deletableCompletedPath = Path.Combine(tempDir, "completed-deletable.ts");
            var activePath = Path.Combine(tempDir, "fb_test_0003.ts");
            File.WriteAllBytes(completedPath, new byte[100]);
            File.WriteAllBytes(deletableCompletedPath, new byte[200]);
            File.WriteAllBytes(activePath, new byte[50]);

            var onSegmentCompleted = manager.GetType().GetMethod("OnSegmentCompleted")
                ?? throw new InvalidOperationException("FlashbackBufferManager.OnSegmentCompleted not found.");
            var updateDiskBytes = manager.GetType().GetMethod("UpdateDiskBytes")
                ?? throw new InvalidOperationException("FlashbackBufferManager.UpdateDiskBytes not found.");
            var purgeCompleted = manager.GetType().GetMethod("PurgeCompletedSegments")
                ?? throw new InvalidOperationException("FlashbackBufferManager.PurgeCompletedSegments not found.");

            onSegmentCompleted.Invoke(manager, new object[]
            {
                completedPath,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                100L
            });
            onSegmentCompleted.Invoke(manager, new object[]
            {
                deletableCompletedPath,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                200L
            });
            updateDiskBytes.Invoke(manager, new object[] { 50L });
            AssertEqual(350L, GetLongProperty(manager, "TotalDiskBytes"), "Setup should track completed plus active bytes");

            lockedCompleted = new FileStream(completedPath, FileMode.Open, FileAccess.Read, FileShare.None);
            purgeCompleted.Invoke(manager, null);

            AssertEqual(false, File.Exists(activePath), "Partial purge should still delete stale active segment");
            AssertEqual(false, File.Exists(deletableCompletedPath), "Partial purge deletes unlocked completed segments");
            AssertEqual(true, File.Exists(completedPath), "Partial purge retains locked completed segments");
            AssertEqual(100L, GetLongProperty(manager, "TotalDiskBytes"), "Partial purge subtracts deleted completed and active bytes");
            AssertEqual(100L, (long)GetPrivateField(manager, "_completedSegmentBytes")!, "Partial purge preserves retained completed byte accounting");
            AssertEqual(0L, (long)GetPrivateField(manager, "_previousActiveSegmentBytes")!, "Partial purge resets active byte baseline");

            updateDiskBytes.Invoke(manager, new object[] { 25L });
            AssertEqual(125L, GetLongProperty(manager, "TotalDiskBytes"), "Next active bytes are added to retained completed bytes");
            AssertEqual(375L, GetLongProperty(manager, "TotalBytesWritten"), "Next active segment bytes are counted after purge baseline reset");
        }
        finally
        {
            lockedCompleted?.Dispose();
            if (manager is IDisposable disposable)
            {
                disposable.Dispose();
            }

            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }
}
