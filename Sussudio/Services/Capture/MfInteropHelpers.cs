using System;
using System.Runtime.InteropServices;

namespace Sussudio.Services.Capture;

// Shared Media Foundation interop primitives. The enumerator and the source
// reader both ref-count MFStartup/MFShutdown and both wrap a handful of typed
// IMFAttributes accessors with identical bodies; this file centralizes them so
// the two classes can't drift on hresult constants or refcount logic.
internal static class MfInteropHelpers
{
    public const int MfVersion = 0x00020070;
    public const int MfEAttributeNotFound = unchecked((int)0xC00D36E6);

    private static readonly object StartupSync = new();
    private static int _startupRefCount;

    [DllImport("mfplat.dll", ExactSpelling = true)]
    private static extern int MFStartup(int version, int dwFlags);

    [DllImport("mfplat.dll", ExactSpelling = true)]
    private static extern int MFShutdown();

    public static void ThrowIfFailed(int hr, string operation)
    {
        if (hr >= 0)
        {
            return;
        }

        throw new InvalidOperationException($"{operation} failed (hr=0x{hr:X8}).");
    }

    public static void AddStartupReference()
    {
        lock (StartupSync)
        {
            if (_startupRefCount == 0)
            {
                ThrowIfFailed(MFStartup(MfVersion, 0), "MFStartup");
            }

            _startupRefCount++;
        }
    }

    public static void ReleaseStartupReference()
    {
        lock (StartupSync)
        {
            if (_startupRefCount <= 0)
            {
                return;
            }

            _startupRefCount--;
            if (_startupRefCount == 0)
            {
                _ = MFShutdown();
            }
        }
    }

    public static bool TryGetGuid(IMFAttributes attributes, ref Guid key, out Guid value)
    {
        var hr = attributes.GetGUID(ref key, out value);
        if (hr == MfEAttributeNotFound)
        {
            value = Guid.Empty;
            return false;
        }

        ThrowIfFailed(hr, $"IMFAttributes.GetGUID({key})");
        return true;
    }

    public static bool TryGetUInt64(IMFAttributes attributes, ref Guid key, out ulong value)
    {
        var hr = attributes.GetUINT64(ref key, out value);
        if (hr == MfEAttributeNotFound)
        {
            value = 0;
            return false;
        }

        ThrowIfFailed(hr, $"IMFAttributes.GetUINT64({key})");
        return true;
    }

    public static bool TryGetUInt32(IMFAttributes attributes, ref Guid key, out int value)
    {
        var hr = attributes.GetUINT32(ref key, out value);
        if (hr == MfEAttributeNotFound)
        {
            value = 0;
            return false;
        }

        ThrowIfFailed(hr, $"IMFAttributes.GetUINT32({key})");
        return true;
    }

    public static string TryReadAllocatedString(IMFAttributes attributes, ref Guid key)
    {
        IntPtr textPtr = IntPtr.Zero;
        try
        {
            var hr = attributes.GetAllocatedString(ref key, out textPtr, out var length);
            if (hr == MfEAttributeNotFound || textPtr == IntPtr.Zero)
            {
                return string.Empty;
            }

            ThrowIfFailed(hr, $"IMFAttributes.GetAllocatedString({key})");
            return length > 0
                ? Marshal.PtrToStringUni(textPtr, length) ?? string.Empty
                : Marshal.PtrToStringUni(textPtr) ?? string.Empty;
        }
        finally
        {
            if (textPtr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(textPtr);
            }
        }
    }
}
