using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Windows.Storage;
using Sussudio.Services.Audio;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio.Services.Capture;

// High-level capture orchestrator. It owns the lifetime of video capture,
// WASAPI capture/playback, recording sinks, Flashback backend pieces, source
// telemetry, and the snapshots consumed by automation. CaptureSessionCoordinator
// serializes public transitions; this class enforces the actual resource order.
public partial class CaptureService : IDisposable, IAsyncDisposable
{
    private readonly SemaphoreSlim _sessionTransitionLock = new(1, 1);
    // Lock ordering: acquire _sessionTransitionLock before _flashbackBackendLeaseLock.
    private readonly SemaphoreSlim _flashbackBackendLeaseLock = new(1, 1);
    private readonly ISourceSignalTelemetryProvider _sourceTelemetryProvider;
    private readonly IProcessSupervisor _processSupervisor;
    private readonly RecordingArtifactManager _artifactManager = new();

    private int _isDisposed;
    private bool _isInitialized;
    // REVIEWED 2026-04-07: writes serialized by _sessionTransitionLock;
    // unsync reads from UI thread produce at-worst one-frame-stale value (no crash/corruption).
    private bool _isRecording;
    private bool _isVideoPreviewActive;
    private bool _isAudioPreviewActive;
    private CaptureSessionState _sessionState = CaptureSessionState.Uninitialized;
    private CaptureDevice? _currentDevice;
    private CaptureSettings? _currentSettings;
    private SourceSignalTelemetrySnapshot _latestSourceTelemetry = SourceSignalTelemetrySnapshot.CreateUnavailable("telemetry-not-started");
    private readonly CaptureRecordingBackendResources _recordingBackend = new();
    private CaptureSettings? _activeRecordingSettings
    {
        get => _recordingBackend.SettingsSnapshot;
        set => _recordingBackend.SettingsSnapshot = value;
    }

    private LibAvRecordingSink? _libavSink
    {
        get => _recordingBackend.LibAvSink;
        set => _recordingBackend.LibAvSink = value;
    }

    private IRecordingSink? _recordingSink
    {
        get => _recordingBackend.Sink;
        set => _recordingBackend.Sink = value;
    }
    private readonly FlashbackBackendResources _flashbackBackend = new();
    private FlashbackBufferManager? _flashbackBufferManager
    {
        get => _flashbackBackend.BufferManager;
        set => _flashbackBackend.BufferManager = value;
    }

    private FlashbackEncoderSink? _flashbackSink
    {
        get => _flashbackBackend.Sink;
        set => _flashbackBackend.Sink = value;
    }

    private FlashbackExporter? _flashbackExporter
    {
        get => _flashbackBackend.Exporter;
        set => _flashbackBackend.Exporter = value;
    }

    private FlashbackPlaybackController? _flashbackPlaybackController
    {
        get => _flashbackBackend.PlaybackController;
        set => _flashbackBackend.PlaybackController = value;
    }

    private CaptureSettings? _flashbackBackendSettings
    {
        get => _flashbackBackend.SettingsSnapshot;
        set => _flashbackBackend.SettingsSnapshot = value;
    }

    // Flashback uses a preview-owned continuous encoder when the user is not
    // recording, but can also become the recording backend. These flags track
    // deferred enable/settings changes so recording stop can restore the safe
    // preview backend without mutating capture topology mid-recording.
    private volatile bool _flashbackEnabled = true;
    private bool _hasAv1Nvenc;
    private bool _pendingFlashbackSettingsChange;
    private bool _pendingFlashbackEnableAfterRecording;
    private long _flashbackRecordingStartBytes;
    private readonly PreviewAudioGraphResources _previewAudioGraph = new();
    private WasapiAudioCapture? _wasapiAudioCapture
    {
        get => _previewAudioGraph.ProgramCapture;
        set => _previewAudioGraph.ProgramCapture = value;
    }

    private WasapiAudioCapture? _microphoneCapture
    {
        get => _previewAudioGraph.MicrophoneCapture;
        set => _previewAudioGraph.MicrophoneCapture = value;
    }

    private WasapiAudioPlayback? _wasapiAudioPlayback
    {
        get => _previewAudioGraph.Playback;
        set => _previewAudioGraph.Playback = value;
    }

    private float _previewVolume
    {
        get => _previewAudioGraph.PreviewVolume;
        set => _previewAudioGraph.PreviewVolume = value;
    }

    private bool _isMonitoringMuted
    {
        get => _previewAudioGraph.IsMonitoringMuted;
        set => _previewAudioGraph.IsMonitoringMuted = value;
    }

    private string? _micMonitorDeviceId;
    private string? _micMonitorDeviceName;
    private bool _micMonitorEnabled;
    private int _fatalCleanupInProgress;
    private int _flashbackCleanupInProgress;
    private int _flashbackRecordingStartInProgress;
    private int _flashbackRecordingFinalizeInProgress;
    private long _sessionGeneration;
    private UnifiedVideoCapture? _unifiedVideoCapture;
    private Task? _pendingLibAvDrainTask
    {
        get => _recordingBackend.PendingLibAvDrainTask;
        set => _recordingBackend.PendingLibAvDrainTask = value;
    }

    private RecordingContext? _recordingContext
    {
        get => _recordingBackend.Context;
        set => _recordingBackend.Context = value;
    }

    private readonly Stopwatch _recordingStopwatch = new();
    private RecordingIntegritySummary _lastRecordingIntegrity = RecordingIntegritySummary.NotStarted;
    private RecordingIntegrityCounterSnapshot? _recordingIntegrityCounterBaseline;
    private RecordingAudioIntegrityCounterSnapshot? _recordingIntegrityAudioBaseline;
    private bool _lastUsePostMuxAudio;
    private FinalizeResult? _lastExportResult;
    private long _lastFlashbackExportResultId;
    private readonly SemaphoreSlim _flashbackExportOperationLock = new(1, 1);
    private readonly object _flashbackExportDiagnosticsLock = new();
    private bool _flashbackExportActive;
    private long _flashbackExportId;
    private string _flashbackExportStatus = "NotStarted";
    private string _flashbackExportOutputPath = string.Empty;
    private long _flashbackExportStartedUtcUnixMs;
    private long _flashbackExportLastProgressUtcUnixMs;
    private long _flashbackExportCompletedUtcUnixMs;
    private int _flashbackExportSegmentsProcessed;
    private int _flashbackExportTotalSegments;
    private double _flashbackExportPercent;
    private long _flashbackExportInPointMs;
    private long _flashbackExportOutPointMs;
    private string _flashbackExportMessage = string.Empty;
    private string _flashbackExportFailureKind = string.Empty;
    private long _flashbackExportForceRotateFallbacks;
    private long _flashbackExportLastForceRotateFallbackUtcUnixMs;
    private int _flashbackExportLastForceRotateFallbackSegments;
    private long _flashbackExportLastForceRotateFallbackInPointMs;
    private long _flashbackExportLastForceRotateFallbackOutPointMs;
    private string? _audioDeviceId;
    private string? _audioDeviceName;
    private bool _mfConvertersDisabled;
    private uint? _actualWidth;
    private uint? _actualHeight;
    private double? _actualFrameRate;
    private string? _actualFrameRateArg;
    private uint? _actualFrameRateNumerator;
    private uint? _actualFrameRateDenominator;
    private string? _actualPixelFormat;
    private string _activeVideoInputPixelFormat = "nv12";
    private long _videoFramesDropped;
    private string? _firstObservedFramePixelFormat;
    private string? _latestObservedFramePixelFormat;
    private string? _latestObservedSurfaceFormat;
    private long _observedP010FrameCount;
    private long _observedNv12FrameCount;
    private long _observedOtherFrameCount;
    private long _lastMfSourceReaderFramesDelivered;
    private long _lastMfSourceReaderFramesDropped;
    private string? _lastMfSourceReaderNegotiatedFormat;
    private readonly object _telemetryPollSync = new();

    // Telemetry is advisory and read-only: it gates UI choices and diagnostics
    // but must not block capture or recording. Polling has its own generation so
    // stale results from an old device/session cannot overwrite the live state.
    private CancellationTokenSource? _telemetryPollCts;
    private Task? _telemetryPollTask;
    private long _telemetryPollGeneration;
    private const int TelemetryPollIntervalMs = 500;
    private const int TelemetryPollStopDrainTimeoutMs = 750;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<Exception>? ErrorOccurred;
    public event Action? PreCleanupRequested;
    public event EventHandler<ulong>? FrameCaptured;
    public event EventHandler<AudioLevelEventArgs>? AudioLevelUpdated;
    public event EventHandler<AudioLevelEventArgs>? MicrophoneAudioLevelUpdated;
    public event EventHandler<SourceSignalTelemetrySnapshot>? SourceTelemetryUpdated;

    public bool IsRecording => _isRecording;
    public bool IsInitialized => _isInitialized;
    public bool IsVideoPreviewActive => _isVideoPreviewActive;
    public bool IsAudioPreviewActive => _isAudioPreviewActive;
    public CaptureSessionState SessionState => _sessionState;

    public CaptureService() : this(new ProcessSupervisor(), null)
    {
    }

    internal CaptureService(IProcessSupervisor processSupervisor, ISourceSignalTelemetryProvider? sourceSignalTelemetryProvider = null)
    {
        _processSupervisor = processSupervisor;
        _sourceTelemetryProvider = sourceSignalTelemetryProvider ?? CreateDefaultTelemetryProvider();
    }

    private static ISourceSignalTelemetryProvider CreateDefaultTelemetryProvider()
    {
        return new NativeXuAtCommandProvider();
    }

}
