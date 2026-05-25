using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionHealthPolicy_OwnsHealthTolerances()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var policyText = ReadRepoFile("tools/Common/DiagnosticSessionHealthPolicy.cs")
            .Replace("\r\n", "\n");
        var tolerancesText = ReadRepoFile("tools/Common/DiagnosticSessionHealthTolerances.cs")
            .Replace("\r\n", "\n");

        AssertContains(policyText, "internal static class DiagnosticSessionHealthPolicy");
        AssertContains(policyText, "internal readonly record struct DiagnosticHealthObservation");
        AssertContains(policyText, "internal static DiagnosticHealthObservation BuildSessionDiagnosticHealthObservation(");
        AssertContains(policyText, "private static DiagnosticHealthObservation BuildWorstDiagnosticHealthObservationAfterOffset(");
        AssertContains(policyText, "private const double FlashbackDiagnosticWarmupFraction = 0.20;");
        AssertDoesNotContain(policyText, "internal static bool IsSparseSourceCaptureCadenceWarningRun(");
        AssertDoesNotContain(policyText, "internal static bool IsToleratedFlashbackScenarioWarning(");
        AssertContains(tolerancesText, "internal static class DiagnosticSessionHealthTolerances");
        AssertContains(tolerancesText, "internal static bool IsSourceSignalDiagnosticHealthObservation(");
        AssertContains(tolerancesText, "internal static bool IsSourceCaptureDiagnosticHealthObservation(");
        AssertContains(tolerancesText, "internal static bool IsPreviewSchedulerDiagnosticHealthObservation(");
        AssertContains(tolerancesText, "internal static bool IsFlashbackForceRotateDrainDiagnosticHealthObservation(");
        AssertContains(tolerancesText, "internal static bool IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(tolerancesText, "internal static bool IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(tolerancesText, "internal static bool IsSparsePreviewSchedulerStressRun(");
        AssertContains(tolerancesText, "internal static bool IsToleratedFlashbackScenarioWarning(");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionHealthPolicy;");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionHealthTolerances;");
        AssertDoesNotContain(runnerText, "private readonly record struct DiagnosticHealthObservation");
        AssertDoesNotContain(runnerText, "private static DiagnosticHealthObservation BuildSessionDiagnosticHealthObservation(");
        AssertDoesNotContain(runnerText, "private static bool IsSparseSourceCaptureCadenceWarningRun(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionScenarioPlan_OwnsScenarioFlags()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var bootstrapText = ReadDiagnosticSessionRunContextSource();
        var planText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioPlan.cs")
            .Replace("\r\n", "\n");
        var catalogText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.cs")
            .Replace("\r\n", "\n");

        AssertContains(catalogText, "internal static partial class DiagnosticSessionScenarioCatalog");
        AssertContains(catalogText, "TryGetEntry(normalized, out _)");
        AssertContains(catalogText, "internal const string HelpList =");
        AssertContains(catalogText, "internal const string Description =");
        AssertContains(catalogText, "internal static IReadOnlyList<string> Names => Entries.Select");
        AssertContains(catalogText, "TryGetEntry(scenario, out var entry) && entry.RequiresPreview");
        AssertContains(catalogText, "entry.FlashbackExportVerificationFileName");
        AssertContains(catalogText, "internal static IReadOnlyList<DiagnosticSessionScenarioCatalogEntry> Entries { get; }");
        AssertContains(catalogText, ".. CreateCoreScenarioEntries(),");
        AssertContains(catalogText, ".. CreateFlashbackPlaybackScenarioEntries(),");
        AssertContains(catalogText, ".. CreateFlashbackExportScenarioEntries(),");
        AssertContains(catalogText, ".. CreateFlashbackRecordingScenarioEntries(),");
        AssertContains(catalogText, "CreateCombinedScenarioEntry()");
        AssertContains(catalogText, "internal readonly record struct DiagnosticSessionScenarioCatalogEntry(");
        AssertContains(catalogText, "private static DiagnosticSessionScenarioCatalogEntry[] CreateCoreScenarioEntries()");
        AssertContains(catalogText, "new(Observe)");
        AssertContains(catalogText, "FlashbackExportVerificationFileName: \"flashback-stress-export.mp4\"");
        AssertContains(catalogText, "private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackPlaybackScenarioEntries()");
        AssertContains(catalogText, "FlashbackPlayback,");
        AssertContains(catalogText, "DiagnosticSessionScenarioPlan.Create(runFlashbackPlayback: true)");
        AssertContains(catalogText, "DiagnosticSessionScenarioPlan.Create(runFlashbackSegmentPlayback: true)");
        AssertContains(catalogText, "private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackExportScenarioEntries()");
        AssertContains(catalogText, "FlashbackExportVerificationFileName: \"flashback-range-export.mp4\"");
        AssertContains(catalogText, "DiagnosticSessionScenarioPlan.Create(runFlashbackPlaybackPreviewCycle: true)");
        AssertContains(catalogText, "private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackRecordingScenarioEntries()");
        AssertContains(catalogText, "RequiresRecording: true");
        AssertContains(catalogText, "DiagnosticSessionScenarioPlan.Create(runFlashbackExportRejected: true)");
        AssertContains(catalogText, "private static DiagnosticSessionScenarioCatalogEntry CreateCombinedScenarioEntry()");
        AssertContains(catalogText, "DiagnosticSessionScenarioPlan.Create(runCombined: true)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionScenarioCatalog.Entries.cs")),
            "Diagnostic session scenario entries folded into the catalog owner");
        AssertContains(planText, "internal readonly record struct DiagnosticSessionScenarioPlan(");
        AssertContains(planText, "internal static DiagnosticSessionScenarioPlan Create(");
        AssertContains(planText, "internal static DiagnosticSessionScenarioPlan From(string scenario)");
        AssertContains(planText, "DiagnosticSessionScenarioCatalog.TryGetEntry(scenario, out var entry)");
        AssertContains(planText, "? entry.Plan");
        AssertContains(planText, "internal bool RequiresFlashbackRecordingReadiness");
        AssertContains(planText, "internal bool UsesFlashbackScenarioWarningPolicy");
        AssertContains(planText, "internal bool ToleratesSourceSignalHealthWarning");
        AssertContains(planText, "internal bool ToleratesFlashbackForceRotateDrainWarning");
        AssertContains(planText, "internal bool IsPreviewCycleScenario");
        AssertContains(planText, "internal bool ToleratesSparsePreviewSchedulerStressTransitions");
        AssertContains(planText, "RunFlashbackSegmentPlayback");
        AssertContains(bootstrapText, "var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);");
        AssertContains(runnerText, "ScenarioPlan = RunBootstrap.ScenarioPlan;");
        AssertDoesNotContain(runnerText, "scenario == \"flashback-playback\"");
        AssertDoesNotContain(runnerText, "scenario == \"flashback-stress\"");
        AssertDoesNotContain(runnerText, "scenario == \"combined\"");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionScenarioSetup_OwnsInitialMutations()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var setupText = ReadDiagnosticSessionScenarioSetupSource();
        AssertContains(setupText, "internal static class DiagnosticSessionScenarioSetup");
        AssertContains(setupText, "internal static async Task<DiagnosticSessionScenarioSetupResult> RunAsync(");
        AssertContains(setupText, "SetupFlashbackStateAsync(");
        AssertContains(setupText, "StartPreviewIfNeededAsync(");
        AssertContains(setupText, "StartRecordingIfNeededAsync(");
        AssertContains(setupText, "internal readonly record struct DiagnosticSessionScenarioSetupResult(");
        AssertContains(setupText, "private readonly record struct DiagnosticSessionFlashbackSetupResult(");
        AssertContains(setupText, "DiagnosticSessionCommandChannel commandChannel,");
        AssertContains(setupText, "DiagnosticSessionScenarioCatalog.NeedsFlashback(scenario)");
        AssertContains(setupText, "scenarioPlan.RunFlashbackExportRejected");
        AssertContains(setupText, "AutomationCommandKind.SetFlashbackEnabled,");
        AssertContains(setupText, "actions.Add(\"flashback enabled\")");
        AssertContains(setupText, "actions.Add(\"flashback disabled for rejected export\")");
        AssertContains(setupText, "DiagnosticSessionScenarioCatalog.NeedsPreview(scenario)");
        AssertContains(setupText, "AutomationCommandKind.SetPreviewEnabled,");
        AssertContains(setupText, "actions.Add(\"preview started\")");
        AssertContains(setupText, "await tryWaitAsync(\"VideoFramesFlowing\", 15_000)");
        AssertContains(setupText, "DiagnosticSessionScenarioCatalog.NeedsRecording(scenario)");
        AssertContains(setupText, "WaitForFlashbackStressBufferReadyAsync(SendByNameAsync, cancellationToken)");
        AssertContains(setupText, "AutomationCommandKind.SetRecordingEnabled,");
        AssertContains(setupText, "actions.Add(\"recording started\")");
        AssertContains(setupText, "await tryWaitAsync(\"RecordingFileGrowing\", 20_000)");
        AssertContains(runnerText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertContains(runnerText, "context.CommandChannel,");
        AssertContains(runnerText, "startedPreview = setupResult.StartedPreview;");
        AssertContains(runnerText, "startedRecording = setupResult.StartedRecording;");
        AssertContains(runnerText, "enabledFlashback = setupResult.EnabledFlashback;");
        AssertContains(runnerText, "disabledFlashback = setupResult.DisabledFlashback;");
        AssertDoesNotContain(runnerText, "actions.Add(\"flashback enabled\")");
        AssertDoesNotContain(runnerText, "actions.Add(\"preview started\")");
        AssertDoesNotContain(runnerText, "actions.Add(\"recording started\")");
        AssertDoesNotContain(runnerText, "WaitForFlashbackStressBufferReadyAsync(");
        AssertDoesNotContain(setupText, "sendAsync(\"SetFlashbackEnabled\"");
        AssertDoesNotContain(setupText, "sendAsync(\"SetPreviewEnabled\"");
        AssertDoesNotContain(setupText, "sendAsync(\"SetRecordingEnabled\"");

        return Task.CompletedTask;
    }
}
