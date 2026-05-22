using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal sealed class FlashbackRecordingSessionMetrics
{
    public int SampleCount { get; init; }
    public bool BackendObserved { get; init; }
    public bool FileGrowthObserved { get; init; }
    public long VideoFramesSubmittedDelta { get; init; }
    public long VideoEncoderPacketsWrittenDelta { get; init; }
    public long IntegritySequenceGapsAtEnd { get; init; }
    public long IntegrityQueueDroppedFramesAtEnd { get; init; }
    public long IntegritySequenceGapsDelta { get; init; }
    public long IntegrityQueueDroppedFramesDelta { get; init; }
}

internal static partial class DiagnosticSessionFlashbackMetrics
{
    internal static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(
        JsonElement initialSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples)
    {
        var recordingSamples = samples
            .Select(sample => sample.Snapshot)
            .Where(snapshot => GetBool(snapshot, "IsRecording"))
            .ToArray();
        if (recordingSamples.Length == 0)
        {
            return new FlashbackRecordingSessionMetrics();
        }

        var firstRecordingSample = recordingSamples[0];
        var finalRecordingSample = recordingSamples[^1];
        return new FlashbackRecordingSessionMetrics
        {
            SampleCount = recordingSamples.Length,
            BackendObserved = recordingSamples.Any(snapshot =>
                string.Equals(GetString(snapshot, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase)),
            FileGrowthObserved = recordingSamples.Any(snapshot => GetBool(snapshot, "RecordingFileGrowing")),
            VideoFramesSubmittedDelta =
                (GetNullableLong(finalRecordingSample, "FlashbackVideoFramesSubmittedToEncoder") ?? 0) -
                (GetNullableLong(firstRecordingSample, "FlashbackVideoFramesSubmittedToEncoder") ?? 0),
            VideoEncoderPacketsWrittenDelta =
                (GetNullableLong(finalRecordingSample, "FlashbackVideoEncoderPacketsWritten") ?? 0) -
                (GetNullableLong(firstRecordingSample, "FlashbackVideoEncoderPacketsWritten") ?? 0),
            IntegritySequenceGapsAtEnd = GetNullableLong(finalRecordingSample, "RecordingIntegritySequenceGaps") ?? 0,
            IntegrityQueueDroppedFramesAtEnd = GetNullableLong(finalRecordingSample, "RecordingIntegrityQueueDroppedFrames") ?? 0,
            IntegritySequenceGapsDelta = GetResetAwareCounterDelta(
                finalRecordingSample,
                firstRecordingSample,
                "RecordingIntegritySequenceGaps"),
            IntegrityQueueDroppedFramesDelta = GetResetAwareCounterDelta(
                finalRecordingSample,
                firstRecordingSample,
                "RecordingIntegrityQueueDroppedFrames")
        };
    }
}
