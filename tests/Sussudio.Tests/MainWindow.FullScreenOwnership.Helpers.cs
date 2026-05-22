using System.Linq;

static partial class Program
{
    private static string ReadMainWindowFullScreenAdapterSource()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.FullScreen.Composition.cs",
            }.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}
