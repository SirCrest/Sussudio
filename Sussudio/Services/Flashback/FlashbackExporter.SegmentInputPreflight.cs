using System.IO;
using FFmpeg.AutoGen;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    private bool TryOpenSegmentInputForExport(
        FlashbackExportSegment segment,
        string segmentPath,
        int templateStreamCount,
        int[] streamMap,
        ref RequestedSegmentSkipTracker requestedSegmentSkips,
        out int currentStreamCount)
    {
        currentStreamCount = 0;

        if (!File.Exists(segmentPath))
        {
            Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segmentPath)}' reason='not_found'");
            requestedSegmentSkips.Track(segment, "not_found");
            return false;
        }

        OpenInput(segmentPath);
        ThrowIfError(ffmpeg.avformat_find_stream_info(_activeInputContext, null), "avformat_find_stream_info");

        if (!TryGetInputStreamCount(_activeInputContext, "segment_export", out currentStreamCount, out var streamCountFailure))
        {
            Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segmentPath)}' reason='invalid_stream_count' detail='{streamCountFailure}'");
            requestedSegmentSkips.Track(segment, "invalid_stream_count");
            CloseActiveInput();
            return false;
        }

        if (currentStreamCount != templateStreamCount)
        {
            Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segmentPath)}' reason='stream_count_mismatch' expected={templateStreamCount} actual={currentStreamCount}");
            requestedSegmentSkips.Track(segment, "stream_count_mismatch");
            CloseActiveInput();
            return false;
        }

        var streamLayoutMismatch = FindSegmentStreamLayoutMismatch(
            _activeInputContext,
            _activeOutputContext,
            streamMap,
            currentStreamCount);
        if (streamLayoutMismatch != null)
        {
            Logger.Log($"FLASHBACK_EXPORT_SEGMENT_SKIP path='{Path.GetFileName(segmentPath)}' reason='stream_layout_mismatch' detail='{streamLayoutMismatch}'");
            requestedSegmentSkips.Track(segment, "stream_layout_mismatch");
            CloseActiveInput();
            return false;
        }

        return true;
    }
}
