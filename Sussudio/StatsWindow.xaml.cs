using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;
using Windows.Graphics;
using Sussudio.Services.Runtime;
using Sussudio.ViewModels;

namespace Sussudio;

// Detached diagnostics window. It polls a StatsSnapshot provider and renders
// the same live counters as the dock without owning capture or automation state.
public sealed partial class StatsWindow : Window
{
    private readonly Func<StatsSnapshot> _dataProvider;
    private readonly Action? _closedCallback;
    private readonly StatsWindowPresentationController _presentationController;
    private readonly DispatcherQueueTimer _pollTimer;

    public StatsWindow(Func<StatsSnapshot> dataProvider, Action? closedCallback = null)
    {
        ArgumentNullException.ThrowIfNull(dataProvider);

        InitializeComponent();

        _dataProvider = dataProvider;
        _closedCallback = closedCallback;
        _presentationController = CreatePresentationController();

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

    private StatsWindowPresentationController CreatePresentationController()
    {
        var telemetryDetailsController = new StatsWindowTelemetryDetailsController(new StatsWindowTelemetryDetailsControllerContext
        {
            ResourceOwner = RootGrid,
            TelemetryDetailsContent = TelemetryDetailsContent
        });

        return new StatsWindowPresentationController(
            new StatsWindowPresentationControllerContext
            {
                SessionStateValue = SessionStateValue,
                DiagnosticStatusValue = DiagnosticStatusValue,
                DiagnosticStageValue = DiagnosticStageValue,
                DiagnosticEvidenceValue = DiagnosticEvidenceValue,
                SourceResolutionValue = SourceResolutionValue,
                SourceFrameRateValue = SourceFrameRateValue,
                SourceHdrValue = SourceHdrValue,
                SourceFormatValue = SourceFormatValue,
                TelemetryOriginValue = TelemetryOriginValue,
                SourceFpsValue = SourceFpsValue,
                SourceExpectedFpsValue = SourceExpectedFpsValue,
                SourceAvgValue = SourceAvgValue,
                SourceP95Value = SourceP95Value,
                SourceJitterValue = SourceJitterValue,
                SourceGapsValue = SourceGapsValue,
                SourceDropsValue = SourceDropsValue,
                PreviewFpsValue = PreviewFpsValue,
                PreviewAvgValue = PreviewAvgValue,
                PreviewP95Value = PreviewP95Value,
                PreviewSlowValue = PreviewSlowValue,
                PipelineLatencyValue = PipelineLatencyValue,
                SourceDeliveredValue = SourceDeliveredValue,
                SourceDroppedValue = SourceDroppedValue,
                RendererRenderedValue = RendererRenderedValue,
                RendererDroppedValue = RendererDroppedValue,
                PerfScoreValue = PerfScoreValue
            },
            telemetryDetailsController);
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
        var presentation = StatsPresentationBuilder.BuildStatsWindowPresentation(snapshot);
        _presentationController.Apply(presentation);
    }
}

internal sealed class StatsWindowPresentationControllerContext
{
    public required TextBlock SessionStateValue { get; init; }
    public required TextBlock DiagnosticStatusValue { get; init; }
    public required TextBlock DiagnosticStageValue { get; init; }
    public required TextBlock DiagnosticEvidenceValue { get; init; }
    public required TextBlock SourceResolutionValue { get; init; }
    public required TextBlock SourceFrameRateValue { get; init; }
    public required TextBlock SourceHdrValue { get; init; }
    public required TextBlock SourceFormatValue { get; init; }
    public required TextBlock TelemetryOriginValue { get; init; }
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
    public required TextBlock PipelineLatencyValue { get; init; }
    public required TextBlock SourceDeliveredValue { get; init; }
    public required TextBlock SourceDroppedValue { get; init; }
    public required TextBlock RendererRenderedValue { get; init; }
    public required TextBlock RendererDroppedValue { get; init; }
    public required TextBlock PerfScoreValue { get; init; }
}

internal sealed class StatsWindowPresentationController
{
    private readonly StatsWindowPresentationControllerContext _context;
    private readonly StatsWindowTelemetryDetailsController _telemetryDetailsController;

    public StatsWindowPresentationController(
        StatsWindowPresentationControllerContext context,
        StatsWindowTelemetryDetailsController telemetryDetailsController)
    {
        _context = context;
        _telemetryDetailsController = telemetryDetailsController;
    }

    public void Apply(StatsWindowPresentation presentation)
    {
        SetTextIfChanged(_context.SessionStateValue, presentation.SessionState);
        SetTextIfChanged(_context.DiagnosticStatusValue, presentation.DiagnosticStatus);
        SetTextIfChanged(_context.DiagnosticStageValue, presentation.DiagnosticStage);
        SetTextIfChanged(_context.DiagnosticEvidenceValue, presentation.DiagnosticEvidence);
        SetTextIfChanged(_context.SourceResolutionValue, presentation.SourceResolution);
        SetTextIfChanged(_context.SourceFrameRateValue, presentation.SourceFrameRate);
        SetTextIfChanged(_context.SourceHdrValue, presentation.SourceHdr);
        SetTextIfChanged(_context.SourceFormatValue, presentation.SourceFormat);
        SetTextIfChanged(_context.TelemetryOriginValue, presentation.TelemetryOrigin);
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
        SetTextIfChanged(_context.PipelineLatencyValue, presentation.PipelineLatency);
        SetTextIfChanged(_context.SourceDeliveredValue, presentation.SourceDelivered);
        SetTextIfChanged(_context.SourceDroppedValue, presentation.SourceDropped);
        SetTextIfChanged(_context.RendererRenderedValue, presentation.RendererRendered);
        SetTextIfChanged(_context.RendererDroppedValue, presentation.RendererDropped);
        SetTextIfChanged(_context.PerfScoreValue, presentation.PerformanceScore);
        _telemetryDetailsController.Apply(presentation.TelemetryDetails);
    }

    private static void SetTextIfChanged(TextBlock target, string value)
    {
        if (!string.Equals(target.Text, value, StringComparison.Ordinal))
        {
            target.Text = value;
        }
    }
}

internal sealed class StatsWindowTelemetryDetailsControllerContext
{
    public required FrameworkElement ResourceOwner { get; init; }
    public required StackPanel TelemetryDetailsContent { get; init; }
}

internal sealed class StatsWindowTelemetryDetailsController
{
    private readonly StatsWindowTelemetryDetailsControllerContext _context;

    public StatsWindowTelemetryDetailsController(StatsWindowTelemetryDetailsControllerContext context)
    {
        _context = context;
    }

    public void Apply(StatsWindowTelemetryDetailsPresentation presentation)
    {
        _context.TelemetryDetailsContent.Children.Clear();

        if (presentation.IsEmpty)
        {
            _context.TelemetryDetailsContent.Children.Add(new TextBlock
            {
                Text = presentation.EmptyText,
                Style = GetStyle("StatsLabelStyle"),
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        foreach (var row in presentation.Rows)
        {
            if (row.GroupHeader != null)
            {
                _context.TelemetryDetailsContent.Children.Add(new TextBlock
                {
                    Text = row.GroupHeader,
                    Margin = new Thickness(0, 8, 0, 2),
                    Style = GetStyle("StatsSectionHeaderStyle")
                });
            }

            _context.TelemetryDetailsContent.Children.Add(CreateTelemetryDetailRow(row.Label, row.Value));
        }
    }

    private Grid CreateTelemetryDetailRow(string label, string value)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelBlock = new TextBlock
        {
            Text = label,
            Style = GetStyle("StatsLabelStyle")
        };
        var valueBlock = new TextBlock
        {
            Text = value,
            Style = GetStyle("StatsValueStyle"),
            HorizontalAlignment = HorizontalAlignment.Right,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(valueBlock, 1);

        grid.Children.Add(labelBlock);
        grid.Children.Add(valueBlock);
        return grid;
    }

    private Style GetStyle(string key) => (Style)_context.ResourceOwner.Resources[key];
}
