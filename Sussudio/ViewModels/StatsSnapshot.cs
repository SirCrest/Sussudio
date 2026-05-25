using System;
using System.Collections.Generic;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio;

public sealed record StatsSnapshot(
    int SourceCadenceSamples,
    double SourceObservedFps,
    double SourceExpectedFps,
    double SourceAvgIntervalMs,
    double SourceP95IntervalMs,
    double SourceMaxIntervalMs,
    double SourceJitterMs,
    long SourceSevereGaps,
    long SourceEstDrops,
    double SourceEstDropPct,
    int PreviewCadenceSamples,
    double PreviewObservedFps,
    double PreviewAvgIntervalMs,
    double PreviewP95IntervalMs,
    double PreviewP99IntervalMs,
    double PreviewOnePercentLowFps,
    long PreviewSlowFrames,
    double PreviewSlowPct,
    int MjpegPacketHashSamples,
    double MjpegPacketHashInputFps,
    double MjpegPacketHashUniqueFps,
    double MjpegPacketHashDuplicatePercent,
    long MjpegPacketHashLongestDuplicateRun,
    string MjpegPacketHashPattern,
    bool MjpegPacketHashLastFrameDuplicate,
    int VisualCadenceSamples,
    double VisualCadenceOutputFps,
    double VisualCadenceChangeFps,
    double VisualCadenceRepeatPercent,
    long VisualCadenceRepeatFrames,
    long VisualCadenceLongestRepeatRun,
    double VisualCadenceMotionScore,
    string VisualCadenceMotionConfidence,
    int VisualCenterCadenceSamples,
    double VisualCenterCadenceOutputFps,
    double VisualCenterCadenceChangeFps,
    double VisualCenterCadenceRepeatPercent,
    double VisualCenterCadenceMotionScore,
    string VisualCenterCadenceMotionConfidence,
    double PipelineLatencyMs,
    long SourceFramesDelivered,
    long SourceFramesDropped,
    long RendererFramesSubmitted,
    long RendererFramesRendered,
    long RendererFramesDropped,
    double PerformanceScore,
    bool Previewing,
    bool Recording,
    int PreviewNaturalWidth = 0,
    int PreviewNaturalHeight = 0,
    int? CaptureWidth = null,
    int? CaptureHeight = null,
    double? CaptureFrameRate = null,
    int? SourceWidth = null,
    int? SourceHeight = null,
    double? SourceFrameRateExact = null,
    bool? SourceIsHdr = null,
    string? SourceVideoFormat = null,
    string? SourceColorimetry = null,
    string? ReaderSourceSubtype = null,
    string? NegotiatedPixelFormat = null,
    string? TelemetryOrigin = null,
    string? TelemetryConfidence = null,
    IReadOnlyList<SourceTelemetryDetailEntry>? SourceTelemetryDetails = null,
    string? DiagnosticSummary = null,
    string? DiagnosticHealthStatus = null,
    string? DiagnosticLikelyStage = null,
    string? DiagnosticEvidence = null,
    double? AvSyncCaptureDriftMs = null,
    double? AvSyncCaptureDriftRateMsPerSec = null,
    double? AvSyncEncoderDriftMs = null,
    long? AvSyncEncoderCorrectionSamples = null,
    string? EncoderCodecName = null,
    int EncoderWidth = 0,
    int EncoderHeight = 0,
    double EncoderFrameRate = 0,
    uint EncoderTargetBitRate = 0,
    IReadOnlyList<double>? MjpegPacketHashRecentUniqueIntervalsMs = null,
    IReadOnlyList<double>? VisualCadenceRecentChangeIntervalsMs = null,
    IReadOnlyList<double>? VisualCenterCadenceRecentChangeIntervalsMs = null,
    IReadOnlyList<double>? PreviewRecentPresentIntervalsMs = null,
    IReadOnlyList<double>? PreviewRecentLatencyMs = null);

internal static class StatsSnapshotBuilder
{
    public static StatsSnapshot Build(
        CaptureHealthSnapshot health,
        StatsSnapshotRenderMetrics renderer,
        StatsSnapshotViewState viewState)
    {
        ArgumentNullException.ThrowIfNull(health);

        var sourceDropPercent = StatsPresentationBuilder.Sanitize(health.CaptureCadenceEstimatedDropPercent);
        var previewSlowPercent = StatsPresentationBuilder.Sanitize(renderer.PreviewSlowPercent);
        var performanceScore = Math.Clamp(100.0 - sourceDropPercent - previewSlowPercent, 0.0, 100.0);
        var diagnostic = StatsPresentationBuilder.BuildStatsDiagnosticSummary(
            health,
            viewState.IsPreviewing,
            viewState.IsRecording,
            sourceDropPercent,
            previewSlowPercent,
            renderer.FramesSubmitted,
            renderer.FramesDropped,
            renderer.PreviewCadenceSamples);

        var telemetryDetails = new List<SourceTelemetryDetailEntry>(health.SourceTelemetryDetails);
        var captureCardFormat = health.ReaderSourceSubtype ?? health.NegotiatedPixelFormat;
        if (!string.IsNullOrWhiteSpace(captureCardFormat))
        {
            telemetryDetails.Add(new SourceTelemetryDetailEntry("Capture Card / UVC", "Capture Format", captureCardFormat));
        }

        return new StatsSnapshot(
            SourceCadenceSamples: health.CaptureCadenceSampleCount,
            SourceObservedFps: StatsPresentationBuilder.Sanitize(health.CaptureCadenceObservedFps),
            SourceExpectedFps: StatsPresentationBuilder.Sanitize(health.ExpectedFrameRate),
            SourceAvgIntervalMs: StatsPresentationBuilder.Sanitize(health.CaptureCadenceAverageIntervalMs),
            SourceP95IntervalMs: StatsPresentationBuilder.Sanitize(health.CaptureCadenceP95IntervalMs),
            SourceMaxIntervalMs: StatsPresentationBuilder.Sanitize(health.CaptureCadenceMaxIntervalMs),
            SourceJitterMs: StatsPresentationBuilder.Sanitize(health.CaptureCadenceJitterStdDevMs),
            SourceSevereGaps: health.CaptureCadenceSevereGapCount,
            SourceEstDrops: health.CaptureCadenceEstimatedDroppedFrames,
            SourceEstDropPct: sourceDropPercent,
            PreviewCadenceSamples: renderer.PreviewCadenceSamples,
            PreviewObservedFps: StatsPresentationBuilder.Sanitize(renderer.PreviewObservedFps),
            PreviewAvgIntervalMs: StatsPresentationBuilder.Sanitize(renderer.PreviewAvgIntervalMs),
            PreviewP95IntervalMs: StatsPresentationBuilder.Sanitize(renderer.PreviewP95IntervalMs),
            PreviewP99IntervalMs: StatsPresentationBuilder.Sanitize(renderer.PreviewP99IntervalMs),
            PreviewOnePercentLowFps: StatsPresentationBuilder.Sanitize(renderer.PreviewOnePercentLowFps),
            PreviewSlowFrames: renderer.PreviewSlowFrames,
            PreviewSlowPct: previewSlowPercent,
            MjpegPacketHashSamples: health.MjpegPacketHashSampleCount,
            MjpegPacketHashInputFps: StatsPresentationBuilder.Sanitize(health.MjpegPacketHashInputObservedFps),
            MjpegPacketHashUniqueFps: StatsPresentationBuilder.Sanitize(health.MjpegPacketHashUniqueObservedFps),
            MjpegPacketHashDuplicatePercent: StatsPresentationBuilder.Sanitize(health.MjpegPacketHashDuplicateFramePercent),
            MjpegPacketHashLongestDuplicateRun: health.MjpegPacketHashLongestDuplicateRun,
            MjpegPacketHashPattern: health.MjpegPacketHashPattern,
            MjpegPacketHashLastFrameDuplicate: health.MjpegPacketHashLastFrameDuplicate,
            VisualCadenceSamples: health.VisualCadenceSampleCount,
            VisualCadenceOutputFps: StatsPresentationBuilder.Sanitize(health.VisualCadenceOutputObservedFps),
            VisualCadenceChangeFps: StatsPresentationBuilder.Sanitize(health.VisualCadenceChangeObservedFps),
            VisualCadenceRepeatPercent: StatsPresentationBuilder.Sanitize(health.VisualCadenceRepeatFramePercent),
            VisualCadenceRepeatFrames: health.VisualCadenceRepeatFrameCount,
            VisualCadenceLongestRepeatRun: health.VisualCadenceLongestRepeatRun,
            VisualCadenceMotionScore: StatsPresentationBuilder.Sanitize(health.VisualCadenceMotionScore),
            VisualCadenceMotionConfidence: health.VisualCadenceMotionConfidence,
            VisualCenterCadenceSamples: health.VisualCenterCadenceSampleCount,
            VisualCenterCadenceOutputFps: StatsPresentationBuilder.Sanitize(health.VisualCenterCadenceOutputObservedFps),
            VisualCenterCadenceChangeFps: StatsPresentationBuilder.Sanitize(health.VisualCenterCadenceChangeObservedFps),
            VisualCenterCadenceRepeatPercent: StatsPresentationBuilder.Sanitize(health.VisualCenterCadenceRepeatFramePercent),
            VisualCenterCadenceMotionScore: StatsPresentationBuilder.Sanitize(health.VisualCenterCadenceMotionScore),
            VisualCenterCadenceMotionConfidence: health.VisualCenterCadenceMotionConfidence,
            PipelineLatencyMs: StatsPresentationBuilder.Sanitize(renderer.PipelineLatencyMs),
            SourceFramesDelivered: health.VideoFramesArrived,
            SourceFramesDropped: health.VideoFramesDropped,
            RendererFramesSubmitted: renderer.FramesSubmitted,
            RendererFramesRendered: renderer.FramesRendered,
            RendererFramesDropped: renderer.FramesDropped,
            PerformanceScore: performanceScore,
            Previewing: viewState.IsPreviewing,
            Recording: viewState.IsRecording,
            PreviewNaturalWidth: renderer.PreviewNaturalWidth,
            PreviewNaturalHeight: renderer.PreviewNaturalHeight,
            CaptureWidth: ToPositiveInt(health.NegotiatedWidth),
            CaptureHeight: ToPositiveInt(health.NegotiatedHeight),
            CaptureFrameRate: health.NegotiatedFrameRate,
            SourceWidth: health.SourceWidth,
            SourceHeight: health.SourceHeight,
            SourceFrameRateExact: health.SourceFrameRateExact,
            SourceIsHdr: health.SourceIsHdr,
            SourceVideoFormat: health.SourceVideoFormat,
            SourceColorimetry: health.SourceColorimetry,
            ReaderSourceSubtype: health.ReaderSourceSubtype,
            NegotiatedPixelFormat: health.NegotiatedPixelFormat,
            TelemetryOrigin: health.SourceTelemetryOrigin.ToString(),
            TelemetryConfidence: health.SourceTelemetryConfidence.ToString(),
            SourceTelemetryDetails: telemetryDetails,
            DiagnosticSummary: health.SourceTelemetryDiagnosticSummary,
            DiagnosticHealthStatus: diagnostic.HealthStatus,
            DiagnosticLikelyStage: diagnostic.LikelyStage,
            DiagnosticEvidence: diagnostic.Evidence,
            AvSyncCaptureDriftMs: health.AvSyncCaptureDriftMs,
            AvSyncCaptureDriftRateMsPerSec: health.AvSyncCaptureDriftRateMsPerSec,
            AvSyncEncoderDriftMs: health.AvSyncEncoderDriftMs,
            AvSyncEncoderCorrectionSamples: health.AvSyncEncoderCorrectionSamples,
            EncoderCodecName: health.EncoderCodecName,
            EncoderWidth: health.EncoderWidth,
            EncoderHeight: health.EncoderHeight,
            EncoderFrameRate: health.EncoderFrameRate,
            EncoderTargetBitRate: health.EncoderTargetBitRate,
            MjpegPacketHashRecentUniqueIntervalsMs: health.MjpegPacketHashRecentUniqueIntervalsMs ?? Array.Empty<double>(),
            VisualCadenceRecentChangeIntervalsMs: health.VisualCadenceRecentChangeIntervalsMs ?? Array.Empty<double>(),
            VisualCenterCadenceRecentChangeIntervalsMs: health.VisualCenterCadenceRecentChangeIntervalsMs ?? Array.Empty<double>(),
            PreviewRecentPresentIntervalsMs: renderer.PreviewRecentPresentIntervalsMs ?? Array.Empty<double>(),
            PreviewRecentLatencyMs: renderer.PreviewRecentLatencyMs ?? Array.Empty<double>());
    }

    private static int? ToPositiveInt(uint? value)
    {
        if (!value.HasValue || value.Value == 0 || value.Value > int.MaxValue)
        {
            return null;
        }

        return (int)value.Value;
    }
}

internal readonly record struct StatsSnapshotViewState(
    bool IsPreviewing,
    bool IsRecording);

internal readonly record struct StatsSnapshotRenderMetrics(
    int PreviewCadenceSamples,
    double PreviewObservedFps,
    double PreviewAvgIntervalMs,
    double PreviewP95IntervalMs,
    double PreviewP99IntervalMs,
    double PreviewOnePercentLowFps,
    long PreviewSlowFrames,
    double PreviewSlowPercent,
    double PipelineLatencyMs,
    long FramesSubmitted,
    long FramesRendered,
    long FramesDropped,
    int PreviewNaturalWidth,
    int PreviewNaturalHeight,
    IReadOnlyList<double>? PreviewRecentPresentIntervalsMs,
    IReadOnlyList<double>? PreviewRecentLatencyMs);
