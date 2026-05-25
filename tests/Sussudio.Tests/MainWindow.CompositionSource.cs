static partial class Program
{
    private static string ReadMainWindowCompositionSource()
        => ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
}

namespace Sussudio.Tests
{
    internal static class MainWindowCompositionSource
    {
        public static string Read()
            => RuntimeContractSource.ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
    }
}
