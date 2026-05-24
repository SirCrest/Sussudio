namespace Sussudio.Tests;

internal static class MainWindowCompositionSource
{
    public static string Read()
        => RuntimeContractSource.ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
}
