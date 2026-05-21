using System.Linq;

static partial class Program
{
    private static string ReadMainWindowPreviewTransitionsAdapterSource()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.PreviewTransitions.cs",
                "Sussudio/MainWindow.PreviewTransitions.AudioFade.cs",
                "Sussudio/MainWindow.PreviewTransitions.ButtonActions.cs",
                "Sussudio/MainWindow.PreviewTransitions.FadeIn.cs",
                "Sussudio/MainWindow.PreviewTransitions.Overlay.cs",
                "Sussudio/MainWindow.PreviewTransitions.Animation.cs",
                "Sussudio/MainWindow.PreviewTransitions.Reinit.cs",
            }.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}
