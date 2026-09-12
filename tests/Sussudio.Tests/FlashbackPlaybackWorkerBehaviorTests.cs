using System.Collections;
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
        session.AssertCommandCompleted();
    }

    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type TypeOf(string name) => SussudioAssembly.Load().GetType(name, throwOnError: true)!;
    private static T Read<T>(object value, string property) => (T)value.GetType().GetProperty(property, Members)!.GetValue(value)!;
    private static object? Field(object value, string field) => value.GetType().GetField(field, Members)!.GetValue(value);
    private static void SetField(object value, string field, object? data) => value.GetType().GetField(field, Members)!.SetValue(value, data);
    private static void Set(object value, string property, object? data) => value.GetType().GetProperty(property, Members)!.SetValue(value, data);

    private static object? Invoke(object value, string method, params object?[] arguments)
    {
        try { return value.GetType().GetMethod(method, Members)!.Invoke(value, arguments); }
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
            var mailbox = Field(Controller, "_commandMailbox")!;
            _reader = Read<object>(Read<object>(mailbox, "CurrentGeneration"), "Reader");
            // An empty frame carries no native handle; actual held-frame release
            // remains covered by FlashbackPrebufferBehaviorTests.
            Invoke(Field(Worker, "PrebufferedFrames")!, "Enqueue",
                Activator.CreateInstance(TypeOf("Sussudio.Services.Flashback.DecodedVideoFrame"))!);
        }

        public object Controller { get; }
        public object Worker { get; }
        public object Decoder { get; }

        public bool Execute(string kind, CancellationTokenSource cancellation)
        {
            var command = Activator.CreateInstance(TypeOf("Sussudio.Services.Flashback.FlashbackPlaybackCommandMailbox+Command"))!;
            Set(command, "Kind", Enum.Parse(TypeOf("Sussudio.Services.Flashback.FlashbackPlaybackCommandMailbox+CommandKind"), kind));
            Set(command, "Position", TimeSpan.FromMilliseconds(2500));
            Set(command, "Delta", TimeSpan.FromMilliseconds(40));
            return (bool)Invoke(Controller, "ExecutePlaybackCommand", Worker, command, _reader, cancellation)!;
        }

        public void AssertCommandCompleted()
        {
            Assert.Empty((ICollection)Field(Worker, "PrebufferedFrames")!);
            Assert.Equal(-1, Field(Controller, "_activeCommandKind"));
            Assert.Equal(0L, Field(Controller, "_activeCommandStartedTimestamp"));
            Assert.False(Read<bool>(Controller, "PlaybackThreadAlive"));
        }

        public void Dispose()
        {
            try { ((IDisposable)Controller).Dispose(); }
            finally
            {
                try { ((IDisposable)Decoder).Dispose(); }
                finally { ((IDisposable)_buffer).Dispose(); }
            }
        }
    }
}
