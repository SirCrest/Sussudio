using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Sussudio.Models;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class StatsOverlayCompositionControllerContext
{
    public required StatsOverlayShellContext Shell { get; init; }
    public required StatsOverlaySnapshotSourceContext SnapshotSources { get; init; }
    public required StatsOverlayDockTargetsContext DockTargets { get; init; }
    public required StatsOverlayHardwareSourceContext HardwareSources { get; init; }
    public required StatsOverlayFrameTimeTargetsContext FrameTimeTargets { get; init; }
}

internal sealed class StatsOverlayShellContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required ToggleButton StatsToggle { get; init; }
    public required Border StatsDockPanel { get; init; }
    public required FrameworkElement FrameTimeOverlay { get; init; }
    public required ToggleButton FrameTimeOverlayToggle { get; init; }
    public required Func<bool> IsWindowClosing { get; init; }
    public required Action<bool> SetStatsVisible { get; init; }
    public required Action<string> Log { get; init; }
}

internal sealed class StatsOverlaySnapshotSourceContext
{
    public required Func<CaptureHealthSnapshot> GetCaptureHealthSnapshot { get; init; }
    public required Func<D3D11PreviewRenderer?> GetRenderer { get; init; }
    public required Func<double> GetPreviewMinPresentationIntervalMs { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsRecording { get; init; }
}

internal sealed class StatsOverlayDockTargetsContext
{
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
}

internal sealed class StatsOverlayHardwareSourceContext
{
    public required Func<ParallelMjpegDecodePipeline.PipelineTimingMetrics?> GetMjpegPipelineTimingDetails { get; init; }
    public required Func<int?> GetPendingPreviewFrameCount { get; init; }
    public required Func<NvmlSnapshot?> GetNvmlSnapshot { get; init; }
}

internal sealed class StatsOverlayFrameTimeTargetsContext
{
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

internal sealed class StatsSnapshotProviderContext
{
    public required Func<CaptureHealthSnapshot> GetCaptureHealthSnapshot { get; init; }
    public required Func<D3D11PreviewRenderer?> GetRenderer { get; init; }
    public required Func<double> GetPreviewMinPresentationIntervalMs { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsRecording { get; init; }
}

internal sealed class StatsSnapshotProvider
{
    private const int RecentSampleCount = 180;

    private readonly StatsSnapshotProviderContext _context;

    public StatsSnapshotProvider(StatsSnapshotProviderContext context)
    {
        _context = context;
    }

    public StatsSnapshot GetSnapshot()
    {
        var health = _context.GetCaptureHealthSnapshot();
        var renderer = BuildRenderMetrics(_context.GetRenderer(), _context.GetPreviewMinPresentationIntervalMs());
        var viewState = new StatsSnapshotViewState(_context.IsPreviewing(), _context.IsRecording());

        return StatsSnapshotBuilder.Build(health, renderer, viewState);
    }

    private static StatsSnapshotRenderMetrics BuildRenderMetrics(
        D3D11PreviewRenderer? renderer,
        double previewMinPresentationIntervalMs)
    {
        var presentCadence = renderer?.GetPresentCadenceMetrics(previewMinPresentationIntervalMs);
        return new StatsSnapshotRenderMetrics(
            PreviewCadenceSamples: presentCadence?.SampleCount ?? 0,
            PreviewObservedFps: presentCadence?.ObservedFps ?? 0,
            PreviewAvgIntervalMs: presentCadence?.AverageIntervalMs ?? 0,
            PreviewP95IntervalMs: presentCadence?.P95IntervalMs ?? 0,
            PreviewP99IntervalMs: presentCadence?.P99IntervalMs ?? 0,
            PreviewOnePercentLowFps: presentCadence?.OnePercentLowFps ?? 0,
            PreviewSlowFrames: presentCadence?.SlowFrameCount ?? 0,
            PreviewSlowPercent: presentCadence?.SlowFramePercent ?? 0,
            PipelineLatencyMs: renderer?.GetEstimatedPipelineLatencyMs() ?? 0,
            FramesSubmitted: renderer?.FramesSubmitted ?? 0,
            FramesRendered: renderer?.FramesRendered ?? 0,
            FramesDropped: renderer?.FramesDropped ?? 0,
            PreviewNaturalWidth: renderer?.NaturalWidth ?? 0,
            PreviewNaturalHeight: renderer?.NaturalHeight ?? 0,
            PreviewRecentPresentIntervalsMs: renderer?.GetRecentPresentIntervalsMs(RecentSampleCount) ?? Array.Empty<double>(),
            PreviewRecentLatencyMs: renderer?.GetRecentPipelineLatencyMs(RecentSampleCount) ?? Array.Empty<double>());
    }
}

internal sealed class StatsSectionChromeControllerContext
{
    public required Border StatsDockPanel { get; init; }
    public required StackPanel DiagnosticsContent { get; init; }
    public required Action RefreshDiagnosticsSection { get; init; }
}

internal sealed class StatsSectionChromeController
{
    private readonly StatsSectionChromeControllerContext _context;

    public StatsSectionChromeController(StatsSectionChromeControllerContext context)
    {
        _context = context;
    }

    public void ToggleFromHeader(object sender)
    {
        if (sender is not Grid header || header.Tag is not string contentName)
        {
            return;
        }

        var content = _context.StatsDockPanel.FindName(contentName) as StackPanel;
        if (content == null)
        {
            return;
        }

        var collapsing = content.Visibility == Visibility.Visible;
        content.Visibility = collapsing ? Visibility.Collapsed : Visibility.Visible;

        var chevronName = contentName.Replace("_Content", "_Chevron", StringComparison.Ordinal);
        SetChevronExpanded(chevronName, expanded: !collapsing);

        if (!collapsing && ReferenceEquals(content, _context.DiagnosticsContent))
        {
            _context.RefreshDiagnosticsSection();
        }
    }

    public void SetVisible(string section, bool visible)
    {
        var contentName = section + "_Content";
        var content = _context.StatsDockPanel.FindName(contentName) as StackPanel;
        if (content == null)
        {
            return;
        }

        content.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        var chevronName = section + "_Chevron";
        SetChevronExpanded(chevronName, visible);

        if (visible && contentName == "Diagnostics_Content")
        {
            _context.RefreshDiagnosticsSection();
        }
    }

    private void SetChevronExpanded(string chevronName, bool expanded)
    {
        if (_context.StatsDockPanel.FindName(chevronName) is FontIcon chevron &&
            chevron.RenderTransform is RotateTransform rotate)
        {
            rotate.Angle = expanded ? 0 : -90;
        }
    }
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
            GetCaptureHealthSnapshot = context.SnapshotSources.GetCaptureHealthSnapshot,
            GetRenderer = context.SnapshotSources.GetRenderer,
            GetPreviewMinPresentationIntervalMs = context.SnapshotSources.GetPreviewMinPresentationIntervalMs,
            IsPreviewing = context.SnapshotSources.IsPreviewing,
            IsRecording = context.SnapshotSources.IsRecording
        });
    }

    private StatsOverlayController CreateOverlayController(StatsOverlayCompositionControllerContext context)
    {
        return new StatsOverlayController(new StatsOverlayControllerContext
        {
            DispatcherQueue = context.Shell.DispatcherQueue,
            StatsToggle = context.Shell.StatsToggle,
            StatsDockPanel = context.Shell.StatsDockPanel,
            FrameTimeOverlay = context.Shell.FrameTimeOverlay,
            FrameTimeOverlayToggle = context.Shell.FrameTimeOverlayToggle,
            IsWindowClosing = context.Shell.IsWindowClosing,
            SetStatsVisible = context.Shell.SetStatsVisible,
            GetStatsSnapshot = GetStatsSnapshot,
            UpdateStatsDock = _statsDockControllerGraph.RefreshDock,
            UpdateFrameTimeOverlay = UpdateFrameTimeOverlay,
            Log = context.Shell.Log
        });
    }

    private StatsSectionChromeController CreateSectionChromeController(StatsOverlayCompositionControllerContext context)
    {
        return new StatsSectionChromeController(new StatsSectionChromeControllerContext
        {
            StatsDockPanel = context.Shell.StatsDockPanel,
            DiagnosticsContent = context.DockTargets.DiagnosticsContent,
            RefreshDiagnosticsSection = _statsDockControllerGraph.RefreshDiagnosticsSection
        });
    }

    private StatsDockControllerGraph CreateDockControllerGraph(StatsOverlayCompositionControllerContext context)
    {
        return new StatsDockControllerGraph(new StatsDockControllerGraphContext
        {
            IsWindowClosing = context.Shell.IsWindowClosing,
            StatsDockPanel = context.Shell.StatsDockPanel,
            DiagnosticsContent = context.DockTargets.DiagnosticsContent,
            GetStatsSnapshot = GetStatsSnapshot,
            SessionStateValue = context.DockTargets.SessionStateValue,
            SummaryCaptureValue = context.DockTargets.SummaryCaptureValue,
            SummaryPreviewValue = context.DockTargets.SummaryPreviewValue,
            SummaryRendererFpsValue = context.DockTargets.SummaryRendererFpsValue,
            SummaryVisualFpsValue = context.DockTargets.SummaryVisualFpsValue,
            SummaryLatencyValue = context.DockTargets.SummaryLatencyValue,
            SourceResolutionValue = context.DockTargets.SourceResolutionValue,
            SourceFrameRateValue = context.DockTargets.SourceFrameRateValue,
            SourceHdrValue = context.DockTargets.SourceHdrValue,
            SourceFormatValue = context.DockTargets.SourceFormatValue,
            TelemetryOriginValue = context.DockTargets.TelemetryOriginValue,
            AdcOnOffValue = context.DockTargets.AdcOnOffValue,
            AdcGainValue = context.DockTargets.AdcGainValue,
            SourceFpsValue = context.DockTargets.SourceFpsValue,
            SourceExpectedFpsValue = context.DockTargets.SourceExpectedFpsValue,
            SourceAvgValue = context.DockTargets.SourceAvgValue,
            SourceP95Value = context.DockTargets.SourceP95Value,
            SourceJitterValue = context.DockTargets.SourceJitterValue,
            SourceGapsValue = context.DockTargets.SourceGapsValue,
            SourceDropsValue = context.DockTargets.SourceDropsValue,
            PreviewFpsValue = context.DockTargets.PreviewFpsValue,
            PreviewAvgValue = context.DockTargets.PreviewAvgValue,
            PreviewP95Value = context.DockTargets.PreviewP95Value,
            PreviewSlowValue = context.DockTargets.PreviewSlowValue,
            VisualFpsValue = context.DockTargets.VisualFpsValue,
            VisualMotionValue = context.DockTargets.VisualMotionValue,
            PipelineLatencyValue = context.DockTargets.PipelineLatencyValue,
            SourceDeliveredValue = context.DockTargets.SourceDeliveredValue,
            SourceDroppedValue = context.DockTargets.SourceDroppedValue,
            RendererRenderedValue = context.DockTargets.RendererRenderedValue,
            RendererDroppedValue = context.DockTargets.RendererDroppedValue,
            PerformanceScoreValue = context.DockTargets.PerformanceScoreValue,
            AvSyncDriftValue = context.DockTargets.AvSyncDriftValue,
            AvSyncDriftRateValue = context.DockTargets.AvSyncDriftRateValue,
            AvSyncEncoderRow = context.DockTargets.AvSyncEncoderRow,
            AvSyncEncoderValue = context.DockTargets.AvSyncEncoderValue,
            EncoderSection = context.DockTargets.EncoderSection,
            EncoderCodecValue = context.DockTargets.EncoderCodecValue,
            EncoderResolutionValue = context.DockTargets.EncoderResolutionValue,
            EncoderFrameRateValue = context.DockTargets.EncoderFrameRateValue,
            EncoderBitrateValue = context.DockTargets.EncoderBitrateValue,
            DecodeSection = context.DockTargets.DecodeSection,
            DecodeContent = context.DockTargets.DecodeContent,
            GpuContent = context.DockTargets.GpuContent,
            GetMjpegPipelineTimingDetails = context.HardwareSources.GetMjpegPipelineTimingDetails,
            GetPendingPreviewFrameCount = context.HardwareSources.GetPendingPreviewFrameCount,
            GetNvmlSnapshot = context.HardwareSources.GetNvmlSnapshot
        });
    }

    private static FrameTimeOverlayPresentationController CreateFrameTimeOverlayPresentationController(
        StatsOverlayCompositionControllerContext context)
    {
        return new FrameTimeOverlayPresentationController(new FrameTimeOverlayPresentationControllerContext
        {
            SourceValue = context.FrameTimeTargets.FrameTimeSourceValue,
            VisualValue = context.FrameTimeTargets.FrameTimeVisualValue,
            PreviewValue = context.FrameTimeTargets.FrameTimePreviewValue,
            LatencyValue = context.FrameTimeTargets.FrameTimeLatencyValue,
            StatusValue = context.FrameTimeTargets.FrameTimeStatusValue,
            Canvas = context.FrameTimeTargets.FrameTimeCanvas,
            VisualLine = context.FrameTimeTargets.FrameTimeVisualLine,
            PreviewLine = context.FrameTimeTargets.FrameTimePreviewLine,
            ExpectedLine = context.FrameTimeTargets.FrameTimeExpectedLine
        });
    }
}
