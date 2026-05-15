using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;
using Sussudio.Models;
using Sussudio.Services.Automation;
using Sussudio.Services.Gpu;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Tools;
using Sussudio.ViewModels;

namespace Sussudio;

// Main window composition root. This partial owns construction and service
// wiring; feature-specific UI behavior lives in sibling partials/controllers.
public sealed partial class MainWindow : Window, IAutomationWindowControl
{
    public MainViewModel ViewModel { get; }
    private readonly DispatcherQueue _dispatcherQueue;
    private NvmlMonitor? _nvmlMonitor;
    private readonly IAutomationDiagnosticsHub _automationDiagnosticsHub;
    private readonly NamedPipeAutomationServer _automationPipeServer;
    private readonly bool _automationTokenRequired;
    private readonly string _automationPipeName;
    private bool _suppressFlashbackEnabledToggle;
    private FullScreenController _fullScreenController = null!;
    private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)
        => Math.Abs(a - b) < tolerance;

    private static bool IsAutoFrameRateOption(FrameRateOption option)
        => option.Value <= 0 || option.FriendlyValue <= 0;

    public MainWindow()
    {
        InitializeComponent();

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        ViewModel = new MainViewModel();
        ViewModel.StatsSectionVisibilityHandler = SetStatsSectionVisible;
        ViewModel.FrameTimeOverlayVisibilityHandler = SetFrameTimeOverlayVisible;
        _windowTitleBase = BuildWindowTitleBase();
        ApplyWindowTitle();
        var automationToken = Environment.GetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar);
        var automationPipeName = Environment.GetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE");
        if (string.IsNullOrWhiteSpace(automationPipeName))
        {
            automationPipeName = NamedPipeAutomationServer.DefaultPipeName;
        }

        _automationTokenRequired = !string.IsNullOrWhiteSpace(automationToken);
        _automationPipeName = automationPipeName;

        _nvmlMonitor = new NvmlMonitor();

        _automationDiagnosticsHub = new AutomationDiagnosticsHub(
            ViewModel,
            GetPreviewRuntimeSnapshotAsync,
            new RecordingVerifier());
        var automationDispatcher = new AutomationCommandDispatcher(
            ViewModel,
            _automationDiagnosticsHub,
            this,
            automationToken);
        _automationPipeServer = new NamedPipeAutomationServer(
            automationDispatcher,
            _automationPipeName,
            _automationTokenRequired);
        _previewMinPresentationIntervalMs = ResolvePreviewExpectedIntervalMs();
        InitializeStatsOverlayController();

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
        InitializeFlashbackTimelineController();
        InitializeSettingsShelfController();
        InitializeSplashLoadingPhraseController();
        InitializeControlBarAnimationController();
        InitializeShellElevationController();
        InitializePreviewTransitionAnimationController();
        InitializeRecordButtonAnimationController();
        InitializeRecordingButtonActionController();
        InitializeLaunchEntranceAnimationController();
        InitializeLiveSignalInfoController();
        InitializePreviewAudioFadeController();
        InitializeMicrophoneControlsController();
        InitializeResponsiveShellLayoutController();
        InitializeCaptureSelectionBindingController();
        InitializeCaptureDeviceActionController();
        InitializeOutputPathDisplayController();
        InitializeOutputPathActionController();
        InitializePreviewScreenshotController();
    }
}
