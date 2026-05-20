using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackPlaybackController_ScrubCoalescing_DoesNotRequeueControlCommands()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var coalescingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandCoalescing.cs")
            .Replace("\r\n", "\n");
        var controlYieldPolicyText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandControlYieldPolicy.cs")
            .Replace("\r\n", "\n");
        var commandFailuresText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandFailures.cs")
            .Replace("\r\n", "\n");

        var seekBlock = ExtractTextBetween(
            sourceText,
            "private void HandleSeekCommand(",
            "    private void HandleBeginScrubCommand(");

        AssertContains(seekBlock, "commandChannel.Reader.TryPeek(out var newerSeek) &&\n               newerSeek.Kind == CommandKind.Seek");
        AssertContains(seekBlock, "TrackCommandDequeued(newerSeek);");
        AssertContains(seekBlock, "cmd = ResolveSeekCommandPosition(cmd);");
        AssertContains(seekBlock, "newerSeek = ResolveSeekCommandPosition(newerSeek);");
        AssertContains(seekBlock, "FLASHBACK_PLAYBACK_SEEK");

        var beginScrubMethod = ExtractTextBetween(
            sourceText,
            "public bool BeginScrub(TimeSpan position)",
            "    public bool Seek(TimeSpan position)");
        var seekMethod = ExtractTextBetween(
            sourceText,
            "public bool Seek(TimeSpan position)",
            "    private bool SendUpdateScrubCommand");
        var updateScrubBlock = ExtractTextBetween(
            sourceText,
            "private void HandleUpdateScrubCommand(",
            "    private void HandleEndScrubCommand(");
        var updateScrubMethod = ExtractTextBetween(
            sourceText,
            "public bool UpdateScrub(TimeSpan position)",
            "    public bool EndScrub()");
        var drainAbandonedCommands = ExtractTextBetween(
            sourceText,
            "private void DrainAbandonedCommandsOnThreadExit(Channel<PlaybackCommand> commandChannel)",
            "    private static void CompleteCommandChannelForThreadExit");

        AssertContains(coalescingText, "private long _latestScrubUpdateTicks;");
        AssertContains(coalescingText, "private sealed class SeekIntentSlot");
        AssertContains(coalescingText, "private sealed class ScrubUpdateIntentSlot");
        AssertContains(sourceText, "public SeekIntentSlot? SeekSlot { get; init; }");
        AssertContains(sourceText, "public ScrubUpdateIntentSlot? ScrubUpdateSlot { get; init; }");
        AssertContains(coalescingText, "private readonly object _seekSlotSync = new();");
        AssertContains(coalescingText, "private SeekIntentSlot? _queuedSeekSlot;");
        AssertContains(coalescingText, "private ScrubUpdateIntentSlot? _queuedScrubUpdateSlot;");
        AssertContains(coalescingText, "private long _scrubUpdatesCoalesced;");
        AssertContains(coalescingText, "private long _seekCommandsCoalesced;");
        AssertDoesNotContain(rootText, "private sealed class SeekIntentSlot");
        AssertDoesNotContain(rootText, "private sealed class ScrubUpdateIntentSlot");
        AssertDoesNotContain(rootText, "private long _latestScrubUpdateTicks;");
        AssertDoesNotContain(rootText, "private readonly object _seekSlotSync = new();");
        AssertDoesNotContain(rootText, "private SeekIntentSlot? _queuedSeekSlot;");
        AssertDoesNotContain(rootText, "private ScrubUpdateIntentSlot? _queuedScrubUpdateSlot;");
        AssertDoesNotContain(rootText, "private long _scrubUpdatesCoalesced;");
        AssertDoesNotContain(rootText, "private long _seekCommandsCoalesced;");
        AssertContains(sourceText, "public long SeekCommandsCoalesced => Interlocked.Read(ref _seekCommandsCoalesced);");
        AssertContains(sourceText, "public bool HasPositionOverride { get; init; }");
        AssertContains(sourceText, "public bool EndScrub() => EndScrubAt(null);");
        AssertContains(sourceText, "public bool EndScrubAt(TimeSpan position) => EndScrubAt((TimeSpan?)position);");
        AssertContains(sourceText, "private bool EndScrubAt(TimeSpan? position)");
        AssertContains(sourceText, "return SendEndScrubCommand(position);");
        AssertContains(sourceText, "private bool SendEndScrubCommand(TimeSpan? position)");
        AssertContains(sourceText, "var commandTicks = position?.Ticks ??");
        AssertContains(sourceText, "_queuedScrubUpdateSlot?.LatestTicks ??");
        AssertContains(sourceText, "var commandPosition = TimeSpan.FromTicks(commandTicks);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Value.Ticks);");
        AssertContains(sourceText, "HasPositionOverride = position.HasValue");
        AssertContains(sourceText, "HasPositionOverride = command.HasPositionOverride");
        AssertContains(sourceText, "SeekSlot = command.SeekSlot");
        AssertContains(sourceText, "ScrubUpdateSlot = command.ScrubUpdateSlot");
        AssertContains(beginScrubMethod, "Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);");
        AssertContains(seekMethod, "lock (_seekSlotSync)");
        AssertContains(seekMethod, "_queuedScrubUpdateSlot = null;");
        AssertContains(seekMethod, "if (_queuedSeekSlot is { } queuedSlot)");
        AssertContains(seekMethod, "queuedSlot.LatestTicks = position.Ticks;");
        AssertContains(seekMethod, "TrackCoalescedSeekCommand();");
        AssertContains(seekMethod, "ClearLastCommandFailure();");
        AssertContains(seekMethod, "return true;");
        AssertContains(seekMethod, "var slot = new SeekIntentSlot(position.Ticks);");
        AssertContains(seekMethod, "_queuedSeekSlot = slot;");
        AssertContains(seekMethod, "SeekSlot = slot");
        AssertContains(seekMethod, "ClearQueuedSeekSlotUnsafe(slot);");
        AssertContains(seekMethod, "return false;");
        AssertContains(sourceText, "private bool SendCommand(PlaybackCommand command)\n    {\n        lock (_seekSlotSync)\n        {\n            if (!SendCommandCore(command))\n            {\n                return false;\n            }\n\n            if (command.Kind != CommandKind.Seek)\n            {\n                _queuedSeekSlot = null;\n            }\n\n            if (command.Kind != CommandKind.UpdateScrub)\n            {\n                _queuedScrubUpdateSlot = null;\n            }\n\n            return true;\n        }\n    }");
        AssertContains(updateScrubMethod, "return SendUpdateScrubCommand(position);");
        AssertContains(sourceText, "private bool SendUpdateScrubCommand(TimeSpan position)");
        AssertContains(sourceText, "Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);");
        AssertContains(sourceText, "if (_queuedScrubUpdateSlot is { } queuedSlot)");
        AssertContains(sourceText, "queuedSlot.LatestTicks = position.Ticks;");
        AssertContains(sourceText, "ClearLastCommandFailure();");
        AssertContains(sourceText, "var slot = new ScrubUpdateIntentSlot(position.Ticks);");
        AssertContains(sourceText, "_queuedScrubUpdateSlot = slot;");
        AssertContains(sourceText, "ScrubUpdateSlot = slot");
        AssertContains(sourceText, "ClearQueuedScrubUpdateSlotUnsafe(slot);");
        AssertContains(updateScrubMethod, "if (!PlaybackThreadAlive) return RejectCommand(CommandKind.UpdateScrub, \"thread_not_running\", \"thread_not_running\", false, position);");
        AssertContains(sourceText, "TrackCoalescedScrubUpdate();");
        AssertContains(updateScrubBlock, "cmd = ResolveScrubUpdateCommandPosition(cmd);");
        AssertContains(updateScrubBlock, "commandChannel.Reader.TryPeek(out var newer) &&\n               newer.Kind == CommandKind.UpdateScrub");
        AssertContains(updateScrubBlock, "if (!commandChannel.Reader.TryRead(out newer))");
        AssertContains(updateScrubBlock, "TrackCommandDequeued(newer);");
        AssertContains(updateScrubBlock, "newer = ResolveScrubUpdateCommandPosition(newer);");
        AssertContains(updateScrubBlock, "cmd = newer;");
        AssertContains(updateScrubBlock, "if (ShouldYieldScrubUpdateToQueuedControl(commandChannel))");
        AssertContains(updateScrubBlock, "PlaybackPosition = cmd.Position;");
        AssertContains(updateScrubBlock, "MarkCommandNoOp(CommandKind.UpdateScrub, \"superseded_by_control\", cmd.Position);");
        AssertContains(updateScrubBlock, "FLASHBACK_PLAYBACK_SCRUB_UPDATE_NO_FILE");
        AssertContains(updateScrubBlock, "SafeResumePreviewSubmission(\"scrub_update_no_file\")");
        AssertContains(updateScrubBlock, "SetState(FlashbackPlaybackState.Live)");
        AssertContains(controlYieldPolicyText, "private static bool ShouldYieldScrubUpdateToQueuedControl(Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "return next.Kind is CommandKind.EndScrub or CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;");
        AssertContains(controlYieldPolicyText, "private static bool ShouldYieldSeekToQueuedPlay(Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "return next.Kind is CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;");
        AssertContains(controlYieldPolicyText, "private static bool ShouldYieldPauseFromLiveToQueuedSeekOrPlay(Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "return next.Kind is CommandKind.Seek or CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;");
        AssertDoesNotContain(coalescingText, "private static bool ShouldYieldScrubUpdateToQueuedControl(Channel<PlaybackCommand> commandChannel)");
        AssertDoesNotContain(coalescingText, "private static bool ShouldYieldSeekToQueuedPlay(Channel<PlaybackCommand> commandChannel)");
        AssertDoesNotContain(coalescingText, "private static bool ShouldYieldPauseFromLiveToQueuedSeekOrPlay(Channel<PlaybackCommand> commandChannel)");
        AssertContains(drainAbandonedCommands, "ClearQueuedCommandSlotsBarrier();");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)\n        {\n            MarkCommandNoOp(CommandKind.EndScrub, \"live_thread_not_running\", position);\n            return false;\n        }");
        var endScrubBlock = ExtractTextBetween(
            sourceText,
            "private void HandleEndScrubCommand(",
            "    private void HandlePlayCommand(");
        AssertContains(endScrubBlock, "var endScrubPosition = ClampPosition(cmd.Position, frozenValidStart);");
        AssertContains(endScrubBlock, "PlaybackPosition = endScrubPosition;");
        AssertDoesNotContain(endScrubBlock, "TimeSpan.FromTicks(Interlocked.Read(ref _latestScrubUpdateTicks))");
        AssertContains(endScrubBlock, "var endScrubTarget = SaturatingAdd(endScrubPosition, frozenValidStart);");
        AssertDoesNotContain(endScrubBlock, "var endScrubTarget = SaturatingAdd(PlaybackPosition, frozenValidStart);");
        AssertContains(sourceText, "private bool RejectCommand(\n        CommandKind kind,\n        string failure,\n        string reason,\n        bool returnValue,\n        TimeSpan? position = null)");
        AssertContains(commandFailuresText, "private bool RejectCommand(\n        CommandKind kind,\n        string failure,\n        string reason,\n        bool returnValue,\n        TimeSpan? position = null)");
        AssertContains(sourceText, "SetLastCommandFailure($\"{failure}:{kind}{detail}\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_CMD_SKIP kind={kind} reason={reason}{detail}\");");
        AssertContains(sourceText, "private void SetNoFileFailure(CommandKind kind, TimeSpan position)");
        AssertContains(commandFailuresText, "private void SetNoFileFailure(CommandKind kind, TimeSpan position)");
        AssertContains(sourceText, "SetLastCommandFailure($\"no_file:{kind}{FormatCommandDetail(position: position)}\");");
        AssertContains(sourceText, "private static string FormatCommandDetail(PlaybackCommand command)");
        AssertContains(commandFailuresText, "private static string FormatCommandDetail(PlaybackCommand command)");
        AssertContains(sourceText, "return $\" pos_ms={position.Value.TotalMilliseconds.ToString(\"0.###\", CultureInfo.InvariantCulture)}\";");
        AssertContains(sourceText, "return $\" delta_ms={delta.Value.TotalMilliseconds.ToString(\"0.###\", CultureInfo.InvariantCulture)}\";");
        AssertContains(sourceText, "private void SetLastCommandFailure(string failure)\n    {\n        Volatile.Write(ref _lastCommandFailure, failure);\n        Interlocked.Exchange(ref _lastCommandFailureUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());\n    }");
        AssertContains(commandFailuresText, "private void SetLastCommandFailure(string failure)");
        AssertContains(sourceText, "private void MarkCommandQueued(CommandKind kind)");
        AssertContains(sourceText, "private void MarkCommandNoOp(CommandKind kind, string reason, TimeSpan? position = null, TimeSpan? delta = null)");
        AssertContains(commandFailuresText, "private void MarkCommandNoOp(CommandKind kind, string reason, TimeSpan? position = null, TimeSpan? delta = null)");
        AssertContains(sourceText, "private void ClearLastCommandFailure()\n    {\n        Volatile.Write(ref _lastCommandFailure, string.Empty);\n        Interlocked.Exchange(ref _lastCommandFailureUtcUnixMs, 0);\n    }");
        AssertContains(commandFailuresText, "private void ClearLastCommandFailure()");
        AssertContains(sourceText, "private void TrackCoalescedScrubUpdate()");
        AssertContains(sourceText, "Interlocked.Increment(ref _scrubUpdatesCoalesced);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SCRUB_COALESCED");
        var coalescedSeekMethod = ExtractTextBetween(
            sourceText,
            "private void TrackCoalescedSeekCommand()",
            "    private void TrackCommandDequeued");
        AssertContains(coalescedSeekMethod, "Interlocked.Increment(ref _seekCommandsCoalesced);");
        AssertContains(coalescedSeekMethod, "FLASHBACK_PLAYBACK_SEEK_COALESCED");
        AssertDoesNotContain(coalescedSeekMethod, "_commandsDropped");
        AssertContains(sourceText, "private PlaybackCommand ResolveSeekCommandPosition(PlaybackCommand command)");
        AssertContains(sourceText, "if (ReferenceEquals(_queuedSeekSlot, slot))\n            {\n                _queuedSeekSlot = null;\n            }");
        AssertContains(sourceText, "private PlaybackCommand ResolveScrubUpdateCommandPosition(PlaybackCommand command)");
        AssertContains(sourceText, "if (ReferenceEquals(_queuedScrubUpdateSlot, slot))\n            {\n                _queuedScrubUpdateSlot = null;\n            }");
        AssertContains(sourceText, "private void ClearQueuedCommandSlotsBarrier()");
        AssertDoesNotContain(updateScrubBlock, "SendCommand(newer)");
        AssertDoesNotContain(updateScrubBlock, "Non-scrub command consumed");

        return Task.CompletedTask;
    }
}
