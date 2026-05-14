using System;

namespace Sussudio.Services.Recording;

internal sealed unsafe partial class LibAvEncoder
{
    public void SendAudioSamples(ReadOnlySpan<byte> f32leSamples)
    {
        EnsureOpen();

        if (_audio.CodecCtx == null || _audio.Stream == null || _audio.Frame == null || _audio.SwrCtx == null || f32leSamples.IsEmpty)
        {
            return;
        }

        var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");
        var inputBlockAlign = checked(options.AudioChannels * sizeof(float));
        if (f32leSamples.Length % inputBlockAlign != 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=SendAudioSamples msg=Audio payload length is not aligned actual={f32leSamples.Length} block_align={inputBlockAlign}");
        }

        _audioSamplesReceived += f32leSamples.Length / inputBlockAlign;

        var remaining = f32leSamples;
        var frameBytes = checked(_audio.FrameSize * inputBlockAlign);

        if (_audio.AccumulatorBytes > 0)
        {
            var bytesNeeded = frameBytes - _audio.AccumulatorBytes;
            var copyBytes = Math.Min(bytesNeeded, remaining.Length);
            CopyToAccumulator(ref _audio, remaining[..copyBytes], _audio.AccumulatorBytes);
            _audio.AccumulatorBytes += copyBytes;
            remaining = remaining[copyBytes..];

            if (_audio.AccumulatorBytes == frameBytes)
            {
                EncodeStreamChunk(ref _audio, _audio.ResampleBuffer, _audio.FrameSize,
                    trackDriftCorrection: true, DriftCorrectionThresholdMs);
                _audio.AccumulatorBytes = 0;
            }
        }

        while (remaining.Length >= frameBytes)
        {
            var frameSlice = remaining[..frameBytes];
            fixed (byte* inputPtr = frameSlice)
            {
                EncodeStreamChunk(ref _audio, inputPtr, _audio.FrameSize,
                    trackDriftCorrection: true, DriftCorrectionThresholdMs);
            }

            remaining = remaining[frameBytes..];
        }

        if (!remaining.IsEmpty)
        {
            CopyToAccumulator(ref _audio, remaining, 0);
            _audio.AccumulatorBytes = remaining.Length;
        }
    }

    public void SendMicrophoneSamples(ReadOnlySpan<byte> f32leSamples)
    {
        EnsureOpen();

        if (_mic.CodecCtx == null || _mic.Stream == null || _mic.Frame == null || _mic.SwrCtx == null || f32leSamples.IsEmpty)
        {
            return;
        }

        var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");
        var inputBlockAlign = checked(options.MicrophoneChannels * sizeof(float));
        if (f32leSamples.Length % inputBlockAlign != 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=SendMicrophoneSamples msg=Audio payload length is not aligned actual={f32leSamples.Length} block_align={inputBlockAlign}");
        }

        _micSamplesReceived += f32leSamples.Length / inputBlockAlign;

        var remaining = f32leSamples;
        var frameBytes = checked(_mic.FrameSize * inputBlockAlign);

        if (_mic.AccumulatorBytes > 0)
        {
            var bytesNeeded = frameBytes - _mic.AccumulatorBytes;
            var copyBytes = Math.Min(bytesNeeded, remaining.Length);
            CopyToAccumulator(ref _mic, remaining[..copyBytes], _mic.AccumulatorBytes);
            _mic.AccumulatorBytes += copyBytes;
            remaining = remaining[copyBytes..];

            if (_mic.AccumulatorBytes == frameBytes)
            {
                EncodeStreamChunk(ref _mic, _mic.ResampleBuffer, _mic.FrameSize,
                    trackDriftCorrection: false, MicDriftCorrectionThresholdMs);
                _mic.AccumulatorBytes = 0;
            }
        }

        while (remaining.Length >= frameBytes)
        {
            var frameSlice = remaining[..frameBytes];
            fixed (byte* inputPtr = frameSlice)
            {
                EncodeStreamChunk(ref _mic, inputPtr, _mic.FrameSize,
                    trackDriftCorrection: false, MicDriftCorrectionThresholdMs);
            }

            remaining = remaining[frameBytes..];
        }

        if (!remaining.IsEmpty)
        {
            CopyToAccumulator(ref _mic, remaining, 0);
            _mic.AccumulatorBytes = remaining.Length;
        }
    }
}