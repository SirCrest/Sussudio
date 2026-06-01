using System;
using System.Collections.Generic;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Sussudio.Models;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.ViewModels;
using Windows.Foundation;

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

internal sealed class StatsOverlayControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required ToggleButton StatsToggle { get; init; }
    public required Border StatsDockPanel { get; init; }
    public required FrameworkElement FrameTimeOverlay { get; init; }
    public required ToggleButton FrameTimeOverlayToggle { get; init; }
    public required Func<bool> IsWindowClosing { get; init; }
    public required Action<bool> SetStatsVisible { get; init; }
    public required Func<StatsSnapshot> GetStatsSnapshot { get; init; }
    public required Action UpdateStatsDock { get; init; }
    public required Action<StatsSnapshot> UpdateFrameTimeOverlay { get; init; }
    public required Action<string> Log { get; init; }
}

internal sealed class StatsOverlayController
{
    private const double StatsDockPanelWidth = 360;

    private readonly StatsOverlayControllerContext _context;
    private DispatcherQueueTimer? _statsPollTimer;
    private Storyboard? _statsDockStoryboard;
    private Storyboard? _showStatsDockStoryboard;
    private Storyboard? _hideStatsDockStoryboard;
    private bool _toggleBindingsAttached;

    public StatsOverlayController(StatsOverlayControllerContext context)
    {
        _context = context;
    }

    public bool IsFrameTimeOverlayVisible
        => _context.FrameTimeOverlay.Visibility == Visibility.Visible;

    public void AttachToggleBindings()
    {
        if (_toggleBindingsAttached)
        {
            return;
        }

        _context.StatsToggle.Checked += StatsToggle_Checked;
        _context.StatsToggle.Unchecked += StatsToggle_Unchecked;
        _context.FrameTimeOverlayToggle.Checked += FrameTimeOverlayToggle_Checked;
        _context.FrameTimeOverlayToggle.Unchecked += FrameTimeOverlayToggle_Unchecked;
        _toggleBindingsAttached = true;
    }

    public void DetachToggleBindings()
    {
        if (!_toggleBindingsAttached)
        {
            return;
        }

        _context.StatsToggle.Checked -= StatsToggle_Checked;
        _context.StatsToggle.Unchecked -= StatsToggle_Unchecked;
        _context.FrameTimeOverlayToggle.Checked -= FrameTimeOverlayToggle_Checked;
        _context.FrameTimeOverlayToggle.Unchecked -= FrameTimeOverlayToggle_Unchecked;
        _toggleBindingsAttached = false;
    }

    public void HandleStatsToggleChecked()
    {
        if (_context.IsWindowClosing())
        {
            return;
        }

        _context.SetStatsVisible(true);
    }

    public void HandleStatsToggleUnchecked()
        => _context.SetStatsVisible(false);

    public void SyncStatsVisibility(bool visible, bool immediate = false)
    {
        if (_context.StatsToggle.IsChecked != visible)
        {
            _context.StatsToggle.IsChecked = visible;
        }

        ApplyStatsVisibility(visible, immediate);
    }

    public void ApplyStatsVisibility(bool visible, bool immediate = false)
    {
        if (visible)
        {
            ShowDockPanel();
            _context.UpdateStatsDock();
            StartPolling();
            return;
        }

        if (!IsFrameTimeOverlayVisible)
        {
            StopPolling();
        }

        HideDockPanel(immediate);
    }

    public void SetFrameTimeOverlayVisible(bool visible)
    {
        if (_context.FrameTimeOverlayToggle.IsChecked != visible)
        {
            _context.FrameTimeOverlayToggle.IsChecked = visible;
        }

        if (visible)
        {
            SetVisibilityIfChanged(_context.FrameTimeOverlay, Visibility.Visible);
            StartPolling();
            _context.UpdateFrameTimeOverlay(_context.GetStatsSnapshot());
            return;
        }

        SetVisibilityIfChanged(_context.FrameTimeOverlay, Visibility.Collapsed);
        if (_context.StatsDockPanel.Visibility != Visibility.Visible)
        {
            StopPolling();
        }
    }

    public void StartPolling()
    {
        _statsPollTimer ??= _context.DispatcherQueue.CreateTimer();
        _statsPollTimer.Interval = TimeSpan.FromMilliseconds(500);
        _statsPollTimer.IsRepeating = true;
        _statsPollTimer.Tick -= StatsPollTimer_Tick;
        _statsPollTimer.Tick += StatsPollTimer_Tick;
        _statsPollTimer.Start();
    }

    public void StopPolling()
    {
        if (_statsPollTimer == null)
        {
            return;
        }

        _statsPollTimer.Stop();
        _statsPollTimer.Tick -= StatsPollTimer_Tick;
        _statsPollTimer = null;
    }

    public void ShowDockPanel()
    {
        EnsureDockAnimations();
        StopDockAnimation();
        _context.StatsDockPanel.Width = 0;
        _context.StatsDockPanel.Opacity = 0;
        _context.StatsDockPanel.Visibility = Visibility.Visible;
        _statsDockStoryboard = _showStatsDockStoryboard;
        _showStatsDockStoryboard?.Begin();
    }

    public void HideDockPanel(bool immediate = false)
    {
        EnsureDockAnimations();
        StopDockAnimation();
        if (immediate || _context.StatsDockPanel.Visibility != Visibility.Visible)
        {
            _context.StatsDockPanel.Width = 0;
            _context.StatsDockPanel.Visibility = Visibility.Collapsed;
            _context.StatsDockPanel.Opacity = 1;
            return;
        }

        _statsDockStoryboard = _hideStatsDockStoryboard;
        _hideStatsDockStoryboard?.Begin();
    }

    private void StatsPollTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            _context.UpdateStatsDock();
            if (IsFrameTimeOverlayVisible)
            {
                _context.UpdateFrameTimeOverlay(_context.GetStatsSnapshot());
            }
        }
        catch (Exception ex)
        {
            _context.Log($"STATS_POLL_TIMER_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void StatsToggle_Checked(object sender, RoutedEventArgs e)
        => HandleStatsToggleChecked();

    private void StatsToggle_Unchecked(object sender, RoutedEventArgs e)
        => HandleStatsToggleUnchecked();

    private void FrameTimeOverlayToggle_Checked(object sender, RoutedEventArgs e)
        => SetFrameTimeOverlayVisible(true);

    private void FrameTimeOverlayToggle_Unchecked(object sender, RoutedEventArgs e)
        => SetFrameTimeOverlayVisible(false);

    private void StopDockAnimation()
    {
        _statsDockStoryboard?.Stop();
        _statsDockStoryboard = null;
    }

    private void EnsureDockAnimations()
    {
        _showStatsDockStoryboard ??= CreateStatsDockStoryboard(showing: true);
        _hideStatsDockStoryboard ??= CreateStatsDockStoryboard(showing: false);
    }

    private Storyboard CreateStatsDockStoryboard(bool showing)
    {
        var durationMs = showing ? 400 : 300;
        var easing = new CubicEase { EasingMode = showing ? EasingMode.EaseOut : EasingMode.EaseIn };
        var duration = TimeSpan.FromMilliseconds(durationMs);

        var storyboard = new Storyboard();

        var widthAnim = new DoubleAnimation
        {
            To = showing ? StatsDockPanelWidth : 0,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(widthAnim, _context.StatsDockPanel);
        Storyboard.SetTargetProperty(widthAnim, "Width");

        var fade = new DoubleAnimation
        {
            To = showing ? 1 : 0,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, _context.StatsDockPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");

        storyboard.Children.Add(widthAnim);
        storyboard.Children.Add(fade);
        storyboard.Completed += (_, _) =>
        {
            if (!ReferenceEquals(_statsDockStoryboard, storyboard))
            {
                return;
            }

            _statsDockStoryboard = null;
            if (showing)
            {
                _context.StatsDockPanel.Width = StatsDockPanelWidth;
                _context.StatsDockPanel.Opacity = 1;
                return;
            }

            _context.StatsDockPanel.Width = 0;
            _context.StatsDockPanel.Visibility = Visibility.Collapsed;
            _context.StatsDockPanel.Opacity = 1;
        };

        return storyboard;
    }

    private static void SetVisibilityIfChanged(UIElement element, Visibility visibility)
    {
        if (element.Visibility != visibility)
        {
            element.Visibility = visibility;
        }
    }
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

internal sealed class FrameTimeOverlayPresentationControllerContext
{
    public required TextBlock SourceValue { get; init; }
    public required TextBlock VisualValue { get; init; }
    public required TextBlock PreviewValue { get; init; }
    public required TextBlock LatencyValue { get; init; }
    public required TextBlock StatusValue { get; init; }
    public required Canvas Canvas { get; init; }
    public required Polyline VisualLine { get; init; }
    public required Polyline PreviewLine { get; init; }
    public required Line ExpectedLine { get; init; }
}

internal sealed class FrameTimeOverlayPresentationController
{
    private readonly FrameTimeOverlayPresentationControllerContext _context;

    public FrameTimeOverlayPresentationController(FrameTimeOverlayPresentationControllerContext context)
    {
        _context = context;
    }

    public void Apply(StatsSnapshot snapshot)
    {
        var presentation = StatsPresentationBuilder.BuildFrameTimePresentation(snapshot);

        SetTextIfChanged(_context.SourceValue, presentation.SourceText);
        SetTextIfChanged(_context.VisualValue, presentation.VisualText);
        SetTextIfChanged(_context.PreviewValue, presentation.PreviewText);
        SetTextIfChanged(_context.LatencyValue, presentation.LatencyText);
        SetTextIfChanged(_context.StatusValue, presentation.StatusText);

        UpdateExpectedLine(presentation.Range);
        UpdateLine(_context.VisualLine, presentation.VisualSamples, presentation.Range);
        UpdateLine(_context.PreviewLine, presentation.PreviewSamples, presentation.Range);
    }

    private void UpdateLine(
        Polyline line,
        IReadOnlyList<double> samples,
        StatsFrameTimeRange range)
    {
        line.Points.Clear();
        if (samples.Count <= 1)
        {
            return;
        }

        var canvasSize = FrameTimeOverlayGeometry.ResolveCanvasSize(
            _context.Canvas.ActualWidth,
            _context.Canvas.ActualHeight);
        for (var i = 0; i < samples.Count; i++)
        {
            line.Points.Add(FrameTimeOverlayGeometry.ProjectSample(i, samples.Count, samples[i], range, canvasSize));
        }
    }

    private void UpdateExpectedLine(StatsFrameTimeRange range)
    {
        var canvasSize = FrameTimeOverlayGeometry.ResolveCanvasSize(
            _context.Canvas.ActualWidth,
            _context.Canvas.ActualHeight);
        var line = FrameTimeOverlayGeometry.ProjectExpectedLine(range, canvasSize);
        _context.ExpectedLine.X2 = line.X2;
        _context.ExpectedLine.Y1 = line.Y;
        _context.ExpectedLine.Y2 = line.Y;
    }

    private static void SetTextIfChanged(TextBlock target, string value)
    {
        if (!string.Equals(target.Text, value, StringComparison.Ordinal))
        {
            target.Text = value;
        }
    }
}

internal readonly record struct FrameTimeOverlayCanvasSize(double Width, double Height);

internal readonly record struct FrameTimeOverlayExpectedLineGeometry(double X2, double Y);

internal static class FrameTimeOverlayGeometry
{
    public const double FallbackWidth = 500;
    public const double FallbackHeight = 92;

    public static FrameTimeOverlayCanvasSize ResolveCanvasSize(double actualWidth, double actualHeight)
    {
        var width = actualWidth > 1 ? actualWidth : FallbackWidth;
        var height = actualHeight > 1 ? actualHeight : FallbackHeight;
        return new FrameTimeOverlayCanvasSize(width, height);
    }

    public static Point ProjectSample(
        int sampleIndex,
        int sampleCount,
        double sampleMs,
        StatsFrameTimeRange range,
        FrameTimeOverlayCanvasSize canvasSize)
    {
        var x = sampleCount <= 1 ? 0 : sampleIndex * canvasSize.Width / (sampleCount - 1);
        var y = ProjectY(sampleMs, range, canvasSize.Height);
        return new Point(x, y);
    }

    public static FrameTimeOverlayExpectedLineGeometry ProjectExpectedLine(
        StatsFrameTimeRange range,
        FrameTimeOverlayCanvasSize canvasSize)
    {
        var y = ProjectY(range.ExpectedMs, range, canvasSize.Height);
        return new FrameTimeOverlayExpectedLineGeometry(canvasSize.Width, y);
    }

    private static double ProjectY(double frameTimeMs, StatsFrameTimeRange range, double height)
    {
        var normalized = Math.Clamp((frameTimeMs - range.MinMs) / range.SpanMs, 0.0, 1.0);
        return height - normalized * height;
    }
}

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

internal enum StatsDockSimpleRowPool
{
    Decode,
    Gpu
}

internal sealed class StatsDockPresentationControllerContext
{
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
}

internal sealed class StatsDockPresentationController
{
    private static readonly SolidColorBrush MetricNeutralBrush = new(Windows.UI.Color.FromArgb(0xFF, 0xF1, 0xF1, 0xF1));
    private static readonly SolidColorBrush MetricGoodBrush = new(Windows.UI.Color.FromArgb(0xFF, 0x70, 0xF0, 0x8B));
    private static readonly SolidColorBrush MetricInfoBrush = new(Windows.UI.Color.FromArgb(0xFF, 0x55, 0xD6, 0xFF));
    private static readonly SolidColorBrush MetricWarningBrush = new(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xC8, 0x57));
    private static readonly SolidColorBrush MetricBadBrush = new(Windows.UI.Color.FromArgb(0xFF, 0xFF, 0x6B, 0x6B));

    private readonly StatsDockPresentationControllerContext _context;

    public StatsDockPresentationController(StatsDockPresentationControllerContext context)
    {
        _context = context;
    }

    public void Apply(StatsDockPresentation presentation)
    {
        SetTextIfChanged(_context.SessionStateValue, presentation.SessionState);
        SetTextIfChanged(_context.SummaryCaptureValue, presentation.SummaryCapture);
        SetTextIfChanged(_context.SummaryPreviewValue, presentation.SummaryPreview);
        SetTextIfChanged(_context.SummaryRendererFpsValue, presentation.SummaryRendererFps);
        SetTextIfChanged(_context.SummaryVisualFpsValue, presentation.SummaryVisualFps);
        SetTextIfChanged(_context.SummaryLatencyValue, presentation.SummaryLatency);
        SetMetricBrush(_context.SummaryCaptureValue, presentation.SummaryCaptureStatus);
        SetMetricBrush(_context.SummaryRendererFpsValue, presentation.SummaryRendererFpsStatus);
        SetMetricBrush(_context.SummaryVisualFpsValue, presentation.SummaryVisualFpsStatus);
        SetMetricBrush(_context.SummaryLatencyValue, presentation.SummaryLatencyStatus);
        SetTextIfChanged(_context.SourceResolutionValue, presentation.SourceResolution);
        SetTextIfChanged(_context.SourceFrameRateValue, presentation.SourceFrameRate);
        SetTextIfChanged(_context.SourceHdrValue, presentation.SourceHdr);
        SetTextIfChanged(_context.SourceFormatValue, presentation.SourceFormat);
        SetTextIfChanged(_context.TelemetryOriginValue, presentation.TelemetryOrigin);
        SetTextIfChanged(_context.AdcOnOffValue, presentation.AdcOnOff);
        SetTextIfChanged(_context.AdcGainValue, presentation.AdcGain);
        SetTextIfChanged(_context.SourceFpsValue, presentation.SourceFps);
        SetTextIfChanged(_context.SourceExpectedFpsValue, presentation.SourceExpectedFps);
        SetTextIfChanged(_context.SourceAvgValue, presentation.SourceAvg);
        SetTextIfChanged(_context.SourceP95Value, presentation.SourceP95);
        SetTextIfChanged(_context.SourceJitterValue, presentation.SourceJitter);
        SetTextIfChanged(_context.SourceGapsValue, presentation.SourceGaps);
        SetTextIfChanged(_context.SourceDropsValue, presentation.SourceDrops);
        SetTextIfChanged(_context.PreviewFpsValue, presentation.PreviewFps);
        SetTextIfChanged(_context.PreviewAvgValue, presentation.PreviewAvg);
        SetTextIfChanged(_context.PreviewP95Value, presentation.PreviewP95);
        SetTextIfChanged(_context.PreviewSlowValue, presentation.PreviewSlow);
        SetTextIfChanged(_context.VisualFpsValue, presentation.VisualFps);
        SetTextIfChanged(_context.VisualMotionValue, presentation.VisualMotion);
        SetMetricBrush(_context.VisualFpsValue, presentation.VisualFpsStatus);
        SetTextIfChanged(_context.PipelineLatencyValue, presentation.PipelineLatency);
        SetTextIfChanged(_context.SourceDeliveredValue, presentation.SourceDelivered);
        SetTextIfChanged(_context.SourceDroppedValue, presentation.SourceDropped);
        SetTextIfChanged(_context.RendererRenderedValue, presentation.RendererRendered);
        SetTextIfChanged(_context.RendererDroppedValue, presentation.RendererDropped);
        SetTextIfChanged(_context.PerformanceScoreValue, presentation.PerformanceScore);
        SetTextIfChanged(_context.AvSyncDriftValue, presentation.AvSyncDrift);
        SetTextIfChanged(_context.AvSyncDriftRateValue, presentation.AvSyncDriftRate);

        SetVisibilityIfChanged(_context.AvSyncEncoderRow, presentation.EncoderDriftVisible ? Visibility.Visible : Visibility.Collapsed);
        if (presentation.EncoderDriftVisible)
        {
            SetTextIfChanged(_context.AvSyncEncoderValue, presentation.EncoderDrift);
        }

        SetVisibilityIfChanged(_context.EncoderSection, presentation.EncoderActive ? Visibility.Visible : Visibility.Collapsed);
        if (presentation.EncoderActive)
        {
            SetTextIfChanged(_context.EncoderCodecValue, presentation.EncoderCodec);
            SetTextIfChanged(_context.EncoderResolutionValue, presentation.EncoderResolution);
            SetTextIfChanged(_context.EncoderFrameRateValue, presentation.EncoderFrameRate);
            SetTextIfChanged(_context.EncoderBitrateValue, presentation.EncoderBitrate);
        }
    }

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

    private static void SetTextIfChanged(TextBlock target, string value)
    {
        if (!string.Equals(target.Text, value, System.StringComparison.Ordinal))
        {
            target.Text = value;
        }
    }

    private static void SetVisibilityIfChanged(UIElement element, Visibility visibility)
    {
        if (element.Visibility != visibility)
        {
            element.Visibility = visibility;
        }
    }
}

internal sealed class StatsDockRowChromeControllerContext
{
    public required FrameworkElement ResourceOwner { get; init; }
}

internal sealed class StatsDockRowChromeController
{
    private readonly StatsDockRowChromePresenter _rowChrome;
    private readonly List<StatsDockRowChromeSlot> _decodeRowPool = new();
    private readonly List<StatsDockRowChromeSlot> _gpuRowPool = new();

    public StatsDockRowChromeController(StatsDockRowChromeControllerContext context)
    {
        _rowChrome = new StatsDockRowChromePresenter(context.ResourceOwner);
    }

    public void CollapseSimpleRows(StatsDockSimpleRowPool poolKind)
    {
        StatsDockRowChromePresenter.CollapseRows(GetSimpleRowPool(poolKind));
    }

    public void UpdateSimpleRows(
        StatsDockSimpleRowPool poolKind,
        StackPanel container,
        IReadOnlyList<StatsHardwareRowPresentation> rows,
        int minimumCapacity)
    {
        var pool = GetSimpleRowPool(poolKind);
        EnsureRowPool(container, pool, Math.Max(minimumCapacity, rows.Count));
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            _rowChrome.UpdateRowSlot(pool[i], row.Label, row.Value, alt: (i % 2) != 0);
        }

        StatsDockRowChromePresenter.CollapseRows(pool, startIndex: rows.Count);
    }

    private List<StatsDockRowChromeSlot> GetSimpleRowPool(StatsDockSimpleRowPool poolKind)
        => poolKind switch
        {
            StatsDockSimpleRowPool.Decode => _decodeRowPool,
            StatsDockSimpleRowPool.Gpu => _gpuRowPool,
            _ => throw new ArgumentOutOfRangeException(nameof(poolKind), poolKind, null)
        };

    private void EnsureRowPool(StackPanel container, List<StatsDockRowChromeSlot> pool, int requiredCount)
    {
        while (pool.Count < requiredCount)
        {
            var slot = _rowChrome.CreateRowSlot();
            pool.Add(slot);
            container.Children.Add(slot.Row);
        }
    }
}

internal sealed class StatsDiagnosticRowsControllerContext
{
    public required FrameworkElement ResourceOwner { get; init; }
    public required StackPanel DiagnosticsContent { get; init; }
}

internal sealed class StatsDiagnosticRowsController
{
    private readonly StatsDiagnosticRowsControllerContext _context;
    private readonly StatsDockRowChromePresenter _rowChrome;
    private readonly List<DiagnosticsPoolSlot> _diagnosticsRowPool = new();
    private TextBlock? _diagnosticsEmptyStateTextBlock;

    public StatsDiagnosticRowsController(StatsDiagnosticRowsControllerContext context)
    {
        _context = context;
        _rowChrome = new StatsDockRowChromePresenter(context.ResourceOwner);
    }

    public void UpdateDiagnostics(StatsDiagnosticRowsPresentation presentation)
    {
        EnsureDiagnosticsEmptyState();

        if (presentation.IsEmpty)
        {
            StatsDockRowChromePresenter.SetVisibilityIfChanged(_diagnosticsEmptyStateTextBlock!, Visibility.Visible);
            CollapseDiagnosticsPoolSlots();
            return;
        }

        var slotIndex = 0;
        foreach (var row in presentation.Rows)
        {
            EnsureDiagnosticsPoolCapacity(slotIndex + 1);
            UpdateDiagnosticsPoolSlot(
                _diagnosticsRowPool[slotIndex],
                row.GroupHeader,
                row.Label,
                row.Value,
                row.IsAlternate);
            slotIndex++;
        }

        StatsDockRowChromePresenter.SetVisibilityIfChanged(_diagnosticsEmptyStateTextBlock!, Visibility.Collapsed);
        CollapseDiagnosticsPoolSlots(startIndex: slotIndex);
    }

    private void EnsureDiagnosticsEmptyState()
    {
        if (_diagnosticsEmptyStateTextBlock != null)
        {
            return;
        }

        _diagnosticsEmptyStateTextBlock = new TextBlock
        {
            Text = "No diagnostics available",
            Style = _rowChrome.GetStyle("DockStatsLabelStyle"),
            Visibility = Visibility.Collapsed
        };
        _context.DiagnosticsContent.Children.Add(_diagnosticsEmptyStateTextBlock);
    }

    private void EnsureDiagnosticsPoolCapacity(int requiredCount)
    {
        while (_diagnosticsRowPool.Count < requiredCount)
        {
            var rowSlot = _rowChrome.CreateRowSlot();
            var header = CreateDiagnosticGroupHeader("");
            header.Visibility = Visibility.Collapsed;
            _context.DiagnosticsContent.Children.Add(header);
            _context.DiagnosticsContent.Children.Add(rowSlot.Row);
            _diagnosticsRowPool.Add(new DiagnosticsPoolSlot(rowSlot, header));
        }
    }

    private void UpdateDiagnosticsPoolSlot(
        DiagnosticsPoolSlot slot,
        string? groupHeader,
        string label,
        string value,
        bool alt)
    {
        if (groupHeader != null)
        {
            StatsDockRowChromePresenter.SetTextIfChanged(slot.GroupHeader, groupHeader);
            StatsDockRowChromePresenter.SetVisibilityIfChanged(slot.GroupHeader, Visibility.Visible);
        }
        else
        {
            StatsDockRowChromePresenter.SetVisibilityIfChanged(slot.GroupHeader, Visibility.Collapsed);
        }

        _rowChrome.UpdateRowSlot(slot.RowSlot, label, value, alt);
    }

    private void CollapseDiagnosticsPoolSlots(int startIndex = 0)
    {
        for (var i = startIndex; i < _diagnosticsRowPool.Count; i++)
        {
            var slot = _diagnosticsRowPool[i];
            StatsDockRowChromePresenter.SetVisibilityIfChanged(slot.RowSlot.Row, Visibility.Collapsed);
            StatsDockRowChromePresenter.SetVisibilityIfChanged(slot.GroupHeader, Visibility.Collapsed);
        }
    }

    private TextBlock CreateDiagnosticGroupHeader(string title)
    {
        return new TextBlock
        {
            Text = title,
            Margin = new Thickness(0, 8, 0, 2),
            Style = _rowChrome.GetStyle("DockStatsSectionHeaderStyle")
        };
    }

    private sealed record DiagnosticsPoolSlot(
        StatsDockRowChromeSlot RowSlot,
        TextBlock GroupHeader);
}

internal sealed record StatsDockRowChromeSlot(Border Row, TextBlock Label, TextBlock Value);

internal sealed class StatsDockRowChromePresenter
{
    private readonly FrameworkElement _resourceOwner;

    public StatsDockRowChromePresenter(FrameworkElement resourceOwner)
    {
        _resourceOwner = resourceOwner;
    }

    public StatsDockRowChromeSlot CreateRowSlot(string label = "", string value = "", bool alt = false)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelBlock = new TextBlock
        {
            Text = label,
            Style = GetStyle("DockStatsLabelStyle")
        };

        var valueBlock = new TextBlock
        {
            Text = value,
            Style = GetStyle("DockStatsValueStyle"),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(valueBlock, 1);

        grid.Children.Add(labelBlock);
        grid.Children.Add(valueBlock);

        var row = new Border
        {
            Style = GetRowStyle(alt),
            Child = grid
        };
        return new StatsDockRowChromeSlot(row, labelBlock, valueBlock);
    }

    public void UpdateRowSlot(StatsDockRowChromeSlot slot, string label, string value, bool alt)
    {
        SetTextIfChanged(slot.Label, label);
        SetTextIfChanged(slot.Value, value);
        var targetStyle = GetRowStyle(alt);
        if (!ReferenceEquals(slot.Row.Style, targetStyle))
        {
            slot.Row.Style = targetStyle;
        }

        SetVisibilityIfChanged(slot.Row, Visibility.Visible);
    }

    public Style GetStyle(string key) => (Style)_resourceOwner.Resources[key];

    private Style GetRowStyle(bool alt)
        => GetStyle(alt ? "DockStatsRowAltStyle" : "DockStatsRowStyle");

    public static void CollapseRows(IReadOnlyList<StatsDockRowChromeSlot> pool, int startIndex = 0)
    {
        for (var i = startIndex; i < pool.Count; i++)
        {
            SetVisibilityIfChanged(pool[i].Row, Visibility.Collapsed);
        }
    }

    public static void SetTextIfChanged(TextBlock target, string value)
    {
        if (!string.Equals(target.Text, value, StringComparison.Ordinal))
        {
            target.Text = value;
        }
    }

    public static void SetVisibilityIfChanged(UIElement element, Visibility visibility)
    {
        if (element.Visibility != visibility)
        {
            element.Visibility = visibility;
        }
    }
}

internal sealed class StatsDockRefreshControllerContext
{
    public required Func<bool> IsWindowClosing { get; init; }
    public required Func<bool> IsStatsDockVisible { get; init; }
    public required Func<bool> IsDiagnosticsSectionVisible { get; init; }
    public required Func<StatsSnapshot> GetStatsSnapshot { get; init; }
    public required StatsDockPresentationController DockPresentationController { get; init; }
    public required StatsDiagnosticRowsController DiagnosticRowsController { get; init; }
    public required StatsHardwareRowsController HardwareRowsController { get; init; }
}

internal sealed class StatsDockRefreshController
{
    private readonly StatsDockRefreshControllerContext _context;

    public StatsDockRefreshController(StatsDockRefreshControllerContext context)
    {
        _context = context;
    }

    public void RefreshDock()
    {
        if (_context.IsWindowClosing() || !_context.IsStatsDockVisible())
        {
            return;
        }

        var snapshot = _context.GetStatsSnapshot();
        var presentation = StatsPresentationBuilder.BuildDockPresentation(snapshot);

        _context.DockPresentationController.Apply(presentation);

        UpdateDiagnosticsSection(snapshot.SourceTelemetryDetails ?? Array.Empty<SourceTelemetryDetailEntry>(), snapshot.DiagnosticSummary);
        _context.HardwareRowsController.UpdateDecodeSection();
        _context.HardwareRowsController.UpdateGpuSection();
    }

    public void RefreshDiagnosticsSection()
    {
        var snapshot = _context.GetStatsSnapshot();
        UpdateDiagnosticsSection(snapshot.SourceTelemetryDetails ?? Array.Empty<SourceTelemetryDetailEntry>(), snapshot.DiagnosticSummary);
    }

    private void UpdateDiagnosticsSection(IReadOnlyList<SourceTelemetryDetailEntry> telemetryDetails, string? diagnosticSummary)
    {
        if (!_context.IsDiagnosticsSectionVisible())
        {
            return;
        }

        var presentation = StatsPresentationBuilder.BuildDiagnosticRows(telemetryDetails, diagnosticSummary);
        _context.DiagnosticRowsController.UpdateDiagnostics(presentation);
    }
}

internal sealed class StatsHardwareRowsControllerContext
{
    public required UIElement DecodeSection { get; init; }
    public required StackPanel DecodeContent { get; init; }
    public required StackPanel GpuContent { get; init; }
    public required StatsDockRowChromeController RowChromeController { get; init; }
    public required StatsHardwareRowsInputProvider InputProvider { get; init; }
}

internal sealed class StatsHardwareRowsInputProviderContext
{
    public required Func<ParallelMjpegDecodePipeline.PipelineTimingMetrics?> GetMjpegPipelineTimingDetails { get; init; }
    public required Func<int?> GetPendingPreviewFrameCount { get; init; }
    public required Func<NvmlSnapshot?> GetNvmlSnapshot { get; init; }
}

internal sealed class StatsHardwareRowsInputProvider
{
    private readonly StatsHardwareRowsInputProviderContext _context;

    public StatsHardwareRowsInputProvider(StatsHardwareRowsInputProviderContext context)
    {
        _context = context;
    }

    public StatsHardwareDecodeRowsInput? GetDecodeRowsInput()
    {
        var mjpegMetrics = _context.GetMjpegPipelineTimingDetails();
        if (!mjpegMetrics.HasValue || mjpegMetrics.Value.DecoderCount <= 0)
        {
            return null;
        }

        return StatsHardwareRowsInputBuilder.BuildDecodeRowsInput(
            mjpegMetrics.Value,
            _context.GetPendingPreviewFrameCount());
    }

    public StatsHardwareGpuRowsInput? GetGpuRowsInput()
        => StatsHardwareRowsInputBuilder.BuildGpuRowsInput(_context.GetNvmlSnapshot());
}

internal static class StatsHardwareRowsInputBuilder
{
    public static StatsHardwareDecodeRowsInput BuildDecodeRowsInput(
        ParallelMjpegDecodePipeline.PipelineTimingMetrics mjpeg,
        int? pendingPreviewFrameCount)
    {
        var workers = new StatsHardwareDecodeWorkerRowInput[mjpeg.PerDecoder.Length];
        for (var i = 0; i < mjpeg.PerDecoder.Length; i++)
        {
            var worker = mjpeg.PerDecoder[i];
            workers[i] = new StatsHardwareDecodeWorkerRowInput(
                WorkerIndex: worker.WorkerIndex,
                AvgMs: worker.AvgMs,
                P95Ms: worker.P95Ms);
        }

        return new StatsHardwareDecodeRowsInput(
            DecoderCount: mjpeg.DecoderCount,
            DecodeAvgMs: mjpeg.DecodeAvgMs,
            DecodeP95Ms: mjpeg.DecodeP95Ms,
            ReorderAvgMs: mjpeg.ReorderAvgMs,
            ReorderP95Ms: mjpeg.ReorderP95Ms,
            PipelineAvgMs: mjpeg.PipelineAvgMs,
            PipelineP95Ms: mjpeg.PipelineP95Ms,
            TotalEmitted: mjpeg.TotalEmitted,
            TotalDropped: mjpeg.TotalDropped,
            CompressedQueueDepth: mjpeg.CompressedQueueDepth,
            CompressedQueueBytes: mjpeg.CompressedQueueBytes,
            CompressedQueueByteBudget: mjpeg.CompressedQueueByteBudget,
            ReorderBufferDepth: mjpeg.ReorderBufferDepth,
            ReorderSkips: mjpeg.ReorderSkips,
            PendingPreviewFrameCount: pendingPreviewFrameCount,
            PerDecoder: workers);
    }

    public static StatsHardwareGpuRowsInput? BuildGpuRowsInput(NvmlSnapshot? nvml)
        => nvml == null
            ? null
            : new StatsHardwareGpuRowsInput(
                GpuName: nvml.GpuName,
                GpuUtilizationPercent: nvml.GpuUtilizationPercent,
                GpuMemoryUtilizationPercent: nvml.GpuMemoryUtilizationPercent,
                NvdecUtilizationPercent: nvml.NvdecUtilizationPercent,
                NvencUtilizationPercent: nvml.NvencUtilizationPercent,
                PcieTxMBps: nvml.PcieTxMBps,
                PcieRxMBps: nvml.PcieRxMBps,
                VramUsedMB: nvml.VramUsedMB,
                VramTotalMB: nvml.VramTotalMB,
                GpuTemperatureC: nvml.GpuTemperatureC,
                GpuPowerW: nvml.GpuPowerW,
                GpuClockMHz: nvml.GpuClockMHz,
                GpuMemClockMHz: nvml.GpuMemClockMHz);
}

internal sealed class StatsHardwareRowsController
{
    private const int MaxExpectedDecodeRowCount = 14;
    private const int FixedGpuRowCount = 10;

    private readonly StatsHardwareRowsControllerContext _context;

    public StatsHardwareRowsController(StatsHardwareRowsControllerContext context)
    {
        _context = context;
    }

    public void UpdateDecodeSection()
    {
        var input = _context.InputProvider.GetDecodeRowsInput();
        if (!input.HasValue || input.Value.DecoderCount <= 0)
        {
            _context.DecodeSection.Visibility = Visibility.Collapsed;
            _context.RowChromeController.CollapseSimpleRows(StatsDockSimpleRowPool.Decode);
            return;
        }

        _context.DecodeSection.Visibility = Visibility.Visible;
        var rows = StatsPresentationBuilder.BuildHardwareDecodeRows(input.Value);
        _context.RowChromeController.UpdateSimpleRows(
            StatsDockSimpleRowPool.Decode,
            _context.DecodeContent,
            rows,
            MaxExpectedDecodeRowCount);
    }

    public void UpdateGpuSection()
    {
        var rows = StatsPresentationBuilder.BuildHardwareGpuRows(_context.InputProvider.GetGpuRowsInput());
        _context.RowChromeController.UpdateSimpleRows(
            StatsDockSimpleRowPool.Gpu,
            _context.GpuContent,
            rows,
            FixedGpuRowCount);
    }
}
