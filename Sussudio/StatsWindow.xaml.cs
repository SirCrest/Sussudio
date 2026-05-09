using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sussudio.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;
using Windows.Graphics;
using Sussudio.Services.Audio;
using Sussudio.Services.Automation;
using Sussudio.Services.Capture;
using Sussudio.Services.Configuration;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio;

// Detached diagnostics window. It polls a StatsSnapshot provider and renders
// the same live counters as the dock without owning capture or automation state.
public sealed partial class StatsWindow : Window
{
    private readonly Func<StatsSnapshot> _dataProvider;
    private readonly Action? _closedCallback;
    private readonly DispatcherQueueTimer _pollTimer;

    public StatsWindow(Func<StatsSnapshot> dataProvider, Action? closedCallback = null)
    {
        ArgumentNullException.ThrowIfNull(dataProvider);

        InitializeComponent();

        _dataProvider = dataProvider;
        _closedCallback = closedCallback;

        ConfigureWindow();

        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _pollTimer = dispatcherQueue.CreateTimer();
        _pollTimer.Interval = TimeSpan.FromMilliseconds(500);
        _pollTimer.IsRepeating = true;
        _pollTimer.Tick += PollTimer_Tick;

        Closed += StatsWindow_Closed;

        UpdateSnapshot(_dataProvider());
        _pollTimer.Start();
    }

    private const int MinWidth = 340;
    private const int MinHeight = 520;
    private MinSizeWindowSubclass.MinSizeHandle? _minSizeHandle;

    private void ConfigureWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        var dpi = GetDpiForWindow(hwnd);
        var scale = dpi / 96.0;
        appWindow.Resize(new SizeInt32((int)(MinWidth * scale), (int)(MinHeight * scale)));

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = true;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = true;
            presenter.IsAlwaysOnTop = AlwaysOnTopToggle.IsOn;
        }

        _minSizeHandle = MinSizeWindowSubclass.Install(hwnd, MinWidth, MinHeight);
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    private void PollTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        UpdateSnapshot(_dataProvider());
    }

    private void StatsWindow_Closed(object sender, WindowEventArgs args)
    {
        _pollTimer.Stop();
        _pollTimer.Tick -= PollTimer_Tick;
        _closedCallback?.Invoke();
    }

    private void AlwaysOnTopToggle_Toggled(object sender, RoutedEventArgs e)
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsAlwaysOnTop = AlwaysOnTopToggle.IsOn;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in StatsWindow always-on-top toggle: {ex.Message}");
        }
    }

    private void UpdateSnapshot(StatsSnapshot snapshot)
    {
        SessionStateValue.Text = snapshot.Recording
            ? "Recording"
            : snapshot.Previewing
                ? "Previewing"
                : "Idle";
        DiagnosticStatusValue.Text = string.IsNullOrWhiteSpace(snapshot.DiagnosticHealthStatus)
            ? "Unknown"
            : snapshot.DiagnosticHealthStatus;
        DiagnosticStageValue.Text = string.IsNullOrWhiteSpace(snapshot.DiagnosticLikelyStage)
            ? "diagnostic_unavailable"
            : snapshot.DiagnosticLikelyStage;
        DiagnosticEvidenceValue.Text = string.IsNullOrWhiteSpace(snapshot.DiagnosticEvidence)
            ? snapshot.DiagnosticSummary ?? "Diagnostics are not available yet."
            : snapshot.DiagnosticEvidence;

        SourceResolutionValue.Text = snapshot.SourceWidth.HasValue && snapshot.SourceHeight.HasValue
            ? $"{snapshot.SourceWidth} x {snapshot.SourceHeight}"
            : "\u2014";
        SourceFrameRateValue.Text = snapshot.SourceFrameRateExact.HasValue
            ? $"{snapshot.SourceFrameRateExact.Value:0.##} fps"
            : "\u2014";
        SourceHdrValue.Text = FormatSourceHdr(snapshot.SourceIsHdr, snapshot.SourceColorimetry);
        SourceFormatValue.Text = snapshot.SourceVideoFormat ?? "\u2014";
        TelemetryOriginValue.Text = snapshot.TelemetryOrigin is not null and not "Unknown"
            ? $"{snapshot.TelemetryOrigin} ({snapshot.TelemetryConfidence ?? "?"})"
            : "\u2014";

        SourceFpsValue.Text = FormatFps(snapshot.SourceObservedFps);
        SourceExpectedFpsValue.Text = FormatFps(snapshot.SourceExpectedFps);
        SourceAvgValue.Text = $"{FormatMs(snapshot.SourceAvgIntervalMs)} avg";
        SourceP95Value.Text = $"{FormatMs(snapshot.SourceP95IntervalMs)} P95";
        SourceJitterValue.Text = FormatMs(snapshot.SourceJitterMs);
        SourceGapsValue.Text = $"{FormatCount(snapshot.SourceSevereGaps)} severe";
        SourceDropsValue.Text = $"{FormatCount(snapshot.SourceEstDrops)} drops ({FormatPercent(snapshot.SourceEstDropPct)})";

        PreviewFpsValue.Text = FormatFps(snapshot.PreviewObservedFps);
        PreviewAvgValue.Text = $"{FormatMs(snapshot.PreviewAvgIntervalMs)} avg";
        PreviewP95Value.Text = $"{FormatMs(snapshot.PreviewP95IntervalMs)} P95";
        PreviewSlowValue.Text = $"{FormatCount(snapshot.PreviewSlowFrames)} frames ({FormatPercent(snapshot.PreviewSlowPct)})";

        PipelineLatencyValue.Text = $"{FormatMs(snapshot.PipelineLatencyMs)} avg";

        SourceDeliveredValue.Text = $"{FormatCount(snapshot.SourceFramesDelivered)} delivered";
        SourceDroppedValue.Text = $"{FormatCount(snapshot.SourceFramesDropped)} dropped";
        RendererRenderedValue.Text = $"{FormatCount(snapshot.RendererFramesRendered)} rendered";
        RendererDroppedValue.Text = $"{FormatCount(snapshot.RendererFramesDropped)} dropped";

        PerfScoreValue.Text = $"{FormatScore(snapshot.PerformanceScore)} / 100";
        UpdateTelemetryDetails(snapshot.SourceTelemetryDetails ?? Array.Empty<SourceTelemetryDetailEntry>(), snapshot.DiagnosticSummary);
    }

    private void UpdateTelemetryDetails(IReadOnlyList<SourceTelemetryDetailEntry> details, string? diagnosticSummary)
    {
        TelemetryDetailsContent.Children.Clear();

        if (details.Count > 0)
        {
            var currentGroup = string.Empty;
            foreach (var detail in details)
            {
                if (!string.Equals(currentGroup, detail.Group, StringComparison.Ordinal))
                {
                    currentGroup = detail.Group;
                    TelemetryDetailsContent.Children.Add(new TextBlock
                    {
                        Text = currentGroup,
                        Margin = new Thickness(0, 8, 0, 2),
                        Style = (Style)RootGrid.Resources["StatsSectionHeaderStyle"]
                    });
                }

                TelemetryDetailsContent.Children.Add(CreateTelemetryDetailRow(detail.Label, detail.DisplayValue));
            }

            return;
        }

        TelemetryDetailsContent.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(diagnosticSummary) ? "No telemetry details available" : diagnosticSummary,
            Style = (Style)RootGrid.Resources["StatsLabelStyle"],
            TextWrapping = TextWrapping.Wrap
        });
    }

    private Grid CreateTelemetryDetailRow(string label, string value)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelBlock = new TextBlock
        {
            Text = label,
            Style = (Style)RootGrid.Resources["StatsLabelStyle"]
        };
        var valueBlock = new TextBlock
        {
            Text = value,
            Style = (Style)RootGrid.Resources["StatsValueStyle"],
            HorizontalAlignment = HorizontalAlignment.Right,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(valueBlock, 1);

        grid.Children.Add(labelBlock);
        grid.Children.Add(valueBlock);
        return grid;
    }

    private static string FormatFps(double value)
    {
        return Sanitize(value).ToString("0.00");
    }

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

    private static string FormatSourceHdr(bool? isHdr, string? colorimetry)
        => DisplayFormatters.FormatSourceHdr(isHdr, colorimetry);

    private static double Sanitize(double value)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            return 0;
        }

        return value;
    }
}
