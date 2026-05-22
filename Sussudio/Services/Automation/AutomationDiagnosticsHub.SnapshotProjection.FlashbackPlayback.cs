using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackPlaybackProjection BuildFlashbackPlaybackProjection(CaptureHealthSnapshot health)
    {
        var audioMaster = BuildFlashbackPlaybackAudioMasterProjection(health);
        var timing = BuildFlashbackPlaybackTimingProjection(health);
        var decode = BuildFlashbackPlaybackDecodeProjection(health);
        var commands = BuildFlashbackPlaybackCommandProjection(health);

        return new()
        {
            State = health.FlashbackPlaybackState,
            PositionMs = health.FlashbackPlaybackPositionMs,
            DecoderHwAccel = health.FlashbackDecoderHwAccel,
            FrameCount = health.FlashbackPlaybackFrameCount,
            LateFrames = health.FlashbackPlaybackLateFrames,
            DroppedFrames = health.FlashbackPlaybackDroppedFrames,
            AudioMaster = audioMaster,
            Timing = timing,
            Decode = decode,
            Commands = commands
        };
    }

    private readonly record struct FlashbackPlaybackProjection
    {
        public string State { get; init; }
        public long PositionMs { get; init; }
        public string DecoderHwAccel { get; init; }
        public long FrameCount { get; init; }
        public long LateFrames { get; init; }
        public long DroppedFrames { get; init; }
        public FlashbackPlaybackAudioMasterProjection AudioMaster { get; init; }
        public FlashbackPlaybackTimingProjection Timing { get; init; }
        public FlashbackPlaybackDecodeProjection Decode { get; init; }
        public FlashbackPlaybackCommandProjection Commands { get; init; }
    }

    private static FlashbackPlaybackFlattenedProjection BuildFlashbackPlaybackFlattenedProjection(
        FlashbackPlaybackProjection flashbackPlayback)
        => new()
        {
            State = flashbackPlayback.State,
            PositionMs = flashbackPlayback.PositionMs,
            DecoderHwAccel = flashbackPlayback.DecoderHwAccel,
            FrameCount = flashbackPlayback.FrameCount,
            LateFrames = flashbackPlayback.LateFrames,
            DroppedFrames = flashbackPlayback.DroppedFrames,
            AudioMaster = BuildFlashbackPlaybackAudioMasterFlattenedProjection(flashbackPlayback.AudioMaster),
            Timing = BuildFlashbackPlaybackTimingFlattenedProjection(flashbackPlayback.Timing),
            Decode = BuildFlashbackPlaybackDecodeFlattenedProjection(flashbackPlayback.Decode),
            Commands = BuildFlashbackPlaybackCommandFlattenedProjection(flashbackPlayback.Commands)
        };

    private readonly record struct FlashbackPlaybackFlattenedProjection
    {
        public string State { get; init; }
        public long PositionMs { get; init; }
        public string DecoderHwAccel { get; init; }
        public long FrameCount { get; init; }
        public long LateFrames { get; init; }
        public long DroppedFrames { get; init; }
        public FlashbackPlaybackAudioMasterFlattenedProjection AudioMaster { get; init; }
        public FlashbackPlaybackTimingFlattenedProjection Timing { get; init; }
        public FlashbackPlaybackDecodeFlattenedProjection Decode { get; init; }
        public FlashbackPlaybackCommandFlattenedProjection Commands { get; init; }
    }
}
