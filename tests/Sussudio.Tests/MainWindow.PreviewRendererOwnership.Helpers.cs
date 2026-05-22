using System.Linq;

static partial class Program
{
    private static string ReadMainWindowPreviewRendererAdapterSource()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.PreviewRenderer.Composition.cs",
                "Sussudio/MainWindow.PreviewRenderer.ResizeTelemetry.cs",
                "Sussudio/MainWindow.PreviewRenderer.Lifecycle.cs",
            }.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}
