namespace Sussudio.Tools;

public sealed partial class DiagnosticSessionResult
{
    // Preview cadence summary.
    public double PreviewCadenceOnePercentLowFpsAtEnd { get; init; }
    public double PreviewCadenceMinOnePercentLowFpsObserved { get; init; }

    // Preview visual-cadence summary.
    public double VisualCadenceOutputFpsAtEnd { get; init; }
    public double VisualCadenceChangeFpsAtEnd { get; init; }
    public double VisualCadenceMinChangeFpsObserved { get; init; }
    public double VisualCadenceRepeatPercentAtEnd { get; init; }
    public double VisualCadenceMaxRepeatPercentObserved { get; init; }
    public long VisualCadenceRepeatFramesAtEnd { get; init; }
    public long VisualCadenceLongestRepeatRunAtEnd { get; init; }
}
