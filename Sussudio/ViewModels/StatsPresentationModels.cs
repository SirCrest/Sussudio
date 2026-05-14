using System;
using System.Collections.Generic;

namespace Sussudio.ViewModels;

internal sealed record StatsDockPresentation(
    string SessionState,
    string SummaryCapture,
    string SummaryPreview,
    string SummaryRendererFps,
    string SummaryVisualFps,
    string SummaryLatency,
    StatsMetricStatus SummaryCaptureStatus,
    StatsMetricStatus SummaryRendererFpsStatus,
    StatsMetricStatus SummaryVisualFpsStatus,
    StatsMetricStatus SummaryLatencyStatus,
    string SourceResolution,
    string SourceFrameRate,
    string SourceHdr,
    string SourceFormat,
    string TelemetryOrigin,
    string AdcOnOff,
    string AdcGain,
    string SourceFps,
    string SourceExpectedFps,
    string SourceAvg,
    string SourceP95,
    string SourceJitter,
    string SourceGaps,
    string SourceDrops,
    string PreviewFps,
    string PreviewAvg,
    string PreviewP95,
    string PreviewSlow,
    string VisualFps,
    string VisualMotion,
    StatsMetricStatus VisualFpsStatus,
    string PipelineLatency,
    string SourceDelivered,
    string SourceDropped,
    string RendererRendered,
    string RendererDropped,
    string PerformanceScore,
    string AvSyncDrift,
    string AvSyncDriftRate,
    bool EncoderDriftVisible,
    string EncoderDrift,
    bool EncoderActive,
    string EncoderCodec,
    string EncoderResolution,
    string EncoderFrameRate,
    string EncoderBitrate);

internal sealed record StatsFrameTimePresentation(
    string SourceText,
    string VisualText,
    string PreviewText,
    string LatencyText,
    string StatusText,
    StatsFrameTimeRange Range,
    IReadOnlyList<double> VisualSamples,
    IReadOnlyList<double> PreviewSamples);

internal readonly record struct StatsFrameTimeRange(
    double MinMs,
    double MaxMs,
    double ExpectedMs)
{
    public double SpanMs => Math.Max(0.001, MaxMs - MinMs);
}

internal sealed record StatsDiagnosticSummary(
    string HealthStatus,
    string LikelyStage,
    string Evidence);

internal sealed record StatsDiagnosticRowsPresentation(
    bool IsEmpty,
    IReadOnlyList<StatsDiagnosticRowPresentation> Rows);

internal sealed record StatsDiagnosticRowPresentation(
    string? GroupHeader,
    string Label,
    string Value,
    bool IsAlternate);

internal enum StatsMetricStatus
{
    Neutral,
    Good,
    Info,
    Warning,
    Bad
}
