using System.Linq;

static partial class Program
{
    private static string ReadMainWindowShellChromeAdapterSource()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.ShellChrome.NativeWindow.cs",
                "Sussudio/MainWindow.ShellChrome.ControlBar.cs",
                "Sussudio/MainWindow.ShellChrome.LaunchEntrance.cs",
                "Sussudio/MainWindow.ShellChrome.LaunchStartup.cs",
                "Sussudio/MainWindow.ShellChrome.SettingsShelf.cs",
                "Sussudio/MainWindow.ShellChrome.ShellElevation.cs",
                "Sussudio/MainWindow.ShellChrome.ShellPropertyChanged.cs",
                "Sussudio/MainWindow.ShellChrome.SplashPhrases.cs",
            }.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}
