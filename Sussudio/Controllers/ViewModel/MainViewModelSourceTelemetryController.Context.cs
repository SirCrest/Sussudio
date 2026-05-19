using System;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed class MainViewModelSourceTelemetryControllerContext
    {
        public required Func<Action, bool> TryEnqueueOnUiThread { get; init; }
        public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetry { get; init; }
        public required Action<SourceSignalTelemetrySnapshot> SetLatestSourceTelemetry { get; init; }
        public required Action<int?> SetSourceWidth { get; init; }
        public required Action<int?> SetSourceHeight { get; init; }
        public required Action<bool?> SetSourceIsHdr { get; init; }
        public required Func<bool> IsRecording { get; init; }
        public required Func<bool> IsHdrEnabled { get; init; }
        public required Action<bool> SetIsHdrEnabled { get; init; }
        public required Action<string> SetSourceTelemetryAvailability { get; init; }
        public required Action<string> SetSourceTelemetryOriginDetail { get; init; }
        public required Action<string> SetSourceTelemetryConfidence { get; init; }
        public required Action<string?> SetSourceTelemetryDiagnosticSummary { get; init; }
        public required Func<DateTimeOffset?> GetSourceTelemetryTimestampUtc { get; init; }
        public required Action<DateTimeOffset?> SetSourceTelemetryTimestampUtc { get; init; }
        public required Action<double?> SetDetectedSourceFrameRate { get; init; }
        public required Action<string?> SetDetectedSourceFrameRateArg { get; init; }
        public required Action<string> SetSourceFrameRateOrigin { get; init; }
        public required Func<string> GetSourceTelemetrySummaryText { get; init; }
        public required Action<string> SetSourceTelemetrySummaryText { get; init; }
        public required Func<string?> GetLastSourceModeKey { get; init; }
        public required Action<string?> SetLastSourceModeKey { get; init; }
        public required Func<string?> GetSelectedResolution { get; init; }
        public required Func<bool> HasUserOverriddenResolutionForCurrentMode { get; init; }
        public required Action<bool> SetHasUserOverriddenResolutionForCurrentMode { get; init; }
        public required Func<bool> IsAutoFrameRateSelected { get; init; }
        public required Func<bool> HasUserOverriddenFrameRateForCurrentMode { get; init; }
        public required Action<bool> SetHasUserOverriddenFrameRateForCurrentMode { get; init; }
        public required Func<bool> ForceSourceAutoRetarget { get; init; }
        public required Action<bool> SetForceSourceAutoRetarget { get; init; }
        public required Func<int> AvailableResolutionCount { get; init; }
        public required Action<bool> SetPendingModeOptionsRefresh { get; init; }
        public required Action RebuildResolutionOptions { get; init; }
        public required Action UpdateTargetSummary { get; init; }
    }
}
