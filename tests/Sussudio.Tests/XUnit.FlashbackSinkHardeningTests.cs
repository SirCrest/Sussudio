using System;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackSinkHardeningTests
{
    private static string Source()
        => RuntimeContractSource.ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs");

    [Fact]
    public void RotateSegment_UnopenedEncoderEscalatesOnceAfterThreeFailures()
        => global::Program.FlashbackEncoderSink_ExerciseRotationFailures(scriptedSuccess: false);

    [Fact]
    public void RotateSegment_SuccessResetsConsecutiveFailuresAndCommitsSegment()
        => global::Program.FlashbackEncoderSink_ExerciseRotationFailures(scriptedSuccess: true);

    [Fact]
    public void ForceRotate_PreparesPathBeforeEncoderLaneFence()
    {
        var source = Source();
        var method = global::Program.ExtractDeclaredMemberCode(source, "public FlashbackForceRotateResult ForceRotateForExport");
        Assert.Contains("_bufferManager.ReserveSegmentPath()", method);
        Assert.Contains("new ForceRotateRequest(preparedPath)", method);
        Assert.True(method.IndexOf("ReserveSegmentPath", StringComparison.Ordinal) < method.IndexOf("SignalWork", StringComparison.Ordinal));
        Assert.Contains("RotateSegment(currentPts, localRequest.PreparedPath)", source);
    }

    [Fact]
    public void EndRecording_WaitsForAcceptedBoundary_NotLiveQueueEmptiness()
    {
        var source = Source();
        var method = global::Program.ExtractDeclaredMemberCode(source, "public async Task<FinalizeResult> EndRecordingAsync");
        Assert.Contains("CaptureRecordingBoundaryFence", method);
        Assert.Contains("WaitForRecordingBoundaryAsync", method);
        Assert.Contains("var endPts = recordingBoundary.EndPts", method);
        Assert.DoesNotContain("var endPts = _bufferManager.LatestPts", method);

        var capture = global::Program.ExtractDeclaredMemberCode(source, "private RecordingBoundaryFence CaptureRecordingBoundaryFence");
        Assert.Contains("lock (_videoQueueSync)", capture);
        Assert.Contains("_videoFramesEnqueued", capture);
        Assert.Contains("_audioPacketsAccepted", capture);
        Assert.Contains("_microphonePacketsAccepted", capture);
        Assert.Contains("_gpuFramesEnqueued", capture);

        var wait = global::Program.ExtractDeclaredMemberCode(source, "private async Task<bool> WaitForRecordingBoundaryAsync");
        Assert.Contains("_videoPacketsRetired) >= boundary.VideoPacketsAccepted", wait);
        Assert.Contains("_audioPacketsRetired) >= boundary.AudioPacketsAccepted", wait);
        Assert.Contains("_microphonePacketsRetired) >= boundary.MicrophonePacketsAccepted", wait);
        Assert.Contains("_gpuPacketsRetired) >= boundary.GpuPacketsAccepted", wait);
        Assert.Contains("boundary.HasResolvedVideoEndPts", wait);
        Assert.DoesNotContain("_videoQueueDepth) == 0", wait);
        Assert.DoesNotContain("_audioQueueDepth) == 0", wait);
        Assert.DoesNotContain("_microphoneQueueDepth) == 0", wait);
        Assert.DoesNotContain("_gpuQueueDepth) == 0", wait);

        var audioEnqueue = global::Program.ExtractDeclaredMemberCode(source, "private bool TryEnqueueAudioPacket");
        Assert.Contains("Interlocked.Increment(ref acceptedPackets)", audioEnqueue);
        Assert.Contains("Interlocked.Increment(ref retiredPackets)", audioEnqueue);
        Assert.Contains("Interlocked.Increment(ref _videoPacketsRetired)", source);
        Assert.Contains("Interlocked.Increment(ref _gpuPacketsRetired)", source);
        Assert.Contains("Interlocked.Increment(ref _audioPacketsRetired)", source);
        Assert.Contains("Interlocked.Increment(ref _microphonePacketsRetired)", source);

        var fence = global::Program.ExtractDeclaredMemberCode(source, "private sealed class RecordingBoundaryFence");
        Assert.Contains("CaptureAlreadyRetiredVideoPts", fence);
        Assert.Contains("ObserveVideoRetirement", fence);
        Assert.Contains("Interlocked.CompareExchange", fence);
        Assert.Contains("Math.Max(videoPtsTicks, gpuPtsTicks)", fence);

        var videoDrain = global::Program.ExtractDeclaredMemberCode(source, "private bool DrainVideoPackets");
        Assert.Contains("var pts = OnVideoFrameEncoded()", videoDrain);
        Assert.Contains("RetireVideoPacket(gpu: false, pts.Ticks)", videoDrain);

        var gpuDrain = global::Program.ExtractDeclaredMemberCode(source, "private bool DrainGpuPackets");
        Assert.Contains("var pts = OnVideoFrameEncoded()", gpuDrain);
        Assert.Contains("RetireVideoPacket(gpu: true, pts.Ticks)", gpuDrain);
    }

    [Fact]
    public void ForcedExitUnresolvedMarker_PreservesActiveFlashbackSegmentsFirst()
    {
        var lifecycle = RuntimeContractSource.ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs");
        var method = global::Program.ExtractDeclaredMemberCode(
            lifecycle,
            "internal void MarkRecordingFinalizationUnresolved");
        Assert.Contains("PreserveUnresolvedFlashbackRecordingArtifacts", method);
        Assert.Contains("PreserveUnresolvedWithArtifacts", method);

        var preserve = global::Program.ExtractDeclaredMemberCode(
            lifecycle,
            "private IReadOnlyList<string> PreserveUnresolvedFlashbackRecordingArtifacts");
        Assert.Contains("_flashbackBackend.BufferManager == null", preserve);
        AssertInOrder(
            preserve,
            "_flashbackBackend.PreserveRecoverySegments(\"recording_finalization_unresolved\")",
            "GetFlashbackSegments()",
            ".Select(segment => segment.Path)");

        var window = RuntimeContractSource.ReadRepoFile("Sussudio/MainWindow.xaml.cs");
        var emergencyClose = global::Program.ExtractDeclaredMemberCode(
            window,
            "private async Task RunWasapiEmergencyCloseAsync");
        AssertInOrder(
            emergencyClose,
            "MarkRecordingFinalizationUnresolved(",
            "Environment.Exit(1)");
    }

    [Fact]
    public void EncodingLoop_FailsFast_WhenDiskCriticallyLow()
    {
        var method = global::Program.ExtractDeclaredMemberCode(Source(), "private TimeSpan OnVideoFrameEncoded");
        Assert.Contains("IsDiskCriticallyLow", method);
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
