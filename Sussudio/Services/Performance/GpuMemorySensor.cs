using System;
using System.Collections.Generic;
using SharpGen.Runtime;
using Vortice.DXGI;

namespace Sussudio.Services.Performance;

/// <summary>
/// One DXGI adapter and this process's video-memory standing on it. Budget and usage are per process and per segment
/// group: <c>Local</c> is the adapter's own video memory, <c>NonLocal</c> is system memory the GPU can reach. The
/// budget is what the OS currently allows this process (it shrinks under pressure from other applications), so
/// usage close to budget means the next allocation may page.
/// </summary>
public sealed record PerfReportGpuAdapter(
    string Name,
    string Luid,
    bool Software,
    long DedicatedVideoMemoryBytes,
    long LocalBudgetBytes,
    long LocalUsageBytes,
    long NonLocalBudgetBytes,
    long NonLocalUsageBytes);

/// <summary>Adapters read at report time, or the reason none could be read.</summary>
public readonly record struct GpuMemoryReading(IReadOnlyList<PerfReportGpuAdapter> Adapters, string? Error);

/// <summary>
/// Reads <c>IDXGIAdapter3.QueryVideoMemoryInfo</c> for every adapter when a perf report is built. On demand rather than
/// on a timer: the numbers are instantaneous gauges (no history is kept), a report is the only consumer, and a
/// control-plane read costs one DXGI factory and a few queries, so no thread, timer or demand gate is needed and
/// nothing runs while nobody asks. Never throws; a failure is returned as <see cref="GpuMemoryReading.Error"/>
/// and a single adapter that fails is skipped rather than failing the others.
/// </summary>
public static class GpuMemorySensor
{
    public static GpuMemoryReading Read()
    {
        try
        {
            var result = DXGI.CreateDXGIFactory1<IDXGIFactory1>(out var factory);
            using (factory)
            {
                if (result.Failure || factory is null)
                {
                    return new GpuMemoryReading(Array.Empty<PerfReportGpuAdapter>(), $"CreateDXGIFactory1 failed: 0x{result.Code:X8}");
                }

                var adapters = new List<PerfReportGpuAdapter>();
                for (uint index = 0; ; index++)
                {
                    result = factory.EnumAdapters1(index, out var adapter);
                    using (adapter)
                    {
                        if (result == ResultCode.NotFound || adapter is null)
                        {
                            break;
                        }

                        if (result.Failure)
                        {
                            return new GpuMemoryReading(adapters, $"EnumAdapters1 failed: 0x{result.Code:X8}");
                        }

                        var reading = ReadAdapter(adapter);
                        if (reading is not null)
                        {
                            adapters.Add(reading);
                        }
                    }
                }

                return new GpuMemoryReading(adapters, null);
            }
        }
        catch (Exception ex)
        {
            return new GpuMemoryReading(Array.Empty<PerfReportGpuAdapter>(), "Adapter memory query failed: " + ex.GetType().Name);
        }
    }

    private static PerfReportGpuAdapter? ReadAdapter(IDXGIAdapter1 adapter)
    {
        try
        {
            var description = adapter.Description1;
            using var adapter3 = adapter.QueryInterfaceOrNull<IDXGIAdapter3>();
            if (adapter3 is null)
            {
                return null;
            }

            var local = adapter3.QueryVideoMemoryInfo(0, MemorySegmentGroup.Local);
            var nonLocal = adapter3.QueryVideoMemoryInfo(0, MemorySegmentGroup.NonLocal);
            var software = (description.Flags & AdapterFlags.Software) != 0;
            return new PerfReportGpuAdapter(
                description.Description,
                $"0x{(uint)description.Luid.HighPart:X8}_0x{description.Luid.LowPart:X8}",
                software,
                ToLong((ulong)description.DedicatedVideoMemory),
                ToLong(local.Budget),
                ToLong(local.CurrentUsage),
                ToLong(nonLocal.Budget),
                ToLong(nonLocal.CurrentUsage));
        }
        catch (Exception)
        {
            // One adapter that cannot report (a removed or disabled device) must not hide the others.
            return null;
        }
    }

    private static long ToLong(ulong value) => value > long.MaxValue ? long.MaxValue : (long)value;
}
