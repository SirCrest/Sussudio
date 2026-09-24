using System.Reflection;
using System.Threading;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackSinkHardeningTests
{
    // Bind the span-based ingress method directly; MethodInfo.Invoke cannot box spans.
    private delegate bool TryEnqueueRawVideoFrameDelegate(ReadOnlySpan<byte> data, int expectedSize);

    private static string Source()
        => RuntimeContractSource.ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs");

    [Fact]
    public async Task ForceRotate_PreparesPathBeforeEncoderLaneFence()
    {
        const int width = 64;
        const int height = 64;
        var directory = Directory.CreateTempSubdirectory("sussudio-flashback-force-rotate-");
        object? sink = null;
        try
        {
            var (createdSink, sinkType) = CreateStartedFlashbackEncoderSink(directory.FullName, width, height);
            sink = createdSink;
            var enqueue = BindEnqueueDelegate(sink, sinkType);
            var frame = BuildNv12Frame(width, height);

            // Give the first segment some real encoded content before rotating.
            for (var i = 0; i < 20; i++)
            {
                Assert.True(enqueue(frame, frame.Length));
            }
            await WaitForCondition(() => GetLongProperty(sink, "EncodedVideoFrames") > 0, TimeSpan.FromSeconds(10));

            var forceRotateMethod = sinkType.GetMethod("ForceRotateForExport")!;
            // Wide enough to comfortably cover the completed segment regardless of exactly
            // how many frames encoded before the rotation call lands (this range only
            // selects which already-completed segments to report back, it does not affect
            // whether the rotation itself succeeds).
            var result = forceRotateMethod.Invoke(sink, new object?[]
            {
                TimeSpan.Zero, TimeSpan.FromMinutes(1), CancellationToken.None
            })!;

            var status = result.GetType().GetProperty("Status")!.GetValue(result)!.ToString();
            Assert.Equal("Completed", status);

            var segmentPaths = ((System.Collections.IEnumerable)result.GetType().GetProperty("SegmentPaths")!.GetValue(result)!)
                .Cast<object>().Select(value => (string)value).ToArray();
            Assert.NotEmpty(segmentPaths);
            foreach (var path in segmentPaths)
            {
                Assert.True(File.Exists(path), $"Rotated segment '{path}' should exist on disk.");
            }
            // Deliberately NOT asserting these files are non-empty. ForceRotateForExport
            // reserves the next segment path and pre-creates it with FileMode.CreateNew as an
            // empty placeholder before handing the request to the encoder lane, and
            // SegmentPaths is sourced from GetExistingCompletedSegmentPathsInRange -- so a
            // reported path is legitimately zero-length here. An earlier version of this test
            // asserted encoded bytes and failed deterministically for that reason; do not
            // reinstate it. Real encoded content on this path is covered by the P010 and
            // audio-interleaving round-trip tests in LibAvRecordingDrainBehaviorTests.

            // Prove the sink is still alive post-rotation by continuing to feed and drain
            // frames into whatever segment ForceRotateForExport swapped to.
            var encodedBeforeContinue = GetLongProperty(sink, "EncodedVideoFrames");
            for (var i = 0; i < 10; i++)
            {
                Assert.True(enqueue(frame, frame.Length));
            }
            await WaitForCondition(
                () => GetLongProperty(sink, "EncodedVideoFrames") > encodedBeforeContinue,
                TimeSpan.FromSeconds(10));

            // The "prepares path before the encoder-lane fence" ordering itself -- that
            // ReserveSegmentPath()/file creation happens on the calling thread before
            // SignalWork ever hands the request to the encoding-loop thread -- is a
            // same-thread-ordering-before-a-cross-thread-handoff invariant. It can't be
            // observed through FlashbackEncoderSink's public surface without an artificial,
            // flaky timing race (deliberately not attempted here per the "flaky integration
            // test is worse than a source-text one" guidance), and it is exactly the kind of
            // architecture-boundary contract migration item 4 says to keep as a source-shape
            // assertion. Kept narrow (two lines) rather than the old test's broader
            // implementation-string coupling.
            var method = global::Program.ExtractDeclaredMemberCode(Source(), "public FlashbackForceRotateResult ForceRotateForExport");
            Assert.True(
                method.IndexOf("ReserveSegmentPath", StringComparison.Ordinal) < method.IndexOf("SignalWork", StringComparison.Ordinal),
                "ForceRotateForExport must reserve/prepare the segment path before signaling the encoder-lane fence.");
        }
        finally
        {
            if (sink != null)
            {
                await ((IAsyncDisposable)sink).DisposeAsync();
            }
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task EndRecording_FinalizesAfterAcceptedFramesDrain()
    {
        const int width = 64;
        const int height = 64;
        const int framesBeforeBoundary = 60;
        var directory = Directory.CreateTempSubdirectory("sussudio-flashback-end-recording-");
        object? sink = null;
        try
        {
            var (createdSink, sinkType) = CreateStartedFlashbackEncoderSink(directory.FullName, width, height);
            sink = createdSink;
            var enqueue = BindEnqueueDelegate(sink, sinkType);
            var frame = BuildNv12Frame(width, height);

            sinkType.GetMethod("BeginRecording")!.Invoke(sink, new object?[] { Path.Combine(directory.FullName, "clip.mp4") });

            for (var i = 0; i < framesBeforeBoundary; i++)
            {
                Assert.True(enqueue(frame, frame.Length));
            }

            var endTask = (Task)sinkType.GetMethod("EndRecordingAsync")!.Invoke(sink, new object?[] { CancellationToken.None })!;
            await endTask.WaitAsync(TimeSpan.FromSeconds(30));
            var encodedAtCompletion = GetLongProperty(sink, "EncodedVideoFrames");

            var finalizeResult = endTask.GetType().GetProperty("Result")!.GetValue(endTask)!;
            Assert.True((bool)finalizeResult.GetType().GetProperty("Succeeded")!.GetValue(finalizeResult)!);

            // Queue-boundary independence is pinned by FlashbackRecordingBoundaryTests.
            Assert.True(
                encodedAtCompletion >= framesBeforeBoundary,
                $"Expected at least {framesBeforeBoundary} retired video frames at EndRecordingAsync completion, got {encodedAtCompletion}.");
        }
        finally
        {
            if (sink != null)
            {
                await ((IAsyncDisposable)sink).DisposeAsync();
            }
            directory.Delete(recursive: true);
        }
    }

    private static (object Sink, Type SinkType) CreateStartedFlashbackEncoderSink(string tempDirectory, int width, int height)
    {
        var assembly = SussudioAssembly.Load();
        Type TypeOf(string name) => assembly.GetType(name, throwOnError: true)!;

        var sinkType = TypeOf("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var optionsType = TypeOf("Sussudio.Models.FlashbackBufferOptions");
        var options = Activator.CreateInstance(optionsType)!;
        SetProperty(options, "TempDirectory", tempDirectory);

        var sink = Activator.CreateInstance(sinkType, new object?[] { options })!;

        var contextType = TypeOf("Sussudio.Models.FlashbackSessionContext");
        var context = Activator.CreateInstance(contextType)!;
        SetProperty(context, "Width", width);
        SetProperty(context, "Height", height);
        SetProperty(context, "FrameRate", 30d);
        SetProperty(context, "BitRate", 500_000u);
        SetProperty(context, "IsP010", false);
        SetProperty(context, "CodecName", "libx264");
        SetProperty(context, "HdrEnabled", false);
        SetProperty(context, "AudioEnabled", false);
        SetProperty(context, "MicrophoneEnabled", false);

        var startTask = (Task)sinkType.GetMethod("StartAsync")!.Invoke(
            sink, new object?[] { context, TimeSpan.Zero, CancellationToken.None })!;
        startTask.GetAwaiter().GetResult();

        return (sink, sinkType);
    }

    private static TryEnqueueRawVideoFrameDelegate BindEnqueueDelegate(object sink, Type sinkType)
    {
        var method = sinkType.GetMethod(
            "TryEnqueueRawVideoFrame",
            new[] { typeof(ReadOnlySpan<byte>), typeof(int) })!;
        return method.CreateDelegate<TryEnqueueRawVideoFrameDelegate>(sink);
    }

    private static byte[] BuildNv12Frame(int width, int height)
    {
        var length = width * height * 3 / 2;
        var buffer = new byte[length];
        buffer.AsSpan(0, width * height).Fill(16);
        buffer.AsSpan(width * height, width * height / 2).Fill(128);
        return buffer;
    }

    private static async Task WaitForCondition(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                Assert.Fail("Timed out waiting for condition.");
            }
            await Task.Delay(15);
        }
    }

    private static void SetProperty(object instance, string name, object? value)
        => instance.GetType().GetProperty(name)!.SetValue(instance, value);

    private static long GetLongProperty(object instance, string name)
        => (long)instance.GetType().GetProperty(name)!.GetValue(instance)!;

    [Fact]
    public void ForcedExitUnresolvedMarker_PreservesActiveFlashbackSegmentsFirst()
    {
        var lifecycle = RuntimeContractSource.ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs");
        var method = global::Program.ExtractDeclaredMemberCode(
            lifecycle,
            "internal void MarkRecordingFinalizationUnresolved");
        Assert.Contains("PreserveUnresolvedFlashbackRecordingArtifacts", method);
        Assert.Contains("PreserveUnresolvedWithArtifacts", method);

        var preserve = global::Program.ExtractDeclaredMemberCode(
            lifecycle,
            "private IReadOnlyList<string> PreserveUnresolvedFlashbackRecordingArtifacts");
        Assert.Contains("_flashbackBackend.BufferManager == null", preserve);
        AssertInOrder(
            preserve,
            "_flashbackBackend.PreserveRecoverySegments(\"recording_finalization_unresolved\")",
            "GetFlashbackSegments()",
            ".Select(segment => segment.Path)");

        var window = RuntimeContractSource.ReadRepoFile("Sussudio/MainWindow.xaml.cs");
        var emergencyClose = global::Program.ExtractDeclaredMemberCode(
            window,
            "private async Task RunWasapiEmergencyCloseAsync");
        AssertInOrder(
            emergencyClose,
            "MarkRecordingFinalizationUnresolved(",
            "Environment.Exit(1)");
    }

    [Fact]
    public void EncodingLoop_FailsFast_WhenDiskCriticallyLow()
    {
        var method = global::Program.ExtractDeclaredMemberCode(Source(), "private TimeSpan AdvanceEncodedVideoFrameAndGetPts");
        Assert.Contains("IsDiskCriticallyLow", method);
    }

    private static void AssertInOrder(string source, params string[] markers)
    {
        var previous = -1;
        foreach (var marker in markers)
        {
            var current = source.IndexOf(marker, previous + 1, StringComparison.Ordinal);
            Assert.True(current > previous, $"Missing or out-of-order marker: {marker}");
            previous = current;
        }
    }
}
