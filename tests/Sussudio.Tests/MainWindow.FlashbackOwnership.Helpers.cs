using System.Linq;

static partial class Program
{
    private static string ReadMainWindowFlashbackAdapterSource()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.Flashback.Interactions.cs",
                "Sussudio/MainWindow.Flashback.Presentation.cs",
            }.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}
