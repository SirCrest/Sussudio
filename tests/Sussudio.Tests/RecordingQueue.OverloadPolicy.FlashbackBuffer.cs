// Flashback buffer recovery and enqueue-gating assertions for the recording queue policy harness.
static partial class Program
{
    private static void AssertFlashbackBufferRecoveryPolicy(
        string flashbackSource,
        string flashbackBufferSource,
        string flashbackCleanupSource)
    {
        var flashbackBufferDispose = ExtractSourceBlock(
            flashbackBufferSource,
            "public void Dispose()",
            "private void ThrowIfDisposed()");
        AssertDoesNotContain(flashbackBufferDispose, "PurgeAllSegments()");
        AssertContains(flashbackBufferSource, "RecoveryPreserveMarkerFileName");
        AssertContains(flashbackBufferSource, "MarkSessionPreservedForRecovery");
        AssertContains(flashbackBufferSource, "public bool IsSessionPreservedForRecovery");
        AssertContains(flashbackBufferSource, "private bool _preserveSessionForRecovery;");
        AssertContains(flashbackBufferSource, "private bool IsSessionPreservedForRecoveryUnsafe()");
        AssertContains(flashbackBufferSource, "FLASHBACK_BUFFER_PURGE_SKIP reason=recovery_preserved");
        AssertContains(flashbackBufferSource, "FLASHBACK_BUFFER_DISPOSE_PRESERVE_RECOVERY");
        AssertContains(flashbackCleanupSource, "FLASHBACK_STALE_SESSION_PRESERVE_SKIP");
        AssertContains(flashbackCleanupSource, "File.Exists(Path.Combine(fullPath, RecoveryPreserveMarkerFileName))");
        AssertContains(flashbackBufferSource, "DeleteFileForEviction(oldest.Path, oldest.SizeBytes, \"valid_window\")");
        AssertContains(flashbackBufferSource, "DeleteFileForEviction(oldest.Path, oldest.SizeBytes, \"disk_budget\")");
        AssertContains(flashbackBufferSource, "private static bool DeleteEvictedFile");
        AssertContains(flashbackBufferSource, "FLASHBACK_BUFFER_EVICT_DELETE_WARN");
        AssertContains(flashbackBufferSource, "FLASHBACK_BUFFER_SEGMENT_EVICT_DELETED");
        AssertContains(flashbackBufferSource, "public void MarkActiveSegmentStart(string path, TimeSpan startPts)");
        AssertContains(flashbackSource, "_bufferManager.MarkActiveSegmentStart(tsPath, _segmentStartPts);");
        AssertContains(flashbackSource, "_bufferManager.MarkActiveSegmentStart(newPath, _segmentStartPts);");

        var flashbackVideoEnqueue = ExtractSourceBlock(
            flashbackSource,
            "private VideoEnqueueResult TryEnqueueVideoPacket",
            "private VideoEnqueueResult TryEnqueueGpuPacket");
        var flashbackGpuEnqueue = ExtractSourceBlock(
            flashbackSource,
            "private VideoEnqueueResult TryEnqueueGpuPacket",
            "private void FailEncoding");
        var flashbackAudioEnqueue = ExtractSourceBlock(
            flashbackSource,
            "private bool TryEnqueueAudioPacket",
            "public void BeginRecording");
        AssertOccursBefore(flashbackVideoEnqueue, "GetVideoEnqueueRejectReason(isGpu: false)", "TryWriteVideoPacket(queue, packet)");
        AssertOccursBefore(flashbackGpuEnqueue, "GetVideoEnqueueRejectReason(isGpu: true)", "TryWriteGpuPacket(queue, packet)");
        AssertOccursBefore(flashbackAudioEnqueue, "Volatile.Read(ref _forceRotateDraining)", "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio\")");
        AssertContains(flashbackVideoEnqueue, "var rejectReason = GetVideoEnqueueRejectReason(isGpu: false);");
        AssertContains(flashbackVideoEnqueue, "TrackVideoQueueRejected(rejectReason);");
        AssertContains(flashbackGpuEnqueue, "var rejectReason = GetVideoEnqueueRejectReason(isGpu: true);");
        AssertContains(flashbackGpuEnqueue, "TrackGpuQueueRejected(rejectReason);");
        AssertContains(flashbackAudioEnqueue, "if (_disposed ||");
        AssertContains(flashbackAudioEnqueue, "!_started ||");
        AssertContains(flashbackGpuEnqueue, "lock (_videoQueueSync)");
        AssertContains(flashbackAudioEnqueue, "lock (_videoQueueSync)");
    }
}
