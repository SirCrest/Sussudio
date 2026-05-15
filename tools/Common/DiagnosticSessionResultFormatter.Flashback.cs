using System.Text;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static void AppendFlashbackSections(StringBuilder builder, DiagnosticSessionResult result)
    {
        AppendFlashbackPlaybackCommands(builder, result);
        AppendFlashbackPlaybackPerformance(builder, result);
        AppendFlashbackPlaybackDecode(builder, result);
        AppendFlashbackPlaybackStages(builder, result);
        AppendFlashbackRecording(builder, result);
        AppendFlashbackExport(builder, result);
    }
}
