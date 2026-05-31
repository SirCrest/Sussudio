using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackPlaybackController_CommandQueue_AcceptsNewestControlWhenFull()
    {
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var commandType = controllerType.GetNestedType("PlaybackCommand", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PlaybackCommand not found.");
        var commandKindType = controllerType.GetNestedType("CommandKind", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CommandKind not found.");
        var sendCommand = controllerType.GetMethod("SendCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendCommand not found.");
        var commandChannelField = controllerType.GetField("_commandChannel", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_commandChannel not found.");
        var queueCapacityProperty = controllerType.GetProperty("CommandQueueCapacityCommands", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("CommandQueueCapacityCommands not found.");
        var commandsDroppedProperty = controllerType.GetProperty("CommandsDropped", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("CommandsDropped not found.");
        var pendingCommandsProperty = controllerType.GetProperty("PendingCommands", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("PendingCommands not found.");

        var bufferManager = Activator.CreateInstance(
                bufferManagerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { null },
                culture: null)
            ?? throw new InvalidOperationException("FlashbackBufferManager construction failed.");
        using var disposableBuffer = bufferManager as IDisposable;
        var controller = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController construction failed.");
        using var disposableController = controller as IDisposable;

        var playKind = Enum.Parse(commandKindType, "Play");
        var goLiveKind = Enum.Parse(commandKindType, "GoLive");
        var capacity = (int)queueCapacityProperty.GetValue(controller)!;

        for (var i = 0; i < capacity; i++)
        {
            var playCommand = Activator.CreateInstance(commandType)
                ?? throw new InvalidOperationException("PlaybackCommand play construction failed.");
            SetPropertyOrBackingField(playCommand, "Kind", playKind);
            AssertEqual(true, (bool)sendCommand.Invoke(controller, new[] { playCommand })!, $"Play command {i} enqueues");
        }

        AssertEqual(capacity, (int)pendingCommandsProperty.GetValue(controller)!, "Queue starts full");

        var goLiveCommand = Activator.CreateInstance(commandType)
            ?? throw new InvalidOperationException("PlaybackCommand GoLive construction failed.");
        SetPropertyOrBackingField(goLiveCommand, "Kind", goLiveKind);
        AssertEqual(true, (bool)sendCommand.Invoke(controller, new[] { goLiveCommand })!, "Newest GoLive command is accepted when queue is full");
        AssertEqual(capacity, (int)pendingCommandsProperty.GetValue(controller)!, "Drop-oldest accounting keeps pending bounded at capacity");

        var channel = commandChannelField.GetValue(controller)
            ?? throw new InvalidOperationException("Command channel missing.");
        var sawGoLive = false;
        while (TryReadQueuedPlaybackCommand(channel, commandType, out var command) && command != null)
        {
            if (GetPropertyValue(command, "Kind")?.ToString() == "GoLive")
            {
                sawGoLive = true;
            }
        }

        AssertEqual(true, sawGoLive, "Full command queue preserves the newest GoLive command");
        AssertEqual(true, (long)commandsDroppedProperty.GetValue(controller)! > 0, "Dropped-command diagnostics record the evicted older command");

        return Task.CompletedTask;

        static bool TryReadQueuedPlaybackCommand(object channel, Type commandType, out object? command)
        {
            var reader = channel.GetType().GetProperty("Reader")?.GetValue(channel)
                ?? throw new InvalidOperationException("Command channel reader missing.");
            var tryRead = reader.GetType().GetMethod(
                    "TryRead",
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    types: new[] { commandType.MakeByRefType() },
                    modifiers: null)
                ?? throw new InvalidOperationException("Command channel TryRead not found.");
            object?[] args = { null };
            var result = (bool)tryRead.Invoke(reader, args)!;
            command = args[0];
            return result;
        }
    }


    internal static Task FlashbackPlaybackController_ScrubCoalescing_DoesNotRequeueControlCommands()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var commandQueueText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs")
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

        AssertContains(commandQueueText, "private long _latestScrubUpdateTicks;");
        AssertContains(commandQueueText, "private sealed class SeekIntentSlot");
        AssertContains(commandQueueText, "private sealed class ScrubUpdateIntentSlot");
        AssertContains(sourceText, "public SeekIntentSlot? SeekSlot { get; init; }");
        AssertContains(sourceText, "public ScrubUpdateIntentSlot? ScrubUpdateSlot { get; init; }");
        AssertContains(commandQueueText, "private readonly object _seekSlotSync = new();");
        AssertContains(commandQueueText, "private SeekIntentSlot? _queuedSeekSlot;");
        AssertContains(commandQueueText, "private ScrubUpdateIntentSlot? _queuedScrubUpdateSlot;");
        AssertContains(commandQueueText, "private long _scrubUpdatesCoalesced;");
        AssertContains(commandQueueText, "private long _seekCommandsCoalesced;");
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
        AssertContains(commandQueueText, "private static bool ShouldYieldScrubUpdateToQueuedControl(Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "return next.Kind is CommandKind.EndScrub or CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;");
        AssertContains(commandQueueText, "private static bool ShouldYieldSeekToQueuedPlay(Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "return next.Kind is CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;");
        AssertContains(commandQueueText, "private static bool ShouldYieldPauseFromLiveToQueuedSeekOrPlay(Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "return next.Kind is CommandKind.Seek or CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;");
        AssertContains(drainAbandonedCommands, "ClearQueuedCommandSlotsBarrier();");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)\n        {\n            MarkCommandNoOp(CommandKind.EndScrub, \"live_thread_not_running\", position);\n            return false;\n        }");
        var endScrubBlock = ExtractTextBetween(
            sourceText,
            "private void HandleEndScrubCommand(",
            "    private void HandleNudgeCommand(");
        AssertContains(endScrubBlock, "var endScrubPosition = ClampPosition(cmd.Position, frozenValidStart);");
        AssertContains(endScrubBlock, "PlaybackPosition = endScrubPosition;");
        AssertDoesNotContain(endScrubBlock, "TimeSpan.FromTicks(Interlocked.Read(ref _latestScrubUpdateTicks))");
        AssertContains(endScrubBlock, "var endScrubTarget = SaturatingAdd(endScrubPosition, frozenValidStart);");
        AssertDoesNotContain(endScrubBlock, "var endScrubTarget = SaturatingAdd(PlaybackPosition, frozenValidStart);");
        AssertContains(sourceText, "private bool RejectCommand(\n        CommandKind kind,\n        string failure,\n        string reason,\n        bool returnValue,\n        TimeSpan? position = null)");
        AssertContains(commandQueueText, "private bool RejectCommand(\n        CommandKind kind,\n        string failure,\n        string reason,\n        bool returnValue,\n        TimeSpan? position = null)");
        AssertContains(sourceText, "SetLastCommandFailure($\"{failure}:{kind}{detail}\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_CMD_SKIP kind={kind} reason={reason}{detail}\");");
        AssertContains(sourceText, "private void SetNoFileFailure(CommandKind kind, TimeSpan position)");
        AssertContains(commandQueueText, "private void SetNoFileFailure(CommandKind kind, TimeSpan position)");
        AssertContains(sourceText, "SetLastCommandFailure($\"no_file:{kind}{FormatCommandDetail(position: position)}\");");
        AssertContains(sourceText, "private static string FormatCommandDetail(PlaybackCommand command)");
        AssertContains(commandQueueText, "private static string FormatCommandDetail(PlaybackCommand command)");
        AssertContains(sourceText, "return $\" pos_ms={position.Value.TotalMilliseconds.ToString(\"0.###\", CultureInfo.InvariantCulture)}\";");
        AssertContains(sourceText, "return $\" delta_ms={delta.Value.TotalMilliseconds.ToString(\"0.###\", CultureInfo.InvariantCulture)}\";");
        AssertContains(sourceText, "private void SetLastCommandFailure(string failure)\n    {\n        Volatile.Write(ref _lastCommandFailure, failure);\n        Interlocked.Exchange(ref _lastCommandFailureUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());\n    }");
        AssertContains(commandQueueText, "private void SetLastCommandFailure(string failure)");
        AssertContains(sourceText, "private void MarkCommandQueued(CommandKind kind)");
        AssertContains(sourceText, "private void MarkCommandNoOp(CommandKind kind, string reason, TimeSpan? position = null, TimeSpan? delta = null)");
        AssertContains(commandQueueText, "private void MarkCommandNoOp(CommandKind kind, string reason, TimeSpan? position = null, TimeSpan? delta = null)");
        AssertContains(sourceText, "private void ClearLastCommandFailure()\n    {\n        Volatile.Write(ref _lastCommandFailure, string.Empty);\n        Interlocked.Exchange(ref _lastCommandFailureUtcUnixMs, 0);\n    }");
        AssertContains(commandQueueText, "private void ClearLastCommandFailure()");
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
        AssertContains(commandQueueText, "private PlaybackCommand ResolveSeekCommandPosition(PlaybackCommand command)");
        AssertContains(sourceText, "if (ReferenceEquals(_queuedSeekSlot, slot))\n            {\n                _queuedSeekSlot = null;\n            }");
        AssertContains(commandQueueText, "private PlaybackCommand ResolveScrubUpdateCommandPosition(PlaybackCommand command)");
        AssertContains(sourceText, "if (ReferenceEquals(_queuedScrubUpdateSlot, slot))\n            {\n                _queuedScrubUpdateSlot = null;\n            }");
        AssertContains(commandQueueText, "private void ClearQueuedCommandSlotsBarrier()");
        AssertDoesNotContain(updateScrubBlock, "SendCommand(newer)");
        AssertDoesNotContain(updateScrubBlock, "Non-scrub command consumed");

        return Task.CompletedTask;
    }


    internal static Task FlashbackPlaybackController_SeekSlots_PreserveControlCommandBarriers()
    {
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var commandType = controllerType.GetNestedType("PlaybackCommand", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PlaybackCommand not found.");
        var commandKindType = controllerType.GetNestedType("CommandKind", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CommandKind not found.");
        var seekSlotType = controllerType.GetNestedType("SeekIntentSlot", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SeekIntentSlot not found.");
        var scrubUpdateSlotType = controllerType.GetNestedType("ScrubUpdateIntentSlot", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ScrubUpdateIntentSlot not found.");
        var resolve = controllerType.GetMethod("ResolveSeekCommandPosition", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveSeekCommandPosition not found.");
        var resolveScrub = controllerType.GetMethod("ResolveScrubUpdateCommandPosition", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResolveScrubUpdateCommandPosition not found.");
        var sendSeek = controllerType.GetMethod("SendSeekCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendSeekCommand not found.");
        var sendUpdateScrub = controllerType.GetMethod("SendUpdateScrubCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendUpdateScrubCommand not found.");
        var sendEndScrub = controllerType.GetMethod("SendEndScrubCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendEndScrubCommand not found.");
        var sendCommand = controllerType.GetMethod("SendCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendCommand not found.");
        var latestTicksField = seekSlotType.GetField("LatestTicks", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SeekIntentSlot.LatestTicks not found.");
        var scrubLatestTicksField = scrubUpdateSlotType.GetField("LatestTicks", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ScrubUpdateIntentSlot.LatestTicks not found.");
        var queuedSeekSlotField = controllerType.GetField("_queuedSeekSlot", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_queuedSeekSlot not found.");
        var queuedScrubSlotField = controllerType.GetField("_queuedScrubUpdateSlot", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_queuedScrubUpdateSlot not found.");
        var commandChannelField = controllerType.GetField("_commandChannel", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_commandChannel not found.");

        var bufferManager = Activator.CreateInstance(
                bufferManagerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { null },
                culture: null)
            ?? throw new InvalidOperationException("FlashbackBufferManager construction failed.");
        using var disposableBuffer = bufferManager as IDisposable;
        var controller = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController construction failed.");
        using var disposableController = controller as IDisposable;

        var seekKind = Enum.Parse(commandKindType, "Seek");
        var updateScrubKind = Enum.Parse(commandKindType, "UpdateScrub");
        var playKind = Enum.Parse(commandKindType, "Play");
        var oneSecond = TimeSpan.FromSeconds(1);
        var twoSeconds = TimeSpan.FromSeconds(2);
        var threeSeconds = TimeSpan.FromSeconds(3);
        var fourSeconds = TimeSpan.FromSeconds(4);

        var slotA = Activator.CreateInstance(
                seekSlotType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { oneSecond.Ticks },
                culture: null)
            ?? throw new InvalidOperationException("SeekIntentSlot A construction failed.");
        var commandA = Activator.CreateInstance(commandType)
            ?? throw new InvalidOperationException("PlaybackCommand construction failed.");
        SetPropertyOrBackingField(commandA, "Kind", seekKind);
        SetPropertyOrBackingField(commandA, "Position", oneSecond);
        SetPropertyOrBackingField(commandA, "SeekSlot", slotA);

        queuedSeekSlotField.SetValue(controller, slotA);
        latestTicksField.SetValue(slotA, twoSeconds.Ticks);
        var resolvedCoalesced = resolve.Invoke(controller, new[] { commandA })
            ?? throw new InvalidOperationException("Resolve coalesced seek returned null.");
        AssertEqual(twoSeconds, (TimeSpan)GetPropertyValue(resolvedCoalesced, "Position")!, "Coalesced seek slot resolves latest position");
        AssertEqual(null, queuedSeekSlotField.GetValue(controller), "Resolved active seek slot is cleared");

        latestTicksField.SetValue(slotA, oneSecond.Ticks);
        var slotB = Activator.CreateInstance(
                seekSlotType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { threeSeconds.Ticks },
                culture: null)
            ?? throw new InvalidOperationException("SeekIntentSlot B construction failed.");
        queuedSeekSlotField.SetValue(controller, slotB);
        var resolvedBarrier = resolve.Invoke(controller, new[] { commandA })
            ?? throw new InvalidOperationException("Resolve barrier seek returned null.");
        AssertEqual(oneSecond, (TimeSpan)GetPropertyValue(resolvedBarrier, "Position")!, "Older seek slot does not consume later barrier-separated target");
        if (!ReferenceEquals(slotB, queuedSeekSlotField.GetValue(controller)))
        {
            throw new InvalidOperationException("Later seek slot should remain queued after resolving older barrier-separated seek.");
        }

        var scrubSlotA = Activator.CreateInstance(
                scrubUpdateSlotType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { oneSecond.Ticks },
                culture: null)
            ?? throw new InvalidOperationException("ScrubUpdateIntentSlot A construction failed.");
        var updateCommandA = Activator.CreateInstance(commandType)
            ?? throw new InvalidOperationException("PlaybackCommand update construction failed.");
        SetPropertyOrBackingField(updateCommandA, "Kind", updateScrubKind);
        SetPropertyOrBackingField(updateCommandA, "Position", oneSecond);
        SetPropertyOrBackingField(updateCommandA, "ScrubUpdateSlot", scrubSlotA);

        queuedScrubSlotField.SetValue(controller, scrubSlotA);
        scrubLatestTicksField.SetValue(scrubSlotA, twoSeconds.Ticks);
        var resolvedScrubCoalesced = resolveScrub.Invoke(controller, new[] { updateCommandA })
            ?? throw new InvalidOperationException("Resolve coalesced scrub update returned null.");
        AssertEqual(twoSeconds, (TimeSpan)GetPropertyValue(resolvedScrubCoalesced, "Position")!, "Coalesced scrub slot resolves latest position");
        AssertEqual(null, queuedScrubSlotField.GetValue(controller), "Resolved active scrub slot is cleared");

        scrubLatestTicksField.SetValue(scrubSlotA, oneSecond.Ticks);
        var scrubSlotB = Activator.CreateInstance(
                scrubUpdateSlotType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { threeSeconds.Ticks },
                culture: null)
            ?? throw new InvalidOperationException("ScrubUpdateIntentSlot B construction failed.");
        queuedScrubSlotField.SetValue(controller, scrubSlotB);
        var resolvedScrubBarrier = resolveScrub.Invoke(controller, new[] { updateCommandA })
            ?? throw new InvalidOperationException("Resolve barrier scrub update returned null.");
        AssertEqual(oneSecond, (TimeSpan)GetPropertyValue(resolvedScrubBarrier, "Position")!, "Older scrub slot does not consume later barrier-separated target");
        if (!ReferenceEquals(scrubSlotB, queuedScrubSlotField.GetValue(controller)))
        {
            throw new InvalidOperationException("Later scrub slot should remain queued after resolving older barrier-separated scrub update.");
        }

        var producerController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Producer FlashbackPlaybackController construction failed.");
        using var disposableProducerController = producerController as IDisposable;

        AssertEqual(true, (bool)sendSeek.Invoke(producerController, new object[] { oneSecond })!, "First producer seek enqueues");
        AssertEqual(true, (bool)sendSeek.Invoke(producerController, new object[] { twoSeconds })!, "Adjacent producer seek coalesces");
        var playCommand = Activator.CreateInstance(commandType)
            ?? throw new InvalidOperationException("PlaybackCommand play construction failed.");
        SetPropertyOrBackingField(playCommand, "Kind", playKind);
        AssertEqual(true, (bool)sendCommand.Invoke(producerController, new[] { playCommand })!, "Producer play barrier enqueues");
        AssertEqual(null, queuedSeekSlotField.GetValue(producerController), "Accepted non-seek barrier closes active seek slot before later seeks");
        AssertEqual(true, (bool)sendSeek.Invoke(producerController, new object[] { threeSeconds })!, "Post-barrier producer seek enqueues new slot");
        AssertEqual(true, (bool)sendUpdateScrub.Invoke(producerController, new object[] { oneSecond })!, "Producer scrub update barrier enqueues");
        AssertEqual(null, queuedSeekSlotField.GetValue(producerController), "Accepted scrub update barrier closes active seek slot before later seeks");
        AssertEqual(true, (bool)sendSeek.Invoke(producerController, new object[] { fourSeconds })!, "Post-scrub-barrier producer seek enqueues new slot");

        var channel = commandChannelField.GetValue(producerController)
            ?? throw new InvalidOperationException("Producer command channel missing.");
        var firstQueued = ReadQueuedPlaybackCommand(channel, commandType, "first queued command");
        var resolvedFirstQueued = resolve.Invoke(producerController, new[] { firstQueued })
            ?? throw new InvalidOperationException("Resolve first producer seek returned null.");
        AssertEqual("Seek", GetPropertyValue(resolvedFirstQueued, "Kind")?.ToString(), "First queued producer command kind");
        AssertEqual(twoSeconds, (TimeSpan)GetPropertyValue(resolvedFirstQueued, "Position")!, "Adjacent producer seeks resolve to latest pre-barrier position");

        var secondQueued = ReadQueuedPlaybackCommand(channel, commandType, "second queued command");
        AssertEqual("Play", GetPropertyValue(secondQueued, "Kind")?.ToString(), "Second queued producer command is the barrier");

        var thirdQueued = ReadQueuedPlaybackCommand(channel, commandType, "third queued command");
        var resolvedThirdQueued = resolve.Invoke(producerController, new[] { thirdQueued })
            ?? throw new InvalidOperationException("Resolve third producer seek returned null.");
        AssertEqual("Seek", GetPropertyValue(resolvedThirdQueued, "Kind")?.ToString(), "Third queued producer command kind");
        AssertEqual(threeSeconds, (TimeSpan)GetPropertyValue(resolvedThirdQueued, "Position")!, "Post-barrier producer seek keeps its own position");

        var fourthQueued = ReadQueuedPlaybackCommand(channel, commandType, "fourth queued command");
        var resolvedFourthQueued = resolveScrub.Invoke(producerController, new[] { fourthQueued })
            ?? throw new InvalidOperationException("Resolve fourth producer scrub update returned null.");
        AssertEqual("UpdateScrub", GetPropertyValue(resolvedFourthQueued, "Kind")?.ToString(), "Fourth queued producer command is the scrub barrier");
        AssertEqual(oneSecond, (TimeSpan)GetPropertyValue(resolvedFourthQueued, "Position")!, "Scrub barrier command keeps its own position");

        var fifthQueued = ReadQueuedPlaybackCommand(channel, commandType, "fifth queued command");
        var resolvedFifthQueued = resolve.Invoke(producerController, new[] { fifthQueued })
            ?? throw new InvalidOperationException("Resolve fifth producer seek returned null.");
        AssertEqual("Seek", GetPropertyValue(resolvedFifthQueued, "Kind")?.ToString(), "Fifth queued producer command kind");
        AssertEqual(fourSeconds, (TimeSpan)GetPropertyValue(resolvedFifthQueued, "Position")!, "Post-scrub-barrier producer seek keeps its own position");
        AssertEqual(false, TryReadQueuedPlaybackCommand(channel, commandType, out _), "No extra producer commands are queued");

        var scrubProducerController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Scrub producer FlashbackPlaybackController construction failed.");
        using var disposableScrubProducerController = scrubProducerController as IDisposable;

        AssertEqual(true, (bool)sendUpdateScrub.Invoke(scrubProducerController, new object[] { oneSecond })!, "First producer scrub update enqueues");
        AssertEqual(true, (bool)sendUpdateScrub.Invoke(scrubProducerController, new object[] { twoSeconds })!, "Adjacent producer scrub update coalesces");
        AssertEqual(true, (bool)sendEndScrub.Invoke(scrubProducerController, new object?[] { null })!, "Producer end scrub barrier enqueues");
        AssertEqual(null, queuedScrubSlotField.GetValue(scrubProducerController), "EndScrub closes active scrub slot before later updates");
        AssertEqual(true, (bool)sendUpdateScrub.Invoke(scrubProducerController, new object[] { threeSeconds })!, "Post-barrier producer scrub update enqueues new slot");

        var scrubChannel = commandChannelField.GetValue(scrubProducerController)
            ?? throw new InvalidOperationException("Scrub producer command channel missing.");
        var firstScrubQueued = ReadQueuedPlaybackCommand(scrubChannel, commandType, "first queued scrub command");
        var resolvedFirstScrubQueued = resolveScrub.Invoke(scrubProducerController, new[] { firstScrubQueued })
            ?? throw new InvalidOperationException("Resolve first producer scrub update returned null.");
        AssertEqual("UpdateScrub", GetPropertyValue(resolvedFirstScrubQueued, "Kind")?.ToString(), "First queued producer scrub command kind");
        AssertEqual(twoSeconds, (TimeSpan)GetPropertyValue(resolvedFirstScrubQueued, "Position")!, "Adjacent producer scrub updates resolve to latest pre-barrier position");

        var secondScrubQueued = ReadQueuedPlaybackCommand(scrubChannel, commandType, "second queued scrub command");
        AssertEqual("EndScrub", GetPropertyValue(secondScrubQueued, "Kind")?.ToString(), "Second queued producer scrub command is the barrier");
        AssertEqual(twoSeconds, (TimeSpan)GetPropertyValue(secondScrubQueued, "Position")!, "EndScrub snapshots the latest pre-barrier scrub target");

        var thirdScrubQueued = ReadQueuedPlaybackCommand(scrubChannel, commandType, "third queued scrub command");
        var resolvedThirdScrubQueued = resolveScrub.Invoke(scrubProducerController, new[] { thirdScrubQueued })
            ?? throw new InvalidOperationException("Resolve third producer scrub update returned null.");
        AssertEqual("UpdateScrub", GetPropertyValue(resolvedThirdScrubQueued, "Kind")?.ToString(), "Third queued producer scrub command kind");
        AssertEqual(threeSeconds, (TimeSpan)GetPropertyValue(resolvedThirdScrubQueued, "Position")!, "Post-barrier producer scrub update keeps its own position");
        AssertEqual(false, TryReadQueuedPlaybackCommand(scrubChannel, commandType, out _), "No extra producer scrub commands are queued");

        return Task.CompletedTask;

        static object ReadQueuedPlaybackCommand(object channel, Type commandType, string label)
        {
            if (!TryReadQueuedPlaybackCommand(channel, commandType, out var command) || command is null)
            {
                throw new InvalidOperationException($"Expected {label}.");
            }

            return command;
        }

        static bool TryReadQueuedPlaybackCommand(object channel, Type commandType, out object? command)
        {
            var reader = channel.GetType().GetProperty("Reader")?.GetValue(channel)
                ?? throw new InvalidOperationException("Command channel reader missing.");
            var tryRead = reader.GetType().GetMethod(
                    "TryRead",
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    types: new[] { commandType.MakeByRefType() },
                    modifiers: null)
                ?? throw new InvalidOperationException("Command channel TryRead not found.");
            object?[] args = { null };
            var result = (bool)tryRead.Invoke(reader, args)!;
            command = args[0];
            return result;
        }
    }


    internal static Task FlashbackPlaybackController_SeekSlots_PreserveSlotStateAfterRejectedBarriers()
    {
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var commandType = controllerType.GetNestedType("PlaybackCommand", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PlaybackCommand not found.");
        var commandKindType = controllerType.GetNestedType("CommandKind", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CommandKind not found.");
        var sendSeek = controllerType.GetMethod("SendSeekCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendSeekCommand not found.");
        var sendUpdateScrub = controllerType.GetMethod("SendUpdateScrubCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendUpdateScrubCommand not found.");
        var sendEndScrub = controllerType.GetMethod("SendEndScrubCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendEndScrubCommand not found.");
        var sendCommand = controllerType.GetMethod("SendCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendCommand not found.");
        var queuedSeekSlotField = controllerType.GetField("_queuedSeekSlot", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_queuedSeekSlot not found.");
        var queuedScrubSlotField = controllerType.GetField("_queuedScrubUpdateSlot", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_queuedScrubUpdateSlot not found.");
        var commandChannelField = controllerType.GetField("_commandChannel", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("_commandChannel not found.");

        var bufferManager = Activator.CreateInstance(
                bufferManagerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { null },
                culture: null)
            ?? throw new InvalidOperationException("FlashbackBufferManager construction failed.");
        using var disposableBuffer = bufferManager as IDisposable;

        var playKind = Enum.Parse(commandKindType, "Play");
        var playCommand = Activator.CreateInstance(commandType)
            ?? throw new InvalidOperationException("PlaybackCommand play construction failed.");
        SetPropertyOrBackingField(playCommand, "Kind", playKind);
        var oneSecond = TimeSpan.FromSeconds(1);
        var twoSeconds = TimeSpan.FromSeconds(2);
        var failedBarrierController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Failed barrier FlashbackPlaybackController construction failed.");
        using var disposableFailedBarrierController = failedBarrierController as IDisposable;

        AssertEqual(true, (bool)sendSeek.Invoke(failedBarrierController, new object[] { oneSecond })!, "Failed-barrier setup seek enqueues");
        var failedBarrierSlot = queuedSeekSlotField.GetValue(failedBarrierController)
            ?? throw new InvalidOperationException("Failed-barrier seek slot missing.");
        var failedBarrierChannel = commandChannelField.GetValue(failedBarrierController)
            ?? throw new InvalidOperationException("Failed-barrier command channel missing.");
        CompleteQueuedPlaybackCommands(failedBarrierChannel);
        AssertEqual(false, (bool)sendCommand.Invoke(failedBarrierController, new[] { playCommand })!, "Rejected play barrier reports failure");
        if (!ReferenceEquals(failedBarrierSlot, queuedSeekSlotField.GetValue(failedBarrierController)))
        {
            throw new InvalidOperationException("Rejected play barrier should preserve the active seek slot.");
        }

        var failedSeekController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Failed seek FlashbackPlaybackController construction failed.");
        using var disposableFailedSeekController = failedSeekController as IDisposable;

        AssertEqual(true, (bool)sendUpdateScrub.Invoke(failedSeekController, new object[] { oneSecond })!, "Failed-seek setup scrub update enqueues");
        var failedSeekScrubSlot = queuedScrubSlotField.GetValue(failedSeekController)
            ?? throw new InvalidOperationException("Failed-seek scrub slot missing.");
        var failedSeekChannel = commandChannelField.GetValue(failedSeekController)
            ?? throw new InvalidOperationException("Failed-seek command channel missing.");
        CompleteQueuedPlaybackCommands(failedSeekChannel);
        AssertEqual(false, (bool)sendSeek.Invoke(failedSeekController, new object[] { twoSeconds })!, "Rejected seek barrier reports failure");
        if (!ReferenceEquals(failedSeekScrubSlot, queuedScrubSlotField.GetValue(failedSeekController)))
        {
            throw new InvalidOperationException("Rejected seek should preserve the active scrub slot.");
        }
        AssertEqual(null, queuedSeekSlotField.GetValue(failedSeekController), "Rejected seek clears only its own newly-created seek slot");

        var failedScrubUpdateController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Failed scrub update FlashbackPlaybackController construction failed.");
        using var disposableFailedScrubUpdateController = failedScrubUpdateController as IDisposable;

        AssertEqual(true, (bool)sendSeek.Invoke(failedScrubUpdateController, new object[] { oneSecond })!, "Failed-scrub-update setup seek enqueues");
        var failedScrubUpdateSeekSlot = queuedSeekSlotField.GetValue(failedScrubUpdateController)
            ?? throw new InvalidOperationException("Failed-scrub-update seek slot missing.");
        var failedScrubUpdateChannel = commandChannelField.GetValue(failedScrubUpdateController)
            ?? throw new InvalidOperationException("Failed-scrub-update command channel missing.");
        CompleteQueuedPlaybackCommands(failedScrubUpdateChannel);
        AssertEqual(false, (bool)sendUpdateScrub.Invoke(failedScrubUpdateController, new object[] { twoSeconds })!, "Rejected scrub update barrier reports failure");
        if (!ReferenceEquals(failedScrubUpdateSeekSlot, queuedSeekSlotField.GetValue(failedScrubUpdateController)))
        {
            throw new InvalidOperationException("Rejected scrub update should preserve the active seek slot.");
        }
        AssertEqual(null, queuedScrubSlotField.GetValue(failedScrubUpdateController), "Rejected scrub update clears only its own newly-created scrub slot");

        var failedEndScrubController = Activator.CreateInstance(
                controllerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new[] { bufferManager },
                culture: null)
            ?? throw new InvalidOperationException("Failed end scrub FlashbackPlaybackController construction failed.");
        using var disposableFailedEndScrubController = failedEndScrubController as IDisposable;

        AssertEqual(true, (bool)sendUpdateScrub.Invoke(failedEndScrubController, new object[] { oneSecond })!, "Failed-end-scrub setup update enqueues");
        AssertEqual(true, (bool)sendUpdateScrub.Invoke(failedEndScrubController, new object[] { twoSeconds })!, "Failed-end-scrub setup update coalesces");
        var failedEndScrubSlot = queuedScrubSlotField.GetValue(failedEndScrubController)
            ?? throw new InvalidOperationException("Failed-end-scrub slot missing.");
        var failedEndScrubChannel = commandChannelField.GetValue(failedEndScrubController)
            ?? throw new InvalidOperationException("Failed-end-scrub command channel missing.");
        CompleteQueuedPlaybackCommands(failedEndScrubChannel);
        AssertEqual(false, (bool)sendEndScrub.Invoke(failedEndScrubController, new object?[] { null })!, "Rejected end scrub barrier reports failure");
        if (!ReferenceEquals(failedEndScrubSlot, queuedScrubSlotField.GetValue(failedEndScrubController)))
        {
            throw new InvalidOperationException("Rejected end scrub should preserve the active scrub slot.");
        }

        return Task.CompletedTask;

        static void CompleteQueuedPlaybackCommands(object channel)
        {
            var writer = channel.GetType().GetProperty("Writer")?.GetValue(channel)
                ?? throw new InvalidOperationException("Command channel writer missing.");
            var tryComplete = writer.GetType().GetMethod(
                    "TryComplete",
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    types: new[] { typeof(Exception) },
                    modifiers: null)
                ?? throw new InvalidOperationException("Command channel TryComplete not found.");
            _ = (bool)tryComplete.Invoke(writer, new object?[] { null })!;
        }
    }
}

static partial class Program
{
    private static string ReadFlashbackPlaybackControllerPlaybackSource()
        => ReadFlashbackPlaybackControllerSource();

    internal static Task FlashbackPlaybackController_PlaybackThreadExit_RearmsWorkerStart()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var threadLifecycleText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs")
            .Replace("\r\n", "\n");
        var threadLoopText = ExtractTextBetween(
            threadLifecycleText,
            "private void PlaybackThreadEntry(",
            "    private bool EnsurePlaybackThread(");
        var threadCommandDispatchText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs")
            .Replace("\r\n", "\n");
        var threadSeekCommandsText = threadCommandDispatchText;
        var threadSeekScrubCommandsText = threadCommandDispatchText;
        var threadEndScrubCommandText = threadCommandDispatchText;
        var threadPlayCommandText = threadCommandDispatchText;
        var threadPauseCommandText = threadCommandDispatchText;
        var threadNudgeCommandText = threadCommandDispatchText;
        var commandQueueText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs")
            .Replace("\r\n", "\n");
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(threadLoopText, "private void PlaybackThreadEntry(CancellationTokenSource cts, Channel<PlaybackCommand> commandChannel)");
        AssertContains(commandQueueText, "private enum CommandKind");
        AssertContains(commandQueueText, "private readonly struct PlaybackCommand");
        AssertContains(commandQueueText, "public SeekIntentSlot? SeekSlot { get; init; }");
        AssertContains(commandQueueText, "public ScrubUpdateIntentSlot? ScrubUpdateSlot { get; init; }");
        AssertDoesNotContain(rootText, "private enum CommandKind");
        AssertDoesNotContain(rootText, "private readonly struct PlaybackCommand");
        AssertContains(threadLifecycleText, "[DllImport(\"winmm.dll\", ExactSpelling = true)]");
        AssertContains(threadLifecycleText, "private static extern uint timeBeginPeriod(uint uMilliseconds);");
        AssertContains(threadLifecycleText, "private static extern uint timeEndPeriod(uint uMilliseconds);");
        AssertContains(threadLoopText, "Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_ENTER\");");
        AssertContains(threadCommandDispatchText, "private bool ExecutePlaybackCommand(");
        AssertContains(threadSeekCommandsText, "private void HandleSeekCommand(");
        AssertContains(threadSeekScrubCommandsText, "private void HandleBeginScrubCommand(");
        AssertContains(threadSeekScrubCommandsText, "private void HandleUpdateScrubCommand(");
        AssertContains(threadEndScrubCommandText, "private void HandleEndScrubCommand(");
        AssertContains(threadSeekCommandsText, "private void HandleBeginScrubCommand(");
        AssertContains(threadSeekCommandsText, "private void HandleUpdateScrubCommand(");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.ThreadSeekCommands.cs")), "Seek/scrub command handlers stay folded into ThreadCommands.cs");
        AssertContains(threadPlayCommandText, "private void HandlePlayCommand(");
        AssertContains(threadPlayCommandText, "PrimePlaybackAudioBuffer(decoder, prebufferedFrames, ref fileOpen, seekTarget, \"play\", cts.Token);");
        AssertContains(threadPauseCommandText, "private void HandlePauseCommand(");
        AssertContains(threadCommandDispatchText, "private void HandleGoLiveCommand(");
        AssertContains(threadNudgeCommandText, "private void HandleNudgeCommand(");
        AssertContains(threadSeekCommandsText, "cmd = ResolveSeekCommandPosition(cmd);");
        AssertContains(threadSeekScrubCommandsText, "SafeSuppressPreviewSubmission(\"begin_scrub\")");
        AssertContains(threadCommandDispatchText, "Logger.Log(\"FLASHBACK_PLAYBACK_GO_LIVE\");");
        AssertContains(agentMapText, "FlashbackPlaybackController.ThreadCommands.cs");
        AssertContains(agentMapText, "FlashbackPlaybackController.ThreadCommands.cs");
        AssertContains(agentMapText, "FlashbackPlaybackController.ThreadCommands.cs");
        AssertContains(cleanupPlanText, "FlashbackPlaybackController.ThreadCommands.cs");
        AssertContains(cleanupPlanText, "FlashbackPlaybackController.ThreadCommands.cs");
        AssertContains(cleanupPlanText, "FlashbackPlaybackController.ThreadCommands.cs");
        AssertContains(agentMapText, "FlashbackPlaybackController.ThreadCommands.cs");
        AssertContains(cleanupPlanText, "FlashbackPlaybackController.ThreadCommands.cs");
        AssertContains(threadCommandDispatchText, "HandleSeekCommand(ref cmd, commandChannel, cts, ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, ref frozenValidStart, ref pendingExactResumeTarget, ref frameDuration, prebufferedFrames, pacingStopwatch);");
        AssertContains(threadCommandDispatchText, "HandleGoLiveCommand(ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, ref pendingExactResumeTarget);");
        AssertContains(threadLoopText, "if (!ExecutePlaybackCommand(ref cmd, commandChannel, cts, ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, ref frozenValidStart, ref pendingExactResumeTarget, ref frameDuration, prebufferedFrames, pacingStopwatch))");
        AssertDoesNotContain(threadLoopText, "case CommandKind.Seek:");
        AssertDoesNotContain(threadLoopText, "cmd = ResolveSeekCommandPosition(cmd);");
        AssertDoesNotContain(threadLoopText, "SafeSuppressPreviewSubmission(\"begin_scrub\")");
        AssertDoesNotContain(threadLoopText, "Logger.Log(\"FLASHBACK_PLAYBACK_GO_LIVE\");");
        AssertContains(sourceText, "if (Volatile.Read(ref _playbackThreadStarted) != 0 && thread is { IsAlive: true })\n            {\n                SendCommand(new PlaybackCommand { Kind = CommandKind.Stop });\n            }");
        AssertContains(threadCommandDispatchText, "case CommandKind.Stop:\n                    HandleStopCommand(ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, ref pendingExactResumeTarget);\n                    return false;");
        AssertContains(threadCommandDispatchText, "private void HandleStopCommand(");
        AssertContains(threadCommandDispatchText, "isPlaying = false;\n        isScrubbing = false;\n        pendingExactResumeTarget = null;");
        AssertContains(threadCommandDispatchText, "RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_stop\");\n        Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT\");");
        AssertDoesNotContain(threadLoopText, "isPlaying = false;\n                            isScrubbing = false;\n                            pendingExactResumeTarget = null;\n                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_stop\");");
        AssertContains(sourceText, "private void RestoreLiveForPlaybackThreadExit(");
        AssertContains(sourceText, "Interlocked.Exchange(ref _lastVideoPtsTicks, 0);\n        RestoreLiveAudio();\n        SafeResumePreviewSubmission(operation);\n        SetState(FlashbackPlaybackState.Live);");
        AssertDoesNotContain(sourceText, "_suppressAudioUntilPtsTicks");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)\n        {\n            MarkCommandNoOp(CommandKind.GoLive, \"live_thread_not_running\");\n            return false;\n        }");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)\n        {\n            MarkCommandNoOp(CommandKind.Nudge, \"live_thread_not_running\", delta: delta);\n            return false;\n        }");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CMD_NOOP kind={kind} reason={reason}{FormatCommandDetail(position, delta)}");
        AssertContains(threadLifecycleText, "private bool EnsurePlaybackThread(CommandKind commandKind)");
        AssertContains(threadLifecycleText, "private static readonly TimeSpan PlaybackThreadStopTimeout = TimeSpan.FromSeconds(3);");
        AssertContains(threadLifecycleText, "private static readonly TimeSpan PreviewDetachThreadStopTimeout = TimeSpan.FromSeconds(10);");
        AssertContains(threadLifecycleText, "private const int CommandQueueCapacity = 256;");
        AssertContains(threadLifecycleText, "private readonly object _playbackThreadSync = new();");
        AssertContains(threadLifecycleText, "private Thread? _playbackThread;");
        AssertContains(threadLifecycleText, "private int _playbackThreadStarted;");
        AssertContains(threadLifecycleText, "private CancellationTokenSource? _playCts;");
        AssertContains(threadLifecycleText, "private Channel<PlaybackCommand> _commandChannel;");
        AssertDoesNotContain(rootText, "private static readonly TimeSpan PlaybackThreadStopTimeout = TimeSpan.FromSeconds(3);");
        AssertDoesNotContain(rootText, "private static readonly TimeSpan PreviewDetachThreadStopTimeout = TimeSpan.FromSeconds(10);");
        AssertDoesNotContain(rootText, "private const int CommandQueueCapacity = 256;");
        AssertDoesNotContain(rootText, "private readonly object _playbackThreadSync = new();");
        AssertDoesNotContain(rootText, "private Thread? _playbackThread;");
        AssertDoesNotContain(rootText, "private int _playbackThreadStarted;");
        AssertDoesNotContain(rootText, "private CancellationTokenSource? _playCts;");
        AssertDoesNotContain(rootText, "private Channel<PlaybackCommand> _commandChannel;");
        AssertContains(threadLifecycleText, "lock (_playbackThreadSync)");
        AssertContains(sourceText, "if (_disposedFlag != 0) return RejectCommand(commandKind, \"disposed\", \"disposed\", false);");
        AssertContains(threadLifecycleText, "if (Volatile.Read(ref _playbackThreadStarted) != 0)\n            {\n                if (_playbackThread is { IsAlive: true })");
        AssertContains(threadLifecycleText, "FLASHBACK_PLAYBACK_THREAD_RECOVER reason=stale_stopped");
        AssertContains(threadLifecycleText, "Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_RECOVER reason=stale_stopped\");\n                DrainAbandonedCommandsOnThreadExit(_commandChannel);");
        AssertContains(threadLifecycleText, "DisposePlaybackCtsBestEffort(_playCts, \"recover_stale_thread\");");
        AssertContains(threadLifecycleText, "Volatile.Write(ref _playbackThreadStarted, 0);\n            }\n\n            if (Interlocked.CompareExchange(ref _playbackThreadStarted, 1, 0) != 0)");
        AssertContains(threadLifecycleText, "private bool StopPlaybackThread(");
        AssertContains(sourceText, "ObjectDisposedException.ThrowIf(_disposedFlag != 0, this);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_UPDATE_SKIP reason=disposed");
        AssertContains(commandQueueText, "public int CommandQueueCapacityCommands => CommandQueueCapacity;");
        AssertContains(commandQueueText, "public long CommandsEnqueued => Interlocked.Read(ref _commandsEnqueued);");
        AssertContains(commandQueueText, "public long CommandsSkippedNotReady => Interlocked.Read(ref _commandsSkippedNotReady);");
        AssertContains(commandQueueText, "public string LastCommandFailure => Volatile.Read(ref _lastCommandFailure);");
        AssertContains(commandQueueText, "public bool PlaybackThreadAlive => _playbackThread is { IsAlive: true };");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.CommandTelemetry.cs")), "Flashback playback command telemetry stays folded into CommandQueue.cs");
        AssertContains(commandQueueText, "private long _commandsEnqueued;");
        AssertContains(commandQueueText, "private long _commandsProcessed;");
        AssertContains(commandQueueText, "private long _commandsDropped;");
        AssertContains(commandQueueText, "private long _commandsSkippedNotReady;");
        AssertContains(commandQueueText, "private int _pendingCommands;");
        AssertContains(commandQueueText, "private int _maxPendingCommands;");
        AssertContains(commandQueueText, "private long _lastCommandQueueLatencyMs;");
        AssertContains(commandQueueText, "private long _maxCommandQueueLatencyMs;");
        AssertContains(commandQueueText, "private string _lastCommandFailure = string.Empty;");
        AssertContains(commandQueueText, "private bool IsReady => _initialized && _disposedFlag == 0;");
        AssertContains(commandQueueText, "private bool IsNotReady(CommandKind kind, TimeSpan? position = null)");
        AssertContains(commandQueueText, "private bool RejectCommand(");
        AssertContains(commandQueueText, "private static string FormatCommandDetail(PlaybackCommand command)");
        AssertContains(commandQueueText, "private void SetLastCommandFailure(string failure)");
        AssertContains(commandQueueText, "private void MarkCommandNoOp(CommandKind kind, string reason, TimeSpan? position = null, TimeSpan? delta = null)");
        AssertContains(commandQueueText, "private int _activeCommandKind = -1;");
        AssertContains(commandQueueText, "private long _activeCommandStartedTimestamp;");
        AssertDoesNotContain(rootText, "private long _commandsEnqueued;");
        AssertDoesNotContain(rootText, "private int _pendingCommands;");
        AssertDoesNotContain(rootText, "private string _lastCommandFailure = string.Empty;");
        AssertDoesNotContain(rootText, "private int _activeCommandKind = -1;");
        AssertContains(sourceText, "private Channel<PlaybackCommand> _commandChannel;");
        AssertContains(sourceText, "_commandChannel = CreateCommandChannel();");
        AssertContains(sourceText, "_commandChannel = CreateCommandChannel();");
        AssertContains(threadLifecycleText, "private Channel<PlaybackCommand> CreateCommandChannel()");
        AssertContains(threadLifecycleText, "Channel.CreateBounded<PlaybackCommand>");
        AssertContains(threadLifecycleText, "new BoundedChannelOptions(CommandQueueCapacity)");
        AssertContains(threadLifecycleText, "FullMode = BoundedChannelFullMode.Wait");
        AssertContains(sourceText, "private bool IsCommandChannelOpenForDropRetry()");
        AssertContains(sourceText, "private bool TryDropOldestQueuedCommandForNewCommand(out PlaybackCommand droppedCommand)");
        AssertContains(sourceText, "private void TrackDroppedQueuedCommand(PlaybackCommand droppedCommand, CommandKind newCommandKind)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CMD_DROP_OLD kind={droppedCommand.Kind}{detail} new_kind={newCommandKind} reason=channel_full");
        AssertContains(sourceText, "private void ClearQueuedCommandSlotForDroppedCommand(PlaybackCommand command)");
        AssertDoesNotContain(sourceText, "Channel.CreateUnbounded<PlaybackCommand>");
        AssertContains(threadLifecycleText, "catch (Exception ex)\n            {\n                Logger.Log($\"FLASHBACK_PLAYBACK_THREAD_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'\");");
        AssertContains(threadLifecycleText, "DisposePlaybackCtsBestEffort(_playCts, \"thread_start_fail\");");
        AssertContains(threadLifecycleText, "_playbackThread = null;\n                Interlocked.Exchange(ref _playbackThreadStarted, 0);");
        AssertContains(threadLifecycleText, "return RejectCommand(\n                    commandKind,\n                    $\"thread_start_failed:{ex.GetType().Name}:{ex.Message}\",\n                    $\"thread_start_failed type={ex.GetType().Name}\",\n                    false);");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_GO_LIVE\");\n        return;");
        AssertContains(sourceText, "var commandChannel = _commandChannel;");
        AssertContains(sourceText, "_playbackThread = new Thread(() => PlaybackThreadEntry(threadCts, commandChannel))");
        AssertContains(sourceText, "private void PlaybackThreadEntry(CancellationTokenSource cts, Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_TASK");
        AssertContains(sourceText, "SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_PRIORITY");
        AssertContains(threadLifecycleText, "private readonly string _playbackMmcssTask = Environment.GetEnvironmentVariable(\"SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_TASK\") ?? \"Playback\";");
        AssertContains(threadLifecycleText, "private readonly int _playbackMmcssPriority = EnvironmentHelpers.GetIntFromEnv(\"SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_PRIORITY\", 1, -2, 2);");
        AssertDoesNotContain(rootText, "private readonly string _playbackMmcssTask");
        AssertDoesNotContain(rootText, "private readonly int _playbackMmcssPriority");
        AssertContains(sourceText, "using var mmcss = MmcssThreadRegistration.TryRegister(_playbackMmcssTask, _playbackMmcssPriority, message => Logger.Log(message));");
        AssertContains(sourceText, "var canRead = commandChannel.Reader.WaitToReadAsync(cts.Token).AsTask().GetAwaiter().GetResult();");
        AssertContains(sourceText, "if (!canRead)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT channel_closed\");\n                            isScrubbing = false;\n                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"channel_closed\");");
        AssertContains(sourceText, "RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_disposed\");\n                            return;\n                        }");
        AssertContains(sourceText, "if (_disposedFlag != 0)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT\");\n                            isScrubbing = false;\n                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_disposed\");");
        AssertContains(sourceText, "catch (OperationCanceledException)\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_CANCELLED\");");
        AssertContains(sourceText, "catch (Exception ex)\n            {\n                Logger.Log($\"FLASHBACK_PLAYBACK_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'\");\n            }");
        AssertContains(threadLoopText, "finally\n        {\n            CompletePlaybackThreadExit(prebufferedFrames, cts, commandChannel);\n        }");
        AssertContains(threadLifecycleText, "private void CompletePlaybackThreadExit(");
        AssertContains(threadLifecycleText, "ClearPrebufferedFrames(prebufferedFrames, \"thread_exit\");\n        timeEndPeriod(1);");
        AssertDoesNotContain(threadLoopText, "ClearPrebufferedFrames(prebufferedFrames, \"thread_exit\");\n            timeEndPeriod(1);");
        AssertContains(threadLifecycleText, "private bool StopPlaybackThread(TimeSpan timeout, string operation)");
        AssertContains(threadLifecycleText, "var threadExited = true;");
        AssertContains(threadLifecycleText, "if (ReferenceEquals(Thread.CurrentThread, thread))\n                {\n                    Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_JOIN_SKIP reason=self\");\n                    SetLastCommandFailure(\"thread_join_skipped:self\");\n                    threadExited = false;\n                }");
        AssertContains(sourceText, "private static readonly TimeSpan PlaybackThreadStopTimeout = TimeSpan.FromSeconds(3);");
        AssertContains(sourceText, "private static readonly TimeSpan PreviewDetachThreadStopTimeout = TimeSpan.FromSeconds(10);");
        AssertContains(threadLifecycleText, "Logger.Log($\"FLASHBACK_PLAYBACK_THREAD_JOIN_TIMEOUT op={operation} timeout_ms={timeout.TotalMilliseconds:0}\");\n                    SetLastCommandFailure($\"thread_join_timeout:{operation}\");\n                    threadExited = false;");
        AssertContains(threadLifecycleText, "SetLastCommandFailure(\"thread_join_skipped:self\");");
        AssertContains(threadLifecycleText, "SetLastCommandFailure($\"thread_join_timeout:{operation}\");");
        AssertContains(threadLifecycleText, "FLASHBACK_PLAYBACK_STOP_THREAD_COMPLETE op={operation} duration_ms=");
        AssertContains(threadLifecycleText, "thread_was_alive={threadWasAlive} thread_exited={threadExited}");
        AssertContains(threadLifecycleText, "active_at_request={activeKindAtRequest} active_ms_at_request={activeElapsedMsAtRequest:0.###}");
        AssertContains(threadLifecycleText, "if (threadExited)\n            {\n                ApplyDeferredPreviewAttachAfterStopTimeout();\n                DisposePlaybackCtsBestEffort(_playCts, \"stop_thread\");");
        AssertContains(threadLifecycleText, "Interlocked.Exchange(ref _pendingCommands, 0);\n                ClearQueuedCommandSlotsBarrier();\n                Volatile.Write(ref _playbackThreadStarted, 0);");
        AssertContains(sourceText, "Volatile.Write(ref _activeCommandKind, (int)cmd.Kind);");
        AssertContains(sourceText, "Volatile.Write(ref _activeCommandStartedTimestamp, commandStarted);");
        AssertContains(threadCommandDispatchText, "Volatile.Write(ref _activeCommandKind, (int)cmd.Kind);");
        AssertContains(threadCommandDispatchText, "Volatile.Write(ref _activeCommandStartedTimestamp, commandStarted);");
        AssertContains(threadCommandDispatchText, "FLASHBACK_PLAYBACK_CMD_COMPLETE kind={cmd.Kind} duration_ms={commandElapsedMs:0.###}");
        AssertDoesNotContain(threadLoopText, "Volatile.Write(ref _activeCommandKind, (int)cmd.Kind);");
        AssertContains(sourceText, "private static string FormatActiveCommandKind(int rawKind)");
        AssertContains(sourceText, "private double GetActiveCommandElapsedMs(long nowTimestamp)");
        AssertContains(sourceText, "if (cts.IsCancellationRequested)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT cancellation_requested\");");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT cancellation_requested\");\n                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_cancelled\");");
        AssertContains(sourceText, "PaceAndDecodeFrame(decoder, prebufferedFrames, commandChannel, pacingStopwatch, ref frameDuration, ref fileOpen, frozenValidStart, cts.Token)");
        AssertContains(sourceText, "SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.Seek, cts.Token)");
        AssertContains(sourceText, "SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.BeginScrub, cts.Token)");
        AssertContains(sourceText, "SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.UpdateScrub, cts.Token)");
        AssertContains(sourceText, "SeekAndDisplayKeyframe(decoder, ref fileOpen, nudgedPos, frozenValidStart, CommandKind.Nudge, cts.Token)");
        AssertContains(sourceText, "TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, coalescedSeekTarget, \"seek_resume\", cts.Token)");
        AssertContains(sourceText, "TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, endScrubTarget, \"end_scrub\", cts.Token)");
        AssertContains(sourceText, "TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, seekTarget, \"play\", cts.Token)");
        AssertContains(sourceText, "TryDecodeNextVideoFrameWithMetrics(decoder, out var nudgeFrame, cts.Token)");
        AssertContains(sourceText, "CancellationToken cancellationToken)\n    {\n        try\n        {\n            cancellationToken.ThrowIfCancellationRequested();");
        var playbackFramesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var playbackLoopText = playbackFramesText;
        AssertContains(playbackFramesText, "private bool TryReadNextPlaybackFrame(");
        AssertContains(playbackFramesText, "private void ClearPrebufferedFrames(");
        AssertContains(playbackFramesText, "private bool TryResolveAudioDriftFrameSkip(");
        AssertContains(playbackLoopText, "TryResolveAudioDriftFrameSkip(");
        AssertContains(sourceText, "while (skipped < MaxSkipFrames && driftMs < -FrameSkipThresholdMs)\n        {\n            cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(sourceText, "if (commandChannel.Reader.TryPeek(out _))\n            {\n                ReleaseHeldFrameBestEffort(videoFrame, \"av_sync_skip_command_pending\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FRAME_SKIP_COMMAND_PENDING count={skipped}");
        AssertContains(sourceText, "const double FrameSkipThresholdMs = 500.0;");
        // Frame-skip catch-up loop must re-sync the audio clock each iteration so a
        // long catch-up burst does not extrapolate from a stale wall-time anchor.
        AssertContains(sourceText, "private bool TryComputeAudioMasterDriftMs(long videoPtsTicks, out double driftMs)");
        AssertContains(sourceText, "if (!TryComputeAudioMasterDriftMs(videoFrame.Pts.Ticks, out var driftMs) ||\n            driftMs >= -FrameSkipThresholdMs)");
        AssertContains(sourceText, "if (!TryComputeAudioMasterDriftMs(videoFrame.Pts.Ticks, out driftMs))\n            {\n                break;\n            }");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FRAME_SKIP_EOS count={skipped}");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FRAME_SKIP_BUDGET count={skipped}");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FMP4_REOPEN_BEFORE_SEGMENT_SWITCH");
        AssertContains(sourceText, "nextSegmentStart.Value - lastFrameAbsPts > TimeSpan.FromMilliseconds(250)");
        AssertContains(sourceText, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n        {\n            throw;\n        }\n        catch (Exception ex)\n        {\n            SnapToLiveOnError(decoder, ex, ref fileOpen);");
        AssertContains(sourceText, "SafeResumePreviewSubmission(operation);");
        AssertContains(sourceText, "catch (OperationCanceledException)\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_CANCELLED\");\n            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_cancelled\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_FATAL type={ex.GetType().Name} error='{ex.Message}'\");\n            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_fatal\");");
        AssertContains(sourceText, "var decoderToDispose = decoder;\n            decoder = null;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=close");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=dispose");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_COMPLETE was_open={wasOpen}");
        AssertContains(sourceText, "release_ms={releaseMs:0.###} close_ms={closeMs:0.###} dispose_ms={disposeMs:0.###} total_ms={totalMs:0.###}");
        AssertContains(sourceText, "fileOpen = false;\n        _currentOpenFilePath = null;\n        _decoderHwAccel = \"N/A\";");
        AssertContains(threadLifecycleText, "CompleteCommandChannelForThreadExit(commandChannel);\n        DrainAbandonedCommandsOnThreadExit(commandChannel);");
        AssertContains(threadLifecycleText, "private static void CompleteCommandChannelForThreadExit(Channel<PlaybackCommand> commandChannel)");
        AssertContains(threadLifecycleText, "commandChannel.Writer.TryComplete();");
        AssertContains(threadLifecycleText, "FLASHBACK_PLAYBACK_CHANNEL_COMPLETE_WARN");
        AssertContains(threadLifecycleText, "Interlocked.Add(ref _commandsDropped, abandoned);");
        AssertContains(threadLifecycleText, "if (string.IsNullOrEmpty(Volatile.Read(ref _lastCommandFailure)))\n            {\n                SetLastCommandFailure($\"abandoned_on_exit:{abandoned}\");\n            }");
        AssertContains(threadLifecycleText, "Interlocked.Exchange(ref _pendingCommands, 0);");
        AssertDoesNotContain(threadLoopText, "CompleteCommandChannelForThreadExit(commandChannel);\n            DrainAbandonedCommandsOnThreadExit(commandChannel);");
        AssertContains(sourceText, "var ownsPlaybackThread = ReferenceEquals(Thread.CurrentThread, _playbackThread);");
        AssertContains(threadLifecycleText, "var ownsPlaybackThread = ReferenceEquals(Thread.CurrentThread, _playbackThread);");
        AssertContains(sourceText, "var ownsCts = ReferenceEquals(cts, _playCts);");
        AssertContains(threadLifecycleText, "if (ownsPlaybackThread)\n        {\n            _playbackThread = null;\n        }");
        AssertContains(sourceText, "_playbackThread = null;");
        AssertContains(sourceText, "StopPlaybackThread(PlaybackThreadStopTimeout, \"dispose\");\n        _initialized = false;\n        Logger.Log(\"FLASHBACK_PLAYBACK_DISPOSED\");");
        AssertContains(sourceText, "if (_disposedFlag != 0 && command.Kind != CommandKind.Stop)\n        {\n            return RejectCommand(command.Kind, \"disposed\", \"disposed\", false);\n        }");
        AssertContains(threadLifecycleText, "if (ownsCts)\n        {\n            _playCts = null;\n        }\n        DisposePlaybackCtsBestEffort(cts, \"thread_exit\");");
        AssertContains(sourceText, "private static void DisposePlaybackCtsBestEffort(CancellationTokenSource? cts, string operation)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CTS_DISPOSE_WARN");
        AssertContains(threadLifecycleText, "if (ownsPlaybackThread || ownsCts)\n        {\n            Volatile.Write(ref _playbackThreadStarted, 0);\n        }");
        AssertContains(sourceText, "Interlocked.Increment(ref _commandsEnqueued);\n        UpdateMaxPendingCommands(pending);\n        MarkCommandQueued(command.Kind);\n        return true;");

        return Task.CompletedTask;
    }
}
