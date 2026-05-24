static partial class Program
{
    private static string ReadMainWindowCompositionSource()
        => ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
}
