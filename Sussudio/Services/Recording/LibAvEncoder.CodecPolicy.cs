using System;
using System.Collections.Generic;
using System.Globalization;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Recording;

internal sealed unsafe partial class LibAvEncoder
{
    private static void ValidateOptions(LibAvEncoderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.OutputPath))
        {
            throw new ArgumentException("OutputPath is required.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.CodecName))
        {
            throw new ArgumentException("CodecName is required.", nameof(options));
        }

        if (options.Width <= 0 || options.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Width and Height must be positive.");
        }

        if (options.FrameRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "FrameRate must be positive.");
        }

        if (options.FrameRateNumerator.HasValue != options.FrameRateDenominator.HasValue)
        {
            throw new ArgumentException("FrameRateNumerator and FrameRateDenominator must be provided together.", nameof(options));
        }

        if (options.FrameRateNumerator is <= 0 || options.FrameRateDenominator is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "FrameRate numerator/denominator must be positive when provided.");
        }

        if (options.BitRate == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "BitRate must be positive.");
        }

        if (!options.AudioEnabled)
        {
            goto ValidateHdrOptions;
        }

        if (options.AudioSampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "AudioSampleRate must be positive.");
        }

        if (options.AudioChannels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "AudioChannels must be positive.");
        }

        if (options.AudioBitRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "AudioBitRate must be positive.");
        }

        if (options.MicrophoneEnabled)
        {
            if (options.MicrophoneSampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "MicrophoneSampleRate must be positive.");
            }

            if (options.MicrophoneChannels <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "MicrophoneChannels must be positive.");
            }

            if (options.MicrophoneBitRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "MicrophoneBitRate must be positive.");
            }
        }

ValidateHdrOptions:
        if (!options.HdrEnabled)
        {
            return;
        }

        if (!options.IsP010)
        {
            throw new InvalidOperationException("HDR10 encoding requires P010 input.");
        }

        if (!options.CodecName.Contains("hevc", StringComparison.OrdinalIgnoreCase) &&
            !options.CodecName.Contains("av1", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"HDR10 encoding requires HEVC or AV1, but codec '{options.CodecName}' was requested.");
        }
    }

    private static string? GetHdrBitstreamFilterName(string codecName)
    {
        if (codecName.Contains("hevc", StringComparison.OrdinalIgnoreCase))
        {
            return "hevc_metadata";
        }

        if (codecName.Contains("av1", StringComparison.OrdinalIgnoreCase))
        {
            return "av1_metadata";
        }

        return null;
    }

    private static string? GetVideoBitstreamFilterSpec(LibAvEncoderOptions options)
    {
        var filters = new List<string>();
        if (options.HdrEnabled)
        {
            var hdrFilter = GetHdrBitstreamFilterSpec(options.CodecName);
            if (hdrFilter != null)
            {
                filters.Add(hdrFilter);
            }
        }

        var parameterSetFilter = GetMpegTsParameterSetBitstreamFilterName(options);
        if (parameterSetFilter != null)
        {
            filters.Add(parameterSetFilter);
        }

        return filters.Count == 0
            ? null
            : string.Join(",", filters);
    }

    private static string? GetHdrBitstreamFilterSpec(string codecName)
    {
        if (codecName.Contains("hevc", StringComparison.OrdinalIgnoreCase))
        {
            return "hevc_metadata=colour_primaries=9:transfer_characteristics=16:matrix_coefficients=9";
        }

        if (codecName.Contains("av1", StringComparison.OrdinalIgnoreCase))
        {
            return "av1_metadata=color_primaries=9:transfer_characteristics=16:matrix_coefficients=9";
        }

        return null;
    }

    private static string? GetMpegTsParameterSetBitstreamFilterName(LibAvEncoderOptions options)
        => IsMpegTsParameterSetFilterCandidate(options) ? "dump_extra" : null;

    private static bool IsMpegTsParameterSetFilterCandidate(LibAvEncoderOptions options)
        => string.Equals(options.ContainerFormat, "mpegts", StringComparison.OrdinalIgnoreCase) &&
           (options.CodecName.Contains("h264", StringComparison.OrdinalIgnoreCase) ||
            options.CodecName.Contains("hevc", StringComparison.OrdinalIgnoreCase));

    private static int GetExpectedFrameSizeBytes(int width, int height, bool isP010)
        => isP010 ? width * height * 3 : (width * height * 3) / 2;

    private static string MapNvencPreset(string? preset)
    {
        if (string.IsNullOrWhiteSpace(preset) || preset.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            return "p4";
        }

        if (preset.Equals("Fast", StringComparison.OrdinalIgnoreCase))
        {
            return "p1";
        }

        if (preset.Equals("Slow", StringComparison.OrdinalIgnoreCase))
        {
            return "p7";
        }

        return preset.ToLowerInvariant();
    }

    private static bool SupportsSplitEncodeMode(string codecName)
        => codecName.Contains("hevc", StringComparison.OrdinalIgnoreCase) ||
           codecName.Contains("265", StringComparison.OrdinalIgnoreCase) ||
           codecName.Contains("av1", StringComparison.OrdinalIgnoreCase);

    private static bool TryMapSplitEncodeMode(string? splitEncodeMode, out long value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(splitEncodeMode) ||
            splitEncodeMode.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (splitEncodeMode.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
        {
            value = 15;
            return true;
        }

        if (splitEncodeMode.Equals("2-way", StringComparison.OrdinalIgnoreCase) ||
            splitEncodeMode.Equals("2", StringComparison.OrdinalIgnoreCase))
        {
            value = 2;
            return true;
        }

        if (splitEncodeMode.Equals("3-way", StringComparison.OrdinalIgnoreCase) ||
            splitEncodeMode.Equals("3", StringComparison.OrdinalIgnoreCase))
        {
            value = 3;
            return true;
        }

        return false;
    }

    private static bool IsSampleFormatSupported(AVCodec* codec, AVSampleFormat sampleFormat)
    {
        void* supportedFormats = null;
        var supportedFormatCount = 0;
        var result = ffmpeg.avcodec_get_supported_config(
            null,
            codec,
            AVCodecConfig.AV_CODEC_CONFIG_SAMPLE_FORMAT,
            0,
            &supportedFormats,
            &supportedFormatCount);
        if (result < 0 || supportedFormats == null || supportedFormatCount <= 0)
        {
            return true;
        }

        var formats = (AVSampleFormat*)supportedFormats;
        for (var i = 0; i < supportedFormatCount; i++)
        {
            if (formats[i] == sampleFormat)
            {
                return true;
            }
        }

        return false;
    }

    private static AVRational ToAvRational(double value)
    {
        var rational = ffmpeg.av_d2q(value, 1_000_000);
        if (rational.num == 0 || rational.den == 0)
        {
            throw CreateLibAvException($"LIBAV_ENCODER_ERROR operation=ToAvRational msg=Unable to convert frame rate value={value.ToString("0.###", CultureInfo.InvariantCulture)}");
        }

        return rational;
    }

    private static AVRational ResolveFrameRate(LibAvEncoderOptions options)
    {
        if (options.FrameRateNumerator.HasValue && options.FrameRateDenominator.HasValue)
        {
            return new AVRational
            {
                num = options.FrameRateNumerator.Value,
                den = options.FrameRateDenominator.Value
            };
        }

        return ToAvRational(options.FrameRate);
    }

    private static AVRational Invert(AVRational value)
    {
        if (value.num == 0)
        {
            return new AVRational { num = 0, den = 1 };
        }

        return new AVRational
        {
            num = value.den,
            den = value.num
        };
    }
}
