namespace Sussudio.Tools;

public static partial class PresentMonProbe
{
    private sealed record PresentMonRow(
        int RowIndex,
        string SwapChainAddress,
        string PresentMode,
        string PresentRuntime,
        string SyncInterval,
        string AllowsTearing,
        double? CpuStartTimeMs,
        double? BetweenPresentsMs,
        double? BetweenDisplayChangeMs,
        double? DisplayedTimeMs,
        double? UntilDisplayedMs,
        double? InPresentApiMs,
        double? CpuBusyMs,
        double? GpuBusyMs,
        double? GpuTimeMs,
        double? DisplayLatencyMs);
}
