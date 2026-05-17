using static Sussudio.Tools.DiagnosticSessionResultArtifacts;
using static Sussudio.Tools.DiagnosticSessionSummaryWriter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    internal static async Task<DiagnosticSessionResult> BuildAndWriteAsync(
        DiagnosticSessionResultBuildRequest request,
        DiagnosticSessionRunState runState)
    {
        runState.SetStage("result-analysis");
        var analysis = Analyze(request);
        var samples = request.Samples;
        var warnings = request.Warnings;

        var artifactPaths = await WritePreSummaryAsync(
                request.OutputDirectory,
                request.SessionId,
                samples,
                request.Timeline,
                runState)
            .ConfigureAwait(false);

        var completedUtc = DateTimeOffset.UtcNow;
        var terminalState = runState.GetTerminalState();
        runState.SetStage("summary");

        var result = CreateResult(
            request,
            runState,
            analysis,
            artifactPaths,
            completedUtc,
            terminalState);

        return await WriteAsync(result, runState, warnings).ConfigureAwait(false);
    }

    private static DiagnosticSessionResult CreateResult(
        DiagnosticSessionResultBuildRequest request,
        DiagnosticSessionRunState runState,
        DiagnosticSessionResultAnalysis analysis,
        DiagnosticSessionResultArtifactPaths artifactPaths,
        DateTimeOffset completedUtc,
        string terminalState)
    {
        var resultProjections = BuildResultProjectionSet(request, runState, analysis);

        return FlattenResultProjectionSet(
            request,
            runState,
            analysis,
            resultProjections,
            artifactPaths,
            completedUtc,
            terminalState);
    }

    private static DiagnosticSessionResultProjectionSet BuildResultProjectionSet(
        DiagnosticSessionResultBuildRequest request,
        DiagnosticSessionRunState runState,
        DiagnosticSessionResultAnalysis analysis)
    {
        return new DiagnosticSessionResultProjectionSet(
            Overview: BuildOverviewResultProjection(request, runState, analysis),
            Capture: BuildCaptureResultProjection(analysis),
            FlashbackPlayback: BuildFlashbackPlaybackResultProjection(analysis),
            FlashbackRecording: BuildFlashbackRecordingResultProjection(analysis),
            FlashbackExport: BuildFlashbackExportResultProjection(analysis),
            Preview: BuildPreviewResultProjection(analysis),
            PreviewD3D: BuildPreviewD3DResultProjection(analysis),
            PreviewVisualCadence: BuildPreviewVisualCadenceResultProjection(analysis));
    }

    private readonly record struct DiagnosticSessionResultProjectionSet(
        DiagnosticSessionOverviewResultProjection Overview,
        DiagnosticSessionCaptureResultProjection Capture,
        DiagnosticSessionFlashbackPlaybackResultProjection FlashbackPlayback,
        DiagnosticSessionFlashbackRecordingResultProjection FlashbackRecording,
        DiagnosticSessionFlashbackExportResultProjection FlashbackExport,
        DiagnosticSessionPreviewResultProjection Preview,
        DiagnosticSessionPreviewD3DResultProjection PreviewD3D,
        DiagnosticSessionPreviewVisualCadenceResultProjection PreviewVisualCadence);

    private readonly record struct DiagnosticSessionFlashbackPlaybackResultProjection(
        DiagnosticSessionFlashbackPlaybackCommandsResultProjection CommandsResult,
        DiagnosticSessionFlashbackPlaybackCadenceResultProjection CadenceResult,
        DiagnosticSessionFlashbackPlaybackDecodeResultProjection DecodeResult,
        DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection AudioMasterResult,
        DiagnosticSessionFlashbackPlaybackStagesResultProjection StagesResult);

    private static DiagnosticSessionFlashbackPlaybackResultProjection BuildFlashbackPlaybackResultProjection(
        DiagnosticSessionResultAnalysis analysis)
    {
        var playbackSessionMetrics = analysis.PlaybackSessionMetrics;
        var playbackResultMetrics = analysis.PlaybackResultMetrics;
        var commandsResult = BuildFlashbackPlaybackCommandsResultProjection(playbackResultMetrics);
        var cadenceResult = BuildFlashbackPlaybackCadenceResultProjection(playbackSessionMetrics, playbackResultMetrics);
        var decodeResult = BuildFlashbackPlaybackDecodeResultProjection(playbackSessionMetrics, playbackResultMetrics);
        var audioMasterResult = BuildFlashbackPlaybackAudioMasterResultProjection(playbackSessionMetrics, playbackResultMetrics);
        var stagesResult = BuildFlashbackPlaybackStagesResultProjection(playbackSessionMetrics, playbackResultMetrics);

        return new DiagnosticSessionFlashbackPlaybackResultProjection(
            CommandsResult: commandsResult,
            CadenceResult: cadenceResult,
            DecodeResult: decodeResult,
            AudioMasterResult: audioMasterResult,
            StagesResult: stagesResult);
    }
}
