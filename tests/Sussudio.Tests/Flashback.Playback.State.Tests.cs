using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackPlaybackController_InitialState_IsLive()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");

        var controller = ctor.Invoke(new[] { bufferManager });

        // State should be Live before Initialize
        var stateStr = GetPropertyValue(controller, "State")?.ToString();
        AssertEqual("Live", stateStr, "Initial state is Live");

        // PlaybackPosition should be zero
        var position = (TimeSpan)GetPropertyValue(controller, "PlaybackPosition")!;
        AssertEqual(TimeSpan.Zero, position, "Initial PlaybackPosition");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_CommandsNoOpBeforeInitialize()
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

        // These should all no-op without throwing (IsReady is false)
        var playMethod = controllerType.GetMethod("Play", BindingFlags.Public | BindingFlags.Instance);
        var pauseMethod = controllerType.GetMethod("Pause", BindingFlags.Public | BindingFlags.Instance);
        var goLiveMethod = controllerType.GetMethod("GoLive", BindingFlags.Public | BindingFlags.Instance);

        playMethod?.Invoke(controller, null);
        pauseMethod?.Invoke(controller, null);
        goLiveMethod?.Invoke(controller, null);

        // State should still be Live (commands were no-ops)
        var stateStr = GetPropertyValue(controller, "State")?.ToString();
        AssertEqual("Live", stateStr, "State unchanged after no-op commands");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_SuccessfulNoOps_ClearStaleCommandFailure()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");
        var controller = ctor.Invoke(new[] { bufferManager });
        SetPrivateField(controller, "_initialized", true);

        try
        {
            SeedCommandFailure(controller, "old_failure:EndScrub");
            AssertEqual(false, (bool)controllerType.GetMethod("EndScrub")!.Invoke(controller, null)!, "EndScrub live/no-thread no-op reports failure");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "EndScrub no-op clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "EndScrub no-op clears stale failure UTC");

            SeedCommandFailure(controller, "old_failure:GoLive");
            AssertEqual(false, (bool)controllerType.GetMethod("GoLive")!.Invoke(controller, null)!, "GoLive live/no-thread no-op reports failure");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "GoLive no-op clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "GoLive no-op clears stale failure UTC");

            SeedCommandFailure(controller, "old_failure:Nudge");
            AssertEqual(false, (bool)controllerType.GetMethod("NudgePosition")!.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(8.33) })!, "Nudge live/no-thread no-op reports failure");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "Nudge no-op clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "Nudge no-op clears stale failure UTC");
        }
        finally
        {
            (controller as IDisposable)?.Dispose();
            (bufferManager as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_CoalescedCommands_ClearStaleCommandFailure()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");
        var controller = ctor.Invoke(new[] { bufferManager });
        var sendSeek = controllerType.GetMethod("SendSeekCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendSeekCommand not found.");
        var sendUpdateScrub = controllerType.GetMethod("SendUpdateScrubCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendUpdateScrubCommand not found.");

        try
        {
            AssertEqual(true, (bool)sendSeek.Invoke(controller, new object[] { TimeSpan.FromSeconds(1) })!, "Initial seek enqueues");
            var initialSeekQueuedUtc = GetLongProperty(controller, "LastCommandQueuedUtcUnixMs");
            SeedCommandFailure(controller, "old_failure:Seek");
            AssertEqual(true, (bool)sendSeek.Invoke(controller, new object[] { TimeSpan.FromSeconds(2) })!, "Coalesced seek succeeds");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "Coalesced seek clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "Coalesced seek clears stale failure UTC");
            AssertEqual("Seek", GetStringProperty(controller, "LastCommandQueued"), "Coalesced seek keeps physical queued-command name");
            AssertEqual(initialSeekQueuedUtc, GetLongProperty(controller, "LastCommandQueuedUtcUnixMs"), "Coalesced seek does not refresh queued-command timestamp");
            AssertEqual(1L, GetLongProperty(controller, "SeekCommandsCoalesced"), "Coalesced seek counter");

            AssertEqual(true, (bool)sendUpdateScrub.Invoke(controller, new object[] { TimeSpan.FromSeconds(3) })!, "Initial scrub update enqueues");
            var initialScrubQueuedUtc = GetLongProperty(controller, "LastCommandQueuedUtcUnixMs");
            SeedCommandFailure(controller, "old_failure:UpdateScrub");
            AssertEqual(true, (bool)sendUpdateScrub.Invoke(controller, new object[] { TimeSpan.FromSeconds(4) })!, "Coalesced scrub update succeeds");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "Coalesced scrub update clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "Coalesced scrub update clears stale failure UTC");
            AssertEqual("UpdateScrub", GetStringProperty(controller, "LastCommandQueued"), "Coalesced scrub update keeps physical queued-command name");
            AssertEqual(initialScrubQueuedUtc, GetLongProperty(controller, "LastCommandQueuedUtcUnixMs"), "Coalesced scrub update does not refresh queued-command timestamp");
            AssertEqual(1L, GetLongProperty(controller, "ScrubUpdatesCoalesced"), "Coalesced scrub update counter");
        }
        finally
        {
            (controller as IDisposable)?.Dispose();
            (bufferManager as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }
}
