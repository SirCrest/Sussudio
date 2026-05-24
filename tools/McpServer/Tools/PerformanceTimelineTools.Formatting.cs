using System.Text.Json;
using Sussudio.Tools;

namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private static string GetMessage(JsonElement response)
    {
        return AutomationSnapshotFormatter.Get(response, "Message", "Command failed.");
    }

    private static string FormatOptional(string value)
        => string.IsNullOrWhiteSpace(value) ? "none" : value.Trim();

    private static string CompactCell(string value, int maxLength)
    {
        var compact = FormatOptional(value)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('|', '/');

        return compact.Length <= maxLength ? compact : compact[..Math.Max(0, maxLength - 1)] + "~";
    }

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

    private static string FormatCleanupCell(bool fatalCleanup, bool flashbackCleanup, bool forceRotateRequested, bool forceRotateDraining)
        => fatalCleanup ? "F" : flashbackCleanup ? "B" : forceRotateDraining ? "D" : forceRotateRequested ? "R" : "-";

    private static string FormatFlashbackStageCell(TimelineRow row)
        => $"{row.FlashbackPlaybackSegmentSwitches}/{row.FlashbackPlaybackFmp4Reopens}/{row.FlashbackPlaybackWriteHeadWaits}/{row.FlashbackPlaybackNearLiveSnaps}/{row.FlashbackPlaybackLastWriteHeadWaitGapMs}";

    private static string FormatExportFailureKind(string failureKind)
        => CompactCell(string.IsNullOrWhiteSpace(failureKind) ? "-" : failureKind, 6);

    private static string FormatExportOutPoint(long outPointMs)
        => outPointMs < 0 ? "live" : $"{outPointMs}ms";

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "N/A";
        if (bytes >= 1024L * 1024L * 1024L) return $"{bytes / (1024.0 * 1024.0 * 1024.0):0.##}GB";
        if (bytes >= 1024L * 1024L) return $"{bytes / (1024.0 * 1024.0):0.##}MB";
        if (bytes >= 1024L) return $"{bytes / 1024.0:0.##}KB";
        return $"{bytes}B";
    }

    private static string FormatBytesPerSecond(double bytesPerSecond)
        => double.IsFinite(bytesPerSecond) && bytesPerSecond > 0
            ? $"{FormatBytes((long)bytesPerSecond)}/s"
            : "N/A";
}
