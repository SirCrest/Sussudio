using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackValidation
{
    internal static void ValidateFlashbackRecordingSession(
        JsonElement initialSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples,
        List<string> warnings)
    {
        var metrics = BuildFlashbackRecordingMetrics(initialSnapshot, samples);
        if (metrics.SampleCount == 0)
        {
            warnings.Add("flashback recording: no recording samples captured");
            return;
        }

        if (!metrics.BackendObserved)
        {
            warnings.Add("flashback recording: RecordingBackend never reported Flashback");
        }

        if (!metrics.FileGrowthObserved)
        {
            warnings.Add("flashback recording: recording file never reported growth");
        }

        if (metrics.VideoFramesSubmittedDelta <= 0)
        {
            warnings.Add("flashback recording: no Flashback video frames submitted to encoder");
        }

        if (metrics.VideoEncoderPacketsWrittenDelta <= 0)
        {
            warnings.Add("flashback recording: no Flashback encoder packets written");
        }

        if (metrics.IntegritySequenceGapsDelta > 0)
        {
            warnings.Add($"flashback recording: Flashback video sequence gaps increased delta={metrics.IntegritySequenceGapsDelta} end={metrics.IntegritySequenceGapsAtEnd}");
        }

        if (metrics.IntegrityQueueDroppedFramesDelta > 0)
        {
            warnings.Add($"flashback recording: Flashback dropped frames increased delta={metrics.IntegrityQueueDroppedFramesDelta} end={metrics.IntegrityQueueDroppedFramesAtEnd}");
        }
    }
}
