using System;
using System.Runtime.InteropServices;
using Sussudio.Services.Performance;
using Vortice.Direct3D11;

namespace Sussudio.Services.Preview;

/// <summary>
/// D3D11 timestamp queries for <see cref="GpuTimestampRing"/>: per slot one <c>TimestampDisjoint</c> query bracketing a
/// begin and an end <c>Timestamp</c> query. Results are read with <c>DoNotFlush</c> so a poll never submits or waits;
/// <c>S_FALSE</c> means not ready, any failure code means the query machinery is gone (device removed). The query
/// objects belong to the renderer's device and context, so the renderer disposes this object before the context
/// (see <c>CleanupD3DResources</c>). All calls happen on the preview render thread.
/// </summary>
internal sealed class D3D11TimestampQueries : IGpuTimestampBackend, IDisposable
{
    // D3D11_QUERY_DATA_TIMESTAMP_DISJOINT is { UINT64 Frequency; BOOL Disjoint; } = 16 bytes with padding.
    private const uint DisjointDataSize = 16;
    private const uint TimestampDataSize = 8;
    private const int SFalse = 1;

    private readonly ID3D11DeviceContext _context;
    private readonly ID3D11Query?[] _disjoint;
    private readonly ID3D11Query?[] _begin;
    private readonly ID3D11Query?[] _end;
    private IntPtr _scratch;

    private D3D11TimestampQueries(ID3D11DeviceContext context, int slots)
    {
        _context = context;
        _disjoint = new ID3D11Query?[slots];
        _begin = new ID3D11Query?[slots];
        _end = new ID3D11Query?[slots];
        _scratch = Marshal.AllocHGlobal((int)DisjointDataSize);
    }

    public int SlotCount => _disjoint.Length;

    /// <summary>Creates the query ring, or returns null and the reason when the device refuses (for example a driver without timestamp support).</summary>
    public static D3D11TimestampQueries? TryCreate(ID3D11Device device, ID3D11DeviceContext context, int slots, out string? error)
    {
        var queries = new D3D11TimestampQueries(context, Math.Clamp(slots, GpuTimestampRing.MinSlots, GpuTimestampRing.MaxSlots));
        try
        {
            for (var i = 0; i < queries.SlotCount; i++)
            {
                queries._disjoint[i] = device.CreateQuery(QueryType.TimestampDisjoint, QueryFlags.None);
                queries._begin[i] = device.CreateQuery(QueryType.Timestamp, QueryFlags.None);
                queries._end[i] = device.CreateQuery(QueryType.Timestamp, QueryFlags.None);
            }

            error = null;
            return queries;
        }
        catch (Exception ex)
        {
            queries.Dispose();
            error = ex.GetType().Name + ": " + ex.Message;
            return null;
        }
    }

    public bool Begin(int slot)
    {
        var disjoint = _disjoint[slot];
        var begin = _begin[slot];
        if (disjoint is null || begin is null)
        {
            return false;
        }

        _context.Begin(disjoint);
        _context.End(begin);
        return true;
    }

    public void End(int slot)
    {
        var disjoint = _disjoint[slot];
        var end = _end[slot];
        if (disjoint is null || end is null)
        {
            return;
        }

        _context.End(end);
        _context.End(disjoint);
    }

    public GpuTimestampPoll Poll(int slot, out long gpuTicks, out long gpuFrequency, out bool disjoint)
    {
        gpuTicks = 0;
        gpuFrequency = 0;
        disjoint = false;
        var disjointQuery = _disjoint[slot];
        var beginQuery = _begin[slot];
        var endQuery = _end[slot];
        if (_scratch == IntPtr.Zero || disjointQuery is null || beginQuery is null || endQuery is null)
        {
            return GpuTimestampPoll.Failed;
        }

        var result = _context.GetData(disjointQuery, _scratch, DisjointDataSize, AsyncGetDataFlags.DoNotFlush);
        if (result.Code == SFalse)
        {
            return GpuTimestampPoll.NotReady;
        }

        if (result.Failure)
        {
            return GpuTimestampPoll.Failed;
        }

        gpuFrequency = Marshal.ReadInt64(_scratch);
        disjoint = Marshal.ReadInt32(_scratch, 8) != 0;
        if (disjoint)
        {
            return GpuTimestampPoll.Ready;
        }

        result = _context.GetData(beginQuery, _scratch, TimestampDataSize, AsyncGetDataFlags.DoNotFlush);
        if (result.Code == SFalse)
        {
            return GpuTimestampPoll.NotReady;
        }

        if (result.Failure)
        {
            return GpuTimestampPoll.Failed;
        }

        var begin = (ulong)Marshal.ReadInt64(_scratch);
        result = _context.GetData(endQuery, _scratch, TimestampDataSize, AsyncGetDataFlags.DoNotFlush);
        if (result.Code == SFalse)
        {
            return GpuTimestampPoll.NotReady;
        }

        if (result.Failure)
        {
            return GpuTimestampPoll.Failed;
        }

        var end = (ulong)Marshal.ReadInt64(_scratch);

        // An end before the begin would wrap to a huge unsigned value; as a signed value it is negative and the ring skips it.
        gpuTicks = unchecked((long)(end - begin));
        return GpuTimestampPoll.Ready;
    }

    public void Dispose()
    {
        for (var i = 0; i < _disjoint.Length; i++)
        {
            _disjoint[i]?.Dispose();
            _disjoint[i] = null;
            _begin[i]?.Dispose();
            _begin[i] = null;
            _end[i]?.Dispose();
            _end[i] = null;
        }

        if (_scratch != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_scratch);
            _scratch = IntPtr.Zero;
        }
    }
}
