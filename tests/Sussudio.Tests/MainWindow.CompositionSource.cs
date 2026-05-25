using System.Linq;

static partial class Program
{
    private static string ReadMainWindowCompositionSource()
        => ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");

    private static string ReadMainWindowCaptureSelectionBindingsAdapterSource()
        => ReadMainWindowAdapterSource("Sussudio/MainWindow.CaptureBindings.cs");

    private static string ReadMainWindowFlashbackAdapterSource()
        => ReadMainWindowAdapterSource("Sussudio/MainWindow.Flashback.Interactions.cs");

    private static string ReadMainWindowPreviewRendererAdapterSource()
        => ReadMainWindowAdapterSource("Sussudio/MainWindow.PreviewRenderer.Composition.cs");

    private static string ReadMainWindowPreviewStartupAdapterSource()
        => ReadMainWindowAdapterSource("Sussudio/MainWindow.PreviewStartup.Session.Composition.cs");

    private static string ReadMainWindowPreviewTransitionsAdapterSource()
        => ReadMainWindowAdapterSource("Sussudio/MainWindow.PreviewTransitions.Composition.cs");

    private static string ReadMainWindowPropertyChangedPreviewAdapterSource()
        => ReadMainWindowAdapterSource("Sussudio/MainWindow.PreviewRenderer.Composition.cs");

    private static string ReadMainWindowShellChromeAdapterSource()
        => ReadMainWindowAdapterSource("Sussudio/MainWindow.ShellChrome.Composition.cs");

    private static string ReadMainWindowAdapterSource(params string[] files)
        => string.Join(
            "\n",
            files.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}

namespace Sussudio.Tests
{
    internal static class MainWindowCompositionSource
    {
        public static string Read()
            => RuntimeContractSource.ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
    }

    internal static class MainWindowStatsOverlaySource
    {
        public static string Read()
            => string.Join(
                "\n",
                new[]
                {
                    "Sussudio/MainWindow.ShellChrome.Composition.cs",
                }.Select(file => RuntimeContractSource.ReadRepoFile(file).Replace("\r\n", "\n")));
    }
}
