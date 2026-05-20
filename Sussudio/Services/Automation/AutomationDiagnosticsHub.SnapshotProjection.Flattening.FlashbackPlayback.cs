namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
