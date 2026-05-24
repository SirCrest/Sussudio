using System.Linq;

static partial class Program
{
    private static string ReadMainWindowPropertyChangedPreviewAdapterSource()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.PreviewRenderer.Composition.cs",
            }.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}
