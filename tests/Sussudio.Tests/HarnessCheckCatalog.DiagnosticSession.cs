using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddDiagnosticSessionChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Diagnostic session sampler has a named owner",
            DiagnosticSessionSampler_OwnsSampleLoopOrdering);
        await AddCheckAsync(results,
            "Diagnostic session metrics have a named owner",
            DiagnosticSessionMetrics_OwnsSessionMetricProjection);
        await AddCheckAsync(results,
            "Diagnostic session health policy has a named owner",
            DiagnosticSessionHealthPolicy_OwnsHealthTolerances);
        await AddCheckAsync(results,
            "Diagnostic session runner verifies flashback export during playback",
            DiagnosticSessionRunner_VerifiesFlashbackExportPlaybackCommandFlow);
        await AddCheckAsync(results,
            "Diagnostic session runner ignores transient flashback warmup warnings",
            DiagnosticSessionRunner_IgnoresTransientFlashbackWarmupWarnings);
        await AddCheckAsync(results,
            "Diagnostic session runner tolerates sparse source cadence warnings only without source drops",
            DiagnosticSessionRunner_ToleratesSparseSourceCadenceWarningsOnlyWithoutSourceDrops);
        await AddCheckAsync(results,
            "Diagnostic session runner fails unknown initial snapshot without mutating state",
            DiagnosticSessionRunner_UnknownInitialSnapshotFailsWithoutMutatingState);
        await AddCheckAsync(results,
            "Diagnostic session runner retries synthetic pipe connect failures",
            DiagnosticSessionRunner_RetriesSyntheticPipeConnectFailures);
        await AddCheckAsync(results,
            "Diagnostic session runner rejects concurrent invocation on same output directory",
            DiagnosticSessionRunner_RejectsConcurrentInvocationOnSameOutputDirectory);
    }
}
