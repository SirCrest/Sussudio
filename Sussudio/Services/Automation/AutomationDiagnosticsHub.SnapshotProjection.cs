using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private AutomationSnapshot BuildAutomationSnapshot(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime,
        CaptureHealthSnapshot health,
        RecordingStats recordingStats,
        PreviewRuntimeSnapshot previewRuntime,
        PerformanceEvaluation performance,
        DiagnosticEvaluation diagnostic,
        PreviewPacingClassification previewPacingClassification,
        PreviewHdrState previewHdrState,
        AudioSignalState audioSignal,
        bool recordingFileGrowing,
        HdrTruthVerdict hdrTruthVerdict,
        LastOutputProbe lastOutput,
        ProcessResourceSnapshot processResources,
        RecordingVerificationResult? lastVerification,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures)
    {
        var projections = BuildAutomationSnapshotProjectionSet(
            viewModelSnapshot,
            captureRuntime,
            health,
            recordingStats,
            previewRuntime,
            performance,
            diagnostic,
            previewPacingClassification,
            previewHdrState,
            audioSignal,
            recordingFileGrowing,
            hdrTruthVerdict,
            lastOutput,
            processResources,
            lastVerification,
            recentD3DMissedRefreshes,
            recentD3DStatsFailures);

        return BuildAutomationSnapshotFromProjections(projections);
    }

    private static AvSyncProjection BuildAvSyncProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            CaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,
            CaptureDriftRateMsPerSec = captureRuntime.AvSyncCaptureDriftRateMsPerSec,
            EncoderDriftMs = captureRuntime.AvSyncEncoderDriftMs,
            EncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples
        };

    private static AvSyncFlattenedProjection BuildAvSyncFlattenedProjection(AvSyncProjection avSync)
        => new()
        {
            CaptureDriftMs = avSync.CaptureDriftMs,
            CaptureDriftRateMsPerSec = avSync.CaptureDriftRateMsPerSec,
            EncoderDriftMs = avSync.EncoderDriftMs,
            EncoderCorrectionSamples = avSync.EncoderCorrectionSamples
        };

    private readonly record struct AvSyncProjection
    {
        public double? CaptureDriftMs { get; init; }
        public double? CaptureDriftRateMsPerSec { get; init; }
        public double? EncoderDriftMs { get; init; }
        public long? EncoderCorrectionSamples { get; init; }
    }

    private readonly record struct AvSyncFlattenedProjection
    {
        public double? CaptureDriftMs { get; init; }
        public double? CaptureDriftRateMsPerSec { get; init; }
        public double? EncoderDriftMs { get; init; }
        public long? EncoderCorrectionSamples { get; init; }
    }

    private static CaptureCommandProjection BuildCaptureCommandProjection(ViewModelRuntimeSnapshot viewModelSnapshot)
        => new()
        {
            CommandsEnqueued = viewModelSnapshot.CaptureCommandCommandsEnqueued,
            CommandsCompleted = viewModelSnapshot.CaptureCommandCommandsCompleted,
            CommandsFailed = viewModelSnapshot.CaptureCommandCommandsFailed,
            CommandsCanceled = viewModelSnapshot.CaptureCommandCommandsCanceled,
            CommandsCoalesced = viewModelSnapshot.CaptureCommandCommandsCoalesced,
            PendingCommands = viewModelSnapshot.CaptureCommandPendingCommands,
            MaxPendingCommands = viewModelSnapshot.CaptureCommandMaxPendingCommands,
            OldestPendingCommandAgeMs = viewModelSnapshot.CaptureCommandOldestPendingCommandAgeMs,
            LastQueueLatencyMs = viewModelSnapshot.CaptureCommandLastQueueLatencyMs,
            MaxQueueLatencyMs = viewModelSnapshot.CaptureCommandMaxQueueLatencyMs,
            LastCommand = viewModelSnapshot.CaptureCommandLastCommand,
            LastOutcome = viewModelSnapshot.CaptureCommandLastOutcome,
            LastCorrelationId = viewModelSnapshot.CaptureCommandLastCorrelationId,
            LastError = viewModelSnapshot.CaptureCommandLastError
        };

    private readonly record struct CaptureCommandProjection
    {
        public long CommandsEnqueued { get; init; }
        public long CommandsCompleted { get; init; }
        public long CommandsFailed { get; init; }
        public long CommandsCanceled { get; init; }
        public long CommandsCoalesced { get; init; }
        public int PendingCommands { get; init; }
        public int MaxPendingCommands { get; init; }
        public long OldestPendingCommandAgeMs { get; init; }
        public long LastQueueLatencyMs { get; init; }
        public long MaxQueueLatencyMs { get; init; }
        public string LastCommand { get; init; }
        public string LastOutcome { get; init; }
        public string LastCorrelationId { get; init; }
        public string LastError { get; init; }
    }

    private static CaptureCommandFlattenedProjection BuildCaptureCommandFlattenedProjection(
        CaptureCommandProjection captureCommands)
        => new()
        {
            CommandsEnqueued = captureCommands.CommandsEnqueued,
            CommandsCompleted = captureCommands.CommandsCompleted,
            CommandsFailed = captureCommands.CommandsFailed,
            CommandsCanceled = captureCommands.CommandsCanceled,
            CommandsCoalesced = captureCommands.CommandsCoalesced,
            PendingCommands = captureCommands.PendingCommands,
            MaxPendingCommands = captureCommands.MaxPendingCommands,
            OldestPendingCommandAgeMs = captureCommands.OldestPendingCommandAgeMs,
            LastQueueLatencyMs = captureCommands.LastQueueLatencyMs,
            MaxQueueLatencyMs = captureCommands.MaxQueueLatencyMs,
            LastCommand = captureCommands.LastCommand,
            LastOutcome = captureCommands.LastOutcome,
            LastCorrelationId = captureCommands.LastCorrelationId,
            LastError = captureCommands.LastError
        };

    private readonly record struct CaptureCommandFlattenedProjection
    {
        public long CommandsEnqueued { get; init; }
        public long CommandsCompleted { get; init; }
        public long CommandsFailed { get; init; }
        public long CommandsCanceled { get; init; }
        public long CommandsCoalesced { get; init; }
        public int PendingCommands { get; init; }
        public int MaxPendingCommands { get; init; }
        public long OldestPendingCommandAgeMs { get; init; }
        public long LastQueueLatencyMs { get; init; }
        public long MaxQueueLatencyMs { get; init; }
        public string LastCommand { get; init; }
        public string LastOutcome { get; init; }
        public string LastCorrelationId { get; init; }
        public string LastError { get; init; }
    }
}
