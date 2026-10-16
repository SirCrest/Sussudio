using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Sussudio.Services.Performance;

/// <summary>
/// Reads the Windows GPU performance counters through PDH (<c>pdh.dll</c>, no extra package):
/// <c>\GPU Engine(pid_N*)\Utilization Percentage</c> (a rate counter, so the first collect only primes it) and
/// <c>\GPU Process Memory(pid_N*)\Dedicated Usage</c>. The wildcard instance path is expanded by PDH on every
/// collect, so engine instances that appear or disappear (a process starts using the video encoder, the renderer
/// restarts) are picked up without reopening. The pid wildcard also matches longer pids (<c>pid_1086*</c> vs
/// <c>pid_10860</c>); <see cref="GpuEngineAggregator"/> filters to the exact pid.
/// An instance of this class is used by one thread only; it owns its query handle and unmanaged buffer.
/// </summary>
internal sealed class NativePdhGpuCounterReader : IGpuCounterReader
{
    private const uint PdhFmtDouble = 0x00000200;
    private const uint PdhMoreData = 0x800007D2;
    private const uint PdhNoData = 0x800007D5;
    private const uint PdhCalcNegativeDenominator = 0x800007D6;
    private const uint PdhNoInstance = 0x800007D1;
    private const uint PdhItemNotValidated = 0x800007D3;
    private const uint PdhInvalidData = 0xC0000BBA;
    private const uint PdhCstatusNoObject = 0xC0000BB8;
    private const uint PdhCstatusNoCounter = 0xC0000BB9;
    private const uint PdhCstatusValidData = 0x00000000;
    private const uint PdhCstatusNewData = 0x00000001;

    // sizeof(PDH_FMT_COUNTERVALUE_ITEM_W): LPWSTR name (8) + CStatus (4) + padding (4) + value union (8).
    private const int ItemSize = 24;
    private const int ItemStatusOffset = 8;
    private const int ItemValueOffset = 16;

    private const int MaxFormatAttempts = 4;

    private IntPtr _query;
    private IntPtr _engineCounter;
    private IntPtr _memoryCounter;
    private IntPtr _buffer;
    private int _bufferBytes;

    public string? Open(int processId)
    {
        var pid = processId.ToString(CultureInfo.InvariantCulture);
        var status = PdhOpenQueryW(null, IntPtr.Zero, out _query);
        if (status != 0)
        {
            _query = IntPtr.Zero;
            return Describe("PdhOpenQuery", status);
        }

        status = PdhAddEnglishCounterW(_query, $@"\GPU Engine(pid_{pid}*)\Utilization Percentage", IntPtr.Zero, out _engineCounter);
        if (status != 0)
        {
            _engineCounter = IntPtr.Zero;
            var error = Describe("PdhAddCounter GPU Engine", status);
            Close();
            return error;
        }

        // Dedicated VRAM is a bonus: engines still work without it.
        if (PdhAddEnglishCounterW(_query, $@"\GPU Process Memory(pid_{pid}*)\Dedicated Usage", IntPtr.Zero, out _memoryCounter) != 0)
        {
            _memoryCounter = IntPtr.Zero;
        }

        // Prime the rate counter: utilization is a delta between two collects.
        status = PdhCollectQueryData(_query);
        if (status != 0 && status != PdhNoData)
        {
            var error = Describe("PdhCollectQueryData", status);
            Close();
            return error;
        }

        return null;
    }

    public GpuCounterReadResult Read(List<GpuCounterItem> engines, List<GpuCounterItem> memory, out string? detail)
    {
        engines.Clear();
        memory.Clear();
        detail = null;
        if (_query == IntPtr.Zero)
        {
            detail = "GPU counter query is not open";
            return GpuCounterReadResult.Unavailable;
        }

        var status = PdhCollectQueryData(_query);
        if (status == PdhNoData)
        {
            return GpuCounterReadResult.Pending;
        }

        if (status != 0)
        {
            detail = Describe("PdhCollectQueryData", status);
            return GpuCounterReadResult.Unavailable;
        }

        var engineResult = ReadCounter(_engineCounter, engines, out detail);
        if (engineResult != GpuCounterReadResult.Valid)
        {
            return engineResult;
        }

        if (_memoryCounter != IntPtr.Zero)
        {
            // A memory counter that has no data does not invalidate the engine sample.
            ReadCounter(_memoryCounter, memory, out _);
        }

        return GpuCounterReadResult.Valid;
    }

    public void Dispose()
    {
        Close();
        if (_buffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_buffer);
            _buffer = IntPtr.Zero;
            _bufferBytes = 0;
        }
    }

    private GpuCounterReadResult ReadCounter(IntPtr counter, List<GpuCounterItem> items, out string? detail)
    {
        detail = null;
        uint itemCount = 0;
        for (var attempt = 0; attempt < MaxFormatAttempts; attempt++)
        {
            var bufferSize = (uint)_bufferBytes;
            var status = PdhGetFormattedCounterArrayW(counter, PdhFmtDouble, ref bufferSize, out itemCount, _buffer);
            if (status == PdhMoreData)
            {
                // The instance set grew since the last call: size the buffer with slack and ask again.
                EnsureBuffer(checked((int)bufferSize + 4096));
                continue;
            }

            // Measured on Windows 11: while the process holds no GPU engine instance, PdhCollectQueryData returns
            // PDH_NO_DATA, and once the instances vanish (the last D3D device was released) a query that still
            // has another counter with data answers PDH_CSTATUS_ITEM_NOT_VALIDATED. All of these mean "no
            // instance matches the process right now", which is no sample, not 0 %.
            if (status == PdhNoInstance
                || status == PdhItemNotValidated
                || status == PdhCalcNegativeDenominator
                || status == PdhNoData
                || status == PdhInvalidData)
            {
                return GpuCounterReadResult.Pending;
            }

            if (status == PdhCstatusNoObject || status == PdhCstatusNoCounter)
            {
                detail = Describe("PdhGetFormattedCounterArray", status);
                return GpuCounterReadResult.Unavailable;
            }

            if (status != 0)
            {
                detail = Describe("PdhGetFormattedCounterArray", status);
                return GpuCounterReadResult.Unavailable;
            }

            for (var i = 0; i < itemCount; i++)
            {
                var item = _buffer + i * ItemSize;
                var cstatus = (uint)Marshal.ReadInt32(item, ItemStatusOffset);
                if (cstatus != PdhCstatusValidData && cstatus != PdhCstatusNewData)
                {
                    continue;
                }

                var name = Marshal.PtrToStringUni(Marshal.ReadIntPtr(item));
                if (name is null)
                {
                    continue;
                }

                var value = BitConverter.Int64BitsToDouble(Marshal.ReadInt64(item, ItemValueOffset));
                items.Add(new GpuCounterItem(name, value));
            }

            return GpuCounterReadResult.Valid;
        }

        detail = "GPU counter instance list kept changing between reads";
        return GpuCounterReadResult.Pending;
    }

    private void EnsureBuffer(int bytes)
    {
        if (bytes <= _bufferBytes)
        {
            return;
        }

        if (_buffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_buffer);
        }

        _buffer = Marshal.AllocHGlobal(bytes);
        _bufferBytes = bytes;
    }

    private void Close()
    {
        if (_query != IntPtr.Zero)
        {
            PdhCloseQuery(_query);
            _query = IntPtr.Zero;
        }

        _engineCounter = IntPtr.Zero;
        _memoryCounter = IntPtr.Zero;
    }

    private static string Describe(string call, uint status) =>
        $"{call} failed: 0x{status:X8}" + status switch
        {
            PdhCstatusNoObject => " (GPU counter object not installed)",
            PdhCstatusNoCounter => " (GPU counter not found)",
            PdhNoInstance => " (no matching instance)",
            _ => string.Empty,
        };

    [DllImport("pdh.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint PdhOpenQueryW(string? dataSource, IntPtr userData, out IntPtr query);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint PdhAddEnglishCounterW(IntPtr query, string counterPath, IntPtr userData, out IntPtr counter);

    [DllImport("pdh.dll", ExactSpelling = true)]
    private static extern uint PdhCollectQueryData(IntPtr query);

    [DllImport("pdh.dll", ExactSpelling = true)]
    private static extern uint PdhGetFormattedCounterArrayW(IntPtr counter, uint format, ref uint bufferSize, out uint itemCount, IntPtr buffer);

    [DllImport("pdh.dll", ExactSpelling = true)]
    private static extern uint PdhCloseQuery(IntPtr query);
}
