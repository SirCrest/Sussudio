using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    [ObservableProperty]
    public partial bool IsPreviewing { get; set; }

    [ObservableProperty]
    public partial bool IsPreviewReinitializing { get; set; }

    [ObservableProperty]
    public partial bool IsInitialized { get; set; }

    private readonly SemaphoreSlim _previewReinitializeGate = new(1, 1);
    private int _previewReinitializeGeneration;
    private bool _cancelPreviewRestartAfterReinitialize;

    public event EventHandler? PreviewStartRequested;
    public event EventHandler? PreviewStopRequested;
    public event Func<string, Task>? PreviewReinitRequested;
    public event Func<Task>? PreviewRendererStopRequested;
}
