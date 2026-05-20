using System.Text;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static void AppendPreviewVisualCadence(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Preview Visual Cadence: " +
            $"outputFpsEnd={result.VisualCadenceOutputFpsAtEnd:0.##} " +
            $"changeFpsEnd={result.VisualCadenceChangeFpsAtEnd:0.##} " +
            $"changeFpsMin={result.VisualCadenceMinChangeFpsObserved:0.##} " +
            $"repeatPctEnd={result.VisualCadenceRepeatPercentAtEnd:0.###} " +
            $"repeatPctMax={result.VisualCadenceMaxRepeatPercentObserved:0.###} " +
            $"repeatFramesEnd={result.VisualCadenceRepeatFramesAtEnd} " +
            $"longestRepeatRunEnd={result.VisualCadenceLongestRepeatRunAtEnd}");
    }
}
