using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendRecordingSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Recording ==");
        builder.AppendLine($"Recording: {Get(snapshot, "IsRecording")} | Output: {Get(snapshot, "OutputPath")}");
        builder.AppendLine($"Time: {Get(snapshot, "RecordingTime")} | Size: {Get(snapshot, "RecordingSizeInfo")} | Bitrate: {Get(snapshot, "RecordingBitrateInfo")}");
        builder.AppendLine($"Backend: {Get(snapshot, "RecordingBackend")} | Audio Path: {Get(snapshot, "AudioPathMode")} | Mux: {Get(snapshot, "MuxResult")}");
        builder.AppendLine($"Integrity: {Get(snapshot, "RecordingIntegrityStatus")} complete={Get(snapshot, "RecordingIntegrityComplete")} backend={Get(snapshot, "RecordingIntegrityBackend")} source={Get(snapshot, "RecordingIntegritySourceFrames")} accepted={Get(snapshot, "RecordingIntegrityAcceptedFrames")} boundaryDrops={Get(snapshot, "RecordingIntegrityPipelineDroppedFrames")} queueDrops={Get(snapshot, "RecordingIntegrityQueueDroppedFrames")} encoderDrops={Get(snapshot, "RecordingIntegrityEncoderDroppedFrames")} seqGaps={Get(snapshot, "RecordingIntegritySequenceGaps")} submitted={Get(snapshot, "RecordingIntegritySubmittedFrames")} encoded={Get(snapshot, "RecordingIntegrityEncodedFrames")} packets={Get(snapshot, "RecordingIntegrityPacketsWritten")} qMax={Get(snapshot, "RecordingIntegrityQueueMaxDepth")} qOldestMs={Get(snapshot, "RecordingIntegrityQueueOldestFrameAgeMs")} backpressure={Get(snapshot, "RecordingIntegrityBackpressureWaitMs")}ms/{Get(snapshot, "RecordingIntegrityBackpressureEvents")} max={Get(snapshot, "RecordingIntegrityBackpressureMaxWaitMs")}ms reason={Get(snapshot, "RecordingIntegrityReason", "")}");
        builder.AppendLine($"Audio Integrity: {Get(snapshot, "RecordingIntegrityAudioStatus")} enabled={Get(snapshot, "RecordingIntegrityAudioEnabled")} active={Get(snapshot, "RecordingIntegrityAudioCaptureActive")} arrived={Get(snapshot, "RecordingIntegrityAudioFramesArrived")} written={Get(snapshot, "RecordingIntegrityAudioFramesWrittenToSink")} encoded={Get(snapshot, "RecordingIntegrityAudioSamplesEncoded")} drops={Get(snapshot, "RecordingIntegrityAudioDropEvents")} disc={Get(snapshot, "RecordingIntegrityAudioDiscontinuities")} tsErr={Get(snapshot, "RecordingIntegrityAudioTimestampErrors")} gaps={Get(snapshot, "RecordingIntegrityAudioCallbackGaps")} drift={Get(snapshot, "RecordingIntegrityAvSyncDriftMs", "N/A")}ms encoderDrift={Get(snapshot, "RecordingIntegrityEncoderAvSyncDriftMs", "N/A")}ms corr={Get(snapshot, "RecordingIntegrityEncoderAvSyncCorrectionSamples", "N/A")}");
        builder.AppendLine($"Last Output: {Get(snapshot, "LastOutputPath")} ({Get(snapshot, "LastOutputSizeBytes")} bytes) Finalize: {Get(snapshot, "LastFinalizeStatus")}");
        builder.AppendLine();
    }
}
