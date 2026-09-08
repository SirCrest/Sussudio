using Sussudio.Controllers;
using Sussudio.Models;
using Xunit;

public sealed class RecordingSettingsApplicationTests
{
    [Theory]
    [InlineData("format")]
    [InlineData("quality")]
    [InlineData("split")]
    [InlineData("bitrate")]
    [InlineData("preset")]
    public async Task AutomationChangesSubmitOneCompleteSelectionAndAwaitApplication(string setting)
    {
        var fixture = new RecordingSelectionFixture();
        var operation = setting switch
        {
            "format" => fixture.Controller.SetRecordingFormatAsync("hevcmp4"),
            "quality" => fixture.Controller.SetQualityAsync("medium"),
            "split" => fixture.Controller.SetSplitEncodeModeAsync("twoway"),
            "bitrate" => fixture.Controller.SetCustomBitrateAsync(88),
            _ => fixture.Controller.SetPresetAsync("p7")
        };

        var request = Assert.Single(fixture.Requests);
        Assert.Equal(RecordingSettingsSelection.From(fixture.Settings), request.Selection);
        Assert.Equal(setting == "format" ? RecordingSettingsChangeKind.RecordingFormat : RecordingSettingsChangeKind.EncoderParameters, request.Kind);
        Assert.False(operation.IsCompleted);
        Assert.Same(request.Completion.Task, fixture.Controller.PendingApplication);
        Assert.Equal(new[] { "dispatch-enter", "setter", "capture", "apply", "dispatch-exit" }, fixture.Events);
        request.Completion.SetResult(RecordingSettingsApplyDisposition.Applied);
        await operation;
        await EventuallyAsync(() => fixture.Controller.PendingApplication == null);
    }

    [Theory]
    [InlineData(true, false, false, 1)]
    [InlineData(false, false, false, 0)]
    [InlineData(true, true, false, 0)]
    [InlineData(true, false, true, 0)]
    public async Task UiAdmissionAndAutomationAdmissionKeepTheirDifferentContracts(bool previewing, bool recording, bool loading, int uiRequests)
    {
        var fixture = new RecordingSelectionFixture { Previewing = previewing, Recording = recording, Loading = loading };
        fixture.Controller.OnSelectionChanged(RecordingSettingsChangeKind.EncoderParameters, "quality");
        Assert.Equal(uiRequests, fixture.Requests.Count);
        var automation = fixture.Controller.SetQualityAsync("Medium");
        Assert.Equal(uiRequests + 1, fixture.Requests.Count);
        foreach (var request in fixture.Requests)
        {
            request.Completion.SetResult(RecordingSettingsApplyDisposition.Deferred);
        }
        await automation;
    }

    [Fact]
    public void PropertySuppressionIsNestedAndRestoresAfterSetterFailure()
    {
        var fixture = new RecordingSelectionFixture();
        using (fixture.Controller.SuppressPropertyReactions())
        {
            using (fixture.Controller.SuppressPropertyReactions())
            {
                fixture.Controller.OnSelectionChanged(RecordingSettingsChangeKind.EncoderParameters, "nested");
            }
            fixture.Controller.OnSelectionChanged(RecordingSettingsChangeKind.EncoderParameters, "outer");
        }
        Assert.Empty(fixture.Requests);
        fixture.ThrowFromSetter = true;
        Assert.Throws<InvalidOperationException>(() => { _ = fixture.Controller.SetQualityAsync("Medium"); });
        fixture.ThrowFromSetter = false;
        fixture.Controller.OnSelectionChanged(RecordingSettingsChangeKind.EncoderParameters, "after failure");
        Assert.Single(fixture.Requests);
    }

    [Fact]
    public async Task OlderCompletionCannotClearNewerPendingApplication()
    {
        var fixture = new RecordingSelectionFixture();
        var older = fixture.Controller.SetSplitEncodeModeAsync("TwoWay");
        var newer = fixture.Controller.SetQualityAsync("Medium");
        Assert.Equal(SplitEncodeMode.TwoWay, fixture.Requests[1].Selection.SplitEncodeMode);
        fixture.Requests[0].Completion.SetResult(RecordingSettingsApplyDisposition.Superseded);
        await older;
        Assert.Same(fixture.Requests[1].Completion.Task, fixture.Controller.PendingApplication);
        fixture.Controller.ClearPendingIfSameAndCompleted(fixture.Requests[0].Completion.Task);
        Assert.Same(fixture.Requests[1].Completion.Task, fixture.Controller.PendingApplication);
        fixture.Requests[1].Completion.SetResult(RecordingSettingsApplyDisposition.Applied);
        await newer;
        await EventuallyAsync(() => fixture.Controller.PendingApplication == null);
    }

    [Fact]
    public async Task FailedApplicationRemainsObservableToLaterPreviewWaiterAndSameValueCanRetry()
    {
        var fixture = new RecordingSelectionFixture();
        var operation = fixture.Controller.SetQualityAsync("Medium");
        var failure = new InvalidOperationException("injected rebuild failure");
        fixture.Requests[0].Completion.SetException(failure);
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => operation));
        Assert.Same(fixture.Requests[0].Completion.Task, fixture.Controller.PendingApplication);
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Controller.PendingApplication!));
        await EventuallyAsync(() => fixture.Logs.Any(message => message.Contains("injected rebuild failure", StringComparison.Ordinal)));

        var retry = fixture.Controller.SetQualityAsync("Medium");
        Assert.Equal(2, fixture.Requests.Count);
        Assert.Same(fixture.Requests[1].Completion.Task, fixture.Controller.PendingApplication);
        fixture.Requests[1].Completion.SetResult(RecordingSettingsApplyDisposition.Applied);
        await retry;
        await EventuallyAsync(() => fixture.Controller.PendingApplication == null);
    }

    [Fact]
    public async Task FailedAndCanceledTasksClearOnlyWhenObservedOrReplaced()
    {
        var fixture = new RecordingSelectionFixture();
        var operation = fixture.Controller.SetPresetAsync("P7");
        fixture.Requests[0].Completion.SetCanceled();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        var pending = fixture.Controller.PendingApplication;
        Assert.NotNull(pending);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending!);
        fixture.Controller.ClearPendingIfSameAndCompleted(pending!);
        Assert.Null(fixture.Controller.PendingApplication);
        await EventuallyAsync(() => fixture.Logs.Any(message => message.Contains("canceled", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task SynchronousEnqueueFailureUsesTheSameObservableFailurePath()
    {
        var fixture = new RecordingSelectionFixture { ThrowFromApply = true };
        var operation = fixture.Controller.SetQualityAsync("Medium");
        await Assert.ThrowsAsync<InvalidOperationException>(() => operation);
        Assert.NotNull(fixture.Controller.PendingApplication);
        Assert.True(fixture.Controller.PendingApplication!.IsFaulted);
        Assert.Single(fixture.Logs);
    }

    [Fact]
    public void CancellationAndInvalidHdrSelectionDoNotMutateOrSubmit()
    {
        var fixture = new RecordingSelectionFixture { Hdr = true };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => { _ = fixture.Controller.SetQualityAsync("Medium", cancellation.Token); });
        Assert.Throws<InvalidOperationException>(() => { _ = fixture.Controller.SetRecordingFormatAsync("H264Mp4"); });
        Assert.Equal(VideoQuality.High, fixture.Settings.Quality);
        Assert.Empty(fixture.Requests);
    }

    [Fact]
    public void CompleteSelectionCopiesAllEncoderFieldsAndPreservesOtherCaptureSettings()
    {
        var desired = new RecordingSettingsSelection(RecordingFormat.Av1Mp4, VideoQuality.Custom, 88.5, NvencPreset.P7, SplitEncodeMode.ThreeWay);
        var settings = new CaptureSettings { Width = 3840, AudioEnabled = true };
        desired.ApplyTo(settings);
        Assert.Equal(desired, RecordingSettingsSelection.From(settings));
        Assert.True(desired.Matches(settings));
        Assert.Equal(3840, settings.Width);
        Assert.True(settings.AudioEnabled);
        settings.SplitEncodeMode = SplitEncodeMode.Auto;
        Assert.False(desired.Matches(settings));
        Assert.False(desired.Matches(null));
    }

    private static async Task EventuallyAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        while (!condition())
        {
            await Task.Delay(1, timeout.Token);
        }
    }

    private sealed class RecordingSelectionFixture
    {
        public CaptureSettings Settings { get; } = new()
        {
            Format = RecordingFormat.H264Mp4, Quality = VideoQuality.High,
            CustomBitrateMbps = 50, NvencPreset = NvencPreset.P4, SplitEncodeMode = SplitEncodeMode.Auto
        };
        public bool Previewing = true;
        public bool Recording;
        public bool Loading;
        public bool Hdr;
        public bool ThrowFromSetter;
        public bool ThrowFromApply;
        public List<string> Events { get; } = new();
        public System.Collections.Concurrent.ConcurrentQueue<string> Logs { get; } = new();
        public List<Request> Requests { get; } = new();
        public MainViewModelRecordingSettingsController Controller { get; }

        public RecordingSelectionFixture()
        {
            Controller = new MainViewModelRecordingSettingsController(new MainViewModelRecordingSettingsControllerContext
            {
                InvokeOnUiThreadAsync = (callback, _) =>
                {
                    Events.Add("dispatch-enter");
                    try { return callback(); }
                    finally { Events.Add("dispatch-exit"); }
                },
                GetAvailableRecordingFormats = () => Enum.GetNames<RecordingFormat>(),
                GetAvailableQualities = () => Enum.GetNames<VideoQuality>(),
                GetAvailableSplitEncodeModes = () => Enum.GetNames<SplitEncodeMode>(),
                GetAvailablePresets = () => Enum.GetNames<NvencPreset>(),
                IsHdrEnabled = () => Hdr,
                IsHdrCompatibleFormat = format => format != "H264Mp4",
                ClampCustomBitrateMbps = value => Math.Clamp(value, 1, 300),
                IsPreviewing = () => Previewing,
                IsRecording = () => Recording,
                IsLoadingSettings = () => Loading,
                SetSelectedRecordingFormat = value => Set(() => Settings.Format = Enum.Parse<RecordingFormat>(value), RecordingSettingsChangeKind.RecordingFormat),
                SetSelectedQuality = value => Set(() => Settings.Quality = Enum.Parse<VideoQuality>(value)),
                SetSelectedSplitEncodeMode = value => Set(() => Settings.SplitEncodeMode = Enum.Parse<SplitEncodeMode>(value)),
                SetSelectedPreset = value => Set(() => Settings.NvencPreset = Enum.Parse<NvencPreset>(value)),
                SetCustomBitrateMbps = value => Set(() => Settings.CustomBitrateMbps = value),
                SetOutputPath = _ => { },
                CaptureSelection = () => { Events.Add("capture"); return RecordingSettingsSelection.From(Settings); },
                ApplyAsync = (selection, kind, _) =>
                {
                    Events.Add("apply");
                    if (ThrowFromApply) throw new InvalidOperationException("injected enqueue failure");
                    var request = new Request(selection, kind);
                    Requests.Add(request);
                    return request.Completion.Task;
                },
                Log = Logs.Enqueue
            });
        }

        private void Set(Action mutation, RecordingSettingsChangeKind kind = RecordingSettingsChangeKind.EncoderParameters)
        {
            Events.Add("setter");
            if (ThrowFromSetter) throw new InvalidOperationException("injected setter failure");
            mutation();
            Controller.OnSelectionChanged(kind, "property reaction");
        }
    }

    private sealed record Request(RecordingSettingsSelection Selection, RecordingSettingsChangeKind Kind)
    {
        public TaskCompletionSource<RecordingSettingsApplyDisposition> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
