using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Sussudio
{
    public partial class App
    {
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
                // Mutex creation should not fail under normal conditions. If it does
                // (e.g. ACL denial), log and continue rather than blocking launch -
                // the cleanup-corruption hazard is rare and a hard fail would be worse.
                Logger.Log($"SINGLE_INSTANCE_GUARD mutex setup failed; proceeding without guard. msg={ex.Message}");
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
