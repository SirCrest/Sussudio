using System;
using System.Runtime.InteropServices;

namespace Sussudio.Services.Runtime;

// Win32 WNDPROC subclass that enforces a minimum window size by intercepting
// WM_GETMINMAXINFO. WinUI 3 doesn't expose this without dropping to interop,
// and both the main window and the stats window need the same boilerplate.
internal static class MinSizeWindowSubclass
{
    public static MinSizeHandle Install(IntPtr hwnd, int minWidthDip, int minHeightDip)
    {
        var handle = new MinSizeHandle(minWidthDip, minHeightDip);
        handle.OriginalWndProc = SetWindowLongPtr(
            hwnd,
            GWLP_WNDPROC,
            Marshal.GetFunctionPointerForDelegate(handle.Delegate));
        return handle;
    }

    public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // Held by the caller for the lifetime of the window — the GC must not
    // collect the delegate while Win32 still holds the function pointer.
    public sealed class MinSizeHandle
    {
        private readonly int _minWidthDip;
        private readonly int _minHeightDip;

        public MinSizeHandle(int minWidthDip, int minHeightDip)
        {
            _minWidthDip = minWidthDip;
            _minHeightDip = minHeightDip;
            Delegate = HandleMessage;
        }

        public WndProcDelegate Delegate { get; }
        public IntPtr OriginalWndProc { get; set; }

        private IntPtr HandleMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                var dpi = GetDpiForWindow(hWnd);
                var scale = dpi / 96.0;
                var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                mmi.ptMinTrackSize.X = (int)(_minWidthDip * scale);
                mmi.ptMinTrackSize.Y = (int)(_minHeightDip * scale);
                Marshal.StructureToPtr(mmi, lParam, false);
            }

            return CallWindowProc(OriginalWndProc, hWnd, msg, wParam, lParam);
        }
    }

    private const int GWLP_WNDPROC = -4;
    private const uint WM_GETMINMAXINFO = 0x0024;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

#pragma warning disable CS0649 // Populated by Marshal.PtrToStructure for WM_GETMINMAXINFO.
    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }
#pragma warning restore CS0649

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
}
