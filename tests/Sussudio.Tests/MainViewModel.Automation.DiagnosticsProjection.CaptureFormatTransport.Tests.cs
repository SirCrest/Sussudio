using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsCaptureFormatProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var captureFormatProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureFormat = BuildCaptureFormatProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var captureFormatFlattening = BuildCaptureFormatFlattenedProjection(captureFormat);");
        AssertContains(snapshotFlatteningText, "RequestedWidth = captureFormatFlattening.Requested.Width,");
        AssertContains(snapshotFlatteningText, "HdrActivationReason = captureFormatFlattening.HdrRequest.ActivationReason,");
        AssertContains(snapshotFlatteningText, "NegotiatedWidth = captureFormatFlattening.Negotiated.Width,");
        AssertContains(snapshotFlatteningText, "LatestObservedFramePixelFormat = captureFormatFlattening.ReaderObservation.LatestObservedFramePixelFormat,");
        AssertContains(snapshotFlatteningText, "EncoderVideoCodec = captureFormatFlattening.Encoder.VideoCodec,");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatFlattenedProjection BuildCaptureFormatFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "Requested = BuildCaptureFormatRequestedFlattenedProjection(captureFormat),");
        AssertContains(captureFormatProjectionText, "HdrRequest = BuildCaptureFormatHdrRequestFlattenedProjection(captureFormat),");
        AssertContains(captureFormatProjectionText, "Negotiated = BuildCaptureFormatNegotiatedFlattenedProjection(captureFormat),");
        AssertContains(captureFormatProjectionText, "ReaderObservation = BuildCaptureFormatReaderObservationFlattenedProjection(captureFormat),");
        AssertContains(captureFormatProjectionText, "Encoder = BuildCaptureFormatEncoderFlattenedProjection(captureFormat)");
        AssertContains(captureFormatProjectionText, "private readonly record struct CaptureFormatFlattenedProjection");

        AssertContains(captureFormatProjectionText, "private static CaptureFormatRequestedProjection BuildCaptureFormatRequestedProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureRuntime.RequestedWidth,");
        AssertContains(captureFormatProjectionText, "AudioEnabled = captureRuntime.RequestedAudioEnabled");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatHdrRequestProjection BuildCaptureFormatHdrRequestProjection(");
        AssertContains(captureFormatProjectionText, "ActivationReason = captureRuntime.HdrActivationReason,");
        AssertContains(captureFormatProjectionText, "RequestedButSourceNot10Bit = captureRuntime.HdrRequestedButSourceNot10Bit");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatActualProjection BuildCaptureFormatActualProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureRuntime.ActualWidth,");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatNegotiatedProjection BuildCaptureFormatNegotiatedProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,");
        AssertContains(captureFormatProjectionText, "MediaSubtypeToken = captureRuntime.NegotiatedMediaSubtypeToken");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatReaderObservationProjection BuildCaptureFormatReaderObservationProjection(");
        AssertContains(captureFormatProjectionText, "LatestObservedFramePixelFormat = captureRuntime.LatestObservedFramePixelFormat,");
        AssertContains(captureFormatProjectionText, "MfReadwriteDisableConverters = captureRuntime.MfReadwriteDisableConverters");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatEncoderProjection BuildCaptureFormatEncoderProjection(");
        AssertContains(captureFormatProjectionText, "VideoCodec = captureRuntime.EncoderVideoCodec,");
        AssertContains(captureFormatProjectionText, "TenBitPipelineConfirmed = captureRuntime.EncoderTenBitPipelineConfirmed");

        AssertContains(captureFormatProjectionText, "private static CaptureFormatRequestedFlattenedProjection BuildCaptureFormatRequestedFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureFormat.Requested.Width,");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatHdrRequestFlattenedProjection BuildCaptureFormatHdrRequestFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "ActivationReason = captureFormat.HdrRequest.ActivationReason,");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatActualFlattenedProjection BuildCaptureFormatActualFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureFormat.Actual.Width,");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatNegotiatedFlattenedProjection BuildCaptureFormatNegotiatedFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "Width = captureFormat.Negotiated.Width,");
        AssertContains(captureFormatProjectionText, "MediaSubtypeToken = captureFormat.Negotiated.MediaSubtypeToken");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatReaderObservationFlattenedProjection BuildCaptureFormatReaderObservationFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "LatestObservedFramePixelFormat = captureFormat.ReaderObservation.LatestObservedFramePixelFormat,");
        AssertContains(captureFormatProjectionText, "MfReadwriteDisableConverters = captureFormat.ReaderObservation.MfReadwriteDisableConverters");
        AssertContains(captureFormatProjectionText, "private static CaptureFormatEncoderFlattenedProjection BuildCaptureFormatEncoderFlattenedProjection(");
        AssertContains(captureFormatProjectionText, "VideoCodec = captureFormat.Encoder.VideoCodec,");
        AssertContains(captureFormatProjectionText, "TenBitPipelineConfirmed = captureFormat.Encoder.TenBitPipelineConfirmed");
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
        AssertContains(captureFormatProjectionText, "Requested = BuildCaptureFormatRequestedProjection(captureRuntime),");
        AssertContains(captureFormatProjectionText, "HdrRequest = BuildCaptureFormatHdrRequestProjection(captureRuntime),");
        AssertContains(captureFormatProjectionText, "Actual = BuildCaptureFormatActualProjection(captureRuntime),");
        AssertContains(captureFormatProjectionText, "Negotiated = BuildCaptureFormatNegotiatedProjection(captureRuntime),");
        AssertContains(captureFormatProjectionText, "ReaderObservation = BuildCaptureFormatReaderObservationProjection(captureRuntime),");
        AssertContains(captureFormatProjectionText, "Encoder = BuildCaptureFormatEncoderProjection(captureRuntime)");
        AssertContains(captureFormatProjectionText, "public CaptureFormatRequestedProjection Requested { get; init; }");
        AssertContains(captureFormatProjectionText, "public CaptureFormatEncoderProjection Encoder { get; init; }");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsCaptureTransportProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var captureTransportProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureTransport = BuildCaptureTransportProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var captureTransportFlattening = BuildCaptureTransportFlattenedProjection(captureTransport);");
        AssertContains(snapshotFlatteningText, "MemoryPreference = captureTransportFlattening.MemoryPreference,");
        AssertContains(snapshotFlatteningText, "VideoNegotiatedSubtype = captureTransportFlattening.VideoNegotiatedSubtype,");
        AssertContains(snapshotFlatteningText, "FrameLedgerRecentEvents = captureTransportFlattening.FrameLedgerRecentEvents,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryPreference = captureRuntime.MemoryPreference,");
        AssertDoesNotContain(snapshotFlatteningText, "MemoryPreference = captureTransport.MemoryPreference,");
        AssertDoesNotContain(snapshotFlatteningText, "FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents,");
        AssertDoesNotContain(snapshotFlatteningText, "FrameLedgerRecentEvents = captureTransport.FrameLedgerRecentEvents,");

        AssertContains(captureTransportProjectionText, "private static CaptureTransportProjection BuildCaptureTransportProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(captureTransportProjectionText, "MemoryPreference = captureRuntime.MemoryPreference,");
        AssertContains(captureTransportProjectionText, "VideoNegotiatedSubtype = captureRuntime.VideoNegotiatedSubtype,");
        AssertContains(captureTransportProjectionText, "FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents");
        AssertContains(captureTransportProjectionText, "private readonly record struct CaptureTransportProjection");
        AssertContains(captureTransportProjectionText, "private static CaptureTransportFlattenedProjection BuildCaptureTransportFlattenedProjection(");
        AssertContains(captureTransportProjectionText, "MemoryPreference = captureTransport.MemoryPreference,");
        AssertContains(captureTransportProjectionText, "VideoNegotiatedSubtype = captureTransport.VideoNegotiatedSubtype,");
        AssertContains(captureTransportProjectionText, "FrameLedgerRecentEvents = captureTransport.FrameLedgerRecentEvents");
        AssertContains(captureTransportProjectionText, "private readonly record struct CaptureTransportFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsHdrPipelineProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
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
        AssertContains(hdrPipelineProjectionText, "private static HdrPipelineFlattenedProjection BuildHdrPipelineFlattenedProjection(");
        AssertContains(hdrPipelineProjectionText, "IsHdrAvailable = hdrPipeline.IsHdrAvailable,");
        AssertContains(hdrPipelineProjectionText, "HdrRuntimeState = hdrPipeline.HdrRuntimeState,");
        AssertContains(hdrPipelineProjectionText, "HdrWarmupObservedNonP010Frames = hdrPipeline.HdrWarmupObservedNonP010Frames,");
        AssertContains(hdrPipelineProjectionText, "PipelineModeStatus = hdrPipeline.PipelineModeStatus,");
        AssertContains(hdrPipelineProjectionText, "TelemetryAlignmentReason = hdrPipeline.TelemetryAlignmentReason,");
        AssertContains(hdrPipelineProjectionText, "TruthVerdict = hdrPipeline.TruthVerdict");
        AssertContains(hdrPipelineProjectionText, "private readonly record struct HdrPipelineFlattenedProjection");
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
