using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sussudio.Services.Performance;

/// <summary>
/// Process, runtime and compositor totals at one instant. Every cumulative field is a running total, so a window is
/// the difference of two samples. <see cref="DwmFramesComposed"/> and <see cref="DwmRefreshHz"/> are null when the
/// compositor timing call fails.
/// </summary>
public sealed record PerfSystemSample(
    long Timestamp,
    DateTime CapturedUtc,
    long ProcessCpuTicks,
    long PrivateBytes,
    long WorkingSetBytes,
    long IoReadBytes,
    long IoWriteBytes,
    int Handles,
    int ThreadCount,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    double GcPauseMs,
    long GcAllocatedBytes,
    long? DwmFramesComposed,
    double? DwmRefreshHz,
    IReadOnlyList<HotThreadSample> ThreadSamples);

/// <summary>
/// Always-available process sensors: CPU time, memory, I/O, handle and thread counts, GC totals, per-GC pause (from a
/// runtime EventListener that records <c>gc.pause</c> and a trace event), and DWM composition timing. Nothing here
/// runs on a timer: <see cref="Capture"/> reads everything on demand, and the only background work is the GC event
/// listener, which wakes once per collection. Slice-two sensors (PDH GPU engines, VRAM, UI responsiveness probe,
/// NVML) are demand-driven and live elsewhere.
/// </summary>
public static class ProcessSensors
{
    private const string RuntimeEventSourceName = "Microsoft-Windows-DotNETRuntime";
    private const EventKeywords GcKeyword = (EventKeywords)0x1;
    private const int GcSuspendEeBeginEventId = 9;
    private const int GcRestartEeEndEventId = 3;
    private const long DotNetTicksPerSecond = TimeSpan.TicksPerSecond;

    private static readonly IntPtr CurrentProcessPseudoHandle = new(-1);
    private static readonly object StartGate = new();
    private static GcPauseListener? _gcListener;
    private static PerfSystemSample? _processStartBaseline;
    private static long _gcSuspendUtcTicks;

    /// <summary>Starts the GC pause listener and captures the process-start baseline. Idempotent and never throws.</summary>
    public static void EnsureStarted()
    {
        lock (StartGate)
        {
            _ = ProcessStartBaseline;
            if (_gcListener is not null)
            {
                return;
            }

            try
            {
                _gcListener = new GcPauseListener();
            }
            catch (Exception ex)
            {
                Logger.Log($"PROCESS_SENSORS_GC_LISTENER_FAILED type={ex.GetType().Name} message={ex.Message}");
            }
        }
    }

    /// <summary>
    /// The totals a lifetime report subtracts: zero for every cumulative field, timestamped at process creation.
    /// DWM frames start from the first reading this process took.
    /// </summary>
    public static PerfSystemSample ProcessStartBaseline
    {
        get
        {
            var baseline = Volatile.Read(ref _processStartBaseline);
            if (baseline is not null)
            {
                return baseline;
            }

            var now = Stopwatch.GetTimestamp();
            var nowUtc = DateTime.UtcNow;
            var startUtc = nowUtc;
            try
            {
                using var process = Process.GetCurrentProcess();
                startUtc = process.StartTime.ToUniversalTime();
            }
            catch (Exception)
            {
                // Falls back to "now": the lifetime report then covers only the time since the first sensor read.
            }

            var age = nowUtc - startUtc;
            var created = new PerfSystemSample(
                now - (long)(age.TotalSeconds * Stopwatch.Frequency),
                startUtc,
                ProcessCpuTicks: 0,
                PrivateBytes: 0,
                WorkingSetBytes: 0,
                IoReadBytes: 0,
                IoWriteBytes: 0,
                Handles: 0,
                ThreadCount: 0,
                Gen0Collections: 0,
                Gen1Collections: 0,
                Gen2Collections: 0,
                GcPauseMs: 0,
                GcAllocatedBytes: 0,
                DwmFramesComposed: ReadDwm(out _),
                DwmRefreshHz: null,
                ThreadSamples: Array.Empty<HotThreadSample>());
            return Interlocked.CompareExchange(ref _processStartBaseline, created, null) ?? created;
        }
    }

    /// <summary>Reads every sensor now. Control plane only: a handful of syscalls plus a thread enumeration.</summary>
    public static PerfSystemSample Capture()
    {
        var timestamp = Stopwatch.GetTimestamp();
        var capturedUtc = DateTime.UtcNow;

        long cpuTicks = 0;
        if (GetProcessTimes(CurrentProcessPseudoHandle, out _, out _, out var kernel, out var user))
        {
            cpuTicks = kernel + user;
        }

        long privateBytes = 0;
        long workingSet = 0;
        var memory = new ProcessMemoryCountersEx { Size = (uint)Marshal.SizeOf<ProcessMemoryCountersEx>() };
        if (K32GetProcessMemoryInfo(CurrentProcessPseudoHandle, ref memory, memory.Size))
        {
            privateBytes = (long)memory.PrivateUsage;
            workingSet = (long)memory.WorkingSetSize;
        }

        long ioRead = 0;
        long ioWrite = 0;
        if (GetProcessIoCounters(CurrentProcessPseudoHandle, out var io))
        {
            ioRead = (long)io.ReadTransferCount;
            ioWrite = (long)io.WriteTransferCount;
        }

        var handles = GetProcessHandleCount(CurrentProcessPseudoHandle, out var handleCount) ? (int)handleCount : 0;
        var threads = 0;
        try
        {
            using var process = Process.GetCurrentProcess();
            threads = process.Threads.Count;
        }
        catch (Exception)
        {
            // Thread count is informational; a failed enumeration reports zero.
        }

        var dwmFrames = ReadDwm(out var refreshHz);
        return new PerfSystemSample(
            timestamp,
            capturedUtc,
            cpuTicks,
            privateBytes,
            workingSet,
            ioRead,
            ioWrite,
            handles,
            threads,
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2),
            GC.GetTotalPauseDuration().TotalMilliseconds,
            GC.GetTotalAllocatedBytes(precise: false),
            dwmFrames,
            refreshHz,
            HotThreads.Sample());
    }

    private static long? ReadDwm(out double? refreshHz)
    {
        refreshHz = null;
        var info = new DwmTimingInfo { Size = (uint)Marshal.SizeOf<DwmTimingInfo>() };
        try
        {
            if (DwmGetCompositionTimingInfo(IntPtr.Zero, ref info) != 0)
            {
                return null;
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return null;
        }

        if (info.RateRefresh.Denominator != 0)
        {
            refreshHz = (double)info.RateRefresh.Numerator / info.RateRefresh.Denominator;
        }

        return (long)info.FrameNumber;
    }

    private static void OnGcRuntimeEvent(EventWrittenEventArgs e)
    {
        if (e.EventId == GcSuspendEeBeginEventId)
        {
            Volatile.Write(ref _gcSuspendUtcTicks, e.TimeStamp.ToUniversalTime().Ticks);
            return;
        }

        if (e.EventId != GcRestartEeEndEventId)
        {
            return;
        }

        var suspendTicks = Interlocked.Exchange(ref _gcSuspendUtcTicks, 0);
        if (suspendTicks == 0)
        {
            return;
        }

        var endUtcTicks = e.TimeStamp.ToUniversalTime().Ticks;
        var pauseTicks = endUtcTicks - suspendTicks;
        if (pauseTicks < 0)
        {
            return;
        }

        // 100 ns units to the histogram's microseconds.
        PerfMetrics.GcPause.Record(pauseTicks / 10);
        if (FrameTrace.IsEnabled)
        {
            // The listener runs on the runtime's dispatch thread some time after the event; place the span on the
            // trace clock by subtracting how long ago the suspension began.
            var ageTicks = DateTime.UtcNow.Ticks - suspendTicks;
            var start = Stopwatch.GetTimestamp() - ageTicks * Stopwatch.Frequency / DotNetTicksPerSecond;
            FrameTrace.Record(FrameTraceEventKind.GcPause, start, pauseTicks * Stopwatch.Frequency / DotNetTicksPerSecond, GC.CollectionCount(0));
        }
    }

    private sealed class GcPauseListener : EventListener
    {
        // Runs from the base constructor, before any field of this class is initialized: touch only the argument.
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == RuntimeEventSourceName)
            {
                EnableEvents(eventSource, EventLevel.Informational, GcKeyword);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            try
            {
                OnGcRuntimeEvent(eventData);
            }
            catch (Exception)
            {
                // A failed sensor callback must never reach the runtime's event dispatch thread.
            }
        }
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

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct UnsignedRatio
    {
        public uint Numerator;
        public uint Denominator;
    }

    // DWM_TIMING_INFO from dwmapi.h. The header declares it byte-packed (native cbSize is 292); the call rejects any
    // other cbSize, so the whole structure is declared even though only RateRefresh and FrameNumber are read.
    // Verified against the live API: Pack = 1 gives 292 and a refresh rate matching the display.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct DwmTimingInfo
    {
        public uint Size;
        public UnsignedRatio RateRefresh;
        public ulong QpcRefreshPeriod;
        public UnsignedRatio RateCompose;
        public ulong QpcVBlank;
        public ulong RefreshCount;
        public uint DxRefreshCount;
        public ulong QpcCompose;
        public ulong FrameNumber;
        public uint DxPresentCount;
        public ulong RefreshFrame;
        public ulong FrameSubmitted;
        public uint DxPresentSubmitted;
        public ulong FrameConfirmed;
        public uint DxPresentConfirmed;
        public ulong RefreshConfirmed;
        public uint DxRefreshConfirmed;
        public ulong FramesLate;
        public uint FramesOutstanding;
        public ulong FrameDisplayed;
        public ulong QpcFrameDisplayed;
        public ulong RefreshFrameDisplayed;
        public ulong FrameComplete;
        public ulong QpcFrameComplete;
        public ulong FramePending;
        public ulong QpcFramePending;
        public ulong FramesDisplayed;
        public ulong FramesComplete;
        public ulong FramesPending;
        public ulong FramesAvailable;
        public ulong FramesDropped;
        public ulong FramesMissed;
        public ulong RefreshNextDisplayed;
        public ulong RefreshNextPresented;
        public ulong RefreshesDisplayed;
        public ulong RefreshesPresented;
        public ulong RefreshStarted;
        public ulong PixelsReceived;
        public ulong PixelsDrawn;
        public ulong BuffersEmpty;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessTimes(IntPtr process, out long creationTime, out long exitTime, out long kernelTime, out long userTime);

    [DllImport("kernel32.dll", EntryPoint = "K32GetProcessMemoryInfo", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool K32GetProcessMemoryInfo(IntPtr process, ref ProcessMemoryCountersEx counters, uint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessIoCounters(IntPtr process, out IoCounters counters);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessHandleCount(IntPtr process, out uint handleCount);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetCompositionTimingInfo(IntPtr window, ref DwmTimingInfo info);
}
