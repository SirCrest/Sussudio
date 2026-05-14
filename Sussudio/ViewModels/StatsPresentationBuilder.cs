using System;
using Sussudio.Models;
using Sussudio.Services.Automation;

namespace Sussudio.ViewModels;

internal static partial class StatsPresentationBuilder
{
    private const double VisualRepeatTolerancePercent = 0.25;

    public static StatsDockPresentation BuildDockPresentation(StatsSnapshot snapshot)
    {
        var sourceFps = FormatFps(snapshot.SourceObservedFps);
        var sourceExpectedFps = FormatFps(snapshot.SourceExpectedFps);
        var sourceAvg = $"{FormatMs(snapshot.SourceAvgIntervalMs)} avg";
        var sourceP95 = $"{FormatMs(snapshot.SourceP95IntervalMs)} P95";
        var sourceJitter = FormatMs(snapshot.SourceJitterMs);
        var sourceGaps = $"{FormatCount(snapshot.SourceSevereGaps)} severe";
        var sourceDrops = $"{FormatCount(snapshot.SourceEstDrops)} drops ({FormatPercent(snapshot.SourceEstDropPct)})";
        var previewFps = FormatFps(snapshot.PreviewObservedFps);
        var previewAvg = $"{FormatMs(snapshot.PreviewAvgIntervalMs)} avg";
        var previewP95 = $"{FormatMs(snapshot.PreviewP95IntervalMs)} P95";
        var previewSlow = $"{FormatCount(snapshot.PreviewSlowFrames)} frames ({FormatPercent(snapshot.PreviewSlowPct)})";
        var visualFps = snapshot.VisualCadenceSamples <= 0
            ? "\u2014"
            : $"crop {FormatVisualCadenceSummary(snapshot)}";
        var visualMotion = snapshot.VisualCadenceSamples <= 0
            ? "NoSamples"
            : FormatVisualMotionSummary(snapshot);
        var pipelineLatency = $"{FormatMs(snapshot.PipelineLatencyMs)} avg";
        var sourceDelivered = $"{FormatCount(snapshot.SourceFramesDelivered)} delivered";
        var sourceDropped = $"{FormatCount(snapshot.SourceFramesDropped)} dropped";
        var rendererRendered = $"{FormatCount(snapshot.RendererFramesRendered)} rendered";
        var rendererDropped = $"{FormatCount(snapshot.RendererFramesDropped)} dropped";
        var perfScore = $"{FormatScore(snapshot.PerformanceScore)} / 100";
        var sourceResolution = snapshot.SourceWidth.HasValue && snapshot.SourceHeight.HasValue
            ? $"{snapshot.SourceWidth} x {snapshot.SourceHeight}"
            : "\u2014";
        var previewResolution = ResolvePreviewResolutionText(snapshot);
        var previewFrameTimeSummary = FormatPreviewCadenceSummary(snapshot);
        var visualFpsSummary = FormatVisualRepeatSummary(snapshot);
        var captureSummary = ResolveCaptureSummaryText(snapshot);
        var latencySummary = $"{FormatMs(snapshot.PipelineLatencyMs)} avg";
        var sourceFrameRate = snapshot.SourceFrameRateExact.HasValue
            ? $"{snapshot.SourceFrameRateExact.Value:0.##} fps"
            : "\u2014";
        var sourceHdr = FormatSourceHdr(snapshot.SourceIsHdr, snapshot.SourceColorimetry);
        var sourceFormat = snapshot.SourceVideoFormat ?? "\u2014";
        var telemetryOrigin = snapshot.TelemetryOrigin is not null and not "Unknown"
            ? $"{snapshot.TelemetryOrigin} ({snapshot.TelemetryConfidence ?? "?"})"
            : "\u2014";

        var adcOnOff = "\u2014";
        var adcGain = "\u2014";
        if (snapshot.SourceTelemetryDetails is { } details)
        {
            foreach (var detail in details)
            {
                if (detail.Label == TelemetryLabels.AdcAnalog)
                {
                    adcOnOff = detail.DisplayValue;
                }
                else if (detail.Label == TelemetryLabels.AnalogGain)
                {
                    adcGain = detail.DisplayValue;
                }
            }
}
        var encoderVisible = snapshot.Recording && snapshot.AvSyncEncoderDriftMs.HasValue;
        var encoderActive = !string.IsNullOrEmpty(snapshot.EncoderCodecName);
        var encoderCodec = string.Empty;
        var encoderResolution = string.Empty;
        var encoderFrameRate = string.Empty;
        var encoderBitrate = string.Empty;
        if (encoderActive)
        {
            encoderCodec = snapshot.EncoderCodecName switch
            {
                "hevc_nvenc" => "HEVC (NVENC)",
                "h264_nvenc" => "H.264 (NVENC)",
                "av1_nvenc" => "AV1 (NVENC)",
                _ => snapshot.EncoderCodecName!
            };
            var mbps = snapshot.EncoderTargetBitRate / 1_000_000.0;
            encoderResolution = $"{snapshot.EncoderWidth} x {snapshot.EncoderHeight}";
            encoderFrameRate = $"{snapshot.EncoderFrameRate:0.##} fps";
            encoderBitrate = $"{mbps:0.#} Mbps";
        }

        return new StatsDockPresentation(
            SessionState: snapshot.Recording ? "Recording" : snapshot.Previewing ? "Previewing" : "Idle",
            SummaryCapture: captureSummary,
            SummaryPreview: previewResolution,
            SummaryRendererFps: previewFrameTimeSummary,
            SummaryVisualFps: visualFpsSummary,
            SummaryLatency: latencySummary,
            SummaryCaptureStatus: ResolveFrameLaneStatus(snapshot.SourceP95IntervalMs, snapshot.SourceExpectedFps, snapshot.SourceEstDropPct),
            SummaryRendererFpsStatus: ResolvePreviewFrameLaneStatus(snapshot),
            SummaryVisualFpsStatus: ResolveDecodedVisualStatus(snapshot),
            SummaryLatencyStatus: ResolveLatencyStatus(snapshot.PipelineLatencyMs),
            SourceResolution: sourceResolution,
            SourceFrameRate: sourceFrameRate,
            SourceHdr: sourceHdr,
            SourceFormat: sourceFormat,
            TelemetryOrigin: telemetryOrigin,
            AdcOnOff: adcOnOff,
            AdcGain: adcGain,
            SourceFps: sourceFps,
            SourceExpectedFps: sourceExpectedFps,
            SourceAvg: sourceAvg,
            SourceP95: sourceP95,
            SourceJitter: sourceJitter,
            SourceGaps: sourceGaps,
            SourceDrops: sourceDrops,
            PreviewFps: previewFps,
            PreviewAvg: previewAvg,
            PreviewP95: previewP95,
            PreviewSlow: previewSlow,
            VisualFps: visualFps,
            VisualMotion: visualMotion,
            VisualFpsStatus: ResolveDecodedVisualStatus(snapshot),
            PipelineLatency: pipelineLatency,
            SourceDelivered: sourceDelivered,
            SourceDropped: sourceDropped,
            RendererRendered: rendererRendered,
            RendererDropped: rendererDropped,
            PerformanceScore: perfScore,
            AvSyncDrift: FormatSignedMs(snapshot.AvSyncCaptureDriftMs),
            AvSyncDriftRate: FormatSignedMsPerSec(snapshot.AvSyncCaptureDriftRateMsPerSec),
            EncoderDriftVisible: encoderVisible,
            EncoderDrift: encoderVisible
                ? $"{FormatSignedMs(snapshot.AvSyncEncoderDriftMs)} ({snapshot.AvSyncEncoderCorrectionSamples ?? 0} corr)"
                : string.Empty,
            EncoderActive: encoderActive,
            EncoderCodec: encoderCodec,
            EncoderResolution: encoderResolution,
            EncoderFrameRate: encoderFrameRate,
            EncoderBitrate: encoderBitrate);
    }

    public static StatsFrameTimePresentation BuildFrameTimePresentation(StatsSnapshot snapshot)
    {
        var range = ResolveFrameTimeRange(snapshot.SourceExpectedFps);
        return new StatsFrameTimePresentation(
            SourceText: $"Src {FormatMs(snapshot.SourceP95IntervalMs)} P95 / {FormatMs(snapshot.SourceAvgIntervalMs)} avg",
            VisualText: snapshot.VisualCadenceSamples <= 0
                ? "Crop \u2014"
                : $"Crop {FormatVisualCadenceSummary(snapshot)}",
            PreviewText: $"Preview: {FormatPreviewCadenceSummary(snapshot)}",
            LatencyText: $"Lat {FormatMs(snapshot.PipelineLatencyMs)}",
            StatusText: $"Target {FormatMs(range.ExpectedMs)} | blue=crop changes; green=preview presents | range {FormatMs(range.MinMs)}-{FormatMs(range.MaxMs)}",
            Range: range,
            VisualSamples: snapshot.VisualCadenceRecentChangeIntervalsMs ?? Array.Empty<double>(),
            PreviewSamples: snapshot.PreviewRecentPresentIntervalsMs ?? Array.Empty<double>());
    }

    public static StatsFrameTimeRange ResolveFrameTimeRange(double expectedFps)
    {
        var fps = expectedFps > 0 ? expectedFps : 60.0;
        var lowerFps = Math.Max(1.0, fps * 0.75);
        var upperFps = Math.Max(lowerFps + 1.0, fps * 1.25);
        var minMs = 1000.0 / upperFps;
        var maxMs = 1000.0 / lowerFps;
        return new StatsFrameTimeRange(
            MinMs: minMs,
            MaxMs: maxMs,
            ExpectedMs: 1000.0 / fps);
    }

    public static string FormatMs(double value)
    {
        return $"{Sanitize(value):0.00}ms";
    }

    public static double Sanitize(double value)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            return 0;
        }

        return value;
    }

    private static string ResolvePreviewResolutionText(StatsSnapshot snapshot)
    {
        if (snapshot.PreviewNaturalWidth > 0 && snapshot.PreviewNaturalHeight > 0)
        {
            return $"{snapshot.PreviewNaturalWidth} x {snapshot.PreviewNaturalHeight}";
        }

        // The renderer's natural size is the best live-preview answer. If it has not
        // reported yet, the negotiated capture mode is closer than HDMI source timing:
        // a 4K input can legitimately be captured and previewed through a 1080p path.
        if (snapshot.CaptureWidth is > 0 && snapshot.CaptureHeight is > 0)
        {
            return $"{snapshot.CaptureWidth.Value} x {snapshot.CaptureHeight.Value}";
        }

        if (snapshot.SourceWidth.HasValue && snapshot.SourceHeight.HasValue)
        {
            return $"{snapshot.SourceWidth} x {snapshot.SourceHeight}";
        }

        return "\u2014";
    }

    private static string ResolveCaptureSummaryText(StatsSnapshot snapshot)
    {
        // Source telemetry is the HDMI signal entering the card; the compact Capture
        // row should show the negotiated UVC/SourceReader mode the app is consuming.
        if (snapshot.CaptureWidth is not > 0 || snapshot.CaptureHeight is not > 0)
        {
            return "\u2014";
        }

        var frameRate = Sanitize(snapshot.CaptureFrameRate ?? 0);
        var frameRateText = frameRate > 0 ? $" @ {frameRate:0.##} fps" : string.Empty;
        return $"{snapshot.CaptureWidth.Value} x {snapshot.CaptureHeight.Value}{frameRateText}";
    }

    private static StatsMetricStatus ResolveDropStatus(double dropPercent)
        => dropPercent <= 0.01 ? StatsMetricStatus.Good :
           dropPercent <= 0.25 ? StatsMetricStatus.Warning :
           StatsMetricStatus.Bad;

    private static StatsMetricStatus ResolveFpsStatus(double observedFps, double expectedFps)
    {
        if (observedFps <= 0)
        {
            return StatsMetricStatus.Neutral;
        }

        if (expectedFps <= 0)
        {
            return StatsMetricStatus.Info;
        }

        var ratio = observedFps / expectedFps;
        return ratio >= 0.985 ? StatsMetricStatus.Good :
               ratio >= 0.95 ? StatsMetricStatus.Warning :
               StatsMetricStatus.Bad;
    }

    private static StatsMetricStatus ResolveFrameLaneStatus(double p95IntervalMs, double expectedFps, double issuePercent)
    {
        if (p95IntervalMs <= 0 && issuePercent <= 0.01)
        {
            return StatsMetricStatus.Neutral;
        }

        var timingStatus = ResolveFrameTimeStatus(p95IntervalMs, expectedFps);
        var issueStatus = ResolveDropStatus(issuePercent);
        return ResolveWorstStatus(timingStatus, issueStatus);
    }

    private static StatsMetricStatus ResolvePreviewFrameLaneStatus(StatsSnapshot snapshot)
    {
        var currentFrameTimeMs = ResolveCurrentPreviewFrameTimeMs(snapshot);
        if (currentFrameTimeMs <= 0 && snapshot.PreviewOnePercentLowFps <= 0)
        {
            return StatsMetricStatus.Neutral;
        }

        var timingStatus = ResolveFrameTimeStatus(currentFrameTimeMs, snapshot.SourceExpectedFps);
        var lowFpsStatus = ResolveFpsStatus(snapshot.PreviewOnePercentLowFps, snapshot.SourceExpectedFps);
        return ResolveWorstStatus(timingStatus, lowFpsStatus);
    }

    private static StatsMetricStatus ResolveWorstStatus(StatsMetricStatus first, StatsMetricStatus second)
    {
        if (first == StatsMetricStatus.Bad || second == StatsMetricStatus.Bad)
        {
            return StatsMetricStatus.Bad;
        }

        if (first == StatsMetricStatus.Warning || second == StatsMetricStatus.Warning)
        {
            return StatsMetricStatus.Warning;
        }

        if (first == StatsMetricStatus.Good || second == StatsMetricStatus.Good)
        {
            return StatsMetricStatus.Good;
        }

        return first == StatsMetricStatus.Info || second == StatsMetricStatus.Info
            ? StatsMetricStatus.Info
            : StatsMetricStatus.Neutral;
    }

    private static StatsMetricStatus ResolveFrameTimeStatus(double p95IntervalMs, double expectedFps)
    {
        if (p95IntervalMs <= 0)
        {
            return StatsMetricStatus.Neutral;
        }

        expectedFps = Sanitize(expectedFps);
        if (expectedFps <= 0)
        {
            return StatsMetricStatus.Info;
        }

        var budgetMs = 1000.0 / expectedFps;
        return p95IntervalMs <= budgetMs * 1.10 ? StatsMetricStatus.Good :
               p95IntervalMs <= budgetMs * 1.50 ? StatsMetricStatus.Warning :
               StatsMetricStatus.Bad;
    }

    private static StatsMetricStatus ResolveDecodedVisualStatus(StatsSnapshot snapshot)
    {
        if (snapshot.VisualCadenceSamples <= 0)
        {
            return StatsMetricStatus.Neutral;
        }

        if (IsVisualRepeatWithinExpectedDrift(snapshot))
        {
            return StatsMetricStatus.Good;
        }

        if (string.Equals(snapshot.VisualCadenceMotionConfidence, "LowMotion", StringComparison.OrdinalIgnoreCase) &&
            snapshot.VisualCadenceChangeFps < snapshot.SourceExpectedFps * 0.95)
        {
            return StatsMetricStatus.Info;
        }

        return ResolveFpsStatus(snapshot.VisualCadenceChangeFps, snapshot.SourceExpectedFps);
    }

    private static StatsMetricStatus ResolveLatencyStatus(double latencyMs)
        => latencyMs <= 0 ? StatsMetricStatus.Neutral :
           latencyMs <= 100 ? StatsMetricStatus.Good :
           latencyMs <= 150 ? StatsMetricStatus.Warning :
           StatsMetricStatus.Bad;

    private static string FormatFps(double value)
    {
        return Sanitize(value).ToString("0.00");
    }

    private static string FormatHz(double value)
    {
        value = Sanitize(value);
        if (value <= 0)
        {
            return "\u2014";
        }

        var rounded = Math.Round(value);
        return Math.Abs(value - rounded) <= 0.15
            ? $"{rounded:0} Hz"
            : $"{value:0.##} Hz";
    }

    private static string FormatSourceHdr(bool? isHdr, string? colorimetry)
        => DisplayFormatters.FormatSourceHdr(isHdr, colorimetry);

    private static string FormatFrameBudgetMs(double expectedFps)
    {
        expectedFps = Sanitize(expectedFps);
        return expectedFps > 0 ? $"{1000.0 / expectedFps:0.00}ms" : "\u2014";
    }

    private static string FormatPreviewCadenceSummary(StatsSnapshot snapshot)
    {
        if (snapshot.PreviewCadenceSamples <= 0)
        {
            return "\u2014";
        }

        var currentFrameTimeMs = ResolveCurrentPreviewFrameTimeMs(snapshot);
        var currentFrameTime = currentFrameTimeMs > 0
            ? FormatMs(currentFrameTimeMs)
            : "\u2014";
        var onePercentLow = Sanitize(snapshot.PreviewOnePercentLowFps) > 0
            ? $"1% low {FormatFps(snapshot.PreviewOnePercentLowFps)} fps"
            : "1% low \u2014";
        return $"{currentFrameTime} | {onePercentLow}";
    }

    private static double ResolveCurrentPreviewFrameTimeMs(StatsSnapshot snapshot)
    {
        var samples = snapshot.PreviewRecentPresentIntervalsMs;
        if (samples is { Count: > 0 })
        {
            return Sanitize(samples[samples.Count - 1]);
        }

        return Sanitize(snapshot.PreviewAvgIntervalMs);
    }

    private static string FormatVisualRepeatSummary(StatsSnapshot snapshot)
    {
        if (snapshot.VisualCadenceSamples <= 0)
        {
            return "\u2014";
        }

        if (IsVisualRepeatWithinExpectedDrift(snapshot))
        {
            return FormatHz(snapshot.VisualCadenceOutputFps);
        }

        var repeat = FormatPercent(snapshot.VisualCadenceRepeatPercent);
        return $"{FormatHz(snapshot.VisualCadenceChangeFps)} ({repeat} repeat, run {FormatCount(snapshot.VisualCadenceLongestRepeatRun)})";
    }

    private static string FormatVisualCadenceSummary(StatsSnapshot snapshot)
    {
        if (snapshot.VisualCadenceSamples <= 0)
        {
            return "\u2014";
        }

        if (IsVisualRepeatWithinExpectedDrift(snapshot))
        {
            return FormatHz(snapshot.VisualCadenceOutputFps);
        }

        return $"{FormatHz(snapshot.VisualCadenceChangeFps)} / {FormatPercent(snapshot.VisualCadenceRepeatPercent)} rep";
    }

    private static string FormatVisualMotionSummary(StatsSnapshot snapshot)
    {
        if (IsVisualRepeatWithinExpectedDrift(snapshot))
        {
            return $"{FormatPercent(snapshot.VisualCadenceMotionScore)} px / {snapshot.VisualCadenceMotionConfidence}";
        }

        return $"{FormatPercent(snapshot.VisualCadenceRepeatPercent)} repeat / run {FormatCount(snapshot.VisualCadenceLongestRepeatRun)} / {FormatPercent(snapshot.VisualCadenceMotionScore)} px / {snapshot.VisualCadenceMotionConfidence}";
    }

    private static string FormatPercent(double value)
    {
        return $"{Sanitize(value):0.0}%";
    }

    private static string FormatScore(double value)
    {
        return Sanitize(value).ToString("0.0");
    }

    private static string FormatCount(long value)
    {
        return Math.Max(0, value).ToString("N0");
    }

    private static string FormatSignedMs(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value))
        {
            return "\u2014";
        }

        return value.Value >= 0 ? $"+{value.Value:F1}ms" : $"{value.Value:F1}ms";
    }

    private static string FormatSignedMsPerSec(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value))
        {
            return "\u2014";
        }

        return value.Value >= 0 ? $"+{value.Value:F2} ms/s" : $"{value.Value:F2} ms/s";
    }

    private static bool IsVisualRepeatWithinExpectedDrift(StatsSnapshot snapshot)
    {
        if (snapshot.VisualCadenceSamples <= 0)
        {
            return false;
        }

        var expectedRepeatPercent = GetExpectedVisualRepeatPercent(snapshot);
        var allowedRepeatPercent = expectedRepeatPercent + VisualRepeatTolerancePercent;
        return snapshot.VisualCadenceLongestRepeatRun <= 1 &&
               snapshot.VisualCadenceRepeatPercent <= allowedRepeatPercent;
    }

    private static double GetExpectedVisualRepeatPercent(StatsSnapshot snapshot)
    {
        var sourceFps = Sanitize(snapshot.SourceFrameRateExact ?? snapshot.SourceExpectedFps);
        var outputFps = Sanitize(snapshot.VisualCadenceOutputFps);
        if (sourceFps <= 0 || outputFps <= sourceFps)
        {
            return 0;
        }

        return Math.Clamp((outputFps - sourceFps) / outputFps * 100.0, 0.0, 100.0);
    }
}
