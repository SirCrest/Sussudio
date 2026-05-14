using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

public partial class DeviceService
{
    public void BeginBackgroundFormatProbe(CaptureDevice device, long requestId = 0)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (string.IsNullOrWhiteSpace(device.Id) || string.IsNullOrWhiteSpace(device.Name))
        {
            return;
        }

        _ = RunBackgroundFormatProbeAsync(device.Id, device.Name, requestId);
    }

    private async Task RunBackgroundFormatProbeAsync(string deviceId, string deviceName, long requestId)
    {
        await _formatProbeGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var probeDevice = new CaptureDevice
            {
                Id = deviceId,
                Name = deviceName,
                NativeXuInterfacePath = ResolveNativeXuInterfacePath(deviceId)
            };

            var hasEnumeratedFormats = await QuerySupportedFormatsAsync(probeDevice).ConfigureAwait(false);
            var snapshot = CloneFormats(probeDevice.SupportedFormats);
            if (hasEnumeratedFormats)
            {
                TrySaveFormatCache(probeDevice, snapshot);
            }

            FormatProbeCompleted?.Invoke(
                this,
                new DeviceFormatProbeCompletedEventArgs(
                    deviceId,
                    deviceName,
                    snapshot,
                    probeDevice.IsHdrCapable,
                    hasEnumeratedFormats,
                    requestId,
                    Error: null));
        }
        catch (Exception ex)
        {
            Logger.Log($"Background format probe failed for {deviceName}: {ex.Message}");
            FormatProbeCompleted?.Invoke(
                this,
                new DeviceFormatProbeCompletedEventArgs(
                    deviceId,
                    deviceName,
                    Array.Empty<MediaFormat>(),
                    IsHdrCapable: false,
                    HasEnumeratedFormats: false,
                    requestId,
                    Error: ex.Message));
        }
        finally
        {
            _formatProbeGate.Release();
        }
    }

    private async Task<bool> QuerySupportedFormatsAsync(CaptureDevice device)
    {
        try
        {
            var uniqueFormats = new HashSet<MediaFormat>();
            device.IsHdrCapable = false;

            var nativeFormats = await MfDeviceEnumerator.ProbeVideoFormatsAsync(device.Id).ConfigureAwait(false);
            foreach (var nativeFormat in nativeFormats)
            {
                var width = nativeFormat.Width;
                var height = nativeFormat.Height;
                if (width == 0 || height == 0)
                {
                    continue;
                }

                var rawFps = nativeFormat.FrameRate;
                if (rawFps <= 0 &&
                    nativeFormat.FrameRateNumerator > 0 &&
                    nativeFormat.FrameRateDenominator > 0)
                {
                    rawFps = (double)nativeFormat.FrameRateNumerator / nativeFormat.FrameRateDenominator;
                }

                if (rawFps <= 0)
                {
                    continue;
                }

                var pixelFormat = NormalizePixelFormat(nativeFormat.PixelFormat);
                var (numerator, denominator, normalizedFps) = NormalizeFrameRate(rawFps);
                var isHdr = MediaFormat.IsHdrPixelFormat(pixelFormat) || MediaFormat.IsTrue10BitPixelFormat(pixelFormat);
                if (isHdr)
                {
                    device.IsHdrCapable = true;
                }

                uniqueFormats.Add(new MediaFormat
                {
                    Width = width,
                    Height = height,
                    FrameRate = normalizedFps,
                    FrameRateNumerator = numerator,
                    FrameRateDenominator = denominator,
                    PixelFormat = pixelFormat,
                    IsHdr = isHdr
                });
            }

            var sortedFormats = uniqueFormats
                .OrderByDescending(f => (long)f.Width * f.Height)
                .ThenByDescending(f => f.FrameRate)
                .ThenBy(f => MediaFormat.GetPixelFormatPriority(f.PixelFormat))
                .ToList();

            if (sortedFormats.Count == 0)
            {
                Logger.Log($"MF source-reader format discovery produced no rows for {device.Name}.");
            }

            device.SupportedFormats.Clear();
            foreach (var format in sortedFormats)
            {
                device.SupportedFormats.Add(format);
            }

            return sortedFormats.Count > 0;
        }
        catch (Exception ex)
        {
            Logger.Log($"Format discovery failed for {device.Name}: {ex.Message}");
            device.SupportedFormats.Clear();
            device.IsHdrCapable = false;
            return false;
        }
    }

    private static (uint Numerator, uint Denominator, double Fps) NormalizeFrameRate(double fps)
    {
        if (fps <= 0)
        {
            return (30, 1, 30);
        }

        var rounded = Math.Round(fps);
        if (Math.Abs(fps - rounded) <= 0.01 && rounded > 0)
        {
            return ((uint)rounded, 1, rounded);
        }

        var ntscBase = Math.Round(fps * 1001.0 / 1000.0);
        var ntscFps = ntscBase * 1000.0 / 1001.0;
        if (ntscBase > 0 && Math.Abs(fps - ntscFps) <= 0.03)
        {
            return ((uint)(ntscBase * 1000), 1001, ntscFps);
        }

        var numerator = (uint)Math.Round(fps * 1000.0);
        return (numerator, 1000, numerator / 1000.0);
    }

    private static string NormalizePixelFormat(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "UNKNOWN";
        }

        var token = raw.Trim();
        if (token.Equals("yuyv422", StringComparison.OrdinalIgnoreCase))
        {
            return "YUY2";
        }

        if (token.Equals("uyvy422", StringComparison.OrdinalIgnoreCase))
        {
            return "UYVY";
        }

        if (token.Equals("p010le", StringComparison.OrdinalIgnoreCase))
        {
            return "P010";
        }

        return token.ToUpperInvariant();
    }
}
