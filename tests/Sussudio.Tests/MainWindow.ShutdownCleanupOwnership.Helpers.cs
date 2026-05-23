static partial class Program
{
    private static string ReadMainWindowShutdownCleanupAdapterSource()
        => ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.Composition.cs").Replace("\r\n", "\n");
}
