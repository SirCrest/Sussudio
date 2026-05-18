using System;
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
    }
}
