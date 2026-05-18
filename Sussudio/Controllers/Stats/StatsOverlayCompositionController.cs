using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Sussudio.Models;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class StatsOverlayCompositionControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required ToggleButton StatsToggle { get; init; }
    public required Border StatsDockPanel { get; init; }
    public required FrameworkElement FrameTimeOverlay { get; init; }
    public required ToggleButton FrameTimeOverlayToggle { get; init; }
    public required Func<bool> IsWindowClosing { get; init; }
    public required Action<bool> SetStatsVisible { get; init; }
    public required Func<CaptureHealthSnapshot> GetCaptureHealthSnapshot { get; init; }
    public required Func<D3D11PreviewRenderer?> GetRenderer { get; init; }
    public required Func<double> GetPreviewMinPresentationIntervalMs { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Action<string> Log { get; init; }
    public required StackPanel DiagnosticsContent { get; init; }
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
    public required TextBlock FrameTimeSourceValue { get; init; }
    public required TextBlock FrameTimeVisualValue { get; init; }
    public required TextBlock FrameTimePreviewValue { get; init; }
    public required TextBlock FrameTimeLatencyValue { get; init; }
    public required TextBlock FrameTimeStatusValue { get; init; }
    public required Canvas FrameTimeCanvas { get; init; }
    public required Polyline FrameTimeVisualLine { get; init; }
    public required Polyline FrameTimePreviewLine { get; init; }
    public required Line FrameTimeExpectedLine { get; init; }
}

internal sealed class StatsOverlayCompositionController
{
    private readonly StatsOverlayController _statsOverlayController;
    private readonly StatsDockControllerGraph _statsDockControllerGraph;
    private readonly StatsSnapshotProvider _statsSnapshotProvider;
    private readonly FrameTimeOverlayPresentationController _frameTimeOverlayPresentationController;
    private readonly StatsSectionChromeController _statsSectionChromeController;

    public StatsOverlayCompositionController(StatsOverlayCompositionControllerContext context)
    {
        _statsSnapshotProvider = CreateSnapshotProvider(context);
        _frameTimeOverlayPresentationController = CreateFrameTimeOverlayPresentationController(context);
        _statsDockControllerGraph = CreateDockControllerGraph(context);
        _statsOverlayController = CreateOverlayController(context);
        _statsSectionChromeController = CreateSectionChromeController(context);
    }

    public void AttachToggleBindings()
        => _statsOverlayController.AttachToggleBindings();

    public void DetachToggleBindings()
        => _statsOverlayController.DetachToggleBindings();

    public void ApplyStatsVisibility(bool visible, bool immediate = false)
        => _statsOverlayController.SyncStatsVisibility(visible, immediate);

    public bool TryHandlePropertyChanged(string propertyName, bool isStatsVisible)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.IsStatsVisible):
                ApplyStatsVisibility(isStatsVisible);
                return true;

            default:
                return false;
        }
    }

    public void SetFrameTimeOverlayVisible(bool visible)
        => _statsOverlayController.SetFrameTimeOverlayVisible(visible);

    public bool IsFrameTimeOverlayVisible
        => _statsOverlayController.IsFrameTimeOverlayVisible;

    public void StartPolling()
        => _statsOverlayController.StartPolling();

    public void StopPolling()
        => _statsOverlayController.StopPolling();

    public void ShowDockPanel()
        => _statsOverlayController.ShowDockPanel();

    public void HideDockPanel(bool immediate = false)
        => _statsOverlayController.HideDockPanel(immediate);

    public StatsSnapshot GetStatsSnapshot()
        => _statsSnapshotProvider.GetSnapshot();

    public void ToggleSectionFromHeader(object sender)
        => _statsSectionChromeController.ToggleFromHeader(sender);

    public void SetSectionVisible(string section, bool visible)
        => _statsSectionChromeController.SetVisible(section, visible);

    private void UpdateFrameTimeOverlay(StatsSnapshot snapshot)
    {
        if (!IsFrameTimeOverlayVisible)
        {
            return;
        }

        _frameTimeOverlayPresentationController.Apply(snapshot);
    }

    private static StatsSnapshotProvider CreateSnapshotProvider(StatsOverlayCompositionControllerContext context)
    {
        return new StatsSnapshotProvider(new StatsSnapshotProviderContext
        {
            GetCaptureHealthSnapshot = context.GetCaptureHealthSnapshot,
            GetRenderer = context.GetRenderer,
            GetPreviewMinPresentationIntervalMs = context.GetPreviewMinPresentationIntervalMs,
            IsPreviewing = context.IsPreviewing,
            IsRecording = context.IsRecording
        });
    }

    private StatsOverlayController CreateOverlayController(StatsOverlayCompositionControllerContext context)
    {
        return new StatsOverlayController(new StatsOverlayControllerContext
        {
            DispatcherQueue = context.DispatcherQueue,
            StatsToggle = context.StatsToggle,
            StatsDockPanel = context.StatsDockPanel,
            FrameTimeOverlay = context.FrameTimeOverlay,
            FrameTimeOverlayToggle = context.FrameTimeOverlayToggle,
            IsWindowClosing = context.IsWindowClosing,
            SetStatsVisible = context.SetStatsVisible,
            GetStatsSnapshot = GetStatsSnapshot,
            UpdateStatsDock = _statsDockControllerGraph.RefreshDock,
            UpdateFrameTimeOverlay = UpdateFrameTimeOverlay,
            Log = context.Log
        });
    }

    private StatsSectionChromeController CreateSectionChromeController(StatsOverlayCompositionControllerContext context)
    {
        return new StatsSectionChromeController(new StatsSectionChromeControllerContext
        {
            StatsDockPanel = context.StatsDockPanel,
            DiagnosticsContent = context.DiagnosticsContent,
            RefreshDiagnosticsSection = _statsDockControllerGraph.RefreshDiagnosticsSection
        });
    }

    private StatsDockControllerGraph CreateDockControllerGraph(StatsOverlayCompositionControllerContext context)
    {
        return new StatsDockControllerGraph(new StatsDockControllerGraphContext
        {
            IsWindowClosing = context.IsWindowClosing,
            StatsDockPanel = context.StatsDockPanel,
            DiagnosticsContent = context.DiagnosticsContent,
            GetStatsSnapshot = GetStatsSnapshot,
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
            EncoderBitrateValue = context.EncoderBitrateValue,
            DecodeSection = context.DecodeSection,
            DecodeContent = context.DecodeContent,
            GpuContent = context.GpuContent,
            GetMjpegPipelineTimingDetails = context.GetMjpegPipelineTimingDetails,
            GetPendingPreviewFrameCount = context.GetPendingPreviewFrameCount,
            GetNvmlSnapshot = context.GetNvmlSnapshot
        });
    }

    private static FrameTimeOverlayPresentationController CreateFrameTimeOverlayPresentationController(
        StatsOverlayCompositionControllerContext context)
    {
        return new FrameTimeOverlayPresentationController(new FrameTimeOverlayPresentationControllerContext
        {
            SourceValue = context.FrameTimeSourceValue,
            VisualValue = context.FrameTimeVisualValue,
            PreviewValue = context.FrameTimePreviewValue,
            LatencyValue = context.FrameTimeLatencyValue,
            StatusValue = context.FrameTimeStatusValue,
            Canvas = context.FrameTimeCanvas,
            VisualLine = context.FrameTimeVisualLine,
            PreviewLine = context.FrameTimePreviewLine,
            ExpectedLine = context.FrameTimeExpectedLine
        });
    }
}
