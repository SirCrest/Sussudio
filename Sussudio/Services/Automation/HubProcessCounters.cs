using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sussudio.Services.Automation;

// Current-process CPU time and memory for the diagnostics hub snapshot: two kernel32 calls on the current-process
// pseudo-handle. The hub used to call Process.Refresh() and then WorkingSet64, PrivateMemorySize64 and
// TotalProcessorTime, which re-enumerates every process on the machine through NtQuerySystemInformation. That was about
// 21 ms and 14 KB per 500 ms tick, 84% of a refresh (artifacts/perf-overhaul/hub-cost). The counters are the same ones:
// WorkingSetSize is Process.WorkingSet64, PrivateUsage is Process.PrivateMemorySize64, and kernel plus user time in
// 100 ns ticks is Process.TotalProcessorTime. ProcessSensors.Capture is the full sensor read (threads, DWM, display);
// the hub needs only these three numbers every tick, so it keeps this narrow reader instead of calling that one.
internal static class HubProcessCounters
{
    public const double TicksPerMillisecond = 10_000.0;

    private static readonly IntPtr CurrentProcessPseudoHandle = new(-1);
    private static readonly uint MemoryCountersSize = (uint)Marshal.SizeOf<ProcessMemoryCountersEx>();
    private static int _failureLogged;

    // Valid is false when either call failed; the byte and tick fields are then zero. The hub publishes its last good
    // reading in place of a failed one (AutomationDiagnosticsHub.CaptureProcessResourceSnapshot).
    public readonly record struct Reading(bool Valid, long WorkingSetBytes, long PrivateBytes, long CpuTicks);

    public static Reading Read()
    {
        var memory = new ProcessMemoryCountersEx { Size = MemoryCountersSize };
        if (!K32GetProcessMemoryInfo(CurrentProcessPseudoHandle, ref memory, MemoryCountersSize))
        {
            return Failed(nameof(K32GetProcessMemoryInfo));
        }

        if (!GetProcessTimes(CurrentProcessPseudoHandle, out _, out _, out var kernel, out var user))
        {
            return Failed(nameof(GetProcessTimes));
        }

        return new Reading(true, (long)memory.WorkingSetSize, (long)memory.PrivateUsage, kernel + user);
    }

    // A persistent failure would repeat every tick, so only the first one is logged.
    private static Reading Failed(string call)
    {
        if (Interlocked.Exchange(ref _failureLogged, 1) == 0)
        {
            Logger.Log($"DIAGHUB_PROCESS_COUNTERS_FAILED call={call} win32={Marshal.GetLastWin32Error()}");
        }

        return default;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessMemoryCountersEx
    {
        public uint Size;
        public uint PageFaultCount;
        public nuint PeakWorkingSetSize;
        public nuint WorkingSetSize;
        public nuint QuotaPeakPagedPoolUsage;
        public nuint QuotaPagedPoolUsage;
        public nuint QuotaPeakNonPagedPoolUsage;
        public nuint QuotaNonPagedPoolUsage;
        public nuint PagefileUsage;
        public nuint PeakPagefileUsage;
        public nuint PrivateUsage;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessTimes(IntPtr process, out long creationTime, out long exitTime, out long kernelTime, out long userTime);

    [DllImport("kernel32.dll", EntryPoint = "K32GetProcessMemoryInfo", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool K32GetProcessMemoryInfo(IntPtr process, ref ProcessMemoryCountersEx counters, uint size);
}
