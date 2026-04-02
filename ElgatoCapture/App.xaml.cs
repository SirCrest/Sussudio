using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using ElgatoCapture.Services;
using Microsoft.UI.Xaml;

namespace ElgatoCapture
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

            if (!e.IsTerminating && e.ExceptionObject is Exception unhandledEx && !IsRecoverableUnhandled(unhandledEx))
            {
                Logger.LogFatalBreadcrumb("Escalating non-terminating AppDomain unhandled exception to fail-fast.", unhandledEx);
                Environment.FailFast("Fatal AppDomain unhandled exception", unhandledEx);
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
