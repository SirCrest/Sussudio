using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using FFmpeg.AutoGen;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackRotationBehaviorTests : IClassFixture<BundledRuntime>
{
    private readonly BundledRuntime _runtime;

    public FlashbackRotationBehaviorTests(BundledRuntime runtime) => _runtime = runtime;

    [Fact]
    public async Task PreflightFailuresKeepTheOutputUsableAndSuccessfulRotationResetsRetries()
    {
        await using var session = new RotationSession(_runtime);
        var blockedPath = session.CreateInvalidParentPath();
        session.RotationTarget = _ => blockedPath;
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            session.EncodeFrames(60);
            Assert.False(session.Rotate());
            Assert.True(session.EncoderIsOpen);
            Assert.Equal(session.OriginalPath, session.ActivePath);
            Assert.Equal(attempt, session.ConsecutiveFailures);
            Assert.Empty(session.FatalErrors);
        }

        session.RotationTarget = null;
        session.EncodeFrames(60);
        Assert.True(session.Rotate());

        Assert.Equal(3, session.RotationAttempts);
        Assert.Equal(2L, session.TotalFailures);
        Assert.Equal(0, session.ConsecutiveFailures);
        Assert.NotEqual(session.OriginalPath, session.ActivePath);
        Assert.True(session.EncoderIsOpen);
        Assert.Null(session.Failure);
        Assert.Empty(session.FatalErrors);
        session.AssertOriginalSegmentPreserved();
        session.EncodeFrames(60);
        session.AssertFramesReturned();
    }

    [Fact]
    public async Task ThreePreflightFailuresEscalateOnceAndKeepTheOriginalCause()
    {
        await using var session = new RotationSession(_runtime);
        var blockedPath = session.CreateInvalidParentPath();
        session.RotationTarget = _ => blockedPath;
        session.ThrowFromFatalCallback = true;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            session.EncodeFrames(60);
            Assert.False(session.Rotate());
            Assert.True(session.EncoderIsOpen);
            Assert.Equal(attempt, session.ConsecutiveFailures);
        }

        var error = Assert.IsType<IOException>(Assert.Single(session.FatalErrors));
        Assert.Same(session.RotationErrors[^1], error.InnerException);
        Assert.Same(error, session.Failure);
        Assert.False(session.IsStarted);
        session.AssertQueuesCompleted();

        await session.StartOwner().WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Same(error, session.Failure);
        Assert.Single(session.FatalErrors);
        session.AssertFramesReturned();
        session.AssertOriginalSegmentPreserved();
    }

    [Fact]
    public async Task ScheduledNativeRotationFailureStopsTheBatchWithItsOriginalError()
    {
        await using var session = new RotationSession(_runtime);
        var blockedPath = session.CreateUnwritableOutputPath();
        session.RotationTarget = _ => blockedPath;
        session.ThrowFromFatalCallback = true;
        session.BeginRecording();
        session.SetRotationDuration(TimeSpan.FromSeconds(2));
        session.EnqueueFrames(90);
        session.CompleteQueues();

        await session.StartOwner().WaitAsync(TimeSpan.FromSeconds(15));

        session.AssertTerminalRotationFailure();
        Assert.InRange(session.SubmittedFrames, 1L, 89L);
        session.AssertFramesReturned();
        session.AssertOriginalSegmentPreserved();
        var result = await session.EndRecordingAsync();
        AssertFailureResult(result, session, "recording-flashback-encode-failed");
        Assert.Contains(session.RotationErrors[0].Message, Read<string>(result, "StatusMessage"));
    }

    [Fact]
    public async Task ForcedRotationCompletesTheLiveSegmentAndReleasesItsFence()
    {
        await using var session = new RotationSession(_runtime);
        session.EncodeFrames(60);
        _ = session.StartOwner();

        var result = await Task.Run(session.ForceRotate).WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal("Completed", Read<object>(result, "Status").ToString());
        Assert.Equal(new[] { session.OriginalPath }, Read<IReadOnlyList<string>>(result, "SegmentPaths"));
        Assert.Equal(1, session.RotationAttempts);
        Assert.Equal((false, true, true), Assert.Single(session.RotationStates));
        session.AssertForceRotateIdle();
        Assert.NotEqual(session.OriginalPath, session.ActivePath);
        Assert.True(session.EncoderIsOpen);
        Assert.Null(session.Failure);
        Assert.Empty(session.FatalErrors);
        session.AssertFramesReturned();
        session.AssertOriginalSegmentPreserved();
    }

    [Fact]
    public async Task EmptyLiveEdgeCompletesWithoutRotating()
    {
        await using var session = new RotationSession(_runtime);
        _ = session.StartOwner();

        var result = await Task.Run(session.ForceRotate).WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal("Completed", Read<object>(result, "Status").ToString());
        Assert.Empty(Read<IReadOnlyList<string>>(result, "SegmentPaths"));
        Assert.Equal(0, session.RotationAttempts);
        Assert.Empty(session.RotationStates);
        session.AssertForceRotateIdle();
        Assert.Equal(session.OriginalPath, session.ActivePath);
        Assert.True(session.EncoderIsOpen);
        Assert.Null(session.Failure);
    }

    [Fact]
    public async Task CallerCancellationClearsAPendingRequestBeforeTheOwnerConsumesIt()
    {
        await using var session = new RotationSession(_runtime);
        using var cancellation = new CancellationTokenSource();
        var forceRotate = Task.Run(() => session.ForceRotate(cancellation.Token));
        try
        {
            using var publicationTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (!session.IsForceRotateRequested && !forceRotate.IsCompleted)
                await Task.Delay(10, publicationTimeout.Token);

            Assert.True(session.IsForceRotateRequested);
            Assert.True(session.IsForceRotateActive);
            Assert.False(session.IsForceRotateDraining);
            cancellation.Cancel();

            var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => forceRotate.WaitAsync(TimeSpan.FromSeconds(15)));

            Assert.Equal(cancellation.Token, error.CancellationToken);
            session.AssertForceRotateIdle();
            Assert.Equal(0, session.RotationAttempts);
            Assert.Empty(session.RotationStates);
            Assert.Equal(session.OriginalPath, session.ActivePath);
            Assert.Null(session.Failure);
        }
        finally
        {
            // Join the export caller before fixture disposal can start the owner.
            cancellation.Cancel();
            try { await forceRotate; }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        }
    }

    [Fact]
    public async Task ForcedNativeRotationFailureCompletesTheRequestAndReleasesItsFence()
    {
        await using var session = new RotationSession(_runtime);
        var blockedPath = session.CreateUnwritableOutputPath();
        session.RotationTarget = _ => blockedPath;
        session.BeginRecording();
        var framesEncoded = session.ObserveEncodedFrames(60);
        session.EnqueueFrames(60);
        var owner = session.StartOwner();
        await framesEncoded.WaitAsync(TimeSpan.FromSeconds(15));

        var result = await Task.Run(session.ForceRotate).WaitAsync(TimeSpan.FromSeconds(15));
        await owner.WaitAsync(TimeSpan.FromSeconds(15));

        AssertRotationPlanFails(result, session.OriginalPath);
        Assert.Equal((false, true, true), Assert.Single(session.RotationStates));
        session.AssertForceRotateIdle();
        session.AssertTerminalRotationFailure();
        session.AssertFramesReturned();
        session.AssertOriginalSegmentPreserved();
        AssertFailureResult(await session.EndRecordingAsync(), session, "recording-flashback-encode-failed");
    }

    [Fact]
    public async Task EncodingFailureAfterAdmissionRejectsRotationAndPreservesItsCause()
    {
        await using var session = new RotationSession(_runtime);
        session.EncodeFrames(60);
        var failure = new IOException("encoder failed after export admission");
        var forceRotate = Task.Run(session.ForceRotate);
        try
        {
            var preparedPath = await session.WaitForPendingRequest();
            session.FailEncoding(failure);
            var owner = session.StartOwner();

            var result = await forceRotate.WaitAsync(TimeSpan.FromSeconds(5));
            await owner.WaitAsync(TimeSpan.FromSeconds(15));

            AssertRotationPlanFails(result, session.OriginalPath);
            Assert.Same(failure, session.Failure);
            Assert.Same(failure, Assert.Single(session.FatalErrors));
            Assert.False(File.Exists(preparedPath));
            Assert.Equal(0, session.RotationAttempts);
            session.AssertForceRotateIdle();
            session.AssertOriginalSegmentPreserved();
        }
        finally
        {
            session.FailPendingForceRotate();
            await forceRotate.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task ForcedPreflightFailureReportsFailedWithoutClosingTheUsableEncoder()
    {
        await using var session = new RotationSession(_runtime);
        var blockedPath = session.CreateInvalidParentPath();
        session.RotationTarget = _ => blockedPath;
        session.SetRotationDuration(TimeSpan.FromMinutes(1));
        session.EncodeFrames(60);
        var owner = session.StartOwner();

        var result = await Task.Run(session.ForceRotate).WaitAsync(TimeSpan.FromSeconds(15));

        AssertRotationPlanFails(result, session.OriginalPath);
        Assert.Equal(1, session.RotationAttempts);
        Assert.Single(session.RotationErrors);
        Assert.Equal((false, true, true), Assert.Single(session.RotationStates));
        Assert.Equal(1, session.ConsecutiveFailures);
        Assert.True(session.EncoderIsOpen);
        Assert.Equal(session.OriginalPath, session.ActivePath);
        Assert.Null(session.Failure);
        Assert.Empty(session.FatalErrors);
        session.AssertForceRotateIdle();

        session.EnqueueFrames(30);
        session.CompleteQueues();
        await owner.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal(90L, session.SubmittedFrames);
        Assert.Null(session.Failure);
        session.AssertFramesReturned();
        session.AssertOriginalSegmentPreserved();
    }

    [Fact]
    public async Task ShutdownAbandonmentReportsFailedAndDeletesThePreparedPlaceholder()
    {
        await using var session = new RotationSession(_runtime);
        var forceRotate = Task.Run(session.ForceRotate);
        try
        {
            var preparedPath = await session.WaitForPendingRequest();
            Assert.True(File.Exists(preparedPath));

            session.FailPendingForceRotate();
            var result = await forceRotate.WaitAsync(TimeSpan.FromSeconds(5));

            AssertRotationPlanFails(result, session.OriginalPath);
            Assert.False(File.Exists(preparedPath));
            Assert.Equal(0, session.RotationAttempts);
            session.AssertForceRotateIdle();
        }
        finally
        {
            session.FailPendingForceRotate();
            await forceRotate.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task TimeoutBeforeCommitReportsCanceledAndDeletesThePreparedPlaceholder()
    {
        await using var session = new RotationSession(_runtime);
        var forceRotate = Task.Run(session.ForceRotate);
        try
        {
            var preparedPath = await session.WaitForPendingRequest();

            var result = await forceRotate.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal("CanceledBeforeCommit", Read<object>(result, "Status").ToString());
            Assert.Empty(Read<IReadOnlyList<string>>(result, "SegmentPaths"));
            Assert.False(File.Exists(preparedPath));
            Assert.Equal(0, session.RotationAttempts);
            session.AssertForceRotateIdle();
        }
        finally
        {
            session.FailPendingForceRotate();
            await forceRotate.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommittedRotationKeepsOwnershipAfterTheCallerStopsWaiting(bool cancelCaller)
    {
        await using var session = new RotationSession(_runtime);
        using var cancellation = new CancellationTokenSource();
        using var releaseRotation = new ManualResetEventSlim();
        var enteredRotation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        session.RotationTarget = path =>
        {
            enteredRotation.TrySetResult();
            Assert.True(releaseRotation.Wait(TimeSpan.FromSeconds(15)));
            return path;
        };
        session.EncodeFrames(60);
        _ = session.StartOwner();
        var forceRotate = Task.Run(() => session.ForceRotate(cancellation.Token));
        try
        {
            await enteredRotation.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(session.IsForceRotateDraining);
            if (cancelCaller)
            {
                cancellation.Cancel();
                var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => forceRotate.WaitAsync(TimeSpan.FromSeconds(5)));
                Assert.Equal(cancellation.Token, failure.CancellationToken);
            }
            else
            {
                var result = await forceRotate.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Equal("CommittedPending", Read<object>(result, "Status").ToString());
                Assert.Empty(Read<IReadOnlyList<string>>(result, "SegmentPaths"));
            }
            Assert.True(session.IsForceRotateDraining);
        }
        finally
        {
            releaseRotation.Set();
            cancellation.Cancel();
            try { await forceRotate.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            session.AssertForceRotateIdle();
        }

        Assert.Equal(1, session.RotationAttempts);
        Assert.True(session.EncoderIsOpen);
        Assert.Null(session.Failure);
        Assert.Empty(session.FatalErrors);
        session.AssertOriginalSegmentPreserved();
    }

    [Fact]
    public async Task EndingAnInactiveRecordingReportsFailedOutcomeAndPreservesItsPaths()
    {
        await using var session = new RotationSession(_runtime);
        session.EncodeFrames(60);
        session.BeginRecording();
        session.RollBackRecordingStart();

        var result = await session.EndRecordingAsync();

        AssertFailureResult(result, session, "recording-finalization-failed");
        Assert.Equal("Flashback recording was not active.", Read<string>(result, "StatusMessage"));
        Assert.Null(session.Failure);
        Assert.True(session.EncoderIsOpen);
    }

    [Theory]
    [InlineData(48_000, 2, 320_000, 48_000, 2, 320_000)]
    [InlineData(48_000, 2, 320_000, 44_100, 1, 96_000)]
    public async Task TerminalRotationReleasesPendingAudioAndMicrophoneWithoutMorePacketWrites(
        int audioSampleRate, int audioChannels, int audioBitRate,
        int microphoneSampleRate, int microphoneChannels, int microphoneBitRate)
    {
        await using var session = new RotationSession(_runtime, audioEnabled: true, microphoneEnabled: true,
            audioSampleRate, audioChannels, audioBitRate, microphoneSampleRate, microphoneChannels, microphoneBitRate);
        session.AssertNativeAudioOptions();
        var blockedPath = session.CreateUnwritableOutputPath();
        session.RotationTarget = _ => blockedPath;
        session.BeginRecording();
        session.SetRotationDuration(TimeSpan.FromSeconds(2));
        session.EnqueueAudio(samplesPerChannel: 73);
        session.EnqueueFrames(90);
        session.CompleteQueues();

        await session.StartOwner().WaitAsync(TimeSpan.FromSeconds(15));

        session.AssertTerminalRotationFailure();
        Assert.False(session.EncoderWasOpenAtFailure);
        Assert.Equal(73 * audioChannels * sizeof(float), session.PendingAudioBytesAtFailure);
        Assert.Equal(73 * microphoneChannels * sizeof(float), session.PendingMicrophoneBytesAtFailure);
        Assert.Equal(session.PacketBytesAtFailure, session.PacketBytesWritten);
        session.AssertNativeResourcesReleased();
        session.AssertFramesReturned();
        session.AssertOriginalSegmentPreserved();
        AssertFailureResult(await session.EndRecordingAsync(), session, "recording-flashback-encode-failed");
    }

    [Theory]
    [InlineData(48_000, 2, 320_000, 48_000, 2, 320_000)]
    [InlineData(48_000, 2, 320_000, 44_100, 1, 96_000)]
    public async Task NormalCloseStillFlushesPartialAudioAndMicrophoneIntoTheOutput(
        int audioSampleRate, int audioChannels, int audioBitRate,
        int microphoneSampleRate, int microphoneChannels, int microphoneBitRate)
    {
        await using var session = new RotationSession(_runtime, audioEnabled: true, microphoneEnabled: true,
            audioSampleRate, audioChannels, audioBitRate, microphoneSampleRate, microphoneChannels, microphoneBitRate);
        session.AssertNativeAudioOptions();
        session.EnqueueAudio(samplesPerChannel: 73);
        session.EnqueueFrames(60);
        session.CompleteQueues();

        await session.StartOwner().WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Null(session.Failure);
        Assert.Empty(session.FatalErrors);
        session.AssertNativeResourcesReleased();
        session.AssertFramesReturned();
        session.AssertOriginalSegmentPreserved();
        AssertOutputHasVideoAndBothAudioTracks(session.OriginalPath,
            audioSampleRate, audioChannels, microphoneSampleRate, microphoneChannels);
    }

    [Fact]
    public void UnsupportedMicrophoneSampleRateReleasesBothStreamsAfterAudioInitialization()
    {
        var directory = Directory.CreateTempSubdirectory("sussudio-aac-init-failure-");
        try
        {
            using var encoder = (IDisposable)Activator.CreateInstance(_runtime.Type("Sussudio.Services.Recording.LibAvEncoder"))!;
            var options = Activator.CreateInstance(_runtime.Type("Sussudio.Services.Recording.LibAvEncoderOptions"))!;
            Set(options, "OutputPath", Path.Combine(directory.FullName, "unsupported-mic.mp4"));
            Set(options, "CodecName", "libx264");
            Set(options, "Width", 64);
            Set(options, "Height", 64);
            Set(options, "FrameRate", 30d);
            Set(options, "BitRate", 1_000_000u);
            Set(options, "AudioEnabled", true);
            Set(options, "MicrophoneEnabled", true);
            Set(options, "MicrophoneSampleRate", 12_345);

            var error = Assert.Throws<InvalidOperationException>(() => Invoke(encoder, "Initialize", options));

            Assert.Contains("avcodec_open2(mic)", error.Message);
            Assert.False(Read<bool>(encoder, "IsEncoding"));
            // A fresh encoder records this time base only after the audio codec opens.
            var audioState = GetField(encoder, "_audio")!;
            var audioTimeBase = (AVRational)audioState.GetType().GetField("CachedTimeBase")!.GetValue(audioState)!;
            Assert.Equal(1, audioTimeBase.num);
            Assert.Equal(48_000, audioTimeBase.den);
            AssertNativeResourcesReleased(encoder);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private void AssertRotationPlanFails(object result, string preservedPath)
    {
        Assert.Equal("Failed", Read<object>(result, "Status").ToString());
        Assert.Empty(Read<IReadOnlyList<string>>(result, "SegmentPaths"));
        var planner = _runtime.Type("Sussudio.Services.Flashback.FlashbackExportPlanner");
        var preserved = new[] { preservedPath };
        foreach (var requireCompleteLiveEdge in new[] { false, true })
        {
            var plan = planner.GetMethod("PlanLiveEdge", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, new object[] { result, requireCompleteLiveEdge, preserved, preserved })!;
            Assert.Equal("ForceRotateFailed", Read<object>(plan, "FailureKind").ToString());
            Assert.Equal("Flashback export failed: live-edge segment rotation failed.", Read<string>(plan, "FailureMessage"));
            Assert.Same(preserved, Read<IReadOnlyList<string>>(plan, "PreservedArtifacts"));
            Assert.False(Read<bool>(plan, "ForceRotateFallbackUsed"));
        }
    }

    private static void AssertFailureResult(object result, RotationSession session, string failureCode)
    {
        Assert.False(Read<bool>(result, "Succeeded"));
        Assert.Equal("Failed", Read<object>(result, "Outcome").ToString());
        Assert.Equal(failureCode, Read<string>(result, "FailureCode"));
        Assert.Equal(session.RecordingPath, Read<string>(result, "OutputPath"));
        Assert.Equal(new[] { session.OriginalPath }, Read<IReadOnlyList<string>>(result, "PreservedArtifacts"));
        Assert.True(File.Exists(session.OriginalPath));
    }

    // The existing rotation callback only redirects a filesystem destination.
    // Every result and failure below comes from the sink's real libav encoder.
    private sealed class RotationSession : IAsyncDisposable
    {
        private const int Width = 64;
        private const int Height = 64;
        private readonly BundledRuntime _runtime;
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("sussudio-rotation-");
        private readonly CancellationTokenSource _cancellation = new();
        private readonly List<object> _queues = new();
        private readonly List<object> _frames = new();
        private readonly object _manager = null!;
        private readonly object _sink = null!;
        private readonly object _encoder = null!;
        private readonly int _audioSampleRate;
        private readonly int _audioChannels;
        private readonly int _audioBitRate;
        private readonly int _microphoneSampleRate;
        private readonly int _microphoneChannels;
        private readonly int _microphoneBitRate;
        private Task? _owner;
        private bool _initialized;

        public RotationSession(BundledRuntime runtime, bool audioEnabled = false, bool microphoneEnabled = false,
            int audioSampleRate = 48_000, int audioChannels = 2, int audioBitRate = 320_000,
            int microphoneSampleRate = 48_000, int microphoneChannels = 2, int microphoneBitRate = 320_000)
        {
            _runtime = runtime;
            _audioSampleRate = audioSampleRate;
            _audioChannels = audioChannels;
            _audioBitRate = audioBitRate;
            _microphoneSampleRate = microphoneSampleRate;
            _microphoneChannels = microphoneChannels;
            _microphoneBitRate = microphoneBitRate;
            try
            {
                var bufferOptions = Activator.CreateInstance(runtime.Type("Sussudio.Models.FlashbackBufferOptions"))!;
                Set(bufferOptions, "TempDirectory", _directory.FullName);
                Set(bufferOptions, "FreeDiskBytesProvider", (Func<long>)(() => long.MaxValue));
                _manager = Activator.CreateInstance(runtime.Type("Sussudio.Services.Flashback.FlashbackBufferManager"), bufferOptions)!;
                Invoke(_manager, "Initialize", "rotation-test");
                Invoke(_manager, "SetSegmentExtension", ".mp4");
                OriginalPath = (string)Invoke(_manager, "AcquireSegmentPath")!;
                Invoke(_manager, "MarkActiveSegmentStart", OriginalPath, TimeSpan.Zero);
                RecordingPath = Path.Combine(_directory.FullName, "requested-recording.mp4");

                var resultType = runtime.Type("Sussudio.Services.Recording.RotateOutputResult");
                var callbackType = typeof(Func<,>).MakeGenericType(typeof(string), resultType);
                var pathParameter = Expression.Parameter(typeof(string), "path");
                Func<string, object> rotate = RotateNativeOutput;
                var callback = Expression.Lambda(callbackType,
                    Expression.Convert(Expression.Invoke(Expression.Constant(rotate), pathParameter), resultType),
                    pathParameter).Compile();
                var sinkType = runtime.Type("Sussudio.Services.Flashback.FlashbackEncoderSink");
                _sink = sinkType.GetConstructor(PrivateInstance, null,
                    new[] { _manager.GetType(), callbackType }, null)!.Invoke(new object[] { _manager, callback });
                _encoder = GetField(_sink, "_encoder")!;
                var options = Activator.CreateInstance(runtime.Type("Sussudio.Services.Recording.LibAvEncoderOptions"))!;
                Set(options, "OutputPath", OriginalPath);
                Set(options, "CodecName", "libx264");
                Set(options, "Width", Width);
                Set(options, "Height", Height);
                Set(options, "FrameRate", 30d);
                Set(options, "BitRate", 1_000_000u);
                Set(options, "GopSize", 30);
                Set(options, "FragmentedMp4", true);
                Set(options, "AudioEnabled", audioEnabled);
                Set(options, "MicrophoneEnabled", microphoneEnabled);
                Set(options, "AudioSampleRate", audioSampleRate);
                Set(options, "AudioChannels", audioChannels);
                Set(options, "AudioBitRate", audioBitRate);
                Set(options, "MicrophoneSampleRate", microphoneSampleRate);
                Set(options, "MicrophoneChannels", microphoneChannels);
                Set(options, "MicrophoneBitRate", microphoneBitRate);
                Invoke(_encoder, "Initialize", options);

                var context = Activator.CreateInstance(runtime.Type("Sussudio.Models.FlashbackSessionContext"))!;
                Set(context, "FrameRate", 30d);
                Set(context, "CodecName", "libx264");
                Set(context, "AudioEnabled", audioEnabled);
                Set(context, "MicrophoneEnabled", microphoneEnabled);
                SetField(_sink, "_sessionContext", context);
                SetField(_sink, "_width", Width);
                SetField(_sink, "_height", Height);
                SetField(_sink, "_audioEnabled", audioEnabled);
                SetField(_sink, "_microphoneEnabled", microphoneEnabled);
                SetField(_sink, "_tsFilePath", OriginalPath);
                SetField(_sink, "_cts", _cancellation);
                foreach (var name in new[] { "_videoQueue", "_audioQueue", "_microphoneQueue", "_gpuQueue" })
                    _queues.Add(CreateQueue(_sink, name));
                Invoke(_sink, "SetFatalErrorCallback", (Action<Exception>)(error =>
                {
                    FatalErrors.Add(error);
                    if (ThrowFromFatalCallback) throw new InvalidOperationException("Fatal observer failed.");
                }));
                SetField(_sink, "_started", true);
                _initialized = true;
            }
            catch (Exception setupFailure)
            {
                try { DisposeAsync().AsTask().GetAwaiter().GetResult(); }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException("Rotation fixture setup and cleanup failed.", setupFailure, cleanupFailure);
                }
                throw;
            }
        }

        public string OriginalPath { get; } = string.Empty;
        public string RecordingPath { get; } = string.Empty;
        public Func<string, string>? RotationTarget { get; set; }
        public List<Exception> RotationErrors { get; } = new();
        public List<(bool Requested, bool Draining, bool Active)> RotationStates { get; } = new();
        public List<Exception> FatalErrors { get; } = new();
        public bool ThrowFromFatalCallback { get; set; }
        public int RotationAttempts { get; private set; }
        public bool EncoderWasOpenAtFailure { get; private set; }
        public int PendingAudioBytesAtFailure { get; private set; }
        public int PendingMicrophoneBytesAtFailure { get; private set; }
        public long PacketBytesAtFailure { get; private set; }
        public long PacketBytesWritten => Read<long>(_encoder, "TotalBytesWritten");
        public bool EncoderIsOpen => Read<bool>(_encoder, "IsEncoding");
        public bool IsStarted => (bool)GetField(_sink, "_started")!;
        public bool IsForceRotateActive => Read<bool>(_sink, "IsForceRotateActive");
        public bool IsForceRotateRequested => Read<bool>(_sink, "IsForceRotateRequested");
        public bool IsForceRotateDraining => Read<bool>(_sink, "IsForceRotateDraining");
        public Exception? Failure => (Exception?)GetField(_sink, "_encodingFailure");
        public string ActivePath => (string)GetField(_sink, "_tsFilePath")!;
        public int ConsecutiveFailures => (int)GetField(_sink, "_consecutiveRotationFailures")!;
        public long TotalFailures => (long)GetField(_sink, "_segmentRotationFailures")!;
        public long SubmittedFrames => (long)GetField(_sink, "_videoFramesSubmittedToEncoder")!;

        public string CreateInvalidParentPath()
        {
            var parent = Path.Combine(_directory.FullName, "parent-is-a-file");
            File.WriteAllText(parent, "The native transition must not begin.");
            return Path.Combine(parent, "next.mp4");
        }

        public string CreateUnwritableOutputPath()
        {
            var path = Path.Combine(_directory.FullName, "output-is-a-directory.mp4");
            Directory.CreateDirectory(path);
            return path;
        }

        private object RotateNativeOutput(string requestedPath)
        {
            RotationAttempts++;
            RotationStates.Add((IsForceRotateRequested, IsForceRotateDraining, IsForceRotateActive));
            try { return Invoke(_encoder, "RotateOutput", RotationTarget?.Invoke(requestedPath) ?? requestedPath)!; }
            catch (Exception error)
            {
                EncoderWasOpenAtFailure = EncoderIsOpen;
                PendingAudioBytesAtFailure = ReadAudioStateInt("_audio", "AccumulatorBytes");
                PendingMicrophoneBytesAtFailure = ReadAudioStateInt("_mic", "AccumulatorBytes");
                PacketBytesAtFailure = PacketBytesWritten;
                RotationErrors.Add(error);
                throw;
            }
        }

        public void EnqueueAudio(int samplesPerChannel)
        {
            // A real partial AAC input frame remains in each native encoder's
            // accumulator until normal finalization, not segment rotation.
            ReadOnlyMemory<byte> audio = new byte[samplesPerChannel * _audioChannels * sizeof(float)];
            ReadOnlyMemory<byte> microphone = new byte[samplesPerChannel * _microphoneChannels * sizeof(float)];
            Invoke(_sink, "EnqueueAudioSamples", audio);
            if (_microphoneChannels == 2)
            {
                Invoke(_sink, "EnqueueMicrophoneSamples", microphone);
            }
            else
            {
                // The production sink admits stereo only. Feed mono to its real encoder
                // before the owner starts; rotation and close still run on the sink owner.
                Assert.Null(_owner);
                var send = _encoder.GetType().GetMethod("SendMicrophoneSamples")!
                    .CreateDelegate<SendAudioSamples>(_encoder);
                send(microphone.Span);
                Assert.Equal(microphone.Length, ReadAudioStateInt("_mic", "AccumulatorBytes"));
            }
        }

        private delegate void SendAudioSamples(ReadOnlySpan<byte> samples);

        public void AssertNativeAudioOptions()
        {
            FlashbackRotationBehaviorTests.AssertNativeAudioOptions(
                _encoder, "_audio", _audioSampleRate, _audioChannels, _audioBitRate, 1);
            FlashbackRotationBehaviorTests.AssertNativeAudioOptions(
                _encoder, "_mic", _microphoneSampleRate, _microphoneChannels, _microphoneBitRate, 2);
        }

        private int ReadAudioStateInt(string stream, string name)
        {
            var state = GetField(_encoder, stream)!;
            return (int)state.GetType().GetField(name)!.GetValue(state)!;
        }

        public void EnqueueFrames(int count)
        {
            var frameType = _runtime.Type("Sussudio.Services.Contracts.PooledVideoFrame");
            var pixelFormat = Enum.Parse(_runtime.Type("Sussudio.Services.Contracts.PooledVideoPixelFormat"), "Nv12");
            var enqueue = _runtime.Type("Sussudio.Services.Contracts.IRawVideoFrameLeaseTryEncoder")
                .GetMethod("TryEnqueueRawVideoFrame")!;
            for (var index = 0; index < count; index++)
            {
                var tick = Environment.TickCount64;
                var frame = frameType.GetMethod("Rent")!.Invoke(null,
                    new object[] { (long)_frames.Count + 1, tick, tick, Width, Height, pixelFormat, Width * Height * 3 / 2 })!;
                _frames.Add(frame);
                using ((IDisposable)frame)
                {
                    var memory = Read<Memory<byte>>(frame, "Memory");
                    memory.Span[..(Width * Height)].Fill(16);
                    memory.Span[(Width * Height)..].Fill(128);
                    var lease = Invoke(frame, "AddLease")!;
                    try { Assert.True((bool)InvokeMethod(enqueue, _sink, new[] { lease })!); }
                    catch
                    {
                        ((IDisposable)lease).Dispose();
                        throw;
                    }
                }
            }
        }

        public void EncodeFrames(int count)
        {
            EnqueueFrames(count);
            var reader = Read<object>(_queues[0], "Reader");
            Assert.True((bool)Invoke(_sink, "DrainVideoPackets", reader, int.MaxValue)!);
        }

        public bool Rotate()
            => (bool)Invoke(_sink, "RotateSegment", Invoke(_sink, "ResolveEncoderPts"), null)!;

        public void SetRotationDuration(TimeSpan duration) => SetField(_sink, "_segmentDuration", duration);
        public void BeginRecording() => Invoke(_sink, "BeginRecording", RecordingPath);
        public void RollBackRecordingStart() => Invoke(_sink, "CancelRecordingStartRollback", "behavior_test");
        public object ForceRotate()
            => ForceRotate(CancellationToken.None);

        public object ForceRotate(CancellationToken cancellationToken)
            => Invoke(_sink, "ForceRotateForExport", TimeSpan.Zero, TimeSpan.FromSeconds(2), cancellationToken)!;

        public void FailPendingForceRotate() => Invoke(_sink, "FailPendingForceRotate");
        public void FailEncoding(Exception failure) => Invoke(_sink, "FailEncoding", failure);

        public async Task<string> WaitForPendingRequest()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            object? request;
            while ((request = GetField(_sink, "_forceRotateRequest")) == null)
                await Task.Delay(10, timeout.Token);
            return Read<string>(request, "PreparedPath");
        }

        public void AssertForceRotateIdle()
        {
            // Request completion may precede the owner's draining-clear finally.
            Assert.True((bool)Invoke(_sink, "WaitForForceRotateIdle", TimeSpan.FromSeconds(5))!);
            Assert.False(IsForceRotateRequested);
            Assert.False(IsForceRotateDraining);
            Assert.False(IsForceRotateActive);
        }

        public async Task<object> EndRecordingAsync()
        {
            var task = (Task)Invoke(_sink, "EndRecordingAsync", CancellationToken.None)!;
            await task.WaitAsync(TimeSpan.FromSeconds(5));
            return Read<object>(task, "Result");
        }

        public Task ObserveEncodedFrames(long target)
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler<long> handler = (_, count) =>
            {
                if (count >= target) completion.TrySetResult();
            };
            _sink.GetType().GetEvent("FrameEncoded")!.AddEventHandler(_sink, handler);
            return completion.Task;
        }

        public Task StartOwner()
        {
            Assert.Null(_owner);
            _owner = Task.Factory.StartNew(() => Invoke(_sink, "EncodingLoop", _cancellation.Token),
                CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            SetField(_sink, "_encodingTask", _owner);
            return _owner;
        }

        public void CompleteQueues()
        {
            foreach (var queue in _queues) Invoke(Read<object>(queue, "Writer"), "TryComplete", (object?)null);
        }

        public void AssertQueuesCompleted()
        {
            foreach (var queue in _queues)
                Assert.True(Read<Task>(Read<object>(queue, "Reader"), "Completion").IsCompletedSuccessfully);
        }

        public void AssertFramesReturned()
        {
            Assert.Equal(0, (int)GetField(_sink, "_videoQueueDepth")!);
            Assert.All(_frames, frame =>
            {
                Assert.True(Read<bool>(frame, "IsReturned"));
                Assert.Equal(0, Read<int>(frame, "LeaseCount"));
            });
        }

        public void AssertOriginalSegmentPreserved()
        {
            var segments = ((IEnumerable)GetField(_manager, "_completedSegments")!).Cast<object>();
            Assert.Contains(segments, segment => Read<string>(segment, "Path") == OriginalPath);
            using var oldOutput = File.Open(OriginalPath, FileMode.Open, FileAccess.Read, FileShare.None);
            Assert.True(oldOutput.Length > 0);
        }

        public void AssertNativeResourcesReleased()
        {
            FlashbackRotationBehaviorTests.AssertNativeResourcesReleased(_encoder);
        }

        public void AssertTerminalRotationFailure()
        {
            var error = Assert.Single(RotationErrors);
            Assert.Contains("avio_open2(rotate)", error.Message);
            Assert.Same(error, Failure);
            Assert.Same(error, Assert.Single(FatalErrors));
            Assert.Equal(1, RotationAttempts);
            Assert.Equal(1L, TotalFailures);
            Assert.False(EncoderIsOpen);
            Assert.False(IsStarted);
            AssertQueuesCompleted();
        }

        public async ValueTask DisposeAsync()
        {
            List<Exception>? failures = null;
            if (_initialized)
            {
                CompleteQueues();
                if (_owner == null) _ = StartOwner();
                _cancellation.Cancel();
                try { await _owner!.WaitAsync(TimeSpan.FromSeconds(15)); }
                catch (Exception failure) { (failures ??= new()).Add(failure); }
            }
            // Never release native state while its owner could still be running.
            if (_owner is { IsCompleted: false })
                throw new AggregateException("Rotation fixture owner did not stop.", failures!);

            try
            {
                if (_sink != null) await ((IAsyncDisposable)_sink).DisposeAsync();
            }
            catch (Exception failure) { (failures ??= new()).Add(failure); }
            Attempt(() => (_manager as IDisposable)?.Dispose());
            Attempt(_cancellation.Dispose);
            Attempt(() => _directory.Delete(recursive: true));
            if (failures != null) throw new AggregateException("Rotation fixture cleanup failed.", failures);

            void Attempt(Action cleanup)
            {
                try { cleanup(); }
                catch (Exception failure) { (failures ??= new()).Add(failure); }
            }
        }
    }

    private static unsafe void AssertNativeAudioOptions(object encoder, string stream,
        int sampleRate, int channels, int bitRate, int streamIndex)
    {
        var state = GetField(encoder, stream)!;
        var codec = (AVCodecContext*)Pointer.Unbox(state.GetType().GetField("CodecCtx")!.GetValue(state)!);
        var nativeStream = (AVStream*)Pointer.Unbox(state.GetType().GetField("Stream")!.GetValue(state)!);
        Assert.True(codec != null);
        Assert.True(nativeStream != null);
        Assert.Equal(sampleRate, codec->sample_rate);
        Assert.Equal(channels, codec->ch_layout.nb_channels);
        Assert.Equal((long)bitRate, codec->bit_rate);
        Assert.Equal(1, codec->time_base.num);
        Assert.Equal(sampleRate, codec->time_base.den);
        Assert.Equal(streamIndex, nativeStream->index);
        Assert.Equal(1, nativeStream->time_base.num);
        Assert.Equal(sampleRate, nativeStream->time_base.den);
        var cachedTimeBase = (AVRational)state.GetType().GetField("CachedTimeBase")!.GetValue(state)!;
        Assert.Equal(1, cachedTimeBase.num);
        Assert.Equal(sampleRate, cachedTimeBase.den);
    }

    private static void AssertNativeResourcesReleased(object encoder)
    {
        foreach (var field in new[] { "_formatCtx", "_videoCodecCtx", "_videoStream", "_videoFrame", "_packet", "_bsfCtx" })
            AssertNullNativePointer(encoder, field);
        foreach (var stream in new[] { "_audio", "_mic" })
        {
            var state = GetField(encoder, stream)!;
            foreach (var field in new[] { "CodecCtx", "Stream", "Frame", "SwrCtx", "InputAccumulatorBuffer", "SampleQueueBuffer" })
                AssertNullNativePointer(state, field);
            Assert.Equal(0, (int)state.GetType().GetField("AccumulatorBytes")!.GetValue(state)!);
            Assert.Equal(0, (int)state.GetType().GetField("BufferedSamples")!.GetValue(state)!);
        }
    }

    private static unsafe void AssertNullNativePointer(object instance, string name)
    {
        var field = instance.GetType().GetField(name, PrivateInstance | BindingFlags.Public)!;
        Assert.Equal(IntPtr.Zero, (IntPtr)Pointer.Unbox(field.GetValue(instance)!));
    }

    private static unsafe void AssertOutputHasVideoAndBothAudioTracks(string path,
        int audioSampleRate, int audioChannels, int microphoneSampleRate, int microphoneChannels)
    {
        AVFormatContext* input = null;
        AVPacket* packet = null;
        try
        {
            Assert.True(ffmpeg.avformat_open_input(&input, path, null, null) >= 0);
            Assert.True(ffmpeg.avformat_find_stream_info(input, null) >= 0);
            var packetCounts = new int[checked((int)input->nb_streams)];
            Assert.Equal(3, packetCounts.Length);
            Assert.Equal(AVMediaType.AVMEDIA_TYPE_VIDEO, input->streams[0]->codecpar->codec_type);
            foreach (var (index, sampleRate, channels) in new[]
            {
                (1, audioSampleRate, audioChannels),
                (2, microphoneSampleRate, microphoneChannels)
            })
            {
                var parameters = input->streams[index]->codecpar;
                Assert.Equal(AVMediaType.AVMEDIA_TYPE_AUDIO, parameters->codec_type);
                Assert.Equal(AVCodecID.AV_CODEC_ID_AAC, parameters->codec_id);
                Assert.Equal(sampleRate, parameters->sample_rate);
                Assert.Equal(channels, parameters->ch_layout.nb_channels);
            }
            packet = ffmpeg.av_packet_alloc();
            Assert.True(packet != null);
            var readResult = 0;
            for (var count = 0; count < 10_000; count++)
            {
                readResult = ffmpeg.av_read_frame(input, packet);
                if (readResult < 0) break;
                Assert.InRange(packet->stream_index, 0, packetCounts.Length - 1);
                packetCounts[packet->stream_index]++;
                ffmpeg.av_packet_unref(packet);
            }
            Assert.Equal(ffmpeg.AVERROR_EOF, readResult);
            var videoTracks = 0;
            var audioTracks = 0;
            for (var index = 0; index < packetCounts.Length; index++)
            {
                Assert.True(packetCounts[index] > 0, $"Finalized stream {index} has no packets.");
                var codecType = input->streams[index]->codecpar->codec_type;
                if (codecType == AVMediaType.AVMEDIA_TYPE_VIDEO) videoTracks++;
                if (codecType == AVMediaType.AVMEDIA_TYPE_AUDIO) audioTracks++;
            }
            Assert.Equal(1, videoTracks);
            Assert.Equal(2, audioTracks);
        }
        finally
        {
            ffmpeg.av_packet_free(&packet);
            ffmpeg.avformat_close_input(&input);
        }
    }

    private static object CreateQueue(object sink, string name)
    {
        var packetType = sink.GetType().GetField(name, PrivateInstance)!.FieldType.GenericTypeArguments[0];
        var factory = typeof(Channel).GetMethods().Single(method =>
            method.Name == nameof(Channel.CreateUnbounded) && method.GetParameters().Length == 0);
        var queue = factory.MakeGenericMethod(packetType).Invoke(null, null)!;
        SetField(sink, name, queue);
        return queue;
    }

    private static object? Invoke(object target, string name, params object?[] arguments)
    {
        var method = target.GetType().GetMethods(PrivateInstance | BindingFlags.Public).Single(candidate =>
            candidate.Name == name && candidate.GetParameters().Length == arguments.Length);
        return InvokeMethod(method, target, arguments);
    }

    private static object? InvokeMethod(MethodInfo method, object target, object?[] arguments)
    {
        try { return method.Invoke(target, arguments); }
        catch (TargetInvocationException error) when (error.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static object? GetField(object instance, string name) => instance.GetType().GetField(name, PrivateInstance)!.GetValue(instance);
    private static void SetField(object instance, string name, object? value) => instance.GetType().GetField(name, PrivateInstance)!.SetValue(instance, value);
    private static void Set(object instance, string name, object value) => instance.GetType().GetProperty(name)!.SetValue(instance, value);
    private static T Read<T>(object instance, string name) => (T)instance.GetType().GetProperty(name)!.GetValue(instance)!;
}
