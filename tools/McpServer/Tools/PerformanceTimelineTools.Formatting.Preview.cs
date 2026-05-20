namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private static string FormatJitterDepthCell(TimelineRow row)
        => row.MjpegPreviewJitterEnabled
            ? $"{row.MjpegPreviewJitterQueueDepth}/{row.MjpegPreviewJitterTargetDepth}/{row.MjpegPreviewJitterMaxDepth}"
            : "-";

    private static string FormatD3DP99Bottleneck(TimelineRow row)
    {
        var stages = new[]
        {
            ("input", row.PreviewD3DInputUploadP99Ms),
            ("render", row.PreviewD3DRenderSubmitP99Ms),
            ("present", row.PreviewD3DPresentP99Ms),
            ("wait", row.PreviewD3DFrameLatencyWaitP95Ms)
        };

        var dominant = stages
            .Where(stage => double.IsFinite(stage.Item2) && stage.Item2 > 0)
            .OrderByDescending(stage => stage.Item2)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(dominant.Item1))
        {
            return "none";
        }

        var namedTotal = stages
            .Where(stage => double.IsFinite(stage.Item2) && stage.Item2 > 0)
            .Sum(stage => stage.Item2);
        if (row.PreviewD3DTotalP99Ms > 0 &&
            row.PreviewD3DTotalP99Ms > namedTotal * 1.25)
        {
            return $"other({row.PreviewD3DTotalP99Ms:0.0}ms)";
        }

        return $"{dominant.Item1}({dominant.Item2:0.0}ms)";
    }
}
