using System.Text.Json;
using Sussudio.Models;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;
using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal static class DiagnosticSessionScenarioCatalog
{
    internal const string Observe = "observe";
    internal const string PreviewOnly = "preview-only";
    internal const string RecordingOnly = "recording-only";
    internal const string Flashback = "flashback";
    internal const string FlashbackPlayback = "flashback-playback";
    internal const string FlashbackStress = "flashback-stress";
    internal const string FlashbackScrubStress = "flashback-scrub-stress";
    internal const string FlashbackRestartCycle = "flashback-restart-cycle";
    internal const string FlashbackEncoderCycle = "flashback-encoder-cycle";
    internal const string FlashbackExportPlayback = "flashback-export-playback";
    internal const string FlashbackSegmentPlayback = "flashback-segment-playback";
    internal const string FlashbackRangeExport = "flashback-range-export";
    internal const string FlashbackRangeExportAudioSwitch = "flashback-range-export-audio-switch";
    internal const string FlashbackLifecycle = "flashback-lifecycle";
    internal const string FlashbackExportConcurrent = "flashback-export-concurrent";
    internal const string FlashbackDisableDuringExport = "flashback-disable-during-export";
    internal const string FlashbackRotatedExport = "flashback-rotated-export";
    internal const string FlashbackPreviewCycle = "flashback-preview-cycle";
    internal const string FlashbackPlaybackPreviewCycle = "flashback-playback-preview-cycle";
    internal const string FlashbackRecording = "flashback-recording";
    internal const string FlashbackRecordingPreviewCycle = "flashback-recording-preview-cycle";
    internal const string FlashbackRecordingSettingsDeferred = "flashback-recording-settings-deferred";
    internal const string FlashbackRecordingExportRejected = "flashback-recording-export-rejected";
    internal const string FlashbackExportRejected = "flashback-export-rejected";
    internal const string Combined = "combined";
    internal const string HelpList =
        Observe + "|" + PreviewOnly + "|" + RecordingOnly + "|" + Flashback + "|" + FlashbackPlayback + "|" + FlashbackStress + "|" + FlashbackScrubStress + "|" + FlashbackRestartCycle + "|" + FlashbackEncoderCycle + "|" + FlashbackExportPlayback + "|" + FlashbackSegmentPlayback + "|" + FlashbackRangeExport + "|" + FlashbackRangeExportAudioSwitch + "|" + FlashbackLifecycle + "|" + FlashbackExportConcurrent + "|" + FlashbackDisableDuringExport + "|" + FlashbackRotatedExport + "|" + FlashbackPreviewCycle + "|" + FlashbackPlaybackPreviewCycle + "|" + FlashbackRecording + "|" + FlashbackRecordingPreviewCycle + "|" + FlashbackRecordingSettingsDeferred + "|" + FlashbackRecordingExportRejected + "|" + FlashbackExportRejected + "|" + Combined;
    internal const string Description =
        "Session scenario: observe, preview-only, recording-only, flashback, flashback-playback, flashback-stress, flashback-scrub-stress, flashback-restart-cycle, flashback-encoder-cycle, flashback-export-playback, flashback-segment-playback, flashback-range-export, flashback-range-export-audio-switch, flashback-lifecycle, flashback-export-concurrent, flashback-disable-during-export, flashback-rotated-export, flashback-preview-cycle, flashback-playback-preview-cycle, flashback-recording, flashback-recording-preview-cycle, flashback-recording-settings-deferred, flashback-recording-export-rejected, flashback-export-rejected, or combined.";

    internal static IReadOnlyList<DiagnosticSessionScenarioCatalogEntry> Entries { get; } =
    [
        .. CreateCoreScenarioEntries(),
        .. CreateFlashbackPlaybackScenarioEntries(),
        .. CreateFlashbackExportScenarioEntries(),
        .. CreateFlashbackRecordingScenarioEntries(),
        CreateCombinedScenarioEntry()
    ];

    internal static IReadOnlyList<string> Names => Entries.Select(static entry => entry.Name).ToArray();

    private static DiagnosticSessionScenarioCatalogEntry[] CreateCoreScenarioEntries()
        => [
        new(Observe),
        new(
            PreviewOnly,
            RequiresPreview: true),
        new(
            RecordingOnly,
            RequiresRecording: true),
        new(
            Flashback,
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-stress-export.mp4")
    ];

    private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackPlaybackScenarioEntries()
        => [
        new(
            FlashbackPlayback,
            DiagnosticSessionScenarioPlan.Create(runFlashbackPlayback: true),
            RequiresPreview: true,
            RequiresFlashback: true),
        new(
            FlashbackStress,
            DiagnosticSessionScenarioPlan.Create(runFlashbackStress: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-stress-export.mp4"),
        new(
            FlashbackScrubStress,
            DiagnosticSessionScenarioPlan.Create(runFlashbackScrubStress: true),
            RequiresPreview: true,
            RequiresFlashback: true),
        new(
            FlashbackRestartCycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRestartCycle: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-restart-cycle-export.mp4"),
        new(
            FlashbackEncoderCycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackEncoderCycle: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-encoder-cycle-export.mp4"),
        new(
            FlashbackExportPlayback,
            DiagnosticSessionScenarioPlan.Create(runFlashbackExportPlayback: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-export-playback.mp4"),
        new(
            FlashbackSegmentPlayback,
            DiagnosticSessionScenarioPlan.Create(runFlashbackSegmentPlayback: true),
            RequiresPreview: true,
            RequiresFlashback: true)
    ];

    private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackExportScenarioEntries()
        => [
        new(
            FlashbackRangeExport,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRangeExport: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-range-export.mp4"),
        new(
            FlashbackRangeExportAudioSwitch,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRangeExportAudioSwitch: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-range-export-audio-switch.mp4"),
        new(
            FlashbackLifecycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackLifecycle: true),
            RequiresPreview: true,
            RequiresFlashback: true),
        new(
            FlashbackExportConcurrent,
            DiagnosticSessionScenarioPlan.Create(runFlashbackExportConcurrent: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-concurrent-a.mp4"),
        new(
            FlashbackDisableDuringExport,
            DiagnosticSessionScenarioPlan.Create(runFlashbackDisableDuringExport: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-disable-during-export.mp4"),
        new(
            FlashbackRotatedExport,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRotatedExport: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-rotated-export.mp4"),
        new(
            FlashbackPreviewCycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackPreviewCycle: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-preview-off-export.mp4"),
        new(
            FlashbackPlaybackPreviewCycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackPlaybackPreviewCycle: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-playback-preview-cycle.mp4")
    ];

    private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackRecordingScenarioEntries()
        => [
        new(
            FlashbackRecording,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRecording: true),
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true),
        new(
            FlashbackRecordingPreviewCycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRecordingPreviewCycle: true),
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true),
        new(
            FlashbackRecordingSettingsDeferred,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRecordingSettingsDeferred: true),
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true),
        new(
            FlashbackRecordingExportRejected,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRecordingExportRejected: true),
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true),
        new(
            FlashbackExportRejected,
            DiagnosticSessionScenarioPlan.Create(runFlashbackExportRejected: true))
    ];

    private static DiagnosticSessionScenarioCatalogEntry CreateCombinedScenarioEntry()
        => new(
            Combined,
            DiagnosticSessionScenarioPlan.Create(runCombined: true),
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true);

    internal static string Normalize(string? scenario)
    {
        var normalized = string.IsNullOrWhiteSpace(scenario)
            ? Observe
            : scenario.Trim().ToLowerInvariant();

        if (TryGetEntry(normalized, out _))
        {
            return normalized;
        }

        throw new ArgumentException($"Unknown diagnostic session scenario '{scenario}'.", nameof(scenario));
    }

    internal static bool TryGetEntry(string scenario, out DiagnosticSessionScenarioCatalogEntry entry)
    {
        foreach (var candidate in Entries)
        {
            if (string.Equals(candidate.Name, scenario, StringComparison.Ordinal))
            {
                entry = candidate;
                return true;
            }
        }

        entry = default;
        return false;
    }

    internal static bool NeedsPreview(string scenario)
        => TryGetEntry(scenario, out var entry) && entry.RequiresPreview;

    internal static bool NeedsRecording(string scenario)
        => TryGetEntry(scenario, out var entry) && entry.RequiresRecording;

    internal static bool NeedsFlashback(string scenario)
        => TryGetEntry(scenario, out var entry) && entry.RequiresFlashback;

    internal static bool TryGetFlashbackExportVerificationPath(
        string scenario,
        string outputDirectory,
        out string exportPath)
    {
        var fileName = TryGetEntry(scenario, out var entry)
            ? entry.FlashbackExportVerificationFileName
            : null;
        exportPath = fileName is null ? string.Empty : Path.Combine(outputDirectory, fileName);

        return exportPath.Length > 0;
    }
}

internal readonly record struct DiagnosticSessionScenarioCatalogEntry(
    string Name,
    DiagnosticSessionScenarioPlan Plan = default,
    bool RequiresPreview = false,
    bool RequiresRecording = false,
    bool RequiresFlashback = false,
    string? FlashbackExportVerificationFileName = null);

internal readonly record struct DiagnosticSessionScenarioPlan(
    bool RunFlashbackPlayback,
    bool RunFlashbackStress,
    bool RunFlashbackScrubStress,
    bool RunFlashbackRestartCycle,
    bool RunFlashbackEncoderCycle,
    bool RunFlashbackExportPlayback,
    bool RunFlashbackSegmentPlayback,
    bool RunFlashbackRangeExport,
    bool RunFlashbackRangeExportAudioSwitch,
    bool RunFlashbackLifecycle,
    bool RunFlashbackExportConcurrent,
    bool RunFlashbackDisableDuringExport,
    bool RunFlashbackRotatedExport,
    bool RunFlashbackPreviewCycle,
    bool RunFlashbackPlaybackPreviewCycle,
    bool RunFlashbackRecording,
    bool RunFlashbackRecordingPreviewCycle,
    bool RunFlashbackRecordingSettingsDeferred,
    bool RunFlashbackRecordingExportRejected,
    bool RunFlashbackExportRejected,
    bool RunCombined)
{
    internal static DiagnosticSessionScenarioPlan Create(
        bool runFlashbackPlayback = false,
        bool runFlashbackStress = false,
        bool runFlashbackScrubStress = false,
        bool runFlashbackRestartCycle = false,
        bool runFlashbackEncoderCycle = false,
        bool runFlashbackExportPlayback = false,
        bool runFlashbackSegmentPlayback = false,
        bool runFlashbackRangeExport = false,
        bool runFlashbackRangeExportAudioSwitch = false,
        bool runFlashbackLifecycle = false,
        bool runFlashbackExportConcurrent = false,
        bool runFlashbackDisableDuringExport = false,
        bool runFlashbackRotatedExport = false,
        bool runFlashbackPreviewCycle = false,
        bool runFlashbackPlaybackPreviewCycle = false,
        bool runFlashbackRecording = false,
        bool runFlashbackRecordingPreviewCycle = false,
        bool runFlashbackRecordingSettingsDeferred = false,
        bool runFlashbackRecordingExportRejected = false,
        bool runFlashbackExportRejected = false,
        bool runCombined = false)
        => new(
            runFlashbackPlayback,
            runFlashbackStress,
            runFlashbackScrubStress,
            runFlashbackRestartCycle,
            runFlashbackEncoderCycle,
            runFlashbackExportPlayback,
            runFlashbackSegmentPlayback,
            runFlashbackRangeExport,
            runFlashbackRangeExportAudioSwitch,
            runFlashbackLifecycle,
            runFlashbackExportConcurrent,
            runFlashbackDisableDuringExport,
            runFlashbackRotatedExport,
            runFlashbackPreviewCycle,
            runFlashbackPlaybackPreviewCycle,
            runFlashbackRecording,
            runFlashbackRecordingPreviewCycle,
            runFlashbackRecordingSettingsDeferred,
            runFlashbackRecordingExportRejected,
            runFlashbackExportRejected,
            runCombined);

    internal static DiagnosticSessionScenarioPlan From(string scenario)
        => DiagnosticSessionScenarioCatalog.TryGetEntry(scenario, out var entry)
            ? entry.Plan
            : default;

    internal bool RequiresFlashbackRecordingReadiness
        => RunFlashbackRecording ||
           RunFlashbackRecordingPreviewCycle ||
           RunFlashbackRecordingSettingsDeferred ||
           RunFlashbackRecordingExportRejected;

    internal bool RequiresFlashbackRecordingValidation
        => RequiresFlashbackRecordingReadiness;

    internal bool UsesFlashbackScenarioWarningPolicy
        => RunFlashbackPlayback ||
           RunFlashbackStress ||
           RunFlashbackScrubStress ||
           RunFlashbackRestartCycle ||
           RunFlashbackEncoderCycle ||
           RunFlashbackExportPlayback ||
           RunFlashbackSegmentPlayback ||
           RunFlashbackRangeExport ||
           RunFlashbackRangeExportAudioSwitch ||
           RunFlashbackLifecycle ||
           RunFlashbackExportConcurrent ||
           RunFlashbackDisableDuringExport ||
           RunFlashbackRotatedExport ||
           RunFlashbackPreviewCycle ||
           RunFlashbackPlaybackPreviewCycle ||
           RunFlashbackRecording ||
           RunFlashbackRecordingPreviewCycle ||
           RunFlashbackRecordingSettingsDeferred ||
           RunFlashbackRecordingExportRejected ||
           RunFlashbackExportRejected ||
           RunCombined;

    internal bool ToleratesSourceSignalHealthWarning
        => RunFlashbackRangeExport ||
           RunFlashbackRangeExportAudioSwitch ||
           RunFlashbackExportConcurrent ||
           RunFlashbackDisableDuringExport ||
           RunFlashbackRotatedExport ||
           RunFlashbackPreviewCycle ||
           RunFlashbackPlaybackPreviewCycle;

    internal bool ToleratesFlashbackForceRotateDrainWarning
        => RunFlashbackExportPlayback ||
           RunFlashbackScrubStress ||
           RunFlashbackRangeExport ||
           RunFlashbackRangeExportAudioSwitch ||
           RunFlashbackExportConcurrent ||
           RunFlashbackDisableDuringExport ||
           RunFlashbackRotatedExport;

    internal bool IsPreviewCycleScenario
        => RunFlashbackPreviewCycle ||
           RunFlashbackPlaybackPreviewCycle ||
           RunFlashbackRecordingPreviewCycle;

    internal bool ToleratesSparsePreviewSchedulerStressTransitions
        => RunFlashbackScrubStress || RunFlashbackSegmentPlayback;
}

internal static class DiagnosticSessionScenarioStartup
{
    internal static async Task<DiagnosticSessionScenarioStartupResult> StartAsync(
        DiagnosticSessionOptions options,
        DiagnosticSessionScenarioPlan scenarioPlan,
        int durationSeconds,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendRawWithConnectRetryAsync,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendAsyncWithFailurePolicy,
        CancellationToken cancellationToken)
    {
        await StartPresentMonAsync(
                options,
                durationSeconds,
                outputDirectory,
                backgroundTasks,
                actions,
                sendAsync)
            .ConfigureAwait(false);

        RegisterFlashbackScenarioTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            sendRawWithConnectRetryAsync,
            cancellationToken);

        RegisterDeferredFlashbackRecordingSettingsTask(
            scenarioPlan,
            backgroundTasks,
            actions,
            warnings,
            sendAsyncWithFailurePolicy,
            cancellationToken);

        var startedFlashbackPlayback = await TryStartFlashbackPlaybackAsync(
                scenarioPlan,
                actions,
                warnings,
                sendAsync,
                cancellationToken)
            .ConfigureAwait(false);

        return new DiagnosticSessionScenarioStartupResult(startedFlashbackPlayback);
    }

    private static async Task StartPresentMonAsync(
        DiagnosticSessionOptions options,
        int durationSeconds,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync)
    {
        if (!options.IncludePresentMon)
        {
            return;
        }

        var correlationSnapshotResponse = await sendAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(correlationSnapshotResponse, out var correlationSnapshot);
        backgroundTasks.SetPresentMon(PresentMonProbe.RunAsync(PresentMonProbe.CreateOptions(
            durationSeconds: Math.Max(1, durationSeconds),
            processName: "Sussudio",
            presentMonPath: options.PresentMonPath,
            outputFile: Path.Combine(outputDirectory, "presentmon.csv"),
            keepCsv: true,
            correlation: PresentMonProbe.ReadPreviewCorrelation(correlationSnapshot))));
        actions.Add("presentmon capture started");
    }

    private static void RegisterFlashbackScenarioTasks(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendRawWithConnectRetryAsync,
        CancellationToken cancellationToken)
    {
        DiagnosticSessionFlashbackStressScenario.RegisterSelectedFlashbackStressScenarioTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            sendRawWithConnectRetryAsync,
            cancellationToken);

        DiagnosticSessionFlashbackCycleScenarios.RegisterSelectedFlashbackCycleScenarioTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            cancellationToken);

        DiagnosticSessionFlashbackSegmentPlaybackScenarios.RegisterSelectedFlashbackSegmentPlaybackScenarioTask(
            scenarioPlan,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            cancellationToken);

        DiagnosticSessionFlashbackExportScenarios.RegisterSelectedFlashbackExportScenarioTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            sendRawWithConnectRetryAsync,
            cancellationToken);

        DiagnosticSessionFlashbackLifecycleScenarios.RegisterSelectedFlashbackLifecycleScenarioTask(
            scenarioPlan,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            cancellationToken);

        DiagnosticSessionFlashbackPreviewCycleScenarios.RegisterSelectedFlashbackPreviewCycleScenarioTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            cancellationToken);
    }

    private static void RegisterDeferredFlashbackRecordingSettingsTask(
        DiagnosticSessionScenarioPlan scenarioPlan,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendAsyncWithFailurePolicy,
        CancellationToken cancellationToken)
    {
        if (!scenarioPlan.RunFlashbackRecordingSettingsDeferred)
        {
            return;
        }

        backgroundTasks.SetRecordingSettingsDeferred(RunFlashbackRecordingSettingsDeferredAsync(
            actions,
            warnings,
            sendAsyncWithFailurePolicy,
            cancellationToken));
        actions.Add("flashback recording settings deferred started");
    }

    private static async Task<bool> TryStartFlashbackPlaybackAsync(
        DiagnosticSessionScenarioPlan scenarioPlan,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        CancellationToken cancellationToken)
    {
        if (!scenarioPlan.RunFlashbackPlayback)
        {
            return false;
        }

        if (!await WaitForFlashbackStressBufferReadyAsync(sendAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback playback: Flashback buffer did not become playback-ready within 30s");
        }

        var playResponse = await sendAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "play", ["positionMs"] = 1000 },
                null)
            .ConfigureAwait(false);
        if (!IsSuccess(playResponse))
        {
            warnings.Add($"flashback playback: play command failed - {Get(playResponse, "Message", "unknown error")}");
            return false;
        }

        actions.Add("flashback playback started at 1000ms");
        var playingSnapshot = await WaitForFlashbackPlaybackStateAsync(
                sendAsync,
                "Playing",
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);
        if (playingSnapshot is null)
        {
            warnings.Add("flashback playback: playback did not report Playing within 5s");
        }

        return true;
    }
}

internal readonly record struct DiagnosticSessionScenarioStartupResult(bool StartedFlashbackPlayback);

internal static class DiagnosticSessionScenarioSetup
{
    internal static async Task<DiagnosticSessionScenarioSetupResult> RunAsync(
        string scenario,
        DiagnosticSessionScenarioPlan scenarioPlan,
        JsonElement initialSnapshot,
        List<string> actions,
        List<string> warnings,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, Task> tryWaitAsync,
        CancellationToken cancellationToken)
    {
        var flashbackSetup = await SetupFlashbackStateAsync(
                scenario,
                scenarioPlan,
                initialSnapshot,
                actions,
                commandChannel)
            .ConfigureAwait(false);
        var startedPreview = await StartPreviewIfNeededAsync(
                scenario,
                initialSnapshot,
                actions,
                commandChannel,
                tryWaitAsync)
            .ConfigureAwait(false);
        var startedRecording = await StartRecordingIfNeededAsync(
                scenario,
                scenarioPlan,
                initialSnapshot,
                actions,
                warnings,
                commandChannel,
                tryWaitAsync,
                cancellationToken)
            .ConfigureAwait(false);

        return new DiagnosticSessionScenarioSetupResult(
            startedPreview,
            startedRecording,
            flashbackSetup.EnabledFlashback,
            flashbackSetup.DisabledFlashback);
    }

    private static async Task<DiagnosticSessionFlashbackSetupResult> SetupFlashbackStateAsync(
        string scenario,
        DiagnosticSessionScenarioPlan scenarioPlan,
        JsonElement initialSnapshot,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel)
    {
        var enabledFlashback = false;
        var disabledFlashback = false;

        if (DiagnosticSessionScenarioCatalog.NeedsFlashback(scenario) && !GetBool(initialSnapshot, "FlashbackActive"))
        {
            await commandChannel.SendAsync(
                    AutomationCommandKind.SetFlashbackEnabled,
                    new Dictionary<string, object?> { ["enabled"] = true },
                    null)
                .ConfigureAwait(false);
            enabledFlashback = true;
            actions.Add("flashback enabled");
        }

        if (scenarioPlan.RunFlashbackExportRejected && GetBool(initialSnapshot, "FlashbackActive"))
        {
            await commandChannel.SendAsync(
                    AutomationCommandKind.SetFlashbackEnabled,
                    new Dictionary<string, object?> { ["enabled"] = false },
                    null)
                .ConfigureAwait(false);
            disabledFlashback = true;
            actions.Add("flashback disabled for rejected export");
        }

        return new DiagnosticSessionFlashbackSetupResult(enabledFlashback, disabledFlashback);
    }

    private static async Task<bool> StartPreviewIfNeededAsync(
        string scenario,
        JsonElement initialSnapshot,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, Task> tryWaitAsync)
    {
        if (!DiagnosticSessionScenarioCatalog.NeedsPreview(scenario) || GetBool(initialSnapshot, "IsPreviewing"))
        {
            return false;
        }

        await commandChannel.SendAsync(
                AutomationCommandKind.SetPreviewEnabled,
                new Dictionary<string, object?> { ["enabled"] = true },
                null)
            .ConfigureAwait(false);
        actions.Add("preview started");
        await tryWaitAsync("VideoFramesFlowing", 15_000).ConfigureAwait(false);

        return true;
    }

    private static async Task<bool> StartRecordingIfNeededAsync(
        string scenario,
        DiagnosticSessionScenarioPlan scenarioPlan,
        JsonElement initialSnapshot,
        List<string> actions,
        List<string> warnings,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, Task> tryWaitAsync,
        CancellationToken cancellationToken)
    {
        if (!DiagnosticSessionScenarioCatalog.NeedsRecording(scenario) || GetBool(initialSnapshot, "IsRecording"))
        {
            return false;
        }

        Task<JsonElement> SendByNameAsync(string command, Dictionary<string, object?>? payload, int? timeoutMs)
            => commandChannel.SendAsync(command, payload, timeoutMs);

        if (scenarioPlan.RequiresFlashbackRecordingReadiness &&
            !await WaitForFlashbackStressBufferReadyAsync(SendByNameAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback recording: Flashback buffer did not become recording-ready within 30s");
        }

        await commandChannel.SendAsync(
                AutomationCommandKind.SetRecordingEnabled,
                new Dictionary<string, object?> { ["enabled"] = true },
                null)
            .ConfigureAwait(false);
        actions.Add("recording started");
        await tryWaitAsync("RecordingFileGrowing", 20_000).ConfigureAwait(false);

        return true;
    }

    private readonly record struct DiagnosticSessionFlashbackSetupResult(
        bool EnabledFlashback,
        bool DisabledFlashback);
}

internal readonly record struct DiagnosticSessionScenarioSetupResult(
    bool StartedPreview,
    bool StartedRecording,
    bool EnabledFlashback,
    bool DisabledFlashback);
