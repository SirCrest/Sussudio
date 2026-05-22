using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
