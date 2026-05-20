using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsRecordingPipelineProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var recordingPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs")
            .Replace("\r\n", "\n");
        var recordingPipelineFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingPipeline.cs")
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

        AssertContains(recordingPipelineFlatteningText, "private static RecordingPipelineFlattenedProjection BuildRecordingPipelineFlattenedProjection(");
        AssertContains(recordingPipelineFlatteningText, "Encoder = BuildRecordingPipelineEncoderFlattenedProjection(recordingPipeline),");
        AssertContains(recordingPipelineFlatteningText, "Ingest = BuildRecordingPipelineIngestFlattenedProjection(recordingPipeline),");
        AssertContains(recordingPipelineFlatteningText, "VideoQueue = BuildRecordingPipelineVideoQueueFlattenedProjection(recordingPipeline),");
        AssertContains(recordingPipelineFlatteningText, "HardwareQueues = BuildRecordingPipelineHardwareQueuesFlattenedProjection(recordingPipeline)");
        AssertContains(recordingPipelineFlatteningText, "private readonly record struct RecordingPipelineFlattenedProjection");

        var recordingPipelineEncoderFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingPipeline.Encoder.cs")
            .Replace("\r\n", "\n");
        var recordingPipelineIngestFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingPipeline.Ingest.cs")
            .Replace("\r\n", "\n");
        var recordingPipelineVideoQueueFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingPipeline.VideoQueue.cs")
            .Replace("\r\n", "\n");
        var recordingPipelineHardwareQueuesFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingPipeline.HardwareQueues.cs")
            .Replace("\r\n", "\n");

        AssertContains(recordingPipelineEncoderFlatteningText, "private static RecordingPipelineEncoderFlattenedProjection BuildRecordingPipelineEncoderFlattenedProjection(");
        AssertContains(recordingPipelineEncoderFlatteningText, "VideoFramesEnqueued = recordingPipeline.EncoderVideoFramesEnqueued,");
        AssertContains(recordingPipelineEncoderFlatteningText, "EncodingFailed = recordingPipeline.RecordingEncodingFailed,");
        AssertContains(recordingPipelineIngestFlatteningText, "private static RecordingPipelineIngestFlattenedProjection BuildRecordingPipelineIngestFlattenedProjection(");
        AssertContains(recordingPipelineIngestFlatteningText, "ConversionQueueDepth = recordingPipeline.ConversionQueueDepth,");
        AssertContains(recordingPipelineIngestFlatteningText, "VideoDropsBacklogEviction = recordingPipeline.VideoDropsBacklogEviction");
        AssertContains(recordingPipelineVideoQueueFlatteningText, "private static RecordingPipelineVideoQueueFlattenedProjection BuildRecordingPipelineVideoQueueFlattenedProjection(");
        AssertContains(recordingPipelineVideoQueueFlatteningText, "Capacity = recordingPipeline.RecordingVideoQueueCapacity,");
        AssertContains(recordingPipelineVideoQueueFlatteningText, "BackpressureMaxWaitMs = recordingPipeline.RecordingVideoBackpressureMaxWaitMs");
        AssertContains(recordingPipelineHardwareQueuesFlatteningText, "private static RecordingPipelineHardwareQueuesFlattenedProjection BuildRecordingPipelineHardwareQueuesFlattenedProjection(");
        AssertContains(recordingPipelineHardwareQueuesFlatteningText, "GpuFramesEnqueued = recordingPipeline.RecordingGpuFramesEnqueued,");
        AssertContains(recordingPipelineHardwareQueuesFlatteningText, "CudaFramesDropped = recordingPipeline.RecordingCudaFramesDropped");

        AssertContains(recordingPipelineProjectionText, "private static RecordingPipelineProjection BuildRecordingPipelineProjection(CaptureHealthSnapshot health)");
        AssertContains(recordingPipelineProjectionText, "private readonly record struct RecordingPipelineProjection");
        AssertContains(recordingPipelineProjectionText, "EncoderVideoFramesEnqueued = health.VideoFramesEnqueued,");
        AssertContains(recordingPipelineProjectionText, "ConversionQueueDepth = health.ConversionQueueDepth,");
        AssertContains(recordingPipelineProjectionText, "RecordingVideoQueueCapacity = health.RecordingVideoQueueCapacity,");
        AssertContains(recordingPipelineProjectionText, "RecordingCudaFramesDropped = health.RecordingCudaFramesDropped");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsRecordingBackendProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var recordingOutputFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingOutput.cs")
            .Replace("\r\n", "\n");
        var recordingOutputProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs")
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

        AssertContains(recordingOutputFlatteningText, "private static RecordingOutputFlattenedProjection BuildRecordingOutputFlattenedProjection(");
        AssertContains(recordingOutputFlatteningText, "Backend = recordingBackend.Backend,");
        AssertContains(recordingOutputFlatteningText, "AudioPathMode = recordingBackend.AudioPathMode,");
        AssertContains(recordingOutputFlatteningText, "MuxResult = recordingBackend.MuxResult,");
        AssertContains(recordingOutputFlatteningText, "private readonly record struct RecordingOutputFlattenedProjection");

        AssertContains(recordingOutputProjectionText, "private static RecordingBackendProjection BuildRecordingBackendProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(recordingOutputProjectionText, "Backend = captureRuntime.RecordingBackend,");
        AssertContains(recordingOutputProjectionText, "AudioPathMode = captureRuntime.AudioPathMode,");
        AssertContains(recordingOutputProjectionText, "MuxResult = ResolveMuxResult(captureRuntime.MuxSucceeded)");
        AssertContains(recordingOutputProjectionText, "private static string ResolveMuxResult(bool? muxSucceeded)");
        AssertContains(recordingOutputProjectionText, "private readonly record struct RecordingBackendProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsRecordingOutputProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var recordingOutputFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingOutput.cs")
            .Replace("\r\n", "\n");
        var recordingOutputProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs")
            .Replace("\r\n", "\n");

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

        AssertContains(recordingOutputFlatteningText, "OutputPath = recordingOutput.OutputPath,");
        AssertContains(recordingOutputFlatteningText, "RecordingVideoBytes = recordingOutput.RecordingVideoBytes,");
        AssertContains(recordingOutputFlatteningText, "LastOutputPath = recordingOutput.LastOutputPath,");
        AssertContains(recordingOutputFlatteningText, "LastVerification = recordingOutput.LastVerification");

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
