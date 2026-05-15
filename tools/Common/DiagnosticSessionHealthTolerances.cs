namespace Sussudio.Tools;

internal static class DiagnosticSessionHealthTolerances
{
    internal static bool IsSourceSignalDiagnosticHealthObservation(DiagnosticHealthObservation observation)
        => DiagnosticSessionHealthPolicy.IsFailingDiagnosticHealthSeverity(observation.Severity) &&
           string.Equals(observation.LikelyStage, "source_signal", StringComparison.OrdinalIgnoreCase);

    internal static bool IsSourceCaptureDiagnosticHealthObservation(DiagnosticHealthObservation observation)
        => DiagnosticSessionHealthPolicy.IsFailingDiagnosticHealthSeverity(observation.Severity) &&
           string.Equals(observation.LikelyStage, "source_capture", StringComparison.OrdinalIgnoreCase);

    internal static bool IsPreviewSchedulerDiagnosticHealthObservation(DiagnosticHealthObservation observation)
        => DiagnosticSessionHealthPolicy.IsFailingDiagnosticHealthSeverity(observation.Severity) &&
           string.Equals(observation.LikelyStage, "preview_scheduler", StringComparison.OrdinalIgnoreCase);

    internal static bool IsFlashbackForceRotateDrainDiagnosticHealthObservation(DiagnosticHealthObservation observation)
        => DiagnosticSessionHealthPolicy.IsFailingDiagnosticHealthSeverity(observation.Severity) &&
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
