using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.UI.Xaml;
using Sussudio.Services.Recording;

namespace Sussudio
{
    public partial class App : Application
    {
        private Window? _window;

        public App()
        {
            InitializeComponent();

            // Add global exception handlers
            UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Logger.LogSystemInfo();
            try
            {
                LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);
            }
            catch (Exception ex)
            {
                // Surface the missing runtime immediately rather than letting the
                // first export fail with an opaque codec lookup error. The fatal
                // breadcrumb is the only diagnostic this early in startup - the
                // global UnhandledException handlers above are wired but a throw
                // from the ctor propagates up the WinUI activation stack before
                // they catch it, so the breadcrumb in the log is the support trail.
                Logger.LogFatalBreadcrumb("FFMPEG_RUNTIME_MISSING_AT_STARTUP", ex);
                throw;
            }
        }

        private static bool IsRecoverableUnhandled(Exception ex)
        {
            // Task-based exceptions often arrive wrapped in AggregateException when they
            // surface through Task.Wait/.Result or escape the async machinery on a worker
            // thread. Unwrap to a single inner before triage so a wrapped MF_E_NOTACCEPTING
            // or DXGI device-removed isn't misclassified as fatal and routed to FailFast.
            // We unwrap only when there's exactly one inner - a multi-fault aggregate is
            // unusual enough that we'd rather fail fast than guess which inner to trust.
            if (ex is AggregateException agg && agg.InnerExceptions.Count == 1 && agg.InnerException is not null)
            {
                ex = agg.InnerException;
            }

            if (ex is OperationCanceledException) return true;
            if (ex is IOException) return true;
            if (ex is TimeoutException) return true;
            if (ex is System.Runtime.InteropServices.COMException com)
            {
                // HRESULTs are 32-bit unsigned values but COMException.HResult is int.
                // Cast through unchecked so the literal 0x8XXXXXXX values compare correctly
                // (their signed-int reinterpretation is negative).
                unchecked
                {
                    return com.HResult == (int)0x887A0005   // DXGI_ERROR_DEVICE_REMOVED
                        || com.HResult == (int)0x887A0006   // DXGI_ERROR_DEVICE_HUNG
                        || com.HResult == (int)0x887A0007   // DXGI_ERROR_DEVICE_RESET
                        || com.HResult == (int)0x88890004   // AUDCLNT_E_DEVICE_INVALIDATED
                        || com.HResult == (int)0xC00D36B5   // MF_E_NOTACCEPTING
                        || com.HResult == (int)0xC00D4A44;  // MF_E_INVALID_STREAM_DATA
                }
            }
            return false;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Logger.Log("=== UNHANDLED EXCEPTION ===");
            Logger.LogException(e.Exception);
            Logger.Log($"Message: {e.Message}");

            if (IsRecoverableUnhandled(e.Exception))
            {
                Logger.Log("Unhandled exception classified as recoverable; continuing execution.");
                e.Handled = true;
                return;
            }

            TryEmergencyStopRecording("UI");
            Logger.LogFatalBreadcrumb("Fatal UI unhandled exception. Terminating process.", e.Exception);
            Environment.FailFast($"Fatal UI unhandled exception: {e.Message}", e.Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            Logger.Log("=== CURRENT DOMAIN UNHANDLED EXCEPTION ===");
            if (e.ExceptionObject is Exception ex)
            {
                Logger.LogException(ex);
            }
            else
            {
                Logger.Log($"Non-exception error: {e.ExceptionObject}");
            }
            Logger.Log($"IsTerminating: {e.IsTerminating}");

            if (e.ExceptionObject is Exception unhandledEx)
            {
                var recoverable = IsRecoverableUnhandled(unhandledEx);
                if (e.IsTerminating || !recoverable)
                {
                    TryEmergencyStopRecording("AppDomain");
                }

                if (!e.IsTerminating && !recoverable)
                {
                    Logger.LogFatalBreadcrumb("Escalating non-terminating AppDomain unhandled exception to fail-fast.", unhandledEx);
                    Environment.FailFast("Fatal AppDomain unhandled exception", unhandledEx);
                }
            }
        }

        // Best-effort: give the recording backend up to 8 seconds to flush the moov atom
        // before FailFast kills the process. Budget breakdown after fix #12 split:
        //   - LibAvRecordingSink.EmergencyStopTimeoutMs = 5s for the encode-drain,
        //   - DisposeTimeoutMs = 1s grace for the cancel-then-flush window (fix #11),
        //   - ~1-2s coordinator-queue + StopAndDisposeRecordingBackendAsync overhead.
        // Leaves headroom over the downstream ~6s worst case. The previous 3s budget
        // unconditionally cancelled downstream finalizers before they could finish,
        // truncating the file and surfacing nothing actionable.
        // A corrupted-state exception may still bypass this path (AVE is uncatchable
        // in .NET 8+), but ordinary unhandled exceptions on a background thread are
        // recoverable here.
        private void TryEmergencyStopRecording(string source)
        {
            try
            {
                if (_window is not MainWindow mainWindow) return;
                var viewModel = mainWindow.ViewModel;
                if (viewModel == null) return;

                Logger.LogFatalBreadcrumb($"EMERGENCY_FINALIZE_ATTEMPT source={source}");
                var task = viewModel.StopRecordingForEmergencyAsync();
                var finished = task.Wait(TimeSpan.FromSeconds(8));
                if (finished)
                {
                    try
                    {
                        task.GetAwaiter().GetResult();
                    }
                    catch (Exception inner)
                    {
                        Logger.Log($"EMERGENCY_FINALIZE_INNER_FAIL msg={inner.Message}");
                    }
                }

                Logger.LogFatalBreadcrumb(
                    finished ? "EMERGENCY_FINALIZE_DONE" : "EMERGENCY_FINALIZE_TIMEOUT");
            }
            catch (Exception ex)
            {
                Logger.Log($"EMERGENCY_FINALIZE_OUTER_FAIL msg={ex.Message}");
            }
        }

        // Held for the process lifetime so the OS releases ownership on exit/crash.
        // Static field prevents GC from finalizing the Mutex (which would release
        // ownership and allow a racing second instance to acquire it mid-run).
        // Name is in the Local\ namespace so it scopes per-session (RDP/fast-user-switch
        // safe) rather than machine-global. Version suffix lets us bump if semantics change.
        private const string SingleInstanceMutexName = @"Local\Sussudio.SingleInstance.v1";
        private static Mutex? _singleInstanceMutex;

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Single-instance guard MUST run before any startup work that touches the
            // shared flashback temp directory (%TEMP%\Sussudio). The stale-session cleanup
            // heuristic in MainWindow startup will delete 32-hex segment directories it
            // does not recognize as marked, which would destroy an already-running
            // instance's in-flight flashback segments. Acquire the mutex first; if a
            // prior instance owns it, log a fatal breadcrumb and exit cleanly without
            // wiring up a second MainWindow or binding the automation pipe.
            try
            {
                _singleInstanceMutex = new Mutex(initiallyOwned: false, name: SingleInstanceMutexName, createdNew: out var createdNew);
                var acquired = false;
                if (createdNew)
                {
                    // We created it; take ownership now.
                    acquired = _singleInstanceMutex.WaitOne(TimeSpan.Zero, exitContext: false);
                }
                else
                {
                    // Existing mutex (possibly orphaned from a prior crashed instance).
                    // Try a zero-timeout acquisition; AbandonedMutexException means the
                    // previous owner died without releasing - we successfully take ownership.
                    try
                    {
                        acquired = _singleInstanceMutex.WaitOne(TimeSpan.Zero, exitContext: false);
                    }
                    catch (AbandonedMutexException)
                    {
                        Logger.Log("SINGLE_INSTANCE_GUARD acquired abandoned mutex from prior crashed instance");
                        acquired = true;
                    }
                }

                if (!acquired)
                {
                    Logger.LogFatalBreadcrumb($"SINGLE_INSTANCE_GUARD second instance detected (mutex='{SingleInstanceMutexName}'); exiting before touching flashback temp dir.");
                    try { _singleInstanceMutex.Dispose(); } catch { /* best-effort */ }
                    _singleInstanceMutex = null;
                    Environment.Exit(0);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogFatalBreadcrumb(
                    $"SINGLE_INSTANCE_GUARD mutex setup failed; refusing launch. msg={ex.Message}",
                    ex);
                try { _singleInstanceMutex?.Dispose(); } catch { /* best-effort */ }
                _singleInstanceMutex = null;
                Environment.Exit(1);
                return;
            }

            try
            {
                var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "unknown";
                var exeMtimeUtc = "unknown";
                try
                {
                    if (!string.IsNullOrWhiteSpace(exePath) && File.Exists(exePath))
                    {
                        exeMtimeUtc = File.GetLastWriteTimeUtc(exePath).ToString("o");
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning($"Suppressed exception in App.OnLaunched exe mtime probe: {ex.Message}");
                }

                var assembly = Assembly.GetExecutingAssembly();
                var assemblyName = assembly.GetName();
                Logger.Log(
                    "APP_START " +
                    $"exe='{exePath}' " +
                    $"exe_mtime_utc='{exeMtimeUtc}' " +
                    $"assembly='{assemblyName.Name}' " +
                    $"assembly_version='{assemblyName.Version}' " +
                    $"base_dir='{AppContext.BaseDirectory}'");
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Suppressed exception in App.OnLaunched startup logging: {ex.Message}");
            }

            _window = new MainWindow();
            _window.Activate();
            // WinAppSDK terminates the dispatcher thread once the last window closes, so no extra Exit call is needed here.
        }
    }
}
