using System.Text;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static void AppendFlashbackPlaybackPerformance(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Flashback Playback Perf: " +
            BuildFlashbackPlaybackCadencePerformanceText(result) + " " +
            BuildFlashbackPlaybackAudioMasterPerformanceText(result) + " " +
            BuildFlashbackPlaybackSubmitPerformanceText(result));
    }

    private static string BuildFlashbackPlaybackSubmitPerformanceText(DiagnosticSessionResult result)
        => $"submitFailuresEnd={result.FlashbackPlaybackSubmitFailuresAtEnd} " +
           $"submitFailuresDelta={result.FlashbackPlaybackSubmitFailuresDelta}";
}
