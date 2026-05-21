using System.Linq;

static partial class Program
{
    private static string ReadMainWindowShutdownCleanupAdapterSource()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.ShutdownCleanup.cs",
                "Sussudio/MainWindow.ShutdownCleanup.Composition.cs",
                "Sussudio/MainWindow.ShutdownCleanup.Event.cs",
                "Sussudio/MainWindow.ShutdownCleanup.Adapters.cs",
            }.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}
