using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsCaptureFormatProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningCaptureFormatText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.cs")
            .Replace("\r\n", "\n");
        var captureFormatProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureFormat = BuildCaptureFormatProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var captureFormatFlattening = BuildCaptureFormatFlattenedProjection(captureFormat);");
        AssertContains(snapshotFlatteningText, "RequestedWidth = captureFormatFlattening.Requested.Width,");
        AssertContains(snapshotFlatteningText, "HdrActivationReason = captureFormatFlattening.HdrRequest.ActivationReason,");
        AssertContains(snapshotFlatteningText, "NegotiatedWidth = captureFormatFlattening.Negotiated.Width,");
        AssertContains(snapshotFlatteningText, "LatestObservedFramePixelFormat = captureFormatFlattening.ReaderObservation.LatestObservedFramePixelFormat,");
        AssertContains(snapshotFlatteningText, "EncoderVideoCodec = captureFormatFlattening.Encoder.VideoCodec,");
        AssertContains(snapshotFlatteningCaptureFormatText, "private static CaptureFormatFlattenedProjection BuildCaptureFormatFlattenedProjection(");
        AssertContains(snapshotFlatteningCaptureFormatText, "Requested = BuildCaptureFormatRequestedFlattenedProjection(captureFormat),");
        AssertContains(snapshotFlatteningCaptureFormatText, "HdrRequest = BuildCaptureFormatHdrRequestFlattenedProjection(captureFormat),");
        AssertContains(snapshotFlatteningCaptureFormatText, "Negotiated = BuildCaptureFormatNegotiatedFlattenedProjection(captureFormat),");
        AssertContains(snapshotFlatteningCaptureFormatText, "ReaderObservation = BuildCaptureFormatReaderObservationFlattenedProjection(captureFormat),");
        AssertContains(snapshotFlatteningCaptureFormatText, "Encoder = BuildCaptureFormatEncoderFlattenedProjection(captureFormat)");
        AssertContains(snapshotFlatteningCaptureFormatText, "private readonly record struct CaptureFormatFlattenedProjection");

        var snapshotFlatteningCaptureFormatRequestedText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.Requested.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningCaptureFormatHdrRequestText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.HdrRequest.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningCaptureFormatNegotiatedText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.Negotiated.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningCaptureFormatReaderObservationText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.ReaderObservation.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningCaptureFormatEncoderText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.Encoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotFlatteningCaptureFormatRequestedText, "private static CaptureFormatRequestedFlattenedProjection BuildCaptureFormatRequestedFlattenedProjection(");
        AssertContains(snapshotFlatteningCaptureFormatRequestedText, "Width = captureFormat.RequestedWidth,");
        AssertContains(snapshotFlatteningCaptureFormatHdrRequestText, "private static CaptureFormatHdrRequestFlattenedProjection BuildCaptureFormatHdrRequestFlattenedProjection(");
        AssertContains(snapshotFlatteningCaptureFormatHdrRequestText, "ActivationReason = captureFormat.HdrActivationReason,");
        AssertContains(snapshotFlatteningCaptureFormatNegotiatedText, "private static CaptureFormatNegotiatedFlattenedProjection BuildCaptureFormatNegotiatedFlattenedProjection(");
        AssertContains(snapshotFlatteningCaptureFormatNegotiatedText, "Width = captureFormat.NegotiatedWidth,");
        AssertContains(snapshotFlatteningCaptureFormatNegotiatedText, "MediaSubtypeToken = captureFormat.NegotiatedMediaSubtypeToken");
        AssertContains(snapshotFlatteningCaptureFormatReaderObservationText, "private static CaptureFormatReaderObservationFlattenedProjection BuildCaptureFormatReaderObservationFlattenedProjection(");
        AssertContains(snapshotFlatteningCaptureFormatReaderObservationText, "LatestObservedFramePixelFormat = captureFormat.LatestObservedFramePixelFormat,");
        AssertContains(snapshotFlatteningCaptureFormatReaderObservationText, "MfReadwriteDisableConverters = captureFormat.MfReadwriteDisableConverters");
        AssertContains(snapshotFlatteningCaptureFormatEncoderText, "private static CaptureFormatEncoderFlattenedProjection BuildCaptureFormatEncoderFlattenedProjection(");
        AssertContains(snapshotFlatteningCaptureFormatEncoderText, "VideoCodec = captureFormat.EncoderVideoCodec,");
        AssertContains(snapshotFlatteningCaptureFormatEncoderText, "TenBitPipelineConfirmed = captureFormat.EncoderTenBitPipelineConfirmed");
        AssertDoesNotContain(snapshotFlatteningText, "RequestedWidth = captureRuntime.RequestedWidth,");
        AssertDoesNotContain(snapshotFlatteningText, "RequestedWidth = captureFormat.RequestedWidth,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrActivationReason = captureRuntime.HdrActivationReason,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrActivationReason = captureFormat.HdrActivationReason,");
        AssertDoesNotContain(snapshotFlatteningText, "NegotiatedWidth = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,");
        AssertDoesNotContain(snapshotFlatteningText, "NegotiatedWidth = captureFormat.NegotiatedWidth,");
        AssertDoesNotContain(snapshotFlatteningText, "LatestObservedFramePixelFormat = captureRuntime.LatestObservedFramePixelFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "LatestObservedFramePixelFormat = captureFormat.LatestObservedFramePixelFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "EncoderVideoCodec = captureRuntime.EncoderVideoCodec,");
        AssertDoesNotContain(snapshotFlatteningText, "EncoderVideoCodec = captureFormat.EncoderVideoCodec,");

        AssertContains(captureFormatProjectionText, "private static CaptureFormatProjection BuildCaptureFormatProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(captureFormatProjectionText, "private readonly record struct CaptureFormatProjection");
        AssertContains(captureFormatProjectionText, "RequestedWidth = captureRuntime.RequestedWidth,");
        AssertContains(captureFormatProjectionText, "HdrActivationReason = captureRuntime.HdrActivationReason,");
        AssertContains(captureFormatProjectionText, "NegotiatedWidth = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,");
        AssertContains(captureFormatProjectionText, "LatestObservedFramePixelFormat = captureRuntime.LatestObservedFramePixelFormat,");
        AssertContains(captureFormatProjectionText, "EncoderVideoCodec = captureRuntime.EncoderVideoCodec,");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsCaptureTransportProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningCaptureTransportText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureTransport.cs")
            .Replace("\r\n", "\n");
        var captureTransportProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureTransport.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureTransport = BuildCaptureTransportProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var captureTransportFlattening = BuildCaptureTransportFlattenedProjection(captureTransport);");
        AssertContains(snapshotFlatteningText, "MemoryPreference = captureTransportFlattening.MemoryPreference,");
        AssertContains(snapshotFlatteningText, "VideoNegotiatedSubtype = captureTransportFlattening.VideoNegotiatedSubtype,");
        AssertContains(snapshotFlatteningText, "FrameLedgerRecentEvents = captureTransportFlattening.FrameLedgerRecentEvents,");
        AssertContains(snapshotFlatteningCaptureTransportText, "private static CaptureTransportFlattenedProjection BuildCaptureTransportFlattenedProjection(");
        AssertContains(snapshotFlatteningCaptureTransportText, "MemoryPreference = captureTransport.MemoryPreference,");
        AssertContains(snapshotFlatteningCaptureTransportText, "VideoNegotiatedSubtype = captureTransport.VideoNegotiatedSubtype,");
        AssertContains(snapshotFlatteningCaptureTransportText, "FrameLedgerRecentEvents = captureTransport.FrameLedgerRecentEvents");
        AssertContains(snapshotFlatteningCaptureTransportText, "private readonly record struct CaptureTransportFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryPreference = captureRuntime.MemoryPreference,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryPreference = captureTransport.MemoryPreference,");
        AssertDoesNotContain(snapshotFlatteningText, "FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents,");
        AssertDoesNotContain(snapshotFlatteningText, "FrameLedgerRecentEvents = captureTransport.FrameLedgerRecentEvents,");

        AssertContains(captureTransportProjectionText, "private static CaptureTransportProjection BuildCaptureTransportProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(captureTransportProjectionText, "MemoryPreference = captureRuntime.MemoryPreference,");
        AssertContains(captureTransportProjectionText, "VideoNegotiatedSubtype = captureRuntime.VideoNegotiatedSubtype,");
        AssertContains(captureTransportProjectionText, "FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents");
        AssertContains(captureTransportProjectionText, "private readonly record struct CaptureTransportProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsHdrPipelineProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningHdrPipelineText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.HdrPipeline.cs")
            .Replace("\r\n", "\n");
        var hdrPipelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var hdrPipeline = BuildHdrPipelineProjection(viewModelSnapshot, captureRuntime, hdrTruthVerdict);");
        AssertContains(snapshotFlatteningText, "var hdrPipelineFlattening = BuildHdrPipelineFlattenedProjection(hdrPipeline);");
        AssertContains(snapshotFlatteningText, "IsHdrAvailable = hdrPipelineFlattening.IsHdrAvailable,");
        AssertContains(snapshotFlatteningText, "HdrRuntimeState = hdrPipelineFlattening.HdrRuntimeState,");
        AssertContains(snapshotFlatteningText, "HdrWarmupObservedNonP010Frames = hdrPipelineFlattening.HdrWarmupObservedNonP010Frames,");
        AssertContains(snapshotFlatteningText, "PipelineModeStatus = hdrPipelineFlattening.PipelineModeStatus,");
        AssertContains(snapshotFlatteningText, "TelemetryAlignmentReason = hdrPipelineFlattening.TelemetryAlignmentReason,");
        AssertContains(snapshotFlatteningText, "HdrTruthVerdict = hdrPipelineFlattening.TruthVerdict,");
        AssertContains(snapshotFlatteningHdrPipelineText, "private static HdrPipelineFlattenedProjection BuildHdrPipelineFlattenedProjection(");
        AssertContains(snapshotFlatteningHdrPipelineText, "IsHdrAvailable = hdrPipeline.IsHdrAvailable,");
        AssertContains(snapshotFlatteningHdrPipelineText, "HdrRuntimeState = hdrPipeline.HdrRuntimeState,");
        AssertContains(snapshotFlatteningHdrPipelineText, "HdrWarmupObservedNonP010Frames = hdrPipeline.HdrWarmupObservedNonP010Frames,");
        AssertContains(snapshotFlatteningHdrPipelineText, "PipelineModeStatus = hdrPipeline.PipelineModeStatus,");
        AssertContains(snapshotFlatteningHdrPipelineText, "TelemetryAlignmentReason = hdrPipeline.TelemetryAlignmentReason,");
        AssertContains(snapshotFlatteningHdrPipelineText, "TruthVerdict = hdrPipeline.TruthVerdict");
        AssertContains(snapshotFlatteningHdrPipelineText, "private readonly record struct HdrPipelineFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "IsHdrAvailable = viewModelSnapshot.IsHdrAvailable,");
        AssertDoesNotContain(snapshotFlatteningText, "IsHdrAvailable = hdrPipeline.IsHdrAvailable,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrRuntimeState = !string.IsNullOrWhiteSpace(viewModelSnapshot.HdrRuntimeState)");
        AssertDoesNotContain(snapshotFlatteningText, "HdrRuntimeState = hdrPipeline.HdrRuntimeState,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrWarmupObservedNonP010Frames = captureRuntime.HdrWarmupObservedNonP010Frames,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrWarmupObservedNonP010Frames = hdrPipeline.HdrWarmupObservedNonP010Frames,");
        AssertDoesNotContain(snapshotFlatteningText, "PipelineModeStatus = captureRuntime.PipelineModeStatus,");
        AssertDoesNotContain(snapshotFlatteningText, "PipelineModeStatus = hdrPipeline.PipelineModeStatus,");
        AssertDoesNotContain(snapshotFlatteningText, "TelemetryAlignmentReason = captureRuntime.TelemetryAlignmentReason,");
        AssertDoesNotContain(snapshotFlatteningText, "TelemetryAlignmentReason = hdrPipeline.TelemetryAlignmentReason,");
        AssertDoesNotContain(snapshotFlatteningText, "HdrTruthVerdict = hdrPipeline.TruthVerdict,");

        AssertContains(hdrPipelineProjectionText, "private static HdrPipelineProjection BuildHdrPipelineProjection(");
        AssertContains(hdrPipelineProjectionText, "IsHdrAvailable = viewModelSnapshot.IsHdrAvailable,");
        AssertContains(hdrPipelineProjectionText, "HdrRuntimeState = PreferViewModelHdrText(viewModelSnapshot.HdrRuntimeState, captureRuntime.HdrRuntimeState),");
        AssertContains(hdrPipelineProjectionText, "HdrWarmupObservedNonP010Frames = captureRuntime.HdrWarmupObservedNonP010Frames,");
        AssertContains(hdrPipelineProjectionText, "PipelineModeStatus = captureRuntime.PipelineModeStatus,");
        AssertContains(hdrPipelineProjectionText, "TelemetryAlignmentReason = captureRuntime.TelemetryAlignmentReason,");
        AssertContains(hdrPipelineProjectionText, "TruthVerdict = truthVerdict");
        AssertContains(hdrPipelineProjectionText, "private static string PreferViewModelHdrText(string viewModelValue, string runtimeValue)");
        AssertContains(hdrPipelineProjectionText, "private readonly record struct HdrPipelineProjection");

        return Task.CompletedTask;
    }
}
