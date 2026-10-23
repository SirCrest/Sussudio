using System;
using System.Runtime.InteropServices;

namespace Sussudio.Controllers;

internal sealed class WindowScreenshotNativeCapture
{
    private const uint PwRenderFullContent = 0x00000002;
    private const uint DibRgbColors = 0;
    private const int MaximumPixelBufferBytes = 256 * 1024 * 1024;
    private static readonly IntPtr InvalidGdiObject = new(-1);

    private readonly IWindowScreenshotNativeApi _native;

    public WindowScreenshotNativeCapture()
        : this(new WindowsWindowScreenshotNativeApi())
    {
    }

    internal WindowScreenshotNativeCapture(IWindowScreenshotNativeApi native)
    {
        _native = native;
    }

    internal WindowScreenshotCapture Capture(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("Window handle not available.");
        }

        if (!_native.GetWindowRect(hwnd, out var rect))
        {
            throw new InvalidOperationException("Failed to get window rect.");
        }

        var width = (long)rect.Right - rect.Left;
        var height = (long)rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0 || width > int.MaxValue || height > int.MaxValue)
        {
            throw new InvalidOperationException($"Invalid window size: {width}x{height}");
        }

        int stride;
        int bufferLength;
        try
        {
            stride = checked((int)width * 4);
            bufferLength = checked(stride * (int)height);
        }
        catch (OverflowException ex)
        {
            throw new InvalidOperationException($"Window screenshot dimensions are too large: {width}x{height}", ex);
        }

        if (bufferLength > MaximumPixelBufferBytes)
        {
            throw new InvalidOperationException($"Window screenshot pixel buffer exceeds the {MaximumPixelBufferBytes}-byte limit.");
        }

        var pixels = new byte[bufferLength];
        var hdcWindow = IntPtr.Zero;
        var hdcMemory = IntPtr.Zero;
        var bitmap = IntPtr.Zero;
        var previousObject = IntPtr.Zero;
        var bitmapSelected = false;
        var restoreAttempted = false;
        Exception? operationFailure = null;

        try
        {
            hdcWindow = _native.GetDC(hwnd);
            if (hdcWindow == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to acquire window device context.");
            }

            hdcMemory = _native.CreateCompatibleDC(hdcWindow);
            if (hdcMemory == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create compatible device context.");
            }

            bitmap = _native.CreateCompatibleBitmap(hdcWindow, (int)width, (int)height);
            if (bitmap == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create compatible bitmap.");
            }

            previousObject = _native.SelectObject(hdcMemory, bitmap);
            if (IsInvalidGdiObject(previousObject))
            {
                throw new InvalidOperationException("Failed to select screenshot bitmap.");
            }

            bitmapSelected = true;
            if (!_native.PrintWindow(hwnd, hdcMemory, PwRenderFullContent))
            {
                throw new InvalidOperationException("PrintWindow failed.");
            }

            restoreAttempted = true;
            if (IsInvalidGdiObject(_native.SelectObject(hdcMemory, previousObject)))
            {
                throw new InvalidOperationException("Failed to restore the previous device context object.");
            }

            bitmapSelected = false;

            var bitmapInfo = new WindowScreenshotBitmapInfoHeader
            {
                Size = (uint)Marshal.SizeOf<WindowScreenshotBitmapInfoHeader>(),
                Width = (int)width,
                Height = -(int)height,
                Planes = 1,
                BitCount = 32,
                Compression = 0
            };

            var linesRead = _native.GetDIBits(
                hdcWindow,
                bitmap,
                0,
                (uint)height,
                pixels,
                ref bitmapInfo,
                DibRgbColors);
            if (linesRead != height)
            {
                throw new InvalidOperationException($"Screenshot readback incomplete: {linesRead} of {height} scanlines.");
            }

            return new WindowScreenshotCapture((int)width, (int)height, pixels);
        }
        catch (Exception ex)
        {
            operationFailure = ex;
            throw;
        }
        finally
        {
            var cleanupFailure = Cleanup(
                hwnd,
                hdcWindow,
                hdcMemory,
                bitmap,
                previousObject,
                bitmapSelected,
                restoreAttempted);
            if (cleanupFailure is not null)
            {
                if (operationFailure is not null)
                {
                    throw new AggregateException("Screenshot capture failed and native cleanup was incomplete.", operationFailure, cleanupFailure);
                }

                throw new InvalidOperationException("Screenshot capture succeeded but native cleanup was incomplete.", cleanupFailure);
            }
        }
    }

    private Exception? Cleanup(
        IntPtr hwnd,
        IntPtr hdcWindow,
        IntPtr hdcMemory,
        IntPtr bitmap,
        IntPtr previousObject,
        bool bitmapSelected,
        bool restoreAttempted)
    {
        Exception? failure = null;
        var restoreFailed = false;
        var memoryDcDeleteAttempted = false;

        if (bitmapSelected)
        {
            if (!restoreAttempted)
            {
                try
                {
                    restoreAttempted = true;
                    restoreFailed = IsInvalidGdiObject(_native.SelectObject(hdcMemory, previousObject));
                    if (restoreFailed)
                    {
                        failure = new InvalidOperationException("Failed to restore the previous device context object.");
                    }
                }
                catch (Exception ex)
                {
                    restoreFailed = true;
                    failure = ex;
                }
            }
            else
            {
                restoreFailed = true;
            }
        }

        if (restoreFailed && hdcMemory != IntPtr.Zero)
        {
            memoryDcDeleteAttempted = true;
            try
            {
                if (!_native.DeleteDC(hdcMemory))
                {
                    throw new InvalidOperationException("Failed to delete screenshot device context.");
                }
                hdcMemory = IntPtr.Zero;
            }
            catch (Exception ex)
            {
                failure = failure is null ? ex : new AggregateException(failure, ex);
            }
        }

        if (bitmap != IntPtr.Zero && (!restoreFailed || hdcMemory == IntPtr.Zero))
        {
            TryCleanup(() =>
            {
                if (!_native.DeleteObject(bitmap))
                {
                    throw new InvalidOperationException("Failed to delete screenshot bitmap.");
                }
            }, ref failure);
        }

        if (hdcMemory != IntPtr.Zero && !memoryDcDeleteAttempted)
        {
            TryCleanup(() =>
            {
                if (!_native.DeleteDC(hdcMemory))
                {
                    throw new InvalidOperationException("Failed to delete screenshot device context.");
                }
            }, ref failure);
        }

        if (hdcWindow != IntPtr.Zero)
        {
            TryCleanup(() =>
            {
                if (_native.ReleaseDC(hwnd, hdcWindow) == 0)
                {
                    throw new InvalidOperationException("Failed to release window device context.");
                }
            }, ref failure);
        }

        return failure;
    }

    private static void TryCleanup(Action cleanup, ref Exception? failure)
    {
        try
        {
            cleanup();
        }
        catch (Exception ex)
        {
            failure = failure is null ? ex : new AggregateException(failure, ex);
        }
    }

    private static bool IsInvalidGdiObject(IntPtr value)
        => value == IntPtr.Zero || value == InvalidGdiObject;
}

internal readonly record struct WindowScreenshotRect(int Left, int Top, int Right, int Bottom);

[StructLayout(LayoutKind.Sequential)]
internal struct WindowScreenshotBitmapInfoHeader
{
    public uint Size;
    public int Width;
    public int Height;
    public ushort Planes;
    public ushort BitCount;
    public uint Compression;
    public uint ImageSize;
    public int XPelsPerMeter;
    public int YPelsPerMeter;
    public uint ColorsUsed;
    public uint ColorsImportant;
}

internal sealed record WindowScreenshotCapture(int Width, int Height, byte[] Bgra);

internal interface IWindowScreenshotNativeApi
{
    bool GetWindowRect(IntPtr hwnd, out WindowScreenshotRect rect);
    IntPtr GetDC(IntPtr hwnd);
    int ReleaseDC(IntPtr hwnd, IntPtr hdc);
    IntPtr CreateCompatibleDC(IntPtr hdc);
    bool DeleteDC(IntPtr hdc);
    IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);
    IntPtr SelectObject(IntPtr hdc, IntPtr value);
    bool DeleteObject(IntPtr value);
    bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
    int GetDIBits(IntPtr hdc, IntPtr bitmap, uint start, uint lines, byte[] pixels,
        ref WindowScreenshotBitmapInfoHeader header, uint usage);
}

internal sealed class WindowsWindowScreenshotNativeApi : IWindowScreenshotNativeApi
{
    public bool GetWindowRect(IntPtr hwnd, out WindowScreenshotRect rect)
    {
        var succeeded = GetWindowRectNative(hwnd, out var nativeRect);
        rect = new WindowScreenshotRect(nativeRect.Left, nativeRect.Top, nativeRect.Right, nativeRect.Bottom);
        return succeeded;
    }

    public IntPtr GetDC(IntPtr hwnd) => GetDCNative(hwnd);
    public int ReleaseDC(IntPtr hwnd, IntPtr hdc) => ReleaseDCNative(hwnd, hdc);
    public IntPtr CreateCompatibleDC(IntPtr hdc) => CreateCompatibleDCNative(hdc);
    public bool DeleteDC(IntPtr hdc) => DeleteDCNative(hdc);
    public IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height) => CreateCompatibleBitmapNative(hdc, width, height);
    public IntPtr SelectObject(IntPtr hdc, IntPtr value) => SelectObjectNative(hdc, value);
    public bool DeleteObject(IntPtr value) => DeleteObjectNative(value);
    public bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags) => PrintWindowNative(hwnd, hdc, flags);
    public int GetDIBits(IntPtr hdc, IntPtr bitmap, uint start, uint lines, byte[] pixels,
        ref WindowScreenshotBitmapInfoHeader header, uint usage)
        => GetDIBitsNative(hdc, bitmap, start, lines, pixels, ref header, usage);

    [DllImport("user32.dll", EntryPoint = "GetWindowRect")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRectNative(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll", EntryPoint = "GetDC")]
    private static extern IntPtr GetDCNative(IntPtr hwnd);

    [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
    private static extern int ReleaseDCNative(IntPtr hwnd, IntPtr hdc);

    [DllImport("user32.dll", EntryPoint = "PrintWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindowNative(IntPtr hwnd, IntPtr hdc, uint flags);

    [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC")]
    private static extern IntPtr CreateCompatibleDCNative(IntPtr hdc);

    [DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDCNative(IntPtr hdc);

    [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleBitmap")]
    private static extern IntPtr CreateCompatibleBitmapNative(IntPtr hdc, int width, int height);

    [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
    private static extern IntPtr SelectObjectNative(IntPtr hdc, IntPtr value);

    [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObjectNative(IntPtr value);

    [DllImport("gdi32.dll", EntryPoint = "GetDIBits")]
    private static extern int GetDIBitsNative(IntPtr hdc, IntPtr bitmap, uint start, uint lines,
        [Out] byte[] pixels, ref WindowScreenshotBitmapInfoHeader header, uint usage);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
