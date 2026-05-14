using System;
using System.Threading.Tasks;

// Tests for recording sink queue limits, drops, and latency accounting.
static partial class Program
{
    private static Task UnifiedVideoCapture_SinkFanoutLivesInFocusedPartial()
    {
        var rootSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs")
            .Replace("\r\n", "\n");
        var fanoutSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.cs")
            .Replace("\r\n", "\n");

        AssertContains(fanoutSource, "private void EnqueueRecordingFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertContains(fanoutSource, "private void EnqueueRecordingFrame(PooledVideoFrame frame)");
        AssertContains(fanoutSource, "private void EnqueueGpuRecordingFrame(IGpuVideoFrameEncoder encoder, IntPtr texture, int subresource, long sourceSequence)");
        AssertContains(fanoutSource, "private void EnqueueFlashbackFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertContains(fanoutSource, "private void EnqueueFlashbackFrame(PooledVideoFrame frame)");
        AssertContains(fanoutSource, "private void EnqueueFlashbackGpuFrame(IntPtr texture, int subresource, long sourceSequence)");
        AssertContains(fanoutSource, "private void TrackFlashbackRecordingAcceptedSequence(long sourceSequence)");
        AssertDoesNotContain(rootSource, "private void EnqueueRecordingFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertDoesNotContain(rootSource, "private void EnqueueFlashbackFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)");
        AssertDoesNotContain(rootSource, "private static bool TryLegacyRawVideoEnqueue(");

        return Task.CompletedTask;
    }

    private static Task CaptureService_FlashbackBackendOwnershipUsesResourceAggregate()
    {
        var captureSource = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackOrchestration.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeRecord.cs")
                .Replace("\r\n", "\n");
        var backendSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs")
            .Replace("\r\n", "\n");

        AssertContains(backendSource, "internal sealed class FlashbackBackendResources");
        AssertContains(backendSource, "public FlashbackBufferManager? BufferManager { get; set; }");
        AssertContains(backendSource, "public FlashbackEncoderSink? Sink { get; set; }");
        AssertContains(backendSource, "public FlashbackExporter? Exporter { get; set; }");
        AssertContains(backendSource, "public FlashbackPlaybackController? PlaybackController { get; set; }");
        AssertContains(backendSource, "public CaptureSettings? SettingsSnapshot { get; set; }");
        AssertContains(backendSource, "public bool HasAnyResource");
        AssertContains(backendSource, "public bool PreserveSegmentsAfterFailedRecordingFinalize { get; private set; }");
        AssertContains(backendSource, "public void Install(");
        AssertContains(backendSource, "public void ClearRecoveryPreserve()");
        AssertContains(backendSource, "public bool ResolveSegmentPurge(bool requested, string reason)");
        AssertContains(backendSource, "public void PreserveRecoverySegments(string reason)");
        AssertContains(backendSource, "public async Task<FinalizeResult> FinalizeRecordingAsync(");
        AssertContains(backendSource, "private static FinalizeResult PreserveEndArtifactsOnFailure(");
        AssertContains(backendSource, "public FlashbackPlaybackController? TakePlaybackController()");
        AssertContains(backendSource, "public void DetachProducers(");
        AssertContains(backendSource, "public void ClearSinkAndSettings()");
        AssertContains(backendSource, "public void Clear()");

        AssertContains(captureSource, "private readonly FlashbackBackendResources _flashbackBackend = new();");
        AssertDoesNotContain(captureSource, "private FlashbackBufferManager? _flashbackBufferManager;");
        AssertDoesNotContain(captureSource, "private FlashbackEncoderSink? _flashbackSink;");
        AssertDoesNotContain(captureSource, "private FlashbackExporter? _flashbackExporter;");
        AssertDoesNotContain(captureSource, "private FlashbackPlaybackController? _flashbackPlaybackController;");
        AssertDoesNotContain(captureSource, "private CaptureSettings? _flashbackBackendSettings;");
        AssertContains(captureSource, "_flashbackBackend.HasAnyResource");
        AssertContains(captureSource, "_flashbackBackend.Install(");
        AssertContains(captureSource, "_flashbackBackend.TakePlaybackController()");
        AssertContains(captureSource, "_flashbackBackend.DetachProducers(");
        AssertContains(captureSource, "_flashbackBackend.ResolveSegmentPurge(");
        AssertContains(captureSource, "_flashbackBackend.PreserveRecoverySegments(");
        AssertContains(captureSource, "_flashbackBackend.ClearRecoveryPreserve();");
        AssertContains(captureSource, "_flashbackBackend.FinalizeRecordingAsync(");
        AssertContains(captureSource, "_flashbackBackend.ClearSinkAndSettings();");
        AssertContains(captureSource, "_flashbackBackend.Clear();");

        return Task.CompletedTask;
    }

}
