using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using WinRT.Interop;
using Sussudio.Services.Audio;
using Sussudio.Services.Automation;
using Sussudio.Services.Capture;
using Sussudio.Services.Configuration;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio;

public sealed partial class MainWindow
{
    private WindowScreenshotResult CaptureWindowScreenshotCore(string outputPath)
    {
        if (_hwnd == IntPtr.Zero)
        {
            return new WindowScreenshotResult { Succeeded = false, Message = "Window handle not available." };
        }

        if (!GetWindowRect(_hwnd, out var rect))
        {
            return new WindowScreenshotResult { Succeeded = false, Message = "Failed to get window rect." };
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            return new WindowScreenshotResult { Succeeded = false, Message = $"Invalid window size: {width}x{height}" };
        }

        var hdcWindow = GetDC(_hwnd);
        var hdcMemDC = CreateCompatibleDC(hdcWindow);
        var hBitmap = CreateCompatibleBitmap(hdcWindow, width, height);
        var hOld = SelectObject(hdcMemDC, hBitmap);

        try
        {
            // PW_RENDERFULLCONTENT captures DWM-composited content including D3D swap chains
            if (!PrintWindow(_hwnd, hdcMemDC, PW_RENDERFULLCONTENT))
            {
                return new WindowScreenshotResult { Succeeded = false, Message = "PrintWindow failed." };
            }

            SelectObject(hdcMemDC, hOld);

            // Write as PNG using System.Drawing interop
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            SaveHBitmapAsImage(hBitmap, width, height, outputPath);

            var fileInfo = new FileInfo(outputPath);
            return new WindowScreenshotResult
            {
                Succeeded = true,
                Message = $"Window screenshot saved: {width}x{height}",
                FilePath = outputPath,
                CapturedWidth = width,
                CapturedHeight = height,
                FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0
            };
        }
        finally
        {
            DeleteObject(hBitmap);
            DeleteDC(hdcMemDC);
            ReleaseDC(_hwnd, hdcWindow);
        }
    }
    private static void SaveHBitmapAsImage(IntPtr hBitmap, int width, int height, string outputPath)
    {
        var bmi = new BITMAPINFOHEADER
        {
            biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
            biWidth = width,
            biHeight = -height, // top-down
            biPlanes = 1,
            biBitCount = 32,
            biCompression = 0 // BI_RGB
        };

        var stride = width * 4;
        var pixelData = new byte[stride * height];

        var hdcScreen = GetDC(IntPtr.Zero);
        GetDIBits(hdcScreen, hBitmap, 0, (uint)height, pixelData, ref bmi, 0);
        ReleaseDC(IntPtr.Zero, hdcScreen);

        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        if (outputPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            WritePngToStream(stream, width, height, pixelData);
        else
            WriteBmpToStream(stream, width, height, pixelData);
    }
    private static void WritePngToStream(Stream output, int width, int height, byte[] bgra)
    {
        var stride = width * 4;

        // PNG signature
        output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

        // IHDR chunk
        var ihdr = new byte[13];
        WriteBE32(ihdr, 0, width);
        WriteBE32(ihdr, 4, height);
        ihdr[8] = 8;  // bit depth
        ihdr[9] = 6;  // color type: RGBA
        WritePngChunk(output, new byte[] { 73, 72, 68, 82 }, ihdr); // "IHDR"

        // Raw scanlines: filter byte (0=None) + RGBA pixels per row
        var raw = new byte[(stride + 1) * height];
        for (var y = 0; y < height; y++)
        {
            var rowDst = y * (stride + 1);
            raw[rowDst] = 0; // filter: None
            var rowSrc = y * stride;
            for (var x = 0; x < width; x++)
            {
                var s = rowSrc + x * 4;
                var d = rowDst + 1 + x * 4;
                raw[d]     = bgra[s + 2]; // R (from BGRA B)
                raw[d + 1] = bgra[s + 1]; // G
                raw[d + 2] = bgra[s];     // B (from BGRA R)
                raw[d + 3] = 255;         // A
            }
        }

        // Compress with zlib and write IDAT
        byte[] compressed;
        using (var ms = new MemoryStream())
        {
            using (var zlib = new System.IO.Compression.ZLibStream(ms, System.IO.Compression.CompressionLevel.Fastest, leaveOpen: true))
                zlib.Write(raw);
            compressed = ms.ToArray();
        }
        WritePngChunk(output, new byte[] { 73, 68, 65, 84 }, compressed); // "IDAT"

        // IEND
        WritePngChunk(output, new byte[] { 73, 69, 78, 68 }, Array.Empty<byte>()); // "IEND"
    }
    private static void WritePngChunk(Stream output, byte[] type, byte[] data)
    {
        var buf = new byte[4];
        WriteBE32(buf, 0, data.Length);
        output.Write(buf);
        output.Write(type);
        if (data.Length > 0) output.Write(data);
        var crc = PngCrc32(type, data);
        buf[0] = (byte)(crc >> 24); buf[1] = (byte)(crc >> 16);
        buf[2] = (byte)(crc >> 8);  buf[3] = (byte)crc;
        output.Write(buf);
    }
    private static void WriteBE32(byte[] buf, int off, int val)
    {
        buf[off] = (byte)(val >> 24); buf[off + 1] = (byte)(val >> 16);
        buf[off + 2] = (byte)(val >> 8); buf[off + 3] = (byte)val;
    }
    private static uint PngCrc32(byte[] type, byte[] data)
    {
        uint c = 0xFFFFFFFF;
        foreach (var b in type) c = (c >> 8) ^ Crc32Table[(c ^ b) & 0xFF];
        foreach (var b in data) c = (c >> 8) ^ Crc32Table[(c ^ b) & 0xFF];
        return c ^ 0xFFFFFFFF;
    }
    private static readonly uint[] Crc32Table = InitCrc32Table();
    private static void WriteBmpToStream(Stream stream, int width, int height, byte[] bgra)
    {
        var stride = width * 4;
        var pixelDataSize = stride * height;
        var fileSize = 14 + 40 + pixelDataSize;

        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)0x4D42); // 'BM'
        writer.Write(fileSize);
        writer.Write(0);
        writer.Write(14 + 40);

        writer.Write(40);
        writer.Write(width);
        writer.Write(-height); // top-down
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(0);
        writer.Write(pixelDataSize);
        writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);

        writer.Write(bgra);
    }
    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);
    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr ho);
    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr hdc, IntPtr hbm, uint start, uint cLines,
        [Out] byte[] lpvBits, ref BITMAPINFOHEADER lpbmi, uint usage);

    private const uint PW_RENDERFULLCONTENT = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }
}
