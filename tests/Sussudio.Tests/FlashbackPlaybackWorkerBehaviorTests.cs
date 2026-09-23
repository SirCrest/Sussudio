using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackPlaybackWorkerBehaviorTests
{
    [Theory]
    [InlineData("Play", 1250, 1250)]
    [InlineData("Pause", 0, 1250)]
    [InlineData("Seek", 0, 1250)]
    [InlineData("BeginScrub", 0, 1250)]
    [InlineData("UpdateScrub", 0, 1250)]
    [InlineData("Nudge", 0, 0)]
    public void MissingFileRestoresLiveAndRetainsTheClosedDecoder(
        string commandKind, int failurePositionMs, int finalPositionMs)
    {
        using var session = new WorkerSession(commandKind);
        using var cancellation = new CancellationTokenSource();

        Assert.True(session.Execute(commandKind, cancellation));

        Assert.Equal($"no_file:{commandKind} pos_ms={failurePositionMs}",
            Read<string>(session.Controller, "LastCommandFailure"));
        Assert.True(Read<long>(session.Controller, "LastCommandFailureUtcUnixMs") > 0);
        Assert.Equal("Live", Read<object>(session.Controller, "State").ToString());
        Assert.Equal(TimeSpan.FromMilliseconds(finalPositionMs), Read<TimeSpan>(session.Controller, "PlaybackPosition"));
        Assert.False((bool)Field(session.Worker, "IsPlaying")!);
        Assert.False((bool)Field(session.Worker, "IsScrubbing")!);
        Assert.False((bool)Field(session.Worker, "FileOpen")!);
        Assert.Null(Field(session.Worker, "PendingExactResumeTarget"));
        Assert.Equal(0L, Field(session.Controller, "_lastAudioPtsTicks"));
        Assert.Equal(0L, Field(session.Controller, "_lastVideoPtsTicks"));
        Assert.Null(Field(session.Controller, "_currentOpenFilePath"));
        Assert.Same(session.Decoder, Field(session.Worker, "Decoder"));
        Assert.False(Read<bool>(session.Decoder, "IsOpen"));
        Assert.False((bool)Field(session.Decoder, "_disposed")!);
        session.AssertFramesCleared();
        session.AssertCommandCompleted();
    }

    [Theory]
    [InlineData("Pause")]
    [InlineData("Seek")]
    [InlineData("BeginScrub")]
    [InlineData("UpdateScrub")]
    [InlineData("Nudge")]
    public void CancellationBeforeNoFileRecoveryKeepsTheTokenAndUnwindsCommandTelemetry(string commandKind)
    {
        using var session = new WorkerSession(commandKind);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var error = Assert.Throws<OperationCanceledException>(() => session.Execute(commandKind, cancellation));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(string.Empty, Read<string>(session.Controller, "LastCommandFailure"));
        Assert.Same(session.Decoder, Field(session.Worker, "Decoder"));
        Assert.False((bool)Field(session.Decoder, "_disposed")!);
        session.AssertCommandCompleted();
        // PlaybackThreadEntry owns this unwind after the command throws.
        Invoke(session.Controller, "RestoreLiveForPlaybackThreadExit", session.Worker, "thread_cancelled");
        Assert.Null(Field(session.Worker, "Decoder"));
        Assert.True((bool)Field(session.Decoder, "_disposed")!);
        Assert.Equal("Live", Read<object>(session.Controller, "State").ToString());
        session.AssertFramesCleared();
    }

    [Theory]
    [InlineData("GoLive", true)]
    [InlineData("Stop", false)]
    public void LiveAndStopDisposeTheWorkerDecoderAndPreserveTheDispatchLoopDecision(string commandKind, bool keepRunning)
    {
        using var session = new WorkerSession(commandKind);
        using var cancellation = new CancellationTokenSource();
        SetField(session.Worker, "IsPlaying", true);
        SetField(session.Worker, "IsScrubbing", true);

        Assert.Equal(keepRunning, session.Execute(commandKind, cancellation));

        Assert.Null(Field(session.Worker, "Decoder"));
        Assert.True((bool)Field(session.Decoder, "_disposed")!);
        Assert.False((bool)Field(session.Worker, "IsPlaying")!);
        Assert.False((bool)Field(session.Worker, "IsScrubbing")!);
        Assert.Null(Field(session.Worker, "PendingExactResumeTarget"));
        Assert.Equal("Live", Read<object>(session.Controller, "State").ToString());
        session.AssertFramesCleared();
        session.AssertCommandCompleted();
    }

    [Theory]
    [InlineData("Play")]
    [InlineData("Pause")]
    [InlineData("UpdateScrub")]
    [InlineData("EndScrub")]
    public void IgnoredCommandsPreserveRetainedFramesAndWorkerState(string commandKind)
    {
        using var session = new WorkerSession(commandKind);
        using var cancellation = new CancellationTokenSource();
        var playing = commandKind == "Play";
        session.PrepareRetainedState(playing);
        var frame = session.PeekFrame();
        var pacingTicks = session.PacingStopwatch.ElapsedTicks;

        Assert.True(session.Execute(commandKind, cancellation));

        session.AssertRetainedState(frame, pacingTicks, playing);
        session.AssertCommandCompleted();
    }

    [Fact]
    public void IgnoredScrubUpdateReleasesItsMailboxSlotForTheNextUpdate()
    {
        using var session = new WorkerSession("UpdateScrub");
        using var cancellation = new CancellationTokenSource();
        session.PrepareRetainedState(playing: false);
        var frame = session.PeekFrame();
        var pacingTicks = session.PacingStopwatch.ElapsedTicks;
        Assert.Equal("Enqueued", session.EnqueueIntent("UpdateScrub", TimeSpan.FromSeconds(2)));
        Assert.Equal("Coalesced", session.EnqueueIntent("UpdateScrub", TimeSpan.FromSeconds(3)));

        Assert.True(session.ExecuteNext(cancellation));
        session.AssertRetainedState(frame, pacingTicks, playing: false);
        Assert.Equal(0, Read<int>(session.Mailbox, "PendingCommands"));
        Assert.Equal("Enqueued", session.EnqueueIntent("UpdateScrub", TimeSpan.FromSeconds(4)));
        Assert.True(session.ExecuteNext(cancellation));

        session.AssertRetainedState(frame, pacingTicks, playing: false);
        Assert.Equal(2L, Read<long>(session.Mailbox, "CommandsEnqueued"));
        Assert.Equal(2L, Read<long>(session.Mailbox, "CommandsProcessed"));
        Assert.Equal(1L, Read<long>(session.Mailbox, "ScrubUpdatesCoalesced"));
        Assert.Equal(0, Read<int>(session.Mailbox, "PendingCommands"));
        session.AssertCommandCompleted();
    }

    [Fact]
    public void SeekSupersededByPlayPreservesTheLatestExactResumeIntent()
    {
        using var session = new WorkerSession("Seek");
        using var cancellation = new CancellationTokenSource();
        session.SetBufferedDuration(TimeSpan.FromSeconds(10));
        SetField(session.Worker, "IsPlaying", true);
        Assert.Equal("Enqueued", session.EnqueueIntent("Seek", TimeSpan.FromSeconds(2)));
        Assert.Equal("Coalesced", session.EnqueueIntent("Seek", TimeSpan.FromSeconds(3)));
        Assert.True(session.EnqueueControl("Play"));

        Assert.True(session.ExecuteNext(cancellation));

        Assert.Equal(TimeSpan.FromSeconds(3), Read<TimeSpan>(session.Controller, "PlaybackPosition"));
        Assert.Equal(TimeSpan.FromSeconds(3), Field(session.Worker, "PendingExactResumeTarget"));
        Assert.Equal("Paused", Read<object>(session.Controller, "State").ToString());
        Assert.False((bool)Field(session.Worker, "IsPlaying")!);
        Assert.False((bool)Field(session.Worker, "IsScrubbing")!);
        Assert.Same(session.Decoder, Field(session.Worker, "Decoder"));
        Assert.False((bool)Field(session.Decoder, "_disposed")!);
        Assert.Equal(1, Read<int>(session.Mailbox, "PendingCommands"));
        session.AssertFramesCleared();
        session.AssertCommandCompleted();
    }

    [Fact]
    public void ActiveScrubSupersededByControlKeepsTheLatestPosition()
    {
        using var session = new WorkerSession("UpdateScrub");
        using var cancellation = new CancellationTokenSource();
        session.SetBufferedDuration(TimeSpan.FromSeconds(10));
        Assert.Equal("Enqueued", session.EnqueueIntent("UpdateScrub", TimeSpan.FromSeconds(2)));
        Assert.Equal("Coalesced", session.EnqueueIntent("UpdateScrub", TimeSpan.FromSeconds(3)));
        Assert.True(session.EnqueueControl("EndScrub"));

        Assert.True(session.ExecuteNext(cancellation));

        Assert.Equal(TimeSpan.FromSeconds(3), Read<TimeSpan>(session.Controller, "PlaybackPosition"));
        Assert.Null(Field(session.Worker, "PendingExactResumeTarget"));
        Assert.Equal("Scrubbing", Read<object>(session.Controller, "State").ToString());
        Assert.True((bool)Field(session.Worker, "IsScrubbing")!);
        Assert.Same(session.Decoder, Field(session.Worker, "Decoder"));
        Assert.Equal(1, Read<int>(session.Mailbox, "PendingCommands"));
        session.AssertFramesCleared();
        session.AssertCommandCompleted();
    }

    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type TypeOf(string name) => SussudioAssembly.Load().GetType(name, throwOnError: true)!;
    private static T Read<T>(object instance, string name) => (T)instance.GetType().GetProperty(name, Members)!.GetValue(instance)!;
    private static object? Field(object instance, string name) => instance.GetType().GetField(name, Members)!.GetValue(instance);
    private static void SetField(object instance, string name, object? value) => instance.GetType().GetField(name, Members)!.SetValue(instance, value);
    private static void Set(object instance, string name, object? value) => instance.GetType().GetProperty(name, Members)!.SetValue(instance, value);

    private static object? Invoke(object instance, string name, params object?[] arguments)
    {
        try { return instance.GetType().GetMethod(name, Members)!.Invoke(instance, arguments); }
        catch (TargetInvocationException error) when (error.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private sealed class WorkerSession : IDisposable
    {
        private readonly object _buffer;
        private readonly object _reader;
        private readonly object _generation;

        public WorkerSession(string commandKind)
        {
            var options = Activator.CreateInstance(TypeOf("Sussudio.Models.FlashbackBufferOptions"))!;
            // Leave the real buffer uninitialized and empty; no session or media
            // file is created, and its potential directory is private to this case.
            Set(options, "TempDirectory", Path.Combine(Path.GetTempPath(), "sussudio-worker-" + Guid.NewGuid().ToString("N")));
            _buffer = Activator.CreateInstance(TypeOf("Sussudio.Services.Flashback.FlashbackBufferManager"), options)!;
            Controller = Activator.CreateInstance(TypeOf("Sussudio.Services.Flashback.FlashbackPlaybackController"), _buffer)!;
            Worker = Activator.CreateInstance(Controller.GetType().GetNestedType("PlaybackWorkerState", BindingFlags.NonPublic)!, nonPublic: true)!;
            // A closed real decoder bypasses CreateDecoder/FFmpeg initialization.
            // All calls in this fixture run on this test thread, with no playback worker.
            Decoder = Activator.CreateInstance(TypeOf("Sussudio.Services.Flashback.FlashbackDecoder"))!;
            SetField(Worker, "Decoder", Decoder);
            SetField(Worker, "PendingExactResumeTarget", TimeSpan.FromSeconds(7));
            SetField(Worker, "IsScrubbing", commandKind == "UpdateScrub");
            SetField(Controller, "_state", Enum.Parse(TypeOf("Sussudio.Models.FlashbackPlaybackState"),
                commandKind == "Pause" ? "Live" : commandKind == "UpdateScrub" ? "Scrubbing" : "Paused"));
            Set(Controller, "PlaybackPosition", TimeSpan.FromMilliseconds(1250));
            SetField(Controller, "_lastAudioPtsTicks", TimeSpan.FromSeconds(2).Ticks);
            SetField(Controller, "_lastVideoPtsTicks", TimeSpan.FromSeconds(3).Ticks);
            Mailbox = Field(Controller, "_commandMailbox")!;
            _generation = Read<object>(Mailbox, "CurrentGeneration");
            _reader = Read<object>(_generation, "Reader");
            // An empty frame carries no native handle; actual held-frame release
            // remains covered by FlashbackPrebufferBehaviorTests.
            Invoke(Field(Worker, "PrebufferedFrames")!, "Enqueue",
                Activator.CreateInstance(TypeOf("Sussudio.Services.Flashback.DecodedVideoFrame"))!);
        }

        public object Controller { get; }
        public object Worker { get; }
        public object Decoder { get; }
        public object Mailbox { get; }
        public Stopwatch PacingStopwatch => (Stopwatch)Field(Worker, "PacingStopwatch")!;

        private static object CreateCommand(string kind)
        {
            var command = Activator.CreateInstance(TypeOf("Sussudio.Services.Flashback.FlashbackPlaybackCommandMailbox+Command"))!;
            Set(command, "Kind", Enum.Parse(TypeOf("Sussudio.Services.Flashback.FlashbackPlaybackCommandMailbox+CommandKind"), kind));
            Set(command, "Position", TimeSpan.FromMilliseconds(2500));
            Set(command, "Delta", TimeSpan.FromMilliseconds(40));
            return command;
        }

        public bool Execute(string kind, CancellationTokenSource cancellation)
            => (bool)Invoke(Controller, "ExecutePlaybackCommand", Worker, CreateCommand(kind), _reader, cancellation)!;

        public bool ExecuteNext(CancellationTokenSource cancellation)
        {
            object?[] arguments = { _generation, null };
            Assert.True((bool)Invoke(Mailbox, "TryReadForPlayback", arguments)!);
            return (bool)Invoke(Controller, "ExecutePlaybackCommand", Worker, arguments[1], _reader, cancellation)!;
        }

        public string EnqueueIntent(string kind, TimeSpan position)
            => Invoke(Mailbox, kind == "Seek" ? "TryEnqueueSeek" : "TryEnqueueScrubUpdate", position)!.ToString()!;

        public bool EnqueueControl(string kind) => (bool)Invoke(Mailbox, "TryEnqueue", CreateCommand(kind))!;
        public void SetBufferedDuration(TimeSpan duration) => Invoke(_buffer, "UpdateLatestPts", duration);
        public object PeekFrame() => Invoke(Field(Worker, "PrebufferedFrames")!, "Peek")!;
        public void AssertFramesCleared() => Assert.Empty((ICollection)Field(Worker, "PrebufferedFrames")!);

        public void PrepareRetainedState(bool playing)
        {
            SetField(Worker, "IsPlaying", playing);
            SetField(Worker, "IsScrubbing", false);
            SetField(Worker, "FrozenValidStart", TimeSpan.FromSeconds(2));
            SetField(Worker, "FrameDuration", TimeSpan.FromMilliseconds(40));
            SetField(Controller, "_state", Enum.Parse(TypeOf("Sussudio.Models.FlashbackPlaybackState"), playing ? "Playing" : "Paused"));
            var frame = Invoke(Field(Worker, "PrebufferedFrames")!, "Dequeue")!;
            Set(frame, "Pts", TimeSpan.FromMilliseconds(3200));
            Set(frame, "Width", 64);
            Set(frame, "Height", 32);
            Set(frame, "DataLength", 3072);
            Invoke(Field(Worker, "PrebufferedFrames")!, "Enqueue", frame);
            PacingStopwatch.Start();
            SpinWait.SpinUntil(() => PacingStopwatch.ElapsedTicks > 0);
            PacingStopwatch.Stop();
        }

        public void AssertRetainedState(object expectedFrame, long pacingTicks, bool playing)
        {
            Assert.Single(((IEnumerable)Field(Worker, "PrebufferedFrames")!).Cast<object>());
            var frame = PeekFrame();
            foreach (var property in new[] { "Pts", "Data", "HeldFrame", "Width", "Height", "DataLength" })
            {
                Assert.Equal(Read<object>(expectedFrame, property), Read<object>(frame, property));
            }
            Assert.Equal(TimeSpan.FromSeconds(7), Field(Worker, "PendingExactResumeTarget"));
            Assert.Equal(TimeSpan.FromSeconds(2), Field(Worker, "FrozenValidStart"));
            Assert.Equal(TimeSpan.FromMilliseconds(40), Field(Worker, "FrameDuration"));
            Assert.Equal(TimeSpan.FromMilliseconds(1250), Read<TimeSpan>(Controller, "PlaybackPosition"));
            Assert.Equal(playing, Field(Worker, "IsPlaying"));
            Assert.False((bool)Field(Worker, "IsScrubbing")!);
            Assert.False((bool)Field(Worker, "FileOpen")!);
            Assert.Equal(playing ? "Playing" : "Paused", Read<object>(Controller, "State").ToString());
            Assert.Same(Decoder, Field(Worker, "Decoder"));
            Assert.False((bool)Field(Decoder, "_disposed")!);
            Assert.False(Read<bool>(Decoder, "IsOpen"));
            Assert.Equal(pacingTicks, PacingStopwatch.ElapsedTicks);
            Assert.False(PacingStopwatch.IsRunning);
            Assert.Equal(TimeSpan.FromSeconds(2).Ticks, Field(Controller, "_lastAudioPtsTicks"));
            Assert.Equal(TimeSpan.FromSeconds(3).Ticks, Field(Controller, "_lastVideoPtsTicks"));
        }

        public void AssertCommandCompleted()
        {
            Assert.Equal(-1, Field(Controller, "_activeCommandKind"));
            Assert.Equal(0L, Field(Controller, "_activeCommandStartedTimestamp"));
            Assert.False(Read<bool>(Controller, "PlaybackThreadAlive"));
        }

        public void Dispose()
        {
            try
            {
                Invoke(Controller, "ClearPrebufferedFrames", Field(Worker, "PrebufferedFrames"), "behavior_test_cleanup");
                ((IDisposable)Controller).Dispose();
            }
            finally
            {
                try { ((IDisposable)Decoder).Dispose(); }
                finally { ((IDisposable)_buffer).Dispose(); }
            }
        }
    }
}
