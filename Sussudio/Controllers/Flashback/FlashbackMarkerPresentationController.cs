using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Sussudio.Controllers;

internal sealed class FlashbackMarkerPresentationControllerContext
{
    public required FrameworkElement ScrubArea { get; init; }
    public required FrameworkElement InPointMarker { get; init; }
    public required FrameworkElement OutPointMarker { get; init; }
    public required FrameworkElement SelectionRegion { get; init; }
}

internal sealed class FlashbackMarkerPresentationController
{
    private readonly FlashbackMarkerPresentationControllerContext _context;

    public FlashbackMarkerPresentationController(FlashbackMarkerPresentationControllerContext context)
    {
        _context = context;
    }

    public static string FormatDuration(TimeSpan value)
    {
        var totalMinutes = (int)value.TotalMinutes;
        var seconds = value.Seconds;
        return $"{totalMinutes}:{seconds:D2}";
    }

    public void UpdateMarkers(TimeSpan bufferDuration, TimeSpan? inPoint, TimeSpan? outPoint)
    {
        var trackWidth = _context.ScrubArea.ActualWidth;
        var trackHeight = _context.ScrubArea.ActualHeight;
        var hasUsableTrack = IsUsableTrackDimension(trackWidth) &&
                             IsUsableTrackDimension(trackHeight);
        var hasUsableDuration = IsUsableDuration(bufferDuration);

        TimeSpan? inPtVal = null, outPtVal = null;

        if (hasUsableTrack && hasUsableDuration && inPoint is TimeSpan inPt)
        {
            inPtVal = inPt;
            var inX = Math.Clamp(inPt.TotalSeconds / bufferDuration.TotalSeconds * trackWidth, 0, trackWidth);
            _context.InPointMarker.Visibility = Visibility.Visible;
            _context.InPointMarker.Height = trackHeight;
            Canvas.SetLeft(_context.InPointMarker, inX - 1);
        }
        else
        {
            _context.InPointMarker.Visibility = Visibility.Collapsed;
        }

        if (hasUsableTrack && hasUsableDuration && outPoint is TimeSpan outPt)
        {
            outPtVal = outPt;
            var outX = Math.Clamp(outPt.TotalSeconds / bufferDuration.TotalSeconds * trackWidth, 0, trackWidth);
            _context.OutPointMarker.Visibility = Visibility.Visible;
            _context.OutPointMarker.Height = trackHeight;
            Canvas.SetLeft(_context.OutPointMarker, outX - 1);
        }
        else
        {
            _context.OutPointMarker.Visibility = Visibility.Collapsed;
        }

        if (inPtVal is TimeSpan inVal && outPtVal is TimeSpan outVal && hasUsableTrack && hasUsableDuration)
        {
            var inFrac = inVal.TotalSeconds / bufferDuration.TotalSeconds;
            var outFrac = outVal.TotalSeconds / bufferDuration.TotalSeconds;
            var selLeft = Math.Clamp(inFrac * trackWidth, 0, trackWidth);
            var selRight = Math.Clamp(outFrac * trackWidth, 0, trackWidth);
            _context.SelectionRegion.Visibility = Visibility.Visible;
            _context.SelectionRegion.Height = trackHeight;
            _context.SelectionRegion.Width = Math.Max(0, selRight - selLeft);
            Canvas.SetLeft(_context.SelectionRegion, selLeft);
        }
        else
        {
            _context.SelectionRegion.Visibility = Visibility.Collapsed;
        }
    }

    private static bool IsUsableTrackDimension(double value)
        => double.IsFinite(value) && value > 0;

    private static bool IsUsableDuration(TimeSpan value)
        => double.IsFinite(value.TotalSeconds) && value > TimeSpan.Zero;
}
