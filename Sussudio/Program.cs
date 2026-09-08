using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Sussudio.Services.Runtime;

namespace Sussudio;

public static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (NativeFfmpegCapabilityProbe.TryRunChildProcess(args, out var probeExitCode))
        {
            return probeExitCode;
        }

        return AppProcessStartup.RunNormal(StartApplication);
    }

    private static void StartApplication()
    {
#if SUSSUDIO_EXPLICIT_WINDOWS_APP_RUNTIME_STARTUP
        Microsoft.Windows.ApplicationModel.WindowsAppRuntime.Common.AutoInitialize.InitializeWindowsAppSDK();
#endif
        StartWinUiApplication();
    }

    // Keep WinUI type loading behind the SDK initialization above.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StartWinUiApplication()
    {
        // Preserve the WinUI-generated entry sequence after process admission.
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
