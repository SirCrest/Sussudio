using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class WasapiPlaybackTransitionFailureTests
{
    private const int FailureHr = unchecked((int)0x80004005);
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Theory]
    [InlineData("Stop", false)]
    [InlineData("Reset", false)]
    [InlineData("Start", false)]
    [InlineData("Stop", true)]
    [InlineData("Reset", true)]
    [InlineData("Start", true)]
    public async Task FailedTransitionRetainsCauseAfterWorkerTermination(string operation, bool managedFailure)
    {
        using var fixture = new PlaybackFixture(operation, managedFailure);
        fixture.PrepareTransition();

        var error = Record.Exception(() =>
        {
            fixture.RequestTransition();
            fixture.WaitForTransition(1000);
        });

        if (managedFailure)
        {
            Assert.Same(fixture.Client.ManagedFailure, error);
        }
        else
        {
            var nativeError = Assert.IsType<COMException>(error);
            Assert.Equal(FailureHr, nativeError.HResult);
            Assert.Equal($"IAudioClient.{operation}({(operation == "Start" ? "resume" : "pause")}) failed with HRESULT 0x80004005.", nativeError.Message);
        }
        Assert.Contains("RenderThreadMain", error!.StackTrace, StringComparison.Ordinal);
        await fixture.WorkerExited.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.Equal(0, fixture.Read<int>("_started"));
        Assert.Equal(operation == "Start" ? 1 : 0, fixture.Read<int>("_renderingPaused"));
        Assert.Equal(1, fixture.Read<int>("_resourcesReleased"));
        if (operation == "Stop") Assert.Equal(0, fixture.Client.ResetCalls);

        // The failed renderer has disposed its events. Its original failure must
        // still win over both a stopped-worker shortcut and event disposal.
        Assert.Same(error, Record.Exception(() => fixture.WaitPaused(0)));
        Assert.Same(error, Record.Exception(() => fixture.WaitRunning(0)));
        Assert.Same(error, Record.Exception(fixture.Pause));
        Assert.Same(error, Record.Exception(fixture.Resume));
    }

    [Theory]
    [InlineData("Stop")]
    [InlineData("Reset")]
    [InlineData("Start")]
    public async Task PendingTransitionTimesOutAndBlockedWaiterReceivesTheLaterFailure(string operation)
    {
        using var fixture = new PlaybackFixture(operation, holdFailure: true);
        fixture.PrepareTransition();
        fixture.RequestTransition();
        Assert.True(fixture.Client.FailureEntered.Wait(TimeSpan.FromSeconds(1)));

        var elapsed = Stopwatch.StartNew();
        Assert.False(fixture.WaitForTransition(30));
        Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(1));

        Exception? error = null;
        var waiter = new Thread(() => error = Record.Exception(() => fixture.WaitForTransition(1000)))
        {
            IsBackground = true
        };
        waiter.Start();
        try
        {
            Assert.True(SpinWait.SpinUntil(
                () => (waiter.ThreadState & System.Threading.ThreadState.WaitSleepJoin) != 0,
                TimeSpan.FromMilliseconds(500)));
        }
        finally
        {
            fixture.Client.ReleaseFailure.Set();
        }

        Assert.True(waiter.Join(TimeSpan.FromSeconds(2)));
        Assert.Equal(FailureHr, Assert.IsType<COMException>(error).HResult);
        await fixture.WorkerExited.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.Same(error, Record.Exception(() => fixture.WaitForTransition(0)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void SuccessfulTransitionsAndNormalStopKeepTheirExistingContract(int successHr)
    {
        using var fixture = new PlaybackFixture(successHr: successHr);
        fixture.Pause();
        Assert.True(fixture.WaitPaused(1000));
        fixture.Resume();
        Assert.True(fixture.WaitRunning(1000));
        Assert.Equal(1, fixture.Client.StopCalls);
        Assert.Equal(1, fixture.Client.ResetCalls);
        Assert.Equal(2, fixture.Client.StartCalls);
        fixture.Stop();
        Assert.True(fixture.WaitPaused(0));
        Assert.True(fixture.WaitRunning(0));
    }

    [Fact]
    public void NeverStartedPlaybackKeepsItsExistingTransitionContract()
    {
        using var fixture = new PlaybackFixture(start: false);
        fixture.Pause();
        fixture.Resume();
        Assert.True(fixture.WaitPaused(-1));
        Assert.True(fixture.WaitRunning(-1));
        Assert.Equal(0, fixture.Client.StartCalls);
    }

    [Fact]
    public void InitialStartFailureStillThrowsSynchronouslyAndRollsBackWithoutAWorker()
    {
        using var fixture = new PlaybackFixture("InitialStart", start: false);
        var error = Assert.Throws<COMException>(fixture.Start);
        Assert.Equal(FailureHr, error.HResult);
        Assert.Contains("IAudioClient.Start(render)", error.Message, StringComparison.Ordinal);
        Assert.Equal(0, fixture.Read<int>("_started"));
        Assert.Null(fixture.Read<object?>("_renderThread"));
    }

    private sealed class PlaybackFixture : IDisposable
    {
        private readonly object _playback;
        private readonly string? _failureOperation;

        internal PlaybackFixture(string? operation = null, bool managedFailure = false,
            bool holdFailure = false, int successHr = 0, bool start = true)
        {
            var assembly = SussudioAssembly.Load();
            var playbackType = assembly.GetType("Sussudio.Services.Audio.WasapiAudioPlayback", throwOnError: true)!;
            var clientType = assembly.GetType("Sussudio.Services.Audio.IAudioClient", throwOnError: true)!;
            _failureOperation = operation;
            Client = new AudioClientBehavior(operation, managedFailure, holdFailure, successHr);
            var proxy = DispatchProxy.Create(clientType, typeof(AudioClientProxy));
            ((AudioClientProxy)proxy).Call = Client.Call;
            _playback = Activator.CreateInstance(playbackType, nonPublic: true)!;
            Set("_audioClient", proxy);
            // The worker owns this ordinary event; no endpoint activation or
            // audio-client Initialize/GetService call occurs in this fixture.
            Set("_renderEvent", new AutoResetEvent(false));
            Set("_initialized", 1);
            if (start)
            {
                try { Start(); }
                catch { Dispose(); throw; }
            }
        }

        internal AudioClientBehavior Client { get; }
        internal Task WorkerExited => Read<TaskCompletionSource<bool>>("_workerExited").Task;
        internal T Read<T>(string field) => (T)_playback.GetType().GetField(field, InstanceFlags)!.GetValue(_playback)!;
        private void Set(string field, object value) => _playback.GetType().GetField(field, InstanceFlags)!.SetValue(_playback, value);
        internal void Start() => Invoke("Start");
        internal void Stop() => Invoke("Stop");
        internal void Pause() => Invoke("PauseRendering");
        internal void Resume() => Invoke("ResumeRendering", 0d, 0);
        internal bool WaitPaused(int timeoutMs) => (bool)Invoke("WaitForRenderingPaused", timeoutMs)!;
        internal bool WaitRunning(int timeoutMs) => (bool)Invoke("WaitForRenderingRunning", timeoutMs)!;
        internal void RequestTransition() { if (_failureOperation == "Start") Resume(); else Pause(); }
        internal bool WaitForTransition(int timeoutMs) => _failureOperation == "Start" ? WaitRunning(timeoutMs) : WaitPaused(timeoutMs);
        internal void PrepareTransition()
        {
            if (_failureOperation != "Start") return;
            Pause();
            Assert.True(WaitPaused(1000));
        }

        private object? Invoke(string method, params object[] arguments)
        {
            try { return _playback.GetType().GetMethod(method, InstanceFlags)!.Invoke(_playback, arguments); }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        public void Dispose()
        {
            Client.ReleaseFailure.Set();
            ((IDisposable)_playback).Dispose();
            Client.Dispose();
        }
    }

    public class AudioClientProxy : DispatchProxy
    {
        public Func<string, int>? Call { get; set; }
        protected override object Invoke(MethodInfo? targetMethod, object?[]? args)
            => Call!(targetMethod!.Name);
    }

    private sealed class AudioClientBehavior(string? failureOperation, bool managedFailure,
        bool holdFailure, int successHr) : IDisposable
    {
        internal readonly Exception ManagedFailure = new InvalidOperationException("Synthetic audio-client failure.");
        internal readonly ManualResetEventSlim FailureEntered = new(false);
        internal readonly ManualResetEventSlim ReleaseFailure = new(false);
        internal int StartCalls;
        internal int StopCalls;
        internal int ResetCalls;

        internal int Call(string method)
        {
            string operation;
            switch (method)
            {
                case "Start":
                    operation = Interlocked.Increment(ref StartCalls) == 1 ? "InitialStart" : "Start";
                    break;
                case "Stop":
                    Interlocked.Increment(ref StopCalls);
                    operation = "Stop";
                    break;
                case "Reset":
                    Interlocked.Increment(ref ResetCalls);
                    operation = "Reset";
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected native call: {method}");
            }
            if (operation != failureOperation || FailureEntered.IsSet) return successHr;
            FailureEntered.Set();
            if (holdFailure && !ReleaseFailure.Wait(TimeSpan.FromSeconds(3)))
                throw new TimeoutException("Synthetic transition failure gate was not released.");
            if (managedFailure) throw ManagedFailure;
            return FailureHr;
        }

        public void Dispose()
        {
            FailureEntered.Dispose();
            ReleaseFailure.Dispose();
        }
    }
}
