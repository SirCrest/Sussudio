using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.Services;
using ElgatoCapture.ViewModels;
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
using WinRT.Interop;

namespace ElgatoCapture;

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

        StopStatsDockPolling();
        HideStatsDockPanel(immediate);
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
        UpdateStatsDock();
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
    private const double StatsDockPanelWidth = 300;
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
        var sessionState = snapshot.Recording
            ? "Recording"
            : snapshot.Previewing
                ? "Previewing"
                : "Idle";
        var sourceFps = FormatFps(snapshot.SourceObservedFps);
        var sourceExpectedFps = FormatFps(snapshot.SourceExpectedFps);
        var sourceAvg = $"{FormatMs(snapshot.SourceAvgIntervalMs)} avg";
        var sourceP95 = $"{FormatMs(snapshot.SourceP95IntervalMs)} P95";
        var sourceJitter = FormatMs(snapshot.SourceJitterMs);
        var sourceGaps = $"{FormatCount(snapshot.SourceSevereGaps)} severe";
        var sourceDrops = $"{FormatCount(snapshot.SourceEstDrops)} drops ({FormatPercent(snapshot.SourceEstDropPct)})";
        var previewFps = FormatFps(snapshot.PreviewObservedFps);
        var previewAvg = $"{FormatMs(snapshot.PreviewAvgIntervalMs)} avg";
        var previewP95 = $"{FormatMs(snapshot.PreviewP95IntervalMs)} P95";
        var previewSlow = $"{FormatCount(snapshot.PreviewSlowFrames)} frames ({FormatPercent(snapshot.PreviewSlowPct)})";
        var pipelineLatency = $"{FormatMs(snapshot.PipelineLatencyMs)} avg";
        var sourceDelivered = $"{FormatCount(snapshot.SourceFramesDelivered)} delivered";
        var sourceDropped = $"{FormatCount(snapshot.SourceFramesDropped)} dropped";
        var rendererRendered = $"{FormatCount(snapshot.RendererFramesRendered)} rendered";
        var rendererDropped = $"{FormatCount(snapshot.RendererFramesDropped)} dropped";
        var perfScore = $"{FormatScore(snapshot.PerformanceScore)} / 100";

        var sourceResolution = snapshot.SourceWidth.HasValue && snapshot.SourceHeight.HasValue
            ? $"{snapshot.SourceWidth} x {snapshot.SourceHeight}"
            : "\u2014";
        var sourceFrameRate = snapshot.SourceFrameRateExact.HasValue
            ? $"{snapshot.SourceFrameRateExact.Value:0.##} fps"
            : "\u2014";
        var sourceHdr = FormatSourceHdr(snapshot.SourceIsHdr, snapshot.SourceColorimetry);
        var sourceFormat = snapshot.SourceVideoFormat ?? "\u2014";
        var telemetryOrigin = snapshot.TelemetryOrigin is not null and not "Unknown"
            ? $"{snapshot.TelemetryOrigin} ({snapshot.TelemetryConfidence ?? "?"})"
            : "\u2014";

        var adcOnOff = "\u2014";
        var adcGain = "\u2014";
        if (snapshot.SourceTelemetryDetails is { } details)
        {
            foreach (var d in details)
            {
                if (d.Label == TelemetryLabels.AdcAnalog) adcOnOff = d.DisplayValue;
                else if (d.Label == TelemetryLabels.AnalogGain) adcGain = d.DisplayValue;
            }
        }

        SetTextIfChanged(Stats_SessionStateValue, sessionState);
        SetTextIfChanged(Stats_SourceResolutionValue, sourceResolution);
        SetTextIfChanged(Stats_SourceFrameRateValue, sourceFrameRate);
        SetTextIfChanged(Stats_SourceHdrValue, sourceHdr);
        SetTextIfChanged(Stats_SourceFormatValue, sourceFormat);
        SetTextIfChanged(Stats_TelemetryOriginValue, telemetryOrigin);
        SetTextIfChanged(Stats_AdcOnOffValue, adcOnOff);
        SetTextIfChanged(Stats_AdcGainValue, adcGain);
        SetTextIfChanged(Stats_SourceFpsValue, sourceFps);
        SetTextIfChanged(Stats_SourceExpectedFpsValue, sourceExpectedFps);
        SetTextIfChanged(Stats_SourceAvgValue, sourceAvg);
        SetTextIfChanged(Stats_SourceP95Value, sourceP95);
        SetTextIfChanged(Stats_SourceJitterValue, sourceJitter);
        SetTextIfChanged(Stats_SourceGapsValue, sourceGaps);
        SetTextIfChanged(Stats_SourceDropsValue, sourceDrops);
        SetTextIfChanged(Stats_PreviewFpsValue, previewFps);
        SetTextIfChanged(Stats_PreviewAvgValue, previewAvg);
        SetTextIfChanged(Stats_PreviewP95Value, previewP95);
        SetTextIfChanged(Stats_PreviewSlowValue, previewSlow);
        SetTextIfChanged(Stats_PipelineLatencyValue, pipelineLatency);
        SetTextIfChanged(Stats_SourceDeliveredValue, sourceDelivered);
        SetTextIfChanged(Stats_SourceDroppedValue, sourceDropped);
        SetTextIfChanged(Stats_RendererRenderedValue, rendererRendered);
        SetTextIfChanged(Stats_RendererDroppedValue, rendererDropped);
        SetTextIfChanged(Stats_PerfScoreValue, perfScore);
        SetTextIfChanged(Stats_AvSyncDriftValue, FormatSignedMs(snapshot.AvSyncCaptureDriftMs));
        SetTextIfChanged(Stats_AvSyncDriftRateValue, FormatSignedMsPerSec(snapshot.AvSyncCaptureDriftRateMsPerSec));
        var encoderVisible = snapshot.Recording && snapshot.AvSyncEncoderDriftMs.HasValue;
        SetVisibilityIfChanged(Stats_AvSyncEncoderRow, encoderVisible ? Visibility.Visible : Visibility.Collapsed);
        if (encoderVisible)
        {
            var encoderText = $"{FormatSignedMs(snapshot.AvSyncEncoderDriftMs)} ({snapshot.AvSyncEncoderCorrectionSamples ?? 0} corr)";
            SetTextIfChanged(Stats_AvSyncEncoderValue, encoderText);
        }
        var encoderActive = !string.IsNullOrEmpty(snapshot.EncoderCodecName);
        SetVisibilityIfChanged(EncoderSection, encoderActive ? Visibility.Visible : Visibility.Collapsed);
        if (encoderActive)
        {
            var codec = snapshot.EncoderCodecName switch
            {
                "hevc_nvenc" => "HEVC (NVENC)",
                "h264_nvenc" => "H.264 (NVENC)",
                "av1_nvenc" => "AV1 (NVENC)",
                _ => snapshot.EncoderCodecName!
            };
            var mbps = snapshot.EncoderTargetBitRate / 1_000_000.0;
            SetTextIfChanged(Stats_EncoderCodecValue, codec);
            SetTextIfChanged(Stats_EncoderResolutionValue, $"{snapshot.EncoderWidth} x {snapshot.EncoderHeight}");
            SetTextIfChanged(Stats_EncoderFrameRateValue, $"{snapshot.EncoderFrameRate:0.##} fps");
            SetTextIfChanged(Stats_EncoderBitrateValue, $"{mbps:0.#} Mbps");
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

        var bufferInfo = $"reorder={mjpeg.ReorderBufferDepth}";
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

        var slotIndex = 0;
        if (telemetryDetails.Count > 0)
        {
            var currentGroup = string.Empty;
            var alt = true;
            foreach (var detail in telemetryDetails)
            {
                EnsureDiagnosticsPoolCapacity(slotIndex + 1);
                var showHeader = !string.Equals(currentGroup, detail.Group, StringComparison.Ordinal);
                if (showHeader)
                {
                    currentGroup = detail.Group;
                    alt = true;
                }

                UpdateDiagnosticsPoolSlot(
                    _diagnosticsRowPool[slotIndex],
                    showHeader ? currentGroup : null,
                    detail.Label,
                    detail.DisplayValue,
                    alt);
                alt = !alt;
                slotIndex++;
            }

            SetVisibilityIfChanged(_diagnosticsEmptyStateTextBlock!, Visibility.Collapsed);
            CollapseDiagnosticsPoolSlots(startIndex: slotIndex);
            return;
        }

        if (string.IsNullOrWhiteSpace(diagnosticSummary))
        {
            SetVisibilityIfChanged(_diagnosticsEmptyStateTextBlock!, Visibility.Visible);
            CollapseDiagnosticsPoolSlots();
            return;
        }

        var entries = ParseDiagnosticSummary(diagnosticSummary);
        var fallbackAlt = true;
        foreach (var (label, value) in entries)
        {
            EnsureDiagnosticsPoolCapacity(slotIndex + 1);
            UpdateDiagnosticsPoolSlot(_diagnosticsRowPool[slotIndex], null, label, value, fallbackAlt);
            fallbackAlt = !fallbackAlt;
            slotIndex++;
        }

        SetVisibilityIfChanged(_diagnosticsEmptyStateTextBlock!, Visibility.Collapsed);
        CollapseDiagnosticsPoolSlots(startIndex: slotIndex);
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
    private static string FormatFps(double value)
    {
        return Sanitize(value).ToString("0.00");
    }
    private static string FormatSourceHdr(bool? isHdr, string? colorimetry)
        => DisplayFormatters.FormatSourceHdr(isHdr, colorimetry);

    private static string FormatMs(double value)
    {
        return $"{Sanitize(value):0.00}ms";
    }
    private static string FormatPercent(double value)
    {
        return $"{Sanitize(value):0.0}%";
    }
    private static string FormatScore(double value)
    {
        return Sanitize(value).ToString("0.0");
    }
    private static string FormatCount(long value)
    {
        return Math.Max(0, value).ToString("N0");
    }
    private static string FormatSignedMs(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value))
        {
            return "\u2014";
        }

        return value.Value >= 0 ? $"+{value.Value:F1}ms" : $"{value.Value:F1}ms";
    }
    private static string FormatSignedMsPerSec(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value))
        {
            return "\u2014";
        }

        return value.Value >= 0 ? $"+{value.Value:F2} ms/s" : $"{value.Value:F2} ms/s";
    }
    private static void SetTextIfChanged(TextBlock target, string value)
    {
        if (!string.Equals(target.Text, value, StringComparison.Ordinal))
        {
            target.Text = value;
        }
    }
    private static double Sanitize(double value)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            return 0;
        }

        return value;
    }
    private StatsSnapshot GetStatsSnapshot()
    {
        var health = ViewModel.GetCaptureHealthSnapshot();
        var d3d = _d3dRenderer;
        var presentCadence = d3d?.GetPresentCadenceMetrics(_previewMinPresentationIntervalMs);
        var pipelineLatency = d3d?.GetEstimatedPipelineLatencyMs() ?? 0;
        var sourceDropPercent = Sanitize(health.CaptureCadenceEstimatedDropPercent);
        var previewSlowPercent = Sanitize(presentCadence?.SlowFramePercent ?? 0);
        var performanceScore = Math.Clamp(100.0 - sourceDropPercent - previewSlowPercent, 0.0, 100.0);
        var telemetryDetails = new List<SourceTelemetryDetailEntry>(health.SourceTelemetryDetails);
        var captureCardFormat = health.ReaderSourceSubtype ?? health.NegotiatedPixelFormat;
        if (!string.IsNullOrWhiteSpace(captureCardFormat))
        {
            telemetryDetails.Add(new SourceTelemetryDetailEntry("Capture Card / UVC", "Capture Format", captureCardFormat));
        }

        return new StatsSnapshot(
            SourceCadenceSamples: health.CaptureCadenceSampleCount,
            SourceObservedFps: Sanitize(health.CaptureCadenceObservedFps),
            SourceExpectedFps: Sanitize(health.ExpectedFrameRate),
            SourceAvgIntervalMs: Sanitize(health.CaptureCadenceAverageIntervalMs),
            SourceP95IntervalMs: Sanitize(health.CaptureCadenceP95IntervalMs),
            SourceMaxIntervalMs: Sanitize(health.CaptureCadenceMaxIntervalMs),
            SourceJitterMs: Sanitize(health.CaptureCadenceJitterStdDevMs),
            SourceSevereGaps: health.CaptureCadenceSevereGapCount,
            SourceEstDrops: health.CaptureCadenceEstimatedDroppedFrames,
            SourceEstDropPct: sourceDropPercent,
            PreviewCadenceSamples: presentCadence?.SampleCount ?? 0,
            PreviewObservedFps: Sanitize(presentCadence?.ObservedFps ?? 0),
            PreviewAvgIntervalMs: Sanitize(presentCadence?.AverageIntervalMs ?? 0),
            PreviewP95IntervalMs: Sanitize(presentCadence?.P95IntervalMs ?? 0),
            PreviewSlowFrames: presentCadence?.SlowFrameCount ?? 0,
            PreviewSlowPct: previewSlowPercent,
            PipelineLatencyMs: Sanitize(pipelineLatency),
            SourceFramesDelivered: health.VideoFramesArrived,
            SourceFramesDropped: health.VideoFramesDropped,
            RendererFramesSubmitted: d3d?.FramesSubmitted ?? 0,
            RendererFramesRendered: d3d?.FramesRendered ?? 0,
            RendererFramesDropped: d3d?.FramesDropped ?? 0,
            PerformanceScore: performanceScore,
            Previewing: ViewModel.IsPreviewing,
            Recording: ViewModel.IsRecording,
            SourceWidth: health.SourceWidth,
            SourceHeight: health.SourceHeight,
            SourceFrameRateExact: health.SourceFrameRateExact,
            SourceIsHdr: health.SourceIsHdr,
            SourceVideoFormat: health.SourceVideoFormat,
            SourceColorimetry: health.SourceColorimetry,
            ReaderSourceSubtype: health.ReaderSourceSubtype,
            NegotiatedPixelFormat: health.NegotiatedPixelFormat,
            TelemetryOrigin: health.SourceTelemetryOrigin.ToString(),
            TelemetryConfidence: health.SourceTelemetryConfidence.ToString(),
            SourceTelemetryDetails: telemetryDetails,
            DiagnosticSummary: health.SourceTelemetryDiagnosticSummary,
            AvSyncCaptureDriftMs: health.AvSyncCaptureDriftMs,
            AvSyncCaptureDriftRateMsPerSec: health.AvSyncCaptureDriftRateMsPerSec,
            AvSyncEncoderDriftMs: health.AvSyncEncoderDriftMs,
            AvSyncEncoderCorrectionSamples: health.AvSyncEncoderCorrectionSamples,
            EncoderCodecName: health.EncoderCodecName,
            EncoderWidth: health.EncoderWidth,
            EncoderHeight: health.EncoderHeight,
            EncoderFrameRate: health.EncoderFrameRate,
            EncoderTargetBitRate: health.EncoderTargetBitRate);
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
    private static List<(string Label, string Value)> ParseDiagnosticSummary(string summary)
    {
        if (!summary.StartsWith("nativexu:", StringComparison.OrdinalIgnoreCase))
        {
            return new List<(string Label, string Value)>
            {
                ("Summary", summary.Trim())
            };
        }

        var result = new List<(string Label, string Value)>();
        var parts = summary.Split(':');

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            var eqIndex = part.IndexOf('=');
            if (eqIndex > 0)
            {
                var key = part[..eqIndex].Trim();
                var val = part[(eqIndex + 1)..].Trim();
                var label = key switch
                {
                    "vic" => "VIC Code",
                    "vfreq" => "Vert Freq",
                    "quant" => "Quantization",
                    "hdr2sdr" => "HDR to SDR",
                    "eotf" => "EOTF",
                    "fw" => "Firmware",
                    "audiofmt" => "Audio Format",
                    "audiosrate" => "Audio Sample Rate",
                    "inputsrc" => "Input Source",
                    "usbproto" => "USB Protocol",
                    "usbcdc" => "USB CDC",
                    "usblinkst" => "USB Link State",
                    "usbspeed" => "USB Speed",
                    "txhpd" => "TX Hot Plug",
                    "txvrr" => "TX VRR",
                    "uvctiming" => "UVC Timing",
                    "uvcfmt" => "UVC Format",
                    "uvcerr" => "UVC Error",
                    "hdcpmode" => "HDCP Mode",
                    "hdcpver" => "HDCP Version",
                    "rxtxhdcp" => "RX/TX HDCP",
                    "hdr2sdrext" => "HDR2SDR Status",
                    "hdr2sdrcolor" => "HDR2SDR Color",
                    "colorrangesetting" => "Color Range",
                    "vtem" => "VTEM (VRR)",
                    "biterr" => "Bit Errors",
                    "rawtiming" => "Raw Timing",
                    _ => key
                };
                result.Add((label, val));
                continue;
            }

            var entry = part switch
            {
                "nativexu" => ("Origin", "NativeXu"),
                "hdr" => ("HDR", "Yes"),
                "sdr" => ("HDR", "No"),
                "unknown" => ("HDR", "Unknown"),
                _ when part.Contains('x') && part.Length > 3 && char.IsDigit(part[0]) => ("Resolution", part),
                _ when double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var fps) && fps > 0 =>
                    ("Frame Rate", $"{fps:0.##} Hz"),
                _ => ("Info", part)
            };
            result.Add(entry);
        }

        return result;
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
