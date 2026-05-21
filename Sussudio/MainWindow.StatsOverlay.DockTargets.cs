using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private StatsOverlayDockTargetsContext CreateStatsOverlayDockTargetsContext()
        => new()
        {
            DiagnosticsContent = Diagnostics_Content,
            SessionStateValue = Stats_SessionStateValue,
            SummaryCaptureValue = Stats_SummaryCaptureValue,
            SummaryPreviewValue = Stats_SummaryPreviewValue,
            SummaryRendererFpsValue = Stats_SummaryRendererFpsValue,
            SummaryVisualFpsValue = Stats_SummaryVisualFpsValue,
            SummaryLatencyValue = Stats_SummaryLatencyValue,
            SourceResolutionValue = Stats_SourceResolutionValue,
            SourceFrameRateValue = Stats_SourceFrameRateValue,
            SourceHdrValue = Stats_SourceHdrValue,
            SourceFormatValue = Stats_SourceFormatValue,
            TelemetryOriginValue = Stats_TelemetryOriginValue,
            AdcOnOffValue = Stats_AdcOnOffValue,
            AdcGainValue = Stats_AdcGainValue,
            SourceFpsValue = Stats_SourceFpsValue,
            SourceExpectedFpsValue = Stats_SourceExpectedFpsValue,
            SourceAvgValue = Stats_SourceAvgValue,
            SourceP95Value = Stats_SourceP95Value,
            SourceJitterValue = Stats_SourceJitterValue,
            SourceGapsValue = Stats_SourceGapsValue,
            SourceDropsValue = Stats_SourceDropsValue,
            PreviewFpsValue = Stats_PreviewFpsValue,
            PreviewAvgValue = Stats_PreviewAvgValue,
            PreviewP95Value = Stats_PreviewP95Value,
            PreviewSlowValue = Stats_PreviewSlowValue,
            VisualFpsValue = Stats_VisualFpsValue,
            VisualMotionValue = Stats_VisualMotionValue,
            PipelineLatencyValue = Stats_PipelineLatencyValue,
            SourceDeliveredValue = Stats_SourceDeliveredValue,
            SourceDroppedValue = Stats_SourceDroppedValue,
            RendererRenderedValue = Stats_RendererRenderedValue,
            RendererDroppedValue = Stats_RendererDroppedValue,
            PerformanceScoreValue = Stats_PerfScoreValue,
            AvSyncDriftValue = Stats_AvSyncDriftValue,
            AvSyncDriftRateValue = Stats_AvSyncDriftRateValue,
            AvSyncEncoderRow = Stats_AvSyncEncoderRow,
            AvSyncEncoderValue = Stats_AvSyncEncoderValue,
            EncoderSection = EncoderSection,
            EncoderCodecValue = Stats_EncoderCodecValue,
            EncoderResolutionValue = Stats_EncoderResolutionValue,
            EncoderFrameRateValue = Stats_EncoderFrameRateValue,
            EncoderBitrateValue = Stats_EncoderBitrateValue,
            DecodeSection = DecodeSection,
            DecodeContent = Decode_Content,
            GpuContent = GPU_Content,
        };
}
