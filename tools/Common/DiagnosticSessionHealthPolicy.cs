using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal readonly record struct DiagnosticHealthObservation(
    string HealthStatus,
    string LikelyStage,
    string Evidence,
    long OffsetMs,
    int Severity);

internal static class DiagnosticSessionHealthPolicy
{
    private const double FlashbackDiagnosticWarmupFraction = 0.20;
    private const long FlashbackDiagnosticMaxWarmupMs = 10_000;

    internal static DiagnosticHealthObservation BuildSessionDiagnosticHealthObservation(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement finalSnapshot,
        bool isFlashbackScenario)
    {
        var worst = BuildWorstDiagnosticHealthObservation(samples, finalSnapshot);
        if (!isFlashbackScenario ||
            worst.Severity >= 3 ||
            samples.Count == 0)
        {
            return worst;
        }

        var finalOffsetMs = samples[^1].OffsetMs;
        if (finalOffsetMs <= 0)
        {
            return worst;
        }

        var warmupMs = Math.Min(
            FlashbackDiagnosticMaxWarmupMs,
            Math.Max(0, (long)Math.Ceiling(finalOffsetMs * FlashbackDiagnosticWarmupFraction)));
        return BuildWorstDiagnosticHealthObservationAfterOffset(samples, finalSnapshot, warmupMs);
    }

    internal static string GetDiagnosticHealthStatus(JsonElement snapshot)
        => GetString(snapshot, "DiagnosticHealthStatus") ?? "Unknown";

    internal static string GetDiagnosticLikelyStage(JsonElement snapshot)
        => GetString(snapshot, "DiagnosticLikelyStage") ?? "diagnostic_unavailable";

    internal static bool IsFailingDiagnosticHealthSeverity(int severity)
        => severity >= 2;

    private static DiagnosticHealthObservation BuildWorstDiagnosticHealthObservation(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement finalSnapshot)
    {
        var worst = BuildDiagnosticHealthObservation(
            finalSnapshot,
            samples.Count > 0 ? samples[^1].OffsetMs : 0);
        foreach (var sample in samples)
        {
            var observation = BuildDiagnosticHealthObservation(sample.Snapshot, sample.OffsetMs);
            if (observation.Severity > worst.Severity ||
                (observation.Severity == worst.Severity && observation.OffsetMs > worst.OffsetMs))
            {
                worst = observation;
            }
        }

        return worst;
    }

    private static DiagnosticHealthObservation BuildWorstDiagnosticHealthObservationAfterOffset(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement finalSnapshot,
        long minimumOffsetMs)
    {
        var worst = BuildDiagnosticHealthObservation(
            finalSnapshot,
            samples.Count > 0 ? samples[^1].OffsetMs : 0);
        foreach (var sample in samples)
        {
            if (sample.OffsetMs < minimumOffsetMs)
            {
                continue;
            }

            var observation = BuildDiagnosticHealthObservation(sample.Snapshot, sample.OffsetMs);
            if (observation.Severity > worst.Severity ||
                (observation.Severity == worst.Severity && observation.OffsetMs > worst.OffsetMs))
            {
                worst = observation;
            }
        }

        return worst;
    }

    private static DiagnosticHealthObservation BuildDiagnosticHealthObservation(JsonElement snapshot, long offsetMs)
    {
        var healthStatus = GetDiagnosticHealthStatus(snapshot);
        return new DiagnosticHealthObservation(
            healthStatus,
            GetDiagnosticLikelyStage(snapshot),
            GetString(snapshot, "DiagnosticEvidence") ?? string.Empty,
            offsetMs,
            GetDiagnosticHealthSeverity(healthStatus));
    }

    private static int GetDiagnosticHealthSeverity(string? healthStatus)
    {
        return (healthStatus ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "critical" or "failed" or "faulted" or "error" => 4,
            "degraded" => 3,
            "warning" => 2,
            "warmingup" => 1,
            _ => 0
        };
    }

    internal static bool IsSourceSignalDiagnosticHealthObservation(DiagnosticHealthObservation observation)
        => IsFailingDiagnosticHealthSeverity(observation.Severity) &&
           string.Equals(observation.LikelyStage, "source_signal", StringComparison.OrdinalIgnoreCase);

    internal static bool IsSourceCaptureDiagnosticHealthObservation(DiagnosticHealthObservation observation)
        => IsFailingDiagnosticHealthSeverity(observation.Severity) &&
           string.Equals(observation.LikelyStage, "source_capture", StringComparison.OrdinalIgnoreCase);

    internal static bool IsPreviewSchedulerDiagnosticHealthObservation(DiagnosticHealthObservation observation)
        => IsFailingDiagnosticHealthSeverity(observation.Severity) &&
           string.Equals(observation.LikelyStage, "preview_scheduler", StringComparison.OrdinalIgnoreCase);

    internal static bool IsFlashbackForceRotateDrainDiagnosticHealthObservation(DiagnosticHealthObservation observation)
        => IsFailingDiagnosticHealthSeverity(observation.Severity) &&
           string.Equals(observation.LikelyStage, "flashback_recording", StringComparison.OrdinalIgnoreCase) &&
           observation.Evidence.Contains("lastReject=force_rotate_draining", StringComparison.OrdinalIgnoreCase);

    internal static bool IsSparseSourceCaptureCadenceWarningRun(
        DiagnosticHealthObservation observation,
        SourceCadenceSessionMetrics sourceCadenceMetrics,
        long sourceReaderFramesDroppedDelta,
        long videoIngestErrorsDelta,
        int durationSeconds,
        bool visualCadenceHealthy)
    {
        if (!visualCadenceHealthy ||
            !IsSourceCaptureDiagnosticHealthObservation(observation) ||
            sourceReaderFramesDroppedDelta > 0 ||
            videoIngestErrorsDelta > 0 ||
            sourceCadenceMetrics.MaxDropPercentObserved > 0.1)
        {
            return false;
        }

        var allowedSparseEvents = Math.Max(1, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 180.0));
        return sourceCadenceMetrics.MaxEstimatedDroppedFramesObserved <= allowedSparseEvents &&
               sourceCadenceMetrics.MaxSevereGapCountObserved <= allowedSparseEvents;
    }

    internal static bool IsSparsePreviewSchedulerDeadlineDropRun(
        long deadlineDropsDelta,
        long underflowsDelta,
        int durationSeconds,
        bool visualCadenceHealthy)
    {
        if (!visualCadenceHealthy || deadlineDropsDelta <= 0 || underflowsDelta > 0)
        {
            return false;
        }

        var allowedDrops = Math.Max(2, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 10.0));
        return deadlineDropsDelta <= allowedDrops;
    }

    internal static bool IsSparsePreviewSchedulerStressRun(
        long deadlineDropsDelta,
        long underflowsDelta,
        int durationSeconds,
        bool visualCadenceHealthy)
    {
        if (!visualCadenceHealthy || deadlineDropsDelta <= 0 || underflowsDelta < 0)
        {
            return false;
        }

        var allowedDeadlineDrops = Math.Max(6, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 45.0));
        var allowedUnderflows = Math.Max(2, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 120.0));
        return deadlineDropsDelta <= allowedDeadlineDrops &&
               underflowsDelta <= allowedUnderflows;
    }

    internal static bool IsToleratedFlashbackScenarioWarning(
        string warning,
        bool toleratesSourceSignalHealthWarning,
        bool toleratesFlashbackForceRotateDrainWarning,
        bool toleratesPreviewCycleSchedulerWarning)
    {
        if (toleratesSourceSignalHealthWarning &&
            warning.StartsWith(
                "diagnostic health source-signal warning tolerated for export reliability scenario:",
                StringComparison.Ordinal))
        {
            return true;
        }

        if (toleratesFlashbackForceRotateDrainWarning &&
            warning.StartsWith(
                "diagnostic health flashback force-rotate drain warning tolerated for flashback scenario:",
                StringComparison.Ordinal))
        {
            return true;
        }

        return toleratesPreviewCycleSchedulerWarning &&
               warning.StartsWith(
                   "diagnostic health preview scheduler transition warning tolerated for preview-cycle scenario:",
                   StringComparison.Ordinal);
    }
}
