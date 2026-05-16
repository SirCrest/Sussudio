namespace Sussudio.Tools;

public static partial class PresentMonProbe
{
    private static PresentMonAppCorrelation BuildAppCorrelation(
        IReadOnlyList<PresentMonRow> selectedRows,
        PresentMonProbeOptions? options,
        long? captureStartUtcUnixMs)
    {
        if (options?.AppPresentUtcUnixMs is not long appPresentUtcUnixMs || appPresentUtcUnixMs <= 0)
        {
            return new PresentMonAppCorrelation();
        }

        var startUtcUnixMs = options.CaptureStartUtcUnixMs ?? captureStartUtcUnixMs;
        if (!startUtcUnixMs.HasValue || startUtcUnixMs.Value <= 0)
        {
            return new PresentMonAppCorrelation
            {
                Reason = "Capture start timestamp was unavailable.",
                AppPresentId = options.AppPresentId ?? 0,
                AppSourceSequenceNumber = options.AppSourceSequenceNumber ?? -1,
                AppPresentUtcUnixMs = appPresentUtcUnixMs
            };
        }

        var appOffsetMs = appPresentUtcUnixMs - startUtcUnixMs.Value;
        var candidates = selectedRows
            .Where(row => row.CpuStartTimeMs.HasValue)
            .Select(row => new
            {
                Row = row,
                DeltaMs = Math.Abs(row.CpuStartTimeMs!.Value - appOffsetMs)
            })
            .OrderBy(candidate => candidate.DeltaMs)
            .ToList();

        if (candidates.Count == 0)
        {
            return new PresentMonAppCorrelation
            {
                Reason = "No selected PresentMon rows exposed CPUStartTime.",
                AppPresentId = options.AppPresentId ?? 0,
                AppSourceSequenceNumber = options.AppSourceSequenceNumber ?? -1,
                AppPresentUtcUnixMs = appPresentUtcUnixMs,
                AppPresentOffsetMs = appOffsetMs
            };
        }

        var best = candidates[0];
        if (best.DeltaMs > 50.0)
        {
            return new PresentMonAppCorrelation
            {
                Reason = "Nearest PresentMon row was outside the 50ms app-present correlation window.",
                AppPresentId = options.AppPresentId ?? 0,
                AppSourceSequenceNumber = options.AppSourceSequenceNumber ?? -1,
                AppPresentUtcUnixMs = appPresentUtcUnixMs,
                AppPresentOffsetMs = appOffsetMs,
                PresentMonRowIndex = best.Row.RowIndex,
                PresentMonCpuStartTimeMs = best.Row.CpuStartTimeMs.GetValueOrDefault(),
                DeltaMs = best.DeltaMs,
                Outcome = ClassifyPresentOutcome(best.Row),
                PresentMode = best.Row.PresentMode,
                UntilDisplayedMs = best.Row.UntilDisplayedMs,
                DisplayLatencyMs = best.Row.DisplayLatencyMs
            };
        }

        return new PresentMonAppCorrelation
        {
            Available = true,
            Reason = "Nearest selected PresentMon row by app UTC present timestamp.",
            AppPresentId = options.AppPresentId ?? 0,
            AppSourceSequenceNumber = options.AppSourceSequenceNumber ?? -1,
            AppPresentUtcUnixMs = appPresentUtcUnixMs,
            AppPresentOffsetMs = appOffsetMs,
            PresentMonRowIndex = best.Row.RowIndex,
            PresentMonCpuStartTimeMs = best.Row.CpuStartTimeMs.GetValueOrDefault(),
            DeltaMs = best.DeltaMs,
            Outcome = ClassifyPresentOutcome(best.Row),
            PresentMode = best.Row.PresentMode,
            UntilDisplayedMs = best.Row.UntilDisplayedMs,
            DisplayLatencyMs = best.Row.DisplayLatencyMs
        };
    }

    private static string ClassifyPresentOutcome(PresentMonRow row)
    {
        if (!row.DisplayedTimeMs.HasValue)
        {
            return "SupersededOrNotDisplayed";
        }

        if (row.UntilDisplayedMs.GetValueOrDefault() >= 16.0)
        {
            return "DisplayedLate";
        }

        return "Displayed";
    }
}
