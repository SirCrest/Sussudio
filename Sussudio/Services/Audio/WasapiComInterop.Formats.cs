using System;
using System.Runtime.InteropServices;

namespace Sussudio.Services.Audio;

internal static partial class WasapiComInterop
{
    internal static IntPtr AllocFloatStereo48kFormat()
    {
        var format = new WAVEFORMATEXTENSIBLE
        {
            Format = new WAVEFORMATEX
            {
                wFormatTag = WAVE_FORMAT_EXTENSIBLE,
                nChannels = 2,
                nSamplesPerSec = 48_000,
                nAvgBytesPerSec = 48_000 * 2 * 4,
                nBlockAlign = 8,
                wBitsPerSample = 32,
                cbSize = (ushort)(Marshal.SizeOf<WAVEFORMATEXTENSIBLE>() - Marshal.SizeOf<WAVEFORMATEX>())
            },
            wValidBitsPerSample = 32,
            dwChannelMask = 0x3,
            SubFormat = KSDATAFORMAT_SUBTYPE_IEEE_FLOAT
        };

        var ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf<WAVEFORMATEXTENSIBLE>());
        Marshal.StructureToPtr(format, ptr, false);
        return ptr;
    }

    internal static WasapiAudioFormat ReadAudioFormat(IntPtr formatPtr)
    {
        if (formatPtr == IntPtr.Zero)
        {
            throw new InvalidOperationException("WASAPI format pointer is null.");
        }

        var wave = Marshal.PtrToStructure<WAVEFORMATEX>(formatPtr);
        var formatTag = wave.wFormatTag;
        var channels = wave.nChannels;
        var sampleRate = wave.nSamplesPerSec;
        var bitsPerSample = wave.wBitsPerSample;
        var blockAlign = wave.nBlockAlign;
        var subFormat = Guid.Empty;

        if (formatTag == WAVE_FORMAT_EXTENSIBLE && wave.cbSize >= 22)
        {
            var extensible = Marshal.PtrToStructure<WAVEFORMATEXTENSIBLE>(formatPtr);
            subFormat = extensible.SubFormat;
            bitsPerSample = extensible.wValidBitsPerSample == 0
                ? extensible.Format.wBitsPerSample
                : extensible.wValidBitsPerSample;
            blockAlign = extensible.Format.nBlockAlign;
        }

        var sampleType = ResolveSampleType(formatTag, subFormat, bitsPerSample);
        if (channels <= 0)
        {
            throw new InvalidOperationException("WASAPI format has invalid channel count.");
        }

        if (sampleRate <= 0)
        {
            throw new InvalidOperationException("WASAPI format has invalid sample rate.");
        }

        if (blockAlign <= 0)
        {
            throw new InvalidOperationException("WASAPI format has invalid block alignment.");
        }

        var bytesPerSample = blockAlign / channels;
        if (bytesPerSample <= 0)
        {
            throw new InvalidOperationException("WASAPI format has invalid bytes-per-sample.");
        }

        return new WasapiAudioFormat(
            (int)sampleRate,
            channels,
            bitsPerSample,
            blockAlign,
            sampleType);
    }

    private static WasapiSampleType ResolveSampleType(int formatTag, Guid subFormat, int bitsPerSample)
    {
        if (formatTag == WAVE_FORMAT_IEEE_FLOAT ||
            (formatTag == WAVE_FORMAT_EXTENSIBLE && subFormat == KSDATAFORMAT_SUBTYPE_IEEE_FLOAT))
        {
            return bitsPerSample switch
            {
                32 => WasapiSampleType.Float32,
                64 => WasapiSampleType.Float64,
                _ => throw new InvalidOperationException($"Unsupported floating-point bit depth: {bitsPerSample}.")
            };
        }

        if (formatTag == WAVE_FORMAT_PCM ||
            (formatTag == WAVE_FORMAT_EXTENSIBLE && subFormat == KSDATAFORMAT_SUBTYPE_PCM))
        {
            return bitsPerSample switch
            {
                16 => WasapiSampleType.Pcm16,
                24 => WasapiSampleType.Pcm24,
                32 => WasapiSampleType.Pcm32,
                _ => throw new InvalidOperationException($"Unsupported PCM bit depth: {bitsPerSample}.")
            };
        }

        throw new InvalidOperationException(
            $"Unsupported WASAPI sample format: formatTag=0x{formatTag:X4}, subFormat={subFormat}, bits={bitsPerSample}.");
    }
}
