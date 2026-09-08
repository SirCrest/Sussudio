using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

public sealed class CaptureObservationTests
{
    [Fact]
    public async Task SnapshotReads_DoNotChooseBaselineOrAdvanceDerivativeWindow()
    {
        await using var frequent = new CaptureObservationTestSession();
        await using var sparse = new CaptureObservationTestSession();
        for (var i = 0; i < 50; i++) frequent.AssertSnapshots(null, null);
        Assert.Null(frequent.PublishedSample);

        frequent.Sample(0);
        sparse.Sample(0);
        frequent.AssertSnapshots(0, 0);
        frequent.SetCounters(180, 72_024);
        sparse.SetCounters(180, 72_024);
        // Counter changes cannot affect the publication until the worker samples.
        var initialSample = frequent.PublishedSample;
        for (var i = 0; i < 50; i++) frequent.AssertSnapshots(0, 0);
        Assert.Same(initialSample, frequent.PublishedSample);
        frequent.Sample(500);
        sparse.Sample(500);
        frequent.AssertSnapshots(0.5, 0);

        frequent.SetCounters(720, 288_048);
        sparse.SetCounters(720, 288_048);
        for (var i = 0; i < 50; i++) frequent.AssertSnapshots(0.5, 0);
        frequent.Sample(5_000);
        sparse.Sample(5_000);
        frequent.AssertSnapshots(1, 0.2);
        sparse.AssertSnapshots(1, 0.2);
    }

    [Fact]
    public async Task ProducerReplacementAndMissingAudio_ResetAllDriftState()
    {
        await using var session = new CaptureObservationTestSession();
        session.Sample(0);
        session.SetCounters(720, 288_048);
        session.Sample(5_000);
        session.AssertSnapshots(1, 0.2);

        session.ReplaceVideo();
        session.AssertSnapshots(null, null);
        session.SetCounters(120, 48_000);
        session.Sample(6_000);
        session.AssertSnapshots(0, 0);
        session.ReplaceAudio();
        session.AssertSnapshots(null, null);
        session.SetCounters(240, 96_024);
        session.Sample(7_000);
        session.AssertSnapshots(0, 0);

        session.SetAudioPresent(false);
        session.Sample(8_000);
        session.AssertSnapshots(null, null);
        session.SetAudioPresent(true);
        session.SetCounters(360, 144_048);
        session.Sample(9_000);
        session.AssertSnapshots(0, 0);
    }

    [Fact]
    public async Task CounterRollbackRateChangeAndStaleGeneration_CannotReuseOldBaseline()
    {
        await using var session = new CaptureObservationTestSession();
        session.Sample(0);
        session.SetCounters(720, 288_048);
        session.Sample(5_000);
        session.AssertSnapshots(1, 0.2);
        session.SetCounters(120, 48_000);
        session.Sample(6_000);
        session.AssertSnapshots(0, 0);
        session.SetCounters(240, 192_048);
        CaptureObservationTestSession.SetField(session.Video, "_fps", 60d);
        session.Sample(7_000);
        session.AssertSnapshots(0, 0);

        var oldGeneration = session.Generation;
        CaptureObservationTestSession.SetField(session.Service, "_telemetryPollGeneration", oldGeneration + 1);
        CaptureObservationTestSession.Invoke(session.Service, "ResetAvSyncDriftBaseline");
        Assert.False((bool)CaptureObservationTestSession.Invoke(session.Service, "SampleAvSyncDrift", oldGeneration, 8_000L));
        session.AssertSnapshots(null, null);
        session.Sample(8_000);
        session.AssertSnapshots(0, 0);
    }

    [Fact]
    public async Task RetainedSource_KeepsLocalSamplingWhenNativePollingStops()
    {
        await using var session = new CaptureObservationTestSession();
        CaptureObservationTestSession.Invoke(session.Service, "StartTelemetryPoll");
        await (Task)CaptureObservationTestSession.Invoke(session.Service, "StopSourceTelemetryPollingAsync");
        await session.AwaitDeferredSamplerStartAsync();
        Assert.False((bool)CaptureObservationTestSession.GetField(session.Service, "_sourceTelemetryPollingRequested")!);
        Assert.NotNull(CaptureObservationTestSession.GetField(session.Service, "_telemetryPollTask"));
        session.Sample(0);
        session.AssertSnapshots(0, 0);
        session.SetVideoPresent(false);
        Assert.False((bool)CaptureObservationTestSession.Invoke(session.Service, "SampleAvSyncDrift", session.Generation, 1_000L));
        session.AssertSnapshots(null, null);
    }

    [Fact]
    public async Task NewSourceDemand_DuringWorkerRetirement_StartsReplacementWorker()
    {
        await using var session = new CaptureObservationTestSession();
        session.SetVideoPresent(false);
        using var retiringCancellation = new CancellationTokenSource();
        var retiringTask = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CaptureObservationTestSession.SetField(session.Service, "_telemetryPollCts", retiringCancellation);
        CaptureObservationTestSession.SetField(session.Service, "_telemetryPollTask", retiringTask.Task);
        try
        {
            // Hold the old worker between its no-demand decision and task exit.
            Assert.False((bool)CaptureObservationTestSession.Invoke(session.Service, "SampleAvSyncDrift", session.Generation, 0L));
            Assert.True(retiringCancellation.IsCancellationRequested);
            session.SetVideoPresent(true);
            CaptureObservationTestSession.Invoke(session.Service, "EnsureCaptureTelemetrySampling");
            Assert.NotSame(retiringTask.Task, CaptureObservationTestSession.GetField(session.Service, "_telemetryPollTask"));
        }
        finally
        {
            retiringTask.TrySetResult();
        }
        await session.AwaitDeferredSamplerStartAsync();
        Assert.False((bool)CaptureObservationTestSession.GetField(session.Service, "_sourceTelemetryPollingRequested")!);
        session.Sample(0);
        session.AssertSnapshots(0, 0);
    }
    [Fact]
    public async Task ObservedFormat_ComesFromFrameCallbackAndFollowsSourceLifetime()
    {
        await using var session = new CaptureObservationTestSession();
        CaptureObservationTestSession.SetField(session.Service, "_actualPixelFormat", "MJPG");
        session.AssertObservedFormat(null, 0, 0, 0);
        var oldSource = session.Video;
        var callbacks = 0;
        CaptureObservationTestSession.Invoke(oldSource, "SetPixelFormatDetectedCallback", new Action<string>(_ => callbacks++));
        CaptureObservationTestSession.EmitMjpegFrame(oldSource);
        CaptureObservationTestSession.EmitMjpegFrame(oldSource);
        session.AssertObservedFormat("NV12", 0, 1, 0);
        Assert.Equal(1, callbacks);
        Assert.Equal("MJPG", CaptureObservationTestSession.GetProperty(session.RuntimeSnapshot(), "ReaderSourceSubtype"));

        // Recording configuration can change while the same preview source remains alive.
        CaptureObservationTestSession.SetField(session.Service, "_actualPixelFormat", "P010");
        session.AssertObservedFormat("NV12", 0, 1, 0);
        session.ReplaceVideo();
        session.AssertObservedFormat(null, 0, 0, 0);
        CaptureObservationTestSession.EmitMjpegFrame(oldSource);
        session.AssertObservedFormat(null, 0, 0, 0);
        CaptureObservationTestSession.Invoke(session.Video, "FirePixelFormatObserverOnce", "P010");
        session.AssertObservedFormat("P010", 1, 0, 0);
    }

    [Theory]
    [InlineData("NV12")]
    [InlineData("P010")]
    public async Task ConfiguredAndNegotiatedFormats_AreNotObservedEvidence(string configuredFormat)
    {
        await using var session = new CaptureObservationTestSession();
        CaptureObservationTestSession.SetField(session.Service, "_actualPixelFormat", configuredFormat);
        var context = CaptureObservationTestSession.Create("Sussudio.Services.Contracts.RecordingContext");
        CaptureObservationTestSession.SetProperty(context, "Settings", CaptureObservationTestSession.Create("Sussudio.Models.CaptureSettings"));
        CaptureObservationTestSession.SetProperty(context, "HdrPipelineActive", configuredFormat == "P010");
        var backend = CaptureObservationTestSession.GetField(session.Service, "_recordingBackend")!;
        CaptureObservationTestSession.SetProperty(backend, "Context", context);
        session.AssertObservedFormat(null, 0, 0, 0);
    }

    [Theory]
    [InlineData("NV12")]
    [InlineData("P010")]
    public void HdrVerdict_WithoutObservedSamples_RemainsInconclusive(string negotiatedFormat)
    {
        var runtime = CaptureObservationTestSession.Create("Sussudio.Models.CaptureRuntimeSnapshot");
        CaptureObservationTestSession.SetProperty(runtime, "NegotiatedPixelFormat", negotiatedFormat);
        CaptureObservationTestSession.SetProperty(runtime, "SourceIsHdr", true);
        var method = Sussudio.Tests.SussudioAssembly.Load().GetType("Sussudio.Services.Automation.AutomationDiagnosticsHub", true)!
            .GetMethod("BuildHdrTruthVerdict", BindingFlags.Static | BindingFlags.NonPublic)!;
        var verdict = method.Invoke(null, new object?[] { runtime, true, null })!;
        Assert.Equal("unknown", CaptureObservationTestSession.GetProperty(verdict, "PipelineFormat"));
        Assert.Equal("unknown", CaptureObservationTestSession.GetProperty(verdict, "EffectiveBitDepth"));
        Assert.Equal("unknown", CaptureObservationTestSession.GetProperty(verdict, "SourceVsCaptureParity"));
        Assert.Equal("inconclusive", CaptureObservationTestSession.GetProperty(verdict, "FinalClassification"));
        Assert.DoesNotContain((IEnumerable<string>)CaptureObservationTestSession.GetProperty(verdict, "Evidence")!,
            evidence => evidence.StartsWith("observed-", StringComparison.Ordinal));
    }
}

// Uses real managed constructors and actual frame ingress; no device initialization is needed.
internal sealed class CaptureObservationTestSession : IAsyncDisposable
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<IAsyncDisposable> _producers = new();
    public object Service { get; } = Create("Sussudio.Services.Capture.CaptureService");
    public object Video { get; private set; } = null!;
    public object Audio { get; private set; } = null!;
    public object? PublishedSample => GetField(Service, "_latestAvSyncDriftSample");
    public long Generation => (long)GetField(Service, "_telemetryPollGeneration")!;

    public CaptureObservationTestSession()
    {
        ReplaceVideo();
        ReplaceAudio();
        SetCounters(120, 48_000);
    }

    public void ReplaceVideo()
    {
        Video = Create("Sussudio.Services.Capture.UnifiedVideoCapture");
        _producers.Add((IAsyncDisposable)Video);
        SetField(Video, "_fps", 120d);
        SetVideoPresent(true);
    }

    public void ReplaceAudio()
    {
        Audio = Create("Sussudio.Services.Audio.WasapiAudioCapture");
        _producers.Add((IAsyncDisposable)Audio);
        SetAudioPresent(true);
    }

    public void SetVideoPresent(bool present)
        => SetProperty(GetField(Service, "_videoPipeline")!, "Capture", present ? Video : null);

    public void SetAudioPresent(bool present)
        => SetField(GetField(Service, "_previewAudioGraph")!, "ProgramCapture", present ? Audio : null);

    public void SetCounters(long videoFrames, long audioFrames)
    {
        SetField(Video, "_videoFramesArrived", videoFrames);
        SetField(Audio, "_audioFramesArrived", audioFrames);
    }

    public async Task AwaitDeferredSamplerStartAsync()
    {
        Task? deferredStart;
        lock (GetField(Service, "_telemetryPollSync")!)
        {
            deferredStart = GetField(Service, "_telemetryPollCts") == null
                ? (Task?)GetField(Service, "_telemetryPollTask")
                : null;
        }
        if (deferredStart != null) await deferredStart.WaitAsync(TimeSpan.FromSeconds(5));
    }
    public void Sample(long tick) => Invoke(Service, "SampleAvSyncDrift", Generation, tick);
    public object RuntimeSnapshot() => Invoke(Service, "GetRuntimeSnapshot");

    public void AssertSnapshots(double? drift, double? rate)
    {
        foreach (var snapshot in new[] { RuntimeSnapshot(), Invoke(Service, "GetHealthSnapshot") })
        {
            AssertMetric(drift, GetProperty(snapshot, "AvSyncCaptureDriftMs"));
            AssertMetric(rate, GetProperty(snapshot, "AvSyncCaptureDriftRateMsPerSec"));
        }
    }

    public void AssertObservedFormat(string? format, long p010, long nv12, long other)
    {
        var snapshot = RuntimeSnapshot();
        Assert.Equal(format, GetProperty(snapshot, "FirstObservedFramePixelFormat"));
        Assert.Equal(format, GetProperty(snapshot, "LatestObservedFramePixelFormat"));
        Assert.Equal(format, GetProperty(snapshot, "LatestObservedSurfaceFormat"));
        Assert.Equal(p010, GetProperty(snapshot, "ObservedP010FrameCount"));
        Assert.Equal(nv12, GetProperty(snapshot, "ObservedNv12FrameCount"));
        Assert.Equal(other, GetProperty(snapshot, "ObservedOtherFrameCount"));
    }

    private static void AssertMetric(double? expected, object? actual)
    {
        if (expected == null) Assert.Null(actual);
        else Assert.Equal(expected.Value, Assert.IsType<double>(actual), precision: 7);
    }

    public static object Create(string typeName)
        => Activator.CreateInstance(Sussudio.Tests.SussudioAssembly.Load().GetType(typeName, true)!, nonPublic: true)!;
    public static object Invoke(object target, string method, params object?[] arguments)
        => target.GetType().GetMethod(method, InstanceFlags)!.Invoke(target, arguments)!;
    public static object? GetField(object target, string name)
        => target.GetType().GetField(name, InstanceFlags)!.GetValue(target);
    public static void SetField(object target, string name, object? value)
        => target.GetType().GetField(name, InstanceFlags)!.SetValue(target, value);
    public static object? GetProperty(object target, string name)
        => target.GetType().GetProperty(name, InstanceFlags)!.GetValue(target);
    public static void SetProperty(object target, string name, object? value)
        => target.GetType().GetProperty(name, InstanceFlags)!.SetValue(target, value);

    public static void EmitMjpegFrame(object source)
    {
        var assembly = Sussudio.Tests.SussudioAssembly.Load();
        var format = Enum.Parse(assembly.GetType("Sussudio.Services.Contracts.PooledVideoPixelFormat", true)!, "Nv12");
        var frame = assembly.GetType("Sussudio.Services.Contracts.PooledVideoFrame", true)!
            .GetMethod("Rent", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, new object[] { 0L, 0L, 0L, 2, 2, format, 6 })!;
        using ((IDisposable)frame) Invoke(source, "OnMjpegPipelineFrameEmitted", frame);
    }

    public async ValueTask DisposeAsync()
    {
        SetVideoPresent(false);
        SetAudioPresent(false);
        SetProperty(GetField(Service, "_recordingBackend")!, "Context", null);
        await ((IAsyncDisposable)Service).DisposeAsync();
        foreach (var producer in _producers) await producer.DisposeAsync();
    }
}
