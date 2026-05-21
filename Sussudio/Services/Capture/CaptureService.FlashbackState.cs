using System;
using System.Collections.Generic;
using Sussudio.Models;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

// Flashback public state and segment access surface.
public partial class CaptureService
{
    public bool IsFlashbackActive => _flashbackBackend.Sink != null;
    public TimeSpan FlashbackBufferedDuration => _flashbackBackend.BufferManager?.BufferedDuration ?? TimeSpan.Zero;
    public long FlashbackDiskBytes => _flashbackBackend.BufferManager?.TotalDiskBytes ?? 0;
    public int FlashbackSegmentCount => _flashbackBackend.BufferManager?.SegmentCount ?? 0;
    internal FlashbackPlaybackController? FlashbackPlaybackController => _flashbackBackend.PlaybackController;
    internal FlashbackBufferManager? FlashbackBufferManager => _flashbackBackend.BufferManager;
    public long FlashbackOutputBytes => _flashbackBackend.Sink?.OutputBytes ?? 0;
    public long FlashbackTotalBytesWritten => _flashbackBackend.BufferManager?.TotalBytesWritten ?? 0;
    public string? EncoderCodecName => _flashbackBackend.Sink?.CodecName;
    public uint EncoderTargetBitRate => _flashbackBackend.Sink?.TargetBitRate ?? 0;
    public int EncoderWidth => _flashbackBackend.Sink?.EncoderWidth ?? 0;
    public int EncoderHeight => _flashbackBackend.Sink?.EncoderHeight ?? 0;
    public double EncoderFrameRate => _flashbackBackend.Sink?.EncoderFrameRate ?? 0;
    public FinalizeResult? LastExportResult => _lastExportResult;

    internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()
    {
        return _flashbackBackend.BufferManager?.GetSegmentInfoList()
            ?? Array.Empty<FlashbackSegmentInfo>();
    }
}
