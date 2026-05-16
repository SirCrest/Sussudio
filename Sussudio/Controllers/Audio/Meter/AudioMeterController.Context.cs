using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class AudioMeterControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required MainViewModel ViewModel { get; init; }
    public required Border AudioMeterTrack { get; init; }
    public required FrameworkElement AudioMeterContent { get; init; }
    public required Border AudioMeterRawFill { get; init; }
    public required Border AudioMeterFill { get; init; }
    public required RectangleGeometry AudioMeterRawClip { get; init; }
    public required RectangleGeometry AudioMeterColorClip { get; init; }
    public required Border AudioPeakHoldIndicator { get; init; }
    public required TranslateTransform AudioPeakHoldTranslate { get; init; }
    public required Border AudioRangeMinMarker { get; init; }
    public required TranslateTransform AudioRangeMinTranslate { get; init; }
    public required Border AudioRangeMaxMarker { get; init; }
    public required TranslateTransform AudioRangeMaxTranslate { get; init; }
    public required Border MicMeterTrack { get; init; }
    public required FrameworkElement MicMeterContent { get; init; }
    public required RectangleGeometry MicMeterClip { get; init; }
}
