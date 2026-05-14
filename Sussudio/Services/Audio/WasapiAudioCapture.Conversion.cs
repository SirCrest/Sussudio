using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Sussudio.Services.Audio;

internal sealed partial class WasapiAudioCapture
{
    private ConvertedAudioPacket ConvertToOutputFormat(IntPtr dataPtr, int inputFrames, bool silent)
    {
        if (inputFrames <= 0)
        {
            return default;
        }

        if (silent)
        {
            var outputFrames = ComputeResampledFrameCount(inputFrames);
            if (outputFrames <= 0)
            {
                return default;
            }

            var outputLength = outputFrames * OutputBlockAlign;
            var buffer = ArrayPool<byte>.Shared.Rent(outputLength);
            buffer.AsSpan(0, outputLength).Clear();
            return new ConvertedAudioPacket(buffer, outputLength, outputFrames, isPooled: true);
        }

        if (_fastPathCopy)
        {
            var outputLength = inputFrames * OutputBlockAlign;
            var buffer = ArrayPool<byte>.Shared.Rent(outputLength);
            Marshal.Copy(dataPtr, buffer, 0, outputLength);
            return new ConvertedAudioPacket(buffer, outputLength, inputFrames, isPooled: true);
        }

        var decodedSampleCount = inputFrames * OutputChannels;
        var decodedStereo = ArrayPool<float>.Shared.Rent(decodedSampleCount);
        try
        {
            DecodeToStereo(dataPtr, inputFrames, _captureFormat, decodedStereo);

            if (_captureFormat.SampleRate == OutputSampleRate)
            {
                var outputLength = decodedSampleCount * BytesPerFloatSample;
                var buffer = ArrayPool<byte>.Shared.Rent(outputLength);
                var outputSamples = MemoryMarshal.Cast<byte, float>(buffer.AsSpan(0, outputLength));
                decodedStereo.AsSpan(0, decodedSampleCount).CopyTo(outputSamples);
                return new ConvertedAudioPacket(buffer, outputLength, inputFrames, isPooled: true);
            }

            var outputFrames = ComputeResampledFrameCount(inputFrames);
            if (outputFrames <= 0)
            {
                return default;
            }

            var outputSampleCount = outputFrames * OutputChannels;
            var resampledStereo = ArrayPool<float>.Shared.Rent(outputSampleCount);
            try
            {
                ResampleStereoLinear(decodedStereo, inputFrames, resampledStereo, outputFrames);
                var outputLength = outputSampleCount * BytesPerFloatSample;
                var buffer = ArrayPool<byte>.Shared.Rent(outputLength);
                var outputSamples = MemoryMarshal.Cast<byte, float>(buffer.AsSpan(0, outputLength));
                resampledStereo.AsSpan(0, outputSampleCount).CopyTo(outputSamples);
                return new ConvertedAudioPacket(buffer, outputLength, outputFrames, isPooled: true);
            }
            finally
            {
                ArrayPool<float>.Shared.Return(resampledStereo);
            }
        }
        finally
        {
            ArrayPool<float>.Shared.Return(decodedStereo);
        }
    }

    private int ComputeResampledFrameCount(int inputFrames)
    {
        if (inputFrames <= 0)
        {
            return 0;
        }

        if (_captureFormat.SampleRate == OutputSampleRate)
        {
            return inputFrames;
        }

        var sampleRate = _captureFormat.SampleRate;
        var scaledFrames = ((long)inputFrames * OutputSampleRate) + _resampleRemainderNumerator;
        if (scaledFrames <= 0)
        {
            return 0;
        }

        var outputFrames = (int)(scaledFrames / sampleRate);
        _resampleRemainderNumerator = scaledFrames % sampleRate;
        return outputFrames;
    }

    private static void ResampleStereoLinear(
        float[] sourceStereo,
        int sourceFrames,
        float[] destinationStereo,
        int outputFrames)
    {
        if (outputFrames <= 0)
        {
            return;
        }

        if (sourceFrames <= 1 || outputFrames == 1)
        {
            var sampleL = sourceStereo.Length >= 1 ? sourceStereo[0] : 0f;
            var sampleR = sourceStereo.Length >= 2 ? sourceStereo[1] : sampleL;
            for (var i = 0; i < outputFrames; i++)
            {
                destinationStereo[i * 2] = sampleL;
                destinationStereo[i * 2 + 1] = sampleR;
            }

            return;
        }

        var step = (sourceFrames - 1d) / (outputFrames - 1d);
        for (var i = 0; i < outputFrames; i++)
        {
            var srcPosition = i * step;
            var srcIndex = (int)srcPosition;
            var srcNext = Math.Min(srcIndex + 1, sourceFrames - 1);
            var frac = (float)(srcPosition - srcIndex);

            var leftA = sourceStereo[srcIndex * 2];
            var rightA = sourceStereo[srcIndex * 2 + 1];
            var leftB = sourceStereo[srcNext * 2];
            var rightB = sourceStereo[srcNext * 2 + 1];

            destinationStereo[i * 2] = leftA + ((leftB - leftA) * frac);
            destinationStereo[i * 2 + 1] = rightA + ((rightB - rightA) * frac);
        }
    }

    private static unsafe void DecodeToStereo(IntPtr dataPtr, int frameCount, WasapiAudioFormat format, float[] destinationStereo)
    {
        var frameStride = format.BlockAlign > 0
            ? format.BlockAlign
            : format.Channels * format.BytesPerSample;

        var origin = (byte*)dataPtr.ToPointer();
        for (var frame = 0; frame < frameCount; frame++)
        {
            var framePtr = origin + (frame * frameStride);
            if (format.Channels <= 1)
            {
                var mono = ReadSample(framePtr, 0, format);
                destinationStereo[frame * 2] = mono;
                destinationStereo[frame * 2 + 1] = mono;
                continue;
            }

            destinationStereo[frame * 2] = ReadSample(framePtr, 0, format);
            destinationStereo[frame * 2 + 1] = ReadSample(framePtr, 1, format);
        }
    }

    private static unsafe float ReadSample(byte* framePtr, int channelIndex, WasapiAudioFormat format)
    {
        var samplePtr = framePtr + (channelIndex * format.BytesPerSample);
        return format.SampleType switch
        {
            WasapiSampleType.Float32 => *(float*)samplePtr,
            WasapiSampleType.Float64 => (float)(*(double*)samplePtr),
            WasapiSampleType.Pcm16 => *(short*)samplePtr / 32768f,
            WasapiSampleType.Pcm24 => ReadPcm24(samplePtr),
            WasapiSampleType.Pcm32 => *(int*)samplePtr / 2147483648f,
            _ => 0f
        };
    }

    private static unsafe float ReadPcm24(byte* samplePtr)
    {
        var value = samplePtr[0] | (samplePtr[1] << 8) | (samplePtr[2] << 16);
        if ((value & 0x00800000) != 0)
        {
            value |= unchecked((int)0xFF000000);
        }

        return (value << 8) / 2147483648f;
    }

    private static void ReturnPacketBuffer(ConvertedAudioPacket packet)
    {
        if (!packet.IsPooled || packet.Buffer == null)
        {
            return;
        }

        ArrayPool<byte>.Shared.Return(packet.Buffer);
    }

    private readonly struct ConvertedAudioPacket
    {
        public ConvertedAudioPacket(byte[] buffer, int length, int frames, bool isPooled)
        {
            Buffer = buffer;
            Length = length;
            Frames = frames;
            IsPooled = isPooled;
        }

        public byte[]? Buffer { get; }

        public int Length { get; }

        public int Frames { get; }

        public bool IsPooled { get; }
    }
}
