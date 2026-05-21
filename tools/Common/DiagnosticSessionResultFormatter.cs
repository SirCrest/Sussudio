using System.Text;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    public static string Format(DiagnosticSessionResult result)
    {
        var builder = new StringBuilder();
        AppendOverview(builder, result);
        AppendCaptureMode(builder, result);
        AppendRecordingVerification(builder, result);
        AppendPresentMon(builder, result);
        AppendFlashbackSections(builder, result);
        AppendPreviewSections(builder, result);
        AppendProcessPerformance(builder, result);
        AppendArtifacts(builder, result);
        AppendActionsAndWarnings(builder, result);
        return builder.ToString().TrimEnd();
    }
}
