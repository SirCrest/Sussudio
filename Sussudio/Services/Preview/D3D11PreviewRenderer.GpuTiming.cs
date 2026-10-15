using System;
using System.Diagnostics;
using System.Threading;
using Sussudio.Services.Performance;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

// GPU render timing and the display-statistics hook of the perf report. Everything here runs on the preview render
// thread (the owner of the immediate context) except ReadPerfDisplayCounters, which the control plane calls.
//
// What the span measures. The D3D11 timestamp span brackets the render pass only: the pixel-shader Draw of the NV12
// and HDR shader paths, or the VideoProcessorBlt of the video-processor path. It deliberately does NOT cover the HDR
// pre-draw input upload (the CopySubresourceRegion / staging copy into the HDR input texture): that is input transfer,
// already reported on the CPU side as preview.input_upload, and folding it in would make preview.gpu_render mean "copy
// plus draw" and stop it from isolating what the shader costs. It also excludes ClearRenderTargetView, state binding
// and Present (the Present path is preview.present).
//
// What the number means. D3D11 timestamps are in the GPU's own clock domain, not QPC, so only the duration is a GPU
// measurement; the trace span is anchored at the CPU submit time. The preview usually runs on the capture pipeline's
// shared device (and so its shared, multithread-protected immediate context): any other thread's commands that the
// context accepts between the span's Begin and End are inside it, and the GPU may be working on other queued commands
// from the same device. preview.gpu_render is therefore an upper bound on the render pass itself under contention, and
// exact only on a quiet device. A renderer-owned device has no such contamination.
//
// What the number means per render path (GpuTimingPath, shown as path= in the Sensors gpu_timing line):
//   VideoProcessor: the VideoProcessorBlt is the driver's own conversion and scaling work. Some drivers run or defer it
//     outside the queue that wrote the timestamps, so the begin and end timestamps can be identical. A span of zero GPU
//     ticks is not a measurement: it is counted in preview.gpu_timing_unmeasured and not recorded.
//   Nv12Shader and HdrShader: a pixel-shader Draw on the renderer's context. A zero-tick span is not expected there;
//     if one appears it is counted as unmeasured the same way.
//
// When it runs. Never on a WARP/software adapter (a CPU rasterizer's timestamps measure CPU emulation and the queries
// cost real time there). While demand is off, no query object exists. On any failure the ring is disposed and the
// renderer stays off until demand ends or the device is re-created, so a broken driver cannot be hammered every frame.
internal sealed partial class D3D11PreviewRenderer
{
    private const int GpuTimingSlots = 6;

    private readonly CachedDemand _gpuTimingDemand = new(TelemetrySensors.IsGpuTimingWanted, Stopwatch.Frequency);
    private D3D11TimestampQueries? _gpuTimingQueries;
    private GpuTimestampRing? _gpuTimingRing;
    private bool _gpuTimingBlocked;
    private string _gpuTimingStateBase = string.Empty;
    private GpuTimingPath _gpuTimingPath = GpuTimingPath.VideoProcessor;
    private Func<PerfDisplayCounters?>? _perfDisplayProvider;

    // Bumped by every InitializeD3D. The DXGI frame statistics remember the generation that produced them, so the
    // report can tell a counter that restarted with a new swap chain from a counter that merely advanced.
    private long _displayGeneration;
    private long _dxgiFrameStatisticsGeneration;

    /// <summary>
    /// Opens a GPU timing span for the draw that is about to be submitted and returns the slot for
    /// <see cref="EndGpuTiming"/>, or -1 when this frame is not timed (no demand, blocked, ring full). Call it after
    /// every early return of the render pass, immediately before the draw, so a span is only ever opened for a frame
    /// that will draw. With demand off this is one timestamp compare and one volatile read.
    /// </summary>
    private int BeginGpuTiming(long frameId, GpuTimingPath path)
    {
        var now = Stopwatch.GetTimestamp();
        if (!_gpuTimingDemand.IsActive(now))
        {
            if (_gpuTimingRing is not null || _gpuTimingBlocked)
            {
                ReleaseGpuTiming("off: no demand");
            }

            return -1;
        }

        if (_gpuTimingBlocked)
        {
            return -1;
        }

        var ring = _gpuTimingRing ?? CreateGpuTiming();
        if (ring is null)
        {
            return -1;
        }

        if (path != _gpuTimingPath)
        {
            // A path change is rare (an HDR toggle, the first external-texture frame): the state line follows it.
            _gpuTimingPath = path;
            PublishGpuTimingState();
        }

        var slot = ring.Begin(frameId, now, path);
        if (ring.IsFailed)
        {
            BlockGpuTiming("failed: " + ring.FailureReason);
        }

        return slot;
    }

    /// <summary>Closes the span opened by <see cref="BeginGpuTiming"/>. Safe for -1 and after the ring was released.</summary>
    private void EndGpuTiming(int slot)
    {
        if (slot < 0)
        {
            return;
        }

        var ring = _gpuTimingRing;
        if (ring is null)
        {
            return;
        }

        ring.End(slot);
        if (ring.IsFailed)
        {
            BlockGpuTiming("failed: " + ring.FailureReason);
        }
    }

    private GpuTimestampRing? CreateGpuTiming()
    {
        var device = _device;
        var context = _deviceContext;
        if (device is null || context is null)
        {
            // Not ready (the device is being re-created); try again on a later frame.
            return null;
        }

        string adapterName;
        try
        {
            using var dxgiDevice = device.QueryInterface<IDXGIDevice>();
            using var adapter = dxgiDevice.GetAdapter();
            using var adapter1 = adapter.QueryInterface<IDXGIAdapter1>();
            var description = adapter1.Description1;
            adapterName = description.Description;
            if ((description.Flags & AdapterFlags.Software) != 0)
            {
                BlockGpuTiming("disabled: software (WARP) adapter " + adapterName);
                return null;
            }
        }
        catch (Exception ex)
        {
            BlockGpuTiming("unavailable: adapter query failed (" + ex.GetType().Name + ")");
            return null;
        }

        var queries = D3D11TimestampQueries.TryCreate(device, context, GpuTimingSlots, out var error);
        if (queries is null)
        {
            BlockGpuTiming("unavailable: " + (error ?? "query creation failed"));
            return null;
        }

        _gpuTimingQueries = queries;
        _gpuTimingRing = new GpuTimestampRing(queries);
        var shared = Volatile.Read(ref _sharedDeviceActive) != 0;
        _gpuTimingStateBase = $"adapter='{adapterName}' slots={queries.SlotCount} shared_device={shared}";
        PublishGpuTimingState();
        Logger.Log($"D3D11_PREVIEW_GPU_TIMING enabled adapter='{adapterName}' slots={queries.SlotCount} shared={shared}.");
        return _gpuTimingRing;
    }

    // The Sensors gpu_timing state line: the device facts plus the render path the spans currently bracket and what a
    // zero-tick span means. Control-plane string, rebuilt only on enable and on a path change.
    private void PublishGpuTimingState() =>
        TelemetrySensors.SetGpuTimingState(
            $"active {_gpuTimingStateBase} path={_gpuTimingPath} (upper bound under contention; zero-tick spans are counted in preview.gpu_timing_unmeasured)");

    // Disposes the queries and records why GPU timing is off, then stays off until demand ends or the device is re-created.
    private void BlockGpuTiming(string reason)
    {
        DisposeGpuTimingQueries();
        _gpuTimingBlocked = true;
        TelemetrySensors.SetGpuTimingState(reason);
        Logger.Log($"D3D11_PREVIEW_GPU_TIMING blocked reason='{reason}'.");
    }

    // Releases the query objects and clears the failure latch. Idempotent. The queries belong to the current device and
    // context, so this runs before either is disposed (CleanupD3DResources) as well as when demand ends.
    private void ReleaseGpuTiming(string state)
    {
        var ring = _gpuTimingRing;
        var existed = ring is not null || _gpuTimingBlocked;
        DisposeGpuTimingQueries();
        _gpuTimingBlocked = false;
        if (existed)
        {
            TelemetrySensors.SetGpuTimingState(state);
            var tally = ring is null
                ? string.Empty
                : $" recorded={ring.Recorded} unmeasured={ring.Unmeasured} sub_us={ring.SubMicrosecond}" +
                  $" vp={ring.RecordedFor(GpuTimingPath.VideoProcessor)}/{ring.UnmeasuredFor(GpuTimingPath.VideoProcessor)}" +
                  $" nv12={ring.RecordedFor(GpuTimingPath.Nv12Shader)}/{ring.UnmeasuredFor(GpuTimingPath.Nv12Shader)}" +
                  $" hdr={ring.RecordedFor(GpuTimingPath.HdrShader)}/{ring.UnmeasuredFor(GpuTimingPath.HdrShader)}";
            Logger.Log($"D3D11_PREVIEW_GPU_TIMING released reason='{state}'{tally}.");
        }
    }

    private void DisposeGpuTimingQueries()
    {
        _gpuTimingRing = null;
        var queries = _gpuTimingQueries;
        _gpuTimingQueries = null;
        queries?.Dispose();
    }

    private void RegisterPerfDisplaySource()
    {
        var provider = ReadPerfDisplayCounters;
        _perfDisplayProvider = provider;
        PerfDisplaySource.Register(provider);
    }

    private void UnregisterPerfDisplaySource()
    {
        var provider = _perfDisplayProvider;
        if (provider is null)
        {
            return;
        }

        _perfDisplayProvider = null;
        PerfDisplaySource.Unregister(provider);
    }

    // Control plane. Reads the counters TrackDxgiFrameStatistics already samples; adds no call on the present path.
    private PerfDisplayCounters? ReadPerfDisplayCounters()
    {
        lock (_dxgiFrameStatisticsLock)
        {
            if (_dxgiFrameStatisticsSuccessCount == 0)
            {
                return null;
            }

            return new PerfDisplayCounters(
                _dxgiFrameStatisticsGeneration,
                _dxgiFrameStatisticsSuccessCount,
                _dxgiFrameStatisticsPresentCount,
                _dxgiFrameStatisticsPresentRefreshCount,
                _dxgiFrameStatisticsSyncRefreshCount,
                _dxgiFrameStatisticsSyncQpcTime);
        }
    }
}
