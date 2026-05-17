using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Services.Gpu;

namespace Sussudio.Controllers;

internal sealed class StatsDockControllerGraphContext
{
    public required Func<bool> IsWindowClosing { get; init; }
    public required FrameworkElement StatsDockPanel { get; init; }
    public required StackPanel DiagnosticsContent { get; init; }
    public required Func<StatsSnapshot> GetStatsSnapshot { get; init; }
    public required TextBlock SessionStateValue { get; init; }
    public required TextBlock SummaryCaptureValue { get; init; }
    public required TextBlock SummaryPreviewValue { get; init; }
    public required TextBlock SummaryRendererFpsValue { get; init; }
    public required TextBlock SummaryVisualFpsValue { get; init; }
    public required TextBlock SummaryLatencyValue { get; init; }
    public required TextBlock SourceResolutionValue { get; init; }
    public required TextBlock SourceFrameRateValue { get; init; }
    public required TextBlock SourceHdrValue { get; init; }
    public required TextBlock SourceFormatValue { get; init; }
    public required TextBlock TelemetryOriginValue { get; init; }
    public required TextBlock AdcOnOffValue { get; init; }
    public required TextBlock AdcGainValue { get; init; }
    public required TextBlock SourceFpsValue { get; init; }
    public required TextBlock SourceExpectedFpsValue { get; init; }
    public required TextBlock SourceAvgValue { get; init; }
    public required TextBlock SourceP95Value { get; init; }
    public required TextBlock SourceJitterValue { get; init; }
    public required TextBlock SourceGapsValue { get; init; }
    public required TextBlock SourceDropsValue { get; init; }
    public required TextBlock PreviewFpsValue { get; init; }
    public required TextBlock PreviewAvgValue { get; init; }
    public required TextBlock PreviewP95Value { get; init; }
    public required TextBlock PreviewSlowValue { get; init; }
    public required TextBlock VisualFpsValue { get; init; }
    public required TextBlock VisualMotionValue { get; init; }
    public required TextBlock PipelineLatencyValue { get; init; }
    public required TextBlock SourceDeliveredValue { get; init; }
    public required TextBlock SourceDroppedValue { get; init; }
    public required TextBlock RendererRenderedValue { get; init; }
    public required TextBlock RendererDroppedValue { get; init; }
    public required TextBlock PerformanceScoreValue { get; init; }
    public required TextBlock AvSyncDriftValue { get; init; }
    public required TextBlock AvSyncDriftRateValue { get; init; }
    public required UIElement AvSyncEncoderRow { get; init; }
    public required TextBlock AvSyncEncoderValue { get; init; }
    public required UIElement EncoderSection { get; init; }
    public required TextBlock EncoderCodecValue { get; init; }
    public required TextBlock EncoderResolutionValue { get; init; }
    public required TextBlock EncoderFrameRateValue { get; init; }
    public required TextBlock EncoderBitrateValue { get; init; }
    public required UIElement DecodeSection { get; init; }
    public required StackPanel DecodeContent { get; init; }
    public required StackPanel GpuContent { get; init; }
    public required Func<ParallelMjpegDecodePipeline.PipelineTimingMetrics?> GetMjpegPipelineTimingDetails { get; init; }
    public required Func<int?> GetPendingPreviewFrameCount { get; init; }
    public required Func<NvmlSnapshot?> GetNvmlSnapshot { get; init; }
}

internal sealed class StatsDockControllerGraph
{
    private readonly StatsDockRefreshController _refreshController;

    public StatsDockControllerGraph(StatsDockControllerGraphContext context)
    {
        var statsDockPresentationController = new StatsDockPresentationController(new StatsDockPresentationControllerContext
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

        var statsDockRowChromeController = new StatsDockRowChromeController(new StatsDockRowChromeControllerContext
        {
            ResourceOwner = context.StatsDockPanel
        });
        var statsDiagnosticRowsController = new StatsDiagnosticRowsController(new StatsDiagnosticRowsControllerContext
        {
            ResourceOwner = context.StatsDockPanel,
            DiagnosticsContent = context.DiagnosticsContent
        });
        var statsHardwareRowsInputProvider = new StatsHardwareRowsInputProvider(new StatsHardwareRowsInputProviderContext
        {
            GetMjpegPipelineTimingDetails = context.GetMjpegPipelineTimingDetails,
            GetPendingPreviewFrameCount = context.GetPendingPreviewFrameCount,
            GetNvmlSnapshot = context.GetNvmlSnapshot
        });
        var statsHardwareRowsController = new StatsHardwareRowsController(new StatsHardwareRowsControllerContext
        {
            DecodeSection = context.DecodeSection,
            DecodeContent = context.DecodeContent,
            GpuContent = context.GpuContent,
            RowChromeController = statsDockRowChromeController,
            InputProvider = statsHardwareRowsInputProvider
        });

        _refreshController = new StatsDockRefreshController(new StatsDockRefreshControllerContext
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

    public void RefreshDock()
        => _refreshController.RefreshDock();

    public void RefreshDiagnosticsSection()
        => _refreshController.RefreshDiagnosticsSection();
}
