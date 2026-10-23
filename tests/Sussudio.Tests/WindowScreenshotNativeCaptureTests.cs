using Sussudio.Controllers;
using Xunit;

namespace Sussudio.Tests;

public sealed class WindowScreenshotNativeCaptureTests
{
    private static readonly IntPtr Window = new(101);
    private static readonly IntPtr WindowDc = new(201);
    private static readonly IntPtr MemoryDc = new(301);
    private static readonly IntPtr Bitmap = new(401);
    private static readonly IntPtr PreviousObject = new(501);
    private static readonly byte[] Pixels = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];

    [Fact]
    public void CaptureRejectsMissingWindowHandleBeforeNativeCalls()
    {
        var native = new FakeNativeApi();

        Assert.Throws<InvalidOperationException>(() => new WindowScreenshotNativeCapture(native).Capture(IntPtr.Zero));
        Assert.Empty(native.Calls);
    }

    [Fact]
    public void CaptureRejectsWindowRectFailureAndInvalidDimensions()
    {
        var failedRect = new FakeNativeApi { RectSucceeds = false };
        Assert.Throws<InvalidOperationException>(() => new WindowScreenshotNativeCapture(failedRect).Capture(Window));
        Assert.Equal(["GetWindowRect"], failedRect.Calls);

        var invalidSize = new FakeNativeApi { Rect = new WindowScreenshotRect(10, 20, 10, 40) };
        Assert.Throws<InvalidOperationException>(() => new WindowScreenshotNativeCapture(invalidSize).Capture(Window));
        Assert.Equal(["GetWindowRect"], invalidSize.Calls);

        var negativeSize = new FakeNativeApi { Rect = new WindowScreenshotRect(10, 20, 12, 19) };
        Assert.Throws<InvalidOperationException>(() => new WindowScreenshotNativeCapture(negativeSize).Capture(Window));
        Assert.Equal(["GetWindowRect"], negativeSize.Calls);
    }

    [Theory]
    [InlineData("GetDC")]
    [InlineData("CreateCompatibleDC")]
    [InlineData("CreateCompatibleBitmap")]
    public void CaptureRejectsAcquisitionFailureAndReleasesOnlyAcquiredResources(string failedCall)
    {
        var native = new FakeNativeApi { FailedCall = failedCall };

        Assert.Throws<InvalidOperationException>(() => new WindowScreenshotNativeCapture(native).Capture(Window));

        Assert.DoesNotContain("PrintWindow", native.Calls);
        Assert.Equal(failedCall != "GetDC", native.Calls.Contains("ReleaseDC"));
        Assert.Equal(failedCall is "CreateCompatibleBitmap", native.Calls.Contains("DeleteDC"));
        Assert.DoesNotContain("DeleteObject", native.Calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CaptureRejectsFailedBitmapSelectionAndCleansAcquiredResources(int selectionResult)
    {
        var native = new FakeNativeApi { InitialSelectResult = new IntPtr(selectionResult) };

        Assert.Throws<InvalidOperationException>(() => new WindowScreenshotNativeCapture(native).Capture(Window));

        Assert.DoesNotContain("PrintWindow", native.Calls);
        Assert.Equal("DeleteObject", native.Calls[^3]);
        Assert.Equal("DeleteDC", native.Calls[^2]);
        Assert.Equal("ReleaseDC", native.Calls[^1]);
    }

    [Fact]
    public void CaptureReportsPrintWindowFailureAndRestoresBitmapBeforeCleanup()
    {
        var native = new FakeNativeApi { PrintWindowSucceeds = false };

        Assert.Throws<InvalidOperationException>(() => new WindowScreenshotNativeCapture(native).Capture(Window));

        Assert.Contains("SelectObject:restore", native.Calls);
        AssertCleanupOrder(native, "SelectObject:restore", "DeleteObject", "DeleteDC", "ReleaseDC");
        Assert.DoesNotContain("GetDIBits", native.Calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CaptureDeletesMemoryDcBeforeBitmapWhenRestoringSelectionFails(int selectionResult)
    {
        var native = new FakeNativeApi { RestoreSelectResult = new IntPtr(selectionResult) };

        Assert.Throws<InvalidOperationException>(() => new WindowScreenshotNativeCapture(native).Capture(Window));

        AssertCleanupOrder(native, "SelectObject:restore", "DeleteDC", "DeleteObject", "ReleaseDC");
        Assert.DoesNotContain("GetDIBits", native.Calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void CaptureRejectsIncompleteReadbackAndReleasesResources(int scanLines)
    {
        var native = new FakeNativeApi { ScanLinesRead = scanLines };

        Assert.Throws<InvalidOperationException>(() => new WindowScreenshotNativeCapture(native).Capture(Window));

        AssertCleanupOrder(native, "GetDIBits", "DeleteObject", "DeleteDC", "ReleaseDC");
    }

    [Fact]
    public void CapturePropagatesReadbackExceptionAfterReleasingResources()
    {
        var expected = new IOException("readback fault");
        var native = new FakeNativeApi { ReadbackException = expected };

        var actual = Assert.Throws<IOException>(() => new WindowScreenshotNativeCapture(native).Capture(Window));

        Assert.Same(expected, actual);
        AssertCleanupOrder(native, "GetDIBits", "DeleteObject", "DeleteDC", "ReleaseDC");
    }

    [Fact]
    public void CaptureReturnsCompleteBgraOnlyAfterSuccessfulReadbackAndCleansInOwnershipOrder()
    {
        var native = new FakeNativeApi { ScanLinesRead = 2, ReadbackPixels = [.. Pixels] };

        var capture = new WindowScreenshotNativeCapture(native).Capture(Window);

        Assert.Equal(2, capture.Width);
        Assert.Equal(2, capture.Height);
        Assert.Equal(Pixels, capture.Bgra);
        Assert.Equal(2u, native.RequestedScanLines);
        Assert.Equal(WindowDc, native.ReadbackDc);
        Assert.Equal(Bitmap, native.ReadbackBitmap);
        AssertCleanupOrder(native, "SelectObject:restore", "GetDIBits", "DeleteObject", "DeleteDC", "ReleaseDC");
    }

    [Fact]
    public void CaptureRejectsPixelBufferSizeOverflowBeforeAllocatingNativeResources()
    {
        var native = new FakeNativeApi { Rect = new WindowScreenshotRect(0, 0, int.MaxValue, 2) };

        Assert.Throws<InvalidOperationException>(() => new WindowScreenshotNativeCapture(native).Capture(Window));

        Assert.Equal(["GetWindowRect"], native.Calls);
    }

    private static void AssertCleanupOrder(FakeNativeApi native, params string[] calls)
    {
        var actual = native.Calls.Where(call => calls.Contains(call)).ToArray();
        Assert.Equal(calls, actual);
    }

    private sealed class FakeNativeApi : IWindowScreenshotNativeApi
    {
        public List<string> Calls { get; } = [];
        public WindowScreenshotRect Rect { get; set; } = new(10, 20, 12, 22);
        public bool RectSucceeds { get; set; } = true;
        public string? FailedCall { get; set; }
        public IntPtr InitialSelectResult { get; set; } = PreviousObject;
        public IntPtr RestoreSelectResult { get; set; } = Bitmap;
        public bool PrintWindowSucceeds { get; set; } = true;
        public int ScanLinesRead { get; set; } = 2;
        public byte[] ReadbackPixels { get; set; } = [.. Pixels];
        public Exception? ReadbackException { get; set; }
        public uint RequestedScanLines { get; private set; }
        public IntPtr ReadbackDc { get; private set; }
        public IntPtr ReadbackBitmap { get; private set; }

        public bool GetWindowRect(IntPtr hwnd, out WindowScreenshotRect rect)
        {
            Calls.Add("GetWindowRect");
            rect = Rect;
            return RectSucceeds;
        }

        public IntPtr GetDC(IntPtr hwnd)
        {
            Calls.Add("GetDC");
            return FailedCall == "GetDC" ? IntPtr.Zero : WindowDc;
        }

        public int ReleaseDC(IntPtr hwnd, IntPtr hdc)
        {
            Calls.Add("ReleaseDC");
            return 1;
        }

        public IntPtr CreateCompatibleDC(IntPtr hdc)
        {
            Calls.Add("CreateCompatibleDC");
            return FailedCall == "CreateCompatibleDC" ? IntPtr.Zero : MemoryDc;
        }

        public bool DeleteDC(IntPtr hdc)
        {
            Calls.Add("DeleteDC");
            return true;
        }

        public IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height)
        {
            Calls.Add("CreateCompatibleBitmap");
            return FailedCall == "CreateCompatibleBitmap" ? IntPtr.Zero : Bitmap;
        }

        public IntPtr SelectObject(IntPtr hdc, IntPtr value)
        {
            var restoring = value == PreviousObject;
            Calls.Add(restoring ? "SelectObject:restore" : "SelectObject:bitmap");
            return restoring ? RestoreSelectResult : InitialSelectResult;
        }

        public bool DeleteObject(IntPtr value)
        {
            Calls.Add("DeleteObject");
            return true;
        }

        public bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags)
        {
            Calls.Add("PrintWindow");
            return PrintWindowSucceeds;
        }

        public int GetDIBits(IntPtr hdc, IntPtr bitmap, uint start, uint lines, byte[] pixels,
            ref WindowScreenshotBitmapInfoHeader header, uint usage)
        {
            Calls.Add("GetDIBits");
            ReadbackDc = hdc;
            ReadbackBitmap = bitmap;
            RequestedScanLines = lines;
            if (ReadbackException is not null)
            {
                throw ReadbackException;
            }

            Array.Copy(ReadbackPixels, pixels, Math.Min(ReadbackPixels.Length, pixels.Length));
            return ScanLinesRead;
        }
    }
}
