using System;
using System.Runtime.InteropServices;

static partial class EgavdsProbe
{
    private const string DLL = "EGAVDeviceSupport";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SwigExceptionDelegate(string message);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SwigExceptionArgDelegate(string message, string paramName);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate string SwigStringDelegate(string message);

    private static SwigExceptionDelegate _swigExcHandler = msg => Console.WriteLine($"SWIG Exception: {msg}");
    private static SwigExceptionArgDelegate _swigArgHandler = (msg, p) => Console.WriteLine($"SWIG Arg Exception: {msg} ({p})");
    private static SwigStringDelegate _swigStrHandler = s => s;

    private static void RegisterSwigCallbacks()
    {
        SWIGRegisterExceptionCallbacks_EGAVDS(
            _swigExcHandler, _swigExcHandler, _swigExcHandler, _swigExcHandler,
            _swigExcHandler, _swigExcHandler, _swigExcHandler, _swigExcHandler,
            _swigExcHandler, _swigExcHandler, _swigExcHandler);
        SWIGRegisterExceptionCallbacksArgument_EGAVDS(_swigArgHandler, _swigArgHandler, _swigArgHandler);
        SWIGRegisterStringCallback_EGAVDS(_swigStrHandler);
    }

    [DllImport(DLL)] private static extern void SWIGRegisterExceptionCallbacks_EGAVDS(
        SwigExceptionDelegate a1, SwigExceptionDelegate a2, SwigExceptionDelegate a3, SwigExceptionDelegate a4,
        SwigExceptionDelegate a5, SwigExceptionDelegate a6, SwigExceptionDelegate a7, SwigExceptionDelegate a8,
        SwigExceptionDelegate a9, SwigExceptionDelegate a10, SwigExceptionDelegate a11);

    [DllImport(DLL, EntryPoint = "SWIGRegisterExceptionArgumentCallbacks_EGAVDS")]
    private static extern void SWIGRegisterExceptionCallbacksArgument_EGAVDS(
        SwigExceptionArgDelegate a1, SwigExceptionArgDelegate a2, SwigExceptionArgDelegate a3);

    [DllImport(DLL)] private static extern void SWIGRegisterStringCallback_EGAVDS(SwigStringDelegate cb);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_new_EGAVDS_INITIALIZE_PARAMS___")]
    private static extern nint EGAVDS_new_INITIALIZE_PARAMS();

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_delete_EGAVDS_INITIALIZE_PARAMS___")]
    private static extern void EGAVDS_delete_INITIALIZE_PARAMS(HandleRef p);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_appDataDirectoryPath_set___")]
    private static extern void EGAVDS_INITIALIZE_PARAMS_appDataDirectoryPath_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_tmpDirectoryPath_set___")]
    private static extern void EGAVDS_INITIALIZE_PARAMS_tmpDirectoryPath_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_logDirectoryPath_set___")]
    private static extern void EGAVDS_INITIALIZE_PARAMS_logDirectoryPath_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_companyNameShort_set___")]
    private static extern void EGAVDS_INITIALIZE_PARAMS_companyNameShort_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_companyNameLong_set___")]
    private static extern void EGAVDS_INITIALIZE_PARAMS_companyNameLong_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_productNameShort_set___")]
    private static extern void EGAVDS_INITIALIZE_PARAMS_productNameShort_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_productNameLong_set___")]
    private static extern void EGAVDS_INITIALIZE_PARAMS_productNameLong_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_productClientName_set___")]
    private static extern void EGAVDS_INITIALIZE_PARAMS_productClientName_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_isDebug_set___")]
    private static extern void EGAVDS_INITIALIZE_PARAMS_isDebug_set(HandleRef p, bool v);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_edidDirectoryPath_set___")]
    private static extern void EGAVDS_INITIALIZE_PARAMS_edidDirectoryPath_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_firmwareDirectoryPath_set___")]
    private static extern void EGAVDS_INITIALIZE_PARAMS_firmwareDirectoryPath_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_Initialize___")]
    private static extern int EGAVDS_Initialize(HandleRef p);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_InitializeWithJSONFile___")]
    private static extern int EGAVDS_InitializeWithJSONFile([MarshalAs(UnmanagedType.LPUTF8Str)] string path, uint pathLen);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_Deinitialize___")]
    private static extern void EGAVDS_Deinitialize();

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_new_EGAVDS_DEVICE_HANDLE___")]
    private static extern nint EGAVDS_new_DEVICE_HANDLE();

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_delete_EGAVDS_DEVICE_HANDLE___")]
    private static extern void EGAVDS_delete_DEVICE_HANDLE(HandleRef h);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_OpenDevice___")]
    private static extern int EGAVDS_OpenDevice([MarshalAs(UnmanagedType.LPUTF8Str)] string deviceId, HandleRef h);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_CloseDevice___")]
    private static extern void EGAVDS_CloseDevice(HandleRef h);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_SupportsAudioInputSelection___")]
    private static extern bool EGAVDS_SupportsAudioInputSelection(HandleRef h);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_GetAudioInputSelection___")]
    private static extern int EGAVDS_GetAudioInputSelection(HandleRef h, ref int audioInput);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_SetAudioInputSelection___")]
    private static extern int EGAVDS_SetAudioInputSelection(HandleRef h, int audioInput);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_SupportsLineInAudioGainControl___")]
    private static extern bool EGAVDS_SupportsLineInAudioGainControl(HandleRef h);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_GetLineInAudioGain___")]
    private static extern int EGAVDS_GetLineInAudioGain(HandleRef h, ref long v);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_SetLineInAudioGain___")]
    private static extern int EGAVDS_SetLineInAudioGain(HandleRef h, long v);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_GetLineInAudioGainMin___")]
    private static extern int EGAVDS_GetLineInAudioGainMin(HandleRef h, ref long v);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_GetLineInAudioGainMax___")]
    private static extern int EGAVDS_GetLineInAudioGainMax(HandleRef h, ref long v);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_GetLineInAudioGainDefault___")]
    private static extern int EGAVDS_GetLineInAudioGainDefault(HandleRef h, ref long v);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_IsVideoHDR___")]
    private static extern int EGAVDS_IsVideoHDR(HandleRef h, ref bool isHdr);

    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_GetIsConnectionOK___")]
    private static extern int EGAVDS_GetIsConnectionOK(HandleRef h, ref bool isOk);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint SetupDiGetClassDevs(ref Guid classGuid, string? enumerator, nint hwndParent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInterfaces(nint devInfoSet, nint devInfoData, ref Guid interfaceClassGuid, uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(nint devInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, nint deviceInterfaceDetailData, uint deviceInterfaceDetailDataSize, out uint requiredSize, nint deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(nint devInfoSet);

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA
    {
        public int cbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public nuint Reserved;
    }
}
