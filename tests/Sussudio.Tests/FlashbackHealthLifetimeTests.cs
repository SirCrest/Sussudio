using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackHealthLifetimeTests
{
    [Fact]
    public void BackendReplacementForwardsNewController_AndRejectsRetiredCallbacks()
    {
        using var fixture = new BackendFixture();
        var first = fixture.CreateController();
        fixture.Install(first);
        fixture.ChangeState(first, "Paused", "user");
        fixture.ChangeState(first, "Live", "decode_error");
        var queuedChange = Assert.Single(fixture.Changes, change => Read<string>(change, "Reason") == "decode_error");
        Assert.True(fixture.IsCurrent(queuedChange));

        // A playback thread may already hold its invocation list while teardown
        // unsubscribes. Exercise that callback after the controller is replaced.
        var retiredCallback = Assert.IsAssignableFrom<Delegate>(ReadField(first, "StateChanged"));
        var second = fixture.CreateController();
        fixture.Install(second);
        Assert.False(fixture.IsCurrent(queuedChange));
        var countBeforeRetiredEvents = fixture.Changes.Count;
        fixture.ChangeState(first, "Paused", "user");
        retiredCallback.DynamicInvoke(fixture.State("Paused"), fixture.State("Live"), "thread_fatal");
        Assert.Equal(countBeforeRetiredEvents, fixture.Changes.Count);

        fixture.ChangeState(second, "Paused", "user");
        fixture.ChangeState(second, "Live", "decode_error");
        Assert.Equal(countBeforeRetiredEvents + 2, fixture.Changes.Count);
        var replacementChange = fixture.Changes[^1];
        Assert.True(fixture.IsCurrent(replacementChange));
        Assert.NotEqual(Read<long>(queuedChange, "BackendGeneration"), Read<long>(replacementChange, "BackendGeneration"));

        Assert.Same(second, Invoke(fixture.Backend, "TakePlaybackController"));
        Assert.False(fixture.IsCurrent(replacementChange));
        fixture.ChangeState(second, "Paused", "user");
        Assert.Equal(countBeforeRetiredEvents + 2, fixture.Changes.Count);
    }

    [Fact]
    public void ClearDetachesPlaybackNotification_AndInvalidatesQueuedChange()
    {
        using var fixture = new BackendFixture();
        var controller = fixture.CreateController();
        fixture.Install(controller);
        fixture.ChangeState(controller, "Paused", "user");
        var queuedChange = Assert.Single(fixture.Changes);
        var callback = Assert.IsAssignableFrom<Delegate>(ReadField(controller, "StateChanged"));

        Invoke(fixture.Backend, "Clear");
        Assert.False(fixture.IsCurrent(queuedChange));
        callback.DynamicInvoke(fixture.State("Paused"), fixture.State("Live"), "decode_error");
        Assert.Single(fixture.Changes);
        Assert.Null(ReadField(controller, "StateChanged"));
    }

    [Fact]
    public void PrewarmWaitsForInitialization_AndBelongsToEachControllerInstance()
    {
        using var fixture = new BackendFixture();
        Invoke(fixture.Backend, "PreWarmPlayback");
        var first = fixture.CreateController();
        fixture.Install(first);
        Invoke(fixture.Backend, "PreWarmPlayback");
        Assert.Null(ReadField(fixture.Backend, "_preWarmedPlaybackController"));
        Assert.False(Read<bool>(first, "PlaybackThreadAlive"));

        // Warm starts only the managed command-waiting thread; decoder creation
        // and media commands stay lazy. No capture or GPU resource is required.
        WriteField(first, "_initialized", true);
        Invoke(fixture.Backend, "PreWarmPlayback");
        Assert.True(SpinWait.SpinUntil(() => Read<bool>(first, "PlaybackThreadAlive"), TimeSpan.FromSeconds(2)));
        var firstThread = ReadField(first, "_playbackThread");
        Assert.Same(first, ReadField(fixture.Backend, "_preWarmedPlaybackController"));
        for (var attempt = 0; attempt < 5; attempt++)
        {
            Invoke(fixture.Backend, "PreWarmPlayback");
        }
        Assert.Same(firstThread, ReadField(first, "_playbackThread"));
        Assert.Equal(0L, Read<long>(first, "CommandsEnqueued"));

        var second = fixture.CreateController();
        fixture.Install(second);
        Assert.Null(ReadField(fixture.Backend, "_preWarmedPlaybackController"));
        Invoke(fixture.Backend, "PreWarmPlayback");
        Assert.Null(ReadField(fixture.Backend, "_preWarmedPlaybackController"));
        WriteField(second, "_initialized", true);
        Invoke(fixture.Backend, "PreWarmPlayback");
        Assert.Same(second, ReadField(fixture.Backend, "_preWarmedPlaybackController"));
        Assert.True(SpinWait.SpinUntil(() => Read<bool>(second, "PlaybackThreadAlive"), TimeSpan.FromSeconds(2)));
        Assert.NotSame(firstThread, ReadField(second, "_playbackThread"));
        Assert.Equal(0L, Read<long>(second, "CommandsEnqueued"));

        var disposed = fixture.CreateController();
        ((IDisposable)disposed).Dispose();
        fixture.Install(disposed);
        Invoke(fixture.Backend, "PreWarmPlayback");
        Assert.Null(ReadField(fixture.Backend, "_preWarmedPlaybackController"));
    }

    [Fact]
    public void HiddenTimelineHealthTracksBackendFailureRecoveryAndDisable()
    {
        using var fixture = new BackendFixture();
        var service = Uninitialized("Sussudio.Services.Capture.CaptureService");
        WriteField(service, "_flashbackBackend", fixture.Backend);
        var coordinator = Uninitialized("Sussudio.Services.Capture.CaptureSessionCoordinator");
        WriteField(coordinator, "_captureService", service);
        var viewModel = Uninitialized("Sussudio.ViewModels.MainViewModel");
        WriteField(viewModel, "_sessionCoordinator", coordinator);
        Write(viewModel, "IsFlashbackEnabled", true);
        Write(viewModel, "IsFlashbackTimelineVisible", false);
        Write(viewModel, "FlashbackHealthMessage", "");

        Invoke(viewModel, "UpdateFlashbackHealthStatus");
        Assert.Equal("Flashback is not running — use Restart Flashback.", Read<string>(viewModel, "FlashbackHealthMessage"));
        Write(fixture.Backend, "Sink", Uninitialized("Sussudio.Services.Flashback.FlashbackEncoderSink"));
        Invoke(viewModel, "UpdateFlashbackHealthStatus");
        Assert.Equal("", Read<string>(viewModel, "FlashbackHealthMessage"));

        Write(viewModel, "FlashbackHealthMessage", "Returned to live — playback error.");
        Invoke(viewModel, "UpdateFlashbackHealthStatus");
        Assert.Equal("Returned to live — playback error.", Read<string>(viewModel, "FlashbackHealthMessage"));
        Write(fixture.Backend, "Sink", null);
        Invoke(viewModel, "UpdateFlashbackHealthStatus");
        Assert.Equal("Flashback is not running — use Restart Flashback.", Read<string>(viewModel, "FlashbackHealthMessage"));
        Write(viewModel, "IsFlashbackEnabled", false);
        Invoke(viewModel, "UpdateFlashbackHealthStatus");
        Assert.Equal("", Read<string>(viewModel, "FlashbackHealthMessage"));
        Assert.False(Read<bool>(viewModel, "IsFlashbackTimelineVisible"));

        WriteField(viewModel, "_disposeState", 1);
        Write(viewModel, "IsFlashbackEnabled", true);
        Invoke(viewModel, "UpdateFlashbackHealthStatus");
        Assert.Equal("", Read<string>(viewModel, "FlashbackHealthMessage"));
    }

    private static Type TypeOf(string name) => SussudioAssembly.Load().GetType(name, throwOnError: true)!;
    private static object? Invoke(object instance, string name, params object?[] arguments)
        => instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(instance, arguments);
    private static T Read<T>(object instance, string name)
        => (T)instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(instance)!;
    private static void Write(object instance, string name, object? value)
        => instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(instance, value);
    private static object? ReadField(object instance, string name)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance);
    private static void WriteField(object instance, string name, object? value)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);
    private static object Uninitialized(string name)
    {
        var value = RuntimeHelpers.GetUninitializedObject(TypeOf(name));
        GC.SuppressFinalize(value);
        return value;
    }

    private sealed class BackendFixture : IDisposable
    {
        private readonly object _bufferManager;
        private readonly List<object> _controllers = [];

        public BackendFixture()
        {
            Backend = Activator.CreateInstance(TypeOf("Sussudio.Services.Flashback.FlashbackBackendResources"))!;
            _bufferManager = Activator.CreateInstance(TypeOf("Sussudio.Services.Flashback.FlashbackBufferManager"), new object?[] { null })!;
            var stateChanged = Backend.GetType().GetEvent("PlaybackStateChanged")!;
            var change = Expression.Parameter(stateChanged.EventHandlerType!.GetMethod("Invoke")!.GetParameters()[0].ParameterType, "change");
            var handler = Expression.Lambda(stateChanged.EventHandlerType,
                Expression.Invoke(Expression.Constant(new Action<object>(Changes.Add)), Expression.Convert(change, typeof(object))), change).Compile();
            stateChanged.AddEventHandler(Backend, handler);
        }

        public object Backend { get; }
        public List<object> Changes { get; } = [];
        public object CreateController()
        {
            var controller = Activator.CreateInstance(TypeOf("Sussudio.Services.Flashback.FlashbackPlaybackController"), _bufferManager)!;
            _controllers.Add(controller);
            return controller;
        }
        public void Install(object controller) => Invoke(Backend, "Install", _bufferManager, null, null, controller, null);
        public bool IsCurrent(object change) => (bool)Invoke(Backend, "IsCurrentPlaybackStateChange", change)!;
        public object State(string name) => Enum.Parse(TypeOf("Sussudio.Models.FlashbackPlaybackState"), name);
        public void ChangeState(object controller, string state, string reason) => Invoke(controller, "SetState", State(state), reason);
        public void Dispose()
        {
            Invoke(Backend, "Clear");
            foreach (var controller in _controllers)
            {
                ((IDisposable)controller).Dispose();
            }
            ((IDisposable)_bufferManager).Dispose();
        }
    }
}
