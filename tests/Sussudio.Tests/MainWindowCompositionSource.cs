using System.Linq;

namespace Sussudio.Tests;

internal static class MainWindowCompositionSource
{
    public static string Read()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.xaml.cs",
                "Sussudio/MainWindow.ControllerInitialization.cs",
            }.Select(file => RuntimeContractSource.ReadRepoFile(file).Replace("\r\n", "\n")));
}
