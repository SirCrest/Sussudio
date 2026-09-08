using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Runtime;

// Admission precedes App construction because logging and Flashback startup
// both mutate shared files. The callback stays on the mutex-owning thread.
internal static class AppProcessStartup
{
    internal const string SingleInstanceMutexName = @"Local\Sussudio.SingleInstance.v1";

    internal static int RunNormal(Action startApplication, string mutexName = SingleInstanceMutexName)
    {
        ArgumentNullException.ThrowIfNull(startApplication);

        Mutex? mutex = null;
        bool acquired;
        var abandoned = false;
        try
        {
            mutex = new Mutex(initiallyOwned: false, name: mutexName);
            try
            {
                acquired = mutex.WaitOne(TimeSpan.Zero, exitContext: false);
            }
            catch (AbandonedMutexException)
            {
                acquired = true;
                abandoned = true;
            }
        }
        catch (Exception ex)
        {
            mutex?.Dispose();
            Trace.TraceError($"SINGLE_INSTANCE_GUARD mutex setup failed; refusing launch. msg={ex.Message}");
            return 1;
        }

        using (mutex)
        {
            if (!acquired)
            {
                Trace.TraceInformation($"SINGLE_INSTANCE_GUARD second instance detected (mutex='{mutexName}'); exiting before shared startup work.");
                return 0;
            }

            try
            {
                if (abandoned)
                {
                    Trace.TraceInformation("SINGLE_INSTANCE_GUARD acquired abandoned mutex from prior crashed instance");
                }

                startApplication();
                return 0;
            }
            finally
            {
                // Application.Start returns after the existing window-close path;
                // releasing here also handles a failed App constructor.
                mutex.ReleaseMutex();
            }
        }
    }
}
