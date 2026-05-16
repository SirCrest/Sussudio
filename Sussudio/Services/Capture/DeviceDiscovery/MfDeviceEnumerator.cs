using System;
using System.Runtime.InteropServices;

namespace Sussudio.Services.Capture;

// Thin Media Foundation device/format enumerator. It owns MFStartup/MFShutdown
// ref-counting and exposes only the capture-device data the managed app needs.
internal static partial class MfDeviceEnumerator
{
    private const int MfSourceReaderFirstVideoStream = unchecked((int)0xFFFFFFFC);
    private const int MfENoMoreTypes = unchecked((int)0xC00D36B9);

    private static Guid DevSourceAttributeSourceType = new(
        0xC60AC5FE, 0x252A, 0x478F, 0xA0, 0xEF, 0xBC, 0x8F, 0xA5, 0xF7, 0xCA, 0xD3);
    private static Guid DevSourceAttributeSourceTypeVidcapGuid = new(
        0x8AC3587A, 0x4AE7, 0x42D8, 0x99, 0xE0, 0x0A, 0x60, 0x13, 0xEE, 0xF9, 0x0F);
    private static Guid DevSourceAttributeFriendlyName = new(
        0x60D0E559, 0x52F8, 0x4FA2, 0xBB, 0xCE, 0xAC, 0xDB, 0x34, 0xA8, 0xEC, 0x01);
    private static Guid DevSourceAttributeSourceTypeVidcapSymbolicLink = new(
        0x58F0AAD8, 0x22BF, 0x4F8A, 0xBB, 0x3D, 0xD2, 0xC4, 0x97, 0x8C, 0x6E, 0x2F);
    private static Guid MfReadwriteDisableConverters = new(
        0x98D5B065, 0x1374, 0x4847, 0x8D, 0x5D, 0x31, 0x52, 0x0F, 0xEE, 0x71, 0x56);
    private static Guid MfMtSubtype = new(
        0xF7E34C9A, 0x42E8, 0x4714, 0xB7, 0x4B, 0xCB, 0x29, 0xD7, 0x2C, 0x35, 0xE5);
    private static Guid MfMtFrameSize = new(
        0x1652C33D, 0xD6B2, 0x4012, 0xB8, 0x34, 0x72, 0x03, 0x08, 0x49, 0xA3, 0x7D);
    private static Guid MfMtFrameRate = new(
        0xC459A2E8, 0x3D2C, 0x4E44, 0xB1, 0x32, 0xFE, 0xE5, 0x15, 0x6C, 0x7B, 0xB0);
    private static Guid MfVideoFormatP010 = new(
        0x30313050, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
    private static Guid MfVideoFormatNv12 = new(
        0x3231564E, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
    private static Guid MfVideoFormatYuy2 = new(
        0x32595559, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
    private static Guid MfVideoFormatUyvy = new(
        0x59565955, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
    private static Guid MfVideoFormatMjpg = new(
        0x47504A4D, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

    internal sealed record MfVideoDeviceInfo(string Name, string SymbolicLink);

    [DllImport("mfplat.dll", ExactSpelling = true)]
    private static extern int MFCreateAttributes(
        [MarshalAs(UnmanagedType.Interface)] out IMFAttributes ppMFAttributes,
        int cInitialSize);

    [DllImport("mf.dll", ExactSpelling = true)]
    private static extern int MFEnumDeviceSources(
        [MarshalAs(UnmanagedType.Interface)] IMFAttributes pAttributes,
        out IntPtr pppSourceActivate,
        out int pcSourceActivate);

    [DllImport("mf.dll", ExactSpelling = true)]
    private static extern int MFCreateDeviceSource(
        [MarshalAs(UnmanagedType.Interface)] IMFAttributes pAttributes,
        [MarshalAs(UnmanagedType.Interface)] out IMFMediaSource ppSource);

    [DllImport("mfreadwrite.dll", ExactSpelling = true)]
    private static extern int MFCreateSourceReaderFromMediaSource(
        [MarshalAs(UnmanagedType.Interface)] IMFMediaSource pMediaSource,
        [MarshalAs(UnmanagedType.Interface)] IMFAttributes? pAttributes,
        [MarshalAs(UnmanagedType.Interface)] out IMFSourceReader ppSourceReader);

}
