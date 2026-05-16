using System;
using System.IO;
using System.IO.Compression;

namespace Sussudio.Controllers;

internal static class WindowScreenshotImageEncoder
{
    private static readonly uint[] Crc32Table = InitCrc32Table();

    internal static void WriteToStream(Stream output, int width, int height, byte[] bgra, bool png)
    {
        if (png)
        {
            WritePngToStream(output, width, height, bgra);
        }
        else
        {
            WriteBmpToStream(output, width, height, bgra);
        }
    }

    internal static void WritePngToStream(Stream output, int width, int height, byte[] bgra)
    {
        var stride = width * 4;

        output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

        var ihdr = new byte[13];
        WriteBE32(ihdr, 0, width);
        WriteBE32(ihdr, 4, height);
        ihdr[8] = 8;
        ihdr[9] = 6;
        WritePngChunk(output, new byte[] { 73, 72, 68, 82 }, ihdr);

        var raw = new byte[(stride + 1) * height];
        for (var y = 0; y < height; y++)
        {
            var rowDst = y * (stride + 1);
            raw[rowDst] = 0;
            var rowSrc = y * stride;
            for (var x = 0; x < width; x++)
            {
                var s = rowSrc + x * 4;
                var d = rowDst + 1 + x * 4;
                raw[d] = bgra[s + 2];
                raw[d + 1] = bgra[s + 1];
                raw[d + 2] = bgra[s];
                raw[d + 3] = 255;
            }
        }

        byte[] compressed;
        using (var ms = new MemoryStream())
        {
            using (var zlib = new ZLibStream(ms, CompressionLevel.Fastest, leaveOpen: true))
            {
                zlib.Write(raw);
            }
            compressed = ms.ToArray();
        }
        WritePngChunk(output, new byte[] { 73, 68, 65, 84 }, compressed);

        WritePngChunk(output, new byte[] { 73, 69, 78, 68 }, Array.Empty<byte>());
    }

    private static void WritePngChunk(Stream output, byte[] type, byte[] data)
    {
        var buf = new byte[4];
        WriteBE32(buf, 0, data.Length);
        output.Write(buf);
        output.Write(type);
        if (data.Length > 0) output.Write(data);
        var crc = PngCrc32(type, data);
        buf[0] = (byte)(crc >> 24);
        buf[1] = (byte)(crc >> 16);
        buf[2] = (byte)(crc >> 8);
        buf[3] = (byte)crc;
        output.Write(buf);
    }

    private static void WriteBE32(byte[] buf, int off, int val)
    {
        buf[off] = (byte)(val >> 24);
        buf[off + 1] = (byte)(val >> 16);
        buf[off + 2] = (byte)(val >> 8);
        buf[off + 3] = (byte)val;
    }

    private static uint PngCrc32(byte[] type, byte[] data)
    {
        uint c = 0xFFFFFFFF;
        foreach (var b in type) c = (c >> 8) ^ Crc32Table[(c ^ b) & 0xFF];
        foreach (var b in data) c = (c >> 8) ^ Crc32Table[(c ^ b) & 0xFF];
        return c ^ 0xFFFFFFFF;
    }

    internal static uint[] InitCrc32Table()
    {
        var t = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var c = i;
            for (var j = 0; j < 8; j++)
            {
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            }
            t[i] = c;
        }
        return t;
    }

    internal static void WriteBmpToStream(Stream stream, int width, int height, byte[] bgra)
    {
        var stride = width * 4;
        var pixelDataSize = stride * height;
        var fileSize = 14 + 40 + pixelDataSize;

        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)0x4D42);
        writer.Write(fileSize);
        writer.Write(0);
        writer.Write(14 + 40);

        writer.Write(40);
        writer.Write(width);
        writer.Write(-height);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(0);
        writer.Write(pixelDataSize);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        writer.Write(bgra);
    }
}
