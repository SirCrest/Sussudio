using Sussudio.Models;
using Sussudio.Services.Automation;

namespace Sussudio.ViewModels;

internal static partial class StatsPresentationBuilder
{
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

        var encoder = BuildEncoderPresentation(snapshot);

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
            EncoderDriftVisible: encoder.DriftVisible,
            EncoderDrift: encoder.Drift,
            EncoderActive: encoder.Active,
            EncoderCodec: encoder.Codec,
            EncoderResolution: encoder.Resolution,
            EncoderFrameRate: encoder.FrameRate,
            EncoderBitrate: encoder.Bitrate);
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
}
