using System;
using System.IO;

namespace Sussudio.Controllers;

internal static class PreviewScreenshotPlanPolicy
{
    public const string PreviewRequiredStatusText = "Start preview before capturing a screenshot";

    private const string DefaultOutputFolderName = "Sussudio";
    private const string TimestampFormat = "yyyy-MM-dd_HH-mm-ss";

    public static PreviewScreenshotPlan Create(
        string? configuredOutputPath,
        string picturesFolder,
        DateTime timestamp)
    {
        var outputDirectory = string.IsNullOrWhiteSpace(configuredOutputPath)
            ? Path.Combine(picturesFolder, DefaultOutputFolderName)
            : configuredOutputPath;
        var filePath = Path.Combine(outputDirectory, $"Screenshot_{timestamp.ToString(TimestampFormat)}.png");

        return new PreviewScreenshotPlan(outputDirectory, filePath);
    }

    public static string FormatSavedStatus(string filePath)
        => $"Screenshot saved: {Path.GetFileName(filePath)}";

    public static string FormatFailedStatus(string message)
        => $"Screenshot failed: {message}";

    public static string FormatSavedLog(string filePath, int capturedWidth, int capturedHeight)
        => $"SCREENSHOT_SAVED path={filePath} width={capturedWidth} height={capturedHeight}";

    public static string FormatFailedLog(string message)
        => $"SCREENSHOT_FAILED reason={message}";
}

internal readonly record struct PreviewScreenshotPlan(string OutputDirectory, string FilePath);
