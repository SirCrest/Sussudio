using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackPlaybackController_SeekSlots_PreserveControlCommandBarriers()
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
