using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FFmpeg.AutoGen;
using Sussudio.Models;
using Sussudio.Services.Contracts;

namespace Sussudio.Services.Recording;

internal readonly record struct RecordingStructureVerificationResult(
    bool Succeeded,
    long OutputBytes,
    string FailureCode,
    string Detail,
    IReadOnlyList<string> RequestedTracks,
    IReadOnlyList<string> ObservedTracks)
{
    public static RecordingStructureVerificationResult Success(long outputBytes) =>
        new(true, outputBytes, string.Empty, "verified", Array.Empty<string>(), Array.Empty<string>());

    public static RecordingStructureVerificationResult Failure(
        string failureCode,
        string detail,
        long outputBytes = 0) =>
        new(false, outputBytes, failureCode, detail, Array.Empty<string>(), Array.Empty<string>());

    public RecordingStructureVerificationResult WithTrackEvidence(
        IEnumerable<string> requestedTracks,
        IEnumerable<string> observedTracks) =>
        this with
        {
            RequestedTracks = requestedTracks.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            ObservedTracks = observedTracks.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        };
}

/// <summary>
/// Minimal release-boundary verification performed by the same process after
/// the encoder has flushed, written its trailer, and closed output IO. This is
/// deliberately independent of the optional ffprobe diagnostics path: a file
/// is not reported as saved unless libav can reopen it and observe the expected
/// stream topology, codec metadata, and at least one packet per required stream.
/// </summary>
internal sealed unsafe class InProcessRecordingStructureVerifier
{
    private const double MaxVideoDurationShortfallSeconds = 2.0;
    private const int MaxSupportedStreams = 32;
    private const int MaxPacketsToProbe = 16_384;
    private const double MaxPlausibleRecordingDurationSeconds = 365 * 24 * 60 * 60;
    private const double MaxRequestedAudioDurationSkewSeconds = 0.5;

    public RecordingStructureVerificationResult Verify(
        RecordingContext context,
        TimeSpan? expectedRecordingDuration = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        var outputPath = context.FinalOutputPath;
        var requestedTracks = BuildRequestedTracks(context);
        var observedTracks = new List<string>();

        RecordingStructureVerificationResult Failure(
            string failureCode,
            string detail,
            long outputBytes = 0) =>
            RecordingStructureVerificationResult
                .Failure(failureCode, detail, outputBytes)
                .WithTrackEvidence(requestedTracks, observedTracks);

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return Failure(
                "recording-output-path-empty",
                "output path is empty");
        }

        long outputBytes;
        try
        {
            if (!File.Exists(outputPath))
            {
                return Failure(
                    "recording-output-missing",
                    "output file is missing");
            }

            outputBytes = new FileInfo(outputPath).Length;
            if (outputBytes <= 0)
            {
                return Failure(
                    "recording-output-empty",
                    "output file is empty");
            }
        }
        catch (Exception ex)
        {
            return Failure(
                "recording-output-stat-failed",
                $"output file length is unavailable: {ex.Message}");
        }

        AVFormatContext* formatContext = null;
        AVPacket* packet = null;
        try
        {
            LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);
            var openResult = ffmpeg.avformat_open_input(&formatContext, outputPath, null, null);
            if (openResult < 0 || formatContext == null)
            {
                return Failure(
                    "recording-reopen-failed",
                    $"avformat_open_input failed with libav error {openResult}",
                    outputBytes);
            }

            var infoResult = ffmpeg.avformat_find_stream_info(formatContext, null);
            if (infoResult < 0)
            {
                return Failure(
                    "recording-stream-info-failed",
                    $"avformat_find_stream_info failed with libav error {infoResult}",
                    outputBytes);
            }

            var nativeStreamCount = formatContext->nb_streams;
            if (nativeStreamCount == 0 || nativeStreamCount > MaxSupportedStreams)
            {
                return Failure(
                    "recording-stream-count-invalid",
                    $"stream count {nativeStreamCount} is outside 1..{MaxSupportedStreams}",
                    outputBytes);
            }

            var expectedAudioStreams = (context.AudioEnabled ? 1 : 0) +
                                       (context.MicrophoneEnabled ? 1 : 0);
            var videoStreamIndexes = new List<int>(1);
            var audioStreamIndexes = new List<int>(expectedAudioStreams);
            var audioStreamDurations = new List<double>(expectedAudioStreams);
            double? videoStreamDuration = null;
            var streamCount = (int)nativeStreamCount;

            for (var streamIndex = 0; streamIndex < streamCount; streamIndex++)
            {
                var stream = formatContext->streams[streamIndex];
                if (stream == null || stream->codecpar == null)
                {
                    return Failure(
                        "recording-stream-metadata-missing",
                        $"stream {streamIndex} has no codec parameters",
                        outputBytes);
                }

                if (stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    videoStreamIndexes.Add(streamIndex);
                    observedTracks.Add(videoStreamIndexes.Count == 1 ? "video" : $"video:{videoStreamIndexes.Count}");
                    var duration = ResolveStreamDurationSeconds(stream);
                    if (!IsPlausibleDuration(duration))
                    {
                        return Failure(
                            "recording-video-duration-invalid",
                            $"video stream {streamIndex} duration is missing or implausible",
                            outputBytes);
                    }
                    videoStreamDuration = duration;
                    var expectedCodec = ResolveExpectedVideoCodec(
                        context.FileNameFormatOverride ?? context.Settings.Format);
                    if (stream->codecpar->codec_id != expectedCodec)
                    {
                        return Failure(
                            "recording-video-codec-mismatch",
                            $"video codec is {stream->codecpar->codec_id}; expected {expectedCodec}",
                            outputBytes);
                    }

                    if (stream->codecpar->width != checked((int)context.EffectiveWidth) ||
                        stream->codecpar->height != checked((int)context.EffectiveHeight))
                    {
                        return Failure(
                            "recording-video-dimensions-mismatch",
                            $"video dimensions are {stream->codecpar->width}x{stream->codecpar->height}; " +
                            $"expected {context.EffectiveWidth}x{context.EffectiveHeight}",
                            outputBytes);
                    }

                    if (context.HdrPipelineActive &&
                        (stream->codecpar->color_primaries != AVColorPrimaries.AVCOL_PRI_BT2020 ||
                         stream->codecpar->color_trc != AVColorTransferCharacteristic.AVCOL_TRC_SMPTE2084 ||
                         stream->codecpar->color_space != AVColorSpace.AVCOL_SPC_BT2020_NCL))
                    {
                        return Failure(
                            "recording-hdr-metadata-mismatch",
                            "HDR output is missing BT.2020/PQ/non-constant-luminance stream metadata",
                            outputBytes);
                    }
                }
                else if (stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    audioStreamIndexes.Add(streamIndex);
                    observedTracks.Add(ResolveObservedAudioTrackName(context, audioStreamIndexes.Count));
                    var duration = ResolveStreamDurationSeconds(stream);
                    if (!IsPlausibleDuration(duration))
                    {
                        return Failure(
                            "recording-audio-duration-invalid",
                            $"audio stream {streamIndex} duration is missing or implausible",
                            outputBytes);
                    }
                    audioStreamDurations.Add(duration!.Value);
                    if (stream->codecpar->codec_id == AVCodecID.AV_CODEC_ID_NONE ||
                        stream->codecpar->sample_rate <= 0 ||
                        stream->codecpar->ch_layout.nb_channels <= 0)
                    {
                        return Failure(
                            "recording-audio-metadata-invalid",
                            $"audio stream {streamIndex} has invalid codec/rate/channel metadata",
                            outputBytes);
                    }
                }
            }

            if (videoStreamIndexes.Count != 1 || audioStreamIndexes.Count != expectedAudioStreams)
            {
                return Failure(
                    "recording-stream-topology-mismatch",
                    $"observed video={videoStreamIndexes.Count}, audio={audioStreamIndexes.Count}; " +
                    $"expected video=1, audio={expectedAudioStreams}",
                    outputBytes);
            }

            if (videoStreamDuration is { } videoDuration)
            {
                if (expectedRecordingDuration is { } expectedDuration && expectedDuration > TimeSpan.FromMilliseconds(250))
                {
                    var expectedSeconds = expectedDuration.TotalSeconds;
                    var allowedShortfallSeconds = Math.Min(
                        MaxVideoDurationShortfallSeconds,
                        Math.Max(0.25, expectedSeconds * 0.05));
                    if (videoDuration < expectedSeconds - allowedShortfallSeconds)
                    {
                        return Failure(
                            "recording-video-duration-short",
                            $"video duration {videoDuration:0.###}s is shorter than the {expectedSeconds:0.###}s recording interval",
                            outputBytes);
                    }
                }

                for (var audioIndex = 0; audioIndex < audioStreamDurations.Count; audioIndex++)
                {
                    var skewSeconds = Math.Abs(videoDuration - audioStreamDurations[audioIndex]);
                    if (skewSeconds > MaxRequestedAudioDurationSkewSeconds)
                    {
                        return Failure(
                            "recording-audio-duration-mismatch",
                            $"audio stream {audioStreamIndexes[audioIndex]} differs from video duration by {skewSeconds:0.###} seconds",
                            outputBytes);
                    }
                }
            }

            packet = ffmpeg.av_packet_alloc();
            if (packet == null)
            {
                return Failure(
                    "recording-packet-allocation-failed",
                    "libav could not allocate a verification packet",
                    outputBytes);
            }

            var requiredStreams = new HashSet<int>(videoStreamIndexes);
            requiredStreams.UnionWith(audioStreamIndexes);
            for (var packetsRead = 0; packetsRead < MaxPacketsToProbe && requiredStreams.Count > 0; packetsRead++)
            {
                var readResult = ffmpeg.av_read_frame(formatContext, packet);
                if (readResult < 0)
                {
                    break;
                }

                requiredStreams.Remove(packet->stream_index);
                ffmpeg.av_packet_unref(packet);
            }

            if (requiredStreams.Count > 0)
            {
                return Failure(
                    "recording-required-stream-has-no-packets",
                    $"required stream(s) {string.Join(',', requiredStreams)} had no readable packet",
                    outputBytes);
            }

            return RecordingStructureVerificationResult
                .Success(outputBytes)
                .WithTrackEvidence(requestedTracks, observedTracks);
        }
        catch (Exception ex)
        {
            return Failure(
                "recording-structure-verification-exception",
                ex.Message,
                outputBytes);
        }
        finally
        {
            if (packet != null)
            {
                ffmpeg.av_packet_free(&packet);
            }

            if (formatContext != null)
            {
                ffmpeg.avformat_close_input(&formatContext);
            }
        }
    }

    private static AVCodecID ResolveExpectedVideoCodec(RecordingFormat format) => format switch
    {
        RecordingFormat.H264Mp4 => AVCodecID.AV_CODEC_ID_H264,
        RecordingFormat.HevcMp4 => AVCodecID.AV_CODEC_ID_HEVC,
        RecordingFormat.Av1Mp4 => AVCodecID.AV_CODEC_ID_AV1,
        _ => AVCodecID.AV_CODEC_ID_NONE
    };

    private static double? ResolveStreamDurationSeconds(AVStream* stream)
    {
        if (stream == null || stream->duration <= 0 || stream->time_base.num <= 0 || stream->time_base.den <= 0)
        {
            return null;
        }

        return stream->duration * (double)stream->time_base.num / stream->time_base.den;
    }

    private static bool IsPlausibleDuration(double? durationSeconds)
        => durationSeconds is { } duration &&
           double.IsFinite(duration) &&
           duration > 0 &&
           duration <= MaxPlausibleRecordingDurationSeconds;

    private static IReadOnlyList<string> BuildRequestedTracks(RecordingContext context)
    {
        var tracks = new List<string>(3) { "video" };
        if (context.AudioEnabled)
        {
            tracks.Add("device_audio");
        }
        if (context.MicrophoneEnabled)
        {
            tracks.Add("microphone");
        }
        return tracks;
    }

    private static string ResolveObservedAudioTrackName(RecordingContext context, int oneBasedAudioIndex)
    {
        if (oneBasedAudioIndex == 1)
        {
            return context.AudioEnabled ? "device_audio" : "microphone";
        }
        if (oneBasedAudioIndex == 2 && context.AudioEnabled && context.MicrophoneEnabled)
        {
            return "microphone";
        }
        return $"audio:{oneBasedAudioIndex}";
    }
}
