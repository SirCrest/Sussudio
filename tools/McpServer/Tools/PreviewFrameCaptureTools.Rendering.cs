using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace McpServer.Tools;

public static partial class PreviewFrameCaptureTools
{
    private static string BuildPreviewFrameCaptureText(JsonElement data)
    {
        var builder = new StringBuilder();
        builder.AppendLine("== Preview Frame Capture ==");
        builder.AppendLine($"File: {AutomationSnapshotFormatter.Get(data, "FilePath")}");
        builder.AppendLine($"Resolution: {AutomationSnapshotFormatter.Get(data, "CapturedWidth")} x {AutomationSnapshotFormatter.Get(data, "CapturedHeight")}");
        builder.AppendLine($"Renderer: {AutomationSnapshotFormatter.Get(data, "RendererMode")}");
        builder.AppendLine();
        builder.AppendLine("== Pixel Summary ==");
        builder.AppendLine($"Average RGB: R={AutomationSnapshotFormatter.Get(data, "AverageR")} G={AutomationSnapshotFormatter.Get(data, "AverageG")} B={AutomationSnapshotFormatter.Get(data, "AverageB")}");
        builder.AppendLine($"Luminance: avg={AutomationSnapshotFormatter.Get(data, "AverageLuminance")} min={AutomationSnapshotFormatter.Get(data, "MinLuminance")} max={AutomationSnapshotFormatter.Get(data, "MaxLuminance")}");
        builder.AppendLine($"Near Black (<16): {AutomationSnapshotFormatter.Get(data, "NearBlackPercent")}%");
        builder.AppendLine($"Near White (>240): {AutomationSnapshotFormatter.Get(data, "NearWhitePercent")}%");
        builder.AppendLine($"Pure Black: {AutomationSnapshotFormatter.Get(data, "PureBlackPercent")}%");
        builder.AppendLine();
        builder.AppendLine("== Framing ==");
        builder.AppendLine($"Letterbox: top={AutomationSnapshotFormatter.Get(data, "LetterboxTopRows")} bottom={AutomationSnapshotFormatter.Get(data, "LetterboxBottomRows")} rows");
        builder.AppendLine($"Pillarbox: left={AutomationSnapshotFormatter.Get(data, "PillarboxLeftCols")} right={AutomationSnapshotFormatter.Get(data, "PillarboxRightCols")} cols");
        builder.AppendLine($"Content Area: {AutomationSnapshotFormatter.Get(data, "ContentWidth")} x {AutomationSnapshotFormatter.Get(data, "ContentHeight")}");
        builder.AppendLine($"Content Aspect Ratio: {AutomationSnapshotFormatter.Get(data, "ContentAspectRatio")}");
        builder.AppendLine($"Total Pixels: {AutomationSnapshotFormatter.Get(data, "TotalPixels")}");

        AppendLuminanceHistogram(builder, data);
        AppendPreviewFrameCaptureDiagnosis(builder, data);

        return builder.ToString().TrimEnd();
    }

    private static void AppendPreviewFrameCaptureDiagnosis(StringBuilder builder, JsonElement data)
    {
        var diagnosis = BuildPreviewFrameCaptureDiagnosis(data);

        builder.AppendLine();
        builder.AppendLine("== Diagnosis ==");
        if (diagnosis.Count == 0)
        {
            builder.AppendLine("No obvious anomalies detected.");
        }
        else
        {
            foreach (var finding in diagnosis)
            {
                builder.AppendLine($"- {finding}");
            }
        }
    }
}
