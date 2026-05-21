namespace Sussudio.Tools;

internal sealed class SourceCadenceSessionMetrics
{
    public long MaxSevereGapCountObserved { get; set; }
    public long MaxEstimatedDroppedFramesObserved { get; set; }
    public double MaxDropPercentObserved { get; set; }
}

internal sealed class PreviewCadenceSessionMetrics
{
    public double OnePercentLowFpsAtEnd { get; init; }
    public double MinOnePercentLowFpsObserved { get; set; } = double.PositiveInfinity;
}

internal sealed class VisualCadenceSessionMetrics
{
    public double OutputFpsAtEnd { get; init; }
    public double ChangeFpsAtEnd { get; init; }
    public double MinChangeFpsObserved { get; set; } = double.PositiveInfinity;
    public double RepeatPercentAtEnd { get; init; }
    public double MaxRepeatPercentObserved { get; set; }
    public long RepeatFramesAtEnd { get; init; }
    public long LongestRepeatRunAtEnd { get; init; }
}
