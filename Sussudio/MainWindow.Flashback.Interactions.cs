using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing Flashback interaction adapter. Behavior lives in focused controllers.
public sealed partial class MainWindow
{
    private FlashbackCommandController _flashbackCommandController = null!;
    private FlashbackPlayheadMotionController _flashbackPlayheadMotionController = null!;
    private FlashbackPollingController _flashbackPollingController = null!;
    private FlashbackScrubInteractionController _flashbackScrubInteractionController = null!;
    private FlashbackSettingsBindingController _flashbackSettingsBindingController = null!;
    private FlashbackTimelineController _flashbackTimelineController = null!;

    private void InitializeFlashbackCommandController()
    {
        _flashbackCommandController = new FlashbackCommandController(new FlashbackCommandControllerContext
        {
            ViewModel = ViewModel,
            FlashbackEnabledToggle = FlashbackEnabledToggle,
            RunUiEventHandlerAsync = RunUiEventHandlerAsync
        });
    }

    private void FlashbackInButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.SetInPointAtPlayhead();

    private void FlashbackOutButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.SetOutPointAtPlayhead();

    private void FlashbackClearButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.ClearInOutPoints();

    private void FlashbackPlayPauseButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.TogglePlayPause();

    private void FlashbackGoLiveButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.GoLive();

    private void FlashbackExportButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.Export(nameof(FlashbackExportButton_Click));

    private void FlashbackSaveLast5mButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.SaveLast5m(nameof(FlashbackSaveLast5mButton_Click));

    private void FlashbackEnabledToggle_Toggled(object sender, RoutedEventArgs e)
        => _flashbackCommandController.ToggleEnabled(nameof(FlashbackEnabledToggle_Toggled));

    private void FlashbackApplyButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.ApplySettings(nameof(FlashbackApplyButton_Click));

    private void InitializeFlashbackPollingController()
    {
        _flashbackPollingController = new FlashbackPollingController(new FlashbackPollingControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            ViewModel = ViewModel,
            IsWindowClosing = () => _isWindowClosing,
        });
    }

    private void StartFlashbackStatusPolling()
        => _flashbackPollingController.StartStatusPolling();

    private void StopFlashbackStatusPolling()
    {
        _flashbackPollingController.StopStatusPolling();
        StopFlashbackCtiAnchorTimer();
    }

    private void StartFlashbackPlaybackPolling()
        => _flashbackPollingController.StartPlaybackPolling();

    private void StopFlashbackPlaybackPolling()
        => _flashbackPollingController.StopPlaybackPolling();

    // XAML-facing Flashback playhead motion adapter.
    private void InitializeFlashbackPlayheadMotionController()
    {
        _flashbackPlayheadMotionController = new FlashbackPlayheadMotionController(new FlashbackPlayheadMotionControllerContext
        {
            ViewModel = ViewModel,
            DispatcherQueue = _dispatcherQueue,
            IsWindowClosing = () => _isWindowClosing,
            IsScrubbing = () => _flashbackScrubInteractionController.IsScrubbing,
            ScrubArea = FlashbackScrubArea,
            Playhead = FlashbackPlayhead,
            PlayheadHandle = FlashbackPlayheadHandle,
            PlayheadTimeBorder = FlashbackPlayheadTimeBorder,
        });
    }

    private void RequestFlashbackPlayheadSnapOnNextUpdate()
        => _flashbackPlayheadMotionController.RequestSnapOnNextUpdate();

    private void PositionFlashbackMagneticPlayhead(double x, double trackWidth)
        => _flashbackPlayheadMotionController.PositionMagneticPlayhead(x, trackWidth);

    private void RefreshFlashbackCtiMotion(string reason)
        => _flashbackPlayheadMotionController.RefreshCtiMotion(reason);

    private void StopFlashbackCtiAnchorTimer()
        => _flashbackPlayheadMotionController.StopCtiAnchorTimer();

    // XAML-facing Flashback pointer scrub adapter.
    private void InitializeFlashbackScrubInteractionController()
    {
        _flashbackScrubInteractionController = new FlashbackScrubInteractionController(new FlashbackScrubInteractionControllerContext
        {
            ViewModel = ViewModel,
            ScrubArea = FlashbackScrubArea,
            PositionMagneticPlayhead = PositionFlashbackMagneticPlayhead,
            RefreshCtiMotion = RefreshFlashbackCtiMotion,
            GetTickCount64 = () => Environment.TickCount64,
        });
    }

    private void FlashbackScrubArea_PointerPressed(object sender, PointerRoutedEventArgs e)
        => _flashbackScrubInteractionController.PointerPressed(sender as UIElement, e);

    private void FlashbackScrubArea_PointerMoved(object sender, PointerRoutedEventArgs e)
        => _flashbackScrubInteractionController.PointerMoved(sender as UIElement, e);

    private void FlashbackScrubArea_PointerReleased(object sender, PointerRoutedEventArgs e)
        => _flashbackScrubInteractionController.PointerReleased(sender as UIElement, e);

    private void FlashbackScrubArea_PointerCanceled(object sender, PointerRoutedEventArgs e)
        => _flashbackScrubInteractionController.PointerCanceled(sender as UIElement, e);

    private void FlashbackScrubArea_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        => _flashbackScrubInteractionController.PointerCaptureLost(sender as UIElement, e);

    private void ClearFlashbackScrubInteractionForLockout()
        => _flashbackScrubInteractionController.ClearForLockout();

    private void InitializeFlashbackSettingsBindingController()
    {
        _flashbackSettingsBindingController = new FlashbackSettingsBindingController(new FlashbackSettingsBindingControllerContext
        {
            ViewModel = ViewModel,
            FlashbackEnabledToggle = FlashbackEnabledToggle,
            FlashbackGpuDecodeToggle = FlashbackGpuDecodeToggle,
            FlashbackBufferDurationCombo = FlashbackBufferDurationCombo,
            ApplyFlashbackTimelineLockout = ApplyFlashbackTimelineLockout
        });
    }

    private void ApplyInitialFlashbackSettings()
        => _flashbackSettingsBindingController.ApplyInitialSettings();

    private void AttachFlashbackSettingsBindings()
        => _flashbackSettingsBindingController.AttachBindings();

    private void SyncFlashbackGpuDecodeSetting()
        => _flashbackSettingsBindingController.SyncGpuDecodeToggle();

    private void SyncFlashbackBufferDurationSetting()
        => _flashbackSettingsBindingController.SyncBufferDurationSelection();

    private void FlashbackBufferDurationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null || _flashbackSettingsBindingController == null)
        {
            return;
        }

        _flashbackSettingsBindingController.HandleBufferDurationSelectionChanged();
    }

    private void InitializeFlashbackTimelineController()
    {
        _flashbackTimelineController = new FlashbackTimelineController(new FlashbackTimelineControllerContext
        {
            ViewModel = ViewModel,
            FlashbackToggle = FlashbackToggle,
            FlashbackTimelinePanel = FlashbackTimelinePanel,
            FlashbackTrackBackground = FlashbackTrackBackground,
            FlashbackScrubArea = FlashbackScrubArea,
            FlashbackPlayhead = FlashbackPlayhead,
            FlashbackLiveEdge = FlashbackLiveEdge,
            SnapPlayheadOnNextOpen = RequestFlashbackPlayheadSnapOnNextUpdate,
            StartStatusPolling = StartFlashbackStatusPolling,
            StopStatusPolling = StopFlashbackStatusPolling,
            ClearScrubInteraction = ClearFlashbackScrubInteractionForLockout,
        });
    }

    private void FlashbackToggle_Checked(object sender, RoutedEventArgs e)
        => _flashbackTimelineController.OnToggleChecked();

    private void FlashbackToggle_Unchecked(object sender, RoutedEventArgs e)
        => _flashbackTimelineController.OnToggleUnchecked();

    private void ApplyFlashbackTimelineVisibility(bool show)
        => _flashbackTimelineController.ApplyVisibility(show);

    private void ApplyFlashbackTimelineLockout()
        => _flashbackTimelineController.ApplyLockout();

    private void CollapseFlashbackTimelineImmediately()
        => _flashbackTimelineController.CollapseImmediately();
}
