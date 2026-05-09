using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using Windows.Foundation;
using WinRT.Interop;
using Sussudio.Services.Audio;
using Sussudio.Services.Capture;
using Sussudio.Services.Configuration;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio;

// Stats dock and frame-time overlay presentation. This partial only projects
// diagnostics into UI elements; the underlying snapshot data is owned by
// AutomationDiagnosticsHub and CaptureService.
public sealed partial class MainWindow
{
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
    {
        if (visible)
        {
            ShowStatsDockPanel();
            UpdateStatsDock();
            StartStatsDockPolling();
            return;
        }

        if (!IsFrameTimeOverlayVisible())
        {
            StopStatsDockPolling();
        }
        HideStatsDockPanel(immediate);
    }

    private void FrameTimeOverlayToggle_Checked(object sender, RoutedEventArgs e)
    {
        SetVisibilityIfChanged(FrameTimeOverlay, Visibility.Visible);
        StartStatsDockPolling();
        UpdateFrameTimeOverlay(GetStatsSnapshot());
    }

    private void FrameTimeOverlayToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        SetVisibilityIfChanged(FrameTimeOverlay, Visibility.Collapsed);
        if (StatsDockPanel.Visibility != Visibility.Visible)
        {
            StopStatsDockPolling();
        }
    }

    private void SetFrameTimeOverlayVisible(bool visible)
    {
        if (FrameTimeOverlayToggle.IsChecked != visible)
        {
            FrameTimeOverlayToggle.IsChecked = visible;
        }

        if (visible)
        {
            SetVisibilityIfChanged(FrameTimeOverlay, Visibility.Visible);
            StartStatsDockPolling();
            UpdateFrameTimeOverlay(GetStatsSnapshot());
            return;
        }

        SetVisibilityIfChanged(FrameTimeOverlay, Visibility.Collapsed);
        if (StatsDockPanel.Visibility != Visibility.Visible)
        {
            StopStatsDockPolling();
        }
    }
    private void StatsSectionHeader_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not Grid header || header.Tag is not string contentName)
        {
            return;
        }

        var content = StatsDockPanel.FindName(contentName) as StackPanel;
        if (content == null)
        {
            return;
        }

        var collapsing = content.Visibility == Visibility.Visible;
        content.Visibility = collapsing ? Visibility.Collapsed : Visibility.Visible;

        var chevronName = contentName.Replace("_Content", "_Chevron", StringComparison.Ordinal);
        if (StatsDockPanel.FindName(chevronName) is FontIcon chevron &&
            chevron.RenderTransform is RotateTransform rotate)
        {
            rotate.Angle = collapsing ? -90 : 0;
        }

        if (!collapsing && ReferenceEquals(content, Diagnostics_Content))
        {
            var snapshot = GetStatsSnapshot();
            UpdateDiagnosticsSection(snapshot.SourceTelemetryDetails ?? Array.Empty<SourceTelemetryDetailEntry>(), snapshot.DiagnosticSummary);
        }
    }
    private void SetStatsSectionVisible(string section, bool visible)
    {
        var contentName = section + "_Content";
        var content = StatsDockPanel.FindName(contentName) as StackPanel;
        if (content == null)
        {
            return;
        }

        content.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        var chevronName = section + "_Chevron";
        if (StatsDockPanel.FindName(chevronName) is FontIcon chevron &&
            chevron.RenderTransform is RotateTransform rotate)
        {
            rotate.Angle = visible ? 0 : -90;
        }

        if (visible && contentName == "Diagnostics_Content")
        {
            var snapshot = GetStatsSnapshot();
            UpdateDiagnosticsSection(snapshot.SourceTelemetryDetails ?? Array.Empty<SourceTelemetryDetailEntry>(), snapshot.DiagnosticSummary);
        }
    }
    private void StartStatsDockPolling()
    {
        _statsPollTimer ??= _dispatcherQueue.CreateTimer();
        _statsPollTimer.Interval = TimeSpan.FromMilliseconds(500);
        _statsPollTimer.IsRepeating = true;
        _statsPollTimer.Tick -= StatsPollTimer_Tick;
        _statsPollTimer.Tick += StatsPollTimer_Tick;
        _statsPollTimer.Start();
    }
    private void StopStatsDockPolling()
    {
        if (_statsPollTimer == null)
        {
            return;
        }

        _statsPollTimer.Stop();
        _statsPollTimer.Tick -= StatsPollTimer_Tick;
        _statsPollTimer = null;
    }
    private void StatsPollTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            UpdateStatsDock();
            if (IsFrameTimeOverlayVisible())
            {
                UpdateFrameTimeOverlay(GetStatsSnapshot());
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"STATS_POLL_TIMER_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }
    private void ShowStatsDockPanel()
    {
        EnsureStatsDockAnimations();
        StopStatsDockAnimation();
        StatsDockPanel.Width = 0;
        StatsDockPanel.Opacity = 0;
        StatsDockPanel.Visibility = Visibility.Visible;
        _statsDockStoryboard = _showStatsDockStoryboard;
        _showStatsDockStoryboard?.Begin();
    }
    private void HideStatsDockPanel(bool immediate = false)
    {
        EnsureStatsDockAnimations();
        StopStatsDockAnimation();
        if (immediate || StatsDockPanel.Visibility != Visibility.Visible)
        {
            StatsDockPanel.Width = 0;
            StatsDockPanel.Visibility = Visibility.Collapsed;
            StatsDockPanel.Opacity = 1;
            return;
        }

        _statsDockStoryboard = _hideStatsDockStoryboard;
        _hideStatsDockStoryboard?.Begin();
    }
    private void StopStatsDockAnimation()
    {
        _statsDockStoryboard?.Stop();
        _statsDockStoryboard = null;
    }
    private void EnsureStatsDockAnimations()
    {
        _showStatsDockStoryboard ??= CreateStatsDockStoryboard(showing: true);
        _hideStatsDockStoryboard ??= CreateStatsDockStoryboard(showing: false);
    }
    private const double StatsDockPanelWidth = 360;
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
        Storyboard.SetTarget(widthAnim, StatsDockPanel);
        Storyboard.SetTargetProperty(widthAnim, "Width");

        var fade = new DoubleAnimation
        {
            To = showing ? 1 : 0,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, StatsDockPanel);
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
                StatsDockPanel.Width = StatsDockPanelWidth;
                StatsDockPanel.Opacity = 1;
                return;
            }

            StatsDockPanel.Width = 0;
            StatsDockPanel.Visibility = Visibility.Collapsed;
            StatsDockPanel.Opacity = 1;
        };

        return storyboard;
    }
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
    private void UpdateDecodeSection()
    {
        var mjpegMetrics = ViewModel.GetMjpegPipelineTimingDetails();
        if (mjpegMetrics is not { DecoderCount: > 0 } mjpeg)
        {
            DecodeSection.Visibility = Visibility.Collapsed;
            CollapseDiagnosticRows(_decodeRowPool);
            return;
        }

        DecodeSection.Visibility = Visibility.Visible;
        EnsureDiagnosticRowPool(Decode_Content, _decodeRowPool, MaxExpectedDecodeRowCount);

        var rowIndex = 0;
        void SetRow(string label, string value)
        {
            EnsureDiagnosticRowPool(Decode_Content, _decodeRowPool, rowIndex + 1);
            UpdateDiagnosticRowSlot(_decodeRowPool[rowIndex], label, value, alt: (rowIndex % 2) != 0);
            rowIndex++;
        }

        var effectiveFrameTimeMs = mjpeg.DecodeAvgMs / mjpeg.DecoderCount;
        var effectiveFps = effectiveFrameTimeMs > 0 ? 1000.0 / effectiveFrameTimeMs : 0;

        SetRow("Throughput", $"{effectiveFrameTimeMs:0.00}ms ({effectiveFps:0}fps peak)");
        SetRow("Decode", $"{mjpeg.DecodeAvgMs:0.00} / {mjpeg.DecodeP95Ms:0.00}ms  avg/P95 ({mjpeg.DecoderCount}T)");
        SetRow("Reorder", $"{mjpeg.ReorderAvgMs:0.00} / {mjpeg.ReorderP95Ms:0.00}ms  avg/P95");
        SetRow("Pipeline", $"{mjpeg.PipelineAvgMs:0.00} / {mjpeg.PipelineP95Ms:0.00}ms  avg/P95");
        SetRow("Frames", $"{mjpeg.TotalEmitted:N0} emitted / {mjpeg.TotalDropped:N0} dropped");

        var compressedMb = mjpeg.CompressedQueueBytes / (1024.0 * 1024.0);
        var budgetMb = mjpeg.CompressedQueueByteBudget / (1024.0 * 1024.0);
        var bufferInfo = $"compressed={mjpeg.CompressedQueueDepth} ({compressedMb:0.0}/{budgetMb:0.0}MB)  reorder={mjpeg.ReorderBufferDepth}";
        if (_d3dRenderer != null)
        {
            bufferInfo += $"  preview={_d3dRenderer.PendingFrameCount}";
        }

        if (mjpeg.ReorderSkips > 0)
        {
            bufferInfo += $"  skips={mjpeg.ReorderSkips:N0}";
        }

        SetRow("Buffers", bufferInfo);

        foreach (var worker in mjpeg.PerDecoder)
        {
            SetRow($"Thread {worker.WorkerIndex}", $"{worker.AvgMs:0.00} / {worker.P95Ms:0.00}ms");
        }

        CollapseDiagnosticRows(_decodeRowPool, startIndex: rowIndex);
    }
    private void UpdateGpuSection()
    {
        var nvml = _nvmlMonitor?.GetLatestSnapshot();
        EnsureDiagnosticRowPool(GPU_Content, _gpuRowPool, FixedGpuRowCount);

        var rowIndex = 0;
        void SetRow(string label, string value)
        {
            UpdateDiagnosticRowSlot(_gpuRowPool[rowIndex], label, value, alt: (rowIndex % 2) != 0);
            rowIndex++;
        }

        if (nvml == null)
        {
            SetRow("Status", "NVML not available");
            CollapseDiagnosticRows(_gpuRowPool, startIndex: rowIndex);
            return;
        }

        SetRow("GPU", string.IsNullOrWhiteSpace(nvml.GpuName) ? "\u2014" : nvml.GpuName);
        SetRow("Utilization", $"{nvml.GpuUtilizationPercent ?? 0}% (Mem: {nvml.GpuMemoryUtilizationPercent ?? 0}%)");
        SetRow("NVDEC", $"{nvml.NvdecUtilizationPercent ?? 0}%");
        SetRow("NVENC", $"{nvml.NvencUtilizationPercent ?? 0}%");
        SetRow("PCIe TX", $"{nvml.PcieTxMBps ?? 0:0.0} MB/s");
        SetRow("PCIe RX", $"{nvml.PcieRxMBps ?? 0:0.0} MB/s");
        SetRow("VRAM", $"{nvml.VramUsedMB ?? 0} / {nvml.VramTotalMB ?? 0} MB");
        SetRow("Temperature", $"{nvml.GpuTemperatureC ?? 0}°C");
        SetRow("Power", $"{nvml.GpuPowerW ?? 0:0.0}W");
        SetRow("Clocks", $"{nvml.GpuClockMHz ?? 0} MHz (Mem: {nvml.GpuMemClockMHz ?? 0} MHz)");
        CollapseDiagnosticRows(_gpuRowPool, startIndex: rowIndex);
    }
    private void UpdateDiagnosticsSection(IReadOnlyList<SourceTelemetryDetailEntry> telemetryDetails, string? diagnosticSummary)
    {
        if (Diagnostics_Content.Visibility != Visibility.Visible)
        {
            return;
        }

        EnsureDiagnosticsEmptyState();

        var presentation = StatsPresentationBuilder.BuildDiagnosticRows(telemetryDetails, diagnosticSummary);
        if (presentation.IsEmpty)
        {
            SetVisibilityIfChanged(_diagnosticsEmptyStateTextBlock!, Visibility.Visible);
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

        SetVisibilityIfChanged(_diagnosticsEmptyStateTextBlock!, Visibility.Collapsed);
        CollapseDiagnosticsPoolSlots(startIndex: slotIndex);
    }

    private bool IsFrameTimeOverlayVisible()
        => FrameTimeOverlay.Visibility == Visibility.Visible;

    private void UpdateFrameTimeOverlay(StatsSnapshot snapshot)
    {
        if (!IsFrameTimeOverlayVisible())
        {
            return;
        }

        var presentation = StatsPresentationBuilder.BuildFrameTimePresentation(snapshot);
        SetTextIfChanged(FrameTime_SourceValue, presentation.SourceText);
        SetTextIfChanged(FrameTime_VisualValue, presentation.VisualText);
        SetTextIfChanged(FrameTime_PreviewValue, presentation.PreviewText);
        SetTextIfChanged(FrameTime_LatencyValue, presentation.LatencyText);
        SetTextIfChanged(FrameTime_StatusValue, presentation.StatusText);

        UpdateFrameTimeExpectedLine(presentation.Range);

        UpdateFrameTimeLine(
            FrameTime_VisualLine,
            presentation.VisualSamples,
            presentation.Range);
        UpdateFrameTimeLine(
            FrameTime_PreviewLine,
            presentation.PreviewSamples,
            presentation.Range);
    }

    private void UpdateFrameTimeLine(Microsoft.UI.Xaml.Shapes.Polyline line, IReadOnlyList<double> samples, StatsFrameTimeRange range)
    {
        line.Points.Clear();
        if (samples.Count <= 1)
        {
            return;
        }

        var width = FrameTime_Canvas.ActualWidth > 1 ? FrameTime_Canvas.ActualWidth : 500;
        var height = FrameTime_Canvas.ActualHeight > 1 ? FrameTime_Canvas.ActualHeight : 92;
        for (var i = 0; i < samples.Count; i++)
        {
            var x = samples.Count == 1 ? 0 : i * width / (samples.Count - 1);
            var normalized = Math.Clamp((samples[i] - range.MinMs) / range.SpanMs, 0.0, 1.0);
            var y = height - normalized * height;
            line.Points.Add(new Point(x, y));
        }
    }

    private void UpdateFrameTimeExpectedLine(StatsFrameTimeRange range)
    {
        var width = FrameTime_Canvas.ActualWidth > 1 ? FrameTime_Canvas.ActualWidth : 500;
        var height = FrameTime_Canvas.ActualHeight > 1 ? FrameTime_Canvas.ActualHeight : 92;
        var normalized = Math.Clamp((range.ExpectedMs - range.MinMs) / range.SpanMs, 0.0, 1.0);
        var y = height - normalized * height;
        FrameTime_ExpectedLine.X2 = width;
        FrameTime_ExpectedLine.Y1 = y;
        FrameTime_ExpectedLine.Y2 = y;
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

    private sealed record DiagnosticRowSlot(Border Row, TextBlock Label, TextBlock Value);
    private sealed record DiagnosticsPoolSlot(
        Border Row,
        TextBlock? GroupHeader,
        TextBlock Label,
        TextBlock Value);

    private static void SetVisibilityIfChanged(UIElement element, Visibility visibility)
    {
        if (element.Visibility != visibility)
        {
            element.Visibility = visibility;
        }
    }
    private void EnsureDiagnosticRowPool(StackPanel container, List<DiagnosticRowSlot> pool, int requiredCount)
    {
        while (pool.Count < requiredCount)
        {
            var row = CreateDiagnosticRow("", "", alt: false);
            var grid = (Grid)row.Child;
            var labelBlock = (TextBlock)grid.Children[0];
            var valueBlock = (TextBlock)grid.Children[1];
            pool.Add(new DiagnosticRowSlot(row, labelBlock, valueBlock));
            container.Children.Add(row);
        }
    }
    private void UpdateDiagnosticRowSlot(DiagnosticRowSlot slot, string label, string value, bool alt)
    {
        SetTextIfChanged(slot.Label, label);
        SetTextIfChanged(slot.Value, value);
        var targetStyle = (Style)StatsDockPanel.Resources[alt ? "DockStatsRowAltStyle" : "DockStatsRowStyle"];
        if (!ReferenceEquals(slot.Row.Style, targetStyle))
        {
            slot.Row.Style = targetStyle;
        }

        SetVisibilityIfChanged(slot.Row, Visibility.Visible);
    }
    private static void CollapseDiagnosticRows(List<DiagnosticRowSlot> pool, int startIndex = 0)
    {
        for (var i = startIndex; i < pool.Count; i++)
        {
            SetVisibilityIfChanged(pool[i].Row, Visibility.Collapsed);
        }
    }
    private void EnsureDiagnosticsEmptyState()
    {
        if (_diagnosticsEmptyStateTextBlock != null) return;
        _diagnosticsEmptyStateTextBlock = new TextBlock
        {
            Text = "No diagnostics available",
            Style = (Style)StatsDockPanel.Resources["DockStatsLabelStyle"],
            Visibility = Visibility.Collapsed
        };
        Diagnostics_Content.Children.Add(_diagnosticsEmptyStateTextBlock);
    }
    private void EnsureDiagnosticsPoolCapacity(int requiredCount)
    {
        while (_diagnosticsRowPool.Count < requiredCount)
        {
            var row = CreateDiagnosticRow("", "", alt: false);
            var grid = (Grid)row.Child;
            var labelBlock = (TextBlock)grid.Children[0];
            var valueBlock = (TextBlock)grid.Children[1];
            var header = CreateDiagnosticGroupHeader("");
            header.Visibility = Visibility.Collapsed;
            Diagnostics_Content.Children.Add(header);
            Diagnostics_Content.Children.Add(row);
            _diagnosticsRowPool.Add(new DiagnosticsPoolSlot(row, header, labelBlock, valueBlock));
        }
    }
    private void UpdateDiagnosticsPoolSlot(
        DiagnosticsPoolSlot slot,
        string? groupHeader,
        string label,
        string value,
        bool alt)
    {
        if (slot.GroupHeader != null)
        {
            if (groupHeader != null)
            {
                SetTextIfChanged(slot.GroupHeader, groupHeader);
                SetVisibilityIfChanged(slot.GroupHeader, Visibility.Visible);
            }
            else
            {
                SetVisibilityIfChanged(slot.GroupHeader, Visibility.Collapsed);
            }
        }

        SetTextIfChanged(slot.Label, label);
        SetTextIfChanged(slot.Value, value);
        var targetStyle = (Style)StatsDockPanel.Resources[alt ? "DockStatsRowAltStyle" : "DockStatsRowStyle"];
        if (!ReferenceEquals(slot.Row.Style, targetStyle))
        {
            slot.Row.Style = targetStyle;
        }

        SetVisibilityIfChanged(slot.Row, Visibility.Visible);
    }
    private void CollapseDiagnosticsPoolSlots(int startIndex = 0)
    {
        for (var i = startIndex; i < _diagnosticsRowPool.Count; i++)
        {
            var slot = _diagnosticsRowPool[i];
            SetVisibilityIfChanged(slot.Row, Visibility.Collapsed);
            if (slot.GroupHeader != null)
            {
                SetVisibilityIfChanged(slot.GroupHeader, Visibility.Collapsed);
            }
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

    private TextBlock CreateDiagnosticGroupHeader(string title)
    {
        return new TextBlock
        {
            Text = title,
            Margin = new Thickness(0, 8, 0, 2),
            Style = (Style)StatsDockPanel.Resources["DockStatsSectionHeaderStyle"]
        };
    }
    private Border CreateDiagnosticRow(string label, string value, bool alt)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelBlock = new TextBlock
        {
            Text = label,
            Style = (Style)StatsDockPanel.Resources["DockStatsLabelStyle"]
        };

        var valueBlock = new TextBlock
        {
            Text = value,
            Style = (Style)StatsDockPanel.Resources["DockStatsValueStyle"],
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(valueBlock, 1);

        grid.Children.Add(labelBlock);
        grid.Children.Add(valueBlock);

        return new Border
        {
            Style = (Style)StatsDockPanel.Resources[alt ? "DockStatsRowAltStyle" : "DockStatsRowStyle"],
            Child = grid
        };
    }
}
