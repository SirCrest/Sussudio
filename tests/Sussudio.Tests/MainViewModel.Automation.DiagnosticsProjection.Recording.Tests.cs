using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationDiagnosticsRecordingPipelineProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var recordingPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var recordingPipeline = BuildRecordingPipelineProjection(health);");
        AssertContains(snapshotProjectionText, "EncoderVideoFramesEnqueued = recordingPipeline.EncoderVideoFramesEnqueued,");
        AssertContains(snapshotProjectionText, "ConversionQueueDepth = recordingPipeline.ConversionQueueDepth,");
        AssertContains(snapshotProjectionText, "RecordingVideoQueueCapacity = recordingPipeline.RecordingVideoQueueCapacity,");
        AssertContains(snapshotProjectionText, "RecordingGpuFramesEnqueued = recordingPipeline.RecordingGpuFramesEnqueued,");
        AssertContains(snapshotProjectionText, "RecordingCudaFramesDropped = recordingPipeline.RecordingCudaFramesDropped,");
        AssertDoesNotContain(snapshotProjectionText, "EncoderVideoFramesEnqueued = health.VideoFramesEnqueued,");
        AssertDoesNotContain(snapshotProjectionText, "ConversionQueueDepth = health.ConversionQueueDepth,");
        AssertDoesNotContain(snapshotProjectionText, "RecordingVideoQueueCapacity = health.RecordingVideoQueueCapacity,");
        AssertDoesNotContain(snapshotProjectionText, "RecordingCudaFramesDropped = health.RecordingCudaFramesDropped,");

        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineProjection BuildRecordingPipelineProjection(CaptureHealthSnapshot health)");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingPipelineProjection");
        AssertContains(recordingPipelineProjectionText, "EncoderVideoFramesEnqueued = health.VideoFramesEnqueued,");
        AssertContains(recordingPipelineProjectionText, "ConversionQueueDepth = health.ConversionQueueDepth,");
        AssertContains(recordingPipelineProjectionText, "RecordingVideoQueueCapacity = health.RecordingVideoQueueCapacity,");
        AssertContains(recordingPipelineProjectionText, "RecordingCudaFramesDropped = health.RecordingCudaFramesDropped");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsRecordingBackendProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var recordingBackendProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingBackend.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var recordingBackend = BuildRecordingBackendProjection(captureRuntime);");
        AssertContains(snapshotProjectionText, "RecordingBackend = recordingBackend.Backend,");
        AssertContains(snapshotProjectionText, "AudioPathMode = recordingBackend.AudioPathMode,");
        AssertContains(snapshotProjectionText, "MuxResult = recordingBackend.MuxResult,");
        AssertDoesNotContain(snapshotProjectionText, "RecordingBackend = captureRuntime.RecordingBackend,");
        AssertDoesNotContain(snapshotProjectionText, "MuxResult = captureRuntime.MuxSucceeded.HasValue");

        AssertContains(recordingBackendProjectionText, "private static RecordingBackendProjection BuildRecordingBackendProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(recordingBackendProjectionText, "Backend = captureRuntime.RecordingBackend,");
        AssertContains(recordingBackendProjectionText, "AudioPathMode = captureRuntime.AudioPathMode,");
        AssertContains(recordingBackendProjectionText, "MuxResult = ResolveMuxResult(captureRuntime.MuxSucceeded)");
        AssertContains(recordingBackendProjectionText, "private static string ResolveMuxResult(bool? muxSucceeded)");
        AssertContains(recordingBackendProjectionText, "private readonly record struct RecordingBackendProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsRecordingOutputProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var recordingOutputProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var recordingOutput = BuildRecordingOutputProjection(");
        AssertContains(snapshotProjectionText, "OutputPath = recordingOutput.OutputPath,");
        AssertContains(snapshotProjectionText, "RecordingVideoBytes = recordingOutput.RecordingVideoBytes,");
        AssertContains(snapshotProjectionText, "LastOutputPath = recordingOutput.LastOutputPath,");
        AssertContains(snapshotProjectionText, "LastVerification = recordingOutput.LastVerification,");
        AssertDoesNotContain(snapshotProjectionText, "OutputPath = viewModelSnapshot.OutputPath,");
        AssertDoesNotContain(snapshotProjectionText, "RecordingVideoBytes = recordingStats.VideoBytes,");
        AssertDoesNotContain(snapshotProjectionText, "LastOutputPath = captureRuntime.LastOutputPath,");
        AssertDoesNotContain(snapshotProjectionText, "LastOutputSizeBytes = lastOutput.SizeBytes,");

        AssertContains(recordingOutputProjectionText, "private static RecordingOutputProjection BuildRecordingOutputProjection(");
        AssertContains(recordingOutputProjectionText, "OutputPath = viewModelSnapshot.OutputPath,");
        AssertContains(recordingOutputProjectionText, "RecordingVideoBytes = recordingStats.VideoBytes,");
        AssertContains(recordingOutputProjectionText, "LastOutputPath = captureRuntime.LastOutputPath,");
        AssertContains(recordingOutputProjectionText, "LastOutputSizeBytes = lastOutput.SizeBytes,");
        AssertContains(recordingOutputProjectionText, "LastVerification = lastVerification");
        AssertContains(recordingOutputProjectionText, "private readonly record struct RecordingOutputProjection");

        return Task.CompletedTask;
    }

}
