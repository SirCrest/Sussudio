using System.ComponentModel;
using System.Threading.Tasks;
using Sussudio.Controllers;

namespace Sussudio;

// PropertyChanged event envelope for view-model updates. Route order lives in
// MainWindowPropertyChangedRouter; domain property-name switches live in focused controllers.
public sealed partial class MainWindow
{
    private MainWindowPropertyChangedRouter _propertyChangedRouter = null!;

    private void InitializeMainWindowPropertyChangedRouter()
    {
        _propertyChangedRouter = new MainWindowPropertyChangedRouter(new MainWindowPropertyChangedRouterContext
        {
            TryHandleCaptureSelection = TryHandleCaptureSelectionPropertyChanged,
            TryHandleStatusStrip = TryHandleStatusStripPropertyChanged,
            TryHandlePreviewAsync = TryHandlePreviewPropertyChangedAsync,
            TryHandleRecording = TryHandleRecordingPropertyChanged,
            TryHandleOutput = TryHandleOutputPropertyChanged,
            TryHandleCaptureOption = TryHandleCaptureOptionPropertyChanged,
            TryHandleAudio = TryHandleAudioPropertyChanged,
            TryHandleShell = TryHandleShellPropertyChanged,
            TryHandleLiveSignal = TryHandleLiveSignalPropertyChanged,
            TryHandleFlashback = TryHandleFlashbackPropertyChanged
        });
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(
            () => HandleViewModelPropertyChangedAsync(e),
            $"ViewModel_PropertyChanged:{e.PropertyName}");
    }

    private Task HandleViewModelPropertyChangedAsync(PropertyChangedEventArgs e)
        => _propertyChangedRouter.RouteAsync(e.PropertyName);
}
