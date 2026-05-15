using System;
using System.Globalization;
using System.IO;

namespace Sussudio.Controllers;

internal sealed class WindowTitleController
{
    private const string DefaultTitle = "Simple Sussudio";

    private readonly string _baseTitle;

    public WindowTitleController()
        : this(BuildWindowTitleBase())
    {
    }

    internal WindowTitleController(string baseTitle)
    {
        _baseTitle = string.IsNullOrWhiteSpace(baseTitle) ? DefaultTitle : baseTitle;
    }

    public string BuildTitle(bool isRecording, string recordingTime)
        => FormatTitle(_baseTitle, isRecording, recordingTime);

    internal static string BuildWindowTitleBase()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            return DefaultTitle;
        }

        return FormatBuildTitle(File.GetLastWriteTime(exePath));
    }

    internal static string FormatBuildTitle(DateTime buildTime)
    {
        if (buildTime == DateTime.MinValue)
        {
            return DefaultTitle;
        }

        return $"{DefaultTitle} (build {buildTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)})";
    }

    internal static string FormatTitle(string baseTitle, bool isRecording, string recordingTime)
        => isRecording ? $"{baseTitle} - REC {recordingTime}" : baseTitle;
}
