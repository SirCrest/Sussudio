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
                "Sussudio/MainWindow.StatsOverlay.Shell.cs",
                "Sussudio/MainWindow.StatsOverlay.Snapshot.cs",
                "Sussudio/MainWindow.StatsOverlay.DockTargets.cs",
                "Sussudio/MainWindow.StatsOverlay.HardwareSources.cs",
                "Sussudio/MainWindow.StatsOverlay.FrameTime.cs",
                "Sussudio/MainWindow.StatsOverlay.Lifecycle.cs",
                "Sussudio/MainWindow.StatsOverlay.Sections.cs",
            }.Select(file => RuntimeContractSource.ReadRepoFile(file).Replace("\r\n", "\n")));
}
