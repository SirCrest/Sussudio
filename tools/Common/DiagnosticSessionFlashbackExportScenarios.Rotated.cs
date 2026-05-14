using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackExports;
using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;
using static Sussudio.Tools.DiagnosticSessionMetrics;
using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExportScenarios
{
    internal static async Task RunFlashbackRotatedExportAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback rotated export: Flashback buffer did not become export-ready within 30s");
            return;
        }

        var completedSegment = await WaitForFlashbackCompletedSegmentAsync(
                sendCommandAsync,
                TimeSpan.FromSeconds(210),
                cancellationToken)
            .ConfigureAwait(false);
        if (completedSegment is null)
        {
            warnings.Add("flashback rotated export: no completed segment observed within 210s");
            return;
        }

        actions.Add(
            "flashback rotated segment observed " +
            $"seq={completedSegment.Value.SequenceNumber} " +
            $"startMs={completedSegment.Value.StartPtsMs} " +
            $"endMs={completedSegment.Value.EndPtsMs}");

        var exportPath = Path.Combine(outputDirectory, "flashback-rotated-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 180, ["outputPath"] = exportPath },
                300_000)
            .ConfigureAwait(false);
        actions.Add("flashback rotated export requested");

        var exportMessage = AutomationSnapshotFormatter.Get(exportResponse, "Message", string.Empty);
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add(
                $"flashback rotated export: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
            return;
        }

        var exportedSegments = TryParseFlashbackExportSegmentCount(exportMessage);
        if (exportedSegments is null or < 2)
        {
            warnings.Add($"flashback rotated export: expected multi-segment export, got '{exportMessage}'");
        }

        var verifyResponse = await sendCommandAsync(
                "VerifyFile",
                CreateFlashbackExportVerifyPayload(exportPath),
                120_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
        {
            warnings.Add(
                $"flashback rotated export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            return;
        }

        actions.Add("flashback rotated export verified");
    }
}
