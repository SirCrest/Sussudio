using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Sussudio.Services.Capture;

internal static partial class KsExtensionUnitNative
{
    internal static SafeFileHandle? TryOpen(string path, out int? errorCode)
    {
        errorCode = null;
        var readWrite = CreateFile(
            path,
            GenericRead | GenericWrite,
            FileShareRead | FileShareWrite,
            IntPtr.Zero,
            OpenExisting,
            0,
            IntPtr.Zero);

        if (!readWrite.IsInvalid)
        {
            return readWrite;
        }

        var readWriteError = Marshal.GetLastWin32Error();
        readWrite.Dispose();

        var readOnly = CreateFile(
            path,
            GenericRead,
            FileShareRead | FileShareWrite,
            IntPtr.Zero,
            OpenExisting,
            0,
            IntPtr.Zero);

        if (readOnly.IsInvalid)
        {
            var readOnlyError = Marshal.GetLastWin32Error();
            readOnly.Dispose();
            errorCode = readOnlyError != 0 ? readOnlyError : readWriteError;
            return null;
        }

        return readOnly;
    }
}
