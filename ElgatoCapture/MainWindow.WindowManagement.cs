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
using ElgatoCapture.Services.Audio;
using ElgatoCapture.Services.Automation;
using ElgatoCapture.Services.Capture;
using ElgatoCapture.Services.Configuration;
using ElgatoCapture.Services.Flashback;
using ElgatoCapture.Services.Gpu;
using ElgatoCapture.Services.Preview;
using ElgatoCapture.Services.Recording;
using ElgatoCapture.Services.Runtime;
using ElgatoCapture.Services.Telemetry;

namespace ElgatoCapture;

public sealed partial class MainWindow
{
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ((FrameworkElement)this.Content).Loaded -= MainWindow_Loaded;

        // Uncloak the window — XAML content is now rendered (splash overlay covers everything)
        int cloakFalse = 0;
        DwmSetWindowAttribute(_hwnd, DWMWA_CLOAK, ref cloakFalse, sizeof(int));

        // Start device init immediately — runs behind the splash
        _ = RunUiEventHandlerAsync(async () =>
        {
            Logger.Log("=== MainWindow_Loaded - Starting device enumeration ===");
            try
            {
                await ViewModel.InitializeAsync();
                // LoadSettings just pushed saved volume to CaptureService — capture it, reset to 0
                // so WASAPI playback starts silent. The entrance animation will ramp the slider.
                // NOTE: Do NOT toggle SuppressVolumeSave here — PlaySplashAndEntrance already
                // set it to true, and setting it to false would allow intermediate animation
                // ticks and unrelated SaveSettings() calls to persist PreviewVolume = 0.
                // VolumeSaveOverride ensures any save during the fade writes the real value.
                if (_isVolumeFadingIn)
                {
                    _savedPreviewVolume = ViewModel.PreviewVolume;
                    ViewModel.VolumeSaveOverride = _savedPreviewVolume;
                    ViewModel.PreviewVolume = 0;
                }
                await ViewModel.RefreshDevicesAsync();
            }
            finally
            {
                StartAutomationServices();
            }
        }, nameof(MainWindow_Loaded));

        // Start the splash → entrance sequence
        PlaySplashAndEntrance();
    }
    private void StartAutomationServices()
    {
        if (Interlocked.Exchange(ref _automationServicesStarted, 1) != 0)
        {
            return;
        }

        if (_automationPipeServer.Start())
        {
            _automationDiagnosticsHub.Start();
            Logger.Log(
                $"Automation control ready on pipe '{_automationPipeName}' (token required={_automationTokenRequired}).");
        }
        else
        {
            Logger.Log(
                $"Automation control disabled on pipe '{_automationPipeName}' (token required={_automationTokenRequired}).");
        }
    }
    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var nowTick = Environment.TickCount64;
        if (!ViewModel.IsPreviewing ||
            _d3dRenderer == null ||
            PreviewSwapChainPanel.Visibility != Visibility.Visible)
        {
            return;
        }

        var lastLogTick = Interlocked.Read(ref _previewLastResizeLogTick);
        if (nowTick - lastLogTick < 1000)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _previewLastResizeLogTick, nowTick, lastLogTick) == lastLogTick)
        {
            Logger.Log("Preview resize active. Updating compositor transform without resizing swap-chain buffers.");
        }
    }
    private async void MainWindow_Closing(
        Microsoft.UI.Windowing.AppWindow sender,
        Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (Volatile.Read(ref _windowCloseCleanupStarted) != 0 ||
            Volatile.Read(ref _windowCloseAllowedAfterRecordingStop) != 0)
        {
            return;
        }

        if (!ViewModel.IsRecording && !ViewModel.IsRecordingTransitioning)
        {
            return;
        }

        args.Cancel = true;
        Interlocked.Exchange(ref _windowCloseRequested, 0);

        if (Interlocked.Exchange(ref _windowCloseRecordingStopInProgress, 1) != 0)
        {
            Logger.Log("WINDOW_CLOSE_RECORDING_STOP: close already waiting for recording stop.");
            return;
        }

        try
        {
            var stopped = await TryStopRecordingBeforeCloseAsync();
            if (!stopped)
            {
                return;
            }

            Interlocked.Exchange(ref _windowCloseAllowedAfterRecordingStop, 1);
            Interlocked.Exchange(ref _windowCloseRequested, 0);
            RequestWindowClose();
        }
        finally
        {
            Interlocked.Exchange(ref _windowCloseRecordingStopInProgress, 0);
        }
    }

    private async Task<bool> TryStopRecordingBeforeCloseAsync()
    {
        const int StopBudgetMs = 120_000;
        Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording active, awaiting graceful stop...");
        ViewModel.StatusText = "Stopping recording - please wait...";

        FrameworkElement? shutdownContent = null;
        if (this.Content is FrameworkElement content)
        {
            shutdownContent = content;
            shutdownContent.IsHitTestVisible = false;
            shutdownContent.Opacity = 0.5;
        }

        try
        {
            var stopTask = ViewModel.StopRecordingAndWaitAsync();
            var completed = await Task.WhenAny(stopTask, Task.Delay(StopBudgetMs));
            if (completed == stopTask)
            {
                await stopTask;
                Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording stopped cleanly.");
                return true;
            }

            Logger.LogFatalBreadcrumb("RECORDING_FINALIZE_TIMEOUT "
                + $"budget_ms={StopBudgetMs}; close cancelled to protect recording.");
            ViewModel.StatusText = "Still saving recording. Close cancelled.";
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            Logger.Log($"WINDOW_CLOSE_RECORDING_STOP: stop failed: {ex.Message}");
            ViewModel.StatusText = $"Close cancelled: recording stop failed ({ex.Message})";
            return false;
        }
        finally
        {
            if (shutdownContent != null &&
                Volatile.Read(ref _windowCloseAllowedAfterRecordingStop) == 0)
            {
                shutdownContent.IsHitTestVisible = true;
                shutdownContent.Opacity = 1;
            }
        }
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (Interlocked.Exchange(ref _windowCloseCleanupStarted, 1) != 0)
        {
            return;
        }

        _isWindowClosing = true;
        ViewModel.AudioMeterActivated -= EnsureAudioMeterTimerRunning;
        ViewModel.MicrophoneMeterActivated -= EnsureAudioMeterTimerRunning;
        _audioMeterAnimationTimer?.Stop();
        _audioMeterAnimationTimer = null;
        _liveSignalDebounceTimer?.Stop();
        _liveSignalDebounceTimer = null;
        _liveSignalHideDebounceTimer?.Stop();
        _liveSignalHideDebounceTimer = null;
        StopFullScreenAutoHideTimer();
        StopFlashbackStatusPolling();
        StopStatsDockPolling();
        HideStatsDockPanel(immediate: true);
        StopMicMeterRowAnimation();
        RecordingGlowPulseStoryboard.Stop();
        RecordingGlowBorder.Opacity = 0;
        RecPulseStoryboard.Stop();

        if (this.Content is FrameworkElement mainContent)
        {
            mainContent.SizeChanged -= MainWindow_SizeChanged;
        }

        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.PreviewStartRequested -= ViewModel_PreviewStartRequested;
        ViewModel.PreviewStopRequested -= ViewModel_PreviewStopRequested;
        ViewModel.PreviewReinitRequested -= ViewModel_PreviewReinitRequested;
        ViewModel.PreviewRendererStopRequested -= ViewModel_PreviewRendererStopRequested;

        try
        {
            StopPreviewForShutdown();
            ResetPreviewStartupTracking();
        }
        catch (Exception ex)
        {
            Logger.Log($"Preview shutdown cleanup failed: {ex.Message}");
        }

        // Graceful recording stop: the mux finalize (esp. 4K HDR with large buffered
        // NVENC frames) routinely exceeds the prior 5s timeout, producing a truncated
        // MP4 with no moov atom. Block the close on the real stop up to a generous
        // cap; surface "Stopping recording…" and disable input so the user sees the
        // app is working rather than appearing frozen.
        if (ViewModel.IsRecording)
        {
            const int StopBudgetMs = 120_000;
            Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording active, awaiting graceful stop...");
            ViewModel.StatusText = "Stopping recording — please wait…";
            if (this.Content is FrameworkElement shutdownContent)
            {
                shutdownContent.IsHitTestVisible = false;
                shutdownContent.Opacity = 0.5;
            }
            try
            {
                var stopTask = ViewModel.StopRecordingAndWaitAsync();
                var completed = await Task.WhenAny(stopTask, Task.Delay(StopBudgetMs));
                if (completed == stopTask)
                {
                    await stopTask; // propagate any exception
                    Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording stopped cleanly.");
                }
                else
                {
                    Logger.LogFatalBreadcrumb("RECORDING_FINALIZE_TIMEOUT "
                        + $"budget_ms={StopBudgetMs}; window already closed; continuing shutdown cleanup.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"WINDOW_CLOSE_RECORDING_STOP: stop failed: {ex.Message}");
                Logger.LogFatalBreadcrumb("RECORDING_FINALIZE_FAILED_AFTER_CLOSE "
                    + $"window already closed; continuing shutdown cleanup. error='{ex.Message}'");
            }
        }

        try
        {
            await _automationPipeServer.DisposeAsync();
            await _automationDiagnosticsHub.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation shutdown cleanup failed: {ex.Message}");
        }

        _nvmlMonitor?.Dispose();

        try
        {
            await ViewModel.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"ViewModel dispose during window close failed: {ex.Message}");
        }
    }
    private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            action();
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        }

        var enqueued = _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                action();
                completion.TrySetResult(null);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
            finally
            {
                registration.Dispose();
            }
        });

        if (!enqueued)
        {
            registration.Dispose();
            completion.TrySetException(new InvalidOperationException("Failed to enqueue window action on the UI thread."));
        }

        return completion.Task;
    }
    private Microsoft.UI.Windowing.AppWindow GetAppWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
    }
    private Task PresenterActionAsync(Action<Microsoft.UI.Windowing.OverlappedPresenter> action, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var appWindow = GetAppWindow();
            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                action(presenter);
            }
        }, cancellationToken);
    }
    public Task MinimizeAsync(CancellationToken cancellationToken = default) =>
        PresenterActionAsync(p => p.Minimize(), cancellationToken);
    public Task MaximizeAsync(CancellationToken cancellationToken = default) =>
        PresenterActionAsync(p => p.Maximize(), cancellationToken);
    public Task RestoreAsync(CancellationToken cancellationToken = default) =>
        PresenterActionAsync(p => p.Restore(), cancellationToken);
    public Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var appWindow = GetAppWindow();
            appWindow.Move(new Windows.Graphics.PointInt32(x, y));
        }, cancellationToken);
    }
    public Task ResizeToAsync(int width, int height, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var appWindow = GetAppWindow();
            appWindow.Resize(new Windows.Graphics.SizeInt32(Math.Max(1, width), Math.Max(1, height)));
        }, cancellationToken);
    }
    public Task SnapToRegionAsync(AutomationWindowAction region, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var appWindow = GetAppWindow();
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
            var work = displayArea.WorkArea;

            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter &&
                presenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized)
            {
                presenter.Restore();
            }

            int x, y, w, h;
            switch (region)
            {
                case AutomationWindowAction.SnapLeft:
                    x = work.X; y = work.Y; w = work.Width / 2; h = work.Height; break;
                case AutomationWindowAction.SnapRight:
                    x = work.X + work.Width / 2; y = work.Y; w = work.Width - work.Width / 2; h = work.Height; break;
                case AutomationWindowAction.SnapTopLeft:
                    x = work.X; y = work.Y; w = work.Width / 2; h = work.Height / 2; break;
                case AutomationWindowAction.SnapTopRight:
                    x = work.X + work.Width / 2; y = work.Y; w = work.Width - work.Width / 2; h = work.Height / 2; break;
                case AutomationWindowAction.SnapBottomLeft:
                    x = work.X; y = work.Y + work.Height / 2; w = work.Width / 2; h = work.Height - work.Height / 2; break;
                case AutomationWindowAction.SnapBottomRight:
                    x = work.X + work.Width / 2; y = work.Y + work.Height / 2; w = work.Width - work.Width / 2; h = work.Height - work.Height / 2; break;
                case AutomationWindowAction.Center:
                    var curSize = appWindow.Size;
                    w = curSize.Width; h = curSize.Height;
                    x = work.X + (work.Width - w) / 2; y = work.Y + (work.Height - h) / 2; break;
                default:
                    return;
            }

            appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, w, h));
        }, cancellationToken);
    }
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _windowCloseCleanupStarted) != 0)
        {
            return Task.CompletedTask;
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            RequestWindowClose();
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        }

        var enqueued = _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                RequestWindowClose();
                completion.TrySetResult(null);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
            finally
            {
                registration.Dispose();
            }
        });

        if (!enqueued)
        {
            registration.Dispose();
            if (Volatile.Read(ref _windowCloseCleanupStarted) != 0)
            {
                completion.TrySetResult(null);
            }
            else
            {
                completion.TrySetException(new InvalidOperationException("Failed to enqueue window close action on the UI thread."));
            }
        }

        return completion.Task;
    }
    private void RequestWindowClose()
    {
        if (Volatile.Read(ref _windowCloseCleanupStarted) != 0)
        {
            return;
        }

        if (Interlocked.Exchange(ref _windowCloseRequested, 1) != 0)
        {
            return;
        }

        try
        {
            Close();
        }
        catch (Exception ex) when (IsCloseAlreadyInProgressException(ex))
        {
            Logger.Log($"Window close already in progress ({ex.GetType().Name}); treating close request as successful.");
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            Logger.Log($"Window.Close COMException (0x{ex.HResult:X8}); using Application.Current.Exit() fallback.");
            Application.Current.Exit();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in MainWindow.RequestWindowClose: {ex.Message}");
            Interlocked.Exchange(ref _windowCloseRequested, 0);
            throw;
        }
    }
    private static bool IsCloseAlreadyInProgressException(Exception ex)
    {
        if (ex is InvalidOperationException && string.IsNullOrWhiteSpace(ex.Message))
        {
            return true;
        }

        var message = ex.Message ?? string.Empty;
        return message.IndexOf("closing", StringComparison.OrdinalIgnoreCase) >= 0 ||
               message.IndexOf("closed", StringComparison.OrdinalIgnoreCase) >= 0;
    }
    private async Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            ViewModel.StatusText = $"{operationName} failed: {ex.Message}";
        }
    }
    #region Minimum window size (Win32 interop)
    private const int GWLP_WNDPROC = -4;
    private const uint WM_GETMINMAXINFO = 0x0024;
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private struct POINT { public int X, Y; }
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_CLOAK = 13;
    private IntPtr MinSizeWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_GETMINMAXINFO)
        {
            var dpi = GetDpiForWindow(hWnd);
            var scale = dpi / 96.0;
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            mmi.ptMinTrackSize.X = (int)(MinWindowWidth * scale);
            mmi.ptMinTrackSize.Y = (int)(MinWindowHeight * scale);
            Marshal.StructureToPtr(mmi, lParam, false);
        }
        return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
    }
    #endregion
}
