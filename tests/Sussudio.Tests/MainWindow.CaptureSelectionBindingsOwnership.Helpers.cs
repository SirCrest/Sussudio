using System.Linq;

static partial class Program
{
    private static string ReadMainWindowCaptureSelectionBindingsAdapterSource()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.CaptureSelectionBindings.Composition.cs",
            }.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}
