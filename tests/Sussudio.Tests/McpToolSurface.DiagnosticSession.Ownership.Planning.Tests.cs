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
