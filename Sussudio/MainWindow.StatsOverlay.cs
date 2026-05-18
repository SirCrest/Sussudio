using Microsoft.UI.Xaml.Input;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing stats overlay adapter. StatsOverlayCompositionController owns the
// stats controller graph and construction order behind this surface.
public sealed partial class MainWindow
{
    private StatsOverlayCompositionController _statsOverlayCompositionController = null!;

    private void InitializeStatsOverlayCompositionController()
    {
        _statsOverlayCompositionController = new StatsOverlayCompositionController(new StatsOverlayCompositionControllerContext
        {
            Shell = new StatsOverlayShellContext
            {
                DispatcherQueue = _dispatcherQueue,
                StatsToggle = StatsToggle,
                StatsDockPanel = StatsDockPanel,
                FrameTimeOverlay = FrameTimeOverlay,
                FrameTimeOverlayToggle = FrameTimeOverlayToggle,
                IsWindowClosing = () => _isWindowClosing,
                SetStatsVisible = visible => ViewModel.IsStatsVisible = visible,
                Log = message => Logger.Log(message),
            },
            SnapshotSources = new StatsOverlaySnapshotSourceContext
            {
                GetCaptureHealthSnapshot = ViewModel.GetCaptureHealthSnapshot,
                GetRenderer = () => _previewRendererHostController.Renderer,
                GetPreviewMinPresentationIntervalMs = () => _previewRendererHostController.PreviewMinPresentationIntervalMs,
                IsPreviewing = () => ViewModel.IsPreviewing,
                IsRecording = () => ViewModel.IsRecording,
            },
            DockTargets = new StatsOverlayDockTargetsContext
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
            },
            HardwareSources = new StatsOverlayHardwareSourceContext
            {
                GetMjpegPipelineTimingDetails = ViewModel.GetMjpegPipelineTimingDetails,
                GetPendingPreviewFrameCount = () => _previewRendererHostController.PendingFrameCount,
                GetNvmlSnapshot = () => _nvmlMonitor?.GetLatestSnapshot(),
            },
            FrameTimeTargets = new StatsOverlayFrameTimeTargetsContext
            {
                FrameTimeSourceValue = FrameTime_SourceValue,
                FrameTimeVisualValue = FrameTime_VisualValue,
                FrameTimePreviewValue = FrameTime_PreviewValue,
                FrameTimeLatencyValue = FrameTime_LatencyValue,
                FrameTimeStatusValue = FrameTime_StatusValue,
                FrameTimeCanvas = FrameTime_Canvas,
                FrameTimeVisualLine = FrameTime_VisualLine,
                FrameTimePreviewLine = FrameTime_PreviewLine,
                FrameTimeExpectedLine = FrameTime_ExpectedLine,
            },
        });
    }

    private void AttachStatsOverlayToggleBindings()
        => _statsOverlayCompositionController.AttachToggleBindings();

    private void DetachStatsOverlayToggleBindings()
        => _statsOverlayCompositionController.DetachToggleBindings();

    private void ApplyStatsVisibility(bool visible, bool immediate = false)
        => _statsOverlayCompositionController.ApplyStatsVisibility(visible, immediate);

    private void SetFrameTimeOverlayVisible(bool visible)
        => _statsOverlayCompositionController.SetFrameTimeOverlayVisible(visible);

    private bool IsFrameTimeOverlayVisible()
        => _statsOverlayCompositionController.IsFrameTimeOverlayVisible;

    private void StartStatsDockPolling()
        => _statsOverlayCompositionController.StartPolling();

    private void StopStatsDockPolling()
        => _statsOverlayCompositionController.StopPolling();

    private void ShowStatsDockPanel()
        => _statsOverlayCompositionController.ShowDockPanel();

    private void HideStatsDockPanel(bool immediate = false)
        => _statsOverlayCompositionController.HideDockPanel(immediate);

    private StatsSnapshot GetStatsSnapshot()
        => _statsOverlayCompositionController.GetStatsSnapshot();

    private void StatsSectionHeader_Tapped(object sender, TappedRoutedEventArgs e)
        => _statsOverlayCompositionController.ToggleSectionFromHeader(sender);

    private void SetStatsSectionVisible(string section, bool visible)
        => _statsOverlayCompositionController.SetSectionVisible(section, visible);
}
