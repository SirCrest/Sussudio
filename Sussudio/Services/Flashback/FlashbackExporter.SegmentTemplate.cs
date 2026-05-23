using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using FFmpeg.AutoGen;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    private bool TryInitializeSegmentOutputTemplate(
        IReadOnlyList<FlashbackExportSegment> segments,
        string tmpPath,
        bool fastStart,
        CancellationToken ct,
        out int selectedStreamCount,
        out int selectedVideoStreamIndex,
        out int[] selectedStreamMap,
        out string failureMessage)
    {
        selectedStreamCount = 0;
        selectedVideoStreamIndex = -1;
        selectedStreamMap = Array.Empty<int>();
        failureMessage = "Flashback export failed: no usable segment template was found.";

        for (var templateSegIdx = 0; templateSegIdx < segments.Count; templateSegIdx++)
        {
            ct.ThrowIfCancellationRequested();
            var templatePath = segments[templateSegIdx].Path;
            if (!File.Exists(templatePath))
            {
                continue;
            }

            OpenInput(templatePath);
            try
            {
                ThrowIfError(ffmpeg.avformat_find_stream_info(_activeInputContext, null), "avformat_find_stream_info");
                if (!TryGetInputStreamCount(_activeInputContext, "segment_template", out var candidateStreamCount, out var streamCountFailure))
                {
                    Logger.Log($"FLASHBACK_EXPORT_TEMPLATE_SKIP path='{Path.GetFileName(templatePath)}' reason='invalid_stream_count' detail='{streamCountFailure}'");
                    continue;
                }

                var candidateVideoStreamIndex = FindVideoStreamIndex(_activeInputContext);
                LogInputStreams(_activeInputContext, candidateStreamCount);
                if (candidateVideoStreamIndex < 0)
                {
                    Logger.Log($"FLASHBACK_EXPORT_TEMPLATE_SKIP reason='video_stream_missing' seg={templateSegIdx} trying_next_segment={templateSegIdx < segments.Count - 1}");
                    failureMessage = "Flashback export failed: no usable video stream was found in any segment.";
                    continue;
                }

                var videoStream = _activeInputContext->streams[candidateVideoStreamIndex];
                var videoWidth = videoStream->codecpar->width;
                var videoHeight = videoStream->codecpar->height;
                var videoExtradataSize = videoStream->codecpar->extradata_size;
                var videoHasValidParams = videoWidth > 0 && videoHeight > 0;

                if (!videoHasValidParams)
                {
                    Logger.Log($"FLASHBACK_EXPORT_TEMPLATE_SKIP reason='video_params_incomplete' seg={templateSegIdx} " +
                        $"w={videoWidth} " +
                        $"h={videoHeight} " +
                        $"extradata={videoExtradataSize} " +
                        $"trying_next_segment={templateSegIdx < segments.Count - 1}");
                    failureMessage = "Flashback export failed: no segment had complete video parameters.";
                    continue;
                }

                CreateOutputContext(tmpPath, fastStart);
                selectedStreamMap = CopyTemplateStreams(_activeInputContext, _activeOutputContext, candidateStreamCount);
                Logger.Log($"FLASHBACK_EXPORT_STREAM_MAP video_idx={candidateVideoStreamIndex} map=[{string.Join(",", selectedStreamMap)}]");
                OpenOutputIoAndWriteHeader(_activeOutputContext, tmpPath, fastStart);
                selectedStreamCount = candidateStreamCount;
                selectedVideoStreamIndex = candidateVideoStreamIndex;
                Logger.Log($"FLASHBACK_EXPORT_TEMPLATE_SELECTED seg={templateSegIdx} path='{Path.GetFileName(templatePath)}'");
                return true;
            }
            finally
            {
                CloseActiveInput();
            }
        }

        return false;
    }

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
