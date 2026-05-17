using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static string BuildRecordingLane(CaptureRuntimeSnapshot captureRuntime)
    {
        return $"recording integrity={captureRuntime.RecordingIntegrityStatus} complete={captureRuntime.RecordingIntegrityComplete} seqGaps={captureRuntime.RecordingIntegritySequenceGaps} queueDrops={captureRuntime.RecordingIntegrityQueueDroppedFrames}";
    }

    private static string BuildAudioLane(CaptureRuntimeSnapshot captureRuntime)
    {
        return $"audio integrity={captureRuntime.RecordingIntegrityAudioStatus} drops={captureRuntime.RecordingIntegrityAudioDropEvents} disc={captureRuntime.RecordingIntegrityAudioDiscontinuities} gaps={captureRuntime.RecordingIntegrityAudioCallbackGaps}";
    }
}
