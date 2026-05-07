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
using Sussudio.Services.Configuration;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio;

// Main window composition root. This partial owns construction and service
// wiring; feature-specific UI behavior lives in the sibling MainWindow.* files.
public sealed partial class MainWindow : Window, IAutomationWindowControl
{
    private enum PreviewStartupState
    {
        Idle,
        StartingSession,
        RendererAttaching,
        WaitingForFirstVisual,
        Rendering,
        Failed
    }

    private const int PreviewStartupDefaultVisualTimeoutMs = 10000;
    private const int PreviewStartupMinVisualTimeoutMs = 1000;
    private const int PreviewStartupMaxVisualTimeoutMs = 15000;
    private static readonly TimeSpan PreviewStartupPlaybackAdvanceThreshold = TimeSpan.FromMilliseconds(33);
    private static readonly int PreviewStartupVisualTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
        "SUSSUDIO_PREVIEW_START_TIMEOUT_MS",
        PreviewStartupDefaultVisualTimeoutMs,
        PreviewStartupMinVisualTimeoutMs,
        PreviewStartupMaxVisualTimeoutMs);

    public MainViewModel ViewModel { get; }
    private readonly DispatcherQueue _dispatcherQueue;
    private SoftwareBitmapSource? _previewSource;
    private D3D11PreviewRenderer? _d3dRenderer;
    private NvmlMonitor? _nvmlMonitor;
    private SpriteVisual? _videoShadowVisual;
    private SpriteVisual? _controlBarShadowVisual;
    private DispatcherQueueTimer? _statsPollTimer;
    private Storyboard? _statsDockStoryboard;
    private Storyboard? _showStatsDockStoryboard;
    private Storyboard? _hideStatsDockStoryboard;
    private Storyboard? _micMeterRowStoryboard;
    private Storyboard? _showMicMeterRowStoryboard;
    private Storyboard? _hideMicMeterRowStoryboard;
    private Storyboard? _audioMeterMonitoringStoryboard;
    private const double MicMeterRowHeight = 14;
    private long _previewFramesArrived;
    private long _previewFramesDisplayed;
    private long _previewFramesDropped;
    private long _previewLastResizeLogTick;
    private long _previewLastPresentedTick;
    private int _windowCloseRequested;
    private int _windowCloseCleanupStarted;
    private int _windowCloseRecordingStopInProgress;
    private int _windowCloseAllowedAfterRecordingStop;
    private readonly object _windowCloseCompletionLock = new();
    private TaskCompletionSource<object?>? _windowCloseCompletion;
    private double _previewMinPresentationIntervalMs;
    private readonly IAutomationDiagnosticsHub _automationDiagnosticsHub;
    private readonly NamedPipeAutomationServer _automationPipeServer;
    private readonly bool _automationTokenRequired;
    private readonly string _automationPipeName;
    private int _automationServicesStarted;
    private readonly int[] _selectionSyncQueued = new int[9];
    private const int SyncDevice = 0, SyncAudio = 1, SyncResolution = 2, SyncFrameRate = 3,
                       SyncFormat = 4, SyncQuality = 5, SyncPreset = 6, SyncSplitEncode = 7,
                       SyncMicrophone = 8;
    private readonly string _windowTitleBase;
    private DispatcherQueueTimer? _previewStartupWatchdogTimer;
    private DispatcherQueueTimer? _previewStartupTelemetryTimer;
    private PreviewStartupState _previewStartupState = PreviewStartupState.Idle;
    private string? _previewStartupAttemptId;
    private DateTimeOffset? _previewStartupRequestedUtc;
    private DateTimeOffset? _previewRendererAttachedUtc;
    private DateTimeOffset? _previewFirstVisualUtc;
    private string? _previewLastFailureReason;
    private string? _previewStartupMissingSignals;
    private int _previewRecoveryAttemptCount;
    private bool _previewFirstVisualConfirmed;
    private bool _previewStartupExpectGpuDualSignals;
    private bool _previewGpuSignalMediaOpened;
    private bool _previewGpuSignalFirstFrame;
    private bool _previewGpuSignalPlaybackAdvancing;
    private PreviewStartupSignalFlags _previewStartupRequiredSignals = PreviewStartupSignalFlags.None;
    private PreviewStartupSignalFlags _previewStartupReceivedSignals = PreviewStartupSignalFlags.None;
    private PreviewStartupStrategy _previewStartupStrategy = PreviewStartupStrategy.None;
    private TimeSpan _previewStartupLastPlaybackPosition = TimeSpan.Zero;
    private long _previewStartupPositionEventCount;
    private bool _previewStartupPlaybackPositionInitialized;
    private int _previewStartupFailureStopScheduled;
    private long _previewStartupLastPositionDispatchTick;
    private bool _previewStopRequestedByUser;
    private bool _isPreviewReinitAnimating;
    private long _lastRendererStopTick;
    private long _rendererReinitUnsafeWindows;
    public long RendererReinitUnsafeWindows => Interlocked.Read(ref _rendererReinitUnsafeWindows);
    private DispatcherQueueTimer? _previewFadeInTimer;
    private const int PreviewFadeInFrameThreshold = 3;
    private bool _isWindowClosing;
    private bool _toggleLabelsVisible;
    private bool _entranceAnimationPlayed;
    private bool _liveSignalInfoVisible;
    private DispatcherQueueTimer? _liveSignalDebounceTimer;
    private DispatcherQueueTimer? _liveSignalHideDebounceTimer;
    private double _savedPreviewVolume;
    private bool _isVolumeFadingIn;
    private Storyboard? _entranceStoryboard;
    private Storyboard? _previewVolumeFadeStoryboard;
    private bool _isSettingsShelfAnimating;
    private bool _isFlashbackTimelineAnimating;
    private bool _isFlashbackScrubbing;
    private TimeSpan? _lastScrubPointerPosition;
    private bool _suppressFlashbackEnabledToggle;
    private bool _isFullScreen;
    private bool _isFullScreenTransitioning;
    private Windows.Graphics.RectInt32 _preFullScreenBounds;
    private Windows.Graphics.PointInt32 _preFullScreenPosition;
    private bool _preFullScreenSettingsVisible;
    private bool _preFullScreenStatsDockVisible;
    private bool _fullScreenControlsVisible;
    private DispatcherQueueTimer? _fullScreenAutoHideTimer;
    private bool _fullScreenPointerOverControls;
    private const int FullScreenAutoHideDelayMs = 3000;
    private const double FullScreenHotZoneHeight = 150;
    private bool _captureSettingsNarrow;
    private const double ControlBarLabelThreshold = 900.0;
    private const int MinWindowWidth = 900;
    private const int MinWindowHeight = 500;
    private WndProcDelegate? _minSizeWndProc;
    private IntPtr _originalWndProc;
    private IntPtr _hwnd;
    private double _audioPeakHoldLevel;
    private long _audioPeakHoldTimestamp;
    private double _audioRangeMin = 1.0;
    private double _audioRangeMax;
    private long _audioRangeResetTimestamp;
    private double _audioMeterDisplayLevel;
    private double _audioMeterTargetLevel;
    private double _micMeterDisplayLevel;
    private double _micMeterTargetLevel;
    private bool _syncingMicrophoneVolumeControls;
    private int _selectedDecoderCount = 4;
    private LinearGradientBrush? _audioMeterColorBrush;
    private DispatcherQueueTimer? _audioMeterAnimationTimer;
    private readonly List<DiagnosticRowSlot> _decodeRowPool = new();
    private readonly List<DiagnosticRowSlot> _gpuRowPool = new();
    private readonly List<DiagnosticsPoolSlot> _diagnosticsRowPool = new();
    private TextBlock? _diagnosticsEmptyStateTextBlock;

    private const long AudioPeakHoldDurationMs = 1500;
    private const double AudioPeakHoldDecayPerSecond = 0.8;
    private const long AudioRangeWindowMs = 3000;
    private const int MaxExpectedDecodeRowCount = 14;
    private const int FixedGpuRowCount = 10;

    private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)
        => Math.Abs(a - b) < tolerance;

    private static bool IsAutoFrameRateOption(FrameRateOption option)
        => option.Value <= 0 || option.FriendlyValue <= 0;

    private static bool IsPreviewStartupFailedState(PreviewStartupState state)
        => state == PreviewStartupState.Failed;

    private static bool IsPreviewStartupTerminalState(PreviewStartupState state)
        => state is PreviewStartupState.Idle or PreviewStartupState.Rendering or PreviewStartupState.Failed;

    private bool IsPreviewStartupSignalWindowActive()
        => ViewModel.IsPreviewing &&
           !_previewFirstVisualConfirmed &&
           _previewStartupState is PreviewStartupState.StartingSession or PreviewStartupState.RendererAttaching or PreviewStartupState.WaitingForFirstVisual;


    private void ResetPreviewSignalState()
    {
        _previewStartupExpectGpuDualSignals = false;
        _previewGpuSignalMediaOpened = false;
        _previewGpuSignalFirstFrame = false;
        _previewGpuSignalPlaybackAdvancing = false;
        _previewStartupRequiredSignals = PreviewStartupSignalFlags.None;
        _previewStartupReceivedSignals = PreviewStartupSignalFlags.None;
        _previewStartupStrategy = PreviewStartupStrategy.None;
        _previewStartupLastPlaybackPosition = TimeSpan.Zero;
        _previewStartupPositionEventCount = 0;
        _previewStartupPlaybackPositionInitialized = false;
    }

















































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

        // Set window handle for folder picker
        _hwnd = WindowNative.GetWindowHandle(this);
        ViewModel.SetWindowHandle(_hwnd);

        // Cloak the window to prevent white flash before XAML renders
        int cloakTrue = 1;
        DwmSetWindowAttribute(_hwnd, DWMWA_CLOAK, ref cloakTrue, sizeof(int));
        int darkMode = 1;
        DwmSetWindowAttribute(_hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

        // Enforce minimum window size via WM_GETMINMAXINFO
        _minSizeWndProc = MinSizeWndProc;
        _originalWndProc = GetWindowLongPtr(_hwnd, GWLP_WNDPROC);
        SetWindowLongPtr(_hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_minSizeWndProc));

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

        // Fullscreen overlay: show controls when mouse enters bottom hot zone
        ElementCompositionPreview.SetIsTranslationEnabled(FullScreenControlsOverlay, true);
        ((UIElement)Content).PointerMoved += OnFullScreenPointerActivity;
        FullScreenControlsOverlay.PointerEntered += OnFullScreenControlsPointerEntered;
        FullScreenControlsOverlay.PointerExited += OnFullScreenControlsPointerExited;

        // Entrance animation: hide everything initially
        ControlBarBorder.Opacity = 0;
        ControlBarBorder.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 1.0);
        ControlBarBorder.RenderTransform = new TranslateTransform { Y = 16 };
        StatsRow.Opacity = 0;
        StatsRow.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0);
        StatsRow.RenderTransform = new TranslateTransform { Y = -8 };
        PreviewBorder.Opacity = 0;
        PreviewBorderScale.ScaleX = 0.97;
        PreviewBorderScale.ScaleY = 0.97;

        var entranceButtons = GetEntranceButtons();
        foreach (var button in entranceButtons)
        {
            button.Opacity = 0;
            if (button.RenderTransform is ScaleTransform transform)
            {
                transform.ScaleX = 0.85;
                transform.ScaleY = 0.85;
            }
        }

        // Shadow for control bar depth effect
        var shadow = new Microsoft.UI.Xaml.Media.ThemeShadow();
        shadow.Receivers.Add(SettingsOverlayPanel);
        ControlBarBorder.Shadow = shadow;
        ControlBarBorder.Translation = new System.Numerics.Vector3(0, 0, 32);

        // Record button: floating elevation with shadow
        var recShadow = new Microsoft.UI.Xaml.Media.ThemeShadow();
        RecordButton.Shadow = recShadow;
        RecordButton.Translation = new System.Numerics.Vector3(0, 0, 16);

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



















    public Task<WindowScreenshotResult> CaptureWindowScreenshotAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(new WindowScreenshotResult
            {
                Succeeded = false,
                Message = "Screenshot canceled."
            });
        }

        var completion = new TaskCompletionSource<WindowScreenshotResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration cancellationRegistration = default;
        if (cancellationToken.CanBeCanceled)
        {
            cancellationRegistration = cancellationToken.Register(() =>
            {
                completion.TrySetResult(new WindowScreenshotResult
                {
                    Succeeded = false,
                    Message = "Screenshot canceled."
                });
            });
            _ = completion.Task.ContinueWith(
                _ => cancellationRegistration.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetResult(new WindowScreenshotResult
                    {
                        Succeeded = false,
                        Message = "Screenshot canceled."
                    });
                    return;
                }

                var result = CaptureWindowScreenshotCore(outputPath);
                completion.TrySetResult(result);
            }
            catch (Exception ex)
            {
                completion.TrySetResult(new WindowScreenshotResult
                {
                    Succeeded = false,
                    Message = $"Screenshot failed: {ex.Message}"
                });
            }
        }))
        {
            cancellationRegistration.Dispose();
            completion.TrySetResult(new WindowScreenshotResult
            {
                Succeeded = false,
                Message = "Failed to enqueue screenshot capture on the UI thread."
            });
        }

        return completion.Task;
    }







    private static uint[] InitCrc32Table()
    {
        var t = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var c = i;
            for (var j = 0; j < 8; j++)
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            t[i] = c;
        }
        return t;
    }
}
