namespace Sussudio.Tools;

internal readonly record struct FlashbackSegmentProbe(
    int SequenceNumber,
    long StartPtsMs,
    long EndPtsMs,
    bool IsActive);

internal readonly record struct FlashbackSegmentPlaybackTarget(
    FlashbackSegmentProbe Segment,
    long ValidStartPtsMs,
    long BoundaryPositionMs,
    long BufferedDurationMs);
