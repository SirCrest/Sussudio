using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Sussudio.Models;
using Sussudio.Services.Automation;
using Sussudio.Services.Contracts;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Tools;
using Sussudio.ViewModels;
using Windows.Graphics;
using WinRT.Interop;

namespace Sussudio.Controllers;

internal sealed class WindowAutomationHostLifecycleController : IAsyncDisposable
{
    private readonly IAutomationDiagnosticsHub _diagnosticsHub;
    private readonly NamedPipeAutomationServer _pipeServer;
    private readonly bool _tokenRequired;
    private readonly string _pipeName;
    private int _started;

    public WindowAutomationHostLifecycleController(
        IAutomationViewModel viewModel,
        Func<CancellationToken, Task<PreviewRuntimeSnapshot>> previewSnapshotProvider,
        IAutomationWindowControl windowControl)
    {
        var automationToken = Environment.GetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar);
        var automationPipeName = Environment.GetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE");
        if (string.IsNullOrWhiteSpace(automationPipeName))
        {
            automationPipeName = NamedPipeAutomationServer.DefaultPipeName;
        }

        var automationPorts = AutomationViewModelPorts.From(viewModel);
        _diagnosticsHub = new AutomationDiagnosticsHub(
            automationPorts.SnapshotQuery,
            previewSnapshotProvider,
            new RecordingVerifier());
        var automationDispatcher = new AutomationCommandDispatcher(
            automationPorts,
            _diagnosticsHub,
            windowControl,
            automationToken);

        _tokenRequired = !string.IsNullOrWhiteSpace(automationToken);
        _pipeName = automationPipeName;
        _pipeServer = new NamedPipeAutomationServer(
            automationDispatcher,
            _pipeName,
            _tokenRequired);
    }

    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        if (_pipeServer.Start())
        {
            _diagnosticsHub.Start();
            Logger.Log(
                $"Automation control ready on pipe '{_pipeName}' (token required={_tokenRequired}).");
        }
        else
        {
            Logger.Log(
                $"Automation control disabled on pipe '{_pipeName}' (token required={_tokenRequired}).");
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _pipeServer.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation shutdown cleanup failed: {ex.Message}");
        }

        try
        {
            await _diagnosticsHub.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation diagnostics shutdown cleanup failed: {ex.Message}");
        }
    }
}

internal sealed class WindowAutomationControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required MainViewModel ViewModel { get; init; }
    public required Func<AppWindow> GetAppWindow { get; init; }
    public required Func<IntPtr> GetWindowHandle { get; init; }
    public required Func<Action, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }
}

internal sealed class WindowAutomationController
{
    private readonly WindowAutomationControllerContext _context;

    public WindowAutomationController(WindowAutomationControllerContext context)
    {
        _context = context;
    }

    public Task MinimizeAsync(CancellationToken cancellationToken = default) =>
        PresenterActionAsync(p => p.Minimize(), cancellationToken);

    public Task MaximizeAsync(CancellationToken cancellationToken = default) =>
        PresenterActionAsync(p => p.Maximize(), cancellationToken);

    public Task RestoreAsync(CancellationToken cancellationToken = default) =>
        PresenterActionAsync(p => p.Restore(), cancellationToken);

    public Task OpenRecordingsFolderAsync(CancellationToken cancellationToken = default)
    {
        return _context.InvokeOnUiThreadAsync(() =>
        {
            var path = _context.ViewModel.OutputPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException("Output path is not set.");
            }

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Output path does not exist: {path}");
            }

            Process.Start("explorer.exe", path);
        }, cancellationToken);
    }

    public Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        return _context.InvokeOnUiThreadAsync(() =>
        {
            var appWindow = _context.GetAppWindow();
            appWindow.Move(new Windows.Graphics.PointInt32(x, y));
        }, cancellationToken);
    }

    public Task ResizeToAsync(int width, int height, CancellationToken cancellationToken = default)
    {
        return _context.InvokeOnUiThreadAsync(() =>
        {
            var appWindow = _context.GetAppWindow();
            appWindow.Resize(new Windows.Graphics.SizeInt32(Math.Max(1, width), Math.Max(1, height)));
        }, cancellationToken);
    }

    public Task SnapToRegionAsync(AutomationWindowAction region, CancellationToken cancellationToken = default)
    {
        return _context.InvokeOnUiThreadAsync(() =>
        {
            var appWindow = _context.GetAppWindow();
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_context.GetWindowHandle());
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            var work = displayArea.WorkArea;

            if (appWindow.Presenter is OverlappedPresenter presenter &&
                presenter.State == OverlappedPresenterState.Maximized)
            {
                presenter.Restore();
            }

            var currentSize = region == AutomationWindowAction.Center
                ? appWindow.Size
                : default;
            var targetBounds = WindowSnapRegionLayoutPolicy.ResolveTargetBounds(region, work, currentSize);
            if (targetBounds is not { } bounds)
            {
                return;
            }

            appWindow.MoveAndResize(bounds);
        }, cancellationToken);
    }

    private Task PresenterActionAsync(Action<OverlappedPresenter> action, CancellationToken cancellationToken = default)
    {
        return _context.InvokeOnUiThreadAsync(() =>
        {
            var appWindow = _context.GetAppWindow();
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                action(presenter);
            }
        }, cancellationToken);
    }
}

internal static class WindowSnapRegionLayoutPolicy
{
    public static RectInt32? ResolveTargetBounds(
        AutomationWindowAction region,
        RectInt32 workArea,
        SizeInt32 currentSize)
    {
        return region switch
        {
            AutomationWindowAction.SnapLeft => new RectInt32(
                workArea.X,
                workArea.Y,
                workArea.Width / 2,
                workArea.Height),
            AutomationWindowAction.SnapRight => new RectInt32(
                workArea.X + workArea.Width / 2,
                workArea.Y,
                workArea.Width - workArea.Width / 2,
                workArea.Height),
            AutomationWindowAction.SnapTopLeft => new RectInt32(
                workArea.X,
                workArea.Y,
                workArea.Width / 2,
                workArea.Height / 2),
            AutomationWindowAction.SnapTopRight => new RectInt32(
                workArea.X + workArea.Width / 2,
                workArea.Y,
                workArea.Width - workArea.Width / 2,
                workArea.Height / 2),
            AutomationWindowAction.SnapBottomLeft => new RectInt32(
                workArea.X,
                workArea.Y + workArea.Height / 2,
                workArea.Width / 2,
                workArea.Height - workArea.Height / 2),
            AutomationWindowAction.SnapBottomRight => new RectInt32(
                workArea.X + workArea.Width / 2,
                workArea.Y + workArea.Height / 2,
                workArea.Width - workArea.Width / 2,
                workArea.Height - workArea.Height / 2),
            AutomationWindowAction.Center => new RectInt32(
                workArea.X + (workArea.Width - currentSize.Width) / 2,
                workArea.Y + (workArea.Height - currentSize.Height) / 2,
                currentSize.Width,
                currentSize.Height),
            _ => null
        };
    }
}

internal readonly record struct NativeWindowBootstrapResult(IntPtr Hwnd, AppWindow AppWindow);

internal sealed class NativeWindowBootstrapController
{
    private const int MinWindowWidth = 1500;
    private const int MinWindowHeight = 900;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_CLOAK = 13;

    private MinSizeWindowSubclass.MinSizeHandle? _minSizeHandle;
    private EventHandler<object>? _pendingFirstFrameReveal;

    public NativeWindowBootstrapResult Initialize(Window window, Action<IntPtr> setWindowHandle)
    {
        // Set window handle for folder picker and automation adapters.
        var hwnd = WindowNative.GetWindowHandle(window);
        setWindowHandle(hwnd);

        // Cloak the window to prevent white flash before XAML renders.
        SetCloaked(hwnd, cloaked: true);
        SetDarkMode(hwnd, enabled: true);

        // Enforce minimum window size via WM_GETMINMAXINFO.
        _minSizeHandle = MinSizeWindowSubclass.Install(hwnd, MinWindowWidth, MinWindowHeight);

        var appWindow = AppWindow.GetFromWindowId(
            Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
            presenter.Restore();
        }

        // Accommodates a 1920x1080 preview, controls, spacing, and titlebar.
        appWindow.Resize(new Windows.Graphics.SizeInt32(1950, 1450));
        appWindow.SetIcon("Assets\\AppIcon.ico");

        return new NativeWindowBootstrapResult(hwnd, appWindow);
    }

    public AppWindow GetAppWindow(Window window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    public void SetCloaked(IntPtr hwnd, bool cloaked)
    {
        var value = cloaked ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_CLOAK, ref value, sizeof(int));
    }

    public void ScheduleRevealAfterFirstComposedFrame(IntPtr hwnd)
    {
        // Loaded fires after layout but before the first paint; wait for the
        // first composed frame so the cloaked shell never exposes a black frame.
        CancelPendingFirstFrameReveal();
        EventHandler<object>? revealOnFirstFrame = null;
        revealOnFirstFrame = (_, _) =>
        {
            CancelPendingFirstFrameReveal();
            SetCloaked(hwnd, cloaked: false);
        };
        _pendingFirstFrameReveal = revealOnFirstFrame;
        Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += revealOnFirstFrame;
    }

    public void CancelPendingFirstFrameReveal()
    {
        var pending = _pendingFirstFrameReveal;
        if (pending == null)
        {
            return;
        }

        Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= pending;
        _pendingFirstFrameReveal = null;
    }

    private static void SetDarkMode(IntPtr hwnd, bool enabled)
    {
        var value = enabled ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
}

internal readonly record struct WindowCloseLifecycleSnapshot(
    int Requested,
    int CleanupStarted,
    int RecordingStopInProgress,
    int AllowedAfterRecordingStop,
    bool IsClosing);

internal sealed class WindowCloseLifecycleController
{
    private int _closeRequested;
    private int _cleanupStarted;
    private int _recordingStopInProgress;
    private int _allowedAfterRecordingStop;
    private readonly object _completionLock = new();
    private TaskCompletionSource<object?>? _completion;
    private bool _isClosing;

    public bool IsClosing => _isClosing;

    public bool IsCleanupStarted => Volatile.Read(ref _cleanupStarted) != 0;

    public bool IsRecordingStopInProgress => Volatile.Read(ref _recordingStopInProgress) != 0;

    public bool IsAllowedAfterRecordingStop => Volatile.Read(ref _allowedAfterRecordingStop) != 0;

    public WindowCloseLifecycleSnapshot Snapshot => new(
        Volatile.Read(ref _closeRequested),
        Volatile.Read(ref _cleanupStarted),
        Volatile.Read(ref _recordingStopInProgress),
        Volatile.Read(ref _allowedAfterRecordingStop),
        _isClosing);

    public bool TryBeginCleanup()
        => Interlocked.Exchange(ref _cleanupStarted, 1) == 0;

    public void MarkClosing()
        => _isClosing = true;

    public void CompleteRequest(Exception? exception = null)
    {
        TaskCompletionSource<object?>? completion;
        lock (_completionLock)
        {
            completion = _completion;
            _completion = null;
        }

        if (completion == null)
        {
            return;
        }

        if (exception == null)
        {
            completion.TrySetResult(null);
        }
        else
        {
            completion.TrySetException(exception);
        }
    }

    public void ClearRequested()
        => Interlocked.Exchange(ref _closeRequested, 0);

    public bool TryBeginRecordingStop()
        => Interlocked.Exchange(ref _recordingStopInProgress, 1) == 0;

    public void EndRecordingStop()
        => Interlocked.Exchange(ref _recordingStopInProgress, 0);

    public void AllowAfterRecordingStop()
    {
        Interlocked.Exchange(ref _allowedAfterRecordingStop, 1);
        ClearRequested();
    }

    public bool TryMarkRequested()
    {
        if (IsCleanupStarted)
        {
            CompleteRequest();
            return false;
        }

        return Interlocked.Exchange(ref _closeRequested, 1) == 0;
    }

    public void ResetRequestedAfterFailure()
        => ClearRequested();

    public Task CloseAsync(
        DispatcherQueue dispatcherQueue,
        Action requestWindowClose,
        CancellationToken cancellationToken = default)
    {
        if (dispatcherQueue == null)
        {
            throw new ArgumentNullException(nameof(dispatcherQueue));
        }

        if (requestWindowClose == null)
        {
            throw new ArgumentNullException(nameof(requestWindowClose));
        }

        if (IsCleanupStarted)
        {
            return Task.CompletedTask;
        }

        var closeCompletionTask = GetCompletionTask(cancellationToken);

        if (dispatcherQueue.HasThreadAccess)
        {
            requestWindowClose();
            return closeCompletionTask;
        }

        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        }

        var enqueued = dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                requestWindowClose();
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
            if (IsCleanupStarted)
            {
                completion.TrySetResult(null);
            }
            else
            {
                var enqueueFailure = new InvalidOperationException("Failed to enqueue window close action on the UI thread.");
                CompleteRequest(enqueueFailure);
                completion.TrySetException(enqueueFailure);
            }
        }

        return AwaitWindowCloseRequestAsync(completion.Task, closeCompletionTask);
    }

    public static bool IsCloseAlreadyInProgressException(Exception ex)
    {
        if (ex is InvalidOperationException && string.IsNullOrWhiteSpace(ex.Message))
        {
            return true;
        }

        var message = ex.Message ?? string.Empty;
        return message.IndexOf("closing", StringComparison.OrdinalIgnoreCase) >= 0 ||
               message.IndexOf("closed", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private Task GetCompletionTask(CancellationToken cancellationToken)
    {
        TaskCompletionSource<object?> completion;
        lock (_completionLock)
        {
            if (_completion == null || _completion.Task.IsCompleted)
            {
                _completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            completion = _completion;
        }

        return cancellationToken.CanBeCanceled
            ? completion.Task.WaitAsync(cancellationToken)
            : completion.Task;
    }

    private static async Task AwaitWindowCloseRequestAsync(Task enqueueTask, Task closeCompletionTask)
    {
        await enqueueTask.ConfigureAwait(false);
        await closeCompletionTask.ConfigureAwait(false);
    }
}

internal sealed class WindowCloseRequestControllerContext
{
    public required WindowCloseLifecycleController LifecycleController { get; init; }
    public required Action CloseWindow { get; init; }
    public required Action ExitApplication { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<bool> IsRecordingTransitioning { get; init; }
}

internal sealed class WindowCloseRequestController
{
    private readonly WindowCloseRequestControllerContext _context;

    public WindowCloseRequestController(WindowCloseRequestControllerContext context)
    {
        _context = context;
    }

    public void RequestClose()
    {
        if (!_context.LifecycleController.TryMarkRequested())
        {
            return;
        }

        try
        {
            _context.CloseWindow();
            if (!_context.LifecycleController.IsRecordingStopInProgress &&
                !_context.IsRecording() &&
                !_context.IsRecordingTransitioning())
            {
                _context.LifecycleController.CompleteRequest();
            }
        }
        catch (Exception ex) when (WindowCloseLifecycleController.IsCloseAlreadyInProgressException(ex))
        {
            Logger.Log($"Window close already in progress ({ex.GetType().Name}); treating close request as successful.");
            _context.LifecycleController.CompleteRequest();
        }
        catch (COMException ex)
        {
            Logger.Log($"Window.Close COMException (0x{ex.HResult:X8}); using Application.Current.Exit() fallback.");
            _context.LifecycleController.CompleteRequest();
            _context.ExitApplication();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in MainWindow.RequestWindowClose: {ex.Message}");
            _context.LifecycleController.ResetRequestedAfterFailure();
            _context.LifecycleController.CompleteRequest(ex);
            throw;
        }
    }
}

internal sealed class WindowAppClosingControllerContext
{
    public required WindowCloseLifecycleController LifecycleController { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<bool> IsRecordingTransitioning { get; init; }
    public required Func<string> GetStatusText { get; init; }
    public required Func<Task<bool>> StopRecordingBeforeCloseAsync { get; init; }
    public required Action RequestWindowClose { get; init; }
}

internal sealed class WindowAppClosingController
{
    private readonly WindowAppClosingControllerContext _context;

    public WindowAppClosingController(WindowAppClosingControllerContext context)
    {
        _context = context;
    }

    public async Task HandleClosingAsync(AppWindowClosingEventArgs args)
    {
        LogWindowClosingTrigger();

        if (_context.LifecycleController.IsCleanupStarted ||
            _context.LifecycleController.IsAllowedAfterRecordingStop)
        {
            _context.LifecycleController.CompleteRequest();
            return;
        }

        if (!_context.IsRecording() && !_context.IsRecordingTransitioning())
        {
            _context.LifecycleController.CompleteRequest();
            return;
        }

        args.Cancel = true;
        _context.LifecycleController.ClearRequested();

        if (!_context.LifecycleController.TryBeginRecordingStop())
        {
            Logger.Log("WINDOW_CLOSE_RECORDING_STOP: close already waiting for recording stop.");
            return;
        }

        try
        {
            var stopped = await _context.StopRecordingBeforeCloseAsync();
            if (!stopped)
            {
                _context.LifecycleController.CompleteRequest(new InvalidOperationException(_context.GetStatusText()));
                return;
            }

            _context.LifecycleController.AllowAfterRecordingStop();
            _context.LifecycleController.CompleteRequest();
            _context.RequestWindowClose();
        }
        finally
        {
            _context.LifecycleController.EndRecordingStop();
        }
    }

    private void LogWindowClosingTrigger()
    {
        try
        {
            var snapshot = _context.LifecycleController.Snapshot;
            Logger.Log(
                "WINDOW_CLOSING_TRIGGER " +
                $"requested={snapshot.Requested} " +
                $"isRecording={_context.IsRecording()} " +
                $"stack=\n{new System.Diagnostics.StackTrace(true)}");
        }
        catch (Exception logEx)
        {
            System.Diagnostics.Trace.TraceWarning($"WINDOW_CLOSING_TRIGGER log failed: {logEx.Message}");
        }
    }
}

internal sealed class WindowCloseRecordingFinalizationController
{
    private const int StopBudgetMs = 120_000;

    public async Task<bool> StopBeforeCloseAsync(
        MainViewModel viewModel,
        FrameworkElement? shutdownContent,
        Func<bool> isAllowedAfterRecordingStop)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(isAllowedAfterRecordingStop);

        Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording active, awaiting graceful stop...");
        viewModel.StatusText = "Stopping recording â€” please waitâ€¦";

        if (shutdownContent != null)
        {
            shutdownContent.IsHitTestVisible = false;
            shutdownContent.Opacity = 0.5;
        }

        try
        {
            var stopResult = await WaitForRecordingStopAsync(viewModel);
            if (stopResult.Status == RecordingStopWaitStatus.Completed)
            {
                Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording stopped cleanly.");
                return true;
            }

            Logger.LogFatalBreadcrumb("RECORDING_FINALIZE_TIMEOUT "
                + $"budget_ms={StopBudgetMs}; close cancelled to protect recording.");
            viewModel.StatusText = "Still saving recording. Close cancelled.";
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            Logger.Log($"WINDOW_CLOSE_RECORDING_STOP: stop failed: {ex.Message}");
            viewModel.StatusText = $"Close cancelled: recording stop failed ({ex.Message})";
            return false;
        }
        finally
        {
            if (shutdownContent != null &&
                !isAllowedAfterRecordingStop())
            {
                shutdownContent.IsHitTestVisible = true;
                shutdownContent.Opacity = 1;
            }
        }
    }

    public async Task<RecordingStopWaitResult> StopAfterClosedBestEffortAsync(
        MainViewModel viewModel,
        FrameworkElement? shutdownContent)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        if (!viewModel.IsRecording)
        {
            return RecordingStopWaitResult.Completed;
        }

        Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording active, awaiting graceful stop...");
        viewModel.StatusText = "Stopping recording - please wait...";

        if (shutdownContent != null)
        {
            shutdownContent.IsHitTestVisible = false;
            shutdownContent.Opacity = 0.5;
        }

        try
        {
            var stopResult = await WaitForRecordingStopAsync(viewModel);
            if (stopResult.Status == RecordingStopWaitStatus.Completed)
            {
                Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording stopped cleanly.");
            }
            else
            {
                viewModel.MarkRecordingFinalizationUnresolved(
                    $"Recording finalization unresolved after window close timeout ({StopBudgetMs} ms).");
                Logger.LogFatalBreadcrumb("RECORDING_FINALIZE_TIMEOUT "
                    + $"budget_ms={StopBudgetMs}; window already closed; continuing shutdown cleanup.");
            }

            return stopResult;
        }
        catch (Exception ex)
        {
            viewModel.MarkRecordingFinalizationUnresolved(
                $"Recording finalization failed after window close: {ex.Message}");
            Logger.Log($"WINDOW_CLOSE_RECORDING_STOP: stop failed: {ex.Message}");
            Logger.LogFatalBreadcrumb("RECORDING_FINALIZE_FAILED_AFTER_CLOSE "
                + $"window already closed; continuing shutdown cleanup. error='{ex.Message}'");
            return RecordingStopWaitResult.Failed(ex.Message);
        }
    }

    private static async Task<RecordingStopWaitResult> WaitForRecordingStopAsync(MainViewModel viewModel)
    {
        var stopTask = viewModel.StopRecordingAndWaitAsync();
        var completed = await Task.WhenAny(stopTask, Task.Delay(StopBudgetMs));
        if (completed == stopTask)
        {
            await stopTask;
            return RecordingStopWaitResult.Completed;
        }

        return RecordingStopWaitResult.TimedOut;
    }
}

internal enum RecordingStopWaitStatus
{
    Completed,
    TimedOut,
    Failed,
}

internal readonly record struct RecordingStopWaitResult(RecordingStopWaitStatus Status, string? ErrorMessage = null)
{
    public static RecordingStopWaitResult Completed { get; } = new(RecordingStopWaitStatus.Completed);
    public static RecordingStopWaitResult TimedOut { get; } = new(RecordingStopWaitStatus.TimedOut);
    public static RecordingStopWaitResult Failed(string errorMessage) =>
        new(RecordingStopWaitStatus.Failed, errorMessage);
}

internal sealed class WindowShutdownCleanupControllerContext
{
    public required WindowCloseLifecycleController LifecycleController { get; init; }

    public required Func<bool> IsRecording { get; init; }

    public required Func<bool> IsPreviewing { get; init; }

    public required Action CancelNativeShellRevealAfterFirstFrame { get; init; }

    public required Action CompleteWindowCloseRequest { get; init; }

    public required Action DetachMeterActivationHandlers { get; init; }

    public required Action StopTimers { get; init; }

    public required Action StopStatsOverlay { get; init; }

    public required Action StopRecordingVisuals { get; init; }

    public required Action DetachMainContentSizeChanged { get; init; }

    public required Action DetachViewModelEventHandlers { get; init; }

    public required Action StopPreviewForShutdown { get; init; }

    public required Action ResetPreviewStartupTracking { get; init; }

    public required Func<Task<RecordingStopWaitResult>> StopRecordingAfterClosedBestEffortAsync { get; init; }

    public required Func<ValueTask> DisposeAutomationHostAsync { get; init; }

    public required Action DisposeNvmlMonitor { get; init; }

    public required Func<ValueTask> DisposeViewModelAsync { get; init; }
}

internal sealed class WindowShutdownCleanupController
{
    private readonly WindowShutdownCleanupControllerContext _context;

    public WindowShutdownCleanupController(WindowShutdownCleanupControllerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task RunAsync()
    {
        _context.CancelNativeShellRevealAfterFirstFrame();

        if (!_context.LifecycleController.TryBeginCleanup())
        {
            return;
        }

        LogWindowClosedTrigger();

        _context.CompleteWindowCloseRequest();
        _context.LifecycleController.MarkClosing();
        _context.DetachMeterActivationHandlers();
        _context.StopTimers();
        _context.StopStatsOverlay();
        _context.StopRecordingVisuals();
        _context.DetachMainContentSizeChanged();
        _context.DetachViewModelEventHandlers();

        try
        {
            _context.StopPreviewForShutdown();
            _context.ResetPreviewStartupTracking();
        }
        catch (Exception ex)
        {
            Logger.Log($"Preview shutdown cleanup failed: {ex.Message}");
        }

        var recordingStopResult = await _context.StopRecordingAfterClosedBestEffortAsync().ConfigureAwait(false);
        if (recordingStopResult.Status != RecordingStopWaitStatus.Completed)
        {
            Logger.Log(
                "WINDOW_CLOSE_RECORDING_STOP_UNRESOLVED " +
                $"status={recordingStopResult.Status} " +
                $"error='{recordingStopResult.ErrorMessage ?? string.Empty}'");
        }

        await _context.DisposeAutomationHostAsync().ConfigureAwait(false);

        _context.DisposeNvmlMonitor();

        try
        {
            await _context.DisposeViewModelAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"ViewModel dispose during window close failed: {ex.Message}");
        }

        await Logger.ShutdownAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
    }

    private void LogWindowClosedTrigger()
    {
        try
        {
            var snapshot = _context.LifecycleController.Snapshot;
            Logger.Log(
                "WINDOW_CLOSED_TRIGGER " +
                $"requested={snapshot.Requested} " +
                $"recordingStopInProgress={snapshot.RecordingStopInProgress} " +
                $"allowedAfterRecordingStop={snapshot.AllowedAfterRecordingStop} " +
                $"isRecording={_context.IsRecording()} " +
                $"isPreviewing={_context.IsPreviewing()} " +
                $"stack=\n{new StackTrace(true)}");
        }
        catch (Exception logEx)
        {
            Trace.TraceWarning($"WINDOW_CLOSED_TRIGGER log failed: {logEx.Message}");
        }
    }
}
