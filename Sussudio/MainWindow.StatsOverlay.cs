using System;
using System.Collections.Generic;
using Sussudio.Models;
using Sussudio.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Controllers;

namespace Sussudio;

// Stats dock and frame-time overlay presentation. This partial only projects
// diagnostics into UI elements; the underlying snapshot data is owned by
// AutomationDiagnosticsHub and CaptureService.
public sealed partial class MainWindow
{
    private StatsOverlayController _statsOverlayController = null!;
    private StatsDockPresentationController _statsDockPresentationController = null!;
    private StatsDiagnosticRowsController _statsDiagnosticRowsController = null!;
    private StatsHardwareRowsController _statsHardwareRowsController = null!;
    private FrameTimeOverlayPresentationController _frameTimeOverlayPresentationController = null!;

    private void InitializeStatsOverlayController()
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
        _statsOverlayController = new StatsOverlayController(new StatsOverlayControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            StatsDockPanel = StatsDockPanel,
            FrameTimeOverlay = FrameTimeOverlay,
            FrameTimeOverlayToggle = FrameTimeOverlayToggle,
            GetStatsSnapshot = GetStatsSnapshot,
            UpdateStatsDock = UpdateStatsDock,
            UpdateFrameTimeOverlay = UpdateFrameTimeOverlay,
            Log = message => Logger.Log(message)
        });
        _statsDockPresentationController = new StatsDockPresentationController(new StatsDockPresentationControllerContext
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
        _statsDiagnosticRowsController = new StatsDiagnosticRowsController(new StatsDiagnosticRowsControllerContext
        {
            ResourceOwner = StatsDockPanel,
            DiagnosticsContent = Diagnostics_Content
        });
        _statsHardwareRowsController = new StatsHardwareRowsController(new StatsHardwareRowsControllerContext
        {
            DecodeSection = DecodeSection,
            DecodeContent = Decode_Content,
            GpuContent = GPU_Content,
            DiagnosticRowsController = _statsDiagnosticRowsController,
            GetMjpegPipelineTimingDetails = ViewModel.GetMjpegPipelineTimingDetails,
            GetPendingPreviewFrameCount = () => _d3dRenderer?.PendingFrameCount,
            GetNvmlSnapshot = () => _nvmlMonitor?.GetLatestSnapshot()
        });
    }

    private void StatsToggle_Checked(object sender, RoutedEventArgs e)
    {
        if (_isWindowClosing)
        {
            return;
        }

        ViewModel.IsStatsVisible = true;
    }
    private void StatsToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        ViewModel.IsStatsVisible = false;
    }
    private void ApplyStatsVisibility(bool visible, bool immediate = false)
        => _statsOverlayController.ApplyStatsVisibility(visible, immediate);

    private void FrameTimeOverlayToggle_Checked(object sender, RoutedEventArgs e)
        => _statsOverlayController.SetFrameTimeOverlayVisible(true);

    private void FrameTimeOverlayToggle_Unchecked(object sender, RoutedEventArgs e)
        => _statsOverlayController.SetFrameTimeOverlayVisible(false);

    private void SetFrameTimeOverlayVisible(bool visible)
        => _statsOverlayController.SetFrameTimeOverlayVisible(visible);

    private void StartStatsDockPolling()
        => _statsOverlayController.StartPolling();

    private void StopStatsDockPolling()
        => _statsOverlayController.StopPolling();

    private void ShowStatsDockPanel()
        => _statsOverlayController.ShowDockPanel();

    private void HideStatsDockPanel(bool immediate = false)
        => _statsOverlayController.HideDockPanel(immediate);

    private void UpdateStatsDock()
    {
        if (_isWindowClosing || StatsDockPanel.Visibility != Visibility.Visible)
        {
            return;
        }

        var snapshot = GetStatsSnapshot();
        var presentation = StatsPresentationBuilder.BuildDockPresentation(snapshot);

        _statsDockPresentationController.Apply(presentation);

        UpdateDiagnosticsSection(snapshot.SourceTelemetryDetails ?? Array.Empty<SourceTelemetryDetailEntry>(), snapshot.DiagnosticSummary);
        UpdateDecodeSection();
        UpdateGpuSection();
    }

    private void RefreshDiagnosticsSection()
    {
        var snapshot = GetStatsSnapshot();
        UpdateDiagnosticsSection(snapshot.SourceTelemetryDetails ?? Array.Empty<SourceTelemetryDetailEntry>(), snapshot.DiagnosticSummary);
    }

    private void UpdateDiagnosticsSection(IReadOnlyList<SourceTelemetryDetailEntry> telemetryDetails, string? diagnosticSummary)
    {
        if (Diagnostics_Content.Visibility != Visibility.Visible)
        {
            return;
        }

        var presentation = StatsPresentationBuilder.BuildDiagnosticRows(telemetryDetails, diagnosticSummary);
        _statsDiagnosticRowsController.UpdateDiagnostics(presentation);
    }
}
