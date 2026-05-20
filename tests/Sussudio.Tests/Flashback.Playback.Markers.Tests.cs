using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public class FlashbackPlaybackMarkersTests
{
    [Fact]
    public void FlashbackPlaybackController_InOutPoints_DefaultToUnset()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)!;
        var controller = ctor.Invoke(new[] { bufferManager });

        // InPoint and OutPoint properties
        var inPointProp = controllerType.GetProperty("InPoint", BindingFlags.Public | BindingFlags.Instance);
        var outPointProp = controllerType.GetProperty("OutPoint", BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(inPointProp);
        Assert.NotNull(outPointProp);
        foreach (var propertyName in new[]
                 {
                     "CommandsEnqueued",
                     "CommandsProcessed",
                     "CommandsDropped",
                     "CommandsSkippedNotReady",
                     "ScrubUpdatesCoalesced",
                     "PendingCommands",
                     "MaxPendingCommands",
                     "LastCommandQueueLatencyMs",
                     "MaxCommandQueueLatencyMs",
                     "LastCommandQueued",
                     "LastCommandProcessed",
                     "LastCommandQueuedUtcUnixMs",
                     "LastCommandProcessedUtcUnixMs",
                     "LastCommandFailureUtcUnixMs",
                     "LastCommandFailure",
                     "PlaybackThreadAlive"
                 })
        {
            Assert.NotNull(controllerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance));
        }

        // ClearInOutPoints should not throw on a fresh controller
        var clearMethod = controllerType.GetMethod("ClearInOutPoints", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(clearMethod);
        clearMethod!.Invoke(controller, null);

        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        AssertContains(
            sourceText,
            "var pending = Interlocked.Increment(ref _pendingCommands);\n        var droppedOldest = false;\n        var droppedCommand = default(PlaybackCommand);\n        if (!_commandChannel.Writer.TryWrite(queuedCommand) &&\n            (!IsCommandChannelOpenForDropRetry() ||\n             !TryDropOldestQueuedCommandForNewCommand(out droppedCommand) ||\n             !(droppedOldest = _commandChannel.Writer.TryWrite(queuedCommand))))\n        {\n            DecrementPendingCommands();");
        AssertContains(sourceText, "if (droppedOldest)\n        {\n            TrackDroppedQueuedCommand(droppedCommand, queuedCommand.Kind);\n        }");
        AssertContains(sourceText, "UpdateMaxPendingCommands(pending);");
        AssertContains(sourceText, "private bool IsCommandChannelOpenForDropRetry()");
        AssertContains(sourceText, "private bool TryDropOldestQueuedCommandForNewCommand(out PlaybackCommand droppedCommand)");
        AssertContains(sourceText, "private void TrackDroppedQueuedCommand(PlaybackCommand droppedCommand, CommandKind newCommandKind)");
    }

    [Fact]
    public void FlashbackPlaybackController_InOutPoints_ClearInvalidCounterpart()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();

        AssertContains(sourceText, "var outTicks = Interlocked.Read(ref _outPointTicks);\n        if (outTicks != long.MinValue && outTicks <= pos.Ticks)\n        {\n            OutPoint = null;\n            Logger.Log(\"FLASHBACK_PLAYBACK_CLEAR_OUT invalid_range\");\n        }");
        AssertContains(sourceText, "var inTicks = Interlocked.Read(ref _inPointTicks);\n        if (inTicks != long.MinValue && inTicks >= pos.Ticks)\n        {\n            InPoint = null;\n            Logger.Log(\"FLASHBACK_PLAYBACK_CLEAR_IN invalid_range\");\n        }");
        // SetInPointAt/SetOutPointAt accept an explicit user-intended position so
        // mid-GOP scrub clicks don't snap markers to the prior keyframe. Both
        // paths still default to PlaybackPosition when called without an override.
        AssertContains(sourceText, "var pos = overridePosition.HasValue\n            ? NormalizeMarkerPosition(overridePosition.Value)\n            : PlaybackPosition;\n        ClearLastCommandFailure();\n        InPoint = pos;");
        AssertContains(sourceText, "var pos = overridePosition.HasValue\n            ? NormalizeMarkerPosition(overridePosition.Value)\n            : PlaybackPosition;\n        ClearLastCommandFailure();\n        OutPoint = pos;");
        AssertContains(sourceText, "public TimeSpan SetInPoint() => SetInPointAt(null);");
        AssertContains(sourceText, "public TimeSpan SetInPointAt(TimeSpan position) => SetInPointAt((TimeSpan?)position);");
        AssertContains(sourceText, "public TimeSpan SetOutPoint() => SetOutPointAt(null);");
        AssertContains(sourceText, "public TimeSpan SetOutPointAt(TimeSpan position) => SetOutPointAt((TimeSpan?)position);");
        AssertContains(sourceText, "InPoint = null;\n        OutPoint = null;\n        ClearLastCommandFailure();");

        // UI must call the explicit-position overload so the marker matches the
        // visual playhead, not the controller's keyframe-snapped PlaybackPosition.
        var mainWindowFlashback = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackCommandController.cs")
            .Replace("\r\n", "\n");
        AssertContains(mainWindowFlashback, "_context.ViewModel.FlashbackSetInPointAt(_context.ViewModel.FlashbackPlaybackPosition)");
        AssertContains(mainWindowFlashback, "_context.ViewModel.FlashbackSetOutPointAt(_context.ViewModel.FlashbackPlaybackPosition)");
    }

    [Fact]
    public void FlashbackPlaybackController_InOutPointSettersNormalizeMarkers()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var markersText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Markers.cs");
        var markersStateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.MarkersState.cs");

        AssertContains(markersStateText, "private long _inPointFilePtsTicks = long.MinValue;");
        AssertContains(markersStateText, "private long _outPointFilePtsTicks = long.MinValue;");
        AssertContains(markersStateText, "Interlocked.Exchange(ref _inPointTicks, normalized?.Ticks ?? long.MinValue);\n            Interlocked.Exchange(ref _inPointFilePtsTicks, normalized.HasValue ? SaturatingAdd(normalized.Value, _bufferManager.ValidStartPts).Ticks : long.MinValue);");
        AssertContains(markersStateText, "Interlocked.Exchange(ref _outPointTicks, normalized?.Ticks ?? long.MinValue);\n            Interlocked.Exchange(ref _outPointFilePtsTicks, normalized.HasValue ? SaturatingAdd(normalized.Value, _bufferManager.ValidStartPts).Ticks : long.MinValue);");
        AssertContains(markersStateText, "public TimeSpan? InPointFilePts");
        AssertContains(markersStateText, "public TimeSpan? OutPointFilePts");
        AssertContains(markersStateText, "public void RestoreInOutPoints(\n        TimeSpan? inPoint,\n        TimeSpan? outPoint,\n        TimeSpan? inPointFilePts,\n        TimeSpan? outPointFilePts)");
        AssertContains(markersStateText, "Interlocked.Exchange(ref _inPointFilePtsTicks, inPointFilePts.Value.Ticks);");
        AssertContains(markersStateText, "Interlocked.Exchange(ref _outPointFilePtsTicks, outPointFilePts.Value.Ticks);");
        AssertDoesNotContain(markersText, "public void RestoreInOutPoints(");
        AssertContains(sourceText, "private TimeSpan NormalizeMarkerPosition(TimeSpan position)\n    {\n        if (position <= TimeSpan.Zero)\n        {\n            return TimeSpan.Zero;\n        }\n\n        var bufferDuration = _bufferManager.BufferedDuration;\n        return position > bufferDuration ? bufferDuration : position;\n    }");
    }

    [Fact]
    public void FlashbackPlaybackController_InOutPointChangesStopAfterDispose()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        using var controller = (IDisposable)Activator.CreateInstance(controllerType, new[] { bufferManager })!;

        var setInPoint = controllerType.GetMethod("SetInPoint", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackPlaybackController.SetInPoint not found.");
        var setOutPoint = controllerType.GetMethod("SetOutPoint", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackPlaybackController.SetOutPoint not found.");
        var clearInOut = controllerType.GetMethod("ClearInOutPoints", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackPlaybackController.ClearInOutPoints not found.");

        setInPoint.Invoke(controller, null);
        controller.Dispose();
        clearInOut.Invoke(controller, null);
        setOutPoint.Invoke(controller, null);

        Assert.Equal(TimeSpan.Zero, (TimeSpan?)GetPropertyValue(controller, "InPoint"));
        Assert.Null(GetPropertyValue(controller, "OutPoint"));

        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SET_IN_SKIP reason=disposed");
        AssertContains(sourceText, "SetLastCommandFailure(\"disposed:SetInPoint\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SET_OUT_SKIP reason=disposed");
        AssertContains(sourceText, "SetLastCommandFailure(\"disposed:SetOutPoint\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CLEAR_INOUT_SKIP reason=disposed");
        AssertContains(sourceText, "SetLastCommandFailure(\"disposed:ClearInOutPoints\");");
    }

    [Fact]
    public void FlashbackPlaybackController_ClampPosition_BoundsMarkersToBufferedDuration()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();

        AssertContains(sourceText, "var bufferDuration = _bufferManager.BufferedDuration;\n        var inTicks = Interlocked.Read(ref _inPointTicks);");
        AssertContains(sourceText, "var max = outTicks == long.MinValue ? bufferDuration : TimeSpan.FromTicks(outTicks);\n        if (max > bufferDuration) max = bufferDuration;");
        // Eviction-aware scrub clamp: ClampPosition(position, frozenValidStart) must
        // promote min to currentValidStart - frozenValidStart so a scrub-frozen
        // position 0 doesn't resolve to an evicted file PTS and snap-to-live.
        AssertContains(sourceText, "private TimeSpan ClampPosition(TimeSpan position) => ClampPosition(position, null);");
        AssertContains(sourceText, "private TimeSpan ClampPosition(TimeSpan position, TimeSpan? frozenValidStart)");
        AssertContains(sourceText, "var currentValidStart = _bufferManager.ValidStartPts;");
        AssertContains(sourceText, "var evictedDelta = currentValidStart - frozenValidStart.Value;");
    }

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object? GetPropertyValue(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(instance);

    private static string ReadFlashbackPlaybackControllerPlaybackSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderFiles.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderReopen.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderAdjacentSegmentSeek.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderSegmentReopen.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Lifecycle.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PreviewDetachLifecycle.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.MarkersState.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Markers.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PositionMapping.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Metrics.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.MetricsCollection.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackDecodeMetricsCollection.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PreviewFrames.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrameOwnership.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackLiveRecovery.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.SeekDisplay.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackLoop.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackSegmentEdges.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackTiming.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackSoftwareBudget.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterClock.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterPacing.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterFallbacks.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandModels.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Commands.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandCoalescing.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandCoalescingSlots.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandControlYieldPolicy.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandTelemetry.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandFailures.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadScheduling.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadLoop.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadLifecycle.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadChannel.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadSeekCommands.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadSeekScrubCommands.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadEndScrubCommand.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadPlayCommand.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCleanup.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.AudioCallback.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.AudioPreviewGuards.cs"),
            ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.AudioPrebuffer.cs")
        };

        return string.Join("\n", parts);
    }

    private static string ReadRepoFile(string relativePath)
        => RuntimeContractSource.ReadRepoFile(relativePath).Replace("\r\n", "\n");

    private static void AssertContains(string actual, string expectedSubstring)
        => Assert.Contains(expectedSubstring, actual, StringComparison.Ordinal);

    private static void AssertDoesNotContain(string actual, string unexpectedSubstring)
        => Assert.DoesNotContain(unexpectedSubstring, actual, StringComparison.Ordinal);
}
