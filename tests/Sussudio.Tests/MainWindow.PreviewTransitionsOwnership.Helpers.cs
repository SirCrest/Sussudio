using System.Linq;

static partial class Program
{
    private static string ReadMainWindowPreviewTransitionsAdapterSource()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.PreviewTransitions.Composition.cs",
            }.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}
