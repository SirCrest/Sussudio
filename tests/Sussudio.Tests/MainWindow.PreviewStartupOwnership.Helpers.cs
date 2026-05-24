using System.Linq;

static partial class Program
{
    private static string ReadMainWindowPreviewStartupAdapterSource()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.PreviewStartup.Session.Composition.cs",
            }.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}
