using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Gpu;

// Optional NVML telemetry snapshot. All values are nullable because the monitor
// is diagnostic-only and must gracefully degrade on non-NVIDIA systems.
public sealed record NvmlSnapshot(
    string? GpuName,
    uint? GpuUtilizationPercent,
    uint? GpuMemoryUtilizationPercent,
    uint? NvdecUtilizationPercent,
    uint? NvencUtilizationPercent,
    uint? PcieTxKBps,
    uint? PcieRxKBps,
    ulong? VramUsedBytes,
    ulong? VramTotalBytes,
    uint? GpuTemperatureC,
    uint? GpuPowerMilliwatts,
    uint? GpuClockMHz,
    uint? GpuMemClockMHz)
{
    public double? GpuPowerW => GpuPowerMilliwatts.HasValue ? GpuPowerMilliwatts.Value / 1000.0 : null;
    public double? PcieTxMBps => PcieTxKBps.HasValue ? PcieTxKBps.Value / 1024.0 : null;
    public double? PcieRxMBps => PcieRxKBps.HasValue ? PcieRxKBps.Value / 1024.0 : null;
    public ulong? VramUsedMB => VramUsedBytes.HasValue ? VramUsedBytes.Value / (1024 * 1024) : null;
    public ulong? VramTotalMB => VramTotalBytes.HasValue ? VramTotalBytes.Value / (1024 * 1024) : null;
}

// Polls NVIDIA GPU utilization/encoder/decoder counters for stats overlays and
// diagnostic sessions. Missing nvml.dll disables the feature without affecting
// capture, preview, or recording.
public sealed class NvmlMonitor : IDisposable
{
    private const uint NVML_SUCCESS = 0;
    private const uint NVML_PCIE_UTIL_TX_BYTES = 0;
    private const uint NVML_PCIE_UTIL_RX_BYTES = 1;
    private const uint NVML_TEMPERATURE_GPU = 0;
    private const uint NVML_CLOCK_GRAPHICS = 0;
    private const uint NVML_CLOCK_MEM = 2;

    private readonly bool _available;
    private readonly IntPtr _device;
    private readonly string? _gpuName;
    private readonly Timer? _pollTimer;
    private NvmlSnapshot? _latestSnapshot;
    private int _pollInProgress;
    private bool _disposed;

    public NvmlMonitor(int pollIntervalMs = 500)
    {
        try
        {
            if (!NativeLibrary.TryLoad("nvml.dll", out _))
            {
                Logger.Log("NVML_MONITOR_INIT unavailable=true reason=nvml.dll_not_found");
                _available = false;
                return;
            }

            var initResult = nvmlInit_v2();
            if (initResult != NVML_SUCCESS)
            {
                Logger.Log($"NVML_MONITOR_INIT unavailable=true reason=nvmlInit_failed code={initResult}");
                _available = false;
                return;
            }

            var getDeviceResult = nvmlDeviceGetHandleByIndex_v2(0, out var device);
            if (getDeviceResult != NVML_SUCCESS)
            {
                Logger.Log($"NVML_MONITOR_INIT unavailable=true reason=nvmlDeviceGetHandle_failed code={getDeviceResult}");
                nvmlShutdown();
                _available = false;
                return;
            }

            _device = device;
            _gpuName = GetDeviceName(device);
            _available = true;

            Logger.Log($"NVML_MONITOR_INIT available=true gpu='{_gpuName}'");

            // Avoid polling synchronously during startup/GPU pipeline transitions.
            // NVML driver calls can raise corrupted-state access violations that
            // bypass managed catch blocks; keep polling serialized and delayed.
            _pollTimer = new Timer(Poll, null, pollIntervalMs, pollIntervalMs);
        }
        catch (Exception ex)
        {
            Logger.Log($"NVML_MONITOR_INIT unavailable=true reason=exception type={ex.GetType().Name} msg={ex.Message}");
            _available = false;
        }
    }

    public bool IsAvailable => _available;

    public NvmlSnapshot? GetLatestSnapshot() => Volatile.Read(ref _latestSnapshot);

    private void Poll(object? state)
    {
        if (!_available || _disposed)
            return;

        if (Interlocked.Exchange(ref _pollInProgress, 1) != 0)
            return;

        try
        {
            uint? gpuUtil = null, memUtil = null;
            if (nvmlDeviceGetUtilizationRates(_device, out var utilization) == NVML_SUCCESS)
            {
                gpuUtil = utilization.gpu;
                memUtil = utilization.memory;
            }

            uint? nvdecUtil = null;
            if (nvmlDeviceGetDecoderUtilization(_device, out var decoderUtil, out _) == NVML_SUCCESS)
                nvdecUtil = decoderUtil;

            uint? nvencUtil = null;
            if (nvmlDeviceGetEncoderUtilization(_device, out var encoderUtil, out _) == NVML_SUCCESS)
                nvencUtil = encoderUtil;

            uint? pcieTx = null;
            if (nvmlDeviceGetPcieThroughput(_device, NVML_PCIE_UTIL_TX_BYTES, out var txKBps) == NVML_SUCCESS)
                pcieTx = txKBps;

            uint? pcieRx = null;
            if (nvmlDeviceGetPcieThroughput(_device, NVML_PCIE_UTIL_RX_BYTES, out var rxKBps) == NVML_SUCCESS)
                pcieRx = rxKBps;

            ulong? vramUsed = null, vramTotal = null;
            var memInfo = new NvmlMemory_v2 { version = NvmlMemory_v2.StructVersion };
            if (nvmlDeviceGetMemoryInfo_v2(_device, ref memInfo) == NVML_SUCCESS)
            {
                vramUsed = memInfo.used;
                vramTotal = memInfo.total;
            }

            uint? temp = null;
            if (nvmlDeviceGetTemperature(_device, NVML_TEMPERATURE_GPU, out var gpuTemp) == NVML_SUCCESS)
                temp = gpuTemp;

            uint? power = null;
            if (nvmlDeviceGetPowerUsage(_device, out var powerMilliwatts) == NVML_SUCCESS)
                power = powerMilliwatts;

            uint? gpuClock = null;
            if (nvmlDeviceGetClockInfo(_device, NVML_CLOCK_GRAPHICS, out var gfxClock) == NVML_SUCCESS)
                gpuClock = gfxClock;

            uint? memClock = null;
            if (nvmlDeviceGetClockInfo(_device, NVML_CLOCK_MEM, out var mClock) == NVML_SUCCESS)
                memClock = mClock;

            Volatile.Write(ref _latestSnapshot, new NvmlSnapshot(
                _gpuName, gpuUtil, memUtil, nvdecUtil, nvencUtil,
                pcieTx, pcieRx, vramUsed, vramTotal,
                temp, power, gpuClock, memClock));
        }
        catch (Exception ex)
        {
            Logger.Log($"NVML_MONITOR_POLL_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _pollInProgress, 0);
        }
    }

    private static unsafe string? GetDeviceName(IntPtr device)
    {
        var buffer = stackalloc byte[96];
        return nvmlDeviceGetName(device, buffer, 96) == NVML_SUCCESS
            ? Marshal.PtrToStringAnsi((IntPtr)buffer)
            : null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pollTimer?.Dispose();
        if (_available)
        {
            nvmlShutdown();
        }
    }

    // --- NVML P/Invoke ---

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
