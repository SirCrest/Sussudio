using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing stats overlay adapter and controller composition. This also wires the stats dock refresh
// graph because the dock is only driven by the overlay controller.
public sealed partial class MainWindow
{
    private FrameTimeOverlayPresentationController _frameTimeOverlayPresentationController = null!;
    private StatsOverlayController _statsOverlayController = null!;
    private StatsDockRefreshController _statsDockRefreshController = null!;

    private void InitializeStatsOverlayController()
    {
        InitializeFrameTimeOverlayPresentationController();
        InitializeStatsDockRefreshController();
        _statsOverlayController = new StatsOverlayController(new StatsOverlayControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            StatsToggle = StatsToggle,
            StatsDockPanel = StatsDockPanel,
            FrameTimeOverlay = FrameTimeOverlay,
            FrameTimeOverlayToggle = FrameTimeOverlayToggle,
            IsWindowClosing = () => _isWindowClosing,
            SetStatsVisible = visible => ViewModel.IsStatsVisible = visible,
            GetStatsSnapshot = GetStatsSnapshot,
            UpdateStatsDock = _statsDockRefreshController.RefreshDock,
            UpdateFrameTimeOverlay = UpdateFrameTimeOverlay,
            Log = message => Logger.Log(message)
        });
    }

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

    private void InitializeFrameTimeOverlayPresentationController()
    {
        _frameTimeOverlayPresentationController = new FrameTimeOverlayPresentationController(new FrameTimeOverlayPresentationControllerContext
        {
            SourceValue = FrameTime_SourceValue,
            VisualValue = FrameTime_VisualValue,
            PreviewValue = FrameTime_PreviewValue,
            LatencyValue = FrameTime_LatencyValue,
            StatusValue = FrameTime_StatusValue,
            Canvas = FrameTime_Canvas,
            VisualLine = FrameTime_VisualLine,
            PreviewLine = FrameTime_PreviewLine,
            ExpectedLine = FrameTime_ExpectedLine
        });
    }

    private void AttachStatsOverlayToggleBindings()
        => _statsOverlayController.AttachToggleBindings();

    private void DetachStatsOverlayToggleBindings()
        => _statsOverlayController.DetachToggleBindings();

    private void ApplyStatsVisibility(bool visible, bool immediate = false)
        => _statsOverlayController.SyncStatsVisibility(visible, immediate);

    private void SetFrameTimeOverlayVisible(bool visible)
        => _statsOverlayController.SetFrameTimeOverlayVisible(visible);

    private bool IsFrameTimeOverlayVisible()
        => _statsOverlayController.IsFrameTimeOverlayVisible;

    private void UpdateFrameTimeOverlay(StatsSnapshot snapshot)
    {
        if (!IsFrameTimeOverlayVisible())
        {
            return;
        }

        _frameTimeOverlayPresentationController.Apply(snapshot);
    }

    private void StartStatsDockPolling()
        => _statsOverlayController.StartPolling();

    private void StopStatsDockPolling()
        => _statsOverlayController.StopPolling();

    private void ShowStatsDockPanel()
        => _statsOverlayController.ShowDockPanel();

    private void HideStatsDockPanel(bool immediate = false)
        => _statsOverlayController.HideDockPanel(immediate);
}
