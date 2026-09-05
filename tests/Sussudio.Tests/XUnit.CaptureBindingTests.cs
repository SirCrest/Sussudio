using System.Reflection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Sussudio.Controllers;
using Sussudio.Models;
using Sussudio.ViewModels;
using Xunit;

namespace Sussudio.Tests;

public sealed class CaptureBindingTests
{
    [Fact]
    public void CollectionChangesCoalescePerCategoryAndUseLatestSelection()
    {
        var f = new SelectionFixture();
        f.Selection.AttachCollectionBindings();
        Assert.Same(f.Vm.Devices, f.Devices.ItemsSource);
        Assert.Same(f.Vm.AudioInputDevices, f.Audio.ItemsSource);
        Assert.Same(f.Vm.AvailableResolutions, f.Resolutions.ItemsSource);
        var first = new CaptureDevice { Id = "first", Name = "First" };
        var latest = new CaptureDevice { Id = "latest", Name = "Latest" };
        f.Vm.Devices.Add(first);
        f.Vm.Devices.Add(latest);
        f.Vm.Devices.Remove(first);
        f.Vm.SelectedDevice = latest;
        var audio = new AudioInputDevice { Id = "audio" };
        f.Vm.AudioInputDevices.Add(audio);

        Assert.Equal(2, f.Queue.PendingCount);
        Assert.Null(f.Devices.SelectedItem);
        f.Queue.RunNext();
        Assert.Same(latest, f.Devices.SelectedItem);
        Assert.Equal(1, f.Queue.PendingCount);
        f.Queue.RunNext();
        Assert.Same(audio, f.Audio.SelectedItem);
        Assert.Equal(0, f.Queue.PendingCount);

        f.Vm.Devices.Clear();
        Assert.Equal(1, f.Queue.PendingCount);
        f.Queue.RunNext();
        Assert.Null(f.Devices.SelectedItem);
    }

    [Fact]
    public void RejectedQueueDoesNotSynchronizeInline()
    {
        var f = new SelectionFixture();
        f.Queue.AcceptEnqueue = false;
        f.Selection.AttachCollectionBindings();
        f.Vm.Devices.Add(new CaptureDevice { Id = "unselected" });
        f.Vm.Devices.Add(new CaptureDevice { Id = "another" });

        Assert.Equal(0, f.Queue.PendingCount);
        Assert.Null(f.Vm.SelectedDevice);
        Assert.Null(f.Devices.SelectedItem);
    }

    [Fact]
    public void FailedAcceptedCallbackReleasesItsCoalescingSlot()
    {
        var f = new SelectionFixture();
        var queueSync = typeof(CaptureSelectionBindingController).GetMethod("QueueSelectionSync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var failure = new IOException("selection callback failed");
        queueSync.Invoke(f.Selection, new object[] { 0, new Action(() => throw failure) });
        Assert.Same(failure, Assert.Throws<IOException>(() => f.Queue.RunNext()));
        var calls = 0;

        queueSync.Invoke(f.Selection, new object[] { 0, new Action(() => calls++) });

        Assert.Equal(1, f.Queue.PendingCount);
        f.Queue.RunNext();
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public void EmptyModeCollectionsPreserveOnlyActiveDevicePreview(bool hasDevice, bool previewing, bool preserve)
    {
        var f = new SelectionFixture();
        f.Vm.SelectedDevice = hasDevice ? new CaptureDevice { Id = "device" } : null;
        f.Vm.IsPreviewing = previewing;
        var resolution = new ResolutionOption { Value = "1920x1080", IsEnabled = true };
        var rate = new FrameRateOption { Value = 60, FriendlyValue = 60, IsEnabled = true };
        f.Resolutions.SelectedItem = resolution;
        f.FrameRates.SelectedItem = rate;
        f.Formats.SelectedItem = "Hevc";

        f.Selection.EnsureResolutionSelection();
        f.Selection.EnsureFrameRateSelection();
        f.Selection.EnsureFormatSelection();

        Assert.Same(preserve ? resolution : null, f.Resolutions.SelectedItem);
        Assert.Same(preserve ? rate : null, f.FrameRates.SelectedItem);
        Assert.Equal(preserve ? "Hevc" : null, f.Formats.SelectedItem);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void PendingDeviceApplyRequiresIdleRecordingAndReinitialization(bool recording, bool reinitializing, bool enabled)
    {
        var f = new SelectionFixture();
        f.Vm.SelectedDevice = new CaptureDevice { Id = "current" };
        f.Vm.IsRecording = recording;
        f.Vm.IsPreviewReinitializing = reinitializing;
        f.Selection.AttachDeviceSelectionChangedBinding();

        f.Devices.SelectedItem = new CaptureDevice { Id = "other" };

        Assert.Equal(enabled, f.Apply.IsEnabled);
        f.Devices.SelectedItem = new CaptureDevice { Id = "CURRENT" };
        Assert.False(f.Apply.IsEnabled);
        Assert.False(f.Selection.TryHandlePropertyChanged("Unrelated"));
    }

    [Theory]
    [InlineData("success")]
    [InlineData("fault")]
    [InlineData("cancel")]
    public async Task RefreshRestoresButtonAfterPendingOperation(string outcome)
    {
        var f = new SelectionFixture();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        f.Vm.OnRefreshDevicesAsync = () => { calls++; return completion.Task; };

        var operation = f.Actions.RefreshDevicesAsync();

        Assert.False(operation.IsCompleted);
        Assert.False(f.Refresh.IsEnabled);
        Assert.True(Assert.IsType<ProgressRing>(f.Refresh.Content).IsActive);
        Assert.Equal(1, calls);
        await CompleteOperationAsync(completion, operation, outcome);
        Assert.True(f.Refresh.IsEnabled);
        Assert.Equal("\uE72C", Assert.IsType<FontIcon>(f.Refresh.Content).Glyph);
    }

    [Theory]
    [InlineData("success")]
    [InlineData("fault")]
    [InlineData("cancel")]
    public async Task ApplyCapturesSelectedDeviceAndRecomputesButtonInFinally(string outcome)
    {
        var f = new SelectionFixture();
        var original = new CaptureDevice { Id = "original" };
        f.Devices.SelectedItem = original;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CaptureDevice? applied = null;
        var calls = 0;
        f.Vm.OnApplySelectedDeviceAsync = device => { applied = device; calls++; return completion.Task; };

        var operation = f.Actions.ApplySelectedDeviceAsync();

        Assert.False(operation.IsCompleted);
        Assert.False(f.Apply.IsEnabled);
        Assert.Same(original, applied);
        f.Devices.SelectedItem = new CaptureDevice { Id = "changed while pending" };
        Assert.Equal(0, f.ApplyRefreshes);
        await CompleteOperationAsync(completion, operation, outcome);
        Assert.Equal(1, f.ApplyRefreshes);
        Assert.Equal(1, calls);
        Assert.Same(original, applied);
    }

    [Fact]
    public async Task ApplyWithoutTypedSelectionDoesNothing()
    {
        var f = new SelectionFixture();
        var calls = 0;
        f.Vm.OnApplySelectedDeviceAsync = _ => { calls++; return Task.CompletedTask; };
        f.Devices.SelectedItem = "not a device";
        await f.Actions.ApplySelectedDeviceAsync();
        Assert.Equal(0, calls);
        Assert.Equal(0, f.ApplyRefreshes);
        Assert.True(f.Apply.IsEnabled);
    }

    [Fact]
    public void DisabledModeEventsDoNotChangeModelButRateStillRefreshesVisibility()
    {
        var f = new OptionFixture();
        f.Vm.SelectedResolution = "1920x1080";
        f.Vm.SelectedFrameRate = 60;
        f.Binding.AttachCaptureModeSelectionBindings();
        var resolutionSets = f.Vm.SelectedResolutionSetCount;
        var rateSets = f.Vm.SelectedFrameRateSetCount;

        f.Resolution.SelectedItem = new ResolutionOption { Value = "3840x2160", IsEnabled = false };
        f.Rate.SelectedItem = new FrameRateOption { Value = 120, FriendlyValue = 120, IsEnabled = false };

        Assert.Equal(resolutionSets, f.Vm.SelectedResolutionSetCount);
        Assert.Equal(rateSets, f.Vm.SelectedFrameRateSetCount);
        Assert.Equal(new[] { "decoder" }, f.Events);
        f.Resolution.SelectedItem = new ResolutionOption { Value = "3840x2160", IsEnabled = true };
        f.Rate.SelectedItem = new FrameRateOption { Value = 120, FriendlyValue = 120, IsEnabled = true };
        Assert.Equal("3840x2160", f.Vm.SelectedResolution);
        Assert.Equal(120, f.Vm.SelectedFrameRate);
    }

    [Fact]
    public void RecordingBindingsRouteValuesAndSaveVideoFormatBeforePresentation()
    {
        var f = new OptionFixture();
        f.Binding.AttachRecordingOptionBindings();
        f.Format.SelectedItem = "Hevc";
        f.Quality.SelectedItem = "High";
        f.Preset.SelectedItem = "P5";
        f.Split.SelectedItem = "Auto";
        Assert.Equal("Hevc", f.Vm.SelectedRecordingFormat);
        Assert.Equal("High", f.Vm.SelectedQuality);
        Assert.Equal("P5", f.Vm.SelectedPreset);
        Assert.Equal("Auto", f.Vm.SelectedSplitEncodeMode);
        f.Video.SelectedItem = "MJPG";
        Assert.Equal("MJPG", f.Vm.SelectedVideoFormat);
        Assert.Equal(new[] { "save", "decoder" }, f.Events);
        f.Vm.CustomBitrateMbps = 80;
        var sets = f.Vm.CustomBitrateMbpsSetCount;
        f.Bitrate.Value = double.NaN;
        Assert.Equal(sets, f.Vm.CustomBitrateMbpsSetCount);
        f.Bitrate.Value = 95;
        Assert.Equal(95, f.Vm.CustomBitrateMbps);
        f.Hdr.IsChecked = true;
        f.Hdr.RaiseClick();
        Assert.True(f.Vm.IsHdrEnabled);
        f.TrueHdr.IsChecked = true;
        f.TrueHdr.RaiseClick();
        Assert.True(f.Vm.IsTrueHdrPreviewEnabled);
    }

    [Fact]
    public void PresentationAppliesPolicyOutputsToActualControlBoundary()
    {
        var f = new OptionFixture();
        f.Vm.MjpegDecoderCount = 4;
        f.Vm.SelectedVideoFormat = "MJPG";
        f.Vm.SelectedFrameRate = 120;
        f.Vm.IsHdrAvailable = true;
        f.Vm.IsHdrEnabled = true;
        f.Vm.IsCustomBitrateVisible = true;
        f.Vm.AudioClipping = true;

        f.Presentation.ApplyInitialDecoderCountSelection();
        f.Presentation.UpdateDecoderCountVisibility();
        f.Presentation.ApplyHdrToggleEnabledState();
        f.Presentation.ApplyBitrateVisibility();
        f.Presentation.ApplyAudioClipVisibility();

        Assert.Equal(4, f.Decoder.SelectedItem);
        Assert.Equal(Visibility.Visible, f.DecoderPanel.Visibility);
        Assert.True(f.Hdr.IsEnabled);
        Assert.True(f.TrueHdr.IsEnabled);
        Assert.Equal(Visibility.Visible, f.BitratePanel.Visibility);
        Assert.Equal(Visibility.Collapsed, f.PresetPanel.Visibility);
        Assert.Equal(Visibility.Visible, f.Clip.Visibility);
        Assert.False(f.Binding.TryHandlePropertyChanged("Unrelated"));
        Assert.True(f.Binding.TryHandlePropertyChanged(nameof(MainViewModel.SourceWidth)));
        Assert.Contains("overlays", f.Events);
    }

    private static async Task CompleteOperationAsync(TaskCompletionSource completion, Task operation, string outcome)
    {
        switch (outcome)
        {
            case "fault":
                var failure = new IOException("controlled operation failure");
                completion.SetException(failure);
                Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => operation.WaitAsync(TimeSpan.FromSeconds(10))));
                break;
            case "cancel":
                completion.SetCanceled();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(TimeSpan.FromSeconds(10)));
                break;
            default:
                completion.SetResult();
                await operation.WaitAsync(TimeSpan.FromSeconds(10));
                break;
        }
    }

    private sealed class SelectionFixture
    {
        internal MainViewModel Vm { get; } = new();
        internal DispatcherQueue Queue { get; } = new();
        internal ComboBox Devices { get; } = new();
        internal ComboBox Audio { get; } = new();
        internal ComboBox Microphone { get; } = new();
        internal ComboBox Resolutions { get; } = new();
        internal ComboBox FrameRates { get; } = new();
        internal ComboBox Formats { get; } = new();
        internal Button Apply { get; } = new();
        internal Button Refresh { get; } = new();
        internal int ApplyRefreshes;
        internal CaptureSelectionBindingController Selection { get; }
        internal CaptureDeviceActionController Actions { get; }

        internal SelectionFixture()
        {
            Selection = new(new CaptureSelectionBindingControllerContext
            {
                DispatcherQueue = Queue, ViewModel = Vm, DeviceComboBox = Devices, AudioInputComboBox = Audio,
                MicrophoneComboBox = Microphone, ResolutionComboBox = Resolutions, FrameRateComboBox = FrameRates,
                FormatComboBox = Formats, QualityComboBox = new ComboBox(), PresetComboBox = new ComboBox(), SplitEncodeComboBox = new ComboBox(),
                ApplyDeviceButton = Apply, DeviceAudioControlPanel = new StackPanel(), DeviceAudioModeToggle = new ToggleSwitch(),
                AnalogAudioGainPanel = new StackPanel(), AnalogAudioGainSlider = new Slider(), AnalogAudioGainValueTextBlock = new TextBlock()
            });
            Actions = new(new CaptureDeviceActionControllerContext
            {
                ViewModel = Vm, RefreshButton = Refresh, ApplyDeviceButton = Apply, DeviceComboBox = Devices,
                UpdateDeviceApplyButtonState = () => ApplyRefreshes++
            });
        }
    }

    private sealed class OptionFixture
    {
        internal MainViewModel Vm { get; } = new();
        internal ComboBox Resolution { get; } = new();
        internal ComboBox Rate { get; } = new();
        internal ComboBox Format { get; } = new();
        internal ComboBox Quality { get; } = new();
        internal ComboBox Preset { get; } = new();
        internal ComboBox Split { get; } = new();
        internal ComboBox Video { get; } = new();
        internal ComboBox Decoder { get; } = new();
        internal NumberBox Bitrate { get; } = new();
        internal ToggleButton Hdr { get; } = new();
        internal ToggleButton TrueHdr { get; } = new();
        internal FrameworkElement DecoderPanel { get; } = new();
        internal FrameworkElement BitratePanel { get; } = new();
        internal FrameworkElement PresetPanel { get; } = new();
        internal FrameworkElement Clip { get; } = new();
        internal List<string> Events { get; } = new();
        internal CaptureOptionBindingController Binding { get; }
        internal CaptureOptionPresentationController Presentation { get; }

        internal OptionFixture()
        {
            Binding = new(new CaptureOptionBindingControllerContext
            {
                ViewModel = Vm, ResolutionComboBox = Resolution, FrameRateComboBox = Rate, FormatComboBox = Format,
                QualityComboBox = Quality, PresetComboBox = Preset, SplitEncodeComboBox = Split,
                VideoFormatComboBox = Video, DecoderCountComboBox = Decoder, CustomBitrateNumberBox = Bitrate,
                HdrToggle = Hdr, TrueHdrPreviewToggle = TrueHdr,
                ApplyInitialDecoderCountSelection = () => Events.Add("initial-decoder"), ApplyBitrateVisibility = () => Events.Add("bitrate"),
                ApplyHdrToggleEnabledState = () => Events.Add("hdr"), ApplyAudioClipVisibility = () => Events.Add("clip"),
                RefreshHdrHintText = () => Events.Add("hint"), UpdateFpsTelemetryTooltip = () => Events.Add("telemetry"),
                UpdateVideoContentOverlays = () => Events.Add("overlays"), SetHdrPassthroughEnabled = value => Events.Add($"passthrough:{value}"),
                UpdateDecoderCountVisibility = () => Events.Add("decoder"), EnsureResolutionSelection = () => Events.Add("resolution"),
                EnsureFrameRateSelection = () => Events.Add("rate"), EnsureFormatSelection = () => Events.Add("format"),
                EnsureQualitySelection = () => Events.Add("quality"), EnsurePresetSelection = () => Events.Add("preset"),
                EnsureSplitEncodeModeSelection = () => Events.Add("split"), SaveSettings = () => Events.Add("save")
            });
            Presentation = new(new CaptureOptionPresentationControllerContext
            {
                ViewModel = Vm, VideoFormatComboBox = Video, FrameRateComboBox = Rate,
                DecoderCountPanel = DecoderPanel, DecoderCountComboBox = Decoder, HdrToggle = Hdr,
                TrueHdrPreviewToggle = TrueHdr, CustomBitratePanel = BitratePanel, PresetPanel = PresetPanel, AudioClipText = Clip
            });
        }
    }
}
