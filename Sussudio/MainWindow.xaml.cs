using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;
using Sussudio.Services.Gpu;
using Sussudio.ViewModels;

namespace Sussudio;

// Main window composition root. This partial owns construction and service
// wiring; feature-specific UI behavior lives in sibling partials/controllers.
public sealed partial class MainWindow : Window, IAutomationWindowControl
{
    public MainViewModel ViewModel { get; }
    private readonly DispatcherQueue _dispatcherQueue;
    private NvmlMonitor? _nvmlMonitor;
    private FullScreenController _fullScreenController = null!;

    public MainWindow()
    {
        InitializeComponent();

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        ViewModel = new MainViewModel();
        ViewModel.StatsSectionVisibilityHandler = SetStatsSectionVisible;
        ViewModel.FrameTimeOverlayVisibilityHandler = SetFrameTimeOverlayVisible;
        InitializeWindowTitleController();
        ApplyWindowTitle();
        _nvmlMonitor = new NvmlMonitor();
        var automationHost = CreateAutomationHost();
        _automationDiagnosticsHub = automationHost.DiagnosticsHub;
        _automationPipeServer = automationHost.PipeServer;
        _automationTokenRequired = automationHost.TokenRequired;
        _automationPipeName = automationHost.PipeName;
        InitializePreviewRendererHostController();
        InitializeStatsSnapshotProvider();
        InitializeStatsOverlayController();
        InitializeStatsSectionChromeController();

        var appWindow = InitializeNativeShellWindow();
        RegisterCloseLifecycle(appWindow);
        InitializeShellControllers();

        // Subscribe to ViewModel changes
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.PreviewStartRequested += ViewModel_PreviewStartRequested;
        ViewModel.PreviewStopRequested += ViewModel_PreviewStopRequested;
        ViewModel.PreviewReinitRequested += ViewModel_PreviewReinitRequested;
        ViewModel.PreviewRendererStopRequested += ViewModel_PreviewRendererStopRequested;

        // Wire up UI controls to ViewModel
        SetupBindings();
        SetupButtonHoverAnimations();
        SetupControlBarShadow();

        // ESC key exits fullscreen
        ((FrameworkElement)Content).KeyDown += OnContentKeyDown;

        InitializeFullScreenController();

        // Fullscreen overlay: show controls when mouse enters bottom hot zone
        ((UIElement)Content).PointerMoved += OnFullScreenPointerActivity;
        FullScreenControlsOverlay.PointerEntered += OnFullScreenControlsPointerEntered;
        FullScreenControlsOverlay.PointerExited += OnFullScreenControlsPointerExited;

        PrepareLaunchEntranceInitialState();
        ApplyShellElevation();

        // Refresh devices on load - use Loaded event to ensure XAML is fully parsed
        var mainContent = (FrameworkElement)this.Content;
        mainContent.Loaded += MainWindow_Loaded;
        mainContent.SizeChanged += MainWindow_SizeChanged;
        Closed += MainWindow_Closed;
    }

    private void InitializeShellControllers()
    {
        InitializeWindowAutomationController();
        InitializeWindowScreenshotController();
        InitializeFlashbackPollingController();
        InitializeFlashbackScrubInteractionController();
        InitializeFlashbackPlayheadMotionController();
        InitializeFlashbackTimelineController();
        InitializeFlashbackSettingsBindingController();
        InitializeFlashbackCommandController();
        InitializeFlashbackMarkerPresentationController();
        InitializeFlashbackPlaybackPresentationController();
        InitializeFlashbackPlaybackUiCoordinator();
        InitializeFlashbackExportProgressPresentationController();
        InitializeSettingsShelfController();
        InitializeSplashLoadingPhraseController();
        InitializeControlBarAnimationController();
        InitializeShellElevationController();
        InitializePreviewResizeTelemetryController();
        InitializePreviewSurfacePresentationController();
        InitializePreviewStartupSessionController();
        InitializePreviewStartupSignalCoordinator();
        InitializePreviewStartupWatchdogController();
        InitializePreviewStartupOverlayController();
        InitializePreviewFadeInController();
        InitializePreviewTransitionAnimationController();
        InitializePreviewButtonPresentationController();
        InitializeRecordButtonAnimationController();
        InitializeRecordingStatePresentationController();
        InitializeRecordingButtonActionController();
        InitializeLaunchEntranceAnimationController();
        InitializeLiveSignalInfoController();
        InitializeStatusStripPresentationController();
        InitializePreviewAudioFadeController();
        InitializePreviewButtonActionController();
        InitializeMicrophoneControlsController();
        InitializeAudioControlBindingController();
        InitializeAudioControlPresentationController();
        InitializeResponsiveShellLayoutController();
        InitializeCaptureSelectionBindingController();
        InitializeCaptureDeviceActionController();
        InitializeCaptureOptionPresentationController();
        InitializeCaptureOptionBindingController();
        InitializeOutputPathDisplayController();
        InitializeOutputPathActionController();
        InitializePreviewScreenshotController();
    }
}
