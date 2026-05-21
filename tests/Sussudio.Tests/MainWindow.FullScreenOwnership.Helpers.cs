using System.Linq;

static partial class Program
{
    private static string ReadMainWindowFullScreenAdapterSource()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.FullScreen.cs",
                "Sussudio/MainWindow.FullScreen.Composition.cs",
                "Sussudio/MainWindow.FullScreen.Commands.cs",
                "Sussudio/MainWindow.FullScreen.Input.cs",
                "Sussudio/MainWindow.FullScreen.Overlay.cs",
            }.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}
