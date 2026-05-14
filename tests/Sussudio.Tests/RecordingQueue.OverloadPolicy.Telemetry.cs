// Health snapshot and automation formatter assertions for the recording queue policy harness.
static partial class Program
{
    private static void AssertRecordingQueueHealthSnapshotTelemetry(
        string captureServiceSource,
        string captureHealthSnapshotRootSource,
        string captureSnapshotsSource,
        string unifiedVideoCaptureSource)
    {
        AssertContains(unifiedVideoCaptureSource, "encoder is IRawVideoFrameTryEncoder");
        AssertContains(unifiedVideoCaptureSource, "leaseEncoder is IRawVideoFrameLeaseTryEncoder");
        AssertContains(unifiedVideoCaptureSource, "encoder is IGpuVideoFrameTryEncoder");
        AssertContains(unifiedVideoCaptureSource, "BeginFlashbackRecordingAccounting");
        AssertContains(unifiedVideoCaptureSource, "RecordFlashbackRecordingAccounting");
        AssertContains(unifiedVideoCaptureSource, "sink.IsRecordingActive");
        AssertContains(unifiedVideoCaptureSource, "if (accepted)");
        AssertContains(unifiedVideoCaptureSource, "public MjpegPipelineTimingSnapshot GetMjpegPipelineTimingSnapshot()");
        AssertContains(unifiedVideoCaptureSource, "private static MjpegPipelineTimingMetrics CreateMjpegPipelineTimingSummary");
        AssertContains(captureServiceSource, "var timingSnapshot = unifiedVideoCapture.GetMjpegPipelineTimingSnapshot();");
        AssertContains(captureServiceSource, "RecordLastRecordingFailure");
        AssertContains(captureServiceSource, "RecordLastFlashbackFailure");
        AssertContains(captureServiceSource, "ClearLastRecordingFailure");
        AssertContains(captureServiceSource, "ClearLastFlashbackFailure");
        AssertContains(captureSnapshotsSource, "GetLastFailureTelemetry");
        AssertContains(captureSnapshotsSource, "IsFlashbackRecordingBackendOwnedByRecording()");
        AssertContains(captureHealthSnapshotRootSource, "var mjpegHealth = CaptureMjpegHealthSnapshotFields(unifiedVideoCapture);");
        AssertDoesNotContain(captureHealthSnapshotRootSource, "GetMjpegPipelineTimingSnapshot()");
        AssertContains(captureSnapshotsSource, "var timingSnapshot = unifiedVideoCapture?.GetMjpegPipelineTimingSnapshot();");
        AssertContains(captureSnapshotsSource, "private MjpegHealthSnapshotFields CaptureMjpegHealthSnapshotFields(");
        AssertDoesNotContain(captureSnapshotsSource, "unifiedVideoCapture?.GetMjpegPipelineTimingMetrics()");
        AssertDoesNotContain(captureSnapshotsSource, "unifiedVideoCapture?.GetFullMjpegPipelineTimingMetrics()");
        AssertContains(captureSnapshotsSource, "var flashbackVideoQueueLatencyMetrics = fbSink?.VideoQueueLatencyMetrics");
        AssertContains(captureSnapshotsSource, "sink?.VideoQueueLatencyMetrics ??");
        AssertDoesNotContain(captureSnapshotsSource, "var flashbackIsRecordingBackend = _isRecording && IsFlashbackRecordingBackendActive()");
        AssertContains(captureSnapshotsSource, "RecordingEncodingFailureMessage");
        AssertContains(captureSnapshotsSource, "RecordingVideoFramesSubmittedToEncoder = recordingHealth.VideoFramesSubmitted");
        AssertContains(captureSnapshotsSource, "RecordingVideoQueueLatencyP95Ms = recordingHealth.VideoQueueLatencyMetrics.P95Ms");
        AssertContains(captureSnapshotsSource, "RecordingVideoQueueLatencyP99Ms = recordingHealth.VideoQueueLatencyMetrics.P99Ms");
        AssertContains(captureSnapshotsSource, "RecordingVideoQueueOldestFrameAgeMs = recordingHealth.VideoQueueOldestFrameAgeMs");
        AssertContains(captureSnapshotsSource, "RecordingVideoBackpressureWaitMs = recordingHealth.VideoBackpressureWaitMs");
        AssertContains(captureSnapshotsSource, "fbSink?.VideoEncoderPacketsWritten ?? 0");
        AssertContains(captureSnapshotsSource, "fbSink?.VideoSequenceGaps ?? 0");
        AssertContains(captureSnapshotsSource, "fbSink?.VideoQueueOldestFrameAgeMs ?? 0");
        AssertContains(captureSnapshotsSource, "FlashbackVideoQueueLatencyP99Ms = flashbackQueues.VideoQueueLatencyMetrics.P99Ms");
        AssertContains(captureSnapshotsSource, "fbSink?.VideoBackpressureWaitMs ?? 0");
        AssertContains(captureSnapshotsSource, "FatalCleanupInProgress = fatalCleanupInProgress");
        AssertContains(captureSnapshotsSource, "FlashbackCleanupInProgress = flashbackCleanupInProgress");
        AssertContains(captureSnapshotsSource, "fbSink?.IsForceRotateActive ?? false");
        AssertContains(captureSnapshotsSource, "fbSink?.IsForceRotateRequested ?? false");
        AssertContains(captureSnapshotsSource, "fbSink?.IsForceRotateDraining ?? false");
        AssertContains(captureSnapshotsSource, "FlashbackEncodingFailureMessage");
        AssertContains(captureSnapshotsSource, "FlashbackStartupCacheBytes = flashbackBuffer.StartupCacheBytes");
        AssertContains(captureSnapshotsSource, "bufMgr?.StartupCacheBytes ?? 0");
        AssertContains(captureSnapshotsSource, "FlashbackTempDriveFreeBytes = flashbackBuffer.TempDriveFreeBytes");
        AssertContains(captureSnapshotsSource, "bufMgr?.TempDriveAvailableFreeBytes ?? 0");

        var sharedFormatterSource = ReadAutomationSnapshotFormatterSource();
        var ssctlFormatterSource = ReadSsctlSnapshotFormatterSource();
        var mcpAppStateSource = ReadRepoFile("tools/McpServer/Tools/AppStateTools.cs");
        AssertContains(sharedFormatterSource, "FlashbackEncodingFailed");
        AssertContains(sharedFormatterSource, "FlashbackStartupCacheBytes");
        AssertContains(sharedFormatterSource, "FlashbackCleanupInProgress");
        AssertContains(sharedFormatterSource, "FlashbackForceRotateActive");
        AssertContains(sharedFormatterSource, "FlashbackForceRotateRequested");
        AssertContains(sharedFormatterSource, "FlashbackForceRotateDraining");
        AssertContains(ssctlFormatterSource, "FlashbackEncodingFailed");
        AssertContains(ssctlFormatterSource, "FlashbackStartupCacheBytes");
        AssertContains(ssctlFormatterSource, "FlashbackCleanupInProgress");
        AssertContains(ssctlFormatterSource, "FlashbackForceRotateActive");
        AssertContains(ssctlFormatterSource, "FlashbackForceRotateRequested");
        AssertContains(ssctlFormatterSource, "FlashbackForceRotateDraining");
        AssertContains(mcpAppStateSource, "FormatSnapshot(response, includeFlashback: true)");
        AssertOccursBefore(
            sharedFormatterSource,
            "var flashbackFailed = Get(snapshot, \"FlashbackEncodingFailed\", \"false\");",
            "builder.AppendLine(\"== Flashback ==\");");
        AssertOccursBefore(
            ssctlFormatterSource,
            "var flashbackFailed = AutomationSnapshotFormatter.Get(snapshot, \"FlashbackEncodingFailed\", \"false\");",
            "builder.AppendLine(\"== Flashback ==\");");
    }
}
