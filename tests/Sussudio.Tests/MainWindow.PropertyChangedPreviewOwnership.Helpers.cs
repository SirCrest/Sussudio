using System.Linq;

static partial class Program
{
    private static string ReadMainWindowPropertyChangedPreviewAdapterSource()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.PropertyChangedPreview.cs",
                "Sussudio/MainWindow.PropertyChangedPreview.Button.cs",
                "Sussudio/MainWindow.PropertyChangedPreview.Lifecycle.cs",
            }.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}
