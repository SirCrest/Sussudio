using System;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Recording;

internal sealed unsafe partial class LibAvEncoder
{
    private void EncodeStreamChunk(ref AudioStreamState s, byte* inputPtr, int inputSamples,
        bool trackDriftCorrection, double driftCorrectionThresholdMs)
    {
        if (s.CodecCtx == null || s.Stream == null || s.Frame == null || s.SwrCtx == null || inputSamples <= 0)
        {
            return;
        }

        var channelCount = GetStreamChannelCount(ref s);
        if (s.SampleQueueBuffer == null || s.SampleQueueCapacity <= 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=EncodeStreamChunk msg=Audio sample queue is not allocated.");
        }

        if (s.BufferedSamples < 0 || s.BufferedSamples > s.SampleQueueCapacity)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=EncodeStreamChunk msg=Audio queue sample count was out of range buffered={s.BufferedSamples} capacity={s.SampleQueueCapacity}.");
        }

        var availableSamples = s.SampleQueueCapacity - s.BufferedSamples;
        if (availableSamples < inputSamples + MaxDriftCorrectionSamplesPerPass)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=EncodeStreamChunk msg=Audio queue capacity exhausted buffered={s.BufferedSamples} available={availableSamples} requested={inputSamples}.");
        }

        var inputData = stackalloc byte*[1];
        inputData[0] = inputPtr;

        var outputData = stackalloc byte*[channelCount];
        for (var channel = 0; channel < channelCount; channel++)
        {
            outputData[channel] = (byte*)(GetStreamQueuePlane(ref s, channel) + s.BufferedSamples);
        }

        var convertedSamples = ffmpeg.swr_convert(
            s.SwrCtx,
            outputData,
            availableSamples,
            inputData,
            inputSamples);
        if (convertedSamples < 0)
        {
            ThrowIfError(convertedSamples, "swr_convert");
        }

        var queuedSamples = s.BufferedSamples + convertedSamples;
        var queuedStreamSamples = s.NextPts + queuedSamples;
        var correctionSamples = GetDriftCorrectionSamples(
            queuedStreamSamples,
            s.CodecCtx->sample_rate,
            out var correctionVideoFrame,
            out var driftMs,
            driftCorrectionThresholdMs);
        var appliedCorrectionSamples = 0;

        if (correctionSamples < 0)
        {
            var trimmedSamples = Math.Min(-correctionSamples, queuedSamples);
            queuedSamples -= trimmedSamples;
            appliedCorrectionSamples -= trimmedSamples;
        }
        else if (correctionSamples > 0)
        {
            AppendSilentStreamSamples(ref s, queuedSamples, correctionSamples, channelCount);
            queuedSamples += correctionSamples;
            appliedCorrectionSamples += correctionSamples;
        }

        if (trackDriftCorrection && (correctionSamples == 0 || appliedCorrectionSamples == correctionSamples))
        {
            _lastDriftCorrectionVideoFrame = correctionVideoFrame;
        }

        s.BufferedSamples = queuedSamples;
        DrainBufferedFrames(ref s, flushPartialFrame: false);

        if (trackDriftCorrection && appliedCorrectionSamples != 0)
        {
            _driftCorrectionAppliedSamples += appliedCorrectionSamples;
            Logger.Log(
                $"LIBAV_AV_DRIFT_CORRECTION videoFrame={_nextVideoPts} driftMs={driftMs:F1} " +
                $"correctionSamples={appliedCorrectionSamples} totalCorrectionSamples={_driftCorrectionAppliedSamples}");
        }
    }

    private void DrainBufferedFrames(ref AudioStreamState s, bool flushPartialFrame)
    {
        while (s.BufferedSamples >= s.FrameSize || (flushPartialFrame && s.BufferedSamples > 0))
        {
            var sampleCount = s.BufferedSamples >= s.FrameSize
                ? s.FrameSize
                : s.BufferedSamples;
            SendPreparedStreamFrame(ref s, sampleCount);
            RemoveQueuedStreamSamples(ref s, sampleCount);
        }
    }

    private void SendPreparedStreamFrame(ref AudioStreamState s, int sampleCount)
    {
        if (s.CodecCtx == null || s.Frame == null || sampleCount <= 0)
        {
            return;
        }

        ThrowIfError(ffmpeg.av_frame_make_writable(s.Frame), "av_frame_make_writable(audio)");
        CopyQueuedSamplesToStreamFrame(ref s, sampleCount);

        s.Frame->nb_samples = sampleCount;
        var nextPts = s.NextPts;
        s.Frame->pts = nextPts;

        var sendResult = ffmpeg.avcodec_send_frame(s.CodecCtx, s.Frame);
        if (sendResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
        {
            DrainStreamEncoderPackets(ref s);
            sendResult = ffmpeg.avcodec_send_frame(s.CodecCtx, s.Frame);
        }

        ThrowIfError(sendResult, "avcodec_send_frame(audio)");
        s.NextPts = nextPts + sampleCount;
        DrainStreamEncoderPackets(ref s);
    }

    private void CopyQueuedSamplesToStreamFrame(ref AudioStreamState s, int sampleCount)
    {
        if (s.CodecCtx == null || s.Frame == null || s.Frame->extended_data == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyQueuedSamplesToStreamFrame msg=Audio frame storage was not initialized.");
        }

        var bytesPerSample = ffmpeg.av_get_bytes_per_sample(s.CodecCtx->sample_fmt);
        if (bytesPerSample <= 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=CopyQueuedSamplesToStreamFrame msg=Unsupported sample format '{s.CodecCtx->sample_fmt}'.");
        }

        var channelCount = GetStreamChannelCount(ref s);
        if (ffmpeg.av_sample_fmt_is_planar(s.CodecCtx->sample_fmt) == 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyQueuedSamplesToStreamFrame msg=Expected planar audio frame layout.");
        }

        var planeBytes = sampleCount * bytesPerSample;
        for (var channel = 0; channel < channelCount; channel++)
        {
            var source = GetStreamQueuePlane(ref s, channel);
            var destination = (float*)s.Frame->extended_data[channel];
            if (destination == null)
            {
                throw CreateLibAvException(
                    $"LIBAV_ENCODER_ERROR operation=CopyQueuedSamplesToStreamFrame msg=Audio plane pointer was null channel={channel}.");
            }

            Buffer.MemoryCopy(source, destination, planeBytes, planeBytes);
        }
    }

    private void RemoveQueuedStreamSamples(ref AudioStreamState s, int sampleCount)
    {
        if (sampleCount <= 0)
        {
            return;
        }

        if (sampleCount > s.BufferedSamples)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=RemoveQueuedStreamSamples msg=Cannot remove more samples than buffered remove={sampleCount} buffered={s.BufferedSamples}.");
        }

        var remainingSamples = s.BufferedSamples - sampleCount;
        if (remainingSamples > 0)
        {
            var channelCount = GetStreamChannelCount(ref s);
            for (var channel = 0; channel < channelCount; channel++)
            {
                var plane = GetStreamQueuePlane(ref s, channel);
                new ReadOnlySpan<float>(plane + sampleCount, remainingSamples)
                    .CopyTo(new Span<float>(plane, remainingSamples));
            }
        }

        s.BufferedSamples = remainingSamples;
    }

    private void AppendSilentStreamSamples(ref AudioStreamState s, int startSample, int sampleCount, int channelCount)
    {
        if (sampleCount <= 0)
        {
            return;
        }

        for (var channel = 0; channel < channelCount; channel++)
        {
            new Span<float>(GetStreamQueuePlane(ref s, channel) + startSample, sampleCount).Clear();
        }
    }

    private float* GetStreamQueuePlane(ref AudioStreamState s, int channel)
    {
        if (s.SampleQueueBuffer == null || s.SampleQueueCapacity <= 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=GetStreamQueuePlane msg=Audio sample queue was not initialized.");
        }

        return (float*)(s.SampleQueueBuffer + (channel * s.SampleQueueCapacity * sizeof(float)));
    }

    private int GetStreamChannelCount(ref AudioStreamState s)
    {
        var channelCount = (int)(s.CodecCtx != null && s.CodecCtx->ch_layout.nb_channels > 0
            ? s.CodecCtx->ch_layout.nb_channels
            : 0);
        if (channelCount <= 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=GetStreamChannelCount msg=Audio channel count was not available.");
        }

        return channelCount;
    }

}
