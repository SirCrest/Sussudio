using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.ViewModels;
using Xunit;

namespace Sussudio.Tests;

/// <summary>
/// Executes the linked production <see cref="PreviewAudioVolumeTransitionController"/> and
/// <see cref="AudioRampTraceRecorder"/> sources directly against passive model shapes in
/// PreviewAudioTransitionTestBoundaries.cs. Every ramp, clamp, suppression and trace
/// decision under test lives in the production file.
/// </summary>
public sealed class PreviewAudioTransitionControllersTests
{
    private sealed record TracePoint(string Kind, string? Reason, double? TargetVolume, string? Note, long? SessionId);

    private sealed class ControllerHarness
    {
        public double Volume { get; private set; }

        public List<double> VolumeWrites { get; } = new();

        public List<float> SessionVolumeWrites { get; } = new();

        public List<TracePoint> TracePoints { get; } = new();

        public List<string> Logs { get; } = new();

        public List<(long SessionId, string Reason)> CompletedSessions { get; } = new();

        public List<(string Reason, double Target)> BegunSessions { get; } = new();

        public long NextSessionId { get; set; } = 7;

        public PreviewAudioVolumeTransitionController Controller { get; }

        public ControllerHarness(double initialVolume)
        {
            Volume = initialVolume;
            Controller = new PreviewAudioVolumeTransitionController(
                new PreviewAudioVolumeTransitionControllerContext
                {
                    GetPreviewVolume = () => Volume,
                    SetPreviewVolume = value =>
                    {
                        Volume = value;
                        VolumeWrites.Add(value);
                    },
                    SetSessionPreviewVolume = value => SessionVolumeWrites.Add(value),
                    BeginTraceSession = (reason, target) =>
                    {
                        BegunSessions.Add((reason, target));
                        return NextSessionId;
                    },
                    CompleteTraceSession = (id, reason) => CompletedSessions.Add((id, reason)),
                    RecordTracePoint = (kind, reason, target, note, sessionId) =>
                        TracePoints.Add(new TracePoint(kind, reason, target, note, sessionId)),
                    Log = (message, _) => Logs.Add(message)
                });
        }

        public bool HasTrace(string kind) => TracePoints.Exists(point => point.Kind == kind);
    }

    // ---- PersistedVolumeTarget ----------------------------------------------

    [Fact]
    public void PersistedVolumeTarget_UsesTheLiveVolumeWhenNoOverrideIsHeld()
    {
        var harness = new ControllerHarness(0.42);

        Assert.Equal(0.42, harness.Controller.PersistedVolumeTarget, 6);
    }

    [Fact]
    public void PersistedVolumeTarget_PrefersTheOverrideSoAMidRampReadDoesNotPersistZero()
    {
        // During a ramp the live volume is being driven to zero; persisting that
        // would silently reset the user's monitoring level.
        var harness = new ControllerHarness(0.0) { };
        harness.Controller.VolumeSaveOverride = 0.8;

        Assert.Equal(0.8, harness.Controller.PersistedVolumeTarget, 6);
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(2.5, 1.0)]
    public void PersistedVolumeTarget_ClampsIntoTheUnitRange(double stored, double expected)
    {
        var harness = new ControllerHarness(0.5);
        harness.Controller.VolumeSaveOverride = stored;

        Assert.Equal(expected, harness.Controller.PersistedVolumeTarget, 6);
    }

    // ---- HandlePreviewVolumeChanged -----------------------------------------

    [Fact]
    public void HandlePreviewVolumeChanged_ClearsTheOverrideWhenTheUserMovesTheSlider()
    {
        var harness = new ControllerHarness(0.5);
        harness.Controller.VolumeSaveOverride = 0.9;

        harness.Controller.HandlePreviewVolumeChanged(0.3);

        Assert.Null(harness.Controller.VolumeSaveOverride);
        Assert.Equal(0.3f, Assert.Single(harness.SessionVolumeWrites), 5);
        Assert.True(harness.HasTrace("volume-set"));
    }

    [Fact]
    public void HandlePreviewVolumeChanged_KeepsTheOverrideWhileSavesAreSuppressed()
    {
        // A ramp writes the volume many times; those writes must not be mistaken
        // for the user choosing a new level.
        var harness = new ControllerHarness(0.5);
        harness.Controller.VolumeSaveOverride = 0.9;
        harness.Controller.SuppressVolumeSave = true;

        harness.Controller.HandlePreviewVolumeChanged(0.0);

        Assert.Equal(0.9, harness.Controller.VolumeSaveOverride);
    }

    [Theory]
    [InlineData(-0.5, 0f)]
    [InlineData(1.7, 1f)]
    public void HandlePreviewVolumeChanged_ClampsWhatItPushesToTheAudioSession(double value, float expected)
    {
        var harness = new ControllerHarness(0.5);

        harness.Controller.HandlePreviewVolumeChanged(value);

        Assert.Equal(expected, Assert.Single(harness.SessionVolumeWrites));
    }

    // ---- PrimeForAudioTransition --------------------------------------------

    [Fact]
    public void PrimeForAudioTransition_MutesAndRemembersTheTargetSoItCanBeRestored()
    {
        var harness = new ControllerHarness(0.6);

        var target = harness.Controller.PrimeForAudioTransition("device_change");

        Assert.Equal(0.6, target, 6);
        Assert.Equal(0, harness.Volume);
        Assert.Equal(0.6, harness.Controller.VolumeSaveOverride);
        Assert.False(harness.Controller.SuppressVolumeSave, "suppression must not leak past the prime");
        Assert.True(harness.HasTrace("primed"));
        Assert.Contains(harness.Logs, log => log.Contains("PREVIEW_AUDIO_MONITOR_PRIMED"));
    }

    [Fact]
    public void PrimeForAudioTransition_HoldsNoOverrideWhenMonitoringWasAlreadySilent()
    {
        // Nothing was audible, so there is nothing to restore; keeping an override
        // would un-mute audio the user had deliberately turned off.
        var harness = new ControllerHarness(0.0);

        var target = harness.Controller.PrimeForAudioTransition("device_change");

        Assert.Equal(0, target);
        Assert.Null(harness.Controller.VolumeSaveOverride);
        Assert.False(harness.HasTrace("primed"));
    }

    // ---- RestoreAfterUnavailableAudio ---------------------------------------

    [Fact]
    public void RestoreAfterUnavailableAudio_RestoresTheLevelAndDropsTheOverride()
    {
        var harness = new ControllerHarness(0.0);
        harness.Controller.VolumeSaveOverride = 0.7;

        harness.Controller.RestoreAfterUnavailableAudio(0.7, "no_audio");

        Assert.Equal(0.7, harness.Volume, 6);
        Assert.Null(harness.Controller.VolumeSaveOverride);
        Assert.False(harness.Controller.SuppressVolumeSave);
        Assert.Contains(harness.TracePoints, point => point.Kind == "restore" && point.Note == "audio-preview-unavailable");
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(3.0, 1.0)]
    public void RestoreAfterUnavailableAudio_ClampsTheRestoredLevel(double requested, double expected)
    {
        var harness = new ControllerHarness(0.0);

        harness.Controller.RestoreAfterUnavailableAudio(requested, "no_audio");

        Assert.Equal(expected, harness.Volume, 6);
    }

    // ---- RampDownForAudioTransitionAsync ------------------------------------

    [Fact]
    public async Task RampDown_EndsAtSilenceAndNeverRaisesTheVolumeOnTheWayDown()
    {
        var harness = new ControllerHarness(0.8);

        await harness.Controller.RampDownForAudioTransitionAsync("device_change");

        Assert.Equal(0, harness.Volume);
        Assert.Equal(0, harness.VolumeWrites[^1]);
        for (var i = 1; i < harness.VolumeWrites.Count; i++)
        {
            Assert.True(
                harness.VolumeWrites[i] <= harness.VolumeWrites[i - 1] + 1e-9,
                $"volume rose at step {i}: {harness.VolumeWrites[i - 1]} -> {harness.VolumeWrites[i]}");
        }
    }

    [Fact]
    public async Task RampDown_NeverLeavesTheUnitRange()
    {
        var harness = new ControllerHarness(1.0);

        await harness.Controller.RampDownForAudioTransitionAsync("device_change");

        Assert.All(harness.VolumeWrites, value => Assert.InRange(value, 0.0, 1.0));
    }

    [Fact]
    public async Task RampDown_PreservesThePersistedTargetSoRampUpCanRestoreIt()
    {
        var harness = new ControllerHarness(0.65);

        await harness.Controller.RampDownForAudioTransitionAsync("device_change");

        Assert.Equal(0.65, harness.Controller.VolumeSaveOverride);
        Assert.False(harness.Controller.SuppressVolumeSave, "suppression must be released even though the ramp completed");
    }

    [Fact]
    public async Task RampDown_SkipsTheRampWhenAudioIsAlreadySilent()
    {
        var harness = new ControllerHarness(0.0);

        await harness.Controller.RampDownForAudioTransitionAsync("device_change");

        Assert.Contains(harness.TracePoints, point => point.Kind == "ramp-down-skipped" && point.Note == "already-zero");
        Assert.False(harness.HasTrace("ramp-down-start"));
        Assert.Equal(0, harness.Volume);
    }

    [Fact]
    public async Task RampDown_CompletesTheTraceSessionEvenWhenCancelledMidRamp()
    {
        var harness = new ControllerHarness(0.9);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(30));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.Controller.RampDownForAudioTransitionAsync("device_change", cts.Token));

        Assert.Contains(harness.CompletedSessions, session => session.Reason == "device_change");
        Assert.False(harness.Controller.SuppressVolumeSave, "a cancelled ramp must not strand save suppression");
    }

    [Fact]
    public async Task RampDown_ForStopUsesTheStopSpecificLogIdentity()
    {
        var harness = new ControllerHarness(0.5);

        await harness.Controller.RampDownForStopAsync(CancellationToken.None);

        Assert.Contains(harness.Logs, log => log.Contains("PREVIEW_AUDIO_STOP_RAMP_STARTED"));
        Assert.Contains(harness.Logs, log => log.Contains("PREVIEW_AUDIO_STOP_RAMP_COMPLETED"));
    }

    [Fact]
    public async Task RampDown_OpensNoTraceSessionWhenTracingIsDisabled()
    {
        var harness = new ControllerHarness(0.5);

        await harness.Controller.RampDownForAudioTransitionAsync("device_change", traceSession: false);

        Assert.Empty(harness.BegunSessions);
        Assert.Empty(harness.CompletedSessions);
    }

    // ---- RampUpForAudioTransitionAsync --------------------------------------

    [Fact]
    public async Task RampUp_ReachesTheTargetAndNeverLowersTheVolumeOnTheWayUp()
    {
        var harness = new ControllerHarness(0.0);

        await harness.Controller.RampUpForAudioTransitionAsync(0.75, "device_change");

        Assert.Equal(0.75, harness.Volume, 6);
        Assert.Equal(0.75, harness.VolumeWrites[^1], 6);
        for (var i = 1; i < harness.VolumeWrites.Count; i++)
        {
            Assert.True(
                harness.VolumeWrites[i] >= harness.VolumeWrites[i - 1] - 1e-9,
                $"volume fell at step {i}: {harness.VolumeWrites[i - 1]} -> {harness.VolumeWrites[i]}");
        }
    }

    [Fact]
    public async Task RampUp_NeverOvershootsTheTarget()
    {
        var harness = new ControllerHarness(0.0);

        await harness.Controller.RampUpForAudioTransitionAsync(0.4, "device_change");

        Assert.All(harness.VolumeWrites, value => Assert.InRange(value, 0.0, 0.4 + 1e-9));
    }

    [Fact]
    public async Task RampUp_ClearsSuppressionAndTheOverrideOnceTheTargetIsReached()
    {
        var harness = new ControllerHarness(0.0);

        await harness.Controller.RampUpForAudioTransitionAsync(0.5, "device_change");

        Assert.False(harness.Controller.SuppressVolumeSave);
        Assert.Null(harness.Controller.VolumeSaveOverride);
    }

    [Fact]
    public async Task RampUp_SkipsTheRampForASilentTarget()
    {
        var harness = new ControllerHarness(0.3);

        await harness.Controller.RampUpForAudioTransitionAsync(0, "device_change");

        Assert.Contains(harness.TracePoints, point => point.Kind == "ramp-up-skipped" && point.Note == "target-zero");
        Assert.Equal(0, harness.Volume);
        Assert.Null(harness.Controller.VolumeSaveOverride);
        Assert.Contains(harness.CompletedSessions, session => session.SessionId == harness.NextSessionId);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(1.9)]
    public async Task RampUp_ClampsTheRequestedTargetIntoTheUnitRange(double requested)
    {
        var harness = new ControllerHarness(0.0);

        await harness.Controller.RampUpForAudioTransitionAsync(requested, "device_change");

        Assert.All(harness.VolumeWrites, value => Assert.InRange(value, 0.0, 1.0));
        Assert.InRange(harness.Volume, 0.0, 1.0);
    }

    [Fact]
    public async Task RampUp_CompletesTheTraceSessionEvenWhenCancelledMidRamp()
    {
        var harness = new ControllerHarness(0.0);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(30));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.Controller.RampUpForAudioTransitionAsync(0.9, "device_change", cts.Token));

        Assert.Contains(harness.CompletedSessions, session => session.Reason == "device_change");
        Assert.False(harness.Controller.SuppressVolumeSave);
        Assert.Null(harness.Controller.VolumeSaveOverride);
    }

    [Fact]
    public async Task RampDownThenRampUp_RestoresTheOriginalMonitoringLevel()
    {
        // The round trip is the contract users actually feel across a device change.
        var harness = new ControllerHarness(0.62);

        await harness.Controller.RampDownForAudioTransitionAsync("device_change");
        var restoreTarget = harness.Controller.PersistedVolumeTarget;
        await harness.Controller.RampUpForAudioTransitionAsync(restoreTarget, "device_change");

        Assert.Equal(0.62, harness.Volume, 6);
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
