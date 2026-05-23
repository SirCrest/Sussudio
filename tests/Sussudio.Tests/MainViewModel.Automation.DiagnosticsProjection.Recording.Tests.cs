using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsRecordingPipelineProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var recordingPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var recordingPipeline = BuildRecordingPipelineProjection(health);");
        AssertContains(snapshotFlatteningText, "var recordingPipelineFlattening = BuildRecordingPipelineFlattenedProjection(recordingPipeline);");
        AssertContains(snapshotFlatteningText, "EncoderVideoFramesEnqueued = recordingPipelineFlattening.Encoder.VideoFramesEnqueued,");
        AssertContains(snapshotFlatteningText, "ConversionQueueDepth = recordingPipelineFlattening.Ingest.ConversionQueueDepth,");
        AssertContains(snapshotFlatteningText, "RecordingVideoQueueCapacity = recordingPipelineFlattening.VideoQueue.Capacity,");
        AssertContains(snapshotFlatteningText, "RecordingGpuFramesEnqueued = recordingPipelineFlattening.HardwareQueues.GpuFramesEnqueued,");
        AssertContains(snapshotFlatteningText, "RecordingCudaFramesDropped = recordingPipelineFlattening.HardwareQueues.CudaFramesDropped,");
        AssertDoesNotContain(snapshotFlatteningText, "EncoderVideoFramesEnqueued = health.VideoFramesEnqueued,");
        AssertDoesNotContain(snapshotFlatteningText, "ConversionQueueDepth = health.ConversionQueueDepth,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingVideoQueueCapacity = health.RecordingVideoQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingCudaFramesDropped = health.RecordingCudaFramesDropped,");
        AssertDoesNotContain(snapshotFlatteningText, "EncoderVideoFramesEnqueued = recordingPipeline.EncoderVideoFramesEnqueued,");
        AssertDoesNotContain(snapshotFlatteningText, "ConversionQueueDepth = recordingPipeline.ConversionQueueDepth,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingVideoQueueCapacity = recordingPipeline.RecordingVideoQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingCudaFramesDropped = recordingPipeline.RecordingCudaFramesDropped,");

        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineFlattenedProjection BuildRecordingPipelineFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "Encoder = BuildRecordingPipelineEncoderFlattenedProjection(recordingPipeline),");
        AssertContains(recordingPipelineProjectionText, "Ingest = BuildRecordingPipelineIngestFlattenedProjection(recordingPipeline),");
        AssertContains(recordingPipelineProjectionText, "VideoQueue = BuildRecordingPipelineVideoQueueFlattenedProjection(recordingPipeline),");
        AssertContains(recordingPipelineProjectionText, "HardwareQueues = BuildRecordingPipelineHardwareQueuesFlattenedProjection(recordingPipeline)");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingPipelineFlattenedProjection");

        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineEncoderProjection BuildRecordingPipelineEncoderProjection(");
        AssertContains(recordingPipelineProjectionText, "VideoFramesEnqueued = health.VideoFramesEnqueued,");
        AssertContains(recordingPipelineProjectionText, "EncodingFailed = health.RecordingEncodingFailed,");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineIngestProjection BuildRecordingPipelineIngestProjection(");
        AssertContains(recordingPipelineProjectionText, "ConversionQueueDepth = health.ConversionQueueDepth,");
        AssertContains(recordingPipelineProjectionText, "VideoDropsBacklogEviction = health.VideoDropsBacklogEviction");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineVideoQueueProjection BuildRecordingPipelineVideoQueueProjection(");
        AssertContains(recordingPipelineProjectionText, "Capacity = health.RecordingVideoQueueCapacity,");
        AssertContains(recordingPipelineProjectionText, "BackpressureMaxWaitMs = health.RecordingVideoBackpressureMaxWaitMs");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineHardwareQueuesProjection BuildRecordingPipelineHardwareQueuesProjection(");
        AssertContains(recordingPipelineProjectionText, "GpuFramesEnqueued = health.RecordingGpuFramesEnqueued,");
        AssertContains(recordingPipelineProjectionText, "CudaFramesDropped = health.RecordingCudaFramesDropped");

        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineEncoderFlattenedProjection BuildRecordingPipelineEncoderFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "VideoFramesEnqueued = recordingPipeline.Encoder.VideoFramesEnqueued,");
        AssertContains(recordingPipelineProjectionText, "EncodingFailed = recordingPipeline.Encoder.EncodingFailed,");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineIngestFlattenedProjection BuildRecordingPipelineIngestFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "ConversionQueueDepth = recordingPipeline.Ingest.ConversionQueueDepth,");
        AssertContains(recordingPipelineProjectionText, "VideoDropsBacklogEviction = recordingPipeline.Ingest.VideoDropsBacklogEviction");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineVideoQueueFlattenedProjection BuildRecordingPipelineVideoQueueFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "Capacity = recordingPipeline.VideoQueue.Capacity,");
        AssertContains(recordingPipelineProjectionText, "BackpressureMaxWaitMs = recordingPipeline.VideoQueue.BackpressureMaxWaitMs");
        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineHardwareQueuesFlattenedProjection BuildRecordingPipelineHardwareQueuesFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "GpuFramesEnqueued = recordingPipeline.HardwareQueues.GpuFramesEnqueued,");
        AssertContains(recordingPipelineProjectionText, "CudaFramesDropped = recordingPipeline.HardwareQueues.CudaFramesDropped");

        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineProjection BuildRecordingPipelineProjection(CaptureHealthSnapshot health)");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingPipelineProjection");
        AssertContains(recordingPipelineProjectionText, "Encoder = BuildRecordingPipelineEncoderProjection(health),");
        AssertContains(recordingPipelineProjectionText, "Ingest = BuildRecordingPipelineIngestProjection(health),");
        AssertContains(recordingPipelineProjectionText, "VideoQueue = BuildRecordingPipelineVideoQueueProjection(health),");
        AssertContains(recordingPipelineProjectionText, "HardwareQueues = BuildRecordingPipelineHardwareQueuesProjection(health)");
        AssertContains(recordingPipelineProjectionText, "public RecordingPipelineEncoderProjection Encoder { get; init; }");
        AssertContains(recordingPipelineProjectionText, "public RecordingPipelineHardwareQueuesProjection HardwareQueues { get; init; }");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsRecordingBackendProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var recordingPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var recordingBackend = BuildRecordingBackendProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var recordingOutputFlattening = BuildRecordingOutputFlattenedProjection(recordingBackend, recordingOutput);");
        AssertContains(snapshotFlatteningText, "RecordingBackend = recordingOutputFlattening.Backend,");
        AssertContains(snapshotFlatteningText, "AudioPathMode = recordingOutputFlattening.AudioPathMode,");
        AssertContains(snapshotFlatteningText, "MuxResult = recordingOutputFlattening.MuxResult,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingBackend = captureRuntime.RecordingBackend,");
        AssertDoesNotContain(snapshotFlatteningText, "MuxResult = captureRuntime.MuxSucceeded.HasValue");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingBackend = recordingBackend.Backend,");
        AssertDoesNotContain(snapshotFlatteningText, "MuxResult = recordingBackend.MuxResult,");

        AssertContains(recordingPipelineProjectionText, "private static RecordingBackendProjection BuildRecordingBackendProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(recordingPipelineProjectionText, "Backend = captureRuntime.RecordingBackend,");
        AssertContains(recordingPipelineProjectionText, "AudioPathMode = captureRuntime.AudioPathMode,");
        AssertContains(recordingPipelineProjectionText, "MuxResult = ResolveMuxResult(captureRuntime.MuxSucceeded)");
        AssertContains(recordingPipelineProjectionText, "private static string ResolveMuxResult(bool? muxSucceeded)");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingBackendProjection");
        AssertContains(recordingPipelineProjectionText, "private static RecordingOutputFlattenedProjection BuildRecordingOutputFlattenedProjection(");
        AssertContains(recordingPipelineProjectionText, "Backend = recordingBackend.Backend,");
        AssertContains(recordingPipelineProjectionText, "AudioPathMode = recordingBackend.AudioPathMode,");
        AssertContains(recordingPipelineProjectionText, "MuxResult = recordingBackend.MuxResult,");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingOutputFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsRecordingOutputProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var recordingPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs")
            .Replace("\r\n", "\n");
        var obsoleteRecordingOutputPath = System.IO.Path.Combine(
            GetRepoRoot(),
            "Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs");

        AssertContains(snapshotProjectionText, "var recordingOutput = BuildRecordingOutputProjection(");
        AssertContains(snapshotFlatteningText, "OutputPath = recordingOutputFlattening.OutputPath,");
        AssertContains(snapshotFlatteningText, "RecordingVideoBytes = recordingOutputFlattening.RecordingVideoBytes,");
        AssertContains(snapshotFlatteningText, "LastOutputPath = recordingOutputFlattening.LastOutputPath,");
        AssertContains(snapshotFlatteningText, "LastVerification = recordingOutputFlattening.LastVerification,");
        AssertDoesNotContain(snapshotFlatteningText, "OutputPath = viewModelSnapshot.OutputPath,");
        AssertDoesNotContain(snapshotFlatteningText, "OutputPath = recordingOutput.OutputPath,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingVideoBytes = recordingStats.VideoBytes,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingVideoBytes = recordingOutput.RecordingVideoBytes,");
        AssertDoesNotContain(snapshotFlatteningText, "LastOutputPath = captureRuntime.LastOutputPath,");
        AssertDoesNotContain(snapshotFlatteningText, "LastOutputPath = recordingOutput.LastOutputPath,");
        AssertDoesNotContain(snapshotFlatteningText, "LastOutputSizeBytes = lastOutput.SizeBytes,");
        AssertDoesNotContain(snapshotFlatteningText, "LastVerification = recordingOutput.LastVerification,");

        AssertContains(recordingPipelineProjectionText, "private static RecordingOutputProjection BuildRecordingOutputProjection(");
        AssertContains(recordingPipelineProjectionText, "OutputPath = viewModelSnapshot.OutputPath,");
        AssertContains(recordingPipelineProjectionText, "RecordingVideoBytes = recordingStats.VideoBytes,");
        AssertContains(recordingPipelineProjectionText, "LastOutputPath = captureRuntime.LastOutputPath,");
        AssertContains(recordingPipelineProjectionText, "LastOutputSizeBytes = lastOutput.SizeBytes,");
        AssertContains(recordingPipelineProjectionText, "LastVerification = lastVerification");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingOutputProjection");
        AssertContains(recordingPipelineProjectionText, "OutputPath = recordingOutput.OutputPath,");
        AssertContains(recordingPipelineProjectionText, "RecordingVideoBytes = recordingOutput.RecordingVideoBytes,");
        AssertContains(recordingPipelineProjectionText, "LastOutputPath = recordingOutput.LastOutputPath,");
        AssertContains(recordingPipelineProjectionText, "LastVerification = recordingOutput.LastVerification");
        if (System.IO.File.Exists(obsoleteRecordingOutputPath))
        {
            throw new System.InvalidOperationException("Recording output projection should stay consolidated into RecordingPipeline.cs.");
        }

        return Task.CompletedTask;
    }

}
