// EgavdsAudioProbe: Calls EGAVDeviceSupport.dll from Elgato Studio to control audio input switching.
// Uses the SWIG-generated C# entry points directly via P/Invoke.
//
// Usage:
//   EgavdsAudioProbe.exe                    -- query current audio input
//   EgavdsAudioProbe.exe --set hdmi         -- switch to HDMI audio
//   EgavdsAudioProbe.exe --set analog       -- switch to Analog audio
//   EgavdsAudioProbe.exe --gain             -- query current gain + min/max/default
//   EgavdsAudioProbe.exe --set-gain <value> -- set gain value

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

return EgavdsProbe.Run(args);

static class EgavdsProbe
{
    const string DLL = "EGAVDeviceSupport";

    public static int Run(string[] args)
    {
        string? setMode = null;
        long? setGain = null;
        bool queryGain = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--set" && i + 1 < args.Length)
                setMode = args[++i].ToLowerInvariant();
            else if (args[i] == "--gain")
                queryGain = true;
            else if (args[i] == "--set-gain" && i + 1 < args.Length)
                setGain = long.Parse(args[++i]);
        }

        Console.WriteLine("=== EGAVDeviceSupport Audio Probe ===");
        Console.WriteLine();

        // Step 1: Find the device path using SetupDI
        string? devicePath = FindElgato4KXDevicePath();
        if (devicePath == null)
        {
            Console.WriteLine("ERROR: Could not find Elgato 4K X device path");
            return 1;
        }
        Console.WriteLine($"Device path: {devicePath}");

        // Step 2: Initialize EGAVDS
        Console.WriteLine("Registering SWIG callbacks...");
        RegisterSwigCallbacks();

        Console.WriteLine("Initializing EGAVDS...");
        nint initParams = EGAVDS_new_INITIALIZE_PARAMS();
        if (initParams == IntPtr.Zero)
        {
            Console.WriteLine("ERROR: Failed to create init params");
            return 1;
        }

        try
        {
            string appData = Path.Combine(Path.GetTempPath(), "EgavdsProbe");
            Directory.CreateDirectory(appData);
            string logDir = Path.Combine(appData, "logs");
            Directory.CreateDirectory(logDir);

            var initParamsRef = new HandleRef(null, initParams);
            EGAVDS_INITIALIZE_PARAMS_appDataDirectoryPath_set(initParamsRef, appData);
            EGAVDS_INITIALIZE_PARAMS_tmpDirectoryPath_set(initParamsRef, Path.GetTempPath());
            EGAVDS_INITIALIZE_PARAMS_logDirectoryPath_set(initParamsRef, logDir);
            EGAVDS_INITIALIZE_PARAMS_companyNameShort_set(initParamsRef, "Elgato");
            EGAVDS_INITIALIZE_PARAMS_companyNameLong_set(initParamsRef, "Elgato");
            EGAVDS_INITIALIZE_PARAMS_productNameShort_set(initParamsRef, "Probe");
            EGAVDS_INITIALIZE_PARAMS_productNameLong_set(initParamsRef, "EgavdsAudioProbe");
            EGAVDS_INITIALIZE_PARAMS_productClientName_set(initParamsRef, "EgavdsAudioProbe");
            EGAVDS_INITIALIZE_PARAMS_isDebug_set(initParamsRef, true);
            // Studio version also needs edid/firmware paths
            string edidDir = Path.Combine(appData, "edid");
            Directory.CreateDirectory(edidDir);
            string fwDir = Path.Combine(appData, "firmware");
            Directory.CreateDirectory(fwDir);
            EGAVDS_INITIALIZE_PARAMS_edidDirectoryPath_set(initParamsRef, edidDir);
            EGAVDS_INITIALIZE_PARAMS_firmwareDirectoryPath_set(initParamsRef, fwDir);

            int initResult = EGAVDS_Initialize(initParamsRef);
            Console.WriteLine($"EGAVDS_Initialize result: {initResult} ({Res(initResult)})");
            if (initResult != 0)
            {
                string studioCaps = @"C:\Program Files\WindowsApps\Elgato.Studio_1.0.5.895_x64__g54w8ztgkx496\ElgatoDeviceCapabilities.json";
                if (File.Exists(studioCaps))
                {
                    Console.WriteLine($"Trying InitializeWithJSONFile: {studioCaps}");
                    initResult = EGAVDS_InitializeWithJSONFile(studioCaps, (uint)studioCaps.Length);
                    Console.WriteLine($"InitializeWithJSONFile result: {initResult} ({Res(initResult)})");
                }
                if (initResult != 0)
                {
                    Console.WriteLine("ERROR: Initialization failed");
                    return 1;
                }
            }

            // Step 3: Open device
            nint handlePtr = EGAVDS_new_DEVICE_HANDLE();
            var handleRef = new HandleRef(null, handlePtr);
            Console.WriteLine($"Opening device: {devicePath}");
            int openResult = EGAVDS_OpenDevice(devicePath, handleRef);
            Console.WriteLine($"OpenDevice result: {openResult} ({Res(openResult)})");

            if (openResult != 0)
            {
                Console.WriteLine("ERROR: Failed to open device");
                EGAVDS_delete_DEVICE_HANDLE(handleRef);
                return 1;
            }

            try
            {
                // Step 4: Check audio input support
                bool supportsAudio = EGAVDS_SupportsAudioInputSelection(handleRef);
                Console.WriteLine($"SupportsAudioInputSelection: {supportsAudio}");

                if (supportsAudio)
                {
                    int audioInput = 0;
                    int getResult = EGAVDS_GetAudioInputSelection(handleRef, ref audioInput);
                    Console.WriteLine($"GetAudioInputSelection result: {getResult} ({Res(getResult)})");
                    Console.WriteLine($"Current audio input: {audioInput} ({Inp(audioInput)})");

                    if (setMode != null)
                    {
                        int targetInput = setMode switch
                        {
                            "hdmi" => 1,
                            "analog" => 2,
                            _ => throw new ArgumentException($"Unknown mode: {setMode}. Use 'hdmi' or 'analog'.")
                        };

                        Console.WriteLine($"\nSetting audio input to: {targetInput} ({Inp(targetInput)})");
                        int setResult = EGAVDS_SetAudioInputSelection(handleRef, targetInput);
                        Console.WriteLine($"SetAudioInputSelection result: {setResult} ({Res(setResult)})");

                        Thread.Sleep(500);
                        audioInput = 0;
                        getResult = EGAVDS_GetAudioInputSelection(handleRef, ref audioInput);
                        Console.WriteLine($"After set - audio input: {audioInput} ({Inp(audioInput)}), result: {getResult}");
                    }
                }

                // Step 5: Check line-in gain support
                bool supportsGain = EGAVDS_SupportsLineInAudioGainControl(handleRef);
                Console.WriteLine($"\nSupportsLineInAudioGainControl: {supportsGain}");

                if (supportsGain && (queryGain || setGain.HasValue))
                {
                    long gainValue = 0, gainMin = 0, gainMax = 0, gainDefault = 0;
                    EGAVDS_GetLineInAudioGain(handleRef, ref gainValue);
                    EGAVDS_GetLineInAudioGainMin(handleRef, ref gainMin);
                    EGAVDS_GetLineInAudioGainMax(handleRef, ref gainMax);
                    EGAVDS_GetLineInAudioGainDefault(handleRef, ref gainDefault);
                    Console.WriteLine($"Gain: current={gainValue}, min={gainMin}, max={gainMax}, default={gainDefault}");

                    if (setGain.HasValue)
                    {
                        Console.WriteLine($"Setting gain to: {setGain.Value}");
                        int setResult = EGAVDS_SetLineInAudioGain(handleRef, setGain.Value);
                        Console.WriteLine($"SetLineInAudioGain result: {setResult} ({Res(setResult)})");
                    }
                }

                Console.WriteLine("\n--- Additional device info ---");
                bool isHdr = false;
                int hdrResult = EGAVDS_IsVideoHDR(handleRef, ref isHdr);
                Console.WriteLine($"IsVideoHDR: {isHdr} (result: {hdrResult})");

                bool isConnOk = false;
                int connResult = EGAVDS_GetIsConnectionOK(handleRef, ref isConnOk);
                Console.WriteLine($"IsConnectionOK: {isConnOk} (result: {connResult})");
            }
            finally
            {
                Console.WriteLine("\nClosing device...");
                EGAVDS_CloseDevice(handleRef);
                EGAVDS_delete_DEVICE_HANDLE(handleRef);
            }
        }
        finally
        {
            Console.WriteLine("Deinitializing EGAVDS...");
            EGAVDS_Deinitialize();
            EGAVDS_delete_INITIALIZE_PARAMS(new HandleRef(null, initParams));
        }

        Console.WriteLine("Done.");
        return 0;
    }

    static string Inp(int v) => v switch { 0 => "Invalid", 1 => "HDMI", 2 => "Analog", _ => $"Unknown({v})" };
    static string Res(int v) => v == 0 ? "Success" : $"Error({v})";

    // --- Device enumeration via SetupDI ---
    static string? FindElgato4KXDevicePath()
    {
        Guid[] guids =
        [
            new("e5323777-f976-4f5b-9b55-b94699c46e44"), // KSCATEGORY_VIDEO_CAMERA
            new("65E8773D-8F56-11D0-A3B9-00A0C9223196"), // KSCATEGORY_CAPTURE
            new("6994AD05-93EF-11D0-A3CC-00A0C9223196"), // KSCATEGORY_VIDEO
        ];

        foreach (var guid in guids)
        {
            var guidCopy = guid;
            nint devInfo = SetupDiGetClassDevs(ref guidCopy, null, IntPtr.Zero, 0x12);
            if (devInfo == new IntPtr(-1)) continue;

            try
            {
                var did = new SP_DEVICE_INTERFACE_DATA();
                did.cbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>();

                for (uint i = 0; SetupDiEnumDeviceInterfaces(devInfo, IntPtr.Zero, ref guidCopy, i, ref did); i++)
                {
                    SetupDiGetDeviceInterfaceDetail(devInfo, ref did, IntPtr.Zero, 0, out uint requiredSize, IntPtr.Zero);
                    nint detailData = Marshal.AllocHGlobal((int)requiredSize);
                    try
                    {
                        Marshal.WriteInt32(detailData, 8); // SP_DEVICE_INTERFACE_DETAIL_DATA cbSize on x64
                        if (SetupDiGetDeviceInterfaceDetail(devInfo, ref did, detailData, requiredSize, out _, IntPtr.Zero))
                        {
                            string path = Marshal.PtrToStringUni(detailData + 4)!;
                            if (path.Contains("vid_0fd9", StringComparison.OrdinalIgnoreCase) &&
                                path.Contains("pid_009b", StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine($"Found 4K X via {guid}: {path}");
                                return path;
                            }
                        }
                    }
                    finally { Marshal.FreeHGlobal(detailData); }
                }
            }
            finally { SetupDiDestroyDeviceInfoList(devInfo); }
        }
        return null;
    }

    // --- SWIG callback registration ---
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void SwigExceptionDelegate(string message);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void SwigExceptionArgDelegate(string message, string paramName);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate string SwigStringDelegate(string message);

    static SwigExceptionDelegate _swigExcHandler = msg => Console.WriteLine($"SWIG Exception: {msg}");
    static SwigExceptionArgDelegate _swigArgHandler = (msg, p) => Console.WriteLine($"SWIG Arg Exception: {msg} ({p})");
    static SwigStringDelegate _swigStrHandler = s => s;

    static void RegisterSwigCallbacks()
    {
        SWIGRegisterExceptionCallbacks_EGAVDS(
            _swigExcHandler, _swigExcHandler, _swigExcHandler, _swigExcHandler,
            _swigExcHandler, _swigExcHandler, _swigExcHandler, _swigExcHandler,
            _swigExcHandler, _swigExcHandler, _swigExcHandler);
        SWIGRegisterExceptionCallbacksArgument_EGAVDS(_swigArgHandler, _swigArgHandler, _swigArgHandler);
        SWIGRegisterStringCallback_EGAVDS(_swigStrHandler);
    }

    // ======================================================================
    // P/Invoke declarations — all target EGAVDeviceSupport.dll from Elgato Studio
    // ======================================================================

    // SWIG infrastructure
    [DllImport(DLL)] static extern void SWIGRegisterExceptionCallbacks_EGAVDS(
        SwigExceptionDelegate a1, SwigExceptionDelegate a2, SwigExceptionDelegate a3, SwigExceptionDelegate a4,
        SwigExceptionDelegate a5, SwigExceptionDelegate a6, SwigExceptionDelegate a7, SwigExceptionDelegate a8,
        SwigExceptionDelegate a9, SwigExceptionDelegate a10, SwigExceptionDelegate a11);

    [DllImport(DLL, EntryPoint = "SWIGRegisterExceptionArgumentCallbacks_EGAVDS")]
    static extern void SWIGRegisterExceptionCallbacksArgument_EGAVDS(
        SwigExceptionArgDelegate a1, SwigExceptionArgDelegate a2, SwigExceptionArgDelegate a3);

    [DllImport(DLL)] static extern void SWIGRegisterStringCallback_EGAVDS(SwigStringDelegate cb);

    // Lifecycle
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_new_EGAVDS_INITIALIZE_PARAMS___")]
    static extern nint EGAVDS_new_INITIALIZE_PARAMS();
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_delete_EGAVDS_INITIALIZE_PARAMS___")]
    static extern void EGAVDS_delete_INITIALIZE_PARAMS(HandleRef p);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_appDataDirectoryPath_set___")]
    static extern void EGAVDS_INITIALIZE_PARAMS_appDataDirectoryPath_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_tmpDirectoryPath_set___")]
    static extern void EGAVDS_INITIALIZE_PARAMS_tmpDirectoryPath_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_logDirectoryPath_set___")]
    static extern void EGAVDS_INITIALIZE_PARAMS_logDirectoryPath_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_companyNameShort_set___")]
    static extern void EGAVDS_INITIALIZE_PARAMS_companyNameShort_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_companyNameLong_set___")]
    static extern void EGAVDS_INITIALIZE_PARAMS_companyNameLong_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_productNameShort_set___")]
    static extern void EGAVDS_INITIALIZE_PARAMS_productNameShort_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_productNameLong_set___")]
    static extern void EGAVDS_INITIALIZE_PARAMS_productNameLong_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_productClientName_set___")]
    static extern void EGAVDS_INITIALIZE_PARAMS_productClientName_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_isDebug_set___")]
    static extern void EGAVDS_INITIALIZE_PARAMS_isDebug_set(HandleRef p, bool v);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_edidDirectoryPath_set___")]
    static extern void EGAVDS_INITIALIZE_PARAMS_edidDirectoryPath_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_INITIALIZE_PARAMS_firmwareDirectoryPath_set___")]
    static extern void EGAVDS_INITIALIZE_PARAMS_firmwareDirectoryPath_set(HandleRef p, [MarshalAs(UnmanagedType.LPUTF8Str)] string v);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_Initialize___")]
    static extern int EGAVDS_Initialize(HandleRef p);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_InitializeWithJSONFile___")]
    static extern int EGAVDS_InitializeWithJSONFile([MarshalAs(UnmanagedType.LPUTF8Str)] string path, uint pathLen);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_EGAVDS_Deinitialize___")]
    static extern void EGAVDS_Deinitialize();

    // Device handle
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_new_EGAVDS_DEVICE_HANDLE___")]
    static extern nint EGAVDS_new_DEVICE_HANDLE();
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_delete_EGAVDS_DEVICE_HANDLE___")]
    static extern void EGAVDS_delete_DEVICE_HANDLE(HandleRef h);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_OpenDevice___")]
    static extern int EGAVDS_OpenDevice([MarshalAs(UnmanagedType.LPUTF8Str)] string deviceId, HandleRef h);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_CloseDevice___")]
    static extern void EGAVDS_CloseDevice(HandleRef h);

    // Audio input
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_SupportsAudioInputSelection___")]
    static extern bool EGAVDS_SupportsAudioInputSelection(HandleRef h);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_GetAudioInputSelection___")]
    static extern int EGAVDS_GetAudioInputSelection(HandleRef h, ref int audioInput);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_SetAudioInputSelection___")]
    static extern int EGAVDS_SetAudioInputSelection(HandleRef h, int audioInput);

    // Line-in gain
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_SupportsLineInAudioGainControl___")]
    static extern bool EGAVDS_SupportsLineInAudioGainControl(HandleRef h);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_GetLineInAudioGain___")]
    static extern int EGAVDS_GetLineInAudioGain(HandleRef h, ref long v);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_SetLineInAudioGain___")]
    static extern int EGAVDS_SetLineInAudioGain(HandleRef h, long v);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_GetLineInAudioGainMin___")]
    static extern int EGAVDS_GetLineInAudioGainMin(HandleRef h, ref long v);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_GetLineInAudioGainMax___")]
    static extern int EGAVDS_GetLineInAudioGainMax(HandleRef h, ref long v);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_GetLineInAudioGainDefault___")]
    static extern int EGAVDS_GetLineInAudioGainDefault(HandleRef h, ref long v);

    // Misc
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_IsVideoHDR___")]
    static extern int EGAVDS_IsVideoHDR(HandleRef h, ref bool isHdr);
    [DllImport(DLL, EntryPoint = "CSharp_ElgatofEGAVDeviceSupport_GetIsConnectionOK___")]
    static extern int EGAVDS_GetIsConnectionOK(HandleRef h, ref bool isOk);

    // SetupAPI
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern nint SetupDiGetClassDevs(ref Guid classGuid, string? enumerator, nint hwndParent, uint flags);
    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiEnumDeviceInterfaces(nint devInfoSet, nint devInfoData, ref Guid interfaceClassGuid, uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool SetupDiGetDeviceInterfaceDetail(nint devInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, nint deviceInterfaceDetailData, uint deviceInterfaceDetailDataSize, out uint requiredSize, nint deviceInfoData);
    [DllImport("setupapi.dll", SetLastError = true)]
    static extern bool SetupDiDestroyDeviceInfoList(nint devInfoSet);
}

[StructLayout(LayoutKind.Sequential)]
struct SP_DEVICE_INTERFACE_DATA
{
    public int cbSize;
    public Guid InterfaceClassGuid;
    public uint Flags;
    public nuint Reserved;
}
