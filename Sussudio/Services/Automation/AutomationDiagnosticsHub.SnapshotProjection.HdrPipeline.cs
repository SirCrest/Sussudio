using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static HdrPipelineProjection BuildHdrPipelineProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            IsHdrAvailable = viewModelSnapshot.IsHdrAvailable,
            IsHdrEnabled = viewModelSnapshot.IsHdrEnabled,
            HdrOutputActive = captureRuntime.HdrOutputActive,
            HdrRuntimeState = PreferViewModelHdrText(viewModelSnapshot.HdrRuntimeState, captureRuntime.HdrRuntimeState),
            HdrReadinessReason = PreferViewModelHdrText(viewModelSnapshot.HdrReadinessReason, captureRuntime.HdrReadinessReason),
            HdrWarmupState = captureRuntime.HdrWarmupState,
            HdrWarmupRequiredP010Frames = captureRuntime.HdrWarmupRequiredP010Frames,
            HdrWarmupAllowedNonP010Frames = captureRuntime.HdrWarmupAllowedNonP010Frames,
            HdrWarmupObservedP010Frames = captureRuntime.HdrWarmupObservedP010Frames,
            HdrWarmupObservedNonP010Frames = captureRuntime.HdrWarmupObservedNonP010Frames,
            HdrDowngradeCode = captureRuntime.HdrDowngradeCode,
            RequestedPipelineMode = captureRuntime.RequestedPipelineMode,
            ActivePipelineMode = captureRuntime.ActivePipelineMode,
            PipelineModeMatched = captureRuntime.PipelineModeMatched,
            PipelineModeStatus = captureRuntime.PipelineModeStatus,
            PipelineModeReason = captureRuntime.PipelineModeReason,
            TelemetryAlignmentStatus = captureRuntime.TelemetryAlignmentStatus,
            TelemetryAlignmentReason = captureRuntime.TelemetryAlignmentReason
        };

    private static string PreferViewModelHdrText(string viewModelValue, string runtimeValue)
        => !string.IsNullOrWhiteSpace(viewModelValue) ? viewModelValue : runtimeValue;

    private readonly record struct HdrPipelineProjection
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
    }
}
