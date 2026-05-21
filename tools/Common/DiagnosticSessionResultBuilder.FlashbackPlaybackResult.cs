namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
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
