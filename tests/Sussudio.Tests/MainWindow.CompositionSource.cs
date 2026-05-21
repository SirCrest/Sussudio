static partial class Program
{
    private static string ReadMainWindowCompositionSource()
        => string.Join(
            "\n",
            ReadRepoFile("Sussudio/MainWindow.xaml.cs"),
            ReadRepoFile("Sussudio/MainWindow.ControllerInitialization.cs"))
            .Replace("\r\n", "\n");
}
