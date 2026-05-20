namespace Sussudio.Tools;

public sealed partial class DiagnosticSessionResult
{
    // Preview scheduler and jitter-buffer summary.
    public long PreviewSchedulerDroppedAtEnd { get; init; }
    public long PreviewSchedulerDeadlineDropsAtEnd { get; init; }
    public long PreviewSchedulerClearedDropsAtEnd { get; init; }
    public long PreviewSchedulerUnderflowsAtEnd { get; init; }
    public long PreviewSchedulerResumeReprimesAtEnd { get; init; }
    public long PreviewSchedulerDroppedDelta { get; init; }
    public long PreviewSchedulerDeadlineDropsDelta { get; init; }
    public long PreviewSchedulerClearedDropsDelta { get; init; }
    public long PreviewSchedulerUnderflowsDelta { get; init; }
    public long PreviewSchedulerResumeReprimesDelta { get; init; }
    public string PreviewSchedulerLastDropReasonAtEnd { get; init; } = string.Empty;
    public string PreviewSchedulerLastUnderflowReasonAtEnd { get; init; } = string.Empty;
    public double PreviewSchedulerLastUnderflowInputAgeMsAtEnd { get; init; }
    public double PreviewSchedulerLastUnderflowOutputAgeMsAtEnd { get; init; }
    public double PreviewSchedulerMaxScheduleLateMsObserved { get; init; }
    public long PreviewSchedulerScheduleLateDelta { get; init; }
}
