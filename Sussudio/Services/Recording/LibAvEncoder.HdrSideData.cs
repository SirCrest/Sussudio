using System;
using System.Globalization;
using System.Text.RegularExpressions;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Recording;

internal sealed unsafe partial class LibAvEncoder
{
    private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)
    {
        var attached = false;

        if (!string.IsNullOrWhiteSpace(options.HdrMasterDisplayMetadata))
        {
            var masteringMetadata = ffmpeg.av_mastering_display_metadata_create_side_data(_videoFrame);
            if (masteringMetadata == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_mastering_display_metadata_create_side_data msg=Allocation returned null.");
            }

            HdrMasterDisplayMetadata.Apply(masteringMetadata, options.HdrMasterDisplayMetadata);
            attached = true;
        }

        if (options.HdrMaxCll > 0 && options.HdrMaxFall > 0)
        {
            var lightMetadata = ffmpeg.av_content_light_metadata_create_side_data(_videoFrame);
            if (lightMetadata == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_content_light_metadata_create_side_data msg=Allocation returned null.");
            }

            lightMetadata->MaxCLL = (uint)options.HdrMaxCll;
            lightMetadata->MaxFALL = (uint)options.HdrMaxFall;
            attached = true;
        }

        return attached;
    }

    private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)
    {
        var attached = false;

        if (!string.IsNullOrWhiteSpace(options.HdrMasterDisplayMetadata))
        {
            var masteringMetadata = ffmpeg.av_mastering_display_metadata_create_side_data(_hwFrame);
            if (masteringMetadata == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_mastering_display_metadata_create_side_data(hw) msg=Allocation returned null.");
            }

            HdrMasterDisplayMetadata.Apply(masteringMetadata, options.HdrMasterDisplayMetadata);
            attached = true;
        }

        if (options.HdrMaxCll > 0 && options.HdrMaxFall > 0)
        {
            var lightMetadata = ffmpeg.av_content_light_metadata_create_side_data(_hwFrame);
            if (lightMetadata == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_content_light_metadata_create_side_data(hw) msg=Allocation returned null.");
            }

            lightMetadata->MaxCLL = (uint)options.HdrMaxCll;
            lightMetadata->MaxFALL = (uint)options.HdrMaxFall;
            attached = true;
        }

        return attached;
    }
}

/// <summary>
/// Parses and applies HDR mastering display metadata strings to libav structs.
/// Format: G(gx,gy)B(bx,by)R(rx,ry)WP(wx,wy)L(maxL,minL)
/// Values are 50 000-denominator chromaticity and 10 000-denominator luminance.
/// </summary>
internal static unsafe class HdrMasterDisplayMetadata
{
    internal static readonly Regex Regex = new(
        @"^G\((\d+),(\d+)\)B\((\d+),(\d+)\)R\((\d+),(\d+)\)WP\((\d+),(\d+)\)L\((\d+),(\d+)\)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static void Apply(AVMasteringDisplayMetadata* metadata, string masterDisplayMetadata)
    {
        var match = Regex.Match(masterDisplayMetadata);
        if (!match.Success)
        {
            var msg =
                $"LIBAV_ENCODER_ERROR operation=ApplyMasterDisplayMetadata msg=Invalid mastering metadata format value='{masterDisplayMetadata}'.";
            Logger.Log(msg);
            throw new InvalidOperationException(msg);
        }

        var primaries = metadata->display_primaries;
        var red = primaries[0];
        red[0] = ToChromaticityRational(match.Groups[5].Value);
        red[1] = ToChromaticityRational(match.Groups[6].Value);
        primaries[0] = red;

        var green = primaries[1];
        green[0] = ToChromaticityRational(match.Groups[1].Value);
        green[1] = ToChromaticityRational(match.Groups[2].Value);
        primaries[1] = green;

        var blue = primaries[2];
        blue[0] = ToChromaticityRational(match.Groups[3].Value);
        blue[1] = ToChromaticityRational(match.Groups[4].Value);
        primaries[2] = blue;
        metadata->display_primaries = primaries;

        var whitePoint = metadata->white_point;
        whitePoint[0] = ToChromaticityRational(match.Groups[7].Value);
        whitePoint[1] = ToChromaticityRational(match.Groups[8].Value);
        metadata->white_point = whitePoint;

        metadata->max_luminance = ToLuminanceRational(match.Groups[9].Value);
        metadata->min_luminance = ToLuminanceRational(match.Groups[10].Value);
        metadata->has_primaries = 1;
        metadata->has_luminance = 1;
    }

    private static AVRational ToChromaticityRational(string value)
        => new()
        {
            num = int.Parse(value, CultureInfo.InvariantCulture),
            den = 50_000
        };

    private static AVRational ToLuminanceRational(string value)
        => new()
        {
            num = int.Parse(value, CultureInfo.InvariantCulture),
            den = 10_000
        };
}
