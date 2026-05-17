using System;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Recording;

internal sealed unsafe partial class LibAvEncoder
{
    private void ConfigureVideoCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options)
    {
        var frameRate = ResolveFrameRate(options);
        codecContext->width = options.Width;
        codecContext->height = options.Height;
        codecContext->time_base = Invert(frameRate);
        codecContext->framerate = frameRate;
        codecContext->pix_fmt = options.IsP010 ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
        codecContext->bit_rate = options.BitRate;
        codecContext->gop_size = options.GopSize > 0 ? options.GopSize : Math.Max(1, (int)Math.Round(options.FrameRate * 2, MidpointRounding.AwayFromZero));
        codecContext->max_b_frames = 0;

        if (!options.HdrEnabled)
        {
            // MJPEG sources decode to full-range YUV (0-255). Without this flag,
            // NVENC treats the data as limited range (16-235), darkening the output.
            if (options.IsFullRangeInput)
            {
                codecContext->color_range = AVColorRange.AVCOL_RANGE_JPEG;
                codecContext->colorspace = AVColorSpace.AVCOL_SPC_BT709;
                codecContext->color_primaries = AVColorPrimaries.AVCOL_PRI_BT709;
                codecContext->color_trc = AVColorTransferCharacteristic.AVCOL_TRC_BT709;
            }

            return;
        }

        codecContext->color_primaries = AVColorPrimaries.AVCOL_PRI_BT2020;
        codecContext->color_trc = AVColorTransferCharacteristic.AVCOL_TRC_SMPTE2084;
        codecContext->colorspace = AVColorSpace.AVCOL_SPC_BT2020_NCL;
        codecContext->color_range = AVColorRange.AVCOL_RANGE_MPEG;
    }

    private void ApplyEncoderPrivateOptions(AVCodecContext* codecContext, LibAvEncoderOptions options)
    {
        if (!options.CodecName.Contains("_nvenc", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var preset = MapNvencPreset(options.NvencPreset);
        ThrowIfError(ffmpeg.av_opt_set(codecContext->priv_data, "preset", preset, 0), "av_opt_set(preset)");

        if (!TryMapSplitEncodeMode(options.SplitEncodeMode, out var splitEncodeMode))
        {
            throw new InvalidOperationException($"Unknown split encode mode '{options.SplitEncodeMode}'.");
        }

        if (SupportsSplitEncodeMode(options.CodecName))
        {
            ThrowIfError(
                ffmpeg.av_opt_set_int(codecContext->priv_data, "split_encode_mode", splitEncodeMode, 0),
                "av_opt_set_int(split_encode_mode)");
        }
        else if (splitEncodeMode is 2 or 3)
        {
            throw new InvalidOperationException(
                $"Split encode mode '{options.SplitEncodeMode}' is not supported by codec '{options.CodecName}'.");
        }

        if (IsMpegTsParameterSetFilterCandidate(options))
        {
            ThrowIfError(
                ffmpeg.av_opt_set_int(codecContext->priv_data, "forced-idr", 1, 0),
                "av_opt_set_int(forced-idr)");
        }
    }

    private void InitializeVideoBitstreamFilterIfNeeded(LibAvEncoderOptions options)
    {
        var filterSpec = GetVideoBitstreamFilterSpec(options);
        if (filterSpec == null)
        {
            return;
        }

        AVBSFContext* bsfCtx = null;
        ThrowIfError(ffmpeg.av_bsf_list_parse_str(filterSpec, &bsfCtx), "av_bsf_list_parse_str");
        if (bsfCtx == null)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_BSF_INIT_FAIL codec='{options.CodecName}' filter='{filterSpec}' msg=Filter chain allocation returned null.");
        }

        _bsfCtx = bsfCtx;
        ThrowIfError(ffmpeg.avcodec_parameters_from_context(_bsfCtx->par_in, _videoCodecCtx), "avcodec_parameters_from_context(bsf)");
        _bsfCtx->time_base_in = _videoCodecCtx->time_base;

        ThrowIfError(ffmpeg.av_bsf_init(_bsfCtx), "av_bsf_init");
        Logger.Log($"LIBAV_ENCODER_BSF_INIT codec='{options.CodecName}' filter='{filterSpec}' hdr={options.HdrEnabled}");
    }
}
