using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingIntegritySummaryProjection BuildRecordingIntegritySummaryProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Status = captureRuntime.RecordingIntegrityStatus,
            Complete = captureRuntime.RecordingIntegrityComplete,
            Backend = captureRuntime.RecordingIntegrityBackend,
            CompletedUtc = captureRuntime.RecordingIntegrityCompletedUtc,
            Reason = captureRuntime.RecordingIntegrityReason
        };

    private static RecordingIntegritySummaryFlattenedProjection BuildRecordingIntegritySummaryFlattenedProjection(
        RecordingIntegritySummaryProjection summary)
        => new()
        {
            Status = summary.Status,
            Complete = summary.Complete,
            Backend = summary.Backend,
            CompletedUtc = summary.CompletedUtc,
            Reason = summary.Reason
        };

    private readonly record struct RecordingIntegritySummaryProjection
    {
        public string Status { get; init; }
        public bool Complete { get; init; }
        public string Backend { get; init; }
        public DateTimeOffset? CompletedUtc { get; init; }
        public string Reason { get; init; }
    }

    private readonly record struct RecordingIntegritySummaryFlattenedProjection
    {
        public string Status { get; init; }
        public bool Complete { get; init; }
        public string Backend { get; init; }
        public DateTimeOffset? CompletedUtc { get; init; }
        public string Reason { get; init; }
    }
}
