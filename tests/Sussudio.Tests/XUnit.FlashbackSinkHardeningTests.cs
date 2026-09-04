using System;
using System.IO;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackSinkHardeningTests
{
    private static string Source() =>
        File.ReadAllText(TestPaths.Repo("Sussudio/Services/Flashback/FlashbackEncoderSink.cs"));

    [Fact]
    public void VideoEnqueue_DuringForceRotateDrain_UsesQueueGuardRatio()
    {
        var method = SourceSlice.Method(Source(), "private string? GetVideoEnqueueRejectReason");
        // Unconditional rejection is the bug; the guard must consider queue depth
        // exactly like the audio path (ForceRotateQueueGuardRatio).
        Assert.Contains("IsForceRotateQueueGuarded", method);
    }

    [Fact]
    public void RotateSegment_EscalatesAfterConsecutiveFailures()
    {
        var source = Source();
        Assert.Contains("MaxConsecutiveRotationFailures = 3", source);
        var method = SourceSlice.Method(source, "private bool RotateSegment");
        Assert.Contains("_consecutiveRotationFailures", method);
        Assert.Contains("FailEncoding", method);
    }

    [Fact]
    public void EndRecording_WaitsForAcceptedBoundary_NotLiveQueueEmptiness()
    {
        var source = Source();
        var method = SourceSlice.Method(source, "public async Task<FinalizeResult> EndRecordingAsync");
        Assert.Contains("CaptureRecordingBoundaryFence", method);
        Assert.Contains("WaitForRecordingBoundaryAsync", method);
        Assert.Contains("var endPts = recordingBoundary.EndPts", method);
        Assert.DoesNotContain("var endPts = _bufferManager.LatestPts", method);

        var capture = SourceSlice.Method(source, "private RecordingBoundaryFence CaptureRecordingBoundaryFence");
        Assert.Contains("lock (_videoQueueSync)", capture);
        Assert.Contains("_videoFramesEnqueued", capture);
        Assert.Contains("_audioPacketsAccepted", capture);
        Assert.Contains("_microphonePacketsAccepted", capture);
        Assert.Contains("_gpuFramesEnqueued", capture);

        var wait = SourceSlice.Method(source, "private async Task<bool> WaitForRecordingBoundaryAsync");
        Assert.Contains("_videoPacketsRetired) >= boundary.VideoPacketsAccepted", wait);
        Assert.Contains("_audioPacketsRetired) >= boundary.AudioPacketsAccepted", wait);
        Assert.Contains("_microphonePacketsRetired) >= boundary.MicrophonePacketsAccepted", wait);
        Assert.Contains("_gpuPacketsRetired) >= boundary.GpuPacketsAccepted", wait);
        Assert.Contains("boundary.HasResolvedVideoEndPts", wait);
        Assert.DoesNotContain("_videoQueueDepth) == 0", wait);
        Assert.DoesNotContain("_audioQueueDepth) == 0", wait);
        Assert.DoesNotContain("_microphoneQueueDepth) == 0", wait);
        Assert.DoesNotContain("_gpuQueueDepth) == 0", wait);

        var audioEnqueue = SourceSlice.Method(source, "private bool TryEnqueueAudioPacket");
        Assert.Contains("Interlocked.Increment(ref acceptedPackets)", audioEnqueue);
        Assert.Contains("Interlocked.Increment(ref retiredPackets)", audioEnqueue);
        Assert.Contains("Interlocked.Increment(ref _videoPacketsRetired)", source);
        Assert.Contains("Interlocked.Increment(ref _gpuPacketsRetired)", source);
        Assert.Contains("Interlocked.Increment(ref _audioPacketsRetired)", source);
        Assert.Contains("Interlocked.Increment(ref _microphonePacketsRetired)", source);

        var fence = SourceSlice.Method(source, "private sealed class RecordingBoundaryFence");
        Assert.Contains("CaptureAlreadyRetiredVideoPts", fence);
        Assert.Contains("ObserveVideoRetirement", fence);
        Assert.Contains("Interlocked.CompareExchange", fence);
        Assert.Contains("Math.Max(videoPtsTicks, gpuPtsTicks)", fence);

        var videoDrain = SourceSlice.Method(source, "private bool DrainVideoPackets");
        Assert.Contains("var pts = OnVideoFrameEncoded()", videoDrain);
        Assert.Contains("RetireVideoPacket(gpu: false, pts.Ticks)", videoDrain);

        var gpuDrain = SourceSlice.Method(source, "private bool DrainGpuPackets");
        Assert.Contains("var pts = OnVideoFrameEncoded()", gpuDrain);
        Assert.Contains("RetireVideoPacket(gpu: true, pts.Ticks)", gpuDrain);
    }

    [Fact]
    public void ForcedExitUnresolvedMarker_PreservesActiveFlashbackSegmentsFirst()
    {
        var lifecycle = File.ReadAllText(
            TestPaths.Repo("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs"));
        var method = SourceSlice.Method(
            lifecycle,
            "internal void MarkRecordingFinalizationUnresolved");
        Assert.Contains("PreserveUnresolvedFlashbackRecordingArtifacts", method);
        Assert.Contains("PreserveUnresolvedWithArtifacts", method);

        var preserve = SourceSlice.Method(
            lifecycle,
            "private IReadOnlyList<string> PreserveUnresolvedFlashbackRecordingArtifacts");
        Assert.Contains("_flashbackBackend.BufferManager == null", preserve);
        AssertInOrder(
            preserve,
            "_flashbackBackend.PreserveRecoverySegments(\"recording_finalization_unresolved\")",
            "GetFlashbackSegments()",
            ".Select(segment => segment.Path)");

        var window = File.ReadAllText(TestPaths.Repo("Sussudio/MainWindow.xaml.cs"));
        var emergencyClose = SourceSlice.Method(
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
        var method = SourceSlice.Method(Source(), "private TimeSpan OnVideoFrameEncoded");
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

// TestPaths/SourceSlice do not exist as shared helpers in the test project (the
// established convention here is Assembly.LoadFrom + reflection against the
// staged Sussudio.dll — see MIGRATION.md — rather than a compile-time
// ProjectReference or a shared source-slicing utility). Per the plan's fallback
// instruction, these are private, file-scoped copies rather than edits to any
// shared test file. (Mirrors the copy in XUnit.FlashbackFatalPathContractsTests.cs.)
file static class TestPaths
{
    public static string Repo(string relativePath) => Path.Combine(FindRepoRoot(), relativePath);

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sussudio.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate repository root from '{AppContext.BaseDirectory}'.");
    }
}

file static class SourceSlice
{
    /// <summary>
    /// Returns the source text of the method whose declaration starts with
    /// <paramref name="signaturePrefix"/> (e.g. "private void Foo"), from its
    /// signature through the matching closing brace of its body.
    /// </summary>
    public static string Method(string source, string signaturePrefix)
    {
        var start = source.IndexOf(signaturePrefix, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Could not find method starting with '{signaturePrefix}'.");
        }

        var braceOpen = source.IndexOf('{', start);
        if (braceOpen < 0)
        {
            throw new InvalidOperationException($"Could not find method body open brace for '{signaturePrefix}'.");
        }

        var depth = 0;
        for (var i = braceOpen; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(start, i - start + 1);
                }
            }
        }

        throw new InvalidOperationException($"Could not find matching closing brace for '{signaturePrefix}'.");
    }
}
