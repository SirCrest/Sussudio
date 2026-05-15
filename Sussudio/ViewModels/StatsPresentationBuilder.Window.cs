using System;
using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.ViewModels;

internal static partial class StatsPresentationBuilder
{
    public static StatsWindowPresentation BuildStatsWindowPresentation(StatsSnapshot snapshot)
    {
        return new StatsWindowPresentation(
            SessionState: snapshot.Recording
                ? "Recording"
                : snapshot.Previewing
                    ? "Previewing"
                    : "Idle",
            DiagnosticStatus: string.IsNullOrWhiteSpace(snapshot.DiagnosticHealthStatus)
                ? "Unknown"
                : snapshot.DiagnosticHealthStatus,
            DiagnosticStage: string.IsNullOrWhiteSpace(snapshot.DiagnosticLikelyStage)
                ? "diagnostic_unavailable"
                : snapshot.DiagnosticLikelyStage,
            DiagnosticEvidence: string.IsNullOrWhiteSpace(snapshot.DiagnosticEvidence)
                ? snapshot.DiagnosticSummary ?? "Diagnostics are not available yet."
                : snapshot.DiagnosticEvidence,
            SourceResolution: snapshot.SourceWidth.HasValue && snapshot.SourceHeight.HasValue
                ? $"{snapshot.SourceWidth} x {snapshot.SourceHeight}"
                : "\u2014",
            SourceFrameRate: snapshot.SourceFrameRateExact.HasValue
                ? $"{snapshot.SourceFrameRateExact.Value:0.##} fps"
                : "\u2014",
            SourceHdr: FormatSourceHdr(snapshot.SourceIsHdr, snapshot.SourceColorimetry),
            SourceFormat: snapshot.SourceVideoFormat ?? "\u2014",
            TelemetryOrigin: snapshot.TelemetryOrigin is not null and not "Unknown"
                ? $"{snapshot.TelemetryOrigin} ({snapshot.TelemetryConfidence ?? "?"})"
                : "\u2014",
            SourceFps: FormatFps(snapshot.SourceObservedFps),
            SourceExpectedFps: FormatFps(snapshot.SourceExpectedFps),
            SourceAvg: $"{FormatMs(snapshot.SourceAvgIntervalMs)} avg",
            SourceP95: $"{FormatMs(snapshot.SourceP95IntervalMs)} P95",
            SourceJitter: FormatMs(snapshot.SourceJitterMs),
            SourceGaps: $"{FormatCount(snapshot.SourceSevereGaps)} severe",
            SourceDrops: $"{FormatCount(snapshot.SourceEstDrops)} drops ({FormatPercent(snapshot.SourceEstDropPct)})",
            PreviewFps: FormatFps(snapshot.PreviewObservedFps),
            PreviewAvg: $"{FormatMs(snapshot.PreviewAvgIntervalMs)} avg",
            PreviewP95: $"{FormatMs(snapshot.PreviewP95IntervalMs)} P95",
            PreviewSlow: $"{FormatCount(snapshot.PreviewSlowFrames)} frames ({FormatPercent(snapshot.PreviewSlowPct)})",
            PipelineLatency: $"{FormatMs(snapshot.PipelineLatencyMs)} avg",
            SourceDelivered: $"{FormatCount(snapshot.SourceFramesDelivered)} delivered",
            SourceDropped: $"{FormatCount(snapshot.SourceFramesDropped)} dropped",
            RendererRendered: $"{FormatCount(snapshot.RendererFramesRendered)} rendered",
            RendererDropped: $"{FormatCount(snapshot.RendererFramesDropped)} dropped",
            PerformanceScore: $"{FormatScore(snapshot.PerformanceScore)} / 100",
            TelemetryDetails: BuildStatsWindowTelemetryDetails(
                snapshot.SourceTelemetryDetails ?? Array.Empty<SourceTelemetryDetailEntry>(),
                snapshot.DiagnosticSummary));
    }

    private static StatsWindowTelemetryDetailsPresentation BuildStatsWindowTelemetryDetails(
        IReadOnlyList<SourceTelemetryDetailEntry> details,
        string? diagnosticSummary)
    {
        if (details.Count == 0)
        {
            var emptyText = string.IsNullOrWhiteSpace(diagnosticSummary)
                ? "No telemetry details available"
                : diagnosticSummary;
            return new StatsWindowTelemetryDetailsPresentation(
                IsEmpty: true,
                EmptyText: emptyText,
                Rows: Array.Empty<StatsWindowTelemetryDetailRowPresentation>());
        }

        var rows = new List<StatsWindowTelemetryDetailRowPresentation>();
        var currentGroup = string.Empty;
        foreach (var detail in details)
        {
            var groupHeader = string.Equals(currentGroup, detail.Group, StringComparison.Ordinal)
                ? null
                : detail.Group;
            if (groupHeader != null)
            {
                currentGroup = groupHeader;
            }

            rows.Add(new StatsWindowTelemetryDetailRowPresentation(
                GroupHeader: groupHeader,
                Label: detail.Label,
                Value: detail.DisplayValue));
        }

        return new StatsWindowTelemetryDetailsPresentation(
            IsEmpty: false,
            EmptyText: string.Empty,
            Rows: rows);
    }
}
