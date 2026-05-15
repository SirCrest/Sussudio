using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using Windows.Graphics;
using Sussudio.Controllers;
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
        return new StatsWindowPresentationController(new StatsWindowPresentationControllerContext
        {
            ResourceOwner = RootGrid,
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
            PerfScoreValue = PerfScoreValue,
            TelemetryDetailsContent = TelemetryDetailsContent
        });
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
