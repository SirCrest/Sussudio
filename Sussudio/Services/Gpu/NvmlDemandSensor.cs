using Sussudio.Services.Performance;

namespace Sussudio.Services.Gpu;

/// <summary>
/// Lets the telemetry sensor supervisor hold the NVML poll running, so the <c>gpu.nvml.*</c> histograms fill while a
/// measurement window is open or a frame trace runs even with the stats dock hidden. It owns only its own demand
/// source: the dock keeps its separate source, and the main window still owns and disposes the monitor.
/// </summary>
public sealed class NvmlDemandSensor : IDemandSensor
{
    private readonly NvmlMonitor? _monitor;

    public NvmlDemandSensor(NvmlMonitor? monitor)
    {
        _monitor = monitor;
    }

    public string Name => "nvml";

    public bool Start() => _monitor?.SetPollingDemand(NvmlPollingDemand.TelemetrySensors, true) ?? false;

    public void Stop() => _monitor?.SetPollingDemand(NvmlPollingDemand.TelemetrySensors, false);

    public string Describe()
    {
        if (_monitor is not { IsAvailable: true } monitor)
        {
            return "nvml: unavailable (no nvml.dll or no NVIDIA device)";
        }

        var demand = monitor.PollingDemand;
        return demand == NvmlPollingDemand.None
            ? "nvml: parked"
            : "nvml: polling for " + demand;
    }
}
