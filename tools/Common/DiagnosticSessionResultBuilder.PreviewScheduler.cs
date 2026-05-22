using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;
using static Sussudio.Tools.DiagnosticSessionHealthTolerances;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private readonly record struct DiagnosticSessionPreviewSchedulerAnalysis(
        long DroppedAtEnd,
        long DeadlineDropsAtEnd,
        long ClearedDropsAtEnd,
        long UnderflowsAtEnd,
        long ResumeReprimesAtEnd,
        long DroppedDelta,
        long DeadlineDropsDelta,
        long ClearedDropsDelta,
        long UnderflowsDelta,
        long ResumeReprimesDelta,
        long ScheduleLateDelta,
        double MaxScheduleLateMsObserved,
        string LastDropReasonAtEnd,
        string LastUnderflowReasonAtEnd,
        double LastUnderflowInputAgeMsAtEnd,
        double LastUnderflowOutputAgeMsAtEnd);

    private static DiagnosticSessionPreviewSchedulerAnalysis BuildPreviewSchedulerAnalysis(
        JsonElement initialSnapshot,
        JsonElement lastSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples)
    {
        return new DiagnosticSessionPreviewSchedulerAnalysis(
            DroppedAtEnd: GetNullableLong(lastSnapshot, "MjpegPreviewJitterTotalDropped") ?? 0,
            DeadlineDropsAtEnd: GetNullableLong(lastSnapshot, "MjpegPreviewJitterDeadlineDropCount") ?? 0,
            ClearedDropsAtEnd: GetNullableLong(lastSnapshot, "MjpegPreviewJitterClearedDropCount") ?? 0,
            UnderflowsAtEnd: GetNullableLong(lastSnapshot, "MjpegPreviewJitterUnderflowCount") ?? 0,
            ResumeReprimesAtEnd: GetNullableLong(lastSnapshot, "MjpegPreviewJitterResumeReprimeCount") ?? 0,
            DroppedDelta: GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterTotalDropped"),
            DeadlineDropsDelta: GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterDeadlineDropCount"),
            ClearedDropsDelta: GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterClearedDropCount"),
            UnderflowsDelta: GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterUnderflowCount"),
            ResumeReprimesDelta: GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterResumeReprimeCount"),
            ScheduleLateDelta: GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterScheduleLateCount"),
            MaxScheduleLateMsObserved: samples
                .Select(sample => GetDouble(sample.Snapshot, "MjpegPreviewJitterMaxScheduleLateMs"))
                .Append(GetDouble(lastSnapshot, "MjpegPreviewJitterMaxScheduleLateMs"))
                .DefaultIfEmpty(0)
                .Max(),
            LastDropReasonAtEnd: GetString(lastSnapshot, "MjpegPreviewJitterLastDropReason") ?? string.Empty,
            LastUnderflowReasonAtEnd: GetString(lastSnapshot, "MjpegPreviewJitterLastUnderflowReason") ?? string.Empty,
            LastUnderflowInputAgeMsAtEnd: GetDouble(lastSnapshot, "MjpegPreviewJitterLastUnderflowInputAgeMs"),
            LastUnderflowOutputAgeMsAtEnd: GetDouble(lastSnapshot, "MjpegPreviewJitterLastUnderflowOutputAgeMs"));
    }

    private static void ValidateFlashbackPreviewSchedulerAnalysis(
        DiagnosticSessionScenarioPlan scenarioPlan,
        JsonElement lastSnapshot,
        int durationSeconds,
        DiagnosticSessionPreviewSchedulerAnalysis previewScheduler,
        PreviewCadenceSessionMetrics previewCadenceMetrics,
        VisualCadenceSessionMetrics visualCadenceMetrics,
        PreviewD3DMetrics previewD3DMetrics,
        List<string> warnings)
    {
        if (!scenarioPlan.UsesFlashbackScenarioWarningPolicy)
        {
            return;
        }

        var previewTargetFps = GetDouble(lastSnapshot, "ExpectedCaptureFrameRate");
        if (previewTargetFps <= 0)
        {
            previewTargetFps = GetDouble(lastSnapshot, "SelectedExactFrameRate");
        }

        var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, previewTargetFps);
        var toleratesPreviewCycleSchedulerSettling =
            scenarioPlan.IsPreviewCycleScenario && visualCadenceHealthy;
        var toleratesSparsePreviewSchedulerDeadlineDrops =
            IsSparsePreviewSchedulerDeadlineDropRun(
                previewScheduler.DeadlineDropsDelta,
                previewScheduler.UnderflowsDelta,
                durationSeconds,
                visualCadenceHealthy);
        var toleratesSparseScrubSchedulerTransitions =
            scenarioPlan.ToleratesSparsePreviewSchedulerStressTransitions &&
            IsSparsePreviewSchedulerStressRun(
                previewScheduler.DeadlineDropsDelta,
                previewScheduler.UnderflowsDelta,
                durationSeconds,
                visualCadenceHealthy);
        ValidateFlashbackPreviewScheduler(
            previewScheduler.DeadlineDropsDelta,
            previewScheduler.UnderflowsDelta,
            previewD3DMetrics.StatsFailureDelta,
            previewCadenceMetrics,
            visualCadenceMetrics,
            previewD3DMetrics,
            previewTargetFps,
            toleratesPreviewCycleSchedulerSettling ||
                toleratesSparsePreviewSchedulerDeadlineDrops ||
                toleratesSparseScrubSchedulerTransitions,
            warnings);
    }
}
