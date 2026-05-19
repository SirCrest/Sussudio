using System;
using System.Threading.Tasks;

static partial class Program
{
    private static Task NvmlSnapshot_ComputedProperties_ConvertUnits()
    {
        var snapshotType = RequireType("Sussudio.Services.Gpu.NvmlSnapshot");
        // Constructor: GpuName, GpuUtil%, MemUtil%, NvdecUtil%, NvencUtil%, PcieTxKB, PcieRxKB,
        //              VramUsedB, VramTotalB, TempC, PowerMw, ClockMHz, MemClockMHz
        var snapshot = Activator.CreateInstance(snapshotType,
            "RTX 4090",        // GpuName
            (uint?)85,         // GpuUtilizationPercent
            (uint?)40,         // GpuMemoryUtilizationPercent
            (uint?)50,         // NvdecUtilizationPercent
            (uint?)75,         // NvencUtilizationPercent
            (uint?)1024,       // PcieTxKBps (1024 KB/s = 1.0 MB/s)
            (uint?)2048,       // PcieRxKBps (2048 KB/s = 2.0 MB/s)
            (ulong?)2147483648,// VramUsedBytes (2 GB)
            (ulong?)25769803776,// VramTotalBytes (24 GB)
            (uint?)65,         // GpuTemperatureC
            (uint?)350000,     // GpuPowerMilliwatts (350W)
            (uint?)2520,       // GpuClockMHz
            (uint?)10501)!;    // GpuMemClockMHz

        // GpuPowerW = 350000 / 1000 = 350.0
        var powerW = GetPropertyValue(snapshot, "GpuPowerW");
        AssertEqual(350.0, (double)powerW!, "GpuPowerW");

        // PcieTxMBps = 1024 / 1024 = 1.0
        var txMB = GetPropertyValue(snapshot, "PcieTxMBps");
        AssertEqual(1.0, (double)txMB!, "PcieTxMBps");

        // PcieRxMBps = 2048 / 1024 = 2.0
        var rxMB = GetPropertyValue(snapshot, "PcieRxMBps");
        AssertEqual(2.0, (double)rxMB!, "PcieRxMBps");

        // VramUsedMB = 2147483648 / (1024*1024) = 2048
        var usedMB = GetPropertyValue(snapshot, "VramUsedMB");
        AssertEqual(2048UL, (ulong)usedMB!, "VramUsedMB");

        return Task.CompletedTask;
    }

    private static Task NvmlMonitor_NativeInteropLivesInFocusedPartial()
    {
        var monitorText = ReadRepoFile("Sussudio/Services/Gpu/NvmlMonitor.cs");
        var nativeInteropText = ReadRepoFile("Sussudio/Services/Gpu/NvmlMonitor.NativeInterop.cs");

        AssertContains(monitorText, "public sealed partial class NvmlMonitor : IDisposable");
        AssertContains(monitorText, "private void Poll(object? state)");
        AssertContains(monitorText, "public NvmlSnapshot? GetLatestSnapshot()");
        AssertContains(monitorText, "TryLoadNativeLibrary()");
        AssertDoesNotContain(monitorText, "[DllImport(\"nvml.dll\"");
        AssertDoesNotContain(monitorText, "private struct NvmlUtilization");

        AssertContains(nativeInteropText, "public sealed partial class NvmlMonitor");
        AssertContains(nativeInteropText, "private static bool TryLoadNativeLibrary()");
        AssertContains(nativeInteropText, "private static unsafe string? GetDeviceName(IntPtr device)");
        AssertContains(nativeInteropText, "private struct NvmlUtilization");
        AssertContains(nativeInteropText, "[DllImport(\"nvml.dll\"");

        return Task.CompletedTask;
    }
}
