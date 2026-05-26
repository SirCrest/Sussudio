using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Sussudio.Controllers;
using Sussudio.Models;

namespace Sussudio;

public sealed partial class MainWindow
{
    private PreviewAudioFadeController _previewAudioFadeController = null!;
    private PreviewButtonActionController _previewButtonActionController = null!;
    private PreviewButtonPresentationController _previewButtonPresentationController = null!;
    private PreviewFadeInController _previewFadeInController = null!;
    private PreviewLifecycleEventController _previewLifecycleEventController = null!;
    private PreviewRendererHostController _previewRendererHostController = null!;
    private PreviewResizeTelemetryController _previewResizeTelemetryController = null!;
    private PreviewRuntimeSnapshotSamplingController _previewRuntimeSnapshotSamplingController = null!;
    private PreviewSurfacePresentationController _previewSurfacePresentationController = null!;
    private PreviewSurfaceShadowController _previewSurfaceShadowController = null!;
    private PreviewStartupSessionController _previewStartupSessionController = null!;
    private PreviewStartupSignalCoordinator _previewStartupSignalCoordinator = null!;
    private PreviewStartupOverlayController _previewStartupOverlayController = null!;
    private PreviewTransitionAnimationController _previewTransitionAnimationController = null!;
    private PreviewReinitTransitionController _previewReinitTransitionController = null!;
    private PreviewStartupWatchdogController _previewStartupWatchdogController = null!;

    private void InitializePreviewButtonPresentationController()
    {
        _previewButtonPresentationController = new PreviewButtonPresentationController(new PreviewButtonPresentationControllerContext
        {
            PreviewButton = PreviewButton,
            PreviewButtonIcon = PreviewButtonIcon,
        });
    }

    private void ShowStopPreviewButtonPresentation()
        => _previewButtonPresentationController.ShowStopPreview();

    private void ShowStartPreviewButtonPresentation()
        => _previewButtonPresentationController.ShowStartPreview();

    // XAML-facing preview surface adapter. Surface and shadow behavior stays in
    // focused controllers; MainWindow only wires XAML elements to them.
    private void InitializePreviewSurfacePresentationController()
    {
        _previewSurfaceShadowController = new PreviewSurfaceShadowController(new PreviewSurfaceShadowControllerContext
        {
            VideoShadowHost = VideoShadowHost,
            ControlBarShadowHost = ControlBarShadowHost,
            ControlBarBorder = ControlBarBorder,
        });

        _previewSurfacePresentationController = new PreviewSurfacePresentationController(
            new PreviewSurfacePresentationControllerContext
            {
                GetPreviewSwapChainPanel = () => PreviewSwapChainPanel,
                PreviewContentGrid = PreviewContentGrid,
                RecordingGlowBorder = RecordingGlowBorder,
            },
            _previewSurfaceShadowController);
    }

    private void InitializePreviewLifecycleEventController()
    {
        _previewLifecycleEventController = new PreviewLifecycleEventController(new PreviewLifecycleEventControllerContext
        {
            ViewModel = ViewModel,
            ShouldBeginPreviewStartupAttempt = () => ShouldBeginPreviewStartupAttempt,
            BeginPreviewStartupAttempt = BeginPreviewStartupAttempt,
            PrimePreviewAudioFadeIn = PrimePreviewAudioFadeIn,
            IsPreviewReinitAnimating = () => IsPreviewReinitAnimating,
            PreparePreviewStartupPresentation = PreparePreviewStartupPresentation,
            StopPreviewStartupWatchdog = StopPreviewStartupWatchdog,
            StartPreviewStartupWatchdog = StartPreviewStartupWatchdog,
            StopPreviewStartupOverlay = StopPreviewStartupOverlay,
            SetPreviewStartupState = SetPreviewStartupState,
            GetPreviewStartupAttemptLabel = () => PreviewStartupAttemptLabel,
            StartPreviewRendererAsync = StartPreviewRendererAsync,
            IsPreviewFirstVisualConfirmed = () => IsPreviewFirstVisualConfirmed,
            RevealPreviewUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,
            SchedulePreviewStartupFailureStop = SchedulePreviewStartupFailureStop,
            ShowStopPreviewButtonPresentation = ShowStopPreviewButtonPresentation,
            ShowStartPreviewButtonPresentation = ShowStartPreviewButtonPresentation,
            ApplyHdrToggleEnabledState = ApplyHdrToggleEnabledState,
            StopPreviewRendererAsync = StopPreviewRendererAsync,
            ResetPreviewStartupTracking = preserveReinitAnimation => ResetPreviewStartupTracking(
                preserveReinitAnimation: preserveReinitAnimation),
            HandlePreviewReinitializingChanged = HandlePreviewReinitializingChanged,
        });
    }

    private void InitializePreviewRendererHostController()
    {
        _previewRendererHostController = new PreviewRendererHostController(new PreviewRendererHostControllerContext
        {
            ViewModel = ViewModel,
            DispatcherQueue = _dispatcherQueue,
            GetPreviewSwapChainPanel = () => PreviewSwapChainPanel,
            SetPreviewSwapChainPanel = panel => PreviewSwapChainPanel = panel,
            PreviewContentGrid = PreviewContentGrid,
            PreviewImage = PreviewImage,
            PreviewContentGridSizeChangedHandler = OnPreviewContentGridSizeChanged,
            PreviewSwapChainPanelSizeChangedHandler = OnPreviewSwapChainPanelSizeChanged,
            IsPreviewReinitAnimating = () => IsPreviewReinitAnimating,
            ClearPreviewReinitAnimatingForShutdown = () =>
            {
                _previewReinitTransitionController.Clear(nameof(StopPreviewForShutdown));
            },
            GetPreviewStartupAttemptLabel = () => PreviewStartupAttemptLabel,
            IsPreviewFirstVisualConfirmed = () => IsPreviewFirstVisualConfirmed,
            ConfirmPreviewFirstVisual = ConfirmPreviewFirstVisual,
            MarkStartupFailed = reason => SetPreviewStartupState(PreviewStartupState.Failed, reason),
            StopPreviewStartupWatchdog = StopPreviewStartupWatchdog,
            RevealPreviewUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,
            SchedulePreviewStartupFailureStop = SchedulePreviewStartupFailureStop,
            ClearVideoFrameShadow = ClearVideoFrameShadow,
            SetupVideoFrameShadow = SetupVideoFrameShadow,
            SetGpuPreviewVisibility = SetGpuPreviewVisibility,
            ResetPreviewSignalState = ResetPreviewSignalState,
            ResetPreviewResizeTelemetry = ResetPreviewResizeTelemetry,
            StopPreviewFadeInTimer = StopPreviewFadeInTimer,
            ResetPreviewContentTransform = ResetPreviewContentTransform,
            UpdateVideoContentOverlays = UpdateVideoContentOverlays,
            MarkPreviewRendererAttached = MarkPreviewRendererAttached,
            ConfigurePreviewStartupSignals = ConfigurePreviewStartupSignals,
            Log = message => Logger.Log(message)
        });
    }

    private void InitializePreviewResizeTelemetryController()
    {
        _previewResizeTelemetryController = new PreviewResizeTelemetryController();
    }

    private void InitializePreviewRuntimeSnapshotSamplingController()
    {
        _previewRuntimeSnapshotSamplingController = new PreviewRuntimeSnapshotSamplingController(new PreviewRuntimeSnapshotSamplingControllerContext
        {
            UiDispatchController = WindowUiDispatchController,
            ViewModel = ViewModel,
            RendererHostController = _previewRendererHostController,
            StartupSessionController = _previewStartupSessionController,
            StartupSignalCoordinator = _previewStartupSignalCoordinator,
            IsGpuElementVisible = () => PreviewSwapChainPanel.Visibility == Visibility.Visible,
            IsCpuElementVisible = () => PreviewImage.Visibility == Visibility.Visible,
            IsPlaceholderVisible = () => NoDevicePlaceholder.Visibility == Visibility.Visible,
            GetStartupVisualTimeoutMs = () => PreviewStartupVisualTimeoutMs
        });
    }

    private Task StartPreviewRendererAsync()
        => _previewRendererHostController.StartAsync();

    private Task StopPreviewRendererAsync()
        => _previewRendererHostController.StopAsync();

    private void StopPreviewForShutdown()
        => _previewRendererHostController.StopForShutdown();

    public long RendererReinitUnsafeWindows
        => _previewRendererHostController.RendererReinitUnsafeWindows;

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _previewResizeTelemetryController.HandleSizeChanged(
            ViewModel.IsPreviewing,
            _previewRendererHostController.HasD3DRenderer,
            PreviewSwapChainPanel.Visibility);
    }

    private void OnPreviewSwapChainPanelSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var scale = PreviewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;
        _previewRendererHostController.OnPanelSizeChanged(e.NewSize.Width, e.NewSize.Height, scale);
    }

    private void OnPreviewContentGridSizeChanged(object sender, SizeChangedEventArgs e)
        => UpdateVideoContentOverlays();

    private void UpdateVideoContentOverlays()
        => _previewSurfacePresentationController.UpdateVideoContentOverlays(ViewModel.SourceWidth, ViewModel.SourceHeight);

    private void SetupVideoFrameShadow()
        => _previewSurfaceShadowController.SetupVideoFrameShadow();

    private void SetupControlBarShadow()
        => _previewSurfaceShadowController.SetupControlBarShadow();

    private void SetGpuPreviewVisibility(Visibility visibility)
        => _previewSurfacePresentationController.SetGpuPreviewVisibility(visibility);

    private void ClearVideoFrameShadow()
        => _previewSurfaceShadowController.ClearVideoFrameShadow();

    private void FadeInVideoFrameShadow(int delayMs, int durationMs)
        => _previewSurfaceShadowController.FadeInVideoFrameShadow(delayMs, durationMs);

    private void FadeOutVideoFrameShadow(int durationMs)
        => _previewSurfaceShadowController.FadeOutVideoFrameShadow(durationMs);

    private void FadeInControlBarShadow(int delayMs, int durationMs)
        => _previewSurfaceShadowController.FadeInControlBarShadow(delayMs, durationMs);

    private void ResetPreviewResizeTelemetry()
        => _previewResizeTelemetryController.Reset();

    private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
        => await _previewRuntimeSnapshotSamplingController.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);

    private bool IsPreviewStopRequestedByUser
        => _previewLifecycleEventController.StopRequestedByUser;

    private void SetPreviewStopRequestedByUser(bool value)
        => _previewLifecycleEventController.SetStopRequestedByUser(value);

    private Task<bool> TryHandlePreviewPropertyChangedAsync(string propertyName)
        => _previewLifecycleEventController.TryHandlePropertyChangedAsync(propertyName);

    private void ViewModel_PreviewStartRequested(object? sender, EventArgs e)
        => _previewLifecycleEventController.HandlePreviewStartRequested();

    private void ViewModel_PreviewStopRequested(object? sender, EventArgs e)
        => _previewLifecycleEventController.HandlePreviewStopRequested();

    private void InitializePreviewAudioFadeController()
    {
        _previewAudioFadeController = new PreviewAudioFadeController(new PreviewAudioFadeControllerContext
        {
            ViewModel = ViewModel,
            PreviewVolumeSlider = PreviewVolumeSlider,
            PreviewVolumeLabel = PreviewVolumeLabel,
        });
    }

    private bool IsPreviewAudioFadeInActive => _previewAudioFadeController.IsFadingIn;

    private bool IsPreviewAudioFadeAnimationActive => _previewAudioFadeController.IsAnimationActive;

    private void PrimePreviewAudioFadeIn()
        => _previewAudioFadeController.PrimeFadeIn();

    private void StartPreviewAudioFadeIn(int durationMs = 900)
        => _previewAudioFadeController.StartFadeIn(durationMs);

    private Task StartPreviewAudioFadeOutAsync(int durationMs = 450)
        => _previewAudioFadeController.StartFadeOutAsync(durationMs);

    private void CancelPreviewAudioFadeInForUser()
        => _previewAudioFadeController.CancelFadeInForUser();

    private void InitializePreviewButtonActionController()
    {
        _previewButtonActionController = new PreviewButtonActionController(new PreviewButtonActionControllerContext
        {
            ViewModel = ViewModel,
            SetPreviewStopRequestedByUser = SetPreviewStopRequestedByUser,
            GetPreviewStartupAttemptId = () => PreviewStartupAttemptId,
            StopPreviewFadeInTimer = StopPreviewFadeInTimer,
            StartPreviewAudioFadeOutAsync = () => StartPreviewAudioFadeOutAsync(),
            AnimatePreviewOutAsync = AnimatePreviewOutAsync,
            ClearPreviewReinitAnimation = operationName =>
            {
                _previewReinitTransitionController.Clear(operationName, operationName: operationName);
            },
            ResetPreviewContentTransform = ResetPreviewContentTransform,
            RevealPreviewUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,
        });
    }

    private Task TogglePreviewFromButtonAsync()
        => _previewButtonActionController.TogglePreviewAsync(nameof(PreviewButton_Click));

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => TogglePreviewFromButtonAsync(), nameof(PreviewButton_Click));
    }

    private void InitializePreviewFadeInController()
    {
        _previewFadeInController = new PreviewFadeInController(new PreviewFadeInControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            GetRenderer = () => _previewRendererHostController.Renderer,
            AnimatePreviewInAsync = AnimatePreviewInAsync,
            StartPreviewAudioFadeIn = () => StartPreviewAudioFadeIn(),
        });
    }

    private void SchedulePreviewFadeIn()
        => _previewFadeInController.Schedule();

    private void StopPreviewFadeInTimer()
        => _previewFadeInController.Stop();

    private void InitializePreviewStartupOverlayController()
    {
        _previewStartupOverlayController = new PreviewStartupOverlayController(new PreviewStartupOverlayControllerContext
        {
            PreviewLoadingOverlay = PreviewLoadingOverlay,
        });
    }

    private void StartPreviewStartupOverlay()
        => _previewStartupOverlayController.Start();

    private void StopPreviewStartupOverlay()
        => _previewStartupOverlayController.Stop(IsPreviewReinitAnimating);

    private void InitializePreviewTransitionAnimationController()
    {
        _previewTransitionAnimationController = new PreviewTransitionAnimationController(new PreviewTransitionAnimationControllerContext
        {
            PreviewBorder = PreviewBorder,
            PreviewBorderScale = PreviewBorderScale,
            PreviewContentGrid = PreviewContentGrid,
            PreviewContentScale = PreviewContentScale,
            NoDevicePlaceholder = NoDevicePlaceholder,
            StopPreviewFadeInTimer = StopPreviewFadeInTimer,
            StartPreviewStartupOverlay = StartPreviewStartupOverlay,
            StopPreviewStartupOverlay = StopPreviewStartupOverlay,
            FadeOutVideoFrameShadow = FadeOutVideoFrameShadow,
            FadeInVideoFrameShadow = FadeInVideoFrameShadow,
        });
    }

    private void AddPreviewShellEntranceAnimations(Storyboard storyboard, EasingFunctionBase easing, int beginMs, int durationMs)
        => _previewTransitionAnimationController.AddPreviewShellEntranceAnimations(storyboard, easing, beginMs, durationMs);

    private void ResetPreviewContentTransform()
        => _previewTransitionAnimationController.ResetPreviewContentTransform();

    private Task AnimatePreviewOutAsync()
        => _previewTransitionAnimationController.AnimatePreviewOutAsync();

    private Task AnimatePreviewInAsync()
        => _previewTransitionAnimationController.AnimatePreviewInAsync();

    private void PreparePreviewStartupPresentation()
        => _previewTransitionAnimationController.PrepareStartupPresentation();

    private void RevealPreviewUnavailablePlaceholder()
        => _previewTransitionAnimationController.RevealUnavailablePlaceholder();

    private void InitializePreviewReinitTransitionController()
        => _previewReinitTransitionController = new PreviewReinitTransitionController();

    private bool IsPreviewReinitAnimating
        => _previewReinitTransitionController.IsAnimating;

    private async Task ViewModel_PreviewReinitRequested(string reason)
    {
        if (!ViewModel.IsPreviewing)
        {
            return;
        }

        _previewReinitTransitionController.BeginAnimateOut(reason, nameof(ViewModel_PreviewReinitRequested));
        await AnimatePreviewOutAsync();
    }

    private Task ViewModel_PreviewRendererStopRequested()
        => _previewRendererHostController.StopRendererForReinitTeardownAsync();

    private void HandlePreviewReinitializingChanged()
        => _previewReinitTransitionController.HandleReinitializingChanged(
            new PreviewReinitCompletionPresentationContext
            {
                IsPreviewReinitializing = ViewModel.IsPreviewReinitializing,
                IsPreviewing = ViewModel.IsPreviewing,
                IsFirstVisualConfirmed = IsPreviewFirstVisualConfirmed,
                AttemptLabel = PreviewStartupAttemptLabel,
                CallerName = nameof(HandleViewModelPropertyChangedAsync),
                UpdateDeviceApplyButtonState = UpdateDeviceApplyButtonState,
                RevealUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,
                StopPreviewStartupOverlay = StopPreviewStartupOverlay,
                ResetPreviewContentTransform = ResetPreviewContentTransform,
                ShowStartPreviewButtonPresentation = ShowStartPreviewButtonPresentation,
            });

    private void InitializePreviewStartupSessionController()
        => _previewStartupSessionController = new PreviewStartupSessionController(new PreviewStartupSessionControllerContext
        {
            IsPreviewing = () => ViewModel.IsPreviewing,
            IsPreviewStopRequestedByUser = () => IsPreviewStopRequestedByUser,
            GetSelectedDeviceName = () => ViewModel.SelectedDevice?.Name,
            ResetSignalState = ResetPreviewSignalState,
            ResetFailureStopSchedule = ResetPreviewStartupFailureStopSchedule,
            MarkFirstVisualSignalConfirmed = MarkPreviewStartupFirstVisualConfirmed,
            StopWatchdog = StopPreviewStartupWatchdog,
            StopOverlay = StopPreviewStartupOverlay,
            StopFadeInTimer = StopPreviewFadeInTimer,
            ScheduleFadeIn = SchedulePreviewFadeIn,
            CompleteFirstVisualTransition = (attemptLabel, callerName) =>
                _previewReinitTransitionController.CompleteFirstVisualTransition(attemptLabel, callerName),
            ClearReinitTransitionForStartupReset = (preserveReinitAnimation, callerName) =>
                _previewReinitTransitionController.ClearForStartupReset(preserveReinitAnimation, callerName),
            Log = message => Logger.Log(message),
            CreateAttemptId = () => Guid.NewGuid().ToString("N"),
            GetUtcNow = () => DateTimeOffset.UtcNow
        });

    private PreviewStartupState CurrentPreviewStartupState
        => _previewStartupSessionController.State;

    private string PreviewStartupAttemptLabel
        => _previewStartupSessionController.AttemptLabel;

    private string? PreviewStartupAttemptId
        => _previewStartupSessionController.AttemptId;

    private DateTimeOffset? PreviewStartupRequestedUtc
        => _previewStartupSessionController.RequestedUtc;

    private string? PreviewStartupMissingSignals
    {
        get => _previewStartupSessionController.MissingSignals;
        set => _previewStartupSessionController.SetMissingSignals(value);
    }

    private int PreviewStartupRecoveryAttemptCount
        => _previewStartupSessionController.RecoveryAttemptCount;

    private string? PreviewStartupLastFailureReason
        => _previewStartupSessionController.LastFailureReason;

    private bool IsPreviewFirstVisualConfirmed
        => _previewStartupSessionController.FirstVisualConfirmed;

    private bool ShouldBeginPreviewStartupAttempt
        => _previewStartupSessionController.ShouldBeginAttempt;

    private void SetPreviewStartupState(PreviewStartupState state, string? reason = null)
        => _previewStartupSessionController.SetStartupState(state, reason);

    private void MarkPreviewRendererAttached()
        => _previewStartupSessionController.MarkRendererAttached(DateTimeOffset.UtcNow);

    private void BeginPreviewStartupAttempt()
        => _previewStartupSessionController.BeginStartupAttempt();

    private void ConfirmPreviewFirstVisual(string source)
        => _previewStartupSessionController.ConfirmFirstVisual(source);

    private void ResetPreviewStartupTracking(bool keepRecoveryCount = false, bool preserveReinitAnimation = false)
        => _previewStartupSessionController.ResetStartupTracking(keepRecoveryCount, preserveReinitAnimation);

    private void InitializePreviewStartupSignalCoordinator()
        => _previewStartupSignalCoordinator = new PreviewStartupSignalCoordinator(new PreviewStartupSignalCoordinatorContext
        {
            IsSignalWindowActive = IsPreviewStartupSignalWindowActive,
            IsFirstVisualConfirmed = () => IsPreviewFirstVisualConfirmed,
            GetAttemptLabel = () => PreviewStartupAttemptLabel,
            SetMissingSignals = value => PreviewStartupMissingSignals = value,
            Log = message => Logger.Log(message),
            ConfirmFirstVisual = ConfirmPreviewFirstVisual,
            GetPlaybackSnapshotState = GetPreviewStartupPlaybackSnapshotState
        });

    private PreviewStartupReadinessSignalSnapshot PreviewStartupSignalSnapshot
        => _previewStartupSignalCoordinator.Snapshot;

    private bool _previewGpuSignalMediaOpened => PreviewStartupSignalSnapshot.GpuSignalMediaOpened;
    private bool _previewGpuSignalFirstFrame => PreviewStartupSignalSnapshot.GpuSignalFirstFrame;
    private bool _previewGpuSignalPlaybackAdvancing => PreviewStartupSignalSnapshot.GpuSignalPlaybackAdvancing;
    private PreviewStartupSignalFlags _previewStartupRequiredSignals => PreviewStartupSignalSnapshot.RequiredSignals;
    private PreviewStartupSignalFlags _previewStartupReceivedSignals => PreviewStartupSignalSnapshot.ReceivedSignals;
    private PreviewStartupStrategy _previewStartupStrategy => PreviewStartupSignalSnapshot.Strategy;
    private long PreviewStartupGpuPositionEventCount => _previewStartupSignalCoordinator.PositionEventCount;

    private bool IsPreviewStartupSignalWindowActive()
        => _previewStartupSessionController.IsSignalWindowActive(ViewModel.IsPreviewing);

    private void ResetPreviewSignalState()
        => _previewStartupSignalCoordinator.Reset();

    private void ConfigurePreviewStartupSignals(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)
        => _previewStartupSignalCoordinator.Configure(strategy, requiredSignals);

    private string BuildPreviewStartupMissingSignals()
        => _previewStartupSignalCoordinator.BuildMissingSignals();

    private void MarkPreviewStartupFirstVisualConfirmed()
        => _previewStartupSignalCoordinator.MarkFirstVisualConfirmed();

    private void MarkGpuStartupSignal(PreviewStartupSignalFlags signal, string signalName)
        => _previewStartupSignalCoordinator.MarkGpuStartupSignal(signal, signalName);

    private void MarkGpuStartupSignalMediaOpened()
        => MarkGpuStartupSignal(PreviewStartupSignalFlags.MediaOpened, "MediaOpened");

    private void MarkGpuStartupSignalFirstFrame()
        => _previewStartupSignalCoordinator.MarkGpuStartupSignalFirstFrame();

    private void MarkGpuStartupSignalPlaybackAdvancing(TimeSpan position)
        => _previewStartupSignalCoordinator.MarkGpuStartupSignalPlaybackAdvancing(position);

    private void LogPreviewStartupPlaybackSnapshot(string reason)
        => _previewStartupSignalCoordinator.LogPlaybackSnapshot(reason);

    private PreviewStartupPlaybackSnapshotState GetPreviewStartupPlaybackSnapshotState()
    {
        var renderer = _previewRendererHostController.Renderer;
        return new PreviewStartupPlaybackSnapshotState(
            renderer != null,
            renderer?.IsRendering == true,
            PreviewSwapChainPanel.Visibility.ToString());
    }

    private int PreviewStartupVisualTimeoutMs => _previewStartupWatchdogController.VisualTimeoutMs;

    private void InitializePreviewStartupWatchdogController()
        => _previewStartupWatchdogController = new PreviewStartupWatchdogController(new PreviewStartupWatchdogControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            IsWaitingForFirstVisual = () => _previewStartupSessionController.IsWaitingForFirstVisual,
            IsSignalWindowActive = IsPreviewStartupSignalWindowActive,
            IsWindowClosing = () => _isWindowClosing,
            IsPreviewStopRequestedByUser = () => IsPreviewStopRequestedByUser,
            IsPreviewing = () => ViewModel.IsPreviewing,
            GetElapsedMilliseconds = () => _previewStartupSessionController.GetElapsedMilliseconds(DateTimeOffset.UtcNow),
            GetAttemptLabel = () => PreviewStartupAttemptLabel,
            BuildMissingSignals = BuildPreviewStartupMissingSignals,
            GetMissingSignals = () => PreviewStartupMissingSignals,
            SetMissingSignals = value => PreviewStartupMissingSignals = value,
            MarkStartupFailed = reason => SetPreviewStartupState(PreviewStartupState.Failed, reason),
            GetTimeoutDiagnosticSnapshot = GetPreviewStartupTimeoutDiagnosticSnapshot,
            LogPlaybackSnapshot = LogPreviewStartupPlaybackSnapshot,
            StopStartupOverlay = StopPreviewStartupOverlay,
            SetStatusText = value => ViewModel.StatusText = value,
            StopPreviewForFailureAsync = _ => ViewModel.StopPreviewAsync(userInitiated: true, teardownPipeline: true),
            RunUiEventHandlerAsync = RunUiEventHandlerAsync
        });

    private void StopPreviewStartupWatchdog()
        => _previewStartupWatchdogController.Stop();

    private void StartPreviewStartupWatchdog()
        => _previewStartupWatchdogController.Start();

    private void SchedulePreviewStartupFailureStop(string reason)
        => _previewStartupWatchdogController.ScheduleFailureStop(reason);

    private void ResetPreviewStartupFailureStopSchedule()
        => _previewStartupWatchdogController.ResetFailureStopSchedule();

    private PreviewStartupTimeoutDiagnosticSnapshot GetPreviewStartupTimeoutDiagnosticSnapshot()
        => new(
            NoDevicePlaceholder.Visibility.ToString(),
            PreviewSwapChainPanel.Visibility.ToString(),
            PreviewImage.Visibility.ToString(),
            _previewStartupStrategy,
            _previewStartupRequiredSignals,
            _previewStartupReceivedSignals,
            PreviewStartupMissingSignals);
}
