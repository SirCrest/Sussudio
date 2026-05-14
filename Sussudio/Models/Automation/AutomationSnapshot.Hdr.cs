namespace Sussudio.Models;

public sealed partial class AutomationSnapshot
{
    public bool IsHdrAvailable { get; init; }
    public bool IsHdrEnabled { get; init; }
    public bool HdrOutputActive { get; init; }
    public string HdrRuntimeState { get; init; } = "Inactive";
    public string HdrReadinessReason { get; init; } = string.Empty;
    public string HdrWarmupState { get; init; } = "NotStarted";
    public int HdrWarmupRequiredP010Frames { get; init; }
    public int HdrWarmupAllowedNonP010Frames { get; init; }
    public int HdrWarmupObservedP010Frames { get; init; }
    public int HdrWarmupObservedNonP010Frames { get; init; }
    public string HdrDowngradeCode { get; init; } = string.Empty;
    public string RequestedPipelineMode { get; init; } = "SDR";
    public string ActivePipelineMode { get; init; } = "SDR";
    public bool PipelineModeMatched { get; init; } = true;
    public string PipelineModeStatus { get; init; } = "Ready";
    public string PipelineModeReason { get; init; } = string.Empty;
    public string TelemetryAlignmentStatus { get; init; } = "Unknown";
    public string TelemetryAlignmentReason { get; init; } = string.Empty;

    public HdrTruthVerdict? HdrTruthVerdict { get; init; }
}
