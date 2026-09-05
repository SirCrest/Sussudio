using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

[Collection(RecoveryEnvironmentCollection.Name)]
public sealed class RecordingFinalizationTruthTests
{
    public RecordingFinalizationTruthTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public void LifecycleAndOutcomeContracts_AreTypedAndUnambiguous()
    {
        var assembly = SussudioAssembly.Load();
        var lifecycle = assembly.GetType(
            "Sussudio.Services.Contracts.RecordingLifecyclePhase",
            throwOnError: true)!;
        var outcome = assembly.GetType(
            "Sussudio.Services.Contracts.RecordingFinalizeOutcome",
            throwOnError: true)!;

        Assert.Equal(new[] { "Idle", "Recording", "Finalizing" }, Enum.GetNames(lifecycle));
        Assert.Equal(new[] { "None", "Saved", "Failed" }, Enum.GetNames(outcome));
    }

    [Fact]
    public void FinalizeResultFactories_CannotLabelFailureAsSaved()
    {
        var resultType = SussudioAssembly.Load().GetType(
            "Sussudio.Services.Contracts.FinalizeResult",
            throwOnError: true)!;
        var success = resultType.GetMethod(
            "Success",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string), typeof(bool), typeof(long) },
            modifiers: null)!;
        var failure = resultType.GetMethod(
            "Failure",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[]
            {
                typeof(string), typeof(string), typeof(IEnumerable<string>), typeof(string),
                typeof(bool), typeof(string), typeof(bool), typeof(long)
            },
            modifiers: null)!;

        var saved = success.Invoke(null, new object?[] { "saved.mp4", "Recording saved", true, 17L })!;
        Assert.True(Read<bool>(saved, "Succeeded"));
        Assert.Equal("Saved", Read<object>(saved, "Outcome").ToString());
        Assert.True(Read<bool>(saved, "VerificationCompleted"));
        Assert.False(Read<bool>(saved, "CleanupPending"));
        var withTrackEvidence = resultType.GetMethod(
            "WithTrackEvidence",
            BindingFlags.Public | BindingFlags.Instance)!;
        saved = withTrackEvidence.Invoke(
            saved,
            new object?[]
            {
                new[] { "video", "device_audio", "microphone" },
                new[] { "video", "device_audio", "microphone" }
            })!;
        Assert.Equal(
            new[] { "video", "device_audio", "microphone" },
            Read<IReadOnlyList<string>>(saved, "RequestedTracks"));
        Assert.Equal(
            new[] { "video", "device_audio", "microphone" },
            Read<IReadOnlyList<string>>(saved, "ObservedTracks"));

        var failed = failure.Invoke(null, new object?[]
        {
            "partial.mp4",
            "Recording failed",
            new[] { "partial.mp4", "partial.mp4" },
            "recording-finalization-timeout",
            true,
            "partial.mp4.recording-finalization-unresolved.txt",
            false,
            120_000L
        })!;
        Assert.False(Read<bool>(failed, "Succeeded"));
        Assert.Equal("Failed", Read<object>(failed, "Outcome").ToString());
        Assert.True(Read<bool>(failed, "CleanupPending"));
        Assert.Equal("recording-finalization-timeout", Read<string>(failed, "FailureCode"));
        Assert.Equal(120_000L, Read<long>(failed, "FinalizationElapsedMs"));
        Assert.Single(Read<IReadOnlyList<string>>(failed, "PreservedArtifacts"));
    }

    [Fact]
    public void FinalizationOwner_ClosesThenReopensBeforeVerified()
    {
        var sink = RuntimeContractSource.ReadRepoFile(
            "Sussudio/Services/Recording/LibAvRecordingSink.cs");
        var loop = Slice(sink, "private void EncodingLoop", "private void CompleteWriter");

        AssertInOrder(
            loop,
            "_encoder.FlushAndClose();",
            "_structureVerifier.Verify(",
            "_structureVerificationCompleted = true;");
        Assert.Contains("FinalizationNoProgressNotificationMs = 30_000", sink, StringComparison.Ordinal);
        Assert.Contains("FinalizationAbsoluteTimeoutMs = 120_000", sink, StringComparison.Ordinal);
        Assert.Contains("cleanupPending: true", sink, StringComparison.Ordinal);
        Assert.Contains("_finalizationWaitTimedOut", sink, StringComparison.Ordinal);
        Assert.Contains("reason=stop_timeout_already_exhausted", sink, StringComparison.Ordinal);
        Assert.Contains("CleanupCompletionTask", sink, StringComparison.Ordinal);
        Assert.Contains("_cleanupCompletion.TrySetResult(true)", sink, StringComparison.Ordinal);
        Assert.Contains("progressDeadline", sink, StringComparison.Ordinal);
        Assert.Contains("LIBAV_SINK_FINALIZE_NO_PROGRESS_TIMEOUT", sink, StringComparison.Ordinal);

        var verifier = RuntimeContractSource.ReadRepoFile(
            "Sussudio/Services/Recording/Verification/InProcessRecordingStructureVerifier.cs");
        AssertInOrder(
            verifier,
            "avformat_open_input",
            "avformat_find_stream_info",
            "av_read_frame",
            "avformat_close_input");
        Assert.Contains("recording-stream-topology-mismatch", verifier, StringComparison.Ordinal);
        Assert.Contains("recording-required-stream-has-no-packets", verifier, StringComparison.Ordinal);
        Assert.Contains("recording-video-duration-invalid", verifier, StringComparison.Ordinal);
        Assert.Contains("recording-video-duration-short", verifier, StringComparison.Ordinal);
        Assert.Contains("recording-audio-duration-invalid", verifier, StringComparison.Ordinal);
        Assert.Contains("recording-audio-duration-mismatch", verifier, StringComparison.Ordinal);
        Assert.Contains("MaxVideoDurationShortfallSeconds = 2.0", verifier, StringComparison.Ordinal);
        Assert.Contains("Math.Min(", verifier, StringComparison.Ordinal);
        Assert.Contains("BuildRequestedTracks(context)", verifier, StringComparison.Ordinal);
        Assert.Contains("ResolveObservedAudioTrackName", verifier, StringComparison.Ordinal);

        var sourceReader = RuntimeContractSource.ReadRepoFile(
            "Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs");
        Assert.Contains("var readSampleSucceeded = false;", sourceReader, StringComparison.Ordinal);
        AssertInOrder(
            sourceReader,
            "MfInteropHelpers.ThrowIfFailed(hr, \"IMFSourceReader.ReadSample\");",
            "readSampleSucceeded = true;");
        Assert.Contains(
            "if (!readSampleSucceeded || Volatile.Read(ref _strictD3DOutputRequired))",
            sourceReader,
            StringComparison.Ordinal);
    }

    [Fact]
    public void NativeFinalizeErrorsAndRequestedMicrophoneLoss_AreFatal()
    {
        var encoder = RuntimeContractSource.ReadRepoFile(
            "Sussudio/Services/Recording/LibAvEncoder.cs");
        Assert.Contains("RunFinalizationStep(\"video_flush\"", encoder, StringComparison.Ordinal);
        Assert.Contains("RunFinalizationStep(\"trailer_and_close\"", encoder, StringComparison.Ordinal);
        Assert.Contains("throw firstFailure;", encoder, StringComparison.Ordinal);
        Assert.Contains("nativeCloseFailure = new InvalidOperationException(message);", encoder, StringComparison.Ordinal);

        var previewLifecycle = RuntimeContractSource.ReadRepoFile(
            "Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs");
        Assert.Contains("requestedRecordingMicrophoneFailed", previewLifecycle, StringComparison.Ordinal);
        Assert.Contains("RecordLastRecordingFailure(fatalError);", previewLifecycle, StringComparison.Ordinal);
        Assert.Contains("BeginFatalCaptureCleanup(fatalError);", previewLifecycle, StringComparison.Ordinal);

        var recordingLifecycle = RuntimeContractSource.ReadRepoFile(
            "Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs");
        AssertInOrder(
            recordingLifecycle,
            "_recordingBackend.ThrowIfPendingLibAvDrainBlocksReentry();",
            "_currentSettings = settings;");
        Assert.Contains("ValidateRequestedMicrophone(settings);", recordingLifecycle, StringComparison.Ordinal);
        Assert.Contains("CancellationToken.None", recordingLifecycle, StringComparison.Ordinal);
        AssertInOrder(
            recordingLifecycle,
            "var microphoneStopTask = DetachLibAvRecordingAudioBeforeSinkStopAsync();",
            "StopAndDisposeLibAvSinkForFinalizeAsync(",
            "await microphoneStopTask.ConfigureAwait(false);");
        Assert.Equal(
            4,
            CountOccurrences(
                Slice(
                    recordingLifecycle,
                    "private FinalizeResult FoldRecordingAudioFaultIntoFinalizeResult",
                    "private void PublishLibAvRecordingIntegrity"),
                "result.AsFailure("));
        Assert.Contains(
            "result = MergeFinalizeTrackEvidence(result, sinkResult);",
            recordingLifecycle,
            StringComparison.Ordinal);
        Assert.Contains(
            "result = MergeFinalizeTrackEvidence(result, priorResult);",
            recordingLifecycle,
            StringComparison.Ordinal);
        Assert.Contains(
            "priorResult.VerificationCompleted || sinkResult.VerificationCompleted",
            recordingLifecycle,
            StringComparison.Ordinal);
        Assert.Contains(
            "Math.Max(priorResult.FinalizationElapsedMs, sinkResult.FinalizationElapsedMs)",
            recordingLifecycle,
            StringComparison.Ordinal);
        Assert.Contains("recording-sink-dispose-failed", recordingLifecycle, StringComparison.Ordinal);
        Assert.Contains("recording-video-capture-dispose-failed", recordingLifecycle, StringComparison.Ordinal);
        Assert.Contains("recording-program-audio-dispose-failed", recordingLifecycle, StringComparison.Ordinal);
        Assert.True(
            CountOccurrences(
                recordingLifecycle,
                ".AsFailure(") >= 7,
            "Every post-verification failure rewrite must preserve requested and observed tracks.");

        var serviceContracts = RuntimeContractSource.ReadRepoFile(
            "Sussudio/Services/Contracts/ServiceContracts.cs");
        Assert.Contains(
            "public FinalizeResult AsFailure(string statusMessage, string failureCode)",
            serviceContracts,
            StringComparison.Ordinal);
        Assert.Contains(
            ".WithTrackEvidence(RequestedTracks, ObservedTracks)",
            serviceContracts,
            StringComparison.Ordinal);

        var flashback = RuntimeContractSource.ReadRepoFile(
            "Sussudio/Services/Capture/CaptureService.Flashback.cs");
        Assert.Contains("strictRecordingStart", flashback, StringComparison.Ordinal);
        Assert.Contains("Flashback recording requested a microphone", flashback, StringComparison.Ordinal);
        Assert.Contains("FoldRecordingAudioFaultIntoFinalizeResult", flashback, StringComparison.Ordinal);
        Assert.Contains("WaitForFlashbackRecordingFinalizeAsync", flashback, StringComparison.Ordinal);
        Assert.Contains("recording-flashback-finalization-timeout", flashback, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string value, string expected)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(expected, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += expected.Length;
        }

        return count;
    }

    [Fact]
    public void AutomationSnapshot_ProjectsAdditiveRecordingTruthFields()
    {
        var assembly = SussudioAssembly.Load();
        var captureSnapshot = assembly.GetType(
            "Sussudio.Models.CaptureRuntimeSnapshot",
            throwOnError: true)!;
        var automationSnapshot = assembly.GetType(
            "Sussudio.Models.AutomationSnapshot",
            throwOnError: true)!;

        foreach (var propertyName in new[]
        {
            "RecordingLifecyclePhase",
            "RecordingFinalizeOutcome",
            "RecordingFinalizeFailureCode",
            "RecordingFinalizationVerificationCompleted",
            "RecordingFinalizationCleanupPending",
            "RecordingFinalizationElapsedMs",
            "RecordingRecoveryPath",
            "RecordingRequestedTracks",
            "RecordingObservedTracks",
            "RecordingFinalizationProgressStage"
        })
        {
            Assert.NotNull(captureSnapshot.GetProperty(propertyName));
            Assert.NotNull(automationSnapshot.GetProperty(propertyName));
        }

        var projection = RuntimeContractSource.ReadRepoFile(
            "Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs");
        Assert.Contains(
            "RecordingFinalizeOutcome = captureRuntime.RecordingFinalizeOutcome",
            projection,
            StringComparison.Ordinal);
        Assert.Contains(
            "RecordingRecoveryPath = recordingOutputFlattening.RecordingRecoveryPath",
            projection,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RecoveryMarker_RestoresLatestFailureAfterRestart()
    {
        var directory = Path.Combine(Path.GetTempPath(), "SussudioRecoveryTest_" + Guid.NewGuid().ToString("N"));
        var previousRecoveryDirectory = Environment.GetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY");
        Environment.SetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY", Path.Combine(directory, "stable"));
        Directory.CreateDirectory(directory);
        try
        {
            var outputPath = Path.Combine(directory, "failed.mp4");
            var preservedSegmentPath = Path.Combine(directory, "flashback-segment-0001.mkv");
            File.WriteAllBytes(outputPath, new byte[] { 1, 2, 3 });
            File.WriteAllBytes(preservedSegmentPath, new byte[] { 4, 5, 6 });
            var markerPath = outputPath + ".recording-finalization-unresolved.txt";
            File.WriteAllLines(markerPath, new[]
            {
                "status=unresolved",
                "utc=2026-09-03T12:00:00.0000000+00:00",
                "reason=Recording failed during trailer writing.",
                "final_output=" + outputPath,
                "video_output=",
                "audio_temp=",
                "artifact_b64=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(preservedSegmentPath)),
            });
            File.SetLastWriteTimeUtc(markerPath, new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc));

            var invalidNewerMarkerPath = Path.Combine(
                directory,
                "newer-invalid.mp4.recording-finalization-unresolved.txt");
            File.WriteAllLines(invalidNewerMarkerPath, new[] { "status=complete" });
            File.SetLastWriteTimeUtc(invalidNewerMarkerPath, new DateTime(2026, 9, 3, 12, 1, 0, DateTimeKind.Utc));

            var recoveryType = SussudioAssembly.Load().GetType(
                "Sussudio.Services.Recording.RecordingFinalizationRecoveryArtifacts",
                throwOnError: true)!;
            var restore = recoveryType.GetMethod(
                "TryLoadLatest",
                BindingFlags.NonPublic | BindingFlags.Static)!;
            var recovered = restore.Invoke(null, new object?[] { directory });

            Assert.NotNull(recovered);
            Assert.Equal(markerPath, Read<string>(recovered!, "MarkerPath"));
            Assert.Equal(outputPath, Read<string>(recovered!, "OutputPath"));
            Assert.Equal(
                "Recording failed during trailer writing.",
                Read<string>(recovered!, "Reason"));
            Assert.Contains(markerPath, Read<IReadOnlyList<string>>(recovered!, "PreservedArtifacts"));
            Assert.Contains(outputPath, Read<IReadOnlyList<string>>(recovered!, "PreservedArtifacts"));
            Assert.Contains(preservedSegmentPath, Read<IReadOnlyList<string>>(recovered!, "PreservedArtifacts"));

            var viewModel = RuntimeContractSource.ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs");
            Assert.Contains("RestoreLastRecordingFailure(OutputPath)", viewModel, StringComparison.Ordinal);
            var window = RuntimeContractSource.ReadRepoFile("Sussudio/MainWindow.xaml");
            Assert.Contains("RecordingRecoveryOpenLocationButton", window, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY", previousRecoveryDirectory);
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ActiveRecordingJournal_PromotesAbruptExitArtifactsAndRetiresAfterCompletion()
    {
        var directory = Path.Combine(Path.GetTempPath(), "SussudioActiveRecoveryTest_" + Guid.NewGuid().ToString("N"));
        var stableDirectory = Path.Combine(directory, "stable");
        var sessionDirectory = Path.Combine(directory, "flashback-session");
        var previousRecoveryDirectory = Environment.GetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY");
        Environment.SetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY", stableDirectory);
        Directory.CreateDirectory(sessionDirectory);
        try
        {
            var outputPath = Path.Combine(directory, "interrupted.mp4");
            var segmentPath = Path.Combine(sessionDirectory, "fb_interrupted_0001.ts");
            File.WriteAllBytes(segmentPath, new byte[] { 1, 2, 3, 4 });

            var recoveryType = SussudioAssembly.Load().GetType(
                "Sussudio.Services.Recording.RecordingFinalizationRecoveryArtifacts",
                throwOnError: true)!;
            var beginActive = recoveryType.GetMethod(
                "BeginActive",
                BindingFlags.NonPublic | BindingFlags.Static)!;
            var retireActive = recoveryType.GetMethod(
                "RetireActive",
                BindingFlags.NonPublic | BindingFlags.Static)!;
            var restore = recoveryType.GetMethod(
                "TryLoadLatest",
                BindingFlags.NonPublic | BindingFlags.Static)!;

            var markerPath = (string)beginActive.Invoke(
                null,
                new object?[] { outputPath, outputPath, null, new[] { sessionDirectory } })!;
            Assert.True(File.Exists(markerPath));

            var recovered = restore.Invoke(null, new object?[] { null });
            Assert.NotNull(recovered);
            Assert.Equal(outputPath, Read<string>(recovered!, "OutputPath"));
            Assert.Equal(
                "Recording was interrupted before finalization.",
                Read<string>(recovered!, "Reason"));
            Assert.Contains(segmentPath, Read<IReadOnlyList<string>>(recovered!, "PreservedArtifacts"));

            retireActive.Invoke(null, new object?[] { markerPath });
            Assert.False(File.Exists(markerPath));

            var finalizeResultType = SussudioAssembly.Load().GetType(
                "Sussudio.Services.Contracts.FinalizeResult",
                throwOnError: true)!;
            var failure = finalizeResultType.GetMethod(
                "Failure",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[]
                {
                    typeof(string), typeof(string), typeof(IEnumerable<string>), typeof(string),
                    typeof(bool), typeof(string), typeof(bool), typeof(long)
                },
                modifiers: null)!;
            var durableMarkerCheck = SussudioAssembly.Load()
                .GetType("Sussudio.Services.Capture.CaptureService", throwOnError: true)!
                .GetMethod(
                    "HasDurableUnresolvedRecoveryMarker",
                    BindingFlags.NonPublic | BindingFlags.Static)!;
            var markerlessFailure = failure.Invoke(null, new object?[]
            {
                outputPath,
                "Recording failed",
                new[] { outputPath },
                "recording-test-failure",
                false,
                outputPath,
                false,
                0L
            })!;
            Assert.False((bool)durableMarkerCheck.Invoke(null, new[] { markerlessFailure })!);

            var unresolvedMarkerPath = outputPath + ".recording-finalization-unresolved.txt";
            File.WriteAllText(unresolvedMarkerPath, "status=unresolved");
            var durableFailure = failure.Invoke(null, new object?[]
            {
                outputPath,
                "Recording failed",
                new[] { outputPath, unresolvedMarkerPath },
                "recording-test-failure",
                false,
                outputPath,
                false,
                0L
            })!;
            Assert.True((bool)durableMarkerCheck.Invoke(null, new[] { durableFailure })!);

            var lifecycle = RuntimeContractSource.ReadRepoFile(
                "Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs");
            Assert.Contains("RecordingFinalizationRecoveryArtifacts.BeginActive", lifecycle, StringComparison.Ordinal);
            Assert.Contains("RecordingFinalizationRecoveryArtifacts.RetireActive", lifecycle, StringComparison.Ordinal);
            Assert.Contains("result.Succeeded || HasDurableUnresolvedRecoveryMarker(result)", lifecycle, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY", previousRecoveryDirectory);
            Directory.Delete(directory, recursive: true);
        }
    }

    private static T Read<T>(object instance, string propertyName) =>
        (T)instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!
            .GetValue(instance)!;

    private static string Slice(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start, StringComparison.Ordinal);
        Assert.True(startIndex >= 0, $"Missing slice start: {start}");
        var endIndex = source.IndexOf(end, startIndex, StringComparison.Ordinal);
        Assert.True(endIndex > startIndex, $"Missing slice end: {end}");
        return source[startIndex..endIndex];
    }

    private static void AssertInOrder(string source, params string[] markers)
    {
        var previous = -1;
        foreach (var marker in markers)
        {
            var current = source.IndexOf(marker, previous + 1, StringComparison.Ordinal);
            Assert.True(current > previous, $"Missing or out-of-order marker: {marker}");
            previous = current;
        }
    }
}
