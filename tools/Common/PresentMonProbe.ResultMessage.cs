using System.Diagnostics;
using System.Globalization;

namespace Sussudio.Tools;

public static partial class PresentMonProbe
{
    private static string BuildResultMessage(
        ProcessRun run,
        PresentMonCaptureSummary? summary,
        Process targetProcess,
        string parseMessage,
        bool success)
    {
        if (success && summary != null)
        {
            return $"Captured {summary.RawSampleCount} PresentMon frame rows for {targetProcess.ProcessName} ({targetProcess.Id}); selected {summary.SampleCount} rows from swap chain {summary.SelectedSwapChainAddress ?? "(none)"}.";
        }

        if (run.ExitCode == 0 &&
            summary != null &&
            !string.IsNullOrWhiteSpace(summary.ExpectedSwapChainAddress) &&
            !summary.ExpectedSwapChainMatched)
        {
            return $"PresentMon captured {summary.RawSampleCount} frame rows for {targetProcess.ProcessName} ({targetProcess.Id}), but expected swap chain {summary.ExpectedSwapChainAddress} was not present.";
        }

        return $"PresentMon capture did not produce frame rows. exitCode={run.ExitCode?.ToString(CultureInfo.InvariantCulture) ?? "(none)"} timedOut={run.TimedOut}.{parseMessage}";
    }
}
