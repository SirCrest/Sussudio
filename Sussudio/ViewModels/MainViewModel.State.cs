using System;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
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
    public partial string DiskSpaceInfo { get; set; } = "";

    private int _disposeState;
    private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);
}
