using System;
using System.Buffers.Binary;
using System.IO;

namespace Sussudio.Services.Preview;

internal static class PreviewPng16Encoder
{
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] IhdrChunkType = { (byte)'I', (byte)'H', (byte)'D', (byte)'R' };
    private static readonly byte[] IdatChunkType = { (byte)'I', (byte)'D', (byte)'A', (byte)'T' };
    private static readonly byte[] IendChunkType = { (byte)'I', (byte)'E', (byte)'N', (byte)'D' };
    private static readonly uint[] PngCrc32Table = InitPngCrc32Table();

    internal static void WriteCompressedRgb16Png(
        string outputPath,
        int width,
        int height,
        MemoryStream compressedDataStream)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new BinaryWriter(fileStream, System.Text.Encoding.ASCII, leaveOpen: false);

        writer.Write(PngSignature);

        var ihdrData = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdrData.AsSpan(0, 4), checked((uint)width));
        BinaryPrimitives.WriteUInt32BigEndian(ihdrData.AsSpan(4, 4), checked((uint)height));
        ihdrData[8] = 16;
        ihdrData[9] = 2;
        ihdrData[10] = 0;
        ihdrData[11] = 0;
        ihdrData[12] = 0;

        WritePngChunk(writer, IhdrChunkType, ihdrData);
        if (compressedDataStream.TryGetBuffer(out var compressedData))
        {
            WritePngChunk(
                writer,
                IdatChunkType,
                compressedData.Array!,
                compressedData.Offset,
                checked((int)compressedDataStream.Length));
        }
        else
        {
            WritePngChunk(writer, IdatChunkType, compressedDataStream.ToArray());
        }

        WritePngChunk(writer, IendChunkType, Array.Empty<byte>());
    }

    internal static uint[] InitPngCrc32Table()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }

    private static uint UpdatePngCrc32(uint crc, byte[] buffer, int offset, int length)
    {
        for (var i = offset; i < offset + length; i++)
        {
            crc = PngCrc32Table[(crc ^ buffer[i]) & 0xFF] ^ (crc >> 8);
        }

        return crc;
    }

    private static void WritePngChunk(BinaryWriter writer, byte[] chunkType, byte[] data)
    {
        WritePngChunk(writer, chunkType, data, 0, data.Length);
    }

    private static void WritePngChunk(BinaryWriter writer, byte[] chunkType, byte[] data, int dataOffset, int dataLength)
    {
        writer.Write(BinaryPrimitives.ReverseEndianness(checked((uint)dataLength)));
        writer.Write(chunkType);
        if (dataLength > 0)
        {
            writer.Write(data, dataOffset, dataLength);
        }

        var crc = 0xFFFFFFFFu;
        crc = UpdatePngCrc32(crc, chunkType, 0, chunkType.Length);
        if (dataLength > 0)
        {
            crc = UpdatePngCrc32(crc, data, dataOffset, dataLength);
        }

        writer.Write(BinaryPrimitives.ReverseEndianness(crc ^ 0xFFFFFFFFu));
    }
}
