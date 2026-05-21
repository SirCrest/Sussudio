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
        var bootstrapText = ReadRepoFile("tools/Common/DiagnosticSessionRunBootstrap.cs")
            .Replace("\r\n", "\n");
        var planText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioPlan.cs")
            .Replace("\r\n", "\n");
        var planPoliciesText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioPlan.Policies.cs")
            .Replace("\r\n", "\n");
        var catalogText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.cs")
            .Replace("\r\n", "\n");
        var catalogNamesText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Names.cs")
            .Replace("\r\n", "\n");
        var catalogRequirementsText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Requirements.cs")
            .Replace("\r\n", "\n");
        var catalogEntriesText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Entries.cs")
            .Replace("\r\n", "\n");
        var catalogCoreEntriesText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Entries.Core.cs")
            .Replace("\r\n", "\n");
        var catalogPlaybackEntriesText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Entries.FlashbackPlayback.cs")
            .Replace("\r\n", "\n");
        var catalogExportEntriesText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Entries.FlashbackExport.cs")
            .Replace("\r\n", "\n");
        var catalogRecordingEntriesText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Entries.FlashbackRecording.cs")
            .Replace("\r\n", "\n");
        var catalogCombinedEntriesText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.Entries.Combined.cs")
            .Replace("\r\n", "\n");

        AssertContains(catalogText, "internal static partial class DiagnosticSessionScenarioCatalog");
        AssertContains(catalogText, "TryGetEntry(normalized, out _)");
        AssertDoesNotContain(catalogText, "internal const string HelpList =");
        AssertDoesNotContain(catalogText, "internal const string Description =");
        AssertDoesNotContain(catalogText, "entry.RequiresPreview");
        AssertDoesNotContain(catalogText, "entry.FlashbackExportVerificationFileName");
        AssertContains(catalogNamesText, "internal static partial class DiagnosticSessionScenarioCatalog");
        AssertContains(catalogNamesText, "internal const string HelpList =");
        AssertContains(catalogNamesText, "internal const string Description =");
        AssertContains(catalogNamesText, "internal static IReadOnlyList<string> Names => Entries.Select");
        AssertDoesNotContain(catalogNamesText, "NeedsPreview(");
        AssertDoesNotContain(catalogNamesText, "TryGetFlashbackExportVerificationPath(");
        AssertContains(catalogRequirementsText, "internal static partial class DiagnosticSessionScenarioCatalog");
        AssertContains(catalogRequirementsText, "TryGetEntry(scenario, out var entry) && entry.RequiresPreview");
        AssertContains(catalogRequirementsText, "entry.FlashbackExportVerificationFileName");
        AssertDoesNotContain(catalogRequirementsText, "internal const string HelpList =");
        AssertDoesNotContain(catalogText, "internal static IReadOnlyList<DiagnosticSessionScenarioCatalogEntry> Entries { get; }");
        AssertContains(catalogEntriesText, "internal static partial class DiagnosticSessionScenarioCatalog");
        AssertContains(catalogEntriesText, "internal static IReadOnlyList<DiagnosticSessionScenarioCatalogEntry> Entries { get; }");
        AssertContains(catalogEntriesText, ".. CreateCoreScenarioEntries(),");
        AssertContains(catalogEntriesText, ".. CreateFlashbackPlaybackScenarioEntries(),");
        AssertContains(catalogEntriesText, ".. CreateFlashbackExportScenarioEntries(),");
        AssertContains(catalogEntriesText, ".. CreateFlashbackRecordingScenarioEntries(),");
        AssertContains(catalogEntriesText, "CreateCombinedScenarioEntry()");
        AssertContains(catalogEntriesText, "internal readonly record struct DiagnosticSessionScenarioCatalogEntry(");
        AssertDoesNotContain(catalogEntriesText, "DiagnosticSessionScenarioPlan.Create(runFlashbackPlayback: true)");
        AssertContains(catalogCoreEntriesText, "private static DiagnosticSessionScenarioCatalogEntry[] CreateCoreScenarioEntries()");
        AssertContains(catalogCoreEntriesText, "new(Observe)");
        AssertContains(catalogCoreEntriesText, "FlashbackExportVerificationFileName: \"flashback-stress-export.mp4\"");
        AssertContains(catalogPlaybackEntriesText, "private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackPlaybackScenarioEntries()");
        AssertContains(catalogPlaybackEntriesText, "FlashbackPlayback,");
        AssertContains(catalogPlaybackEntriesText, "DiagnosticSessionScenarioPlan.Create(runFlashbackPlayback: true)");
        AssertContains(catalogPlaybackEntriesText, "DiagnosticSessionScenarioPlan.Create(runFlashbackSegmentPlayback: true)");
        AssertContains(catalogExportEntriesText, "private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackExportScenarioEntries()");
        AssertContains(catalogExportEntriesText, "FlashbackExportVerificationFileName: \"flashback-range-export.mp4\"");
        AssertContains(catalogExportEntriesText, "DiagnosticSessionScenarioPlan.Create(runFlashbackPlaybackPreviewCycle: true)");
        AssertContains(catalogRecordingEntriesText, "private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackRecordingScenarioEntries()");
        AssertContains(catalogRecordingEntriesText, "RequiresRecording: true");
        AssertContains(catalogRecordingEntriesText, "DiagnosticSessionScenarioPlan.Create(runFlashbackExportRejected: true)");
        AssertContains(catalogCombinedEntriesText, "private static DiagnosticSessionScenarioCatalogEntry CreateCombinedScenarioEntry()");
        AssertContains(catalogCombinedEntriesText, "DiagnosticSessionScenarioPlan.Create(runCombined: true)");
        AssertContains(planText, "internal readonly partial record struct DiagnosticSessionScenarioPlan(");
        AssertContains(planText, "internal static DiagnosticSessionScenarioPlan Create(");
        AssertContains(planText, "internal static DiagnosticSessionScenarioPlan From(string scenario)");
        AssertContains(planText, "DiagnosticSessionScenarioCatalog.TryGetEntry(scenario, out var entry)");
        AssertContains(planText, "? entry.Plan");
        AssertDoesNotContain(planText, "internal bool RequiresFlashbackRecordingReadiness");
        AssertDoesNotContain(planText, "internal bool UsesFlashbackScenarioWarningPolicy");
        AssertContains(planPoliciesText, "internal readonly partial record struct DiagnosticSessionScenarioPlan");
        AssertContains(planPoliciesText, "internal bool RequiresFlashbackRecordingReadiness");
        AssertContains(planPoliciesText, "internal bool UsesFlashbackScenarioWarningPolicy");
        AssertContains(planPoliciesText, "internal bool ToleratesSourceSignalHealthWarning");
        AssertContains(planPoliciesText, "internal bool ToleratesFlashbackForceRotateDrainWarning");
        AssertContains(planPoliciesText, "internal bool IsPreviewCycleScenario");
        AssertContains(planPoliciesText, "internal bool ToleratesSparsePreviewSchedulerStressTransitions");
        AssertContains(planPoliciesText, "RunFlashbackSegmentPlayback");
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
        var setupRootText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioSetup.cs")
            .Replace("\r\n", "\n");
        var setupFlashbackText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioSetup.Flashback.cs")
            .Replace("\r\n", "\n");
        var setupPreviewText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioSetup.Preview.cs")
            .Replace("\r\n", "\n");
        var setupRecordingText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioSetup.Recording.cs")
            .Replace("\r\n", "\n");
        var setupResultsText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioSetup.Results.cs")
            .Replace("\r\n", "\n");

        AssertContains(setupRootText, "internal static partial class DiagnosticSessionScenarioSetup");
        AssertContains(setupRootText, "internal static async Task<DiagnosticSessionScenarioSetupResult> RunAsync(");
        AssertContains(setupRootText, "SetupFlashbackStateAsync(");
        AssertContains(setupRootText, "StartPreviewIfNeededAsync(");
        AssertContains(setupRootText, "StartRecordingIfNeededAsync(");
        AssertContains(setupResultsText, "internal readonly record struct DiagnosticSessionScenarioSetupResult(");
        AssertContains(setupResultsText, "private readonly record struct DiagnosticSessionFlashbackSetupResult(");
        AssertContains(setupText, "DiagnosticSessionCommandChannel commandChannel,");
        AssertContains(setupFlashbackText, "DiagnosticSessionScenarioCatalog.NeedsFlashback(scenario)");
        AssertContains(setupFlashbackText, "scenarioPlan.RunFlashbackExportRejected");
        AssertContains(setupFlashbackText, "AutomationCommandKind.SetFlashbackEnabled,");
        AssertContains(setupFlashbackText, "actions.Add(\"flashback enabled\")");
        AssertContains(setupFlashbackText, "actions.Add(\"flashback disabled for rejected export\")");
        AssertContains(setupPreviewText, "DiagnosticSessionScenarioCatalog.NeedsPreview(scenario)");
        AssertContains(setupPreviewText, "AutomationCommandKind.SetPreviewEnabled,");
        AssertContains(setupPreviewText, "actions.Add(\"preview started\")");
        AssertContains(setupPreviewText, "await tryWaitAsync(\"VideoFramesFlowing\", 15_000)");
        AssertContains(setupRecordingText, "DiagnosticSessionScenarioCatalog.NeedsRecording(scenario)");
        AssertContains(setupRecordingText, "WaitForFlashbackStressBufferReadyAsync(SendByNameAsync, cancellationToken)");
        AssertContains(setupRecordingText, "AutomationCommandKind.SetRecordingEnabled,");
        AssertContains(setupRecordingText, "actions.Add(\"recording started\")");
        AssertContains(setupRecordingText, "await tryWaitAsync(\"RecordingFileGrowing\", 20_000)");
        AssertDoesNotContain(setupRootText, "AutomationCommandKind.SetFlashbackEnabled");
        AssertDoesNotContain(setupRootText, "AutomationCommandKind.SetPreviewEnabled");
        AssertDoesNotContain(setupRootText, "AutomationCommandKind.SetRecordingEnabled");
        AssertDoesNotContain(setupFlashbackText, "SetPreviewEnabled");
        AssertDoesNotContain(setupFlashbackText, "SetRecordingEnabled");
        AssertDoesNotContain(setupPreviewText, "SetFlashbackEnabled");
        AssertDoesNotContain(setupPreviewText, "SetRecordingEnabled");
        AssertDoesNotContain(setupRecordingText, "SetPreviewEnabled");
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
