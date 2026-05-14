using System;
using System.Collections.Generic;
using Sussudio.Models;
using Sussudio.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Sussudio.Controllers;

namespace Sussudio;

// Stats dock and frame-time overlay presentation. This partial only projects
// diagnostics into UI elements; the underlying snapshot data is owned by
// AutomationDiagnosticsHub and CaptureService.
public sealed partial class MainWindow
{
    private StatsOverlayController _statsOverlayController = null!;
    private StatsDiagnosticRowsController _statsDiagnosticRowsController = null!;

    private void InitializeStatsOverlayController()
    {
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
        _statsDiagnosticRowsController = new StatsDiagnosticRowsController(new StatsDiagnosticRowsControllerContext
        {
            ResourceOwner = StatsDockPanel,
            DiagnosticsContent = Diagnostics_Content
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

        SetTextIfChanged(Stats_SessionStateValue, presentation.SessionState);
        SetTextIfChanged(Stats_SummaryCaptureValue, presentation.SummaryCapture);
        SetTextIfChanged(Stats_SummaryPreviewValue, presentation.SummaryPreview);
        SetTextIfChanged(Stats_SummaryRendererFpsValue, presentation.SummaryRendererFps);
        SetTextIfChanged(Stats_SummaryVisualFpsValue, presentation.SummaryVisualFps);
        SetTextIfChanged(Stats_SummaryLatencyValue, presentation.SummaryLatency);
        SetMetricBrush(Stats_SummaryCaptureValue, presentation.SummaryCaptureStatus);
        SetMetricBrush(Stats_SummaryRendererFpsValue, presentation.SummaryRendererFpsStatus);
        SetMetricBrush(Stats_SummaryVisualFpsValue, presentation.SummaryVisualFpsStatus);
        SetMetricBrush(Stats_SummaryLatencyValue, presentation.SummaryLatencyStatus);
        SetTextIfChanged(Stats_SourceResolutionValue, presentation.SourceResolution);
        SetTextIfChanged(Stats_SourceFrameRateValue, presentation.SourceFrameRate);
        SetTextIfChanged(Stats_SourceHdrValue, presentation.SourceHdr);
        SetTextIfChanged(Stats_SourceFormatValue, presentation.SourceFormat);
        SetTextIfChanged(Stats_TelemetryOriginValue, presentation.TelemetryOrigin);
        SetTextIfChanged(Stats_AdcOnOffValue, presentation.AdcOnOff);
        SetTextIfChanged(Stats_AdcGainValue, presentation.AdcGain);
        SetTextIfChanged(Stats_SourceFpsValue, presentation.SourceFps);
        SetTextIfChanged(Stats_SourceExpectedFpsValue, presentation.SourceExpectedFps);
        SetTextIfChanged(Stats_SourceAvgValue, presentation.SourceAvg);
        SetTextIfChanged(Stats_SourceP95Value, presentation.SourceP95);
        SetTextIfChanged(Stats_SourceJitterValue, presentation.SourceJitter);
        SetTextIfChanged(Stats_SourceGapsValue, presentation.SourceGaps);
        SetTextIfChanged(Stats_SourceDropsValue, presentation.SourceDrops);
        SetTextIfChanged(Stats_PreviewFpsValue, presentation.PreviewFps);
        SetTextIfChanged(Stats_PreviewAvgValue, presentation.PreviewAvg);
        SetTextIfChanged(Stats_PreviewP95Value, presentation.PreviewP95);
        SetTextIfChanged(Stats_PreviewSlowValue, presentation.PreviewSlow);
        SetTextIfChanged(Stats_VisualFpsValue, presentation.VisualFps);
        SetTextIfChanged(Stats_VisualMotionValue, presentation.VisualMotion);
        SetMetricBrush(Stats_VisualFpsValue, presentation.VisualFpsStatus);
        SetTextIfChanged(Stats_PipelineLatencyValue, presentation.PipelineLatency);
        SetTextIfChanged(Stats_SourceDeliveredValue, presentation.SourceDelivered);
        SetTextIfChanged(Stats_SourceDroppedValue, presentation.SourceDropped);
        SetTextIfChanged(Stats_RendererRenderedValue, presentation.RendererRendered);
        SetTextIfChanged(Stats_RendererDroppedValue, presentation.RendererDropped);
        SetTextIfChanged(Stats_PerfScoreValue, presentation.PerformanceScore);
        SetTextIfChanged(Stats_AvSyncDriftValue, presentation.AvSyncDrift);
        SetTextIfChanged(Stats_AvSyncDriftRateValue, presentation.AvSyncDriftRate);
        SetVisibilityIfChanged(Stats_AvSyncEncoderRow, presentation.EncoderDriftVisible ? Visibility.Visible : Visibility.Collapsed);
        if (presentation.EncoderDriftVisible)
        {
            SetTextIfChanged(Stats_AvSyncEncoderValue, presentation.EncoderDrift);
        }

        SetVisibilityIfChanged(EncoderSection, presentation.EncoderActive ? Visibility.Visible : Visibility.Collapsed);
        if (presentation.EncoderActive)
        {
            SetTextIfChanged(Stats_EncoderCodecValue, presentation.EncoderCodec);
            SetTextIfChanged(Stats_EncoderResolutionValue, presentation.EncoderResolution);
            SetTextIfChanged(Stats_EncoderFrameRateValue, presentation.EncoderFrameRate);
            SetTextIfChanged(Stats_EncoderBitrateValue, presentation.EncoderBitrate);
        }

        UpdateDiagnosticsSection(snapshot.SourceTelemetryDetails ?? Array.Empty<SourceTelemetryDetailEntry>(), snapshot.DiagnosticSummary);
        UpdateDecodeSection();
        UpdateGpuSection();
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

    private static readonly SolidColorBrush MetricNeutralBrush = new(Windows.UI.Color.FromArgb(0xFF, 0xF1, 0xF1, 0xF1));
    private static readonly SolidColorBrush MetricGoodBrush = new(Windows.UI.Color.FromArgb(0xFF, 0x70, 0xF0, 0x8B));
    private static readonly SolidColorBrush MetricInfoBrush = new(Windows.UI.Color.FromArgb(0xFF, 0x55, 0xD6, 0xFF));
    private static readonly SolidColorBrush MetricWarningBrush = new(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xC8, 0x57));
    private static readonly SolidColorBrush MetricBadBrush = new(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0x6B, 0x6B));

    private static void SetMetricBrush(TextBlock target, StatsMetricStatus status)
    {
        target.Foreground = status switch
        {
            StatsMetricStatus.Good => MetricGoodBrush,
            StatsMetricStatus.Info => MetricInfoBrush,
            StatsMetricStatus.Warning => MetricWarningBrush,
            StatsMetricStatus.Bad => MetricBadBrush,
            _ => MetricNeutralBrush
        };
    }

    private static void SetVisibilityIfChanged(UIElement element, Visibility visibility)
    {
        if (element.Visibility != visibility)
        {
            element.Visibility = visibility;
        }
    }
    private static void SetTextIfChanged(TextBlock target, string value)
    {
        if (!string.Equals(target.Text, value, StringComparison.Ordinal))
        {
            target.Text = value;
        }
    }
    private StatsSnapshot GetStatsSnapshot()
    {
        var health = ViewModel.GetCaptureHealthSnapshot();
        var d3d = _d3dRenderer;
        var presentCadence = d3d?.GetPresentCadenceMetrics(_previewMinPresentationIntervalMs);
        var renderer = new StatsSnapshotRenderMetrics(
            PreviewCadenceSamples: presentCadence?.SampleCount ?? 0,
            PreviewObservedFps: presentCadence?.ObservedFps ?? 0,
            PreviewAvgIntervalMs: presentCadence?.AverageIntervalMs ?? 0,
            PreviewP95IntervalMs: presentCadence?.P95IntervalMs ?? 0,
            PreviewP99IntervalMs: presentCadence?.P99IntervalMs ?? 0,
            PreviewOnePercentLowFps: presentCadence?.OnePercentLowFps ?? 0,
            PreviewSlowFrames: presentCadence?.SlowFrameCount ?? 0,
            PreviewSlowPercent: presentCadence?.SlowFramePercent ?? 0,
            PipelineLatencyMs: d3d?.GetEstimatedPipelineLatencyMs() ?? 0,
            FramesSubmitted: d3d?.FramesSubmitted ?? 0,
            FramesRendered: d3d?.FramesRendered ?? 0,
            FramesDropped: d3d?.FramesDropped ?? 0,
            PreviewNaturalWidth: d3d?.NaturalWidth ?? 0,
            PreviewNaturalHeight: d3d?.NaturalHeight ?? 0,
            PreviewRecentPresentIntervalsMs: d3d?.GetRecentPresentIntervalsMs(180) ?? Array.Empty<double>(),
            PreviewRecentLatencyMs: d3d?.GetRecentPipelineLatencyMs(180) ?? Array.Empty<double>());
        var viewState = new StatsSnapshotViewState(ViewModel.IsPreviewing, ViewModel.IsRecording);

        return StatsSnapshotBuilder.Build(health, renderer, viewState);
    }
}
