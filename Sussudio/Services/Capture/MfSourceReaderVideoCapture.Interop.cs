using System;
using System.Runtime.InteropServices;

namespace Sussudio.Services.Capture;

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
