using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using Windows.Graphics;

namespace ElgatoCapture;

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
    private IntPtr _originalWndProc;

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

        _originalWndProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC,
            Marshal.GetFunctionPointerForDelegate(_minSizeProc = new WndProcDelegate(MinSizeWndProc)));
    }

    private WndProcDelegate? _minSizeProc;

    private IntPtr MinSizeWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == 0x0024) // WM_GETMINMAXINFO
        {
            var dpi = GetDpiForWindow(hWnd);
            var scale = dpi / 96.0;
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            mmi.ptMinTrackSize.X = (int)(MinWidth * scale);
            mmi.ptMinTrackSize.Y = (int)(MinHeight * scale);
            Marshal.StructureToPtr(mmi, lParam, false);
        }
        return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private const int GWLP_WNDPROC = -4;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

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
        catch
        {
            // Ignore presenter toggle failures; stats polling can continue.
        }
    }

    private void UpdateSnapshot(StatsSnapshot snapshot)
    {
        SessionStateValue.Text = snapshot.Recording
            ? "Recording"
            : snapshot.Previewing
                ? "Previewing"
                : "Idle";

        SourceResolutionValue.Text = snapshot.SourceWidth.HasValue && snapshot.SourceHeight.HasValue
            ? $"{snapshot.SourceWidth} x {snapshot.SourceHeight}"
            : "\u2014";
        SourceFrameRateValue.Text = snapshot.SourceFrameRateExact.HasValue
            ? $"{snapshot.SourceFrameRateExact.Value:0.##} fps"
            : "\u2014";
        SourceHdrValue.Text = snapshot.SourceIsHdr switch
        {
            true => "On",
            false => "Off",
            _ => "\u2014"
        };
        SourceFormatValue.Text = snapshot.NegotiatedPixelFormat ?? "\u2014";
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

    private static double Sanitize(double value)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            return 0;
        }

        return value;
    }
}

public sealed record StatsSnapshot(
    int SourceCadenceSamples,
    double SourceObservedFps,
    double SourceExpectedFps,
    double SourceAvgIntervalMs,
    double SourceP95IntervalMs,
    double SourceMaxIntervalMs,
    double SourceJitterMs,
    long SourceSevereGaps,
    long SourceEstDrops,
    double SourceEstDropPct,
    int PreviewCadenceSamples,
    double PreviewObservedFps,
    double PreviewAvgIntervalMs,
    double PreviewP95IntervalMs,
    long PreviewSlowFrames,
    double PreviewSlowPct,
    double PipelineLatencyMs,
    long SourceFramesDelivered,
    long SourceFramesDropped,
    long RendererFramesSubmitted,
    long RendererFramesRendered,
    long RendererFramesDropped,
    double PerformanceScore,
    bool Previewing,
    bool Recording,
    int? SourceWidth = null,
    int? SourceHeight = null,
    double? SourceFrameRateExact = null,
    bool? SourceIsHdr = null,
    string? NegotiatedPixelFormat = null,
    string? TelemetryOrigin = null,
    string? TelemetryConfidence = null);
