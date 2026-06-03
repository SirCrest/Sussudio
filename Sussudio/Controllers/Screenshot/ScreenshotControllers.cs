using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class PreviewScreenshotControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required Button ScreenshotButton { get; init; }
}

internal sealed class PreviewScreenshotController
{
    private readonly PreviewScreenshotControllerContext _context;

    public PreviewScreenshotController(PreviewScreenshotControllerContext context)
    {
        _context = context;
    }

    public async Task CaptureAsync()
    {
        if (!_context.ViewModel.IsPreviewing)
        {
            _context.ViewModel.StatusText = PreviewScreenshotPlanPolicy.PreviewRequiredStatusText;
            return;
        }

        var plan = PreviewScreenshotPlanPolicy.Create(
            _context.ViewModel.OutputPath,
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            DateTime.Now,
            Guid.NewGuid());
        Directory.CreateDirectory(plan.OutputDirectory);

        _context.ScreenshotButton.IsEnabled = false;
        try
        {
            var result = await _context.ViewModel.CapturePreviewFrameAsync(plan.FilePath);
            if (result.Succeeded)
            {
                _context.ViewModel.StatusText = PreviewScreenshotPlanPolicy.FormatSavedStatus(plan.FilePath);
                Logger.Log(PreviewScreenshotPlanPolicy.FormatSavedLog(plan.FilePath, result.CapturedWidth, result.CapturedHeight));
            }
            else
            {
                _context.ViewModel.StatusText = PreviewScreenshotPlanPolicy.FormatFailedStatus(result.Message);
                Logger.Log(PreviewScreenshotPlanPolicy.FormatFailedLog(result.Message));
            }
        }
        finally
        {
            _context.ScreenshotButton.IsEnabled = true;
        }
    }
}

internal static class PreviewScreenshotPlanPolicy
{
    public const string PreviewRequiredStatusText = "Start preview before capturing a screenshot";

    private const string DefaultOutputFolderName = "Sussudio";
    private const string TimestampFormat = "yyyy-MM-dd_HH-mm-ss_fff";

    public static PreviewScreenshotPlan Create(
        string? configuredOutputPath,
        string picturesFolder,
        DateTime timestamp,
        Guid captureId)
    {
        var outputDirectory = string.IsNullOrWhiteSpace(configuredOutputPath)
            ? Path.Combine(picturesFolder, DefaultOutputFolderName)
            : configuredOutputPath;
        var filePath = Path.Combine(outputDirectory, $"Screenshot_{timestamp.ToString(TimestampFormat)}_{captureId:N}.png");

        return new PreviewScreenshotPlan(outputDirectory, filePath);
    }

    public static string FormatSavedStatus(string filePath)
        => $"Screenshot saved: {Path.GetFileName(filePath)}";

    public static string FormatFailedStatus(string message)
        => $"Screenshot failed: {message}";

    public static string FormatSavedLog(string filePath, int capturedWidth, int capturedHeight)
        => $"SCREENSHOT_SAVED path={filePath} width={capturedWidth} height={capturedHeight}";

    public static string FormatFailedLog(string message)
        => $"SCREENSHOT_FAILED reason={message}";
}

internal readonly record struct PreviewScreenshotPlan(string OutputDirectory, string FilePath);

internal sealed class WindowScreenshotController
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Func<IntPtr> _windowHandleProvider;

    public WindowScreenshotController(
        DispatcherQueue dispatcherQueue,
        Func<IntPtr> windowHandleProvider)
    {
        _dispatcherQueue = dispatcherQueue;
        _windowHandleProvider = windowHandleProvider;
    }

    public Task<WindowScreenshotResult> CaptureAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(new WindowScreenshotResult
            {
                Succeeded = false,
                Message = "Screenshot canceled."
            });
        }

        var completion = new TaskCompletionSource<WindowScreenshotResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration cancellationRegistration = default;
        if (cancellationToken.CanBeCanceled)
        {
            cancellationRegistration = cancellationToken.Register(() =>
            {
                completion.TrySetResult(new WindowScreenshotResult
                {
                    Succeeded = false,
                    Message = "Screenshot canceled."
                });
            });
            _ = completion.Task.ContinueWith(
                _ => cancellationRegistration.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetResult(new WindowScreenshotResult
                    {
                        Succeeded = false,
                        Message = "Screenshot canceled."
                    });
                    return;
                }

                var result = CaptureCore(outputPath);
                completion.TrySetResult(result);
            }
            catch (Exception ex)
            {
                completion.TrySetResult(new WindowScreenshotResult
                {
                    Succeeded = false,
                    Message = $"Screenshot failed: {ex.Message}"
                });
            }
        }))
        {
            cancellationRegistration.Dispose();
            completion.TrySetResult(new WindowScreenshotResult
            {
                Succeeded = false,
                Message = "Failed to enqueue screenshot capture on the UI thread."
            });
        }

        return completion.Task;
    }

    private WindowScreenshotResult CaptureCore(string outputPath)
        => CaptureNative(_windowHandleProvider(), outputPath);

    private static WindowScreenshotResult CaptureNative(IntPtr hwnd, string outputPath)
    {
        if (hwnd == IntPtr.Zero)
        {
            return new WindowScreenshotResult { Succeeded = false, Message = "Window handle not available." };
        }

        if (!GetWindowRect(hwnd, out var rect))
        {
            return new WindowScreenshotResult { Succeeded = false, Message = "Failed to get window rect." };
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            return new WindowScreenshotResult { Succeeded = false, Message = $"Invalid window size: {width}x{height}" };
        }

        var hdcWindow = GetDC(hwnd);
        var hdcMemDC = CreateCompatibleDC(hdcWindow);
        var hBitmap = CreateCompatibleBitmap(hdcWindow, width, height);
        var hOld = SelectObject(hdcMemDC, hBitmap);

        try
        {
            if (!PrintWindow(hwnd, hdcMemDC, PW_RENDERFULLCONTENT))
            {
                return new WindowScreenshotResult { Succeeded = false, Message = "PrintWindow failed." };
            }

            SelectObject(hdcMemDC, hOld);

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
            ReleaseDC(hwnd, hdcWindow);
        }
    }

    private static void SaveHBitmapAsImage(IntPtr hBitmap, int width, int height, string outputPath)
    {
        var bmi = new BITMAPINFOHEADER
        {
            biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
            biWidth = width,
            biHeight = -height,
            biPlanes = 1,
            biBitCount = 32,
            biCompression = 0
        };

        var stride = width * 4;
        var pixelData = new byte[stride * height];

        var hdcScreen = GetDC(IntPtr.Zero);
        GetDIBits(hdcScreen, hBitmap, 0, (uint)height, pixelData, ref bmi, 0);
        ReleaseDC(IntPtr.Zero, hdcScreen);

        using var stream = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        WindowScreenshotImageEncoder.WriteToStream(
            stream,
            width,
            height,
            pixelData,
            outputPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
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
        if (data.Length > 0)
        {
            output.Write(data);
        }

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
        foreach (var b in type)
        {
            c = (c >> 8) ^ Crc32Table[(c ^ b) & 0xFF];
        }

        foreach (var b in data)
        {
            c = (c >> 8) ^ Crc32Table[(c ^ b) & 0xFF];
        }

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
