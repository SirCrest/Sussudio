using System;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

public partial class DeviceService
{
    private static int GetDevicePriority(DeviceCandidate candidate)
    {
        var maxPixelCount = candidate.Device.SupportedFormats
            .Select(f => (long)f.Width * f.Height)
            .DefaultIfEmpty(0)
            .Max();
        var maxFrameRate = candidate.Device.SupportedFormats
            .Select(f => f.FrameRate)
            .DefaultIfEmpty(0)
            .Max();

        var priority = 0;
        if (candidate.PreferredByName) priority += 400;
        if (candidate.LikelyByCapability) priority += 200;
        if (candidate.LikelyByName) priority += 100;
        if (candidate.HasEnumeratedFormats) priority += 50;
        priority += (int)Math.Min(maxFrameRate, 120);
        priority += (int)Math.Min(maxPixelCount / 500_000, 40);
        return priority;
    }

    private static bool LooksLikeHighBandwidthCapture(CaptureDevice device)
    {
        foreach (var format in device.SupportedFormats)
        {
            if ((format.Width >= 1920 && format.FrameRate >= 50) ||
                (format.Width >= 2560 && format.FrameRate >= 30) ||
                (format.Width >= 3840 && format.FrameRate >= 24))
            {
                return true;
            }
        }

        return false;
    }
}
