using FFmpeg.AutoGen;

namespace Sussudio.Services.Recording;

internal sealed unsafe partial class LibAvEncoder
{
    private static unsafe void ApplyMp4MuxerOptions(
        string containerFormat,
        bool fragmentedMp4,
        AVDictionary** muxerOptions,
        string operation)
    {
        if (containerFormat != "mp4")
        {
            return;
        }

        var movflags = fragmentedMp4
            ? "frag_keyframe+empty_moov"
            : "+faststart";
        ThrowIfError(ffmpeg.av_dict_set(muxerOptions, "movflags", movflags, 0), $"av_dict_set(movflags,{operation})");

        if (fragmentedMp4)
        {
            // Keep active Flashback playback A/V interleaving tight. Keyframe-only
            // fragmentation can batch about a GOP of video before matching audio.
            ThrowIfError(ffmpeg.av_dict_set(muxerOptions, "frag_duration", "100000", 0), $"av_dict_set(frag_duration,{operation})");
            ThrowIfError(ffmpeg.av_dict_set(muxerOptions, "flush_packets", "1", 0), $"av_dict_set(flush_packets,{operation})");
        }
    }
}
