using System.Linq;

static partial class Program
{
    private static string ReadMainWindowCaptureSelectionBindingsAdapterSource()
        => string.Join(
            "\n",
            new[]
            {
                "Sussudio/MainWindow.CaptureSelectionBindings.Composition.cs",
                "Sussudio/MainWindow.CaptureSelectionBindings.DeviceSelection.cs",
                "Sussudio/MainWindow.CaptureSelectionBindings.AudioSelection.cs",
                "Sussudio/MainWindow.CaptureSelectionBindings.DeviceAudio.cs",
                "Sussudio/MainWindow.CaptureSelectionBindings.CaptureMode.cs",
                "Sussudio/MainWindow.CaptureSelectionBindings.RecordingSelection.cs",
            }.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
}
