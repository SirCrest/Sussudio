using System.Linq;

static partial class Program
{
    private static string ReadMainWindowFlashbackAdapterSource()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.Flashback.Commands.cs",
                "Sussudio/MainWindow.Flashback.Polling.cs",
                "Sussudio/MainWindow.Flashback.Playhead.cs",
                "Sussudio/MainWindow.Flashback.Scrub.cs",
                "Sussudio/MainWindow.Flashback.Settings.cs",
                "Sussudio/MainWindow.Flashback.Timeline.cs",
                "Sussudio/MainWindow.Flashback.Presentation.Markers.cs",
                "Sussudio/MainWindow.Flashback.Presentation.Playback.cs",
                "Sussudio/MainWindow.Flashback.Presentation.Export.cs",
            }.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}
