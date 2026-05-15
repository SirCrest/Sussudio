using System.Text;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static void AppendPreviewSections(StringBuilder builder, DiagnosticSessionResult result)
    {
        AppendPreviewScheduler(builder, result);
        AppendPreviewD3DPerformance(builder, result);
        AppendPreviewD3DCpuTiming(builder, result);
        AppendPreviewVisualCadence(builder, result);
    }
}
