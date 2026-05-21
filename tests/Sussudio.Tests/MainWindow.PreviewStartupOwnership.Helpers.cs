using System.Linq;

static partial class Program
{
    private static string ReadMainWindowPreviewStartupAdapterSource()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.PreviewStartup.cs",
                "Sussudio/MainWindow.PreviewStartup.Session.cs",
                "Sussudio/MainWindow.PreviewStartup.Session.Composition.cs",
                "Sussudio/MainWindow.PreviewStartup.Session.State.cs",
                "Sussudio/MainWindow.PreviewStartup.Session.Lifecycle.cs",
                "Sussudio/MainWindow.PreviewStartup.Signals.cs",
                "Sussudio/MainWindow.PreviewStartup.Signals.Composition.cs",
                "Sussudio/MainWindow.PreviewStartup.Signals.State.cs",
                "Sussudio/MainWindow.PreviewStartup.Signals.Events.cs",
                "Sussudio/MainWindow.PreviewStartup.Watchdog.cs",
            }.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}
