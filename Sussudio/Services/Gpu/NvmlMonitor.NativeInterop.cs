using System;
using System.Runtime.InteropServices;

namespace Sussudio.Services.Gpu;

public sealed partial class NvmlMonitor
{
    private const uint NVML_SUCCESS = 0;
    private const uint NVML_PCIE_UTIL_TX_BYTES = 0;
    private const uint NVML_PCIE_UTIL_RX_BYTES = 1;
    private const uint NVML_TEMPERATURE_GPU = 0;
    private const uint NVML_CLOCK_GRAPHICS = 0;
    private const uint NVML_CLOCK_MEM = 2;

    private static bool TryLoadNativeLibrary()
        => NativeLibrary.TryLoad("nvml.dll", out _);

    private static unsafe string? GetDeviceName(IntPtr device)
    {
        var buffer = stackalloc byte[96];
        return nvmlDeviceGetName(device, buffer, 96) == NVML_SUCCESS
            ? Marshal.PtrToStringAnsi((IntPtr)buffer)
            : null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NvmlUtilization
    {
        public uint gpu;
        public uint memory;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NvmlMemory_v2
    {
        public uint version;
        public ulong total;
        public ulong reserved;
        public ulong free;
        public ulong used;

        public static uint StructVersion =>
            (uint)(Marshal.SizeOf<NvmlMemory_v2>() | (2 << 24));
    }

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nvmlInit_v2();

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nvmlShutdown();

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nvmlDeviceGetUtilizationRates(IntPtr device, out NvmlUtilization utilization);

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nvmlDeviceGetDecoderUtilization(IntPtr device, out uint utilization, out uint samplingPeriodUs);

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nvmlDeviceGetEncoderUtilization(IntPtr device, out uint utilization, out uint samplingPeriodUs);

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nvmlDeviceGetPcieThroughput(IntPtr device, uint counter, out uint value);

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nvmlDeviceGetMemoryInfo_v2(IntPtr device, ref NvmlMemory_v2 memory);

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nvmlDeviceGetTemperature(IntPtr device, uint sensorType, out uint temp);

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nvmlDeviceGetPowerUsage(IntPtr device, out uint power);

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint nvmlDeviceGetClockInfo(IntPtr device, uint clockType, out uint clock);

    [DllImport("nvml.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe uint nvmlDeviceGetName(IntPtr device, byte* name, uint length);
}
