static partial class Program
{
    private static string ReadMainWindowShutdownCleanupAdapterSource()
        => ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
}
