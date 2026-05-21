using System.Linq;

static partial class Program
{
    private static string ReadMainWindowFlashbackAdapterSource()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.Flashback.cs",
                "Sussudio/MainWindow.Flashback.Commands.cs",
                "Sussudio/MainWindow.Flashback.Polling.cs",
                "Sussudio/MainWindow.Flashback.Playhead.cs",
                "Sussudio/MainWindow.Flashback.Scrub.cs",
                "Sussudio/MainWindow.Flashback.Settings.cs",
                "Sussudio/MainWindow.Flashback.Timeline.cs",
                "Sussudio/MainWindow.Flashback.Presentation.cs",
            }.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}
