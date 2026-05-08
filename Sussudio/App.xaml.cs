using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.UI.Xaml;
using Sussudio.Services.Audio;
using Sussudio.Services.Automation;
using Sussudio.Services.Capture;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

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
            LibAvEncoder.InitializeFFmpeg();
        }

        private static bool IsRecoverableUnhandled(Exception ex)
        {
            return ex is OperationCanceledException;
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

        // Best-effort: give the mux up to 3 seconds to write the moov atom before
        // FailFast kills the process. Better to try and fail than guarantee a
        // truncated MP4. A corrupted-state exception may still bypass this path
        // (AVE is uncatchable in .NET 8+), but ordinary unhandled exceptions on
        // a background thread are recoverable here.
        private void TryEmergencyStopRecording(string source)
        {
            try
            {
                if (_window is not MainWindow mainWindow) return;
                var viewModel = mainWindow.ViewModel;
                if (viewModel == null) return;

                Logger.LogFatalBreadcrumb($"EMERGENCY_FINALIZE_ATTEMPT source={source}");
                var task = viewModel.StopRecordingForEmergencyAsync();
                var finished = task.Wait(TimeSpan.FromSeconds(3));
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
                    System.Diagnostics.Trace.TraceWarning($"Suppressed exception in App.OnLaunched exe mtime probe: {ex.Message}");
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
                System.Diagnostics.Trace.TraceWarning($"Suppressed exception in App.OnLaunched startup logging: {ex.Message}");
            }

            _window = new MainWindow();
            _window.Activate();
            // WinAppSDK terminates the dispatcher thread once the last window closes, so no extra Exit call is needed here.
        }
    }
}
