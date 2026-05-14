using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private DispatcherQueueTimer? _timer;
    private IntPtr _windowHandle;
    private const string LiveInfoUnavailable = "\u2014";

    [ObservableProperty]
    public partial bool IsStatsVisible { get; set; }

    [ObservableProperty]
    public partial bool IsSettingsVisible { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Ready";

    [ObservableProperty]
    public partial string LiveResolution { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial string LiveFrameRate { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial string LivePixelFormat { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial bool IsPreviewing { get; set; }

    [ObservableProperty]
    public partial bool IsPreviewReinitializing { get; set; }

    [ObservableProperty]
    public partial bool IsInitialized { get; set; }

    [ObservableProperty]
    public partial string DiskSpaceInfo { get; set; } = "";

    private int _disposeState;
    private readonly SemaphoreSlim _previewReinitializeGate = new(1, 1);
    private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);
    private int _previewReinitializeGeneration;
    private bool _cancelPreviewRestartAfterReinitialize;

    public event EventHandler? PreviewStartRequested;
    public event EventHandler? PreviewStopRequested;
    public event Func<string, Task>? PreviewReinitRequested;
    public event Func<Task>? PreviewRendererStopRequested;
}
