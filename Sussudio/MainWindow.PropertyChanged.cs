using System.Threading.Tasks;

namespace Sussudio;

// PropertyChanged event envelope for view-model updates. Keep route order here;
// domain property-name switches live in the focused PropertyChanged partials.
public sealed partial class MainWindow
{
    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(
            () => HandleViewModelPropertyChangedAsync(e),
            $"ViewModel_PropertyChanged:{e.PropertyName}");
    }

    private async Task HandleViewModelPropertyChangedAsync(System.ComponentModel.PropertyChangedEventArgs e)
    {
        var propertyName = e.PropertyName ?? string.Empty;

        if (TryHandleCaptureSelectionPropertyChanged(propertyName))
        {
            return;
        }

        if (TryHandleStatusStripPropertyChanged(propertyName))
        {
            return;
        }

        if (await TryHandlePreviewPropertyChangedAsync(propertyName))
        {
            return;
        }

        if (TryHandleRecordingPropertyChanged(propertyName))
        {
            return;
        }

        if (TryHandleOutputPropertyChanged(propertyName))
        {
            return;
        }

        if (TryHandleCaptureOptionPropertyChanged(propertyName))
        {
            return;
        }

        if (TryHandleAudioPropertyChanged(propertyName))
        {
            return;
        }

        if (TryHandleShellPropertyChanged(propertyName))
        {
            return;
        }

        if (TryHandleLiveSignalPropertyChanged(propertyName))
        {
            return;
        }

        if (TryHandleFlashbackPropertyChanged(propertyName))
        {
            return;
        }
    }
}
