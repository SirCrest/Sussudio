using System;
using System.Globalization;
using System.IO;

namespace Sussudio;

// Window title presentation: base build stamp plus recording timer suffix.
public sealed partial class MainWindow
{
    private readonly string _windowTitleBase;

    private static string BuildWindowTitleBase()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            return "Simple Sussudio";
        }

        var buildTime = File.GetLastWriteTime(exePath);
        if (buildTime == DateTime.MinValue)
        {
            return "Simple Sussudio";
        }

        return $"Simple Sussudio (build {buildTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)})";
    }

    private void ApplyWindowTitle()
    {
        if (ViewModel.IsRecording)
        {
            Title = $"{_windowTitleBase} - REC {ViewModel.RecordingTime}";
            return;
        }

        Title = _windowTitleBase;
    }
}
