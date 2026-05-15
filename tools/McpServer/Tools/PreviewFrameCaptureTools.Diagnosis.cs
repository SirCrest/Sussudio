using System.Text.Json;
using Sussudio.Tools;

namespace McpServer.Tools;

public static partial class PreviewFrameCaptureTools
{
    private static List<string> BuildPreviewFrameCaptureDiagnosis(JsonElement data)
    {
        var averageLuminance = AutomationSnapshotFormatter.GetDouble(data, "AverageLuminance");
        var minLuminance = AutomationSnapshotFormatter.GetDouble(data, "MinLuminance");
        var maxLuminance = AutomationSnapshotFormatter.GetDouble(data, "MaxLuminance");
        var pureBlackPercent = AutomationSnapshotFormatter.GetDouble(data, "PureBlackPercent");
        var letterboxTopRows = AutomationSnapshotFormatter.GetInt(data, "LetterboxTopRows");
        var letterboxBottomRows = AutomationSnapshotFormatter.GetInt(data, "LetterboxBottomRows");
        var pillarboxLeftCols = AutomationSnapshotFormatter.GetInt(data, "PillarboxLeftCols");
        var pillarboxRightCols = AutomationSnapshotFormatter.GetInt(data, "PillarboxRightCols");
        var contentAspectRatio = AutomationSnapshotFormatter.GetDouble(data, "ContentAspectRatio");
        var contentWidth = AutomationSnapshotFormatter.GetInt(data, "ContentWidth");
        var contentHeight = AutomationSnapshotFormatter.GetInt(data, "ContentHeight");

        var diagnosis = new List<string>();
        if (pureBlackPercent > 95.0)
        {
            diagnosis.Add("BLANK FRAME: >95% of pixels are pure black.");
        }
        if (averageLuminance < 30.0)
        {
            diagnosis.Add("VERY DARK: average luminance is below 30.");
        }
        if (averageLuminance > 230.0)
        {
            diagnosis.Add("VERY BRIGHT: average luminance is above 230.");
        }
        if (letterboxTopRows > 0 || letterboxBottomRows > 0)
        {
            diagnosis.Add(
                $"LETTERBOXED: top={letterboxTopRows}, bottom={letterboxBottomRows}, estimated source aspect={FormatAspectRatio(contentAspectRatio)} ({contentWidth}x{contentHeight}).");
        }
        if (pillarboxLeftCols > 0 || pillarboxRightCols > 0)
        {
            diagnosis.Add(
                $"PILLARBOXED: left={pillarboxLeftCols}, right={pillarboxRightCols}, estimated source aspect={FormatAspectRatio(contentAspectRatio)} ({contentWidth}x{contentHeight}).");
        }
        if ((maxLuminance - minLuminance) < 30.0)
        {
            diagnosis.Add("LOW CONTRAST: luminance range is under 30.");
        }

        var near16By9 = IsNear(contentAspectRatio, 16.0 / 9.0, 0.05);
        var near16By10 = IsNear(contentAspectRatio, 16.0 / 10.0, 0.05);
        if (contentAspectRatio > 0 && !near16By9 && !near16By10)
        {
            diagnosis.Add($"ASPECT RATIO ALERT: content aspect {FormatAspectRatio(contentAspectRatio)} is not close to 16:9 or 16:10.");
        }

        return diagnosis;
    }

    private static string FormatAspectRatio(double aspectRatio)
        => AutomationSnapshotFormatter.FormatNumber(aspectRatio, "0.###");

    private static bool IsNear(double value, double target, double tolerance)
    {
        return Math.Abs(value - target) <= tolerance;
    }
}
