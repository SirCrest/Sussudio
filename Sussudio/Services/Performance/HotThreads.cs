using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Sussudio.Services.Performance;

/// <summary>
/// One registered hot thread's CPU totals at an instant. <see cref="CpuTicks"/> is kernel plus user time in 100 ns
/// units, <see cref="Cycles"/> is <c>QueryThreadCycleTime</c>. A retired thread (<see cref="Alive"/> false) keeps its
/// final totals. <see cref="StartTimestamp"/> and <see cref="EndTimestamp"/> are Stopwatch timestamps so a report
/// can divide by the part of its interval the thread was actually running.
/// </summary>
public sealed record HotThreadSample(
    long Serial,
    string Role,
    int ThreadId,
    long CpuTicks,
    ulong Cycles,
    string Priority,
    bool Mmcss,
    bool Alive,
    long StartTimestamp,
    long? EndTimestamp);

/// <summary>
/// Registry of the app's dedicated hot threads (capture read loop, MJPEG decode workers, preview render loop, encode
/// and mux threads, audio threads) so a report can say where CPU went. A thread calls <see cref="Register"/> once at
/// the start of its body (next to its MMCSS registration) and disposes the returned token on exit. Control plane
/// only: it takes a lock and a few syscalls per registration and per sample, never on a frame path. Exited threads
/// keep their final totals in a bounded retired list so a window spanning a preview restart still adds up.
/// </summary>
public static class HotThreads
{
    public const int MaxRetired = 64;

    private const uint ThreadQueryLimitedInformation = 0x0800;
    private const int PriorityErrorReturn = int.MaxValue;

    private static readonly object Gate = new();
    private static readonly List<Registration> Active = new();
    private static readonly Queue<HotThreadSample> Retired = new();
    private static long _nextSerial;

    /// <summary>
    /// Registers the calling thread under <paramref name="role"/> (for example <c>preview.render</c>) and names it in
    /// frame traces. Dispose the token on thread exit. A failure to open the thread handle degrades to a token that
    /// reports zero CPU; it never throws.
    /// </summary>
    public static Registration Register(string role, bool mmcss = false)
    {
        var managedThreadId = Environment.CurrentManagedThreadId;
        var nameEntry = FrameTrace.NameCurrentThread(role);
        var registration = new Registration(
            role,
            (int)GetCurrentThreadId(),
            OpenThread(ThreadQueryLimitedInformation, false, GetCurrentThreadId()),
            mmcss,
            Stopwatch.GetTimestamp(),
            managedThreadId,
            nameEntry);
        lock (Gate)
        {
            registration.Serial = ++_nextSerial;
            Active.Add(registration);
        }

        return registration;
    }

    /// <summary>Every live thread plus the retired list, oldest retired first.</summary>
    public static IReadOnlyList<HotThreadSample> Sample()
    {
        lock (Gate)
        {
            var samples = new List<HotThreadSample>(Active.Count + Retired.Count);
            samples.AddRange(Retired);
            foreach (var registration in Active)
            {
                samples.Add(registration.Read(alive: true, endTimestamp: null));
            }

            return samples;
        }
    }

    private static void Retire(Registration registration)
    {
        lock (Gate)
        {
            if (!Active.Remove(registration))
            {
                return;
            }

            // Final totals are read while the handle is still open; the handle is closed straight after.
            Retired.Enqueue(registration.Read(alive: false, endTimestamp: Stopwatch.GetTimestamp()));
            while (Retired.Count > MaxRetired)
            {
                Retired.Dequeue();
            }

            registration.ReleaseHandle();
            registration.RetireTraceName();
        }
    }

    /// <summary>Token returned by <see cref="Register"/>. Disposing it retires the thread.</summary>
    public sealed class Registration : IDisposable
    {
        private IntPtr _handle;
        private volatile bool _mmcss;

        // The trace labels threads by managed id; this registration's label is removed when it retires, and only if the
        // id still maps to it (managed ids are recycled).
        private readonly int _managedThreadId;
        private readonly ThreadNameEntry _nameEntry;

        internal Registration(string role, int threadId, IntPtr handle, bool mmcss, long startTimestamp, int managedThreadId, ThreadNameEntry nameEntry)
        {
            Role = role;
            ThreadId = threadId;
            _handle = handle;
            _mmcss = mmcss;
            StartTimestamp = startTimestamp;
            _managedThreadId = managedThreadId;
            _nameEntry = nameEntry;
        }

        internal long Serial { get; set; }

        public string Role { get; }

        public int ThreadId { get; }

        internal long StartTimestamp { get; }

        /// <summary>Records whether the thread's MMCSS registration succeeded (it usually happens after <see cref="Register"/>).</summary>
        public void MarkMmcss(bool registered) => _mmcss = registered;

        public void Dispose() => Retire(this);

        // Caller holds Gate, so the handle cannot be closed mid-read.
        internal HotThreadSample Read(bool alive, long? endTimestamp)
        {
            long cpuTicks = 0;
            ulong cycles = 0;
            var priority = "Unknown";
            if (_handle != IntPtr.Zero)
            {
                if (GetThreadTimes(_handle, out _, out _, out var kernel, out var user))
                {
                    cpuTicks = kernel + user;
                }

                if (!QueryThreadCycleTime(_handle, out cycles))
                {
                    cycles = 0;
                }

                priority = PriorityName(GetThreadPriority(_handle), _mmcss);
            }

            return new HotThreadSample(Serial, Role, ThreadId, cpuTicks, cycles, priority, _mmcss, alive, StartTimestamp, endTimestamp);
        }

        internal void RetireTraceName() => FrameTrace.RetireThreadName(_managedThreadId, _nameEntry);

        internal void ReleaseHandle()
        {
            var handle = _handle;
            _handle = IntPtr.Zero;
            if (handle != IntPtr.Zero)
            {
                CloseHandleNative(handle);
            }
        }
    }

    /// <summary>
    /// The report's priority label. The seven Win32 base priorities keep their names; any other value is a priority the
    /// MMCSS scheduler (or the kernel) set, shown as <c>mmcss/&lt;n&gt;</c> for a thread registered with MMCSS and
    /// <c>raw/&lt;n&gt;</c> otherwise, so the raw number is always in the label and the form never varies by value.
    /// </summary>
    internal static string PriorityName(int priority, bool mmcss) => priority switch
    {
        PriorityErrorReturn => "Unknown",
        -15 => "Idle",
        -2 => "Lowest",
        -1 => "BelowNormal",
        0 => "Normal",
        1 => "AboveNormal",
        2 => "Highest",
        15 => "TimeCritical",
        _ => (mmcss ? "mmcss/" : "raw/") + priority.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenThread(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint threadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetThreadTimes(IntPtr thread, out long creationTime, out long exitTime, out long kernelTime, out long userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryThreadCycleTime(IntPtr thread, out ulong cycleTime);

    [DllImport("kernel32.dll")]
    private static extern int GetThreadPriority(IntPtr thread);

    [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandleNative(IntPtr handle);
}
