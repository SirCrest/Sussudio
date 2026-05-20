using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static HdrPipelineFlattenedProjection BuildHdrPipelineFlattenedProjection(
        HdrPipelineProjection hdrPipeline)
        => new()
        {
            IsHdrAvailable = hdrPipeline.IsHdrAvailable,
            IsHdrEnabled = hdrPipeline.IsHdrEnabled,
            HdrOutputActive = hdrPipeline.HdrOutputActive,
            HdrRuntimeState = hdrPipeline.HdrRuntimeState,
            HdrReadinessReason = hdrPipeline.HdrReadinessReason,
            HdrWarmupState = hdrPipeline.HdrWarmupState,
            HdrWarmupRequiredP010Frames = hdrPipeline.HdrWarmupRequiredP010Frames,
            HdrWarmupAllowedNonP010Frames = hdrPipeline.HdrWarmupAllowedNonP010Frames,
            HdrWarmupObservedP010Frames = hdrPipeline.HdrWarmupObservedP010Frames,
            HdrWarmupObservedNonP010Frames = hdrPipeline.HdrWarmupObservedNonP010Frames,
            HdrDowngradeCode = hdrPipeline.HdrDowngradeCode,
            RequestedPipelineMode = hdrPipeline.RequestedPipelineMode,
            ActivePipelineMode = hdrPipeline.ActivePipelineMode,
            PipelineModeMatched = hdrPipeline.PipelineModeMatched,
            PipelineModeStatus = hdrPipeline.PipelineModeStatus,
            PipelineModeReason = hdrPipeline.PipelineModeReason,
            TelemetryAlignmentStatus = hdrPipeline.TelemetryAlignmentStatus,
            TelemetryAlignmentReason = hdrPipeline.TelemetryAlignmentReason,
            TruthVerdict = hdrPipeline.TruthVerdict
        };

    private readonly record struct HdrPipelineFlattenedProjection
    {
        public bool IsHdrAvailable { get; init; }
        public bool IsHdrEnabled { get; init; }
        public bool HdrOutputActive { get; init; }
        public string HdrRuntimeState { get; init; }
        public string HdrReadinessReason { get; init; }
        public string HdrWarmupState { get; init; }
        public int HdrWarmupRequiredP010Frames { get; init; }
        public int HdrWarmupAllowedNonP010Frames { get; init; }
        public int HdrWarmupObservedP010Frames { get; init; }
        public int HdrWarmupObservedNonP010Frames { get; init; }
        public string HdrDowngradeCode { get; init; }
        public string RequestedPipelineMode { get; init; }
        public string ActivePipelineMode { get; init; }
        public bool PipelineModeMatched { get; init; }
        public string PipelineModeStatus { get; init; }
        public string PipelineModeReason { get; init; }
        public string TelemetryAlignmentStatus { get; init; }
        public string TelemetryAlignmentReason { get; init; }
        public HdrTruthVerdict TruthVerdict { get; init; }
    }
}
