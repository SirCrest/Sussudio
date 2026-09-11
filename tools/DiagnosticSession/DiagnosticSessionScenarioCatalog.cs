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
        new(Observe, DiagnosticSessionScenarioKind.Observe),
        new(
            PreviewOnly,
            DiagnosticSessionScenarioKind.PreviewOnly,
            RequiresPreview: true),
        new(
            RecordingOnly,
            DiagnosticSessionScenarioKind.RecordingOnly,
            RequiresRecording: true),
        new(
            Flashback,
            DiagnosticSessionScenarioKind.Flashback,
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-stress-export.mp4")
    ];

    private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackPlaybackScenarioEntries()
        => [
        new(
            FlashbackPlayback,
            DiagnosticSessionScenarioKind.FlashbackPlayback,
            RequiresPreview: true,
            RequiresFlashback: true),
        new(
            FlashbackStress,
            DiagnosticSessionScenarioKind.FlashbackStress,
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-stress-export.mp4"),
        new(
            FlashbackScrubStress,
            DiagnosticSessionScenarioKind.FlashbackScrubStress,
            RequiresPreview: true,
            RequiresFlashback: true),
        new(
            FlashbackRestartCycle,
            DiagnosticSessionScenarioKind.FlashbackRestartCycle,
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-restart-cycle-export.mp4"),
        new(
            FlashbackEncoderCycle,
            DiagnosticSessionScenarioKind.FlashbackEncoderCycle,
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-encoder-cycle-export.mp4"),
        new(
            FlashbackExportPlayback,
            DiagnosticSessionScenarioKind.FlashbackExportPlayback,
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-export-playback.mp4"),
        new(
            FlashbackSegmentPlayback,
            DiagnosticSessionScenarioKind.FlashbackSegmentPlayback,
            RequiresPreview: true,
            RequiresFlashback: true)
    ];

    private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackExportScenarioEntries()
        => [
        new(
            FlashbackRangeExport,
            DiagnosticSessionScenarioKind.FlashbackRangeExport,
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-range-export.mp4"),
        new(
            FlashbackRangeExportAudioSwitch,
            DiagnosticSessionScenarioKind.FlashbackRangeExportAudioSwitch,
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-range-export-audio-switch.mp4"),
        new(
            FlashbackLifecycle,
            DiagnosticSessionScenarioKind.FlashbackLifecycle,
            RequiresPreview: true,
            RequiresFlashback: true),
        new(
            FlashbackExportConcurrent,
            DiagnosticSessionScenarioKind.FlashbackExportConcurrent,
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-concurrent-a.mp4"),
        new(
            FlashbackDisableDuringExport,
            DiagnosticSessionScenarioKind.FlashbackDisableDuringExport,
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-disable-during-export.mp4"),
        new(
            FlashbackRotatedExport,
            DiagnosticSessionScenarioKind.FlashbackRotatedExport,
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-rotated-export.mp4"),
        new(
            FlashbackPreviewCycle,
            DiagnosticSessionScenarioKind.FlashbackPreviewCycle,
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-preview-off-export.mp4"),
        new(
            FlashbackPlaybackPreviewCycle,
            DiagnosticSessionScenarioKind.FlashbackPlaybackPreviewCycle,
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-playback-preview-cycle.mp4")
    ];

    private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackRecordingScenarioEntries()
        => [
        new(
            FlashbackRecording,
            DiagnosticSessionScenarioKind.FlashbackRecording,
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true),
        new(
            FlashbackRecordingPreviewCycle,
            DiagnosticSessionScenarioKind.FlashbackRecordingPreviewCycle,
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true),
        new(
            FlashbackRecordingSettingsDeferred,
            DiagnosticSessionScenarioKind.FlashbackRecordingSettingsDeferred,
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true),
        new(
            FlashbackRecordingExportRejected,
            DiagnosticSessionScenarioKind.FlashbackRecordingExportRejected,
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true),
        new(
            FlashbackExportRejected,
            DiagnosticSessionScenarioKind.FlashbackExportRejected)
    ];

    private static DiagnosticSessionScenarioCatalogEntry CreateCombinedScenarioEntry()
        => new(
            Combined,
            DiagnosticSessionScenarioKind.Combined,
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
    DiagnosticSessionScenarioKind Kind,
    bool RequiresPreview = false,
    bool RequiresRecording = false,
    bool RequiresFlashback = false,
    string? FlashbackExportVerificationFileName = null)
{
    internal DiagnosticSessionScenarioPlan Plan => new(Kind);
}

internal enum DiagnosticSessionScenarioKind
{
    Observe,
    PreviewOnly,
    RecordingOnly,
    Flashback,
    FlashbackPlayback,
    FlashbackStress,
    FlashbackScrubStress,
    FlashbackRestartCycle,
    FlashbackEncoderCycle,
    FlashbackExportPlayback,
    FlashbackSegmentPlayback,
    FlashbackRangeExport,
    FlashbackRangeExportAudioSwitch,
    FlashbackLifecycle,
    FlashbackExportConcurrent,
    FlashbackDisableDuringExport,
    FlashbackRotatedExport,
    FlashbackPreviewCycle,
    FlashbackPlaybackPreviewCycle,
    FlashbackRecording,
    FlashbackRecordingPreviewCycle,
    FlashbackRecordingSettingsDeferred,
    FlashbackRecordingExportRejected,
    FlashbackExportRejected,
    Combined
}

internal readonly record struct DiagnosticSessionScenarioPlan(DiagnosticSessionScenarioKind Kind)
{
    internal static DiagnosticSessionScenarioPlan From(string scenario)
        => DiagnosticSessionScenarioCatalog.TryGetEntry(scenario, out var entry)
            ? entry.Plan
            : default;

    internal bool RequiresFlashbackRecordingReadiness
        => Kind is DiagnosticSessionScenarioKind.FlashbackRecording or
           DiagnosticSessionScenarioKind.FlashbackRecordingPreviewCycle or
           DiagnosticSessionScenarioKind.FlashbackRecordingSettingsDeferred or
           DiagnosticSessionScenarioKind.FlashbackRecordingExportRejected;

    internal bool RequiresFlashbackRecordingValidation
        => RequiresFlashbackRecordingReadiness;

    internal bool UsesFlashbackScenarioWarningPolicy
        => Kind is DiagnosticSessionScenarioKind.FlashbackPlayback or
           DiagnosticSessionScenarioKind.FlashbackStress or
           DiagnosticSessionScenarioKind.FlashbackScrubStress or
           DiagnosticSessionScenarioKind.FlashbackRestartCycle or
           DiagnosticSessionScenarioKind.FlashbackEncoderCycle or
           DiagnosticSessionScenarioKind.FlashbackExportPlayback or
           DiagnosticSessionScenarioKind.FlashbackSegmentPlayback or
           DiagnosticSessionScenarioKind.FlashbackRangeExport or
           DiagnosticSessionScenarioKind.FlashbackRangeExportAudioSwitch or
           DiagnosticSessionScenarioKind.FlashbackLifecycle or
           DiagnosticSessionScenarioKind.FlashbackExportConcurrent or
           DiagnosticSessionScenarioKind.FlashbackDisableDuringExport or
           DiagnosticSessionScenarioKind.FlashbackRotatedExport or
           DiagnosticSessionScenarioKind.FlashbackPreviewCycle or
           DiagnosticSessionScenarioKind.FlashbackPlaybackPreviewCycle or
           DiagnosticSessionScenarioKind.FlashbackRecording or
           DiagnosticSessionScenarioKind.FlashbackRecordingPreviewCycle or
           DiagnosticSessionScenarioKind.FlashbackRecordingSettingsDeferred or
           DiagnosticSessionScenarioKind.FlashbackRecordingExportRejected or
           DiagnosticSessionScenarioKind.FlashbackExportRejected or
           DiagnosticSessionScenarioKind.Combined;

    internal bool ToleratesSourceSignalHealthWarning
        => Kind is DiagnosticSessionScenarioKind.FlashbackRangeExport or
           DiagnosticSessionScenarioKind.FlashbackRangeExportAudioSwitch or
           DiagnosticSessionScenarioKind.FlashbackExportConcurrent or
           DiagnosticSessionScenarioKind.FlashbackDisableDuringExport or
           DiagnosticSessionScenarioKind.FlashbackRotatedExport or
           DiagnosticSessionScenarioKind.FlashbackPreviewCycle or
           DiagnosticSessionScenarioKind.FlashbackPlaybackPreviewCycle;

    internal bool ToleratesFlashbackForceRotateDrainWarning
        => Kind is DiagnosticSessionScenarioKind.FlashbackExportPlayback or
           DiagnosticSessionScenarioKind.FlashbackScrubStress or
           DiagnosticSessionScenarioKind.FlashbackRangeExport or
           DiagnosticSessionScenarioKind.FlashbackRangeExportAudioSwitch or
           DiagnosticSessionScenarioKind.FlashbackExportConcurrent or
           DiagnosticSessionScenarioKind.FlashbackDisableDuringExport or
           DiagnosticSessionScenarioKind.FlashbackRotatedExport;

    internal bool ToleratesStrictArtifactDiagnosticHealthWarning
        => Kind is DiagnosticSessionScenarioKind.FlashbackRangeExport or
           DiagnosticSessionScenarioKind.FlashbackRangeExportAudioSwitch or
           DiagnosticSessionScenarioKind.FlashbackExportPlayback or
           DiagnosticSessionScenarioKind.FlashbackRestartCycle or
           DiagnosticSessionScenarioKind.FlashbackEncoderCycle or
           DiagnosticSessionScenarioKind.FlashbackExportConcurrent or
           DiagnosticSessionScenarioKind.FlashbackDisableDuringExport or
           DiagnosticSessionScenarioKind.FlashbackRotatedExport or
           DiagnosticSessionScenarioKind.FlashbackPreviewCycle or
           DiagnosticSessionScenarioKind.FlashbackPlaybackPreviewCycle or
           DiagnosticSessionScenarioKind.FlashbackRecording or
           DiagnosticSessionScenarioKind.FlashbackRecordingPreviewCycle or
           DiagnosticSessionScenarioKind.FlashbackRecordingSettingsDeferred or
           DiagnosticSessionScenarioKind.FlashbackRecordingExportRejected;

    internal bool ToleratesControlOnlyDiagnosticHealthWarning
        => Kind is DiagnosticSessionScenarioKind.FlashbackLifecycle or
           DiagnosticSessionScenarioKind.FlashbackExportRejected;

    internal bool IsPreviewCycleScenario
        => Kind is DiagnosticSessionScenarioKind.FlashbackPreviewCycle or
           DiagnosticSessionScenarioKind.FlashbackPlaybackPreviewCycle or
           DiagnosticSessionScenarioKind.FlashbackRecordingPreviewCycle;

    internal bool ToleratesSparsePreviewSchedulerStressTransitions
        => Kind is DiagnosticSessionScenarioKind.FlashbackScrubStress or
           DiagnosticSessionScenarioKind.FlashbackSegmentPlayback or
           DiagnosticSessionScenarioKind.FlashbackRestartCycle or
           DiagnosticSessionScenarioKind.FlashbackEncoderCycle;
}

internal static class DiagnosticSessionScenarioStartup
{
    internal static async Task StartAsync(
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
        DiagnosticSessionScenarioPhaseState phaseState,
        CancellationToken cancellationToken)
    {
        await StartPresentMonAsync(
                options,
                durationSeconds,
                outputDirectory,
                backgroundTasks,
                actions,
                sendAsync,
                cancellationToken)
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

        await TryStartFlashbackPlaybackAsync(
                scenarioPlan,
                outputDirectory,
                actions,
                warnings,
                sendAsync,
                phaseState,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task StartPresentMonAsync(
        DiagnosticSessionOptions options,
        int durationSeconds,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        CancellationToken cancellationToken)
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
            correlation: PresentMonProbe.ReadPreviewCorrelation(correlationSnapshot)), cancellationToken));
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
        if (!(scenarioPlan.Kind == DiagnosticSessionScenarioKind.FlashbackRecordingSettingsDeferred))
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

    private static async Task TryStartFlashbackPlaybackAsync(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        DiagnosticSessionScenarioPhaseState phaseState,
        CancellationToken cancellationToken)
    {
        if (!(scenarioPlan.Kind == DiagnosticSessionScenarioKind.FlashbackPlayback))
        {
            return;
        }

        if (phaseState.InitialFlashbackPlaybackActive)
        {
            actions.Add("existing flashback playback retained");
            return;
        }

        if (!await WaitForFlashbackStressBufferReadyAsync(sendAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback playback: Flashback buffer did not become export-ready within 30s");
            return;
        }

        var prerollExportPath = Path.Combine(outputDirectory, "flashback-playback-preroll.mp4");
        var prerollExportResponse = await sendAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = prerollExportPath },
                AutomationPipeProtocol.GetDefaultResponseTimeout("FlashbackExport"))
            .ConfigureAwait(false);
        if (!IsSuccess(prerollExportResponse))
        {
            warnings.Add($"flashback playback: preroll export failed - {Get(prerollExportResponse, "Message", "unknown error")}");
            return;
        }

        actions.Add("flashback playback preroll export completed");

        var playbackTarget = await DiagnosticSessionFlashbackSegments.WaitForFlashbackPlayableCompletedSegmentAsync(
                sendAsync,
                TimeSpan.FromSeconds(45),
                cancellationToken)
            .ConfigureAwait(false);
        if (playbackTarget is null)
        {
            warnings.Add("flashback playback: Flashback buffer did not produce a playable completed segment within 45s");
            return;
        }

        var target = playbackTarget.Value;
        var playPositionMs = Math.Max(0, target.BoundaryPositionMs - 500);
        phaseState.PlaybackStartUnconfirmed = true;
        var playResponse = await sendAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "play", ["positionMs"] = playPositionMs },
                null)
            .ConfigureAwait(false);
        phaseState.PlaybackStartUnconfirmed = HasUnconfirmedCommandOutcome(playResponse);
        if (!IsSuccess(playResponse))
        {
            warnings.Add($"flashback playback: play command failed - {Get(playResponse, "Message", "unknown error")}");
            return;
        }

        phaseState.StartedFlashbackPlayback = true;
        actions.Add(
            "flashback playback started at completed segment " +
            $"segment={target.Segment.SequenceNumber} positionMs={playPositionMs}");
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
    }
}

internal static class DiagnosticSessionScenarioSetup
{
    internal static async Task RunAsync(
        string scenario,
        DiagnosticSessionScenarioPlan scenarioPlan,
        JsonElement initialSnapshot,
        List<string> actions,
        List<string> warnings,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, Task> tryWaitAsync,
        DiagnosticSessionScenarioPhaseState phaseState,
        CancellationToken cancellationToken)
    {
        await SetupFlashbackStateAsync(
                scenario,
                scenarioPlan,
                initialSnapshot,
                actions,
                commandChannel,
                phaseState)
            .ConfigureAwait(false);
        await StartPreviewIfNeededAsync(
                scenario,
                initialSnapshot,
                actions,
                commandChannel,
                tryWaitAsync,
                phaseState)
            .ConfigureAwait(false);
        await StartRecordingIfNeededAsync(
                scenario,
                scenarioPlan,
                initialSnapshot,
                actions,
                warnings,
                commandChannel,
                tryWaitAsync,
                phaseState,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task SetupFlashbackStateAsync(
        string scenario,
        DiagnosticSessionScenarioPlan scenarioPlan,
        JsonElement initialSnapshot,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        DiagnosticSessionScenarioPhaseState phaseState)
    {
        if (DiagnosticSessionScenarioCatalog.NeedsFlashback(scenario) && !GetBool(initialSnapshot, "FlashbackActive"))
        {
            phaseState.FlashbackEnableUnconfirmed = true;
            var response = await commandChannel.SendAsync(
                    AutomationCommandKind.SetFlashbackEnabled,
                    new Dictionary<string, object?> { ["enabled"] = true },
                    null)
                .ConfigureAwait(false);
            phaseState.FlashbackEnableUnconfirmed = HasUnconfirmedCommandOutcome(response);
            if (IsSuccess(response))
            {
                phaseState.EnabledFlashback = true;
                actions.Add("flashback enabled");
            }
        }

        if ((scenarioPlan.Kind == DiagnosticSessionScenarioKind.FlashbackExportRejected) && GetBool(initialSnapshot, "FlashbackActive"))
        {
            phaseState.FlashbackDisableUnconfirmed = true;
            var response = await commandChannel.SendAsync(
                    AutomationCommandKind.SetFlashbackEnabled,
                    new Dictionary<string, object?> { ["enabled"] = false },
                    null)
                .ConfigureAwait(false);
            phaseState.FlashbackDisableUnconfirmed = HasUnconfirmedCommandOutcome(response);
            if (IsSuccess(response))
            {
                phaseState.DisabledFlashback = true;
                actions.Add("flashback disabled for rejected export");
            }
        }
    }

    private static async Task StartPreviewIfNeededAsync(
        string scenario,
        JsonElement initialSnapshot,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, Task> tryWaitAsync,
        DiagnosticSessionScenarioPhaseState phaseState)
    {
        if (!DiagnosticSessionScenarioCatalog.NeedsPreview(scenario) || GetBool(initialSnapshot, "IsPreviewing"))
        {
            return;
        }

        phaseState.PreviewStartUnconfirmed = true;
        var response = await commandChannel.SendAsync(
                AutomationCommandKind.SetPreviewEnabled,
                new Dictionary<string, object?> { ["enabled"] = true },
                null)
            .ConfigureAwait(false);
        phaseState.PreviewStartUnconfirmed = HasUnconfirmedCommandOutcome(response);
        if (!IsSuccess(response))
            return;

        phaseState.StartedPreview = true;
        actions.Add("preview started");
        await tryWaitAsync("VideoFramesFlowing", 15_000).ConfigureAwait(false);
    }

    private static async Task StartRecordingIfNeededAsync(
        string scenario,
        DiagnosticSessionScenarioPlan scenarioPlan,
        JsonElement initialSnapshot,
        List<string> actions,
        List<string> warnings,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, Task> tryWaitAsync,
        DiagnosticSessionScenarioPhaseState phaseState,
        CancellationToken cancellationToken)
    {
        if (!DiagnosticSessionScenarioCatalog.NeedsRecording(scenario) || GetBool(initialSnapshot, "IsRecording"))
        {
            return;
        }

        Task<JsonElement> SendByNameAsync(string command, Dictionary<string, object?>? payload, int? timeoutMs)
            => commandChannel.SendAsync(command, payload, timeoutMs);

        if (scenarioPlan.RequiresFlashbackRecordingReadiness &&
            !await WaitForFlashbackStressBufferReadyAsync(SendByNameAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback recording: Flashback buffer did not become recording-ready within 30s");
        }

        phaseState.RecordingStartUnconfirmed = true;
        var response = await commandChannel.SendAsync(
                AutomationCommandKind.SetRecordingEnabled,
                new Dictionary<string, object?> { ["enabled"] = true },
                null)
            .ConfigureAwait(false);
        phaseState.RecordingStartUnconfirmed = HasUnconfirmedCommandOutcome(response);
        if (!IsSuccess(response))
            return;

        phaseState.StartedRecording = true;
        actions.Add("recording started");
        await tryWaitAsync("RecordingFileGrowing", 20_000).ConfigureAwait(false);
    }
}
