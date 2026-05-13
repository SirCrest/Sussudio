using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Tools;
using Sussudio.ViewModels;
using Sussudio.Controllers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using WinRT.Interop;
using Sussudio.Services.Audio;
using Sussudio.Services.Automation;
using Sussudio.Services.Capture;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio;

// Main window composition root. This partial owns construction and service
// wiring; feature-specific UI behavior lives in sibling partials/controllers.
public sealed partial class MainWindow : Window, IAutomationWindowControl
{
    public MainViewModel ViewModel { get; }
    private readonly DispatcherQueue _dispatcherQueue;
    private SoftwareBitmapSource? _previewSource;
    private D3D11PreviewRenderer? _d3dRenderer;
    private NvmlMonitor? _nvmlMonitor;
    private SpriteVisual? _videoShadowVisual;
    private SpriteVisual? _controlBarShadowVisual;
    private long _previewFramesArrived;
    private long _previewFramesDisplayed;
    private long _previewFramesDropped;
    private long _previewLastPresentedTick;
    private double _previewMinPresentationIntervalMs;
    private readonly IAutomationDiagnosticsHub _automationDiagnosticsHub;
    private readonly NamedPipeAutomationServer _automationPipeServer;
    private readonly bool _automationTokenRequired;
    private readonly string _automationPipeName;
    private readonly string _windowTitleBase;
    private long _lastRendererStopTick;
    private long _rendererReinitUnsafeWindows;
    public long RendererReinitUnsafeWindows => Interlocked.Read(ref _rendererReinitUnsafeWindows);
    private bool _isFlashbackScrubbing;
    private TimeSpan? _lastScrubPointerPosition;
    private bool _suppressFlashbackEnabledToggle;
    private FullScreenController _fullScreenController = null!;
    private const int MinWindowWidth = 900;
    private const int MinWindowHeight = 500;
    private MinSizeWindowSubclass.MinSizeHandle? _minSizeHandle;
    private IntPtr _hwnd;
    private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)
        => Math.Abs(a - b) < tolerance;

    private static bool IsAutoFrameRateOption(FrameRateOption option)
        => option.Value <= 0 || option.FriendlyValue <= 0;

    private double ResolvePreviewExpectedIntervalMs()
    {
        var sourceFps = ViewModel.SelectedFormat?.FrameRateExact ?? 0;
        if (sourceFps <= 0)
        {
            sourceFps = 60;
        }

        return Math.Max(1.0, 1000.0 / sourceFps);
    }

    private static bool IsHdrSubtype(string? subtype)
        => MediaFormat.IsHdrPixelFormat(subtype);

    private static string BuildWindowTitleBase()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            return "Simple Sussudio";
        }

        var buildTime = File.GetLastWriteTime(exePath);
        if (buildTime == DateTime.MinValue)
        {
            return "Simple Sussudio";
        }

        return $"Simple Sussudio (build {buildTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)})";
    }

    private void ApplyWindowTitle()
    {
        if (ViewModel.IsRecording)
        {
            Title = $"{_windowTitleBase} - REC {ViewModel.RecordingTime}";
            return;
        }

        Title = _windowTitleBase;
    }

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

        // Set window handle for folder picker
        _hwnd = WindowNative.GetWindowHandle(this);
        ViewModel.SetWindowHandle(_hwnd);
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

        // Cloak the window to prevent white flash before XAML renders
        int cloakTrue = 1;
        DwmSetWindowAttribute(_hwnd, DWMWA_CLOAK, ref cloakTrue, sizeof(int));
        int darkMode = 1;
        DwmSetWindowAttribute(_hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

        // Enforce minimum window size via WM_GETMINMAXINFO
        _minSizeHandle = MinSizeWindowSubclass.Install(_hwnd, MinWindowWidth, MinWindowHeight);

        // Set initial window size and constraints
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
            Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd));
        appWindow.Closing += MainWindow_Closing;

        // Ensure window is not maximized
        if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;

            // Force normal (non-maximized) state
            presenter.Restore();
        }

        // Set window size to accommodate 1920x1080 preview + UI controls
        // Height calculation: 1080px video + ~250px UI controls + ~120px padding/spacing/titlebar
        appWindow.Resize(new Windows.Graphics.SizeInt32(1950, 1450));

        // Set title bar icon
        appWindow.SetIcon("Assets\\AppIcon.ico");

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
    private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            return GetPreviewRuntimeSnapshot();
        }

        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var completion = new TaskCompletionSource<PreviewRuntimeSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration registration = default;
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    completion.TrySetCanceled(cancellationToken);
                });
            }

            var enqueued = _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        completion.TrySetCanceled(cancellationToken);
                        return;
                    }

                    completion.TrySetResult(GetPreviewRuntimeSnapshot());
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
                finally
                {
                    registration.Dispose();
                }
            });

            if (enqueued)
            {
                return await completion.Task.ConfigureAwait(false);
            }

            registration.Dispose();
            if (attempt >= maxAttempts)
            {
                break;
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException("Failed to enqueue preview snapshot operation.");
    }










}
