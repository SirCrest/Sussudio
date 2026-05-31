using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Sussudio.Services.Capture;

// Shared Media Foundation interop primitives. The enumerator and the source
// reader both ref-count MFStartup/MFShutdown and both wrap a handful of typed
// IMFAttributes accessors with identical bodies; keep these beside the MF COM
// contracts so the two callers cannot drift on hresult constants or refcounts.
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

    public static bool MatchesSymbolicLink(string? target, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        return string.Equals(target, candidate, StringComparison.OrdinalIgnoreCase) ||
               candidate.Contains(target, StringComparison.OrdinalIgnoreCase) ||
               target.Contains(candidate, StringComparison.OrdinalIgnoreCase);
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

[ComImport]
[Guid("2cd2d921-c447-44a7-a13c-4adabfc247e3")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFAttributes
{
    [PreserveSig]
    int GetItem(ref Guid guidKey, IntPtr pValue);

    [PreserveSig]
    int GetItemType(ref Guid guidKey, out int pType);

    [PreserveSig]
    int CompareItem(ref Guid guidKey, IntPtr value, [MarshalAs(UnmanagedType.Bool)] out bool pbResult);

    [PreserveSig]
    int Compare(IMFAttributes pTheirs, int matchType, [MarshalAs(UnmanagedType.Bool)] out bool pbResult);

    [PreserveSig]
    int GetUINT32(ref Guid guidKey, out int punValue);

    [PreserveSig]
    int GetUINT64(ref Guid guidKey, out ulong punValue);

    [PreserveSig]
    int GetDouble(ref Guid guidKey, out double pfValue);

    [PreserveSig]
    int GetGUID(ref Guid guidKey, out Guid pguidValue);

    [PreserveSig]
    int GetStringLength(ref Guid guidKey, out int pcchLength);

    [PreserveSig]
    int GetString(ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszValue, int cchBufSize, out int pcchLength);

    [PreserveSig]
    int GetAllocatedString(ref Guid guidKey, out IntPtr ppwszValue, out int pcchLength);

    [PreserveSig]
    int GetBlobSize(ref Guid guidKey, out int pcbBlobSize);

    [PreserveSig]
    int GetBlob(ref Guid guidKey, IntPtr pBuf, int cbBufSize, out int pcbBlobSize);

    [PreserveSig]
    int GetAllocatedBlob(ref Guid guidKey, out IntPtr ppBuf, out int pcbSize);

    [PreserveSig]
    int GetUnknown(ref Guid guidKey, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

    [PreserveSig]
    int SetItem(ref Guid guidKey, IntPtr value);

    [PreserveSig]
    int DeleteItem(ref Guid guidKey);

    [PreserveSig]
    int DeleteAllItems();

    [PreserveSig]
    int SetUINT32(ref Guid guidKey, int unValue);

    [PreserveSig]
    int SetUINT64(ref Guid guidKey, ulong unValue);

    [PreserveSig]
    int SetDouble(ref Guid guidKey, double fValue);

    [PreserveSig]
    int SetGUID(ref Guid guidKey, ref Guid guidValue);

    [PreserveSig]
    int SetString(ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] string wszValue);

    [PreserveSig]
    int SetBlob(ref Guid guidKey, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBuf, int cbBufSize);

    [PreserveSig]
    int SetUnknown(ref Guid guidKey, [MarshalAs(UnmanagedType.IUnknown)] object? pUnknown);

    [PreserveSig]
    int LockStore();

    [PreserveSig]
    int UnlockStore();

    [PreserveSig]
    int GetCount(out int pcItems);

    [PreserveSig]
    int GetItemByIndex(int unIndex, out Guid pguidKey, IntPtr pValue);

    [PreserveSig]
    int CopyAllItems(IMFAttributes pDest);
}

[ComImport]
[Guid("44ae0fa8-ea31-4109-8d2e-4cae4997c555")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFMediaType : IMFAttributes
{
    [PreserveSig]
    int GetMajorType(out Guid pguidMajorType);

    [PreserveSig]
    int IsCompressedFormat([MarshalAs(UnmanagedType.Bool)] out bool pfCompressed);

    [PreserveSig]
    int IsEqual(IMFMediaType pIMediaType, out int pdwFlags);

    [PreserveSig]
    int GetRepresentation(Guid guidRepresentation, out IntPtr ppvRepresentation);

    [PreserveSig]
    int FreeRepresentation(Guid guidRepresentation, IntPtr pvRepresentation);
}

[ComImport]
[Guid("7FEE9E9A-4A89-47A6-899C-B6A53A70FB67")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFActivate : IMFAttributes
{
    [PreserveSig]
    int ActivateObject(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

    [PreserveSig]
    int ShutdownObject();

    [PreserveSig]
    int DetachObject();
}

[ComImport]
[Guid("279a808d-aec7-40c8-9c6b-a6b492c78a66")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFMediaSource
{
}

[ComImport]
[Guid("70ae66f2-c809-4e4f-8915-bdcb406b7993")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFSourceReader
{
    [PreserveSig]
    int GetStreamSelection(int dwStreamIndex, [MarshalAs(UnmanagedType.Bool)] out bool pfSelected);

    [PreserveSig]
    int SetStreamSelection(int dwStreamIndex, [MarshalAs(UnmanagedType.Bool)] bool fSelected);

    [PreserveSig]
    int GetNativeMediaType(int dwStreamIndex, int dwMediaTypeIndex, out IMFMediaType? ppMediaType);

    [PreserveSig]
    int GetCurrentMediaType(int dwStreamIndex, out IMFMediaType? ppMediaType);

    [PreserveSig]
    int SetCurrentMediaType(int dwStreamIndex, IntPtr pdwReserved, IMFMediaType pMediaType);

    [PreserveSig]
    int SetCurrentPosition(ref Guid guidTimeFormat, IntPtr varPosition);

    [PreserveSig]
    int ReadSample(
        int dwStreamIndex,
        int dwControlFlags,
        out int pdwActualStreamIndex,
        out int pdwStreamFlags,
        out long pllTimestamp,
        out IMFSample? ppSample);

    [PreserveSig]
    int Flush(int dwStreamIndex);

    [PreserveSig]
    int GetServiceForStream(int dwStreamIndex, ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppvObject);

    [PreserveSig]
    int GetPresentationAttribute(int dwStreamIndex, ref Guid guidAttribute, IntPtr pvarAttribute);
}

/// <summary>
/// Flattened IMFSample COM interface - does NOT use C# interface inheritance.
/// .NET COM interop miscalculates vtable slot offsets when using
/// <c>IMFSample : IMFAttributes</c>, causing derived methods to dispatch to
/// wrong vtable entries. This flattened layout explicitly reserves slots 3-32
/// for the 30 inherited IMFAttributes methods, then places the 14 IMFSample
/// methods at the correct slots 33-46.
/// </summary>
[ComImport]
[Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFSample
{
    // IMFAttributes vtable slots 3-32 (30 methods). These placeholders reserve
    // the correct vtable positions. Never call them through this interface; use
    // IMFAttributes directly for attribute access.
    [PreserveSig] int _Attr_GetItem(ref Guid guidKey, IntPtr pValue);
    [PreserveSig] int _Attr_GetItemType(ref Guid guidKey, out int pType);
    [PreserveSig] int _Attr_CompareItem(ref Guid guidKey, IntPtr value, [MarshalAs(UnmanagedType.Bool)] out bool pbResult);
    [PreserveSig] int _Attr_Compare(IMFAttributes pTheirs, int matchType, [MarshalAs(UnmanagedType.Bool)] out bool pbResult);
    [PreserveSig] int _Attr_GetUINT32(ref Guid guidKey, out int punValue);
    [PreserveSig] int _Attr_GetUINT64(ref Guid guidKey, out ulong punValue);
    [PreserveSig] int _Attr_GetDouble(ref Guid guidKey, out double pfValue);
    [PreserveSig] int _Attr_GetGUID(ref Guid guidKey, out Guid pguidValue);
    [PreserveSig] int _Attr_GetStringLength(ref Guid guidKey, out int pcchLength);
    [PreserveSig] int _Attr_GetString(ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszValue, int cchBufSize, out int pcchLength);
    [PreserveSig] int _Attr_GetAllocatedString(ref Guid guidKey, out IntPtr ppwszValue, out int pcchLength);
    [PreserveSig] int _Attr_GetBlobSize(ref Guid guidKey, out int pcbBlobSize);
    [PreserveSig] int _Attr_GetBlob(ref Guid guidKey, IntPtr pBuf, int cbBufSize, out int pcbBlobSize);
    [PreserveSig] int _Attr_GetAllocatedBlob(ref Guid guidKey, out IntPtr ppBuf, out int pcbSize);
    [PreserveSig] int _Attr_GetUnknown(ref Guid guidKey, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
    [PreserveSig] int _Attr_SetItem(ref Guid guidKey, IntPtr value);
    [PreserveSig] int _Attr_DeleteItem(ref Guid guidKey);
    [PreserveSig] int _Attr_DeleteAllItems();
    [PreserveSig] int _Attr_SetUINT32(ref Guid guidKey, int unValue);
    [PreserveSig] int _Attr_SetUINT64(ref Guid guidKey, ulong unValue);
    [PreserveSig] int _Attr_SetDouble(ref Guid guidKey, double fValue);
    [PreserveSig] int _Attr_SetGUID(ref Guid guidKey, ref Guid guidValue);
    [PreserveSig] int _Attr_SetString(ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] string wszValue);
    [PreserveSig] int _Attr_SetBlob(ref Guid guidKey, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBuf, int cbBufSize);
    [PreserveSig] int _Attr_SetUnknown(ref Guid guidKey, [MarshalAs(UnmanagedType.IUnknown)] object? pUnknown);
    [PreserveSig] int _Attr_LockStore();
    [PreserveSig] int _Attr_UnlockStore();
    [PreserveSig] int _Attr_GetCount(out int pcItems);
    [PreserveSig] int _Attr_GetItemByIndex(int unIndex, out Guid pguidKey, IntPtr pValue);
    [PreserveSig] int _Attr_CopyAllItems(IMFAttributes pDest);

    [PreserveSig]
    int GetSampleFlags(out int pdwSampleFlags);

    [PreserveSig]
    int SetSampleFlags(int dwSampleFlags);

    [PreserveSig]
    int GetSampleTime(out long phnsSampleTime);

    [PreserveSig]
    int SetSampleTime(long hnsSampleTime);

    [PreserveSig]
    int GetSampleDuration(out long phnsSampleDuration);

    [PreserveSig]
    int SetSampleDuration(long hnsSampleDuration);

    [PreserveSig]
    int GetBufferCount(out int pdwBufferCount);

    [PreserveSig]
    int GetBufferByIndex(int dwIndex, out IMFMediaBuffer ppBuffer);

    [PreserveSig]
    int ConvertToContiguousBuffer(out IMFMediaBuffer ppBuffer);

    [PreserveSig]
    int AddBuffer(IMFMediaBuffer pBuffer);

    [PreserveSig]
    int RemoveBufferByIndex(int dwIndex);

    [PreserveSig]
    int RemoveAllBuffers();

    [PreserveSig]
    int GetTotalLength(out int pcbTotalLength);

    [PreserveSig]
    int CopyToBuffer(IMFMediaBuffer pBuffer);
}

[ComImport]
[Guid("045FA593-8799-42b8-BC8D-8968C6453507")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFMediaBuffer
{
    [PreserveSig]
    int Lock(out IntPtr ppbBuffer, out int pcbMaxLength, out int pcbCurrentLength);

    [PreserveSig]
    int Unlock();

    [PreserveSig]
    int GetCurrentLength(out int pcbCurrentLength);

    [PreserveSig]
    int SetCurrentLength(int cbCurrentLength);

    [PreserveSig]
    int GetMaxLength(out int pcbMaxLength);
}

[ComImport]
[Guid("7DC9D5F9-9ED9-44EC-9BBF-0600BB589FBB")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMF2DBuffer
{
    [PreserveSig]
    int Lock2D(out IntPtr ppbScanline0, out int plPitch);

    [PreserveSig]
    int Unlock2D();

    [PreserveSig]
    int GetScanline0AndPitch(out IntPtr pbScanline0, out int plPitch);

    [PreserveSig]
    int IsContiguousFormat([MarshalAs(UnmanagedType.Bool)] out bool pfIsContiguous);

    [PreserveSig]
    int GetContiguousLength(out int pcbLength);

    [PreserveSig]
    int ContiguousCopyTo(IntPtr pbDestBuffer, int cbDestBuffer);

    [PreserveSig]
    int ContiguousCopyFrom(IntPtr pbSrcBuffer, int cbSrcBuffer);
}

[ComImport]
[Guid("e7174cfa-1c9e-48b1-8866-626226bfc258")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFDXGIBuffer
{
    [PreserveSig]
    int GetResource(ref Guid riid, out IntPtr ppvObject);

    [PreserveSig]
    int GetSubresourceIndex(out uint puSubresource);

    [PreserveSig]
    int GetUnknown(ref Guid guid, ref Guid riid, out IntPtr ppvObject);
}

public sealed partial class MfSourceReaderVideoCapture
{
    private static class MfInterop
    {
        [DllImport("mfplat.dll", ExactSpelling = true)]
        internal static extern int MFCreateAttributes(
            [MarshalAs(UnmanagedType.Interface)] out IMFAttributes ppMFAttributes,
            int cInitialSize);

        [DllImport("mf.dll", ExactSpelling = true)]
        internal static extern int MFEnumDeviceSources(
            [MarshalAs(UnmanagedType.Interface)] IMFAttributes pAttributes,
            out IntPtr pppSourceActivate,
            out int pcSourceActivate);

        [DllImport("mf.dll", ExactSpelling = true)]
        internal static extern int MFCreateDeviceSource(
            [MarshalAs(UnmanagedType.Interface)] IMFAttributes pAttributes,
            [MarshalAs(UnmanagedType.Interface)] out IMFMediaSource ppSource);

        [DllImport("mfreadwrite.dll", ExactSpelling = true)]
        internal static extern int MFCreateSourceReaderFromMediaSource(
            [MarshalAs(UnmanagedType.Interface)] IMFMediaSource pMediaSource,
            [MarshalAs(UnmanagedType.Interface)] IMFAttributes? pAttributes,
            [MarshalAs(UnmanagedType.Interface)] out IMFSourceReader ppSourceReader);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        internal static extern int MFCreateMediaType(
            [MarshalAs(UnmanagedType.Interface)] out IMFMediaType ppMFType);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        internal static extern int MFCreateDXGIDeviceManager(out uint pResetToken, out IntPtr ppDeviceManager);
    }

    private static class MfConstants
    {
        internal const int MF_SOURCE_READER_FIRST_VIDEO_STREAM = unchecked((int)0xFFFFFFFC);
        internal const int MF_SOURCE_READERF_ENDOFSTREAM = 0x00000002;
    }

    private static class MfHResults
    {
        internal const int MF_E_NO_MORE_TYPES = unchecked((int)0xC00D36B9);
        internal const int MF_E_INVALIDREQUEST = unchecked((int)0xC00D36B2);
        internal const int MF_E_SHUTDOWN = unchecked((int)0xC00D3E85);
    }

    private static class MfGuids
    {
        internal static Guid MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE = new(
            0xC60AC5FE, 0x252A, 0x478F, 0xA0, 0xEF, 0xBC, 0x8F, 0xA5, 0xF7, 0xCA, 0xD3);
        internal static Guid MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID = new(
            0x8AC3587A, 0x4AE7, 0x42D8, 0x99, 0xE0, 0x0A, 0x60, 0x13, 0xEE, 0xF9, 0x0F);
        internal static Guid MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK = new(
            0x58F0AAD8, 0x22BF, 0x4F8A, 0xBB, 0x3D, 0xD2, 0xC4, 0x97, 0x8C, 0x6E, 0x2F);
        internal static Guid MF_READWRITE_DISABLE_CONVERTERS = new(
            0x98D5B065, 0x1374, 0x4847, 0x8D, 0x5D, 0x31, 0x52, 0x0F, 0xEE, 0x71, 0x56);
        internal static Guid MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS = new(
            0xA634A91C, 0x822B, 0x41B9, 0xA4, 0x94, 0x4D, 0xE4, 0x64, 0x36, 0x12, 0xB0);
        internal static Guid MF_SOURCE_READER_D3D_MANAGER = new(
            0xEC822DA2, 0xE1E9, 0x4B29, 0xA0, 0xD8, 0x56, 0x3C, 0x71, 0x9F, 0x52, 0x69);
        internal static Guid MF_MT_MAJOR_TYPE = new(
            0x48EBA18E, 0xF8C9, 0x4687, 0xBF, 0x11, 0x0A, 0x74, 0xC9, 0xF9, 0x6A, 0x8F);
        internal static Guid MF_MT_SUBTYPE = new(
            0xF7E34C9A, 0x42E8, 0x4714, 0xB7, 0x4B, 0xCB, 0x29, 0xD7, 0x2C, 0x35, 0xE5);
        internal static Guid MF_MT_ALL_SAMPLES_INDEPENDENT = new(
            0xC9173739, 0x5E56, 0x461C, 0xB7, 0x13, 0x46, 0xFB, 0x99, 0x5C, 0xB9, 0x5F);
        internal static Guid MF_MT_FRAME_SIZE = new(
            0x1652C33D, 0xD6B2, 0x4012, 0xB8, 0x34, 0x72, 0x03, 0x08, 0x49, 0xA3, 0x7D);
        internal static Guid MF_MT_FRAME_RATE = new(
            0xC459A2E8, 0x3D2C, 0x4E44, 0xB1, 0x32, 0xFE, 0xE5, 0x15, 0x6C, 0x7B, 0xB0);
        internal static Guid MF_MT_FRAME_RATE_RANGE_MIN = new(
            0xD2E7558C, 0xDC1F, 0x403F, 0x9A, 0x72, 0xD2, 0x8B, 0xB1, 0xEB, 0x3B, 0x5E);
        internal static Guid MF_MT_FRAME_RATE_RANGE_MAX = new(
            0xE3371D41, 0xB4CF, 0x4A05, 0xBD, 0x4E, 0x20, 0xB8, 0x8B, 0xB2, 0xC4, 0xD6);
        internal static Guid MF_MT_PIXEL_ASPECT_RATIO = new(
            0xC6376A1E, 0x8D0A, 0x4027, 0xBE, 0x45, 0x6D, 0x9A, 0x0A, 0xD3, 0x9B, 0xB6);
        internal static Guid MF_MT_INTERLACE_MODE = new(
            0xE2724BB8, 0xE676, 0x4806, 0xB4, 0xB2, 0xA8, 0xD6, 0xEF, 0xB4, 0x4C, 0xCD);
        internal static Guid MFMediaType_Video = new(
            0x73646976, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
        internal static Guid MFVideoFormat_P010 = new(
            0x30313050, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
        internal static Guid MFVideoFormat_NV12 = new(
            0x3231564E, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
        internal static Guid MFVideoFormat_MJPG = new(
            0x47504A4D, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
    }
}
