using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Sussudio.Services.Performance;

/// <summary>Writes captured audio as a canonical IEEE-float WAV (format tag 3 with a fact chunk) that any editor opens.</summary>
public static class AudioWavWriter
{
    private const int FormatIeeeFloat = 3;
    private const int BitsPerSample = 32;

    public static void WriteFloat(Stream stream, ReadOnlySpan<float> interleaved, int channels, int sampleRate)
    {
        if (channels < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(channels));
        }

        if (sampleRate < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        var frames = interleaved.Length / channels;
        var dataBytes = (long)frames * channels * sizeof(float);
        if (dataBytes > uint.MaxValue - 64)
        {
            throw new ArgumentException("The audio is too long for a WAV file.", nameof(interleaved));
        }

        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write((uint)(4 + (8 + 16) + (8 + 4) + (8 + dataBytes)));
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));

        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16u);
        writer.Write((ushort)FormatIeeeFloat);
        writer.Write((ushort)channels);
        writer.Write((uint)sampleRate);
        writer.Write((uint)(sampleRate * channels * sizeof(float)));
        writer.Write((ushort)(channels * sizeof(float)));
        writer.Write((ushort)BitsPerSample);

        writer.Write(Encoding.ASCII.GetBytes("fact"));
        writer.Write(4u);
        writer.Write((uint)frames);

        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write((uint)dataBytes);
        writer.Flush();
        stream.Write(MemoryMarshal.AsBytes(interleaved.Slice(0, frames * channels)));
    }
}
