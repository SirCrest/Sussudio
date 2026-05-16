using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// Stats dock controller graph composition for presentation, diagnostics, hardware rows, and refresh.
public sealed partial class MainWindow
{
    private StatsDockRefreshController _statsDockRefreshController = null!;

    private void InitializeStatsDockRefreshController()
    {
        var statsDockPresentationController = new StatsDockPresentationController(new StatsDockPresentationControllerContext
        {
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
            EncoderBitrateValue = Stats_EncoderBitrateValue
        });
        var statsDockRowChromeController = new StatsDockRowChromeController(new StatsDockRowChromeControllerContext
        {
            ResourceOwner = StatsDockPanel
        });
        var statsDiagnosticRowsController = new StatsDiagnosticRowsController(new StatsDiagnosticRowsControllerContext
        {
            ResourceOwner = StatsDockPanel,
            DiagnosticsContent = Diagnostics_Content
        });
        var statsHardwareRowsInputProvider = new StatsHardwareRowsInputProvider(new StatsHardwareRowsInputProviderContext
        {
            GetMjpegPipelineTimingDetails = ViewModel.GetMjpegPipelineTimingDetails,
            GetPendingPreviewFrameCount = () => _previewRendererHostController.PendingFrameCount,
            GetNvmlSnapshot = () => _nvmlMonitor?.GetLatestSnapshot()
        });
        var statsHardwareRowsController = new StatsHardwareRowsController(new StatsHardwareRowsControllerContext
        {
            DecodeSection = DecodeSection,
            DecodeContent = Decode_Content,
            GpuContent = GPU_Content,
            RowChromeController = statsDockRowChromeController,
            InputProvider = statsHardwareRowsInputProvider
        });
        _statsDockRefreshController = new StatsDockRefreshController(new StatsDockRefreshControllerContext
        {
            IsWindowClosing = () => _isWindowClosing,
            IsStatsDockVisible = () => StatsDockPanel.Visibility == Visibility.Visible,
            IsDiagnosticsSectionVisible = () => Diagnostics_Content.Visibility == Visibility.Visible,
            GetStatsSnapshot = GetStatsSnapshot,
            DockPresentationController = statsDockPresentationController,
            DiagnosticRowsController = statsDiagnosticRowsController,
            HardwareRowsController = statsHardwareRowsController
        });
    }
}
