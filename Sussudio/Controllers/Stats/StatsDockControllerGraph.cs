using Microsoft.UI.Xaml;

namespace Sussudio.Controllers;

internal sealed class StatsDockControllerGraph
{
    private readonly StatsDockRefreshController _refreshController;

    public StatsDockControllerGraph(StatsDockControllerGraphContext context)
    {
        var statsDockPresentationController = CreatePresentationController(context);
        var statsDockRowChromeController = CreateRowChromeController(context);
        var statsDiagnosticRowsController = CreateDiagnosticRowsController(context);
        var statsHardwareRowsInputProvider = CreateHardwareRowsInputProvider(context);
        var statsHardwareRowsController = CreateHardwareRowsController(
            context,
            statsDockRowChromeController,
            statsHardwareRowsInputProvider);

        _refreshController = CreateRefreshController(
            context,
            statsDockPresentationController,
            statsDiagnosticRowsController,
            statsHardwareRowsController);
    }

    public void RefreshDock()
        => _refreshController.RefreshDock();

    public void RefreshDiagnosticsSection()
        => _refreshController.RefreshDiagnosticsSection();

    private static StatsDockPresentationController CreatePresentationController(
        StatsDockControllerGraphContext context)
    {
        return new StatsDockPresentationController(new StatsDockPresentationControllerContext
        {
            SessionStateValue = context.SessionStateValue,
            SummaryCaptureValue = context.SummaryCaptureValue,
            SummaryPreviewValue = context.SummaryPreviewValue,
            SummaryRendererFpsValue = context.SummaryRendererFpsValue,
            SummaryVisualFpsValue = context.SummaryVisualFpsValue,
            SummaryLatencyValue = context.SummaryLatencyValue,
            SourceResolutionValue = context.SourceResolutionValue,
            SourceFrameRateValue = context.SourceFrameRateValue,
            SourceHdrValue = context.SourceHdrValue,
            SourceFormatValue = context.SourceFormatValue,
            TelemetryOriginValue = context.TelemetryOriginValue,
            AdcOnOffValue = context.AdcOnOffValue,
            AdcGainValue = context.AdcGainValue,
            SourceFpsValue = context.SourceFpsValue,
            SourceExpectedFpsValue = context.SourceExpectedFpsValue,
            SourceAvgValue = context.SourceAvgValue,
            SourceP95Value = context.SourceP95Value,
            SourceJitterValue = context.SourceJitterValue,
            SourceGapsValue = context.SourceGapsValue,
            SourceDropsValue = context.SourceDropsValue,
            PreviewFpsValue = context.PreviewFpsValue,
            PreviewAvgValue = context.PreviewAvgValue,
            PreviewP95Value = context.PreviewP95Value,
            PreviewSlowValue = context.PreviewSlowValue,
            VisualFpsValue = context.VisualFpsValue,
            VisualMotionValue = context.VisualMotionValue,
            PipelineLatencyValue = context.PipelineLatencyValue,
            SourceDeliveredValue = context.SourceDeliveredValue,
            SourceDroppedValue = context.SourceDroppedValue,
            RendererRenderedValue = context.RendererRenderedValue,
            RendererDroppedValue = context.RendererDroppedValue,
            PerformanceScoreValue = context.PerformanceScoreValue,
            AvSyncDriftValue = context.AvSyncDriftValue,
            AvSyncDriftRateValue = context.AvSyncDriftRateValue,
            AvSyncEncoderRow = context.AvSyncEncoderRow,
            AvSyncEncoderValue = context.AvSyncEncoderValue,
            EncoderSection = context.EncoderSection,
            EncoderCodecValue = context.EncoderCodecValue,
            EncoderResolutionValue = context.EncoderResolutionValue,
            EncoderFrameRateValue = context.EncoderFrameRateValue,
            EncoderBitrateValue = context.EncoderBitrateValue
        });
    }

    private static StatsDockRowChromeController CreateRowChromeController(
        StatsDockControllerGraphContext context)
    {
        return new StatsDockRowChromeController(new StatsDockRowChromeControllerContext
        {
            ResourceOwner = context.StatsDockPanel
        });
    }

    private static StatsDiagnosticRowsController CreateDiagnosticRowsController(
        StatsDockControllerGraphContext context)
    {
        return new StatsDiagnosticRowsController(new StatsDiagnosticRowsControllerContext
        {
            ResourceOwner = context.StatsDockPanel,
            DiagnosticsContent = context.DiagnosticsContent
        });
    }

    private static StatsHardwareRowsInputProvider CreateHardwareRowsInputProvider(
        StatsDockControllerGraphContext context)
    {
        return new StatsHardwareRowsInputProvider(new StatsHardwareRowsInputProviderContext
        {
            GetMjpegPipelineTimingDetails = context.GetMjpegPipelineTimingDetails,
            GetPendingPreviewFrameCount = context.GetPendingPreviewFrameCount,
            GetNvmlSnapshot = context.GetNvmlSnapshot
        });
    }

    private static StatsHardwareRowsController CreateHardwareRowsController(
        StatsDockControllerGraphContext context,
        StatsDockRowChromeController statsDockRowChromeController,
        StatsHardwareRowsInputProvider statsHardwareRowsInputProvider)
    {
        return new StatsHardwareRowsController(new StatsHardwareRowsControllerContext
        {
            DecodeSection = context.DecodeSection,
            DecodeContent = context.DecodeContent,
            GpuContent = context.GpuContent,
            RowChromeController = statsDockRowChromeController,
            InputProvider = statsHardwareRowsInputProvider
        });
    }

    private static StatsDockRefreshController CreateRefreshController(
        StatsDockControllerGraphContext context,
        StatsDockPresentationController statsDockPresentationController,
        StatsDiagnosticRowsController statsDiagnosticRowsController,
        StatsHardwareRowsController statsHardwareRowsController)
    {
        return new StatsDockRefreshController(new StatsDockRefreshControllerContext
        {
            IsWindowClosing = context.IsWindowClosing,
            IsStatsDockVisible = () => context.StatsDockPanel.Visibility == Visibility.Visible,
            IsDiagnosticsSectionVisible = () => context.DiagnosticsContent.Visibility == Visibility.Visible,
            GetStatsSnapshot = context.GetStatsSnapshot,
            DockPresentationController = statsDockPresentationController,
            DiagnosticRowsController = statsDiagnosticRowsController,
            HardwareRowsController = statsHardwareRowsController
        });
    }
}
