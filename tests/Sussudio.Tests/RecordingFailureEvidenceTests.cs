using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sussudio.Tests;

public sealed class RecordingFailureEvidenceTests
{
    [Theory]
    [InlineData(true, 100, 5, 0, false)]
    [InlineData(true, 99, 5, 0, false)]
    [InlineData(true, 101, 6, 0, false)]
    [InlineData(true, 101, 5, 1, false)]
    [InlineData(true, 101, 5, 0, true)]
    [InlineData(false, 100, 5, 0, true)]
    public void MicrophoneIntegrityUsesRecordingIntervalAndPreservesEvidence(
        bool requested, long samples, long drops, long discontinuities, bool succeeds)
    {
        var service = CreatePolicyOwner();
        SetField(service, "_recordingMicrophoneSamplesBaseline", 100L);
        SetField(service, "_recordingMicrophoneDropsBaseline", 5L);
        var saved = VerifiedResult();
        var result = Invoke(service, "FoldRequestedMicrophoneIntegrityIntoFinalizeResult",
            saved, requested, samples, drops, discontinuities);

        AssertFold(saved, result, succeeds, "recording-microphone-integrity-failed");
        var failed = Invoke(saved, "AsFailure", "Earlier failure", "earlier-failure");
        Assert.Same(failed, Invoke(service, "FoldRequestedMicrophoneIntegrityIntoFinalizeResult",
            failed, requested, samples, drops, discontinuities));
    }

    [Theory]
    [InlineData(true, 1, 0, 0, 0, false)]
    [InlineData(true, 0, 480, 0, 0, false)]
    [InlineData(true, 1, 480, 1, 0, false)]
    [InlineData(true, 1, 480, 0, 1, false)]
    [InlineData(true, 1, 480, 0, 0, true)]
    [InlineData(false, 0, 0, 1, 1, true)]
    public void ProgramAudioIntegrityRequiresDeliveredSamplesWithoutLoss(
        bool enabled, long frames, long samples, long drops, long discontinuities, bool succeeds)
    {
        var service = CreatePolicyOwner();
        var counterType = service.GetType().GetNestedType("RecordingAudioIntegrityCounterSnapshot", BindingFlags.NonPublic)!;
        var counters = Activator.CreateInstance(counterType, new object?[]
        {
            enabled, true, frames, frames, samples, drops, discontinuities, 0L, 0L,
            null, null, null, null
        })!;
        var saved = VerifiedResult();
        var result = Invoke(service, "FoldRequestedProgramAudioIntegrityIntoFinalizeResult", saved, counters);

        AssertFold(saved, result, succeeds, "recording-program-audio-integrity-failed");
        var failed = Invoke(saved, "AsFailure", "Earlier failure", "earlier-failure");
        Assert.Same(failed, Invoke(service, "FoldRequestedProgramAudioIntegrityIntoFinalizeResult", failed, counters));
    }

    [Theory]
    [InlineData("program", true, false, false, false)]
    [InlineData("microphone", false, true, false, false)]
    [InlineData("unknown", true, false, false, false)]
    [InlineData("program", false, true, false, true)]
    [InlineData("microphone", true, false, false, true)]
    [InlineData("program", true, false, true, true)]
    public void CaptureFaultOnlyFailsRequestedAudioAndIsConsumedOnce(
        string source, bool audio, bool microphone, bool cancelled, bool succeeds)
    {
        var service = CreatePolicyOwner();
        var graph = service.GetType().GetField("_previewAudioGraph", PrivateInstance)!.GetValue(service)!;
        Invoke(graph, "RecordCaptureFault", source, new IOException("Device disconnected"));
        var settings = Activator.CreateInstance(TypeOf("Sussudio.Models.CaptureSettings"))!;
        Set(settings, "AudioEnabled", audio);
        Set(settings, "MicrophoneEnabled", microphone);
        var saved = VerifiedResult();
        var result = Invoke(service, "FoldRecordingAudioFaultIntoFinalizeResult", saved,
            cancelled ? new OperationCanceledException() : null, settings);

        AssertFold(saved, result, succeeds, "recording-audio-capture-failed");
        Assert.Same(saved, Invoke(service, "FoldRecordingAudioFaultIntoFinalizeResult", saved, null, settings));

        Invoke(graph, "RecordCaptureFault", source, new IOException("Later fault"));
        var failed = Invoke(saved, "AsFailure", "Earlier failure", "earlier-failure");
        Assert.Same(failed, Invoke(service, "FoldRecordingAudioFaultIntoFinalizeResult", failed, null, settings));
    }

    [Fact]
    public void RuntimeFailureOverridesSavedOutcomeWithoutLosingVerifiedTrackEvidence()
    {
        var service = CreatePolicyOwner();
        var saved = VerifiedResult();
        Assert.Same(saved, Invoke(service, "FoldRecordedRuntimeFailureIntoFinalizeResult", saved));
        Invoke(service, "RecordLastRecordingFailure", new IOException("Encoder stopped"));
        var result = Invoke(service, "FoldRecordedRuntimeFailureIntoFinalizeResult", saved);

        AssertFold(saved, result, succeeds: false, "recording-runtime-failed");
        Assert.Contains("Encoder stopped", Read<string>(result, "StatusMessage"), StringComparison.Ordinal);
        Assert.Same(result, Invoke(service, "FoldRecordedRuntimeFailureIntoFinalizeResult", result));
    }

    [Fact]
    public void FailureRewritesAndEvidenceMergeKeepPrimaryRecoveryMetadata()
    {
        var saved = VerifiedResult();
        var failed = Invoke(saved, "AsFailure", "Cleanup failed", "recording-sink-dispose-failed");
        AssertFold(saved, failed, succeeds: false, "recording-sink-dispose-failed");
        var rewritten = Invoke(failed, "AsFailureWithArtifacts", new[] { "replacement.ts" }, "replacement.ts");
        Assert.Equal(new[] { "replacement.ts" }, Read<IReadOnlyList<string>>(rewritten, "PreservedArtifacts"));
        Assert.Equal("replacement.ts", Read<string>(rewritten, "RecoveryPath"));
        Assert.Equal(Read<IReadOnlyList<string>>(failed, "RequestedTracks"), Read<IReadOnlyList<string>>(rewritten, "RequestedTracks"));
        Assert.Equal(Read<IReadOnlyList<string>>(failed, "ObservedTracks"), Read<IReadOnlyList<string>>(rewritten, "ObservedTracks"));
        Assert.Equal(Read<string>(failed, "FailureCode"), Read<string>(rewritten, "FailureCode"));
        Assert.False(Read<bool>(rewritten, "Succeeded"));
        Assert.Equal("Failed", Read<object>(rewritten, "Outcome").ToString());

        var supplementary = Invoke(saved, "WithTrackEvidence",
            new[] { "VIDEO", "microphone" }, new[] { "VIDEO", "microphone" });
        var merged = TypeOf("Sussudio.Services.Capture.CaptureService")
            .GetMethod("MergeFinalizeTrackEvidence", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new[] { failed, supplementary })!;
        AssertPreservedMetadata(failed, merged, compareTracks: false);
        Assert.Equal(new[] { "video", "device_audio", "microphone" }, Read<IReadOnlyList<string>>(merged, "RequestedTracks"));
        Assert.Equal(new[] { "video", "device_audio", "microphone" }, Read<IReadOnlyList<string>>(merged, "ObservedTracks"));
        Assert.Equal("recording-sink-dispose-failed", Read<string>(merged, "FailureCode"));
        Assert.False(Read<bool>(merged, "Succeeded"));
    }

    private static void AssertFold(object saved, object result, bool succeeds, string failureCode)
    {
        if (succeeds)
        {
            Assert.Same(saved, result);
            return;
        }

        Assert.False(Read<bool>(result, "Succeeded"));
        Assert.Equal("Failed", Read<object>(result, "Outcome").ToString());
        Assert.Equal(failureCode, Read<string>(result, "FailureCode"));
        Assert.False(string.IsNullOrWhiteSpace(Read<string>(result, "StatusMessage")));
        AssertPreservedMetadata(saved, result);
    }

    private static void AssertPreservedMetadata(object original, object result, bool compareTracks = true)
    {
        Assert.Equal(Read<string>(original, "OutputPath"), Read<string>(result, "OutputPath"));
        Assert.Equal(Read<string>(original, "RecoveryPath"), Read<string>(result, "RecoveryPath"));
        Assert.Equal(Read<bool>(original, "VerificationCompleted"), Read<bool>(result, "VerificationCompleted"));
        Assert.Equal(Read<bool>(original, "CleanupPending"), Read<bool>(result, "CleanupPending"));
        Assert.Equal(Read<long>(original, "FinalizationElapsedMs"), Read<long>(result, "FinalizationElapsedMs"));
        Assert.Equal(Read<IReadOnlyList<string>>(original, "PreservedArtifacts"), Read<IReadOnlyList<string>>(result, "PreservedArtifacts"));
        if (compareTracks)
        {
            Assert.Equal(Read<IReadOnlyList<string>>(original, "RequestedTracks"), Read<IReadOnlyList<string>>(result, "RequestedTracks"));
            Assert.Equal(Read<IReadOnlyList<string>>(original, "ObservedTracks"), Read<IReadOnlyList<string>>(result, "ObservedTracks"));
        }
    }

    private static object VerifiedResult()
    {
        var type = TypeOf("Sussudio.Services.Contracts.FinalizeResult");
        var saved = type.GetMethod("Success", new[] { typeof(string), typeof(string), typeof(bool), typeof(long) })!
            .Invoke(null, new object[] { "saved.mp4", "Verified", true, 1234L })!;
        Set(saved, "CleanupPending", true);
        Set(saved, "RecoveryPath", "recovery.ts");
        Set(saved, "PreservedArtifacts", new[] { "saved.mp4", "recovery.ts" });
        return Invoke(saved, "WithTrackEvidence",
            new[] { "video", "device_audio", "microphone" }, new[] { "video", "device_audio" });
    }

    private static object CreatePolicyOwner()
    {
        // These policy folds need only counters, the fault inbox, and its telemetry lock.
        // Avoid constructing capture devices or starting a capture session in unit tests.
        var service = RuntimeHelpers.GetUninitializedObject(TypeOf("Sussudio.Services.Capture.CaptureService"));
        GC.SuppressFinalize(service);
        SetField(service, "_recordingFailureTelemetryLock", new object());
        SetField(service, "_previewAudioGraph", Activator.CreateInstance(TypeOf("Sussudio.Services.Capture.PreviewAudioGraphResources"))!);
        return service;
    }

    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private static Type TypeOf(string name) => SussudioAssembly.Load().GetType(name, throwOnError: true)!;
    private static object Invoke(object instance, string name, params object?[] arguments)
        => instance.GetType().GetMethod(name, PrivateInstance | BindingFlags.Public)!.Invoke(instance, arguments)!;
    private static T Read<T>(object instance, string name) => (T)instance.GetType().GetProperty(name)!.GetValue(instance)!;
    private static void Set(object instance, string name, object value) => instance.GetType().GetProperty(name)!.SetValue(instance, value);
    private static void SetField(object instance, string name, object value) => instance.GetType().GetField(name, PrivateInstance)!.SetValue(instance, value);
}
