using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Sussudio.Services.Capture;

/// <summary>
/// Flattened IMFSample COM interface — does NOT use C# interface inheritance.
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
    // ── IMFAttributes vtable slots 3–32 (30 methods) ──
    // These placeholders reserve the correct vtable positions.
    // Never called through this interface — use IMFAttributes directly for attribute access.
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

    // ── IMFSample vtable slots 33–46 (14 methods) ──
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
