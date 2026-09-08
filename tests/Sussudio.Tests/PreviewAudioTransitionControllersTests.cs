using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Controllers;
using Sussudio.ViewModels;
using Xunit;

namespace Sussudio.Tests;

/// <summary>
/// Executes the linked production <see cref="PreviewAudioVolumeTransitionController"/> and
/// <see cref="AudioRampTraceRecorder"/> sources directly against passive model shapes in
/// PreviewAudioTransitionTestBoundaries.cs. Every ramp, clamp, writer admission and trace
/// decision under test lives in the production file.
/// </summary>
public sealed class PreviewAudioTransitionControllersTests
{
    private sealed class ControllerHarness : IDisposable
    {
        public double Volume { get; private set; }
        public List<double> VolumeWrites { get; } = new();
        public List<float> SessionVolumeWrites { get; } = new();
        public List<string> TraceKinds { get; } = new();
        public List<string> Logs { get; } = new();
        public List<(long SessionId, string Reason)> CompletedSessions { get; } = new();
        public PreviewAudioVolumeTransitionController Controller { get; }

        public ControllerHarness(double initialVolume, Func<int, CancellationToken, Task>? delay = null)
        {
            Volume = initialVolume;
            PreviewAudioVolumeTransitionController? controller = null;
            Controller = new(new PreviewAudioVolumeTransitionControllerContext
            {
                GetPreviewVolume = () => Volume,
                SetPreviewVolume = value =>
                {
                    Volume = value;
                    VolumeWrites.Add(value);
                    // Exercise the same reentrant observable hook as MainViewModel.
                    controller?.HandlePreviewVolumeChanged(value);
                },
                SetSessionPreviewVolume = value => SessionVolumeWrites.Add(value),
                BeginTraceSession = (_, _) => 7,
                CompleteTraceSession = (id, reason) => CompletedSessions.Add((id, reason)),
                RecordTracePoint = (kind, _, _, _, _) => TraceKinds.Add(kind),
                Log = (message, _) => Logs.Add(message),
                DelayAsync = delay ?? ((_, token) => { token.ThrowIfCancellationRequested(); return Task.CompletedTask; })
            });
            controller = Controller;
        }

        public void Dispose() => Controller.Dispose();
    }

    private sealed class PausedRampDelay
    {
        private int _calls;
        public TaskCompletionSource<bool> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Resume { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task DelayAsync(int milliseconds, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _calls) != 1) return Task.CompletedTask;
            Entered.TrySetResult(true);
            return Resume.Task.WaitAsync(cancellationToken);
        }
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(0.42, 0.42)]
    [InlineData(2.5, 1.0)]
    public void ExplicitUserVolumeOwnsTheClampedPersistedTarget(double requested, double expected)
    {
        using var h = new ControllerHarness(0.8);
        h.Controller.SetUserVolume(requested);
        Assert.Equal(expected, h.Controller.RequestedVolume, 6);
        Assert.Equal(expected, h.Volume, 6);
        Assert.Equal((float)expected, h.SessionVolumeWrites[^1], 5);
    }

    [Fact]
    public void PrimePublishesSilenceThroughTheRealPropertyHookWithoutChangingTheRequestedTarget()
    {
        using var h = new ControllerHarness(0.8);
        h.Controller.PrimeForAudioTransition("start");
        Assert.Equal(0.8, h.Controller.RequestedVolume, 6);
        Assert.Equal(0, h.Volume);
        Assert.Equal(0f, h.SessionVolumeWrites[^1]);
    }

    [Theory]
    [InlineData(0.3)]
    [InlineData(0.0)]
    public async Task UserInputAfterPrimeAndBeforeStartupResumesUsesTheLatestTarget(double requested)
    {
        using var h = new ControllerHarness(0.8);
        var operation = h.Controller.PrimeForAudioTransition("start");
        // Represents a request while the caller awaits backend startup/readiness.
        h.Controller.SetUserVolume(requested);
        Assert.Equal(requested, h.Controller.RequestedVolume, 6);
        Assert.Equal(0, h.Volume);
        await h.Controller.RampUpForAudioTransitionAsync(operation, "start");
        Assert.Equal(requested, h.Volume, 6);
        Assert.Equal(requested, h.Controller.RequestedVolume, 6);
        Assert.All(h.VolumeWrites, value => Assert.InRange(value, 0, requested));
    }

    [Fact]
    public async Task UserInputDuringRampUpSupersedesAllRemainingWritesAndCompletion()
    {
        var pause = new PausedRampDelay();
        using var h = new ControllerHarness(0.8, pause.DelayAsync);
        var operation = h.Controller.PrimeForAudioTransition("monitor_on");
        var ramp = h.Controller.RampUpForAudioTransitionAsync(operation, "monitor_on");
        await pause.Entered.Task;
        h.Controller.SetUserVolume(0.3);
        var writeCount = h.VolumeWrites.Count;
        Assert.Equal(0.3, h.Controller.RequestedVolume, 6);
        pause.Resume.TrySetResult(true);
        await ramp;
        Assert.Equal(0.3, h.Volume, 6);
        Assert.Equal(0.3f, h.SessionVolumeWrites[^1], 5);
        Assert.Equal(writeCount, h.VolumeWrites.Count);
        Assert.Contains(h.CompletedSessions, item => item.Reason == "monitor_on");
    }

    [Fact]
    public void SameValueUserRequestStillRevokesAnActiveWriter()
    {
        using var h = new ControllerHarness(0.8);
        var operation = h.Controller.PrimeForAudioTransition("start");
        var writer = h.Controller.BeginWriter(operation, muteOutput: false)!.Value;
        h.Controller.SetUserVolume(0);
        Assert.True(writer.CancellationToken.IsCancellationRequested);
        Assert.False(h.Controller.TryApplyTransient(writer, 0.8));
        Assert.False(h.Controller.TryCompleteWriter(writer));
        Assert.Equal(0, h.Controller.RequestedVolume);
        Assert.Equal(0, h.Volume);
    }

    [Fact]
    public async Task UserInputDuringRampDownPreservesTheRequestedLevelAndLetsTheBackendSequenceContinue()
    {
        var pause = new PausedRampDelay();
        using var h = new ControllerHarness(0.8, pause.DelayAsync);
        var operation = h.Controller.BeginTransition("input_change");
        var ramp = h.Controller.RampDownForAudioTransitionAsync(operation, "input_change");
        await pause.Entered.Task;
        h.Controller.SetUserVolume(0.3);
        pause.Resume.TrySetResult(true);
        await ramp;
        Assert.Equal(0, h.Volume);
        Assert.Equal(0.3, h.Controller.RequestedVolume, 6);
        // The input replacement finishes under the original operation identity.
        await h.Controller.RampUpForAudioTransitionAsync(operation, "input_change");
        Assert.Equal(0.3, h.Volume, 6);
    }

    [Fact]
    public void UnavailableAudioRestoresLatestRequestAndRejectsAnOlderOperation()
    {
        using var h = new ControllerHarness(0.8);
        var obsolete = h.Controller.PrimeForAudioTransition("old");
        var current = h.Controller.PrimeForAudioTransition("new");
        h.Controller.SetUserVolume(0.3);
        h.Controller.RestoreAfterUnavailableAudio(obsolete, "old");
        Assert.Equal(0, h.Volume);
        h.Controller.RestoreAfterUnavailableAudio(current, "new");
        Assert.Equal(0.3, h.Volume, 6);
        Assert.Equal(0.3, h.Controller.RequestedVolume, 6);
    }

    [Fact]
    public void ObsoleteWriterCompletionAndFinallyCannotClearANewerWriter()
    {
        using var h = new ControllerHarness(0.8);
        var oldOperation = h.Controller.BeginTransition("old");
        var oldWriter = h.Controller.BeginWriter(oldOperation, muteOutput: true)!.Value;
        var currentOperation = h.Controller.PrimeForAudioTransition("new");
        var currentWriter = h.Controller.BeginWriter(currentOperation, muteOutput: false)!.Value;
        Assert.True(h.Controller.TryApplyTransient(currentWriter, 0.2));
        h.Controller.EndWriter(oldWriter);
        Assert.False(h.Controller.TryCompleteWriter(oldWriter));
        Assert.True(h.Controller.TryApplyTransient(currentWriter, 0.4));
        Assert.True(h.Controller.TryCompleteWriter(currentWriter));
        Assert.Equal(0.8, h.Volume, 6);
    }

    [Fact]
    public async Task StoryboardStyleWriterCanSupersedeATaskRampWithoutOldFinallyPublishing()
    {
        var pause = new PausedRampDelay();
        using var h = new ControllerHarness(0.8, pause.DelayAsync);
        var first = h.Controller.PrimeForAudioTransition("task");
        var ramp = h.Controller.RampUpForAudioTransitionAsync(first, "task");
        await pause.Entered.Task;
        var second = h.Controller.BeginTransition("storyboard");
        var writer = h.Controller.BeginWriter(second, muteOutput: false)!.Value;
        Assert.True(h.Controller.TryApplyTransient(writer, 0.4));
        pause.Resume.TrySetResult(true);
        await ramp;
        Assert.Equal(0.4, h.Volume, 6);
        Assert.True(h.Controller.TryCompleteWriter(writer));
        Assert.Equal(0.8, h.Volume, 6);
    }

    [Fact]
    public async Task CallerCancellationStillPropagatesWithoutReplacingTheRequestedVolume()
    {
        var pause = new PausedRampDelay();
        using var h = new ControllerHarness(0.8, pause.DelayAsync);
        using var cancellation = new CancellationTokenSource();
        var operation = h.Controller.PrimeForAudioTransition("start");
        var ramp = h.Controller.RampUpForAudioTransitionAsync(operation, "start", cancellation.Token);
        await pause.Entered.Task;
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ramp);
        Assert.Equal(0.8, h.Controller.RequestedVolume, 6);
    }

    [Fact]
    public async Task CanceledStopRestoresLatestRequestAndReleasesTheMuteHold()
    {
        var pause = new PausedRampDelay();
        using var h = new ControllerHarness(0.8, pause.DelayAsync);
        using var cancellation = new CancellationTokenSource();
        var stop = h.Controller.RampDownForStopAsync(cancellation.Token);
        await pause.Entered.Task;
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stop);
        Assert.Equal(0.8, h.Volume, 6);
        h.Controller.SetUserVolume(0.3);
        Assert.Equal(0.3, h.Volume, 6);
    }

    [Fact]
    public async Task FailedStopRampRestoresTheRequestedLevel()
    {
        using var h = new ControllerHarness(0.8, (_, _) => Task.FromException(new InvalidOperationException("delay failure")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Controller.RampDownForStopAsync(CancellationToken.None));
        Assert.Equal(0.8, h.Volume, 6);
        Assert.Equal(0.8, h.Controller.RequestedVolume, 6);
        h.Controller.SetUserVolume(0.2);
        Assert.Equal(0.2, h.Volume, 6);
    }

    [Fact]
    public async Task DisposingDuringARampRejectsEveryLaterPublication()
    {
        var pause = new PausedRampDelay();
        using var h = new ControllerHarness(0.8, pause.DelayAsync);
        var operation = h.Controller.PrimeForAudioTransition("start");
        var ramp = h.Controller.RampUpForAudioTransitionAsync(operation, "start");
        await pause.Entered.Task;
        h.Controller.Dispose();
        var writeCount = h.VolumeWrites.Count;
        var sessionWriteCount = h.SessionVolumeWrites.Count;
        h.Controller.SetUserVolume(0.3);
        h.Controller.RestoreAfterUnavailableAudio(operation, "late");
        pause.Resume.TrySetResult(true);
        await ramp;
        Assert.Equal(writeCount, h.VolumeWrites.Count);
        Assert.Equal(sessionWriteCount, h.SessionVolumeWrites.Count);
        Assert.Null(h.Controller.BeginWriter(operation, muteOutput: false));
    }

    [Fact]
    public void DisposingBetweenPrimeAndResumeRejectsTheContinuation()
    {
        using var h = new ControllerHarness(0.8);
        var operation = h.Controller.PrimeForAudioTransition("start");
        h.Controller.Dispose();
        Assert.Null(h.Controller.BeginWriter(operation, muteOutput: false));
        Assert.Equal(0.8, h.Controller.RequestedVolume, 6);
        Assert.Equal(0, h.Volume);
    }

    [Fact]
    public async Task UninterruptedRampsKeepTheirEndpointsStepCountsAndTiming()
    {
        var delays = new List<int>();
        using var h = new ControllerHarness(0.62, (ms, _) => { delays.Add(ms); return Task.CompletedTask; });
        var operation = h.Controller.BeginTransition("input_change");
        await h.Controller.RampDownForAudioTransitionAsync(operation, "input_change");
        Assert.Equal(18, delays.Count);
        Assert.All(delays, delay => Assert.Equal(25, delay));
        Assert.Equal(0, h.Volume);
        Assert.Equal(0.62, h.Controller.RequestedVolume, 6);
        for (var i = 1; i < h.VolumeWrites.Count; i++) Assert.True(h.VolumeWrites[i] <= h.VolumeWrites[i - 1]);
        h.VolumeWrites.Clear();
        delays.Clear();
        await h.Controller.RampUpForAudioTransitionAsync(operation, "input_change");
        Assert.Equal(30, delays.Count);
        Assert.All(delays, delay => Assert.Equal(30, delay));
        Assert.Equal(0.62, h.Volume, 6);
        for (var i = 1; i < h.VolumeWrites.Count; i++) Assert.True(h.VolumeWrites[i] >= h.VolumeWrites[i - 1]);
    }

    [Fact]
    public async Task StopRampRetainsItsDiagnosticIdentity()
    {
        using var h = new ControllerHarness(0.5);
        await h.Controller.RampDownForStopAsync(CancellationToken.None);
        Assert.Contains(h.Logs, log => log.Contains("PREVIEW_AUDIO_STOP_RAMP_STARTED"));
        Assert.Contains(h.Logs, log => log.Contains("PREVIEW_AUDIO_STOP_RAMP_COMPLETED"));
        Assert.Equal(0.5, h.Controller.RequestedVolume, 6);
    }

    // ---- AudioRampTraceRecorder ---------------------------------------------

    private static AudioRampTraceRecorder CreateRecorder(
        Func<CaptureRuntimeSnapshot>? runtime = null,
        List<string>? logs = null)
        => new(new AudioRampTraceRecorderContext
        {
            GetRuntimeSnapshot = runtime ?? (() => new CaptureRuntimeSnapshot()),
            GetPreviewVolume = () => 0.5,
            GetIsAudioEnabled = () => true,
            GetIsAudioPreviewEnabled = () => true,
            GetAudioPeak = () => 0.25,
            Log = message => (logs ?? new List<string>()).Add(message)
        });

    [Fact]
    public void Recorder_StartsEmptyAndIdle()
    {
        var snapshot = CreateRecorder().GetSnapshot();

        Assert.Equal(0, snapshot.EntryCount);
        Assert.Empty(snapshot.Entries);
        Assert.False(snapshot.IsSamplingActive);
        Assert.Equal(2048, snapshot.Capacity);
    }

    [Fact]
    public void Recorder_DropsAnUnsolicitedVolumeSetWhileNoSessionIsSampling()
    {
        // Slider movement outside a transition is not forensically interesting and
        // would otherwise flush the ring before the next real investigation.
        var recorder = CreateRecorder();

        recorder.RecordPoint("volume-set");

        Assert.Equal(0, recorder.GetSnapshot().EntryCount);
    }

    [Fact]
    public void Recorder_KeepsAnyOtherKindEvenWithNoActiveSession()
    {
        var recorder = CreateRecorder();

        recorder.RecordPoint("primed", "device_change", 0.5);

        var snapshot = recorder.GetSnapshot();
        Assert.Equal(1, snapshot.EntryCount);
        Assert.Equal("primed", snapshot.Entries[0].Kind);
        Assert.Equal("device_change", snapshot.Entries[0].Reason);
    }

    [Fact]
    public void Recorder_ProjectsRuntimeEvidenceOntoTheEntry()
    {
        var recorder = CreateRecorder(() => new CaptureRuntimeSnapshot
        {
            WasapiPlaybackTargetVolumePercent = 80,
            WasapiPlaybackCurrentVolumePercent = 40,
            WasapiPlaybackOutputPeak = 0.9,
            WasapiPlaybackQueueDepth = 3,
            AudioFramesArrived = 1234,
            IsAudioPreviewActive = true
        });

        recorder.RecordPoint("primed");

        var entry = recorder.GetSnapshot().Entries[0];
        Assert.Equal(80, entry.PlaybackTargetVolumePercent);
        Assert.Equal(40, entry.PlaybackCurrentVolumePercent);
        Assert.Equal(0.9, entry.PlaybackOutputPeak);
        Assert.Equal(3, entry.PlaybackQueueDepth);
        Assert.Equal(1234, entry.AudioFramesArrived);
        Assert.True(entry.IsAudioPreviewActive);
        Assert.Equal(50, entry.PreviewVolumePercent, 6);
        Assert.Equal(0.25, entry.CaptureAudioPeak, 6);
    }

    [Fact]
    public void Recorder_ReportsAZeroOutputAgeWhenThePlaybackThreadHasNeverTicked()
    {
        var recorder = CreateRecorder(() => new CaptureRuntimeSnapshot
        {
            WasapiPlaybackOutputLevelLastTickMs = 0
        });

        recorder.RecordPoint("primed");

        Assert.Equal(0, recorder.GetSnapshot().Entries[0].PlaybackOutputAgeMs);
    }

    [Fact]
    public void Recorder_NeverReportsANegativeOutputAge()
    {
        // A tick stamped in the future (clock skew) must not surface as negative age.
        var recorder = CreateRecorder(() => new CaptureRuntimeSnapshot
        {
            WasapiPlaybackOutputLevelLastTickMs = Environment.TickCount64 + 100_000
        });

        recorder.RecordPoint("primed");

        Assert.Equal(0, recorder.GetSnapshot().Entries[0].PlaybackOutputAgeMs);
    }

    [Fact]
    public void Recorder_AssignsStrictlyIncreasingSequenceNumbers()
    {
        var recorder = CreateRecorder();

        for (var i = 0; i < 25; i++)
        {
            recorder.RecordPoint("primed");
        }

        var entries = recorder.GetSnapshot(maxEntries: 25).Entries;
        for (var i = 1; i < entries.Length; i++)
        {
            Assert.True(entries[i].Sequence > entries[i - 1].Sequence, $"sequence did not advance at {i}");
        }
    }

    [Fact]
    public void Recorder_RetainsTheNewestEntriesOnceTheRingWraps()
    {
        var recorder = CreateRecorder();
        const int Capacity = 2048;

        for (var i = 0; i < Capacity + 120; i++)
        {
            recorder.RecordPoint("primed", note: i.ToString());
        }

        var snapshot = recorder.GetSnapshot(maxEntries: Capacity);
        Assert.Equal(Capacity, snapshot.EntryCount);
        Assert.Equal(Capacity, snapshot.Entries.Length);
        Assert.Equal((Capacity + 120 - 1).ToString(), snapshot.Entries[^1].Note);
        Assert.Equal("120", snapshot.Entries[0].Note);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-5, 0)]
    [InlineData(3, 3)]
    [InlineData(9999, 40)]
    public void Recorder_ClampsTheRequestedSnapshotWindow(int requested, int expected)
    {
        var recorder = CreateRecorder();
        for (var i = 0; i < 40; i++)
        {
            recorder.RecordPoint("primed");
        }

        Assert.Equal(expected, recorder.GetSnapshot(requested).Entries.Length);
    }

    [Fact]
    public void Recorder_ReturnsTheMostRecentEntriesWhenTheWindowIsSmallerThanTheBuffer()
    {
        var recorder = CreateRecorder();
        for (var i = 0; i < 40; i++)
        {
            recorder.RecordPoint("primed", note: i.ToString());
        }

        var entries = recorder.GetSnapshot(maxEntries: 5).Entries;

        Assert.Equal(5, entries.Length);
        Assert.Equal("35", entries[0].Note);
        Assert.Equal("39", entries[^1].Note);
    }

    [Fact]
    public void Recorder_ReportsTheEntryCountSeparatelyFromTheReturnedWindow()
    {
        var recorder = CreateRecorder();
        for (var i = 0; i < 40; i++)
        {
            recorder.RecordPoint("primed");
        }

        var snapshot = recorder.GetSnapshot(maxEntries: 5);

        Assert.Equal(40, snapshot.EntryCount);
        Assert.Equal(5, snapshot.Entries.Length);
    }

    [Fact]
    public async Task Recorder_BeginSessionMarksSamplingActiveAndAdvancesTheSessionId()
    {
        var recorder = CreateRecorder();

        var first = recorder.BeginSession("device_change", 0.6);
        var snapshot = recorder.GetSnapshot();

        Assert.True(first > 0);
        Assert.True(snapshot.IsSamplingActive);
        Assert.Equal(first, snapshot.ActiveSessionId);
        Assert.Equal("device_change", snapshot.ActiveReason);
        Assert.Contains(snapshot.Entries, entry => entry.Kind == "session-start");

        var second = recorder.BeginSession("preview_stop", 0.2);
        Assert.True(second > first, "a superseding session must take a new id");

        recorder.CompleteSession(second, "preview_stop");
        await Task.Delay(50);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Recorder_TraceFaultRetiresSamplerAndAllowsRestart(int failingRead)
    {
        var reads = 0;
        var failure = new InvalidOperationException("runtime snapshot failed");
        var logs = new List<string>();
        using var recorder = CreateRecorder(
            () => Interlocked.Increment(ref reads) == failingRead ? throw failure : new CaptureRuntimeSnapshot(),
            logs);

        // Session-start and the first sample both run before BeginSession returns.
        if (failingRead == 1)
        {
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => recorder.BeginSession("failed", 0.5)));
        }
        else
        {
            recorder.BeginSession("failed", 0.5);
            Assert.Contains(logs, message => message.Contains("AUDIO_RAMP_TRACE_SAMPLER_FAIL"));
        }

        var failedSnapshot = recorder.GetSnapshot();
        Assert.False(failedSnapshot.IsSamplingActive);
        var replacement = recorder.BeginSession("replacement", 0.7);
        Assert.True(replacement > failedSnapshot.ActiveSessionId);
        Assert.True(recorder.GetSnapshot().IsSamplingActive);
        Assert.Equal(replacement, recorder.GetSnapshot().ActiveSessionId);

        recorder.Dispose();
        recorder.Dispose();
        Assert.False(recorder.GetSnapshot().IsSamplingActive);
        Assert.Equal(0, recorder.BeginSession("after-dispose", 0.5));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Recorder_ObsoleteTraceFaultCannotRetireReplacement(int failingRead)
    {
        var reads = 0;
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim(false);
        var failure = new InvalidOperationException("obsolete snapshot failed");
        using var recorder = CreateRecorder(() =>
        {
            if (Interlocked.Increment(ref reads) == failingRead)
            {
                entered.TrySetResult(true);
                if (!release.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException("The obsolete snapshot was not released.");
                }
                throw failure;
            }
            return new CaptureRuntimeSnapshot();
        });

        var olderBegin = Task.Run(() => recorder.BeginSession("older", 0.2));
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var olderSession = recorder.GetSnapshot().ActiveSessionId;
            var replacement = recorder.BeginSession("replacement", 0.8);
            Assert.True(replacement > olderSession);
            release.Set();

            if (failingRead == 1)
            {
                var observed = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await olderBegin.WaitAsync(TimeSpan.FromSeconds(5));
                });
                Assert.Same(failure, observed);
            }
            else
            {
                Assert.Equal(olderSession, await olderBegin.WaitAsync(TimeSpan.FromSeconds(5)));
            }

            var snapshot = recorder.GetSnapshot();
            Assert.True(snapshot.IsSamplingActive);
            Assert.Equal(replacement, snapshot.ActiveSessionId);
            Assert.Equal("replacement", snapshot.ActiveReason);
        }
        finally
        {
            release.Set();
            try { await olderBegin.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (InvalidOperationException) when (failingRead == 1) { }
        }
    }

    [Fact]
    public async Task Recorder_CompletionTraceFailureStillStopsSampler()
    {
        var failSynchronousSnapshot = new AsyncLocal<bool>();
        var failure = new InvalidOperationException("completion snapshot failed");
        using var recorder = CreateRecorder(
            () => failSynchronousSnapshot.Value ? throw failure : new CaptureRuntimeSnapshot());
        var session = recorder.BeginSession("completing", 0.6);

        // The sampler captured the normal context; only the synchronous completion fails.
        failSynchronousSnapshot.Value = true;
        try
        {
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => recorder.CompleteSession(session, "completing")));
        }
        finally
        {
            failSynchronousSnapshot.Value = false;
        }

        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (recorder.GetSnapshot().IsSamplingActive)
        {
            await Task.Delay(10, deadline.Token);
        }

        Assert.Equal(session, recorder.GetSnapshot().ActiveSessionId);
        var replacement = recorder.BeginSession("replacement", 0.8);
        Assert.True(replacement > session);
        Assert.True(recorder.GetSnapshot().IsSamplingActive);
    }

    [Fact]
    public async Task Recorder_ObsoleteDelayedCompletionCannotStopReplacement()
    {
        using var recorder = CreateRecorder();
        var olderSession = recorder.BeginSession("older", 0.2);
        var replacement = recorder.BeginSession("replacement", 0.8);
        var stopAfterDelay = typeof(AudioRampTraceRecorder).GetMethod(
            "StopSamplerAfterDelayAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Sampler completion method was not found.");

        // Use the existing delay parameter to exercise an obsolete timer after replacement.
        await (Task)stopAfterDelay.Invoke(recorder, new object[] { olderSession, 0 })!;
        var snapshot = recorder.GetSnapshot();
        Assert.True(snapshot.IsSamplingActive);
        Assert.Equal(replacement, snapshot.ActiveSessionId);

        await (Task)stopAfterDelay.Invoke(recorder, new object[] { replacement, 0 })!;
        Assert.False(recorder.GetSnapshot().IsSamplingActive);
    }

    [Fact]
    public async Task Recorder_DisposeDuringSamplerFaultRemainsSafe()
    {
        var reads = 0;
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim(false);
        using var recorder = CreateRecorder(() =>
        {
            if (Interlocked.Increment(ref reads) == 2)
            {
                entered.TrySetResult(true);
                if (!release.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException("The disposed sampler snapshot was not released.");
                }
                throw new InvalidOperationException("snapshot failed after disposal");
            }
            return new CaptureRuntimeSnapshot();
        });

        var begin = Task.Run(() => recorder.BeginSession("disposing", 0.6));
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            recorder.Dispose();
            recorder.Dispose();
            Assert.False(recorder.GetSnapshot().IsSamplingActive);
            Assert.Equal(0, recorder.BeginSession("after-dispose", 0.5));
            release.Set();
            await begin.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(recorder.GetSnapshot().IsSamplingActive);
        }
        finally
        {
            release.Set();
            await begin.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public void Recorder_IgnoresACompletionForANonSession()
    {
        var recorder = CreateRecorder();

        recorder.CompleteSession(0, "device_change");

        Assert.Equal(0, recorder.GetSnapshot().EntryCount);
    }

    [Fact]
    public async Task Recorder_RecordsACompletionPointForARealSession()
    {
        var recorder = CreateRecorder();
        var sessionId = recorder.BeginSession("device_change", 0.6);

        recorder.CompleteSession(sessionId, "device_change");

        Assert.Contains(recorder.GetSnapshot().Entries, entry => entry.Kind == "session-complete");
        await Task.Delay(50);
    }

    [Fact]
    public void Recorder_ClampsTheRecordedVolumePercentagesIntoRange()
    {
        var recorder = new AudioRampTraceRecorder(new AudioRampTraceRecorderContext
        {
            GetRuntimeSnapshot = () => new CaptureRuntimeSnapshot(),
            GetPreviewVolume = () => 4.0,
            GetIsAudioEnabled = () => true,
            GetIsAudioPreviewEnabled = () => false,
            GetAudioPeak = () => 0.1,
            Log = _ => { }
        });

        recorder.RecordPoint("primed", targetVolume: -3.0);

        var entry = recorder.GetSnapshot().Entries[0];
        Assert.Equal(100, entry.PreviewVolumePercent, 6);
        Assert.Equal(0, entry.TargetVolumePercent, 6);
    }
}
