using System;

namespace Sussudio.Services.Recording;

internal sealed unsafe partial class LibAvEncoder
{
    private static void ValidateOptions(LibAvEncoderOptions options)
    {
        ValidateRequiredVideoOptions(options);

        if (options.AudioEnabled)
        {
            ValidateAudioOptions(options);
        }

        ValidateHdrOptions(options);
    }

    private static void ValidateRequiredVideoOptions(LibAvEncoderOptions options)
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
    }

    private static void ValidateAudioOptions(LibAvEncoderOptions options)
    {
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
    }

    private static void ValidateHdrOptions(LibAvEncoderOptions options)
    {
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
}
