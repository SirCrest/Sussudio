using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using System.IO;

namespace Sussudio.Tests
{

public sealed class FlashbackModelsTests
{
    private static Type RequireType(Assembly asm, string name)
        => asm.GetType(name, throwOnError: true)!;

    private static object CreateInstance(Type type)
        => Activator.CreateInstance(type, nonPublic: true)!;

    private static object? Get(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance)!.GetValue(instance);

    private static T Get<T>(object instance, string propertyName)
        => (T)Get(instance, propertyName)!;

    private static void Set(object instance, string propertyName, object? value)
    {
        var type = instance.GetType();
        var property = type.GetProperty(propertyName, ReflectionFlags.Instance);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = type.GetField($"<{propertyName}>k__BackingField", ReflectionFlags.Instance)
            ?? type.GetField($"_{char.ToLowerInvariant(propertyName[0])}{propertyName[1..]}", ReflectionFlags.Instance)
            ?? type.GetField(propertyName, ReflectionFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    [Fact]
    public void FlashbackBufferOptions_MaxDiskBytes_ScalesWithDuration()
    {
        var asm = SussudioAssembly.Load();
        var optionsType = RequireType(asm, "Sussudio.Models.FlashbackBufferOptions");
        var options = CreateInstance(optionsType);

        const long safetyBytesPerSecond = 57L * 1024L * 1024L;

        Set(options, "BufferDuration", TimeSpan.FromMinutes(5));
        var maxBytes = Get<long>(options, "MaxDiskBytes");
        Assert.Equal(300L * safetyBytesPerSecond, maxBytes);

        Set(options, "BufferDuration", TimeSpan.FromMinutes(1));
        var oneMinuteBytes = Get<long>(options, "MaxDiskBytes");
        Assert.Equal(60L * safetyBytesPerSecond, oneMinuteBytes);
        Assert.Equal(maxBytes, oneMinuteBytes * 5);

        Set(options, "BufferDuration", TimeSpan.Zero);
        Assert.Equal(0L, Get<long>(options, "MaxDiskBytes"));

        Set(options, "BufferDuration", TimeSpan.FromTicks(-1));
        Assert.Equal(0L, Get<long>(options, "MaxDiskBytes"));

        Set(options, "BufferDuration", TimeSpan.MaxValue);
        Assert.Equal(long.MaxValue, Get<long>(options, "MaxDiskBytes"));
    }

}

public sealed class FlashbackDecoderContractsTests
{
    [Theory]
    [InlineData(760.0, "capture.ts", 758.0)]
    [InlineData(1.0, "capture.TS", 0.0)]
    [InlineData(0.0, "capture.ts", 0.0)]
    [InlineData(760.0, "capture.mp4", 760.0)]
    public void ExactMpegTsSeeksAllowThePrecedingKeyframe(double target, string path, double expected)
    {
        var decoder = SussudioAssembly.Load().GetType("Sussudio.Services.Flashback.FlashbackDecoder", true)!;
        var resolve = decoder.GetMethod("ResolveExactSeekStart", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.Equal(TimeSpan.FromSeconds(expected),
            (TimeSpan)resolve.Invoke(null, new object[] { TimeSpan.FromSeconds(target), path })!);
    }

    public FlashbackDecoderContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackDecoderCalculatesNv12FrameBufferSizes()
        => global::Program.FlashbackDecoder_CalculateFrameBufferSize_Nv12();

    [Fact]
    public Task FlashbackDecoderCalculatesP010FrameBufferSizes()
        => global::Program.FlashbackDecoder_CalculateFrameBufferSize_P010();

    [Fact]
    public Task FlashbackDecoderDefaultsToClosedState()
        => global::Program.FlashbackDecoder_DefaultState_IsNotOpenAndNotInitialized();

    [Fact]
    public Task FlashbackDecoderDisposeBeforeInitializeIsSafe()
        => global::Program.FlashbackDecoder_DisposeBeforeInitialize_DoesNotThrow();

    [Fact]
    public Task FlashbackDecoderUnreferencesDiscardedAudioFrames()
        => global::Program.FlashbackDecoder_DiscardedAudioFramesAreUnreffed();

    [Fact]
    public Task FlashbackDecoderMjpegPlaybackUsesLowLatencySingleThreadDecode()
        => global::Program.FlashbackDecoder_MjpegPlaybackUsesSingleThreadLowLatencyDecode();

    [Fact]
    public Task FlashbackDecoderRejectsInvalidTimestamps()
        => global::Program.FlashbackDecoder_PtsConversionRejectsInvalidTimestamps();

    [Fact]
    public Task FlashbackDecoderInputStreamsAndFrameSizesAreBounded()
        => global::Program.FlashbackDecoder_InputStreamsAndFrameSizesAreBounded();

    [Fact]
    public Task FlashbackDecoderAudioOutputBuffersAreBounded()
        => global::Program.FlashbackDecoder_AudioOutputBuffersAreBounded();

    [Fact]
    public Task FlashbackDecoderSoftwareFramePlanesAreValidated()
        => global::Program.FlashbackDecoder_SoftwareFramePlanesAreValidated();

    [Fact]
    public Task FlashbackDecoderD3D11FramesAreValidated()
        => global::Program.FlashbackDecoder_D3D11FramesAreValidated();

    [Fact]
    public Task FlashbackDecoderRejectsInitializeAfterDispose()
        => global::Program.FlashbackDecoder_RejectsInitializeAfterDispose();

    [Fact]
    public Task FlashbackDecoderClearsAudioCallbackOnDispose()
        => global::Program.FlashbackDecoder_ClearsAudioCallbackOnDispose();
}

public sealed class FlashbackEncoderSinkContractsTests
{
    public FlashbackEncoderSinkContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackEncoderResolvesFractionalFrameRates()
        => global::Program.FlashbackEncoderSink_ResolveFrameRateParts_ParsesFractionalRates();

    [Fact]
    public Task FlashbackEncoderMapsCodecNames()
        => global::Program.FlashbackEncoderSink_MapCodecName_MapsFormats();

    [Fact]
    public Task FlashbackEncoderCountersDefaultToZero()
        => global::Program.FlashbackEncoderSink_CountersDefaultToZero();

    [Fact]
    public Task FlashbackEncoderBoundsHighResolutionCpuQueueCapacity()
        => global::Program.FlashbackEncoderSink_HighResolutionCpuQueueCapacityIsBounded();

    [Fact]
    public Task FlashbackExportThrottleRespondsToLiveQueuePressure()
        => global::Program.CaptureService_FlashbackExportThrottleRespondsToLiveQueuePressure();

    [Fact]
    public Task FlashbackEncoderForceRotateDrainDoesNotRejectVideoByLifecycleState()
        => global::Program.FlashbackEncoderSink_ForceRotateDrainingDoesNotRejectVideoByLifecycleState();

    [Fact]
    public Task FlashbackEncoderDisposeResetsGpuQueueDepth()
        => global::Program.FlashbackEncoderSink_DisposeResetsGpuQueueDepth();

    [Fact]
    public Task FlashbackEncoderPtsGuardsInvalidFrameRates()
        => global::Program.FlashbackEncoderSink_EncoderPtsGuardsInvalidFrameRate();

    [Fact]
    public void FlashbackEncoderRestartKeepsTheNextFrameTimestamp()
    {
        var sink = SussudioAssembly.Load().GetType("Sussudio.Services.Flashback.FlashbackEncoderSink", true)!;
        var resolve = sink.GetMethod("ResolveInitialEncoderPts", BindingFlags.Static | BindingFlags.NonPublic)!;
        (long VideoPts, long AudioPts) Resolve(TimeSpan position, double fps)
            => ((long, long))resolve.Invoke(null, new object[] { position, fps })!;

        // This exact resumed timestamp previously truncated to video frame 43207,
        // repeating the preceding segment's final frame timestamp at 4K120.
        Assert.Equal((43_208L, 17_283_200L), Resolve(TimeSpan.FromTicks(3_600_666_666), 120));
        Assert.Equal((12_001L, 9_610_401L), Resolve(TimeSpan.FromSeconds(12_001 / (60_000.0 / 1001)), 60_000.0 / 1001));
        Assert.Equal((0L, 0L), Resolve(TimeSpan.Zero, 120));
    }

    [Fact]
    public Task FlashbackEncoderSinkRestoresActiveSegmentAfterRotationFailure()
        => global::Program.FlashbackEncoderSink_RotateFailureRestoresActiveSegment();

    [Fact]
    public Task FlashbackEncoderSinkRegistersSegmentsOnCancellationAndRotationFailure()
        => global::Program.FlashbackEncoderSink_RegistersSegmentsOnCancellationAndRotationFailure();

    [Fact]
    public Task FlashbackEncoderSinkRejectsForceRotateAfterEncoderFailure()
        => global::Program.FlashbackEncoderSink_ForceRotateRejectsFailedEncoder();

    [Fact]
    public Task FlashbackEncoderSinkLogsFatalSegmentRegistrationFailures()
        => global::Program.FlashbackEncoderSink_FatalSegmentRegistrationFailuresAreLogged();

    [Fact]
    public Task FlashbackEncoderSinkValidatesAudioPacketsBeforeRent()
        => global::Program.FlashbackEncoderSink_AudioPacketsAreValidatedBeforeRent();

    [Fact]
    public Task FlashbackEncoderSinkInterleavesAudioWithBoundedVideoBatches()
        => global::Program.FlashbackEncoderSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches();

}

public sealed class FlashbackExporterContractsTests
{
    public FlashbackExporterContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackSuppressedExceptionsUseAppLogs()
        => global::Program.FlashbackSuppressedExceptionsUseAppLogs();

    [Fact]
    public Task FlashbackOutputTransactionCleanupIgnoresNonexistentDirectories()
        => global::Program.FlashbackOutputTransaction_CleanupOrphanedTempFiles_HandlesNonexistentDirectory();

    [Fact]
    public Task FlashbackOutputTransactionCleanupDeletesOrphanedTempFiles()
        => global::Program.FlashbackOutputTransaction_CleanupOrphanedTempFiles_DeletesTempFiles();

    [Fact]
    public Task FlashbackExporterDoesNotScanUserOutputDirectoryForOrphans()
        => global::Program.FlashbackExporter_DoesNotScanUserOutputDirectoryForOrphans();

    [Fact]
    public Task FlashbackExporterRejectsNullRequests()
        => global::Program.FlashbackExporter_RejectsNullRequests();

    [Fact]
    public Task FlashbackExporterFailsWhenInputFileIsMissing()
        => global::Program.FlashbackExporter_ExportAsync_ReturnsFailure_WhenInputFileNotFound();

    [Fact]
    public Task FlashbackExporterFailsWhenOutputPathIsEmpty()
        => global::Program.FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathEmpty();

    [Fact]
    public Task FlashbackExporterFailsWhenNoSegmentPathsAreProvided()
        => global::Program.FlashbackExporter_ExportSegmentsAsync_ReturnsFailure_WhenNoSegments();

    [Fact]
    public Task FlashbackExporterOutputPathValidationReturnsFailure()
        => global::Program.FlashbackExporter_OutputPathValidation_ReturnsFailure();

    [Fact]
    public Task FlashbackExportFailureClassifierMapsCommandFailures()
        => global::Program.FlashbackExportFailureClassifier_MapsCommandFailures();

    [Fact]
    public Task FlashbackExporterRejectsDirectoryOutputPaths()
        => global::Program.FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathIsDirectory();

    [Fact]
    public Task FlashbackExporterRejectsInvalidExportRanges()
        => global::Program.FlashbackExporter_RejectsInvalidExportRanges();

    [Fact]
    public Task FlashbackExporterRejectsEmptySegmentPaths()
        => global::Program.FlashbackExporter_RejectsEmptySegmentPaths();

    [Fact]
    public Task FlashbackExporterRejectsDuplicateSegmentPaths()
        => global::Program.FlashbackExporter_RejectsDuplicateSegmentPaths();

    [Fact]
    public Task FlashbackExporterPreservesLaterValidMicrophoneStreamTemplate()
        => global::Program.FlashbackExporter_PreservesLaterValidMicrophoneStreamTemplate();

    [Fact]
    public Task FlashbackExporterRetainsHiddenDecoderPreroll()
        => global::Program.FlashbackExporter_RetainsHiddenDecoderPreroll();

    [Fact]
    public Task FlashbackExporterPreservesTimelineAcrossDelayedSegmentPackets()
        => global::Program.FlashbackExporter_PreservesTimelineAcrossDelayedSegmentPackets();

    [Fact]
    public Task FlashbackExporterReturnsCancellationResultWhileWaitingForExportLock()
        => global::Program.FlashbackExporter_ReturnsCancellationResult_WhenLockWaitCancelled();

    [Fact]
    public Task FlashbackExporterCancellationWinsBeforeValidation()
        => global::Program.FlashbackExporter_CancellationWinsBeforeValidation();

    [Fact]
    public Task FlashbackExporterFailsFastWhenSegmentFilesAreGone()
        => global::Program.FlashbackExporter_ReturnsFailure_WhenSegmentFilesAreGone();

    [Fact]
    public Task FlashbackExporterRejectsOutputPathsThatOverwriteSourceSegments()
        => global::Program.FlashbackExporter_RejectsOutputPathThatOverwritesSource();

    [Fact]
    public Task FlashbackOutputTransactionRejectsEmptyTempAndPreservesExistingOutput()
        => global::Program.FlashbackOutputTransaction_EmptyTempDoesNotReplaceExistingOutput();

    [Fact]
    public Task FlashbackExporterRefusesToOverwriteExistingDestinationForSingleFile()
        => global::Program.FlashbackExporter_RefusesOverwriteForSingleFile();

    [Fact]
    public Task FlashbackExporterRefusesToOverwriteExistingDestinationForSegments()
        => global::Program.FlashbackExporter_RefusesOverwriteForSegments();

    [Fact]
    public Task FlashbackOutputTransactionPreservesOutputAfterPostMoveValidationFailure()
        => global::Program.FlashbackOutputTransaction_PostMoveValidationFailurePreservesOutput();

    [Fact]
    public void FlashbackOutputTransactionReportsMissingDirectoryWithoutCollisionRetries()
    {
        var temp = Directory.CreateTempSubdirectory("sussudio-reservation-error-");
        try
        {
            var outputPath = Path.Combine(temp.FullName, "missing", "export.mp4");
            var transactionType = SussudioAssembly.Load().GetType(
                "Sussudio.Services.Flashback.FlashbackExportOutputTransaction", throwOnError: true)!;
            var reserve = transactionType.GetMethod("TryReserve", BindingFlags.Static | BindingFlags.NonPublic)!;
            var arguments = new object?[] { outputPath, null, string.Empty, string.Empty };

            Assert.False((bool)reserve.Invoke(null, arguments)!);
            Assert.Null(arguments[1]);
            Assert.Equal("flashback-export-output-write-failed", arguments[3]);
            var message = Assert.IsType<string>(arguments[2]);
            var context = $"Flashback export failed: could not create temporary output file before writing '{outputPath}': ";
            Assert.StartsWith(context, message);
            Assert.Contains(".mp4.tmp", message[context.Length..]);
            Assert.DoesNotContain("unique temporary output", message);
            Assert.Empty(temp.EnumerateFileSystemInfos());
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    [Fact]
    public Task FlashbackOutputTransactionCreatesUniqueTempOutputPaths()
        => global::Program.FlashbackOutputTransaction_CreatesUniqueTempOutputPaths();

    [Fact]
    public Task FlashbackOutputTransactionReservationBlocksPathReplacement()
        => global::Program.FlashbackOutputTransaction_ReservationBlocksPathReplacement();

    [Fact]
    public Task FlashbackOutputTransactionPublishesWithLiveReservation()
        => global::Program.FlashbackExportOutputTransaction_PublishesWithLiveReservation();

    [Fact]
    public Task FlashbackOutputTransactionRefusesDestinationCreatedAfterReservation()
        => global::Program.FlashbackExportOutputTransaction_RefusesDestinationCreatedAfterReservation();

    [Fact]
    public Task FlashbackOutputTransactionDoesNotPublishOrDeleteAReplacedTempPath()
        => global::Program.FlashbackOutputTransaction_ReplacedTempPathIsNeverPublishedOrDeleted();

    [Fact]
    public Task FlashbackOutputTransactionAbandonDeletesOnlyItsUncommittedTemp()
        => global::Program.FlashbackOutputTransaction_AbandonDeletesOnlyItsUncommittedTemp();
}

public sealed class FlashbackPlaybackContractsTests
{
    public FlashbackPlaybackContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackPlaybackInitialStateIsLive()
        => global::Program.FlashbackPlaybackController_InitialState_IsLive();

    [Fact]
    public Task FlashbackPlaybackCommandsNoOpBeforeInitialize()
        => global::Program.FlashbackPlaybackController_CommandsNoOpBeforeInitialize();

    [Fact]
    public Task FlashbackPlaybackSuccessfulNoOpsClearStaleFailures()
        => global::Program.FlashbackPlaybackController_SuccessfulNoOps_ClearStaleCommandFailure();

    [Fact]
    public Task FlashbackPlaybackCoalescedCommandsClearStaleFailures()
        => global::Program.FlashbackPlaybackController_CoalescedCommands_ClearStaleCommandFailure();

    [Fact]
    public Task FlashbackPlaybackCommandQueueAcceptsNewestControlWhenFull()
        => global::Program.FlashbackPlaybackController_CommandQueue_AcceptsNewestControlWhenFull();

    [Fact]
    public Task FlashbackCommandPositionsClampBeforeFileLookup()
        => global::Program.FlashbackPlaybackController_ClampsCommandPositionsBeforeFileLookup();

    [Fact]
    public Task FlashbackPlaybackTimestampArithmeticIsSaturating()
        => global::Program.FlashbackPlaybackController_TimestampArithmeticIsSaturating();

    [Fact]
    public Task FlashbackPauseFromLiveDisplaysBufferedFrameBeforePaused()
        => global::Program.FlashbackPlaybackController_PauseFromLive_DisplaysBufferedFrameBeforePaused();

    [Fact]
    public Task FlashbackPlaybackPtsCadenceTelemetryTracksMismatches()
        => global::Program.FlashbackPlaybackController_PtsCadenceTelemetry_TracksMismatches();

    [Fact]
    public Task FlashbackNudgeOpensDecoderAfterPauseFromLive()
        => global::Program.FlashbackPlaybackController_NudgeCreatesDecoderWhenPaused();

    [Fact]
    public Task FlashbackPlaybackInOutPointsDefaultToUnset()
        => global::Program.FlashbackPlaybackController_InOutPoints_DefaultToUnset();

    [Fact]
    public void PlaybackRejectionsKeepReadinessThreadAndDisposalAccountingSeparateFromMailboxFailures()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("sussudio-playback-rejections-").FullName;
        try
        {
            const BindingFlags members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var assembly = SussudioAssembly.Load();
            var bufferManagerType = assembly.GetType("Sussudio.Services.Flashback.FlashbackBufferManager", throwOnError: true)!;
            var optionsType = assembly.GetType("Sussudio.Models.FlashbackBufferOptions", throwOnError: true)!;
            var options = Activator.CreateInstance(optionsType)!;
            optionsType.GetProperty("TempDirectory")!.SetValue(options, tempDirectory);
            using var bufferManager = (IDisposable)Activator.CreateInstance(bufferManagerType, new[] { options })!;
            var controllerType = assembly.GetType("Sussudio.Services.Flashback.FlashbackPlaybackController", throwOnError: true)!;
            using var controller = (IDisposable)Activator.CreateInstance(controllerType, new object[] { bufferManager })!;
            var updateScrub = controllerType.GetMethod("UpdateScrub", members)!.CreateDelegate<Func<TimeSpan, bool>>(controller);
            var position = TimeSpan.FromMilliseconds(1250);

            Assert.False(updateScrub(position));
            Assert.Equal(1L, RejectedCount());
            Assert.Equal("not_ready:UpdateScrub pos_ms=1250", LastFailure());

            controllerType.GetField("_initialized", members)!.SetValue(controller, true);
            Assert.False(updateScrub(position));
            Assert.Equal(2L, RejectedCount());
            Assert.Equal("thread_not_running:UpdateScrub pos_ms=1250", LastFailure());

            var goLive = controllerType.GetMethod("GoLive", members)!.CreateDelegate<Func<bool>>(controller);
            Assert.False(goLive());
            Assert.Equal(2L, RejectedCount());
            Assert.Equal(string.Empty, LastFailure());

            var mailbox = controllerType.GetField("_commandMailbox", members)!.GetValue(controller)!;
            var generation = mailbox.GetType().GetProperty("CurrentGeneration", members)!.GetValue(mailbox)!;
            mailbox.GetType().GetMethod("Complete", members)!.Invoke(mailbox, new[] { generation });
            var sendSeek = controllerType.GetMethod("SendSeekCommand", members)!.CreateDelegate<Func<TimeSpan, bool>>(controller);
            Assert.False(sendSeek(position));
            Assert.Equal(2L, RejectedCount());
            Assert.Equal("write_failed:Seek pos_ms=1250", LastFailure());

            controller.Dispose();
            var ensureThread = controllerType.GetMethod("EnsurePlaybackThread", members)!;
            var pause = Enum.Parse(ensureThread.GetParameters()[0].ParameterType, "Pause");
            Assert.False((bool)ensureThread.Invoke(controller, new[] { pause })!);
            Assert.Equal(3L, RejectedCount());
            Assert.Equal("disposed:Pause", LastFailure());
            Assert.False((bool)controllerType.GetProperty("PlaybackThreadAlive", members)!.GetValue(controller)!);

            long RejectedCount() => (long)controllerType.GetProperty("CommandsRejected", members)!.GetValue(controller)!;
            string LastFailure() => (string)controllerType.GetProperty("LastCommandFailure", members)!.GetValue(controller)!;
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public Task FlashbackPlaybackInOutPointChangesStopAfterDispose()
        => global::Program.FlashbackPlaybackController_InOutPointChangesStopAfterDispose();

}
}

static partial class Program
{
    internal static Task CaptureService_FlashbackExportThrottleRespondsToLiveQueuePressure()
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var resolve = serviceType.GetMethod("ResolveFlashbackExportThrottleDelayMs", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveFlashbackExportThrottleDelayMs not found.");
        AssertEqual(0, (int)resolve.Invoke(null, new object[] { 0.49, 29L, false })!, "Flashback export throttle idle");
        AssertEqual(1, (int)resolve.Invoke(null, new object[] { 0.49, 0L, true })!, "Flashback export throttle high-resolution live baseline");
        AssertEqual(16, (int)resolve.Invoke(null, new object[] { 0.50, 0L, true })!, "High-resolution baseline must not hide queue pressure");
        AssertEqual(25, (int)resolve.Invoke(null, new object[] { 0.90, 0L, true })!, "High-resolution severe pressure still backs off");
        AssertEqual(16, (int)resolve.Invoke(null, new object[] { 0.50, 0L, false })!, "Flashback export throttle queue half full");
        AssertEqual(16, (int)resolve.Invoke(null, new object[] { 0.0, 30L, false })!, "Flashback export throttle oldest frame mild pressure");
        AssertEqual(20, (int)resolve.Invoke(null, new object[] { 0.70, 0L, false })!, "Flashback export throttle medium queue pressure");
        AssertEqual(20, (int)resolve.Invoke(null, new object[] { 0.0, 50L, false })!, "Flashback export throttle medium frame age");
        AssertEqual(25, (int)resolve.Invoke(null, new object[] { 0.85, 0L, false })!, "Flashback export throttle severe queue pressure");
        AssertEqual(25, (int)resolve.Invoke(null, new object[] { 0.0, 90L, false })!, "Flashback export throttle severe frame age");

        return Task.CompletedTask;
    }

    internal static async Task FlashbackExporter_ExportAsync_ReturnsFailure_WhenInputFileNotFound()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("Sussudio.Models.FlashbackExportRequest");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportAsync", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ExportAsync not found.");

        var nonexistentInput = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.ts");
        var outputPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid():N}.mp4");
        var request = Activator.CreateInstance(requestType)!;
        SetPropertyBackingField(request, "InputPath", nonexistentInput);
        SetPropertyBackingField(request, "InPoint", TimeSpan.Zero);
        SetPropertyBackingField(request, "OutPoint", TimeSpan.FromSeconds(10));
        SetPropertyBackingField(request, "OutputPath", outputPath);
        SetPropertyBackingField(request, "FastStart", true);

        var task = exportMethod.Invoke(exporter, new object?[]
        {
            request,
            null,
            CancellationToken.None
        }) as Task ?? throw new InvalidOperationException("ExportAsync did not return Task.");

        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        var succeeded = GetBoolProperty(result, "Succeeded");
        AssertEqual(false, succeeded, "Export fails when input file not found");
    }

    internal static async Task FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathEmpty()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("Sussudio.Models.FlashbackExportRequest");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportAsync", BindingFlags.Public | BindingFlags.Instance)!;

        // Create a real temp file so input validation passes
        var tempInput = Path.Combine(Path.GetTempPath(), $"fb_input_{Guid.NewGuid():N}.ts");
            File.WriteAllBytes(tempInput, new byte[] { 0x47 }); // MPEG-TS sync byte
        try
        {
            var request = Activator.CreateInstance(requestType)!;
            SetPropertyBackingField(request, "InputPath", tempInput);
            SetPropertyBackingField(request, "InPoint", TimeSpan.Zero);
            SetPropertyBackingField(request, "OutPoint", TimeSpan.FromSeconds(10));
            SetPropertyBackingField(request, "OutputPath", "");
            SetPropertyBackingField(request, "FastStart", true);

            var task = exportMethod.Invoke(exporter, new object?[]
            {
                request,
                null,
                CancellationToken.None
            }) as Task ?? throw new InvalidOperationException("ExportAsync did not return Task.");

            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Export fails when output path empty");
        }
        finally
        {
            global::Program.TryDeleteFile(tempInput);
        }
    }

    internal static async Task FlashbackExporter_ExportAsync_ReturnsFailure_WhenOutputPathIsDirectory()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("Sussudio.Models.FlashbackExportRequest");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportAsync", BindingFlags.Public | BindingFlags.Instance)!;

        var tempInput = Path.Combine(Path.GetTempPath(), $"fb_input_{Guid.NewGuid():N}.ts");
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"fb_export_dir_{Guid.NewGuid():N}");
        File.WriteAllBytes(tempInput, new byte[] { 0x47 });
        Directory.CreateDirectory(outputDirectory);
        try
        {
            var request = Activator.CreateInstance(requestType)!;
            SetPropertyBackingField(request, "InputPath", tempInput);
            SetPropertyBackingField(request, "InPoint", TimeSpan.Zero);
            SetPropertyBackingField(request, "OutPoint", TimeSpan.FromSeconds(10));
            SetPropertyBackingField(request, "OutputPath", outputDirectory);
            SetPropertyBackingField(request, "FastStart", true);

            var task = exportMethod.Invoke(exporter, new object?[]
            {
                request,
                null,
                CancellationToken.None
            }) as Task ?? throw new InvalidOperationException("ExportAsync did not return Task.");

            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Export fails when output path is a directory");
            AssertContains(GetStringProperty(result, "StatusMessage"), "output path is a directory");
            AssertEqual(false, File.Exists(outputDirectory + ".tmp"), "Directory-target export does not create temp output");
        }
        finally
        {
            global::Program.TryDeleteFile(tempInput);
            global::Program.TryDeleteDirectory(outputDirectory);
        }
    }

    internal static async Task FlashbackExporter_ExportSegmentsAsync_ReturnsFailure_WhenNoSegments()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportSegmentsAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("ExportSegmentsAsync not found.");

        var emptySegments = Array.CreateInstance(segmentType, 0);
        var outputPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid():N}.mp4");

        var task = exportMethod.Invoke(exporter, new object?[]
        {
            emptySegments,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10),
            outputPath,
            true,
            null,
            CancellationToken.None
        }) as Task ?? throw new InvalidOperationException("ExportSegmentsAsync did not return Task.");

        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Export segments fails when no segments");
    }

    internal static async Task FlashbackExporter_RejectsNullRequests()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("Sussudio.Models.FlashbackExportRequest");
        var exporter = Activator.CreateInstance(exporterType)!;
        var exportMethod = exporterType.GetMethod("ExportAsync", new[] { requestType, typeof(IProgress<>).MakeGenericType(RequireType("Sussudio.Models.ExportProgress")), typeof(CancellationToken) })
            ?? throw new InvalidOperationException("FlashbackExporter.ExportAsync(request) not found.");

        var task = exportMethod.Invoke(exporter, new object?[]
        {
            null,
            null,
            CancellationToken.None
        }) as Task ?? throw new InvalidOperationException("ExportAsync did not return Task.");

        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Null export request reports failure");
        AssertContains(GetStringProperty(result, "StatusMessage"), "request is required");
    }

    internal static Task FlashbackExporter_ReturnsCancellationResult_WhenLockWaitCancelled()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var exporter = Activator.CreateInstance(exporterType)!;
        try
        {
            var segment = Activator.CreateInstance(segmentType)!;
            SetPropertyBackingField(segment, "Path", Path.Combine(Path.GetTempPath(), $"fb_missing_{Guid.NewGuid():N}.mp4"));
            var segments = Array.CreateInstance(segmentType, 1);
            segments.SetValue(segment, 0);
            var outputPath = Path.Combine(Path.GetTempPath(), $"fb_cancelled_{Guid.NewGuid():N}.mp4");

            var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");

            var result = exportSegmentsCore.Invoke(exporter, new object?[]
            {
                segments,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                outputPath,
                true,
                null,
                cts.Token
            }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Cancelled export reports failure result");
            AssertContains(GetStringProperty(result, "StatusMessage"), "cancelled");
            AssertEqual("flashback-export-cancelled", GetStringProperty(result, "FailureCode"), "Cancellation carries explicit identity");
            AssertEqual(false, File.Exists(outputPath), "Cancelled export does not create output");
            AssertEqual(false, File.Exists(outputPath + ".tmp"), "Cancelled export does not leave temp output");
        }
        finally
        {
            if (exporter is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_CancellationWinsBeforeValidation()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var exporter = Activator.CreateInstance(exporterType)!;
        try
        {
            var exportCore = exporterType.GetMethod("ExportCore", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackExporter.ExportCore not found.");
            var singleOutputPath = Path.Combine(Path.GetTempPath(), $"fb_cancel_single_{Guid.NewGuid():N}.mp4");
            var singleResult = exportCore.Invoke(exporter, new object?[]
            {
                Path.Combine(Path.GetTempPath(), $"fb_missing_{Guid.NewGuid():N}.ts"),
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                singleOutputPath,
                true,
                null,
                cts.Token
            }) ?? throw new InvalidOperationException("ExportCore returned null.");

            AssertEqual(false, GetBoolProperty(singleResult, "Succeeded"), "Cancelled single-file export reports failure");
            AssertContains(GetStringProperty(singleResult, "StatusMessage"), "cancelled");
            AssertEqual("flashback-export-cancelled", GetStringProperty(singleResult, "FailureCode"), "Cancellation carries explicit identity");
            AssertDoesNotContain(GetStringProperty(singleResult, "StatusMessage"), "not found");

            var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");
            var emptySegments = Array.CreateInstance(segmentType, 0);
            var segmentOutputPath = Path.Combine(Path.GetTempPath(), $"fb_cancel_segments_{Guid.NewGuid():N}.mp4");
            var segmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
            {
                emptySegments,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1),
                segmentOutputPath,
                true,
                null,
                cts.Token
            }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

            AssertEqual(false, GetBoolProperty(segmentResult, "Succeeded"), "Cancelled segment export reports failure");
            AssertContains(GetStringProperty(segmentResult, "StatusMessage"), "cancelled");
            AssertEqual("flashback-export-cancelled", GetStringProperty(segmentResult, "FailureCode"), "Cancellation carries explicit identity");
            AssertDoesNotContain(GetStringProperty(segmentResult, "StatusMessage"), "no segment paths");
        }
        finally
        {
            if (exporter is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExportFailureClassifier_MapsCommandFailures()
    {
        var method = RequireType("Sussudio.Services.Flashback.FlashbackExportFailureCodes").GetMethod("Classify", BindingFlags.Static | BindingFlags.NonPublic)!;
        var codes = RequireType("Sussudio.Services.Flashback.FlashbackExportFailureCodes");
        var create = codes.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)!;
        var categories = new[] { "BufferInactive", "InvalidRequest", "InvalidRange", "UnavailableDuringRecording", "InvalidOutputPath", "InputUnavailable", "OutputWriteFailed", "InputReadFailed", "NoMediaWritten", "IncompleteLiveEdge", "ForceRotateFailed", "SegmentUnavailable", "InvalidInputStream", "Disposed", "Cancelled", "Timeout", "Failed" };
        foreach (var category in categories)
        {
            var code = codes.GetField(category, BindingFlags.Static | BindingFlags.NonPublic)!.GetRawConstantValue();
            var result = create.Invoke(null, new object?[] { "cancel-timeout.mp4", "unrelated output path cancel timeout segment path", code, null });
            AssertEqual(category, method.Invoke(null, new[] { result })?.ToString(), "FailureCode selects the wire category independently of display text");
        }
        var unknown = create.Invoke(null, new object?[] { "cancel.mp4", "Flashback export cancelled.", "unknown-code", null });
        AssertEqual("Failed", method.Invoke(null, new[] { unknown })?.ToString(), "Unknown code never falls back to message parsing");
        // The export diagnostics that classify failures moved out of CaptureService into
        // FlashbackExportState, so the classifier call is pinned against its new owner.
        var source = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExportState.cs");
        AssertContains(source, "FlashbackExportFailureCodes.Classify(result)");
        AssertDoesNotContain(source, "ContainsFlashbackExportFailureText");
        // Neither owner may regress to parsing display text for the failure category.
        AssertDoesNotContain(
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs"),
            "ContainsFlashbackExportFailureText");

        return Task.CompletedTask;
    }
    internal static Task FlashbackExporter_RejectsInvalidExportRanges()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_invalid_range_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var inputPath = Path.Combine(tempDir, "input.ts");
        File.WriteAllBytes(inputPath, new byte[] { 0x47 });

        try
        {
            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var exportCore = exporterType.GetMethod("ExportCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportCore not found.");
                var singleOutputPath = Path.Combine(tempDir, "single-invalid.mp4");
                var singleResult = exportCore.Invoke(exporter, new object?[]
                {
                    inputPath,
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(5),
                    singleOutputPath,
                    true,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportCore returned null.");

                AssertEqual(false, GetBoolProperty(singleResult, "Succeeded"), "Empty single-file export range reports failure");
                AssertContains(GetStringProperty(singleResult, "StatusMessage"), "export range is empty or invalid");
                AssertEqual(false, File.Exists(singleOutputPath), "Invalid single-file range does not create output");
                AssertEqual(false, File.Exists(singleOutputPath + ".tmp"), "Invalid single-file range does not leave temp output");

                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", inputPath);
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);
                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");
                var segmentOutputPath = Path.Combine(tempDir, "segment-invalid.mp4");
                var segmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.FromSeconds(-1),
                    TimeSpan.FromSeconds(1),
                    segmentOutputPath,
                    true,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(segmentResult, "Succeeded"), "Negative segment export in point reports failure");
                AssertContains(GetStringProperty(segmentResult, "StatusMessage"), "in point must not be negative");
                AssertEqual(false, File.Exists(segmentOutputPath), "Invalid segment range does not create output");
                AssertEqual(false, File.Exists(segmentOutputPath + ".tmp"), "Invalid segment range does not leave temp output");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_PreservesLaterValidMicrophoneStreamTemplate()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var templateStreamScore = exporterType.GetMethod(
                "TemplateStreamContributesToTemplateScore",
                BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TemplateStreamContributesToTemplateScore not found.");
        var avMediaType = templateStreamScore.GetParameters()[0].ParameterType;
        var audioParamsMatch = exporterType
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(method =>
                method.Name == "AudioParamsMatchOrCanUseTemplate" &&
                method.GetParameters().Length == 6);
        var sourceText = ReadFlashbackExporterSource();
        var streamsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExporter.cs")
            .Replace("\r\n", "\n");
        var templateSelectionBlock = ExtractTextBetween(
            sourceText,
            "private bool TryInitializeSegmentOutputTemplate(",
            "    private bool TryOpenSegmentInputForExport");
        var audioType = Enum.Parse(avMediaType, "AVMEDIA_TYPE_AUDIO");
        var videoType = Enum.Parse(avMediaType, "AVMEDIA_TYPE_VIDEO");
        var subtitleType = Enum.Parse(avMediaType, "AVMEDIA_TYPE_SUBTITLE");

        AssertEqual(
            false,
            (bool)templateStreamScore.Invoke(null, new object?[] { audioType, 0, 0, 0, 2 })!,
            "audio template stream without sample rate is not usable");
        AssertEqual(
            false,
            (bool)templateStreamScore.Invoke(null, new object?[] { audioType, 0, 0, 48000, 0 })!,
            "audio template stream without channels is not usable");
        AssertEqual(
            true,
            (bool)templateStreamScore.Invoke(null, new object?[] { audioType, 0, 0, 48000, 2 })!,
            "audio template stream with sample rate and channels is usable");
        AssertEqual(
            false,
            (bool)templateStreamScore.Invoke(null, new object?[] { videoType, 0, 1080, 0, 0 })!,
            "video template stream without width is not usable");
        AssertEqual(
            true,
            (bool)templateStreamScore.Invoke(null, new object?[] { videoType, 1920, 1080, 0, 0 })!,
            "video template stream with dimensions is usable");
        AssertEqual(
            true,
            (bool)templateStreamScore.Invoke(null, new object?[] { subtitleType, 0, 0, 0, 0 })!,
            "non-audio/video streams keep their template slot");

        AssertEqual(
            true,
            (bool)audioParamsMatch.Invoke(null, new object?[] { 0, 2, 8, 48000, 2, 8 })!,
            "incomplete input sample rate can use complete template audio params");
        AssertEqual(
            false,
            (bool)audioParamsMatch.Invoke(null, new object?[] { 48000, 2, 8, 44100, 2, 8 })!,
            "audio sample-rate mismatch is rejected");
        AssertEqual(
            false,
            (bool)audioParamsMatch.Invoke(null, new object?[] { 48000, 2, 8, 48000, 1, 8 })!,
            "audio channel mismatch is rejected");
        AssertEqual(
            false,
            (bool)audioParamsMatch.Invoke(null, new object?[] { 48000, 2, 8, 48000, 2, 9 })!,
            "audio sample-format mismatch is rejected");
        AssertEqual(
            true,
            (bool)audioParamsMatch.Invoke(null, new object?[] { 48000, 2, 8, 48000, 2, 8 })!,
            "matching complete audio params are accepted");

        AssertContains(streamsText, "private static int CountUsableTemplateStreams(AVFormatContext* inputContext, int inputStreamCount)");
        AssertContains(streamsText, "private static bool TemplateStreamContributesToTemplateScore(");
        AssertContains(streamsText, "AVMediaType.AVMEDIA_TYPE_AUDIO => channelCount > 0 && sampleRate > 0,");
        AssertContains(streamsText, "AVMediaType.AVMEDIA_TYPE_VIDEO => width > 0 && height > 0,");
        AssertContains(streamsText, "private static bool AudioParamsMatchOrCanUseTemplate(AVCodecParameters* inputCodec, AVCodecParameters* templateCodec)");
        AssertContains(streamsText, "var inputAudioParamsIncomplete = inputSampleRate <= 0 || inputChannelCount <= 0;");
        AssertContains(streamsText, "var templateHasCompleteAudioParams = templateSampleRate > 0 && templateChannelCount > 0;");
        AssertContains(streamsText, "return templateHasCompleteAudioParams;");
        AssertContains(streamsText, "if (!AudioParamsMatchOrCanUseTemplate(inputCodec, templateCodec))");
        AssertContains(streamsText, "audio_params expected=");
        AssertContains(templateSelectionBlock, "var bestMappedStreamCount = -1;");
        AssertContains(templateSelectionBlock, "var candidateMappedStreamCount = CountUsableTemplateStreams(_activeInputContext, candidateStreamCount);");
        AssertContains(templateSelectionBlock, "FLASHBACK_EXPORT_TEMPLATE_CANDIDATE");
        AssertContains(templateSelectionBlock, "mapped_streams={candidateMappedStreamCount}");
        AssertContains(templateSelectionBlock, "if (candidateMappedStreamCount > bestMappedStreamCount)");
        AssertContains(templateSelectionBlock, "bestTemplatePath = templatePath;");
        AssertContains(templateSelectionBlock, "OpenInput(bestTemplatePath);");
        AssertContains(templateSelectionBlock, "FLASHBACK_EXPORT_TEMPLATE_SELECTED");
        AssertContains(templateSelectionBlock, "mapped_streams={bestMappedStreamCount}");
        AssertOccursBefore(
            templateSelectionBlock,
            "var candidateMappedStreamCount = CountUsableTemplateStreams(_activeInputContext, candidateStreamCount);",
            "OpenInput(bestTemplatePath);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_RetainsHiddenDecoderPreroll()
    {
        var exporter = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var normalize = exporter.GetMethod("NormalizePacketTimestamps", BindingFlags.Static | BindingFlags.NonPublic)!;
        var resolveBase = exporter.GetMethod("ResolveSegmentOutputTimestampBaseUs", BindingFlags.Static | BindingFlags.NonPublic)!;
        // A GOP before a non-keyframe cut must retain distinct negative timestamps.
        // Clamping them to zero destroys decoding order and exposes the lead-in.
        Assert.Equal((-300L, -320L), ((long, long))normalize.Invoke(null, new object[] { -300L, -320L, true })!);
        Assert.Equal((-290L, -310L), ((long, long))normalize.Invoke(null, new object[] { -290L, -310L, true })!);
        Assert.Equal((0L, 0L), ((long, long))normalize.Invoke(null, new object[] { -300L, -320L, false })!);
        Assert.Equal((long.MinValue, long.MinValue), ((long, long))normalize.Invoke(null, new object[] { long.MinValue, long.MinValue, true })!);
        Assert.Equal(1_350_000L, resolveBase.Invoke(null, new object[] { 1_000_000L, 350_000L }));
        Assert.Equal(long.MaxValue, resolveBase.Invoke(null, new object[] { long.MaxValue - 2, 10L }));
        var createState = exporter.GetMethod("CreateSegmentPacketWriteState", BindingFlags.Static | BindingFlags.NonPublic)!;
        var state = createState.Invoke(null, new object[] { 0, 2, true, 165_700_000L, 30_000L, 5_130_000L, 0L, 0, 8_333L })!;
        state.GetType().GetProperty("MinBaseUs")!.SetValue(state, 165_630_667L);
        Assert.Equal(165_700_000L, state.GetType().GetProperty("TimestampOriginUs")!.GetValue(state));
        Assert.Equal(165_730_000L, resolveBase.Invoke(null, new object[] { state.GetType().GetProperty("TimestampOriginUs")!.GetValue(state)!, 30_000L }));
        var source = ReadFlashbackExporterSource();
        AssertContains(source, "outputContext->avoid_negative_ts = ffmpeg.AVFMT_AVOID_NEG_TS_DISABLED;");
        AssertContains(source, "\"use_editlist\", \"1\"");
        AssertContains(source, "ffmpeg.av_seek_frame(_activeInputContext, state.VideoStreamIndex, seekTimestamp, ffmpeg.AVSEEK_FLAG_BACKWARD)");
        AssertContains(source, "if ((packet->flags & ffmpeg.AV_PKT_FLAG_KEY) == 0) continue;");
        AssertContains(source, "var keyframeLimitUs = Math.Max(targetUs, state.TimestampBasesUs[state.VideoStreamIndex]);");
        AssertContains(source, "if (ptsUs > keyframeLimitUs) break;");
        AssertContains(source, "FreeBufferedPackets(state.BufferedPackets, state.BufferedStreamIndices);");
        AssertContains(source, "preservePreroll: state.UseSegmentTimeline && state.SegmentInOffsetUs > 0");
        AssertDoesNotContain(source, "comparePtsUs < state.SegmentInOffsetUs");
        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_PreservesTimelineAcrossDelayedSegmentPackets()
    {
        var exporter = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var resolveOffset = exporter.GetMethod("ResolveSegmentOutputOffsetUs", BindingFlags.Static | BindingFlags.NonPublic)!;
        long Offset(TimeSpan? start, TimeSpan inPoint, long fallback)
            => (long)resolveOffset.Invoke(null, new object?[] { start, inPoint, fallback })!;

        // Real 4K120 rotation: the next file contains two delayed packets before
        // its nominal 150-second boundary. Packing after the preceding file's
        // last frame would overlap them and collapse two frame durations.
        var offsetUs = Offset(TimeSpan.FromMilliseconds(1_002_975), TimeSpan.FromMilliseconds(852_975), 149_983_333);
        Assert.Equal(150_000_000L, offsetUs);
        Assert.Equal(149_983_333L, offsetUs - 16_667);
        Assert.Equal(149_991_667L, offsetUs - 8_333);
        Assert.Equal(175_000L, Offset(TimeSpan.FromMilliseconds(1_002_975), TimeSpan.FromMilliseconds(1_002_800), 158_333));
        Assert.Equal(0L, Offset(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12), 0));
        Assert.Equal(7_000_000L, Offset(null, TimeSpan.Zero, 7_000_000));
        AssertContains(ReadFlashbackExporterSource(), "ResolveSegmentOutputOffsetUs(segment.StartPts, inPoint, outputPtsOffsetUs)");
        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_RejectsEmptySegmentPaths()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_empty_segment_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", " ");
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);
                var outputPath = Path.Combine(tempDir, "empty-segment-export.mp4");

                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");

                var result = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    outputPath,
                    true,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Empty segment path export reports failure");
                AssertContains(GetStringProperty(result, "StatusMessage"), "segment path at index 0 is empty");
                AssertEqual(false, File.Exists(outputPath), "Empty segment path export does not create output");
                AssertEqual(false, File.Exists(outputPath + ".tmp"), "Empty segment path export does not leave temp output");

                var nullSegments = Array.CreateInstance(segmentType, 1);
                var nullSegmentOutputPath = Path.Combine(tempDir, "null-segment-export.mp4");
                var nullSegmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    nullSegments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    nullSegmentOutputPath,
                    true,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null for null segment.");

                AssertEqual(false, GetBoolProperty(nullSegmentResult, "Succeeded"), "Null segment export reports failure");
                AssertContains(GetStringProperty(nullSegmentResult, "StatusMessage"), "segment path at index 0 is empty");
                AssertEqual(false, File.Exists(nullSegmentOutputPath), "Null segment export does not create output");
                AssertEqual(false, File.Exists(nullSegmentOutputPath + ".tmp"), "Null segment export does not leave temp output");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_RejectsDuplicateSegmentPaths()
    {
        var sourceText = ReadFlashbackExporterSource();
        AssertContains(sourceText, "var duplicateSegmentIndex = FindDuplicateSegmentPathIndex(segments);");
        AssertContains(sourceText, "Flashback export failed: duplicate segment path at index {duplicateSegmentIndex}.");
        AssertContains(sourceText, "private static int FindDuplicateSegmentPathIndex(IReadOnlyList<FlashbackExportSegment> segments)");

        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_duplicate_segment_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var segmentPath = Path.Combine(tempDir, "segment-0.ts");
                File.WriteAllText(segmentPath, "segment");

                var firstSegment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(firstSegment, "Path", segmentPath);
                var duplicateSegment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(duplicateSegment, "Path", Path.Combine(tempDir, ".", "segment-0.ts"));

                var segments = Array.CreateInstance(segmentType, 2);
                segments.SetValue(firstSegment, 0);
                segments.SetValue(duplicateSegment, 1);
                var outputPath = Path.Combine(tempDir, "duplicate-segment-export.mp4");

                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");

                var result = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    outputPath,
                    true,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Duplicate segment path export reports failure");
                AssertContains(GetStringProperty(result, "StatusMessage"), "duplicate segment path at index 1");
                AssertEqual(false, File.Exists(outputPath), "Duplicate segment path export does not create output");
                AssertEqual(false, File.Exists(outputPath + ".tmp"), "Duplicate segment path export does not leave temp output");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_ReturnsFailure_WhenSegmentFilesAreGone()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_missing_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", Path.Combine(tempDir, "missing-segment.ts"));
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);
                var outputPath = Path.Combine(tempDir, "missing-export.mp4");

                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");

                var result = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    outputPath,
                    true,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Missing segment export reports failure");
                AssertContains(GetStringProperty(result, "StatusMessage"), "no readable segment files");
                AssertEqual(false, File.Exists(outputPath), "Missing segment export does not create output");
                AssertEqual(false, File.Exists(outputPath + ".tmp"), "Missing segment export does not leave temp output");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_OutputPathValidation_ReturnsFailure()
    {
        var sourceText = ReadFlashbackExporterSource();

        AssertContains(sourceText, "if (!TryValidateOutputPath(outputPath, out var normalizedOutputPath, out var outputPathFailure))\n        {\n            Logger.Log($\"FLASHBACK_EXPORT_FAIL reason='{outputPathFailure}'\");\n            return FlashbackExportFailureCodes.Create(outputPath, outputPathFailure, FlashbackExportFailureCodes.InvalidOutputPath);\n        }\n        outputPath = normalizedOutputPath;");
        AssertContains(sourceText, "if (!TryValidateExportRange(inPoint, outPoint, out var rangeFailure))");
        AssertContains(sourceText, "private static bool TryValidateExportRange(TimeSpan inPoint, TimeSpan outPoint, out string failureMessage)");
        AssertContains(sourceText, "failureMessage = \"Flashback export failed: in point must not be negative.\";");
        AssertContains(sourceText, "failureMessage = \"Flashback export failed: export range is empty or invalid.\";");
        AssertContains(sourceText, "var invalidSegmentIndex = FindInvalidSegmentPathIndex(segments);");
        AssertContains(sourceText, "Flashback export failed: segment path at index {invalidSegmentIndex} is empty.");
        AssertContains(sourceText, "private static int FindInvalidSegmentPathIndex(IReadOnlyList<FlashbackExportSegment> segments)");
        AssertContains(sourceText, "private static bool TryValidateOutputPath(string outputPath, out string fullOutputPath, out string failureMessage)");
        AssertContains(sourceText, "fullOutputPath = Path.GetFullPath(outputPath);");
        AssertContains(sourceText, "failureMessage = \"Flashback export failed: output path is required.\";");
        AssertContains(sourceText, "catch (Exception ex)\n        {\n            failureMessage = $\"Flashback export failed: output path is invalid '{outputPath}'.\";\n            Logger.Log($\"FLASHBACK_EXPORT_PATH_VALIDATE_WARN path='{outputPath}' type={ex.GetType().Name} msg='{ex.Message}'\");\n            return false;\n        }");
        AssertContains(sourceText, "failureMessage = $\"Flashback export failed: output path is invalid '{outputPath}'.\";");
        AssertContains(sourceText, "failureMessage = $\"Flashback export failed: output directory does not exist for '{outputPath}'.\";");
        AssertContains(sourceText, "if (Directory.Exists(fullOutputPath))\n        {\n            failureMessage = $\"Flashback export failed: output path is a directory '{outputPath}'.\";\n            return false;\n        }");
        AssertContains(sourceText, "FLASHBACK_EXPORT_PATH_COMPARE_WARN");
        AssertContains(sourceText, "FLASHBACK_EXPORT_PROGRESS_ESTIMATE_WARN");

        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var validateOutputPath = exporterType.GetMethod("TryValidateOutputPath", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TryValidateOutputPath not found.");
        var args = new object?[] { ".\\flashback-relative-output.mp4", null, null };
        var isValid = (bool)validateOutputPath.Invoke(null, args)!;
        AssertEqual(true, isValid, "Relative output path validates when current directory exists");
        AssertEqual(
            Path.GetFullPath(".\\flashback-relative-output.mp4"),
            (string)args[1]!,
            "Relative output path is normalized to full path");
        AssertEqual(string.Empty, (string)args[2]!, "Valid output path has no failure message");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_RejectsOutputPathThatOverwritesSource()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_paths_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var sourcePath = Path.Combine(tempDir, "fb_source_0001.mp4");
            File.WriteAllBytes(sourcePath, new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 });

            var exporter = Activator.CreateInstance(exporterType)!;
            try
            {
                var exportCore = exporterType.GetMethod("ExportCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportCore not found.");
                var singleResult = exportCore.Invoke(exporter, new object?[]
                {
                    sourcePath,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    sourcePath,
                    true,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportCore returned null.");

                AssertEqual(false, GetBoolProperty(singleResult, "Succeeded"), "Single-file export rejects source overwrite");
                AssertContains(GetStringProperty(singleResult, "StatusMessage"), "must not overwrite source segment");
                AssertEqual(8L, new FileInfo(sourcePath).Length, "Single-file rejection preserves source bytes");

                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", sourcePath);
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);

                var exportSegmentsCore = exporterType.GetMethod("ExportSegmentsCore", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FlashbackExporter.ExportSegmentsCore not found.");
                var segmentResult = exportSegmentsCore.Invoke(exporter, new object?[]
                {
                    segments,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(1),
                    sourcePath,
                    true,
                    null,
                    CancellationToken.None
                }) ?? throw new InvalidOperationException("ExportSegmentsCore returned null.");

                AssertEqual(false, GetBoolProperty(segmentResult, "Succeeded"), "Segment export rejects source overwrite");
                AssertContains(GetStringProperty(segmentResult, "StatusMessage"), "must not overwrite source segment");
                AssertEqual(8L, new FileInfo(sourcePath).Length, "Segment rejection preserves source bytes");

                var sourceText = ReadFlashbackExporterSource();
                AssertDoesNotContain(sourceText, "var tmpPath = outputPath + \".tmp\";");
                AssertContains(sourceText, "FlashbackExportOutputTransaction.TryReserve(outputPath, out outputTransaction, out var tempOutputFailure, out var tempOutputFailureCode)");
            }
            finally
            {
                if (exporter is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        finally
        {
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackOutputTransaction_CreatesUniqueTempOutputPaths()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_temp_unique_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? firstTransaction = null;
        object? secondTransaction = null;

        try
        {
            var outputPath = Path.Combine(tempDir, "export.mp4");
            firstTransaction = ReserveOutputTransaction(outputPath);
            var firstTempPath = GetOutputTransactionTemporaryPath(firstTransaction);
            secondTransaction = ReserveOutputTransaction(outputPath);
            var secondTempPath = GetOutputTransactionTemporaryPath(secondTransaction);

            AssertEqual(true, File.Exists(firstTempPath), "First unique temp file is reserved");
            AssertEqual(true, File.Exists(secondTempPath), "Second unique temp file is reserved");
            AssertEqual(false, string.Equals(firstTempPath, secondTempPath, StringComparison.OrdinalIgnoreCase), "Temp paths are unique per export");
            AssertEqual(false, string.Equals(firstTempPath, outputPath + ".tmp", StringComparison.OrdinalIgnoreCase), "Temp path is not deterministic output-plus-tmp");
            AssertContains(Path.GetFileName(firstTempPath), ".mp4.tmp");
            AssertContains(Path.GetFileName(secondTempPath), ".mp4.tmp");
        }
        finally
        {
            if (firstTransaction is IDisposable firstDisposable)
            {
                firstDisposable.Dispose();
            }

            if (secondTransaction is IDisposable secondDisposable)
            {
                secondDisposable.Dispose();
            }

            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackOutputTransaction_CleanupOrphanedTempFiles_HandlesNonexistentDirectory()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExportOutputTransaction");
        var cleanup = exporterType.GetMethod("CleanupOrphanedTempFiles", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CleanupOrphanedTempFiles not found.");

        cleanup.Invoke(null, new object[] { Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}") });

        return Task.CompletedTask;
    }

    internal static Task FlashbackOutputTransaction_CleanupOrphanedTempFiles_DeletesTempFiles()
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExportOutputTransaction");
        var cleanup = exporterType.GetMethod("CleanupOrphanedTempFiles", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CleanupOrphanedTempFiles not found.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_cleanup_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var orphan1 = Path.Combine(tempDir, "clip_a.mp4.tmp");
            var orphan2 = Path.Combine(tempDir, "clip_b.mp4.tmp");
            var recentTemp = Path.Combine(tempDir, "clip_recent.mp4.tmp");
            var lockedTemp = Path.Combine(tempDir, "clip_locked.mp4.tmp");
            var unrelated = Path.Combine(tempDir, "unrelated.mp4");
            var legacyTemp = Path.Combine(tempDir, "fb_export_temp_001.ts");

            File.WriteAllText(orphan1, "data");
            File.WriteAllText(orphan2, "data");
            File.WriteAllText(recentTemp, "keep");
            File.WriteAllText(lockedTemp, "keep");
            File.WriteAllText(unrelated, "keep");
            File.WriteAllText(legacyTemp, "keep");
            var oldEnough = DateTime.UtcNow - TimeSpan.FromMinutes(30);
            File.SetLastWriteTimeUtc(orphan1, oldEnough);
            File.SetLastWriteTimeUtc(orphan2, oldEnough);
            File.SetLastWriteTimeUtc(lockedTemp, oldEnough);

            using var lockedStream = new FileStream(lockedTemp, FileMode.Open, FileAccess.Read, FileShare.None);

            cleanup.Invoke(null, new object[] { tempDir });

            AssertEqual(false, File.Exists(orphan1), "First mp4 temp deleted");
            AssertEqual(false, File.Exists(orphan2), "Second mp4 temp deleted");
            AssertEqual(true, File.Exists(recentTemp), "Recent mp4 temp preserved");
            AssertEqual(true, File.Exists(lockedTemp), "Locked mp4 temp preserved");
            AssertEqual(true, File.Exists(unrelated), "Unrelated file preserved");
            AssertEqual(true, File.Exists(legacyTemp), "Legacy TS temp preserved by mp4 cleanup");
        }
        finally
        {
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_DoesNotScanUserOutputDirectoryForOrphans()
    {
        var sourceText = ReadFlashbackExporterSource();

        AssertDoesNotContain(sourceText, "private static void CleanupOrphanedTempFilesNearOutput(string outputPath)");
        AssertDoesNotContain(sourceText, "FLASHBACK_EXPORT_ORPHAN_OUTPUT_SCAN_FAIL");

        var singleExportBlock = ExtractTextBetween(
            sourceText,
            "private FinalizeResult ExportCore(",
            "    private FinalizeResult ExportSegmentsCore(");
        AssertDoesNotContain(singleExportBlock, "var tmpPath = outputPath + \".tmp\";");
        AssertDoesNotContain(singleExportBlock, "CleanupOrphanedTempFilesNearOutput(outputPath);");

        var segmentExportBlock = ExtractTextBetween(
            sourceText,
            "private FinalizeResult ExportSegmentsCore(",
            "    private SegmentPacketWriteResult WriteSegmentPacketsToActiveOutput(");
        AssertDoesNotContain(segmentExportBlock, "var tmpPath = outputPath + \".tmp\";");
        AssertDoesNotContain(segmentExportBlock, "CleanupOrphanedTempFilesNearOutput(outputPath);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackOutputTransaction_EmptyTempDoesNotReplaceExistingOutput()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_finalize_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? transaction = null;

        try
        {
            var outputPath = Path.Combine(tempDir, "existing-export.mp4");
            var existingBytes = new byte[] { 0x65, 0x78, 0x70, 0x6f, 0x72, 0x74 };
            File.WriteAllBytes(outputPath, existingBytes);
            transaction = ReserveOutputTransaction(outputPath);
            var tmpPath = GetOutputTransactionTemporaryPath(transaction);
            ReleaseOutputTransactionReservation(transaction);
            var finalized = PublishOutputTransaction(transaction, outputPath, out _, out var failureMessage, out var failureCode, out var outputPreserved);

            AssertEqual(false, finalized, "Invalid temp output is rejected");
            AssertContains(failureMessage, "temporary output file is empty before replacing");
            AssertEqual("flashback-export-no-media-written", failureCode, "Empty temporary media carries NoMediaWritten");
            AssertEqual(false, outputPreserved, "Pre-move validation failure has no preserved output");
            AssertEqual(true, File.Exists(outputPath), "Existing export remains present");
            AssertEqual(existingBytes.Length, new FileInfo(outputPath).Length, "Existing export length is preserved");
            AssertEqual(false, File.Exists(tmpPath), "Invalid temp output is deleted");
        }
        finally
        {
            if (transaction is IDisposable disposable)
            {
                disposable.Dispose();
            }
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExporter_RefusesOverwriteForSingleFile()
        => VerifyExporterRefusesExistingDestinationAsync(useSegments: false);

    internal static Task FlashbackExporter_RefusesOverwriteForSegments()
        => VerifyExporterRefusesExistingDestinationAsync(useSegments: true);

    private static async Task VerifyExporterRefusesExistingDestinationAsync(bool useSegments)
    {
        var exporterType = RequireType("Sussudio.Services.Flashback.FlashbackExporter");
        var requestType = RequireType("Sussudio.Models.FlashbackExportRequest");
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_existing_{useSegments}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? exporter = null;

        try
        {
            var inputPath = Path.Combine(tempDir, "input.ts");
            var outputPath = Path.Combine(tempDir, "existing-take.mp4");
            var existingBytes = new byte[] { 0x6f, 0x6c, 0x64 };
            File.WriteAllBytes(inputPath, new byte[] { 0x47 });
            File.WriteAllBytes(outputPath, existingBytes);

            exporter = Activator.CreateInstance(exporterType)!;
            var request = Activator.CreateInstance(requestType)!;
            if (useSegments)
            {
                var segmentType = RequireType("Sussudio.Models.FlashbackExportSegment");
                var segment = Activator.CreateInstance(segmentType)!;
                SetPropertyBackingField(segment, "Path", inputPath);
                SetPropertyBackingField(segment, "StartPts", TimeSpan.Zero);
                SetPropertyBackingField(segment, "EndPts", TimeSpan.FromSeconds(1));
                var segments = Array.CreateInstance(segmentType, 1);
                segments.SetValue(segment, 0);
                SetPropertyBackingField(request, "Segments", segments);
            }
            else
            {
                SetPropertyBackingField(request, "InputPath", inputPath);
            }

            SetPropertyBackingField(request, "InPoint", TimeSpan.Zero);
            SetPropertyBackingField(request, "OutPoint", TimeSpan.FromSeconds(1));
            SetPropertyBackingField(request, "OutputPath", outputPath);
            SetPropertyBackingField(request, "FastStart", true);

            var export = exporterType.GetMethod("ExportAsync", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("FlashbackExporter.ExportAsync not found.");
            var task = export.Invoke(exporter, new object?[] { request, null, CancellationToken.None }) as Task
                ?? throw new InvalidOperationException("FlashbackExporter.ExportAsync did not return Task.");
            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task)!;

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), $"Segments={useSegments} refuses an existing destination");
            AssertContains(GetStringProperty(result, "StatusMessage"), "Flashback export does not overwrite existing files");
            AssertEqual("flashback-export-invalid-output-path", GetStringProperty(result, "FailureCode"), "Existing destination carries InvalidOutputPath");
            AssertEqual(
                true,
                existingBytes.AsSpan().SequenceEqual(File.ReadAllBytes(outputPath)),
                $"Segments={useSegments} preserves destination bytes");
            AssertEqual(0, Directory.EnumerateFiles(tempDir, "*.mp4.tmp").Count(), $"Segments={useSegments} creates no temporary output");
        }
        finally
        {
            if (exporter is IDisposable disposable)
            {
                disposable.Dispose();
            }
            global::Program.TryDeleteDirectory(tempDir);
        }
    }

    internal static Task FlashbackExportOutputTransaction_PublishesWithLiveReservation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_publish_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? transaction = null;

        try
        {
            var outputPath = Path.Combine(tempDir, "final.mp4");
            var expectedBytes = new byte[] { 0x66, 0x69, 0x6e, 0x61, 0x6c };
            transaction = ReserveOutputTransaction(outputPath);
            var tempPath = GetOutputTransactionTemporaryPath(transaction);
            WriteTransactionTempWhileReservationIsLive(tempPath, expectedBytes);

            var published = PublishOutputTransaction(transaction, outputPath, out var outputBytes, out var failureMessage, out _, out var outputPreserved);

            AssertEqual(true, published, $"Owned temporary output publishes: {failureMessage}");
            AssertEqual(true, outputPreserved, "Published output is reported as preserved");
            AssertEqual((long)expectedBytes.Length, outputBytes, "Published byte count is reported");
            AssertEqual(
                true,
                expectedBytes.AsSpan().SequenceEqual(File.ReadAllBytes(outputPath)),
                "Published bytes match the completed temporary output");
            AssertEqual(false, File.Exists(tempPath), "Temporary path is consumed by publication");

            ((IDisposable)transaction).Dispose();
            AssertEqual(
                true,
                expectedBytes.AsSpan().SequenceEqual(File.ReadAllBytes(outputPath)),
                "Disposal never removes a published output");
        }
        finally
        {
            if (transaction is IDisposable disposable)
            {
                disposable.Dispose();
            }
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackOutputTransaction_ReservationBlocksPathReplacement()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_temp_reservation_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? transaction = null;

        try
        {
            var outputPath = Path.Combine(tempDir, "export.mp4");
            transaction = ReserveOutputTransaction(outputPath);
            var tempPath = GetOutputTransactionTemporaryPath(transaction);
            var replacementPath = tempPath + ".replacement";
            var replacementBlocked = false;

            try
            {
                File.Move(tempPath, replacementPath);
            }
            catch (IOException)
            {
                replacementBlocked = true;
            }
            catch (UnauthorizedAccessException)
            {
                replacementBlocked = true;
            }

            AssertEqual(true, replacementBlocked, "Open reservation blocks replacement before native writer opens by name");
            AssertEqual(true, File.Exists(tempPath), "Reserved path remains present");
            AssertEqual(false, File.Exists(replacementPath), "Replacement path was not created");
        }
        finally
        {
            if (transaction is IDisposable disposable)
            {
                disposable.Dispose();
            }

            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackExportOutputTransaction_RefusesDestinationCreatedAfterReservation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_publish_race_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? transaction = null;

        try
        {
            var outputPath = Path.Combine(tempDir, "final.mp4");
            var existingBytes = new byte[] { 0x65, 0x78, 0x69, 0x73, 0x74, 0x69, 0x6e, 0x67 };
            transaction = ReserveOutputTransaction(outputPath);
            var tempPath = GetOutputTransactionTemporaryPath(transaction);
            WriteTransactionTempWhileReservationIsLive(tempPath, new byte[] { 0x6e, 0x65, 0x77 });
            File.WriteAllBytes(outputPath, existingBytes);

            var published = PublishOutputTransaction(transaction, outputPath, out _, out var failureMessage, out var failureCode, out var outputPreserved);

            AssertEqual(false, published, "A destination created after reservation is not replaced");
            AssertContains(failureMessage, "destination file already exists");
            AssertEqual("flashback-export-invalid-output-path", failureCode, "Destination race carries InvalidOutputPath");
            AssertEqual(false, outputPreserved, "Move failure has no preserved output");
            AssertEqual(
                true,
                existingBytes.AsSpan().SequenceEqual(File.ReadAllBytes(outputPath)),
                "The raced-in destination keeps its bytes");
            AssertEqual(false, File.Exists(tempPath), "The owned temporary output is cleaned after refusal");
        }
        finally
        {
            if (transaction is IDisposable disposable)
            {
                disposable.Dispose();
            }
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackOutputTransaction_PostMoveValidationFailurePreservesOutput()
    {
        var transactionType = RequireType("Sussudio.Services.Flashback.FlashbackExportOutputTransaction");
        var finalizeCore = transactionType.GetMethod("TryPublishCore", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.TryPublishCore not found.");
        var validatorType = transactionType.GetNestedType("CompletedOutputValidator", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.CompletedOutputValidator not found.");
        var validatorMethod = typeof(Program).GetMethod(
            nameof(ValidateFinalOutputFailureAfterMove),
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ValidateFinalOutputFailureAfterMove not found.");
        var validator = Delegate.CreateDelegate(validatorType, validatorMethod);

        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_final_validate_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? transaction = null;

        try
        {
            var outputPath = Path.Combine(tempDir, "final.mp4");
            transaction = ReserveOutputTransaction(outputPath);
            var tmpPath = GetOutputTransactionTemporaryPath(transaction);
            ReleaseOutputTransactionReservation(transaction);
            File.WriteAllBytes(tmpPath, new byte[] { 0x66, 0x69, 0x6e, 0x61, 0x6c });

            var args = new object?[] { outputPath, 0L, string.Empty, string.Empty, validator, false };
            var finalized = (bool)(finalizeCore.Invoke(transaction, args)
                ?? throw new InvalidOperationException("TryPublishCore returned null."));

            AssertEqual(false, finalized, "Final validation failure is rejected");
            AssertContains((string)args[2]!, "forced final validation failure");
            AssertEqual("flashback-export-output-write-failed", (string)args[3]!, "Post-move validation preserves explicit failure identity");
            AssertEqual(true, (bool)args[5]!, "Post-move validation reports the moved output as preserved");
            AssertEqual(false, File.Exists(tmpPath), "Temporary output was moved before final validation");
            AssertEqual(true, File.Exists(outputPath), "Invalid moved final output is not deleted by path");
            AssertEqual(5L, new FileInfo(outputPath).Length, "Moved output bytes remain for caller/operator inspection");
        }
        finally
        {
            if (transaction is IDisposable disposable)
            {
                disposable.Dispose();
            }
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackOutputTransaction_ReplacedTempPathIsNeverPublishedOrDeleted()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_replaced_temp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? transaction = null;

        try
        {
            var outputPath = Path.Combine(tempDir, "final.mp4");
            transaction = ReserveOutputTransaction(outputPath);
            var tempPath = GetOutputTransactionTemporaryPath(transaction);
            ReleaseOutputTransactionReservation(transaction);
            File.WriteAllBytes(tempPath, new byte[] { 0x6f, 0x77, 0x6e, 0x65, 0x64 });

            var originalPath = tempPath + ".original";
            File.Move(tempPath, originalPath);
            var replacementBytes = new byte[] { 0x6e, 0x6f, 0x74, 0x2d, 0x6f, 0x75, 0x72, 0x73 };
            File.WriteAllBytes(tempPath, replacementBytes);

            var published = PublishOutputTransaction(transaction, outputPath, out _, out var failureMessage, out var failureCode, out var outputPreserved);

            AssertEqual(false, published, "Replaced temp path is not published");
            AssertEqual(false, outputPreserved, "Failed move does not report a preserved output");
            AssertContains(failureMessage, "temporary output path was replaced");
            AssertEqual(false, File.Exists(outputPath), "Replacement is not published as final output");
            AssertEqual(5L, new FileInfo(originalPath).Length, "Original reserved temp remains outside the replacement path");
            AssertEqual((long)replacementBytes.Length, new FileInfo(tempPath).Length, "Replacement temp remains intact after failed publication");

            ((IDisposable)transaction).Dispose();
            AssertEqual((long)replacementBytes.Length, new FileInfo(tempPath).Length, "Disposal does not delete a replacement temp path");
        }
        finally
        {
            if (transaction is IDisposable disposable)
            {
                disposable.Dispose();
            }
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackOutputTransaction_AbandonDeletesOnlyItsUncommittedTemp()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"fb_export_abandon_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        object? transaction = null;

        try
        {
            var outputPath = Path.Combine(tempDir, "final.mp4");
            transaction = ReserveOutputTransaction(outputPath);
            var tempPath = GetOutputTransactionTemporaryPath(transaction);
            ReleaseOutputTransactionReservation(transaction);
            File.WriteAllBytes(tempPath, new byte[] { 0x70, 0x61, 0x72, 0x74, 0x69, 0x61, 0x6c });

            ((IDisposable)transaction).Dispose();

            AssertEqual(false, File.Exists(tempPath), "Uncommitted transaction temp is deleted on abandonment");
            AssertEqual(false, File.Exists(outputPath), "Abandonment never creates a final output");
        }
        finally
        {
            if (transaction is IDisposable disposable)
            {
                disposable.Dispose();
            }
            global::Program.TryDeleteDirectory(tempDir);
        }

        return Task.CompletedTask;
    }

    private static object ReserveOutputTransaction(string outputPath)
    {
        var transactionType = RequireType("Sussudio.Services.Flashback.FlashbackExportOutputTransaction");
        var reserve = transactionType.GetMethod("TryReserve", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.TryReserve not found.");
        var args = new object?[] { outputPath, null, string.Empty, string.Empty };
        var reserved = (bool)(reserve.Invoke(null, args)
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.TryReserve returned null."));
        AssertEqual(true, reserved, "Temporary output transaction is reserved");
        return args[1] ?? throw new InvalidOperationException("Temporary output transaction was not returned.");
    }

    private static string GetOutputTransactionTemporaryPath(object transaction)
    {
        var property = transaction.GetType().GetProperty(
            "TemporaryPath",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.TemporaryPath not found.");
        return property.GetValue(transaction)?.ToString()
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.TemporaryPath was null.");
    }

    private static void ReleaseOutputTransactionReservation(object transaction)
    {
        var release = transaction.GetType().GetMethod("ReleaseReservation", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.ReleaseReservation not found.");
        release.Invoke(transaction, null);
    }

    private static void WriteTransactionTempWhileReservationIsLive(string tempPath, byte[] bytes)
    {
        using var writer = new FileStream(
            tempPath,
            FileMode.Open,
            FileAccess.Write,
            FileShare.ReadWrite);
        writer.SetLength(0);
        writer.Write(bytes, 0, bytes.Length);
        writer.Flush(flushToDisk: true);
    }

    private static bool PublishOutputTransaction(
        object transaction,
        string outputPath,
        out long outputBytes,
        out string failureMessage,
        out string failureCode,
        out bool outputPreserved)
    {
        var publish = transaction.GetType().GetMethod("TryPublish", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.TryPublish not found.");
        var args = new object?[] { outputPath, 0L, string.Empty, string.Empty, false };
        var published = (bool)(publish.Invoke(transaction, args)
            ?? throw new InvalidOperationException("FlashbackExportOutputTransaction.TryPublish returned null."));
        outputBytes = (long)args[1]!;
        failureMessage = (string)args[2]!;
        failureCode = (string)args[3]!;
        outputPreserved = (bool)args[4]!;
        return published;
    }

    internal static Task FlashbackPlaybackController_InitialState_IsLive()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");

        var controller = ctor.Invoke(new[] { bufferManager });

        var stateStr = GetPropertyValue(controller, "State")?.ToString();
        AssertEqual("Live", stateStr, "Initial state is Live");

        var position = (TimeSpan)GetPropertyValue(controller, "PlaybackPosition")!;
        AssertEqual(TimeSpan.Zero, position, "Initial PlaybackPosition");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_CommandsNoOpBeforeInitialize()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)!;
        var controller = ctor.Invoke(new[] { bufferManager });

        var playMethod = controllerType.GetMethod("Play", BindingFlags.Public | BindingFlags.Instance);
        var pauseMethod = controllerType.GetMethod("Pause", BindingFlags.Public | BindingFlags.Instance);
        var goLiveMethod = controllerType.GetMethod("GoLive", BindingFlags.Public | BindingFlags.Instance);

        playMethod?.Invoke(controller, null);
        pauseMethod?.Invoke(controller, null);
        goLiveMethod?.Invoke(controller, null);

        var stateStr = GetPropertyValue(controller, "State")?.ToString();
        AssertEqual("Live", stateStr, "State unchanged after no-op commands");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_SuccessfulNoOps_ClearStaleCommandFailure()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");
        var controller = ctor.Invoke(new[] { bufferManager });
        SetPrivateField(controller, "_initialized", true);

        try
        {
            SeedCommandFailure(controller, "old_failure:EndScrub");
            AssertEqual(false, (bool)controllerType.GetMethod("EndScrub")!.Invoke(controller, null)!, "EndScrub live/no-thread no-op reports failure");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "EndScrub no-op clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "EndScrub no-op clears stale failure UTC");

            SeedCommandFailure(controller, "old_failure:GoLive");
            AssertEqual(false, (bool)controllerType.GetMethod("GoLive")!.Invoke(controller, null)!, "GoLive live/no-thread no-op reports failure");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "GoLive no-op clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "GoLive no-op clears stale failure UTC");

            SeedCommandFailure(controller, "old_failure:Nudge");
            AssertEqual(false, (bool)controllerType.GetMethod("NudgePosition")!.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(8.33) })!, "Nudge live/no-thread no-op reports failure");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "Nudge no-op clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "Nudge no-op clears stale failure UTC");
        }
        finally
        {
            (controller as IDisposable)?.Dispose();
            (bufferManager as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_CoalescedCommands_ClearStaleCommandFailure()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");
        var controller = ctor.Invoke(new[] { bufferManager });
        var sendSeek = controllerType.GetMethod("SendSeekCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendSeekCommand not found.");
        var sendUpdateScrub = controllerType.GetMethod("SendUpdateScrubCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendUpdateScrubCommand not found.");

        try
        {
            AssertEqual(true, (bool)sendSeek.Invoke(controller, new object[] { TimeSpan.FromSeconds(1) })!, "Initial seek enqueues");
            var initialSeekQueuedUtc = GetLongProperty(controller, "LastCommandQueuedUtcUnixMs");
            SeedCommandFailure(controller, "old_failure:Seek");
            AssertEqual(true, (bool)sendSeek.Invoke(controller, new object[] { TimeSpan.FromSeconds(2) })!, "Coalesced seek succeeds");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "Coalesced seek clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "Coalesced seek clears stale failure UTC");
            AssertEqual("Seek", GetStringProperty(controller, "LastCommandQueued"), "Coalesced seek keeps physical queued-command name");
            AssertEqual(initialSeekQueuedUtc, GetLongProperty(controller, "LastCommandQueuedUtcUnixMs"), "Coalesced seek does not refresh queued-command timestamp");
            AssertEqual(1L, GetLongProperty(controller, "SeekCommandsCoalesced"), "Coalesced seek counter");

            AssertEqual(true, (bool)sendUpdateScrub.Invoke(controller, new object[] { TimeSpan.FromSeconds(3) })!, "Initial scrub update enqueues");
            var initialScrubQueuedUtc = GetLongProperty(controller, "LastCommandQueuedUtcUnixMs");
            SeedCommandFailure(controller, "old_failure:UpdateScrub");
            AssertEqual(true, (bool)sendUpdateScrub.Invoke(controller, new object[] { TimeSpan.FromSeconds(4) })!, "Coalesced scrub update succeeds");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "Coalesced scrub update clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "Coalesced scrub update clears stale failure UTC");
            AssertEqual("UpdateScrub", GetStringProperty(controller, "LastCommandQueued"), "Coalesced scrub update keeps physical queued-command name");
            AssertEqual(initialScrubQueuedUtc, GetLongProperty(controller, "LastCommandQueuedUtcUnixMs"), "Coalesced scrub update does not refresh queued-command timestamp");
            AssertEqual(1L, GetLongProperty(controller, "ScrubUpdatesCoalesced"), "Coalesced scrub update counter");
        }
        finally
        {
            (controller as IDisposable)?.Dispose();
            (bufferManager as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_ClampsCommandPositionsBeforeFileLookup()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        // All three scrub-related command paths must clamp via the eviction-aware
        // overload so a long-held scrub doesn't resolve to evicted file PTS.
        const string seekClampBeforeOpen = "cmd = cmd with { Position = ClampPosition(cmd.Position, worker.FrozenValidStart) };\n        var seekResumeTarget = ClampPlaybackTargetToMinimumLiveLead(";
        const string scrubClampBeforeOpen = "cmd = cmd with { Position = ClampPosition(cmd.Position, worker.FrozenValidStart) };\n        worker.Decoder ??= CreateDecoder();\n        EnsureFileOpen(worker.Decoder, ref worker.FileOpen, SaturatingAdd(cmd.Position, worker.FrozenValidStart));";

        AssertContains(sourceText, seekClampBeforeOpen);
        AssertContains(sourceText, "SaturatingAdd(cmd.Position, worker.FrozenValidStart),\n            worker.FrozenValidStart,\n            \"seek\");");
        AssertContains(sourceText, "cmd = cmd with { Position = ClampPosition(SaturatingSubtract(seekResumeTarget, worker.FrozenValidStart), worker.FrozenValidStart) };");
        AssertContains(sourceText, "if (_commandMailbox.ShouldYieldSeekToQueuedPlay(commandChannel))\n        {\n            PlaybackPosition = cmd.Position;\n            worker.PendingExactResumeTarget = seekResumeTarget;");
        AssertContains(sourceText, "worker.Decoder ??= CreateDecoder();\n        EnsureFileOpen(worker.Decoder, ref worker.FileOpen, seekResumeTarget);");
        AssertEqual(1, sourceText.Split(scrubClampBeforeOpen, StringSplitOptions.None).Length - 1, "BeginScrub clamps before file lookup with frozen reference");
        var updateScrubBlock = ExtractTextBetween(
            sourceText,
            "private void HandleUpdateScrubCommand(",
            "    private void HandleEndScrubCommand(");
        AssertContains(updateScrubBlock, "cmd = cmd with { Position = ClampPosition(cmd.Position, worker.FrozenValidStart) };\n        if (_commandMailbox.ShouldYieldScrubUpdateToQueuedControl(commandChannel))");
        AssertContains(updateScrubBlock, "worker.Decoder ??= CreateDecoder();\n        EnsureFileOpen(worker.Decoder, ref worker.FileOpen, SaturatingAdd(cmd.Position, worker.FrozenValidStart));");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_TimestampArithmeticIsSaturating()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();

        AssertContains(sourceText, "private static TimeSpan SaturatingAdd(TimeSpan left, TimeSpan right)");
        AssertContains(sourceText, "private static TimeSpan SaturatingSubtract(TimeSpan left, TimeSpan right)");
        AssertContains(sourceText, "if (rightTicks > 0 && leftTicks > long.MaxValue - rightTicks)");
        AssertContains(sourceText, "if (rightTicks < 0 && leftTicks < long.MinValue - rightTicks)");
        AssertContains(sourceText, "if (rightTicks < 0 && leftTicks > long.MaxValue + rightTicks)");
        AssertContains(sourceText, "if (rightTicks > 0 && leftTicks < long.MinValue + rightTicks)");
        AssertDoesNotContain(sourceText, "cmd.Position + worker.FrozenValidStart");
        AssertDoesNotContain(sourceText, "PlaybackPosition + worker.FrozenValidStart");
        AssertDoesNotContain(sourceText, "PlaybackPosition + cmd.Delta");
        AssertDoesNotContain(sourceText, "bufferPosition + validStartPts");
        AssertDoesNotContain(sourceText, "pos + frozenValidStart");
        AssertDoesNotContain(sourceText, "nudgeFrame.Pts - worker.FrozenValidStart");
        AssertDoesNotContain(sourceText, "frame.Pts - validStartPts");
        AssertDoesNotContain(sourceText, "videoFrame.Pts - frozenValidStart");
        AssertDoesNotContain(sourceText, "latestAbsPts - lastFrameAbsPts");
        AssertDoesNotContain(sourceText, "absoluteLatestPts - absoluteFramePts");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_PauseFromLive_DisplaysBufferedFrameBeforePaused()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var publicPauseBlock = ExtractTextBetween(
            sourceText,
            "public bool Pause()",
            "    public bool GoLive()");

        var pauseFromLiveBlock = ExtractTextBetween(
            sourceText,
            "else if (State == FlashbackPlaybackState.Live)",
            "    private void HandleNudgeCommand(");

        AssertContains(publicPauseBlock, "return SendCommand(new PlaybackCommand { Kind = CommandKind.Pause });");
        AssertDoesNotContain(publicPauseBlock, "SeekAndDisplay");
        AssertContains(pauseFromLiveBlock, "SafeSuppressPreviewSubmission(\"pause_from_live\");");
        AssertContains(pauseFromLiveBlock, "SafePauseRendering(\"pause_from_live\");");
        AssertContains(pauseFromLiveBlock, "var pauseTarget = ResolvePauseFromLiveTarget(worker.FrozenValidStart);");
        AssertContains(pauseFromLiveBlock, "EnsureFileOpen(worker.Decoder, ref worker.FileOpen, SaturatingAdd(pausePos, worker.FrozenValidStart));");
        AssertContains(pauseFromLiveBlock, "if (!IsDecoderFileReady(worker.Decoder, worker.FileOpen))");
        AssertContains(pauseFromLiveBlock, "SetNoFileFailure(CommandKind.Pause, pausePos);");
        AssertContains(pauseFromLiveBlock, "if (_commandMailbox.ShouldYieldPauseFromLiveToQueuedSeekOrPlay(commandChannel))");
        AssertContains(pauseFromLiveBlock, "FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE_DEFER_DISPLAY");
        AssertContains(pauseFromLiveBlock, "if (!SeekAndDisplayKeyframe(worker.Decoder, ref worker.FileOpen, pausePos, worker.FrozenValidStart, CommandKind.Pause, cts.Token))");
        AssertContains(pauseFromLiveBlock, "RestoreLiveAfterSeekDisplayFailure(worker.Decoder, ref worker.FileOpen, \"pause_from_live_display_failed\");");
        AssertContains(pauseFromLiveBlock, "worker.PendingExactResumeTarget = SaturatingAdd(PlaybackPosition, worker.FrozenValidStart);");
        AssertContains(pauseFromLiveBlock, "SetState(FlashbackPlaybackState.Paused, \"user\");");
        AssertContains(pauseFromLiveBlock, "frozen_frame=true");
        AssertContains(sourceText, "private TimeSpan ResolvePauseFromLiveTarget(TimeSpan frozenValidStart)");
        AssertContains(sourceText, "return ClampPlaybackTargetToMinimumLiveLead(latestPts, frozenValidStart, \"pause_from_live\");");
        AssertDoesNotContain(pauseFromLiveBlock, "SeekAndDisplayExactFrame");
        AssertDoesNotContain(sourceText, "private void SeekAndDisplayExactFrame");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_NudgeCreatesDecoderWhenPaused()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();

        var nudgeBlock = ExtractTextBetween(
            sourceText,
            "private void HandleNudgeCommand(",
            "\n}");

        AssertContains(nudgeBlock, "worker.Decoder ??= CreateDecoder();");
        AssertContains(nudgeBlock, "EnsureFileOpen(worker.Decoder, ref worker.FileOpen, SaturatingAdd(nudgedPos, worker.FrozenValidStart));");
        AssertContains(nudgeBlock, "if (!IsDecoderFileReady(worker.Decoder, worker.FileOpen))");
        AssertContains(nudgeBlock, "FLASHBACK_PLAYBACK_NUDGE_NO_FILE");
        AssertContains(nudgeBlock, "RestoreLiveAfterNoFile(\"nudge_no_file\");");
        AssertContains(nudgeBlock, "if (!SeekAndDisplayKeyframe(worker.Decoder, ref worker.FileOpen, nudgedPos, worker.FrozenValidStart, CommandKind.Nudge, cts.Token))");
        AssertContains(nudgeBlock, "RestoreLiveAfterSeekDisplayFailure(worker.Decoder, ref worker.FileOpen, \"nudge_display_failed\");");
        AssertDoesNotContain(nudgeBlock, "if (worker.Decoder != null)");
        var noFileRestore = ExtractDeclaredMemberCode(sourceText, "private void RestoreLiveAfterNoFile(");
        AssertContains(noFileRestore, "ReleasePlaybackFrameForLive(operation);\n        RestoreLiveAudio();\n        SafeResumePreviewSubmission(operation);\n        SafeResumeRendering(operation);\n        SetState(FlashbackPlaybackState.Live, operation);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_InOutPoints_DefaultToUnset()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)!;
        var controller = ctor.Invoke(new[] { bufferManager });

        var inPointProp = controllerType.GetProperty("InPoint", BindingFlags.Public | BindingFlags.Instance);
        var outPointProp = controllerType.GetProperty("OutPoint", BindingFlags.Public | BindingFlags.Instance);

        AssertNotNull(inPointProp, "FlashbackPlaybackController.InPoint");
        AssertNotNull(outPointProp, "FlashbackPlaybackController.OutPoint");
        foreach (var propertyName in new[]
                 {
                     "CommandsEnqueued",
                     "CommandsProcessed",
                     "CommandsDropped",
                     "CommandsRejected",
                     "ScrubUpdatesCoalesced",
                     "PendingCommands",
                     "MaxPendingCommands",
                     "LastCommandQueueLatencyMs",
                     "MaxCommandQueueLatencyMs",
                     "LastCommandQueued",
                     "LastCommandProcessed",
                     "LastCommandQueuedUtcUnixMs",
                     "LastCommandProcessedUtcUnixMs",
                     "LastCommandFailureUtcUnixMs",
                     "LastCommandFailure",
                     "PlaybackThreadAlive"
                 })
        {
            AssertNotNull(controllerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance), propertyName);
        }

        var clearMethod = controllerType.GetMethod("ClearInOutPoints", BindingFlags.Public | BindingFlags.Instance);
        AssertNotNull(clearMethod, "FlashbackPlaybackController.ClearInOutPoints");
        clearMethod!.Invoke(controller, null);

        return Task.CompletedTask;
    }



    internal static Task FlashbackPlaybackController_InOutPointChangesStopAfterDispose()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        using var controller = (IDisposable)Activator.CreateInstance(controllerType, new[] { bufferManager })!;

        var setInPoint = controllerType.GetMethod("SetInPoint", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackPlaybackController.SetInPoint not found.");
        var setOutPoint = controllerType.GetMethod("SetOutPoint", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackPlaybackController.SetOutPoint not found.");
        var clearInOut = controllerType.GetMethod("ClearInOutPoints", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackPlaybackController.ClearInOutPoints not found.");

        setInPoint.Invoke(controller, null);
        controller.Dispose();
        clearInOut.Invoke(controller, null);
        setOutPoint.Invoke(controller, null);

        AssertEqual((TimeSpan?)TimeSpan.Zero, (TimeSpan?)GetPropertyValue(controller, "InPoint"), "disposed InPoint");
        AssertEqual<object?>(null, GetPropertyValue(controller, "OutPoint"), "disposed OutPoint");

        var sourceText = ReadFlashbackPlaybackControllerSource();
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SET_IN_SKIP reason=disposed");
        AssertContains(sourceText, "SetLastCommandFailure(\"disposed:SetInPoint\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SET_OUT_SKIP reason=disposed");
        AssertContains(sourceText, "SetLastCommandFailure(\"disposed:SetOutPoint\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CLEAR_INOUT_SKIP reason=disposed");
        AssertContains(sourceText, "SetLastCommandFailure(\"disposed:ClearInOutPoints\");");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_PtsCadenceTelemetry_TracksMismatches()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");
        var controller = ctor.Invoke(new[] { bufferManager });
        var track = controllerType.GetMethod("TrackDecodedPtsCadence", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TrackDecodedPtsCadence not found.");
        var reset = controllerType.GetMethod("ResetPlaybackMetrics", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResetPlaybackMetrics not found.");
        var expected = TimeSpan.FromMilliseconds(1000.0 / 120.0);

        try
        {
            track.Invoke(controller, new object[] { expected, expected });
            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 60.0), expected });
            AssertEqual(0L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "matching decoded PTS cadence count");

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 30.0), expected });
            AssertEqual(1L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "slow decoded PTS cadence count");
            AssertNearlyEqual(1000.0 / 60.0, GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs"), 0.1, "slow decoded PTS cadence delta");
            AssertNearlyEqual(expected.TotalMilliseconds, GetDoubleProperty(controller, "LastPlaybackPtsCadenceExpectedMs"), 0.1, "decoded PTS expected cadence");
            if (GetLongProperty(controller, "LastPlaybackPtsCadenceMismatchUtcUnixMs") <= 0)
            {
                throw new InvalidOperationException("Expected decoded PTS cadence mismatch timestamp to be populated.");
            }

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 30.0), expected });
            AssertEqual(2L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "duplicate decoded PTS cadence count");
            AssertNearlyEqual(0.0, GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs"), 0.1, "duplicate decoded PTS cadence delta");

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(25.0), expected });
            AssertEqual(3L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "backward decoded PTS cadence count");
            if (GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs") >= 0)
            {
                throw new InvalidOperationException("Expected backward decoded PTS cadence delta to be negative.");
            }

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 24.0), expected });
            AssertEqual(3L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "valid cadence after backward PTS remains clean");

            reset.Invoke(controller, null);
            AssertEqual(0L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "decoded PTS cadence reset count");
            AssertEqual(0.0, GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs"), "decoded PTS cadence reset delta");
        }
        finally
        {
            (controller as IDisposable)?.Dispose();
            (bufferManager as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_ResolveFrameRateParts_ParsesFractionalRates()
    {
        var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var method = sinkType.GetMethod("ResolveFrameRateParts", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveFrameRateParts not found.");

        // "60000/1001" â†’ (60000, 1001)
        var result1 = method.Invoke(null, new object[] { "60000/1001" });
        var (num1, den1) = GetTupleValues(result1!);
        AssertEqual(60000, num1, "60000/1001 numerator");
        AssertEqual(1001, den1, "60000/1001 denominator");

        // "30/1" â†’ (30, 1)
        var result2 = method.Invoke(null, new object[] { "30/1" });
        var (num2, den2) = GetTupleValues(result2!);
        AssertEqual(30, num2, "30/1 numerator");
        AssertEqual(1, den2, "30/1 denominator");

        // null â†’ (null, null)
        var result3 = method.Invoke(null, new object?[] { null });
        var (num3, den3) = GetNullableTupleValues(result3!);
        if (num3 != null)
            throw new InvalidOperationException($"Expected null numerator for null input, got {num3}");

        // Empty string â†’ (null, null)
        var result4 = method.Invoke(null, new object[] { "" });
        var (num4, den4) = GetNullableTupleValues(result4!);
        if (num4 != null)
            throw new InvalidOperationException($"Expected null numerator for empty input, got {num4}");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_MapCodecName_MapsFormats()
    {
        var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var method = sinkType.GetMethod("MapCodecName", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("MapCodecName not found.");

        var formatType = RequireType("Sussudio.Models.RecordingFormat");

        var hevc = method.Invoke(null, new[] { Enum.Parse(formatType, "HevcMp4") })?.ToString();
        AssertContains(hevc ?? "", "hevc");

        var h264 = method.Invoke(null, new[] { Enum.Parse(formatType, "H264Mp4") })?.ToString();
        AssertContains(h264 ?? "", "264");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_CountersDefaultToZero()
    {
        var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
        var ctor = sinkType.GetConstructor(new[] { optionsType })
            ?? throw new InvalidOperationException("FlashbackEncoderSink(FlashbackBufferOptions) constructor not found.");
        var sink = ctor.Invoke(new object?[] { null })!;

        AssertEqual(0L, GetLongProperty(sink, "DroppedVideoFrames"), "DroppedVideoFrames");
        AssertEqual(0L, GetLongProperty(sink, "EncodedVideoFrames"), "EncodedVideoFrames");
        AssertEqual(0L, GetLongProperty(sink, "AudioSamplesReceived"), "AudioSamplesReceived");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_HighResolutionCpuQueueCapacityIsBounded()
    {
        var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var contextType = RequireType("Sussudio.Models.FlashbackSessionContext");
        var resolve = sinkType.GetMethod("ResolveVideoQueueCapacity", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveVideoQueueCapacity not found.");

        var fourKContext = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(fourKContext, "Width", 3840);
        SetPropertyBackingField(fourKContext, "Height", 2160);
        var normalContext = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(normalContext, "Width", 1920);
        SetPropertyBackingField(normalContext, "Height", 1080);

        AssertEqual(128, (int)resolve.Invoke(null, new[] { fourKContext, false })!, "4K CPU Flashback queue capacity");
        AssertEqual(180, (int)resolve.Invoke(null, new[] { fourKContext, true })!, "4K GPU Flashback queue capacity");
        AssertEqual(180, (int)resolve.Invoke(null, new[] { normalContext, false })!, "1080p CPU Flashback queue capacity");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_EncoderPtsGuardsInvalidFrameRate()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        AssertDoesNotContain(sourceText, "TimeSpan.FromSeconds(_encoder.NextVideoPts / frameRate)");
        AssertDoesNotContain(sourceText, "TimeSpan.FromSeconds(_encoder.NextVideoPts / finalFrameRate)");
        AssertDoesNotContain(sourceText, "ptsBaseOffset.TotalSeconds * context.FrameRate");
        AssertContains(sourceText, "var sessionFrameRate = ResolveSessionFrameRate(context.FrameRate);");
        AssertContains(sourceText, "var sessionContext = context with { FrameRate = sessionFrameRate };");
        AssertContains(sourceText, "_encoder.Initialize(CreateOptions(sessionContext, tsPath));");
        AssertContains(sourceText, "_bufferManager.EncodeFrameRate = sessionFrameRate;");
        AssertContains(sourceText, "private const double FallbackSessionFrameRate = 30.0;");
        AssertContains(sourceText, "private const double MaxSessionFrameRate = 1000.0;");
        AssertContains(sourceText, "var currentPts = ResolveEncoderPts();");
        AssertContains(sourceText, "var finalPts = ResolveEncoderPts();");
        AssertContains(sourceText, "var crashPts = ResolveEncoderPts();");
        AssertContains(sourceText, "var pts = ResolveEncoderPts();");
        AssertContains(sourceText, "private TimeSpan ResolveEncoderPts()");
        AssertContains(sourceText, "var frameRate = ResolveSessionFrameRate(_sessionContext?.FrameRate ?? 30.0);");
        AssertContains(sourceText, "if (!double.IsFinite(seconds) || seconds <= 0)");
        AssertContains(sourceText, "if (!double.IsFinite(frameRate) || frameRate <= 0)\n        {\n            return FallbackSessionFrameRate;\n        }");
        AssertContains(sourceText, "return Math.Min(frameRate, MaxSessionFrameRate);");
        AssertContains(sourceText, "private static (int? Numerator, int? Denominator) ResolveSessionFrameRateParts(int? numerator, int? denominator)");
        AssertContains(sourceText, "if (!double.IsFinite(fps) || fps <= 0 || fps > MaxSessionFrameRate)");
        AssertContains(sourceText, "FrameRateNumerator = frameRateNumerator,");
        AssertContains(sourceText, "FrameRateDenominator = frameRateDenominator,");
        AssertContains(sourceText, "private static long ToNonNegativeLongSaturated(double value)");
        AssertContains(sourceText, "private static long NonNegativeByteDelta(long currentBytes, long startBytes)");
        AssertContains(sourceText, "private static TimeSpan NonNegativeDuration(TimeSpan end, TimeSpan start)");
        AssertContains(sourceText, "private static (TimeSpan StartPts, TimeSpan EndPts) ResumeEvictionBestEffort(\n        FlashbackBufferManager bufferManager,\n        string operation)");
        AssertContains(sourceText, "return bufferManager.ResumeEviction();");
        AssertContains(sourceText, "FLASHBACK_SINK_EVICTION_RESUME_WARN");
        AssertContains(sourceText, "return (bufferManager.RecordingStartPts, bufferManager.RecordingEndPts);");
        AssertContains(sourceText, "var finalSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));");
        AssertContains(sourceText, "var crashSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));");
        var rotateBlock = ExtractDeclaredMemberCode(
            sourceText,
            "private bool RotateSegment(TimeSpan currentPts, string? preparedPath = null)");
        // Preserve the completed segment's baseline before native rotation resets
        // byte counters or the sink advances its active segment's PTS and baseline.
        AssertOccursBefore(rotateBlock, "var completedStartPts = _segmentStartPts;", "_encoder.RotateOutput(newPath)");
        AssertOccursBefore(rotateBlock, "var completedStartBytes = Interlocked.Read(ref _segmentStartBytes);", "_encoder.RotateOutput(newPath)");
        AssertOccursBefore(rotateBlock, "_encoder.RotateOutput(newPath)", "completedSegmentBytes = NonNegativeByteDelta(result.PreviousTotalBytes, completedStartBytes);");
        AssertOccursBefore(rotateBlock, "completedSegmentBytes = NonNegativeByteDelta(result.PreviousTotalBytes, completedStartBytes);", "Interlocked.Exchange(ref _segmentStartBytes, _encoder.TotalBytesWritten);");
        AssertContains(rotateBlock, "_bufferManager.OnSegmentCompleted(completedPath!, completedStartPts, currentPts, completedSegmentBytes);");
        AssertContains(rotateBlock, "var failPts = encoderRotated ? currentPts : ResolveEncoderPts();");
        AssertContains(rotateBlock, "var failSegmentBytes = encoderRotated\n                            ? completedSegmentBytes\n                            : NonNegativeByteDelta(_encoder.TotalBytesWritten, completedStartBytes);");
        AssertContains(rotateBlock, "_bufferManager.OnSegmentCompleted(completedPath, completedStartPts, failPts, failSegmentBytes);");
        AssertDoesNotContain(rotateBlock, "result.PreviousTotalBytes - completedStartBytes");
        AssertDoesNotContain(rotateBlock, "_encoder.TotalBytesWritten - completedStartBytes");
        AssertDoesNotContain(sourceText, "_encoder.TotalBytesWritten - Interlocked.Read(ref _segmentStartBytes)");
        AssertDoesNotContain(sourceText, "result.PreviousTotalBytes - Interlocked.Read(ref _segmentStartBytes)");
        AssertDoesNotContain(sourceText, "LastRecordingEndPts - LastRecordingStartPts");

        return Task.CompletedTask;
    }



    internal static Task FlashbackEncoderSink_ForceRotateDrainingDoesNotRejectVideoByLifecycleState()
    {
        var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
        var ctor = sinkType.GetConstructor(new[] { optionsType })
            ?? throw new InvalidOperationException("FlashbackEncoderSink(FlashbackBufferOptions) constructor not found.");
        var sink = ctor.Invoke(new object?[] { null })!;
        var rejectReason = sinkType.GetMethod("GetVideoEnqueueRejectReason", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetVideoEnqueueRejectReason not found.");

        try
        {
            SetPrivateField(sink, "_started", true);
            SetPrivateField(sink, "_forceRotateDraining", true);

            // The encoder-lane fence must not reject live video.
            AssertEqual<string?>(null, rejectReason.Invoke(sink, new object[] { false }) as string, "Force-rotate draining accepts CPU video");
            AssertEqual<string?>(null, rejectReason.Invoke(sink, new object[] { true }) as string, "Force-rotate draining accepts GPU video");

            SetPrivateField(sink, "_videoQueueDepth", 180);
            SetPrivateField(sink, "_gpuQueueDepth", 8);
            AssertEqual<string?>(null, rejectReason.Invoke(sink, new object[] { false }) as string, "Force-rotate draining ignores CPU queue depth");
            AssertEqual<string?>(null, rejectReason.Invoke(sink, new object[] { true }) as string, "Force-rotate draining ignores GPU queue depth");

        }
        finally
        {
            (sink as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_DisposeResetsGpuQueueDepth()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        AssertContains(sourceText, "ReturnRemainingGpuBuffers(_gpuQueue, ref _gpuQueueDepth);");
        AssertContains(sourceText, "private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)");
        AssertContains(sourceText, "Interlocked.Exchange(ref queueDepth, 0);");
        AssertContains(sourceText, "var timeoutFailure = new TimeoutException(\"Flashback encode drain timed out while stopping.\");");
        AssertContains(sourceText, "_encodingFailure ??= timeoutFailure;");
        AssertContains(sourceText, "_encodingFailure ??= ex;");
        AssertContains(sourceText, "CancelEncodingCts(\"dispose\");");
        AssertContains(sourceText, "CancelEncodingCts(\"stop_timeout\");");
        AssertContains(sourceText, "private void CancelEncodingCts(string operation)");
        AssertContains(sourceText, "FLASHBACK_SINK_CANCEL_WARN");
        AssertContains(sourceText, "DisposeCtsBestEffort(_cts, \"start_fail\");");
        AssertContains(sourceText, "DisposeCtsBestEffort(_cts, \"finalize_dispose\");");
        AssertContains(sourceText, "DisposeWorkAvailableBestEffort(\"finalize_dispose\");");
        AssertContains(sourceText, "DisposeEncoderBestEffort(\"start_fail\");");
        AssertContains(sourceText, "DisposeEncoderBestEffort(\"finalize_dispose\");");
        AssertContains(sourceText, "DisposeEncoderBestEffort(\"encoding_loop_fatal\");");
        AssertContains(sourceText, "FLASHBACK_SINK_CTS_DISPOSE_WARN");
        AssertContains(sourceText, "FLASHBACK_SINK_WORK_SIGNAL_DISPOSE_WARN");
        AssertContains(sourceText, "private void SignalWork(string operation)");
        AssertContains(sourceText, "FLASHBACK_SINK_WORK_SIGNAL_SKIPPED");
        AssertContains(sourceText, "SignalWork(\"force_rotate_idle\");");
        AssertContains(sourceText, "SignalWork(\"force_rotate_request\");");
        AssertEqual(1, sourceText.Split("_workAvailable.Set();", StringSplitOptions.None).Length - 1, "All work-signal wakeups go through SignalWork");
        AssertContains(sourceText, "FLASHBACK_SINK_ENCODER_DISPOSE_WARN");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_SINK_BUFFER_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_RECORDING_FAIL type={failure.GetType().Name} error='{failure.Message}'\");");
        AssertContains(sourceText, "ReturnVideoPacketBestEffort(packet);");
        AssertContains(sourceText, "ReleaseGpuTextureBestEffort(packet.Texture);");
        AssertContains(sourceText, "FLASHBACK_SINK_RETURN_VIDEO_PACKET_WARN");
        AssertContains(sourceText, "FLASHBACK_SINK_RELEASE_GPU_PACKET_WARN");
        AssertContains(sourceText, "public long VideoQueueRejectedFrames => Interlocked.Read(ref _videoQueueRejectedFrames);");
        AssertContains(sourceText, "public string? LastVideoQueueRejectReason => Volatile.Read(ref _lastVideoQueueRejectReason);");
        AssertContains(sourceText, "public long GpuQueueRejectedFrames => Interlocked.Read(ref _gpuQueueRejectedFrames);");
        AssertContains(sourceText, "public string? LastGpuQueueRejectReason => Volatile.Read(ref _lastGpuQueueRejectReason);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _videoQueueRejectedFrames, 0);");
        AssertContains(sourceText, "Volatile.Write(ref _lastVideoQueueRejectReason, null);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _gpuQueueRejectedFrames, 0);");
        AssertContains(sourceText, "Volatile.Write(ref _lastGpuQueueRejectReason, null);");
        AssertContains(sourceText, "private string? GetVideoEnqueueRejectReason(bool isGpu)");
        AssertContains(sourceText, "private string? GetVideoInputRejectReason(Channel<VideoFramePacket>? queue, int expectedSize, bool dataIsEmpty)");
        AssertContains(sourceText, "private string? GetGpuInputRejectReason(Channel<GpuFramePacket>? queue, IntPtr texture)");
        AssertContains(sourceText, "Rotation has an encoder-lane fence.");
        AssertDoesNotContain(sourceText, "return \"force_rotate_draining\";");
        AssertContains(sourceText, "return \"cancelled\";");
        AssertContains(sourceText, "return \"disposed\";");
        AssertContains(sourceText, "return \"not_started\";");
        AssertContains(sourceText, "return \"queue_null\";");
        AssertContains(sourceText, "return \"invalid_expected_size\";");
        AssertContains(sourceText, "return dataIsEmpty ? \"data_empty\" : null;");
        AssertContains(sourceText, "return texture == IntPtr.Zero ? \"null_texture\" : null;");
        AssertContains(sourceText, "TrackGpuQueueRejected(\"invalid_subresource\");");
        AssertContains(sourceText, "? $\"encoding_failed:{failure.GetType().Name}\"");
        AssertContains(sourceText, "private void TrackVideoQueueRejected(string reason)");
        AssertContains(sourceText, "private void TrackGpuQueueRejected(string reason)");
        AssertContains(sourceText, "FLASHBACK_SINK_VIDEO_QUEUE_REJECT");
        AssertContains(sourceText, "FLASHBACK_SINK_GPU_QUEUE_REJECT");
        AssertContains(sourceText, "total == 1 || total % 30 == 0");
        // The claim/high-water/rollback sequence now lives in QueueAdmission
        // (behaviorally covered by QueueAdmissionTests) and is shared with
        // LibAvRecordingSink; each lane here supplies only its own counters and tag.
        AssertContains(sourceText, "private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(sourceText, "QueueAdmission.TryWrite(queue, packet, ref _videoQueueDepth, ref _videoQueueMaxDepth, \"video\", DecrementQueueDepth)");
        AssertContains(sourceText, "private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(sourceText, "QueueAdmission.TryWrite(queue, packet, ref _gpuQueueDepth, ref _gpuQueueMaxDepth, \"gpu\", DecrementQueueDepth)");
        AssertContains(sourceText, "private static bool TryWriteAudioPacket(");
        AssertContains(sourceText, "QueueAdmission.TryWrite(queue, packet, ref queueDepth, queueName, DecrementQueueDepth)");
        AssertDoesNotContain(sourceText, "var depth = Interlocked.Increment(ref _videoQueueDepth);");
        AssertDoesNotContain(sourceText, "var depth = Interlocked.Increment(ref _gpuQueueDepth);");
        AssertContains(sourceText, "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio\")");
        AssertContains(sourceText, "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio_after_evict\")");
        AssertContains(sourceText, "private static void DecrementQueueDepth(ref int target, string queueName)");
        // The clamped CAS loop now lives in AtomicCounter (behaviorally covered by
        // AtomicCounterTests); this sink keeps only its own underflow diagnostic.
        AssertContains(sourceText, "if (!AtomicCounter.TryDecrement(ref target))");
        AssertDoesNotContain(sourceText, "Interlocked.CompareExchange(ref target, current - 1, current)");
        AssertContains(sourceText, "FLASHBACK_SINK_QUEUE_DEPTH_UNDERFLOW");
        AssertContains(sourceText, "DecrementQueueDepth(ref _videoQueueDepth, \"video\");");
        AssertContains(sourceText, "DecrementQueueDepth(ref _gpuQueueDepth, \"gpu\");");
        AssertContains(sourceText, "DecrementQueueDepth(ref _audioQueueDepth, \"audio\");");
        AssertContains(sourceText, "DecrementQueueDepth(ref _microphoneQueueDepth, \"microphone\");");
        AssertDoesNotContain(sourceText, "private bool WaitForBackpressureRetryCancellation()");
        AssertDoesNotContain(sourceText, "=> WaitForCancellation(TimeSpan.FromMilliseconds(1));");
        AssertContains(sourceText, "private bool WaitForCancellation(TimeSpan timeout)");
        AssertContains(sourceText, "return cts.Token.WaitHandle.WaitOne(timeout);");
        AssertContains(sourceText, "catch (ObjectDisposedException)\n        {\n            return true;\n        }");
        AssertDoesNotContain(sourceText, "if (WaitForBackpressureRetryCancellation())");
        AssertContains(sourceText, "TrackVideoQueueRejected(\"queue_full\");");
        AssertContains(sourceText, "TrackGpuQueueRejected(\"queue_full\");");
        AssertDoesNotContain(sourceText, "FLASHBACK_SINK_VIDEO_BACKPRESSURE_DROP");
        AssertDoesNotContain(sourceText, "FLASHBACK_SINK_GPU_BACKPRESSURE_DROP");
        AssertDoesNotContain(sourceText, "FailEncoding(overloadFailure);");
        AssertDoesNotContain(sourceText, "Flashback recording video queue overloaded after");
        AssertDoesNotContain(sourceText, "Flashback GPU recording queue overloaded after");
        AssertContains(sourceText, "if (WaitForCancellation(TimeSpan.FromMilliseconds(10)))\n            {\n                return false;\n            }");
        AssertDoesNotContain(sourceText, "var depth = Interlocked.Decrement(ref target);");
        AssertDoesNotContain(sourceText, "Interlocked.Exchange(ref target, 0);\n        Logger.Log($\"FLASHBACK_SINK_QUEUE_DEPTH_UNDERFLOW");
        AssertDoesNotContain(sourceText, "Interlocked.Decrement(ref _videoQueueDepth)");
        AssertDoesNotContain(sourceText, "Interlocked.Decrement(ref _gpuQueueDepth)");
        AssertDoesNotContain(sourceText, "AtomicMax.Update(ref _videoQueueMaxDepth, Interlocked.Increment(ref _videoQueueDepth))");
        AssertDoesNotContain(sourceText, "AtomicMax.Update(ref _gpuQueueMaxDepth, Interlocked.Increment(ref _gpuQueueDepth))");
        AssertDoesNotContain(sourceText, "queue.Writer.TryWrite(packet))\n        {\n            Interlocked.Increment(ref queueDepth);");
        AssertDoesNotContain(sourceText, "Marshal.Release(packet.Texture);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_AudioPacketsAreValidatedBeforeRent()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        AssertContains(sourceText, "private const int AudioInputBlockAlignBytes = 2 * sizeof(float);");
        AssertContains(sourceText, "private const int MaxAudioPacketBytes = 4 * 1024 * 1024;");
        AssertContains(sourceText, "if (!TryValidateAudioPacketLength(samples.Length, \"audio\"))");
        AssertContains(sourceText, "if (!TryValidateAudioPacketLength(samples.Length, \"microphone\"))");
        AssertContains(sourceText, "private static bool TryValidateAudioPacketLength(int byteLength, string source)");
        AssertContains(sourceText, "if (byteLength <= 0 || byteLength > MaxAudioPacketBytes)");
        AssertContains(sourceText, "FLASHBACK_SINK_AUDIO_PACKET_REJECT source={source} reason=size");
        AssertContains(sourceText, "if (byteLength % AudioInputBlockAlignBytes != 0)");
        AssertContains(sourceText, "FLASHBACK_SINK_AUDIO_PACKET_REJECT source={source} reason=alignment");
        AssertContains(sourceText, "return byteLength > 0 ? byteLength / AudioInputBlockAlignBytes : 0;");
        AssertDoesNotContain(sourceText, "const int inputBlockAlign = 2 * sizeof(float);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        AssertContains(sourceText, "private const int VideoDrainBatchLimit = 24;");
        AssertContains(sourceText, "private const int GpuDrainBatchLimit = 16;");
        AssertContains(sourceText, "DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit)");
        AssertContains(sourceText, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertContains(sourceText, "private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(sourceText, "while (drainedCount < maxPackets)");
        AssertContains(sourceText, "private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(sourceText, "while (drainedCount < maxPackets && reader.TryRead(out var packet))");

        var loopBlock = ExtractTextBetween(
            sourceText,
            "private void EncodingLoop(CancellationToken cancellationToken)",
            "    private bool DrainVideoPackets");
        AssertOccursBefore(loopBlock, "DrainAudioPackets(audioQueue.Reader)", "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertOccursBefore(loopBlock, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)", "// Audio AGAIN");
        var secondAudioDrainBlock = ExtractTextBetween(
            loopBlock,
            "// Audio AGAIN",
            "// Handle force-rotate requests");
        AssertContains(secondAudioDrainBlock, "DrainAudioPackets(audioQueue.Reader)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_RotateFailureRestoresActiveSegment()
    {
        var sinkText = ReadFlashbackEncoderSinkSource();
        var bufferText = ReadFlashbackBufferManagerSource();

        var rotateBlock = ExtractTextBetween(
            sinkText,
            "private bool RotateSegment(TimeSpan currentPts, string? preparedPath = null)",
            "    public FlashbackForceRotateResult ForceRotateForExport");
        AssertContains(rotateBlock, "var completedPath = _tsFilePath;");
        AssertContains(rotateBlock, "string? newPath = null;");
        AssertContains(rotateBlock, "var encoderRotated = false;");
        AssertContains(rotateBlock, "var completedStartPts = _segmentStartPts;");
        AssertContains(rotateBlock, "newPath = preparedPath ?? _bufferManager.GenerateSegmentPath();");
        AssertContains(rotateBlock, "encoderRotated = true;");
        AssertOccursBefore(rotateBlock, "encoderRotated = true;", "_tsFilePath = newPath;");
        AssertOccursBefore(rotateBlock, "_tsFilePath = newPath;", "_bufferManager.OnSegmentCompleted(completedPath!, completedStartPts, currentPts, completedSegmentBytes);");
        AssertContains(rotateBlock, "if (newPath != null && !encoderRotated)");
        AssertContains(rotateBlock, "_bufferManager.AbandonReservedSegmentPath(newPath);");
        AssertContains(rotateBlock, "_bufferManager.AbandonGeneratedSegmentPath(newPath, completedPath);");

        var abandonBlock = ExtractTextBetween(
            bufferText,
            "public void AbandonGeneratedSegmentPath",
            "    public void OnSegmentCompleted");
        AssertContains(abandonBlock, "if (IsSameSegmentPath(_activeSegmentPath, generatedPath))");
        AssertContains(abandonBlock, "_activeSegmentPath = restoreActivePath;");
        AssertContains(abandonBlock, "_nextSegmentIndex--;");
        AssertContains(abandonBlock, "if (!IsSameSegmentPath(generatedPath, restoreActivePath))");
        AssertContains(abandonBlock, "TryDeleteFile(generatedPath);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_RegistersSegmentsOnCancellationAndRotationFailure()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        var cancelBlock = ExtractTextBetween(
            sourceText,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)",
            "        catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_SINK_ENCODING_LOOP_FATAL");
        AssertContains(cancelBlock, "Logger.Log(\"FLASHBACK_SINK_ENCODING_LOOP_CANCELLED\");");
        AssertContains(cancelBlock, "FailPendingForceRotate();");
        AssertContains(cancelBlock, "var cancelPts = ResolveEncoderPts();");
        AssertContains(cancelBlock, "if (cancelPts > _segmentStartPts)");
        AssertContains(cancelBlock, "var cancelSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));");
        AssertContains(cancelBlock, "_bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, cancelPts, cancelSegmentBytes);");
        AssertContains(cancelBlock, "FLASHBACK_SINK_ENCODING_LOOP_CANCELLED_SEGMENT_REGISTERED");
        AssertContains(cancelBlock, "FLASHBACK_SINK_CANCELLED_SEGMENT_REGISTER_FAIL");
        AssertContains(cancelBlock, "ReturnAllRemainingQueuedBuffers();");
        AssertOccursBefore(cancelBlock, "_bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, cancelPts, cancelSegmentBytes);", "ReturnAllRemainingQueuedBuffers();");

        var rotateFailureBlock = ExtractTextBetween(
            sourceText,
            "catch (Exception ex)\n        {\n            var terminalFailure = encoderRotated || !_encoder.IsEncoding;",
            "    public FlashbackForceRotateResult ForceRotateForExport");
        AssertContains(rotateFailureBlock, "Interlocked.Increment(ref _segmentRotationFailures);");
        AssertContains(rotateFailureBlock, "var failPts = encoderRotated ? currentPts : ResolveEncoderPts();");
        AssertContains(rotateFailureBlock, "if (failPts > completedStartPts)");
        AssertContains(rotateFailureBlock, "NonNegativeByteDelta(_encoder.TotalBytesWritten, completedStartBytes);");
        AssertContains(rotateFailureBlock, "_bufferManager.OnSegmentCompleted(completedPath, completedStartPts, failPts, failSegmentBytes);");
        AssertContains(rotateFailureBlock, "FLASHBACK_SINK_ROTATE_FAIL_SEGMENT_REGISTERED");
        AssertContains(rotateFailureBlock, "FLASHBACK_SINK_ROTATE_FAIL_SEGMENT_REGISTER_FAIL");
        AssertContains(rotateFailureBlock, "_segmentStartPts = currentPts;");
        AssertOccursBefore(rotateFailureBlock, "_bufferManager.OnSegmentCompleted(completedPath, completedStartPts, failPts, failSegmentBytes);", "_segmentStartPts = currentPts;");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_ForceRotateRejectsFailedEncoder()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        var forceRotateBlock = ExtractTextBetween(
            sourceText,
            "public FlashbackForceRotateResult ForceRotateForExport",
            "    private bool TryCancelForceRotate");
        AssertContains(forceRotateBlock, "CancellationToken cancellationToken = default");
        AssertContains(forceRotateBlock, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(forceRotateBlock, "if (inPoint < TimeSpan.Zero || outPoint <= inPoint)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_RANGE");
        AssertOccursBefore(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_RANGE", "var request = new ForceRotateRequest(preparedPath);");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_INACTIVE");
        AssertContains(forceRotateBlock, "if (_encodingFailure != null || _encodingTask?.IsCompleted == true)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED");
        AssertContains(forceRotateBlock, "return FlashbackForceRotateResult.Failed();");
        AssertContains(forceRotateBlock, "_bufferManager.ReserveSegmentPath();");
        AssertContains(forceRotateBlock, "var request = new ForceRotateRequest(preparedPath);");
        AssertContains(forceRotateBlock, "if (!_started || _disposed || _encodingFailure != null || _encodingTask?.IsCompleted == true)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_AFTER_LOCK");
        AssertOccursBefore(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_AFTER_LOCK", "_forceRotateRequest = request;");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_FatalSegmentRegistrationFailuresAreLogged()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        var fatalBlock = ExtractTextBetween(
            sourceText,
            "catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_SINK_ENCODING_LOOP_FATAL",
            "            ReturnAllRemainingQueuedBuffers();");
        AssertContains(fatalBlock, "catch (Exception segmentEx)");
        AssertContains(fatalBlock, "FLASHBACK_SINK_FATAL_SEGMENT_REGISTER_FAIL");
        AssertContains(fatalBlock, "Preserve the original fatal error.");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_CommandQueue_AcceptsNewestControlWhenFull()
    {
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var mailboxType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackCommandMailbox");
        var commandType = mailboxType.GetNestedType("Command", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PlaybackCommand not found.");
        var commandKindType = mailboxType.GetNestedType("CommandKind", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CommandKind not found.");
        var sendCommand = controllerType.GetMethod("SendCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendCommand not found.");
        var commandMailboxField = controllerType.GetField("_commandMailbox", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_commandMailbox not found.");
        var queueCapacityProperty = controllerType.GetProperty("CommandQueueCapacityCommands", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("CommandQueueCapacityCommands not found.");
        var commandsDroppedProperty = controllerType.GetProperty("CommandsDropped", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("CommandsDropped not found.");
        var pendingCommandsProperty = controllerType.GetProperty("PendingCommands", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("PendingCommands not found.");

        var bufferManager = Activator.CreateInstance(
                bufferManagerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { null },
                culture: null)
            ?? throw new InvalidOperationException("FlashbackBufferManager construction failed.");
        using var disposableBuffer = bufferManager as IDisposable;
        var controller = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController construction failed.");
        using var disposableController = controller as IDisposable;

        var playKind = Enum.Parse(commandKindType, "Play");
        var goLiveKind = Enum.Parse(commandKindType, "GoLive");
        var capacity = (int)queueCapacityProperty.GetValue(controller)!;

        for (var i = 0; i < capacity; i++)
        {
            var playCommand = Activator.CreateInstance(commandType)
                ?? throw new InvalidOperationException("PlaybackCommand play construction failed.");
            SetPropertyOrBackingField(playCommand, "Kind", playKind);
            AssertEqual(true, (bool)sendCommand.Invoke(controller, new[] { playCommand })!, $"Play command {i} enqueues");
        }

        AssertEqual(capacity, (int)pendingCommandsProperty.GetValue(controller)!, "Queue starts full");

        var goLiveCommand = Activator.CreateInstance(commandType)
            ?? throw new InvalidOperationException("PlaybackCommand GoLive construction failed.");
        SetPropertyOrBackingField(goLiveCommand, "Kind", goLiveKind);
        AssertEqual(true, (bool)sendCommand.Invoke(controller, new[] { goLiveCommand })!, "Newest GoLive command is accepted when queue is full");
        AssertEqual(capacity, (int)pendingCommandsProperty.GetValue(controller)!, "Drop-oldest accounting keeps pending bounded at capacity");

        var mailbox = commandMailboxField.GetValue(controller)
            ?? throw new InvalidOperationException("Command mailbox missing.");
        var generation = mailbox.GetType().GetProperty("CurrentGeneration", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(mailbox)
            ?? throw new InvalidOperationException("Command mailbox generation missing.");
        var channel = generation.GetType().GetProperty("Channel", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(generation)
            ?? throw new InvalidOperationException("Command channel missing.");
        var sawGoLive = false;
        while (TryReadQueuedPlaybackCommand(channel, commandType, out var command) && command != null)
        {
            if (GetPropertyValue(command, "Kind")?.ToString() == "GoLive")
            {
                sawGoLive = true;
            }
        }

        AssertEqual(true, sawGoLive, "Full command queue preserves the newest GoLive command");
        AssertEqual(true, (long)commandsDroppedProperty.GetValue(controller)! > 0, "Dropped-command diagnostics record the evicted older command");

        return Task.CompletedTask;

        static bool TryReadQueuedPlaybackCommand(object channel, Type commandType, out object? command)
        {
            var reader = channel.GetType().GetProperty("Reader")?.GetValue(channel)
                ?? throw new InvalidOperationException("Command channel reader missing.");
            var tryRead = reader.GetType().GetMethod(
                    "TryRead",
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    types: new[] { commandType.MakeByRefType() },
                    modifiers: null)
                ?? throw new InvalidOperationException("Command channel TryRead not found.");
            object?[] args = { null };
            var result = (bool)tryRead.Invoke(reader, args)!;
            command = args[0];
            return result;
        }
    }


    private static string ReadFlashbackPlaybackControllerPlaybackSource()
        => ReadFlashbackPlaybackControllerSource();

    internal static Task FlashbackDecoder_DiscardedAudioFramesAreUnreffed()
    {
        var sourceText = ReadFlashbackDecoderSource();

        var audioDecodeBlock = ExtractTextBetween(
            sourceText,
            "private void DecodeAndDeliverAudioPacket",
            "private DecodedVideoFrame ConvertAndOutputVideoFrame()");
        AssertContains(audioDecodeBlock, "if (callback == null)\n            {\n                ffmpeg.av_frame_unref(_audioFrame);\n                continue; // Codec advanced, but no delivery during seek/scrub\n            }");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_MjpegPlaybackUsesSingleThreadLowLatencyDecode()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "if (codecPar->codec_id == AVCodecID.AV_CODEC_ID_MJPEG)\n        {\n            _videoCodecCtx->thread_count = 1;\n        }");
        AssertContains(sourceText, "_videoCodecCtx->thread_count = Math.Clamp(Environment.ProcessorCount / 2, 1, 12);");
        AssertOccursBefore(sourceText, "_videoCodecCtx->thread_type = ffmpeg.FF_THREAD_FRAME | ffmpeg.FF_THREAD_SLICE;", "ThrowIfError(\n            ffmpeg.avcodec_open2(_videoCodecCtx, codec, null),");
        AssertOccursBefore(
            sourceText,
            "if (codecPar->codec_id == AVCodecID.AV_CODEC_ID_MJPEG)",
            "ThrowIfError(\n            ffmpeg.avcodec_open2(_videoCodecCtx, codec, null),");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_PtsConversionRejectsInvalidTimestamps()
    {
        var sourceText = ReadFlashbackDecoderSource();
        var timestampFragmentPath = Path.Combine(
            GetRepoRoot(),
            "Sussudio",
            "Services",
            "Flashback",
            "FlashbackDecoder.Timestamps.cs");

        AssertEqual(false, File.Exists(timestampFragmentPath), "Flashback decoder timestamp helpers stay folded into caller owners");
        AssertContains(sourceText, "var pts = DecodePtsToTimeSpan(ResolveBestEffortFrameTimestamp(_videoFrame), _videoTimeBase);");
        AssertContains(sourceText, "var pts = DecodePtsToTimeSpan(ResolveBestEffortFrameTimestamp(_audioFrame), _audioTimeBase);");
        AssertContains(sourceText, "var streamTimestamp = ToStreamTimestamp(target, _videoTimeBase);");
        AssertContains(sourceText, "_formatCtx, _videoStreamIndex, streamTimestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);");
        AssertContains(sourceText, "var timestampUs = ToAvTimeBaseTimestamp(target);");
        AssertContains(sourceText, "_formatCtx, -1, timestampUs, ffmpeg.AVSEEK_FLAG_BACKWARD);");
        AssertContains(sourceText, "FLASHBACK_DECODER_SEEK_FALLBACK_OK");
        AssertContains(sourceText, "private static TimeSpan DecodePtsToTimeSpan(long pts, AVRational timeBase)");
        AssertContains(sourceText, "private static long ResolveBestEffortFrameTimestamp(AVFrame* frame)");
        AssertContains(sourceText, "frame->best_effort_timestamp != ffmpeg.AV_NOPTS_VALUE");
        AssertContains(sourceText, "return frame->best_effort_timestamp;");
        AssertContains(sourceText, "return frame->pts;");
        AssertContains(sourceText, "if (pts == ffmpeg.AV_NOPTS_VALUE || timeBase.num <= 0 || timeBase.den <= 0)");
        AssertContains(sourceText, "if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)");
        AssertContains(sourceText, "private static long ToAvTimeBaseTimestamp(TimeSpan value)");
        AssertContains(sourceText, "private static long ToStreamTimestamp(TimeSpan value, AVRational timeBase)");
        AssertContains(sourceText, "if (value <= TimeSpan.Zero || timeBase.num <= 0 || timeBase.den <= 0)");
        AssertContains(sourceText, "var timestamp = value.TotalSeconds * timeBase.den / timeBase.num;");
        AssertContains(sourceText, "if (!double.IsFinite(microseconds) || microseconds >= long.MaxValue)\n        {\n            return long.MaxValue;\n        }");
        AssertContains(sourceText, "if (!double.IsFinite(timestamp) || timestamp >= long.MaxValue)\n        {\n            return long.MaxValue;\n        }");
        AssertContains(sourceText, "private bool _suppressRecoverableSeekLogsForNextVideoFrame;");
        AssertContains(sourceText, "_suppressRecoverableSeekLogsForNextVideoFrame = true;");
        AssertContains(sourceText, "using var recoverableSeekLogScope = BeginRecoverableSeekLogSuppressionIfNeeded();");
        AssertContains(sourceText, "private IDisposable? BeginRecoverableSeekLogSuppressionIfNeeded()");
        AssertContains(sourceText, "return LibAvEncoder.SuppressRecoverableSeekFfmpegLogs();");
        AssertContains(sourceText, "_suppressRecoverableSeekLogsForNextVideoFrame = false;");
        AssertDoesNotContain(sourceText, "(long)(target.TotalSeconds * ffmpeg.AV_TIME_BASE)");
        AssertDoesNotContain(sourceText, "var seconds = (double)_videoFrame->pts * _videoTimeBase.num / _videoTimeBase.den;\n            pts = TimeSpan.FromSeconds(seconds);");
        AssertDoesNotContain(sourceText, "var seconds = (double)_audioFrame->pts * _audioTimeBase.num / _audioTimeBase.den;\n            pts = TimeSpan.FromSeconds(seconds);");
        AssertDoesNotContain(sourceText, "DecodePtsToTimeSpan(_videoFrame->pts, _videoTimeBase)");
        AssertDoesNotContain(sourceText, "DecodePtsToTimeSpan(_audioFrame->pts, _audioTimeBase)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_InputStreamsAndFrameSizesAreBounded()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "private const int MaxSupportedInputStreams = 64;");
        AssertContains(sourceText, "private const int MaxDecodedVideoDimension = 8192;");
        AssertContains(sourceText, "private const int MaxDecodedVideoFrameBytes = 512 * 1024 * 1024;");
        AssertContains(sourceText, "private const int MaxMpegTsProbeSizeBytes = 20 * 1024 * 1024;");
        AssertContains(sourceText, "private const int MaxMpegTsAnalyzeDurationUs = 5 * 1000 * 1000;");
        AssertContains(sourceText, "_formatCtx->probesize = MaxMpegTsProbeSizeBytes;");
        AssertContains(sourceText, "_formatCtx->max_analyze_duration = MaxMpegTsAnalyzeDurationUs;");
        AssertContains(sourceText, "codecId is AVCodecID.AV_CODEC_ID_HEVC or AVCodecID.AV_CODEC_ID_H264");
        AssertContains(sourceText, "ffmpeg.av_dict_set(&probeOptions[i], \"skip_frame\", \"all\", 0)");
        AssertContains(sourceText, "ffmpeg.av_dict_free(&probeOptions[i]);");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_formatCtx, out var streamCount, out var streamCountFailure))");
        AssertContains(sourceText, "if (!IsValidStreamIndex(_videoStreamIndex, streamCount))");
        AssertContains(sourceText, "if (_audioStreamIndex >= 0 && !IsValidStreamIndex(_audioStreamIndex, streamCount))");
        AssertContains(sourceText, "FLASHBACK_DECODER_AUDIO_WARN reason=invalid_stream_index");
        AssertContains(sourceText, "ValidateVideoDimensions(_videoWidth, _videoHeight);");
        AssertContains(sourceText, "private static void ValidateVideoDimensions(int width, int height)");
        AssertContains(sourceText, "width > MaxDecodedVideoDimension");
        AssertContains(sourceText, "height > MaxDecodedVideoDimension");
        AssertContains(sourceText, "(width & 1) != 0");
        AssertContains(sourceText, "var pixels = (long)width * height;");
        AssertContains(sourceText, "if (bytes <= 0 || bytes > MaxDecodedVideoFrameBytes || bytes > int.MaxValue)");
        AssertDoesNotContain(sourceText, "return width * height * 2 + width * (height / 2) * 2;");
        AssertDoesNotContain(sourceText, "return width * height + width * (height / 2);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_AudioOutputBuffersAreBounded()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "private const int MaxDecodedAudioFrameBytes = 16 * 1024 * 1024;");
        AssertContains(sourceText, "byte[]? result = null;\n        var returnResultToPool = true;");
        AssertContains(sourceText, "if (inputSamples <= 0)\n            {\n                return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };\n            }");
        AssertContains(sourceText, "maxOutputSamples = ToBoundedAudioSampleCount((long)inputSamples * 2);");
        AssertContains(sourceText, "if (!TryCalculateAudioBufferBytes(maxOutputSamples, out var outputBytesNeeded))");
        AssertContains(sourceText, "FLASHBACK_DECODER_AUDIO_WARN reason=invalid_output_size");
        AssertContains(sourceText, "if (!TryCalculateAudioBufferBytes(outputSamplesProduced, out var validBytes) || validBytes > result.Length)");
        AssertContains(sourceText, "FLASHBACK_DECODER_AUDIO_WARN reason=invalid_converted_size");
        AssertContains(sourceText, "returnResultToPool = false;");
        AssertContains(sourceText, "finally\n        {\n            ffmpeg.av_frame_unref(_audioFrame);\n            if (returnResultToPool && result is { Length: > 0 })");
        AssertContains(sourceText, "ArrayPool<byte>.Shared.Return(result);");
        AssertContains(sourceText, "private static int ToBoundedAudioSampleCount(long sampleCount)");
        AssertContains(sourceText, "private static bool TryCalculateAudioBufferBytes(int sampleCount, out int bytes)");
        AssertContains(sourceText, "var calculated = (long)sampleCount * OutputAudioChannels * sizeof(float);");
        AssertDoesNotContain(sourceText, "var outputBytesNeeded = maxOutputSamples * OutputAudioChannels * sizeof(float);");
        AssertDoesNotContain(sourceText, "var validBytes = outputSamplesProduced * OutputAudioChannels * sizeof(float);");

        return Task.CompletedTask;
    }


    internal static Task FlashbackDecoder_SoftwareFramePlanesAreValidated()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "if (actualFormat != AVPixelFormat.AV_PIX_FMT_NONE && actualFormat != _decodedPixelFormat)");
        AssertContains(sourceText, "if (!TryValidateSoftwareVideoFrame(_videoFrame, _decodedPixelFormat, _videoWidth, _videoHeight, _isHdr, out var frameFailure))");
        AssertContains(sourceText, "FLASHBACK_DECODER_VIDEO_WARN reason=invalid_software_frame");
        AssertContains(sourceText, "ffmpeg.av_frame_unref(_videoFrame);\n            return default;");
        var softwareOutputBlock = ExtractTextBetween(
            sourceText,
            "var outputSize = CalculateFrameBufferSize(_videoWidth, _videoHeight, _isHdr);",
            "    private void CopyFramePlanesToBuffer");
        AssertContains(softwareOutputBlock, "finally\n        {\n            ffmpeg.av_frame_unref(_videoFrame);\n        }");
        AssertOccursBefore(softwareOutputBlock, "CopyFramePlanesToBuffer((byte*)dataPtr, outputSize);", "finally\n        {\n            ffmpeg.av_frame_unref(_videoFrame);\n        }");
        AssertContains(sourceText, "private static bool TryValidateSoftwareVideoFrame(");
        AssertContains(sourceText, "width_mismatch frame={frame->width} expected={width}");
        AssertContains(sourceText, "height_mismatch frame={frame->height} expected={height}");
        AssertContains(sourceText, "private static bool IsConvertibleSdrPlanarFormat(AVPixelFormat format)");
        AssertContains(sourceText, "format == AVPixelFormat.AV_PIX_FMT_YUV420P ||\n           format == AVPixelFormat.AV_PIX_FMT_YUVJ420P");
        AssertContains(sourceText, "if (!isHdr && IsConvertibleSdrPlanarFormat(format))");
        AssertContains(sourceText, "format == AVPixelFormat.AV_PIX_FMT_YUV420P10LE");
        AssertContains(sourceText, "failure = $\"unsupported_format:{format}\";");
        AssertContains(sourceText, "IsConvertibleSdrPlanarFormat(_decodedPixelFormat) ||");
        AssertContains(sourceText, "private static bool TryValidatePlane(AVFrame* frame, int planeIndex, int minLineSize, out string failure)");
        AssertContains(sourceText, "var plane = (uint)planeIndex;");
        AssertContains(sourceText, "failure = $\"plane_{planeIndex}_null\";");
        AssertContains(sourceText, "failure = $\"plane_{planeIndex}_linesize:{frame->linesize[plane]}<{minLineSize}\";");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_D3D11FramesAreValidated()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "if (!TryValidateD3D11VideoFrame(clonedFrame, _videoWidth, _videoHeight, out var d3dFrameFailure))");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_DECODER_VIDEO_WARN reason=invalid_d3d11_frame detail='{d3dFrameFailure}' w={_videoWidth} h={_videoHeight}\");\n                ffmpeg.av_frame_free(&clonedFrame);\n                return default;");
        AssertContains(sourceText, "private static bool TryValidateD3D11VideoFrame(AVFrame* frame, int width, int height, out string failure)");
        AssertContains(sourceText, "failure = \"texture_null\";");
        AssertContains(sourceText, "failure = $\"subresource_out_of_range:{subresource}\";");
        AssertOccursBefore(sourceText, "TryValidateD3D11VideoFrame(clonedFrame, _videoWidth, _videoHeight", "var texturePtr = (IntPtr)clonedFrame->data[0];");

        return Task.CompletedTask;
    }


    internal static Task FlashbackDecoder_RejectsInitializeAfterDispose()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        using var decoder = (IDisposable)Activator.CreateInstance(decoderType)!;
        var initialize = decoderType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackDecoder.Initialize not found.");

        decoder.Dispose();

        var invocation = Assert.Throws<TargetInvocationException>(
            () => initialize.Invoke(decoder, new object[] { IntPtr.Zero, IntPtr.Zero }));
        Assert.IsType<ObjectDisposedException>(invocation.InnerException);

        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var d3d11Text = rootText;
        AssertContains(rootText, "public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)");
        var initializeBlock = ExtractTextBetween(
            d3d11Text,
            "public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)",
            "    private static AVPixelFormat GetFormatD3D11");
        AssertContains(initializeBlock, "ThrowIfDisposed();");
        AssertOccursBefore(initializeBlock, "ThrowIfDisposed();", "if (_initialized)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_ClearsAudioCallbackOnDispose()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        using var decoder = (IDisposable)Activator.CreateInstance(decoderType)!;
        var callbackProperty = decoderType.GetProperty("AudioChunkCallback", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackDecoder.AudioChunkCallback not found.");

        var callbackType = callbackProperty.PropertyType;
        var callbackParameter = Expression.Parameter(callbackType.GetGenericArguments()[0], "chunk");
        var callback = Expression.Lambda(callbackType, Expression.Empty(), callbackParameter).Compile();
        callbackProperty.SetValue(decoder, callback);

        decoder.Dispose();

        AssertEqual(null, callbackProperty.GetValue(decoder), "Disposed decoder clears audio callback");

        var sourceText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var disposeBlock = ExtractTextBetween(
            sourceText,
            "public void Dispose()",
            "// Free persistent D3D11VA device context");
        AssertContains(disposeBlock, "AudioChunkCallback = null;");
        AssertOccursBefore(disposeBlock, "AudioChunkCallback = null;", "CloseFileCore();");

        return Task.CompletedTask;
    }

    internal static Task FlashbackSuppressedExceptionsUseAppLogs()
    {
        var decoderText = ReadFlashbackDecoderSource();
        var d3d11Text = decoderText;
        var d3d11DiscoveryText = d3d11Text;

        var openFileBlock = ExtractTextBetween(
            decoderText,
            "public void OpenFile(string filePath)",
            "    /// <summary>\n    /// Closes the currently open file");
        AssertContains(openFileBlock, "FLASHBACK_DECODER_OPEN_WARN");
        AssertContains(openFileBlock, "CloseFileCore();\n            throw;");
        AssertContains(decoderText, "var closedPath = _currentFilePath;\n        CloseFileCore();\n        Logger.Log($\"FLASHBACK_DECODER_CLOSE path='{closedPath}'\");");
        AssertContains(decoderText, "_currentPosition = TimeSpan.Zero;\n        _currentFilePath = null;\n        _needsConvert = false;");
        AssertDoesNotContain(openFileBlock, "System.Diagnostics.Trace.TraceWarning");
        AssertContains(decoderText, "FLASHBACK_DECODER_INIT d3d11va=false reason=exception type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(d3d11Text, "var codec = FindD3D11VADecoder(codecPar->codec_id, out var codecName);");
        AssertContains(d3d11Text, "FLASHBACK_DECODER_D3D11VA_SKIP reason=no_d3d11_device_ctx_decoder");
        AssertContains(d3d11Text, "FLASHBACK_DECODER_D3D11VA_SKIP reason=exception type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(d3d11Text, "private static string DescribeHardwareConfigs");
        AssertContains(d3d11DiscoveryText, "private static AVCodec* FindD3D11VADecoder(AVCodecID codecId, out string codecName)");
        AssertContains(d3d11DiscoveryText, "ffmpeg.avcodec_find_decoder_by_name(preferredName)");
        AssertContains(d3d11DiscoveryText, "AVCodecID.AV_CODEC_ID_AV1 => \"av1\"");
        AssertContains(d3d11DiscoveryText, "FLASHBACK_DECODER_D3D11VA_SELECT source=preferred codec={codecName}");
        AssertContains(d3d11DiscoveryText, "FLASHBACK_DECODER_D3D11VA_CANDIDATE source={source} codec={codecName} configs=[{hardwareConfigSummary}] d3d11_device_ctx={hasD3D11DeviceConfig}");
        AssertContains(d3d11DiscoveryText, "private static string DescribeHardwareConfigs(AVCodec* codec, out bool hasD3D11DeviceConfig)");
        AssertContains(d3d11DiscoveryText, "ffmpeg.avcodec_get_hw_config(codec, i)");
        AssertContains(d3d11DiscoveryText, "pixelFormat == AVPixelFormat.AV_PIX_FMT_D3D11");
        AssertContains(d3d11DiscoveryText, "deviceType == AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA");
        AssertContains(d3d11DiscoveryText, "AvCodecHwConfigMethodHwDeviceCtx");
        AssertContains(d3d11DiscoveryText, "private static string FormatHardwareConfigMethods(int methods)");
        AssertContains(d3d11DiscoveryText, "private static string GetPixelFormatName(AVPixelFormat pixelFormat)");
        AssertContains(d3d11DiscoveryText, "private static string GetHardwareDeviceName(AVHWDeviceType deviceType)");

        return Task.CompletedTask;
    }



    // FlashbackDecoder: CalculateFrameBufferSize

    internal static Task FlashbackDecoder_CalculateFrameBufferSize_Nv12()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        var method = decoderType.GetMethod("CalculateFrameBufferSize",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CalculateFrameBufferSize not found.");

        // NV12: width * height + width * (height / 2)
        var size1080 = (int)method.Invoke(null, new object[] { 1920, 1080, false })!;
        AssertEqual(1920 * 1080 + 1920 * (1080 / 2), size1080, "NV12 1080p buffer size");

        var size720 = (int)method.Invoke(null, new object[] { 1280, 720, false })!;
        AssertEqual(1280 * 720 + 1280 * (720 / 2), size720, "NV12 720p buffer size");

        var size4k = (int)method.Invoke(null, new object[] { 3840, 2160, false })!;
        AssertEqual(3840 * 2160 + 3840 * (2160 / 2), size4k, "NV12 4K buffer size");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_CalculateFrameBufferSize_P010()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        var method = decoderType.GetMethod("CalculateFrameBufferSize",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CalculateFrameBufferSize not found.");

        // P010: width * height * 2 + width * (height / 2) * 2
        var size1080 = (int)method.Invoke(null, new object[] { 1920, 1080, true })!;
        AssertEqual(1920 * 1080 * 2 + 1920 * (1080 / 2) * 2, size1080, "P010 1080p buffer size");

        var size4k = (int)method.Invoke(null, new object[] { 3840, 2160, true })!;
        AssertEqual(3840 * 2160 * 2 + 3840 * (2160 / 2) * 2, size4k, "P010 4K buffer size");

        // P010 should be exactly 2x NV12
        var nv12Size = (int)method.Invoke(null, new object[] { 1920, 1080, false })!;
        AssertEqual(nv12Size * 2, size1080, "P010 is 2x NV12");

        return Task.CompletedTask;
    }

    // FlashbackDecoder: state guard properties








    internal static Task FlashbackDecoder_DefaultState_IsNotOpenAndNotInitialized()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        var decoder = Activator.CreateInstance(decoderType)!;

        var isOpenProp = decoderType.GetProperty("IsOpen",
            BindingFlags.Public | BindingFlags.Instance);
        AssertNotNull(isOpenProp, "FlashbackDecoder.IsOpen");
        AssertEqual(false, (bool)isOpenProp!.GetValue(decoder)!, "IsOpen default");

        return Task.CompletedTask;
    }

    // FlashbackDecoder: Dispose is safe when not initialized

    internal static Task FlashbackDecoder_DisposeBeforeInitialize_DoesNotThrow()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        var decoder = Activator.CreateInstance(decoderType)!;

        // Dispose via IDisposable
        if (decoder is IDisposable disposable)
        {
            disposable.Dispose();
        }
        else
        {
            var disposeMethod = decoderType.GetMethod("Dispose",
                BindingFlags.Public | BindingFlags.Instance);
            disposeMethod?.Invoke(decoder, null);
        }

        return Task.CompletedTask;
    }
}
