using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
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

        // Wait up to eight seconds for recording finalization before fatal shutdown.
        // This is best effort: queueing, backend finalization, and cleanup may exceed
        // this outer limit even though the LibAv sink has a shorter emergency wait.
        // A timeout records the unresolved output; it does not cancel the stop task
        // or prove the file is complete. Some fatal errors can bypass this handler.
        private void TryEmergencyStopRecording(string source)
        {
            try
            {
                if (_window is not MainWindow mainWindow) return;
                var viewModel = mainWindow.ViewModel;
                if (viewModel == null) return;

                FinalizeRecordingForEmergency(
                    () => viewModel.StopRecordingForEmergencyAsync(),
                    viewModel.MarkRecordingFinalizationUnresolved,
                    source,
                    TimeSpan.FromSeconds(8));
            }
            catch (Exception ex)
            {
                Logger.Log($"EMERGENCY_FINALIZE_OUTER_FAIL msg={ex.Message}");
            }
        }

        private static void FinalizeRecordingForEmergency(
            Func<Task> stopRecording,
            Action<string> markUnresolved,
            string source,
            TimeSpan timeout)
        {
            Logger.LogFatalBreadcrumb($"EMERGENCY_FINALIZE_ATTEMPT source={source}");
            bool finished;
            try
            {
                // Starting can fail synchronously, and Wait throws for a faulted or
                // canceled task. Both must reach recovery marking before termination.
                var task = stopRecording();
                finished = task.Wait(timeout);
            }
            catch (Exception ex)
            {
                var failure = ex is AggregateException aggregate && aggregate.InnerExceptions.Count == 1
                    ? aggregate.InnerExceptions[0]
                    : ex;
                markUnresolved(
                    $"Emergency recording finalization failed after {source}: {failure.Message}");
                Logger.Log($"EMERGENCY_FINALIZE_INNER_FAIL msg={failure.Message}");
                Logger.LogFatalBreadcrumb("EMERGENCY_FINALIZE_FAILED", ex);
                return;
            }

            if (!finished)
            {
                markUnresolved(
                    $"Emergency recording finalization remained unresolved after the eight-second {source} deadline.");
            }

            Logger.LogFatalBreadcrumb(
                finished ? "EMERGENCY_FINALIZE_DONE" : "EMERGENCY_FINALIZE_TIMEOUT");
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
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
                    Logger.Log($"Suppressed exception in App.OnLaunched exe mtime probe: {ex.Message}");
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
                Logger.Log($"Suppressed exception in App.OnLaunched startup logging: {ex.Message}");
            }

            _window = new MainWindow();
            _window.Activate();
            // WinAppSDK terminates the dispatcher thread once the last window closes, so no extra Exit call is needed here.
        }
    }
}
