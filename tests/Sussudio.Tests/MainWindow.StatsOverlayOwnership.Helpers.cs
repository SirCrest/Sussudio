using System.Linq;

namespace Sussudio.Tests;

internal static class MainWindowStatsOverlaySource
{
    public static string Read()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.StatsOverlay.Composition.cs",
            }.Select(file => RuntimeContractSource.ReadRepoFile(file).Replace("\r\n", "\n")));
}
