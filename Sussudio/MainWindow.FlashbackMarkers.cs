using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Sussudio;

// Flashback timeline marker presentation. This partial owns marker placement,
// selection-region layout, and compact duration text formatting.
public sealed partial class MainWindow
{
    private static string FormatFlashbackDuration(TimeSpan ts)
    {
        var totalMinutes = (int)ts.TotalMinutes;
        var seconds = ts.Seconds;
        return $"{totalMinutes}:{seconds:D2}";
    }

    private void UpdateFlashbackMarkers()
    {
        var bufferDuration = ViewModel.FlashbackBufferFilledDuration;
        var trackWidth = FlashbackScrubArea.ActualWidth;
        var trackHeight = FlashbackScrubArea.ActualHeight;
        var hasUsableTrack = IsUsableFlashbackTrackDimension(trackWidth) &&
                             IsUsableFlashbackTrackDimension(trackHeight);
        var hasUsableDuration = IsUsableFlashbackDuration(bufferDuration);

        TimeSpan? inPtVal = null, outPtVal = null;

        if (hasUsableTrack && hasUsableDuration && ViewModel.FlashbackInPoint is TimeSpan inPt)
        {
            inPtVal = inPt;
            var inX = Math.Clamp(inPt.TotalSeconds / bufferDuration.TotalSeconds * trackWidth, 0, trackWidth);
            FlashbackInPointMarker.Visibility = Visibility.Visible;
            FlashbackInPointMarker.Height = trackHeight;
            Canvas.SetLeft(FlashbackInPointMarker, inX - 1);
        }
        else
        {
            FlashbackInPointMarker.Visibility = Visibility.Collapsed;
        }

        if (hasUsableTrack && hasUsableDuration && ViewModel.FlashbackOutPoint is TimeSpan outPt)
        {
            outPtVal = outPt;
            var outX = Math.Clamp(outPt.TotalSeconds / bufferDuration.TotalSeconds * trackWidth, 0, trackWidth);
            FlashbackOutPointMarker.Visibility = Visibility.Visible;
            FlashbackOutPointMarker.Height = trackHeight;
            Canvas.SetLeft(FlashbackOutPointMarker, outX - 1);
        }
        else
        {
            FlashbackOutPointMarker.Visibility = Visibility.Collapsed;
        }

        // Selection region between in/out points.
        if (inPtVal is TimeSpan inVal && outPtVal is TimeSpan outVal && hasUsableTrack && hasUsableDuration)
        {
            var inFrac = inVal.TotalSeconds / bufferDuration.TotalSeconds;
            var outFrac = outVal.TotalSeconds / bufferDuration.TotalSeconds;
            var selLeft = Math.Clamp(inFrac * trackWidth, 0, trackWidth);
            var selRight = Math.Clamp(outFrac * trackWidth, 0, trackWidth);
            FlashbackSelectionRegion.Visibility = Visibility.Visible;
            FlashbackSelectionRegion.Height = trackHeight;
            FlashbackSelectionRegion.Width = Math.Max(0, selRight - selLeft);
            Canvas.SetLeft(FlashbackSelectionRegion, selLeft);
        }
        else
        {
            FlashbackSelectionRegion.Visibility = Visibility.Collapsed;
        }
    }
}
