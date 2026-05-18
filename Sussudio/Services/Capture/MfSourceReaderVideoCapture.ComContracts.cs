using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Sussudio.Services.Capture;

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
